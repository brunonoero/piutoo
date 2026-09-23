namespace Piootoo.Shared.Models;

/// <summary>
/// Una consegna di giornate di spread dal raccoglitore: per ogni simbolo e giorno UTC, l'istogramma
/// dello spread ora per ora. Vedi <c>SpreadDailyStore</c>.
///
/// <para><b>Perche' istogrammi e non percentili.</b> I percentili di due giorni non si sommano, gli
/// istogrammi si': tenendo il conteggio di ogni spread osservato il server ricalcola in modo
/// <b>esatto</b> la distribuzione di una finestra qualunque — gli ultimi trenta giorni, oggi — senza
/// che il bot debba rimisurare il mese intero.</para>
/// </summary>
public sealed class SpreadDailyRequest
{
    /// <summary>Conto su cui si e' misurato: il server ne ricava la cartella del broker.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    public string BotVersion { get; set; } = string.Empty;

    public List<SpreadDayDto> Days { get; set; } = new();
}

/// <summary>Un simbolo in un giorno UTC. Un giorno senza tick si manda lo stesso, con le ore vuote:
/// dice che il mercato era chiuso, e senza il bot tornerebbe a cercarlo ogni giorno.</summary>
public sealed class SpreadDayDto
{
    /// <summary>Simbolo Piootoo (<c>@FESX</c>).</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Nome sul broker (<c>EU50.cash</c>).</summary>
    public string BrokerSymbol { get; set; } = string.Empty;

    /// <summary>Il giorno UTC misurato, a mezzanotte.</summary>
    public DateTime DayUtc { get; set; }

    /// <summary>Passo di quotazione: l'unita' in cui sono contati gli spread dell'istogramma.</summary>
    public decimal TickSize { get; set; }

    public decimal PipSize { get; set; }

    /// <summary>Cifre decimali del simbolo, per scrivere i prezzi come li scrive il broker.</summary>
    public int Digits { get; set; }

    public DateTime? FirstTickUtc { get; set; }
    public DateTime? LastTickUtc { get; set; }

    /// <summary>Le ore con almeno un tick. Un'ora assente e' un'ora senza quotazioni.</summary>
    public List<SpreadHourDto> Hours { get; set; } = new();
}

public sealed class SpreadHourDto
{
    /// <summary>Ora UTC di apertura, 0..23.</summary>
    public int Hour { get; set; }

    /// <summary>Coppie <c>[spread in tick, quante volte]</c>.</summary>
    public List<long[]> Bins { get; set; } = new();
}

public sealed class SpreadDailyResponse
{
    public string Broker { get; set; } = string.Empty;

    /// <summary>Giornate registrate, come <c>@FESX 2026-09-22</c>.</summary>
    public List<string> Stored { get; set; } = new();

    /// <summary>Il file per simbolo riscritto dalla finestra mobile, o null se non c'era niente da scrivere.</summary>
    public string? SymbolFile { get; set; }

    public List<string> Warnings { get; set; } = new();
}

/// <summary>Quali giornate il server ha gia', per simbolo: il bot misura solo quelle che mancano.</summary>
public sealed class SpreadDailyStatus
{
    public string Broker { get; set; } = string.Empty;

    /// <summary>Per simbolo (<c>@FESX</c>), i giorni presenti come <c>yyyy-MM-dd</c>.</summary>
    public Dictionary<string, List<string>> Days { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
