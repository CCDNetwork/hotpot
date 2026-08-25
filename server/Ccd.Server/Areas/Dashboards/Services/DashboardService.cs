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
        return ResolvePeriod(period, null, null);
    }

    /// <summary>
    /// Resolves a period name to a (From, To) window, with an override for
    /// "custom" that takes explicit dates. Explicit dates are anchored to UTC
    /// calendar days: from -&gt; 00:00:00 UTC of the picked day, to -&gt;
    /// 23:59:59.999 UTC of the picked day (inclusive). This mirrors how the
    /// InforEuro-active-rate FX rule expects UTC-anchored ranges.
    /// </summary>
    public static DashboardPeriod ResolvePeriod(string period, DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow;

        if (period == "custom" && from.HasValue && to.HasValue)
        {
            var f = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            var t = DateTime.SpecifyKind(to.Value.Date, DateTimeKind.Utc)
                .AddDays(1).AddTicks(-1);
            // Guard against inverted input: swap rather than error so a picker
            // that stores from > to during selection can't blank every widget.
            if (t < f)
            {
                (f, t) = (t, f);
            }
            return new DashboardPeriod(f, t);
        }

        return period switch
        {
            "7d" => new DashboardPeriod(now.AddDays(-7), now),
            "90d" => new DashboardPeriod(now.AddDays(-90), now),
            "current-month" => new DashboardPeriod(
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                now
            ),
            // UnixEpoch rather than DateTime.MinValue: Npgsql requires Kind=Utc
            // and maps MinValue to -infinity, which breaks date_trunc bucketing.
            "all-time" => new DashboardPeriod(DateTime.UnixEpoch, now),
            // Default: rolling last 30 days (committee-confirmed)
            _ => new DashboardPeriod(now.AddDays(-30), now),
        };
    }

    // USD is the deployment-wide default. Not configurable per tenant:
    // SSO-only deployments (UNICEF) have no superadmin who could edit a
    // Settings row. Users flip the view via the dashboard's currency toggle,
    // which sends the choice on the query string.
    public const string DefaultDisplayCurrency = "USD";

    public static string ResolveDisplayCurrency(string displayCurrency)
    {
        return string.IsNullOrWhiteSpace(displayCurrency)
            ? DefaultDisplayCurrency
            : displayCurrency.Trim().ToUpperInvariant();
    }

    // Whitelist: `bucket` is interpolated into SQL (Dapper cannot parameterise
    // date_trunc's field name), so only these values may reach the query.
    private static string ResolveGranularity(string granularity)
    {
        return granularity switch
        {
            "daily" => "day",
            "monthly" => "month",
            "quarterly" => "quarter",
            "annual" => "year",
            // Default: weekly (delivery-lead default; user-selectable)
            _ => "week",
        };
    }

    // ------------------------------------------------------------- overview

    public async Task<OverviewSummaryResponse> GetOverviewSummary(
        string period,
        Guid? organizationId,
        string displayCurrency,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);
        var display = ResolveDisplayCurrency(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        var bookingsInPeriod = BookingsOverlappingPeriod(from, to, organizationId);

        var householdsAssisted = await bookingsInPeriod
            .Select(b => b.HouseholdId)
            .Distinct()
            .CountAsync();

        var activeOrganizations = await _context.Bookings
            .Where(b => b.CreatedAt >= from && b.CreatedAt <= to)
            .Select(b => b.OrganizationId)
            .Distinct()
            .CountAsync();

        var totalOnboarded = await _context.Organizations.CountAsync();

        // FX groups the assistance amount by its *own* month (start_date, or
        // upload date as a fallback) — not by when the row was inserted. This
        // uses the InforEuro rate that was current when the assistance was
        // delivered, and it dodges the "current month not fetched yet" gap
        // that would otherwise trip rows created today.
        var groups = await bookingsInPeriod
            .GroupBy(b => new
            {
                b.Currency,
                Year = (b.StartDate ?? b.CreatedAt).Year,
                Month = (b.StartDate ?? b.CreatedAt).Month
            })
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
            ActiveOrganizations = activeOrganizations,
            TotalOnboarded = totalOnboarded,
            ValueTransferred = BuildValueSummary(groups, display, lookup),
            AvgTransfer = BuildAvgTransfer(groups, display, lookup)
        };
    }

    public async Task<OverviewTrendResponse> GetOverviewTrend(
        string period,
        Guid? organizationId,
        string granularity,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);
        var bucket = ResolveGranularity(granularity);
        var connection = _context.Database.GetDbConnection();

        // COALESCE(start_date, created_at) matches the summary tile's
        // `StartDate ?? CreatedAt` grouping — otherwise a booking with a null
        // start_date counts in the summary total but is dropped from the trend
        // and the two numbers stop agreeing.
        var households = (await connection.QueryAsync<TrendPointResponse>(
            @"SELECT date_trunc(@bucket, COALESCE(start_date, created_at)) AS bucket,
                     count(DISTINCT household_id)::int AS count
                FROM booking
               WHERE COALESCE(start_date, created_at) >= @from
                 AND COALESCE(start_date, created_at) <= @to
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
        string displayCurrency,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);
        var display = ResolveDisplayCurrency(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        var groups = await BookingsOverlappingPeriod(from, to, organizationId)
            .GroupBy(b => new
            {
                b.OrganizationId,
                b.Organization.Name,
                b.Currency,
                Year = (b.StartDate ?? b.CreatedAt).Year,
                Month = (b.StartDate ?? b.CreatedAt).Month
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
        Guid? organizationId,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);

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
        string displayCurrency,
        string overlapScope,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);
        var display = ResolveDisplayCurrency(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        var events = ConflictEventsInPeriod(from, to, organizationId, overlapScope);
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

        // FX groups by overlap_start_date (the day the disputed money would
        // have flowed) rather than first_detected_at. Same rationale as the
        // Overview grouping: rate lookup keyed on when the assistance is due,
        // not when the row was written.
        var groups = await events
            .GroupBy(e => new
            {
                Currency = e.ProposedCurrency,
                Year = e.OverlapStartDate.Year,
                Month = e.OverlapStartDate.Month
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
        string granularity,
        string overlapScope,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);
        var bucket = ResolveGranularity(granularity);
        var crossOnly = IsCrossOrgOnly(overlapScope);
        var connection = _context.Database.GetDbConnection();

        var overlaps = await connection.QueryAsync<TrendPointResponse>(
            @"SELECT date_trunc(@bucket, first_detected_at) AS bucket,
                     count(*)::int AS count
                FROM booking_conflict_event
               WHERE first_detected_at >= @from
                 AND first_detected_at <= @to
                 AND (@organizationId::uuid IS NULL
                      OR requesting_organization_id = @organizationId::uuid
                      OR blocking_organization_id = @organizationId::uuid)
                 AND (NOT @crossOnly OR requesting_organization_id <> blocking_organization_id)
               GROUP BY 1
               ORDER BY 1",
            new { bucket, from, to, organizationId, crossOnly }
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

    // Composition intentionally uses ALL events (no scope filter) — the split
    // is *about* the within-vs-across composition, so hiding one side would
    // defeat the chart's purpose. This is the only dedup endpoint that
    // ignores the overlapScope filter by design.
    public async Task<DuplicatesSplitResponse> GetDuplicatesSplit(
        string period,
        Guid? organizationId,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);
        var events = ConflictEventsInPeriod(from, to, organizationId, "all");

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
        Guid? organizationId,
        string overlapScope,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);

        return await ConflictEventsInPeriod(from, to, organizationId, overlapScope)
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
        string overlapScope,
        int page,
        int pageSize,
        string sort,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);
        var display = ResolveDisplayCurrency(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var events = ConflictEventsInPeriod(from, to, organizationId, overlapScope);
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
        var display = ResolveDisplayCurrency(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        // Both sides of a conflict can open its drill: the requesting org (who
        // hit the duplicate) and the blocking org (whose prior booking caused
        // it). The list endpoint already surfaces rows from both sides, so the
        // detail must accept both — otherwise a blocker-side row 404s on click.
        var conflictEvent = await _context.BookingConflictEvents
            .Include(e => e.RequestingOrganization)
            .Include(e => e.BlockingOrganization)
            .Include(e => e.BlockingBooking)
            .FirstOrDefaultAsync(e =>
                e.Id == id
                && (organizationId == null
                    || e.RequestingOrganizationId == organizationId
                    || e.BlockingOrganizationId == organizationId)
            ) ?? throw new Helpers.NotFoundException("Conflict event not found");

        var response = MapConflictEvent(conflictEvent, new ConflictEventDetailResponse(), display, lookup);

        if (conflictEvent.BlockingBooking != null)
        {
            var booking = conflictEvent.BlockingBooking;
            var fxDate = booking.StartDate ?? booking.CreatedAt;
            var converted = lookup.Convert(
                booking.Amount,
                booking.Currency,
                display,
                fxDate.Year,
                fxDate.Month
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

    // ----------------------------------------------------------------- drills

    /// <summary>
    /// Per-(currency, year, month) rows powering the value-FX drill on both
    /// tabs. Grouping matches the tile aggregation exactly — sums add up.
    /// `source=bookings` reads Booking (start_date ?? created_at);
    /// `source=conflicts` reads BookingConflictEvent (overlap_start_date).
    /// </summary>
    public async Task<ValueFxDrillResponse> GetValueFxDrill(
        string source,
        string period,
        Guid? organizationId,
        string displayCurrency,
        string overlapScope,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);
        var display = ResolveDisplayCurrency(displayCurrency);
        var lookup = await _exchangeRateService.GetLookupAsync();

        List<CurrencyMonthGroup> groups;
        if (string.Equals(source, "conflicts", StringComparison.OrdinalIgnoreCase))
        {
            // Value-of-overlaps drill inherits the dedup tab's scope so the
            // drill totals match the tile that opened it.
            groups = await ConflictEventsInPeriod(from, to, organizationId, overlapScope)
                .GroupBy(e => new
                {
                    Currency = e.ProposedCurrency,
                    Year = e.OverlapStartDate.Year,
                    Month = e.OverlapStartDate.Month
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
        }
        else
        {
            groups = await BookingsOverlappingPeriod(from, to, organizationId)
                .GroupBy(b => new
                {
                    b.Currency,
                    Year = (b.StartDate ?? b.CreatedAt).Year,
                    Month = (b.StartDate ?? b.CreatedAt).Month
                })
                .Select(g => new CurrencyMonthGroup
                {
                    Currency = g.Key.Currency,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(b => b.Amount),
                    Rounds = 0,
                    Count = g.Count()
                })
                .ToListAsync();
        }

        var rows = groups
            .OrderBy(g => g.Currency)
            .ThenBy(g => g.Year)
            .ThenBy(g => g.Month)
            .Select(g =>
            {
                var rate = lookup.RateNativeToDisplay(g.Currency, display, g.Year, g.Month);
                var converted = rate.HasValue ? g.Amount * rate.Value : (decimal?)null;
                var status = rate.HasValue
                    ? ConversionStatus.Ok
                    : (string.Equals(g.Currency, display, StringComparison.OrdinalIgnoreCase)
                        ? ConversionStatus.Ok
                        : ConversionStatus.MissingRate);
                return new ValueFxRowResponse
                {
                    Currency = g.Currency,
                    Year = g.Year,
                    Month = g.Month,
                    Native = g.Amount,
                    Rate = rate,
                    Converted = converted,
                    Count = g.Count,
                    ConversionStatus = status
                };
            })
            .ToList();

        var nativeTotals = groups
            .GroupBy(g => g.Currency)
            .Select(g => new CurrencyAmountResponse
            {
                Currency = g.Key,
                Amount = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Currency)
            .ToList();

        decimal? convertedTotal = null;
        string totalsStatus = ConversionStatus.Ok;
        if (rows.Count > 0)
        {
            var summed = 0m;
            var anyMissing = false;
            var anyConverted = false;
            foreach (var r in rows)
            {
                if (r.Converted.HasValue)
                {
                    summed += r.Converted.Value;
                    anyConverted = true;
                }
                else
                {
                    anyMissing = true;
                }
            }
            convertedTotal = anyConverted ? summed : (decimal?)null;
            totalsStatus = anyMissing
                ? (anyConverted ? ConversionStatus.Partial : ConversionStatus.MissingRate)
                : ConversionStatus.Ok;
        }

        return new ValueFxDrillResponse
        {
            DisplayCurrency = display,
            Rows = rows,
            Totals = new ValueFxTotalsResponse
            {
                Native = nativeTotals,
                Converted = convertedTotal,
                ConversionStatus = totalsStatus
            }
        };
    }

    /// <summary>
    /// Per-org totals for orgs feeding data during the period. Mirrors the
    /// existing `GetOverviewPartners` grouping (org × currency), with the
    /// household count folded in.
    /// </summary>
    public async Task<List<OrganizationDrillRowResponse>> GetOrganizationsDrill(
        string period,
        Guid? organizationId,
        string displayCurrency,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var partners = await GetOverviewPartners(period, organizationId, displayCurrency, fromParam, toParam);
        return partners.Select(p => new OrganizationDrillRowResponse
        {
            OrganizationId = p.OrganizationId,
            OrganizationName = p.OrganizationName,
            BookingsCount = p.Bookings,
            HouseholdsCount = p.Households,
            NativeCurrency = p.NativeCurrency,
            NativeAmount = p.NativeAmount,
            ConvertedAmount = p.ConvertedAmount,
            ConversionStatus = p.ConversionStatus
        }).ToList();
    }

    /// <summary>
    /// Pre-booking submissions in the period — one row per submission_id,
    /// with success/fail counts. Successful and failed rows are pulled from
    /// booking_log where is_prebooking = true.
    /// </summary>
    public async Task<PrebookingRunDrillResponse> GetPrebookingRunsDrill(
        string period,
        Guid? organizationId,
        int page,
        int pageSize,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);

        var logs = _context.BookingLogs
            .Where(l => l.IsPrebooking
                && l.SubmissionId != null
                && l.CreatedAt >= from
                && l.CreatedAt <= to
                && (organizationId == null || l.OrganizationId == organizationId));

        var grouped = logs
            .GroupBy(l => l.SubmissionId!.Value)
            .Select(g => new
            {
                SubmissionId = g.Key,
                UploadedAt = g.Min(l => l.CreatedAt),
                UploadedByFirstName = g.Min(l => l.UploadedBy.FirstName),
                UploadedByLastName = g.Min(l => l.UploadedBy.LastName),
                OrganizationName = g.Min(l => l.Organization.Name),
                TotalRows = g.Count(),
                SuccessRows = g.Count(l => l.IsSuccess),
                FailedRows = g.Count(l => !l.IsSuccess)
            });

        var totalCount = await grouped.CountAsync();
        var rows = await grouped
            .OrderByDescending(x => x.UploadedAt)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PrebookingRunDrillResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Data = rows.Select(r => new PrebookingRunRowResponse
            {
                SubmissionId = r.SubmissionId,
                UploadedAt = r.UploadedAt,
                UploadedByName = string.Join(" ",
                    new[] { r.UploadedByFirstName, r.UploadedByLastName }
                        .Where(s => !string.IsNullOrWhiteSpace(s))),
                OrganizationName = r.OrganizationName,
                TotalRows = r.TotalRows,
                SuccessRows = r.SuccessRows,
                FailedRows = r.FailedRows
            }).ToList()
        };
    }

    /// <summary>
    /// Individual pre-booking booking_log rows in the period. This is what
    /// backs the "Household records checked" drill — one row per household
    /// the wizard evaluated.
    /// </summary>
    public async Task<RecordsCheckedDrillResponse> GetRecordsCheckedDrill(
        string period,
        Guid? organizationId,
        int page,
        int pageSize,
        DateTime? fromParam = null,
        DateTime? toParam = null
    )
    {
        var (from, to) = ResolvePeriod(period, fromParam, toParam);

        var logs = _context.BookingLogs
            .Where(l => l.IsPrebooking
                && l.CreatedAt >= from
                && l.CreatedAt <= to
                && (organizationId == null || l.OrganizationId == organizationId));

        var totalCount = await logs.CountAsync();
        var rows = await logs
            .OrderByDescending(l => l.CreatedAt)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new RecordsCheckedRowResponse
            {
                Id = l.Id,
                SubmissionId = l.SubmissionId,
                CreatedAt = l.CreatedAt,
                OrganizationName = l.Organization.Name,
                IsSuccess = l.IsSuccess,
                Currency = l.Currency,
                Amount = l.Amount,
                StartDate = l.StartDate,
                EndDate = l.EndDate
            })
            .ToListAsync();

        return new RecordsCheckedDrillResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Data = rows
        };
    }

    // -------------------------------------------- consecutive-months metric

    /// <summary>
    /// Histogram of consecutive booked-assistance-months per household over
    /// the trailing 12 months. Each booking's rounds are exploded into
    /// monthly assistance dates; rows for the same household (identified by
    /// deterministic ciphertext, which lets us group across orgs without
    /// decrypting) are combined; consecutive months form a run; each run is
    /// counted once and bucketed 1..5 exact / 6+ open-ended. Released
    /// bookings (start_date/end_date = null) are excluded — those months
    /// were cancelled and shouldn't count as booked assistance.
    /// This metric ignores the org filter by design: cross-org linkage is
    /// central to the "consecutive months" question — restricting to one
    /// org would produce artificially short runs.
    /// </summary>
    public async Task<ConsecutiveMonthsHistogramResponse> GetConsecutiveMonthsHistogram()
    {
        var connection = _context.Database.GetDbConnection();

        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        var windowEnd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var rows = await connection.QueryAsync<BucketRow>(
            @"WITH booking_months AS (
                SELECT
                  b.household_id AS subject,
                  date_trunc('month', b.start_date + (m * interval '1 month'))::date AS assistance_month
                FROM booking b
                CROSS JOIN generate_series(0, b.rounds - 1) AS m
                WHERE b.start_date IS NOT NULL
                  AND b.end_date IS NOT NULL
              ),
              in_window AS (
                SELECT DISTINCT subject, assistance_month
                FROM booking_months
                WHERE assistance_month >= @windowStart::date
                  AND assistance_month <= @windowEnd::date
              ),
              numbered AS (
                SELECT
                  subject,
                  assistance_month,
                  (assistance_month
                     - (row_number() OVER (PARTITION BY subject ORDER BY assistance_month) * interval '1 month')
                  )::date AS island_group
                FROM in_window
              ),
              run_lengths AS (
                SELECT subject, island_group, count(*)::int AS run_length
                FROM numbered
                GROUP BY subject, island_group
              )
              SELECT
                CASE WHEN run_length >= 6 THEN 6 ELSE run_length END AS bucket,
                count(*)::int AS episodes
              FROM run_lengths
              GROUP BY bucket
              ORDER BY bucket",
            new { windowStart, windowEnd }
        );

        // Pad missing buckets so the frontend always sees 1..5 + 6+ in order.
        var byBucket = rows.ToDictionary(r => r.Bucket, r => r.Episodes);
        var buckets = new List<ConsecutiveMonthsBucketResponse>();
        for (var i = 1; i <= 6; i++)
        {
            buckets.Add(new ConsecutiveMonthsBucketResponse
            {
                Bucket = i == 6 ? "6+" : i.ToString(),
                Episodes = byBucket.GetValueOrDefault(i, 0)
            });
        }

        return new ConsecutiveMonthsHistogramResponse
        {
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            Buckets = buckets
        };
    }

    /// <summary>
    /// Detail rows for one bucket of the consecutive-months histogram. Bucket
    /// "6+" returns runs of length >= 6; "1".."5" return exact matches. Each
    /// row lists the organizations that booked the household during the run.
    /// </summary>
    public async Task<ConsecutiveMonthsDrillResponse> GetConsecutiveMonthsDetails(
        string bucket,
        int page,
        int pageSize
    )
    {
        var connection = _context.Database.GetDbConnection();

        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        var windowEnd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var openEnded = string.Equals(bucket, "6+", StringComparison.Ordinal);
        var exactLength = openEnded ? 6 : int.TryParse(bucket, out var b) ? b : 0;
        if (!openEnded && (exactLength < 1 || exactLength > 5))
            throw new Helpers.BadRequestException("bucket must be 1, 2, 3, 4, 5 or 6+");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Count runs matching the bucket
        var totalCount = await connection.QuerySingleAsync<int>(
            @"WITH booking_months AS (
                SELECT b.household_id AS subject,
                       date_trunc('month', b.start_date + (m * interval '1 month'))::date AS assistance_month
                FROM booking b
                CROSS JOIN generate_series(0, b.rounds - 1) AS m
                WHERE b.start_date IS NOT NULL AND b.end_date IS NOT NULL
              ),
              in_window AS (
                SELECT DISTINCT subject, assistance_month FROM booking_months
                WHERE assistance_month >= @windowStart::date AND assistance_month <= @windowEnd::date
              ),
              numbered AS (
                SELECT subject, assistance_month,
                       (assistance_month - (row_number() OVER (PARTITION BY subject ORDER BY assistance_month) * interval '1 month'))::date AS island_group
                FROM in_window
              ),
              run_lengths AS (
                SELECT subject, island_group, count(*)::int AS run_length
                FROM numbered GROUP BY subject, island_group
              )
              SELECT count(*)::int
              FROM run_lengths
              WHERE (@openEnded AND run_length >= 6)
                 OR (NOT @openEnded AND run_length = @exactLength)",
            new { windowStart, windowEnd, openEnded, exactLength }
        );

        var rows = await connection.QueryAsync<RunRow>(
            @"WITH booking_months AS (
                SELECT b.household_id AS subject,
                       date_trunc('month', b.start_date + (m * interval '1 month'))::date AS assistance_month
                FROM booking b
                CROSS JOIN generate_series(0, b.rounds - 1) AS m
                WHERE b.start_date IS NOT NULL AND b.end_date IS NOT NULL
              ),
              in_window AS (
                SELECT DISTINCT subject, assistance_month FROM booking_months
                WHERE assistance_month >= @windowStart::date AND assistance_month <= @windowEnd::date
              ),
              numbered AS (
                SELECT subject, assistance_month,
                       (assistance_month - (row_number() OVER (PARTITION BY subject ORDER BY assistance_month) * interval '1 month'))::date AS island_group
                FROM in_window
              ),
              runs AS (
                SELECT subject, island_group,
                       count(*)::int AS run_length,
                       min(assistance_month) AS run_start,
                       max(assistance_month) AS run_end
                FROM numbered GROUP BY subject, island_group
              ),
              matched AS (
                SELECT * FROM runs
                WHERE (@openEnded AND run_length >= 6)
                   OR (NOT @openEnded AND run_length = @exactLength)
              ),
              orgs_per_run AS (
                SELECT m.subject, m.island_group,
                       array_agg(DISTINCT o.name ORDER BY o.name) AS org_names
                FROM matched m
                JOIN booking b ON b.household_id = m.subject
                              AND b.start_date IS NOT NULL
                              AND b.end_date IS NOT NULL
                              AND date_trunc('month', b.start_date)::date <= m.run_end
                              AND (date_trunc('month', b.start_date) + ((b.rounds - 1) * interval '1 month'))::date >= m.run_start
                JOIN organization o ON o.id = b.organization_id
                GROUP BY m.subject, m.island_group
              )
              SELECT m.subject AS Subject,
                     m.run_length AS RunLength,
                     m.run_start AS RunStart,
                     m.run_end AS RunEnd,
                     coalesce(op.org_names, ARRAY[]::text[]) AS OrgNames
              FROM matched m
              LEFT JOIN orgs_per_run op USING(subject, island_group)
              ORDER BY m.run_length DESC, m.run_end DESC
              LIMIT @limit OFFSET @offset",
            new
            {
                windowStart,
                windowEnd,
                openEnded,
                exactLength,
                limit = pageSize,
                offset = (page - 1) * pageSize
            }
        );

        return new ConsecutiveMonthsDrillResponse
        {
            Bucket = bucket,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Data = rows.Select(r => new ConsecutiveMonthsRunResponse
            {
                HouseholdIdMasked = MaskId(r.Subject),
                RunStartMonth = r.RunStart,
                RunEndMonth = r.RunEnd,
                MonthsCount = r.RunLength,
                OrganizationNames = r.OrgNames?.ToList() ?? new List<string>()
            }).ToList()
        };
    }

    private class BucketRow { public int Bucket { get; set; } public int Episodes { get; set; } }
    private class RunRow
    {
        public string Subject { get; set; }
        public int RunLength { get; set; }
        public DateTime RunStart { get; set; }
        public DateTime RunEnd { get; set; }
        public string[] OrgNames { get; set; }
    }

    // Reintroduced (previously removed with the households drill): last 6
    // chars of the deterministic ciphertext — enough to distinguish rows in
    // a UI list without leaking the plaintext ID.
    private static string MaskId(string cipher)
    {
        if (string.IsNullOrWhiteSpace(cipher))
            return "—";
        return cipher.Length <= 6 ? cipher : "…" + cipher.Substring(cipher.Length - 6);
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
        // End Date is exclusive (first day NOT covered), so a booking whose
        // end date equals the window's `from` doesn't actually overlap the
        // window — strict `>` treats adjacent bookings as non-overlapping.
        return _context.Bookings.Where(b =>
            b.StartDate != null
            && b.EndDate != null
            && b.StartDate <= to
            && b.EndDate > from
            && (organizationId == null || b.OrganizationId == organizationId)
        );
    }

    // Anything other than "all" means cross-org-only (the default). Kept
    // permissive so an omitted / misspelled scope silently gives the safer
    // (cleaner) reading rather than accidentally exposing intra-org noise.
    private static bool IsCrossOrgOnly(string overlapScope) =>
        !string.Equals(overlapScope, "all", StringComparison.OrdinalIgnoreCase);

    private IQueryable<BookingConflictEvent> ConflictEventsInPeriod(
        DateTime from,
        DateTime to,
        Guid? organizationId,
        string overlapScope = "cross"
    )
    {
        // Org scope covers *involvement* on either side: an event where your
        // org is the blocker is as relevant to you as one where it's the
        // requester. The detail endpoint uses the same predicate so any row
        // visible in the list is openable.
        var query = _context.BookingConflictEvents.Where(e =>
            e.FirstDetectedAt >= from
            && e.FirstDetectedAt <= to
            && (organizationId == null
                || e.RequestingOrganizationId == organizationId
                || e.BlockingOrganizationId == organizationId)
        );

        if (IsCrossOrgOnly(overlapScope))
        {
            // Intra-org overlaps (same org on both sides) are treated as
            // data-entry noise, not a coordination signal — hidden by default.
            query = query.Where(e => e.RequestingOrganizationId != e.BlockingOrganizationId);
        }

        return query;
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
            conflictEvent.OverlapStartDate.Year,
            conflictEvent.OverlapStartDate.Month
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
