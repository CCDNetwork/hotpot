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
            errors.Add("Invalid Head Of Household ID");
            MarkInvalid("headofhouseholdid");
        }

        // ----------------------------
        // SPOUSE ID
        // ----------------------------
        if (!ExcelFieldValidator.IsSpouseIdValid(record.SpouseId))
        {
            errors.Add("Invalid Spouse ID");
            MarkInvalid("spouseid");
        }

        // ----------------------------
        // Modality
        // ----------------------------
        if (!ExcelFieldValidator.IsModalityValid(record.Modality))
        {
            errors.Add("Invalid Modality");
            MarkInvalid("Modality");
        }


        // ----------------------------
        // AMOUNT
        // ----------------------------
        if (!ExcelFieldValidator.IsAmountValid(record.Amount, out var _))
        {
            errors.Add("Invalid Amount");
            MarkInvalid("amount");
        }

        // ----------------------------
        // CURRENCY
        // ----------------------------
        if (!ExcelFieldValidator.IsCurrencyValid(record.Currency))
        {
            errors.Add("Invalid Currency");
            MarkInvalid("currency");
        }

        // ----------------------------
        // START DATE
        // ----------------------------
        if (!ExcelFieldValidator.IsDateValid(record.StartDate, out var start))
        {
            errors.Add("Invalid Start Date");
            MarkInvalid("startdate");
        }

        // ----------------------------
        // END DATE
        // ----------------------------
        if (!ExcelFieldValidator.IsDateValid(record.EndDate, out var end))
        {
            errors.Add("Invalid End Date");
            MarkInvalid("enddate");
        }

        // ----------------------------
        // ROUNDS
        // ----------------------------
        if (!ExcelFieldValidator.IsRoundsValid(record.Rounds, out var roundsValue))
        {
            errors.Add("Rounds must be 1 or 3");
            MarkInvalid("rounds");
        }

        // ----------------------------
        // RANGE CHECK (only if both parsed)
        // ----------------------------
        if (errors.Count == 0 && !ExcelFieldValidator.IsDateRangeValid(start, end, roundsValue))
        {
            errors.Add($"Date range must not exceed {roundsValue} month(s)");
            MarkInvalid("startdate");
            MarkInvalid("enddate");
        }

        return errors;
    }
}
