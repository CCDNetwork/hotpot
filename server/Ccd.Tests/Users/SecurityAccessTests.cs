using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ccd.Tests.Users;

/// <summary>
/// Security and access fixes: module permissions enforced by the server,
/// delete-all endpoint removed, roles and user status read from the database
/// on every request.
/// </summary>
[Collection("Api")]
public class SecurityAccessTests
{
    private readonly ApiFixture _api;

    public SecurityAccessTests(ApiFixture api)
    {
        _api = api;
    }

    private CcdContext Db() => _api.GetService<CcdContext>();

    // CreateOrganization gives users Deduplication + Referral, not Booking.

    [Theory]
    [InlineData(UserRole.User, HttpStatusCode.Forbidden)]
    [InlineData(UserRole.OrganizationAdmin, HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Admin, HttpStatusCode.OK)]
    [InlineData(UserRole.UserAdmin, HttpStatusCode.OK)]
    public async Task ModulePermission_IsEnforcedByTheServer(string role, HttpStatusCode bookingExpected)
    {
        var (_, _, headers) = await _api.CreateOrganization(role: role);

        await _api.Request<object>("/api/v1/deduplication/bookings", HttpMethod.Get, headers, null, bookingExpected);
        await _api.Request<object>("/api/v1/deduplication/listings", HttpMethod.Get, headers, null, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ModulePermission_TakesEffectWithoutSigningInAgain()
    {
        var (_, user, headers) = await _api.CreateOrganization(role: UserRole.User);
        await _api.Request<object>("/api/v1/deduplication/listings", HttpMethod.Get, headers, null, HttpStatusCode.OK);

        var db = Db();
        var membership = await db.UserOrganizations.FirstAsync(uo => uo.UserId == user.Id);
        membership.Permissions = [UserPermission.Referral];
        await db.SaveChangesAsync();

        // Same token, but the Deduplication permission was removed.
        await _api.Request<object>("/api/v1/deduplication/listings", HttpMethod.Get, headers, null,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAllEndpoint_IsGone()
    {
        var (_, _, admin) = await _api.CreateOrganization(role: UserRole.Admin);
        await _api.Request<object>("/api/v1/deduplication", HttpMethod.Delete, admin, null, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletedUser_LosesAccessOnNextRequest()
    {
        var (_, user, headers) = await _api.CreateOrganization(role: UserRole.Admin);
        await _api.Request<object>("/api/v1/users/me", HttpMethod.Get, headers, null, HttpStatusCode.OK);

        var db = Db();
        var entity = await db.Users.FirstAsync(u => u.Id == user.Id);
        entity.IsDeleted = true;
        await db.SaveChangesAsync();

        await _api.Request<object>("/api/v1/users/me", HttpMethod.Get, headers, null, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DemotedAdmin_LosesAdminFunctionsOnNextRequest()
    {
        var (org, user, headers) = await _api.CreateOrganization(role: UserRole.Admin);
        await _api.Request<object>($"/api/v1/users?organizationId={org.Id}", HttpMethod.Get, headers, null,
            HttpStatusCode.OK);

        var db = Db();
        var membership = await db.UserOrganizations.FirstAsync(uo => uo.UserId == user.Id);
        membership.Role = UserRole.User;
        await db.SaveChangesAsync();

        // The token still says 'admin'.
        await _api.Request<object>($"/api/v1/users?organizationId={org.Id}", HttpMethod.Get, headers, null,
            HttpStatusCode.Forbidden);
    }
}
