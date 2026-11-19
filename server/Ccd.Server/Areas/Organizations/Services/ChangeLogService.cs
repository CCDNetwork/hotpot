using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Organizations;

/// <summary>
/// Writes entries to the admin change log. Entries are added to the context
/// only; the caller's SaveChanges stores them together with the change itself.
/// </summary>
public class ChangeLogService
{
    private readonly CcdContext _context;
    private readonly DateTimeProvider _dateTimeProvider;

    public ChangeLogService(CcdContext context, DateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Add(
        string entityType,
        Guid? entityId,
        string action,
        Guid? performedById,
        string oldValue = null,
        string newValue = null,
        Guid? organizationId = null,
        string organizationName = null,
        string subjectEmail = null
    )
    {
        if (organizationId != null && organizationName == null)
            organizationName = await _context.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync();

        var performedByEmail = performedById == null
            ? null
            : await _context.Users
                .Where(u => u.Id == performedById)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

        var now = _dateTimeProvider.UtcNow;
        _context.ChangeLogEntries.Add(new ChangeLogEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            OrganizationId = organizationId,
            OrganizationName = organizationName,
            SubjectEmail = subjectEmail,
            PerformedById = performedById,
            PerformedByEmail = performedByEmail,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    /// <summary>
    /// Builds a readable summary of changed fields, e.g.
    /// "Name: Old → New; Permissions: booking, referral → booking".
    /// Returns null when nothing changed.
    /// </summary>
    public static string Describe(params (string Field, string Old, string New)[] fields)
    {
        var parts = fields
            .Where(f => (f.Old ?? "") != (f.New ?? ""))
            .Select(f => $"{f.Field}: {Show(f.Old)} → {Show(f.New)}")
            .ToList();

        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    public static string List(IEnumerable<string> values) =>
        values == null ? "" : string.Join(", ", values.Where(v => !string.IsNullOrWhiteSpace(v)).OrderBy(v => v));

    private static string Show(string value) => string.IsNullOrEmpty(value) ? "(none)" : value;
}
