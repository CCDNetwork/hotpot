using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Deduplication;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Dashboards;

public class DashboardService
{
    // Households × 6 ≈ individuals reached; multiplier confirmed by the
    // Oversight Committee (question 4.2).
    public const int IndividualsPerHousehold = 6;

    private readonly CcdContext _context;
    private readonly ExchangeRateService _exchangeRateService;

    public DashboardService(CcdContext context, ExchangeRateService exchangeRateService)
    {
        _context = context;
        _exchangeRateService = exchangeRateService;
    }

    // ---------------------------------------------------------------- filters

    public record DashboardPeriod(DateTime From, DateTime To);

    public static DashboardPeriod ResolvePeriod(string period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            "7d" => new DashboardPeriod(now.AddDays(-7), now),
            "90d" => new DashboardPeriod(now.AddDays(-90), now),
            "current-month" => new DashboardPeriod(
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                now
            ),
            // Default: rolling last 30 days (committee-confirmed)
            _ => new DashboardPeriod(now.AddDays(-30), now),
        };
    }

    public async Task<string> ResolveDisplayCurrencyAsync(string displayCurrency)
    {
        if (!string.IsNullOrWhiteSpace(displayCurrency))
            return displayCurrency.Trim().ToUpperInvariant();

        var settings = await _context.Settings.AsNoTracking().FirstOrDefaultAsync();
        return settings?.DashboardDisplayCurrency ?? "EUR";
    }

    private static string ResolveGranularity(string granularity)
    {
        return granularity switch
        {
            "daily" => "day",
            "monthly" => "month",
            // Default: weekly (delivery-lead default; user-selectable)
            _ => "week",
        };
    }

    // ------------------------------------------------------------- overview

    public async Task<OverviewSummaryResponse> GetOverviewSummary(
        string period,
        Guid? organizationId,
        string displayCurrency
    )
    {
        var (from, to) = ResolvePeriod(period);
        var display = await ResolveDisplayCurrencyAsync(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        var bookingsInPeriod = BookingsOverlappingPeriod(from, to, organizationId);

        var householdsAssisted = await bookingsInPeriod
            .Select(b => b.HouseholdId)
            .Distinct()
            .CountAsync();

        var activePartners = await _context.Bookings
            .Where(b => b.CreatedAt >= from && b.CreatedAt <= to)
            .Select(b => b.OrganizationId)
            .Distinct()
            .CountAsync();

        var totalOnboarded = await _context.Organizations.CountAsync();

        var groups = await bookingsInPeriod
            .GroupBy(b => new { b.Currency, b.CreatedAt.Year, b.CreatedAt.Month })
            .Select(g => new CurrencyMonthGroup
            {
                Currency = g.Key.Currency,
                Year = g.Key.Year,
                Month = g.Key.Month,
                Amount = g.Sum(b => b.Amount),
                Rounds = g.Sum(b => b.Rounds),
                Count = g.Count()
            })
            .ToListAsync();

        return new OverviewSummaryResponse
        {
            HouseholdsAssisted = householdsAssisted,
            IndividualsReached = householdsAssisted * IndividualsPerHousehold,
            ActivePartners = activePartners,
            TotalOnboarded = totalOnboarded,
            ValueTransferred = BuildValueSummary(groups, display, lookup),
            AvgTransfer = BuildAvgTransfer(groups, display, lookup)
        };
    }

    public async Task<OverviewTrendResponse> GetOverviewTrend(
        string period,
        Guid? organizationId,
        string granularity
    )
    {
        var (from, to) = ResolvePeriod(period);
        var bucket = ResolveGranularity(granularity);
        var connection = _context.Database.GetDbConnection();

        var households = (await connection.QueryAsync<TrendPointResponse>(
            @"SELECT date_trunc(@bucket, start_date) AS bucket,
                     count(DISTINCT household_id)::int AS count
                FROM booking
               WHERE start_date IS NOT NULL
                 AND start_date >= @from
                 AND start_date <= @to
                 AND (@organizationId::uuid IS NULL OR organization_id = @organizationId::uuid)
               GROUP BY 1
               ORDER BY 1",
            new { bucket, from, to, organizationId }
        )).ToList();

        var newPartners = (await connection.QueryAsync<TrendPointResponse>(
            @"SELECT date_trunc(@bucket, created_at) AS bucket,
                     count(*)::int AS count
                FROM organization
               WHERE created_at >= @from
                 AND created_at <= @to
               GROUP BY 1
               ORDER BY 1",
            new { bucket, from, to }
        )).ToList();

        return new OverviewTrendResponse
        {
            Households = households,
            NewPartners = newPartners
        };
    }

    public async Task<List<PartnerRowResponse>> GetOverviewPartners(
        string period,
        Guid? organizationId,
        string displayCurrency
    )
    {
        var (from, to) = ResolvePeriod(period);
        var display = await ResolveDisplayCurrencyAsync(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        var groups = await BookingsOverlappingPeriod(from, to, organizationId)
            .GroupBy(b => new
            {
                b.OrganizationId,
                b.Organization.Name,
                b.Currency,
                b.CreatedAt.Year,
                b.CreatedAt.Month
            })
            .Select(g => new
            {
                g.Key.OrganizationId,
                g.Key.Name,
                g.Key.Currency,
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(b => b.Amount),
                Bookings = g.Count()
            })
            .ToListAsync();

        // Distinct households per (org, currency) can't be derived from the
        // month groups (a household may span months) — count separately.
        var households = await BookingsOverlappingPeriod(from, to, organizationId)
            .GroupBy(b => new { b.OrganizationId, b.Currency })
            .Select(g => new
            {
                g.Key.OrganizationId,
                g.Key.Currency,
                Households = g.Select(b => b.HouseholdId).Distinct().Count()
            })
            .ToListAsync();

        var householdLookup = households.ToDictionary(
            h => (h.OrganizationId, h.Currency),
            h => h.Households
        );

        return groups
            .GroupBy(g => new { g.OrganizationId, g.Name, g.Currency })
            .Select(g =>
            {
                var converted = SumConverted(
                    g.Select(x => (x.Amount, x.Year, x.Month)),
                    g.Key.Currency,
                    display,
                    lookup,
                    out var status
                );

                return new PartnerRowResponse
                {
                    OrganizationId = g.Key.OrganizationId,
                    OrganizationName = g.Key.Name,
                    Households = householdLookup.GetValueOrDefault((g.Key.OrganizationId, g.Key.Currency)),
                    NativeCurrency = g.Key.Currency,
                    NativeAmount = g.Sum(x => x.Amount),
                    ConvertedAmount = converted,
                    ConversionStatus = status,
                    Bookings = g.Sum(x => x.Bookings)
                };
            })
            .OrderByDescending(r => r.Households)
            .ToList();
    }

    public async Task<List<ModalityRowResponse>> GetOverviewModality(
        string period,
        Guid? organizationId
    )
    {
        var (from, to) = ResolvePeriod(period);

        var rows = await BookingsOverlappingPeriod(from, to, organizationId)
            .GroupBy(b => b.Modality)
            .Select(g => new
            {
                Modality = g.Key,
                Households = g.Select(b => b.HouseholdId).Distinct().Count()
            })
            .OrderByDescending(g => g.Households)
            .ToListAsync();

        var total = rows.Sum(r => r.Households);

        return rows
            .Select(r => new ModalityRowResponse
            {
                Modality = r.Modality,
                Households = r.Households,
                Share = total == 0 ? 0 : (double)r.Households / total
            })
            .ToList();
    }

    // ---------------------------------------------------------- deduplication

    public async Task<DuplicatesSummaryResponse> GetDuplicatesSummary(
        string period,
        Guid? organizationId,
        string displayCurrency
    )
    {
        var (from, to) = ResolvePeriod(period);
        var display = await ResolveDisplayCurrencyAsync(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        var events = ConflictEventsInPeriod(from, to, organizationId);
        var uniqueOverlaps = await events.CountAsync();

        var prebookingLogs = _context.BookingLogs.Where(l =>
            l.IsPrebooking
            && l.CreatedAt >= from
            && l.CreatedAt <= to
            && (organizationId == null || l.OrganizationId == organizationId)
        );

        var prebookingRuns = await prebookingLogs
            .Where(l => l.SubmissionId != null)
            .Select(l => l.SubmissionId)
            .Distinct()
            .CountAsync();

        var householdRecordsChecked = await prebookingLogs.CountAsync();

        var groups = await events
            .GroupBy(e => new
            {
                Currency = e.ProposedCurrency,
                e.FirstDetectedAt.Year,
                e.FirstDetectedAt.Month
            })
            .Select(g => new CurrencyMonthGroup
            {
                Currency = g.Key.Currency,
                Year = g.Key.Year,
                Month = g.Key.Month,
                Amount = g.Sum(e => e.ProposedAmount),
                Rounds = 0,
                Count = g.Count()
            })
            .ToListAsync();

        return new DuplicatesSummaryResponse
        {
            UniqueOverlaps = uniqueOverlaps,
            PrebookingRuns = prebookingRuns,
            HouseholdRecordsChecked = householdRecordsChecked,
            OverlapRate = householdRecordsChecked == 0
                ? null
                : (double)uniqueOverlaps / householdRecordsChecked,
            ValueOfOverlaps = BuildValueSummary(groups, display, lookup)
        };
    }

    public async Task<List<DuplicatesTrendPointResponse>> GetDuplicatesTrend(
        string period,
        Guid? organizationId,
        string granularity
    )
    {
        var (from, to) = ResolvePeriod(period);
        var bucket = ResolveGranularity(granularity);
        var connection = _context.Database.GetDbConnection();

        var overlaps = await connection.QueryAsync<TrendPointResponse>(
            @"SELECT date_trunc(@bucket, first_detected_at) AS bucket,
                     count(*)::int AS count
                FROM booking_conflict_event
               WHERE first_detected_at >= @from
                 AND first_detected_at <= @to
                 AND (@organizationId::uuid IS NULL OR requesting_organization_id = @organizationId::uuid)
               GROUP BY 1
               ORDER BY 1",
            new { bucket, from, to, organizationId }
        );

        var recordsChecked = await connection.QueryAsync<TrendPointResponse>(
            @"SELECT date_trunc(@bucket, created_at) AS bucket,
                     count(*)::int AS count
                FROM booking_log
               WHERE is_prebooking = true
                 AND created_at >= @from
                 AND created_at <= @to
                 AND (@organizationId::uuid IS NULL OR organization_id = @organizationId::uuid)
               GROUP BY 1
               ORDER BY 1",
            new { bucket, from, to, organizationId }
        );

        var merged = new SortedDictionary<DateTime, DuplicatesTrendPointResponse>();

        foreach (var point in overlaps)
        {
            merged.TryAdd(point.Bucket, new DuplicatesTrendPointResponse { Bucket = point.Bucket });
            merged[point.Bucket].UniqueOverlaps = point.Count;
        }

        foreach (var point in recordsChecked)
        {
            merged.TryAdd(point.Bucket, new DuplicatesTrendPointResponse { Bucket = point.Bucket });
            merged[point.Bucket].HouseholdRecordsChecked = point.Count;
        }

        return merged.Values.ToList();
    }

    public async Task<DuplicatesSplitResponse> GetDuplicatesSplit(
        string period,
        Guid? organizationId
    )
    {
        var (from, to) = ResolvePeriod(period);
        var events = ConflictEventsInPeriod(from, to, organizationId);

        var withinAgency = await events
            .CountAsync(e => e.RequestingOrganizationId == e.BlockingOrganizationId);
        var acrossAgency = await events
            .CountAsync(e => e.RequestingOrganizationId != e.BlockingOrganizationId);

        return new DuplicatesSplitResponse
        {
            WithinAgency = withinAgency,
            AcrossAgency = acrossAgency
        };
    }

    public async Task<List<BlockingPartnerRowResponse>> GetBlockingPartners(
        string period,
        Guid? organizationId
    )
    {
        var (from, to) = ResolvePeriod(period);

        return await ConflictEventsInPeriod(from, to, organizationId)
            .GroupBy(e => new { e.BlockingOrganizationId, e.BlockingOrganization.Name })
            .Select(g => new BlockingPartnerRowResponse
            {
                OrganizationId = g.Key.BlockingOrganizationId,
                OrganizationName = g.Key.Name,
                OverlapsCaused = g.Count()
            })
            .OrderByDescending(r => r.OverlapsCaused)
            .ToListAsync();
    }

    public async Task<ConflictEventListResponse> GetConflictEvents(
        string period,
        Guid? organizationId,
        string displayCurrency,
        int page,
        int pageSize,
        string sort
    )
    {
        var (from, to) = ResolvePeriod(period);
        var display = await ResolveDisplayCurrencyAsync(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var events = ConflictEventsInPeriod(from, to, organizationId);
        var totalCount = await events.CountAsync();

        var ordered = sort switch
        {
            "firstDetectedAt" => events.OrderBy(e => e.FirstDetectedAt),
            "-firstDetectedAt" => events.OrderByDescending(e => e.FirstDetectedAt),
            "detectionCount" => events.OrderBy(e => e.DetectionCount),
            "-detectionCount" => events.OrderByDescending(e => e.DetectionCount),
            "proposedAmount" => events.OrderBy(e => e.ProposedAmount),
            "-proposedAmount" => events.OrderByDescending(e => e.ProposedAmount),
            "lastDetectedAt" => events.OrderBy(e => e.LastDetectedAt),
            _ => events.OrderByDescending(e => e.LastDetectedAt),
        };

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(e => e.RequestingOrganization)
            .Include(e => e.BlockingOrganization)
            .ToListAsync();

        return new ConflictEventListResponse
        {
            Data = rows.Select(e => MapConflictEvent(e, new ConflictEventRowResponse(), display, lookup)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ConflictEventDetailResponse> GetConflictEvent(
        Guid id,
        Guid? organizationId,
        string displayCurrency
    )
    {
        var display = await ResolveDisplayCurrencyAsync(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        var conflictEvent = await _context.BookingConflictEvents
            .Include(e => e.RequestingOrganization)
            .Include(e => e.BlockingOrganization)
            .Include(e => e.BlockingBooking)
            .FirstOrDefaultAsync(e =>
                e.Id == id
                && (organizationId == null || e.RequestingOrganizationId == organizationId)
            ) ?? throw new Helpers.NotFoundException("Conflict event not found");

        var response = MapConflictEvent(conflictEvent, new ConflictEventDetailResponse(), display, lookup);

        if (conflictEvent.BlockingBooking != null)
        {
            var booking = conflictEvent.BlockingBooking;
            var converted = lookup.Convert(
                booking.Amount,
                booking.Currency,
                display,
                booking.CreatedAt.Year,
                booking.CreatedAt.Month
            );

            response.BlockingBooking = new BlockingBookingResponse
            {
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                Amount = booking.Amount,
                Currency = booking.Currency,
                ConvertedAmount = converted,
                ConversionStatus = converted == null && !string.Equals(booking.Currency, display, StringComparison.OrdinalIgnoreCase)
                    ? ConversionStatus.MissingRate
                    : ConversionStatus.Ok,
                Modality = booking.Modality,
                Rounds = booking.Rounds,
                CreatedAt = booking.CreatedAt
            };
        }

        return response;
    }

    // -------------------------------------------------------------- internals

    private class CurrencyMonthGroup
    {
        public string Currency { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
        public int Rounds { get; set; }
        public int Count { get; set; }
    }

    private IQueryable<Booking> BookingsOverlappingPeriod(
        DateTime from,
        DateTime to,
        Guid? organizationId
    )
    {
        return _context.Bookings.Where(b =>
            b.StartDate != null
            && b.EndDate != null
            && b.StartDate <= to
            && b.EndDate >= from
            && (organizationId == null || b.OrganizationId == organizationId)
        );
    }

    private IQueryable<BookingConflictEvent> ConflictEventsInPeriod(
        DateTime from,
        DateTime to,
        Guid? organizationId
    )
    {
        return _context.BookingConflictEvents.Where(e =>
            e.FirstDetectedAt >= from
            && e.FirstDetectedAt <= to
            && (organizationId == null || e.RequestingOrganizationId == organizationId)
        );
    }

    private static decimal? SumConverted(
        IEnumerable<(decimal Amount, int Year, int Month)> monthGroups,
        string native,
        string display,
        ExchangeRateLookup lookup,
        out string status
    )
    {
        decimal sum = 0;
        var any = false;
        var missing = false;

        foreach (var (amount, year, month) in monthGroups)
        {
            var converted = lookup.Convert(amount, native, display, year, month);
            if (converted == null)
            {
                missing = true;
                continue;
            }

            sum += converted.Value;
            any = true;
        }

        status = missing
            ? (any ? ConversionStatus.Partial : ConversionStatus.MissingRate)
            : ConversionStatus.Ok;

        return any || !missing ? sum : null;
    }

    private ValueSummaryResponse BuildValueSummary(
        List<CurrencyMonthGroup> groups,
        string display,
        ExchangeRateLookup lookup
    )
    {
        var breakdown = groups
            .GroupBy(g => g.Currency)
            .Select(g =>
            {
                var converted = SumConverted(
                    g.Select(x => (x.Amount, x.Year, x.Month)),
                    g.Key,
                    display,
                    lookup,
                    out var status
                );

                return new CurrencyBreakdownResponse
                {
                    Currency = g.Key,
                    Native = g.Sum(x => x.Amount),
                    Converted = converted,
                    ConversionStatus = status,
                    Count = g.Sum(x => x.Count)
                };
            })
            .OrderByDescending(b => b.Native)
            .ToList();

        var convertible = breakdown.Where(b => b.Converted != null).ToList();
        var displayStatus = breakdown.All(b => b.ConversionStatus == ConversionStatus.Ok)
            ? ConversionStatus.Ok
            : convertible.Count > 0
                ? ConversionStatus.Partial
                : ConversionStatus.MissingRate;

        return new ValueSummaryResponse
        {
            Native = breakdown
                .Select(b => new CurrencyAmountResponse { Currency = b.Currency, Amount = b.Native })
                .ToList(),
            Display = new DisplayAmountResponse
            {
                Currency = display,
                Amount = breakdown.Count == 0
                    ? 0
                    : convertible.Count > 0
                        ? convertible.Sum(b => b.Converted.Value)
                        : null,
                ConversionStatus = breakdown.Count == 0 ? ConversionStatus.Ok : displayStatus
            },
            Breakdown = breakdown
        };
    }

    private AvgTransferResponse BuildAvgTransfer(
        List<CurrencyMonthGroup> groups,
        string display,
        ExchangeRateLookup lookup
    )
    {
        var breakdown = groups
            .GroupBy(g => g.Currency)
            .Select(g =>
            {
                var bookings = g.Sum(x => x.Count);
                var converted = SumConverted(
                    g.Select(x => (x.Amount, x.Year, x.Month)),
                    g.Key,
                    display,
                    lookup,
                    out var status
                );

                return new AvgTransferBreakdownResponse
                {
                    Currency = g.Key,
                    AvgNative = bookings == 0 ? 0 : decimal.Round(g.Sum(x => x.Amount) / bookings, 2),
                    AvgConverted = converted == null || bookings == 0
                        ? null
                        : decimal.Round(converted.Value / bookings, 2),
                    ConversionStatus = status,
                    AvgRounds = bookings == 0 ? 0 : decimal.Round((decimal)g.Sum(x => x.Rounds) / bookings, 1),
                    Bookings = bookings
                };
            })
            .OrderByDescending(b => b.Bookings)
            .ToList();

        var convertible = breakdown.Where(b => b.AvgConverted != null).ToList();
        var totalConvertibleBookings = convertible.Sum(b => b.Bookings);

        return new AvgTransferResponse
        {
            Display = new DisplayAmountResponse
            {
                Currency = display,
                Amount = totalConvertibleBookings == 0
                    ? (breakdown.Count == 0 ? 0 : null)
                    : decimal.Round(
                        convertible.Sum(b => b.AvgConverted.Value * b.Bookings) / totalConvertibleBookings,
                        2
                    ),
                ConversionStatus = breakdown.Count == 0 || breakdown.All(b => b.ConversionStatus == ConversionStatus.Ok)
                    ? ConversionStatus.Ok
                    : convertible.Count > 0
                        ? ConversionStatus.Partial
                        : ConversionStatus.MissingRate
            },
            Breakdown = breakdown
        };
    }

    private static T MapConflictEvent<T>(
        BookingConflictEvent conflictEvent,
        T response,
        string display,
        ExchangeRateLookup lookup
    ) where T : ConflictEventRowResponse
    {
        var converted = lookup.Convert(
            conflictEvent.ProposedAmount,
            conflictEvent.ProposedCurrency,
            display,
            conflictEvent.FirstDetectedAt.Year,
            conflictEvent.FirstDetectedAt.Month
        );

        response.Id = conflictEvent.Id;
        response.SubjectLabel = BuildSubjectLabel(conflictEvent.SubjectKey);
        response.RequestingOrganizationId = conflictEvent.RequestingOrganizationId;
        response.RequestingOrganizationName = conflictEvent.RequestingOrganization?.Name;
        response.BlockingOrganizationId = conflictEvent.BlockingOrganizationId;
        response.BlockingOrganizationName = conflictEvent.BlockingOrganization?.Name;
        response.OverlapStartDate = conflictEvent.OverlapStartDate;
        response.OverlapEndDate = conflictEvent.OverlapEndDate;
        response.ProposedAmount = conflictEvent.ProposedAmount;
        response.ProposedCurrency = conflictEvent.ProposedCurrency;
        response.ConvertedAmount = converted;
        response.ConversionStatus = converted == null
            && !string.Equals(conflictEvent.ProposedCurrency, display, StringComparison.OrdinalIgnoreCase)
                ? ConversionStatus.MissingRate
                : ConversionStatus.Ok;
        response.ProposedModality = conflictEvent.ProposedModality;
        response.ProposedRounds = conflictEvent.ProposedRounds;
        response.FirstDetectedAt = conflictEvent.FirstDetectedAt;
        response.LastDetectedAt = conflictEvent.LastDetectedAt;
        response.DetectionCount = conflictEvent.DetectionCount;
        response.Category = conflictEvent.RequestingOrganizationId == conflictEvent.BlockingOrganizationId
            ? "within_same_agency"
            : "across_agencies";

        return response;
    }

    private static string BuildSubjectLabel(string subjectKey)
    {
        if (string.IsNullOrEmpty(subjectKey))
            return "HH …?";

        var suffix = subjectKey.Length <= 6 ? subjectKey : subjectKey[^6..];
        return $"HH …{suffix.ToLowerInvariant()}";
    }
}
