using System.Diagnostics;
using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Utilities;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// Misura una combinazione di parametri su serie gia' caricate, in memoria e senza artefatti.
///
/// <para><b>Che cosa e', e cosa non e'.</b> E' il motore di esecuzione vero —
/// <see cref="PiootooTradingService"/> — dentro lo stesso loop del backtest: stessa regola su
/// quando una barra si valuta, stesso ordine dentro il tick (prima la strategia, poi stop, target e
/// uscite a tempo), stesso flat di fine settimana, stessa deadline dal piano. Non e' un simulatore
/// parallelo, e non deve diventarlo: una sweep che ottimizzasse su regole proprie consegnerebbe
/// parametri che il backtest non riproduce.</para>
///
/// <para><b>Cosa cambia rispetto al backtest.</b> Non scrive nulla su disco, non produce
/// diagnostica per evento, non calcola equity per ora o per settimana, e l'orologio del run e' per
/// default quello della strategia invece del feed piu' fitto del portafoglio. Sono le tre ragioni
/// per cui un run costa millisecondi invece di decine di secondi — e la terza cambia i numeri, non
/// solo il tempo: vedi <see cref="SweepJob.ClockTimeframeMinutes"/>.</para>
///
/// <para>Una istanza per thread: <see cref="PiootooTradingService"/> e i cursori non sono thread
/// safe. Le serie invece sono di sola lettura e si condividono.</para>
/// </summary>
public sealed class SweepRunner(SweepSeries series)
{
    /// <summary>Le barre su cui questo runner misura.</summary>
    public SweepSeries Series { get; } = series;

