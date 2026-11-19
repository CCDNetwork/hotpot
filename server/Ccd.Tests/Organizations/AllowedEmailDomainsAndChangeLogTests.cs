using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ccd.Tests.Organizations;

/// <summary>
/// Allowed email domains (managed by Admin and Organisation Admin) and the
/// admin change log (readable by Admin, User Admin and Organisation Admin).
/// </summary>
[Collection("Api")]
public class AllowedEmailDomainsAndChangeLogTests
{
    private readonly ApiFixture _api;

    public AllowedEmailDomainsAndChangeLogTests(ApiFixture api)
    {
        _api = api;
    }

    private CcdContext Db() => _api.GetService<CcdContext>();

    // A domain can belong to only one organisation, and the test database keeps
    // organisations between runs, so every test uses its own domains.
    private readonly string _s = Guid.NewGuid().ToString("N")[..8];

    private static OrganizationAddRequest NewOrganization(params string[] domains) => new()
    {
        Name = $"Org {Guid.NewGuid():N}"[..20],
        IsMpcaActive = true, IsWashActive = false, IsShelterActive = false, IsFoodAssistanceActive = false,
        IsLivelihoodsActive = false, IsProtectionActive = false, IsIncentivePaymentsActive = false,
        Activities = [],
        EmailDomains = domains.ToList(),
    };

    private Task<OrganizationResponse> CreateOrganization(ApiFixture.Headers headers, params string[] domains) =>
        _api.Request<OrganizationResponse>("/api/v1/organizations", HttpMethod.Post, headers,
            NewOrganization(domains), HttpStatusCode.OK);

    private static UserAddRequest NewUser(Guid organizationId, string email, string role = UserRole.User) => new()
    {
        Email = email, FirstName = "Test", LastName = "User",
        OrganizationId = organizationId, Role = role, Permissions = [UserPermission.Booking],
    };

    private async Task<List<ChangeLogEntryResponse>> ChangeLog(ApiFixture.Headers headers) =>
        (await _api.Request<PagedApiResponse<ChangeLogEntryResponse>>("/api/v1/change-log?pageSize=100",
            HttpMethod.Get, headers, null, HttpStatusCode.OK)).Data;

