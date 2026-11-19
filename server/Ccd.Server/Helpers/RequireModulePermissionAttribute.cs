using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ccd.Server.Helpers;

/// <summary>
/// Requires a module permission (Booking, Deduplication, Referral) in the
/// caller's current organisation. Previously these permissions only hid menu
/// items; this enforces them on the server.
///
/// Admins and User Admins keep access to every module (as before). Everyone
/// else, including Organisation Admins, needs the permission ticked on their
/// user. Roles and permissions come from the database (PermissionLevelMiddleware).
/// Use together with [PermissionLevel(UserRole.User)], which checks membership.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireModulePermissionAttribute : ActionFilterAttribute
{
    private readonly string _permission;

    public RequireModulePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public static bool BypassesModulePermissions(string role) =>
        role is UserRole.Admin or UserRole.UserAdmin;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var items = context.HttpContext.Items;
        var userId = (Guid?)items["UserId"];
        if (!userId.HasValue)
            throw new UnauthorizedException("Unauthorized. Please provide valid JWT token.");

        var organizationId = (Guid?)items["OrganizationId"];
        var role = PermissionLevelAttribute.getOrganizationRole((string)items["OrganizationRoles"], organizationId);

        if (!BypassesModulePermissions(role))
        {
            var permissions = organizationId != null
                && items[PermissionLevelMiddleware.OrganizationPermissionsKey] is Dictionary<Guid, List<string>> all
                && all.TryGetValue(organizationId.Value, out var list)
                    ? list
                    : new List<string>();

            if (!permissions.Contains(_permission))
                throw new ForbiddenException($"You don't have permission to use this module ({_permission}).");
        }

        await base.OnActionExecutionAsync(context, next);
    }
}
