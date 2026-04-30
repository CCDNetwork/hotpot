using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Ccd.Server.Helpers;
using Ccd.Server.Users;
using Microsoft.Extensions.Logging;

namespace Ccd.Server.Authentication;

public class B2cClaimMappingService
{
    private readonly UserService _userService;
    private readonly DateTimeProvider _dateTimeProvider;
    private readonly ILogger<B2cClaimMappingService> _logger;

    public B2cClaimMappingService(
        UserService userService,
        DateTimeProvider dateTimeProvider,
        ILogger<B2cClaimMappingService> logger
    )
    {
        _userService = userService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<User> BindAsync(ClaimsPrincipal principal)
    {
        var email = GetEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedException("No email claim found in B2C token.");

        var user = await _userService.GetUserByEmail(email);

        if (user == null)
            throw new ForbiddenException("USER_NOT_INVITED");

        if (user.Status == UserStatus.Disabled)
            throw new ForbiddenException("USER_DISABLED");

        if (user.Status == UserStatus.Pending)
        {
            user.Status = UserStatus.Active;
            user.ActivatedAt = _dateTimeProvider.UtcNow;
            await _userService.UpdateUser(user);

            _logger.LogInformation(
                "User activated via B2C login: {UserId}, {Email}, event=user.activated",
                user.Id,
                user.Email
            );
        }

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
