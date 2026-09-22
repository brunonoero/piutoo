using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>L'orologio veloce serve a qualcosa?</b>
///
/// <para>Le prime cinque fasi di ogni sweep girano sull'orologio al timeframe della strategia, che
/// costa tredici volte meno di quello al minuto ma vede solo la chiusura di ogni barra: se dentro
/// una barra da quattro ore il prezzo ha colpito lo stop e poi e' risalito, li' lo stop non scatta.
/// <c>SweepRunnerParityTests.FastClockOverstatesTightStops</c> misura <i>quanto</i> quella
/// differenza vale sui valori assoluti, e la risposta e' "moltissimo".</para>
///
/// <para>Ma alla sweep i valori assoluti delle fasi veloci non servono: le servono per <b>ordinare</b>
/// le combinazioni e passare le migliori alla fase dopo, che poi rimisura al minuto. La domanda che
/// decide se quell'architettura regge e' quindi un'altra, e fino al 21/09/2026 nessuno l'aveva posta:
/// <b>l'ordinamento si conserva?</b></para>
///
/// <para>L'argomento a favore e' che dentro una fase veloce lo <b>stop e' fisso</b> — lo sceglie la
/// sesta fase — quindi tutte le combinazioni sbagliano nello stesso verso e la classifica
/// sopravvive. L'argomento contro e' che l'errore non dipende solo dallo stop ma da quanto una
/// configurazione capita dentro barre volatili, e questo cambia con canale, pattern e orari, cioe'
/// proprio con cio' che le fasi veloci scelgono.</para>
///
/// <para>Il test non asserisce una soglia: <b>misura</b> la correlazione di rango di Spearman fra i
/// due orologi su un campione di configurazioni vere e la stampa. Se e' alta, il percorso veloce fa
/// il suo lavoro e le prime cinque fasi hanno senso. Se e' bassa, quelle fasi stanno scegliendo
/// rumore e l'intera architettura a due orologi va rifatta — comprese le ricerche gia' girate.</para>
///
/// <para>Legge il feed vero: senza, si salta invece di fallire.</para>
/// </summary>
public sealed class SweepFastClockRankingTests(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const string StrategyId = "PT3B_FDAX_PCH_001_240";
    private const int Timeframe = 240;

    // Il campione di ricerca vero della sweep di stasera, cosi' la misura parla di quel run.
    private static readonly DateTime StartUtc = new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task FastClockRankingAgreesWithTheMinuteClock()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var series = await LoadAsync();
        if (series is null)
        {
            output.WriteLine("feed assente: saltato.");
            return;
        }

        var runner = new SweepRunner(series);

        // Lo stop resta al default della sweep (1500) in TUTTE le configurazioni, come accade
        // davvero nelle fasi veloci: e' la condizione in cui l'argomento "l'errore e' comune" ha
        // qualche speranza di reggere. Varia cio' che quelle fasi scelgono davvero.
        var configurazioni = BuildConfigurations();
        output.WriteLine($"{configurazioni.Count} configurazioni, stop fisso a 1500\n");

        // Le soglie della sweep del 21/09 (50 trade, 5 per tratto, nessun minimo di perdite): la
        // misura confronta gli orologi, e deve restare confrontabile con i numeri di quella sera —
        // i default del criterio sono stati alzati il 22/09 e qui non c'entrano.
        var objective = new WorstSubPeriodObjective(MinTrades: 50, MinTradesPerSubPeriod: 5, MinLosingTrades: 0);
        var misure = new List<(string Nome, decimal? Veloce, decimal? Minuto, decimal NetVeloce, decimal NetMinuto)>();

        foreach (var (nome, parametri) in configurazioni)
        {
            var fast = runner.Run(Job() with { Parameters = parametri });
            var accurate = runner.Run(Job() with { Parameters = parametri, ClockTimeframeMinutes = 1 });

            misure.Add((nome, objective.Score(fast), objective.Score(accurate), fast.NetProfit, accurate.NetProfit));
            output.WriteLine(
                $"{nome,-42} veloce {fast.Trades,4}t {fast.NetProfit,9:N0} (p {Fmt(objective.Score(fast))}) | " +
                $"minuto {accurate.Trades,4}t {accurate.NetProfit,9:N0} (p {Fmt(objective.Score(accurate))})");
        }

        // Solo le configurazioni ammissibili su ENTRAMBI gli orologi: dove il punteggio e' null la
        // configurazione non e' stata giudicata, e un rango inventato falserebbe la correlazione.
        var confrontabili = misure
            .Where(m => m.Veloce.HasValue && m.Minuto.HasValue)
            .ToList();

        output.WriteLine($"\nammissibili su entrambi: {confrontabili.Count} su {misure.Count}");
        if (confrontabili.Count < 5)
        {
            output.WriteLine("troppo poche per una correlazione: misura non conclusiva.");
            return;
        }

        var rhoPunteggio = Spearman(
            confrontabili.Select(m => m.Veloce!.Value).ToList(),
            confrontabili.Select(m => m.Minuto!.Value).ToList());
        var rhoNetto = Spearman(
            confrontabili.Select(m => m.NetVeloce).ToList(),
            confrontabili.Select(m => m.NetMinuto).ToList());

        output.WriteLine($"\nSpearman sul PUNTEGGIO dell'obiettivo: {rhoPunteggio:N3}");
        output.WriteLine($"Spearman sul NETTO:                    {rhoNetto:N3}");

        // Quante delle prime 3 del veloce stanno fra le prime 3 del minuto: e' la domanda operativa,
        // perche' il beam ne passa 2 e il resoconto ne tiene poche.
        var topVeloce = confrontabili.OrderByDescending(m => m.Veloce!.Value).Take(3).Select(m => m.Nome).ToHashSet();
        var topMinuto = confrontabili.OrderByDescending(m => m.Minuto!.Value).Take(3).Select(m => m.Nome).ToHashSet();
        output.WriteLine($"prime 3 in comune fra i due orologi: {topVeloce.Intersect(topMinuto).Count()} su 3");
        output.WriteLine($"  veloce: {string.Join(", ", topVeloce)}");
        output.WriteLine($"  minuto: {string.Join(", ", topMinuto)}");
    }

    // La stessa misura su NQ a 4 ore, con un orologio intermedio, viveva qui fino al 22/09/2026 e
    // girava su PT2_NQ_PCH_001_240. Quella classe e' stata rimossa insieme al resto della serie
    // PT2 e non ha un sostituto a 4 ore nel catalogo, quindi lo studio e' stato tolto invece di
    // essere ripuntato su una cella che non e' la sua. La risposta che aveva dato resta in
    // decisioni.md (22/09/2026): su NQ l'orologio a 15 minuti ordina come quello al minuto,
    // Spearman 0,980 e 2 prime 3 su 3, mentre su FDAX il veloce dava 0,021. Serviva a sapere se
    // ES, BP ed EC — che dal vendor hanno i 15 minuti ma non il minuto — fossero cercabili.
    private static string Fmt(decimal? value) => value.HasValue ? value.Value.ToString("N2") : "n/d";

    /// <summary>
    /// Un campione di configurazioni come quelle che le fasi veloci confrontano davvero: canale,
    /// offset, direzione, finestra oraria e pattern neutrali. Non e' la griglia completa — servono
    /// due run ciascuna — ma copre l'intervallo delle scelte vere.
    /// </summary>
    private static List<(string Nome, Dictionary<string, object> Parametri)> BuildConfigurations()
    {
        var lista = new List<(string, Dictionary<string, object>)>();

        foreach (var canale in new[] { 1, 15, 40, 100 })
        foreach (var direzione in new[] { 0, 1 })
            lista.Add(($"canale {canale,3}, dir {direzione}", new Dictionary<string, object>
            {
                ["ChannelBars"] = canale,
                ["Direction"] = direzione,
                ["StopLoss"] = 1500,
                ["TakeProfit"] = 3000
            }));

        foreach (var (start, end) in new[] { (-1, -1), (3, 18), (3, 10), (8, 20), (0, 6) })
            lista.Add(($"orari {start,3}-{end,3}", new Dictionary<string, object>
            {
                ["StartHour"] = start,
                ["EndHour"] = end,
                ["StopLoss"] = 1500,
                ["TakeProfit"] = 3000
            }));

        foreach (var neutrale in new[] { 55, 4, 11, 44, 20 })
            lista.Add(($"pattern neutrale {neutrale,2}", new Dictionary<string, object>
            {
                ["PtnNeutYes"] = neutrale,
                ["StopLoss"] = 1500,
                ["TakeProfit"] = 3000
            }));

        foreach (var offset in new[] { 0, 2, 5, 10 })
            lista.Add(($"offset {offset,2} tick", new Dictionary<string, object>
            {
                ["OffsetTicks"] = offset,
                ["StopLoss"] = 1500,
                ["TakeProfit"] = 3000
            }));

        return lista;
    }

    /// <summary>
    /// Correlazione di rango di Spearman, con i ranghi medi sui pari. 1 = stesso ordine, 0 = nessuna
    /// relazione, −1 = ordine rovesciato.
    /// </summary>
    private static double Spearman(List<decimal> a, List<decimal> b)
    {
        var ra = Ranks(a);
        var rb = Ranks(b);
        var n = ra.Count;
        var mediaA = ra.Average();
        var mediaB = rb.Average();

        double num = 0, denA = 0, denB = 0;
        for (var i = 0; i < n; i++)
        {
            var da = ra[i] - mediaA;
            var db = rb[i] - mediaB;
            num += da * db;
            denA += da * da;
            denB += db * db;
        }

        return denA == 0 || denB == 0 ? 0 : num / Math.Sqrt(denA * denB);
    }

    private static List<double> Ranks(List<decimal> values)
    {
        var ordinati = values
            .Select((v, i) => (Valore: v, Indice: i))
            .OrderBy(x => x.Valore)
            .ToList();

        var ranghi = new double[values.Count];
        var posizione = 0;
        while (posizione < ordinati.Count)
        {
            var fine = posizione;
            while (fine + 1 < ordinati.Count && ordinati[fine + 1].Valore == ordinati[posizione].Valore)
                fine++;

            var rangoMedio = (posizione + fine) / 2.0 + 1;
            for (var k = posizione; k <= fine; k++)
                ranghi[ordinati[k].Indice] = rangoMedio;

            posizione = fine + 1;
        }

        return ranghi.ToList();
    }

    private static SweepJob Job() => new(StrategyId)
    {
        InitialCapital = 1_000_000m,
        CommissionPerContract = 19.23m,
        Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
    };

    private static async Task<SweepSeries?> LoadAsync()
    {
        if (!Directory.Exists(Path.Combine(RepositoryPath, "datafeed-external", "ICS")))
            return null;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external"
        };
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        return await SweepSeries.LoadAsync(
            dataFeed, "@FDAX", [Timeframe, 1], StartUtc, EndUtc, warmupDays: 30d, broker: "ICS");
    }
}