    // ------------------------------------------------------------ who manages domains

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.OrganizationAdmin)]
    public async Task AdminAndOrganizationAdmin_CreateOrganizationWithDomains_AndManageThem(string role)
    {
        var (_, _, headers) = await _api.CreateOrganization(role: role);

        var org = await CreateOrganization(headers, $"@Agency-{_s}.ORG", $"second-{_s}.example.net");
        var domains = await _api.Request<List<OrganizationEmailDomainResponse>>(
            $"/api/v1/organizations/{org.Id}/email-domains", HttpMethod.Get, headers, null, HttpStatusCode.OK);
        Assert.Equal(new[] { $"agency-{_s}.org", $"second-{_s}.example.net" }, domains.Select(d => d.Domain));

        // Add, edit, remove later.
        var added = await _api.Request<OrganizationEmailDomainResponse>(
            $"/api/v1/organizations/{org.Id}/email-domains", HttpMethod.Post, headers,
            new EmailDomainRequest { Domain = $"third-{_s}.example.org" }, HttpStatusCode.OK);
        await _api.Request<OrganizationEmailDomainResponse>(
            $"/api/v1/organizations/{org.Id}/email-domains/{added.Id}", HttpMethod.Put, headers,
            new EmailDomainRequest { Domain = $"fourth-{_s}.example.org" }, HttpStatusCode.OK);
        await _api.Request<object>(
            $"/api/v1/organizations/{org.Id}/email-domains/{added.Id}", HttpMethod.Delete, headers, null,
            HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(UserRole.UserAdmin)]
    [InlineData(UserRole.User)]
    public async Task UserAdminAndUser_CannotSeeOrManageDomains(string role)
    {
        var (_, _, admin) = await _api.CreateOrganization(role: UserRole.Admin);
        var org = await CreateOrganization(admin, $"agency-{_s}.org");
        var (_, _, headers) = await _api.CreateOrganization(role: role);

        await _api.Request<object>($"/api/v1/organizations/{org.Id}/email-domains", HttpMethod.Get,
            headers, null, HttpStatusCode.Forbidden);
        await _api.Request<object>($"/api/v1/organizations/{org.Id}/email-domains", HttpMethod.Post,
            headers, new EmailDomainRequest { Domain = $"x-{_s}.example.org" }, HttpStatusCode.Forbidden);
        await _api.Request<object>("/api/v1/organizations", HttpMethod.Post, headers,
            NewOrganization($"y-{_s}.example.org"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InvalidDomain_CreatesNothing()
    {
        var (_, _, headers) = await _api.CreateOrganization(role: UserRole.OrganizationAdmin);
        var request = NewOrganization($"good-{_s}.example.org", "gmail.com");

        await _api.Request<object>("/api/v1/organizations", HttpMethod.Post, headers, request,
            HttpStatusCode.BadRequest);
        Assert.False(await Db().Organizations.AnyAsync(o => o.Name == request.Name));
    }

    // ------------------------------------------------------------ the restriction

    [Fact]
    public async Task UsersCanOnlyBeCreatedOnAllowedDomains_UnrestrictedWithoutDomains()
    {
        var (_, _, orgAdmin) = await _api.CreateOrganization(role: UserRole.OrganizationAdmin);
        var restricted = await CreateOrganization(orgAdmin, $"agency-{_s}.org");
        var open = await CreateOrganization(orgAdmin);

        var (_, _, userAdmin) = await _api.CreateOrganization(role: UserRole.UserAdmin);

        await _api.Request<object>("/api/v1/users", HttpMethod.Post, userAdmin,
            NewUser(restricted.Id, $"u{Guid.NewGuid():N}@other.org"), HttpStatusCode.BadRequest);
        await _api.Request<object>("/api/v1/users", HttpMethod.Post, userAdmin,
            NewUser(restricted.Id, $"u{Guid.NewGuid():N}@sub.agency-{_s}.org"), HttpStatusCode.BadRequest);
        await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, userAdmin,
            NewUser(restricted.Id, $"u{Guid.NewGuid():N}@AGENCY-{_s}.org"), HttpStatusCode.Created);
        await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, userAdmin,
            NewUser(open.Id, $"u{Guid.NewGuid():N}@gmail.com"), HttpStatusCode.Created);
    }

    // ------------------------------------------------------------ change log

    [Theory]
    [InlineData(UserRole.Admin, HttpStatusCode.OK)]
    [InlineData(UserRole.UserAdmin, HttpStatusCode.OK)]
    [InlineData(UserRole.OrganizationAdmin, HttpStatusCode.OK)]
    [InlineData(UserRole.User, HttpStatusCode.Forbidden)]
    public async Task ChangeLog_VisibleToAdminRolesOnly(string role, HttpStatusCode expected)
    {
        var (_, _, headers) = await _api.CreateOrganization(role: role);
        await _api.Request<object>("/api/v1/change-log", HttpMethod.Get, headers, null, expected);
    }

    [Fact]
    public async Task ChangeLog_RecordsOrganizationDomainUserAndRoleChanges()
    {
        var (_, orgAdminUser, orgAdmin) = await _api.CreateOrganization(role: UserRole.OrganizationAdmin);
        var (_, _, admin) = await _api.CreateOrganization(role: UserRole.Admin);

        // Organisation created (with a domain), then edited.
        var org = await CreateOrganization(orgAdmin, $"agency-{_s}.org");
        var update = new OrganizationUpdateRequest
        {
            Name = org.Name + " X", IsMpcaActive = true, IsWashActive = true, IsShelterActive = false,
            IsFoodAssistanceActive = false, IsLivelihoodsActive = false, IsProtectionActive = false,
            IsIncentivePaymentsActive = false, Activities = [],
        };
        await _api.Request<OrganizationResponse>($"/api/v1/organizations/{org.Id}", HttpMethod.Put, orgAdmin,
            update, HttpStatusCode.OK);

        // User created by an Admin, then role and permissions changed, then deleted.
        await _api.MarkAdminRoles(org.Id, userAdmin: false, organizationAdmin: true);
        var email = $"u{Guid.NewGuid():N}@agency-{_s}.org";
        var created = await _api.Request<UserResponse>("/api/v1/users", HttpMethod.Post, admin,
            NewUser(org.Id, email), HttpStatusCode.Created);
        await _api.Request<UserResponse>($"/api/v1/users/{created.Id}", HttpMethod.Patch, admin,
            new { role = UserRole.OrganizationAdmin, permissions = new[] { "booking", "referral" } },
            HttpStatusCode.OK);
        // Existing behaviour: the response body is looked up in the caller's
        // organisation, so deleting a user of another organisation returns 204.
        await _api.Request<object>($"/api/v1/users/{created.Id}", HttpMethod.Delete, admin, null,
            HttpStatusCode.NoContent);

        var log = await ChangeLog(orgAdmin);
        ChangeLogEntryResponse Find(string type, string action, Func<ChangeLogEntryResponse, bool> extra = null) =>
            log.FirstOrDefault(e => e.EntityType == type && e.Action == action && (extra?.Invoke(e) ?? true));

        var orgCreated = Find(ChangeLogEntityType.Organization, ChangeLogAction.Created, e => e.OrganizationName == org.Name);
        Assert.NotNull(orgCreated);
        Assert.Equal(orgAdminUser.Email, orgCreated.PerformedByEmail);
        Assert.NotNull(Find(ChangeLogEntityType.OrganizationEmailDomain, ChangeLogAction.Added,
            e => e.NewValue == $"agency-{_s}.org" && e.OrganizationName == org.Name));
        var orgUpdated = Find(ChangeLogEntityType.Organization, ChangeLogAction.Updated, e => e.OrganizationName == org.Name + " X");
        Assert.Contains("Name:", orgUpdated.NewValue);
        Assert.Contains("WASH", orgUpdated.NewValue);

        Assert.NotNull(Find(ChangeLogEntityType.User, ChangeLogAction.Created, e => e.SubjectEmail == email));
        var role = Find(ChangeLogEntityType.UserRole, ChangeLogAction.RoleChanged, e => e.SubjectEmail == email);
        Assert.Equal("User", role.OldValue);
        Assert.Equal("Organisation Admin", role.NewValue);
        Assert.Contains("Permissions:", Find(ChangeLogEntityType.User, ChangeLogAction.Updated, e => e.SubjectEmail == email).NewValue);
        Assert.NotNull(Find(ChangeLogEntityType.User, ChangeLogAction.Deleted, e => e.SubjectEmail == email));
    }

    // ------------------------------------------------------------ one organisation per domain

    [Fact]
    public async Task Domain_CannotBeAllowedForTwoOrganisations()
    {
        var (_, _, orgAdmin) = await _api.CreateOrganization(role: UserRole.OrganizationAdmin);
        var first = await CreateOrganization(orgAdmin, $"agency-{_s}.org");
        var second = await CreateOrganization(orgAdmin, $"other-{_s}.org");

        // Adding it to another organisation (any letter case) is refused, naming the owner.
        var error = await _api.Request<ErrorBody>(
            $"/api/v1/organizations/{second.Id}/email-domains", HttpMethod.Post, orgAdmin,
            new EmailDomainRequest { Domain = $"@AGENCY-{_s}.org" }, HttpStatusCode.BadRequest);
        Assert.Equal(
            $"'agency-{_s}.org' is already an allowed email domain of {first.Name}. "
            + "An email domain can belong to only one organisation; remove it there first.",
            error.ErrorMessage);

        // The check used by the form gives the same answer.
        await _api.Request<object>($"/api/v1/organizations/{second.Id}/email-domains/check", HttpMethod.Post,
            orgAdmin, new EmailDomainRequest { Domain = $"agency-{_s}.org" }, HttpStatusCode.BadRequest);

        // Changing an existing domain into it is refused.
        var other = (await Db().OrganizationEmailDomains.AsNoTracking()
            .FirstAsync(d => d.OrganizationId == second.Id)).Id;
        await _api.Request<object>($"/api/v1/organizations/{second.Id}/email-domains/{other}", HttpMethod.Put,
            orgAdmin, new EmailDomainRequest { Domain = $"agency-{_s}.org" }, HttpStatusCode.BadRequest);

        // A new organisation with it is refused, and nothing is created.
        var request = NewOrganization($"agency-{_s}.org");
        await _api.Request<object>("/api/v1/organizations", HttpMethod.Post, orgAdmin, request,
            HttpStatusCode.BadRequest);
        Assert.False(await Db().Organizations.AnyAsync(o => o.Name == request.Name));

        // Once removed from the first organisation, it can be added to the second.
        var owned = (await Db().OrganizationEmailDomains.AsNoTracking()
            .FirstAsync(d => d.OrganizationId == first.Id)).Id;
        await _api.Request<object>($"/api/v1/organizations/{first.Id}/email-domains/{owned}", HttpMethod.Delete,
            orgAdmin, null, HttpStatusCode.NoContent);
        await _api.Request<object>($"/api/v1/organizations/{second.Id}/email-domains", HttpMethod.Post, orgAdmin,
            new EmailDomainRequest { Domain = $"agency-{_s}.org" }, HttpStatusCode.OK);
    }

    [Fact]
    public async Task DomainSharedBeforeTheRule_IsShown_AndCanStillBeEditedOrRemoved()
    {
        var (_, _, orgAdmin) = await _api.CreateOrganization(role: UserRole.OrganizationAdmin);
        var first = await CreateOrganization(orgAdmin, $"agency-{_s}.org");
        var second = await CreateOrganization(orgAdmin);

        // Simulate data from before the rule: the same domain on both organisations.
        var db = Db();
        db.OrganizationEmailDomains.Add(new OrganizationEmailDomain
        {
            OrganizationId = second.Id, Domain = $"agency-{_s}.org",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var list = await _api.Request<List<OrganizationEmailDomainResponse>>(
            $"/api/v1/organizations/{second.Id}/email-domains", HttpMethod.Get, orgAdmin, null, HttpStatusCode.OK);
        var shared = Assert.Single(list);
        Assert.Equal(new[] { first.Name }, shared.AlsoUsedBy);

        // Saving it unchanged is fine; it can be removed.
        await _api.Request<object>($"/api/v1/organizations/{second.Id}/email-domains/{shared.Id}", HttpMethod.Put,
            orgAdmin, new EmailDomainRequest { Domain = $"Agency-{_s}.org" }, HttpStatusCode.OK);
        await _api.Request<object>($"/api/v1/organizations/{second.Id}/email-domains/{shared.Id}",
            HttpMethod.Delete, orgAdmin, null, HttpStatusCode.NoContent);
    }

    private class ErrorBody
    {
        public string ErrorMessage { get; set; }
    }
}
