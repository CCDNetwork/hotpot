using System;
using System.Collections.Generic;
using Ccd.Server.Dashboards;
using Xunit;

namespace Ccd.Tests.Dashboards;

public class ExchangeRateLookupTests
{
    private static ExchangeRate Rate(
        string currency,
        decimal rate,
        int year = 2026,
        int month = 7,
        string source = "inforeuro"
    )
    {
        return new ExchangeRate
        {
            Id = Guid.NewGuid(),
            Currency = currency,
            BaseCurrency = "EUR",
            Rate = rate,
            Year = year,
            Month = month,
            Source = source,
            FetchedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void Convert_SameCurrency_ReturnsAmountUnchanged()
    {
        var lookup = new ExchangeRateLookup(new List<ExchangeRate>());

        Assert.Equal(100m, lookup.Convert(100m, "USD", "USD", 2026, 7));
    }

    [Fact]
    public void Convert_PivotsThroughEur()
    {
        // 1 ILS = 0.25 EUR, 1 USD = 0.90 EUR → 100 ILS = 25 EUR = 27.78 USD
        var lookup = new ExchangeRateLookup(new List<ExchangeRate>
        {
            Rate("ILS", 0.25m),
            Rate("USD", 0.90m)
        });

        var converted = lookup.Convert(100m, "ILS", "USD", 2026, 7);

        Assert.NotNull(converted);
        Assert.Equal(27.78m, decimal.Round(converted.Value, 2));
    }

    [Fact]
    public void Convert_EurDisplay_IsSameCodePath()
    {
        var lookup = new ExchangeRateLookup(new List<ExchangeRate> { Rate("ILS", 0.25m) });

        Assert.Equal(25m, lookup.Convert(100m, "ILS", "EUR", 2026, 7));
    }

    [Fact]
    public void Convert_EurNative_IsSameCodePath()
    {
        var lookup = new ExchangeRateLookup(new List<ExchangeRate> { Rate("USD", 0.90m) });

        var converted = lookup.Convert(90m, "EUR", "USD", 2026, 7);

        Assert.NotNull(converted);
        Assert.Equal(100m, decimal.Round(converted.Value, 2));
    }

    [Fact]
    public void Convert_MissingRate_ReturnsNull()
    {
        var lookup = new ExchangeRateLookup(new List<ExchangeRate> { Rate("USD", 0.90m) });

        Assert.Null(lookup.Convert(100m, "ILS", "USD", 2026, 7));
        Assert.Null(lookup.Convert(100m, "USD", "ILS", 2026, 7));
        // Rate exists for a different month
        Assert.Null(lookup.Convert(100m, "USD", "EUR", 2026, 8));
    }

    [Fact]
    public void Convert_ManualRate_OverridesInforeuro()
    {
        var lookup = new ExchangeRateLookup(new List<ExchangeRate>
        {
            Rate("ILS", 0.25m, source: "inforeuro"),
            Rate("ILS", 0.30m, source: "manual")
        });

        Assert.Equal(30m, lookup.Convert(100m, "ILS", "EUR", 2026, 7));
    }

    [Fact]
    public void Convert_IsCaseInsensitiveOnCurrencyCodes()
    {
        var lookup = new ExchangeRateLookup(new List<ExchangeRate> { Rate("ILS", 0.25m) });

        Assert.Equal(25m, lookup.Convert(100m, "ils", "eur", 2026, 7));
    }
}
