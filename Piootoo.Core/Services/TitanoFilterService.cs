using System.Globalization;
using System.Text;
using System.Text.Json;
using Piootoo.Core.Optimization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Utilities;

namespace Piootoo.Core.Services;

public class TitanoFilterService
{
    private readonly PiootooSettings _settings;
    private readonly TitanoSetupService _setupService;
    private readonly string _resultsPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public TitanoFilterService(PiootooSettings settings, TitanoSetupService setupService)
    {
        _settings = settings;
        _setupService = setupService;
        _resultsPath = Path.Combine(_settings.GetSettingsPath(), "results", "titano");
        Directory.CreateDirectory(_resultsPath);
    }

    public TitanoFilterResult Apply(BacktestingResult backtesting, TitanoFilterRequest request)
    {
        _setupService.ApplySetupToRequest(request);

        var strategyWeeks = BuildStrategyWeeks(backtesting);
        var weeks = BuildCalendarWeeks(backtesting, strategyWeeks);
        var result = new TitanoFilterResult
        {
            BacktestingId = backtesting.JobId,
            Name = request.Name,
            Code = request.Code,
            SetupId = request.SetupId,
            LookbackWeeks = request.LookbackWeeks,
            StartDate = backtesting.StartDate,
            EndDate = backtesting.EndDate,
            InitialCapital = backtesting.InitialCapital,
            Rules = request.Rules,
            TradingRules = request.TradingRules,
            OriginalFinalEquity = backtesting.FinalEquity,
            OriginalTotalProfit = backtesting.TotalProfit,
            OriginalMaxDrawdown = backtesting.MaxDrawdown
        };

        var filteredEquity = backtesting.InitialCapital;
        var filteredPeak = filteredEquity;
        var cooldownUntil = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var summaries = strategyWeeks.Keys.ToDictionary(
            key => key,
            key => new TitanoStrategySummary
            {
                StrategyKey = key,
                StrategyName = ExtractStrategyName(key),
                Symbol = ExtractSymbol(key)
            },
            StringComparer.OrdinalIgnoreCase);

        var minHistoryWeeks = request.Rules.MinWeeksBeforeRulesApply > 0
            ? request.Rules.MinWeeksBeforeRulesApply
            : request.LookbackWeeks;

        for (var index = 0; index < weeks.Count; index++)
        {
            var week = weeks[index];
            var previousWeeks = weeks.Take(index).TakeLast(Math.Max(1, request.LookbackWeeks)).ToList();
            var isWarmup = previousWeeks.Count < minHistoryWeeks;
            var decisions = new List<TitanoStrategyDecision>();
            decimal originalWeeklyProfit = 0m;
            var originalWeeklyTrades = 0;
            var originalWinningTrades = 0;
            var originalLosingTrades = 0;

            foreach (var (strategyKey, weeklyData) in strategyWeeks)
            {
                weeklyData.TryGetValue(week.Key, out var currentData);
                var metrics = CalculateMetrics(weeklyData, previousWeeks, backtesting.InitialCapital);
                var decision = Decide(strategyKey, metrics, request.Rules, isWarmup, previousWeeks, weeklyData, backtesting.InitialCapital);

                if (cooldownUntil.TryGetValue(strategyKey, out var disabledUntilIndex) && index < disabledUntilIndex)
                {
                    decision.IsEnabled = false;
                    decision.Reasons.Add($"cooldown attivo fino alla settimana {disabledUntilIndex + 1}");
                }

                decisions.Add(decision);

                var currentProfit = currentData?.Profit ?? 0m;
                var currentTrades = currentData?.Trades ?? 0;
                originalWeeklyProfit += currentProfit;
                originalWeeklyTrades += currentTrades;
                originalWinningTrades += currentData?.WinningTrades ?? 0;
                originalLosingTrades += currentData?.LosingTrades ?? 0;
                summaries[strategyKey].ProfitIfAlwaysEnabled += currentProfit;
            }

            ApplyMaxStrategiesOn(decisions, request.Rules.MaxStrategiesOn);

            decimal filteredWeeklyProfit = 0m;
            var filteredWeeklyTrades = 0;
            var filteredWinningTrades = 0;
            var filteredLosingTrades = 0;

            foreach (var decision in decisions)
            {
                strategyWeeks.TryGetValue(decision.StrategyKey, out var weeklyData);
                StrategyWeekData? currentData = null;
                weeklyData?.TryGetValue(week.Key, out currentData);
                var currentProfit = currentData?.Profit ?? 0m;
                var currentTrades = currentData?.Trades ?? 0;

                if (decision.IsEnabled)
                {
                    filteredWeeklyProfit += currentProfit;
                    filteredWeeklyTrades += currentTrades;
                    filteredWinningTrades += currentData?.WinningTrades ?? 0;
                    filteredLosingTrades += currentData?.LosingTrades ?? 0;
                    summaries[decision.StrategyKey].EnabledWeeks++;
                    summaries[decision.StrategyKey].ProfitWhenEnabled += currentProfit;
                }
                else
                {
                    summaries[decision.StrategyKey].DisabledWeeks++;
                    if (request.Rules.CooldownWeeksAfterOff > 0 &&
                        !decision.Reasons.Any(reason => reason.StartsWith("limite strategie ON", StringComparison.OrdinalIgnoreCase)))
                    {
                        cooldownUntil[decision.StrategyKey] = index + request.Rules.CooldownWeeksAfterOff;
                    }
                }
            }

            filteredEquity += filteredWeeklyProfit;
            filteredPeak = Math.Max(filteredPeak, filteredEquity);
            var filteredDrawdown = filteredPeak > 0 ? (filteredEquity - filteredPeak) / filteredPeak : 0m;

            result.WeeklyResults.Add(new TitanoWeeklyResult
            {
                Year = week.Year,
                Week = week.Week,
                WeekStart = week.Start,
                WeekEnd = week.End,
                OriginalEquity = backtesting.InitialCapital + result.WeeklyResults.Sum(w => w.OriginalWeeklyProfit) + originalWeeklyProfit,
                FilteredEquity = filteredEquity,
                OriginalWeeklyProfit = originalWeeklyProfit,
                FilteredWeeklyProfit = filteredWeeklyProfit,
                FilteredDrawdown = filteredDrawdown,
                OriginalWeeklyTrades = originalWeeklyTrades,
                FilteredWeeklyTrades = filteredWeeklyTrades,
                OriginalWinningTrades = originalWinningTrades,
                OriginalLosingTrades = originalLosingTrades,
                FilteredWinningTrades = filteredWinningTrades,
                FilteredLosingTrades = filteredLosingTrades,
                EnabledStrategies = decisions.Where(d => d.IsEnabled).Select(d => d.StrategyKey).OrderBy(s => s).ToList(),
                StrategyDecisions = decisions.OrderByDescending(d => d.Score).ToList()
            });
            result.OriginalTotalTrades += originalWeeklyTrades;
            result.FilteredTotalTrades += filteredWeeklyTrades;
        }

        result.FilteredFinalEquity = filteredEquity;
        result.FilteredTotalProfit = filteredEquity - backtesting.InitialCapital;
        result.FilteredMaxDrawdown = result.WeeklyResults.Any() ? result.WeeklyResults.Min(w => w.FilteredDrawdown) : 0m;
        result.SuspendedStrategyTrades = Math.Max(0, result.OriginalTotalTrades - result.FilteredTotalTrades);
        result.StrategySummaries = summaries.Values.OrderByDescending(s => s.ProfitWhenEnabled).ToList();

        SaveResult(result, backtesting);
        return result;
    }

