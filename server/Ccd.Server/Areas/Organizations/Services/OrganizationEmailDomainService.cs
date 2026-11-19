using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Organizations;

public class OrganizationEmailDomainService
{
    private readonly CcdContext _context;
    private readonly DateTimeProvider _dateTimeProvider;

    public OrganizationEmailDomainService(CcdContext context, DateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<List<string>> GetAllowedDomains(Guid organizationId)
    {
        return await _context.OrganizationEmailDomains
            .Where(d => d.OrganizationId == organizationId)
            .OrderBy(d => d.Domain)
            .Select(d => d.Domain)
            .ToListAsync();
    }

    public async Task<List<OrganizationEmailDomainResponse>> GetDomains(Guid organizationId)
    {
        await EnsureOrganization(organizationId);

        var domains = await _context.OrganizationEmailDomains
            .Where(d => d.OrganizationId == organizationId)
            .OrderBy(d => d.Domain)
            .ToListAsync();

        var sharedWith = await GetSharingOrganizations(domains.Select(d => d.Domain).ToList(), organizationId);

        return domains
            .Select(d => new OrganizationEmailDomainResponse
            {
                Id = d.Id,
                Domain = d.Domain,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                AlsoUsedBy = sharedWith.TryGetValue(d.Domain, out var names) ? names : new List<string>(),
            })
            .ToList();
    }

    /// <summary>
    /// Validates a domain before an Admin confirms it: returns its normalised
    /// form and the other organisations that already use it.
    /// </summary>
    public async Task<EmailDomainCheckResponse> Check(Guid organizationId, string input, Guid? excludeDomainId = null)
    {
        await EnsureOrganization(organizationId);
        return await CheckCore(organizationId, input, excludeDomainId);
    }

    /// <summary>
    /// Validates a domain for an organisation that is still being created:
    /// normalises it and lists every organisation already using it.
    /// </summary>
    public Task<EmailDomainCheckResponse> CheckForNewOrganization(string input) =>
        CheckCore(null, input, null);

    /// <summary>
    /// Validates the whole list for a new organisation before anything is
    /// saved. Returns the normalised, de-duplicated domains or throws one
    /// BadRequest naming every invalid entry, including domains that already
    /// belong to another organisation.
    /// </summary>
    public async Task<List<string>> ValidateForNewOrganization(IEnumerable<string> inputs)
    {
        var domains = new List<string>();
        var errors = new List<string>();

        foreach (var input in inputs ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (EmailDomainRules.TryNormalize(input, out var domain, out var error))
            {
                if (!domains.Contains(domain))
                    domains.Add(domain);
            }
            else
            {
                errors.Add(error);
            }
        }

        var taken = await GetSharingOrganizations(domains, null);
        foreach (var domain in domains)
            if (taken.TryGetValue(domain, out var names))
                errors.Add(DomainTakenMessage(domain, names));

        if (errors.Count > 0)
            throw new BadRequestException(string.Join(" ", errors));

        return domains;
    }

    /// <summary>
    /// Adds already-validated domains to a newly created organisation and logs
    /// each one. SaveChanges is left to the caller (same transaction as the
    /// organisation).
    /// </summary>
    public async Task AddForNewOrganization(Organization organization, List<string> domains, Guid performedById)
    {
        var now = _dateTimeProvider.UtcNow;
        foreach (var domain in domains)
        {
            var entity = new OrganizationEmailDomain
            {
                OrganizationId = organization.Id,
                Domain = domain,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _context.OrganizationEmailDomains.Add(entity);
            await AddLog(organization, entity.Id, ChangeLogAction.Added, null, domain, performedById);
        }
    }

    private async Task<EmailDomainCheckResponse> CheckCore(Guid? organizationId, string input, Guid? excludeDomainId)
    {
        if (!EmailDomainRules.TryNormalize(input, out var domain, out var error))
            throw new BadRequestException(error);

        if (organizationId != null)
        {
            var duplicate = await _context.OrganizationEmailDomains.AnyAsync(d =>
                d.OrganizationId == organizationId && d.Domain == domain && d.Id != excludeDomainId);
            if (duplicate)
                throw new BadRequestException($"'{domain}' is already on this organisation's list.");
        }

        // An email domain can belong to only one organisation.
        var sharedWith = await GetSharingOrganizations(new List<string> { domain }, organizationId);
        if (sharedWith.TryGetValue(domain, out var names))
            throw new BadRequestException(DomainTakenMessage(domain, names));

        return new EmailDomainCheckResponse { Domain = domain };
    }

    private static string DomainTakenMessage(string domain, List<string> organizationNames) =>
        $"'{domain}' is already an allowed email domain of {string.Join(", ", organizationNames)}. "
        + "An email domain can belong to only one organisation; remove it there first.";

    public async Task<OrganizationEmailDomainResponse> Add(Guid organizationId, string input, Guid performedById)
    {
        var organization = await EnsureOrganization(organizationId);
        var check = await Check(organizationId, input);
        var now = _dateTimeProvider.UtcNow;

        var entity = new OrganizationEmailDomain
        {
            OrganizationId = organizationId,
            Domain = check.Domain,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _context.OrganizationEmailDomains.Add(entity);
        await AddLog(organization, entity.Id, ChangeLogAction.Added, null, entity.Domain, performedById);
        await _context.SaveChangesAsync();

        return ToResponse(entity, check.AlsoUsedBy);
    }

    public async Task<OrganizationEmailDomainResponse> Update(Guid organizationId, Guid domainId, string input, Guid performedById)
    {
        var organization = await EnsureOrganization(organizationId);
        var entity = await FindDomain(organizationId, domainId);

        // Unchanged (e.g. only letter case or spaces differ): nothing to check,
        // even for a domain that was shared before duplicates were blocked.
        if (EmailDomainRules.TryNormalize(input, out var normalized, out _) && normalized == entity.Domain)
            return ToResponse(entity, await SharedWith(entity.Domain, organizationId));

        var check = await Check(organizationId, input, excludeDomainId: domainId);

        var oldDomain = entity.Domain;
        entity.Domain = check.Domain;
        entity.UpdatedAt = _dateTimeProvider.UtcNow;
        await AddLog(organization, entity.Id, ChangeLogAction.Updated, oldDomain, entity.Domain, performedById);
        await _context.SaveChangesAsync();

        return ToResponse(entity, check.AlsoUsedBy);
    }

    public async Task Remove(Guid organizationId, Guid domainId, Guid performedById)
    {
        var organization = await EnsureOrganization(organizationId);
        var entity = await FindDomain(organizationId, domainId);

        _context.OrganizationEmailDomains.Remove(entity);
        await AddLog(organization, entity.Id, ChangeLogAction.Removed, entity.Domain, null, performedById);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes every domain of an organisation that is being deleted, logging
    /// each removal. SaveChanges is left to the caller.
    /// </summary>
    public async Task RemoveAllForDeletedOrganization(Organization organization, Guid performedById)
    {
        var domains = await _context.OrganizationEmailDomains
            .Where(d => d.OrganizationId == organization.Id)
            .ToListAsync();

        foreach (var entity in domains)
        {
            _context.OrganizationEmailDomains.Remove(entity);
            await AddLog(organization, entity.Id, ChangeLogAction.Removed, entity.Domain, null, performedById,
                note: "organisation deleted");
        }
    }

    public async Task<List<ChangeLogEntryResponse>> GetHistory(Guid organizationId)
    {
        await EnsureOrganization(organizationId);

        return await _context.ChangeLogEntries
            .Where(e => e.OrganizationId == organizationId
                        && e.EntityType == ChangeLogEntityType.OrganizationEmailDomain)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ChangeLogEntryResponse
            {
                Id = e.Id,
                Action = e.Action,
                OldValue = e.OldValue,
                NewValue = e.NewValue,
                PerformedByEmail = e.PerformedByEmail,
                CreatedAt = e.CreatedAt,
            })
            .ToListAsync();
    }

    private async Task<Organization> EnsureOrganization(Guid organizationId)
    {
        return await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId)
               ?? throw new NotFoundException("Organisation not found");
    }

    private async Task<OrganizationEmailDomain> FindDomain(Guid organizationId, Guid domainId)
    {
        return await _context.OrganizationEmailDomains
                   .FirstOrDefaultAsync(d => d.Id == domainId && d.OrganizationId == organizationId)
               ?? throw new NotFoundException("Domain not found");
    }

    private async Task<List<string>> SharedWith(string domain, Guid organizationId) =>
        (await GetSharingOrganizations(new List<string> { domain }, organizationId))
            .TryGetValue(domain, out var names) ? names : new List<string>();

    private async Task<Dictionary<string, List<string>>> GetSharingOrganizations(List<string> domains, Guid? organizationId)
    {
        if (domains.Count == 0)
            return new Dictionary<string, List<string>>();

        var rows = await _context.OrganizationEmailDomains
            .Where(d => domains.Contains(d.Domain) && d.OrganizationId != organizationId)
            .Select(d => new { d.Domain, d.Organization.Name })
            .ToListAsync();

        return rows
            .GroupBy(r => r.Domain)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Name).OrderBy(n => n).ToList());
    }

    private async Task AddLog(
        Organization organization,
        Guid domainId,
        string action,
        string oldValue,
        string newValue,
        Guid performedById,
        string note = null
    )
    {
        var performedByEmail = await _context.Users
            .Where(u => u.Id == performedById)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();
        var now = _dateTimeProvider.UtcNow;

        _context.ChangeLogEntries.Add(new ChangeLogEntry
        {
            EntityType = ChangeLogEntityType.OrganizationEmailDomain,
            EntityId = domainId,
            Action = action,
            OldValue = oldValue,
            NewValue = note == null ? newValue : $"({note})",
            OrganizationId = organization.Id,
            OrganizationName = organization.Name,
            PerformedById = performedById,
            PerformedByEmail = performedByEmail,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    private static OrganizationEmailDomainResponse ToResponse(OrganizationEmailDomain entity, List<string> alsoUsedBy)
    {
        return new OrganizationEmailDomainResponse
        {
            Id = entity.Id,
            Domain = entity.Domain,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            AlsoUsedBy = alsoUsedBy,
        };
    }
}
