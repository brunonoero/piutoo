using System.Globalization;
using System.Text;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT5DAVStrategies.Engines;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>La riconciliazione delle PT5DAV con i trade del simulatore Python.</b> Esegue ogni strategia
/// di un simbolo con il motore vero (<see cref="SweepRunner"/>, orologio al minuto, come la ricerca
/// risolve stop e target) sul feed interno, e la confronta trade per trade con
/// <c>piootoo-repository/PT5DAV/trades_per_strategia/</c>, fonte <c>storia</c>.
///
/// <para><b>Perche' il feed interno.</b> Su NQ e' la stessa serie della ricerca: gli ingressi
/// BIAS_BO cadono al tick sui massimi del nostro <c>@NQ_240</c> (2.506 su 2.511). Le condizioni del
/// confronto sono quelle della ricerca e non quelle di un conto: nessuna commissione, nessuno
/// spread, <c>RejectWrongSideLevels</c> spento (la ricerca riempie all'apertura uno stop gia'
/// scavalcato), overnight e overweek permessi al piano. Quello che resta diverso si vede nei numeri.</para>
///
/// <para>Scrive un resoconto in <c>piootoo-repository/PT5DAV/verifica/</c>. Gira solo con
/// <c>PIOOTOO_STUDI=1</c>; <c>PIOOTOO_PT5DAV_DA</c>/<c>PIOOTOO_PT5DAV_A</c> (yyyy-MM-dd) cambiano il periodo.</para>
/// </summary>
public sealed class Pt5DavParityStudy(ITestOutputHelper output)
{
    private const string RepositoryPath = @"C:\piootoo-dev\piootoo-repository";

    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public Task NqPilotMatchesThePythonTrades() => RunAsync("NQ", broker: null, source: "storia",
        ReadDate("PIOOTOO_PT5DAV_DA", new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
        ReadDate("PIOOTOO_PT5DAV_A", new DateTime(2025, 5, 30, 0, 0, 0, DateTimeKind.Utc)));

    /// <summary>
    /// Il periodo che la ricerca non ha mai visto (27/08/2025 → 09/09/2026), sul minuto del broker
    /// FTMO, contro i trade fonte <c>broker</c>. Qui i prezzi sono veri: e' la verifica delle
    /// soglie in percentuale, che sul feed interno (serie continua aggiustata) scattano una barra
    /// dopo.
    /// </summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public Task NqPilotMatchesThePythonTradesOnFtmo() => RunAsync("NQ", broker: "FTMO", source: "broker",
        new DateTime(2025, 8, 27, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));

    /// <summary>
    /// Gli altri simboli sullo stesso periodo e sullo stesso archivio FTMO. Il feed interno non si
    /// usa qui: per questi simboli copre pochi timeframe e non e' detto che sia la serie della ricerca.
    /// </summary>
    [Theory]
    [Trait("Category", ResearchStudy.Category)]
    [InlineData("BP")]
    [InlineData("BTC")]
    [InlineData("CC")]
    [InlineData("CL")]
    [InlineData("ES")]
    [InlineData("FDAX")]
    [InlineData("GC")]
    [InlineData("KC")]
    [InlineData("YM")]
    public Task OtherSymbolsMatchThePythonTradesOnFtmo(string symbol) => RunAsync(symbol, broker: "FTMO", source: "broker",
        new DateTime(2025, 8, 27, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));

    /// <summary>
    /// Ricostruisce dal minuto gli aggregati FTMO di BP e BTC con il calendario corrente. Serve dopo
    /// il 24/09/2026, quando il calendario dei due simboli e' passato agli orari del CFD: gli aggregati
    /// sono cache derivata e quelli su disco erano mascherati con la pausa CME (e, per BTC, senza fine
    /// settimana). Si fa in-process e non dal server perche' il server acceso ha il calendario vecchio
    /// in memoria e riavviarlo fermerebbe le sessioni live.
    /// </summary>
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task RebuildFtmoAggregatesForCfdCalendars()
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var store = new ExternalDatafeedStore(new PiootooSettings
        {
            BasePath = RepositoryPath,
            ExternalRepositoryPath = @"[BasePath]\datafeed-external"
        });

