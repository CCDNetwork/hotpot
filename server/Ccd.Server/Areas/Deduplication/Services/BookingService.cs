using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Storage;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Deduplication;

public class BookingService
{
    private readonly CcdContext _context;
    private readonly IMapper _mapper;

    private readonly IStorageService _storageService;

    private readonly Dictionary<string, int> HeaderIndexCache = new();

    public BookingService(CcdContext context, IMapper mapper, IStorageService storageService)
    {
        _context = context;
        _mapper = mapper;
        _storageService = storageService;
    }

    private readonly string _selectSql =
    @$"SELECT DISTINCT ON (b.id)
                 b.*
            FROM
                 booking as b
            WHERE
                (@organizationId is null OR b.organization_id = @organizationId)
                AND (
                    @activities::text[] IS NULL AND b.start_date IS NOT NULL AND b.end_date IS NOT NULL
                    OR @activities::text[] IS NOT NULL AND (
                        ('previous' = ANY(@activities::text[]) AND b.start_date IS NOT NULL AND b.end_date IS NOT NULL AND b.end_date < CURRENT_DATE)
                        OR
                        ('current'  = ANY(@activities::text[]) AND b.start_date IS NOT NULL AND b.end_date IS NOT NULL AND b.start_date <= CURRENT_DATE AND b.end_date >= CURRENT_DATE)
                        OR
                        ('upcoming' = ANY(@activities::text[]) AND b.start_date IS NOT NULL AND b.end_date IS NOT NULL AND b.start_date > CURRENT_DATE)
                        OR
                        ('released' = ANY(@activities::text[]) AND b.start_date IS NULL AND b.end_date IS NULL)
                    )
                )";

    private object getSelectSqlParams(Guid? organizationId = null, string[] activities = null)
    {
        return new { organizationId, activities };
    }

    private async Task resolveDependencies(BookingResponse listing)
    {
        await Task.CompletedTask;
    }


    public async Task<PagedApiResponse<BookingResponse>> GetAllBookingsApi(Guid organizationId, RequestParameters requestParameters, string activity)
    {
        var activityFilter = activity?
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(BookingActivity.IsValidActivity)
            .Distinct()
            .ToArray();

        if (activityFilter?.Length == 0)
            activityFilter = null;

        return await PagedApiResponse<BookingResponse>.GetFromSql(
            _context,
            _selectSql,
            getSelectSqlParams(organizationId, activityFilter),
            requestParameters,
            resolveDependencies
        );
    }


    public async Task<(bool, string, Guid)> BookingDeduplicationStep1(Guid organizationId, Guid userId, BookingDeduplicationRequestStep1 model)
    {
        var file = model.File ?? throw new BadRequestException("File is required");
        using var workbook = new XLWorkbook(file.OpenReadStream());

        var worksheet = workbook.Worksheet(1);
        var lastColumnIndex = worksheet.LastColumnUsed().ColumnNumber() + 1;
        var lastRowNumber = worksheet.LastRowUsed().RowNumber();

        // Add headers
        worksheet.Cell(1, lastColumnIndex).Value = "Duplicate of (excel row number)";
        worksheet.Cell(1, lastColumnIndex).Style.Fill.BackgroundColor = XLColor.Gainsboro;
        worksheet.Cell(1, lastColumnIndex).Style.Font.Bold = true;

        // STEP 1 — Validate row fields
        var isExcelValid = ValidateRowFields(worksheet, lastRowNumber);

        // STEP 2 — Validate internal Excel duplicates
        var allValidNationalIds = ValidateExcelDuplicates(worksheet, lastRowNumber, lastColumnIndex, ref isExcelValid);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        var savedFile = await _storageService.SaveFile(StorageType.GetById(StorageType.Assets.Id), memoryStream, userId, model.File.FileName);
        var fileApi = await _storageService.GetFileApiById(savedFile.Id);

        return (isExcelValid, fileApi.Url, savedFile.Id);
    }

