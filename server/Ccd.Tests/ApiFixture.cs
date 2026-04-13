using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Ccd.Server;
using Ccd.Server.Authentication;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Notifications;
using Ccd.Server.Organizations;
using Ccd.Server.Storage;
using Ccd.Server.Users;
using Ccd.Tests.Mocks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Ccd.Tests;

[Collection("Api")]
public class ApiFixture
{
    private readonly HttpClient _client;

    private readonly TestServer _server;
    public readonly string DEFAULT_PASSWORD = "Test1234!";

    public ApiFixture()
    {
        _server = new TestServer(
            new WebHostBuilder()
                .UseConfiguration(
                    new ConfigurationBuilder().AddJsonFile("appsettings.json").Build()
                )
                .ConfigureTestServices(services =>
                {
                    services.RemoveAll<INotificationService>();
                    services.TryAddScoped<INotificationService, MockNotificationService>();

                    services.RemoveAll<IStorageService>();
                    services.TryAddScoped<IStorageService, MockStorageService>();
                })
                .UseStartup<Startup>()
        );

        _client = _server.CreateClient();
    }

    public async Task<T> Request<T>(
        string url,
        HttpMethod method,
        dynamic headers,
        dynamic payload,
        HttpStatusCode expectedResponseCode
    )
    {
        var processedUrl = url;
        dynamic token;
        dynamic organizationId = null;
        dynamic language = null;

        try
        {
            token = headers?.Token;
            organizationId = headers?.OrganizationId;
            language = headers?.Language;
        }
        catch
        {
            token = headers;
        }

        if (token != null)
            if (token.Contains("apiKey="))
            {
                if (!processedUrl.Contains("?"))
                    processedUrl += $"?{token}";
                else
                    processedUrl += $"&{token}";
            }

        var request = new HttpRequestMessage(method, processedUrl);

        if (payload != null)
        {
            if (payload is MultipartFormDataContent)
                request.Content = payload;
            else
                request.Content = new StringContent(
                    Json.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );
        }

        if (token != null && !token.Contains("apiKey=")) request.Headers.Add("Authorization", "Bearer " + token);

        if (organizationId != null) request.Headers.Add("organization-id", organizationId.ToString());

        if (language != null) request.Headers.Add("language", language.ToString());

        var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (
            expectedResponseCode != response.StatusCode
            && response.StatusCode != HttpStatusCode.NotFound
        )
        {
            Console.WriteLine("------- ERROR RESPONSE --------");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            Console.WriteLine("-------------------------------");
        }

        Assert.Equal(expectedResponseCode, response.StatusCode);

        var content =
            response.StatusCode != HttpStatusCode.NotFound
                ? await response.Content.ReadAsStringAsync()
                : "";

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        if (content == "") return default;

        return JsonSerializer.Deserialize<T>(content, options);
    }

    private CcdContext GetCcdContext()
    {
        return _server.Host.Services.CreateScope().ServiceProvider.GetService<CcdContext>();
    }

    public T GetService<T>()
    {
        return _server.Host.Services.CreateScope().ServiceProvider.GetService<T>();
    }

    public async Task<UserResponse> CreateUser(Headers headers)
    {
        var userAddData = new UserAddRequest
        {
            Email = $"user_{Guid.NewGuid()}@e2e.com",
            FirstName = $"User {Guid.NewGuid()}",
            LastName = "Lastname",
            Role = UserRole.User
        };

        var user = await Request<UserResponse>(
            "/api/v1/users",
            HttpMethod.Post,
            headers,
            userAddData,
            HttpStatusCode.Created
        );

        await SetPasswordAndActivate(user.Id, DEFAULT_PASSWORD);

        return user;
    }

