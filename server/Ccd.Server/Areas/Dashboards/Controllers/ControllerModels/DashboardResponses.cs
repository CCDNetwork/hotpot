using System;
using System.Collections.Generic;

namespace Ccd.Server.Dashboards;

// Conversion status values used across currency-bearing responses:
// "ok" — every underlying month-group had a rate; "partial" — some groups were
// missing a rate (display total covers only the converted part); "missing_rate"
// — nothing could be converted, show native only.
public static class ConversionStatus
{
    public const string Ok = "ok";
    public const string Partial = "partial";
    public const string MissingRate = "missing_rate";
}

public class CurrencyAmountResponse
{
    public string Currency { get; set; }
    public decimal Amount { get; set; }
}

public class DisplayAmountResponse
{
    public string Currency { get; set; }
    public decimal? Amount { get; set; }
    public string ConversionStatus { get; set; }
}

public class CurrencyBreakdownResponse
{
    public string Currency { get; set; }
    public decimal Native { get; set; }
    public decimal? Converted { get; set; }
    public string ConversionStatus { get; set; }
    public int Count { get; set; }
}

public class ValueSummaryResponse
{
    public List<CurrencyAmountResponse> Native { get; set; }
    public DisplayAmountResponse Display { get; set; }
    public List<CurrencyBreakdownResponse> Breakdown { get; set; }
}

public class AvgTransferBreakdownResponse
{
    public string Currency { get; set; }
    public decimal AvgNative { get; set; }
    public decimal? AvgConverted { get; set; }
    public string ConversionStatus { get; set; }
    public decimal AvgRounds { get; set; }
    public int Bookings { get; set; }
}

public class AvgTransferResponse
{
    public DisplayAmountResponse Display { get; set; }
    public List<AvgTransferBreakdownResponse> Breakdown { get; set; }
}

public class OverviewSummaryResponse
{
    public int HouseholdsAssisted { get; set; }
    public int IndividualsReached { get; set; }
    public int ActivePartners { get; set; }
    public int TotalOnboarded { get; set; }
    public ValueSummaryResponse ValueTransferred { get; set; }
    public AvgTransferResponse AvgTransfer { get; set; }
}

public class TrendPointResponse
{
    public DateTime Bucket { get; set; }
    public int Count { get; set; }
}

public class OverviewTrendResponse
{
    public List<TrendPointResponse> Households { get; set; }
    public List<TrendPointResponse> NewPartners { get; set; }
}

public class PartnerRowResponse
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public int Households { get; set; }
    public string NativeCurrency { get; set; }
    public decimal NativeAmount { get; set; }
    public decimal? ConvertedAmount { get; set; }
    public string ConversionStatus { get; set; }
    public int Bookings { get; set; }
}

public class ModalityRowResponse
{
    public string Modality { get; set; }
    public int Households { get; set; }
    public double Share { get; set; }
}

public class DuplicatesSummaryResponse
{
    public int UniqueOverlaps { get; set; }
    public int PrebookingRuns { get; set; }
    public int HouseholdRecordsChecked { get; set; }

    // unique overlaps / pre-booking records checked — mixed cardinality by
    // design (overlaps also come from Booking runs); the tile labels the formula.
    public double? OverlapRate { get; set; }

    public ValueSummaryResponse ValueOfOverlaps { get; set; }
}

public class DuplicatesTrendPointResponse
{
    public DateTime Bucket { get; set; }
    public int UniqueOverlaps { get; set; }
    public int HouseholdRecordsChecked { get; set; }
}

public class DuplicatesSplitResponse
{
    public int WithinAgency { get; set; }
    public int AcrossAgency { get; set; }
}

public class BlockingPartnerRowResponse
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public int OverlapsCaused { get; set; }
}

public class ConflictEventRowResponse
{
    public Guid Id { get; set; }

    // Truncated HMAC-derived label ("HH …a3f9c1") — stable per household,
    // never the plaintext ID.
    public string SubjectLabel { get; set; }

    public Guid RequestingOrganizationId { get; set; }
    public string RequestingOrganizationName { get; set; }
    public Guid BlockingOrganizationId { get; set; }
    public string BlockingOrganizationName { get; set; }

    public DateOnly OverlapStartDate { get; set; }
    public DateOnly OverlapEndDate { get; set; }

    public decimal ProposedAmount { get; set; }
    public string ProposedCurrency { get; set; }
    public decimal? ConvertedAmount { get; set; }
    public string ConversionStatus { get; set; }
    public string ProposedModality { get; set; }
    public int ProposedRounds { get; set; }

    public DateTime FirstDetectedAt { get; set; }
    public DateTime LastDetectedAt { get; set; }
    public int DetectionCount { get; set; }

    // "within_same_agency" | "across_agencies"
    public string Category { get; set; }
}

public class ConflictEventListResponse
{
    public List<ConflictEventRowResponse> Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class BlockingBookingResponse
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal? ConvertedAmount { get; set; }
    public string ConversionStatus { get; set; }
    public string Modality { get; set; }
    public int Rounds { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConflictEventDetailResponse : ConflictEventRowResponse
{
    public BlockingBookingResponse BlockingBooking { get; set; }
}
