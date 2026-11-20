using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using Ccd.Server.Deduplication;
using Xunit;

namespace Ccd.Tests.Deduplication;

/// <summary>
/// CWG-agreed MPCA amount on booking upload (Step 1, both wizards):
/// ILS only, amount = 1250 × rounds.
/// </summary>
public class BookingAmountRuleTests
{
    private static readonly string[] Headers =
        ["headofhouseholdid", "spouseid", "modality", "amount", "currency", "startdate", "enddate", "rounds"];

    private static (List<string> Errors, IXLWorksheet Sheet) Validate(string amount, string currency, string rounds)
    {
        var record = new BookingFileRecord
        {
            HeadOfHouseHoldId = "612997276",
            SpouseId = "",
            Modality = "MPCA",
            Amount = amount,
            Currency = currency,
            StartDate = "20290701",
            EndDate = rounds switch { "2" => "20290831", "3" => "20290930", _ => "20290731" },
            Rounds = rounds,
        };
        var sheet = new XLWorkbook().AddWorksheet("Sheet1");
        var errors = BookingRecordExcelValidator.ValidateAndColorRow(
            record, sheet, 2, (header, _) => System.Array.IndexOf(Headers, header.ToLower()) + 1);
        return (errors, sheet);
    }

    private static bool IsRed(IXLWorksheet sheet, string header) =>
        sheet.Cell(2, System.Array.IndexOf(Headers, header) + 1).Style.Fill.BackgroundColor == XLColor.Red;

    [Theory]
    [InlineData("ILS", 1, 1250)]
    [InlineData("ILS", 2, 2500)]
    [InlineData(" ils ", 3, 3750)]
    public void ExpectedAmount_Is1250IlsPerRound(string currency, int rounds, decimal expected)
    {
        Assert.Equal(expected, BookingAmountRule.ExpectedAmount(currency, rounds));
    }

    [Fact]
    public void IlsIsTheOnlyAllowedCurrency_Today()
    {
        // CWG decision. Allowing another currency = adding it, with its agreed
        // amount per round, to BookingAmountRule.AmountPerRoundByCurrency.
        Assert.Equal(new[] { "ILS" }, BookingAmountRule.AllowedCurrencies);
        Assert.Equal("ILS", BookingAmountRule.AllowedCurrenciesText);
        Assert.True(BookingAmountRule.IsAllowedCurrency("ils"));
        Assert.False(BookingAmountRule.IsAllowedCurrency("USD"));
        Assert.Throws<System.ArgumentException>(() => BookingAmountRule.ExpectedAmount("USD", 1));
    }

    [Theory]
    [InlineData("1250", "ILS", "1")]
    [InlineData("1250.00", "ILS", "1")]
    [InlineData("2,500.00", "ILS", "2")]
    [InlineData("2500", "ils", "2")]
    [InlineData("3750.0", " ILS ", "3")]
    public void AgreedAmountInIls_IsAccepted(string amount, string currency, string rounds)
    {
        var (errors, _) = Validate(amount, currency, rounds);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("1000", "1", "Amount must be 1250.00 ILS for Rounds 1 — got 1000.00 ILS")]
    [InlineData("3750", "1", "Amount must be 1250.00 ILS for Rounds 1 — got 3750.00 ILS")]
    [InlineData("1250", "2", "Amount must be 2500.00 ILS for Rounds 2 — got 1250.00 ILS")]
    [InlineData("2,000.50", "2", "Amount must be 2500.00 ILS for Rounds 2 — got 2000.50 ILS")]
    [InlineData("1250", "3", "Amount must be 3750.00 ILS for Rounds 3 — got 1250.00 ILS")]
    public void OtherAmount_IsRejected_WithTheExpectedAmount(string amount, string rounds, string message)
    {
        var (errors, sheet) = Validate(amount, "ILS", rounds);
        Assert.Equal(message, Assert.Single(errors));
        Assert.True(IsRed(sheet, "amount"));
    }

    [Theory]
    [InlineData("USD", "Currency must be ILS — got USD")]
    [InlineData("EUR", "Currency must be ILS — got EUR")]
    [InlineData("", "Currency must be ILS — got empty")]
    public void OtherCurrency_IsRejected(string currency, string message)
    {
        // The amount check is skipped when the currency is wrong: one clear error.
        var (errors, sheet) = Validate("2000", currency, "2");
        Assert.Equal(message, Assert.Single(errors));
        Assert.True(IsRed(sheet, "currency"));
        Assert.False(IsRed(sheet, "amount"));
    }

    [Fact]
    public void AmountError_IsReportedTogetherWithOtherRowErrors()
    {
        var record = new BookingFileRecord
        {
            HeadOfHouseHoldId = "123",
            SpouseId = "",
            Modality = "MPCA",
            Amount = "1000",
            Currency = "ILS",
            StartDate = "20290701",
            EndDate = "20290731",
            Rounds = "1",
        };
        var sheet = new XLWorkbook().AddWorksheet("Sheet1");
        var errors = BookingRecordExcelValidator.ValidateAndColorRow(
            record, sheet, 2, (header, _) => System.Array.IndexOf(Headers, header.ToLower()) + 1);

        Assert.Contains(errors, e => e.StartsWith("Invalid Head Of Household ID"));
        Assert.Contains(errors, e => e.StartsWith("Amount must be 1250.00 ILS"));
    }

    [Fact]
    public void AmountParsing_DoesNotDependOnTheServerCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            // In Croatian, '.' is the thousands separator and ',' the decimal point.
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("hr-HR");
            Assert.Equal(1250m, ExcelFieldValidator.ParseAmount("1250.00"));
            Assert.Equal(2500m, ExcelFieldValidator.ParseAmount("2,500.00"));
            Assert.Empty(Validate("1250.00", "ILS", "1").Errors);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