    private static void ApplyMaxStrategiesOn(List<TitanoStrategyDecision> decisions, int maxStrategiesOn)
    {
        if (maxStrategiesOn <= 0)
        {
            return;
        }

        foreach (var decision in decisions.Where(d => d.IsEnabled).OrderByDescending(d => d.Score).Skip(maxStrategiesOn))
        {
            decision.IsEnabled = false;
            decision.Reasons.Add($"limite strategie ON ({maxStrategiesOn})");
        }
    }

    private void SaveResult(TitanoFilterResult result, BacktestingResult backtesting)
    {
        var safeName = MakeSafeFileName(string.IsNullOrWhiteSpace(result.Code) ? result.Name : result.Code);
        var prefix = $"titano_{safeName}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var backtestDirectory = !string.IsNullOrWhiteSpace(backtesting.ResultFilePath)
            ? Path.GetDirectoryName(backtesting.ResultFilePath)
            : null;
        var outputPath = string.IsNullOrWhiteSpace(backtestDirectory)
            ? _resultsPath
            : Path.Combine(backtestDirectory, "titano");
        Directory.CreateDirectory(outputPath);
        result.HtmlReportFilePath = Path.Combine(outputPath, $"{prefix}.html");
        result.ResultFilePath = Path.Combine(outputPath, $"{prefix}.json");

        File.WriteAllText(result.HtmlReportFilePath, BuildHtml(result, backtesting));
        File.WriteAllText(result.ResultFilePath, JsonSerializer.Serialize(result, _jsonOptions));
    }

