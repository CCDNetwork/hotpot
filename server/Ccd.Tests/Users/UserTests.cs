using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Ccd.Server.Authentication;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Ccd.Tests.Mocks;
using Xunit;

namespace Ccd.Tests.Users;

[Collection("Api")]
public class UserTests
{
    private readonly ApiFixture _api;

    public UserTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async void User_Crud_Success()
    {
        var (organization, user, headers) = await _api.CreateOrganization(role: UserRole.Admin);

        // create a new user
        var userAddData = new UserAddRequest
        {
            Email = "user_" + Guid.NewGuid() + "@e2e.com",
            FirstName = "Test",
            LastName = $"Testsson {Guid.NewGuid().ToString()[..8]}",
            OrganizationId = organization.Id,
            Role = UserRole.User,
            Permissions = [UserPermission.Referral, UserPermission.Deduplication]
        };

        var result = await _api.Request<UserResponse>(
            "/api/v1/users",
            HttpMethod.Post,
            headers,
            userAddData,
            HttpStatusCode.Created
        );

        Assert.Equal(userAddData.Email, result.Email);
        Assert.Equal(userAddData.FirstName, result.FirstName);
        Assert.Equal(userAddData.LastName, result.LastName);

        await _api.SetPasswordAndActivate(result.Id, _api.DEFAULT_PASSWORD);

        // log in created user
        var loginData = new UserLoginRequest
        {
            Username = userAddData.Email,
            Password = _api.DEFAULT_PASSWORD
        };

        var loginResult = await _api.Request<UserAuthenticationResponse>(
            "/api/v1/authentication/login",
            HttpMethod.Post,
            null,
            loginData,
            HttpStatusCode.OK
        );

        Assert.Equal(userAddData.Email, loginResult.User.Email);
        Assert.Equal(userAddData.FirstName, loginResult.User.FirstName);
        Assert.Equal(userAddData.LastName, loginResult.User.LastName);

        // find owner and new user in all users list
        var userList = await _api.Request<PagedApiResponse<UserResponse>>(
            $"/api/v1/users?organizationId={organization.Id}&pageSize=99999",
            HttpMethod.Get,
            headers,
            null,
            HttpStatusCode.OK
        );

        Assert.Equal(2, userList.Data.Count);
        Assert.NotNull(userList.Data.FirstOrDefault(e => e.Email == user.Email));
        Assert.NotNull(userList.Data.FirstOrDefault(e => e.Email == userAddData.Email));

        // update new user with owner credentials
        var userId = loginResult.User.Id;

        var newUserHeaders = new ApiFixture.Headers
        {
            OrganizationId = organization.Id,
            Token = loginResult.Token
        };

        var userUpdateData = new UserUpdateRequest
        {
            FirstName = "Some",
            LastName = "User",
            Language = "en",
            Role = UserRole.User,
            Permissions = [UserPermission.Referral]
        };

        await _api.Request<UserAuthenticationResponse>(
            $"/api/v1/users/{userId}",
            HttpMethod.Put,
            headers,
            userUpdateData,
            HttpStatusCode.OK
        );

        // update new user with its credentials
        userUpdateData = new UserUpdateRequest
        {
            FirstName = "Other",
            LastName = "User"
        };

        await _api.Request<UserResponse>(
            "/api/v1/users/me",
            HttpMethod.Put,
            newUserHeaders,
            userUpdateData,
            HttpStatusCode.OK
        );

        // check user data with owner credentials
        var newUser = await _api.Request<UserResponse>(
            $"/api/v1/users/{userId}",
            HttpMethod.Get,
            headers,
            null,
            HttpStatusCode.OK
        );

        Assert.Equal(userAddData.Email, newUser.Email);
        Assert.Equal(userUpdateData.FirstName, newUser.FirstName);
        Assert.Equal(userUpdateData.LastName, newUser.LastName);
        Assert.Equal(userUpdateData.Language, newUser.Language);

        // check user data with its credentials
        newUser = await _api.Request<UserResponse>(
            "/api/v1/users/me",
            HttpMethod.Get,
            newUserHeaders,
            null,
            HttpStatusCode.OK
        );

        Assert.Equal(userAddData.Email, newUser.Email);
        Assert.Equal(userUpdateData.FirstName, newUser.FirstName);
        Assert.Equal(userUpdateData.LastName, newUser.LastName);
        Assert.Equal(userUpdateData.Language, newUser.Language);

        // change password using its credentials
        userUpdateData = new UserUpdateRequest
        {
            FirstName = "Other",
            LastName = "User",
            Password = "Something1!",
        };

        await _api.Request<UserAuthenticationResponse>(
            "/api/v1/users/me",
            HttpMethod.Put,
            newUserHeaders,
            userUpdateData,
            HttpStatusCode.OK
        );

        // login using old password
        await _api.Request<UserAuthenticationResponse>(
            "/api/v1/authentication/login",
            HttpMethod.Post,
            null,
            loginData,
            HttpStatusCode.Unauthorized
        );

        // login using new password
        loginData.Password = userUpdateData.Password;

        var loginResponse = await _api.Request<UserAuthenticationResponse>(
            "/api/v1/authentication/login",
            HttpMethod.Post,
            null,
            loginData,
            HttpStatusCode.OK
        );

        // delete user
        await _api.Request<UserResponse>(
            $"/api/v1/users/{userId}",
            HttpMethod.Delete,
            headers,
            null,
            HttpStatusCode.OK
        );

        // fail to get user by id
        await _api.Request<UserResponse>(
            $"/api/v1/users/{userId}",
            HttpMethod.Get,
            headers,
            null,
            HttpStatusCode.NotFound
        );

        // user can't log in if it isn't a member of any organizations
        await _api.Request<UserAuthenticationResponse>(
            "/api/v1/authentication/login",
            HttpMethod.Post,
            null,
            loginData,
            HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async void User_Permissions_Success()
    {
        var (organization, adminUser, adminHeaders) = await _api.CreateOrganization(role: UserRole.Admin);

        // create a new user
        var userAddData = new UserAddRequest
        {
            Email = "user_" + Guid.NewGuid() + "@e2e.com",
            FirstName = "Test",
            LastName = $"Testsson {Guid.NewGuid().ToString()[..8]}",
            Role = UserRole.User,
            Permissions = [],
            OrganizationId = organization.Id
        };

        var created = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post,
            adminHeaders, userAddData, HttpStatusCode.Created);

        await _api.SetPasswordAndActivate(created.Id, _api.DEFAULT_PASSWORD);

        var loginData = new UserLoginRequest { Username = userAddData.Email, Password = _api.DEFAULT_PASSWORD };
        var loginResult = await _api.Request<UserAuthenticationResponse>("/api/v1/authentication/login",
            HttpMethod.Post,
            null, loginData, HttpStatusCode.OK);

        var user = loginResult.User;
        var userHeaders = new ApiFixture.Headers { Token = loginResult.Token, OrganizationId = organization.Id };

        var userUpdateData = new UserUpdateRequest
            { FirstName = "Some", LastName = "User", Permissions = [], Role = UserRole.User };

        // admin can update user
        await _api.Request<UserAuthenticationResponse>($"/api/v1/users/{user.Id}", HttpMethod.Put, adminHeaders,
            userUpdateData, HttpStatusCode.OK);

        // user can update himself
        await _api.Request<UserAuthenticationResponse>($"/api/v1/users/me", HttpMethod.Put, userHeaders,
            userUpdateData, HttpStatusCode.OK);

        // user can't update admin
        await _api.Request<UserAuthenticationResponse>($"/api/v1/users/{adminUser.Id}", HttpMethod.Put, userHeaders,
            userUpdateData, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async void User_Superadmin_Success()
    {
        var (organization, adminUser, adminHeaders) = await _api.CreateOrganization(role: UserRole.Admin);

        // create a new user
        var userAddData = new UserAddRequest
        {
            Email = "user_" + Guid.NewGuid() + "@e2e.com",
            FirstName = "Test",
            LastName = $"Testsson {Guid.NewGuid().ToString()[..8]}",
            Role = UserRole.User,
            Permissions = [],
            OrganizationId = organization.Id
        };

        var created = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post,
            adminHeaders, userAddData, HttpStatusCode.Created);

        await _api.SetPasswordAndActivate(created.Id, _api.DEFAULT_PASSWORD);

        // log in as regular user
        var loginData = new UserLoginRequest { Username = userAddData.Email, Password = _api.DEFAULT_PASSWORD };

        var loginResult = await _api.Request<UserAuthenticationResponse>("/api/v1/authentication/login",
            HttpMethod.Post,
            null, loginData, HttpStatusCode.OK);

        var user = loginResult.User;

        Assert.False(user.IsSuperAdmin);

        // log in as superadmin user
        loginData = new UserLoginRequest
        {
            Username = "superadmin",
            Password = StaticConfiguration.SuperadminPassword
        };

        loginResult = await _api.Request<UserAuthenticationResponse>(
            "/api/v1/authentication/login",
            HttpMethod.Post,
            null,
            loginData,
            HttpStatusCode.OK
        );

        user = loginResult.User;

        Assert.True(user.IsSuperAdmin);
    }
}