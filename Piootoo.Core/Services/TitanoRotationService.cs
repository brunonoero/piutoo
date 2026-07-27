using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services;

/// <summary>Calcola rotazioni riproducibili esclusivamente dai trade persistiti.</summary>
public sealed class TitanoRotationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly ConcurrentDictionary<string, object> Gates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Cache dei manifest già letti, invalidata sul timestamp del file.
    ///
    /// <see cref="Resolve"/> viene invocato una volta per barra da ogni sessione live e a ogni
    /// polling di ogni account: senza cache ogni chiamata rileggeva e deserializzava l'intero
    /// manifest da disco, più l'enumerazione della cartella degli override.
    /// </summary>
    private static readonly ConcurrentDictionary<string, (DateTime WrittenAtUtc, TitanoRotationManifest Manifest)>
        ManifestCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly WorkspaceService _workspaces;

    public TitanoRotationService(WorkspaceService workspaces) => _workspaces = workspaces;

    /// <summary>
    /// Codici di esecuzione (ITradingStrategy.Name) delle strategie del masterfilter.
    ///
    /// Il masterfilter contiene Id di classe (<c>Easy_218_GC_60</c>) mentre i trade persistiti
    /// portano il codice di esecuzione (<c>TOP_UA_218</c>). Confrontarli direttamente — come
    /// faceva la versione precedente — significa non trovare mai un trade per nessuna strategia:
    /// tutte le metriche restano a zero e la rotazione disabilita tutto per sempre.
    /// Vedi docs/PROGETTO.md §3.2.
    /// </summary>
    private string[] GetMasterExecutionCodes(string workspaceId) =>
        StrategyCatalog.ResolveExecutionCodes(
            _workspaces.GetMasterFilter(workspaceId).StrategiesFilter.Where(x => !string.IsNullOrWhiteSpace(x)));

    public TitanoRotationManifest Run(TitanoRotationRequest request)
    {
        Validate(request);
        var backtestPath = _workspaces.GetBacktestPath(request.WorkspaceId, request.BacktestFolder);
        if (!Directory.Exists(backtestPath)) throw new DirectoryNotFoundException($"Backtest '{request.BacktestFolder}' non trovato.");
        var tradesPath = Path.Combine(backtestPath, TradingPersistenceSchema.TradesFileName);
        if (!File.Exists(tradesPath)) throw new FileNotFoundException("trades.json non trovato nel backtest.", tradesPath);

        var sourceBytes = File.ReadAllBytes(tradesPath);
        var sourceHash = Sha(sourceBytes);
        var master = GetMasterExecutionCodes(request.WorkspaceId);
        var masterHash = Sha(Encoding.UTF8.GetBytes(string.Join("\n", master)));
        var configHash = Sha(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonOptions)));
        var runId = $"{request.RotationPeriod.ToString().ToLowerInvariant()}-{sourceHash[..12]}-{masterHash[..8]}-{configHash[..12]}";
        var runPath = Path.Combine(backtestPath, "titano", runId);
        var manifestPath = Path.Combine(runPath, "manifest.json");

        lock (Gates.GetOrAdd(manifestPath, _ => new object()))
        {
            if (File.Exists(manifestPath))
            {
                var existing = ReadManifest(manifestPath);
                EnsureHtmlReport(runPath, existing);
                return existing;
            }
            var trades = JsonSerializer.Deserialize<List<PersistedTrade>>(sourceBytes, JsonOptions) ?? [];
            ValidateTrades(trades);
            var periods = BuildPeriods(request).ToList();
            var decisions = BuildDecisions(request, periods, trades, master, masterHash).ToList();
            var manifest = new TitanoRotationManifest
            {
                RunId = runId,
                Config = request,
                SourceTradesSha256 = sourceHash,
                MasterFilterHash = masterHash,
                ConfigSha256 = configHash,
                GeneratedAtUtc = DateTime.UtcNow,
                Periods = decisions,
                OriginalEquity = BuildOriginalEquity(request, trades, master),
                FilteredEquity = BuildEquity(request, trades, decisions, master),
                WalkForward = BuildWalkForward(request, periods, trades, decisions, master)
            };

            Directory.CreateDirectory(runPath);
            foreach (var decision in decisions)
                WriteNewAtomic(Path.Combine(runPath, $"period-{decision.PeriodId}.json"), decision);
            WriteNewAtomic(manifestPath, manifest);
            EnsureHtmlReport(runPath, manifest);
            return manifest;
        }
    }

    public IReadOnlyList<TitanoRunInfo> ListRuns(string workspaceId, string backtestFolder)
    {
        var root = Path.Combine(_workspaces.GetBacktestPath(workspaceId, backtestFolder), "titano");
        if (!Directory.Exists(root)) return [];
        return Directory.EnumerateFiles(root, "manifest.json", SearchOption.AllDirectories)
            .Select(path => ReadManifest(path))
            .Select(x => new TitanoRunInfo
            {
                RunId = x.RunId, WorkspaceId = workspaceId, BacktestFolder = backtestFolder,
                Status = TitanoRunStatus.Completed, GeneratedAtUtc = x.GeneratedAtUtc,
                ManifestPath = Path.Combine(root, x.RunId, "manifest.json"), PeriodCount = x.Periods.Count
            }).OrderByDescending(x => x.GeneratedAtUtc).ToArray();
    }

    public TitanoRotationManifest Get(string workspaceId, string backtestFolder, string runId)
    {
        var safeRunId = SafeSegment(runId);
        var path = Path.Combine(_workspaces.GetBacktestPath(workspaceId, backtestFolder), "titano", safeRunId, "manifest.json");
        if (!File.Exists(path)) throw new FileNotFoundException($"Run Titano '{runId}' non trovato.");

        // Un manifest è immutabile una volta scritto (il runId è l'hash dei suoi input), ma la
        // cartella può ricevere nuovi file di hard-stop-reset: la chiave di cache tiene conto sia
        // del manifest sia dell'ultima modifica della directory.
        var writtenAtUtc = File.GetLastWriteTimeUtc(path);
        var directory = Path.GetDirectoryName(path)!;
        var directoryTouchedAtUtc = Directory.GetLastWriteTimeUtc(directory);
        var stamp = writtenAtUtc > directoryTouchedAtUtc ? writtenAtUtc : directoryTouchedAtUtc;

        if (ManifestCache.TryGetValue(path, out var cached) && cached.WrittenAtUtc == stamp)
        {
            return cached.Manifest;
        }

        var manifest = ReadManifest(path);
        manifest.HardStopResets.AddRange(ReadResets(directory));
        ManifestCache[path] = (stamp, manifest);
        return manifest;
    }

    public string GetHtmlReportPath(string workspaceId, string backtestFolder, string runId)
    {
        var safeRunId = SafeSegment(runId);
        var runPath = Path.Combine(
            _workspaces.GetBacktestPath(workspaceId, backtestFolder), "titano", safeRunId);
        var manifestPath = Path.Combine(runPath, "manifest.json");
        if (!File.Exists(manifestPath)) throw new FileNotFoundException($"Run Titano '{runId}' non trovato.");
        EnsureHtmlReport(runPath, ReadManifest(manifestPath));
        return Path.Combine(runPath, "report.html");
    }

    private const string EquityChartMarker = "id=\"equityComparisonTable\"";

    private static void EnsureHtmlReport(string runPath, TitanoRotationManifest manifest)
    {
        var path = Path.Combine(runPath, "report.html");
        if (File.Exists(path) && File.ReadAllText(path).Contains(EquityChartMarker, StringComparison.Ordinal))
            return;

        static string H(object? value) =>
            WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture)) ?? string.Empty;
        static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
        static string Percent(decimal value) => value.ToString("P2", CultureInfo.InvariantCulture);

        var originalEquity = ResolveOriginalEquity(runPath, manifest);
        var filteredEquity = manifest.FilteredEquity;
        var initialCapital = manifest.Config.InitialCapital;
        var originalFinal = originalEquity.LastOrDefault()?.Balance ?? initialCapital;
        var filteredFinal = filteredEquity.LastOrDefault()?.Balance ?? initialCapital;
        var originalProfit = originalFinal - initialCapital;
        var filteredProfit = filteredFinal - initialCapital;
        var originalMaxDrawdown = CalculateMaxDrawdown(originalEquity, initialCapital);
        var filteredMaxDrawdown = CalculateMaxDrawdown(filteredEquity, initialCapital);
        var enabledStates = manifest.Periods.SelectMany(x => x.Strategies).Count(x => x.Enabled);
        var equitySeriesJson = JsonSerializer.Serialize(
            BuildEquityComparisonSeries(initialCapital, manifest.Config.StartUtc, originalEquity, filteredEquity),
            JsonOptions);
        var html = new StringBuilder();
        html.Append("""
            <!doctype html><html><head><meta charset="utf-8"><title>Report Titano</title>
            <style>
            body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f3f5f8;color:#172033}
            main{max-width:1400px;margin:auto;padding:28px}.cards{display:flex;gap:14px;flex-wrap:wrap}
            .card{background:white;border-radius:9px;padding:16px;min-width:180px;box-shadow:0 1px 5px #ccd2dc}
            .value{font-size:24px;font-weight:650;margin-top:6px}h1,h2{margin:0 0 18px}
            h2{margin-top:28px}table{width:100%;border-collapse:collapse;background:white;font-size:13px}
            th,td{padding:9px 10px;border-bottom:1px solid #e3e7ed;text-align:left;white-space:nowrap}
            th{background:#202b40;color:white;position:sticky;top:0}.enabled{color:#08783e;font-weight:600}
            .disabled,.hardstopped{color:#b42318;font-weight:600}.reduced{color:#a15c00;font-weight:600}
            .scroll{overflow:auto;max-height:520px;border-radius:8px;box-shadow:0 1px 5px #ccd2dc}
            canvas{width:100%;height:520px;background:white;border-radius:8px;box-shadow:0 1px 5px #ccd2dc}
            .legend{display:flex;flex-wrap:wrap;gap:12px;margin-top:14px}.legend span{display:inline-flex;align-items:center;gap:6px;font-size:13px}
            .swatch{width:14px;height:3px;display:inline-block}.muted{color:#5c677d;font-size:13px;margin:0 0 12px}
            .good{color:#08783e}.bad{color:#b42318}
            </style></head><body><main>
            """);
        html.Append($"<h1>Report Titano</h1><p>Run <strong>{H(manifest.RunId)}</strong> · generato {H(manifest.GeneratedAtUtc.ToString("u"))}</p>");
        html.Append("<section class=\"cards\">");
        html.Append($"<div class=\"card\">Capitale finale filtrato<div class=\"value\">{H(Money(filteredFinal))}</div></div>");
        html.Append($"<div class=\"card\">Profitto netto filtrato<div class=\"value\">{H(Money(filteredProfit))}</div></div>");
        html.Append($"<div class=\"card\">Periodi<div class=\"value\">{manifest.Periods.Count}</div></div>");
        html.Append($"<div class=\"card\">Stati abilitati<div class=\"value\">{enabledStates}</div></div>");
        html.Append($"<div class=\"card\">Trade originali<div class=\"value\">{originalEquity.Count}</div></div>");
        html.Append($"<div class=\"card\">Trade filtrati<div class=\"value\">{filteredEquity.Count}</div></div></section>");

        html.Append("<h2>Confronto equity</h2>");
        html.Append("<div class=\"card\" style=\"margin-top:18px;padding:18px;background:white;border-radius:9px;box-shadow:0 1px 5px #ccd2dc\">");
        html.Append("<table id=\"equityComparisonTable\"><tr><th>Metrica</th><th>Backtesting</th><th>Titano</th></tr>");
        html.Append($"<tr><td>Capitale finale</td><td>{H(Money(originalFinal))}</td><td class=\"{(filteredFinal >= originalFinal ? "good" : "bad")}\">{H(Money(filteredFinal))}</td></tr>");
        html.Append($"<tr><td>Profitto netto</td><td>{H(Money(originalProfit))}</td><td class=\"{(filteredProfit >= originalProfit ? "good" : "bad")}\">{H(Money(filteredProfit))}</td></tr>");
        html.Append($"<tr><td>Max drawdown</td><td>{H(Percent(originalMaxDrawdown))}</td><td>{H(Percent(filteredMaxDrawdown))}</td></tr>");
        html.Append($"<tr><td>Trade contabilizzati</td><td>{originalEquity.Count}</td><td>{filteredEquity.Count}</td></tr></table></div>");
        html.Append("<div class=\"card\" style=\"margin-top:18px;padding:18px;background:white;border-radius:9px;box-shadow:0 1px 5px #ccd2dc\">");
        html.Append("<h2>Equity trade-level — originale vs filtrato Titano</h2>");
        html.Append("<p class=\"muted\">Curva cumulativa sui trade chiusi del master filter: originale (100% allocazione, senza costi Titano) vs filtrata (allocazione e costi simulati).</p>");
        html.Append("<canvas id=\"equityComparisonChart\" data-drawdown-bars=\"true\" width=\"1400\" height=\"560\"></canvas>");
        html.Append("<div id=\"equityComparisonLegend\" class=\"legend\"></div></div>");
        html.Append("<h2>Decisioni per periodo</h2><div class=\"scroll\"><table><thead><tr>" +
                    "<th>Periodo effettivo</th><th>Strategia</th><th>Stato</th><th>Allocazione</th>" +
                    "<th>Score</th><th>Voti</th><th>Return breve</th><th>Return lungo</th>" +
                    "<th>Drawdown</th><th>Motivo</th></tr></thead><tbody>");
        foreach (var period in manifest.Periods)
        foreach (var state in period.Strategies)
        {
            var css = state.State.ToString().ToLowerInvariant();
            html.Append($"<tr><td>{H(period.EffectiveFromUtc.ToString("u"))} – {H(period.EffectiveToUtc.ToString("u"))}</td>" +
                        $"<td>{H(state.StrategyCode)}</td><td class=\"{css}\">{H(state.State)}</td>" +
                        $"<td>{H(Percent(state.AllocationMultiplier))}</td><td>{H(state.Score.ToString("F3", CultureInfo.InvariantCulture))}</td>" +
                        $"<td>{state.PassingFilters}/{state.TotalFilters}</td><td>{H(Percent(state.Metrics.ShortReturn))}</td>" +
                        $"<td>{H(Percent(state.Metrics.LongReturn))}</td><td>{H(Percent(state.Metrics.CurrentDrawdown))}</td>" +
                        $"<td>{H(state.Reason)}</td></tr>");
        }
        html.Append("</tbody></table></div><h2>Walk-forward</h2><div class=\"scroll\"><table><thead><tr>" +
                    "<th>Periodo</th><th>Calibrazione</th><th>Valutazione</th><th>Profitto IS</th><th>Profitto OOS</th><th>Avviso</th>" +
                    "</tr></thead><tbody>");
        foreach (var item in manifest.WalkForward)
            html.Append($"<tr><td>{H(item.EvaluationPeriodId)}</td><td>{H(item.CalibrationFromUtc.ToString("u"))} – {H(item.CalibrationToUtc.ToString("u"))}</td>" +
                        $"<td>{H(item.EvaluationFromUtc.ToString("u"))} – {H(item.EvaluationToUtc.ToString("u"))}</td>" +
                        $"<td>{H(Money(item.InSampleNetProfit))}</td><td>{H(Money(item.OutOfSampleNetProfit))}</td>" +
                        $"<td>{(item.InSampleOnlyImprovementWarning ? "Migliora solo in-sample" : "")}</td></tr>");
        html.Append("</tbody></table></div>");
        html.Append("<script>");
        html.Append($"const equityComparisonSeries = {equitySeriesJson};");
        html.Append("""
            (function drawEquityComparisonChart() {
              const canvas = document.getElementById('equityComparisonChart');
              const legend = document.getElementById('equityComparisonLegend');
              if (!canvas || !legend) return;
              if (!equityComparisonSeries.length) {
                legend.innerHTML = '<span>Nessun trade master nel run: curve piatte al capitale iniziale.</span>';
                return;
              }
              const points = equityComparisonSeries.map(p => ({...p, time: new Date(p.t).getTime()}));
              const ctx = canvas.getContext('2d');
              const pad = {left: 74, right: 74, top: 28, bottom: 54};
              const vals = points.flatMap(p => [p.original, p.filtered]);
              const min = Math.min(...vals), max = Math.max(...vals);
              const yMin = min === max ? min - 1 : min;
              const yMax = min === max ? max + 1 : max;
              const minTime = Math.min(...points.map(p => p.time));
              const maxTime = Math.max(...points.map(p => p.time));
              const x = t => pad.left + ((t - minTime) / Math.max(1, maxTime - minTime)) * (canvas.width - pad.left - pad.right);
              const y = v => canvas.height - pad.bottom - ((v - yMin) / Math.max(1, yMax - yMin)) * (canvas.height - pad.top - pad.bottom);
              ctx.clearRect(0, 0, canvas.width, canvas.height);
              ctx.strokeStyle = '#ccd2dc'; ctx.lineWidth = 1; ctx.fillStyle = '#5c677d'; ctx.font = '12px Segoe UI,Arial,sans-serif';
              for (let i = 0; i <= 5; i++) {
                const yy = pad.top + i * (canvas.height - pad.top - pad.bottom) / 5;
                ctx.beginPath(); ctx.moveTo(pad.left, yy); ctx.lineTo(canvas.width - pad.right, yy); ctx.stroke();
                ctx.fillText((yMax - i * (yMax - yMin) / 5).toFixed(0), 8, yy + 4);
              }
              const maxDd = Math.max(0, ...points.flatMap(p => [p.originalDrawdown, p.filteredDrawdown]));
              const plotHeight = canvas.height - pad.top - pad.bottom;
              const slotWidth = (canvas.width - pad.left - pad.right) / Math.max(1, points.length);
              const barWidth = Math.max(1, slotWidth * 0.36);
              points.forEach(p => {
                const originalHeight = maxDd === 0 ? 0 : p.originalDrawdown / maxDd * plotHeight;
                const filteredHeight = maxDd === 0 ? 0 : p.filteredDrawdown / maxDd * plotHeight;
                const xx = x(p.time);
                ctx.fillStyle = 'rgba(239,68,68,0.30)';
                ctx.fillRect(xx - barWidth, canvas.height - pad.bottom - originalHeight, barWidth, originalHeight);
                ctx.fillStyle = 'rgba(245,158,11,0.34)';
                ctx.fillRect(xx, canvas.height - pad.bottom - filteredHeight, barWidth, filteredHeight);
              });
              ctx.fillStyle = '#5c677d';
              for (let i = 0; i <= 5; i++) {
                const yy = pad.top + i * plotHeight / 5;
                ctx.fillText((maxDd * (5 - i) / 5 * 100).toFixed(1) + '%', canvas.width - pad.right + 8, yy + 4);
              }
              function line(key, color, width) {
                ctx.strokeStyle = color; ctx.lineWidth = width; ctx.beginPath();
                points.forEach((p, i) => { const xx = x(p.time); const yy = y(p[key]); if (i === 0) ctx.moveTo(xx, yy); else ctx.lineTo(xx, yy); });
                ctx.stroke();
              }
              line('original', '#1d7afc', 2.5);
              line('filtered', '#08783e', 2.5);
              legend.innerHTML = '<span><i class="swatch" style="background:#1d7afc"></i>Equity originale (master)</span><span><i class="swatch" style="background:#08783e"></i>Equity filtrata Titano</span><span><i class="swatch" style="background:rgba(239,68,68,.6)"></i>DD backtest</span><span><i class="swatch" style="background:rgba(245,158,11,.7)"></i>DD Titano (scala destra)</span>';
            })();
            </script></main></body></html>
            """);

        var tempPath = path + $".{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, html.ToString(), new UTF8Encoding(false));
        File.Move(tempPath, path, overwrite: true);
    }

    public TitanoHardStopReset ResetHardStop(
        string workspaceId, string backtestFolder, string runId, TitanoHardStopResetRequest request)
    {
        RequireUtc(request.RequestedAtUtc, nameof(request.RequestedAtUtc));
        if (string.IsNullOrWhiteSpace(request.StrategyCode) || string.IsNullOrWhiteSpace(request.RequestedBy) ||
            string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("StrategyCode, RequestedBy e Reason sono obbligatori.");
        var manifest = Get(workspaceId, backtestFolder, runId);
        var next = manifest.Periods.Where(x => x.EffectiveFromUtc > request.RequestedAtUtc)
            .OrderBy(x => x.EffectiveFromUtc).FirstOrDefault()
            ?? throw new ArgumentException("Non esiste un periodo successivo nel run.");
        var reset = new TitanoHardStopReset
        {
            ResetId = $"{request.RequestedAtUtc:yyyyMMddTHHmmssfffffffZ}-{SafeSegment(request.StrategyCode)}",
            StrategyCode = request.StrategyCode, RequestedAtUtc = request.RequestedAtUtc,
            EffectiveFromUtc = next.EffectiveFromUtc, RequestedBy = request.RequestedBy, Reason = request.Reason
        };
        var directory = Path.Combine(_workspaces.GetBacktestPath(workspaceId, backtestFolder), "titano", SafeSegment(runId));
        WriteNewAtomic(Path.Combine(directory, $"hard-stop-reset-{reset.ResetId}.json"), reset);
        return reset;
    }

    /// <param name="mode">
    /// Cambia una cosa sola: cosa fare quando nessun periodo copre <paramref name="timestampUtc"/>.
    /// In <see cref="TitanoFilterMode.Realtime"/> una barra oltre la fine del manifest ricade
    /// sull'ultimo periodo calcolato — è la rotazione in vigore finché non se ne produce una nuova.
    /// Nelle altre modalità resta scoperta, e il chiamante deve trattarlo come condizione esplicita.
    /// </param>
    public TitanoEffectiveStrategies Resolve(
        string workspaceId, string backtestFolder, string runId, DateTime timestampUtc,
        TitanoFilterMode mode = TitanoFilterMode.BacktestRotationFile)
    {
        RequireUtc(timestampUtc, nameof(timestampUtc));
        var manifest = Get(workspaceId, backtestFolder, runId);
        var master = GetMasterExecutionCodes(workspaceId);
        var period = manifest.Periods.SingleOrDefault(x => timestampUtc >= x.EffectiveFromUtc && timestampUtc < x.EffectiveToUtc);

        var usedLatestPeriod = false;
        if (period is null && mode == TitanoFilterMode.Realtime && manifest.Periods.Count > 0)
        {
            var last = manifest.Periods.OrderBy(x => x.EffectiveToUtc).Last();
            if (timestampUtc >= last.EffectiveToUtc)
            {
                period = last;
                usedLatestPeriod = true;
            }
        }

        var enabled = period?.Strategies.Where(x => x.Enabled).Select(x => x.StrategyCode)
            .Order(StringComparer.Ordinal).ToArray() ?? [];
        var resets = manifest.HardStopResets.Where(x => x.EffectiveFromUtc <= timestampUtc)
            .GroupBy(x => x.StrategyCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Max(r => r.EffectiveFromUtc), StringComparer.OrdinalIgnoreCase);
        var states = period?.Strategies.Where(x => master.Contains(x.StrategyCode, StringComparer.OrdinalIgnoreCase))
            .Select(x => new TitanoEffectiveStrategy
            {
                StrategyCode = x.StrategyCode,
                AllocationMultiplier = x.HardStopped && resets.ContainsKey(x.StrategyCode)
                    ? ComputeAllocation(x.Score, manifest.Config) : x.AllocationMultiplier,
                State = x.HardStopped && resets.ContainsKey(x.StrategyCode)
                    ? TitanoStrategyStatus.Reduced : x.State,
                CooldownRemaining = x.CooldownRemaining,
                HardStopped = x.HardStopped && !resets.ContainsKey(x.StrategyCode),
                Reason = x.HardStopped && resets.ContainsKey(x.StrategyCode)
                    ? $"hard stop resettato manualmente ({resets[x.StrategyCode]:O})" : x.Reason,
                Score = x.Score, PassingFilters = x.PassingFilters, TotalFilters = x.TotalFilters,
                ConsecutiveOnPeriods = x.ConsecutiveOnPeriods
            }).OrderBy(x => x.StrategyCode, StringComparer.Ordinal).ToArray() ?? [];
        enabled = states.Where(x => x.AllocationMultiplier > 0).Select(x => x.StrategyCode).ToArray();
        return new TitanoEffectiveStrategies
        {
            RunId = runId, TimestampUtc = timestampUtc, PeriodId = period?.PeriodId,
            MasterStrategies = master, TitanoEnabledStrategies = enabled,
            EffectiveStrategies = master.Intersect(enabled, StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray(),
            StrategyStates = states,
            // Dopo l'ultimo periodo storico Resolve usa l'ultima decisione come filtro realtime
            // della rotazione successiva; false resta riservato agli istanti precedenti al manifest.
            HasActivePeriod = period is not null,
            UsedLatestPeriod = usedLatestPeriod,
            PeriodFromUtc = period?.EffectiveFromUtc,
            PeriodToUtc = period?.EffectiveToUtc,
            ManifestFromUtc = manifest.Periods.Count == 0 ? null : manifest.Periods.Min(x => x.EffectiveFromUtc),
            ManifestToUtc = manifest.Periods.Count == 0 ? null : manifest.Periods.Max(x => x.EffectiveToUtc)
        };
    }

    public static IEnumerable<(DateTime Start, DateTime End)> BuildPeriods(TitanoRotationRequest request)
    {
        var current = PeriodStart(request.StartUtc, request);
        while (current < request.EndUtc)
        {
            var next = request.RotationPeriod switch
            {
                TitanoRotationPeriod.Weekly => current.AddDays(7),
                TitanoRotationPeriod.Biweekly => current.AddDays(14),
                _ => current.AddMonths(1)
            };
            yield return (current, next);
            current = next;
        }
    }

    private static IEnumerable<TitanoRotationDecision> BuildDecisions(
        TitanoRotationRequest request, IReadOnlyList<(DateTime Start, DateTime End)> periods,
        IReadOnlyList<PersistedTrade> trades, IReadOnlyList<string> master, string masterHash)
    {
        var previous = new Dictionary<string, TitanoStrategyState>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i + 1 < periods.Count; i++)
        {
            var source = periods[i];
            var effective = periods[i + 1];
            // PRIMA PASSATA: metriche e voti di tutte le strategie del periodo. Il sizing per
            // percentile confronta le strategie fra loro, quindi nessuno score è calcolabile finché
            // non si conoscono tutti gli altri.
            var measured = master.Select(code =>
            {
                var rows = trades.Where(t => t.StrategyCode.Equals(code, StringComparison.OrdinalIgnoreCase) &&
                                             t.ExitTimeUtc < source.End)
                    .OrderBy(t => t.ExitTimeUtc).ThenBy(t => t.TradeId, StringComparer.Ordinal).ToList();
                var metrics = CalculateMetrics(rows, source.End, request);
                var votes = EvaluateVotes(metrics, request);
                return (Code: code, Metrics: metrics, Votes: votes,
                        RawScore: votes.Count == 0 ? 0m : votes.Average(x => x.Score));
            }).ToList();

            // Percentile per singolo voto, poi media: un voto quasi costante fra le strategie
            // (drawdown, volatilità) smette di pesare come una costante e torna a discriminare
            // solo per quel poco che effettivamente varia.
            var percentileScores = ComputePercentileScores(measured
                .Select(x => (IReadOnlyList<TitanoFilterVote>)x.Votes).ToList());

            // SECONDA PASSATA: decisione per strategia.
            var states = measured.Select((measurement, index) =>
            {
                var code = measurement.Code;
                var metrics = measurement.Metrics;
                var votes = measurement.Votes;
                previous.TryGetValue(code, out var prior);
                var passing = votes.Count(x => x.Passed);
                var rawScore = measurement.RawScore;
                var score = request.CrossSectionalSizing ? percentileScores[index] : rawScore;
                var hardStopped = prior?.HardStopped == true || metrics.CurrentDrawdown >= request.HardStopDrawdown;
                var cooldown = prior?.Enabled == false
                    ? Math.Max(0, prior.CooldownRemaining - 1)
                    : 0;
                var eligible = passing >= request.MinimumPassingFilters;
                var mayDisable = prior is null || prior.ConsecutiveOnPeriods >= request.MinimumOnPeriods;

                // Con il sizing per percentile lo score è un RANGO, non un giudizio: usarlo per
                // accendere e spegnere significherebbe spegnere sempre la peggiore del gruppo anche
                // quando va benissimo. L'ON/OFF resta quindi ai soli cancelli assoluti.
                var disable = metrics.CurrentDrawdown > request.MaximumCurrentDrawdown || !eligible ||
                              (!request.CrossSectionalSizing && rawScore < request.DisableCompositeScore);
                var reenable = metrics.CurrentDrawdown <= request.ReenableMaximumCurrentDrawdown &&
                               eligible && cooldown == 0 &&
                               (request.CrossSectionalSizing || rawScore >= request.ReenableCompositeScore);
                var on = prior is null ? !disable : prior.Enabled ? (!disable || !mayDisable) : reenable;
                if (hardStopped) on = false;
                if (prior?.Enabled == true && !on) cooldown = request.CooldownPeriodsAfterOff;
                var multiplier = on ? ComputeAllocation(score, request) : 0m;
                var reasons = votes.Where(x => !x.Passed).Select(x => x.Reason).ToList();
                if (hardStopped) reasons.Insert(0, $"hard stop drawdown {metrics.CurrentDrawdown:P2} >= {request.HardStopDrawdown:P2}");
                else if (!on && cooldown > 0) reasons.Add($"cooldown: {cooldown} periodi residui");
                if (reasons.Count == 0)
                    reasons.Add(request.CrossSectionalSizing
                        ? $"voto {passing}/{votes.Count}, percentile {score:F3} (score assoluto {rawScore:F3})"
                        : $"voto {passing}/{votes.Count}, score {score:F3}");
                var newStatus = hardStopped ? TitanoStrategyStatus.HardStopped :
                    multiplier == 0 ? TitanoStrategyStatus.Disabled :
                    multiplier == 1 ? TitanoStrategyStatus.Enabled : TitanoStrategyStatus.Reduced;
                var transitionType = ClassifyTransition(prior, newStatus);
                var anomalies = DetectAnomalies(multiplier > 0, multiplier, newStatus, hardStopped, passing, request.MinimumPassingFilters);
                var state = new TitanoStrategyState
                {
                    StrategyCode = code, Enabled = multiplier > 0, AllocationMultiplier = multiplier,
                    State = newStatus,
                    CooldownRemaining = cooldown,
                    ConsecutiveOnPeriods = multiplier > 0 ? (prior?.ConsecutiveOnPeriods ?? 0) + 1 : 0,
                    HardStopped = hardStopped, PassingFilters = passing, TotalFilters = votes.Count,
                    Votes = votes, Score = score, RawScore = rawScore,
                    Reason = string.Join("; ", reasons), Reasons = reasons,
                    Metrics = metrics,
                    PreviousState = prior?.State,
                    TransitionType = transitionType,
                    AnomalyFlags = anomalies
                };
                previous[code] = state;
                return state;
            }).OrderBy(x => x.StrategyCode, StringComparer.Ordinal).ToList();
            yield return new TitanoRotationDecision
            {
                PeriodId = $"{effective.Start:yyyyMMddTHHmmssZ}-{effective.End:yyyyMMddTHHmmssZ}",
                PeriodStartUtc = source.Start, PeriodEndUtc = source.End,
                EffectiveFromUtc = effective.Start, EffectiveToUtc = effective.End,
                SourceBacktestFolder = request.BacktestFolder, MasterFilterHash = masterHash, Strategies = states
            };
        }
    }

    private static List<TitanoEquityPoint> BuildOriginalEquity(
        TitanoRotationRequest request, IEnumerable<PersistedTrade> trades, IReadOnlyList<string> master)
    {
        var balance = request.InitialCapital;
        var result = new List<TitanoEquityPoint>();
        foreach (var trade in trades.OrderBy(x => x.ExitTimeUtc).ThenBy(x => x.TradeId, StringComparer.Ordinal))
        {
            if (!master.Contains(trade.StrategyCode, StringComparer.OrdinalIgnoreCase))
                continue;
            balance += trade.NetProfit;
            result.Add(new TitanoEquityPoint
            {
                TimestampUtc = trade.ExitTimeUtc, TradeId = trade.TradeId, StrategyCode = trade.StrategyCode,
                NetProfit = trade.NetProfit, AllocationMultiplier = 1m,
                Costs = 0m, Balance = balance, Equity = balance
            });
        }
        return result;
    }

    private static List<TitanoEquityPoint> BuildEquity(TitanoRotationRequest request, IEnumerable<PersistedTrade> trades,
        IReadOnlyList<TitanoRotationDecision> decisions, IReadOnlyList<string> master)
    {
        var balance = request.InitialCapital;
        var result = new List<TitanoEquityPoint>();
        foreach (var trade in trades.OrderBy(x => x.ExitTimeUtc).ThenBy(x => x.TradeId, StringComparer.Ordinal))
        {
            var period = decisions.SingleOrDefault(x => trade.EntryTimeUtc >= x.EffectiveFromUtc && trade.EntryTimeUtc < x.EffectiveToUtc);
            var state = period?.Strategies.SingleOrDefault(x => x.StrategyCode.Equals(trade.StrategyCode, StringComparison.OrdinalIgnoreCase));
            if (period is null || state is null || !master.Contains(trade.StrategyCode, StringComparer.OrdinalIgnoreCase) ||
                !state.Enabled)
                continue;
            var quantity = trade.Quantity == 0 ? 1m : trade.Quantity;
            var costs = (request.CommissionPerUnit + request.SlippagePerUnit) * quantity * state.AllocationMultiplier;
            var net = trade.NetProfit * state.AllocationMultiplier - costs;
            balance += net;
            result.Add(new TitanoEquityPoint
            {
                TimestampUtc = trade.ExitTimeUtc, TradeId = trade.TradeId, StrategyCode = trade.StrategyCode,
                NetProfit = net, AllocationMultiplier = state.AllocationMultiplier,
                Costs = costs, Balance = balance, Equity = balance
            });
        }
        return result;
    }

    private static List<TitanoEquityPoint> ResolveOriginalEquity(string runPath, TitanoRotationManifest manifest)
    {
        if (manifest.OriginalEquity.Count > 0)
            return manifest.OriginalEquity;

        var trades = TryLoadTrades(runPath);
        if (trades is null || trades.Count == 0)
            return [];

        var master = manifest.Periods
            .SelectMany(period => period.Strategies)
            .Select(state => state.StrategyCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return master.Length == 0 ? [] : BuildOriginalEquity(manifest.Config, trades, master);
    }

    private static List<PersistedTrade>? TryLoadTrades(string runPath)
    {
        var tradesPath = Path.GetFullPath(Path.Combine(runPath, "..", "..", TradingPersistenceSchema.TradesFileName));
        if (!File.Exists(tradesPath))
            return null;
        return JsonSerializer.Deserialize<List<PersistedTrade>>(File.ReadAllText(tradesPath), JsonOptions) ?? [];
    }

    private static decimal CalculateMaxDrawdown(IReadOnlyList<TitanoEquityPoint> points, decimal initialCapital)
    {
        if (points.Count == 0)
            return 0m;

        var peak = initialCapital;
        var maximumDrawdown = 0m;
        foreach (var point in points)
        {
            peak = Math.Max(peak, point.Equity);
            if (peak == 0)
                continue;
            maximumDrawdown = Math.Max(maximumDrawdown, (peak - point.Equity) / Math.Abs(peak));
        }
        return maximumDrawdown;
    }

    private static List<object> BuildEquityComparisonSeries(
        decimal initialCapital,
        DateTime startUtc,
        IReadOnlyList<TitanoEquityPoint> original,
        IReadOnlyList<TitanoEquityPoint> filtered)
    {
        var series = new List<object>
        {
            new
            {
                t = startUtc.ToString("O", CultureInfo.InvariantCulture),
                original = initialCapital,
                filtered = initialCapital,
                originalDrawdown = 0m,
                filteredDrawdown = 0m
            }
        };
        if (original.Count == 0 && filtered.Count == 0)
            return series;

        var origBal = initialCapital;
        var filtBal = initialCapital;
        var origPeak = initialCapital;
        var filtPeak = initialCapital;
        var events = original.Select(point => (point.TimestampUtc, IsOriginal: true, point))
            .Concat(filtered.Select(point => (point.TimestampUtc, IsOriginal: false, point)))
            .OrderBy(item => item.TimestampUtc)
            .ThenBy(item => item.IsOriginal)
            .ToList();

        foreach (var group in events.GroupBy(item => item.TimestampUtc))
        {
            foreach (var item in group)
            {
                if (item.IsOriginal)
                    origBal = item.point.Equity;
                else
                    filtBal = item.point.Equity;
            }
            origPeak = Math.Max(origPeak, origBal);
            filtPeak = Math.Max(filtPeak, filtBal);

            series.Add(new
            {
                t = group.Key.ToString("O", CultureInfo.InvariantCulture),
                original = origBal,
                filtered = filtBal,
                originalDrawdown = origPeak == 0 ? 0 : (origPeak - origBal) / Math.Abs(origPeak),
                filteredDrawdown = filtPeak == 0 ? 0 : (filtPeak - filtBal) / Math.Abs(filtPeak)
            });
        }

        return series;
    }

    public static TitanoPeriodMetrics CalculateMetrics(
        IReadOnlyList<PersistedTrade> rows, DateTime cutoffUtc, TitanoRotationRequest request)
    {
        var eligible = rows.Where(x => x.ExitTimeUtc < cutoffUtc)
            .OrderBy(x => x.ExitTimeUtc).ThenBy(x => x.TradeId, StringComparer.Ordinal).ToList();
        var equity = request.InitialCapital;
        var peak = equity;
        var maximumDrawdown = 0m;
        var points = new List<(DateTime Time, decimal Equity)> { (DateTime.MinValue, equity) };
        var returns = new List<(DateTime Time, decimal Value)>();
        foreach (var trade in eligible)
        {
            var prior = equity;
            equity += trade.NetProfit;
            returns.Add((trade.ExitTimeUtc, prior == 0 ? 0 : trade.NetProfit / Math.Abs(prior)));
            peak = Math.Max(peak, equity);
            var drawdown = peak == 0 ? 0 : (peak - equity) / Math.Abs(peak);
            maximumDrawdown = Math.Max(maximumDrawdown, drawdown);
            points.Add((trade.ExitTimeUtc, equity));
        }

        decimal EquityAt(DateTime time) => points.LastOrDefault(x => x.Time < time).Equity is var value && value != 0
            ? value : request.InitialCapital;
        var shortStart = EquityAt(cutoffUtc.AddDays(-request.ShortWindowDays));
        var longStart = EquityAt(cutoffUtc.AddDays(-request.LongWindowDays));
        var movingPoints = points.Where(x => x.Time >= cutoffUtc.AddDays(-request.MovingAverageWindowDays)).Select(x => x.Equity).ToArray();
        if (movingPoints.Length == 0) movingPoints = [equity];
        var average = movingPoints.Average();
        var equityStdDev = PopulationStdDev(movingPoints);
        var recentReturns = returns.Where(x => x.Time >= cutoffUtc.AddDays(-request.ShortWindowDays)).Select(x => x.Value).ToArray();
        var shortRows = eligible.Where(x => x.ExitTimeUtc >= cutoffUtc.AddDays(-request.ShortWindowDays)).ToList();
        return new TitanoPeriodMetrics
        {
            Trades = shortRows.Count, WinningTrades = shortRows.Count(x => x.NetProfit > 0),
            GrossProfit = shortRows.Sum(x => x.GrossProfit), NetProfit = shortRows.Sum(x => x.NetProfit),
            Commission = shortRows.Sum(x => x.Commission), CurrentEquity = equity,
            ShortStartEquity = shortStart, LongStartEquity = longStart,
            ShortReturn = shortStart == 0 ? 0 : (equity - shortStart) / Math.Abs(shortStart),
            LongReturn = longStart == 0 ? 0 : (equity - longStart) / Math.Abs(longStart),
            MovingAverageEquity = average, EquityStandardDeviation = equityStdDev,
            ZScore = equityStdDev == 0 ? 0 : (equity - average) / equityStdDev,
            CurrentDrawdown = peak == 0 ? 0 : (peak - equity) / Math.Abs(peak),
            MaximumDrawdown = maximumDrawdown,
            ReturnVolatility = PopulationStdDev(recentReturns)
        };
    }

    private static List<TitanoFilterVote> EvaluateVotes(TitanoPeriodMetrics metrics, TitanoRotationRequest request)
    {
        TitanoFilterVote Vote(string name, bool passed, decimal score, string failure) => new()
        {
            Filter = name, Passed = passed, Score = Math.Clamp(score, 0m, 1m),
            Reason = passed ? $"{name}: superato" : failure
        };
        var shortPassed = metrics.Trades >= request.MinimumTrades && metrics.ShortReturn >= request.MinimumShortReturn &&
                          (!request.RequireEquityAboveMovingAverage || metrics.CurrentEquity >= metrics.MovingAverageEquity);
        var zPassed = metrics.ZScore >= request.MinimumZScore && metrics.ZScore <= request.MaximumZScore;
        var ddPassed = metrics.CurrentDrawdown <= request.MaximumCurrentDrawdown &&
                       metrics.MaximumDrawdown <= request.MaximumObservedDrawdown;
        return
        [
            Vote("short-performance", shortPassed,
                metrics.Trades < request.MinimumTrades ? 0 : 0.5m + Math.Clamp(metrics.ShortReturn - request.MinimumShortReturn, -0.5m, 0.5m),
                $"performance breve insufficiente ({metrics.Trades} trade, {metrics.ShortReturn:P2})"),
            Vote("long-performance", metrics.LongReturn >= request.MinimumLongReturn,
                0.5m + Math.Clamp(metrics.LongReturn - request.MinimumLongReturn, -0.5m, 0.5m),
                $"return lungo {metrics.LongReturn:P2} < {request.MinimumLongReturn:P2}"),
            Vote("z-score", zPassed, zPassed ? 1m : 0m,
                $"z-score {metrics.ZScore:F2} fuori [{request.MinimumZScore:F2}, {request.MaximumZScore:F2}]"),
            Vote("drawdown", ddPassed, 1m - metrics.CurrentDrawdown / Math.Max(0.000001m, request.MaximumCurrentDrawdown),
                $"drawdown corrente/massimo {metrics.CurrentDrawdown:P2}/{metrics.MaximumDrawdown:P2}"),
            Vote("volatilità", metrics.ReturnVolatility <= request.MaximumReturnVolatility,
                1m - metrics.ReturnVolatility / Math.Max(0.000001m, request.MaximumReturnVolatility),
                $"volatilità {metrics.ReturnVolatility:P2} > {request.MaximumReturnVolatility:P2}")
        ];
    }

    public static decimal SelectMultiplier(decimal score, IReadOnlyList<TitanoSizingTier> tiers) =>
        tiers.OrderByDescending(x => x.MinimumScore).FirstOrDefault(x => score >= x.MinimumScore)?.AllocationMultiplier ?? 0m;

    /// <summary>
    /// Traduce lo score in allocazione. Con <see cref="TitanoRotationRequest.CrossSectionalSizing"/>
    /// è una curva continua tra il minimo e il massimo configurati, arrotondata al passo: una
    /// strategia che peggiora viene ridotta gradualmente invece di saltare da 100% a 50%.
    /// Altrimenti si ricade sugli scaglioni storici.
    /// </summary>
    public static decimal ComputeAllocation(decimal score, TitanoRotationRequest request)
    {
        if (!request.CrossSectionalSizing)
            return SelectMultiplier(score, request.SizingTiers);

        var floor = request.MinimumAllocationMultiplier;
        var cap = Math.Max(floor, request.MaximumAllocationMultiplier);
        var value = floor + (cap - floor) * Math.Clamp(score, 0m, 1m);

        if (request.AllocationStep > 0)
            value = Math.Round(value / request.AllocationStep, MidpointRounding.AwayFromZero) * request.AllocationStep;

        return Math.Clamp(value, floor, cap);
    }

    /// <summary>
    /// Score composito come media dei percentili dei singoli voti, calcolati fra le strategie dello
    /// stesso periodo.
    ///
    /// <para>Il punto è la scala. I voti assoluti sono normalizzati su intervalli molto più larghi
    /// della variazione reale — la performance è misurata su ±50% quando i rendimenti veri sono
    /// pochi punti percentuali, il drawdown è rapportato al proprio tetto — quindi restano
    /// schiacciati vicino a un valore fisso e la loro media non discrimina nulla. Il percentile
    /// ridà a ciascun voto l'intera scala 0..1 <i>indipendentemente da quanto poco vari in
    /// assoluto</i>.</para>
    ///
    /// <para>I pari merito ricevono il rango medio, così due strategie identiche prendono la stessa
    /// allocazione. Con una sola strategia il rango non ha significato e si restituisce 1: non
    /// avrebbe senso dimezzare l'unica strategia del portafoglio per il solo fatto di essere anche
    /// la peggiore.</para>
    /// </summary>
    private static IReadOnlyList<decimal> ComputePercentileScores(
        IReadOnlyList<IReadOnlyList<TitanoFilterVote>> votesByStrategy)
    {
        var count = votesByStrategy.Count;
        if (count == 0) return [];
        if (count == 1) return [1m];

        var voteCount = votesByStrategy.Max(x => x.Count);
        var totals = new decimal[count];

        for (var vote = 0; vote < voteCount; vote++)
        {
            var values = votesByStrategy
                .Select(x => vote < x.Count ? x[vote].Score : 0m)
                .ToArray();

            for (var i = 0; i < count; i++)
            {
                var below = values.Count(v => v < values[i]);
                var equal = values.Count(v => v == values[i]);
                // Rango medio dei pari merito, normalizzato su [0,1].
                totals[i] += (below + (equal - 1) / 2m) / (count - 1);
            }
        }

        return totals.Select(x => Math.Clamp(voteCount == 0 ? 0m : x / voteCount, 0m, 1m)).ToArray();
    }

    public static decimal RoundQuantity(decimal quantity, decimal multiplier, decimal step, decimal minimum)
    {
        if (quantity <= 0 || multiplier <= 0) return 0;
        var rounded = Math.Floor(quantity * multiplier / step) * step;
        return rounded < minimum ? 0 : rounded;
    }

    private static List<TitanoWalkForwardResult> BuildWalkForward(
        TitanoRotationRequest request, IReadOnlyList<(DateTime Start, DateTime End)> periods,
        IReadOnlyList<PersistedTrade> trades, IReadOnlyList<TitanoRotationDecision> decisions,
        IReadOnlyList<string> master)
    {
        var result = new List<TitanoWalkForwardResult>();
        for (var i = request.CalibrationPeriods; i < periods.Count; i += Math.Max(1, request.EvaluationPeriods))
        {
            var calibrationStart = request.WalkForwardMode == TitanoWalkForwardMode.Expanding
                ? periods[0].Start : periods[i - request.CalibrationPeriods].Start;
            var calibrationEnd = periods[i].Start;
            var evaluationEnd = periods[Math.Min(periods.Count - 1, i + request.EvaluationPeriods - 1)].End;
            decimal Profit(DateTime from, DateTime to, bool filtered) => trades.Where(t =>
                    t.ExitTimeUtc >= from && t.ExitTimeUtc < to &&
                    master.Contains(t.StrategyCode, StringComparer.OrdinalIgnoreCase) &&
                    (!filtered || decisions.Any(d => t.EntryTimeUtc >= d.EffectiveFromUtc && t.EntryTimeUtc < d.EffectiveToUtc &&
                        d.Strategies.Any(s => s.StrategyCode.Equals(t.StrategyCode, StringComparison.OrdinalIgnoreCase) && s.Enabled))))
                .Sum(t => t.NetProfit);
            var isFiltered = Profit(calibrationStart, calibrationEnd, true);
            var isRaw = Profit(calibrationStart, calibrationEnd, false);
            var oosFiltered = Profit(calibrationEnd, evaluationEnd, true);
            var oosRaw = Profit(calibrationEnd, evaluationEnd, false);
            result.Add(new TitanoWalkForwardResult
            {
                EvaluationPeriodId = $"{calibrationEnd:yyyyMMddTHHmmssZ}-{evaluationEnd:yyyyMMddTHHmmssZ}",
                CalibrationFromUtc = calibrationStart, CalibrationToUtc = calibrationEnd,
                EvaluationFromUtc = calibrationEnd, EvaluationToUtc = evaluationEnd,
                InSampleNetProfit = isFiltered, OutOfSampleNetProfit = oosFiltered,
                InSampleOnlyImprovementWarning = isFiltered > isRaw && oosFiltered <= oosRaw
            });
        }
        return result;
    }

    /// <summary>
    /// Classifica il cambio di stato di una strategia rispetto al periodo precedente, per rendere
    /// immediatamente visibili le transizioni rilevanti senza dover confrontare manualmente due periodi.
    /// </summary>
    private static string ClassifyTransition(TitanoStrategyState? prior, TitanoStrategyStatus newStatus)
    {
        if (prior is null) return "NewlyTracked";
        if (prior.State == newStatus) return "Unchanged";
        if (newStatus == TitanoStrategyStatus.HardStopped) return "HardStopTriggered";
        if (prior.State == TitanoStrategyStatus.HardStopped) return "HardStopReleased";
        var priorOn = prior.State is TitanoStrategyStatus.Enabled or TitanoStrategyStatus.Reduced;
        var nowOn = newStatus is TitanoStrategyStatus.Enabled or TitanoStrategyStatus.Reduced;
        if (priorOn && !nowOn) return "EnabledToDisabled";
        if (!priorOn && nowOn) return "DisabledToEnabled";
        return "AllocationChanged"; // es. Enabled <-> Reduced
    }

    /// <summary>
    /// Controlli di coerenza automatici sullo stato calcolato, per intercettare bug di calcolo nella
    /// rotazione (es. contraddizioni tra Enabled/AllocationMultiplier/HardStopped) senza dover rileggere
    /// tutta la logica di BuildDecisions ogni volta.
    /// </summary>
    private static List<string> DetectAnomalies(
        bool enabled, decimal multiplier, TitanoStrategyStatus state, bool hardStopped,
        int passingFilters, int minimumPassingFilters)
    {
        var anomalies = new List<string>();
        if (enabled && multiplier <= 0)
            anomalies.Add($"Enabled=true ma AllocationMultiplier={multiplier} (dovrebbe essere > 0)");
        if (!enabled && multiplier > 0)
            anomalies.Add($"Enabled=false ma AllocationMultiplier={multiplier} (dovrebbe essere 0)");
        if (hardStopped && enabled)
            anomalies.Add("HardStopped=true ma Enabled=true (una strategia in hard stop non dovrebbe essere abilitata)");
        if (state == TitanoStrategyStatus.HardStopped && !hardStopped)
            anomalies.Add("State=HardStopped ma HardStopped=false");
        if (state == TitanoStrategyStatus.Enabled && multiplier != 1m)
            anomalies.Add($"State=Enabled ma AllocationMultiplier={multiplier} (atteso 1)");
        if (state == TitanoStrategyStatus.Disabled && multiplier != 0m)
            anomalies.Add($"State=Disabled ma AllocationMultiplier={multiplier} (atteso 0)");
        if (enabled && passingFilters < minimumPassingFilters)
            anomalies.Add($"Enabled=true con soli {passingFilters}/{minimumPassingFilters} filtri minimi superati");
        return anomalies;
    }

    private static decimal PopulationStdDev(IReadOnlyCollection<decimal> values)
    {
        if (values.Count == 0) return 0;
        var mean = values.Average();
        return (decimal)Math.Sqrt((double)values.Average(x => (x - mean) * (x - mean)));
    }

    private static DateTime PeriodStart(DateTime value, TitanoRotationRequest request)
    {
        var date = value.Date;
        return request.RotationPeriod switch
        {
            TitanoRotationPeriod.Weekly => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
            TitanoRotationPeriod.Monthly => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => BiweeklyStart(date, request.BiweeklyAnchorUtc ?? request.StartUtc)
        };
    }

    private static DateTime BiweeklyStart(DateTime date, DateTime anchor)
    {
        var anchorDate = anchor.Date;
        var days = (int)Math.Floor((date - anchorDate).TotalDays / 14d) * 14;
        return anchorDate.AddDays(days);
    }

    private static void Validate(TitanoRotationRequest request)
    {
        RequireUtc(request.StartUtc, nameof(request.StartUtc));
        RequireUtc(request.EndUtc, nameof(request.EndUtc));
        if (request.BiweeklyAnchorUtc.HasValue) RequireUtc(request.BiweeklyAnchorUtc.Value, nameof(request.BiweeklyAnchorUtc));
        if (request.EndUtc <= request.StartUtc) throw new ArgumentException("EndUtc deve essere successivo a StartUtc.");
        if (!request.TimeZoneId.Equals("UTC", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("La versione 1 supporta esclusivamente il calendario UTC.");
        if (request.InitialCapital <= 0 || request.MinimumTrades < 0 ||
            request.ShortWindowDays <= 0 || request.LongWindowDays < request.ShortWindowDays ||
            request.MovingAverageWindowDays <= 0 || request.MinimumZScore > request.MaximumZScore ||
            request.MaximumCurrentDrawdown < 0 || request.MaximumObservedDrawdown < 0 ||
            request.MaximumReturnVolatility < 0 || request.ReenableMaximumCurrentDrawdown > request.MaximumCurrentDrawdown ||
            request.ReenableCompositeScore < request.DisableCompositeScore ||
            request.MinimumPassingFilters is < 0 or > 5 || request.CooldownPeriodsAfterOff < 0 ||
            request.MinimumOnPeriods < 0 || request.HardStopDrawdown <= request.MaximumCurrentDrawdown ||
            request.QuantityStep <= 0 || request.MinimumIntentQuantity < 0 ||
            request.CalibrationPeriods <= 0 || request.EvaluationPeriods <= 0 ||
            request.SizingTiers.Count == 0 || request.SizingTiers.Any(x => x.AllocationMultiplier is < 0 or > 1) ||
            request.MinimumAllocationMultiplier is < 0 or > 1 ||
            request.MaximumAllocationMultiplier is < 0 or > 1 ||
            request.MinimumAllocationMultiplier > request.MaximumAllocationMultiplier ||
            request.AllocationStep < 0 || request.AllocationStep > 1)
            throw new ArgumentException("Configurazione Titano non valida.");
    }

    private static void ValidateTrades(IEnumerable<PersistedTrade> trades)
    {
        foreach (var trade in trades)
        {
            if (string.IsNullOrWhiteSpace(trade.StrategyCode))
                throw new InvalidDataException($"Trade '{trade.TradeId}' privo di StrategyCode; migrazione/catalog mapping richiesto.");
            RequireUtc(trade.EntryTimeUtc, $"{trade.TradeId}.EntryTimeUtc");
            RequireUtc(trade.ExitTimeUtc, $"{trade.TradeId}.ExitTimeUtc");
        }
    }

    private static void RequireUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc) throw new ArgumentException($"{name} deve essere UTC.");
    }

    private static string SafeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.Contains('/') || value.Contains('\\') || value.Contains(".."))
            throw new ArgumentException("Identificatore Titano non valido.");
        return value;
    }

    private static string Sha(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
    private static TitanoRotationManifest ReadManifest(string path) =>
        JsonSerializer.Deserialize<TitanoRotationManifest>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidDataException($"Manifest Titano non valido: {path}");

    private static IReadOnlyList<TitanoHardStopReset> ReadResets(string directory) =>
        Directory.EnumerateFiles(directory, "hard-stop-reset-*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .Select(path => JsonSerializer.Deserialize<TitanoHardStopReset>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidDataException($"Override Titano non valido: {path}"))
            .ToArray();

    private static void WriteNewAtomic<T>(string path, T value)
    {
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, JsonOptions);
                stream.Flush(true);
            }
            File.Move(temp, path, overwrite: false);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
