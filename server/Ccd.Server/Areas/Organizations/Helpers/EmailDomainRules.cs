using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ccd.Server.Organizations;

/// <summary>
/// Normalisation, validation and matching rules for organisation email domains.
///
/// - Normalised to lowercase ASCII; internationalised domains to punycode.
/// - Exact match only: "example.org" does not cover "team.example.org".
/// - Public email providers (gmail.com, outlook.com, …) cannot be added.
///   Organisations whose staff use them stay without a domain list.
/// </summary>
public static class EmailDomainRules
{
    private static readonly IdnMapping Idn = new() { AllowUnassigned = false, UseStd3AsciiRules = true };

    private static readonly Regex Label = new("^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.Compiled);
    private static readonly Regex Tld = new("^([a-z]{2,63}|xn--[a-z0-9-]{1,59})$", RegexOptions.Compiled);

    /// <summary>Public / consumer email providers that cannot be allowed for an organisation.</summary>
    public static readonly IReadOnlySet<string> PublicDomains = new HashSet<string>
    {
        "gmail.com", "googlemail.com",
        "outlook.com", "hotmail.com", "live.com", "msn.com", "passport.com",
        "hotmail.co.uk", "hotmail.fr", "hotmail.it", "hotmail.de", "hotmail.es",
        "outlook.fr", "outlook.de", "outlook.es", "outlook.it", "live.co.uk", "live.fr",
        "yahoo.com", "ymail.com", "rocketmail.com", "yahoo.co.uk", "yahoo.fr", "yahoo.de",
        "yahoo.es", "yahoo.it", "yahoo.ca", "yahoo.com.au",
        "icloud.com", "me.com", "mac.com",
        "aol.com", "aim.com",
        "proton.me", "protonmail.com", "pm.me",
        "gmx.com", "gmx.net", "gmx.de", "web.de",
        "mail.com", "email.com", "zoho.com", "zohomail.com",
        "yandex.com", "yandex.ru", "mail.ru", "inbox.ru", "list.ru", "bk.ru",
        "tutanota.com", "tuta.io", "fastmail.com", "hushmail.com",
        "qq.com", "163.com", "126.com", "sina.com",
        "rediffmail.com", "lycos.com", "mailinator.com",
    };

    /// <summary>
    /// Normalises and validates a domain entered by an Admin. On failure,
    /// <paramref name="error"/> holds a message suitable for the user.
    /// </summary>
    public static bool TryNormalize(string input, out string domain, out string error)
    {
        domain = null;
        error = null;

        var value = (input ?? "").Trim().Trim('.').ToLowerInvariant();
        if (value.StartsWith("@"))
            value = value[1..];

        if (string.IsNullOrEmpty(value))
        {
            error = "Enter a domain, for example example.org.";
            return false;
        }

        if (value.Contains('@') || value.Contains('/') || value.Contains(':') || value.Contains(' ') || value.Contains('*'))
        {
            error = "Enter only the domain part of the email address, for example example.org (no '@', spaces, wildcards or web addresses).";
            return false;
        }

        string ascii;
        try
        {
            ascii = Idn.GetAscii(value).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            error = $"'{input?.Trim()}' is not a valid domain.";
            return false;
        }

        var labels = ascii.Split('.');
        if (ascii.Length > 253 || labels.Length < 2 || !labels.All(Label.IsMatch) || !Tld.IsMatch(labels[^1]))
        {
            error = $"'{input?.Trim()}' is not a valid domain. Use a full domain such as example.org.";
            return false;
        }

        if (PublicDomains.Contains(ascii))
        {
            error = $"'{ascii}' is a public email provider and cannot be restricted to one organisation. "
                    + "Leave the domain list empty for organisations whose staff use public email addresses.";
            return false;
        }

        domain = ascii;
        return true;
    }

    /// <summary>Normalised domain part of an email address, or null if it has none.</summary>
    public static string DomainOfEmail(string email)
    {
        var value = (email ?? "").Trim();
        var at = value.LastIndexOf('@');
        if (at < 0 || at == value.Length - 1)
            return null;

        var part = value[(at + 1)..].Trim().Trim('.').ToLowerInvariant();
        try
        {
            return Idn.GetAscii(part).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return part;
        }
    }

    /// <summary>
    /// True when the organisation has no domain list (unrestricted) or the
    /// email's domain exactly matches one of the allowed domains.
    /// </summary>
    public static bool IsEmailAllowed(string email, IReadOnlyCollection<string> allowedDomains)
    {
        if (allowedDomains == null || allowedDomains.Count == 0)
            return true;

        var domain = DomainOfEmail(email);
        return domain != null && allowedDomains.Contains(domain);
    }
}
