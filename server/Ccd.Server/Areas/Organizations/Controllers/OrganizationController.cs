using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
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

    public OrganizationController(OrganizationService organizationService, IMapper mapper)
    {
        _organizationService = organizationService;
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
        var organization = _mapper.Map<Organization>(model);
        var newOrganizaiton = await _organizationService.AddOrganization(organization);

        var activities = new List<Activity>();
        foreach (var activity in model.Activities)
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

        if (model.Name == null)
            model.Name = organization.Name;

        _mapper.Map(model, organization);

        await _organizationService.UpdateOrganization(organization);

        var activitiesToUpdate = new List<Activity>();
        var activitiesToAdd = new List<Activity>();

        foreach (var activity in model.Activities)
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


        var result = await _organizationService.GetOrganizationApi(id);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<OrganizationResponse>> Delete(Guid id)
    {
        var organization = await _organizationService.GetOrganizationById(id) ?? throw new NotFoundException();

        var result = await _organizationService.GetOrganizationApi(organization.Id);

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
}
