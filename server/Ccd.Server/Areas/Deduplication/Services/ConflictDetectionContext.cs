using System;

namespace Ccd.Server.Deduplication;

/// <summary>
/// Everything ProcessValidBookings needs to record a booking_conflict_event for
/// a row that ValidateDatabaseDuplicates marked as a DB duplicate. The subject
/// is the uploaded ID that matched the existing booking — it can be either the
/// head-of-household ID or the spouse ID.
/// </summary>
public record ConflictDetectionContext(
    Guid BlockingBookingId,
    Guid BlockingOrganizationId,
    string MatchedIdPlaintext,
    DateTime ProposedStart,
    DateTime ProposedEnd,
    decimal ProposedAmount,
    string ProposedCurrency,
    string ProposedModality,
    int ProposedRounds
);
