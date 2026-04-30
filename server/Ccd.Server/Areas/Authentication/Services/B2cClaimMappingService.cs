using System.Security.Claims;
using System.Threading.Tasks;
using Ccd.Server.Helpers;
using Ccd.Server.Users;

namespace Ccd.Server.Authentication;

public class B2cClaimMappingService
{
    private readonly UserService _userService;

    public B2cClaimMappingService(UserService userService)
    {
        _userService = userService;
    }

    public async Task<User> BindAsync(ClaimsPrincipal principal)
    {
        var email = GetEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedException("No email claim found in B2C token.");

        var user = await _userService.GetUserByEmail(email);

        if (user == null)
            throw new ForbiddenException("USER_NOT_INVITED");

        return user;
    }

    private static string GetEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
               ?? principal.FindFirstValue("emails")
               ?? principal.FindFirstValue("preferred_username")
               ?? principal.FindFirstValue("email");
    }
}
