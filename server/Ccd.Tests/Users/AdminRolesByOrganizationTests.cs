using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ccd.Server.Areas.Users.Controllers.ControllerModels;
using Ccd.Server.Data;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ccd.Tests.Users;

/// <summary>
/// Admin roles by organisation: User Admin only for users of the organisation
/// marked by USER_ADMIN_ORGANIZATION, Organisation Admin only for the one marked
/// by ORGANIZATION_ADMIN_ORGANIZATION (which can also have User Admins), User
/// everywhere, Admin unchanged.
/// </summary>
[Collection("Api")]
public class AdminRolesByOrganizationTests
{
    private readonly ApiFixture _api;

    public AdminRolesByOrganizationTests(ApiFixture api)
    {
        _api = api;
    }

    private CcdContext Db() => _api.GetService<CcdContext>();

    private static UserAddRequest NewUser(Guid organizationId, string role) => new()
    {
        Email = $"role_{Guid.NewGuid():N}@e2e.com",
        FirstName = "Role",
        LastName = "Test",
        OrganizationId = organizationId,
        Role = role,
        Permissions = [UserPermission.Booking],
    };

    private async Task<Organization> TargetOrganization(bool userAdmin, bool organizationAdmin)
    {
        var (organization, _, _) = await _api.CreateOrganization(role: UserRole.User);
        await _api.MarkAdminRoles(organization.Id, userAdmin, organizationAdmin);
        return organization;
    }

    [Theory]
    // Any other organisation: User only (Admin still grants Admin).
    [InlineData(false, false, UserRole.User, HttpStatusCode.Created)]
    [InlineData(false, false, UserRole.UserAdmin, HttpStatusCode.BadRequest)]
    [InlineData(false, false, UserRole.OrganizationAdmin, HttpStatusCode.BadRequest)]
    [InlineData(false, false, UserRole.Admin, HttpStatusCode.Created)]
    // User Admin organisation (e.g. Save the Children).
    [InlineData(true, false, UserRole.UserAdmin, HttpStatusCode.Created)]
    [InlineData(true, false, UserRole.OrganizationAdmin, HttpStatusCode.BadRequest)]
    [InlineData(true, false, UserRole.User, HttpStatusCode.Created)]
    // Organisation Admin organisation (e.g. UNICEF): also User Admin.
    [InlineData(false, true, UserRole.OrganizationAdmin, HttpStatusCode.Created)]
    [InlineData(false, true, UserRole.UserAdmin, HttpStatusCode.Created)]
    [InlineData(false, true, UserRole.User, HttpStatusCode.Created)]
    public async Task CreateUser_RoleMustFitTheOrganisation(
        bool userAdminOrg, bool organizationAdminOrg, string role, HttpStatusCode expected)
    {
        var (_, _, admin) = await _api.CreateOrganization(role: UserRole.Admin);
        var target = await TargetOrganization(userAdminOrg, organizationAdminOrg);

        await _api.Request<object>("/api/v1/users", HttpMethod.Post, admin, NewUser(target.Id, role), expected);
    }

