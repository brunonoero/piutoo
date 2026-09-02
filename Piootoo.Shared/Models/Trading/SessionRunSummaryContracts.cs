namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Scheda di un run di sessione, scritta accanto a <c>signals.json</c> e <c>trades.json</c>.
///
/// <para><b>Perche' esiste.</b> Gli artefatti di sessione contengono per default i soli intent
/// <i>riempiti</i> (<c>PersistOnlyFilledIntents</c>): su un run normale il 97% dei record
/// descrive ordini mai andati a mercato, e tenerli costa piu' di quanto valgano. Il prezzo pero'
/// e' che dagli artefatti non si distingue <b>"la strategia non ha mai emesso un intent"</b> da
/// <b>"ne ha emessi tanti e sono stati tutti rifiutati"</b> — che e' esattamente la domanda a cui
/// serve rispondere quando una strategia non opera. In compare-0015 la famiglia VBO ha zero fill
/// su tre strategie e due simboli, e con i soli artefatti la causa non e' determinabile.</para>
///
/// <para>Questa scheda e' il riassunto che sopravvive al filtro: conta gli intent per stato senza
/// tenerli, e affianca la copertura di storia di ogni stream. E' minuscola, si scrive una volta
/// alla chiusura del run, e rende inutile rifare il run con
/// <c>PIOOTOO_PERSIST_ALL_INTENTS=1</c> per sapere <i>dove</i> guardare.</para>
/// </summary>
public static class SessionRunSummarySchema
{
    public const int Version = 1;
    public const string FileName = "session-summary.json";
}

public sealed class SessionRunSummary
{
    public int SchemaVersion { get; init; } = SessionRunSummarySchema.Version;
    public required string SessionId { get; init; }
    public required string ExecutionMode { get; init; }
    public DateTime GeneratedAtUtc { get; init; }

    /// <summary>Prima e ultima barra chiusa vista dalla sessione, su qualunque stream.</summary>
    public DateTime? FirstBarUtc { get; init; }
    public DateTime? LastBarUtc { get; init; }

    public int IntentsEmitted { get; init; }
    public int IntentsFilled { get; init; }
    public int IntentsRejected { get; init; }
    public int IntentsCancelled { get; init; }
    public int IntentsOther { get; init; }

    public IReadOnlyList<SessionStreamSummary> Streams { get; init; } = [];
    public IReadOnlyList<SessionStrategySummary> Strategies { get; init; } = [];

    /// <summary>
    /// Anomalie rilevate in automatico, nello stesso spirito di <c>backtest-summary.json</c>: una
    /// riga per problema, in testa al file, cosi' che chi apre l'artefatto non debba prima sapere
    /// che cosa cercare.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

/// <summary>Copertura di storia di una coppia (simbolo, timeframe).</summary>
public sealed class SessionStreamSummary
{
    public required string Symbol { get; init; }
    public int TimeframeMinutes { get; init; }

    /// <summary>
    /// Massimo numero di barre che lo stream ha accumulato nel run. E' il massimo storico, non il
    /// conteggio finale: la storia viene potata a cio' che serve, e il conteggio finale
    /// direbbe meno del vero.
    /// </summary>
    public int HistoryBarsHighWater { get; init; }

    /// <summary>Il piu' alto <c>RequiredCandles</c> fra le strategie che insistono su questo stream.</summary>
    public int RequiredCandles { get; init; }

    public int StrategiesOnStream { get; init; }

    /// <summary>Strategie che non hanno MAI raggiunto la propria soglia di storia in tutto il run.</summary>
    public int StrategiesNeverEvaluated { get; init; }
}

public sealed class SessionStrategySummary
{
    public required string StrategyCode { get; init; }
    public required string Symbol { get; init; }
    public int TimeframeMinutes { get; init; }
    public int RequiredCandles { get; init; }

    /// <summary>
    /// false quando lo stream non ha mai avuto abbastanza barre: la strategia non e' stata
    /// valutata nemmeno una volta, e il server lo fa <b>in silenzio</b>. E' la prima cosa da
    /// guardare quando una strategia non ha prodotto niente.
    /// </summary>
    public bool EverEvaluable { get; init; }

    public int IntentsEmitted { get; init; }
    public int IntentsFilled { get; init; }
    public int IntentsRejected { get; init; }
    public int IntentsCancelled { get; init; }
    public DateTime? FirstIntentUtc { get; init; }
    public DateTime? LastIntentUtc { get; init; }
}
