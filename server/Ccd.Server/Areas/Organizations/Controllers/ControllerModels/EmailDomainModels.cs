using System;
using System.Collections.Generic;

namespace Ccd.Server.Organizations;

public class EmailDomainRequest
{
    public string Domain { get; set; }
}

public class OrganizationEmailDomainResponse
{
    public Guid Id { get; set; }
    public string Domain { get; set; }

    /// <summary>Other organisations whose list contains the same domain.</summary>
    public List<string> AlsoUsedBy { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EmailDomainCheckResponse
{
    /// <summary>The domain as it will be saved (normalised).</summary>
    public string Domain { get; set; }
    public List<string> AlsoUsedBy { get; set; } = new();
}

public class ChangeLogEntryResponse
{
    public Guid Id { get; set; }
    public string EntityType { get; set; }
    public string OrganizationName { get; set; }
    public string SubjectEmail { get; set; }
    public string Action { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public string PerformedByEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}
