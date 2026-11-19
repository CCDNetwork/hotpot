using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;

namespace Ccd.Server.Organizations;

/// <summary>
/// Allowed email domains of an organisation. Platform Admins manage the list
/// for every organisation (same reach as organisation and user management);
/// every change is written to the change log.
/// </summary>
[ApiController]
[Route("/api/v1/organizations/{organizationId}/email-domains")]
public class OrganizationEmailDomainController : ControllerBaseExtended
{
    private readonly OrganizationEmailDomainService _service;

    public OrganizationEmailDomainController(OrganizationEmailDomainService service)
    {
        _service = service;
    }

    /// <summary>
    /// Validate a domain on the Add Organisation form, before the organisation
    /// exists. Returns the normalised domain and every organisation using it.
    /// </summary>
    [HttpPost("/api/v1/organizations/email-domains/check")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<EmailDomainCheckResponse>> CheckForNewOrganization([FromBody] EmailDomainRequest model)
    {
        return Ok(await _service.CheckForNewOrganization(model?.Domain));
    }

    [HttpGet]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<List<OrganizationEmailDomainResponse>>> GetAll(Guid organizationId)
    {
        return Ok(await _service.GetDomains(organizationId));
    }

    /// <summary>
    /// Validate and normalise a domain before the Admin confirms it; also
    /// returns other organisations already using it (for the warning).
    /// Saves nothing.
    /// </summary>
    [HttpPost("check")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<EmailDomainCheckResponse>> Check(
        Guid organizationId,
        [FromBody] EmailDomainRequest model,
        [FromQuery] Guid? excludeDomainId = null
    )
    {
        return Ok(await _service.Check(organizationId, model?.Domain, excludeDomainId));
    }

    [HttpPost]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<OrganizationEmailDomainResponse>> Add(
        Guid organizationId,
        [FromBody] EmailDomainRequest model
    )
    {
        return Ok(await _service.Add(organizationId, model?.Domain, UserId));
    }

    [HttpPut("{domainId}")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<OrganizationEmailDomainResponse>> Update(
        Guid organizationId,
        Guid domainId,
        [FromBody] EmailDomainRequest model
    )
    {
        return Ok(await _service.Update(organizationId, domainId, model?.Domain, UserId));
    }

    [HttpDelete("{domainId}")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult> Remove(Guid organizationId, Guid domainId)
    {
        await _service.Remove(organizationId, domainId, UserId);
        return NoContent();
    }

    [HttpGet("history")]
    [PermissionLevel(UserRole.OrganizationAdmin)]
    public async Task<ActionResult<List<ChangeLogEntryResponse>>> GetHistory(Guid organizationId)
    {
        return Ok(await _service.GetHistory(organizationId));
    }
}
