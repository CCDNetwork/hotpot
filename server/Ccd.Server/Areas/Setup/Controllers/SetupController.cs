using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ccd.Server.Setup;

// TEMPORARY bootstrap endpoint. Creates the initial organization + admin user so the
// first person can sign in via B2C (which binds to a DB user purely by email).
// This is intentionally public and unauthenticated. Trigger it ONCE, then delete this file.
[ApiController]
[Route("/api/v1/setup")]
public class SetupController : ControllerBaseExtended
{
    // Values to bootstrap. The email MUST match the address used to authenticate at B2C.
    private const string OrganizationName = "INIT";
    private const string UserEmail = "admin@init.hr";
    private const string UserFirstName = "Admin";
    private const string UserLastName = "Admin";

    private readonly OrganizationService _organizationService;
    private readonly UserService _userService;

    public SetupController(OrganizationService organizationService, UserService userService)
    {
        _organizationService = organizationService;
        _userService = userService;
    }

    [HttpPost("bootstrap")]
    [AllowAnonymous]
    public async Task<ActionResult> Bootstrap()
    {
        // Single-use guard: if the bootstrap user already exists, this has already run. Fail.
        var existingUser = await _userService.GetUserByEmail(UserEmail);
        if (existingUser != null)
            throw new ConflictException("Setup has already been run.");

        var organization = await _organizationService.AddOrganization(
            new Organization { Name = OrganizationName }
        );

        var user = await _userService.AddUser(
            new User
            {
                Email = UserEmail,
                FirstName = UserFirstName,
                LastName = UserLastName,
                ActivatedAt = DateTime.UtcNow
            }
        );

        await _userService.SetOrganizationRole(
            user.Id,
            organization.Id,
            UserRole.Admin,
            new List<string>
            {
                UserPermission.Referral,
                UserPermission.Deduplication,
                UserPermission.Booking
            }
        );

        return Ok(
            new
            {
                organizationId = organization.Id,
                userId = user.Id,
                email = user.Email
            }
        );
    }
}