    public async Task<(Organization, User, Headers)> CreateOrganization(
        UserRegistrationRequest registrationRequest = null,
        string email = null,
        string role = UserRole.Admin
    )
    {
        var randomGuid = Guid.NewGuid().ToString();

        var context = GetCcdContext();

        var organization = new Organization
        {
            Name = $"Organization {randomGuid}",
            IsMpcaActive = true,
            IsWashActive = true,
            IsShelterActive = true,
            IsFoodAssistanceActive = true,
            IsLivelihoodsActive = true,
            IsProtectionActive = true
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var user = new User
        {
            Email = email ?? $"useremail_{randomGuid}@e2e.com",
            Password = "test123",
            FirstName = $"User {randomGuid}",
            LastName = "Doe",
            ActivatedAt = DateTime.UtcNow.AddDays(-1)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.UserOrganizations.Add(new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = organization.Id,
            Role = role,
            Permissions = [UserPermission.Deduplication, UserPermission.Referral]
        });

        await context.SaveChangesAsync();

        var headers = GetUserHeaders(user, organization, role);

        return (organization, user, headers);
    }

    public Headers GetUserHeaders(User user, Organization organization, string role)
    {
        var headers = new Headers
        {
            OrganizationId = organization.Id,
            Token = AuthenticationHelper.GenerateToken(
                user,
                new Dictionary<string, string> { { organization.Id.ToString(), role } }
            )
        };
        return headers;
    }

    public async Task<(User, Headers)> CreateOrganizationUser(Headers headers)
    {
        var userAddData = new UserAddRequest
        {
            Email = $"user_{Guid.NewGuid()}@e2e.com",
            FirstName = $"User {Guid.NewGuid()}",
            LastName = "Lastname",
            Role = UserRole.User
        };

        var userResult = await Request<UserResponse>(
            "/api/v1/users",
            HttpMethod.Post,
            headers,
            userAddData,
            HttpStatusCode.Created
        );

        var user = await SetPasswordAndActivate(userResult.Id, DEFAULT_PASSWORD);

        var loginData = new UserLoginRequest
        {
            Username = userAddData.Email,
            Password = DEFAULT_PASSWORD
        };

        var loginResult = await Request<UserAuthenticationResponse>(
            "/api/v1/authentication/login",
            HttpMethod.Post,
            headers,
            loginData,
            HttpStatusCode.OK
        );

        var newHeaders = new Headers
        {
            Token = loginResult.Token,
            OrganizationId = loginResult.Organizations[0].Id
        };

        return (user, newHeaders);
    }

    public async Task<User> SetPasswordAndActivate(Guid userId, string password)
    {
        var context = GetCcdContext();
        var user = await context.Users.FirstOrDefaultAsync(e => e.Id == userId);

        user.Password = AuthenticationHelper.HashPassword(user, password);
        user.ActivatedAt = DateTime.UtcNow;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        return user;
    }

    public void SetDate(DateTime date)
    {
        var service = GetService<DateTimeProvider>();
        service.SetDateTime(new DateTime(date.Ticks, DateTimeKind.Utc));
    }

    public void SetOnlyDate(DateTime date)
    {
        var service = GetService<DateTimeProvider>();
        service.SetDateTime(
            new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc)
        );
    }

    public DateTime GetUtcNow()
    {
        var service = GetService<DateTimeProvider>();

        return service.UtcNow;
    }

    public async Task ActivateUser(Guid userId)
    {
        var context = GetCcdContext();

        var user = await context.Users.FirstOrDefaultAsync(e => e.Id == userId);

        user.ActivatedAt = DateTime.UtcNow;
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public MultipartFormDataContent GetDummyImageForm(string name, int storageTypeId)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        form.Add(fileContent, "file", name);
        form.Add(new StringContent(storageTypeId.ToString()), "storageTypeId");

        return form;
    }

    public MultipartFormDataContent GetTextFileForm(string name, string content)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        form.Add(fileContent, "file", name);

        return form;
    }

    public async Task<FileResponse> AddImage(Headers headers, string name, int storageTypeId)
    {
        var form = GetDummyImageForm(name, storageTypeId);

        var fileResponse = await Request<FileResponse>(
            "/api/v1/storage/files",
            HttpMethod.Post,
            headers,
            form,
            HttpStatusCode.OK
        );

        return fileResponse;
    }

    public async Task ResetSettings()
    {
        var context = GetCcdContext();

        var settings = await context.Settings.FirstAsync();

        settings.DeploymentCountry = Server.Settings.Settings.DEFAULT_SETTINGS.DeploymentCountry;
        settings.DeploymentName = Server.Settings.Settings.DEFAULT_SETTINGS.DeploymentName;
        settings.AdminLevel1Name = Server.Settings.Settings.DEFAULT_SETTINGS.AdminLevel1Name;
        settings.AdminLevel2Name = Server.Settings.Settings.DEFAULT_SETTINGS.AdminLevel2Name;
        settings.AdminLevel3Name = Server.Settings.Settings.DEFAULT_SETTINGS.AdminLevel3Name;
        settings.AdminLevel4Name = Server.Settings.Settings.DEFAULT_SETTINGS.AdminLevel4Name;
        settings.MetabaseUrl = Server.Settings.Settings.DEFAULT_SETTINGS.MetabaseUrl;

        context.Settings.Update(settings);
        await context.SaveChangesAsync();
    }

    public async Task<Server.AdministrativeRegions.AdministrativeRegion> AddAdministrativeRegion(int level, string name, Guid? parentId)
    {
        var context = GetCcdContext();

        var result = context.AdministrativeRegions.Add(new Server.AdministrativeRegions.AdministrativeRegion
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Level = level,
            Name = name
        }).Entity;

        await context.SaveChangesAsync();

        return result;
    }

    public class Headers
    {
        public string Token { get; set; }
        public Guid? OrganizationId { get; set; }
        public string AccessKeyId { get; set; }
        public string AccessKeySecret { get; set; }
        public string Language { get; set; }
    }
}