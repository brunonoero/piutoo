using System.Reflection;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'allargamento dello stop di <see cref="StopMoneyPolicy"/> deve valere per <b>tutte</b> le
/// strategie del catalogo, senza eccezioni e senza doverlo ripetere in ogni classe.
///
/// <para>Vale per costruzione perche' il fattore si applica in un punto solo — l'arricchimento del
/// segnale in <c>StatelessEasyStrategyBase.Evaluate</c>, l'unico passaggio che backtest e sessione
/// percorrono entrambi. Questi test verificano le due meta' di quella frase: che nessuna strategia
/// del catalogo stia fuori da quel passaggio, e che il segnale che ne esce porti davvero lo stop
/// dichiarato moltiplicato.</para>
/// </summary>
public sealed class StopMoneyPolicyConformanceTests(ITestOutputHelper output)
{
    /// <summary>
    /// La meta' strutturale: una strategia che non passasse da <c>StatelessEasyStrategyBase</c>
    /// emetterebbe lo stop della ricerca e nessuno se ne accorgerebbe, perche' il segnale sarebbe
    /// comunque ben formato.
    /// </summary>
    [Fact]
    public void EveryRegisteredStrategyPassesThroughTheSingleWideningPoint()
    {
        var outside = new List<string>();

        foreach (var definition in StrategyFactory.GetRegisteredStrategies())
        {
            var strategy = StrategyFactory.CreateStrategy(
                definition.Id, definition.Symbol, definition.TimeframeMinutes);

            if (strategy is null)
            {
                outside.Add($"{definition.Id}: StrategyFactory non riesce a istanziarla.");
                continue;
            }

            if (strategy is not StatelessEasyStrategyBase)
                outside.Add($"{definition.Id}: non deriva da StatelessEasyStrategyBase.");
        }

        Assert.True(outside.Count == 0,
            "Strategie fuori dal punto in cui lo stop viene allargato:" + Environment.NewLine +
            string.Join(Environment.NewLine, outside));
    }

    /// <summary>
    /// La meta' di comportamento: su ogni ingresso che le strategie riescono a emettere su dati
    /// sintetici, lo stop del segnale e' quello dichiarato per il fattore. Le strategie che sui
    /// sintetici non entrano vengono elencate nell'output, non fatte fallire: i loro gate sono
    /// coperti dai test per motore.
    /// </summary>
    [Fact]
    public void EveryEmittedEntryCarriesTheDeclaredStopWidened()
    {
        var violations = new List<string>();
        var exercised = new List<string>();
        var silent = new List<string>();

        foreach (var definition in StrategyFactory.GetRegisteredStrategies())
        {
            var strategy = StrategyFactory.CreateStrategy(
                definition.Id, definition.Symbol, definition.TimeframeMinutes);
            if (strategy is null) continue;

            if (CheckStrategy(definition.Id, strategy, violations))
                exercised.Add(definition.Id);
            else
                silent.Add(definition.Id);
        }

        output.WriteLine($"Strategie con almeno un ingresso sui sintetici: {exercised.Count}");
        output.WriteLine($"Strategie che non entrano sui sintetici: {silent.Count}");
        if (silent.Count > 0)
            output.WriteLine(string.Join(", ", silent));

        Assert.True(violations.Count == 0,
            "Ingressi con uno stop diverso da quello dichiarato per il fattore:" + Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// "Nessuno stop" resta nessuno stop: moltiplicare un valore assente non lo trasforma in un
    /// livello, e una strategia senza stop deve continuare a uscire solo per target o per tempo.
    /// </summary>
    [Fact]
    public void NoDeclaredStopStaysWithoutStop()
    {
        Assert.Null(StopMoneyPolicy.Widen(null));
        Assert.Equal(0m, StopMoneyPolicy.Widen(0m));
    }

    /// <returns><c>true</c> se la strategia ha emesso almeno un ingresso.</returns>
    private static bool CheckStrategy(string id, ITradingStrategy strategy, List<string> violations)
    {
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

                var declared = ReadDeclaredStop(strategy, emitted.Type);
                var expected = declared > 0m ? declared * StopMoneyPolicy.Multiplier : (decimal?)null;

                if (emitted.StopLossMoneyPerFutureContract != expected)
                {
                    violations.Add(
                        $"{id}: dichiarato {declared}, atteso {Describe(expected)}, " +
                        $"emesso {Describe(emitted.StopLossMoneyPerFutureContract)}.");
                }
            }
        }

        return seenEntry;
    }

    private static string Describe(decimal? stopMoney) =>
        stopMoney is null ? "nessuno stop" : "$" + stopMoney.Value;

    /// <summary>
    /// Lo stop dichiarato dal motore, letto dal campo protetto: <c>StopMoney</c> per i motori che
    /// lo dichiarano una volta sola, <c>StopMoneyLong</c>/<c>StopMoneyShort</c> per BIASW, che lo
    /// dichiara per verso.
    /// </summary>
    private static decimal ReadDeclaredStop(ITradingStrategy strategy, SignalType side)
    {
        var perSide = side == SignalType.Buy ? "StopMoneyLong" : "StopMoneyShort";
        return ReadField(strategy, perSide) ?? ReadField(strategy, "StopMoney") ?? 0m;
    }

    private static decimal? ReadField(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(instance) is { } value)
                return Convert.ToDecimal(value);
        }

        return null;
    }

    private static IEnumerable<TradeSignal> Flatten(TradeSignal signal)
    {
        yield return signal;
        if (signal.CompanionSignals is null) yield break;
        foreach (var companion in signal.CompanionSignals)
            yield return companion;
    }

    /// <summary>
    /// La stessa serie sintetica di <see cref="StrategyContractConformanceTests"/>: oscillante
    /// abbastanza da far scattare breakout e pattern direzionali, senza pretese sul P&amp;L.
    /// </summary>
    private static OhlcvData[] BuildSyntheticSession(int timeframeMinutes, int count)
    {
        var bars = new OhlcvData[count];
        var cursor = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var price = 2000m;

        for (var i = 0; i < count; i++)
        {
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