    private static Dictionary<string, Dictionary<WeekKey, StrategyWeekData>> BuildStrategyWeeks(BacktestingResult backtesting)
    {
        var strategyWeeks = new Dictionary<string, Dictionary<WeekKey, StrategyWeekData>>(StringComparer.OrdinalIgnoreCase);
        var ordered = backtesting.StrategyResults
            .Where(r => r.Equity != 0)
            .OrderBy(r => r.DateTime)
            .GroupBy(r => MakeStrategyKey(r.Symbol, !string.IsNullOrWhiteSpace(r.StrategyCode) ? r.StrategyCode : r.StrategyName));

        foreach (var strategyGroup in ordered)
        {
            strategyWeeks[strategyGroup.Key] = new Dictionary<WeekKey, StrategyWeekData>();
            foreach (var weekGroup in strategyGroup.GroupBy(r => WeekKey.FromDate(r.DateTime)))
            {
                var rows = weekGroup.OrderBy(r => r.DateTime).ToList();
                var first = rows.First();
                var last = rows.Last();
                var profit = (last.Equity - backtesting.InitialCapital) - (first.Equity - backtesting.InitialCapital);
                var trades = rows.Count(r => r.Signal.HasValue);
                strategyWeeks[strategyGroup.Key][weekGroup.Key] = new StrategyWeekData
                {
                    Profit = profit,
                    Trades = trades,
                    WinningTrades = rows.Count(r => r.Signal.HasValue && r.Profit > 0),
                    LosingTrades = rows.Count(r => r.Signal.HasValue && r.Profit < 0)
                };
            }
        }

        return strategyWeeks;
    }

    private static List<(WeekKey Key, int Year, int Week, DateTime Start, DateTime End)> BuildCalendarWeeks(
        BacktestingResult backtesting,
        Dictionary<string, Dictionary<WeekKey, StrategyWeekData>> strategyWeeks)
    {
        var keys = strategyWeeks.Values.SelectMany(v => v.Keys).Distinct().OrderBy(k => k.Year).ThenBy(k => k.Week).ToList();
        if (keys.Any())
        {
            return keys.Select(k => (k, k.Year, k.Week, k.Start, k.Start.AddDays(6))).ToList();
        }

        return backtesting.WeeklyResults
            .OrderBy(w => w.Year)
            .ThenBy(w => w.Week)
            .Select(w => (new WeekKey(w.Year, w.Week, w.WeekStart), w.Year, w.Week, w.WeekStart, w.WeekEnd))
            .ToList();
    }

    private static TitanoStrategyMetrics CalculateMetrics(
        Dictionary<WeekKey, StrategyWeekData> weeklyData,
        List<(WeekKey Key, int Year, int Week, DateTime Start, DateTime End)> previousWeeks,
        decimal initialCapital)
    {
        var data = previousWeeks
            .Select(w => weeklyData.TryGetValue(w.Key, out var value) ? value : new StrategyWeekData())
            .ToList();

        if (!data.Any())
        {
            return new TitanoStrategyMetrics { ProfitFactor = 10m };
        }

        var profit = data.Sum(d => d.Profit);
        var gains = data.Where(d => d.Profit > 0).Sum(d => d.Profit);
        var losses = Math.Abs(data.Where(d => d.Profit < 0).Sum(d => d.Profit));
        var trades = data.Sum(d => d.Trades);
        var winningTrades = data.Sum(d => d.WinningTrades);
        var positiveWeeks = data.Count(d => d.Profit > 0);
        var losingStreak = 0;
        var maxLosingStreak = 0;
        decimal curve = 0m;
        decimal peak = 0m;
        decimal maxDrawdown = 0m;

        foreach (var week in data)
        {
            curve += week.Profit;
            peak = Math.Max(peak, curve);
            maxDrawdown = Math.Min(maxDrawdown, peak > 0 ? (curve - peak) / peak : 0m);
            losingStreak = week.Profit < 0 ? losingStreak + 1 : 0;
            maxLosingStreak = Math.Max(maxLosingStreak, losingStreak);
        }

        var weeklyReturns = data.Select(d => d.Profit).ToArray();

        return new TitanoStrategyMetrics
        {
            RollingProfit = profit,
            RollingMaxDrawdown = maxDrawdown,
            WinRate = data.Count > 0 ? (decimal)positiveWeeks / data.Count : 0m,
            TradeWinRate = trades > 0 ? (decimal)winningTrades / trades : 0m,
            Trades = trades,
            ProfitFactor = losses > 0 ? gains / losses : gains > 0 ? 10m : 0m,
            PositiveWeeksRatio = data.Count > 0 ? (decimal)positiveWeeks / data.Count : 0m,
            ConsecutiveLosingWeeks = maxLosingStreak,
            SharpeRatio = AdvancedMetrics.CalculateSharpeRatio(weeklyReturns)
        };
    }

