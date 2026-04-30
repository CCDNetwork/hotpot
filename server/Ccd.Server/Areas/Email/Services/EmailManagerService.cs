using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Ccd.Server.Notifications;

namespace Ccd.Server.Email;

public class EmailManagerService
{
    private readonly INotificationService _notificationService;
    private readonly CcdContext _context;

    public EmailManagerService(
        INotificationService notificationService,
        CcdContext context
    )
    {
        _notificationService = notificationService;
        _context = context;
    }

    public async Task SendWelcomeMail(string to, string firstName, string activationLink)
    {
        var resourceStream = typeof(EmailManagerService).Assembly.GetManifestResourceStream(
            "Ccd.Server.Emails.Welcome.html"
        );
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);

        var html = reader.ReadToEnd();

        html = html.Replace("{{ FirstName }}", firstName);
        html = html.Replace("{{ ActivationLink }}", activationLink);

        await _notificationService.SendEmail(to, "Welcome to Ccd", html);
    }

    public async Task SendAccountReadyMail(string to, string firstName, string profilePageLink, string searchPageLink)
    {
        var resourceStream = typeof(EmailManagerService).Assembly.GetManifestResourceStream(
            "Ccd.Server.Emails.AccountReady.html"
        );
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);

        var html = reader.ReadToEnd();

        html = html.Replace("{{ FirstName }}", firstName);
        html = html.Replace("{{ ProfilePageLink }}", profilePageLink);
        html = html.Replace("{{ SearchPageLink }}", searchPageLink);

        await _notificationService.SendEmail(to, "Welcome to Ccd!", html);
    }

    public async Task SendForgotPasswordMail(string to, string firstName, string resetPasswordLink)
    {
        var resourceStream = typeof(EmailManagerService).Assembly.GetManifestResourceStream(
            "Ccd.Server.Emails.ForgotPassword.html"
        );
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);

        var html = reader.ReadToEnd();

        html = html.Replace("{{ FirstName }}", firstName);
        html = html.Replace("{{ ResetPasswordLink }}", resetPasswordLink);

        await _notificationService.SendEmail(to, "Reset Password Request for Ccd Account", html);
    }

    public async Task SendB2cInviteMail(string to, string firstName, string email, string signupLink)
    {
        var resourceStream = typeof(EmailManagerService).Assembly.GetManifestResourceStream(
            "Ccd.Server.Emails.B2cInvitation.html"
        );
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);

        var html = reader.ReadToEnd();

        html = html.Replace("{{ FirstName }}", firstName);
        html = html.Replace("{{ Email }}", email);
        html = html.Replace("{{ SignupLink }}", signupLink);

        await _notificationService.SendEmail(to, "You've been invited to HotPot", html);
    }

    public async Task SendResetPasswordMail(string to, string firstName, string searchPageLink)
    {
        var resourceStream = typeof(EmailManagerService).Assembly.GetManifestResourceStream(
            "Ccd.Server.Emails.ResetPasswordConfirm.html"
        );
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);

        var html = reader.ReadToEnd();

        html = html.Replace("{{ FirstName }}", firstName);
        html = html.Replace("{{ SearchPageLink }}", searchPageLink);

        await _notificationService.SendEmail(to, "Reset Password Confirmation", html);
    }
}
