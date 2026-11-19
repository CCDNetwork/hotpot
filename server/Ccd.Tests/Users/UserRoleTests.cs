using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ccd.Server.Areas.Users.Controllers.ControllerModels;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Xunit;

namespace Ccd.Tests.Users;

[Collection("Api")]
public class UserRoleTests
{
    private readonly ApiFixture _api;

    public UserRoleTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task UserAdmin_CanManageUsers_ButNotOrganizations()
    {
        var (organization, _, adminHeaders) = await _api.CreateOrganization(role: UserRole.Admin);
        // These tests are about what each role can do, not who can get it.
        await _api.MarkAdminRoles(organization.Id, userAdmin: true, organizationAdmin: true);
        var (_, userAdminHeaders) = await addUserToOrganization(organization, adminHeaders, UserRole.UserAdmin);

        // users CRUD is allowed
        await _api.Request<PagedApiResponse<UserResponse>>("/api/v1/users", HttpMethod.Get, userAdminHeaders,
            null, HttpStatusCode.OK);

        var created = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, userAdminHeaders,
            userAddRequest(organization, UserRole.User), HttpStatusCode.Created);

        await _api.Request<UserResponse>($"/api/v1/users/{created.Id}", HttpMethod.Get, userAdminHeaders, null,
            HttpStatusCode.OK);
        await _api.Request<UserResponse>($"/api/v1/users/{created.Id}", HttpMethod.Put, userAdminHeaders,
            userUpdateRequest(UserRole.User), HttpStatusCode.OK);
        await _api.Request<UserResponse>($"/api/v1/users/{created.Id}", HttpMethod.Delete, userAdminHeaders, null,
            HttpStatusCode.OK);

        // organizations CRUD is forbidden
        await _api.Request<OrganizationResponse>($"/api/v1/organizations/{organization.Id}", HttpMethod.Get,
            userAdminHeaders, null, HttpStatusCode.Forbidden);
        await _api.Request<OrganizationResponse>("/api/v1/organizations", HttpMethod.Post, userAdminHeaders,
            organizationAddRequest(), HttpStatusCode.Forbidden);
        await _api.Request<OrganizationResponse>($"/api/v1/organizations/{organization.Id}", HttpMethod.Put,
            userAdminHeaders, organizationUpdateRequest(), HttpStatusCode.Forbidden);
        await _api.Request<OrganizationResponse>("/api/v1/organizations/me", HttpMethod.Put, userAdminHeaders,
            organizationUpdateRequest(), HttpStatusCode.Forbidden);
        await _api.Request<OrganizationResponse>($"/api/v1/organizations/{organization.Id}", HttpMethod.Delete,
            userAdminHeaders, null, HttpStatusCode.Forbidden);

        // organization lookups available to every user still work
        await _api.Request<PagedApiResponse<OrganizationResponse>>("/api/v1/organizations", HttpMethod.Get,
            userAdminHeaders, null, HttpStatusCode.OK);
        await _api.Request<OrganizationResponse>("/api/v1/organizations/me", HttpMethod.Get, userAdminHeaders,
            null, HttpStatusCode.OK);