    private static TitanoStrategyDecision Decide(
        string strategyKey,
        TitanoStrategyMetrics metrics,
        TitanoFilterRules rules,
        bool isWarmup,
        List<(WeekKey Key, int Year, int Week, DateTime Start, DateTime End)> previousWeeks,
        Dictionary<WeekKey, StrategyWeekData> weeklyData,
        decimal initialCapital)
    {
        var reasons = new List<string>();
        if (metrics.RollingProfit < rules.MinRollingProfit) reasons.Add($"profit rolling {metrics.RollingProfit:F2} < {rules.MinRollingProfit:F2}");
        if (metrics.RollingMaxDrawdown < rules.MaxRollingDrawdown) reasons.Add($"max DD {metrics.RollingMaxDrawdown:P1} < {rules.MaxRollingDrawdown:P1}");
        if (metrics.WinRate < rules.MinWinRate) reasons.Add($"settimane positive {metrics.WinRate:P1} < {rules.MinWinRate:P1}");
        if (metrics.PositiveWeeksRatio < rules.MinPositiveWeeksRatio) reasons.Add($"settimane positive {metrics.PositiveWeeksRatio:P1} < {rules.MinPositiveWeeksRatio:P1}");
        if (metrics.Trades < rules.MinTrades) reasons.Add($"trade {metrics.Trades} < {rules.MinTrades}");
        if (rules.MinTradeWinRate > 0 && metrics.TradeWinRate < rules.MinTradeWinRate) reasons.Add($"win rate trade {metrics.TradeWinRate:P1} < {rules.MinTradeWinRate:P1}");
        if (metrics.ProfitFactor < rules.MinProfitFactor) reasons.Add($"profit factor {metrics.ProfitFactor:F2} < {rules.MinProfitFactor:F2}");
        if (metrics.ConsecutiveLosingWeeks > rules.MaxConsecutiveLosingWeeks) reasons.Add($"loss streak {metrics.ConsecutiveLosingWeeks} > {rules.MaxConsecutiveLosingWeeks}");
        if (rules.MinSharpeRatio > 0 && metrics.SharpeRatio < rules.MinSharpeRatio) reasons.Add($"Sharpe {metrics.SharpeRatio:F2} < {rules.MinSharpeRatio:F2}");

        if (rules.MaxWeeklyLoss < 0 && previousWeeks.Any())
        {
            var lastWeek = previousWeeks[^1];
            if (weeklyData.TryGetValue(lastWeek.Key, out var lastData) && lastData.Profit < rules.MaxWeeklyLoss * initialCapital)
            {
                reasons.Add($"ultima settimana {lastData.Profit:F2} < {rules.MaxWeeklyLoss:P1} capitale");
            }
        }

        if (rules.MaxSingleWeekReturn > 0)
        {
            foreach (var previousWeek in previousWeeks)
            {
                if (weeklyData.TryGetValue(previousWeek.Key, out var weekData) &&
                    weekData.Profit > rules.MaxSingleWeekReturn * initialCapital)
                {
                    reasons.Add($"spike settimanale {weekData.Profit:F2} > {rules.MaxSingleWeekReturn:P1} capitale");
                    break;
                }
            }
        }

        var score = metrics.RollingProfit
            + metrics.ProfitFactor * 100m
            + metrics.WinRate * 100m
            + metrics.TradeWinRate * 100m
            + metrics.PositiveWeeksRatio * 100m
            + metrics.SharpeRatio * 50m
            + metrics.RollingMaxDrawdown * 100m
            - metrics.ConsecutiveLosingWeeks * 25m;

        return new TitanoStrategyDecision
        {
            StrategyKey = strategyKey,
            StrategyName = ExtractStrategyName(strategyKey),
            Symbol = ExtractSymbol(strategyKey),
            IsEnabled = isWarmup || reasons.Count == 0,
            Score = score,
            Metrics = metrics,
            Reasons = isWarmup ? new List<string> { $"warmup: servono {previousWeeks.Count} settimane di storia" } : reasons
        };
    }

