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
        if (!ExcelFieldValidator.IsAmountValid(record.Amount, out var _))
        {
            errors.Add("Invalid Amount — expected a positive number (e.g., 1250.00)");
            MarkInvalid("amount");
        }

        // ----------------------------
        // CURRENCY
        // ----------------------------
        if (!ExcelFieldValidator.IsCurrencyValid(record.Currency))
        {
            errors.Add("Invalid Currency — expected an ISO-4217 code (e.g., ILS, USD, EUR)");
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
            errors.Add("Invalid End Date — expected an 8-digit YYYYMMDD value (e.g., 20260301)");
            MarkInvalid("enddate");
        }

        // ----------------------------
        // ROUNDS
        // ----------------------------
        if (!ExcelFieldValidator.IsRoundsValid(record.Rounds, out var roundsValue))
        {
            errors.Add("Invalid Rounds — expected 1 or 3");
            MarkInvalid("rounds");
        }

        // ----------------------------
        // RANGE CHECK (only if fields parsed)
        // The check is exact equality: End Date must equal Start Date + rounds
        // calendar months (day-of-month preserved). A range too short OR too
        // long fails the same way — surface the specific expected end date so
        // the user can see whether to shorten, extend, or adjust rounds.
        // ----------------------------
        if (errors.Count == 0 && !ExcelFieldValidator.IsDateRangeValid(start, end, roundsValue))
        {
            // End Date is exclusive: the first calendar day NOT covered by the
            // booking. So a rounds=1 booking starting on the 1st of a month
            // has End Date = the 1st of the next month (last covered day is
            // the last day of the start month). Message makes that explicit.
            var expectedEnd = start.AddMonths(roundsValue);
            errors.Add(
                $"End Date must be Start Date + {roundsValue} calendar month(s) — "
                + $"expected {expectedEnd:yyyyMMdd} (the first day NOT covered) "
                + $"for Start Date {start:yyyyMMdd} with Rounds {roundsValue} "
                + $"(got {end:yyyyMMdd})"
            );
            MarkInvalid("startdate");
            MarkInvalid("enddate");
        }

        return errors;
    }
}
