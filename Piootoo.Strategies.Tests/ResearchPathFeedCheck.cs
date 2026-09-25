using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le strategie uscite da un percorso (<see cref="ResearchPathStudy"/>), rimisurate sullo stesso periodo
/// della prova sul broker ma sul <b>feed interno</b>. Separa due cause che la sola prova su FTMO confonde:
/// se la strategia regge sul feed interno e non su quello del broker il problema e' il feed o il costo;
/// se non regge neanche li', il mercato dopo il 2020 non e' quello su cui e' stata trovata.
///
/// <para>Legge i parametri dalle righe <c>Parametri:</c> del resoconto indicato da
/// <c>PIOOTOO_PERCORSO_CONTROLLO</c> (<c>@FDAX|240</c>). Le due misure usano gli stessi costi FTMO: cambia
/// solo il feed.</para>
/// </summary>
public sealed class ResearchPathFeedCheck(ITestOutputHelper output)
{
    public const string CellVariable = "PIOOTOO_PERCORSO_CONTROLLO";
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const string Broker = "FTMO";

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task InternalFeedAgainstBrokerFeedOnTheHoldout()
    {
        var cell = Environment.GetEnvironmentVariable(CellVariable);
        if (string.IsNullOrWhiteSpace(cell))
        {
            output.WriteLine($"{CellVariable} non impostata: niente da controllare.");
            return;
        }

        if (ResearchStudy.IsSkipped(output)) return;

        var parts = cell.Split('|', StringSplitOptions.TrimEntries);
        var symbol = "@" + parts[0].TrimStart('@').ToUpperInvariant();
        var timeframe = int.Parse(parts[1], CultureInfo.InvariantCulture);
        var folder = Path.Combine(RepositoryPath, "ricerca", "percorso");
        var reportPath = Path.Combine(folder, $"{symbol.TrimStart('@').ToLowerInvariant()}-{timeframe}.md");
        var strategies = Parse(File.ReadAllLines(reportPath));

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var key = symbol.TrimStart('@');
        var spread = SpreadTable.Load(settings.GetSpreadPath(), Broker, SpreadStatistic.Median, SpreadResolution.PerSymbol).Points[key];
        var swap = SwapTable.Load(settings.GetSwapPath(), Broker).Specs[key];

        // Lo stesso periodo della prova sul broker in ResearchPathStudy.
        var from = new DateTime(2020, 11, 9, 0, 0, 0, DateTimeKind.Utc);
        // Il feed interno del DAX finisce a maggio 2025: oltre, il confronto misurerebbe un feed contro
        // il nulla. Terza parte opzionale della cella: la fine del periodo comune.
        var to = parts.Length > 2
            ? DateTime.SpecifyKind(DateTime.Parse(parts[2], CultureInfo.InvariantCulture), DateTimeKind.Utc)
            : new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        var internalFeed = await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], from, to, null, warmupDays: 30d);
        var brokerFeed = await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], from, to, Broker, warmupDays: 30d);

        var instrument = InstrumentRegistry.Get(symbol);
        var threshold = ResearchCriteria.AverageTradeThreshold(brokerFeed.Bars(timeframe), instrument.PointValue, instrument.TickSize);

        var report = new StringBuilder();
        report.AppendLine($"# Feed interno contro feed {Broker} — {symbol} {timeframe}m, {from:yyyy-MM-dd} → {to:yyyy-MM-dd}");
        report.AppendLine();
        report.AppendLine($"Stesse strategie del percorso (`{Path.GetFileName(reportPath)}`), stessi costi {Broker} (spread {spread} punti, swap). " +
                          $"Cambia solo il feed. Soglia di average trade {threshold:N0}.");
        report.AppendLine();
        report.AppendLine("| motore | feed | trade | netto | DD | net/DD | avg trade | finestre in utile |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|---|");

        foreach (var (engine, parameters) in strategies)
        {
            // Il resoconto non scrive simbolo e timeframe: li danno la cella.
            parameters["Symbol"] = symbol;
            parameters["TimeframeMinutes"] = timeframe;
            var job = new SweepJob(ResearchPathEngines.Container(engine), parameters)
            {
                InitialCapital = 1_000_000m,
                CommissionPerContract = 0m,
                ClockTimeframeMinutes = 1,
                SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spread },
                Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swap },
                Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
            };

            foreach (var (label, series) in new[] { ("interno", internalFeed), (Broker, brokerFeed) })
            {
                var outcome = new SweepRunner(series).Run(job);
                var windows = Windows(series, job, 4);
                var ratio = outcome.MaxClosedTradeDrawdown > 0m ? outcome.NetProfit / outcome.MaxClosedTradeDrawdown : 0m;
                var line = $"| {engine} | {label} | {outcome.Trades} | {outcome.NetProfit:N0} | {outcome.MaxClosedTradeDrawdown:N0} | {ratio:N2} | " +
                           $"{outcome.AverageTrade:N0} | {windows.Count(net => net > 0m)}/4 ({string.Join(" / ", windows.Select(net => net.ToString("N0", CultureInfo.InvariantCulture)))}) |";
                report.AppendLine(line);
                output.WriteLine(line);
                Console.WriteLine(line);
            }

            File.WriteAllText(Path.Combine(folder, $"{symbol.TrimStart('@').ToLowerInvariant()}-{timeframe}-feed-interno.md"),
                report.ToString(), Encoding.UTF8);
        }
    }

    public const string TradeVariable = "PIOOTOO_PERCORSO_TRADE";

    /// <summary>
    /// Una strategia del resoconto trade per trade sui due feed: accoppia gli ingressi (stesso lato,
    /// entro una barra) e dice da dove viene la differenza di netto — prezzo d'ingresso, prezzo d'uscita,
    /// motivo d'uscita diverso, trade che esistono su un feed solo. Cella in
    /// <c>PIOOTOO_PERCORSO_TRADE</c>: <c>MOTORE|@SIMBOLO|TIMEFRAME|FINE</c>.
    /// </summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task TradeByTradeOnTheTwoFeeds()
    {
        var cell = Environment.GetEnvironmentVariable(TradeVariable);
        if (string.IsNullOrWhiteSpace(cell))
        {
            output.WriteLine($"{TradeVariable} non impostata: niente da confrontare.");
            return;
        }

        if (ResearchStudy.IsSkipped(output)) return;

        var parts = cell.Split('|', StringSplitOptions.TrimEntries);
        var engine = parts[0].ToUpperInvariant();
        var symbol = "@" + parts[1].TrimStart('@').ToUpperInvariant();
        var timeframe = int.Parse(parts[2], CultureInfo.InvariantCulture);
        var from = new DateTime(2020, 11, 9, 0, 0, 0, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(DateTime.Parse(parts[3], CultureInfo.InvariantCulture), DateTimeKind.Utc);

        var folder = Path.Combine(RepositoryPath, "ricerca", "percorso");
        var name = $"{symbol.TrimStart('@').ToLowerInvariant()}-{timeframe}";
        var parameters = Parse(File.ReadAllLines(Path.Combine(folder, name + ".md")))
            .First(entry => string.Equals(entry.Engine, engine, StringComparison.OrdinalIgnoreCase)).Parameters;
        parameters["Symbol"] = symbol;
        parameters["TimeframeMinutes"] = timeframe;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var key = symbol.TrimStart('@');
        var job = new SweepJob(ResearchPathEngines.Container(engine), parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = SpreadTable.Load(settings.GetSpreadPath(), Broker, SpreadStatistic.Median, SpreadResolution.PerSymbol).Points[key]
            },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = SwapTable.Load(settings.GetSwapPath(), Broker).Specs[key] },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        var internalTrades = new SweepRunner(await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], from, to, null, warmupDays: 30d))
            .Run(job).ClosedTrades.OrderBy(trade => trade.EntryDate).ToList();
        var brokerTrades = new SweepRunner(await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], from, to, Broker, warmupDays: 30d))
            .Run(job).ClosedTrades.OrderBy(trade => trade.EntryDate).ToList();

        // Accoppiamento: stesso lato, ingresso entro una barra. Ogni trade si usa una volta.
        var used = new HashSet<int>();
        var pairs = new List<(Piootoo.Shared.Models.TradingResult Internal, Piootoo.Shared.Models.TradingResult Broker)>();
        var onlyInternal = new List<Piootoo.Shared.Models.TradingResult>();
        foreach (var trade in internalTrades)
        {
            var match = brokerTrades
                .Select((candidate, index) => (candidate, index))
                .Where(entry => !used.Contains(entry.index) && entry.candidate.Direction == trade.Direction &&
                                Math.Abs((entry.candidate.EntryDate - trade.EntryDate).TotalMinutes) <= timeframe)
                .OrderBy(entry => Math.Abs((entry.candidate.EntryDate - trade.EntryDate).TotalMinutes))
                .FirstOrDefault();
            if (match.candidate is null) { onlyInternal.Add(trade); continue; }
            used.Add(match.index);
            pairs.Add((trade, match.candidate));
        }

        var onlyBroker = brokerTrades.Where((_, index) => !used.Contains(index)).ToList();
        var instrument = InstrumentRegistry.Get(symbol);
        var pointValue = instrument.PointValue;

        var csv = new StringBuilder("entrata;lato;ingresso interno;ingresso ftmo;uscita interno;uscita ftmo;motivo interno;motivo ftmo;uscita alle interno;uscita alle ftmo;netto interno;netto ftmo;differenza\n");
        foreach (var (inside, broker) in pairs)
        {
            csv.AppendLine(string.Join(';', inside.EntryDate.ToString("yyyy-MM-dd HH:mm"), inside.Direction,
                inside.EntryPrice, broker.EntryPrice, inside.ExitPrice, broker.ExitPrice, inside.ExitReason, broker.ExitReason,
                inside.ExitDate.ToString("yyyy-MM-dd HH:mm"), broker.ExitDate.ToString("yyyy-MM-dd HH:mm"),
                inside.NetProfit.ToString("0", CultureInfo.InvariantCulture), broker.NetProfit.ToString("0", CultureInfo.InvariantCulture),
                (broker.NetProfit - inside.NetProfit).ToString("0", CultureInfo.InvariantCulture)));
        }
        File.WriteAllText(Path.Combine(folder, $"{name}-{engine.ToLowerInvariant()}-trade-per-trade.csv"), csv.ToString(), Encoding.UTF8);

        // Da dove viene la differenza: le quattro parti sommano alla differenza totale.
        var totalGap = brokerTrades.Sum(trade => trade.NetProfit) - internalTrades.Sum(trade => trade.NetProfit);
        var sameReason = pairs.Where(pair => pair.Internal.ExitReason == pair.Broker.ExitReason).ToList();
        var otherReason = pairs.Where(pair => pair.Internal.ExitReason != pair.Broker.ExitReason).ToList();
        decimal Sign(Piootoo.Shared.Models.TradingResult trade) => trade.Direction == Piootoo.Shared.Enums.SignalType.Buy ? 1m : -1m;
        var entryPart = pairs.Sum(pair => -(pair.Broker.EntryPrice - pair.Internal.EntryPrice) * Sign(pair.Internal) * pointValue);
        var exitPart = pairs.Sum(pair => (pair.Broker.ExitPrice - pair.Internal.ExitPrice) * Sign(pair.Internal) * pointValue);
        var differences = pairs.Select(pair => pair.Broker.NetProfit - pair.Internal.NetProfit).OrderByDescending(Math.Abs).ToList();

        var report = new StringBuilder();
        report.AppendLine($"# {engine} {symbol} {timeframe}m trade per trade — interno contro {Broker}, {from:yyyy-MM-dd} → {to:yyyy-MM-dd}");
        report.AppendLine();
        report.AppendLine($"- trade: interno {internalTrades.Count} (netto {internalTrades.Sum(t => t.NetProfit):N0}), {Broker} {brokerTrades.Count} (netto {brokerTrades.Sum(t => t.NetProfit):N0}); differenza {totalGap:N0}");
        report.AppendLine($"- accoppiati {pairs.Count}; solo interno {onlyInternal.Count} (netto {onlyInternal.Sum(t => t.NetProfit):N0}); solo {Broker} {onlyBroker.Count} (netto {onlyBroker.Sum(t => t.NetProfit):N0})");
        report.AppendLine($"- sugli accoppiati la differenza e' {differences.Sum():N0}: prezzo d'ingresso {entryPart:N0}, prezzo d'uscita {exitPart:N0}, resto (swap) {differences.Sum() - entryPart - exitPart:N0}");
        report.AppendLine($"- stesso motivo d'uscita su {sameReason.Count} coppie (differenza {sameReason.Sum(p => p.Broker.NetProfit - p.Internal.NetProfit):N0}); motivo diverso su {otherReason.Count} (differenza {otherReason.Sum(p => p.Broker.NetProfit - p.Internal.NetProfit):N0})");
        report.AppendLine($"- concentrazione: le 10 coppie con la differenza piu' grande fanno {differences.Take(10).Sum():N0}, le 30 fanno {differences.Take(30).Sum():N0}; mediana della differenza {Median(differences):N0} per trade");
        report.AppendLine($"- differenza media sul prezzo d'ingresso {pairs.Average(p => p.Broker.EntryPrice - p.Internal.EntryPrice):N2} punti, sul prezzo d'uscita {pairs.Average(p => p.Broker.ExitPrice - p.Internal.ExitPrice):N2}");
        report.AppendLine();
        report.AppendLine("| motivo d'uscita interno → FTMO | coppie | differenza |");
        report.AppendLine("|---|---:|---:|");
        foreach (var group in pairs.GroupBy(pair => $"{pair.Internal.ExitReason} → {pair.Broker.ExitReason}").OrderByDescending(g => Math.Abs(g.Sum(p => p.Broker.NetProfit - p.Internal.NetProfit))))
            report.AppendLine($"| {group.Key} | {group.Count()} | {group.Sum(p => p.Broker.NetProfit - p.Internal.NetProfit):N0} |");
        report.AppendLine();
        report.AppendLine("| anno | differenza |");
        report.AppendLine("|---|---:|");
        foreach (var group in pairs.GroupBy(pair => pair.Internal.EntryDate.Year).OrderBy(g => g.Key))
            report.AppendLine($"| {group.Key} | {group.Sum(p => p.Broker.NetProfit - p.Internal.NetProfit):N0} |");

        File.WriteAllText(Path.Combine(folder, $"{name}-{engine.ToLowerInvariant()}-trade-per-trade.md"), report.ToString(), Encoding.UTF8);
        output.WriteLine(report.ToString());
        Console.WriteLine(report.ToString());
    }

    public const string LeverVariable = "PIOOTOO_PERCORSO_LEVA";

    /// <summary>
    /// Il passo a mano su una leva di una strategia del resoconto, misurato su <b>tre</b> periodi e due
    /// feed: la ricerca (interno 2008-2020), il periodo comune ai due feed (nov 2020 → mag 2025) su
    /// entrambi, e FTMO fino a oggi. Per il periodo comune dice anche quanti trade esistono su un feed
    /// solo: e' la misura dei trade "di confine" che una rottura di pochi punti accende o no. Cella in
    /// <c>PIOOTOO_PERCORSO_LEVA</c>: <c>MOTORE|@SIMBOLO|TIMEFRAME|LEVA|v1,v2,...</c>.
    /// </summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task LeverOnTheTwoFeeds()
    {
        var cell = Environment.GetEnvironmentVariable(LeverVariable);
        if (string.IsNullOrWhiteSpace(cell))
        {
            output.WriteLine($"{LeverVariable} non impostata: niente da misurare.");
            return;
        }

        if (ResearchStudy.IsSkipped(output)) return;

        var parts = cell.Split('|', StringSplitOptions.TrimEntries);
        var engine = parts[0].ToUpperInvariant();
        var symbol = "@" + parts[1].TrimStart('@').ToUpperInvariant();
        var timeframe = int.Parse(parts[2], CultureInfo.InvariantCulture);
        var lever = parts[3];
        var values = parts[4].Split(',').Select(value => decimal.Parse(value, CultureInfo.InvariantCulture)).ToList();

        var folder = Path.Combine(RepositoryPath, "ricerca", "percorso");
        var name = $"{symbol.TrimStart('@').ToLowerInvariant()}-{timeframe}";
        var parameters = Parse(File.ReadAllLines(Path.Combine(folder, name + ".md")))
            .First(entry => string.Equals(entry.Engine, engine, StringComparison.OrdinalIgnoreCase)).Parameters;
        parameters["Symbol"] = symbol;
        parameters["TimeframeMinutes"] = timeframe;
        // Sesta parte opzionale: parametri fissati per tutte le righe, CHIAVE=VALORE separati da virgola.
        foreach (var pair in (parts.Length > 5 ? parts[5] : string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            parameters[kv[0].Trim()] = decimal.Parse(kv[1], CultureInfo.InvariantCulture);
        }

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var key = symbol.TrimStart('@');
        var template = new SweepJob(ResearchPathEngines.Container(engine))
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = SpreadTable.Load(settings.GetSpreadPath(), Broker, SpreadStatistic.Median, SpreadResolution.PerSymbol).Points[key]
            },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = SwapTable.Load(settings.GetSwapPath(), Broker).Specs[key] },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        var research = await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], Utc(2008, 1, 1), Utc(2020, 11, 9), null, warmupDays: 30d);
        var commonInternal = await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], Utc(2020, 11, 9), Utc(2025, 6, 1), null, warmupDays: 30d);
        var brokerAll = await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), Broker, warmupDays: 30d);
        var commonBroker = brokerAll.Between(brokerAll.StartUtc, Utc(2025, 6, 1));

        var instrument = InstrumentRegistry.Get(symbol);
        var researchThreshold = ResearchCriteria.AverageTradeThreshold(research.Bars(timeframe), instrument.PointValue, instrument.TickSize);
        var brokerThreshold = ResearchCriteria.AverageTradeThreshold(brokerAll.Bars(timeframe), instrument.PointValue, instrument.TickSize);

        string Cell(SweepOutcome outcome, decimal threshold)
        {
            var ratio = outcome.MaxClosedTradeDrawdown > 0m ? outcome.NetProfit / outcome.MaxClosedTradeDrawdown : 0m;
            return $"{outcome.Trades} · {outcome.NetProfit:N0} · {ratio:N2} · {outcome.AverageTrade:N0}{(outcome.AverageTrade >= threshold ? " ✔" : string.Empty)}";
        }

        var report = new StringBuilder();
        report.AppendLine($"# {engine} {symbol} {timeframe}m — leva {lever} sui due feed");
        report.AppendLine();
        report.AppendLine($"Gli altri parametri sono quelli del percorso (`{name}.md`). Costi {Broker}. Ogni cella: trade · netto · net/DD · average trade " +
                          $"(✔ = sopra la soglia: {researchThreshold:N0} nella ricerca, {brokerThreshold:N0} su {Broker}). " +
                          "\"Solo su un feed\" = trade del periodo comune che esistono su un feed e non sull'altro, con il loro netto.");
        report.AppendLine();
        report.AppendLine($"| {lever} | ricerca interno 2008-2020 | interno nov 2020-mag 2025 | {Broker} nov 2020-mag 2025 | solo su un feed (interno / {Broker}) | {Broker} nov 2020-set 2026 | finestre {Broker} |");
        report.AppendLine("|---:|---|---|---|---|---|---|");

        var rows = new string[values.Count];
        Parallel.For(0, values.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, index =>
        {
            var job = template with { Parameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase) { [lever] = values[index] } };
            var inResearch = new SweepRunner(research).Run(job);
            var inCommon = new SweepRunner(commonInternal).Run(job);
            var brCommon = new SweepRunner(commonBroker).Run(job);
            var brAll = new SweepRunner(brokerAll).Run(job);
            var windows = Windows(brokerAll, job, 4);

            var brokerUsed = new HashSet<int>();
            var onlyInternal = new List<decimal>();
            foreach (var trade in inCommon.ClosedTrades)
            {
                var match = brCommon.ClosedTrades
                    .Select((candidate, i) => (candidate, i))
                    .FirstOrDefault(entry => !brokerUsed.Contains(entry.i) && entry.candidate.Direction == trade.Direction &&
                                             Math.Abs((entry.candidate.EntryDate - trade.EntryDate).TotalMinutes) <= timeframe);
                if (match.candidate is null) onlyInternal.Add(trade.NetProfit);
                else brokerUsed.Add(match.i);
            }

            var onlyBroker = brCommon.ClosedTrades.Where((_, i) => !brokerUsed.Contains(i)).Select(trade => trade.NetProfit).ToList();
            rows[index] = $"| {values[index]} | {Cell(inResearch, researchThreshold)} | {Cell(inCommon, brokerThreshold)} | {Cell(brCommon, brokerThreshold)} | " +
                          $"{onlyInternal.Count} ({onlyInternal.Sum():N0}) / {onlyBroker.Count} ({onlyBroker.Sum():N0}) | {Cell(brAll, brokerThreshold)} | " +
                          $"{windows.Count(net => net > 0m)}/4 ({string.Join(" / ", windows.Select(net => net.ToString("N0", CultureInfo.InvariantCulture)))}) |";
        });

        foreach (var row in rows) report.AppendLine(row);
        File.WriteAllText(Path.Combine(folder, $"{name}-{engine.ToLowerInvariant()}-{lever.ToLowerInvariant()}-due-feed.md"), report.ToString(), Encoding.UTF8);
        output.WriteLine(report.ToString());
        Console.WriteLine(report.ToString());
    }

    public const string YearsVariable = "PIOOTOO_PERCORSO_ANNI";

    /// <summary>
    /// Una strategia del resoconto, con eventuali parametri cambiati, anno per anno su tutto il feed
    /// interno e su tutto il feed del broker. Per ogni anno anche la soglia di average trade di
    /// quell'anno (15% del range medio della barra): il rapporto average trade / soglia confronta anni
    /// con prezzi diversi, che i netti in denaro non confrontano. Cella in <c>PIOOTOO_PERCORSO_ANNI</c>:
    /// <c>MOTORE|@SIMBOLO|TIMEFRAME|CHIAVE=VALORE,...</c>.
    /// </summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task YearsOnTheTwoFeeds()
    {
        var cell = Environment.GetEnvironmentVariable(YearsVariable);
        if (string.IsNullOrWhiteSpace(cell))
        {
            output.WriteLine($"{YearsVariable} non impostata: niente da misurare.");
            return;
        }

        if (ResearchStudy.IsSkipped(output)) return;

        var parts = cell.Split('|', StringSplitOptions.TrimEntries);
        var engine = parts[0].ToUpperInvariant();
        var symbol = "@" + parts[1].TrimStart('@').ToUpperInvariant();
        var timeframe = int.Parse(parts[2], CultureInfo.InvariantCulture);

        var folder = Path.Combine(RepositoryPath, "ricerca", "percorso");
        var name = $"{symbol.TrimStart('@').ToLowerInvariant()}-{timeframe}";
        var parameters = Parse(File.ReadAllLines(Path.Combine(folder, name + ".md")))
            .First(entry => string.Equals(entry.Engine, engine, StringComparison.OrdinalIgnoreCase)).Parameters;
        parameters["Symbol"] = symbol;
        parameters["TimeframeMinutes"] = timeframe;
        var changes = parts.Length > 3 ? parts[3] : string.Empty;
        foreach (var pair in changes.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            parameters[kv[0].Trim()] = decimal.Parse(kv[1], CultureInfo.InvariantCulture);
        }

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var key = symbol.TrimStart('@');
        var job = new SweepJob(ResearchPathEngines.Container(engine), parameters)
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = SpreadTable.Load(settings.GetSpreadPath(), Broker, SpreadStatistic.Median, SpreadResolution.PerSymbol).Points[key]
            },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = SwapTable.Load(settings.GetSwapPath(), Broker).Specs[key] },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        var feeds = new[]
        {
            (Label: "interno", Series: await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], Utc(2008, 1, 1), Utc(2025, 6, 1), null, warmupDays: 30d)),
            (Label: Broker, Series: await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), Broker, warmupDays: 30d))
        };

        var instrument = InstrumentRegistry.Get(symbol);
        var results = feeds.AsParallel().Select(feed =>
        {
            var trades = new SweepRunner(feed.Series).Run(job).ClosedTrades;
            var bars = feed.Series.Bars(timeframe).Where(bar => bar.DateTime >= feed.Series.StartUtc).ToList();
            var thresholds = bars.GroupBy(bar => bar.DateTime.Year).ToDictionary(
                group => group.Key,
                group => ResearchCriteria.AverageTradeThreshold(group.ToList(), instrument.PointValue, instrument.TickSize));
            return (feed.Label, Years: trades.GroupBy(trade => trade.ExitDate.Year).ToDictionary(group => group.Key, group => group.ToList()), thresholds);
        }).ToDictionary(result => result.Label);

        var report = new StringBuilder();
        report.AppendLine($"# {engine} {symbol} {timeframe}m anno per anno{(changes.Length > 0 ? $" ({changes})" : string.Empty)}");
        report.AppendLine();
        report.AppendLine($"Costi {Broker}. Per anno: trade · netto · average trade · **average trade / soglia dell'anno** (15% del range medio della barra; 1,00 = alla soglia). " +
                          "Il rapporto confronta anni con prezzi diversi.");
        report.AppendLine();
        report.AppendLine($"| anno | interno | {Broker} |");
        report.AppendLine("|---|---|---|");
        var years = results.Values.SelectMany(result => result.Years.Keys).Distinct().OrderBy(year => year);
        foreach (var year in years)
        {
            string Cell(string label)
            {
                var result = results[label];
                if (!result.Years.TryGetValue(year, out var trades)) return "-";
                var net = trades.Sum(trade => trade.NetProfit);
                var average = net / trades.Count;
                var ratio = result.thresholds.TryGetValue(year, out var threshold) && threshold > 0m ? average / threshold : 0m;
                return $"{trades.Count} · {net:N0} · {average:N0} · **{ratio:N2}**";
            }

            report.AppendLine($"| {year} | {Cell("interno")} | {Cell(Broker)} |");
        }

        File.WriteAllText(Path.Combine(folder, $"{name}-{engine.ToLowerInvariant()}-anni.md"), report.ToString(), Encoding.UTF8);
        output.WriteLine(report.ToString());
        Console.WriteLine(report.ToString());
    }

    public const string RuleVariable = "PIOOTOO_REGOLA";

    /// <summary>
    /// Una regola <b>fissata</b> su altri mercati, senza ritoccare niente: la conferma pulita di
    /// un'ipotesi nata guardando un mercato solo. Per ogni mercato la regola con la leva sotto esame
    /// spenta e accesa, su ogni feed disponibile (interno lungo dove c'e' il minuto, FTMO sempre). Cella
    /// in <c>PIOOTOO_REGOLA</c>: <c>MOTORE|TIMEFRAME|@SIM1,@SIM2|LEVA=VALORE|CHIAVE=VALORE,...</c>.
    /// </summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task FixedRuleOnOtherMarkets()
    {
        var cell = Environment.GetEnvironmentVariable(RuleVariable);
        if (string.IsNullOrWhiteSpace(cell))
        {
            output.WriteLine($"{RuleVariable} non impostata: niente da misurare.");
            return;
        }

        if (ResearchStudy.IsSkipped(output)) return;

        var parts = cell.Split('|', StringSplitOptions.TrimEntries);
        var engine = parts[0].ToUpperInvariant();
        var timeframe = int.Parse(parts[1], CultureInfo.InvariantCulture);
        var symbols = parts[2].Split(',', StringSplitOptions.TrimEntries).Select(s => "@" + s.TrimStart('@').ToUpperInvariant()).ToList();
        var leverPair = parts[3].Split('=', 2);
        var fixedPairs = parts.Length > 4 ? parts[4] : string.Empty;

        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var spreadTable = SpreadTable.Load(settings.GetSpreadPath(), Broker, SpreadStatistic.Median, SpreadResolution.PerSymbol);
        var swapTable = SwapTable.Load(settings.GetSwapPath(), Broker);
        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

        var report = new StringBuilder();
        report.AppendLine($"# Regola fissata {engine} {timeframe}m su altri mercati: {leverPair[0]} spento contro {leverPair[1]}");
        report.AppendLine();
        report.AppendLine($"Parametri fissi: base del percorso {engine}, `{fixedPairs}`. Costi {Broker} del mercato. Ogni cella: trade · netto · net/DD · average trade / soglia · anni in utile · quota del netto dall'anno migliore.");
        report.AppendLine();
        report.AppendLine($"| mercato | feed e periodo | {leverPair[0]} spento | {leverPair[0]} = {leverPair[1]} |");
        report.AppendLine("|---|---|---|---|");

        foreach (var symbol in symbols)
        {
            var key = symbol.TrimStart('@');
            if (!spreadTable.Points.TryGetValue(key, out var spread) || !swapTable.Specs.TryGetValue(key, out var swap))
            {
                report.AppendLine($"| {symbol} | costi {Broker} assenti | - | - |");
                continue;
            }

            var definition = ResearchPathEngines.Definition(engine, symbol, timeframe);
            var parameters = new Dictionary<string, object>(definition.Base, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in fixedPairs.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                parameters[kv[0].Trim()] = decimal.Parse(kv[1], CultureInfo.InvariantCulture);
            }

            var template = new SweepJob(ResearchPathEngines.Container(engine))
            {
                InitialCapital = 1_000_000m,
                CommissionPerContract = 0m,
                ClockTimeframeMinutes = 1,
                SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spread },
                Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swap },
                Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
            };

            var feeds = new List<(string Label, SweepSeries Series)>();
            if (File.Exists(Path.Combine(RepositoryPath, "datafeed", $"{symbol}_1.json")))
                feeds.Add(("interno 2008-2020", await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], Utc(2008, 1, 1), Utc(2020, 11, 9), null, warmupDays: 30d)));
            if (File.Exists(Path.Combine(RepositoryPath, "datafeed-external", Broker, $"{symbol}_1.json")))
                feeds.Add(($"{Broker} fino a set 2026", await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], Utc(2020, 11, 9), Utc(2026, 9, 1), Broker, warmupDays: 30d)));

            var instrument = InstrumentRegistry.Get(symbol);
            foreach (var (label, series) in feeds)
            {
                var threshold = ResearchCriteria.AverageTradeThreshold(series.Bars(timeframe), instrument.PointValue, instrument.TickSize);
                var cells = new[] { "0", leverPair[1] }.AsParallel().AsOrdered().Select(value =>
                {
                    var job = template with
                    {
                        Parameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase)
                        {
                            ["Symbol"] = symbol, ["TimeframeMinutes"] = timeframe,
                            [leverPair[0]] = decimal.Parse(value, CultureInfo.InvariantCulture)
                        }
                    };
                    var outcome = new SweepRunner(series).Run(job);
                    var byYear = outcome.ClosedTrades.GroupBy(trade => trade.ExitDate.Year).Select(group => group.Sum(trade => trade.NetProfit)).ToList();
                    var ratio = outcome.MaxClosedTradeDrawdown > 0m ? outcome.NetProfit / outcome.MaxClosedTradeDrawdown : 0m;
                    var bestShare = outcome.NetProfit > 0m && byYear.Count > 0 ? byYear.Max() / outcome.NetProfit : 0m;
                    return $"{outcome.Trades} · {outcome.NetProfit:N0} · {ratio:N2} · {(threshold > 0m ? outcome.AverageTrade / threshold : 0m):N2} · " +
                           $"{byYear.Count(net => net > 0m)}/{byYear.Count} · {bestShare:P0}";
                }).ToList();
                report.AppendLine($"| {symbol} | {label} | {cells[0]} | {cells[1]} |");
                output.WriteLine($"{symbol} {label}: {cells[0]} | {cells[1]}");
                Console.WriteLine($"{symbol} {label}: {cells[0]} | {cells[1]}");
            }

            File.WriteAllText(Path.Combine(RepositoryPath, "ricerca", "percorso", $"regola-{engine.ToLowerInvariant()}-{timeframe}-altri-mercati.md"),
                report.ToString(), Encoding.UTF8);
        }
    }

    private static decimal Median(List<decimal> values)
    {
        if (values.Count == 0) return 0m;
        var sorted = values.OrderBy(value => value).ToList();
        return sorted[sorted.Count / 2];
    }

    /// <summary>Le coppie (motore, parametri) dalle sezioni del resoconto che hanno una riga "Parametri:".</summary>
    private static List<(string Engine, Dictionary<string, object> Parameters)> Parse(string[] lines)
    {
        var result = new List<(string, Dictionary<string, object>)>();
        string? engine = null;
        foreach (var line in lines)
        {
            var header = Regex.Match(line, @"^## (\w+) ");
            if (header.Success && ResearchPathEngines.Keys.Contains(header.Groups[1].Value, StringComparer.OrdinalIgnoreCase))
            {
                engine = header.Groups[1].Value;
                continue;
            }

            if (engine is null || !line.StartsWith("Parametri: `", StringComparison.Ordinal))
                continue;

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in line["Parametri: `".Length..].TrimEnd('`').Split(", "))
            {
                var parts = pair.Split('=', 2);
                parameters[parts[0]] = decimal.Parse(parts[1], CultureInfo.InvariantCulture);
            }

            result.Add((engine, parameters));
            engine = null;
        }

        return result;
    }

    private static List<decimal> Windows(SweepSeries series, SweepJob job, int count)
    {
        var span = (series.EndUtc - series.StartUtc) / count;
        return Enumerable.Range(0, count)
            .Select(i => new SweepRunner(series.Between(series.StartUtc + span * i, i == count - 1 ? series.EndUtc : series.StartUtc + span * (i + 1)))
                .Run(job).NetProfit)
            .ToList();
    }
}