    private static string BuildHtml(TitanoFilterResult result, BacktestingResult backtesting)
    {
        var symbols = result.StrategySummaries
            .Select(summary => NormalizeSymbol(summary.Symbol))
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(symbol => symbol)
            .ToList();
        var symbolsText = symbols.Any() ? string.Join(", ", symbols) : "N/D";

        var weeklySeries = result.WeeklyResults
            .OrderBy(w => w.WeekStart)
            .Select(w => new
            {
                t = TradingDateTime.ToFeedUtc(w.WeekStart).ToString("O"),
                original = w.OriginalEquity,
                filtered = w.FilteredEquity,
                active = w.EnabledStrategies.Count
            })
            .ToList();

        var hourlyEquitySeries = BuildHourlyEquitySeries(result, backtesting);
        var weeklyJson = JsonSerializer.Serialize(weeklySeries);
        var hourlyEquityJson = JsonSerializer.Serialize(hourlyEquitySeries);

        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html lang=\"it\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        html.AppendLine($"<title>Titano - {Escape(result.Name)}</title>");
        html.AppendLine("<style>body{font-family:Arial,Helvetica,sans-serif;margin:24px;background:#0f172a;color:#e5e7eb}.card{background:#111827;border:1px solid #334155;border-radius:12px;padding:18px;margin-bottom:16px}canvas{width:100%;height:520px;background:#020617;border-radius:10px}.legend{display:flex;flex-wrap:wrap;gap:12px;margin-top:14px}.legend span{display:inline-flex;align-items:center;gap:6px;font-size:13px}.swatch{width:14px;height:3px;display:inline-block}table{width:100%;border-collapse:collapse}td,th{border-bottom:1px solid #334155;padding:7px;text-align:right}td:first-child,th:first-child{text-align:left}.good{color:#22c55e}.bad{color:#fb7185}.metrics{display:flex;flex-wrap:wrap;gap:10px}.metric{background:#020617;border:1px solid #334155;border-radius:10px;padding:10px 12px;min-width:150px}.metric span{display:block;color:#94a3b8;font-size:12px}.metric b{display:block;color:#f8fafc;font-size:15px;margin-top:3px}.muted{color:#94a3b8;font-size:13px;margin:0 0 12px}</style></head><body>");
        html.AppendLine($"<h1>Titano - {Escape(result.Name)} <small>{Escape(result.Code)}</small></h1>");
        html.AppendLine("<div class=\"card\"><h2>Riepilogo simulazione</h2><div class=\"metrics\">");
        html.AppendLine($"<div class=\"metric\"><span>Range simulazione (UTC)</span><b>{result.StartDate:yyyy-MM-dd HH:mm}Z - {result.EndDate:yyyy-MM-dd HH:mm}Z</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>Symbol usati</span><b>{Escape(symbolsText)}</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>Strategie</span><b>{result.StrategySummaries.Count}</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>Trade originali</span><b>{result.OriginalTotalTrades}</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>Trade filtrati</span><b>{result.FilteredTotalTrades}</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>Trade non eseguiti per strategia sospesa</span><b>{result.SuspendedStrategyTrades}</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>Capitale iniziale</span><b>{result.InitialCapital:F2}</b></div>");
        html.AppendLine("</div></div>");
        AppendTitanoPeriodSummaryHtml(html, "Resoconto annuale Titano", result.WeeklyResults.GroupBy(w => w.Year).Select(g => new TitanoPeriodSummary(
            Label: g.Key.ToString(),
            OriginalProfit: g.Sum(w => w.OriginalWeeklyProfit),
            FilteredProfit: g.Sum(w => w.FilteredWeeklyProfit),
            OriginalTrades: g.Sum(w => w.OriginalWeeklyTrades),
            FilteredTrades: g.Sum(w => w.FilteredWeeklyTrades),
            OriginalWinningTrades: g.Sum(w => w.OriginalWinningTrades),
            OriginalLosingTrades: g.Sum(w => w.OriginalLosingTrades),
            FilteredWinningTrades: g.Sum(w => w.FilteredWinningTrades),
            FilteredLosingTrades: g.Sum(w => w.FilteredLosingTrades))).OrderBy(r => r.Label));
        AppendTitanoPeriodSummaryHtml(html, "Resoconto mensile Titano", result.WeeklyResults.GroupBy(w => new { w.WeekStart.Year, w.WeekStart.Month }).Select(g => new TitanoPeriodSummary(
            Label: $"{g.Key.Year}-{g.Key.Month:00}",
            OriginalProfit: g.Sum(w => w.OriginalWeeklyProfit),
            FilteredProfit: g.Sum(w => w.FilteredWeeklyProfit),
            OriginalTrades: g.Sum(w => w.OriginalWeeklyTrades),
            FilteredTrades: g.Sum(w => w.FilteredWeeklyTrades),
            OriginalWinningTrades: g.Sum(w => w.OriginalWinningTrades),
            OriginalLosingTrades: g.Sum(w => w.OriginalLosingTrades),
            FilteredWinningTrades: g.Sum(w => w.FilteredWinningTrades),
            FilteredLosingTrades: g.Sum(w => w.FilteredLosingTrades))).OrderBy(r => r.Label));

        html.AppendLine("<div class=\"card\">");
        html.AppendLine("<h2>Equity oraria - originale vs filtrato Titano</h2>");
        html.AppendLine("<p class=\"muted\">Equity globale del backtest e equity filtrata Titano (interpolata per settimana).</p>");
        html.AppendLine("<canvas id=\"hourlyEquityChart\" width=\"1400\" height=\"560\"></canvas>");
        html.AppendLine("<div id=\"hourlyEquityLegend\" class=\"legend\"></div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class=\"card\">");
        html.AppendLine("<h2>Equity settimanale - originale vs filtrato Titano</h2>");
        html.AppendLine("<canvas id=\"weeklyEquityChart\" width=\"1400\" height=\"520\"></canvas>");
        html.AppendLine("<div id=\"weeklyEquityLegend\" class=\"legend\"></div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class=\"card\">");
        html.AppendLine("<h2>Strategie attive nel tempo</h2>");
        html.AppendLine("<canvas id=\"strategyCountChart\" width=\"1400\" height=\"420\"></canvas>");
        html.AppendLine("<div id=\"strategyCountLegend\" class=\"legend\"></div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class=\"card\"><h2>Confronto</h2><table><tr><th>Metriche</th><th>Originale</th><th>Filtrato</th><th>Differenza</th></tr>");
        html.AppendLine($"<tr><td>Profit</td><td>{result.OriginalTotalProfit:F2}</td><td>{result.FilteredTotalProfit:F2}</td><td>{result.FilteredTotalProfit - result.OriginalTotalProfit:F2}</td></tr>");
        html.AppendLine($"<tr><td>Final equity</td><td>{result.OriginalFinalEquity:F2}</td><td>{result.FilteredFinalEquity:F2}</td><td>{result.FilteredFinalEquity - result.OriginalFinalEquity:F2}</td></tr>");
        html.AppendLine($"<tr><td>Max drawdown</td><td>{result.OriginalMaxDrawdown:F2}</td><td>{result.FilteredMaxDrawdown:P2}</td><td>{result.FilteredMaxDrawdown - result.OriginalMaxDrawdown:F2}</td></tr></table></div>");
        html.AppendLine("<div class=\"card\"><h2>Strategie</h2><table><tr><th>Strategia</th><th>Symbol</th><th>On</th><th>Off</th><th>Profit On</th><th>Profit Always</th></tr>");
        foreach (var s in result.StrategySummaries.Take(200))
        {
            html.AppendLine($"<tr><td>{Escape(s.StrategyName)}</td><td>{Escape(s.Symbol)}</td><td>{s.EnabledWeeks}</td><td>{s.DisabledWeeks}</td><td>{s.ProfitWhenEnabled:F2}</td><td>{s.ProfitIfAlwaysEnabled:F2}</td></tr>");
        }
        html.AppendLine("</table></div>");

        html.AppendLine("<script>");
        html.AppendLine($"const weeklySeries = {weeklyJson};");
        html.AppendLine($"const hourlyEquitySeries = {hourlyEquityJson};");
        html.AppendLine("function drawEquityChart(canvasId, legendId, series, originalKey, filteredKey) {");
        html.AppendLine("  const canvas = document.getElementById(canvasId);");
        html.AppendLine("  const legend = document.getElementById(legendId);");
        html.AppendLine("  if (!series.length) { legend.innerHTML = '<span>Nessun dato disponibile</span>'; return; }");
        html.AppendLine("  const points = series.map(p => ({...p, time: new Date(p.t).getTime()}));");
        html.AppendLine("  const ctx = canvas.getContext('2d');");
        html.AppendLine("  const pad = {left: 74, right: 24, top: 28, bottom: 54};");
        html.AppendLine("  const vals = points.flatMap(p => [p[originalKey], p[filteredKey]]);");
        html.AppendLine("  const min = Math.min(...vals), max = Math.max(...vals);");
        html.AppendLine("  const yMin = min === max ? min - 1 : min;");
        html.AppendLine("  const yMax = min === max ? max + 1 : max;");
        html.AppendLine("  const minTime = Math.min(...points.map(p => p.time));");
        html.AppendLine("  const maxTime = Math.max(...points.map(p => p.time));");
        html.AppendLine("  const x = t => pad.left + ((t - minTime) / Math.max(1, maxTime - minTime)) * (canvas.width - pad.left - pad.right);");
        html.AppendLine("  const y = v => canvas.height - pad.bottom - ((v - yMin) / Math.max(1, yMax - yMin)) * (canvas.height - pad.top - pad.bottom);");
        html.AppendLine("  ctx.clearRect(0, 0, canvas.width, canvas.height);");
        html.AppendLine("  ctx.strokeStyle = '#334155'; ctx.lineWidth = 1; ctx.fillStyle = '#94a3b8'; ctx.font = '12px Arial';");
        html.AppendLine("  for (let i = 0; i <= 5; i++) { const yy = pad.top + i * (canvas.height - pad.top - pad.bottom) / 5; ctx.beginPath(); ctx.moveTo(pad.left, yy); ctx.lineTo(canvas.width - pad.right, yy); ctx.stroke(); ctx.fillText((yMax - i * (yMax - yMin) / 5).toFixed(0), 8, yy + 4); }");
        html.AppendLine("  function line(key, color, width) { ctx.strokeStyle = color; ctx.lineWidth = width; ctx.beginPath(); points.forEach((p, i) => { const xx = x(p.time); const yy = y(p[key]); if (i === 0) ctx.moveTo(xx, yy); else ctx.lineTo(xx, yy); }); ctx.stroke(); }");
        html.AppendLine("  line(originalKey, '#38bdf8', 2.5);");
        html.AppendLine("  line(filteredKey, '#22c55e', 2.5);");
        html.AppendLine("  legend.innerHTML = '<span><i class=\"swatch\" style=\"background:#38bdf8\"></i>Equity originale</span><span><i class=\"swatch\" style=\"background:#22c55e\"></i>Equity filtrata Titano</span>';");
        html.AppendLine("}");
        html.AppendLine("function drawStrategyCountChart(canvasId, legendId, series) {");
        html.AppendLine("  const canvas = document.getElementById(canvasId);");
        html.AppendLine("  const legend = document.getElementById(legendId);");
        html.AppendLine("  if (!series.length) { legend.innerHTML = '<span>Nessun dato disponibile</span>'; return; }");
        html.AppendLine("  const points = series.map(p => ({...p, time: new Date(p.t).getTime()}));");
        html.AppendLine("  const ctx = canvas.getContext('2d');");
        html.AppendLine("  const pad = {left: 74, right: 24, top: 28, bottom: 54};");
        html.AppendLine("  const maxCount = Math.max(...points.map(p => p.active), 1);");
        html.AppendLine("  const minTime = Math.min(...points.map(p => p.time));");
        html.AppendLine("  const maxTime = Math.max(...points.map(p => p.time));");
        html.AppendLine("  const x = t => pad.left + ((t - minTime) / Math.max(1, maxTime - minTime)) * (canvas.width - pad.left - pad.right);");
        html.AppendLine("  const y = v => canvas.height - pad.bottom - (v / maxCount) * (canvas.height - pad.top - pad.bottom);");
        html.AppendLine("  ctx.clearRect(0, 0, canvas.width, canvas.height);");
        html.AppendLine("  ctx.strokeStyle = '#334155'; ctx.lineWidth = 1; ctx.fillStyle = '#94a3b8'; ctx.font = '12px Arial';");
        html.AppendLine("  const step = Math.max(1, Math.ceil(maxCount / 6));");
        html.AppendLine("  for (let v = 0; v <= maxCount; v += step) { const yy = y(v); ctx.beginPath(); ctx.moveTo(pad.left, yy); ctx.lineTo(canvas.width - pad.right, yy); ctx.stroke(); ctx.fillText(String(v), 12, yy + 4); }");
        html.AppendLine("  ctx.strokeStyle = '#f97316'; ctx.lineWidth = 2.5; ctx.beginPath();");
        html.AppendLine("  points.forEach((p, i) => { const xx = x(p.time); const yy = y(p.active); if (i === 0) ctx.moveTo(xx, yy); else ctx.lineTo(xx, yy); });");
        html.AppendLine("  ctx.stroke();");
        html.AppendLine("  legend.innerHTML = '<span><i class=\"swatch\" style=\"background:#f97316\"></i>Strategie attive (Titano ON)</span>';");
        html.AppendLine("}");
        html.AppendLine("drawEquityChart('hourlyEquityChart', 'hourlyEquityLegend', hourlyEquitySeries, 'original', 'filtered');");
        html.AppendLine("drawEquityChart('weeklyEquityChart', 'weeklyEquityLegend', weeklySeries, 'original', 'filtered');");
        html.AppendLine("drawStrategyCountChart('strategyCountChart', 'strategyCountLegend', weeklySeries);");
        html.AppendLine("</script></body></html>");
        return html.ToString();
    }

