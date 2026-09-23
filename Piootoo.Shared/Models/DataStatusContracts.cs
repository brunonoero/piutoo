namespace Piootoo.Shared.Models;

/// <summary>
/// Cosa c'e' e cosa manca, per ogni simbolo di un broker, di tutto cio' che serve a una cella di
/// ricerca o a un backtest su quel broker: barre, spread, swap, scheda, contratto. E' la risposta a
/// "posso lanciare?" senza aprire sei cartelle.
/// </summary>
public sealed class BrokerDataStatus
{
    public string Broker { get; set; } = string.Empty;
    public DateTime GeneratedUtc { get; set; }

    /// <summary>File di spread e di swap letti, per sapere di quale misura si parla.</summary>
    public string? SpreadFile { get; set; }
    public string? SwapFile { get; set; }

    public List<SymbolDataStatus> Symbols { get; set; } = new();
}

public sealed class SymbolDataStatus
{
    /// <summary>Simbolo Piootoo (<c>@FESX</c>).</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Nome sul broker, dalla tabella di conversione; vuoto se la tabella non lo mappa.</summary>
    public string? BrokerSymbol { get; set; }

    /// <summary>Contratto nel registro e calendario di mercato: senza, nessun run parte.</summary>
    public bool Registered { get; set; }

    public DateTime? MinuteFromUtc { get; set; }
    public DateTime? MinuteToUtc { get; set; }

    /// <summary>Aggregati presenti sopra il minuto, in minuti.</summary>
    public List<int> Aggregates { get; set; } = new();

    /// <summary>Aggregati costruiti prima della maschera di negoziazione: vanno ricostruiti.</summary>
    public List<int> AggregatesWithoutWindow { get; set; } = new();

    public bool HasSpread { get; set; }
    public string? SpreadMedian { get; set; }
    public DateTime? SpreadFromUtc { get; set; }
    public DateTime? SpreadToUtc { get; set; }

    /// <summary>Giornate di spread raccolte dal raccoglitore, sull'archivio giornaliero.</summary>
    public int SpreadDays { get; set; }

    /// <summary><c>manuale</c>, <c>auto</c> o null se manca.</summary>
    public string? Swap { get; set; }

    public DateTime? SymbolInfoSeenUtc { get; set; }

    /// <summary>Tutto cio' che serve a un run su questo broker c'e'.</summary>
    public bool Ready { get; set; }

    /// <summary>Cosa manca, a parole: e' la colonna da leggere quando <see cref="Ready"/> e' falso.</summary>
    public List<string> Missing { get; set; } = new();
}