    /// <summary>
    /// Esegue una combinazione e restituisce le sue metriche. Solleva se la strategia non esiste,
    /// se il suo simbolo non e' quello delle serie o se il timeframe richiesto non e' caricato:
    /// una sweep che prosegue in silenzio su dati che non ha e' la stessa cosa del datafeed mancante.
    /// </summary>
    public SweepOutcome Run(SweepJob job)
    {
        var started = Stopwatch.StartNew();
        var strategy = CreateStrategy(job);

        var strategyBars = Series.Bars(strategy.TimeframeMinutes);
        if (strategyBars.Length == 0)
        {
            throw new InvalidOperationException(
                $"{job.StrategyId}: le serie caricate non hanno il timeframe {strategy.TimeframeMinutes}m di " +
                $"{Series.Symbol} (presenti: {string.Join(", ", Series.Timeframes)}).");
        }

        var clock = job.ClockTimeframeMinutes ?? strategy.TimeframeMinutes;
        var markBars = clock == strategy.TimeframeMinutes ? strategyBars : Series.Bars(clock);
        if (markBars.Length == 0)
        {
            throw new InvalidOperationException(
                $"{job.StrategyId}: l'orologio chiede il timeframe {clock}m, che non e' fra le serie caricate.");
        }

        if (!PiootooBacktestingService.ShouldEvaluateStrategy(strategy.TimeframeMinutes, clock))
        {
            throw new InvalidOperationException(
                $"{job.StrategyId}: il timeframe {strategy.TimeframeMinutes}m non e' un multiplo dell'orologio {clock}m.");
        }

        var trading = new PiootooTradingService { RejectWrongSideLevels = job.RejectWrongSideLevels };
        trading.Initialize(job.InitialCapital, job.CommissionPerContract);
        if (job.SpreadPoints is not null)
        {
            foreach (var (spreadSymbol, points) in job.SpreadPoints)
                trading.SpreadPoints[StrategyKeys.NormalizeSymbol(spreadSymbol)] = points;
        }

        if (job.SpreadPointsByHour is not null)
        {
            foreach (var (spreadSymbol, byHour) in job.SpreadPointsByHour)
                trading.SpreadPointsByHour[StrategyKeys.NormalizeSymbol(spreadSymbol)] = byHour;
        }

        if (job.Swap is not null)
        {
            foreach (var (swapSymbol, spec) in job.Swap)
                trading.SwapSpecs[StrategyKeys.NormalizeSymbol(swapSymbol)] = spec;
        }

        var strategyCursor = new CandleWindowCursor(strategyBars);
        var markCursor = clock == strategy.TimeframeMinutes ? strategyCursor : new CandleWindowCursor(markBars);
        var symbol = StrategyKeys.NormalizeSymbol(strategy.Symbol);
        var weekEndFlat = job.Holding.WeekEnd;
        var window = (int)(strategy.RequiredCandles * 1.2);

        var evaluations = 0;
        var signalsEmitted = 0;
        DateTime? lastEvaluatedBar = null;

        // L'orologio parte dall'inizio del run allineato al tick, come nel backtest: i giorni
        // precedenti sono nelle serie per il solo riscaldamento.
        var currentDate = AlignToClock(Series.StartUtc, clock);
        var currentPrices = new Dictionary<string, decimal>(1, StringComparer.OrdinalIgnoreCase);
        var currentBars = new Dictionary<string, OhlcvData>(1, StringComparer.OrdinalIgnoreCase);
        var signals = new List<TradeSignal>(2);

        while (currentDate <= Series.EndUtc)
        {
            if (PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(job.Holding, currentDate, clock))
            {
                currentDate = currentDate.AddMinutes(clock);
                continue;
            }

            signals.Clear();
            currentPrices.Clear();
            currentBars.Clear();

            var markBar = markCursor.LastCandle(currentDate);
            if (markBar is not null)
            {
                // Il mark e' l'ultimo prezzo noto anche stantio; la barra entra solo se e' di questo
                // tick, perche' e' quella che fa scattare trigger e riempimenti.
                currentPrices[symbol] = markBar.Close;
                if (PiootooBacktestingService.BelongsToCurrentTick(markBar.DateTime, currentDate, clock))
                    currentBars[symbol] = markBar;
            }

            var candles = strategyCursor.Window(currentDate, window);
            if (candles.Length >= strategy.RequiredCandles)
            {
                var bar = candles[^1];
                if (PiootooBacktestingService.IsStrategyBarClosedInTick(
                        bar.DateTime, strategy.TimeframeMinutes, currentDate, clock) &&
                    lastEvaluatedBar != bar.DateTime)
                {
                    lastEvaluatedBar = bar.DateTime;

                    if (!PiootooBacktestingService.IsStrategyCandleStale(
                            strategy.TimeframeMinutes, bar.DateTime, currentDate))
                    {
                        currentPrices.TryAdd(symbol, bar.Close);
                        if (!currentBars.ContainsKey(symbol) &&
                            PiootooBacktestingService.BelongsToCurrentTick(bar.DateTime, currentDate, clock))
                        {
                            currentBars[symbol] = bar;
                        }

                        evaluations++;
                        Evaluate(strategy, trading, candles, currentDate, symbol, job.Holding, signals);
                        signalsEmitted += signals.Count;
                    }
                }
            }

            if (currentPrices.Count == 0)
            {
                currentDate = currentDate.AddMinutes(clock);
                continue;
            }

            // L'ordine dentro il tick e' quello del backtest, ed e' misurato: prima la strategia,
            // poi stop, target e uscite a tempo. Invertirlo cambia i trade, non l'efficienza.
            if (signals.Count > 0)
                trading.ProcessSignals(signals, currentPrices, currentBars, currentDate);

            trading.UpdateMarketPrices(currentPrices, currentBars, currentDate);

            if (!job.Holding.AllowOverweek &&
                weekEndFlat.IsFlatTrigger(currentDate, currentDate.AddMinutes(-clock)))
            {
                trading.CancelAllPendingOrders();
            }

            currentDate = currentDate.AddMinutes(clock);
        }

        started.Stop();
        return Summarize(job, strategy.Name, trading, evaluations, signalsEmitted, started.Elapsed);
    }

