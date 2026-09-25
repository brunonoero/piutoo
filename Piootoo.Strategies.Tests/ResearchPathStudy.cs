using System.Globalization;
using System.Text;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il percorso di ricerca v4 (<see cref="ResearchPath"/>) su una cella, e la <b>prova sul broker</b>
/// della strategia che ne esce, su un periodo che il percorso non ha visto.
///
/// <para><b>Quale cella</b> lo dice <c>PIOOTOO_PERCORSO</c>, nella forma <c>MOTORI|@SIMBOLO|TIMEFRAME</c>:
/// <c>PCH,LFHL|@FDAX|240</c>, oppure <c>*|@FDAX|240</c> per tutti i motori. Le serie si caricano una
/// volta per tutti i motori della cella.</para>
///
/// <para><b>Dove si cerca e dove si prova.</b> Dove c'e' la storia lunga al minuto nel feed interno
/// (DAX dal 2008, Nasdaq dal 2006) si cerca li' fino all'inizio del feed FTMO, e si prova su tutto il
/// feed FTMO: sei anni mai visti, con i prezzi del broker. Per gli altri simboli si cerca sul feed
/// FTMO fino al 2024-09 e si prova sui due anni dopo. Costi sempre di FTMO, spread mediano e swap,
/// anche sul feed interno: la ricerca deve pagare cio' che paghera' il conto.</para>
/// </summary>
public sealed class ResearchPathStudy(ITestOutputHelper output)
{
    public const string CellVariable = "PIOOTOO_PERCORSO";
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";
    private const string Broker = "FTMO";

    private sealed record Periods(string? SearchBroker, DateTime SearchFrom, DateTime SearchTo, DateTime HoldoutFrom, DateTime HoldoutTo);

    private static readonly DateTime End = Utc(2026, 9, 1);

