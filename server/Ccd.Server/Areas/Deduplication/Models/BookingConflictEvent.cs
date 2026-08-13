using System;
using System.ComponentModel.DataAnnotations.Schema;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;

namespace Ccd.Server.Deduplication;

public class BookingConflictEvent
{
    public Guid Id { get; set; } = IdProvider.NewId();

    // Fingerprint components — uniqueness enforced by idx_conflict_event_fingerprint
    [ForeignKey("RequestingOrganization")] public Guid RequestingOrganizationId { get; set; }
    public Organization RequestingOrganization { get; set; }
    [ForeignKey("BlockingOrganization")] public Guid BlockingOrganizationId { get; set; }
    public Organization BlockingOrganization { get; set; }
    public string SubjectKey { get; set; }
    public DateOnly OverlapStartDate { get; set; }
    public DateOnly OverlapEndDate { get; set; }

    // Non-uniqueness-bearing reference for drill-down; populated on first detection
    [ForeignKey("BlockingBooking")] public Guid? BlockingBookingId { get; set; }
    public Booking BlockingBooking { get; set; }

    // Requesting-partner proposed values (source-of-truth; never converted on write)
    public decimal ProposedAmount { get; set; }
    public string ProposedCurrency { get; set; }
    public string ProposedModality { get; set; }
    public int ProposedRounds { get; set; }

    public DateTime FirstDetectedAt { get; set; }
    public DateTime LastDetectedAt { get; set; }
    public int DetectionCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
