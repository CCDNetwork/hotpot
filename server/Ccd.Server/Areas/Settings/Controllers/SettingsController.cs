using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Mvc;

namespace Ccd.Server.Settings;

[ApiController]
[Route("/api/v1/settings")]
public class SettingsController : ControllerBaseExtended
{
    private readonly IMapper _mapper;
    private readonly SettingsService _settingsService;


    public SettingsController(SettingsService settingsService, IMapper mapper)
    {
        _settingsService = settingsService;
        _mapper = mapper;
    }

    [HttpGet]
    [PermissionLevel(UserRole.User, allowSuperAdmin: true)]
    public async Task<ActionResult<SettingsResponse>> GetSettings()
    {
        var settings = await _settingsService.GetSettingsApi() ?? throw new NotFoundException();
        return Ok(settings);
    }

    [HttpPut]
    [PermissionLevel(UserRole.SuperAdmin, allowSuperAdmin: true)]
    public async Task<ActionResult<SettingsResponse>> Update(
        [FromBody] SettingsUpdateRequest model
    )
    {
        await _settingsService.UpdateSettingsApi(model);

        var result = await _settingsService.GetSettingsApi();

        return Ok(result);
    }
}