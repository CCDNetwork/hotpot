using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;

namespace Ccd.Server.Dashboards;

[ApiController]
[Route("/api/v1/dashboards")]
public class DashboardsController : ControllerBaseExtended
{
    private readonly DashboardService _dashboardService;

    public DashboardsController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Own-org filter rule (committee 3.1): a caller may filter by an
    /// organization only when it is their own; any other value degrades to the
    /// platform-wide aggregate.
    /// </summary>
    private Guid? ResolveOrganizationFilter(Guid? organizationId)
    {
        return organizationId != null && organizationId == OrganizationId
            ? organizationId
            : null;
    }

    [HttpGet("overview/summary")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<OverviewSummaryResponse>> GetOverviewSummary(
        string period,
        Guid? organizationId,
        string displayCurrency,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetOverviewSummary(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("overview/trend")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<OverviewTrendResponse>> GetOverviewTrend(
        string period,
        Guid? organizationId,
        string granularity,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetOverviewTrend(
            period,
            ResolveOrganizationFilter(organizationId),
            granularity,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("overview/partners")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<PartnerRowResponse>>> GetOverviewPartners(
        string period,
        Guid? organizationId,
        string displayCurrency,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetOverviewPartners(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("overview/modality")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<ModalityRowResponse>>> GetOverviewModality(
        string period,
        Guid? organizationId,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetOverviewModality(
            period,
            ResolveOrganizationFilter(organizationId),
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("duplicates/summary")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<DuplicatesSummaryResponse>> GetDuplicatesSummary(
        string period,
        Guid? organizationId,
        string displayCurrency,
        string overlapScope,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetDuplicatesSummary(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency,
            overlapScope,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("duplicates/trend")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<DuplicatesTrendPointResponse>>> GetDuplicatesTrend(
        string period,
        Guid? organizationId,
        string granularity,
        string overlapScope,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetDuplicatesTrend(
            period,
            ResolveOrganizationFilter(organizationId),
            granularity,
            overlapScope,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("duplicates/split")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<DuplicatesSplitResponse>> GetDuplicatesSplit(
        string period,
        Guid? organizationId,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetDuplicatesSplit(
            period,
            ResolveOrganizationFilter(organizationId),
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("duplicates/blocking-partners")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<BlockingPartnerRowResponse>>> GetBlockingPartners(
        string period,
        Guid? organizationId,
        string overlapScope,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetBlockingPartners(
            period,
            ResolveOrganizationFilter(organizationId),
            overlapScope,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("duplicates/events")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ConflictEventListResponse>> GetConflictEvents(
        string period,
        Guid? organizationId,
        string displayCurrency,
        string overlapScope,
        int page = 1,
        int pageSize = 20,
        string sort = null,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetConflictEvents(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency,
            overlapScope,
            page,
            pageSize,
            sort,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("duplicates/events/{id}")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ConflictEventDetailResponse>> GetConflictEvent(
        Guid id,
        Guid? organizationId,
        string displayCurrency
    )
    {
        var result = await _dashboardService.GetConflictEvent(
            id,
            ResolveOrganizationFilter(organizationId),
            displayCurrency
        );
        return Ok(result);
    }

    /// <summary>
    /// Value-FX drill: per-(currency, year, month) rows with the InforEuro
    /// rate that was applied to each. `source=bookings` powers the Overview
    /// value tiles; `source=conflicts` powers the Deduplication value tile.
    /// </summary>
    [HttpGet("drills/value")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ValueFxDrillResponse>> GetValueFxDrill(
        string source,
        string period,
        Guid? organizationId,
        string displayCurrency,
        string overlapScope,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetValueFxDrill(
            source,
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency,
            overlapScope,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("overview/drills/organizations")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<OrganizationDrillRowResponse>>> GetOrganizationsDrill(
        string period,
        Guid? organizationId,
        string displayCurrency,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetOrganizationsDrill(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("duplicates/drills/prebooking-runs")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<PrebookingRunDrillResponse>> GetPrebookingRunsDrill(
        string period,
        Guid? organizationId,
        int page = 1,
        int pageSize = 25,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetPrebookingRunsDrill(
            period,
            ResolveOrganizationFilter(organizationId),
            page,
            pageSize,
            from,
            to
        );
        return Ok(result);
    }

    [HttpGet("duplicates/drills/records-checked")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<RecordsCheckedDrillResponse>> GetRecordsCheckedDrill(
        string period,
        Guid? organizationId,
        int page = 1,
        int pageSize = 25,
        DateTime? from = null,
        DateTime? to = null
    )
    {
        var result = await _dashboardService.GetRecordsCheckedDrill(
            period,
            ResolveOrganizationFilter(organizationId),
            page,
            pageSize,
            from,
            to
        );
        return Ok(result);
    }

    /// <summary>
    /// Consecutive-assistance-months histogram — trailing 12 months,
    /// cross-org, released bookings excluded, buckets 1..5 exact + 6+.
    /// See DashboardService for the run-detection semantics.
    /// </summary>
    [HttpGet("overview/consecutive-months")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ConsecutiveMonthsHistogramResponse>> GetConsecutiveMonthsHistogram()
    {
        var result = await _dashboardService.GetConsecutiveMonthsHistogram();
        return Ok(result);
    }

    [HttpGet("overview/consecutive-months/details")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ConsecutiveMonthsDrillResponse>> GetConsecutiveMonthsDetails(
        string bucket,
        int page = 1,
        int pageSize = 25
    )
    {
        var result = await _dashboardService.GetConsecutiveMonthsDetails(bucket, page, pageSize);
        return Ok(result);
    }
}
