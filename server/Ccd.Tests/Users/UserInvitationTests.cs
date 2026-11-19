using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ccd.Tests.Users;

/// <summary>
/// User invitations: the user is kept and the response says whether the email
/// went out; "Invitation pending" for password sign-in; Resend invitation.
/// (No SendGrid key in tests, so emails are skipped and count as sent.)
/// </summary>
[Collection("Api")]
public class UserInvitationTests
{
    private readonly ApiFixture _api;

    public UserInvitationTests(ApiFixture api)
    {
        _api = api;
    }

    private CcdContext Db() => _api.GetService<CcdContext>();

    private async Task<(Guid OrgId, ApiFixture.Headers Admin, UserResponse Invited)> InviteUser()
    {
        var (org, _, admin) = await _api.CreateOrganization(role: UserRole.Admin);
        var invited = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, admin, new UserAddRequest
        {
            Email = $"invite_{Guid.NewGuid():N}@e2e.com",
            FirstName = "Invited",
            LastName = "User",
            OrganizationId = org.Id,
            Role = UserRole.User,
            Permissions = [UserPermission.Booking],
        }, HttpStatusCode.Created);
        return (org.Id, admin, invited);
    }

    [Fact]
    public async Task CreateUser_ReportsInvitationSent_AndPending()
    {
        var (_, _, invited) = await InviteUser();

        Assert.True(invited.InvitationEmailSent);
        Assert.True(invited.InvitationPending);

        // The invitation link is valid for 2 hours.
        var user = await Db().Users.AsNoTracking().FirstAsync(u => u.Id == invited.Id);
        Assert.NotNull(user.PasswordResetCode);
        var validFor = user.PasswordResetCodeExpiresAt!.Value - DateTime.UtcNow;
        Assert.InRange(validFor.TotalMinutes, 115, 121);
    }

    [Fact]
    public async Task Resend_IssuesNewLink_AndIsLogged()
    {
        var (orgId, admin, invited) = await InviteUser();
        var before = (await Db().Users.AsNoTracking().FirstAsync(u => u.Id == invited.Id)).PasswordResetCode;

        await _api.Request<object>($"/api/v1/users/{invited.Id}/resend-invitation", HttpMethod.Post, admin, null,
            HttpStatusCode.OK);

        var after = (await Db().Users.AsNoTracking().FirstAsync(u => u.Id == invited.Id)).PasswordResetCode;
        Assert.NotEqual(before, after);

        var logged = await Db().ChangeLogEntries.AnyAsync(e =>
            e.EntityType == ChangeLogEntityType.User && e.EntityId == invited.Id
            && e.Action == ChangeLogAction.InvitationResent && e.OrganizationId == orgId);
        Assert.True(logged);
    }

    [Fact]
    public async Task Resend_RefusedOnceThePasswordIsSet()
    {
        var (_, admin, invited) = await InviteUser();
        await _api.SetPasswordAndActivate(invited.Id, _api.DEFAULT_PASSWORD);

        await _api.Request<object>($"/api/v1/users/{invited.Id}/resend-invitation", HttpMethod.Post, admin, null,
            HttpStatusCode.BadRequest);

        var list = await _api.Request<Ccd.Server.Helpers.PagedApiResponse<UserResponse>>(
            $"/api/v1/users?pageSize=99999", HttpMethod.Get, admin, null, HttpStatusCode.OK);
        Assert.False(list.Data.Single(u => u.Id == invited.Id).InvitationPending);
    }

    [Fact]
    public async Task Resend_RefusedForSystemAccount_AndForOrdinaryUsers()
    {
        var (_, admin, invited) = await InviteUser();
        await _api.Request<object>($"/api/v1/users/{User.SYSTEM_USER.Id}/resend-invitation", HttpMethod.Post,
            admin, null, HttpStatusCode.BadRequest);

        var (_, _, plain) = await _api.CreateOrganization(role: UserRole.User);
        await _api.Request<object>($"/api/v1/users/{invited.Id}/resend-invitation", HttpMethod.Post, plain, null,
            HttpStatusCode.Forbidden);
    }
}
