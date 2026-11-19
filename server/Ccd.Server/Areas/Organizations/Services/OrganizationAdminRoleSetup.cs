using System;
using System.Linq;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ccd.Server.Organizations;

/// <summary>
/// Marks which organisation's users can hold the User Admin and the
/// Organisation Admin role, from the deployment settings
/// USER_ADMIN_ORGANIZATION and ORGANIZATION_ADMIN_ORGANIZATION. Runs at startup.
///
/// For each setting:
/// - empty: nothing changes;
/// - exactly one organisation has that name (case-insensitive): it gets the
///   mark, and any other organisation that had it loses it;
/// - no organisation (or more than one) has that name: a warning is logged and
///   nothing changes, so a typo or a later rename never removes the mark.
///
/// The mark is stored on the organisation and is not editable in the app.
/// </summary>
public static class OrganizationAdminRoleSetup
{
    public static void Apply(CcdContext context, ILogger logger) =>
        Apply(
            context,
            logger,
            StaticConfiguration.UserAdminOrganization,
            StaticConfiguration.OrganizationAdminOrganization
        );

    public static void Apply(
        CcdContext context,
        ILogger logger,
        string userAdminOrganization,
        string organizationAdminOrganization
    )
    {
        Mark(context, logger, "USER_ADMIN_ORGANIZATION", userAdminOrganization,
            o => o.AllowsUserAdmin, (o, v) => o.AllowsUserAdmin = v);
        Mark(context, logger, "ORGANIZATION_ADMIN_ORGANIZATION", organizationAdminOrganization,
            o => o.AllowsOrganizationAdmin, (o, v) => o.AllowsOrganizationAdmin = v);
        context.SaveChanges();
    }

    private static void Mark(
        CcdContext context,
        ILogger logger,
        string setting,
        string name,
        Func<Organization, bool> get,
        Action<Organization, bool> set
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var wanted = name.Trim().ToLower();
        var matches = context.Organizations.Where(o => o.Name.Trim().ToLower() == wanted).ToList();

        if (matches.Count != 1)
        {
            logger.LogWarning(
                "{Setting}: {Count} organisations are named '{Name}' (expected exactly 1); admin role marking left unchanged",
                setting, matches.Count, name);
            return;
        }

        var target = matches[0];
        foreach (var other in context.Organizations.ToList().Where(o => o.Id != target.Id && get(o)))
        {
            set(other, false);
            logger.LogInformation("{Setting}: removed the mark from '{Name}'", setting, other.Name);
        }

        if (!get(target))
        {
            set(target, true);
            logger.LogInformation("{Setting}: marked '{Name}'", setting, target.Name);
        }
    }
}
