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
        string displayCurrency
    )
    {
        var result = await _dashboardService.GetOverviewSummary(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency
        );
        return Ok(result);
    }

    [HttpGet("overview/trend")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<OverviewTrendResponse>> GetOverviewTrend(
        string period,
        Guid? organizationId,
        string granularity
    )
    {
        var result = await _dashboardService.GetOverviewTrend(
            period,
            ResolveOrganizationFilter(organizationId),
            granularity
        );
        return Ok(result);
    }

    [HttpGet("overview/partners")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<PartnerRowResponse>>> GetOverviewPartners(
        string period,
        Guid? organizationId,
        string displayCurrency
    )
    {
        var result = await _dashboardService.GetOverviewPartners(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency
        );
        return Ok(result);
    }

    [HttpGet("overview/modality")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<ModalityRowResponse>>> GetOverviewModality(
        string period,
        Guid? organizationId
    )
    {
        var result = await _dashboardService.GetOverviewModality(
            period,
            ResolveOrganizationFilter(organizationId)
        );
        return Ok(result);
    }

    [HttpGet("duplicates/summary")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<DuplicatesSummaryResponse>> GetDuplicatesSummary(
        string period,
        Guid? organizationId,
        string displayCurrency
    )
    {
        var result = await _dashboardService.GetDuplicatesSummary(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency
        );
        return Ok(result);
    }

    [HttpGet("duplicates/trend")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<DuplicatesTrendPointResponse>>> GetDuplicatesTrend(
        string period,
        Guid? organizationId,
        string granularity
    )
    {
        var result = await _dashboardService.GetDuplicatesTrend(
            period,
            ResolveOrganizationFilter(organizationId),
            granularity
        );
        return Ok(result);
    }

    [HttpGet("duplicates/split")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<DuplicatesSplitResponse>> GetDuplicatesSplit(
        string period,
        Guid? organizationId
    )
    {
        var result = await _dashboardService.GetDuplicatesSplit(
            period,
            ResolveOrganizationFilter(organizationId)
        );
        return Ok(result);
    }

    [HttpGet("duplicates/blocking-partners")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<BlockingPartnerRowResponse>>> GetBlockingPartners(
        string period,
        Guid? organizationId
    )
    {
        var result = await _dashboardService.GetBlockingPartners(
            period,
            ResolveOrganizationFilter(organizationId)
        );
        return Ok(result);
    }

    [HttpGet("duplicates/events")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ConflictEventListResponse>> GetConflictEvents(
        string period,
        Guid? organizationId,
        string displayCurrency,
        int page = 1,
        int pageSize = 20,
        string sort = null
    )
    {
        var result = await _dashboardService.GetConflictEvents(
            period,
            ResolveOrganizationFilter(organizationId),
            displayCurrency,
            page,
            pageSize,
            sort
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
}
