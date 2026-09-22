using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>La 002 sul periodo lungo: il test che decide se fidarsene.</b>
///
/// <para><b>Perche'.</b> La griglia grossa su FDAX 4 ore sul 2014-2026
/// (<c>ricerca/fdax-4h-griglia-grossa-lunga.md</c>) ha mostrato che i parametri <i>strutturali</i>
/// della 002 — canale 1, entrambe le direzioni — stanno nella regione peggiore dello spazio: la
/// cella corrispondente col motore nudo fa +11.504 in campione e <b>−121.891</b> fuori, e tutte e
/// venticinque le combinazioni di stop e target in quella regione hanno il fuori campione negativo.
/// La 002 non e' il motore nudo: ha il pattern neutro 44 (sessione stretta) e la finestra 03-18. Ma
/// quei due filtri sono stati scelti su tre anni, 2022-2025, e su di essi poggia <b>tutto</b> il
/// margine.</para>
///
/// <para><b>Cosa misura.</b> La 002 esattamente com'e', senza toccarle un parametro, su campione
/// 2014-07 → 2021-01 e validazione 2021-01 → 2026-09. Se i filtri reggono anche li', il margine e'
/// loro e la strategia sta in piedi su dodici anni invece che su tre. Se cadono, la 002 e' una
/// configurazione trovata dentro il rumore di un periodo corto.</para>
///
/// <para>Gira anche la <b>001</b> sullo stesso periodo, che e' la stessa classe con l'uscita a fine
/// sessione invece che alle 21: la differenza fra le due sul lungo e' la stessa leva misurata su un
/// campione tre volte piu' grande.</para>
///
/// <para><b>Non asserisce una soglia</b>, stampa i numeri: e' una misura, non un test di
/// regressione. Il verdetto lo scrive chi legge, con i criteri di <c>metodo-unger</c> §8.2.</para>
/// </summary>
public sealed class Pt3b002LongPeriodTests(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private static readonly DateTime StartUtc = new(2014, 7, 18, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SplitUtc = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task The002AndThe001MeasuredOverTwelveYears()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        if (!Directory.Exists(Path.Combine(RepositoryPath, "datafeed-external", "ICS")))
        {
            output.WriteLine("feed assente: saltato.");
            return;
        }

        // Stessi costi della griglia lunga, cosi' i due resoconti si confrontano.
        var spread = SpreadTable.Load(settings.GetSpreadPath(), "ICS", SpreadStatistic.Median, SpreadResolution.PerSymbol);
        var swap = SwapTable.Load(settings.GetSwapPath(), "ICS");
        var key = StrategyKeys.NormalizeSymbol("@FDAX");

        var series = await SweepSeries.LoadAsync(
            new PiootooDataFeedService(new DatafeedCatalog(settings)), "@FDAX", [240, 1],
            StartUtc, EndUtc, warmupDays: 30d, broker: "ICS");

        var inSample = series.Between(series.StartUtc, SplitUtc);
        var outOfSample = series.Between(SplitUtc, series.EndUtc);
        output.WriteLine($"feed {series.StartUtc:yyyy-MM-dd} → {series.EndUtc:yyyy-MM-dd}, split {SplitUtc:yyyy-MM-dd}");
        output.WriteLine($"spread {spread.Points[key]} pt, swap long {swap.For(key).LongPointsPerNight} pt/notte, commissione 19,23/lato\n");

        foreach (var id in new[] { "PT3B_FDAX_PCH_002_240", "PT3B_FDAX_PCH_001_240" })
        {
            var job = new SweepJob(id)
            {
                InitialCapital = 1_000_000m,
                CommissionPerContract = 19.23m,
                ClockTimeframeMinutes = 1,
                SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spread.Points[key] },
                Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swap.For(key) },
                Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
            };

            var dentro = new SweepRunner(inSample).Run(job);
            var fuori = new SweepRunner(outOfSample).Run(job);

            output.WriteLine($"=== {id}");
            output.WriteLine($"  campione   {dentro.Trades,5} trade  netto {dentro.NetProfit,12:N0}  DD {dentro.MaxClosedTradeDrawdown,10:N0}  PF {Fmt(dentro.ProfitFactor)}  avg {Avg(dentro),8:N0}");
            output.WriteLine($"  validazione{fuori.Trades,5} trade  netto {fuori.NetProfit,12:N0}  DD {fuori.MaxClosedTradeDrawdown,10:N0}  PF {Fmt(fuori.ProfitFactor)}  avg {Avg(fuori),8:N0}");
            output.WriteLine($"  soglia average trade: 266 in campione, 365 fuori (15% del range medio della barra)\n");
        }
    }

    private static decimal Avg(SweepOutcome o) => o.Trades > 0 ? o.NetProfit / o.Trades : 0m;
    private static string Fmt(decimal? v) => v.HasValue ? v.Value.ToString("N2") : "n/d";
}
