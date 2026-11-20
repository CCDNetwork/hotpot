using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ccd.Server.Deduplication;

/// <summary>
/// Single place for the CWG-agreed MPCA booking amount (platform-wide, not per
/// organisation or household). The amount is the agreed entitlement per
/// household: amount per round × rounds, in one of the allowed currencies.
///
/// Today (CWG decision): ILS only — 1250.00 ILS per round, so
///   Rounds 1 → 1250.00 ILS, Rounds 2 → 2500.00 ILS, Rounds 3 → 3750.00 ILS.
///
/// Enforced on booking upload Step 1 (Pre-booking Check and Make Bookings).
/// Existing data was aligned by migration AlignBookingAmountsToRule.
///
/// To change the amounts or allow another currency once the CWG agrees it, edit
/// <see cref="AmountPerRoundByCurrency"/> and release (no effective dates: the
/// change applies to every upload from the release). Each currency needs its
/// own agreed amount, otherwise its bookings can't be checked. Also update the
/// booking template readme and examples.
/// </summary>
public static class BookingAmountRule
{
    /// <summary>Allowed currencies and their agreed amount per round.</summary>
    public static readonly IReadOnlyDictionary<string, decimal> AmountPerRoundByCurrency =
        new Dictionary<string, decimal>
        {
            ["ILS"] = 1250m,
            // e.g. ["USD"] = 340m, once agreed by the CWG.
        };

    /// <summary>Allowed currency codes, e.g. "ILS".</summary>
    public static IReadOnlyList<string> AllowedCurrencies { get; } = AmountPerRoundByCurrency.Keys.ToList();

    /// <summary>Currency as stored: trimmed, upper case (ils → ILS).</summary>
    public static string Normalize(string currency) => currency?.Trim().ToUpperInvariant() ?? "";

    public static bool IsAllowedCurrency(string currency) =>
        AmountPerRoundByCurrency.ContainsKey(Normalize(currency));

    /// <summary>Agreed amount for a booking: amount per round of that currency × rounds.</summary>
    public static decimal ExpectedAmount(string currency, int rounds) =>
        AmountPerRoundByCurrency.TryGetValue(Normalize(currency), out var perRound)
            ? perRound * rounds
            : throw new ArgumentException($"Currency '{currency}' is not allowed for bookings.");

    public static bool IsExpectedAmount(decimal amount, string currency, int rounds) =>
        amount == ExpectedAmount(currency, rounds);

    /// <summary>"ILS" with one allowed currency, "one of ILS, USD" with several.</summary>
    public static string AllowedCurrenciesText =>
        AllowedCurrencies.Count == 1 ? AllowedCurrencies[0] : "one of " + string.Join(", ", AllowedCurrencies);

    /// <summary>Amount as shown in messages: two decimals, no thousands separator (e.g. 2500.00).</summary>
    public static string Format(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
