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
/// L'allargamento dello stop di <see cref="StopMoneyPolicy"/> deve valere per le strategie
/// elencate nella policy, per <b>tutte</b> quelle e per nessun'altra, senza doverlo ripetere in
/// ogni classe.
///
/// <para>Vale per costruzione perche' il fattore si applica in un punto solo — l'arricchimento del
/// segnale in <c>StatelessEasyStrategyBase.Evaluate</c>, l'unico passaggio che backtest e sessione
/// percorrono entrambi. Questi test verificano le meta' di quella frase: che nessuna strategia del
/// catalogo stia fuori da quel passaggio, che il segnale di una strategia in elenco porti lo stop
/// dichiarato moltiplicato, che quello di una strategia fuori elenco porti la distanza della
/// ricerca, e che nessun codice dell'elenco sia un refuso.</para>
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
        // Con l'elenco vuoto (dal 24/09/2026) si prova su un codice qualunque: il risultato atteso
        // e' lo stesso, dentro o fuori elenco.
        var widened = StopMoneyPolicy.WidenedStrategies.FirstOrDefault() ?? "ANY_CODE";

        Assert.Null(StopMoneyPolicy.Widen(widened, null));
        Assert.Equal(0m, StopMoneyPolicy.Widen(widened, 0m));
    }

    /// <summary>
    /// L'interruttore governa davvero l'allargamento, e il fattore dichiarato agli artefatti e'
    /// quello davvero applicato. Il test vale in entrambe le posizioni dell'interruttore perche'
    /// non ne assume nessuna: fallisce se qualcuno spegne <c>Enabled</c> lasciando <c>Widen</c> a
    /// moltiplicare, o se lo accende lasciando <c>EffectiveMultiplier</c> a 1 — cioe' esattamente
    /// nei casi in cui un run direbbe di aver fatto una cosa e ne avrebbe fatta un'altra.
    /// </summary>
    [Fact]
    public void TheSwitchGovernsBothTheWideningAndWhatTheRunDeclares()
    {
        Assert.Equal(
            StopMoneyPolicy.Enabled ? StopMoneyPolicy.Multiplier : 1m,
            StopMoneyPolicy.EffectiveMultiplier);

        // Con l'elenco vuoto (dal 24/09/2026) non c'e' una strategia su cui provare l'allargamento:
        // resta verificata la dichiarazione del fattore qui sopra.
        var widened = StopMoneyPolicy.WidenedStrategies.FirstOrDefault();
        if (widened is null)
            return;

        Assert.Equal(StopMoneyPolicy.Enabled, StopMoneyPolicy.AppliesTo(widened));
        Assert.Equal(
            1000m * StopMoneyPolicy.EffectiveMultiplier,
            StopMoneyPolicy.Widen(widened, 1000m));

        // Il target ha un fattore proprio e lo stesso interruttore: a 1 la strategia tiene
        // l'obiettivo della ricerca anche mentre lo stop si allarga.
        Assert.Equal(
            StopMoneyPolicy.Enabled ? StopMoneyPolicy.TargetMultiplier : 1m,
            StopMoneyPolicy.EffectiveTargetMultiplier);
        Assert.Equal(
            2000m * StopMoneyPolicy.EffectiveTargetMultiplier,
            StopMoneyPolicy.WidenTarget(widened, 2000m));
    }

    /// <summary>
    /// Una strategia fuori elenco tiene la distanza di stop della ricerca. E' la meta' che
    /// distingue un allargamento selettivo da uno generale, e senza questa verifica un elenco
    /// ignorato passerebbe inosservato: gli stop sarebbero tutti allargati e nessun test se ne
    /// accorgerebbe.
    /// </summary>
    [Fact]
    public void AStrategyOutsideTheListKeepsTheResearchStop()
    {
        var outside = StrategyFactory.GetRegisteredStrategies()
            .Select(definition => definition.Id)
            .FirstOrDefault(id => !StopMoneyPolicy.AppliesTo(id));

        Assert.NotNull(outside);
        Assert.False(StopMoneyPolicy.AppliesTo(outside));
        Assert.Equal(1000m, StopMoneyPolicy.Widen(outside, 1000m));
        Assert.Equal(1000m, StopMoneyPolicy.Widen(null, 1000m));
        Assert.Equal(1000m, StopMoneyPolicy.Widen("   ", 1000m));
        Assert.Equal(2000m, StopMoneyPolicy.WidenTarget(outside, 2000m));
        Assert.Null(StopMoneyPolicy.WidenTarget(outside, null));
    }

    /// <summary>
    /// Ogni codice dell'elenco esiste nel catalogo. Un codice scritto male non fa rumore — la
    /// strategia semplicemente non viene allargata, il run gira e il summary dichiara un elenco che
    /// contiene un nome che non opera — quindi il refuso va trovato qui e non leggendo un'equity
    /// che non torna.
    /// </summary>
    [Fact]
    public void EveryListedStrategyExistsInTheCatalogue()
    {
        var catalogue = StrategyFactory.GetRegisteredStrategies()
            .Select(definition => definition.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unknown = StopMoneyPolicy.WidenedStrategies
            .Where(code => !catalogue.Contains(code))
            .ToList();

        Assert.True(unknown.Count == 0,
            "Codici in StopMoneyPolicy che il catalogo non conosce:" + Environment.NewLine +
            string.Join(Environment.NewLine, unknown));
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

                // Le PT5DAV dichiarano lo stop in multipli di ATR50 (StopAtr), non in denaro: il
                // valore dipende dalla storia e non si legge da un campo. Qui si verifica che lo stop
                // ci sia quando e' dichiarato; che sia quello giusto lo misura Pt5DavParityStudy
                // contro i trade della ricerca.
                if (ReadField(strategy, "StopAtr") is > 0m)
                {
                    if (emitted.StopLossMoneyPerFutureContract is not > 0m)
                        violations.Add($"{id}: stop in ATR dichiarato, nessuno stop emesso.");
                    continue;
                }

                var declared = ReadDeclaredStop(strategy, emitted.Type);
                var factor = StopMoneyPolicy.AppliesTo(emitted.StrategyCode)
                    ? StopMoneyPolicy.EffectiveMultiplier
                    : 1m;
                var expected = declared > 0m ? declared * factor : (decimal?)null;

                if (emitted.StopLossMoneyPerFutureContract != expected)
                {
                    violations.Add(
                        $"{id}: dichiarato {declared}, fattore {factor}, atteso {Describe(expected)}, " +
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
