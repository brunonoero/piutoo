using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Verifica meccanicamente il contratto che <c>ITradingStrategy</c> e <c>TradeSignal</c> già
/// descrivono a parole.
///
/// <para>Il contratto era corretto fin dall'inizio; mancava solo qualcosa che lo applicasse, e
/// 41 strategie su 44 lo violavano in silenzio. Questi test fanno fallire la build invece di
/// lasciar passare un backtest che produce numeri plausibili e sbagliati.</para>
///
/// <para>Le strategie non ancora migrate sono elencate in <see cref="NotYetMigrated"/>: il test
/// le salta e il conteggio va a zero man mano che la migrazione procede. La lista è
/// deliberatamente esplicita — una strategia nuova non ci finisce per inerzia.</para>
/// </summary>
public sealed class StrategyContractConformanceTests(ITestOutputHelper output)
{
    /// <summary>
    /// Strategie ancora nella vecchia forma tradotta a mano. Rimuovere la voce quando la
    /// strategia viene riscritta su un motore parametrico.
    /// </summary>
    private static readonly HashSet<string> NotYetMigrated = new(StringComparer.OrdinalIgnoreCase)
    {
        // data2 (120m) non fornito dal test sintetico
        "Easy_123_CL_5",
        // uscite runtime su livelli dist + data2 15m
        "Easy_661_GC_30",
    };

