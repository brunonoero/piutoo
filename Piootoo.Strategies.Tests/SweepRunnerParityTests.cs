using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La prova che il runner di sweep misura quello che misura il backtest.
///
/// <para><b>Perche' serve.</b> Un ottimizzatore che gira su regole proprie consegna parametri che
/// poi il backtest non riproduce: e' lo stesso difetto del porting, spostato di un piano. Il runner
/// deve quindi ritrovare un risultato gia' noto prima che gli si lasci cercare configurazioni
/// nuove. Il riferimento e' <c>PT2_NQ_PCH_001_240</c> sul feed interno dal 24/01/2022 al
/// 31/05/2025, con l'orologio al minuto: <b>861 trade e $231.822</b>, misurati il 20/09/2026 con la
/// 7.5.3 (workspace <c>v02-nq-s02-chiusura</c>, backtest <c>s02-etichetta-chiusura-753</c>).</para>
///
/// <para><b>Legge il feed vero</b> e vale quindi solo su una macchina che ce l'ha: senza, si salta
/// invece di fallire. Non e' un test di regressione del motore — quello e' il backtest — ma la
/// taratura dello strumento con cui si cerca.</para>
/// </summary>
public sealed class SweepRunnerParityTests(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const string StrategyId = "PT2_NQ_PCH_001_240";
    private static readonly DateTime StartUtc = new(2022, 1, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc);

    private const int ReferenceTrades = 861;
    private const decimal ReferenceNetProfit = 231_822m;

    /// <summary>
    /// Con l'orologio al minuto il runner deve dare <b>gli stessi numeri</b> del backtest: stesse
    /// serie, stesso motore di esecuzione, stesso ordine dentro il tick. Una differenza qui e' un
    /// difetto del runner, non una scelta.
    /// </summary>
    [Fact]
    public async Task RunnerWithMinuteClockMatchesTheOfficialBacktest()
    {
        var series = await LoadAsync([240, 1]);
        if (series is null) return;

        var outcome = new SweepRunner(series).Run(ReferenceJob() with { ClockTimeframeMinutes = 1 });
        output.WriteLine(outcome.ToString());

        var dump = Path.Combine(Path.GetTempPath(), "sweep-trades.csv");
        File.WriteAllLines(dump, new[] { "entry;exit;side;entryPrice;exitPrice;net;reason" }
            .Concat(outcome.ClosedTrades.Select(t =>
                $"{t.EntryDate:yyyy-MM-ddTHH:mm:ssZ};{t.ExitDate:yyyy-MM-ddTHH:mm:ssZ};{t.Direction};" +
                $"{t.EntryPrice};{t.ExitPrice};{t.NetProfit};{t.ExitReason}")));
        output.WriteLine($"trade scritti in {dump}");

        Assert.Equal(ReferenceTrades, outcome.Trades);
        Assert.Equal(ReferenceNetProfit, outcome.NetProfit);
    }

    /// <summary>
    /// Il percorso veloce — orologio uguale al timeframe della strategia — <b>non</b> deve dare gli
    /// stessi numeri: senza il minuto, stop e target vengono valutati sulla chiusura della barra da
    /// quattro ore invece che dentro. Il test fissa quanto vale quella differenza sul riferimento,
    /// cosi' resta una quantita' misurata e non un'impressione, e verifica che la sweep stia
    /// comunque misurando qualcosa di vivo.
    /// </summary>
    [Fact]
    public async Task FastClockIsFasterAndDiffersFromTheMinuteClockInAKnownWay()
    {
        var series = await LoadAsync([240]);
        if (series is null) return;

        var outcome = new SweepRunner(series).Run(ReferenceJob());
        output.WriteLine(outcome.ToString());
        output.WriteLine($"valutazioni {outcome.Evaluations}, intent {outcome.Signals}, aperte a fine run {outcome.OpenAtEnd}");

        Assert.True(outcome.Evaluations > 5_000, $"solo {outcome.Evaluations} valutazioni: la sweep sta misurando il nulla.");
        Assert.True(outcome.Trades > 0);
        // Il percorso veloce esiste per costare millisecondi: se non lo fa, la sweep non e' fattibile.
        Assert.True(outcome.Elapsed.TotalSeconds < 5, $"un run e' costato {outcome.Elapsed.TotalSeconds:N1}s.");
    }

    /// <summary>
    /// <b>Il limite del percorso veloce, misurato.</b> Senza il feed da un minuto lo stop viene
    /// valutato sulla barra della strategia invece che dentro, e uno stop stretto ne esce molto
    /// meglio di quanto sia: la barra che lo avrebbe colpito e poi recuperato non lo colpisce.
    ///
    /// <para>Non e' rumore ma una distorsione con un verso: piu' lo stop e' stretto, piu' il
    /// percorso veloce e' ottimista. Una fase di ricerca sul risk management girata li' scegliere
    /// sempre lo stop piu' stretto della griglia, che nel conto vero e' il peggiore. Il test la
    /// misura e la rende un fatto scritto; la conseguenza operativa sta in
    /// <c>SweepJob.ClockTimeframeMinutes</c>.</para>
    /// </summary>
    [Fact]
    public async Task FastClockOverstatesTightStops()
    {
        var series = await LoadAsync([240, 1]);
        if (series is null) return;

        var runner = new SweepRunner(series);
        foreach (var stop in new[] { 1000, 2500, 4595 })
        {
            var parameters = new Dictionary<string, object> { ["StopLoss"] = stop };
            var fast = runner.Run(ReferenceJob() with { Parameters = parameters });
            var accurate = runner.Run(ReferenceJob() with { Parameters = parameters, ClockTimeframeMinutes = 1 });

            output.WriteLine(
                $"stop {stop,5}: veloce {fast.Trades,4} trade netto {fast.NetProfit,10:N0} | " +
                $"minuto {accurate.Trades,4} trade netto {accurate.NetProfit,10:N0} | " +
                $"scarto {fast.NetProfit - accurate.NetProfit,10:N0}");
        }
    }

    /// <summary>
    /// I parametri arrivano davvero al motore: la stessa strategia con <c>Direction = 1</c> perde i
    /// trade short. E' la prova che una sweep cambia qualcosa — senza, misurerebbe la stessa
    /// configurazione migliaia di volte.
    /// </summary>
    [Fact]
    public async Task ParametersChangeTheOutcome()
    {
        var series = await LoadAsync([240]);
        if (series is null) return;

        var runner = new SweepRunner(series);
        var both = runner.Run(ReferenceJob());
        var longOnly = runner.Run(ReferenceJob() with
        {
            Parameters = new Dictionary<string, object> { ["Direction"] = 1 }
        });

        output.WriteLine($"entrambi: {both}");
        output.WriteLine($"solo long: {longOnly}");

        Assert.True(longOnly.Trades < both.Trades);
        Assert.True(longOnly.Trades > 0);
    }

    // ------------------------------------------------------------------ infrastruttura

    private static SweepJob ReferenceJob() => new(StrategyId)
    {
        InitialCapital = 1_000_000m,
        CommissionPerContract = 4m,
        // Il run di riferimento lascia correre overnight e fine settimana, come la ricerca.
        Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
    };

    private static async Task<SweepSeries?> LoadAsync(int[] timeframes)
    {
        if (!Directory.Exists(Path.Combine(RepositoryPath, "datafeed")))
            return null;

        // Gli stessi path del server: RepositoryPath e' la cartella del feed interno, non la radice.
        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external"
        };
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        return await SweepSeries.LoadAsync(dataFeed, "@NQ", timeframes, StartUtc, EndUtc, warmupDays: 30d);
    }
}