    public async Task<(bool, string, Guid)> BookingDeduplicationStep2(Guid organizationId, Guid userId, BookingDeduplicationRequestStep2 model)
    {
        var file = await _storageService.GetFileById(model.FileId) ?? throw new BadRequestException("File not found");
        using var workbook = new XLWorkbook(_storageService.GetFileStream(file));

        var worksheet = workbook.Worksheet(1);
        var lastColumnIndex = worksheet.LastColumnUsed().ColumnNumber() + 1;
        var lastRowNumber = worksheet.LastRowUsed().RowNumber();

        // Add headers
        worksheet.Cell(1, lastColumnIndex).Value = "Already booked";
        worksheet.Cell(1, lastColumnIndex).Style.Fill.BackgroundColor = XLColor.Gainsboro;
        worksheet.Cell(1, lastColumnIndex).Style.Font.Bold = true;

        var isExcelValid = true;

        // STEP 1 — On BookingDeduplicationStep1 we already validated fields and internal duplicates
        var allValidNationalIds = GetAllValidNationalIds(worksheet, lastRowNumber);

        // STEP 2 — Validate duplicates from DB
        ValidateDatabaseDuplicates(worksheet, lastRowNumber, lastColumnIndex, allValidNationalIds, ref isExcelValid);

        await ProcessValidBookings(organizationId, userId, file.Id, worksheet, lastRowNumber, lastColumnIndex, allValidNationalIds);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        var savedFile = await _storageService.SaveFile(StorageType.GetById(StorageType.Assets.Id), memoryStream, userId, file.FileName);
        var fileApi = await _storageService.GetFileApiById(savedFile.Id);

        return (isExcelValid, fileApi.Url, savedFile.Id);
    }


    private bool ValidateRowFields(IXLWorksheet worksheet, int lastRowNumber)
    {
        bool isValid = true;

        for (var i = 2; i <= lastRowNumber; i++)
        {
            var headOfHouseHoldCell = worksheet.Cell(i, GetHeaderIndex("headofhouseholdid", worksheet));
            var spouseIdCell = worksheet.Cell(i, GetHeaderIndex("spouseid", worksheet));
            var modalityCell = worksheet.Cell(i, GetHeaderIndex("modality", worksheet));
            var amountCell = worksheet.Cell(i, GetHeaderIndex("amount", worksheet));
            var currencyCell = worksheet.Cell(i, GetHeaderIndex("currency", worksheet));
            var startDateCell = worksheet.Cell(i, GetHeaderIndex("startdate", worksheet));
            var endDateCell = worksheet.Cell(i, GetHeaderIndex("enddate", worksheet));
            var roundsCell = worksheet.Cell(i, GetHeaderIndex("rounds", worksheet));

            var fileRecord = new BookingFileRecord
            {
                HeadOfHouseHoldId = headOfHouseHoldCell.Value.ToString(),
                SpouseId = spouseIdCell.Value.ToString(),
                Modality = modalityCell.Value.ToString(),
                Amount = amountCell.Value.ToString(),
                Currency = currencyCell.Value.ToString(),
                StartDate = startDateCell.Value.ToString(),
                EndDate = endDateCell.Value.ToString(),
                Rounds = roundsCell.Value.ToString(),
            };

            var errors = BookingRecordExcelValidator.ValidateAndColorRow(
                fileRecord,
                worksheet,
                i,
                GetHeaderIndex
            );

            if (errors.Count != 0)
            {
                Console.WriteLine($"Row {i} errors: {string.Join(", ", errors)}");
                isValid = false;
            }
        }

        return isValid;
    }

    private HashSet<string> GetAllValidNationalIds(IXLWorksheet worksheet, int lastRowNumber)
    {
        var hohIndex = GetHeaderIndex("headofhouseholdid", worksheet);
        var spouseIndex = GetHeaderIndex("spouseid", worksheet);

        var allValidIds = new HashSet<string>();

        for (var i = 2; i <= lastRowNumber; i++)
        {
            var hohId = worksheet.Cell(i, hohIndex).GetString().Trim();
            var spouseId = worksheet.Cell(i, spouseIndex).GetString().Trim();

            if (!string.IsNullOrWhiteSpace(hohId))
            {
                allValidIds.Add(hohId);
            }

            if (!string.IsNullOrWhiteSpace(spouseId))
            {
                allValidIds.Add(spouseId);
            }
        }

        return allValidIds;
    }

