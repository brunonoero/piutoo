using System.Globalization;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>Il run neutro delle PT8DAV ai costi veri</b>, da dare in ingresso a <c>piootoo-plan-builder</c>:
/// ogni strategia a un contratto sul feed FTMO, con lo spread FTMO per ora UTC, lo swap FTMO, la
/// commissione dei piani e la tenuta dei piani PT5DAV (overnight e overweek ammessi). Motore vero,
/// orologio al minuto, livelli gia' scavalcati trattati come sul conto.
///
/// <para><b>Perche' uno studio e non un backtest del server.</b> Il server installato non ha ancora
/// queste classi, e avviarne un secondo sullo stesso repository gli farebbe riprendere le sessioni
/// live dell'altro. Lo studio usa lo stesso <see cref="PiootooTradingService"/> attraverso
/// <see cref="SweepRunner"/> e scrive <c>trades.json</c> con <see cref="TradingJsonStore"/>, il
/// formato che il costruttore di piani legge. Non e' un run con un piano: quello si fa dopo, dal
/// server, sui piani composti.</para>
///
/// <para><b>Periodo.</b> Dal 01/06/2022, dove finisce l'ottimizzazione della ricerca, alla fine
/// dell'archivio. Il costruttore lo divide con <c>--from</c>/<c>--to</c>: fino al 31/05/2025 e' il
/// tratto su cui la ricerca ha filtrato (ma con il suo simulatore e i suoi costi), dal 01/06/2025 e'
/// quello che nessuna scelta ha visto. Prima della data d'inizio le strategie si valutano senza
/// operare, per scaldare l'ATR50.</para>
///
/// <para>Scrive in <c>piootoo-repository/ricerca/pt8dav-piani/run-ftmo/{SIMBOLO}/</c>. Gira solo con
/// <c>PIOOTOO_STUDI=1</c>; <c>PIOOTOO_PT8DAV_COSTI</c> sceglie il broker di spread e swap (default
/// <c>FTMO</c>; <c>FINTOKEI</c> scrive in <c>run-fintokei</c>, sempre sul feed FTMO, perche' il feed
/// Fintokei comincia a fine 2025).</para>
/// </summary>
public sealed class Pt8DavNeutralRunStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";

    private static readonly DateTime Start = new(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Primo giorno dell'archivio FTMO per simbolo: prima non c'e' niente da caricare.</summary>
    private static readonly Dictionary<string, DateTime> ArchiveStart = new(StringComparer.Ordinal)
    {
        ["BP"] = Utc(2020, 4, 2), ["CL"] = Utc(2020, 11, 10), ["ES"] = Utc(2020, 11, 10),
        ["FDAX"] = Utc(2020, 11, 10), ["GC"] = Utc(2020, 5, 7), ["KC"] = Utc(2023, 8, 15),
        ["NQ"] = Utc(2022, 5, 17), ["YM"] = Utc(2020, 11, 10)
    };

    [Theory]
    [Trait("Category", ResearchStudy.Category)]
    [InlineData("BP")]
    [InlineData("CL")]
    [InlineData("ES")]
    [InlineData("FDAX")]
    [InlineData("GC")]
    [InlineData("KC")]
    [InlineData("NQ")]
    [InlineData("YM")]
    public async Task NeutralRunAtBrokerCosts(string symbol)
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var costs = Environment.GetEnvironmentVariable("PIOOTOO_PT8DAV_COSTI") is { Length: > 0 } broker ? broker : "FTMO";
        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };

        var spread = SpreadTable.Load(settings.GetSpreadPath(), costs, SpreadStatistic.Median, SpreadResolution.PerHour);
        var swaps = SwapTable.Load(settings.GetSwapPath(), costs).Specs;
        if (!spread.Points.ContainsKey(symbol) || !swaps.ContainsKey(symbol))
        {
            // Un simbolo che il broker non tratta (il caffe' su Fintokei) non ha un run: si dice e si salta.
            output.WriteLine($"{symbol}: {costs} non ha spread o swap per questo simbolo, nessun run.");
            return;
        }

        // La commissione dei piani: 2 per contratto e per lato su FTMO, 0 su Fintokei (registro-piani.md).
        var commission = string.Equals(costs, "FINTOKEI", StringComparison.OrdinalIgnoreCase) ? 0m : 2m;

        var strategies = StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true)
            .Where(d => d.Id.StartsWith($"PT8DAV_{symbol}_", StringComparison.Ordinal))
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(strategies);

        var entriesFrom = Start > ArchiveStart[symbol] ? Start : ArchiveStart[symbol];
        var runFrom = entriesFrom.AddDays(-300) > ArchiveStart[symbol] ? entriesFrom.AddDays(-300) : ArchiveStart[symbol];
        var timeframes = strategies.Select(d => d.TimeframeMinutes).Append(1).Distinct().Order().ToArray();
        var series = await SweepSeries.LoadAsync(
            new PiootooDataFeedService(new DatafeedCatalog(settings)), $"@{symbol}", timeframes, runFrom, End, "FTMO", warmupDays: 150d);
        var runner = new SweepRunner(series);

        var trades = new List<PersistedTrade>();
        foreach (var definition in strategies)
        {
            var outcome = runner.Run(new SweepJob(definition.Id)
            {
                InitialCapital = 10_000_000m,
                CommissionPerContract = commission,
                ClockTimeframeMinutes = 1,
                RejectWrongSideLevels = true,
                SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [symbol] = spread.Points[symbol] },
                SpreadPointsByHour = spread.PointsByHour.TryGetValue(symbol, out var byHour)
                    ? new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase) { [symbol] = byHour }
                    : null,
                Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [symbol] = swaps[symbol] },
                Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true },
                EntriesFromUtc = entriesFrom
            });

            var index = 0;
            foreach (var trade in outcome.ClosedTrades)
            {
                trades.Add(new PersistedTrade
                {
                    TradeId = $"{definition.Id}-trade-{++index:D6}",
                    CorrelationId = $"pt8dav-neutro-{costs.ToLowerInvariant()}",
                    StrategyCode = trade.StrategyCode,
                    StrategyName = trade.StrategyName,
                    Symbol = StrategyKeys.NormalizeSymbol(trade.Symbol),
                    Direction = trade.Direction,
                    Quantity = trade.Quantity,
                    EntryTimeUtc = TradingDateTime.ToFeedUtc(trade.EntryDate),
                    ExitTimeUtc = TradingDateTime.ToFeedUtc(trade.ExitDate),
                    EntryPrice = trade.EntryPrice,
                    ExitPrice = trade.ExitPrice,
                    ExitReason = trade.ExitReason.ToString(),
                    GrossProfit = trade.GrossProfit,
                    NetProfit = trade.NetProfit,
                    Commission = trade.Commission
                });
            }

            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{definition.Id}: {outcome.Trades} trade, netto {outcome.NetProfit:0}, DD chiuso {outcome.MaxClosedTradeDrawdown:0}"));
        }

        var folder = Path.Combine(RepositoryPath, "ricerca", "pt8dav-piani", $"run-{costs.ToLowerInvariant()}", symbol);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        new TradingJsonStore(folder).WriteTrades(trades);
        output.WriteLine($"{symbol}: {trades.Count} trade di {strategies.Count} strategie in {folder}");
    }

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
