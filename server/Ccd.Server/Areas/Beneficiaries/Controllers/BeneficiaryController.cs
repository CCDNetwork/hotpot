using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using System;

namespace Ccd.Server.Beneficiaries;

[ApiController]
[Route("/api/v1/beneficiaries")]
public class BeneficiaryController : ControllerBaseExtended
{
    private readonly BeneficaryService _beneficaryService;

    public BeneficiaryController(BeneficaryService beneficaryService)
    {
        _beneficaryService = beneficaryService;
    }

    [HttpGet]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult<PagedApiResponse<BeneficaryResponse>>> GetBeneficiaries([FromQuery] RequestParameters requestParameters)
    {
        var response = await _beneficaryService.GetBeneficiariesApi(requestParameters, this.OrganizationId);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult<BeneficaryResponse>> GetBeneficiary(Guid id)
    {
        var response = await _beneficaryService.GetBeneficiaryApi(this.OrganizationId, id);
        return Ok(response);
    }

    [HttpPatch("{id}/status")]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult<BeneficaryResponse>> PatchBeneficiaryStatus(Guid id, [FromBody] BeneficaryStatusPatchRequest model)
    {
        var response = await _beneficaryService.PatchBeneficiaryStatus(this.OrganizationId, id, model);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult> DeleteBeneficiary(Guid id)
    {
        await _beneficaryService.DeleteBeneficiary(this.OrganizationId, id);
        return NoContent();
    }
}
