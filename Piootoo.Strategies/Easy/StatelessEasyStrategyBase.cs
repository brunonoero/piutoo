using System.Collections.Concurrent;
using System.Reflection;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Adapter per le strategie EasyLanguage convertite. Ogni valutazione usa una
/// nuova istanza temporanea: i campi runtime vengono letti/scritti solo nello
/// snapshot posseduto dall'engine, mai nell'istanza registrata della strategia.
///
/// <para>
/// <b>Nota sulle performance.</b> Questo è il punto più caldo dell'intero sistema: viene
/// attraversato una volta per barra per strategia, quindi milioni di volte in un backtest.
/// Tutti i metadati di riflessione (field, metodi, proprietà) sono cacheati per tipo nelle
/// mappe statiche qui sotto. Prima lo erano zero: ogni valutazione faceva cinque scansioni
/// complete dei field più due lookup di membri, ed era il costo dominante del loop.
/// Non aggiungere chiamate a <c>GetType().GetProperty(...)</c> o <c>GetMethod(...)</c> nel
/// percorso di valutazione senza passare dalle cache.
/// </para>
/// </summary>
public abstract class StatelessEasyStrategyBase : ITradingStrategy
{
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> InstanceFieldCache = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> MarketPositionFieldCache = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> EntriesTodayFieldCache = new();
    private static readonly ConcurrentDictionary<Type, (FieldInfo? Stop, FieldInfo? Profit)> MoneyRiskFieldCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string Member), PropertyInfo?> PropertyCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string Member), MethodInfo?> MethodCache = new();

    public virtual bool IsPositionCloseDependent => false;

    string ITradingStrategy.Name => ReadProperty<string>(nameof(ITradingStrategy.Name));
    string ITradingStrategy.Description => ReadProperty<string>(nameof(ITradingStrategy.Description));
    string ITradingStrategy.Symbol => ReadProperty<string>(nameof(ITradingStrategy.Symbol));
    int ITradingStrategy.TimeframeMinutes => ReadProperty<int>(nameof(ITradingStrategy.TimeframeMinutes));
    int ITradingStrategy.RequiredCandles => ReadProperty<int>(nameof(ITradingStrategy.RequiredCandles));

    void ITradingStrategy.Initialize(Dictionary<string, object>? parameters)
    {
        var method = ResolveMethod(GetType(), nameof(ITradingStrategy.Initialize),
            [typeof(Dictionary<string, object>)])
            ?? throw new InvalidOperationException($"{GetType().Name} does not expose Initialize.");
        method.Invoke(this, [parameters]);
    }

    TradeSignal ITradingStrategy.GenerateSignal(OhlcvData[] data, DateTime currentDate)
        => InvokeGenerateSignal(this, data, currentDate);

    /// <summary>
    /// Esegue il codice legacy su un clone effimero, sincronizzando la posizione
    /// dal broker e restituendo la memoria tecnica all'engine.
    /// </summary>
    public TradeSignal Evaluate(StrategyEvaluationRequest request)
    {
        var type = GetType();
        var evaluationInstance = (StatelessEasyStrategyBase?)Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Cannot instantiate {type.FullName}.");

        var fields = GetInstanceFields(type);
        CopyFields(fields, this, evaluationInstance);
        RestoreRuntimeState(fields, evaluationInstance, request.Execution.RuntimeState);
        SetMarketPositionFields(type, evaluationInstance, request.Execution.Position?.Direction);
        SetEntriesTodayFields(type, evaluationInstance, request.Execution.EntriesToday);

        var signal = InvokeGenerateSignal(evaluationInstance, request.Ohlcv, request.BarTimeUtc);
        signal.RuntimeState = CaptureRuntimeState(fields, evaluationInstance);

        if (signal.Type is SignalType.Buy or SignalType.Sell)
        {
            EnrichSignal(evaluationInstance, signal);
        }

        if (signal.CompanionSignals is not null)
        {
            foreach (var companion in signal.CompanionSignals)
            {
                EnrichSignal(evaluationInstance, companion);
            }
        }

        return signal;
    }

    private void EnrichSignal(object evaluationInstance, TradeSignal signal)
    {
        signal.Symbol = string.IsNullOrWhiteSpace(signal.Symbol)
            ? ((ITradingStrategy)this).Symbol
            : signal.Symbol;
        signal.StrategyCode = string.IsNullOrWhiteSpace(signal.StrategyCode)
            ? ((ITradingStrategy)this).Name
            : signal.StrategyCode;

        AttachMoneyRiskFromEasyInputs(evaluationInstance, signal);
    }

    // ------------------------------------------------------------------ riflessione cacheata

    private T ReadProperty<T>(string propertyName)
    {
        var type = GetType();
        var property = PropertyCache.GetOrAdd(
            (type, propertyName),
            key => key.Type.GetProperty(key.Member, BindingFlags.Instance | BindingFlags.Public));

        return property?.GetValue(this) is T value
            ? value
            : throw new InvalidOperationException($"{type.Name} does not expose {propertyName}.");
    }

    private static MethodInfo? ResolveMethod(Type type, string methodName, Type[] parameterTypes) =>
        MethodCache.GetOrAdd(
            (type, methodName),
            _ => type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, parameterTypes));

    private static TradeSignal InvokeGenerateSignal(object instance, OhlcvData[] data, DateTime currentDate)
    {
        var type = instance.GetType();
        var method = ResolveMethod(type, nameof(ITradingStrategy.GenerateSignal),
            [typeof(OhlcvData[]), typeof(DateTime)])
            ?? throw new InvalidOperationException($"{type.Name} does not expose GenerateSignal.");

        return (TradeSignal?)method.Invoke(instance, [data, currentDate])
            ?? throw new InvalidOperationException($"{type.Name}.GenerateSignal returned null.");
    }

    private static void CopyFields(FieldInfo[] fields, object source, object target)
    {
        foreach (var field in fields)
            field.SetValue(target, field.GetValue(source));
    }

    private static void RestoreRuntimeState(
        FieldInfo[] fields, object target, IReadOnlyDictionary<string, object?> state)
    {
        if (state.Count == 0) return;
        foreach (var field in fields)
        {
            if (state.TryGetValue(field.Name, out var value) && value is not null && field.FieldType.IsInstanceOfType(value))
                field.SetValue(target, value);
        }
    }

    private static IReadOnlyDictionary<string, object?> CaptureRuntimeState(FieldInfo[] fields, object source)
    {
        var state = new Dictionary<string, object?>(fields.Length, StringComparer.Ordinal);
        foreach (var field in fields)
            state[field.Name] = field.GetValue(source);
        return state;
    }

    private static void SetMarketPositionFields(Type type, object target, SignalType? direction)
    {
        var fields = MarketPositionFieldCache.GetOrAdd(type, static key => GetInstanceFields(key)
            .Where(field => field.FieldType == typeof(int) &&
                (field.Name.Equals("_currentMP", StringComparison.OrdinalIgnoreCase) ||
                 field.Name.Equals("_marketPosition", StringComparison.OrdinalIgnoreCase) ||
                 field.Name.Equals("_mp", StringComparison.OrdinalIgnoreCase)))
            .ToArray());

        if (fields.Length == 0) return;

        var marketPosition = direction switch
        {
            SignalType.Buy => 1,
            SignalType.Sell => -1,
            _ => 0
        };

        foreach (var field in fields)
            field.SetValue(target, marketPosition);
    }

    private static void SetEntriesTodayFields(Type type, object target, int entriesToday)
    {
        var fields = EntriesTodayFieldCache.GetOrAdd(type, static key => GetInstanceFields(key)
            .Where(field => field.FieldType == typeof(int) &&
                field.Name.Equals("_entriesToday", StringComparison.OrdinalIgnoreCase))
            .ToArray());

        foreach (var field in fields)
            field.SetValue(target, entriesToday);
    }

    private static void AttachMoneyRiskFromEasyInputs(object source, TradeSignal signal)
    {
        var (stopField, profitField) = MoneyRiskFieldCache.GetOrAdd(source.GetType(), static type =>
        {
            var fields = GetInstanceFields(type);
            return (
                fields.FirstOrDefault(f => f.Name.Equals("_myStop", StringComparison.OrdinalIgnoreCase)),
                fields.FirstOrDefault(f => f.Name.Equals("_myProfit", StringComparison.OrdinalIgnoreCase)));
        });

        var stop = ReadPositiveNumber(stopField, source);
        var profit = ReadPositiveNumber(profitField, source);

        // Le sorgenti EasyLanguage usano SetStopContract: MyStop/MyProfit
        // sono dollari per contratto. Non sovrascrivere valori già dichiarati
        // esplicitamente dall'intent.
        if (stop.HasValue && !signal.StopLossMoneyPerFutureContract.HasValue)
        {
            signal.StopLoss = null;
            signal.StopLossMoneyPerFutureContract = stop.Value;
        }
        else if (signal.StopLossMoneyPerFutureContract.HasValue)
        {
            signal.StopLoss = null;
        }

        if (profit.HasValue && !signal.TakeProfitMoneyPerFutureContract.HasValue)
        {
            signal.TakeProfit = null;
            signal.TakeProfitMoneyPerFutureContract = profit.Value;
        }
        else if (signal.TakeProfitMoneyPerFutureContract.HasValue)
        {
            signal.TakeProfit = null;
        }
    }

    private static decimal? ReadPositiveNumber(FieldInfo? field, object source)
    {
        var raw = field?.GetValue(source);
        if (raw is null) return null;

        var value = Convert.ToDecimal(raw);
        return value > 0 ? value : null;
    }

    private static FieldInfo[] GetInstanceFields(Type type) =>
        InstanceFieldCache.GetOrAdd(type, static key =>
        {
            var fields = new List<FieldInfo>();
            for (var current = key; current is not null && current != typeof(StatelessEasyStrategyBase); current = current.BaseType)
                fields.AddRange(current.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            return fields.ToArray();
        });
}
