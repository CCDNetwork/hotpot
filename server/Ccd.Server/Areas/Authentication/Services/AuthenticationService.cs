using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Email;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Users;
using Microsoft.Extensions.Logging;

namespace Ccd.Server.Authentication;

public class AuthenticationService
{
    private const int PasswordResetCodeExpiryMinutes = 15;

    // Invitation links (set your first password) are valid for 2 hours,
    // longer than the 15-minute password-reset link. A resend issues a new
    // link and invalidates the previous one.
    private const int InvitationCodeExpiryHours = 2;

    private readonly DateTimeProvider _dateTimeProvider;
    private readonly EmailManagerService _emailManagerService;
    private readonly IMapper _mapper;
    private readonly OrganizationService _organizationService;
    private readonly SendGridService _sendGridService;
    private readonly UserService _userService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserService userService,
        EmailManagerService emailManagerService,
        DateTimeProvider dateTimeProvider,
        IMapper mapper,
        OrganizationService organizationService,
        SendGridService sendgridService,
        ILogger<AuthenticationService> logger
    )
    {
        _logger = logger;
        _userService = userService;
        _emailManagerService = emailManagerService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
        _organizationService = organizationService;
        _sendGridService = sendgridService;
    }

    private async Task<UserAuthenticationResponse> generateAuthenticationResponse(User user)
    {
        var userData = await _userService.GetUserApi(id: user.Id);
        var organizations = await _userService.GetOrganizationsForUser(user.Id);
        var organizationRoles = new Dictionary<string, string>();
        var userOrganizations = new List<OrganizationUserResponse>();

        foreach (var organization in organizations)
        {
            organizationRoles[organization.Id.ToString()] = await _userService.GetOrganizationRole(
                user.Id,
                organization.Id
            );

            var ut = _mapper.Map<OrganizationUserResponse>(organization);
            ut.Role = organizationRoles[organization.Id.ToString()];

            userOrganizations.Add(ut);
        }

        return new UserAuthenticationResponse(
            AuthenticationHelper.GenerateToken(user, organizationRoles),
            userData,
            userOrganizations
        );
    }

    public async Task<UserAuthenticationResponse> Authenticate(string email, string password)
    {
        // check for superadmin login
        if (email == "superadmin" && password == StaticConfiguration.SuperadminPassword)
            return await generateAuthenticationResponse(User.SYSTEM_USER);

        var user =
            await _userService.GetUserByEmail(email)
            ?? throw new UnauthorizedException("Invalid username or password");

        if (!AuthenticationHelper.VerifyPassword(user, password))
            throw new UnauthorizedException("Invalid username or password");

        if (!user.ActivatedAt.HasValue)
            throw new UnauthorizedException("User is not active");

        return await generateAuthenticationResponse(user);
    }

    public async Task<UserAuthenticationResponse> Activate(string email, string activationCode)
    {
        var user =
            await _userService.GetUserByEmail(email)
            ?? throw new BadRequestException("User not found");

        if (user.ActivatedAt.HasValue)
            throw new BadRequestException("User is already activated");

        if (user.ActivationCode != activationCode)
            throw new BadRequestException("Invalid activation code");

        var organizations = await _userService.GetOrganizationsForUser(user.Id);

        if (organizations.Count == 0)
            throw new BadRequestException("User has no organization");

        user.ActivatedAt = _dateTimeProvider.UtcNow;

        await _userService.UpdateUser(user);

        await _emailManagerService.SendAccountReadyMail(
            user.Email,
            user.FirstName,
            $"{StaticConfiguration.WebAppUrl}/profile",
            StaticConfiguration.WebAppUrl
        );

        return await generateAuthenticationResponse(user);
    }

    public async Task ForgotPassword(string email)
    {
        var user =
            await _userService.GetUserByEmail(email)
            ?? throw new BadRequestException("User with given email address not found");

        IssuePasswordResetCode(user);
        await _userService.UpdateUser(user);

        await SendPasswordResetEmail(
            user,
            StaticConfiguration.SendgridPasswordResetEmailTemplateId
        );
    }

    /// <summary>
    /// Prepares the new user for first sign-in and emails the invitation.
    /// The user is kept even if the email cannot be sent (e.g. email service
    /// down): returns false so the Admin can be told and resend later.
    /// </summary>
    public async Task<bool> InviteUser(User user)
    {
        if (StaticConfiguration.IsB2C)
        {
            user.ActivatedAt = _dateTimeProvider.UtcNow;
        }
        else
        {
            IssueInvitationCode(user);
        }
        await _userService.UpdateUser(user);

        try
        {
            await SendInvitationEmail(user);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Invitation email to user {UserId} could not be sent; user kept for resend", user.Id);
            return false;
        }
    }

    /// <summary>
    /// Sends the invitation again. Password sign-in: issues a fresh
    /// set-password link (only while the user has not set a password yet).
    /// B2C: resends the sign-in link. Throws if the email cannot be sent.
    /// </summary>
    public async Task ResendInvitation(User user)
    {
        if (!StaticConfiguration.IsB2C)
        {
            if (user.ActivatedAt.HasValue)
                throw new BadRequestException(
                    "This user has already set a password. They can use 'Forgot your password?' on the sign-in page."
                );

            IssueInvitationCode(user);
            await _userService.UpdateUser(user);
        }

        try
        {
            await SendInvitationEmail(user);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Resending invitation to user {UserId} failed", user.Id);
            throw new BadRequestException("The invitation email could not be sent. Please try again later.");
        }
    }

    private async Task SendInvitationEmail(User user)
    {
        if (StaticConfiguration.IsB2C)
            await SendB2cInviteEmail(user);
        else
            await SendPasswordResetEmail(user, StaticConfiguration.SendgridInvitationEmailTemplateId);
    }

    public async Task ResetPassword(string email, string passwordResetCode, string password)
    {
        var user =
            await _userService.GetUserByEmail(email)
            ?? throw new BadRequestException("User with given email address not found");

        if (
            string.IsNullOrEmpty(user.PasswordResetCode)
            || user.PasswordResetCode != passwordResetCode
        )
            throw new BadRequestException("Invalid password reset code");

        if (
            !user.PasswordResetCodeExpiresAt.HasValue
            || user.PasswordResetCodeExpiresAt.Value < _dateTimeProvider.UtcNow
        )
            throw new BadRequestException("Password reset code has expired");

        PasswordPolicyValidator.Validate(password);

        user.Password = AuthenticationHelper.HashPassword(user, password);
        user.PasswordResetCode = null;
        user.PasswordResetCodeExpiresAt = null;

        if (!user.ActivatedAt.HasValue)
            user.ActivatedAt = _dateTimeProvider.UtcNow;

        await _userService.UpdateUser(user);
    }

    private void IssueInvitationCode(User user)
    {
        user.PasswordResetCode = Guid.NewGuid().ToString();
        user.PasswordResetCodeExpiresAt = _dateTimeProvider.UtcNow.AddHours(InvitationCodeExpiryHours);
    }

    private void IssuePasswordResetCode(User user)
    {
        user.PasswordResetCode = Guid.NewGuid().ToString();
        user.PasswordResetCodeExpiresAt = _dateTimeProvider.UtcNow.AddMinutes(
            PasswordResetCodeExpiryMinutes
        );
    }

    private async Task SendB2cInviteEmail(User user)
    {
        var signupUrl = StaticConfiguration.WebAppUrl + "/sign-in";

        var templateId = StaticConfiguration.SendgridB2cInvitationEmailTemplateId;
        if (!string.IsNullOrEmpty(templateId))
        {
            var templateData = new Dictionary<string, string>
            {
                { "firstName", user.FirstName },
                { "email", user.Email },
                { "buttonLink", signupUrl }
            };

            await _sendGridService.SendEmail(user.Email, templateId, templateData);
        }
        else
        {
            await _emailManagerService.SendB2cInviteMail(
                user.Email,
                user.FirstName,
                user.Email,
                signupUrl
            );
        }
    }

    private async Task SendPasswordResetEmail(User user, string templateId)
    {
        var resetLink =
            StaticConfiguration.WebAppUrl
            + $"/reset-password?email={WebUtility.UrlEncode(user.Email)}&code={user.PasswordResetCode}";

        var templateData = new Dictionary<string, string>
        {
            { "firstName", user.FirstName },
            { "buttonLink", resetLink }
        };

        await _sendGridService.SendEmail(user.Email, templateId, templateData);
    }
}