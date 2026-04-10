using System.Net;
using System.Net.Http;
using Ccd.Server.Authentication;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Settings;
using Xunit;

namespace Ccd.Tests.Settings;

[Collection("Api")]
public class SettingsTests
{
    private readonly ApiFixture _api;

    public SettingsTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async void Settings_CRUD_Success()
    {
        var (organization, _, headers) = await _api.CreateOrganization();

        await _api.ResetSettings();

        // any user can get settings
        var result =
            await _api.Request<SettingsResponse>("/api/v1/settings", HttpMethod.Get, headers, null, HttpStatusCode.OK);

        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.Id, result.Id);
        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.DeploymentCountry, result.DeploymentCountry);
        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.DeploymentName, result.DeploymentName);
        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.AdminLevel1Name, result.AdminLevel1Name);
        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.AdminLevel2Name, result.AdminLevel2Name);
        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.AdminLevel3Name, result.AdminLevel3Name);
        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.AdminLevel4Name, result.AdminLevel4Name);
        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.MetabaseUrl, result.MetabaseUrl);

        var updateData = new SettingsUpdateRequest
        {
            DeploymentCountry = "DeploymentCountry_updated",
            DeploymentName = "DeploymentName_updated",
            AdminLevel1Name = "AdminLevel1Name_updated",
            AdminLevel2Name = "AdminLevel2Name_updated",
            AdminLevel3Name = "AdminLevel3Name_updated",
            AdminLevel4Name = "AdminLevel4Name_updated",
            MetabaseUrl = "MetabaseUrl_updated"
        };

        await _api.Request<SettingsResponse>(
            "/api/v1/settings",
            HttpMethod.Put,
            headers,
            updateData,
            HttpStatusCode.Forbidden
        );

        // log in as superadmin user
        var loginData = new UserLoginRequest
        {
            Username = "superadmin",
            Password = StaticConfiguration.SuperadminPassword
        };

        var loginResult = await _api.Request<UserAuthenticationResponse>(
            "/api/v1/authentication/login",
            HttpMethod.Post,
            null,
            loginData,
            HttpStatusCode.OK
        );

        var saHeaders = new ApiFixture.Headers
        {
            Token = loginResult.Token
        };

        // update settings
        await _api.Request<SettingsResponse>(
            "/api/v1/settings",
            HttpMethod.Put,
            saHeaders,
            updateData,
            HttpStatusCode.OK
        );
        
        // check if settings have been updated
        result =
            await _api.Request<SettingsResponse>("/api/v1/settings", HttpMethod.Get, headers, null, HttpStatusCode.OK);

        Assert.Equal(Ccd.Server.Settings.Settings.DEFAULT_SETTINGS.Id, result.Id);
        Assert.Equal(updateData.DeploymentCountry, result.DeploymentCountry);
        Assert.Equal(updateData.DeploymentName, result.DeploymentName);
        Assert.Equal(updateData.AdminLevel1Name, result.AdminLevel1Name);
        Assert.Equal(updateData.AdminLevel2Name, result.AdminLevel2Name);
        Assert.Equal(updateData.AdminLevel3Name, result.AdminLevel3Name);
        Assert.Equal(updateData.AdminLevel4Name, result.AdminLevel4Name);
        Assert.Equal(updateData.MetabaseUrl, result.MetabaseUrl);
    }
}