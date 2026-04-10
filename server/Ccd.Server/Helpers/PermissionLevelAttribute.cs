using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ccd.Server.Helpers;

/// <summary>
///     This attribute can be used on any controller endpoint and will check if the caller has required
///     permission level using the organization data prepared by the PermissionLevelMiddleware
/// </summary>
public class PermissionLevelAttribute : ActionFilterAttribute
{
    private readonly string _requiredLevel;
    private readonly bool _allowSuperAdmin;

    public PermissionLevelAttribute(string requiredLevel, bool allowSuperAdmin = false)
    {
        _requiredLevel = requiredLevel;
        _allowSuperAdmin = allowSuperAdmin;
    }

    private bool isAuthorizedRole(string userRole, string requiredRole)
    {
        if (requiredRole == UserRole.Admin && userRole == UserRole.Admin)
            return true;
        if (
            requiredRole == UserRole.User
            && (userRole == UserRole.Admin || userRole == UserRole.User)
        )
            return true;

        return false;
    }

    private CcdContext getCcdContext(ActionExecutingContext context)
    {
        return (CcdContext)context.HttpContext.RequestServices.GetService(typeof(CcdContext));
    }

    public static string getOrganizationRole(string organizationRoles, Guid? organizationId)
    {
        if (string.IsNullOrEmpty(organizationRoles))
            return "";
        if (!organizationId.HasValue)
            return "";

        var roles = Json.Deserialize<Dictionary<string, string>>(organizationRoles);

        return roles.ContainsKey(organizationId.Value.ToString()) ? roles[organizationId.Value.ToString()] : "";
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        var organizationId = (Guid?)context.HttpContext.Items["OrganizationId"];
        var userId = (Guid?)context.HttpContext.Items["UserId"];
        var organizationRoles = (string)context.HttpContext.Items["OrganizationRoles"];

        if (!userId.HasValue) throw new UnauthorizedException("Unauthorized. Please provide valid JWT token.");

        if (userId == User.SYSTEM_USER.Id)
        {
            if (_allowSuperAdmin)
            {
                await base.OnActionExecutionAsync(context, next);
                return;
            }
            throw new ForbiddenException("Superadmin access is not allowed on this endpoint.");
        }

        var role = getOrganizationRole(organizationRoles, organizationId);
        if (!isAuthorizedRole(role, _requiredLevel))
            throw new ForbiddenException(
                $"Unauthorized. Authorization of '{_requiredLevel}' level is required. You have '{role}' level."
            );

        await base.OnActionExecutionAsync(context, next);
    }
}