using System;
using Ccd.Server.Deduplication;
using Xunit;

namespace Ccd.Tests.Deduplication;

// All tests that touch the CONFLICT_EVENT_HMAC_KEY env var live in this class —
// xunit runs tests within a class sequentially, so the mutations cannot race.
public class ConflictEventHasherTests : IDisposable
{
    private const string KeyA = "ZGV2LW9ubHktY29uZmxpY3QtZXZlbnQtaG1hY3MtMzI=";
    private const string KeyB = "YW5vdGhlci1zZWNyZXQtaG1hYy1rZXktZm9yLXQtMzI=";

    private readonly string _originalKey;

    public ConflictEventHasherTests()
    {
        _originalKey = Environment.GetEnvironmentVariable("CONFLICT_EVENT_HMAC_KEY");
        Environment.SetEnvironmentVariable("CONFLICT_EVENT_HMAC_KEY", KeyA);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CONFLICT_EVENT_HMAC_KEY", _originalKey);
    }

    [Fact]
    public void SameInput_YieldsSameSubjectKey()
    {
        var first = ConflictEventHasher.ComputeSubjectKey("123456789");
        var second = ConflictEventHasher.ComputeSubjectKey("123456789");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length); // SHA-256 hex
    }

    [Fact]
    public void InputIsNormalizedBeforeHashing()
    {
        var trimmed = ConflictEventHasher.ComputeSubjectKey("123456789");
        var padded = ConflictEventHasher.ComputeSubjectKey("  123456789  ");

        Assert.Equal(trimmed, padded);
    }

    [Fact]
    public void DifferentInput_YieldsDifferentSubjectKey()
    {
        var first = ConflictEventHasher.ComputeSubjectKey("123456789");
        var second = ConflictEventHasher.ComputeSubjectKey("987654321");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void DifferentSecret_YieldsDifferentSubjectKey()
    {
        var withKeyA = ConflictEventHasher.ComputeSubjectKey("123456789");

        Environment.SetEnvironmentVariable("CONFLICT_EVENT_HMAC_KEY", KeyB);
        var withKeyB = ConflictEventHasher.ComputeSubjectKey("123456789");

        Assert.NotEqual(withKeyA, withKeyB);
    }

    [Fact]
    public void EmptyInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => ConflictEventHasher.ComputeSubjectKey(""));
        Assert.Throws<ArgumentException>(() => ConflictEventHasher.ComputeSubjectKey("   "));
        Assert.Throws<ArgumentException>(() => ConflictEventHasher.ComputeSubjectKey(null));
    }
}
