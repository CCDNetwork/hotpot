using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;

namespace Ccd.Server.Referrals;

[ApiController]
[Route("/api/v1/referrals")]
public class ReferralController : ControllerBaseExtended
{
    private readonly ExportService _exportService;
    private readonly IMapper _mapper;
    private readonly ReferralService _referralService;
    private readonly UserService _userService;

    public ReferralController(
        ReferralService referralService,
        UserService userService,
        IMapper mapper,
        ExportService exportService
    )
    {
        _referralService = referralService;
        _userService = userService;
        _mapper = mapper;
        _exportService = exportService;
    }

    [HttpGet]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<PagedApiResponse<ReferralResponse>>> GetAll(
        [FromQuery] RequestParameters requestParams,
        [FromQuery] bool received = false
    )
    {
        var referrals = await _referralService.GetReferralsApi(
            OrganizationId,
            requestParams,
            received
        );
        return Ok(referrals);
    }

    [HttpGet("{id}")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ReferralResponse>> GetReferral(Guid id)
    {
        var referral = await _referralService.GetReferralApi(OrganizationId, id);

        if (referral == null)
            throw new NotFoundException();

        return Ok(referral);
    }

    [HttpGet("{caseNumber}/case-number")]
    public async Task<ActionResult<ReferralCaseNumberResponse>> GetReferralByCaseNumber(
        string caseNumber
    )
    {
        var result = await _referralService.GetReferralByCaseNumberApi(caseNumber);
        return Ok(result);
    }

    [HttpPost]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ReferralResponse>> Add([FromBody] ReferralAddRequest model)
    {
        if (!model.IsDraft)
        {
            var missingFields = new List<string>();
            var fieldsToCheck = new Dictionary<string, object>
            {
                { nameof(model.OrganizationReferredToId), model.OrganizationReferredToId },
                { nameof(model.FirstName), model.FirstName },
                { nameof(model.PatronymicName), model.PatronymicName },
                { nameof(model.Surname), model.Surname },
                { nameof(model.Gender), model.Gender },
                { nameof(model.Required), model.Required }
            };

            missingFields = fieldsToCheck
                .Where(field => field.Value == null)
                .Select(field => field.Key)
                .ToList();

            if (missingFields.Count != 0)
            {
                var missingFieldsMessage = string.Join(", ", missingFields);
                var errorMessage =
                    $"The following fields are required but missing: {missingFieldsMessage}.";
                throw new BadRequestException(errorMessage);
            }
        }

        var newReferral = await _referralService.AddReferral(OrganizationId, model);
        var result = await _referralService.GetReferralApi(OrganizationId, newReferral.Id);

        return Ok(result);
    }

    [HttpPatch("{id}")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ReferralResponse>> Update(
        Guid id,
        [FromBody] ReferralPatchRequest model
    )
    {
        var referral =
            await _referralService.GetReferralById(OrganizationId, id)
            ?? throw new NotFoundException("Referral not found.");
        if (model.Status != null && !ReferralStatus.IsValid(model.Status))
            throw new BadRequestException("Invalid status.");
        if (referral.IsDraft)
            model.Status = ReferralStatus.UnderReview;

        var updatedFieldsText = await _referralService.GetUpdatedFieldText(model, referral);
        model.Patch(referral);

        await _referralService.UpdateReferral(referral);

        var result = await _referralService.GetReferralApi(OrganizationId, id);

        var userUpdated = await _userService.GetUserById(UserId);
        await _referralService.AddDiscussionBot(
            id,
            new DiscussionAddRequest
            {
                Text =
                    @$"
                Referral has been updated by {userUpdated.FirstName} {userUpdated.LastName}.<br>
                Changes:<br>
                {updatedFieldsText}
            "
            }
        );

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ReferralResponse>> Delete(Guid id)
    {
        var referral =
            await _referralService.GetReferralById(OrganizationId, id)
            ?? throw new NotFoundException();

        var result = await _referralService.GetReferralApi(OrganizationId, referral.Id);

        await _referralService.DeleteReferral(referral);

        return Ok(result);
    }

    [HttpPatch("{id}/withdraw")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ReferralResponse>> RefferalWithdraw(
        Guid id,
        [FromBody] DiscussionAddRequest model
    )
    {
        var referral =
            await _referralService.GetReferralById(OrganizationId, id)
            ?? throw new NotFoundException();

        // Since we removed the Withdrawn status and the client wants all "withdrawn" referrals to go into draft, this is the change that reflects on that
        if (referral.IsDraft)
            throw new BadRequestException("Referral is already withdrawn.");

        referral.IsDraft = true;
        referral.OrganizationReferredTo = null;
        referral.OrganizationReferredToId = null;
        referral.ServiceCategory = null;
        referral.SubactivitiesIds = null;

        await _referralService.UpdateReferral(referral);

        var result = await _referralService.GetReferralApi(OrganizationId, id);

        var userUpdated = await _userService.GetUserById(UserId);
        await _referralService.AddDiscussionBot(
            id,
            new DiscussionAddRequest
            {
                Text =
                    @$"
                Referral has been withdrawn by {userUpdated.FirstName} {userUpdated.LastName}.<br>
                Reason: {model.Text}
            "
            }
        );

        return Ok(result);
    }

    [HttpPatch("{id}/reject")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<ReferralResponse>> RefferalReject(
        Guid id,
        [FromBody] DiscussionAddRequest model
    )
    {
        var referral =
            await _referralService.GetReferralById(OrganizationId, id)
            ?? throw new NotFoundException();

        // Same here, we dont have "rejected" status anymore but a new flag "isRejected" which we check for instead of that previous status
        if (referral.IsRejected)
            throw new BadRequestException("Referral is already rejected.");

        referral.IsRejected = true;

        await _referralService.UpdateReferral(referral);

        var result = await _referralService.GetReferralApi(OrganizationId, id);

        var userUpdated = await _userService.GetUserById(UserId);
        await _referralService.AddDiscussionBot(
            id,
            new DiscussionAddRequest
            {
                Text =
                    @$"
                Referral has been rejected by {userUpdated.FirstName} {userUpdated.LastName}.<br>
                Reason: {model.Text}
            "
            }
        );

        return Ok(result);
    }

    [HttpGet("{id}/discussions")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<List<DiscussionResponse>>> GetDiscussions(Guid id)
    {
        var result = await _referralService.GetDiscussionsApi(id);
        return Ok(result);
    }

    [HttpPost("{id}/discussions")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<DiscussionResponse>> AddDiscussion(
        Guid id,
        [FromBody] DiscussionAddRequest model
    )
    {
        var newDiscussion = await _referralService.AddDiscussion(id, model);
        var result = await _referralService.GetDiscussionApi(newDiscussion.Id);

        return Ok(result);
    }

    [HttpGet("focal-point/users")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<PagedApiResponse<FocalPointUsersResponse>>> GetFocalPointUsers(
        [FromQuery] RequestParameters requestParams,
        [FromQuery] string permission
    )
    {
        var userResponse = await _userService.GetUsersApi(
            OrganizationId,
            requestParams,
            permission
        );
        var data = _mapper.Map<List<UserResponse>, List<FocalPointUsersResponse>>(
            userResponse.Data
        );

        return Ok(
            new PagedApiResponse<FocalPointUsersResponse> { Data = data, Meta = userResponse.Meta }
        );
    }

    [HttpPost("batch-create")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult<BatchCreateResponse>> BatchCreate(
        [FromForm] BatchCreateRequest model
    )
    {
        var response = await _referralService.CreateBatchReferrals(OrganizationId, UserId, model);
        return Ok(response);
    }

    [HttpGet("export")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> ExportReferrals(
        [FromQuery] RequestParameters requestParameters,
        [FromQuery] bool received = false
    )
    {
        var referrals = await _referralService.GetReferralsApi(
            OrganizationId,
            requestParameters,
            received
        );

        var xlsx = _exportService.ExportXls<ReferralResponse, ReferralExportResponse>(
            referrals.Data
        );

        return File(
            xlsx,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "export.xlsx"
        );
    }

    [HttpGet("template")]
    [PermissionLevel(UserRole.User)]
    public async Task<ActionResult> DownloadTemplate()
    {
        var resourceStream = typeof(ReferralService).Assembly.GetManifestResourceStream(
            "Ccd.Server.Templates.Referrals_template.xlsx"
        );

        using var reader = new StreamReader(resourceStream, Encoding.UTF8);

        var memoryStream = new MemoryStream();
        await reader.BaseStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return File(
            memoryStream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Referrals_template.xlsx"
        );
    }
}
