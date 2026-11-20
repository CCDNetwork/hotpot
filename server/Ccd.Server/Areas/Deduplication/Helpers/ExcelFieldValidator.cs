using System;
using System.Collections.Generic;
using System.Linq;

namespace Ccd.Server.Deduplication;

public static class ExcelFieldValidator
{
    // Allowed values (can be extended)
    private static readonly HashSet<string> AllowedOrganizations =
    [
        "UNICEF"
    ];

    private static readonly HashSet<string> AllowedActivities =
    [
        "MPCA"
    ];


    // ----------------------------
    // NATIONAL ID
    // ----------------------------
    public static bool IsNationalIdValid(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        return id.All(char.IsDigit) && id.Length == 9;
    }

    // ----------------------------
    // SPOUSE ID
    // ----------------------------
    public static bool IsSpouseIdValid(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return true;

        return id.All(char.IsDigit) && id.Length == 9;
    }

    // ----------------------------
    // ORGANIZATION
    // ----------------------------
    public static bool IsOrganizationValid(string organization)
    {
        if (string.IsNullOrWhiteSpace(organization))
            return false;

        return true;

        // return AllowedOrganizations.Contains(organization.Trim().ToUpper());
    }

    // ----------------------------
    // MODALITY
    // ----------------------------
    public static bool IsModalityValid(string modality)
    {
        if (string.IsNullOrWhiteSpace(modality))
            return false;

        return AllowedActivities.Contains(modality.Trim().ToUpper());
    }

    // Currency: see BookingAmountRule (allowed currencies and agreed amounts).

    // ----------------------------
    // ROUNDS
    // ----------------------------
    public static bool IsRoundsValid(string rounds, out int value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(rounds))
            return false;

        return int.TryParse(rounds, out value) && value is >= 1 and <= 3;
    }

    // ----------------------------
    // AMOUNT
    // ----------------------------
    public static bool IsAmountValid(string amount, out decimal value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(amount))
            return false;

        return TryParseAmount(amount, out value) && value > 0;
    }

    /// <summary>
    /// Amount as written in the upload: '.' is the decimal point and ',' a
    /// thousands separator (1,250.00), independent of the server's culture.
    /// Used by validation and by saving, so both read the same number.
    /// </summary>
    public static bool TryParseAmount(string amount, out decimal value) =>
        decimal.TryParse(
            (amount ?? "").Replace(",", "").Trim(),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out value
        );

    public static decimal ParseAmount(string amount) =>
        TryParseAmount(amount, out var value)
            ? value
            : throw new FormatException($"Invalid amount '{amount}'");

    // ----------------------------
    // DATE FORMAT (YYYYMMDD)
    // ----------------------------
    public static bool IsDateValid(string dateString, out DateTime date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(dateString))
            return false;

        if (dateString.Length != 8 || !dateString.All(char.IsDigit))
            return false;

        return DateTime.TryParseExact(
            dateString,
            "yyyyMMdd",
            null,
            System.Globalization.DateTimeStyles.None,
            out date
        );
    }

    // ----------------------------
    // DATE RANGE: End Date is the LAST covered day (inclusive) and must equal
    // Start Date + rounds calendar months - 1 day.
    // ----------------------------
    public static bool IsDateRangeValid(DateTime start, DateTime inclusiveEnd, int rounds)
    {
        return inclusiveEnd >= start && inclusiveEnd == BookingDates.ExpectedInclusiveEnd(start, rounds);
    }
}
