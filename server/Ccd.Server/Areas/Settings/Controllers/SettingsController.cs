using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Settings;

[ApiController]
[Route("/api/v1/settings")]
public class SettingsController : ControllerBaseExtended
{
    private readonly CcdContext _context;
    private readonly IMapper _mapper;
    private readonly SettingsService _settingsService;


    public SettingsController(CcdContext context, SettingsService settingsService, IMapper mapper)
    {
        _context = context;
        _settingsService = settingsService;
        _mapper = mapper;
    }

    [HttpGet]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<SettingsResponse>> GetSettings()
    {
        var settings = await _settingsService.GetSettingsApi() ?? throw new NotFoundException();
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<SettingsResponse>> Update(
        [FromBody] SettingsUpdateRequest model
    )
    {
        // check that user is superadmin
        if (this.UserId != Ccd.Server.Users.User.SYSTEM_USER.Id)
        {
            throw new UnauthorizedException("Only superadmin can change settings");
        }
        
        await _settingsService.UpdateSettingsApi(model);

        var result = await _settingsService.GetSettingsApi();

        return Ok(result);
    }

    [HttpPost("initial-setup")]
    public async Task<ActionResult> InitialSetup([FromBody] InitialSetupRequest request)
    {
        // Check that user is superadmin
        if (this.UserId != Ccd.Server.Users.User.SYSTEM_USER.Id)
        {
            throw new UnauthorizedException("Only superadmin can perform initial setup");
        }

        // Check if organizations already exist
        var hasOrganizations = await _context.Organizations.AnyAsync();
        if (hasOrganizations)
        {
            throw new BadRequestException("Initial setup has already been completed");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Update deployment settings
            var settings = await _context.Settings.FirstAsync();
            settings.DeploymentName = request.DeploymentName;
            settings.DeploymentCountry = request.DeploymentCountry;
            settings.AdminLevel1Name = request.AdminLevel1Name;
            settings.AdminLevel2Name = request.AdminLevel2Name;
            settings.AdminLevel3Name = request.AdminLevel3Name;
            settings.AdminLevel4Name = request.AdminLevel4Name;
            settings.MetabaseUrl = request.MetabaseUrl;
            settings.FundingSources = request.FundingSources ?? new List<string>();
            _context.Settings.Update(settings);
            await _context.SaveChangesAsync();

            // 2. Create organization
            var organization = new Organization
            {
                Name = request.OrganizationName,
                IsMpcaActive = request.IsMpcaActive,
                IsWashActive = request.IsWashActive,
                IsShelterActive = request.IsShelterActive,
                IsFoodAssistanceActive = request.IsFoodAssistanceActive,
                IsLivelihoodsActive = request.IsLivelihoodsActive,
                IsProtectionActive = request.IsProtectionActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // 3. Create user
            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ActivatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.Password = AuthenticationHelper.HashPassword(user, request.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 4. Link user to organization with admin role
            var userOrganization = new UserOrganization
            {
                UserId = user.Id,
                OrganizationId = organization.Id,
                Role = "admin",
                Permissions = request.Permissions ?? new List<string>()
            };
            _context.UserOrganizations.Add(userOrganization);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                organizationId = organization.Id,
                userId = user.Id
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}