    private HashSet<string> ValidateExcelDuplicates(
        IXLWorksheet worksheet,
        int lastRowNumber,
        int duplicateColumnIndex,
        ref bool isExcelValid
    )
    {
        var hohIndex = GetHeaderIndex("headofhouseholdid", worksheet);
        var spouseIndex = GetHeaderIndex("spouseid", worksheet);

        // Global map → ID appears only once across both columns
        var globalDict = new Dictionary<string, int>();

        // Return set for DB checks
        var allValidIds = new HashSet<string>();

        for (var i = 2; i <= lastRowNumber; i++)
        {
            var hohId = worksheet.Cell(i, hohIndex).GetString().Trim();
            var spouseId = worksheet.Cell(i, spouseIndex).GetString().Trim();

            // ---- HANDLE HoH ID ----
            if (!string.IsNullOrWhiteSpace(hohId))
            {
                if (globalDict.TryGetValue(hohId, out int firstRow))
                {
                    // Mark duplicate in the SINGLE duplicate column
                    var duplicateCell = worksheet.Cell(i, duplicateColumnIndex);
                    duplicateCell.Value = firstRow;
                    duplicateCell.Style.Fill.BackgroundColor = XLColor.Redwood;

                    isExcelValid = false;
                }
                else
                {
                    globalDict[hohId] = i;
                    allValidIds.Add(hohId);
                }
            }

            // ---- HANDLE Spouse ID ----
            if (!string.IsNullOrWhiteSpace(spouseId))
            {
                if (globalDict.TryGetValue(spouseId, out int firstRow))
                {
                    // Mark duplicate in SAME duplicate column
                    var duplicateCell = worksheet.Cell(i, duplicateColumnIndex);
                    duplicateCell.Value = firstRow;
                    duplicateCell.Style.Fill.BackgroundColor = XLColor.Redwood;

                    isExcelValid = false;
                }
                else
                {
                    globalDict[spouseId] = i;
                    allValidIds.Add(spouseId);
                }
            }
        }

        return allValidIds;
    }

    private void ValidateDatabaseDuplicates(
        IXLWorksheet worksheet,
        int lastRowNumber,
        int alreadyBookedColumnIndex,
        HashSet<string> allExcelIds,
        ref bool isExcelValid
    )
    {
        var hohIndex = GetHeaderIndex("headofhouseholdid", worksheet);
        var spouseIndex = GetHeaderIndex("spouseid", worksheet);
        var startDateIndex = GetHeaderIndex("startdate", worksheet);

        // Get all overlapping DB records for the Excel IDs (skip released bookings)
        var existingBookings = _context.Bookings
            .Where(b => (allExcelIds.Contains(b.HouseholdId) ||
                        allExcelIds.Contains(b.SpouseId)) &&
                        b.StartDate != null && b.EndDate != null)
            .Include(b => b.Organization)
            .ToList();

        for (int row = 2; row <= lastRowNumber; row++)
        {
            var hohId = worksheet.Cell(row, hohIndex).GetString().Trim();
            var spouseId = worksheet.Cell(row, spouseIndex).GetString().Trim();
            var startDateStr = worksheet.Cell(row, startDateIndex).GetString().Trim();

            if (string.IsNullOrWhiteSpace(startDateStr))
                continue;

            var startDate = ParseExcelDateUtc(startDateStr);

            string matchedId = null;
            Booking dbRecord = null;

            // Check HoH ID
            if (!string.IsNullOrWhiteSpace(hohId))
            {
                dbRecord = existingBookings.FirstOrDefault(b =>
                    (b.HouseholdId == hohId || b.SpouseId == hohId) &&
                    b.EndDate >= startDate
                );

                if (dbRecord != null)
                    matchedId = hohId;
            }

            // Check Spouse ID (only if HoH didn't match)
            if (dbRecord == null && !string.IsNullOrWhiteSpace(spouseId))
            {
                dbRecord = existingBookings.FirstOrDefault(b =>
                    (b.HouseholdId == spouseId || b.SpouseId == spouseId) &&
                    b.EndDate >= startDate
                );

                if (dbRecord != null)
                    matchedId = spouseId;
            }

            if (dbRecord != null)
            {
                var alreadyBookedCell = worksheet.Cell(row, alreadyBookedColumnIndex);
                alreadyBookedCell.Value =
                    $"{matchedId} already has a booking by {dbRecord.Organization.Name} from {dbRecord.StartDate:yyyy-MM-dd} until {dbRecord.EndDate:yyyy-MM-dd}";
                alreadyBookedCell.Style.Fill.BackgroundColor = XLColor.RedPigment;

                isExcelValid = false;
            }
        }
    }


