using ClosedXML.Excel;
using System;
using System.Collections.Generic;

namespace Ccd.Server.Deduplication;

public static class BookingRecordExcelValidator
{
    public static List<string> ValidateAndColorRow(
        BookingFileRecord record,
        IXLWorksheet worksheet,
        int rowNumber,
        Func<string, IXLWorksheet, int> getHeaderIndex
    )
    {
        var errors = new List<string>();

        // Helper to mark a cell red
        void MarkInvalid(string header)
        {
            int col = getHeaderIndex(header, worksheet);
            worksheet.Cell(rowNumber, col).Style.Fill.BackgroundColor = XLColor.Red;
        }

        // ----------------------------
        // HEAD OF HOUSEHOLD ID
        // ----------------------------
        if (!ExcelFieldValidator.IsNationalIdValid(record.HeadOfHouseHoldId))
        {
            errors.Add("Invalid Head Of Household ID — expected a 9-digit number");
            MarkInvalid("headofhouseholdid");
        }

        // ----------------------------
        // SPOUSE ID
        // ----------------------------
        if (!ExcelFieldValidator.IsSpouseIdValid(record.SpouseId))
        {
            errors.Add("Invalid Spouse ID — expected a 9-digit number or empty");
            MarkInvalid("spouseid");
        }

        // ----------------------------
        // Modality
        // ----------------------------
        if (!ExcelFieldValidator.IsModalityValid(record.Modality))
        {
            errors.Add("Invalid Modality — expected MPCA");
            MarkInvalid("Modality");
        }


        // ----------------------------
        // AMOUNT
        // ----------------------------
        if (!ExcelFieldValidator.IsAmountValid(record.Amount, out var amountValue))
        {
            errors.Add("Invalid Amount — expected a positive number (e.g., 1250.00)");
            MarkInvalid("amount");
        }

        // ----------------------------
        // CURRENCY — must be in BookingAmountRule (today ILS only: the CWG
        // defined the transfer value in ILS)
        // ----------------------------
        var currencyValid = BookingAmountRule.IsAllowedCurrency(record.Currency);
        if (!currencyValid)
        {
            var got = string.IsNullOrWhiteSpace(record.Currency) ? "empty" : record.Currency.Trim();
            errors.Add($"Currency must be {BookingAmountRule.AllowedCurrenciesText} — got {got}");
            MarkInvalid("currency");
        }

        // ----------------------------
        // START DATE
        // ----------------------------
        if (!ExcelFieldValidator.IsDateValid(record.StartDate, out var start))
        {
            errors.Add("Invalid Start Date — expected an 8-digit YYYYMMDD value (e.g., 20260201)");
            MarkInvalid("startdate");
        }

        // ----------------------------
        // END DATE
        // ----------------------------
        if (!ExcelFieldValidator.IsDateValid(record.EndDate, out var end))
        {
            errors.Add("Invalid End Date — expected an 8-digit YYYYMMDD value (e.g., 20260228)");
            MarkInvalid("enddate");
        }

        // ----------------------------
        // ROUNDS
        // ----------------------------
        if (!ExcelFieldValidator.IsRoundsValid(record.Rounds, out var roundsValue))
        {
            errors.Add("Invalid Rounds — expected 1, 2 or 3");
            MarkInvalid("rounds");
        }
        else if (currencyValid
                 && ExcelFieldValidator.IsAmountValid(record.Amount, out _)
                 && !BookingAmountRule.IsExpectedAmount(amountValue, record.Currency, roundsValue))
        {
            // ----------------------------
            // AMOUNT ↔ ROUNDS (CWG-agreed amount per currency; see BookingAmountRule)
            // ----------------------------
            var currency = BookingAmountRule.Normalize(record.Currency);
            var expected = BookingAmountRule.ExpectedAmount(currency, roundsValue);
            errors.Add(
                $"Amount must be {BookingAmountRule.Format(expected)} {currency} for Rounds {roundsValue} — got "
                + $"{BookingAmountRule.Format(amountValue)} {currency}"
            );
            MarkInvalid("amount");
        }

        // ----------------------------
        // RANGE CHECK (only if fields parsed)
        // The check is exact equality: End Date is the LAST covered day and
        // must equal Start Date + rounds calendar months - 1 day. A range too
        // short OR too long fails the same way — surface the specific expected
        // end date so the user can see whether to shorten, extend, or adjust
        // rounds. (Stored as uploaded; see BookingDates.)
        // ----------------------------
        if (errors.Count == 0 && !ExcelFieldValidator.IsDateRangeValid(start, end, roundsValue))
        {
            var expectedEnd = BookingDates.ExpectedInclusiveEnd(start, roundsValue);
            errors.Add(
                $"End Date must be the last day covered: Start Date + {roundsValue} calendar month(s) - 1 day — "
                + $"expected {expectedEnd:yyyyMMdd} "
                + $"for Start Date {start:yyyyMMdd} with Rounds {roundsValue} "
                + $"(got {end:yyyyMMdd})"
            );
            MarkInvalid("startdate");
            MarkInvalid("enddate");
        }

        return errors;
    }
}
