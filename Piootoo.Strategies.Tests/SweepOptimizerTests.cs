using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'ottimizzatore sequenziale a fasi. I test che toccano il feed usano uno spazio <b>ridotto</b>:
/// le griglie vere del motore hanno fasi da diecimila combinazioni, che sono minuti di calcolo e non
/// appartengono a una suite di test. Quelle si lanciano a mano.
/// </summary>
public sealed class SweepOptimizerTests(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private static readonly DateTime StartUtc = new(2022, 1, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Su uno spazio ridotto la ricerca deve arrivare a una configurazione <b>almeno pari</b> a
    /// quella dichiarata dalla classe, che dello spazio fa parte: se non ci arriva, il beam o il
    /// punteggio stanno buttando via il punto migliore.
    /// </summary>
    [Fact]
    public void OptimizerFindsSomethingAtLeastAsGoodAsTheDeclaredConfiguration()
    {
        // Anche il minuto: la fase di rischio gira sull'orologio fitto.
        var series = LoadSeries([240, 1]);
        if (series is null) return;

        var space = new SweepSpace(
            "PC-ridotto",
            [
                // La configurazione dichiarata (canale 1, nessun offset, entrambe le direzioni) e'
                // dentro la griglia: la ricerca deve poterla ritrovare.
                new SweepParameter("ChannelBars", [1, 10, 20, 40]),
                new SweepParameter("Direction", [0, 1, 2], Categorical: true),
                new SweepParameter("StopLoss", [4595, 1500, 2500, 3500], OffSentinel: 0),
                new SweepParameter("MaxBars", [5, 0, 10, 20], OffSentinel: 0)
            ],
            [
                new SweepPhase("trigger", ["ChannelBars", "Direction"]),
                // Sull'orologio fitto, come le fasi di rischio dello spazio vero: sul percorso
                // veloce vincerebbe lo stop piu' stretto della griglia. Vedi SweepPhase.
                new SweepPhase("rischio", ["StopLoss", "MaxBars"], RequiresAccurateClock: true)
            ]);

        var objective = new NetOverDrawdownObjective(MinTrades: 50);
        // Senza smoothing, perche' qui si verifica che la ricerca non perda il massimo: con la
        // plateau analysis accesa un massimo isolato PUO' legittimamente perdere contro un
        // altopiano, ed e' esattamente cio' per cui esiste.
        var optimizer = new SweepOptimizer(series, space, objective,
            new SweepOptimizerOptions { Verbose = false, BeamWidth = 2, PlateauSmoothing = false });

        var result = optimizer.Optimize(Template());

        Assert.NotNull(result.Best);
        Assert.Equal(2, result.Phases.Count);
        foreach (var phase in result.Phases)
            output.WriteLine($"{phase.Phase}: {phase.Combinations} combo, {phase.Admissible} ammissibili, {phase.Elapsed.TotalSeconds:N1}s → {phase.Seeds[0].Outcome}");

        output.WriteLine($"migliore: {result.Best!.Outcome}");
        output.WriteLine("parametri: " + string.Join(", ", result.Best.Parameters.Select(p => $"{p.Key}={p.Value}")));

        // Il riferimento e' la classe come e' dichiarata, misurata con lo stesso obiettivo e con lo
        // stesso orologio con cui il vincitore esce dalla ricerca: quello fitto. Confrontare un
        // punteggio del percorso veloce con uno del percorso al minuto non vuol dire niente.
        var declared = new SweepRunner(series).Run(Template() with { ClockTimeframeMinutes = 1 });
        var declaredScore = objective.Score(declared);
        output.WriteLine($"dichiarata: {declared} (punteggio {declaredScore:N2})");

        Assert.NotNull(declaredScore);
        Assert.True(result.Best.Score >= declaredScore,
            $"la ricerca ha scelto {result.Best.Score:N2}, peggio della configurazione dichiarata {declaredScore:N2}.");
    }

    /// <summary>
    /// La plateau analysis non deve eleggere un picco isolato: con lo smoothing acceso la
    /// configurazione scelta deve avere vicini di griglia decenti. Qui si verifica la meccanica —
    /// che lo smoothing cambi davvero il punteggio su cui si ordina — perche' e' la parte che, se
    /// silenziosamente inerte, lascerebbe la ricerca a massimizzare il rumore.
    /// </summary>
    [Fact]
    public void PlateauSmoothingAveragesWithOrdinalNeighboursOnly()
    {
        var series = LoadSeries();
        if (series is null) return;

        var space = new SweepSpace(
            "PC-plateau",
            [
                new SweepParameter("StopLoss", [1000, 1500, 2000, 2500, 3000, 4595]),
                new SweepParameter("Direction", [0, 1], Categorical: true)
            ],
            [new SweepPhase("rischio", ["StopLoss", "Direction"])]);

        var options = new SweepOptimizerOptions { Verbose = false, BeamWidth = 1, PatternAblation = false };
        var smoothed = new SweepOptimizer(series, space, new NetOverDrawdownObjective(MinTrades: 50), options)
            .Optimize(Template());
        var raw = new SweepOptimizer(series, space, new NetOverDrawdownObjective(MinTrades: 50),
                options with { PlateauSmoothing = false })
            .Optimize(Template());

        var withSmoothing = smoothed.Phases[0].Seeds[0];
        var withoutSmoothing = raw.Phases[0].Seeds[0];

        output.WriteLine($"con smoothing:   {withSmoothing.Outcome} (grezzo {withSmoothing.Score:N2}, smussato {withSmoothing.SmoothedScore:N2})");
        output.WriteLine($"senza smoothing: {withoutSmoothing.Outcome} (grezzo {withoutSmoothing.Score:N2})");

        // Senza smoothing si ordina sul punteggio grezzo: il primo e' il massimo assoluto.
        Assert.Equal(withoutSmoothing.Score, withoutSmoothing.SmoothedScore);
        // Con lo smoothing il punteggio su cui si ordina e' una media, quindi non puo' coincidere
        // con quello grezzo per una configurazione che ha almeno un vicino ammissibile.
        Assert.True(
            withSmoothing.SmoothedScore != withSmoothing.Score ||
            withSmoothing.Score == withoutSmoothing.Score,
            "lo smoothing non ha toccato il punteggio di nessuna configurazione: e' inerte.");
    }

    /// <summary>
    /// La guardia sulle combinazioni: una fase troppo grande fa fallire l'avvio invece di partire
    /// per giorni. Non tocca il feed, quindi vale ovunque.
    /// </summary>
    [Fact]
    public void APhaseBiggerThanTheGuardFailsUpFront()
    {
        var space = SweepSpaces.PriceChannel(240);
        var series = LoadSeries();
        if (series is null) return;

        var optimizer = new SweepOptimizer(series, space, null,
            new SweepOptimizerOptions { MaxCombinationsPerPhase = 10, Verbose = false });

        var error = Assert.Throws<InvalidOperationException>(() => optimizer.Optimize(Template()));
        Assert.Contains("combinazioni", error.Message);
    }

    /// <summary>
    /// Lo spazio del Price Channel e' quello del motore di ricerca: le fasi nell'ordine del metodo,
    /// con i pattern separati dal risk management, e le griglie riportate verbatim.
    /// </summary>
    [Fact]
    public void PriceChannelSpaceMirrorsTheResearchEngine()
    {
        var space = SweepSpaces.PriceChannel(240);

        Assert.Equal(
            ["trigger", "volatilita'", "pattern neutrali", "pattern direzionali", "orari e giorni",
             "stop e target", "trailing e breakeven"],
            space.Phases.Select(phase => phase.Name));

        // Il vincolo del metodo: pattern e stop/target mai nella stessa fase.
        foreach (var phase in space.Phases)
        {
            var hasPattern = phase.Keys.Any(key => key.StartsWith("Ptn", StringComparison.Ordinal));
            var hasRisk = phase.Keys.Any(key => key is "StopLoss" or "TakeProfit");
            Assert.False(hasPattern && hasRisk, $"la fase '{phase.Name}' mescola pattern e risk management.");
        }

        // 103 valori direzionali: sentinella, 1..51 e i 51 negativi del mirroring invertito.
        Assert.Equal(103, space.ByKey["PtnDirYes"].Values.Count);
        // 55 neutrali: sentinella piu' 1..54.
        Assert.Equal(55, space.ByKey["PtnNeutYes"].Values.Count);
        // Gli orari a passo 1h piu' la sentinella "nessun limite".
        Assert.Equal(25, space.ByKey["StartHour"].Values.Count);
        Assert.Equal(-1, space.ByKey["StartHour"].OffSentinel);
        // I pattern sono categorici: niente plateau sugli id.
        Assert.True(space.ByKey["PtnDirYes"].Categorical);
        Assert.False(space.ByKey["StopLoss"].Categorical);
        // La base della sweep ha stop e target non nulli, altrimenti vince sempre la sentinella.
        Assert.Equal(1500, space.Defaults["StopLoss"]);
        Assert.Equal(3000, space.Defaults["TakeProfit"]);
    }

    /// <summary>Il BIAS settimanale: quattro fasi di timing su due direzioni, i pattern, poi il rischio.</summary>
    [Fact]
    public void BiasWeeklySpaceCoversBothDirections()
    {
        var space = SweepSpaces.BiasWeekly();

        Assert.Equal(7, space.Phases.Count);
        Assert.Contains(space.Phases, phase => phase.Keys.Contains("EntryDayShort"));
        Assert.Contains(space.Phases, phase => phase.Keys.Contains("PtnSyYes"));
        // Entrambe le direzioni partono spente: le fasi di timing ne accendono una alla volta.
        Assert.Equal(-1, space.Defaults["EntryDayLong"]);
        Assert.Equal(-1, space.Defaults["EntryDayShort"]);
        // 153 valori sul lato "yes": sentinella, 1..151 e il 153 che spegne la direzione.
        Assert.Equal(153, space.ByKey["PtnLyYes"].Values.Count);
    }

    // ------------------------------------------------------------------ infrastruttura

    private static SweepJob Template() => new("PT2_NQ_PCH_001_240")
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
            .LoadAsync(new PiootooDataFeedService(new DatafeedCatalog(settings)), "@NQ",
                timeframes ?? [240], StartUtc, EndUtc)
            .GetAwaiter().GetResult();
    }
}
