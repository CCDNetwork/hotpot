using System;
using Ccd.Server.Deduplication;
using Xunit;

namespace Ccd.Tests.Deduplication;

/// <summary>
/// End Date is inclusive (last covered day) in upload, storage and queries.
/// These tests pin the upload rule and its consistency with migrated data.
/// </summary>
public class BookingDatesTests
{
    [Theory]
    [InlineData("2026-02-01", 1, "2026-02-28")]
    [InlineData("2026-02-01", 3, "2026-04-30")]
    [InlineData("2026-01-15", 2, "2026-03-14")]
    [InlineData("2026-11-01", 2, "2026-12-31")]
    [InlineData("2026-12-01", 1, "2026-12-31")] // year boundary
    [InlineData("2028-01-30", 1, "2028-02-28")] // AddMonths clamps to Feb 29 (leap year), minus one day
    [InlineData("2027-01-31", 1, "2027-02-27")] // AddMonths clamps to Feb 28, minus one day
    public void ExpectedInclusiveEnd_IsStartPlusRoundsMinusOneDay(string start, int rounds, string expected)
    {
        Assert.Equal(DateTime.Parse(expected), BookingDates.ExpectedInclusiveEnd(DateTime.Parse(start), rounds));
    }

    [Theory]
    [InlineData("2026-02-01", 1)]
    [InlineData("2026-02-01", 3)]
    [InlineData("2028-01-30", 1)]
    [InlineData("2027-01-31", 1)]
    public void MigratedEnd_EqualsTheNewInclusiveRule(string start, int rounds)
    {
        // Before migration ConvertBookingEndDateToInclusive, HotPot stored
        // Start + rounds months (exclusive). The migration subtracts one day;
        // the result must equal what an upload under the new rule stores, so
        // old and new bookings compare consistently.
        var startDate = DateTime.Parse(start);
        var oldStored = startDate.AddMonths(rounds);
        Assert.Equal(BookingDates.ExpectedInclusiveEnd(startDate, rounds), oldStored.AddDays(-1));
    }

    [Theory]
    [InlineData("2026-02-01", "2026-04-30", 3, true)]
    [InlineData("2026-02-01", "2026-05-01", 3, false)] // old exclusive value is now rejected
    [InlineData("2026-02-01", "2026-04-29", 3, false)]
    [InlineData("2026-02-01", "2026-02-28", 1, true)]
    public void UploadRangeRule_UsesInclusiveEnd(string start, string end, int rounds, bool valid)
    {
        Assert.Equal(valid, ExcelFieldValidator.IsDateRangeValid(DateTime.Parse(start), DateTime.Parse(end), rounds));
    }
}