    /// <summary>Storia lunga del feed interno, fino all'inizio del minuto FTMO.</summary>
    private static readonly Dictionary<string, Periods> LongHistory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["@FDAX"] = new(null, Utc(2008, 1, 1), Utc(2020, 11, 9), Utc(2020, 11, 9), End),
        ["@NQ"] = new(null, Utc(2008, 1, 1), Utc(2022, 5, 16), Utc(2022, 5, 16), End)
    };

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task ResearchPathOnTheCellFromTheEnvironment()
    {
        var cell = Environment.GetEnvironmentVariable(CellVariable);
        if (string.IsNullOrWhiteSpace(cell))
        {
            output.WriteLine($"{CellVariable} non impostata: nessuna cella da lanciare.");
            return;
        }

        if (ResearchStudy.IsSkipped(output)) return;

        var parts = cell.Split('|', StringSplitOptions.TrimEntries);
        var engines = parts[0] == "*" ? ResearchPathEngines.Keys.ToList() : parts[0].Split(',', StringSplitOptions.TrimEntries).ToList();
        var symbol = "@" + parts[1].TrimStart('@').ToUpperInvariant();
        var timeframe = int.Parse(parts[2], CultureInfo.InvariantCulture);
        await RunAsync(engines, symbol, timeframe);
    }

    private async Task RunAsync(List<string> engines, string symbol, int timeframe)
    {
        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external",
            SpreadPath = @"[BasePath]\spread"
        };
        var key = symbol.TrimStart('@');
        var spreadValue = SpreadTable.Load(settings.GetSpreadPath(), Broker, SpreadStatistic.Median, SpreadResolution.PerSymbol).Points[key];
        var swapSpec = SwapTable.Load(settings.GetSwapPath(), Broker).Specs[key];
        var template = new SweepJob(ResearchPathEngines.Container(engines[0]))
        {
            InitialCapital = 1_000_000m,
            CommissionPerContract = 0m,
            ClockTimeframeMinutes = 1,
            SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spreadValue },
            Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swapSpec },
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
        };

        var periods = LongHistory.TryGetValue(symbol, out var longHistory)
            ? longHistory
            : new Periods(Broker, Utc(2020, 11, 9), Utc(2024, 9, 1), Utc(2024, 9, 1), End);

        var dataFeed = new PiootooDataFeedService(new DatafeedCatalog(settings));
        var history = await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], periods.SearchFrom, periods.SearchTo,
            periods.SearchBroker, warmupDays: 30d);
        var holdout = await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], periods.HoldoutFrom, periods.HoldoutTo,
            Broker, warmupDays: 30d);

        // Lo stesso periodo della prova sul feed interno, dove c'e': il controllo "due feed". Il feed interno
        // puo' finire prima (quello del DAX si ferma a maggio 2025), e allora il confronto si fa sul pezzo comune.
        SweepSeries? internalHoldout = null;
        if (periods.SearchBroker is null)
        {
            var loaded = await SweepSeries.LoadAsync(dataFeed, symbol, [timeframe, 1], periods.HoldoutFrom, periods.HoldoutTo, null, warmupDays: 30d);
            var minutes = loaded.Bars(1);
            if (minutes.Length > 0 && minutes[^1].DateTime > periods.HoldoutFrom.AddDays(365))
            {
                var commonEnd = minutes[^1].DateTime < periods.HoldoutTo ? minutes[^1].DateTime.Date : periods.HoldoutTo;
                internalHoldout = loaded.Between(periods.HoldoutFrom, commonEnd);
            }
        }

        var instrument = InstrumentRegistry.Get(symbol);
        var threshold = ResearchCriteria.AverageTradeThreshold(history.Bars(timeframe), instrument.PointValue, instrument.TickSize);
        var holdoutThreshold = ResearchCriteria.AverageTradeThreshold(holdout.Bars(timeframe), instrument.PointValue, instrument.TickSize);

        var report = new StringBuilder();
        report.AppendLine($"# Percorso v4 — {symbol} {timeframe}m");
        report.AppendLine();
        report.AppendLine($"- ricerca: {(periods.SearchBroker is null ? "feed interno" : $"feed {periods.SearchBroker}")} {periods.SearchFrom:yyyy-MM-dd} → {periods.SearchTo:yyyy-MM-dd} " +
                          $"({history.Bars(timeframe).Length:N0} barre); scelta sui primi 2/3, conferma sull'ultimo terzo");
        report.AppendLine($"- prova sul broker: feed {Broker} {periods.HoldoutFrom:yyyy-MM-dd} → {periods.HoldoutTo:yyyy-MM-dd} ({holdout.Bars(timeframe).Length:N0} barre), mai vista dal percorso");
        report.AppendLine($"- costi {Broker}: spread {spreadValue} punti (mediana), swap long {swapSpec.LongPointsPerNight} short {swapSpec.ShortPointsPerNight} pt/notte; orologio al minuto");
        report.AppendLine($"- soglia di average trade (15% del range medio della barra): {threshold:N0} nella ricerca, {holdoutThreshold:N0} nella prova");
        report.AppendLine();
        Write(report.ToString());

        var summary = new List<string>();
        foreach (var engine in engines)
        {
            var definition = ResearchPathEngines.Definition(engine, symbol, timeframe);
            var job = template with { StrategyId = ResearchPathEngines.Container(engine) };
            var path = new ResearchPath(history, new PathOptions { AverageTradeThreshold = threshold, Log = Write });

            Write($"## {definition.Engine}\n");
            var result = path.Run(job, definition);

            var section = new StringBuilder();
            section.AppendLine($"{result.Runs:N0} simulazioni in {result.Elapsed.TotalMinutes:N1} minuti, conferma dal {path.ConfirmFromUtc:yyyy-MM-dd}.");
            section.AppendLine();
            section.AppendLine("| passo | chiave | prima | dopo | esito | motivo |");
            section.AppendLine("|---|---|---|---|---|---|");
            foreach (var decision in result.Decisions)
                section.AppendLine($"| {decision.Step} | {decision.Key} | {Show(decision.Before)} | {Show(decision.After)} | {(decision.Accepted ? "✔" : "·")} | {decision.Reason} |");
            section.AppendLine();

            string verdict;
            if (result.Abandoned is not null)
            {
                section.AppendLine($"**Abbandonato**: {result.Abandoned}.");
                verdict = $"{engine}: abbandonato ({result.Abandoned})";
            }
            else
            {
                var h = result.History!;
                section.AppendLine($"**Storia di ricerca**: {h.Trades} trade, netto {h.NetProfit:N0}, DD {h.MaxClosedTradeDrawdown:N0}, " +
                                   $"average trade {h.AverageTrade:N0}, UngerFit {ResearchCriteria.UngerFit(h.AverageTrade, threshold, h.MaxClosedTradeDrawdown):N2}.");
                section.AppendLine();
                section.AppendLine("| cancello | esito | dettaglio |");
                section.AppendLine("|---|---|---|");
                foreach (var gate in result.Gates)
                    section.AppendLine($"| {gate.Name} | {(gate.Passed ? "passa" : "**no**")} | {gate.Detail} |");
                section.AppendLine();

                // La prova sul broker: stessi parametri, feed e periodo che il percorso non ha visto.
                var probe = new SweepRunner(holdout).Run(job with { Parameters = result.Parameters });
                var windows = Windows(holdout, job, result.Parameters, 4);
                var fit = ResearchCriteria.UngerFit(probe.AverageTrade, holdoutThreshold, probe.MaxClosedTradeDrawdown);
                var ratio = probe.MaxClosedTradeDrawdown > 0m ? probe.NetProfit / probe.MaxClosedTradeDrawdown : 0m;
                section.AppendLine($"**Prova su {Broker}**: {probe.Trades} trade, netto {probe.NetProfit:N0}, DD {probe.MaxClosedTradeDrawdown:N0}, " +
                                   $"net/DD {ratio:N2}, average trade {probe.AverageTrade:N0} (soglia {holdoutThreshold:N0}), UngerFit {fit:N2}, " +
                                   $"finestre in utile {windows.Count(net => net > 0m)}/{windows.Count} ({string.Join(" / ", windows.Select(net => net.ToString("N0", CultureInfo.InvariantCulture)))}).");
                section.AppendLine();

                // I controlli dopo la prova (25/09/2026): la ricerca su FDAX 4h ha consegnato configurazioni
                // ottime in campione che i soli cancelli non fermavano — una viveva di un anno, una di pochi
                // trade decisi da un feed, una di un mercato solo.
                var checks = new List<PathGate>
                {
                    new("prova sul broker", probe.NetProfit > 0m && ratio >= 1m && probe.AverageTrade >= holdoutThreshold && windows.Count(net => net > 0m) >= 3,
                        $"net/DD {ratio:N2} (serve 1), average trade {probe.AverageTrade:N0} (soglia {holdoutThreshold:N0}), finestre {windows.Count(net => net > 0m)}/4 (servono 3)")
                };
                if (internalHoldout is not null)
                    checks.Add(TwoFeeds(job, result.Parameters, internalHoldout, holdout));
                checks.Add(Years(h, history, probe, holdout, timeframe, instrument));
                if (checks[0].Passed)
                    checks.Add(await OtherMarkets(job, result.Parameters, symbol, timeframe, dataFeed, settings));

                section.AppendLine("| controllo | esito | dettaglio |");
                section.AppendLine("|---|---|---|");
                foreach (var check in checks)
                    section.AppendLine($"| {check.Name} | {(check.Passed ? "passa" : "**no**")} | {check.Detail} |");
                section.AppendLine();
                section.AppendLine("Parametri: `" + string.Join(", ", result.Parameters
                    .Where(entry => entry.Key is not ("Symbol" or "TimeframeMinutes"))
                    .Select(entry => $"{entry.Key}={Convert.ToString(entry.Value, CultureInfo.InvariantCulture)}")) + "`");

                var failed = result.Gates.Where(gate => !gate.Passed).Select(gate => gate.Name)
                    .Concat(checks.Where(check => !check.Passed).Select(check => check.Name)).ToList();
                var candidate = failed.Count == 0 && checks.Any(check => check.Name == "altri mercati");
                verdict = $"{engine}: {(candidate ? "**CANDIDATA**" : "scartata")}; ricerca {h.Trades} trade netto {h.NetProfit:N0} avg {h.AverageTrade:N0}; " +
                          $"prova {probe.Trades} trade netto {probe.NetProfit:N0} net/DD {ratio:N2} avg {probe.AverageTrade:N0} fin {windows.Count(net => net > 0m)}/{windows.Count}; " +
                          (failed.Count == 0 ? "tutti i cancelli e i controlli" : "falliti: " + string.Join(", ", failed));
            }

            Write(section.ToString());
            report.AppendLine($"## {definition.Engine}").AppendLine().Append(section).AppendLine();
            summary.Add(verdict);
            SaveReport(symbol, timeframe, report, summary);
        }
    }

    /// <summary>
    /// Stessi parametri, stesso periodo, feed interno e feed del broker. Passa se guadagna su entrambi con
    /// net/DD almeno 1: una strategia che vive dei trade decisi da pochi punti di differenza fra i feed
    /// (LFHL su FDAX 4h, 25/09/2026: 75 trade su 305 esistevano su un feed solo) non e' una strategia.
    /// </summary>
    private static PathGate TwoFeeds(SweepJob job, IReadOnlyDictionary<string, object> parameters, SweepSeries internalFeed, SweepSeries brokerFeed)
    {
        var broker = brokerFeed.Between(internalFeed.StartUtc, internalFeed.EndUtc);
        var outcomes = new[] { internalFeed, broker }.AsParallel().AsOrdered()
            .Select(series => new SweepRunner(series).Run(job with { Parameters = parameters })).ToList();
        decimal Ratio(SweepOutcome outcome) => outcome.MaxClosedTradeDrawdown > 0m ? outcome.NetProfit / outcome.MaxClosedTradeDrawdown : 0m;
        var passed = outcomes.All(outcome => outcome.NetProfit > 0m && Ratio(outcome) >= 1m);
        return new PathGate("due feed", passed,
            $"{internalFeed.StartUtc:yyyy-MM} → {internalFeed.EndUtc:yyyy-MM}: interno {outcomes[0].Trades} trade {outcomes[0].NetProfit:N0} net/DD {Ratio(outcomes[0]):N2}, " +
            $"{Broker} {outcomes[1].Trades} trade {outcomes[1].NetProfit:N0} net/DD {Ratio(outcomes[1]):N2}");
    }

    /// <summary>
    /// Anno per anno su ricerca e prova insieme: almeno il 60% degli anni in utile e nessun anno oltre il 40%
    /// del netto. LFHL su FDAX 4h faceva meta' del netto nel solo 2022.
    /// </summary>
    private static PathGate Years(SweepOutcome history, SweepSeries historySeries, SweepOutcome probe, SweepSeries probeSeries,
        int timeframe, InstrumentSpec instrument)
    {
        var byYear = history.ClosedTrades.Concat(probe.ClosedTrades)
            .GroupBy(trade => trade.ExitDate.Year)
            .OrderBy(group => group.Key)
            .Select(group => (Year: group.Key, Net: group.Sum(trade => trade.NetProfit), Trades: group.Count()))
            .ToList();
        var total = byYear.Sum(year => year.Net);
        var profitable = byYear.Count(year => year.Net > 0m);
        var best = byYear.Count == 0 ? 0m : byYear.Max(year => year.Net);
        var share = total > 0m ? best / total : 1m;
        var passed = byYear.Count > 0 && profitable * 10 >= byYear.Count * 6 && share <= 0.40m;

        var thresholds = historySeries.Bars(timeframe).Concat(probeSeries.Bars(timeframe))
            .GroupBy(bar => bar.DateTime.Year)
            .ToDictionary(group => group.Key, group => ResearchCriteria.AverageTradeThreshold(group.ToList(), instrument.PointValue, instrument.TickSize));
        var detail = string.Join(" ", byYear.Select(year =>
            $"{year.Year}:{(thresholds.TryGetValue(year.Year, out var t) && t > 0m ? year.Net / year.Trades / t : 0m):N1}"));
        return new PathGate("anni", passed,
            $"{profitable}/{byYear.Count} anni in utile (serve 60%), anno migliore {share:P0} del netto (massimo 40%); average trade / soglia per anno: {detail}");
    }

    /// <summary>Mercati fratelli per il controllo "altri mercati": stessa famiglia, feed FTMO dal 2020.</summary>
    private static readonly string[][] Families =
    [
        ["@FDAX", "@NQ", "@ES", "@YM", "@FESX", "@FCE", "@Z", "@NIY"],
        ["@GC", "@SI", "@PL"],
        ["@CL", "@BRN"]
    ];

    private readonly Dictionary<string, SweepSeries?> _siblings = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// La stessa configurazione, senza i giorni esclusi (scelti sul mercato della ricerca), sui mercati della
    /// stessa famiglia, feed e costi FTMO dal novembre 2020. Passa se almeno meta' guadagna con net/DD ≥ 1:
    /// un'ipotesi che vale su un mercato solo e' con ogni probabilita' un adattamento a quel mercato.
    /// </summary>
    private async Task<PathGate> OtherMarkets(SweepJob job, IReadOnlyDictionary<string, object> parameters, string symbol, int timeframe,
        PiootooDataFeedService dataFeed, PiootooSettings settings)
    {
        var family = Families.FirstOrDefault(members => members.Contains(symbol, StringComparer.OrdinalIgnoreCase));
        if (family is null)
            return new PathGate("altri mercati", false, "nessuna famiglia di mercati per questo simbolo: controllo da fare a mano");

        var spreads = SpreadTable.Load(settings.GetSpreadPath(), Broker, SpreadStatistic.Median, SpreadResolution.PerSymbol).Points;
        var swaps = SwapTable.Load(settings.GetSwapPath(), Broker).Specs;
        var results = new List<string>();
        var good = 0;
        var measured = 0;
        foreach (var sibling in family.Where(member => !string.Equals(member, symbol, StringComparison.OrdinalIgnoreCase)))
        {
            var key = sibling.TrimStart('@');
            if (!spreads.TryGetValue(key, out var spread) || !swaps.TryGetValue(key, out var swap)) continue;
            if (!_siblings.TryGetValue(sibling, out var series))
            {
                series = File.Exists(Path.Combine(RepositoryPath, "datafeed-external", Broker, $"{sibling}_1.json"))
                    ? await SweepSeries.LoadAsync(dataFeed, sibling, [timeframe, 1], Utc(2020, 11, 9), End, Broker, warmupDays: 30d)
                    : null;
                _siblings[sibling] = series;
            }

            if (series is null) continue;
            var siblingParameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase) { ["Symbol"] = sibling };
            foreach (var calendar in new[] { "SkipDay", "NotEntryDayLong", "NotEntryDayShort" })
                if (siblingParameters.ContainsKey(calendar)) siblingParameters[calendar] = -1;

            var outcome = new SweepRunner(series).Run(job with
            {
                Parameters = siblingParameters,
                SpreadPoints = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [key] = spread },
                Swap = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase) { [key] = swap }
            });
            var ratio = outcome.MaxClosedTradeDrawdown > 0m ? outcome.NetProfit / outcome.MaxClosedTradeDrawdown : 0m;
            measured++;
            if (outcome.NetProfit > 0m && ratio >= 1m) good++;
            results.Add($"{sibling} {ratio:N2}");
        }

        return new PathGate("altri mercati", measured > 0 && good * 2 >= measured,
            $"{good}/{measured} con net/DD ≥ 1 (serve meta'): {string.Join(", ", results)}");
    }

    private static List<decimal> Windows(SweepSeries series, SweepJob job, IReadOnlyDictionary<string, object> parameters, int count)
    {
        var span = (series.EndUtc - series.StartUtc) / count;
        return Enumerable.Range(0, count)
            .Select(i => new SweepRunner(series.Between(series.StartUtc + span * i, i == count - 1 ? series.EndUtc : series.StartUtc + span * (i + 1)))
                .Run(job with { Parameters = parameters }).NetProfit)
            .ToList();
    }

    /// <summary>Il resoconto si riscrive a ogni motore: un run interrotto lascia quello che ha fatto.</summary>
    private static void SaveReport(string symbol, int timeframe, StringBuilder report, List<string> summary)
    {
        var folder = Path.Combine(RepositoryPath, "ricerca", "percorso");
        Directory.CreateDirectory(folder);
        var text = new StringBuilder(report.ToString());
        text.AppendLine("## Riepilogo").AppendLine();
        foreach (var line in summary) text.AppendLine($"- {line}");
        File.WriteAllText(Path.Combine(folder, $"{symbol.TrimStart('@').ToLowerInvariant()}-{timeframe}.md"), text.ToString(), Encoding.UTF8);
    }

    /// <summary>Un valore del passo: le leve di un trigger scelto insieme sono un dizionario.</summary>
    private static string Show(object? value) => value switch
    {
        null => "-",
        IReadOnlyDictionary<string, object> levers => string.Join(", ", levers.Select(entry =>
            $"{entry.Key}={Convert.ToString(entry.Value, CultureInfo.InvariantCulture)}")),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private void Write(string text)
    {
        output.WriteLine(text);
        Console.WriteLine(text);
    }

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