    private static List<object> BuildHourlyEquitySeries(TitanoFilterResult result, BacktestingResult backtesting)
    {
        var weeks = result.WeeklyResults.OrderBy(w => w.WeekStart).ToList();
        var hourly = backtesting.HourlyResults
            .Where(row => row.Equity != 0)
            .OrderBy(row => row.DateTime)
            .ToList();

        if (!weeks.Any() || !hourly.Any())
        {
            return new List<object>();
        }

        var points = new List<object>();
        foreach (var hour in hourly)
        {
            var hourUtc = TradingDateTime.ToFeedUtc(hour.DateTime);
            var weekIndex = weeks.FindLastIndex(w => hourUtc >= TradingDateTime.ToFeedUtc(w.WeekStart));
            if (weekIndex < 0)
            {
                weekIndex = 0;
            }

            var week = weeks[weekIndex];
            var weekStartFiltered = weekIndex == 0
                ? backtesting.InitialCapital
                : weeks[weekIndex - 1].FilteredEquity;

            var weekHours = hourly
                .Where(row =>
                    TradingDateTime.ToFeedUtc(row.DateTime) >= TradingDateTime.ToFeedUtc(week.WeekStart) &&
                    TradingDateTime.ToFeedUtc(row.DateTime) <= TradingDateTime.ToFeedUtc(week.WeekEnd))
                .ToList();

            var indexInWeek = weekHours.FindIndex(row => row.DateTime == hour.DateTime);
            if (indexInWeek < 0)
            {
                indexInWeek = weekHours.Count - 1;
            }

            var progress = weekHours.Count <= 1 ? 1m : (decimal)indexInWeek / (weekHours.Count - 1);
            var filtered = weekStartFiltered + (week.FilteredEquity - weekStartFiltered) * progress;

            points.Add(new
            {
                t = hourUtc.ToString("O"),
                original = hour.Equity,
                filtered
            });
        }

        return points;
    }

