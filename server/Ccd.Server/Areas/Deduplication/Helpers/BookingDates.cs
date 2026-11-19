using System;

namespace Ccd.Server.Deduplication;

/// <summary>
/// Single place for the booking End Date convention.
///
/// End Date is INCLUSIVE everywhere — upload, storage (booking, booking_log,
/// booking_conflict_event.overlap_end_date), API responses, exports and every
/// overlap / activity / dashboard query: it is the LAST day covered.
///
///   3 rounds from 2026-02-01 → End Date 2026-04-30.
///
/// Two bookings overlap when existing.End >= incoming.Start; a booking ending
/// 2026-01-31 and one starting 2026-02-01 are back-to-back (no overlap).
/// Data stored before migration ConvertBookingEndDateToInclusive used the
/// exclusive end (first day NOT covered); that migration shifted it by -1 day.
/// </summary>
public static class BookingDates
{
    /// <summary>Expected End Date (last covered day) for a booking of the given rounds.</summary>
    public static DateTime ExpectedInclusiveEnd(DateTime start, int rounds) =>
        start.AddMonths(rounds).AddDays(-1);
}
