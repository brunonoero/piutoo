namespace Piootoo.Shared.Models.Strategies;

/// <summary>
/// Scheda completa di una strategia, pensata per essere <b>letta fuori dal sistema</b>: è il
/// pacchetto che si allega a una revisione del porting, si diffonde a chi ha scritto il run di
/// ricerca, o si archivia accanto a un backtest.
///
/// <para><b>Perché non basta <see cref="StrategyCatalogItem"/>.</b> Il catalogo dice che una
/// strategia esiste e come si chiama; non dice con quali numeri è stata tradotta, da quale riga di
/// quale run viene, né cosa diceva il motore Python da cui è nata. Chi deve verificare una
/// conversione ha bisogno di tutti e tre insieme, e finora doveva aprire tre posti diversi
/// (sorgente C#, dossier del paniere, <c>easy_engine_py/</c>) sperando di guardare le versioni
/// giuste.</para>
///
/// <para><b>Cosa è autorevole e cosa no.</b> I <see cref="Sources"/> con
/// <see cref="StrategyExportDocument.FromAssembly"/> a <c>true</c> sono il sorgente <i>compilato
/// dentro il binario che ha risposto</i>: descrivono esattamente il codice che gira. Gli altri —
/// dossier e motore Python — sono letti dal repository dati al momento dell'export e possono
/// essere stati rigenerati dopo la traduzione. <see cref="Warnings"/> elenca ciò che non si è
/// potuto raccogliere, invece di lasciare campi vuoti da interpretare.</para>
/// </summary>
public sealed class StrategyExport
{
    /// <summary>
    /// Versione del formato di questo file. Si alza quando un campo cambia significato, così un
    /// export archiviato resta interpretabile: chi lo rilegge sa con quale schema è stato scritto.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Istante di generazione, UTC come tutto il resto del sistema.</summary>
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>Versione di Piootoo del server che ha prodotto l'export.</summary>
    public string PiootooVersion { get; set; } = string.Empty;

    /// <summary>Chi è la strategia: i due identificatori, simbolo, timeframe, tenuta.</summary>
    public StrategyExportIdentity Identity { get; set; } = new();

    /// <summary>
    /// Specifica del contratto di riferimento. Serve a rendere leggibili i parametri in denaro:
    /// uno stop di $4.000 è 80 punti su ES e 200 su NQ, e senza il moltiplicatore il numero da
    /// solo non si può confrontare con il report di ricerca.
    /// </summary>
    public StrategyExportInstrument? Instrument { get; set; }