    /// <summary>
    /// Ogni simbolo del catalogo deve avere una specifica verificata. Senza questa, la
    /// conversione denaro→punti userebbe un valore inventato e falserebbe stop, target e P&amp;L
    /// mantenendo numeri credibili.
    /// </summary>
    [Fact]
    public void EverySymbolInCatalogHasVerifiedInstrumentSpec()
    {
        var missing = StrategyFactory.GetRegisteredStrategies()
            .Select(s => s.Symbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(symbol => !InstrumentRegistry.TryGet(symbol, out _))
            .ToArray();

        Assert.True(missing.Length == 0,
            $"Simboli senza InstrumentSpec verificata: {string.Join(", ", missing)}. " +
            "Aggiungerli in InstrumentRegistry dopo aver verificato la specifica dell'exchange.");
    }

    /// <summary>
    /// Un simbolo sconosciuto deve produrre un errore, non un valore di comodo. È la
    /// regressione che protegge dal difetto più costoso trovato nell'audit.
    /// </summary>
    [Fact]
    public void UnknownSymbolThrowsInsteadOfFallingBackToOne()
    {
        var ex = Assert.Throws<InstrumentSpecNotFoundException>(
            () => InstrumentRegistry.PointValue("@SIMBOLO_INESISTENTE"));
        Assert.Contains("InstrumentRegistry", ex.Message);
    }

    [Fact]
    public void MigratedStrategiesRespectSignalContract()
    {
        var violations = new List<string>();
        var skipped = 0;

        foreach (var definition in StrategyFactory.GetRegisteredStrategies())
        {
            if (NotYetMigrated.Contains(definition.Id))
            {
                skipped++;
                continue;
            }

            var strategy = StrategyFactory.CreateStrategy(
                definition.Id, definition.Symbol, definition.TimeframeMinutes);
            if (strategy is null)
            {
                violations.Add($"{definition.Id}: StrategyFactory non riesce a istanziarla.");
                continue;
            }

            CheckStrategy(definition.Id, strategy, violations, output);
        }

        output.WriteLine($"Strategie ancora da migrare (saltate): {skipped}");
        Assert.True(violations.Count == 0,
            "Violazioni del contratto:" + Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static void CheckStrategy(
        string id, ITradingStrategy strategy, List<string> violations, ITestOutputHelper output)
    {
        // Sei sessioni piene sono il minimo per ricostruire d0..d5 senza troncare la più
        // vecchia: OHLCMulti5 riparte da zero a ogni valutazione e vede solo la finestra
        // ricevuta. Alcune strategie TF/legacy dichiarano un lookback più corto intenzionale
        // (es. Easy_156 = 100): in quel caso è un warning, non una violazione.
        var barsPerDay = Math.Max(1, 1440 / Math.Max(1, strategy.TimeframeMinutes));
        if (strategy.RequiredCandles < 6 * barsPerDay)
        {
            output.WriteLine(
                $"{id}: RequiredCandles={strategy.RequiredCandles} < sei sessioni " +
                $"({6 * barsPerDay}): verificare che i pattern non leggano d4/d5.");
        }

        var bars = BuildSyntheticSession(strategy.TimeframeMinutes, strategy.RequiredCandles + 60);
        var seenEntry = false;

        for (var i = strategy.RequiredCandles; i < bars.Length; i++)
        {
            var window = bars[..(i + 1)];
            var barTime = window[^1].DateTime;

            var signal = strategy.Evaluate(new StrategyEvaluationRequest
            {
                Ohlcv = window,
                BarTimeUtc = barTime,
                Execution = new StrategyExecutionSnapshot
                {
                    StrategyCode = strategy.Name,
                    Symbol = InstrumentRegistry.Normalize(strategy.Symbol),
                    BarTimeUtc = barTime,
                    DollarsPerPoint = InstrumentRegistry.PointValue(strategy.Symbol),
                    EntriesToday = 0
                }
            });

            foreach (var emitted in Flatten(signal))
            {
                if (emitted.Type is not (SignalType.Buy or SignalType.Sell)) continue;
                seenEntry = true;
                CheckEntrySignal(id, emitted, barTime, violations);
            }
        }

        if (!seenEntry)
        {
            // I gate di pattern/sessione spesso non scattano su OHLC sintetici uniformi.
            // Non è una violazione del contratto d'ingresso: la regressione dedicata vive
            // nei test per motore e in EngineCatalogMigrationTests.
            output.WriteLine(
                $"{id}: nessun ingresso su dati sintetici — contratto non esercitato qui.");
        }
    }

    private static void CheckEntrySignal(
        string id, TradeSignal signal, DateTime barTime, List<string> violations)
    {
        // 1. Stop e limit devono essere ordini veri, non market travestiti.
        if (signal.OrderType is TradeOrderType.Stop or TradeOrderType.Limit)
        {
            if (signal.ValidFromUtc is null || signal.ExpiresAtUtc is null)
            {
                violations.Add(
                    $"{id}: ordine {signal.OrderType} senza ValidFromUtc/ExpiresAtUtc — " +
                    "verrebbe eseguito sulla barra corrente invece che sulla successiva.");
            }
            else if (signal.ValidFromUtc <= barTime)
            {
                violations.Add(
                    $"{id}: ValidFromUtc ({signal.ValidFromUtc:O}) non è successivo alla barra " +
                    $"di segnale ({barTime:O}): sarebbe look-ahead.");
            }
        }

        // 2. Il rischio si dichiara in denaro sul contratto di riferimento. Il campo StopLoss è
        //    letto dall'engine come PUNTI: usarlo per un valore in dollari sbaglia di un fattore
        //    pari al PointValue (su GC, 100x).
        if (signal.StopLoss is not null)
        {
            violations.Add(
                $"{id}: StopLoss valorizzato ({signal.StopLoss}). L'engine lo interpreta come " +
                "punti — usare StopLossMoneyPerFutureContract.");
        }

        if (signal.TakeProfit is not null)
        {
            violations.Add(
                $"{id}: TakeProfit valorizzato ({signal.TakeProfit}). L'engine lo interpreta " +
                "come punti — usare TakeProfitMoneyPerFutureContract.");
        }

        // 3. L'uscita deve essere interamente descritta all'ingresso, altrimenti in
        //    ExternalBroker la posizione non chiuderebbe mai (il server emette solo Entry).
        var hasExit = signal.StopLossMoneyPerFutureContract is > 0
                      || signal.TakeProfitMoneyPerFutureContract is > 0
                      || signal.CloseAtUtc is not null
                      || signal.MaxBarsInPosition is > 0;

        if (!hasExit)
        {
            violations.Add(
                $"{id}: ingresso senza alcuna condizione di uscita dichiarata. In ExternalBroker " +
                "la posizione resterebbe aperta a tempo indeterminato.");
        }

        if (signal.CloseAtUtc is { } closeAt && closeAt <= barTime)
        {
            violations.Add(
                $"{id}: CloseAtUtc ({closeAt:O}) non è successivo alla barra di segnale " +
                $"({barTime:O}): la posizione chiuderebbe prima di aprirsi.");
        }

        if (signal.Quantity <= 0)
            violations.Add($"{id}: quantità non positiva ({signal.Quantity}).");
    }

    private static IEnumerable<TradeSignal> Flatten(TradeSignal signal)
    {
        yield return signal;
        if (signal.CompanionSignals is null) yield break;
        foreach (var companion in signal.CompanionSignals)
            yield return companion;
    }

    /// <summary>
    /// Serie sintetica continua, senza buchi, con un andamento oscillante abbastanza ampio da
    /// far scattare breakout e pattern direzionali. Non serve a validare il P&amp;L: serve solo a
    /// far emettere segnali di cui verificare la forma.
    /// </summary>
    private static OhlcvData[] BuildSyntheticSession(int timeframeMinutes, int count)
    {
        var bars = new OhlcvData[count];
        var cursor = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var price = 2000m;

        for (var i = 0; i < count; i++)
        {
            // Onda lenta più una componente veloce: produce sessioni con range variabile, che è
            // ciò che i pattern di compressione/espansione confrontano.
            var slow = (decimal)Math.Sin(i / 17.0) * 25m;
            var fast = (decimal)Math.Sin(i / 3.0) * 6m;
            var open = price;
            var close = price + slow / 4m + fast;
            var high = Math.Max(open, close) + 4m + Math.Abs(fast) / 2m;
            var low = Math.Min(open, close) - 4m - Math.Abs(fast) / 2m;

            bars[i] = new OhlcvData
            {
                DateTime = cursor,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = 1000
            };

            price = close;
            cursor = cursor.AddMinutes(timeframeMinutes);
        }

        return bars;
    }
}
