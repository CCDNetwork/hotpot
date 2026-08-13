using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Dashboards;

public class ExchangeRateService
{
    private readonly CcdContext _context;

    public ExchangeRateService(CcdContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loads the full rate table into an in-memory lookup. The table is small
    /// (tens of currencies × months), and dashboard queries convert many
    /// (currency, month) groups per request — one read beats N lookups.
    /// </summary>
    public async Task<ExchangeRateLookup> GetLookupAsync()
    {
        var rates = await _context.ExchangeRates.AsNoTracking().ToListAsync();
        return new ExchangeRateLookup(rates);
    }
}

public class ExchangeRateLookup
{
    private readonly Dictionary<(string Currency, int Year, int Month), decimal> _rates = new();

    public ExchangeRateLookup(IEnumerable<ExchangeRate> rates)
    {
        // 'manual' rows override 'inforeuro' rows for the same (currency, year, month)
        foreach (var rate in rates.OrderBy(r => r.Source == "manual" ? 1 : 0))
            _rates[(rate.Currency.ToUpperInvariant(), rate.Year, rate.Month)] = rate.Rate;
    }

    /// <summary>1 unit of currency = X EUR; null when no rate is known.</summary>
    public decimal? RateToEur(string currency, int year, int month)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return null;

        if (string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
            return 1m;

        return _rates.TryGetValue((currency.ToUpperInvariant(), year, month), out var rate)
            ? rate
            : null;
    }

    /// <summary>
    /// Generic pivot through EUR: display = native × rate(native→EUR) / rate(display→EUR).
    /// EUR display and native == display fall out of the same formula — no special
    /// code path. Returns null when a needed rate is missing ("missing_rate").
    /// </summary>
    public decimal? Convert(decimal amount, string native, string display, int year, int month)
    {
        if (string.Equals(native, display, StringComparison.OrdinalIgnoreCase))
            return amount;

        var nativeToEur = RateToEur(native, year, month);
        var displayToEur = RateToEur(display, year, month);

        if (nativeToEur == null || displayToEur is null or 0)
            return null;

        return amount * nativeToEur.Value / displayToEur.Value;
    }
}
