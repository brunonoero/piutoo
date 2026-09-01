using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>
/// Genera il report HTML di un backtest che il proprio run non ha prodotto: quelli eseguiti
/// dall'engine esterno (cBot cTrader), che archiviano <c>trades.json</c> e <c>signals.json</c> ma
/// non il report, e i run interni interrotti prima della scrittura degli artefatti.
/// </summary>
/// <remarks>
/// <para>Il report è lo stesso dei run interni — stesso <see cref="BacktestHtmlReport"/>, stesse
/// tabelle, stessi grafici — perché serve proprio a mettere a confronto i due motori sugli stessi
/// numeri. Cambia la sorgente: qui non ci sono né barre né snapshot dell'engine, solo i trade
/// chiusi, quindi la curva equity è <b>quella realizzata</b>, un gradino a ogni chiusura, senza
/// mark-to-market delle posizioni aperte. Il report lo dichiara in testa: due curve che si
/// assomigliano ma sono costruite in modo diverso sarebbero un confronto falso.</para>
///
/// <para>La cartella non viene toccata oltre al file del report: <c>trades.json</c> resta
/// l'artefatto autorevole del run, e rigenerare il report non deve poterlo alterare.</para>
/// </remarks>
public sealed class ExternalBacktestReportService(WorkspaceService workspaces)
{
    /// <summary>
    /// Nome fisso, così rigenerare sostituisce invece di accumulare: <see
    /// cref="WorkspaceService.GetBacktestHtmlReportPath"/> serve l'HTML più recente della cartella,
    /// e una collezione di report datati renderebbe l'apertura del dettaglio una lotteria.
    /// </summary>
    public const string ReportFileName = "backtest-report-ricostruito.html";

    /// <summary>
    /// Capitale usato quando il run non l'ha registrato. Le cartelle scritte prima che
    /// <see cref="BacktestOriginInfo.InitialCapital"/> esistesse non lo hanno, e senza una base i
    /// rendimenti percentuali non sono calcolabili: si assume il capitale di default delle sessioni
    /// e lo si dichiara nel report, invece di stampare percentuali senza denominatore noto.
    /// </summary>
    public const decimal FallbackInitialCapital = 100_000m;

    /// <summary>
    /// Ricostruisce e scrive il report. Restituisce il percorso del file generato.
    /// </summary>
    /// <param name="initialCapital">
    /// Sovrascrive il capitale registrato nel marcatore di origine. Serve a rileggere lo stesso run
    /// con la base di un altro conto, senza riscrivere gli artefatti.
    /// </param>
    /// <exception cref="DirectoryNotFoundException">La cartella di backtest non esiste.</exception>
    /// <exception cref="InvalidOperationException">
    /// La cartella ha già il report del proprio run — è l'artefatto di quel run e non va scavalcato
    /// da una ricostruzione — oppure non contiene trade chiusi da cui ricostruire.
    /// </exception>
    public string Generate(string workspaceId, string folderName, decimal? initialCapital = null)
    {
        var backtestPath = workspaces.GetBacktestPath(workspaceId, folderName);
        if (!Directory.Exists(backtestPath))
            throw new DirectoryNotFoundException(
                $"Backtest '{folderName}' non trovato nel workspace '{workspaceId}'.");

        // Il criterio è la presenza del report del run, non l'origine dichiarata: le cartelle
        // scritte prima del marcatore hanno origine ignota pur essendo run interni completi, e su
        // quelle una ricostruzione affiancherebbe al report del motore una curva diversa (equity
        // realizzata invece di mark-to-market) che, essendo più recente, lo scavalcherebbe
        // all'apertura del dettaglio.
        if (HasRunReport(backtestPath))
            throw new InvalidOperationException(
                $"Il backtest '{folderName}' ha già il report prodotto dal proprio run: " +
                "ricostruirlo dai soli trade darebbe una curva diversa (equity realizzata invece di " +
                "mark-to-market) sotto lo stesso nome.");

        var origin = WorkspaceService.ReadBacktestOrigin(backtestPath);

        var trades = workspaces.GetBacktestTrades(workspaceId, folderName)
            .OrderBy(trade => trade.ExitTimeUtc)
            .ThenBy(trade => trade.TradeId, StringComparer.Ordinal)
            .ToList();

        if (trades.Count == 0)
            throw new InvalidOperationException(
                $"Il backtest '{folderName}' non ha trade chiusi in trades.json: non c'è nulla da " +
                "cui ricostruire il report.");

        var capital = initialCapital ?? origin?.InitialCapital ?? FallbackInitialCapital;
        var result = BuildResult(folderName, origin, trades, capital);
        var notes = BuildNotes(origin, trades, capital, initialCapital.HasValue);

        var reportPath = Path.Combine(backtestPath, ReportFileName);
        BacktestHtmlReport.Write(
            reportPath,
            result,
            trades.Select(trade => BacktestReportTrade.From(trade)).ToList(),
            notes);

        return reportPath;
    }

