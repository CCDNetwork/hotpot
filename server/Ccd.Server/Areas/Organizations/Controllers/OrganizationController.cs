using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using System.Collections.Generic;

namespace Ccd.Server.Organizations;

[ApiController]
[Route("/api/v1/organizations")]
public class OrganizationController : ControllerBaseExtended
{
    private readonly IMapper _mapper;
    private readonly OrganizationService _organizationService;
    private readonly OrganizationEmailDomainService _emailDomainService;
    private readonly ChangeLogService _changeLog;
    private readonly CcdContext _context;

    public OrganizationController(
        OrganizationService organizationService,
        OrganizationEmailDomainService emailDomainService,
        ChangeLogService changeLog,
        CcdContext context,
        IMapper mapper
    )
    {
        _organizationService = organizationService;
        _emailDomainService = emailDomainService;
        _changeLog = changeLog;
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<PagedApiResponse<OrganizationResponse>>> GetAll(
        [FromQuery] RequestParameters requestParams
    )
    {
        var organizations = await _organizationService.GetOrganizationsApi(requestParams);
        return Ok(organizations);
    }

    [HttpGet("{id}")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<OrganizationResponse>> GetOrganization(Guid id)
    {
        var organization = await _organizationService.GetOrganizationApi(id);

        if (organization == null)
            throw new NotFoundException();

        return Ok(organization);
    }


    [HttpPost]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<OrganizationResponse>> Add([FromBody] OrganizationAddRequest model)
    {
        // Validate the allowed email domains before anything is saved, so an
        // invalid domain never leaves a half-created organisation behind.
        var emailDomains = _emailDomainService.ValidateForNewOrganization(model.EmailDomains);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var organization = _mapper.Map<Organization>(model);
        var newOrganizaiton = await _organizationService.AddOrganization(organization);

        var activities = new List<Activity>();
        foreach (var activity in model.Activities ?? new List<ActivityAddRequest>())
        {
            var isValid = ServiceType.IsValid(activity.ServiceType);
            if (isValid)
            {
                var act = new Activity
                {
                    Title = activity.Title,
                    ServiceType = activity.ServiceType,
                    OrganizationId = newOrganizaiton.Id
                };
                activities.Add(act);
            }
        }
        await _organizationService.AddActivities(activities);

        await _changeLog.Add(
            ChangeLogEntityType.Organization,
            newOrganizaiton.Id,
            ChangeLogAction.Created,
            UserId,
            newValue: Summary(newOrganizaiton, activities.Select(a => a.Title)),
            organizationId: newOrganizaiton.Id,
            organizationName: newOrganizaiton.Name
        );

        if (emailDomains.Count > 0)
            await _emailDomainService.AddForNewOrganization(newOrganizaiton, emailDomains, UserId);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var result = await _organizationService.GetOrganizationApi(newOrganizaiton.Id);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<OrganizationResponse>> Update(
        [FromBody] OrganizationUpdateRequest model,
        Guid id
    )
    {
        var organization = await _organizationService.GetOrganizationById(id) ?? throw new NotFoundException();

        // Snapshot for the change log.
        var oldName = organization.Name;
        var oldServices = Services(organization);
        var oldActivities = await ActivityTitles(organization.Id);

        if (model.Name == null)
            model.Name = organization.Name;

        _mapper.Map(model, organization);

        await _organizationService.UpdateOrganization(organization);

        var activitiesToUpdate = new List<Activity>();
        var activitiesToAdd = new List<Activity>();

        foreach (var activity in model.Activities ?? new List<ActivityUpdateRequest>())
        {
            var isValid = ServiceType.IsValid(activity.ServiceType);
            if (isValid)
            {
                if (activity.Id != null)
                {
                    var act = new Activity
                    {
                        Id = (Guid)activity.Id,
                        Title = activity.Title,
                        ServiceType = activity.ServiceType,
                        OrganizationId = organization.Id
                    };
                    activitiesToUpdate.Add(act);
                }
                else
                {
                    var act = new Activity
                    {
                        Title = activity.Title,
                        ServiceType = activity.ServiceType,
                        OrganizationId = organization.Id
                    };
                    activitiesToAdd.Add(act);
                }
            }
        }
        var activitiesToDelete = await _organizationService.GetActivitiesToDelete(organization.Id, activitiesToUpdate);
        await _organizationService.DeleteActivities(activitiesToDelete);
        await _organizationService.UpdateActivities(activitiesToUpdate);
        await _organizationService.AddActivities(activitiesToAdd);

        var changes = ChangeLogService.Describe(
            ("Name", oldName, organization.Name),
            ("Services", oldServices, Services(organization)),
            ("Activities", oldActivities, await ActivityTitles(organization.Id))
        );
        if (changes != null)
        {
            await _changeLog.Add(
                ChangeLogEntityType.Organization,
                organization.Id,
                ChangeLogAction.Updated,
                UserId,
                newValue: changes,
                organizationId: organization.Id,
                organizationName: organization.Name
            );
            await _context.SaveChangesAsync();
        }

        var result = await _organizationService.GetOrganizationApi(id);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<OrganizationResponse>> Delete(Guid id)
    {
        var organization = await _organizationService.GetOrganizationById(id) ?? throw new NotFoundException();

        var result = await _organizationService.GetOrganizationApi(organization.Id);

        // Domain rows reference the organisation: remove them (logged) first.
        await _emailDomainService.RemoveAllForDeletedOrganization(organization, UserId);
        await _changeLog.Add(
            ChangeLogEntityType.Organization,
            organization.Id,
            ChangeLogAction.Deleted,
            UserId,
            oldValue: organization.Name,
            organizationId: organization.Id,
            organizationName: organization.Name
        );
        await _organizationService.DeleteOrganization(organization);

        return Ok(result);
    }

    [HttpGet("me")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<OrganizationResponse>> GetCurrentOrganization()
    {
        return await _organizationService.GetOrganizationApi(this.OrganizationId);
    }

    [HttpPut("me")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<OrganizationResponse>> UpdateCurrentOrganization(OrganizationUpdateRequest model)
    {
        return await Update(model, this.OrganizationId);
    }

    // ---------------------------------------------------------------- change-log helpers

    private static string Services(Organization o) => ChangeLogService.List(new[]
    {
        o.IsMpcaActive ? "MPCA" : null,
        o.IsWashActive ? "WASH" : null,
        o.IsShelterActive ? "Shelter" : null,
        o.IsFoodAssistanceActive ? "Food Assistance" : null,
        o.IsLivelihoodsActive ? "Livelihoods" : null,
        o.IsProtectionActive ? "Protection" : null,
        o.IsIncentivePaymentsActive ? "Incentive Payments" : null,
    });

    private async Task<string> ActivityTitles(Guid organizationId) =>
        ChangeLogService.List(await _context.Activities.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId)
            .Select(a => a.Title)
            .ToListAsync());

    private static string Summary(Organization o, IEnumerable<string> activities)
    {
        var services = Services(o);
        var activityList = ChangeLogService.List(activities);
        return $"Services: {(services == "" ? "(none)" : services)}"
               + (activityList == "" ? "" : $"; Activities: {activityList}");
    }
}
