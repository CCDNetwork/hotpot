using System.Collections.Generic;
using Ccd.Server.Organizations;
using Xunit;

namespace Ccd.Tests.Organizations;

public class EmailDomainRulesTests
{
    [Theory]
    [InlineData("unicef.org", "unicef.org")]
    [InlineData("  UNICEF.org ", "unicef.org")]
    [InlineData("@savethechildren.org", "savethechildren.org")]
    [InlineData("example.org.", "example.org")]
    [InlineData("team.example.org", "team.example.org")]
    [InlineData("my-ngo.ps", "my-ngo.ps")]
    [InlineData("münchen.de", "xn--mnchen-3ya.de")]
    public void TryNormalize_AcceptsAndNormalises(string input, string expected)
    {
        Assert.True(EmailDomainRules.TryNormalize(input, out var domain, out var error), error);
        Assert.Equal(expected, domain);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("localhost")]
    [InlineData("john@example.org")]
    [InlineData("https://example.org")]
    [InlineData("*.example.org")]
    [InlineData("exa mple.org")]
    [InlineData("-example.org")]
    [InlineData("example.123")]
    public void TryNormalize_RejectsInvalid(string input)
    {
        Assert.False(EmailDomainRules.TryNormalize(input, out var domain, out var error));
        Assert.Null(domain);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("gmail.com")]
    [InlineData("GMAIL.COM")]
    [InlineData("outlook.com")]
    [InlineData("hotmail.com")]
    [InlineData("yahoo.com")]
    [InlineData("icloud.com")]
    [InlineData("proton.me")]
    public void TryNormalize_RejectsPublicProviders(string input)
    {
        Assert.False(EmailDomainRules.TryNormalize(input, out _, out var error));
        Assert.Contains("public email provider", error);
    }

    [Fact]
    public void IsEmailAllowed_UnrestrictedWhenNoDomains()
    {
        Assert.True(EmailDomainRules.IsEmailAllowed("someone@gmail.com", new List<string>()));
        Assert.True(EmailDomainRules.IsEmailAllowed("someone@gmail.com", null));
    }

    [Theory]
    [InlineData("a.person@unicef.org", true)]
    [InlineData("A.Person@UNICEF.ORG", true)]
    [InlineData(" a.person@unicef.org ", true)]
    [InlineData("a.person@savethechildren.org", true)]
    [InlineData("a.person@team.unicef.org", false)] // exact match: subdomains not covered
    [InlineData("a.person@unicef.org.evil.com", false)]
    [InlineData("a.person@gmail.com", false)]
    [InlineData("not-an-email", false)]
    public void IsEmailAllowed_ExactDomainMatch(string email, bool expected)
    {
        var allowed = new List<string> { "unicef.org", "savethechildren.org" };
        Assert.Equal(expected, EmailDomainRules.IsEmailAllowed(email, allowed));
    }
}