    private static void Evaluate(
        ITradingStrategy strategy,
        PiootooTradingService trading,
        OhlcvData[] candles,
        DateTime currentDate,
        string symbol,
        AccountHoldingPolicy holding,
        List<TradeSignal> signals)
    {
        var execution = trading.GetExecutionSnapshot(strategy.Name, strategy.Symbol, currentDate);
        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = candles,
            BarTimeUtc = currentDate,
            Execution = execution
        });

        if (signal?.RuntimeState is not null)
            trading.CaptureStrategyRuntimeState(strategy.Name, strategy.Symbol, signal.RuntimeState);

        if (signal is null || signal.Type == SignalType.Hold)
            return;

        TradingDateTime.NormalizeSignalToUtc(signal);
        Accept(signal);

        if (signal.CompanionSignals is null)
            return;

        foreach (var companion in signal.CompanionSignals)
            Accept(companion);

        void Accept(TradeSignal accepted)
        {
            if (string.IsNullOrWhiteSpace(accepted.Symbol)) accepted.Symbol = symbol;
            if (string.IsNullOrWhiteSpace(accepted.StrategyCode)) accepted.StrategyCode = strategy.Name;
            if (string.IsNullOrWhiteSpace(accepted.StrategyName)) accepted.StrategyName = strategy.Name;
            PiootooBacktestingService.ApplyAccountHolding(accepted, holding);
            signals.Add(accepted);
        }
    }

    private ITradingStrategy CreateStrategy(SweepJob job)
    {
        var parameters = job.Parameters is null
            ? null
            : new Dictionary<string, object>(job.Parameters, StringComparer.OrdinalIgnoreCase);

        // Una sweep crea un'istanza per combinazione: due righe di console ciascuna sarebbero il
        // costo dominante, non un dettaglio.
        StrategyFactory.LogStrategyCreation = false;
        var strategy = StrategyFactory.CreateStrategy(job.StrategyId, Series.Symbol, 0, parameters)
            ?? throw new InvalidOperationException($"Strategia sconosciuta: {job.StrategyId}.");

        var strategySymbol = StrategyKeys.NormalizeSymbol(strategy.Symbol);
        if (!string.Equals(strategySymbol, Series.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{job.StrategyId} opera su {strategySymbol}, le serie caricate sono di {Series.Symbol}.");
        }

        return strategy;
    }

    /// <summary>
    /// L'orologio parte dal primo tick allineato: con un tick di 240 minuti un avvio alle 00:00
    /// resta alle 00:00, e un avvio a meta' bucket sale al successivo invece di sfalsare ogni tick
    /// del run rispetto alle barre.
    /// </summary>
    private static DateTime AlignToClock(DateTime startUtc, int clockMinutes)
    {
        if (clockMinutes <= 1) return startUtc;

        var ticks = TimeSpan.FromMinutes(clockMinutes).Ticks;
        var remainder = startUtc.Ticks % ticks;
        return remainder == 0 ? startUtc : new DateTime(startUtc.Ticks - remainder + ticks, DateTimeKind.Utc);
    }

    private static SweepOutcome Summarize(
        SweepJob job,
        string strategyCode,
        PiootooTradingService trading,
        int evaluations,
        int signals,
        TimeSpan elapsed)
    {
        var trades = trading.GetClosedTrades().OrderBy(t => t.ExitDate).ToArray();
        decimal net = 0m, peak = 0m, drawdown = 0m, profits = 0m, losses = 0m;
        var winners = 0;

        foreach (var trade in trades)
        {
            net += trade.NetProfit;
            if (trade.NetProfit > 0) { winners++; profits += trade.NetProfit; }
            else losses += -trade.NetProfit;

            if (net > peak) peak = net;
            var gap = peak - net;
            if (gap > drawdown) drawdown = gap;
        }

        return new SweepOutcome
        {
            Job = job,
            StrategyCode = strategyCode,
            Trades = trades.Length,
            Winners = winners,
            Losers = trades.Length - winners,
            ClosedTrades = trades,
            NetProfit = net,
            MaxClosedTradeDrawdown = drawdown,
            ProfitFactor = losses > 0m ? profits / losses : null,
            OpenAtEnd = trading.GetSnapshot().OpenPositionsCount,
            Evaluations = evaluations,
            Signals = signals,
            Elapsed = elapsed
        };
    }
}
