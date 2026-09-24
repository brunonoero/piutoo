using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.ResearchContainers;

/// <summary>
/// Simbolo e timeframe di un contenitore generico: arrivano dai parametri, non dalla classe, cosi' un
/// contenitore per motore vale per ogni mercato e timeframe. I default servono solo al catalogo, che
/// istanzia le classi senza parametri.
/// </summary>
public sealed class ResearchContainerIdentity
{
    public string Symbol { get; set; } = "@FDAX";
    public int TimeframeMinutes { get; set; } = 240;
}

/// <summary>
/// Imposta le leve di un motore da un dizionario di parametri, <b>per nome</b>: e' l'<c>Initialize</c>
/// comune dei contenitori generici (<c>RC_*</c>).
///
/// <para><b>Perche' per nome e non a mano.</b> I contenitori PT3B hanno un <c>Initialize</c> scritto a
/// mano per motore, e la skill <c>griglia-grossa</c> avverte che una chiave che la classe non legge
/// rende la leva <b>inerte in silenzio</b>: la griglia gira tre volte la stessa cosa e nessuno se ne
/// accorge. Qui una chiave che il motore non ha <b>ferma</b> la configurazione con un errore, a meno
/// che non sia una delle chiavi comuni della griglia al loro valore spento (pattern alle sentinelle,
/// nessun orario, nessun giorno escluso): quelle un motore che non le ha le ignora, perche' spente non
/// cambierebbero niente.</para>
///
/// <para>La riflessione si paga alla creazione della strategia, una volta per combinazione, non a ogni
/// barra: i membri si risolvono una volta per tipo e restano in cache.</para>
/// </summary>
public static class ResearchContainerSettings
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>Nomi della griglia e della sweep -> nome del membro del motore.</summary>
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["PtnNeutYes"] = "NeutralYes",
        ["PtnNeutNo"] = "NeutralNo",
        ["PtnDirYes"] = "DirectionalYes",
        ["PtnDirNo"] = "DirectionalNo",
        ["StopLoss"] = "StopMoney",
        ["TakeProfit"] = "ProfitMoney",
        ["TrailingStop"] = "TrailingStopMoney",
        ["BreakEven"] = "BreakEvenMoney",
        ["StopAtr"] = "StopAtrMultiplier",
        ["TargetAtr"] = "TargetAtrMultiplier",
        ["BbLength"] = "BollingerLength",
        ["BbNumDevs"] = "BollingerNumDevs"
    };

    /// <summary>Chiavi comuni che un motore senza quel membro ignora, ma solo al valore spento.</summary>
    private static readonly Dictionary<string, decimal> IgnorableWhenOff = new(StringComparer.Ordinal)
    {
        ["PtnNeutYes"] = 55, ["PtnNeutNo"] = 56, ["PtnDirYes"] = 52, ["PtnDirNo"] = 53,
        ["StartHour"] = -1, ["EndHour"] = -1, ["SkipDay"] = -1,
        ["DvolMin"] = 0, ["OffsetTicks"] = 0, ["TrailingStop"] = 0, ["BreakEven"] = 0
    };

    private static readonly ConcurrentDictionary<(Type, string), MemberInfo?> Members = new();

    private static readonly MethodInfo ExitHourConverter =
        typeof(EasyEngineBase).GetMethod("ResearchExitHourOrOff", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(nameof(EasyEngineBase), "ResearchExitHourOrOff");

    /// <summary>
    /// Il motore nudo, prima di qualunque parametro: un contratto, etichetta sull'apertura come tutta
    /// la ricerca PT3B, tutto il giorno, intraday, niente denaro fisso. Il resto sono i default del
    /// motore, che per i pattern sono gia' le sentinelle.
    /// </summary>
    public static void Prepare(EasyEngineBase engine)
    {
        Set(engine, "Contracts", 1);
        Set(engine, "ResearchLabelsBarsOnOpen", true);
        Set(engine, "IntradayOnly", true);
        Set(engine, "StopMoney", 0);
        Set(engine, "ProfitMoney", 0);
        Set(engine, "TrailingStopMoney", 0);
        Set(engine, "BreakEvenMoney", 0);
        Set(engine, "MaxBars", 0);
        if (Find(engine.GetType(), "TradingWindow") is not null)
            Set(engine, "TradingWindow", Piootoo.Shared.Configuration.ZonedWindow.AllDay);
    }

    /// <summary>Applica i parametri. Vedi la nota sulla classe per cosa ferma e cosa si ignora.</summary>
    public static void Apply(EasyEngineBase engine, ResearchContainerIdentity identity, IDictionary<string, object>? parameters)
    {
        if (parameters is null)
            return;

        if (parameters.TryGetValue("Symbol", out var symbol) && symbol is string text && !string.IsNullOrWhiteSpace(text))
            identity.Symbol = "@" + text.Trim().TrimStart('@').ToUpperInvariant();
        if (parameters.TryGetValue("TimeframeMinutes", out var timeframe) && System.Convert.ToInt32(timeframe, CultureInfo.InvariantCulture) > 0)
            identity.TimeframeMinutes = System.Convert.ToInt32(timeframe, CultureInfo.InvariantCulture);

        // Il tick e' dello strumento: senza, i motori che misurano in tick userebbero quello del
        // simbolo di default del catalogo.
        if (Find(engine.GetType(), "TickSize") is not null && InstrumentRegistry.TryGet(identity.Symbol, out var instrument))
            Set(engine, "TickSize", instrument.TickSize);

        foreach (var (key, value) in parameters)
        {
            if (key is "Symbol" or "TimeframeMinutes")
                continue;

            ApplyOne(engine, key, value);
        }
    }

    private static void ApplyOne(EasyEngineBase engine, string key, object value)
    {
        var type = engine.GetType();

        switch (key)
        {
            case "ExitHour":
                Set(engine, "SessionExitTime", ExitHourConverter.Invoke(null, [value]));
                return;

            case "IntradayOnly":
                Set(engine, "IntradayOnly", System.Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0);
                return;

            case "LevelOffsetTicks":
                // Una leva sola per i due lati del reversal sui livelli di ieri.
                Set(engine, "LongLevelOffsetTicks", System.Convert.ToInt32(value, CultureInfo.InvariantCulture));
                Set(engine, "ShortLevelOffsetTicks", System.Convert.ToInt32(value, CultureInfo.InvariantCulture));
                return;

            case "SkipDay" when Find(type, "SkipDay") is null && Find(type, "DayToFilter") is not null:
                // Il reversal Bollinger conta i giorni da 1 (la ricerca da 0) e usa -1 per "nessuno".
                var day = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
                Set(engine, "DayToFilter", day < 0 ? -1 : day + 1);
                return;

            case "StartHour" or "EndHour":
                if (System.Convert.ToDecimal(value, CultureInfo.InvariantCulture) == -1m)
                    return;
                throw new ArgumentException(
                    $"{type.Name}: '{key}' diverso da -1 non e' supportato dai contenitori generici. La finestra oraria si cerca " +
                    "con la sweep, sui contenitori della cella.");
        }

        var member = Aliases.TryGetValue(key, out var alias) ? alias : key;
        if (Find(type, member) is null)
        {
            if (IgnorableWhenOff.TryGetValue(key, out var off) && System.Convert.ToDecimal(value, CultureInfo.InvariantCulture) == off)
                return;

            throw new ArgumentException(
                $"{type.Name} non ha la leva '{key}'" + (member != key ? $" ('{member}')" : string.Empty) +
                ": la griglia la variarebbe senza effetto. Toglila dalla configurazione del motore.");
        }

        Set(engine, member, value);
    }

    private static MemberInfo? Find(Type type, string name) =>
        Members.GetOrAdd((type, name), key =>
        {
            for (var current = key.Item1; current is not null; current = current.BaseType)
            {
                var field = current.GetField(key.Item2, Instance | BindingFlags.DeclaredOnly);
                if (field is not null && !field.IsInitOnly)
                    return field;

                var property = current.GetProperty(key.Item2, Instance | BindingFlags.DeclaredOnly);
                if (property?.SetMethod is not null)
                    return property;
            }

            return null;
        });

    private static void Set(EasyEngineBase engine, string name, object? value)
    {
        var member = Find(engine.GetType(), name)
                     ?? throw new ArgumentException($"{engine.GetType().Name} non ha il membro '{name}'.");

        switch (member)
        {
            case FieldInfo field:
                field.SetValue(engine, ConvertValue(value, field.FieldType));
                break;
            case PropertyInfo property:
                property.SetValue(engine, ConvertValue(value, property.PropertyType));
                break;
        }
    }

    private static object? ConvertValue(object? value, Type target)
    {
        if (value is null)
            return null;

        var type = Nullable.GetUnderlyingType(target) ?? target;
        if (type.IsInstanceOfType(value))
            return value;
        if (type.IsEnum)
            return Enum.ToObject(type, System.Convert.ToInt32(value, CultureInfo.InvariantCulture));
        if (type == typeof(bool))
            return System.Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;

        return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }
}
