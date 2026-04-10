using System;
using System.Linq;
using Ccd.Server.Data;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;

namespace Ccd.Server.Helpers;

public class ControllerBaseExtended : ControllerBase
{
    protected Guid OrganizationId => getOrganizationId();

    protected Guid? OrganizationIdOrNull => getOrganizationIdOrNull();

    protected Organization Organization =>
        getCcdServerContext().Organizations.FirstOrDefault(e => e.Id == OrganizationId);

    protected Guid UserId => getUserId();

    protected bool IsSuperAdmin => getIsSuperAdmin();

    protected Guid MemberId => getCurrentMemberId();

    protected bool IsUser => getIsUser();

    protected bool IsAdmin => getIsAdmin();

    private Guid? getOrganizationIdOrNull()
    {
        return (Guid?)HttpContext.Items["OrganizationId"];
    }

    private Guid getOrganizationId()
    {
        var userId = (Guid?)HttpContext.Items["UserId"];
        if (userId == Users.User.SYSTEM_USER.Id) return new Guid("00000000-0000-0000-0000-000000000001");

        var organizationId = getOrganizationIdOrNull();
        if (organizationId == null)
            throw new UnauthorizedException("Organization ID not found.");
        return organizationId.Value;
    }

    private bool getIsAdmin()
    {
        var organizationId = getOrganizationIdOrNull();

        if (organizationId == null)
            throw new UnauthorizedException("Organization ID not found.");

        var organizationRoles = (string)HttpContext.Items["OrganizationRoles"];

        var role = PermissionLevelAttribute.getOrganizationRole(organizationRoles, organizationId);

        return role == UserRole.Admin;
    }

    private CcdContext getCcdServerContext()
    {
        return (CcdContext)HttpContext.RequestServices.GetService(typeof(CcdContext));
    }

    private Guid getUserId()
    {
        var userId = (Guid?)HttpContext.Items["UserId"];
        if (userId == null)
            throw new UnauthorizedException("User ID not found.");
        return userId.Value;
    }

    private bool getIsSuperAdmin()
    {
        var userId = (Guid?)HttpContext.Items["UserId"];
        return userId.HasValue && userId.Value == Users.User.SYSTEM_USER.Id;
    }

    private Guid getCurrentMemberId()
    {
        var memberId = (Guid?)HttpContext.Items["MemberId"];
        if (memberId == null)
            throw new UnauthorizedException("Member ID not found.");
        return memberId.Value;
    }

    private bool getIsUser()
    {
        return HttpContext.Items["UserId"] != null;
    }
}