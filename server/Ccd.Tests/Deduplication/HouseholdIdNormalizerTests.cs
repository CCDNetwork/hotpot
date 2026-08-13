using Ccd.Server.Deduplication;
using Xunit;

namespace Ccd.Tests.Deduplication;

public class HouseholdIdNormalizerTests
{
    [Theory]
    [InlineData("123456789", "123456789")]
    [InlineData("  123456789", "123456789")]
    [InlineData("123456789  ", "123456789")]
    [InlineData("\t123456789\n", "123456789")]
    public void Normalize_TrimsWhitespace(string input, string expected)
    {
        Assert.Equal(expected, HouseholdIdNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_PassesThroughNullAndWhitespace(string input)
    {
        Assert.Equal(input, HouseholdIdNormalizer.Normalize(input));
    }
}