    /// <summary>
    /// Un HTML già presente è quello scritto dal motore a fine run. In un run interrotto e in uno
    /// eseguito dall'engine esterno non c'è, ed è proprio il caso in cui la ricostruzione serve.
    /// Il file di questo servizio non conta: rigenerarlo è lecito e sostituisce il precedente.
    /// </summary>
    private static bool HasRunReport(string backtestPath)
        => new DirectoryInfo(backtestPath)
            .EnumerateFiles("*.html", SearchOption.TopDirectoryOnly)
            .Any(file => !string.Equals(file.Name, ReportFileName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Traduce i trade chiusi nella forma che il report si aspetta dal motore interno: curva equity
    /// globale, curva per strategia e anagrafica delle strategie viste nel run.
    /// </summary>
    /// <remarks>
    /// L'equity per strategia parte dal capitale iniziale e ci somma il proprio netto cumulato: è la
    /// stessa convenzione di <c>PiootooTradingService.GetStrategyEquities</c>, altrimenti le due
    /// serie non sarebbero sovrapponibili nello stesso grafico. La prima riga di ogni serie è il
    /// capitale all'inizio del run, così tutte le curve partono dalla stessa base anche se la
    /// strategia ha chiuso il primo trade mesi dopo.
    /// </remarks>
    private static BacktestingResult BuildResult(
        string folderName,
        BacktestOriginInfo? origin,
        IReadOnlyList<PersistedTrade> trades,
        decimal initialCapital)
    {
        var startUtc = trades.Min(trade => trade.EntryTimeUtc);
        var endUtc = trades.Max(trade => trade.ExitTimeUtc);

        var result = new BacktestingResult
        {
            JobId = origin?.SessionId ?? string.Empty,
            SetupName = string.IsNullOrWhiteSpace(origin?.PlanCode)
                ? folderName
                : $"{folderName} ({origin!.PlanCode})",
            SetupId = folderName,
            StartDate = startUtc,
            EndDate = endUtc,
            InitialCapital = initialCapital,
            // Il feed lo dichiara il marcatore del run, non questa ricostruzione: sulle cartelle
            // scritte prima del campo resta ignoto, e stampare "ignoto" e' l'unica risposta onesta.
            PriceSource = origin?.ResolvedPriceSource,
            // L'orologio non è sintetico: le righe finiscono dove finiscono i trade, quindi non c'è
            // coda piatta da troncare e i resoconti non vanno tagliati.
            DataCoverageEndUtc = null
        };

        result.HourlyResults.Add(new HourlyResult
        {
            DateTime = startUtc,
            Equity = initialCapital,
            Balance = initialCapital,
            Drawdown = 0m,
            Profit = 0m
        });

        var equity = initialCapital;
        var peak = initialCapital;
        foreach (var trade in trades)
        {
            equity += trade.NetProfit;
            peak = Math.Max(peak, equity);
            result.HourlyResults.Add(new HourlyResult
            {
                DateTime = trade.ExitTimeUtc,
                Equity = equity,
                Balance = equity,
                // Percentuale dal picco, la stessa unità di TradingState.UpdateDrawdown: il report
                // legge questo campo per le barre del grafico globale.
                Drawdown = peak > 0 ? (peak - equity) / peak * 100m : 0m,
                Profit = trade.NetProfit
            });
        }

        var strategyEquities = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var strategies = new Dictionary<string, StrategyInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var trade in trades)
        {
            var key = StrategyKeys.MakeStrategyKey(trade.Symbol, CodeOf(trade));
            if (!strategies.ContainsKey(key))
            {
                strategies[key] = new StrategyInfo
                {
                    Name = trade.StrategyName,
                    StrategyCode = CodeOf(trade),
                    Symbol = trade.Symbol,
                    TimeframeMinutes = TimeframeFromCode(CodeOf(trade))
                };
                strategyEquities[key] = initialCapital;
                result.StrategyResults.Add(new StrategyHourlyResult
                {
                    StrategyName = trade.StrategyName,
                    StrategyCode = CodeOf(trade),
                    Symbol = trade.Symbol,
                    DateTime = startUtc,
                    Equity = initialCapital,
                    Profit = 0m,
                    Contracts = 0m
                });
            }

            strategyEquities[key] += trade.NetProfit;
            result.StrategyResults.Add(new StrategyHourlyResult
            {
                StrategyName = trade.StrategyName,
                StrategyCode = CodeOf(trade),
                Symbol = trade.Symbol,
                DateTime = trade.ExitTimeUtc,
                Equity = strategyEquities[key],
                Profit = trade.NetProfit,
                Contracts = trade.Quantity,
                EntryPrice = trade.EntryPrice,
                ExitPrice = trade.ExitPrice
            });
        }

        result.StrategiesInfo.AddRange(strategies.Values.OrderBy(info => info.StrategyCode, StringComparer.Ordinal));
        result.StrategiesUsed.AddRange(result.StrategiesInfo.Select(info => info.StrategyCode));
        result.FinalEquity = equity;
        result.TotalProfit = equity - initialCapital;
        result.MaxDrawdown = result.HourlyResults.Max(row => row.Drawdown);
        result.TotalReturn = initialCapital != 0 ? result.TotalProfit / initialCapital * 100m : 0m;
        result.TotalTrades = trades.Count;
        result.WinRate = trades.Count(trade => trade.NetProfit > 0) * 100m / trades.Count;
        return result;
    }

    private static IReadOnlyList<string> BuildNotes(
        BacktestOriginInfo? origin,
        IReadOnlyList<PersistedTrade> trades,
        decimal initialCapital,
        bool capitalFromCaller)
    {
        var notes = new List<string>
        {
            "Report ricostruito dai trade archiviati" +
            (origin?.Origin == BacktestOrigin.ExternalBroker
                ? " dall'engine esterno"
                : string.Empty) +
            ": l'equity è quella realizzata, un gradino alla chiusura di ogni trade. Le posizioni " +
            "aperte non sono valorizzate a mercato — il server non ha le barre di questo run — " +
            "quindi la curva è più squadrata di quella di un backtest interno, e il drawdown è " +
            "misurato fra chiusure, non fra massimi intraday."
        };

        if (origin is { PlanCode: not null } or { ExecutionKey: not null })
        {
            notes.Add(
                $"Piano {origin.PlanCode ?? "n/d"}, esecuzione {origin.ExecutionKey ?? "n/d"}, " +
                $"sessione {origin.SessionId ?? "n/d"}.");
        }

        notes.Add(capitalFromCaller
            ? $"Capitale iniziale {initialCapital:F2} indicato per questa generazione."
            : origin?.InitialCapital is not null
                ? $"Capitale iniziale {initialCapital:F2}, come dichiarato all'apertura della sessione."
                : $"Capitale iniziale non registrato nel run: assunto {initialCapital:F2}. " +
                  "Profit e drawdown in valuta non ne dipendono, le percentuali sì.");

        var accounts = trades
            .Select(trade => trade.AccountNumber)
            .Where(account => !string.IsNullOrWhiteSpace(account))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (accounts.Count > 1)
        {
            notes.Add(
                $"Il run ha operato su {accounts.Count} conti ({string.Join(", ", accounts)}): l'equity " +
                "è la somma dei netti, non il saldo di un singolo conto.");
        }

        return notes;
    }

    private static string CodeOf(PersistedTrade trade)
        => string.IsNullOrWhiteSpace(trade.StrategyCode) ? trade.StrategyName : trade.StrategyCode;

    /// <summary>
    /// Timeframe dedotto dal suffisso del codice strategia (<c>PTS_NQ_TFM_001_60</c> → 60).
    /// </summary>
    /// <remarks>
    /// Il trade non lo riporta e il catalogo qui non serve: il timeframe compare solo
    /// nell'anagrafica del report, e dedurlo dal codice — l'unica cosa che il run esterno scrive —
    /// costa nulla. Zero quando il suffisso non è un numero, invece di indovinare.
    /// </remarks>
    private static int TimeframeFromCode(string strategyCode)
    {
        var lastSeparator = strategyCode.LastIndexOf('_');
        return lastSeparator >= 0
               && int.TryParse(strategyCode[(lastSeparator + 1)..], out var minutes)
            ? minutes
            : 0;
    }
}
