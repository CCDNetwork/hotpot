using Ccd.Server.Dashboards;
using Xunit;

namespace Ccd.Tests.Dashboards;

public class InforEuroPayloadTests
{
    [Fact]
    public void Parse_RootArray_WithIsoA3CodeAndValue()
    {
        const string payload =
            "[{\"isoA3Code\":\"USD\",\"value\":1.09,\"country\":\"USA\"},"
            + "{\"isoA3Code\":\"ILS\",\"value\":4.05,\"country\":\"Israel\"}]";

        var rates = ExchangeRateFetchJob.ParseInforEuroPayload(payload);

        Assert.Equal(2, rates.Count);
        Assert.Equal(1.09m, rates["USD"]);
        Assert.Equal(4.05m, rates["ILS"]);
    }

    [Fact]
    public void Parse_ObjectWrappedArray_WithAlternateFieldNames()
    {
        const string payload =
            "{\"rates\":[{\"currencyIso\":\"JOD\",\"amount\":0.77}]}";

        var rates = ExchangeRateFetchJob.ParseInforEuroPayload(payload);

        Assert.Single(rates);
        Assert.Equal(0.77m, rates["JOD"]);
    }

    [Fact]
    public void Parse_StringAmounts_AreAccepted()
    {
        const string payload = "[{\"isoA3Code\":\"USD\",\"value\":\"1.0875\"}]";

        var rates = ExchangeRateFetchJob.ParseInforEuroPayload(payload);

        Assert.Equal(1.0875m, rates["USD"]);
    }

    [Fact]
    public void Parse_UnusableEntries_AreSkipped()
    {
        const string payload =
            "[{\"country\":\"Nowhere\"},{\"isoA3Code\":\"USD\"},{\"value\":2.0},"
            + "{\"isoA3Code\":\"ILS\",\"value\":4.05}]";

        var rates = ExchangeRateFetchJob.ParseInforEuroPayload(payload);

        Assert.Single(rates);
        Assert.Equal(4.05m, rates["ILS"]);
    }

    [Fact]
    public void Parse_NonArrayPayload_ReturnsEmpty()
    {
        Assert.Empty(ExchangeRateFetchJob.ParseInforEuroPayload("{\"error\":\"nope\"}"));
        Assert.Empty(ExchangeRateFetchJob.ParseInforEuroPayload("\"just a string\""));
    }
}
