using System;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Areas.Users.Controllers.ControllerModels;
using Ccd.Server.Authentication;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Users;

[ApiController]
[Route("/api/v1/users")]
public class UserController : ControllerBaseExtended
{
    private readonly AuthenticationService _authenticationService;
    private readonly CcdContext _context;
    private readonly DateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;
    private readonly UserService _userService;
    private readonly OrganizationEmailDomainService _emailDomainService;
    private readonly ChangeLogService _changeLog;

    public UserController(
        UserService userService,
        IMapper mapper,
        CcdContext context,
        DateTimeProvider dateTimeProvider,
        AuthenticationService authenticationService,
        OrganizationEmailDomainService emailDomainService,
        ChangeLogService changeLog
    )
    {
        _emailDomainService = emailDomainService;
        _changeLog = changeLog;
        _userService = userService;
        _mapper = mapper;
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _authenticationService = authenticationService;
    }

    [HttpGet]
    [PermissionLevel(UserRole.UserAdmin)]
    public async Task<ActionResult<PagedApiResponse<UserResponse>>> GetAll(
        [FromQuery] RequestParameters requestParams,
        [FromQuery] Guid? organizationId
    )
    {
        var users = await _userService.GetUsersApi(organizationId, requestParams);
        return Ok(users);
    }

    [HttpGet("{id}")]
    [PermissionLevel(UserRole.UserAdmin)]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
        var user =
            await _userService.GetUserApi(null, id) ?? throw new NotFoundException();

