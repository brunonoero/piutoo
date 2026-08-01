using Piootoo.Shared.Configuration;

namespace Piootoo.Strategies.Tests;

public sealed class InstrumentRegistryTests
{
    [Theory]
    [InlineData("@BP", 62500, 0.0001)]
    [InlineData("@EC", 125000, 0.00005)]
    public void CmeFxContractsHaveVerifiedPointAndTickValues(
        string symbol,
        decimal expectedPointValue,
        decimal expectedTickSize)
    {
        var spec = InstrumentRegistry.Get(symbol);

        Assert.Equal(expectedPointValue, spec.PointValue);
        Assert.Equal(expectedTickSize, spec.TickSize);
        Assert.Equal("USD", spec.Currency);
    }
}
