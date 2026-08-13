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

    public static string AzureStorageConnectionString =>
        Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
        ?? _configuration.GetValue<string>("AzureStorageConnectionString")
        ?? "";

    public static string AzureBlobContainerName =>
        Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_NAME")
        ?? _configuration.GetValue<string>("AzureBlobContainerName")
        ?? "";

    public static string FileCleanupCron =>
        Environment.GetEnvironmentVariable("FILE_CLEANUP_CRON")
        ?? _configuration.GetValue<string>("FileCleanupCron")
        ?? "0 * * * *"; // hourly at minute 0

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

    // Deployment-scoped HMAC secret for booking_conflict_event.subject_key.
    // Deliberately separate from ENCRYPTION_KEY (domain separation): rotating or
    // compromising one must not affect the other.
    public static string ConflictEventHmacKey =>
        Environment.GetEnvironmentVariable("CONFLICT_EVENT_HMAC_KEY")
        ?? _configuration.GetValue<string>("ConflictEventHmacKey");

    public static string FxSourceProvider =>
        Environment.GetEnvironmentVariable("FX_SOURCE_PROVIDER")
        ?? _configuration.GetValue<string>("FxSource:Provider")
        ?? "inforeuro";

    public static string FxSourceBaseUrl =>
        Environment.GetEnvironmentVariable("FX_SOURCE_BASE_URL")
        ?? _configuration.GetValue<string>("FxSource:BaseUrl")
        ?? "https://ec.europa.eu/budg/inforeuro/api/public/monthly-rates";

    public static string FxSourceBaseCurrency =>
        Environment.GetEnvironmentVariable("FX_SOURCE_BASE_CURRENCY")
        ?? _configuration.GetValue<string>("FxSource:BaseCurrency")
        ?? "EUR";

    public static string FxFetchCron =>
        Environment.GetEnvironmentVariable("FX_FETCH_CRON")
        ?? _configuration.GetValue<string>("FxSource:FetchCron")
        ?? "0 2 1 * *"; // 1st of each month at 02:00 UTC

    public static int FxBootstrapMonths =>
        int.Parse(
            Environment.GetEnvironmentVariable("FX_BOOTSTRAP_MONTHS")
                ?? _configuration.GetValue<string>("FxSource:BootstrapMonths")
                ?? "12"
        );

    public static string[] CorsAllowedOrigins =>
        (
            Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
            ?? _configuration.GetValue<string>("CorsAllowedOrigins")
            ?? ""
        ).Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

    public static string AuthProvider =>
        Environment.GetEnvironmentVariable("AUTH_PROVIDER")
        ?? _configuration.GetValue<string>("AuthProvider")
        ?? "local";

    public static bool IsB2C => string.Equals(AuthProvider, "b2c", StringComparison.OrdinalIgnoreCase);

    public static string B2cTenant =>
        Environment.GetEnvironmentVariable("B2C_TENANT")
        ?? _configuration.GetValue<string>("B2C:Tenant")
        ?? "";

    public static string B2cUserFlow =>
        Environment.GetEnvironmentVariable("B2C_USER_FLOW")
        ?? _configuration.GetValue<string>("B2C:UserFlow")
        ?? "";

    public static string B2cClientId =>
        Environment.GetEnvironmentVariable("B2C_CLIENT_ID")
        ?? _configuration.GetValue<string>("B2C:ClientId")
        ?? "";

    public static string B2cRedirectUri =>
        Environment.GetEnvironmentVariable("B2C_REDIRECT_URI")
        ?? _configuration.GetValue<string>("B2C:RedirectUri")
        ?? "";

    public static string B2cAuthority =>
        $"https://{B2cTenant}.b2clogin.com/{B2cTenant}.onmicrosoft.com/{B2cUserFlow}";

    public static string SendgridB2cInvitationEmailTemplateId =>
        Environment.GetEnvironmentVariable("SENDGRID_B2C_INVITATION_EMAIL_TEMPLATE_ID")
        ?? _configuration.GetValue<string>("SendgridB2cInvitationEmailTemplateId")
        ?? "";

    public static int RateLimitLoginInitPermitPerMinute =>
        int.Parse(
            Environment.GetEnvironmentVariable("RATE_LIMIT_LOGIN_INIT_PER_MINUTE")
                ?? _configuration.GetValue<string>("RateLimiting:LoginInitPerMinute")
                ?? "10"
        );

    public static int RateLimitAuthPermitPerMinute =>
        int.Parse(
            Environment.GetEnvironmentVariable("RATE_LIMIT_AUTH_PER_MINUTE")
                ?? _configuration.GetValue<string>("RateLimiting:AuthPerMinute")
                ?? "10"
        );

    public static int RateLimitGlobalPermitPerMinute =>
        int.Parse(
            Environment.GetEnvironmentVariable("RATE_LIMIT_GLOBAL_PER_MINUTE")
                ?? _configuration.GetValue<string>("RateLimiting:GlobalPerMinute")
                ?? "300"
        );

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

        if (CorsAllowedOrigins.Length == 0)
            errors.Add("CorsAllowedOrigins");

        if (errors.Count > 0)
            throw new Exception(string.Join(", ", errors));
    }
}