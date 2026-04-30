using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ccd.Server.Authentication;

[ApiController]
[Route("/api/v1/authentication/b2c")]
public class B2cAuthenticationController : ControllerBaseExtended
{
    private readonly B2cClaimMappingService _b2cClaimMappingService;
    private readonly UserService _userService;
    private readonly IMapper _mapper;

    public B2cAuthenticationController(
        B2cClaimMappingService b2cClaimMappingService,
        UserService userService,
        IMapper mapper
    )
    {
        _b2cClaimMappingService = b2cClaimMappingService;
        _userService = userService;
        _mapper = mapper;
    }

    [HttpPost("token-exchange")]
    [Authorize(AuthenticationSchemes = "B2C")]
    public async Task<ActionResult<UserAuthenticationResponse>> TokenExchange()
    {
        var user = await _b2cClaimMappingService.BindAsync(User);

        var organizations = await _userService.GetOrganizationsForUser(user.Id);
        var organizationRoles = new Dictionary<string, string>();
        var userOrganizations = new List<OrganizationUserResponse>();

        foreach (var organization in organizations)
        {
            var role = await _userService.GetOrganizationRole(user.Id, organization.Id);
            organizationRoles[organization.Id.ToString()] = role;

            var orgResponse = _mapper.Map<OrganizationUserResponse>(organization);
            orgResponse.Role = role;
            userOrganizations.Add(orgResponse);
        }

        var token = AuthenticationHelper.GenerateToken(user, organizationRoles);
        var userData = await _userService.GetUserApi(id: user.Id);

        return Ok(new UserAuthenticationResponse(token, userData, userOrganizations));
    }
}
