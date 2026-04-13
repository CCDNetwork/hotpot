using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.Email;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Users;

namespace Ccd.Server.Authentication;

public class AuthenticationService
{
    private const int PasswordResetCodeExpiryMinutes = 15;

    private readonly DateTimeProvider _dateTimeProvider;
    private readonly EmailManagerService _emailManagerService;
    private readonly IMapper _mapper;
    private readonly OrganizationService _organizationService;
    private readonly SendGridService _sendGridService;
    private readonly UserService _userService;

    public AuthenticationService(
        UserService userService,
        EmailManagerService emailManagerService,
        DateTimeProvider dateTimeProvider,
        IMapper mapper,
        OrganizationService organizationService,
        SendGridService sendgridService
    )
    {
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

    public async Task InviteUser(User user)
    {
        IssuePasswordResetCode(user);
        await _userService.UpdateUser(user);

        await SendPasswordResetEmail(
            user,
            StaticConfiguration.SendgridInvitationEmailTemplateId
        );
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

    private void IssuePasswordResetCode(User user)
    {
        user.PasswordResetCode = Guid.NewGuid().ToString();
        user.PasswordResetCodeExpiresAt = _dateTimeProvider.UtcNow.AddMinutes(
            PasswordResetCodeExpiryMinutes
        );
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