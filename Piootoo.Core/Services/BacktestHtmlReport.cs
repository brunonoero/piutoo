using System.Globalization;
using System.Text;
using System.Text.Json;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>
/// Trade chiuso nella forma minima che serve al report: chi, dove, quando è uscito e con quale
/// risultato netto.
/// </summary>
/// <remarks>
/// Il report non è generato solo dal motore interno, che ha in mano i <see cref="TradingResult"/>
/// dell'engine: è generato anche a posteriori dal <c>trades.json</c> di un run esterno, dove quegli
/// oggetti non esistono più. Un tipo proprio evita di ricostruire un <see cref="TradingResult"/>
/// finto — il suo <c>GrossProfit</c> è calcolato dai prezzi per il valore punto, e un run esterno
/// riporta il denaro già fatto, non il valore punto con cui è stato fatto.
/// </remarks>
public sealed record BacktestReportTrade(
    string Symbol,
    string StrategyCode,
    string StrategyName,
    DateTime ExitDateUtc,
    decimal NetProfit)
{
    public static BacktestReportTrade From(TradingResult trade) => new(
        trade.Symbol,
        trade.StrategyCode,
        trade.StrategyName,
        trade.ExitDate,
        trade.NetProfit);

    public static BacktestReportTrade From(PersistedTrade trade) => new(
        trade.Symbol,
        trade.StrategyCode,
        trade.StrategyName,
        trade.ExitTimeUtc,
        trade.NetProfit);
}

/// <summary>
/// Il piano che ha governato il run, nella forma che serve al report: chi ha deciso l'universo, cosa
/// ha spento, cosa ha imposto su tenuta e commissione.
/// </summary>
/// <remarks>
/// Un tipo proprio e non il <c>TradingPlan</c>: il report si scrive anche a posteriori dagli
/// artefatti di un run esterno, dove del piano resta il solo codice nel marcatore di origine, e il
/// file su disco puo' nel frattempo essere cambiato. Cio' che il report deve dichiarare e' quello
/// che il run ha <i>applicato</i>, non quello che il piano dice oggi.
/// </remarks>
public sealed record BacktestPlanReportInfo(
    string? PlanCode,
    string? PlanName = null,
    string? BrokerCode = null,
    string? SymbolConversionCode = null,
    /// <summary>
    /// Le strategie che il piano ha lasciato girare, per nome di esecuzione: quelle che si
    /// ritrovano in <c>trades.json</c>. Si elencano le <b>attive</b> e non le escluse — chi legge un
    /// report vuole sapere cosa ha prodotto quella equity, non cosa non l'ha prodotta; il conto
    /// delle esclusioni, e il perche' di ciascuna, restano in <c>backtest-summary.json</c>.
    /// Vuota quando il report e' ricostruito a posteriori e l'elenco non e' piu' ricavabile.
    /// </summary>
    IReadOnlyList<string>? ActiveStrategies = null,
    AccountHoldingPolicy? Holding = null,
    decimal? CommissionPerContract = null);

/// <summary>
/// Lo spread che il run ha applicato all'ingresso, per simbolo, nella forma che serve al report.
/// </summary>
/// <param name="Source">
/// Da dove vengono i numeri: broker, statistica e file della misura, oppure la nota che sono stati
/// scritti a mano. E' meta' dell'informazione — <c>@NQ 2,00</c> da solo non dice se e' la mediana di
/// agosto su FTMO o una media di due anni fa su un altro conto.
/// </param>
/// <param name="Points">Spread in punti dello strumento, per simbolo. Vuoto = run senza spread.</param>
public sealed record BacktestSpreadReportInfo(
    string Source,
    IReadOnlyDictionary<string, decimal> Points,
    IReadOnlyDictionary<string, IReadOnlyList<decimal>>? PointsByHour = null);

