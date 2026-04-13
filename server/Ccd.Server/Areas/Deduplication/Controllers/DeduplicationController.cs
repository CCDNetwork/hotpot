using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ccd.Server.Helpers;
using Ccd.Server.Notifications;
using Ccd.Server.Users;
using Ccd.Server.Deduplication.Controllers.ControllerModels;
using Ccd.Server.Referrals;
using System;

namespace Ccd.Server.Deduplication;

[ApiController]
[Route("/api/v1/deduplication")]
public class DeduplicationController : ControllerBaseExtended
{
    private readonly DeduplicationService _deduplicationService;
    private readonly BookingService _bookingService;
    private readonly ExportService _exportService;

    public DeduplicationController(DeduplicationService deduplicationService, BookingService bookingService, ExportService exportService)
    {
        _deduplicationService = deduplicationService;
        _bookingService = bookingService;
        _exportService = exportService;
    }

    [HttpGet("listings")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<DeduplicationListResponse>> GetAllListings([FromQuery] RequestParameters requestParameters)
    {
        var listings = await _deduplicationService.GetAllListings(this.OrganizationId, requestParameters);
        return Ok(listings);
    }

    [HttpDelete]
    [PermissionLevel(UserRole.Admin)]
    public async Task<ActionResult> DeleteListings()
    {
        await _deduplicationService.DeleteListings();
        return NoContent();
    }

    [HttpPost("dataset")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<DatasetDeduplicationResponse>> DatasetDeduplicate([FromForm] DatasetDeduplicationRequest model)
    {
        if (model.TemplateId == Guid.Empty) throw new BadRequestException("Template ID is required.");

        var response = await _deduplicationService.DatasetDeduplication(this.OrganizationId, this.UserId, model);
        return Ok(response);
    }

    [HttpPost("same-organization")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> SameOrganizationDeduplication([FromBody] SameOrganizationDeduplicationRequest model)
    {
        if (model.TemplateId == Guid.Empty) throw new BadRequestException("Template ID is required.");

        var response = await _deduplicationService.SameOrganizationDeduplication(this.OrganizationId, this.UserId, model);
        return Ok(response);
    }

    [HttpPost("system-organizations")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> SystemOrganizationsDeduplication([FromBody] SystemOrganizationsDeduplicationRequest model)
    {
        var result = await _deduplicationService.SystemOrganizationsDeduplication(this.OrganizationId, this.UserId, model);
        return Ok(result);
    }

    [HttpPost("finish")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> FinishDeduplication([FromBody] SystemOrganizationsDeduplicationRequest model)
    {
        var result = await _deduplicationService.FinishDeduplication(this.OrganizationId, this.UserId, model);
        return Ok(result);
    }

    [HttpPost("booking/step-1")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> BookingDeduplicateStep1([FromForm] BookingDeduplicationRequestStep1 model)
    {
        var (isValid, savedFileUrl, savedFileId) = await _bookingService.BookingDeduplicationStep1(this.OrganizationId, this.UserId, model);
        return Ok(new { isValid, fileUrl = savedFileUrl, fileId = savedFileId });
    }

    [HttpPost("booking/step-2")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> BookingDeduplicateStep2([FromBody] BookingDeduplicationRequestStep2 model)
    {
        var (isValid, savedFileUrl, savedFileId) = await _bookingService.BookingDeduplicationStep2(this.OrganizationId, this.UserId, model, model.IsPrebooking);
        return Ok(new { isValid, fileUrl = savedFileUrl, fileId = savedFileId });
    }

    [HttpPost("wizard-finish")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> WizardFinish([FromBody] WizardFinishRequest model)
    {
        await _bookingService.WizardFinish(this.UserId, model.FileId);
        return NoContent();
    }

    [HttpGet("bookings/export")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> ExportBookings(
        [FromQuery] RequestParameters requestParameters,
        string activity
    )
    {
        var bookings = await _bookingService.GetAllBookingsApi(
            OrganizationId,
            requestParameters,
            activity
        );

        var xlsx = _exportService.ExportXls<BookingResponse, BookingExportResponse>(
            bookings.Data
        );

        return File(
            xlsx,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "bookings-export.xlsx"
        );
    }

    [HttpGet("bookings")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<PagedApiResponse<BookingResponse>>> GetAllBookingListings([FromQuery] RequestParameters requestParameters, string activity)
    {
        var bookings = await _bookingService.GetAllBookingsApi(this.OrganizationId, requestParameters, activity);
        return Ok(bookings);
    }

    [HttpPost("booking/{id}/release")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> ReleaseBooking(Guid id)
    {
        await _bookingService.ReleaseBooking(id, this.OrganizationId);
        return NoContent();
    }

    [HttpPost("booking/batch-release")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<BatchReleaseBookingResponse>> BatchReleaseBookings(
        [FromForm] BatchReleaseBookingRequest model
    )
    {
        var result = await _bookingService.BatchReleaseBookings(this.OrganizationId, model.File);
        return Ok(result);
    }
}
