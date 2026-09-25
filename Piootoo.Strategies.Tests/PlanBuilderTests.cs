using Piootoo.Core.Planning;
using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il costruttore di piani: P&amp;L giornaliero, le due correlazioni e la costruzione golosa. Le serie di
/// prova sono giorni sintetici con trade da un'uscita al giorno; ogni strategia e' scritta in modo che
/// il suo rapporto con le altre sia noto a priori.
/// </summary>
public sealed class PlanBuilderTests
{
    private static readonly DateTime Start = new(2025, 1, 6, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Correlazione 1 con se stessa traslata di scala, −1 con lo specchio, NaN con una serie ferma.</summary>
    [Fact]
    public void CorrelationSeesSameOppositeAndFlatSeries()
    {
        decimal[] a = [1, -2, 3, -1, 2, 5];
        decimal[] twice = a.Select(v => v * 2).ToArray();
        decimal[] mirror = a.Select(v => -v).ToArray();
        decimal[] flat = [0, 0, 0, 0, 0, 0];

        Assert.Equal(1.0, PlanBuilder.Correlation(a, twice), 6);
        Assert.Equal(-1.0, PlanBuilder.Correlation(a, mirror), 6);
        Assert.True(double.IsNaN(PlanBuilder.Correlation(a, flat)));
    }

    /// <summary>
    /// La correlazione nelle code vede cio' che la media nasconde: due serie scorrelate nei giorni normali
    /// che perdono insieme nei cinque giorni peggiori hanno una correlazione bassa e una coda alta.
    /// </summary>
    [Fact]
    public void TheTailCorrelationSeesJointLossesThatTheAverageHides()
    {
        var a = new decimal[100];
        var b = new decimal[100];
        for (var i = 0; i < 100; i++)
        {
            // Giorni normali: movimenti piccoli, di verso indipendente fra le due.
            a[i] = i % 2 == 0 ? 40 : -35;
            b[i] = i % 3 == 0 ? 50 : -25;
        }

        // Cinque crash comuni, di profondita' diverse.
        foreach (var (day, loss) in new[] { (10, -100m), (30, -150m), (50, -120m), (70, -200m), (90, -130m) })
        {
            a[day] = loss;
            b[day] = loss * 0.8m;
        }

        var overall = PlanBuilder.Correlation(a, b);
        var tail = PlanBuilder.TailCorrelation(a, b, 0.05);

        Assert.True(tail > 0.9, $"coda {tail:0.00}");
        Assert.True(tail > overall + 0.05, $"coda {tail:0.00}, totale {overall:0.00}");
    }

    /// <summary>
    /// I giorni in cui nessuna candidata opera non entrano nell'asse, e un giorno in cui una strategia non
    /// ha chiuso nulla vale zero per lei.
    /// </summary>
    [Fact]
    public void TheDayAxisHoldsOnlyDaysWithSomeTrade()
    {
        var trades = new List<PlanTrade>();
        trades.AddRange(Daily("A_X_AAA_001_60", "NQ", [10, 20], dayStep: 2));  // giorni 0 e 2
        trades.AddRange(Daily("B_X_BBB_001_60", "GC", [5], dayStep: 1, firstDay: 1)); // giorno 1

        var result = PlanBuilder.Build(trades, new PlanBuilderOptions { MinTrades = 1, MaxCorrelation = 1, MaxTailCorrelation = 1 });

        Assert.Equal(3, result.Days.Count);
        var a = result.Candidates.Single(c => c.Code == "A_X_AAA_001_60");
        Assert.Equal(new decimal[] { 10, 0, 20 }, a.Daily);
    }

    /// <summary>
    /// La costruzione golosa: il seme e' la migliore, la gemella correlata resta fuori dal suo piano, la
    /// strategia speculare entra. La gemella finisce nel secondo piano.
    /// </summary>
    [Fact]
    public void AGreedyPlanKeepsTheCorrelatedTwinOutAndTakesTheMirror()
    {
        var pattern = Pattern(60);
        var trades = new List<PlanTrade>();
        trades.AddRange(Daily("S_NQ_AAA_001_60", "NQ", pattern.Select(v => v * 2 + 3).ToArray()));
        trades.AddRange(Daily("S_ES_BBB_001_60", "ES", pattern.Select(v => v * 2 + 2).ToArray()));    // gemella
        trades.AddRange(Daily("S_GC_CCC_001_60", "GC", pattern.Select(v => -v + 2).ToArray()));       // specchio

        var result = PlanBuilder.Build(trades, new PlanBuilderOptions { MinTrades = 10, Plans = 2 });

        Assert.Equal(2, result.Plans.Count);
        var first = result.Plans[0].Members.Select(m => m.Code).ToList();
        Assert.Contains("S_NQ_AAA_001_60", first);
        Assert.Contains("S_GC_CCC_001_60", first);
        Assert.DoesNotContain("S_ES_BBB_001_60", first);
        Assert.Equal(new[] { "S_ES_BBB_001_60" }, result.Plans[1].Members.Select(m => m.Code));
    }

    /// <summary>I tetti per simbolo e per famiglia si rispettano anche fra strategie scorrelate.</summary>
    [Fact]
    public void SymbolAndFamilyCapsHold()
    {
        var trades = new List<PlanTrade>();
        for (var k = 0; k < 4; k++)
            trades.AddRange(Daily($"S_NQ_F{k}X_001_60", "NQ", Independent(60, k)));
        for (var k = 0; k < 4; k++)
            trades.AddRange(Daily($"S_G{k}_FAM_001_60", $"G{k}", Independent(60, 10 + k)));

        var result = PlanBuilder.Build(trades, new PlanBuilderOptions
        {
            MinTrades = 10, Plans = 1, MaxStrategiesPerPlan = 8, MaxPerSymbol = 2, MaxPerFamily = 2,
            MaxCorrelation = 1, MaxTailCorrelation = 1
        });

        var members = result.Plans.Single().Members;
        Assert.True(members.Count(m => m.Symbol == "NQ") <= 2);
        Assert.True(members.Count(m => m.Family == "FAM") <= 2);
        Assert.Equal(4, members.Count);
    }

    /// <summary>La perdita giornaliera del conto e' un vincolo del piano: una candidata che la sforerebbe non entra.</summary>
    [Fact]
    public void TheDailyLossLimitKeepsAStrategyOut()
    {
        var trades = new List<PlanTrade>();
        trades.AddRange(Daily("S_NQ_AAA_001_60", "NQ", Enumerable.Repeat(10m, 40).ToArray()));
        var crash = Enumerable.Repeat(12m, 40).ToArray();
        crash[20] = -300m;
        trades.AddRange(Daily("S_GC_BBB_001_60", "GC", crash));

        var result = PlanBuilder.Build(trades, new PlanBuilderOptions
        {
            MinTrades = 10, Plans = 1, MaxDailyLoss = 200m, MaxCorrelation = 1, MaxTailCorrelation = 1
        });

        Assert.Equal(new[] { "S_NQ_AAA_001_60" }, result.Plans.Single().Members.Select(m => m.Code));
        Assert.Contains("S_GC_BBB_001_60", result.Unassigned);
    }

    /// <summary>Pochi trade o netto non positivo: la strategia non e' una candidata, e il resoconto dice perche'.</summary>
    [Fact]
    public void FewTradesOrNoProfitAreExcludedWithAReason()
    {
        var trades = new List<PlanTrade>();
        trades.AddRange(Daily("S_NQ_AAA_001_60", "NQ", [5, 5]));
        trades.AddRange(Daily("S_GC_BBB_001_60", "GC", Enumerable.Repeat(-1m, 40).ToArray()));

        var result = PlanBuilder.Build(trades, new PlanBuilderOptions { MinTrades = 10 });

        Assert.Empty(result.Candidates);
        Assert.Contains(result.Excluded, e => e.Code == "S_NQ_AAA_001_60" && e.Reason.Contains("trade"));
        Assert.Contains(result.Excluded, e => e.Code == "S_GC_BBB_001_60" && e.Reason.Contains("utile"));
    }

    /// <summary>La famiglia e' la sigla del motore nel nome di catalogo; un nome fuori convenzione e' una famiglia a se'.</summary>
    [Fact]
    public void TheFamilyIsTheEngineCode()
    {
        Assert.Equal("FBO", PlanBuilder.Family("PT6EXO_NQ_FBO_001_60"));
        Assert.Equal("PCH", PlanBuilder.Family("PT3B_FDAX_PCH_002_240"));
        Assert.Equal("strana", PlanBuilder.Family("strana"));
    }

    /// <summary>
    /// Un run si legge dallo store, che fonde il journal di un run interrotto invece di leggere un array a
    /// meta'; il resoconto e i file escono tutti.
    /// </summary>
    [Fact]
    public void ARunIsReadThroughTheStoreAndTheReportIsWritten()
    {
        var root = Path.Combine(Path.GetTempPath(), $"piootoo-plan-{Guid.NewGuid():N}");
        try
        {
            var store = new TradingJsonStore(Path.Combine(root, "run"));
            store.WriteTrades(Persisted("S_NQ_AAA_001_60", "NQ", 0, 20));
            store.AppendTrades(Persisted("S_GC_BBB_001_60", "GC", 1000, 20));

            var trades = PlanBuilder.ReadRun(Path.Combine(root, "run"));
            Assert.Equal(40, trades.Count);

            var options = new PlanBuilderOptions { MinTrades = 10, MaxCorrelation = 1, MaxTailCorrelation = 1 };
            var result = PlanBuilder.Build(trades, options);
            PlanBuilderReport.WriteAll(result, options, ["run"], Path.Combine(root, "out"));

            foreach (var file in new[] { "plan-builder.md", "plans.json", "correlazioni.csv", "correlazioni-code.csv" })
                Assert.True(File.Exists(Path.Combine(root, "out", file)), file);
            Assert.Contains("### Piano 1", File.ReadAllText(Path.Combine(root, "out", "plan-builder.md")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    // ------------------------------------------------------------------ supporto

    private static IEnumerable<PlanTrade> Daily(string code, string symbol, decimal[] values, int dayStep = 1, int firstDay = 0) =>
        values.Select((value, index) => new PlanTrade(code, symbol, Start.AddDays(firstDay + index * dayStep).AddHours(15), value));

    /// <summary>Una serie pseudo-casuale deterministica, di media nulla.</summary>
    private static decimal[] Pattern(int days) =>
        Enumerable.Range(0, days).Select(i => (decimal)((i * 37 % 11) - 5)).ToArray();

    /// <summary>Serie diverse per seme, in utile, poco correlate fra loro.</summary>
    private static decimal[] Independent(int days, int seed) =>
        Enumerable.Range(0, days).Select(i => (decimal)(((i + seed * 7) * (31 + seed) % 13) - 5) + 1m).ToArray();

    private static IEnumerable<PersistedTrade> Persisted(string code, string symbol, int idOffset, int count) =>
        Enumerable.Range(0, count).Select(i => new PersistedTrade
        {
            TradeId = $"t{idOffset + i}",
            StrategyCode = code,
            StrategyName = code,
            Symbol = symbol,
            Direction = SignalType.Buy,
            Quantity = 1,
            EntryTimeUtc = Start.AddDays(i),
            ExitTimeUtc = Start.AddDays(i).AddHours(4),
            NetProfit = i % 3 == 0 ? -5m : 10m
        });
}