    private static string MakeStrategyKey(string symbol, string strategyCode) => $"{NormalizeSymbol(symbol)}|{strategyCode.Trim()}";
    private static void AppendTitanoPeriodSummaryHtml(StringBuilder html, string title, IEnumerable<TitanoPeriodSummary> rows)
    {
        var periodRows = rows.ToList();
        if (!periodRows.Any())
        {
            return;
        }

        html.AppendLine($"<div class=\"card\"><h2>{Escape(title)}</h2><table>");
        html.AppendLine("<tr><th>Periodo</th><th>Profit originale</th><th>Profit filtrato</th><th>Trade originali</th><th>Trade filtrati</th><th>Eliminati da Titano</th><th>Win originali</th><th>Persi originali</th><th>Win filtrati</th><th>Persi filtrati</th></tr>");

        foreach (var row in periodRows)
        {
            var removedTrades = Math.Max(0, row.OriginalTrades - row.FilteredTrades);
            html.AppendLine($"<tr><td>{Escape(row.Label)}</td><td>{row.OriginalProfit:F2}</td><td>{row.FilteredProfit:F2}</td><td>{row.OriginalTrades}</td><td>{row.FilteredTrades}</td><td>{removedTrades}</td><td>{row.OriginalWinningTrades}</td><td>{row.OriginalLosingTrades}</td><td>{row.FilteredWinningTrades}</td><td>{row.FilteredLosingTrades}</td></tr>");
        }

        html.AppendLine("</table></div>");
    }

    private static string NormalizeSymbol(string symbol) => symbol.Trim().TrimStart('@').ToUpperInvariant();
    private static string ExtractSymbol(string key) => key.Split('|', 2)[0];
    private static string ExtractStrategyName(string key) => key.Split('|', 2).Length == 2 ? key.Split('|', 2)[1] : key;
    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value);
    private static string MakeSafeFileName(string value) => string.Join("_", value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();

    private sealed class StrategyWeekData
    {
        public decimal Profit { get; set; }
        public int Trades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
    }

    private sealed record TitanoPeriodSummary(
        string Label,
        decimal OriginalProfit,
        decimal FilteredProfit,
        int OriginalTrades,
        int FilteredTrades,
        int OriginalWinningTrades,
        int OriginalLosingTrades,
        int FilteredWinningTrades,
        int FilteredLosingTrades);

    private readonly record struct WeekKey(int Year, int Week, DateTime Start)
    {
        public static WeekKey FromDate(DateTime date)
        {
            var culture = CultureInfo.CurrentCulture;
            var week = culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var daysToSubtract = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysToSubtract < 0) daysToSubtract += 7;
            return new WeekKey(date.Year, week, date.AddDays(-daysToSubtract).Date);
        }
    }
}
