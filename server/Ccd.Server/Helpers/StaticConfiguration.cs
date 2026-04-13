using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Ccd.Server.Helpers;

public class StaticConfiguration
{
    private static IConfiguration _configuration;

    public static string WebAppUrl =>
        Environment.GetEnvironmentVariable("WEB_APP_URL")
        ?? _configuration.GetValue<string>("WebAppUrl");

    public static string DbConnectionString =>
        Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        ?? _configuration.GetValue<string>("ConnectionStrings:CcdServerDB");

    public static string AppSettingsSecret =>
        Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? _configuration.GetValue<string>("AppSettings:Secret");

    public static string AppSettingsExpirationMinutes =>
        Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES")
        ?? _configuration.GetValue<string>("AppSettings:ExpirationMinutes");

    public static string SentryDsn =>
        Environment.GetEnvironmentVariable("SENTRY_DSN")
        ?? _configuration.GetValue<string>("Sentry:Dsn");

    public static string ApiKey =>
        Environment.GetEnvironmentVariable("API_KEY") ?? _configuration.GetValue<string>("ApiKey");

    public static string ApiUrl =>
        Environment.GetEnvironmentVariable("API_URL") ?? _configuration.GetValue<string>("ApiUrl");

    public static string AppUrl =>
        Environment.GetEnvironmentVariable("APP_URL") ?? _configuration.GetValue<string>("AppUrl");

    public static string StorageUrl =>
        Environment.GetEnvironmentVariable("STORAGE_URL")
        ?? _configuration.GetValue<string>("StorageUrl");

    public static string StoragePath =>
        Environment.GetEnvironmentVariable("STORAGE_PATH")
        ?? _configuration.GetValue<string>("StoragePath");

    public static string NotificationServiceUrl =>
        Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL")
        ?? _configuration.GetValue<string>("NotificationServiceUrl");

    public static string EmailSender =>
        Environment.GetEnvironmentVariable("EMAIL_SENDER")
        ?? _configuration.GetValue<string>("EmailSender");

    public static string SendgridApiKey =>
        Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
        ?? _configuration.GetValue<string>("SendgridApiKey");

    public static string SendgridSenderEmail =>
        Environment.GetEnvironmentVariable("SENDGRID_SENDER_EMAIL")
        ?? _configuration.GetValue<string>("SendgridSenderEmail");

    public static string SendgridInvitationEmailTemplateId =>
        Environment.GetEnvironmentVariable("SENDGRID_INVITATION_EMAIL_TEMPLATE_ID")
        ?? _configuration.GetValue<string>("SendgridInvitationEmailTemplateId");

    public static string SendgridPasswordResetEmailTemplateId =>
        Environment.GetEnvironmentVariable("SENDGRID_PASSWORD_RESET_EMAIL_TEMPLATE_ID")
        ?? _configuration.GetValue<string>("SendgridPasswordResetEmailTemplateId");

    public static string SuperadminPassword =>
        Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD")
        ?? _configuration.GetValue<string>("SuperadminPassword");

    public static string EncryptionKey =>
        Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
        ?? _configuration.GetValue<string>("EncryptionKey");

    public static void Initialize(IConfiguration configuration)
    {
        _configuration = configuration;

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DbConnectionString))
            errors.Add("ConnectionStringsCcdServerDB");

        if (string.IsNullOrWhiteSpace(AppSettingsSecret))
            errors.Add("AppSettingsSecret");

        if (string.IsNullOrWhiteSpace(ApiKey))
            errors.Add("ApiKey");

        if (errors.Count > 0)
            throw new Exception(string.Join(", ", errors));
    }
}