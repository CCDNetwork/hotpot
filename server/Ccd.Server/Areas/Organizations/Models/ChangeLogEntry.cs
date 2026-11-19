using System;
using Ccd.Server.Helpers;

namespace Ccd.Server.Organizations;

/// <summary>
/// Append-only record of administrative actions: organisations, allowed email
/// domains, users and roles. Shown in the Admin "Change log" view.
///
/// Deliberately no foreign keys: entries must survive the deletion of the
/// organisation or user they describe, so names and emails are snapshots.
/// </summary>
public class ChangeLogEntry
{
    public Guid Id { get; set; } = IdProvider.NewId();

    /// <summary>Area, see <see cref="ChangeLogEntityType"/>.</summary>
    public string EntityType { get; set; }
    public Guid? EntityId { get; set; }

    /// <summary>See <see cref="ChangeLogAction"/>.</summary>
    public string Action { get; set; }

    /// <summary>Previous value (e.g. old domain, old role). Null for creations.</summary>
    public string OldValue { get; set; }

    /// <summary>New value, or a readable summary of what changed for updates.</summary>
    public string NewValue { get; set; }

    public Guid? OrganizationId { get; set; }
    public string OrganizationName { get; set; }

    /// <summary>For user and role entries: the affected user's email (snapshot).</summary>
    public string SubjectEmail { get; set; }

    public Guid? PerformedById { get; set; }
    public string PerformedByEmail { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public static class ChangeLogEntityType
{
    public const string Organization = "organization";
    public const string OrganizationEmailDomain = "organization_email_domain";
    public const string User = "user";
    public const string UserRole = "user_role";
}

public static class ChangeLogAction
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";

    // Allowed email domains
    public const string Added = "added";
    public const string Removed = "removed";

    // Roles
    public const string RoleChanged = "role_changed";

    // Users
    public const string InvitationResent = "invitation_resent";
}
