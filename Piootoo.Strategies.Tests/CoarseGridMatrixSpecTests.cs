using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le celle della matrice sul feed del vendor (<c>MOTORE|@SIMBOLO|TIMEFRAME|VENDOR</c>): 14 anni di
/// campione invece di tre, costi FTMO e un CSV che non sovrascrive quello della cella FTMO.
/// </summary>
public sealed class CoarseGridMatrixSpecTests
{
    [Fact]
    public void AVendorCellSearchesFourteenYearsWithTheBrokerCosts()
    {
        var spec = CoarseGridMatrixTests.Spec("REG|@NQ|240|VENDOR");

        Assert.Equal(CoarseGridStudy.VendorFeed, spec.FeedBroker);
        Assert.Equal(["FTMO"], spec.SpreadBrokers);
        Assert.Equal(["FTMO"], spec.SwapBrokers);
        Assert.Equal(new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc), spec.StartUtc);
        Assert.Equal(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), spec.SplitUtc);
        Assert.Equal(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), spec.EndUtc);
        Assert.Equal(250, spec.MinInSampleTrades);
        Assert.Equal(Path.Combine("matrice", "nq-240-reg-vendor.csv"), spec.CsvName);
    }

    [Fact]
    public void ABrokerCellKeepsTheFtmoSplit()
    {
        var spec = CoarseGridMatrixTests.Spec("REG|@NQ|240|FTMO");

        Assert.Equal("FTMO", spec.FeedBroker);
        Assert.Equal(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), spec.StartUtc);
        Assert.Equal(190, spec.MinInSampleTrades);
        Assert.Equal(Path.Combine("matrice", "nq-240-reg.csv"), spec.CsvName);
    }

    /// <summary>CAL rifiuta un'ora di uscita e ha la tenuta propria: la cella non le permuta.</summary>
    [Fact]
    public void TheCalendarCellHasItsOwnExitsAndHolding()
    {
        var spec = CoarseGridMatrixTests.Spec("CAL|@FDAX|60|VENDOR");

        Assert.Equal([-1], spec.ExitHours);
        Assert.Equal([0], spec.HoldDays);
        Assert.Equal([1, 2], spec.Directions);
    }
}
