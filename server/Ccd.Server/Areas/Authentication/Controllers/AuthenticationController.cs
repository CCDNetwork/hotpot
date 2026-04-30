using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ccd.Server.Authentication;

[ApiController]
[Route("/api/v1/authentication")]
[EnableRateLimiting(RateLimitingSetup.AuthPolicyName)]
public class OrganizationController : ControllerBaseExtended
{
    private readonly AuthenticationService _authenticationService;
    private readonly DateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public OrganizationController(AuthenticationService authenticationService, DateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _authenticationService = authenticationService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    // [HttpPost("registration")]
    // public async Task<ActionResult<UserAuthenticationResponse>> Register([FromBody] UserRegistrationRequest model)
    // {
    //     return Created("", await _authenticationService.Register(model));
    // }

    [HttpPost("login")]
    public async Task<ActionResult<UserAuthenticationResponse>> Authenticate([FromBody] UserLoginRequest model)
    {
        return Ok(await _authenticationService.Authenticate(model.Username, model.Password));
    }

    [HttpPost("activation")]
    public async Task<ActionResult<UserAuthenticationResponse>> Activate([FromBody] UserActivationRequest model)
    {
        var authResponse = await _authenticationService.Activate(model.Email, model.ActivationCode);
        return Ok(authResponse);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
    {
        await _authenticationService.ForgotPassword(model.Email);
        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
    {
        await _authenticationService.ResetPassword(model.Email, model.PasswordResetCode, model.Password);
        return Ok();
    }

    [HttpPost("login-init")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingSetup.LoginInitPolicyName)]
    public ActionResult<LoginInitResponse> LoginInit([FromBody] LoginInitRequest model)
    {
        return Ok(new LoginInitResponse { LoginHint = model.Email });
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public ActionResult<AuthConfigResponse> GetAuthConfig()
    {
        return Ok(new AuthConfigResponse { AuthProvider = StaticConfiguration.AuthProvider });
    }
}
