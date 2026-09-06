using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Shared.Models;

/// <summary>
/// Cosa c'e' in <c>piootoo-repository/spread/</c>: una cartella per broker con i CSV di
/// <c>PiootooSpreadDumpBot</c>. La console non apre quelle cartelle, le chiede al server come
/// qualsiasi altro dato.
/// </summary>
public sealed class SpreadBrokerInfo
{
    /// <summary>Nome della cartella, es. <c>FTMOPLATFORM</c>. E' cio' che va in <c>SpreadBroker</c>.</summary>
    public required string Broker { get; init; }

    /// <summary>Simboli misurati nel file per simbolo.</summary>
    public int SymbolCount { get; init; }

    /// <summary>
    /// Se accanto al file per simbolo c'e' il gemello per ora. Senza, la risoluzione
    /// <see cref="SpreadResolution.PerHour"/> non e' proponibile per questo broker: il run
    /// fallirebbe all'avvio, e farlo scoprire dopo il lancio sarebbe scortese.
    /// </summary>
    public bool HasHourly { get; init; }

    /// <summary>Nome del file per simbolo: porta la finestra misurata, che e' meta' dell'informazione.</summary>
    public required string FileName { get; init; }

    /// <summary>Ultima scrittura del file: l'eta' della misura, e una misura vecchia va vista.</summary>
    public DateTime LastWriteUtc { get; init; }
}

/// <summary>
/// Gli spread che un run applicherebbe, per come sono adesso sul disco: il preventivo che la
/// schermata di backtesting mostra prima di lanciare.
///
/// <para>Serve perche' lo spread e' l'unico parametro del run che non si scrive ma si <i>trova</i>,
/// e fino a qui si vedeva solo a run finito, nel report. Un numero sbagliato — la misura del broker
/// che non c'entra, un simbolo mai quotato — costava un backtest intero per essere scoperto.</para>
/// </summary>
public sealed class SpreadTableInfo
{
    public required string Broker { get; init; }

    /// <summary>Etichetta della misura: broker, colonna, risoluzione, file e data.</summary>
    public required string Source { get; init; }

    /// <summary>Colonna letta (<c>p50Spread</c>, <c>avgSpread</c>, <c>p90Spread</c>).</summary>
    public required string Column { get; init; }

    /// <summary>Risoluzione effettivamente caricata.</summary>
    public SpreadResolution Resolution { get; init; }

    /// <summary>
    /// Cose che non fermano un run ma che chi lancia deve sapere: finestra coperta a meta', simbolo
    /// senza tick, ore ripiegate sulla costante.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<SpreadSymbolInfo> Symbols { get; init; } = [];
}

/// <summary>Una riga del preventivo: lo spread di un simbolo, con l'escursione oraria se c'e'.</summary>
public sealed class SpreadSymbolInfo
{
    public required string Symbol { get; init; }

    /// <summary>La costante per simbolo. Con la risoluzione per ora e' il ripiego delle ore non quotate.</summary>
    public decimal Points { get; init; }

    /// <summary>Se per questo simbolo esistono le 24 ore. False = paga comunque la costante.</summary>
    public bool HasHours { get; init; }

    public decimal HourMin { get; init; }

    /// <summary>Ora UTC in cui cade il minimo: dice se il minimo riguarda o no le ore di una strategia.</summary>
    public int HourMinAt { get; init; }

    public decimal HourMax { get; init; }

    public int HourMaxAt { get; init; }

    /// <summary>Ore delle 24 che il broker non ha quotato e che portano il ripiego.</summary>
    public int FallbackHours { get; init; }
}
