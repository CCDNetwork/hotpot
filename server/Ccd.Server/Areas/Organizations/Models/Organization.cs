using System;
using System.ComponentModel.DataAnnotations;
using Ccd.Server.Helpers;

namespace Ccd.Server.Organizations;

public class Organization
{
    public Guid Id { get; set; } = IdProvider.NewId();

    [Required, QuickSearchable]
    public string Name { get; set; }
    public bool IsMpcaActive { get; set; }
    public bool IsWashActive { get; set; }
    public bool IsShelterActive { get; set; }
    public bool IsFoodAssistanceActive { get; set; }
    public bool IsLivelihoodsActive { get; set; }
    public bool IsProtectionActive { get; set; }
    public bool IsIncentivePaymentsActive { get; set; }

    /// <summary>
    /// Users of this organisation can be given the User Admin role. Set only at
    /// startup from the USER_ADMIN_ORGANIZATION deployment setting
    /// (OrganizationAdminRoleSetup); never editable in the app, so renaming the
    /// organisation doesn't affect it.
    /// </summary>
    public bool AllowsUserAdmin { get; set; }

    /// <summary>
    /// Users of this organisation can be given the Organisation Admin role. Set
    /// only at startup from ORGANIZATION_ADMIN_ORGANIZATION; never editable in the app.
    /// </summary>
    public bool AllowsOrganizationAdmin { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}