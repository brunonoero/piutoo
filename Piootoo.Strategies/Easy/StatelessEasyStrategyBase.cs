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
/// </summary>
public abstract class StatelessEasyStrategyBase : ITradingStrategy
{
    public virtual bool IsPositionCloseDependent => false;

    string ITradingStrategy.Name => ReadProperty<string>(nameof(ITradingStrategy.Name));
    string ITradingStrategy.Description => ReadProperty<string>(nameof(ITradingStrategy.Description));
    string ITradingStrategy.Symbol => ReadProperty<string>(nameof(ITradingStrategy.Symbol));
    int ITradingStrategy.TimeframeMinutes => ReadProperty<int>(nameof(ITradingStrategy.TimeframeMinutes));
    int ITradingStrategy.RequiredCandles => ReadProperty<int>(nameof(ITradingStrategy.RequiredCandles));

    void ITradingStrategy.Initialize(Dictionary<string, object>? parameters)
        => InvokeLegacy(nameof(ITradingStrategy.Initialize), new[] { typeof(Dictionary<string, object>) }, parameters);

    TradeSignal ITradingStrategy.GenerateSignal(OhlcvData[] data, DateTime currentDate)
        => InvokeLegacy<TradeSignal>(nameof(ITradingStrategy.GenerateSignal), new[] { typeof(OhlcvData[]), typeof(DateTime) }, data, currentDate);

    /// <summary>
    /// Esegue il codice legacy su un clone effimero, sincronizzando la posizione
    /// dal broker e restituendo la memoria tecnica all'engine.
    /// </summary>
    public TradeSignal Evaluate(StrategyEvaluationRequest request)
    {
        var evaluationInstance = (StatelessEasyStrategyBase?)Activator.CreateInstance(GetType())
            ?? throw new InvalidOperationException($"Cannot instantiate {GetType().FullName}.");

        CopyFields(this, evaluationInstance);
        RestoreRuntimeState(evaluationInstance, request.Execution.RuntimeState);
        SetMarketPositionFields(evaluationInstance, request.Execution.Position?.Direction);
        SetEntriesTodayFields(evaluationInstance, request.Execution.EntriesToday);

        var signal = evaluationInstance.InvokeLegacy<TradeSignal>(
            nameof(ITradingStrategy.GenerateSignal),
            new[] { typeof(OhlcvData[]), typeof(DateTime) },
            request.Ohlcv,
            request.BarTimeUtc);
        signal.RuntimeState = CaptureRuntimeState(evaluationInstance);

        if (signal.Type is SignalType.Buy or SignalType.Sell)
        {
            EnrichSignal(evaluationInstance, signal);
        }

        if (signal.CompanionSignals is not null)
        {
            foreach (var companion in signal.CompanionSignals)
            {
                EnrichSignal(evaluationInstance, companion);
                companion.IsPositionCloseDependent = IsPositionCloseDependent;
            }
        }

        signal.IsPositionCloseDependent = IsPositionCloseDependent;
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

    private T ReadProperty<T>(string propertyName)
    {
        var property = GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(this) is T value
            ? value
            : throw new InvalidOperationException($"{GetType().Name} does not expose {propertyName}.");
    }

    private object? InvokeLegacy(string methodName, Type[] parameterTypes, params object?[] arguments)
    {
        var method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, parameterTypes);
        return method?.Invoke(this, arguments)
            ?? throw new InvalidOperationException($"{GetType().Name} does not expose {methodName}.");
    }

    private T InvokeLegacy<T>(string methodName, Type[] parameterTypes, params object?[] arguments)
        => (T)InvokeLegacy(methodName, parameterTypes, arguments)!;

    private static void CopyFields(object source, object target)
    {
        foreach (var field in GetInstanceFields(source.GetType()))
        {
            field.SetValue(target, field.GetValue(source));
        }
    }

    private static void RestoreRuntimeState(object target, IReadOnlyDictionary<string, object?> state)
    {
        foreach (var field in GetInstanceFields(target.GetType()))
        {
            if (state.TryGetValue(field.Name, out var value) && value is not null && field.FieldType.IsInstanceOfType(value))
            {
                field.SetValue(target, value);
            }
        }
    }

    private static IReadOnlyDictionary<string, object?> CaptureRuntimeState(object source)
        => GetInstanceFields(source.GetType()).ToDictionary(field => field.Name, field => field.GetValue(source), StringComparer.Ordinal);

    private static void SetMarketPositionFields(object target, SignalType? direction)
    {
        var marketPosition = direction switch
        {
            SignalType.Buy => 1,
            SignalType.Sell => -1,
            _ => 0
        };

        foreach (var field in GetInstanceFields(target.GetType())
                     .Where(field => field.FieldType == typeof(int) &&
                         (field.Name.Equals("_currentMP", StringComparison.OrdinalIgnoreCase) ||
                          field.Name.Equals("_marketPosition", StringComparison.OrdinalIgnoreCase) ||
                          field.Name.Equals("_mp", StringComparison.OrdinalIgnoreCase))))
        {
            field.SetValue(target, marketPosition);
        }
    }

    private static void SetEntriesTodayFields(object target, int entriesToday)
    {
        foreach (var field in GetInstanceFields(target.GetType())
                     .Where(field => field.FieldType == typeof(int) &&
                         field.Name.Equals("_entriesToday", StringComparison.OrdinalIgnoreCase)))
        {
            field.SetValue(target, entriesToday);
        }
    }

    private static void AttachMoneyRiskFromEasyInputs(object source, TradeSignal signal)
    {
        var fields = GetInstanceFields(source.GetType());
        var stop = ReadPositiveNumber(fields, source, "_myStop");
        var profit = ReadPositiveNumber(fields, source, "_myProfit");

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

    private static decimal? ReadPositiveNumber(IEnumerable<FieldInfo> fields, object source, string name)
    {
        var field = fields.FirstOrDefault(field => field.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (field?.GetValue(source) is null)
        {
            return null;
        }

        var value = Convert.ToDecimal(field.GetValue(source));
        return value > 0 ? value : null;
    }

    private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
    {
        for (var current = type; current is not null && current != typeof(StatelessEasyStrategyBase); current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }
}
