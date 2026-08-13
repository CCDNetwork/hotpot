using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ccd.Server.Dashboards;

/// <summary>
/// Hangfire job: pulls InforEuro monthly rates and upserts one exchange_rate row
/// per currency for the (year, month, source) triple. Idempotent — running twice
/// does not create duplicates. Runs monthly (Run) and on startup (Bootstrap) to
/// backfill the last N months so historical dashboards render from day one.
/// </summary>
public class ExchangeRateFetchJob
{
    public const string JobId = "exchange-rate-fetch";

    private readonly CcdContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRateFetchJob> _logger;

    public ExchangeRateFetchJob(
        CcdContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<ExchangeRateFetchJob> logger
    )
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Monthly cron entrypoint — refreshes the current month (InforEuro can
    /// update rates mid-month) and re-fetches the previous month too so any
    /// late publication is picked up.
    /// </summary>
    public async Task Run()
    {
        var now = DateTime.UtcNow;
        await SafeFetchMonth(now.Year, now.Month);
        var previous = now.AddMonths(-1);
        await SafeFetchMonth(previous.Year, previous.Month);
    }

    /// <summary>
    /// Startup entrypoint — fetches the current month unconditionally, then
    /// backfills the previous N months (only those missing from the table).
    /// Prevents the "current month missing until next cron" gap that showed
    /// up on every deployment during the first days of a new month.
    /// </summary>
    public async Task Bootstrap()
    {
        var now = DateTime.UtcNow;
        // Current month first, unconditionally: even if a row exists, the
        // upsert refreshes it to whatever InforEuro is publishing today.
        await SafeFetchMonth(now.Year, now.Month);

        // Then backfill history, skipping any month already present.
        var months = StaticConfiguration.FxBootstrapMonths;
        var cursor = now;
        for (var i = 0; i < months; i++)
        {
            cursor = cursor.AddMonths(-1);

            var hasMonth = await _context.ExchangeRates.AnyAsync(r =>
                r.Year == cursor.Year
                && r.Month == cursor.Month
                && r.Source == StaticConfiguration.FxSourceProvider
            );

            if (hasMonth)
                continue;

            await SafeFetchMonth(cursor.Year, cursor.Month);
        }
    }

    private async Task SafeFetchMonth(int year, int month)
    {
        try
        {
            await FetchMonth(year, month);
        }
        catch (Exception ex)
        {
            // A missing month degrades to "no rate" indicators on the
            // dashboard; it must not block startup or the remaining months.
            _logger.LogError(ex, "FX fetch failed for {Year}-{Month}", year, month);
        }
    }

    private async Task FetchMonth(int year, int month)
    {
        var url = $"{StaticConfiguration.FxSourceBaseUrl}?year={year}&month={month}";
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        var rates = ParseInforEuroPayload(payload);

        if (rates.Count == 0)
        {
            _logger.LogWarning("InforEuro returned no parseable rates for {Year}-{Month}", year, month);
            return;
        }

        var source = StaticConfiguration.FxSourceProvider;
        var existing = await _context.ExchangeRates
            .Where(r => r.Year == year && r.Month == month && r.Source == source)
            .ToListAsync();

        var upserted = 0;
        foreach (var (currency, amountPerEur) in rates)
        {
            if (amountPerEur <= 0)
                continue;

            // InforEuro publishes units-per-EUR; we store the inverse so that
            // 1 unit of currency = rate units of EUR.
            var rateToEur = decimal.Round(1m / amountPerEur, 12);

            var row = existing.FirstOrDefault(r => r.Currency == currency);
            if (row != null)
            {
                row.Rate = rateToEur;
                row.BaseCurrency = StaticConfiguration.FxSourceBaseCurrency;
                row.FetchedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ExchangeRates.Add(new ExchangeRate
                {
                    Id = IdProvider.NewId(),
                    Currency = currency,
                    BaseCurrency = StaticConfiguration.FxSourceBaseCurrency,
                    Rate = rateToEur,
                    Year = year,
                    Month = month,
                    Source = source,
                    FetchedAt = DateTime.UtcNow
                });
            }

            upserted++;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Upserted {Count} exchange rates for {Year}-{Month}", upserted, year, month);
    }

    /// <summary>
    /// Tolerant parse of the InforEuro monthly-rates payload: accepts a root
    /// array or an object wrapping one, and several candidate property names for
    /// the ISO code and the units-per-EUR value.
    /// </summary>
    public static Dictionary<string, decimal> ParseInforEuroPayload(string payload)
    {
        var result = new Dictionary<string, decimal>();

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var array = root;
        if (root.ValueKind == JsonValueKind.Object)
        {
            array = default;
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    array = property.Value;
                    break;
                }
            }
        }

        if (array.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var iso = GetString(item, "isoA3Code", "currencyIso", "currencyIsoCode", "iso", "code");
            var amount = GetDecimal(item, "value", "amount", "rate");

            if (string.IsNullOrWhiteSpace(iso) || amount == null)
                continue;

            result[iso.Trim().ToUpperInvariant()] = amount.Value;
        }

        return result;
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }

    private static decimal? GetDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return number;

            if (value.ValueKind == JsonValueKind.String
                && decimal.TryParse(
                    value.GetString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsed
                ))
                return parsed;
        }

        return null;
    }
}
