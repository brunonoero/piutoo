using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La validazione fuori campione: il passo che separa una configurazione che ha vinto una gara da
/// una strategia.
/// </summary>
public sealed class SweepValidationTests(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private static readonly DateTime StartUtc = new(2022, 1, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SplitUtc = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// <c>Between</c> non copia le barre e non apre la porta al look-ahead: la finestra limita
    /// l'orologio del run, e il cursore continua a restituire solo le barre fino all'istante
    /// corrente. Si verifica sui trade, che devono cadere tutti dentro la finestra.
    /// </summary>
    [Fact]
    public void BetweenRestrictsTheRunWithoutLookAhead()
    {
        var series = LoadSeries();
        if (series is null) return;

        var window = series.Between(SplitUtc, EndUtc);
        var outcome = new SweepRunner(window).Run(Template());

        Assert.NotEmpty(outcome.ClosedTrades);
        Assert.All(outcome.ClosedTrades, trade =>
        {
            Assert.True(trade.EntryDate >= SplitUtc, $"ingresso {trade.EntryDate:u} prima della finestra.");
            Assert.True(trade.EntryDate <= EndUtc, $"ingresso {trade.EntryDate:u} dopo la finestra.");
        });

        // Il periodo intero contiene piu' trade della sua seconda meta': se cosi' non fosse, la
        // finestra non starebbe restringendo niente.
        var whole = new SweepRunner(series).Run(Template());
        output.WriteLine($"intero {whole.Trades} trade, finestra {outcome.Trades}");
        Assert.True(outcome.Trades < whole.Trades);
    }

    /// <summary>
    /// La meccanica del verdetto su una configurazione vera: la PT2 come e' dichiarata, cercata in
    /// campione fino al 2024 e validata sul resto. Non si asserisce che passi — non e' compito di un
    /// test decidere se una strategia e' buona — ma che il verdetto sia coerente con cio' che e'
    /// stato misurato.
    /// </summary>
    [Fact]
    public void ValidationJudgesTheDeclaredConfiguration()
    {
        // La validazione gira sempre sull'orologio fitto: il minuto va caricato.
        var series = LoadSeries([240, 1]);
        if (series is null) return;

        var validator = new SweepValidator(
            series.Between(StartUtc, SplitUtc),
            series.Between(SplitUtc, EndUtc),
            new NetOverDrawdownObjective(MinTrades: 50),
            new SweepValidationOptions { StabilityWindows = 4, MinProfitableWindows = 3 });

        var validation = validator.Validate(Template(), new Dictionary<string, object>());

        output.WriteLine(validation.ToString());
        foreach (var window in validation.StabilityWindows)
            output.WriteLine($"  {window.FromUtc:yyyy-MM-dd} → {window.ToUtc:yyyy-MM-dd}: {window.Trades} trade, {window.NetProfit:N0}");

        Assert.Equal(4, validation.StabilityWindows.Count);
        Assert.NotEmpty(validation.Verdict);

        // Il verdetto deve essere quello che i criteri impongono, non un'opinione: se passa, ogni
        // soglia e' rispettata; se non passa, almeno una e' violata.
        if (validation.Passed)
        {
            Assert.True(validation.OutOfSample.Trades >= 20);
            Assert.True(validation.ScoreRetention is null or >= 0.5m);
            Assert.True(validation.ProfitableWindows >= 3);
        }
        else
        {
            Assert.True(
                validation.OutOfSampleScore is null or <= 0m ||
                validation.InSampleScore is null or <= 0m ||
                validation.OutOfSample.Trades < 20 ||
                validation.ScoreRetention < 0.5m ||
                validation.ProfitableWindows < 3 ||
                validation.Years is { Passes: false } ||
                validation.BestTradeShareInSample > ResearchCriteria.MaxBestTradeShare ||
                validation.BestTradeShareOutOfSample > ResearchCriteria.MaxBestTradeShare);
        }
    }

    /// <summary>
    /// Il validatore <b>discrimina</b>: una configurazione che non regge viene scartata con un
    /// verdetto che dice perche', e una che regge passa. Sono due asserzioni nello stesso test di
    /// proposito — un validatore che scarta tutto passerebbe la prima e fallirebbe la seconda, ed e'
    /// il modo tipico in cui una validazione smette di validare senza che nessuno se ne accorga.
    ///
    /// <para><b>La configurazione scartata cade in campione, non fuori</b>, ed e' un cambiamento
    /// rispetto a prima. Fino al 22/09/2026 il test girava su <c>PT2_NQ_PCH_001_240</c> e costruiva
    /// il caso opposto: bene dentro, male fuori. Su <c>@FDAX</c> in questo periodo quel caso non
    /// esiste — misurato su sei stop da 100 a 5000, il fuori campione e' sempre migliore del
    /// campione, con tenute fra il 119% e il 5.322% — quindi asserirlo avrebbe richiesto di
    /// scegliere un periodo apposta, cioe' di costruire il risultato invece di misurarlo. Lo stop a
    /// 500 e' l'unico dei sei che cade, e cade perche' in campione perde 7.911.</para>
    /// </summary>
    [Fact]
    public void TheValidatorRejectsWhatDoesNotHoldAndPassesWhatDoes()
    {
        var series = LoadSeries([240, 1]);
        if (series is null) return;

        var validator = new SweepValidator(
            series.Between(StartUtc, SplitUtc),
            series.Between(SplitUtc, EndUtc),
            // Soglie severe: quello che non tiene deve cadere, ed e' il punto della validazione.
            new NetOverDrawdownObjective(MinTrades: 50),
            new SweepValidationOptions
            {
                MinScoreRetention = 0.8m,
                // Due finestre bastano a questo test e costano due run al minuto invece di quattro.
                StabilityWindows = 2,
                MinProfitableWindows = 2,
                // Il test riguarda tenuta e finestre; i cancelli della ricerca hanno i propri test.
                ResearchGates = false
            });

        var scartata = validator.Validate(Template(),
            new Dictionary<string, object> { ["StopLoss"] = 500, ["MaxBars"] = 0 });
        output.WriteLine($"scartata: {scartata}");
        Assert.False(scartata.Passed);
        Assert.NotEmpty(scartata.Verdict);

        var promossa = validator.Validate(Template(),
            new Dictionary<string, object> { ["StopLoss"] = 2000, ["MaxBars"] = 0 });
        output.WriteLine($"promossa: {promossa}");
        Assert.True(promossa.Passed, $"doveva passare: {promossa}");
    }

    /// <summary>
    /// Il confine fra campione e validazione deve cadere dentro il periodo: fuori, la ricerca
    /// girerebbe su un periodo vuoto senza dirlo.
    /// </summary>
    [Fact]
    public void ASplitOutsideThePeriodFailsUpFront()
    {
        var series = LoadSeries();
        if (series is null) return;

        var error = Assert.Throws<ArgumentException>(() => SweepSearch.Run(
            series, EndUtc.AddYears(1), SweepSpaces.PriceChannel(240), Template()));

        Assert.Contains("campione", error.Message);
    }

    /// <summary>
    /// Ricerca e validazione in un passaggio, su uno spazio ridotto: il resoconto deve portare le
    /// fasi, le finaliste e il verdetto di ognuna.
    /// </summary>
    [Fact]
    public void SearchOptimizesInSampleAndValidatesOutOfSample()
    {
        var series = LoadSeries([240, 1]);
        if (series is null) return;

        var space = new SweepSpace(
            "PC-ridotto",
            [
                new SweepParameter("ChannelBars", [1, 10, 20]),
                new SweepParameter("StopLoss", [4595, 2500, 3500], OffSentinel: 0)
            ],
            [
                new SweepPhase("trigger", ["ChannelBars"]),
                new SweepPhase("rischio", ["StopLoss"], RequiresAccurateClock: true)
            ]);

        var result = SweepSearch.Run(
            series, SplitUtc, space, Template(),
            new NetOverDrawdownObjective(MinTrades: 30),
            new SweepOptimizerOptions { Verbose = false, BeamWidth = 2, PlateauSmoothing = false },
            new SweepValidationOptions { StabilityWindows = 3, MinProfitableWindows = 2 },
            topCandidates: 3);

        output.WriteLine(result.Describe());

        Assert.Equal(2, result.Optimization.Phases.Count);
        Assert.NotEmpty(result.Validations);
        // Ogni finalista porta con se' entrambe le misure: un risultato in campione senza il suo
        // numero fuori campione accanto e' una cifra che invita a sbagliare.
        Assert.All(result.Validations, validation =>
        {
            Assert.NotNull(validation.InSample);
            Assert.NotNull(validation.OutOfSample);
            Assert.NotEmpty(validation.Verdict);
        });
    }

    // ------------------------------------------------------------------ infrastruttura

    // Su @FDAX dal 22/09/2026: la classe di prima era PT2_NQ_PCH_001_240, rimossa con il resto
    // della serie PT2. Qui serve una Price Channel a 4 ore che legga tutte le chiavi della sweep, e
    // quale simbolo sia non cambia cosa il test misura.
    private static SweepJob Template() => new("PT3B_FDAX_PCH_001_240")
    {
        InitialCapital = 1_000_000m,
        CommissionPerContract = 4m,
        Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
    };

    private static SweepSeries? LoadSeries(int[]? timeframes = null)
    {
        if (!Directory.Exists(Path.Combine(RepositoryPath, "datafeed")))
            return null;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external"
        };

        return SweepSeries
            .LoadAsync(new PiootooDataFeedService(new DatafeedCatalog(settings)), "@FDAX",
                timeframes ?? [240], StartUtc, EndUtc)
            .GetAwaiter().GetResult();
    }
}