/// <summary>
/// Report HTML di un backtest: riepilogo, resoconto annuale e mensile, equity globale e per
/// strategia. Il file è autosufficiente — grafici disegnati su canvas, nessuna risorsa esterna —
/// perché viene aperto sia dal visualizzatore della console sia dal browser, offline.
/// </summary>
/// <remarks>
/// Vive fuori da <see cref="PiootooBacktestingService"/> perché ha due chiamanti: il motore interno
/// a fine run, e <see cref="ExternalBacktestReportService"/> per i run dell'engine esterno, che i
/// trade li archivia ma il report no. Due generatori avrebbero prodotto due report diversi per gli
/// stessi trade, e il confronto interno/esterno è proprio ciò a cui il report serve.
/// </remarks>
public static class BacktestHtmlReport
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Note in testa al report, per dichiarare come è stato costruito quando non viene dal run.
    /// Vuote per il motore interno: lì il report è l'artefatto del run, non una ricostruzione.
    /// </summary>
    private static void AppendNotesHtml(StringBuilder html, IReadOnlyList<string>? notes)
    {
        if (notes is null || notes.Count == 0)
        {
            return;
        }

        html.AppendLine("  <div class=\"card\">");
        foreach (var note in notes)
        {
            html.AppendLine($"    <p class=\"muted\">{System.Net.WebUtility.HtmlEncode(note)}</p>");
        }

        html.AppendLine("  </div>");
    }

    /// <summary>
    /// Il feed da cui il run ha letto le barre, sotto il titolo. Non e' un dettaglio da riepilogo:
    /// due curve di equity prodotte su archivi diversi non sono confrontabili nemmeno quando motore,
    /// strategie e periodo coincidono, e chi apre il file mesi dopo non ha altro modo di saperlo.
    /// Un run che non lo dichiara lo dice, invece di lasciar credere al datafeed interno.
    /// </summary>
    private static void AppendFeedHtml(StringBuilder html, RunPriceSource? priceSource)
    {
        var label = (priceSource ?? new RunPriceSource()).FeedLabel;
        html.AppendLine($"  <p class=\"feed\">Feed: {System.Net.WebUtility.HtmlEncode(label)}</p>");
    }

    /// <summary>
    /// Il piano che ha governato il run. Vale la stessa ragione del feed: universo, strategie
    /// spente, orari di tenuta e commissione cambiano l'equity senza comparire in un solo trade, e
    /// due report identici in tutto il resto possono descrivere piani diversi. Un run <i>senza</i>
    /// piano lo dice: il silenzio si leggerebbe come "il piano c'era, non l'abbiamo scritto".
    /// </summary>
    private static void AppendPlanHtml(StringBuilder html, BacktestPlanReportInfo? plan)
    {
        if (plan is null || string.IsNullOrWhiteSpace(plan.PlanCode))
        {
            html.AppendLine("  <p class=\"feed\">Piano: nessuno — intero masterfilter, parametri del run.</p>");
            return;
        }

        var name = string.IsNullOrWhiteSpace(plan.PlanName)
            ? plan.PlanCode
            : $"{plan.PlanName} · {plan.PlanCode}";

        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Piano</h2>");
        html.AppendLine("    <div class=\"metrics\">");
        html.AppendLine($"      <div class=\"metric\"><span>Piano</span><b>{System.Net.WebUtility.HtmlEncode(name)}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Broker</span><b>{System.Net.WebUtility.HtmlEncode(Or(plan.BrokerCode, "nessuno"))}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Tabella simboli</span><b>{System.Net.WebUtility.HtmlEncode(Or(plan.SymbolConversionCode, "nessuna (opera tutto)"))}</b></div>");

        if (plan.ActiveStrategies is { Count: > 0 } active)
        {
            html.AppendLine($"      <div class=\"metric\"><span>Strategie attive</span><b>{active.Count}</b></div>");
        }

        if (plan.Holding is { } holding)
        {
            html.AppendLine($"      <div class=\"metric\"><span>Overnight</span><b>{(holding.AllowOvernight ? "permesso" : $"piatto {holding.SessionFlatUtcHhmm:0000}Z")}</b></div>");
            html.AppendLine($"      <div class=\"metric\"><span>Fine settimana</span><b>{(holding.AllowOverweek ? "permesso" : $"piatto {holding.WeekEnd.FromUtcHhmm:0000}Z")}</b></div>");
        }

        if (plan.CommissionPerContract is { } commission)
        {
            html.AppendLine($"      <div class=\"metric\"><span>Commissione</span><b>{commission:F2} / contratto</b></div>");
        }

        html.AppendLine("    </div>");

        // L'elenco per esteso sotto le metriche: il numero da solo dice quante hanno operato, non
        // quali, e due run dello stesso piano a distanza di settimane possono avere lo stesso conto
        // con strategie diverse — il masterfilter cambia, e il piano spegne per Id.
        if (plan.ActiveStrategies is { Count: > 0 } names)
        {
            var joined = string.Join(", ", names.Select(System.Net.WebUtility.HtmlEncode));
            html.AppendLine($"    <p class=\"muted\" style=\"font-size:12px;line-height:1.6\">{joined}</p>");
        }

        html.AppendLine("  </div>");

        static string Or(string? value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    /// <summary>
    /// Lo spread applicato all'ingresso, un valore per simbolo. Vale la stessa ragione del feed e
    /// del piano: cambia l'equity senza comparire in un solo trade, e due report identici in tutto
    /// il resto possono descrivere run con costi di transazione diversi.
    ///
    /// <para>Si elencano <b>tutti i simboli del run</b> e non i soli misurati: un simbolo senza
    /// spread e' un'assenza di misura, e una riga mancante si legge come "non c'era niente da dire".
    /// E' esattamente il caso che si vuole vedere — una strategia che gira gratis perche' il suo
    /// strumento non e' mai stato misurato.</para>
    ///
    /// <para>Un run <i>senza</i> spread lo dichiara invece di tacere: il silenzio si leggerebbe come
    /// "lo spread c'era, non l'abbiamo scritto", ed e' il malinteso che rende due cartelle
    /// indistinguibili.</para>
    /// </summary>
    private static void AppendSpreadHtml(
        StringBuilder html,
        BacktestSpreadReportInfo? spread,
        IReadOnlyList<string> symbols)
    {
        // Null NON e' "nessuno spread": e' "di spread qui non si puo' dire niente". E' il caso del
        // report ricostruito da un run dell'engine esterno, dove i trade vengono da un conto vero e
        // lo spread l'ha pagato il broker, non un modello. Dichiarare "nessuno" li' sarebbe falso.
        if (spread is null)
            return;

        if (spread.Points.Count == 0)
        {
            html.AppendLine("  <p class=\"feed\">Spread: nessuno — ingressi al prezzo del feed.</p>");
            return;
        }

        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Spread applicato all'ingresso</h2>");
        html.AppendLine($"    <p class=\"muted\">Misura: {System.Net.WebUtility.HtmlEncode(spread.Source)}. " +
                        "Peggiora il solo prezzo di ingresso (long +spread, short −spread); " +
                        "trigger, livelli e uscite restano sul prezzo del feed.</p>");

        // Con la risoluzione per ora la colonna del simbolo non e' piu' cio' che il run ha pagato ma
        // il ripiego delle ore senza quotazione: dirlo qui evita di leggere quel numero come il
        // costo, che e' l'unico modo in cui questa scheda puo' ingannare.
        var byHour = spread.PointsByHour is { Count: > 0 } ? spread.PointsByHour : null;
        if (byHour is not null)
        {
            html.AppendLine("    <p class=\"muted\">Risoluzione per ora UTC: si applica il valore dell'ora " +
                            "dell'ingresso. La colonna «Spread» e' il ripiego delle ore che il broker non " +
                            "ha quotato, non il costo del run.</p>");
        }

        html.AppendLine("    <table class=\"summary-table\">");
        html.AppendLine(byHour is null
            ? "      <thead><tr><th>Symbol</th><th>Spread (punti)</th></tr></thead>"
            : "      <thead><tr><th>Symbol</th><th>Spread (punti)</th><th>Per ora: min → max (UTC)</th></tr></thead>");
        html.AppendLine("      <tbody>");

        // I simboli del run per primi, poi gli eventuali misurati che il run non ha toccato: la
        // tabella copre venti strumenti e il run spesso due, e vedere gli altri aiuta a capire se la
        // misura e' quella giusta.
        var listed = new List<string>(symbols);
        foreach (var measured in spread.Points.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
        {
            if (!listed.Contains(measured, StringComparer.OrdinalIgnoreCase))
                listed.Add(measured);
        }

        foreach (var symbol in listed)
        {
            var used = symbols.Contains(symbol, StringComparer.OrdinalIgnoreCase);
            var value = spread.Points.TryGetValue(symbol, out var points)
                ? points.ToString("0.####", CultureInfo.InvariantCulture)
                : "non misurato";

            var label = System.Net.WebUtility.HtmlEncode(symbol) + (used ? string.Empty : " <span class=\"muted\">(non usato dal run)</span>");

            if (byHour is null)
            {
                html.AppendLine($"        <tr><td>{label}</td><td>{value}</td></tr>");
                continue;
            }

            html.AppendLine($"        <tr><td>{label}</td><td>{value}</td><td>{DescribeHours(byHour, symbol)}</td></tr>");
        }

        html.AppendLine("      </tbody>");
        html.AppendLine("    </table>");
        html.AppendLine("  </div>");
    }

    /// <summary>
    /// L'escursione oraria di un simbolo con le <b>ore</b> in cui cade, non i soli due numeri: chi
    /// legge il report sa in che ore opera la strategia, e «12 punti alle 22 UTC» gli dice se quel
    /// massimo la riguarda o e' la riapertura serale che non vede mai.
    /// </summary>
    private static string DescribeHours(
        IReadOnlyDictionary<string, IReadOnlyList<decimal>> byHour,
        string symbol)
    {
        if (!byHour.TryGetValue(symbol, out var hours) || hours.Count == 0)
            return "<span class=\"muted\">costante per simbolo</span>";

        var minHour = 0;
        var maxHour = 0;
        for (var hour = 1; hour < hours.Count; hour++)
        {
            if (hours[hour] < hours[minHour])
                minHour = hour;
            if (hours[hour] > hours[maxHour])
                maxHour = hour;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:0.####} ({1:00}) → {2:0.####} ({3:00})",
            hours[minHour], minHour, hours[maxHour], maxHour);
    }

    public static void Write(
        string filePath,
        BacktestingResult result,
        IReadOnlyList<BacktestReportTrade> closedTrades,
        IReadOnlyList<string>? notes = null,
        BacktestPlanReportInfo? plan = null,
        BacktestSpreadReportInfo? spread = null)
    {
        var series = result.StrategyResults
            .Where(row => row.Equity != 0)
            .GroupBy(row => StrategyKeys.MakeStrategyKey(row.Symbol, StrategyKeys.CodeOf(row)))
            .Select(group => new
            {
                key = group.Key,
                label = group.Key,
                points = group
                    .OrderBy(row => row.DateTime)
                    .Select(row => new
                    {
                        t = row.DateTime.ToString("O"),
                        equity = row.Equity,
                        profit = row.Profit,
                        signal = row.Signal?.ToString()
                    })
                    .ToList()
            })
            .Where(group => group.points.Any())
            .ToList();

        var chartJson = JsonSerializer.Serialize(series, JsonOptions);
        var globalSeries = BuildGlobalChartPoints(result.HourlyResults)
            .Select(row => new
            {
                t = row.DateTime.ToString("O"),
                equity = row.Equity,
                profit = row.Profit,
                drawdown = row.Drawdown
            })
            .ToList();
        var globalChartJson = JsonSerializer.Serialize(globalSeries, JsonOptions);
        var title = System.Net.WebUtility.HtmlEncode($"{result.SetupName} - Equity per strategia");
        var symbols = result.StrategiesInfo
            .Select(info => StrategyKeys.NormalizeSymbol(info.Symbol))
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(symbol => symbol)
            .ToList();
        if (!symbols.Any())
        {
            symbols = result.StrategyResults
                .Select(row => StrategyKeys.NormalizeSymbol(row.Symbol))
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(symbol => symbol)
                .ToList();
        }
        var symbolsText = symbols.Any() ? string.Join(", ", symbols) : "N/D";
        // Un segnale non implica un trade: uno stop può scadere senza fill e un ingresso può
        // restare aperto. Il report usa esclusivamente i trade chiusi dall'engine, che sono poi
        // persistiti in trades.json.
        var totalTrades = closedTrades.Count;
        var strategyCount = result.StrategiesInfo
            .Select(info => StrategyKeys.MakeStrategyKey(info.Symbol, StrategyKeys.CodeOf(info)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (strategyCount == 0)
        {
            strategyCount = series.Count;
        }
        var html = new StringBuilder();

        if (!series.Any() && !globalSeries.Any())
        {
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html lang=\"it\"><head><meta charset=\"utf-8\">");
            html.AppendLine($"<title>{title}</title>");
            html.AppendLine("<style>body{font-family:Arial,Helvetica,sans-serif;margin:24px;background:#0f172a;color:#e5e7eb}.card{background:#111827;border:1px solid #334155;border-radius:12px;padding:18px;margin-bottom:16px}.muted{color:#94a3b8}.feed{color:#94a3b8;font-size:13px;margin:-8px 0 18px}.metrics{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}.metric{background:#020617;border:1px solid #334155;border-radius:10px;padding:10px 12px}.metric b{display:block;color:#f8fafc}.summary-table{width:100%;border-collapse:collapse;margin-top:10px}.summary-table th,.summary-table td{border-bottom:1px solid #334155;padding:9px 10px;text-align:right}.summary-table th:first-child,.summary-table td:first-child{text-align:left}.positive{color:#22c55e}.negative{color:#fb7185}.top-strategies{text-align:left;font-size:12px;line-height:1.5}</style>");
            html.AppendLine("</head><body>");
            html.AppendLine($"<h1>{title}</h1>");
            AppendFeedHtml(html, result.PriceSource);
            AppendNotesHtml(html, notes);
            AppendPlanHtml(html, plan);
            AppendSpreadHtml(html, spread, symbols);
            AppendBacktestSummaryHtml(html, result, symbolsText, totalTrades, strategyCount);
            AppendYearlySummaryHtml(html, result, closedTrades);
            AppendMonthlySummaryHtml(html, result, closedTrades);
            html.AppendLine("<div class=\"card\"><p class=\"muted\">Nessuna equity per strategia disponibile: il backtest non ha prodotto trade gestiti dal motore.</p></div>");
            html.AppendLine("</body></html>");
            AtomicFileWriter.WriteAllText(filePath, html.ToString());
            return;
        }

        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"it\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine($"  <title>{title}</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body{font-family:Arial,Helvetica,sans-serif;margin:24px;background:#0f172a;color:#e5e7eb}");
        html.AppendLine("    .card{background:#111827;border:1px solid #334155;border-radius:12px;padding:18px;margin-bottom:16px}");
        html.AppendLine("    canvas{width:100%;height:560px;background:#020617;border-radius:10px}");
        html.AppendLine("    .legend{display:flex;flex-wrap:wrap;gap:12px;margin-top:14px}");
        html.AppendLine("    .legend span{display:inline-flex;align-items:center;gap:6px;font-size:13px}");
        html.AppendLine("    .swatch{width:14px;height:3px;display:inline-block}");
        html.AppendLine("    .muted{color:#94a3b8}");
        html.AppendLine("    .feed{color:#94a3b8;font-size:13px;margin:-8px 0 18px}");
        html.AppendLine("    .metrics{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0 18px}");
        html.AppendLine("    .metric{background:#020617;border:1px solid #334155;border-radius:10px;padding:10px 12px;min-width:150px}");
        html.AppendLine("    .metric span{display:block;color:#94a3b8;font-size:12px}");
        html.AppendLine("    .metric b{display:block;color:#f8fafc;font-size:15px;margin-top:3px}");
        html.AppendLine("    .summary-table{width:100%;border-collapse:collapse;margin-top:10px}");
        html.AppendLine("    .summary-table th,.summary-table td{border-bottom:1px solid #334155;padding:9px 10px;text-align:right}");
        html.AppendLine("    .summary-table th:first-child,.summary-table td:first-child{text-align:left}");
        html.AppendLine("    .positive{color:#22c55e}");
        html.AppendLine("    .negative{color:#fb7185}");
        html.AppendLine("    .summary-table td.top-strategies{text-align:left;font-size:12px;line-height:1.5}");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"  <h1>{title}</h1>");
        AppendFeedHtml(html, result.PriceSource);
        AppendNotesHtml(html, notes);
        AppendPlanHtml(html, plan);
        AppendSpreadHtml(html, spread, symbols);
        AppendBacktestSummaryHtml(html, result, symbolsText, totalTrades, strategyCount);
        AppendYearlySummaryHtml(html, result, closedTrades);
        AppendMonthlySummaryHtml(html, result, closedTrades);
        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Equity globale</h2>");
        html.AppendLine("    <canvas id=\"globalEquityChart\" width=\"1400\" height=\"560\"></canvas>");
        html.AppendLine("    <div id=\"globalLegend\" class=\"legend\"></div>");
        html.AppendLine("  </div>");
        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Equity per strategia</h2>");
        html.AppendLine("    <canvas id=\"equityChart\" width=\"1400\" height=\"560\"></canvas>");
        html.AppendLine("    <div id=\"legend\" class=\"legend\"></div>");
        html.AppendLine("  </div>");
        html.AppendLine("  <script>");
        html.AppendLine($"    const series = {chartJson};");
        html.AppendLine($"    const globalSeries = {globalChartJson};");
        html.AppendLine("    const colors = ['#38bdf8','#f97316','#22c55e','#e879f9','#facc15','#fb7185','#a78bfa','#2dd4bf','#c084fc','#f87171'];");
        html.AppendLine("    const pad2 = n => String(n).padStart(2, '0');");
        html.AppendLine("    function timeTicks(minTime, maxTime) {");
        html.AppendLine("      const span = Math.max(1, maxTime - minTime), HOUR = 3600000, DAY = 24 * HOUR, target = 14;");
        html.AppendLine("      const ticks = [], push = t => { if (t >= minTime && t <= maxTime) ticks.push(t); };");
        // Sotto i tre giorni l'unita' leggibile e' l'ora: su una finestra cosi' corta due tick
        // giornalieri direbbero solo che il run e' durato poco.
        html.AppendLine("      if (span <= 3 * DAY) {");
        html.AppendLine("        const step = [HOUR, 2 * HOUR, 3 * HOUR, 6 * HOUR, 12 * HOUR].find(s => span / s <= target) || DAY;");
        html.AppendLine("        for (let cursor = Math.ceil(minTime / step) * step; cursor <= maxTime; cursor += step) push(cursor);");
        html.AppendLine("        return { ticks, label: t => { const d = new Date(t); return pad2(d.getUTCDate()) + '/' + pad2(d.getUTCMonth() + 1) + ' ' + pad2(d.getUTCHours()) + ':' + pad2(d.getUTCMinutes()); } };");
        html.AppendLine("      }");
        // Fino a ~sette mesi si ancora alla mezzanotte UTC del primo giorno e si avanza a passo
        // fisso: i tick restano allineati fra il grafico globale e quello per strategia.
        html.AppendLine("      if (span <= 200 * DAY) {");
        html.AppendLine("        const stepDays = [1, 2, 3, 7, 14, 28].find(d => span / (d * DAY) <= target) || 28;");
        html.AppendLine("        const first = new Date(minTime);");
        html.AppendLine("        let cursor = Date.UTC(first.getUTCFullYear(), first.getUTCMonth(), first.getUTCDate());");
        html.AppendLine("        while (cursor <= maxTime) { push(cursor); cursor += stepDays * DAY; }");
        html.AppendLine("        return { ticks, label: t => { const d = new Date(t); return pad2(d.getUTCDate()) + '/' + pad2(d.getUTCMonth() + 1); } };");
        html.AppendLine("      }");
        // Oltre, il mese di calendario: i mesi non hanno la stessa lunghezza, quindi qui si cammina
        // sul calendario e si dirada, invece di sommare millisecondi.
        html.AppendLine("      const months = [];");
        html.AppendLine("      { const first = new Date(minTime); let cursor = Date.UTC(first.getUTCFullYear(), first.getUTCMonth(), 1); while (cursor <= maxTime) { if (cursor >= minTime) months.push(cursor); const c = new Date(cursor); cursor = Date.UTC(c.getUTCFullYear(), c.getUTCMonth() + 1, 1); } }");
        html.AppendLine("      const stride = Math.max(1, Math.ceil(months.length / 14));");
        html.AppendLine("      months.forEach((t, i) => { if (i % stride === 0) ticks.push(t); });");
        html.AppendLine("      return { ticks, label: t => { const d = new Date(t); return pad2(d.getUTCMonth() + 1) + '/' + d.getUTCFullYear(); } };");
        html.AppendLine("    }");
        html.AppendLine("    function drawChart(canvasId, legendId, chartSeries, showDrawdown = false) {");
        html.AppendLine("      const canvas = document.getElementById(canvasId);");
        html.AppendLine("      const legend = document.getElementById(legendId);");
        html.AppendLine("      if (!chartSeries.length) { legend.innerHTML = '<span>Nessun dato disponibile</span>'; return; }");
        html.AppendLine("      const ctx = canvas.getContext('2d');");
        html.AppendLine("      const pad = {left: 74, right: showDrawdown ? 74 : 24, top: 28, bottom: 54};");
        // `Math.min(...array)` passa un argomento per elemento: sopra ~100k punti supera il limite
        // dello stack e lancia RangeError. Il grafico globale (una serie) restava sotto la soglia,
        // quello per strategia (n serie sulle stesse barre) no, e non veniva disegnato affatto.
        // Qui si scandisce in un passaggio solo, memorizzando anche il timestamp già convertito:
        // `new Date(p.t).getTime()` veniva altrimenti rifatto per ogni punto a ogni disegno.
        html.AppendLine("      let minTime = Infinity, maxTime = -Infinity, minEquity = Infinity, maxEquity = -Infinity;");
        html.AppendLine("      for (const s of chartSeries) { for (const p of s.points) { if (p.time === undefined) { p.time = new Date(p.t).getTime(); } if (p.time < minTime) minTime = p.time; if (p.time > maxTime) maxTime = p.time; if (p.equity < minEquity) minEquity = p.equity; if (p.equity > maxEquity) maxEquity = p.equity; } }");
        html.AppendLine("      if (!isFinite(minTime) || !isFinite(minEquity)) { legend.innerHTML = '<span>Nessun dato disponibile</span>'; return; }");
        html.AppendLine("      const yMin = minEquity === maxEquity ? minEquity - 1 : minEquity;");
        html.AppendLine("      const yMax = minEquity === maxEquity ? maxEquity + 1 : maxEquity;");
        html.AppendLine("      const x = t => pad.left + ((t - minTime) / Math.max(1, maxTime - minTime)) * (canvas.width - pad.left - pad.right);");
        html.AppendLine("      const y = v => canvas.height - pad.bottom - ((v - yMin) / Math.max(1, yMax - yMin)) * (canvas.height - pad.top - pad.bottom);");
        html.AppendLine("      ctx.clearRect(0,0,canvas.width,canvas.height);");
        html.AppendLine("      ctx.strokeStyle = '#334155'; ctx.lineWidth = 1; ctx.fillStyle = '#94a3b8'; ctx.font = '12px Arial';");
        html.AppendLine("      for (let i=0;i<=5;i++){ const yy = pad.top + i*(canvas.height-pad.top-pad.bottom)/5; ctx.beginPath(); ctx.moveTo(pad.left,yy); ctx.lineTo(canvas.width-pad.right,yy); ctx.stroke(); const val = yMax - i*(yMax-yMin)/5; ctx.fillText(val.toFixed(2), 8, yy+4); }");
        // Asse X: il passo lo sceglie la DURATA del run, non il calendario. Un tick al mese lasciava
        // un backtest di trenta giorni con una etichetta sola — e quella, per giunta, poteva cadere
        // fuori dal range e sparire — quindi su periodi brevi si scende a giorni e poi a ore,
        // puntando a una decina di etichette qualunque sia la scala. Tutti i timestamp sono UTC
        // (invariante del progetto): si usano getUTC* e non le varianti locali, altrimenti giorno e
        // mese cambierebbero con il fuso del browser.
        html.AppendLine("      const tickInfo = timeTicks(minTime, maxTime);");
        html.AppendLine("      ctx.textAlign = 'center';");
        html.AppendLine("      tickInfo.ticks.forEach(tick => { const xx = x(tick); ctx.strokeStyle = '#1e293b'; ctx.beginPath(); ctx.moveTo(xx, pad.top); ctx.lineTo(xx, canvas.height - pad.bottom); ctx.stroke(); ctx.fillStyle = '#94a3b8'; ctx.fillText(tickInfo.label(tick), xx, canvas.height - pad.bottom + 20); });");
        html.AppendLine("      ctx.textAlign = 'left'; ctx.strokeStyle = '#334155'; ctx.fillStyle = '#94a3b8';");
        // Stesso `Math.max(...array)` di cui sopra, sopravvissuto sul drawdown: e' l'ultimo punto
        // del disegno che passa un argomento per elemento, e su 570.000 punti lancia RangeError.
        // Lanciandolo qui, dentro la prima chiamata a drawChart, interrompeva lo script prima della
        // seconda: non spariva il solo grafico globale, sparivano tutti e due.
        html.AppendLine("      let maxDd = 0; if (showDrawdown) { for (const p of chartSeries[0].points) { const v = Math.abs(p.drawdown || 0); if (v > maxDd) maxDd = v; } }");
        html.AppendLine("      if (showDrawdown) { const dd = chartSeries[0].points; const plotH = canvas.height-pad.top-pad.bottom; const barW = Math.max(1, (canvas.width-pad.left-pad.right)/Math.max(1,dd.length)*0.8); ctx.fillStyle='rgba(239,68,68,0.28)'; dd.forEach(p=>{ const h=maxDd===0?0:Math.abs(p.drawdown||0)/maxDd*plotH; ctx.fillRect(x(p.time)-barW/2,canvas.height-pad.bottom-h,barW,h); }); ctx.fillStyle='#fca5a5'; for(let i=0;i<=5;i++){ const val=maxDd*(5-i)/5; const yy=pad.top+i*plotH/5; ctx.fillText(val.toFixed(2),canvas.width-pad.right+8,yy+4); } }");
        html.AppendLine("      chartSeries.forEach((s, idx) => { const color = colors[idx % colors.length]; ctx.strokeStyle = color; ctx.lineWidth = 2; ctx.beginPath(); s.points.forEach((p, i) => { const xx = x(p.time); const yy = y(p.equity); if(i===0) ctx.moveTo(xx, yy); else ctx.lineTo(xx, yy); }); ctx.stroke(); });");
        html.AppendLine("      legend.innerHTML = chartSeries.map((s,idx)=>`<span><i class=\"swatch\" style=\"background:${colors[idx % colors.length]}\"></i>${s.label}</span>`).join('') + (showDrawdown ? '<span><i class=\"swatch\" style=\"background:rgba(239,68,68,.55)\"></i>Drawdown globale (scala destra)</span>' : '');");
        html.AppendLine("    }");
        html.AppendLine("    drawChart('globalEquityChart', 'globalLegend', [{ label: 'Equity globale', points: globalSeries }], true);");
        html.AppendLine("    drawChart('equityChart', 'legend', series);");
        html.AppendLine("  </script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        AtomicFileWriter.WriteAllText(filePath, html.ToString());
    }

    private static void AppendBacktestSummaryHtml(
        StringBuilder html,
        BacktestingResult result,
        string symbolsText,
        int totalTrades,
        int strategyCount)
    {
        // `result.MaxDrawdown` è già una percentuale (TradingState.UpdateDrawdown moltiplica per
        // 100): mostrarla accanto a `maxDrawdownPercent` ripeteva lo stesso numero facendolo
        // passare per un importo. Le due metriche sono ricalcolate qui dalla curva equity, così
        // valuta e percentuale hanno unità dichiarate e la stessa sorgente.
        var maxDrawdownPercent = CalculateMaxDrawdownPercent(result.HourlyResults, result.InitialCapital);
        var maxDrawdownValue = CalculateMaxDrawdown(result.HourlyResults, result.InitialCapital);
        // Il profit da solo non e' confrontabile fra due run con capitale iniziale diverso: la
        // percentuale e' rispetto al capitale iniziale, la stessa base del "Return %" annuale.
        var totalProfitPercent = result.InitialCapital != 0
            ? result.TotalProfit / result.InitialCapital * 100m
            : 0m;
        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Riepilogo simulazione</h2>");
        html.AppendLine("    <div class=\"metrics\">");
        html.AppendLine($"      <div class=\"metric\"><span>Range simulazione</span><b>{result.StartDate:yyyy-MM-dd HH:mm} - {result.EndDate:yyyy-MM-dd HH:mm}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Symbol usati</span><b>{System.Net.WebUtility.HtmlEncode(symbolsText)}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Strategie</span><b>{strategyCount}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Trade effettuati</span><b>{totalTrades}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Capitale iniziale</span><b>{result.InitialCapital:F2}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Profit totale</span><b>{result.TotalProfit:F2}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Profit totale %</span><b>{totalProfitPercent:F2}%</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Max drawdown</span><b>{maxDrawdownValue:F2}</b></div>");
        html.AppendLine($"      <div class=\"metric\"><span>Max drawdown %</span><b>{maxDrawdownPercent:F2}%</b></div>");
        html.AppendLine("    </div>");
        html.AppendLine("  </div>");
    }

    private static void AppendYearlySummaryHtml(
        StringBuilder html,
        BacktestingResult result,
        IReadOnlyList<BacktestReportTrade> closedTrades)
    {
        var orderedRows = result.HourlyResults
            .Where(row => row.Equity != 0)
            .OrderBy(row => row.DateTime)
            .ToList();

        if (!orderedRows.Any())
        {
            return;
        }

        orderedRows = TruncateToDataCoverage(orderedRows, result.DataCoverageEndUtc);
        if (!orderedRows.Any())
        {
            return;
        }

        var previousYearEndEquity = result.InitialCapital;
        var yearlyRows = new List<(int Year, decimal StartEquity, decimal EndEquity, decimal Profit, decimal MaxDrawdown, decimal MaxDrawdownPercent, decimal ReturnPct, int WinningTrades, int LosingTrades)>();

        foreach (var yearGroup in orderedRows.GroupBy(row => row.DateTime.Year).OrderBy(group => group.Key))
        {
            var yearRows = yearGroup.OrderBy(row => row.DateTime).ToList();
            var endEquity = yearRows.Last().Equity;
            var profit = endEquity - previousYearEndEquity;
            var maxDrawdown = CalculateMaxDrawdown(yearRows, previousYearEndEquity);
            // Percentuale rispetto al picco corrente, non all'equity di inizio anno: il picco più
            // alto del periodo può non essere quello del drawdown massimo in valuta, quindi le due
            // colonne possono riferirsi a punti diversi della curva. È voluto — la percentuale
            // deve dire quanto si è perso dal massimo, che è la grandezza confrontabile fra anni.
            var maxDrawdownPercent = CalculateMaxDrawdownPercent(yearRows, previousYearEndEquity);
            var returnPct = previousYearEndEquity != 0 ? profit / previousYearEndEquity * 100m : 0m;
            var yearTrades = closedTrades
                .Where(trade => trade.ExitDateUtc.Year == yearGroup.Key)
                .ToList();
            var winningTrades = yearTrades.Count(trade => trade.NetProfit > 0);
            var losingTrades = yearTrades.Count(trade => trade.NetProfit < 0);

            yearlyRows.Add((yearGroup.Key, previousYearEndEquity, endEquity, profit, maxDrawdown, maxDrawdownPercent, returnPct, winningTrades, losingTrades));
            previousYearEndEquity = endEquity;
        }

        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Resoconto annuale</h2>");
        AppendCoverageNoteHtml(html, result);
        html.AppendLine("    <table class=\"summary-table\">");
        html.AppendLine("      <thead><tr><th>Anno</th><th>Equity iniziale</th><th>Equity finale</th><th>Profit</th><th>Return %</th><th>Max DD anno</th><th>Max DD %</th><th>Trade win</th><th>Trade persi</th></tr></thead>");
        html.AppendLine("      <tbody>");

        foreach (var row in yearlyRows)
        {
            var profitClass = row.Profit >= 0 ? "positive" : "negative";
            html.AppendLine(
                $"        <tr><td>{row.Year}</td><td>{row.StartEquity:F2}</td><td>{row.EndEquity:F2}</td><td class=\"{profitClass}\">{row.Profit:F2}</td><td class=\"{profitClass}\">{row.ReturnPct:F2}%</td><td class=\"negative\">{row.MaxDrawdown:F2}</td><td class=\"negative\">{row.MaxDrawdownPercent:F2}%</td><td>{row.WinningTrades}</td><td>{row.LosingTrades}</td></tr>");
        }

        html.AppendLine("      </tbody>");
        html.AppendLine("    </table>");
        html.AppendLine("  </div>");
    }

    private static void AppendMonthlySummaryHtml(
        StringBuilder html,
        BacktestingResult result,
        IReadOnlyList<BacktestReportTrade> closedTrades)
    {
        var orderedRows = result.HourlyResults
            .Where(row => row.Equity != 0)
            .OrderBy(row => row.DateTime)
            .ToList();

        if (!orderedRows.Any())
        {
            return;
        }

        orderedRows = TruncateToDataCoverage(orderedRows, result.DataCoverageEndUtc);
        if (!orderedRows.Any())
        {
            return;
        }

        var previousMonthEndEquity = result.InitialCapital;

        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h2>Resoconto mensile</h2>");
        AppendCoverageNoteHtml(html, result);
        html.AppendLine("    <table class=\"summary-table\">");
        html.AppendLine("      <thead><tr><th>Mese</th><th>Equity iniziale</th><th>Equity finale</th><th>Profit</th><th>Return %</th><th>Max DD mese</th><th>Max DD %</th><th>Trade win</th><th>Trade persi</th><th>Migliori 3 strategie</th></tr></thead>");
        html.AppendLine("      <tbody>");

        foreach (var monthGroup in orderedRows.GroupBy(row => new { row.DateTime.Year, row.DateTime.Month }).OrderBy(group => group.Key.Year).ThenBy(group => group.Key.Month))
        {
            var monthRows = monthGroup.OrderBy(row => row.DateTime).ToList();
            var endEquity = monthRows.Last().Equity;
            var profit = endEquity - previousMonthEndEquity;
            var maxDrawdown = CalculateMaxDrawdown(monthRows, previousMonthEndEquity);
            var maxDrawdownPercent = CalculateMaxDrawdownPercent(monthRows, previousMonthEndEquity);
            var returnPct = previousMonthEndEquity != 0 ? profit / previousMonthEndEquity * 100m : 0m;
            var monthTrades = closedTrades
                .Where(trade => trade.ExitDateUtc.Year == monthGroup.Key.Year && trade.ExitDateUtc.Month == monthGroup.Key.Month)
                .ToList();
            var profitClass = profit >= 0 ? "positive" : "negative";
            var topStrategiesHtml = BuildTopStrategiesCellHtml(monthTrades);

            html.AppendLine(
                $"        <tr><td>{monthGroup.Key.Year}-{monthGroup.Key.Month:00}</td><td>{previousMonthEndEquity:F2}</td><td>{endEquity:F2}</td><td class=\"{profitClass}\">{profit:F2}</td><td class=\"{profitClass}\">{returnPct:F2}%</td><td class=\"negative\">{maxDrawdown:F2}</td><td class=\"negative\">{maxDrawdownPercent:F2}%</td><td>{monthTrades.Count(trade => trade.NetProfit > 0)}</td><td>{monthTrades.Count(trade => trade.NetProfit < 0)}</td><td class=\"top-strategies\">{topStrategiesHtml}</td></tr>");

            previousMonthEndEquity = endEquity;
        }

        html.AppendLine("      </tbody>");
        html.AppendLine("    </table>");
        html.AppendLine("  </div>");
    }

    /// <summary>
    /// Le tre strategie con il profit netto piu' alto nel periodo, gia' formattate come cella HTML.
    /// </summary>
    /// <remarks>
    /// La classifica e' sui trade <em>chiusi</em> nel periodo, l'unica grandezza confrontabile fra
    /// strategie: l'equity per strategia include il mark-to-market delle posizioni aperte e farebbe
    /// vincere il mese a chi ha solo un trade ancora in corso. La chiave e' (Symbol, StrategyCode)
    /// come nel resto del report, cosi' la stessa strategia su due simboli resta distinta.
    /// </remarks>
    private static string BuildTopStrategiesCellHtml(IReadOnlyList<BacktestReportTrade> periodTrades)
    {
        if (periodTrades.Count == 0)
        {
            return "<span class=\"muted\">-</span>";
        }

        var top = periodTrades
            .GroupBy(trade => StrategyKeys.MakeStrategyKey(
                trade.Symbol,
                string.IsNullOrWhiteSpace(trade.StrategyCode) ? trade.StrategyName : trade.StrategyCode))
            .Select(group => new { Key = group.Key, Profit = group.Sum(trade => trade.NetProfit), Trades = group.Count() })
            .OrderByDescending(row => row.Profit)
            .ThenBy(row => row.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        return string.Join("<br>", top.Select(row =>
        {
            var cssClass = row.Profit >= 0 ? "positive" : "negative";
            var label = System.Net.WebUtility.HtmlEncode(row.Key);
            return $"{label} <span class=\"{cssClass}\">{row.Profit:F2}</span> <span class=\"muted\">({row.Trades})</span>";
        }));
    }

    /// <summary>
    /// Larghezza del canvas dell'equity globale. La riduzione punta a questa, non a un numero
    /// arbitrario: e' il limite oltre il quale due punti cadono nello stesso pixel.
    /// </summary>
    private const int GlobalChartCanvasWidth = 1400;

    /// <summary>
    /// Sotto questa soglia la serie globale finisce nel report intera: un run corto non ha niente
    /// da guadagnare dalla riduzione, e il report resta punto per punto quello del motore.
    /// </summary>
    private const int GlobalChartFullDetailThreshold = 8000;

    /// <summary>
    /// Riduce la serie di equity globale alla risoluzione che il grafico puo' davvero disegnare.
    /// </summary>
    /// <remarks>
    /// <para><c>HourlyResults</c> ha una riga per ogni tick dell'orologio, sempre: e' la serie su
    /// cui si calcolano <c>MaxDrawdown</c> e i resoconti annuale e mensile, e li' la risoluzione
    /// piena serve. Al <b>grafico</b> no. Un run di tredici mesi con orologio a un minuto produce
    /// 570.000 punti su un canvas largo 1400 pixel — quattrocento punti per colonna — e il report
    /// arrivava a 131 MB, di cui 106 di soli punti globali. Peggio: <c>Math.max(0, ...punti)</c>
    /// nel disegno del drawdown passava un argomento per elemento e lanciava RangeError, che
    /// interrompeva lo script e lasciava <b>entrambi</b> i grafici bianchi (08/09/2026). Il limite
    /// dello spread e' via, ma una serie che nessuno puo' vedere resta peso inutile nel file.</para>
    ///
    /// <para><b>Non e' un campionamento a passo fisso.</b> Prendere un punto ogni N taglierebbe i
    /// picchi, e su una curva di equity il picco e' l'informazione: il massimo, il minimo e il
    /// drawdown peggiore sono esattamente cio' che si guarda. Le righe si dividono in colonne
    /// contigue larghe un pixel e da ognuna si tengono la prima, l'ultima, quella di equity minima,
    /// quella di equity massima e quella di drawdown massimo — in ordine di tempo e senza
    /// ripetizioni. Il tracciato che ne esce e' pixel per pixel quello della serie intera, con gli
    /// estremi al posto giusto, e la scala del drawdown resta quella vera.</para>
    /// </remarks>
    private static List<HourlyResult> BuildGlobalChartPoints(List<HourlyResult> rows)
    {
        var ordered = rows.OrderBy(row => row.DateTime).ToList();
        if (ordered.Count <= GlobalChartFullDetailThreshold)
        {
            return ordered;
        }

        var kept = new List<HourlyResult>(GlobalChartCanvasWidth * 5);
        var lastKeptIndex = -1;
        for (var column = 0; column < GlobalChartCanvasWidth; column++)
        {
            var from = (int)((long)column * ordered.Count / GlobalChartCanvasWidth);
            var to = (int)((long)(column + 1) * ordered.Count / GlobalChartCanvasWidth) - 1;
            if (to < from)
            {
                continue;
            }

            var minEquityIndex = from;
            var maxEquityIndex = from;
            var maxDrawdownIndex = from;
            for (var index = from + 1; index <= to; index++)
            {
                if (ordered[index].Equity < ordered[minEquityIndex].Equity)
                {
                    minEquityIndex = index;
                }

                if (ordered[index].Equity > ordered[maxEquityIndex].Equity)
                {
                    maxEquityIndex = index;
                }

                if (Math.Abs(ordered[index].Drawdown) > Math.Abs(ordered[maxDrawdownIndex].Drawdown))
                {
                    maxDrawdownIndex = index;
                }
            }

            // Ordinati per indice e non per ruolo: la spezzata si disegna nell'ordine in cui i
            // punti stanno nella lista, e invertirli farebbe tornare indietro la linea nel tempo.
            Span<int> candidates = [from, minEquityIndex, maxEquityIndex, maxDrawdownIndex, to];
            candidates.Sort();
            foreach (var index in candidates)
            {
                if (index > lastKeptIndex)
                {
                    kept.Add(ordered[index]);
                    lastKeptIndex = index;
                }
            }
        }

        return kept;
    }

    /// <summary>
    /// Scarta le righe di equity successive all'ultima barra realmente presente nel datafeed.
    /// </summary>
    /// <remarks>
    /// L'orologio del backtest è sintetico e arriva fino a <c>EndDate</c> anche quando il feed
    /// finisce prima: le righe in eccesso hanno equity costante e nei resoconti si presentano come
    /// mesi a profitto zero, indistinguibili da mesi in cui il sistema non ha operato. Tagliarle è
    /// coerente con l'invariante "datafeed mancante = errore esplicito": meglio una tabella più
    /// corta con una nota che una tabella completa e muta. Con copertura ignota non si tocca nulla.
    /// </remarks>
    private static List<HourlyResult> TruncateToDataCoverage(
        List<HourlyResult> orderedRows,
        DateTime? dataCoverageEndUtc)
    {
        if (!dataCoverageEndUtc.HasValue)
        {
            return orderedRows;
        }

        var limit = dataCoverageEndUtc.Value;
        return orderedRows.Where(row => row.DateTime <= limit).ToList();
    }

    /// <summary>
    /// Nota esplicita quando i resoconti sono più corti dell'intervallo richiesto.
    /// </summary>
    private static void AppendCoverageNoteHtml(StringBuilder html, BacktestingResult result)
    {
        if (!result.DataCoverageEndUtc.HasValue || result.EndDate <= result.DataCoverageEndUtc.Value)
        {
            return;
        }

        html.AppendLine(
            $"    <p class=\"muted\">Tabella troncata al {result.DataCoverageEndUtc.Value:yyyy-MM-dd HH:mm} UTC, " +
            $"ultima barra disponibile nel datafeed: il backtest era richiesto fino al " +
            $"{result.EndDate:yyyy-MM-dd HH:mm} UTC, ma i periodi successivi non hanno dati e " +
            $"comparirebbero come periodi senza operatività.</p>");
    }

    private static decimal CalculateMaxDrawdown(IEnumerable<HourlyResult> yearRows, decimal initialPeak)
    {
        var peak = initialPeak;
        var maxDrawdown = 0m;

        foreach (var row in yearRows.OrderBy(item => item.DateTime))
        {
            if (row.Equity > peak)
            {
                peak = row.Equity;
            }

            var drawdown = peak - row.Equity;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
        }

        return maxDrawdown;
    }

    private static decimal CalculateMaxDrawdownPercent(IEnumerable<HourlyResult> rows, decimal initialPeak)
    {
        var peak = initialPeak;
        var maximum = 0m;
        foreach (var row in rows.OrderBy(item => item.DateTime))
        {
            peak = Math.Max(peak, row.Equity);
            if (peak != 0)
                maximum = Math.Max(maximum, (peak - row.Equity) / Math.Abs(peak));
        }
        return maximum * 100m;
    }
}