        // other admin endpoints are allowed
        await _api.Request<object>("/api/v1/beneficiary-attribute", HttpMethod.Get, userAdminHeaders, null,
            HttpStatusCode.OK);
    }

    [Fact]
    public async Task OrganizationAdmin_CanManageOrganizations_ButNotUsers()
    {
        var (organization, adminUser, adminHeaders) = await _api.CreateOrganization(role: UserRole.Admin);
        // These tests are about what each role can do, not who can get it.
        await _api.MarkAdminRoles(organization.Id, userAdmin: true, organizationAdmin: true);
        var (_, organizationAdminHeaders) =
            await addUserToOrganization(organization, adminHeaders, UserRole.OrganizationAdmin);

        // users CRUD is forbidden
        await _api.Request<object>("/api/v1/users", HttpMethod.Get, organizationAdminHeaders, null,
            HttpStatusCode.Forbidden);
        await _api.Request<UserResponse>($"/api/v1/users/{adminUser.Id}", HttpMethod.Get, organizationAdminHeaders,
            null, HttpStatusCode.Forbidden);
        await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, organizationAdminHeaders,
            userAddRequest(organization, UserRole.User), HttpStatusCode.Forbidden);
        await _api.Request<UserResponse>($"/api/v1/users/{adminUser.Id}", HttpMethod.Put, organizationAdminHeaders,
            userUpdateRequest(UserRole.Admin), HttpStatusCode.Forbidden);
        await _api.Request<UserResponse>($"/api/v1/users/{adminUser.Id}", HttpMethod.Patch,
            organizationAdminHeaders, new UserPatchRequest { FirstName = "Changed" }, HttpStatusCode.Forbidden);
        await _api.Request<UserResponse>($"/api/v1/users/{adminUser.Id}", HttpMethod.Delete,
            organizationAdminHeaders, null, HttpStatusCode.Forbidden);

        // organizations CRUD is allowed
        var newOrganization = await _api.Request<OrganizationResponse>("/api/v1/organizations", HttpMethod.Post,
            organizationAdminHeaders, organizationAddRequest(), HttpStatusCode.OK);
        await _api.Request<OrganizationResponse>($"/api/v1/organizations/{newOrganization.Id}", HttpMethod.Get,
            organizationAdminHeaders, null, HttpStatusCode.OK);
        await _api.Request<OrganizationResponse>($"/api/v1/organizations/{newOrganization.Id}", HttpMethod.Put,
            organizationAdminHeaders, organizationUpdateRequest(), HttpStatusCode.OK);
        await _api.Request<OrganizationResponse>("/api/v1/organizations/me", HttpMethod.Put,
            organizationAdminHeaders, organizationUpdateRequest(), HttpStatusCode.OK);
        await _api.Request<OrganizationResponse>($"/api/v1/organizations/{newOrganization.Id}", HttpMethod.Delete,
            organizationAdminHeaders, null, HttpStatusCode.OK);

        // other admin endpoints are allowed
        await _api.Request<object>("/api/v1/beneficiary-attribute", HttpMethod.Get, organizationAdminHeaders, null,
            HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserAdmin_CannotGrantAdminRole()
    {
        var (organization, adminUser, adminHeaders) = await _api.CreateOrganization(role: UserRole.Admin);
        // These tests are about what each role can do, not who can get it.
        await _api.MarkAdminRoles(organization.Id, userAdmin: true, organizationAdmin: true);
        var (_, userAdminHeaders) = await addUserToOrganization(organization, adminHeaders, UserRole.UserAdmin);

        // can't create an admin
        await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, userAdminHeaders,
            userAddRequest(organization, UserRole.Admin), HttpStatusCode.Forbidden);

        // can create other admin-level roles
        await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, userAdminHeaders,
            userAddRequest(organization, UserRole.UserAdmin), HttpStatusCode.Created);
        await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, userAdminHeaders,
            userAddRequest(organization, UserRole.OrganizationAdmin), HttpStatusCode.Created);

        // can't promote an existing user to admin
        var user = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, userAdminHeaders,
            userAddRequest(organization, UserRole.User), HttpStatusCode.Created);

        await _api.Request<UserResponse>($"/api/v1/users/{user.Id}", HttpMethod.Put, userAdminHeaders,
            userUpdateRequest(UserRole.Admin), HttpStatusCode.Forbidden);
        await _api.Request<UserResponse>($"/api/v1/users/{user.Id}", HttpMethod.Patch, userAdminHeaders,
            new UserPatchRequest { Role = UserRole.Admin }, HttpStatusCode.Forbidden);

        // can edit an existing admin while keeping its role
        await _api.Request<UserResponse>($"/api/v1/users/{adminUser.Id}", HttpMethod.Put, userAdminHeaders,
            userUpdateRequest(UserRole.Admin), HttpStatusCode.OK);
        await _api.Request<UserResponse>($"/api/v1/users/{adminUser.Id}", HttpMethod.Patch, userAdminHeaders,
            new UserPatchRequest { FirstName = "Changed", Role = UserRole.Admin }, HttpStatusCode.OK);

        // admin can grant the admin role
        var promoted = await _api.Request<UserResponse>($"/api/v1/users/{user.Id}", HttpMethod.Put, adminHeaders,
            userUpdateRequest(UserRole.Admin), HttpStatusCode.OK);

        Assert.Equal(UserRole.Admin, promoted.Role);
    }

    [Theory]
    [InlineData(UserRole.UserAdmin)]
    [InlineData(UserRole.OrganizationAdmin)]
    public async Task Admin_CanChangeOwnRole(string newRole)
    {
        var (organization, adminUser, adminHeaders) = await _api.CreateOrganization(role: UserRole.Admin);
        await _api.MarkAdminRoles(organization.Id, userAdmin: true, organizationAdmin: true);

        var result = await _api.Request<UserResponse>($"/api/v1/users/{adminUser.Id}", HttpMethod.Put,
            adminHeaders, userUpdateRequest(newRole), HttpStatusCode.OK);

        Assert.Equal(newRole, result.Role);
    }

    [Fact]
    public async Task User_CannotUpdateCurrentOrganization()
    {
        var (_, _, userHeaders) = await _api.CreateOrganization(role: UserRole.User);

        await _api.Request<OrganizationResponse>("/api/v1/organizations/me", HttpMethod.Put, userHeaders,
            organizationUpdateRequest(), HttpStatusCode.Forbidden);
    }

    private async Task<(UserResponse, ApiFixture.Headers)> addUserToOrganization(
        Organization organization,
        ApiFixture.Headers adminHeaders,
        string role
    )
    {
        var user = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, adminHeaders,
            userAddRequest(organization, role), HttpStatusCode.Created);

        var headers = _api.GetUserHeaders(new User { Id = user.Id, Email = user.Email }, organization, role);

        return (user, headers);
    }

    private static UserAddRequest userAddRequest(Organization organization, string role)
    {
        return new UserAddRequest
        {
            Email = $"user_{Guid.NewGuid()}@e2e.com",
            FirstName = "Test",
            LastName = $"Testsson {Guid.NewGuid().ToString()[..8]}",
            OrganizationId = organization.Id,
            Role = role,
            Permissions = []
        };
    }

    private static UserUpdateRequest userUpdateRequest(string role)
    {
        return new UserUpdateRequest
        {
            FirstName = "Some",
            LastName = "User",
            Role = role,
            Permissions = []
        };
    }

    private static OrganizationAddRequest organizationAddRequest()
    {
        return new OrganizationAddRequest
        {
            Name = $"Organization {Guid.NewGuid()}",
            Activities = []
        };
    }

    private static OrganizationUpdateRequest organizationUpdateRequest()
    {
        return new OrganizationUpdateRequest
        {
            Name = $"Organization {Guid.NewGuid()}",
            Activities = []
        };
    }
}