    private async Task ProcessValidBookings(
        Guid organizationId,
        Guid userId,
        Guid savedFileId,
        IXLWorksheet worksheet,
        int lastRowNumber,
        int alreadyBookedColumnIndex,
        HashSet<string> allValidExcelIds
    )
    {
        var hohIndex = GetHeaderIndex("headofhouseholdid", worksheet);
        var spouseIndex = GetHeaderIndex("spouseid", worksheet);
        var startDateIndex = GetHeaderIndex("startdate", worksheet);
        var endDateIndex = GetHeaderIndex("enddate", worksheet);
        var amountIndex = GetHeaderIndex("amount", worksheet);
        var currencyIndex = GetHeaderIndex("currency", worksheet);
        var roundsIndex = GetHeaderIndex("rounds", worksheet);
        var modalityIndex = GetHeaderIndex("modality", worksheet);

        // NEW DB fetch: get all bookings that match any Excel ID in either column (skip released bookings)
        var existingBookings = _context.Bookings
            .Where(b => (allValidExcelIds.Contains(b.HouseholdId) ||
                        allValidExcelIds.Contains(b.SpouseId)) &&
                        b.StartDate != null && b.EndDate != null)
            .ToList();

        var bookingLogs = new List<BookingLog>();

        // For fast lookup
        Booking FindExisting(string id)
        {
            return existingBookings.FirstOrDefault(b =>
                b.HouseholdId == id || b.SpouseId == id
            );
        }

        for (int row = 2; row <= lastRowNumber; row++)
        {
            var hohId = worksheet.Cell(row, hohIndex).GetString().Trim();
            var spouseId = worksheet.Cell(row, spouseIndex).GetString().Trim();

            var isSuccess = true;

            // If Excel validation colored this row red → fail
            if (worksheet.Cell(row, hohIndex).Style.Fill.BackgroundColor == XLColor.Red ||
                worksheet.Cell(row, spouseIndex).Style.Fill.BackgroundColor == XLColor.Red)
            {
                isSuccess = false;
            }

            // Already has booking according to previous step → fail
            if (worksheet.Cell(row, alreadyBookedColumnIndex).Style.Fill.BackgroundColor == XLColor.RedPigment)
            {
                isSuccess = false;
            }

            var startDate = ParseExcelDateUtc(worksheet.Cell(row, startDateIndex).GetString());
            var endDate = ParseExcelDateUtc(worksheet.Cell(row, endDateIndex).GetString());
            var amount = decimal.Parse(worksheet.Cell(row, amountIndex).GetString().Replace(",", ""));
            var rounds = int.Parse(worksheet.Cell(row, roundsIndex).GetString());

            var currency = worksheet.Cell(row, currencyIndex).GetString().Trim();
            var modality = worksheet.Cell(row, modalityIndex).GetString().Trim();

            var bookingLog = new BookingLog
            {
                Id = IdProvider.NewId(),
                HouseholdId = hohId,
                SpouseId = spouseId,
                StartDate = startDate,
                EndDate = endDate,
                Amount = amount,
                Currency = currency,
                Rounds = rounds,
                OrganizationId = organizationId,
                UploadedById = userId,
                FileId = savedFileId,
                Modality = modality,
                IsSuccess = isSuccess,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            bookingLogs.Add(bookingLog);

            if (!isSuccess)
                continue;

            // Find existing using new unified ID-space logic
            Booking existing = null;

            if (!string.IsNullOrWhiteSpace(hohId))
                existing = FindExisting(hohId);

            if (existing == null && !string.IsNullOrWhiteSpace(spouseId))
                existing = FindExisting(spouseId);

            var booking = new Booking
            {
                Id = IdProvider.NewId(),
                HouseholdId = hohId,
                SpouseId = spouseId,
                StartDate = startDate,
                EndDate = endDate,
                Amount = amount,
                Currency = currency,
                Rounds = rounds,
                OrganizationId = organizationId,
                UploadedById = userId,
                FileId = savedFileId,
                Modality = modality,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            existingBookings.Add(booking);
        }

        _context.BookingLogs.AddRange(bookingLogs);

        await _context.SaveChangesAsync();
    }



    public async Task ReleaseBooking(Guid bookingId, Guid organizationId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.OrganizationId == organizationId)
            ?? throw new NotFoundException("Booking not found");

        booking.StartDate = null;
        booking.EndDate = null;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<BatchReleaseBookingResponse> BatchReleaseBookings(Guid organizationId, IFormFile file)
    {
        using var workbook = new XLWorkbook(file.OpenReadStream());
        var worksheet = workbook.Worksheet(1);
        var lastRowNumber = worksheet.LastRowUsed().RowNumber();

        var householdIdIndex = GetHeaderIndex("headofhouseholdid", worksheet);
        var dateStartedIndex = GetHeaderIndex("startdate", worksheet);

        var rows = new List<(string HouseholdId, DateTime StartDate)>();

        for (var i = 2; i <= lastRowNumber; i++)
        {
            var householdId = worksheet.Cell(i, householdIdIndex).GetString().Trim();
            var dateStartedStr = worksheet.Cell(i, dateStartedIndex).GetString().Trim();

            if (string.IsNullOrWhiteSpace(householdId) || string.IsNullOrWhiteSpace(dateStartedStr))
                continue;

            try
            {
                var startDate = ParseExcelDateUtc(dateStartedStr);
                rows.Add((householdId, startDate));
            }
            catch
            {
                // Skip rows with unparseable dates
            }
        }

        var total = rows.Count;

        var householdIds = rows.Select(r => r.HouseholdId).Distinct().ToHashSet();

        var bookings = await _context.Bookings
            .Where(b =>
                b.OrganizationId == organizationId
                && householdIds.Contains(b.HouseholdId)
                && b.StartDate != null
                && b.EndDate != null
            )
            .ToListAsync();

        var released = 0;

        foreach (var row in rows)
        {
            var booking = bookings.FirstOrDefault(b =>
                b.HouseholdId == row.HouseholdId
                && b.StartDate.HasValue
                && b.StartDate.Value.Date == row.StartDate.Date
            );

            if (booking == null)
                continue;

            booking.StartDate = null;
            booking.EndDate = null;
            booking.UpdatedAt = DateTime.UtcNow;
            released++;
        }

        await _context.SaveChangesAsync();

        return new BatchReleaseBookingResponse
        {
            Total = total,
            Released = released,
            Skipped = total - released,
        };
    }

    private int GetHeaderIndex(string headerName, IXLWorksheet worksheet)
    {
        if (HeaderIndexCache.ContainsKey(headerName))
        {
            return HeaderIndexCache[headerName];
        }

        var lastColumnIndex = worksheet.LastColumnUsed().ColumnNumber();
        for (var col = 1; col <= lastColumnIndex; col++)
        {
            var cellValue = worksheet.Cell(1, col).Value.ToString().ToLower().Replace(" ", "");
            if (string.Equals(cellValue, headerName, StringComparison.OrdinalIgnoreCase))
            {
                HeaderIndexCache[headerName] = col;
                return col;
            }
        }

        throw new BadRequestException($"Header '{headerName}' not found in the uploaded file.");
    }

    private static DateTime ParseExcelDateUtc(string value)
    {
        var dt = DateTime.ParseExact(value, "yyyyMMdd", null);
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }
}