    /// <summary>
    /// I parametri del motore così come li ha impostati il costruttore della strategia, letti
    /// dall'istanza. Sono i numeri della traduzione: è questa la parte da confrontare riga per
    /// riga con il report di sweep.
    /// </summary>
    public Dictionary<string, StrategyExportParameter> Parameters { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Da dove viene la strategia: motore, riga di dossier, file Python.</summary>
    public StrategyExportConversion Conversion { get; set; } = new();

    /// <summary>
    /// I testi integrali: sorgente C# della classe e del motore, motore Python, scheda del
    /// dossier. Sono la parte pesante dell'export ed è quella che porta i commenti di conversione.
    /// </summary>
    public List<StrategyExportDocument> Sources { get; set; } = new();

    /// <summary>
    /// Cosa non si è potuto raccogliere e perché. Un export incompleto lo dichiara: un campo
    /// vuoto senza spiegazione si legge come "non esiste" invece che come "non è stato trovato".
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>I due identificatori e le coordinate operative della strategia.</summary>
public sealed class StrategyExportIdentity
{
    /// <summary>Id di classe: la chiave di <b>selezione</b> (masterfilter, catalogo, factory).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Codice di <b>esecuzione</b> (<c>ITradingStrategy.Name</c>): quello che finisce in
    /// <c>signals.json</c>, <c>trades.json</c> e nelle chiavi di posizione. Confonderlo con
    /// <see cref="Id"/> ha già svuotato dei report.
    /// </summary>
    public string ExecutionCode { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public int TimeframeMinutes { get; set; }

    public string BarType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Nome completo del tipo .NET, per risalire alla classe senza indovinare.</summary>
    public string ClassFullName { get; set; } = string.Empty;

    /// <summary>Barre di storia che la strategia pretende prima di poter essere valutata.</summary>
    public int RequiredCandles { get; set; }

    /// <summary>Cosa la strategia <b>dichiara</b> di voler tenere; il piano può troncarla.</summary>
    public bool Overnight { get; set; }

    /// <summary>Se può attraversare il fine settimana. Implica <see cref="Overnight"/>.</summary>
    public bool Overweek { get; set; }

    /// <summary>Etichetta pronta da leggere: "intraday", "overnight", "overnight+overweek".</summary>
    public string HoldingLabel { get; set; } = string.Empty;
}

/// <summary>Il contratto su cui sono misurati i parametri in denaro.</summary>
public sealed class StrategyExportInstrument
{
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Denaro per un punto di prezzo, per una unità di quantità.</summary>
    public decimal PointValue { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal TickSize { get; set; }

    /// <summary>Fuso IANA in cui gli orari di sessione dichiarati dalla strategia sono corretti.</summary>
    public string SessionTimeZone { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Un parametro del motore con la sua provenienza. La classe che lo dichiara conta: dice se è una
/// scelta di questa strategia, del motore, o della base comune a tutti.
/// </summary>
public sealed class StrategyExportParameter
{
    /// <summary>Il valore impostato sull'istanza. <c>null</c> è un valore, non un'assenza.</summary>
    public object? Value { get; set; }

    /// <summary>Classe che dichiara il membro (es. <c>PriceChannelEngine</c>).</summary>
    public string DeclaredIn { get; set; } = string.Empty;

    /// <summary>Tipo .NET del membro, per leggere senza ambiguità un valore numerico.</summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>Le coordinate della traduzione: da quale motore e da quale riga di ricerca viene.</summary>
public sealed class StrategyExportConversion
{
    /// <summary>Sigla del motore di ricerca (<c>PC</c>, <c>TF_M</c>, …), se riconosciuta.</summary>
    public string? EngineCode { get; set; }

    /// <summary>Classe C# del motore da cui la strategia deriva.</summary>
    public string? EngineClass { get; set; }

    /// <summary>File del motore Python corrispondente, relativo al repository dati.</summary>
    public string? PythonEngineFile { get; set; }

    /// <summary>
    /// Identificativo della scheda del dossier che corrisponde a questa strategia <b>nell'edizione
    /// allegata</b>, trovato per impronta numerica (simbolo, timeframe, motore, stop, target) e non
    /// per S-ID. È <c>null</c> quando l'impronta non trova una scheda sola.
    /// </summary>
    public string? DossierId { get; set; }

    /// <summary>
    /// L'S-ID che il sorgente della classe <b>dichiara</b> nel paragrafo "Codice sorgente".
    ///
    /// <para>Vale come storia, non come puntatore: gli S-ID sono ordinati per atteso/trade e
    /// <b>scorrono a ogni rigenerazione</b> del dossier, quindi quello scritto al momento della
    /// traduzione punta quasi sempre a un'altra scheda nell'edizione corrente. Quando i due
    /// differiscono l'export lo dice in <c>warnings</c>. Vedi
    /// <c>docs/domini/mappa-strategie-pts.md</c>.</para>
    /// </summary>
    public string? DeclaredDossierId { get; set; }

    /// <summary>File del dossier da cui la scheda è stata estratta, relativo al repository dati.</summary>
    public string? DossierFile { get; set; }
}

/// <summary>Un testo integrale allegato all'export.</summary>
public sealed class StrategyExportDocument
{
    /// <summary>
    /// A cosa serve il documento: <c>strategy</c> (la classe tradotta), <c>engine</c> (il motore
    /// C#), <c>engine-python</c> (il motore di ricerca), <c>dossier</c> (la scheda del paniere).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Linguaggio del testo: <c>csharp</c>, <c>python</c>, <c>markdown</c>.</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Nome o percorso da cui il testo viene, per poterlo ritrovare.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Vero quando il testo è il sorgente compilato <b>dentro l'assembly che ha risposto</b>:
    /// descrive il codice che gira davvero. Falso quando è letto dal repository dati, che può
    /// essere stato rigenerato dopo la traduzione.
    /// </summary>
    public bool FromAssembly { get; set; }

    public string Text { get; set; } = string.Empty;
}