        foreach (var symbol in new[] { "@BP", "@BTC" })
        {
            var response = await store.RebuildFromMinutesAsync("FTMO", symbol, [15, 30, 60, 240]);
            foreach (var stream in response.Streams)
            {
                output.WriteLine(
                    $"{stream.Symbol} {stream.TimeframeMinutes}m: barre {stream.BarsBefore} -> {stream.BarsAfter}, {stream.Grid}");
            }
        }
    }

    private async Task RunAsync(string symbol, string? broker, string source, DateTime start, DateTime end)
    {
        if (ResearchStudy.IsSkipped(output)) return;

        var strategies = StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true)
            .Where(d => d.Id.StartsWith($"PT5DAV_{symbol}_", StringComparison.Ordinal))
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(strategies);

        var timeframes = strategies.Select(d => d.TimeframeMinutes).Append(1).Distinct().Order().ToArray();
        var settings = new PiootooSettings
        {
            BasePath = RepositoryPath,
            RepositoryPath = @"[BasePath]\datafeed",
            ExternalRepositoryPath = @"[BasePath]\datafeed-external"
        };
        var series = await SweepSeries.LoadAsync(
            new PiootooDataFeedService(new DatafeedCatalog(settings)), $"@{symbol}", timeframes, start, end, broker, warmupDays: 150d);
        var runner = new SweepRunner(series);
        var rome = new SessionClock("Europe/Rome");
        var label = broker is null ? symbol : $"{symbol}-{broker}";

        var report = new StringBuilder();
        report.AppendLine($"# Riconciliazione PT5DAV {symbol} — feed {broker ?? "interno"}, {start:yyyy-MM-dd} → {end:yyyy-MM-dd}");
        report.AppendLine();
        report.AppendLine("Motore vero (SweepRunner) con orologio al minuto, senza costi, `RejectWrongSideLevels` spento, overnight e overweek permessi. " +
                          $"Confronto con `trades_per_strategia/<codice>.csv`, fonte `{source}`, sullo stesso periodo. Un trade corrisponde se ha " +
                          "la stessa barra d'ingresso e lo stesso lato. Punti = somma di (uscita − ingresso) × lato.");
        report.AppendLine();
        report.AppendLine("| strategia | codice | Python | nostri | in comune | % Python | % nostri | Δ ingresso mediano | stessa uscita | punti Python | punti nostri | punti comuni Py/nostri |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var definition in strategies)
        {
            var strategy = StrategyFactory.CreateStrategy(definition.Id, definition.Symbol, definition.TimeframeMinutes)!;
            var code = ((Pt5DavEngineBase)strategy).ResearchCode;
            var python = LoadPython(code, source, rome, start, end);

            var outcome = runner.Run(new SweepJob(definition.Id)
            {
                InitialCapital = 10_000_000m,
                CommissionPerContract = 0m,
                ClockTimeframeMinutes = 1,
                RejectWrongSideLevels = false,
                Holding = AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true }
            });

            var ours = outcome.ClosedTrades
                .Where(t => t.EntryDate >= start && t.EntryDate < end)
                .Select(t => new Trade(
                    BarStart(t.EntryDate, definition.TimeframeMinutes, rome),
                    t.Direction == SignalType.Buy ? 1 : -1,
                    t.EntryPrice, t.ExitPrice,
                    BarStart(t.ExitDate.AddMinutes(-1), definition.TimeframeMinutes, rome),
                    t.ExitReason.ToString()))
                .ToList();

            var byKey = python.GroupBy(t => (t.EntryBar, t.Side)).ToDictionary(g => g.Key, g => g.First());
            var matched = ours.Where(t => byKey.ContainsKey((t.EntryBar, t.Side))).ToList();
            var entryDiffs = matched.Select(t => Math.Abs(t.Entry - byKey[(t.EntryBar, t.Side)].Entry)).Order().ToList();
            var sameExit = matched.Count(t => t.ExitBar == byKey[(t.EntryBar, t.Side)].ExitBar);
            var pointsPython = python.Sum(t => t.Points);
            var pointsOurs = ours.Sum(t => t.Points);
            var matchedPython = matched.Sum(t => byKey[(t.EntryBar, t.Side)].Points);
            var matchedOurs = matched.Sum(t => t.Points);

            var line =
                $"| {definition.Id} | {code} | {python.Count} | {ours.Count} | {matched.Count} | " +
                $"{Percent(matched.Count, python.Count)} | {Percent(matched.Count, ours.Count)} | " +
                $"{(entryDiffs.Count > 0 ? entryDiffs[entryDiffs.Count / 2] : 0m):0.##} | {Percent(sameExit, matched.Count)} | " +
                $"{pointsPython:N1} | {pointsOurs:N1} | {matchedPython:N1} / {matchedOurs:N1} |";
            report.AppendLine(line);
            output.WriteLine(line);

            WriteDetail(label, definition.Id, python, ours);
        }

        var folder = Path.Combine(RepositoryPath, "PT5DAV", "verifica");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, $"riconciliazione-{label}.md"), report.ToString(), Encoding.UTF8);
    }

    private sealed record Trade(DateTime EntryBar, int Side, decimal Entry, decimal Exit, DateTime ExitBar, string Reason)
    {
        public decimal Points => (Exit - Entry) * Side;
    }

    private static List<Trade> LoadPython(string code, string source, SessionClock rome, DateTime start, DateTime end)
    {
        var path = Path.Combine(RepositoryPath, "PT5DAV", "trades_per_strategia", code + ".csv");
        var lines = File.ReadAllLines(path);
        var header = lines[0].Split(',');
        int Col(string name) => Array.IndexOf(header, name);
        var (fonte, entryTime, exitTime, side, entryPrice, exitPrice, reason) = (
            Col("fonte"), Col("entry_time"), Col("exit_time"), Col("side"), Col("entry_price"), Col("exit_price"), Col("exit_reason"));

        var trades = new List<Trade>();
        foreach (var raw in lines.Skip(1))
        {
            var f = raw.Split(',');
            if (f[fonte] != source) continue;

            var entry = rome.ToUtc(ParseResearchTime(f[entryTime]));
            if (entry < start || entry >= end) continue;

            trades.Add(new Trade(
                entry,
                f[side] == "L" ? 1 : -1,
                decimal.Parse(f[entryPrice], CultureInfo.InvariantCulture),
                decimal.Parse(f[exitPrice], CultureInfo.InvariantCulture),
                rome.ToUtc(ParseResearchTime(f[exitTime])),
                f[reason]));
        }

        return trades;
    }

    /// <summary>
    /// Un orario dei CSV della ricerca. pandas scrive la sola data quando tutta la colonna cade a
    /// mezzanotte (le uscite BIASW di NQ, sempre giovedi' alle 00:00): e' la mezzanotte, non un errore.
    /// </summary>
    private static DateTime ParseResearchTime(string text) =>
        DateTime.ParseExact(text, ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None);

    /// <summary>
    /// L'apertura della barra della strategia che contiene l'istante: la griglia e' ancorata alla
    /// mezzanotte di Roma, come il feed e la ricerca.
    /// </summary>
    private static DateTime BarStart(DateTime instantUtc, int timeframeMinutes, SessionClock rome)
    {
        var local = rome.ToSessionTime(instantUtc);
        var minutes = (local - local.Date).TotalMinutes;
        var floored = local.Date.AddMinutes(Math.Floor(minutes / timeframeMinutes) * timeframeMinutes);
        return rome.ToUtc(floored);
    }

    private void WriteDetail(string symbol, string id, List<Trade> python, List<Trade> ours)
    {
        var folder = Path.Combine(RepositoryPath, "PT5DAV", "verifica", symbol);
        Directory.CreateDirectory(folder);
        var sb = new StringBuilder("fonte,ingresso_utc,lato,prezzo_ingresso,uscita_utc,prezzo_uscita,motivo\n");
        foreach (var (source, list) in new[] { ("python", python), ("nostro", ours) })
        foreach (var t in list.OrderBy(t => t.EntryBar))
        {
            sb.Append(source).Append(',').Append(t.EntryBar.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
              .Append(',').Append(t.Side > 0 ? "L" : "S")
              .Append(',').Append(t.Entry.ToString(CultureInfo.InvariantCulture))
              .Append(',').Append(t.ExitBar.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
              .Append(',').Append(t.Exit.ToString(CultureInfo.InvariantCulture))
              .Append(',').Append(t.Reason).Append('\n');
        }

        File.WriteAllText(Path.Combine(folder, id + ".csv"), sb.ToString(), Encoding.UTF8);
    }

    private static string Percent(int part, int whole) =>
        whole == 0 ? "–" : (100.0 * part / whole).ToString("0", CultureInfo.InvariantCulture) + "%";

    private static DateTime ReadDate(string variable, DateTime fallback) =>
        Environment.GetEnvironmentVariable(variable) is { Length: > 0 } text
            ? DateTime.SpecifyKind(DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Utc)
            : fallback;
}