    [Fact]
    public async Task UserAdmin_AssignsOrganisationAdminToUnicefUsers_ButNotAdmin()
    {
        var (sci, _, userAdmin) = await _api.CreateOrganization(role: UserRole.UserAdmin);
        await _api.MarkAdminRoles(sci.Id, userAdmin: true, organizationAdmin: false);
        var unicef = await TargetOrganization(userAdmin: false, organizationAdmin: true);

        await _api.Request<object>("/api/v1/users", HttpMethod.Post, userAdmin,
            NewUser(unicef.Id, UserRole.OrganizationAdmin), HttpStatusCode.Created);
        await _api.Request<object>("/api/v1/users", HttpMethod.Post, userAdmin,
            NewUser(sci.Id, UserRole.UserAdmin), HttpStatusCode.Created);
        await _api.Request<object>("/api/v1/users", HttpMethod.Post, userAdmin,
            NewUser(unicef.Id, UserRole.Admin), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EditUser_RoleMustFitTheOrganisation()
    {
        var (_, _, admin) = await _api.CreateOrganization(role: UserRole.Admin);
        var unicef = await TargetOrganization(userAdmin: false, organizationAdmin: true);
        var user = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, admin,
            NewUser(unicef.Id, UserRole.User), HttpStatusCode.Created);

        // A UNICEF user can become User Admin (e.g. taking over from Save the Children)...
        await _api.Request<object>($"/api/v1/users/{user.Id}", HttpMethod.Patch, admin,
            new UserPatchRequest { Role = UserRole.UserAdmin }, HttpStatusCode.OK);
        // ...or Organisation Admin, but never Admin unless an Admin grants it.
        var promoted = await _api.Request<UserResponse>($"/api/v1/users/{user.Id}", HttpMethod.Patch, admin,
            new UserPatchRequest { Role = UserRole.OrganizationAdmin }, HttpStatusCode.OK);
        Assert.Equal(UserRole.OrganizationAdmin, promoted.Role);

        // A Save the Children user still can't be Organisation Admin.
        var sci = await TargetOrganization(userAdmin: true, organizationAdmin: false);
        var sciUser = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, admin,
            NewUser(sci.Id, UserRole.User), HttpStatusCode.Created);
        await _api.Request<object>($"/api/v1/users/{sciUser.Id}", HttpMethod.Patch, admin,
            new UserPatchRequest { Role = UserRole.OrganizationAdmin }, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExistingRoleThatNoLongerFits_MustBeCorrectedWhenEdited()
    {
        // A User Admin in an organisation without the mark (created before the rule).
        var (_, existing, _) = await _api.CreateOrganization(role: UserRole.UserAdmin);
        var (_, _, admin) = await _api.CreateOrganization(role: UserRole.Admin);

        // Not touched at release: still a User Admin.
        var membership = await Db().UserOrganizations.AsNoTracking().FirstAsync(uo => uo.UserId == existing.Id);
        Assert.Equal(UserRole.UserAdmin, membership.Role);

        // Editing without changing the role is fine; keeping the role is refused.
        await _api.Request<object>($"/api/v1/users/{existing.Id}", HttpMethod.Patch, admin,
            new UserPatchRequest { FirstName = "Renamed" }, HttpStatusCode.OK);
        await _api.Request<object>($"/api/v1/users/{existing.Id}", HttpMethod.Patch, admin,
            new UserPatchRequest { FirstName = "Renamed", Role = UserRole.UserAdmin }, HttpStatusCode.BadRequest);
        await _api.Request<object>($"/api/v1/users/{existing.Id}", HttpMethod.Patch, admin,
            new UserPatchRequest { FirstName = "Renamed", Role = UserRole.User }, HttpStatusCode.OK);
    }

    [Fact]
    public async Task Marks_AreReturned_ButCannotBeChangedThroughTheApp()
    {
        var (_, _, orgAdmin) = await _api.CreateOrganization(role: UserRole.OrganizationAdmin);
        var target = await TargetOrganization(userAdmin: false, organizationAdmin: true);

        var read = await _api.Request<OrganizationResponse>($"/api/v1/organizations/{target.Id}", HttpMethod.Get,
            orgAdmin, null, HttpStatusCode.OK);
        Assert.True(read.AllowsOrganizationAdmin);
        Assert.False(read.AllowsUserAdmin);

        // An edit that tries to set the marks is ignored.
        await _api.Request<object>($"/api/v1/organizations/{target.Id}", HttpMethod.Put, orgAdmin, new
        {
            name = target.Name + " renamed",
            isMpcaActive = true,
            activities = Array.Empty<object>(),
            allowsUserAdmin = true,
            allowsOrganizationAdmin = false,
        }, HttpStatusCode.OK);

        var stored = await Db().Organizations.AsNoTracking().FirstAsync(o => o.Id == target.Id);
        Assert.False(stored.AllowsUserAdmin);
        Assert.True(stored.AllowsOrganizationAdmin);
    }

    [Fact]
    public async Task StartupSetup_MarksByName_MovesTheMark_AndIgnoresUnknownNames()
    {
        var (sci, _, _) = await _api.CreateOrganization(role: UserRole.User);
        var (unicef, _, _) = await _api.CreateOrganization(role: UserRole.User);
        var (other, _, _) = await _api.CreateOrganization(role: UserRole.User);

        // Case-insensitive, surrounding spaces ignored.
        OrganizationAdminRoleSetup.Apply(Db(), NullLogger.Instance, $"  {sci.Name.ToUpper()} ", unicef.Name.ToLower());
        await AssertMarks(sci.Id, userAdmin: true, organizationAdmin: false);
        await AssertMarks(unicef.Id, userAdmin: false, organizationAdmin: true);

        // A different name moves the mark: only one organisation per role.
        OrganizationAdminRoleSetup.Apply(Db(), NullLogger.Instance, other.Name, unicef.Name);
        await AssertMarks(sci.Id, userAdmin: false, organizationAdmin: false);
        await AssertMarks(other.Id, userAdmin: true, organizationAdmin: false);

        // Unknown name or empty setting: nothing changes (e.g. after a rename).
        OrganizationAdminRoleSetup.Apply(Db(), NullLogger.Instance, "No such organisation", null);
        await AssertMarks(other.Id, userAdmin: true, organizationAdmin: false);
        await AssertMarks(unicef.Id, userAdmin: false, organizationAdmin: true);
    }

    private async Task AssertMarks(Guid organizationId, bool userAdmin, bool organizationAdmin)
    {
        var organization = await Db().Organizations.AsNoTracking().FirstAsync(o => o.Id == organizationId);
        Assert.Equal(userAdmin, organization.AllowsUserAdmin);
        Assert.Equal(organizationAdmin, organization.AllowsOrganizationAdmin);
    }
}
