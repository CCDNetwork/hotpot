using System;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;

namespace Ccd.Server.Templates;

[ApiController]
[Route("/api/v1/templates")]
public class TemplateController : ControllerBaseExtended
{
    private readonly IMapper _mapper;
    private readonly TemplateService _templateService;

    public TemplateController(TemplateService templateService, IMapper mapper)
    {
        _templateService = templateService;
        _mapper = mapper;
    }

    [HttpGet]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult<PagedApiResponse<TemplateResponse>>> GetAll(
        [FromQuery] RequestParameters requestParams
    )
    {
        var templates = await _templateService.GetTemplatesApi(OrganizationId, requestParams);
        return Ok(templates);
    }

    [HttpGet("{id}")]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult<TemplateResponse>> GetTemplate(Guid id)
    {
        var template = await _templateService.GetTemplateApi(OrganizationId, id);

        if (template == null)
            throw new NotFoundException();

        return Ok(template);
    }


    [HttpPost]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult<TemplateResponse>> Add([FromBody] TemplateAddRequest model)
    {
        var newTemplate = await _templateService.AddTemplate(OrganizationId, model);
        var result = await _templateService.GetTemplateApi(OrganizationId, newTemplate.Id);

        return Ok(result);
    }

    [HttpPatch("{id}")]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult<TemplateResponse>> Update(
        [FromBody] TemplatePatchRequest model,
        Guid id
    )
    {
        var template = await _templateService.GetTemplateById(OrganizationId, id) ??
                       throw new NotFoundException("Template not found.");

        model.Patch(template);

        _mapper.Map(model, template);

        await _templateService.UpdateTemplate(template);
        var result = await _templateService.GetTemplateApi(OrganizationId, id);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [PermissionLevel(UserRole.User)]
    [RequireModulePermission(UserPermission.Deduplication)]
    public async Task<ActionResult<TemplateResponse>> Delete(Guid id)
    {
        var template = await _templateService.GetTemplateById(OrganizationId, id) ??
                       throw new NotFoundException("Template not found.");

        var result = await _templateService.GetTemplateApi(OrganizationId, template.Id);

        await _templateService.DeleteTemplate(template);

        return Ok(result);
    }
}