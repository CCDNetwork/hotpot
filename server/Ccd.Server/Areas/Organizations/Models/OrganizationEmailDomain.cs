using System;
using System.ComponentModel.DataAnnotations;
using Ccd.Server.Helpers;

namespace Ccd.Server.Organizations;

/// <summary>
/// An email domain allowed for users of one organisation. When an organisation
/// has at least one entry, a new user can only be created in it with an email
/// address on one of its domains. Organisations without entries stay
/// unrestricted.
/// </summary>
public class OrganizationEmailDomain
{
    public Guid Id { get; set; } = IdProvider.NewId();
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }

    /// <summary>Normalised: lowercase ASCII, internationalised domains in punycode.</summary>
    [Required]
    public string Domain { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