        return Ok(user);
    }

    [HttpPost]
    [PermissionLevel(UserRole.UserAdmin)]
    public async Task<ActionResult<UserResponse>> Add([FromBody] UserAddRequest model)
    {
        if (model.OrganizationId == Guid.Empty)
            throw new BadRequestException("OrganizationId is required.");

        var existingUser = await _userService.GetUserByEmail(model.Email);
        if (existingUser != null)
            throw new BadRequestException("User with this email already exists");

        if (!UserRole.IsValidRole(model.Role))
            throw new BadRequestException("Invalid role");
        await EnsureRoleAllowed(model.OrganizationId, null, model.Role);

        // Allowed email domains: if the organisation has a list, the new user's
        // email must be on one of its domains (exact match). Organisations
        // without a list are unrestricted. Email and organisation can't be
        // changed after creation, so this is the only place the rule applies.
        var allowedDomains = await _emailDomainService.GetAllowedDomains(model.OrganizationId);
        if (!EmailDomainRules.IsEmailAllowed(model.Email, allowedDomains))
            throw new BadRequestException(
                $"The email domain '{EmailDomainRules.DomainOfEmail(model.Email)}' is not allowed for this organisation. "
                + $"Allowed domains: {string.Join(", ", allowedDomains)}."
            );

        var user = _mapper.Map<User>(model);

        user = await _userService.AddUser(user);
        await _userService.SetOrganizationRole(user.Id, model.OrganizationId, model.Role, model.Permissions);

        await _changeLog.Add(
            ChangeLogEntityType.User,
            user.Id,
            ChangeLogAction.Created,
            UserId,
            newValue: $"Role: {RoleLabel(model.Role)}; Permissions: {PermissionList(model.Permissions)}",
            organizationId: model.OrganizationId,
            subjectEmail: user.Email
        );
        await _context.SaveChangesAsync();

        // The user is kept even if the email fails (e.g. email service down);
        // the Admin is told and can use "Resend invitation".
        var invitationSent = await _authenticationService.InviteUser(user);

        var result = await _userService.GetUserApi(model.OrganizationId, user.Id);
        result.InvitationEmailSent = invitationSent;

        return Created("", result);
    }

    /// <summary>
    /// Sends the invitation email again (e.g. after an email outage or an
    /// expired link). Password sign-in: only while the user has not set a
    /// password yet; each resend issues a new link.
    /// </summary>
    [HttpPost("{id}/resend-invitation")]
    [PermissionLevel(UserRole.UserAdmin)]
    public async Task<ActionResult> ResendInvitation(Guid id)
    {
        var user = await _userService.GetUserById(id) ?? throw new NotFoundException();
        if (user.Id == Ccd.Server.Users.User.SYSTEM_USER.Id)
            throw new BadRequestException("Invitations cannot be sent to the system account.");

        await _authenticationService.ResendInvitation(user);

        var membership = await _context.UserOrganizations.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == user.Id);
        await _changeLog.Add(
            ChangeLogEntityType.User,
            user.Id,
            ChangeLogAction.InvitationResent,
            UserId,
            organizationId: membership?.OrganizationId,
            subjectEmail: user.Email
        );
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("{id}")]
    [PermissionLevel(UserRole.UserAdmin)]
    public async Task<ActionResult<UserResponse>> Update(
        [FromBody] UserUpdateRequest model,
        Guid id
    )
    {
        var user = await _userService.GetUserById(id) ?? throw new NotFoundException();
        var userOrganization = await _context.UserOrganizations.FirstOrDefaultAsync(
            e => e.UserId == user.Id && e.OrganizationId == OrganizationId
        ) ?? throw new NotFoundException();
        if (!UserRole.IsValidRole(model.Role))
            throw new BadRequestException("Invalid role");
        await EnsureRoleAllowed(userOrganization.OrganizationId, userOrganization.Role, model.Role);

        var before = Snapshot(user, userOrganization);

        userOrganization.Permissions = model.Permissions;
        userOrganization.Role = model.Role;

        _mapper.Map(model, user);

        if (model.Password != null)
        {
            PasswordPolicyValidator.Validate(model.Password);
            user.Password = AuthenticationHelper.HashPassword(user, model.Password);
        }

        await LogUserChanges(user, userOrganization, before, passwordChanged: model.Password != null);
        user = await _userService.UpdateUser(user);

        var result = await _userService.GetUserApi(OrganizationId, user.Id);

        return Ok(result);
    }

    [HttpPatch("{id}")]
    [PermissionLevel(UserRole.UserAdmin)]
    public async Task<ActionResult<UserResponse>> Patch(Guid id, [FromBody] UserPatchRequest model)
    {
        var user = await _userService.GetUserById(id) ?? throw new NotFoundException();
        var membership = await _context.UserOrganizations.FirstOrDefaultAsync(e => e.UserId == user.Id);
        var before = Snapshot(user, membership);
        model.Patch(user);

        if (model.Role != null && UserRole.IsValidRole(model.Role))
        {
            var userTenant = await _context.UserOrganizations.FirstOrDefaultAsync(
                e => e.UserId == user.Id
            ) ?? throw new NotFoundException();
            await EnsureRoleAllowed(userTenant.OrganizationId, userTenant.Role, model.Role);
            userTenant.Role = model.Role;
            _context.UserOrganizations.Update(userTenant);
        }


        if (model.Permissions != null)
        {
            var userTenant = await _context.UserOrganizations.FirstOrDefaultAsync(
                e => e.UserId == user.Id
            ) ?? throw new NotFoundException();
            userTenant.Permissions = model.Permissions;
            _context.UserOrganizations.Update(userTenant);
        }

        if (model.Password != null)
        {
            PasswordPolicyValidator.Validate(model.Password);
            user.Password = AuthenticationHelper.HashPassword(user, model.Password);
        }
        user.ActivatedAt = _dateTimeProvider.UtcNow;

        if (membership != null)
            await LogUserChanges(user, membership, before, passwordChanged: model.Password != null);
        var newUser = await _userService.UpdateUser(user);
        var response = await _userService.GetUserApi(id: newUser.Id);

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [PermissionLevel(UserRole.UserAdmin)]
    public async Task<ActionResult<UserResponse>> Delete(Guid id)
    {
        var user = await _userService.GetUserById(id) ?? throw new NotFoundException();

        var result = await _userService.GetUserApi(OrganizationId, user.Id);

        var membership = await _context.UserOrganizations.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == user.Id);
        await _changeLog.Add(
            ChangeLogEntityType.User,
            user.Id,
            ChangeLogAction.Deleted,
            UserId,
            oldValue: membership == null ? null : $"Role: {RoleLabel(membership.Role)}",
            organizationId: membership?.OrganizationId,
            subjectEmail: user.Email
        );

        await _userService.DeleteUser(user);

        return Ok(result);
    }

    [HttpGet("me")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<UserMeResponse>> GetCurrentUser()
    {
        var result = await _userService.GetUserApi(OrganizationId, UserId);
        var user = _mapper.Map<UserMeResponse>(result);

        return Ok(user);
    }

    [HttpPut("me")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<UserMeResponse>> UpdateCurrentUser(UserUpdateMeRequest model)
    {
        var user =
            await _userService.GetUserById(UserId)
            ?? throw new NotFoundException();
        model.Patch(user);

        if (model.Password != null)
        {
            PasswordPolicyValidator.Validate(model.Password);
            user.Password = AuthenticationHelper.HashPassword(user, model.Password);
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return await GetCurrentUser();
    }

    // ---------------------------------------------------------------- change-log helpers

    private record UserSnapshot(string Name, string Role, string Permissions);

    private static UserSnapshot Snapshot(User user, UserOrganization membership) =>
        new($"{user.FirstName} {user.LastName}".Trim(), membership?.Role, PermissionList(membership?.Permissions));

    /// <summary>Logs a role change (own entry) and any other field changes (one entry).</summary>
    private async Task LogUserChanges(User user, UserOrganization membership, UserSnapshot before, bool passwordChanged)
    {
        if (before.Role != membership.Role)
            await _changeLog.Add(
                ChangeLogEntityType.UserRole,
                user.Id,
                ChangeLogAction.RoleChanged,
                UserId,
                oldValue: RoleLabel(before.Role),
                newValue: RoleLabel(membership.Role),
                organizationId: membership.OrganizationId,
                subjectEmail: user.Email
            );

        var changes = ChangeLogService.Describe(
            ("Name", before.Name, $"{user.FirstName} {user.LastName}".Trim()),
            ("Permissions", before.Permissions, PermissionList(membership.Permissions))
        );
        if (passwordChanged)
            changes = changes == null ? "Password changed" : changes + "; Password changed";

        if (changes != null)
            await _changeLog.Add(
                ChangeLogEntityType.User,
                user.Id,
                ChangeLogAction.Updated,
                UserId,
                newValue: changes,
                organizationId: membership.OrganizationId,
                subjectEmail: user.Email
            );
    }

    private static string PermissionList(List<string> permissions)
    {
        var list = ChangeLogService.List(permissions);
        return list == "" ? "(none)" : list;
    }

    private static string RoleLabel(string role) => UserRole.Label(role);

    /// <summary>
    /// The role must be one the user's organisation allows (UserRole.AssignableRoles):
    /// User Admin only in the USER_ADMIN_ORGANIZATION, Organisation Admin only in
    /// the ORGANIZATION_ADMIN_ORGANIZATION, Admin only granted by an Admin.
    /// Also applies when an existing role is kept, so a role that no longer
    /// fits has to be corrected when the user is edited.
    /// </summary>
    private async Task EnsureRoleAllowed(Guid organizationId, string currentTargetRole, string newRole)
    {
        var organization = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId)
            ?? throw new BadRequestException("Organisation not found.");

        var allowed = UserRole.AssignableRoles(
            CurrentRole,
            organization.AllowsUserAdmin,
            organization.AllowsOrganizationAdmin,
            currentTargetRole
        );
        if (allowed.Contains(newRole))
            return;

        if (newRole == UserRole.Admin)
            throw new ForbiddenException("Only admins can assign the admin role.");

        throw new BadRequestException(
            $"The role '{RoleLabel(newRole)}' can't be assigned to users of '{organization.Name}'. "
            + $"Allowed roles: {string.Join(", ", allowed.Select(RoleLabel))}."
        );
    }
}
