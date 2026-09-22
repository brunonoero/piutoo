namespace Piootoo.Shared.Models.Brokers;

/// <summary>
/// Quello che un broker dichiara sui propri strumenti, spinto da un cBot verso
/// <c>piootoo-repository/symbol-info/{BROKER}/</c>.
///
/// <para><b>Perche' esiste.</b> Le stesse specifiche oggi vengono ricopiate a mano in tre posti —
/// il moltiplicatore di contratto in <c>symbol-conversions.json</c>, il finanziamento in
/// <c>swap/{BROKER}_swap-by-symbol.csv</c>, tick e pip un po' ovunque — leggendole dalla scheda del
/// simbolo in cTrader. Ricopiare a mano venti strumenti per ogni broker nuovo e' il punto del
/// sistema in cui un numero sbagliato non si vede: nessun test puo' accorgersene, perche' il numero
/// vero sta su uno schermo.</para>
///
/// <para><b>E' una MISURA, e va datata.</b> Le tariffe di swap cambiano quando il broker le cambia,
/// e nessuno lo annuncia. Un valore senza la data in cui e' stato rilevato non e' una misura, e' un
/// numero: per questo l'archivio tiene <b>scatti</b>, e ogni scatto dice da quando a quando quelle
/// specifiche sono state viste.</para>
/// </summary>
public sealed class SymbolInfoIngestRequestDto
{
    /// <summary>Codice del broker: e' la sottocartella, e due broker non si mescolano mai.</summary>
    public string Broker { get; set; } = string.Empty;

    /// <summary>
    /// Conto da cui la rilevazione viene. Alcune voci — commissioni, volumi minimi — dipendono dal
    /// conto e non dal solo broker, quindi uno scatto che non dice da quale conto viene non e'
    /// verificabile.
    /// </summary>
    public string? AccountNumber { get; set; }

    /// <summary>Versione del bot che ha rilevato, per riconoscere gli scatti di una versione con un difetto noto.</summary>
    public string? BotVersion { get; set; }

    /// <summary>
    /// Istante della rilevazione. Nullo = adesso secondo il server. Lo dichiara il bot perche' una
    /// rilevazione fatta a mercato chiuso vale meno di una a mercato aperto, e l'ora lo dice.
    /// </summary>
    public DateTime? TakenUtc { get; set; }

    public List<SymbolInfoDto> Symbols { get; set; } = [];
}

/// <summary>Le specifiche di un singolo strumento presso quel broker.</summary>
public sealed class SymbolInfoDto
{
    /// <summary>Nome dello strumento <b>sul broker</b> (<c>USTEC</c>, <c>US100.cash</c>).</summary>
    public string BrokerSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Nome Piootoo corrispondente (<c>@NQ</c>), quando il bot lo conosce — cioe' quando il piano
    /// glielo ha detto. Su un broker <b>nuovo</b> la tabella di conversione non esiste ancora e qui
    /// c'e' <c>null</c>: l'archivio registra cosa il broker dichiara, la mappatura e' una decisione e
    /// vive altrove.
    /// </summary>
    public string? PiootooSymbol { get; set; }

    /// <summary>
    /// Ogni proprieta' che l'API espone, per nome, come stringa. Si tiene il dump <b>intero</b> e non
    /// i soli campi che oggi si leggono: costa poco, e fra sei mesi servira' un campo che oggi non
    /// guardiamo. Le stringhe e non i tipi perche' l'API cambia fra versioni della piattaforma, e un
    /// archivio che rifiuta un campo nuovo e' un archivio che perde la rilevazione.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Cose che il bot ha notato e che non sono errori: un valore a zero perche' non erano ancora
    /// arrivati i tick, una proprieta' che ha sollevato in lettura. Vanno tenute: uno zero senza
    /// spiegazione, sei mesi dopo, sembra una misura.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>Esito dell'ingestione, uno per simbolo.</summary>
public sealed class SymbolInfoIngestResponseDto
{
    public string Broker { get; set; } = string.Empty;
    public DateTime TakenUtc { get; set; }
    public List<SymbolInfoIngestResultDto> Symbols { get; set; } = [];

    /// <summary>Simboli le cui specifiche sono cambiate rispetto all'ultimo scatto.</summary>
    public int Changed { get; set; }

    /// <summary>Simboli identici all'ultimo scatto: nessuno scatto nuovo, solo la data di ultima vista.</summary>
    public int Unchanged { get; set; }

    public int Rejected { get; set; }
}

public sealed class SymbolInfoIngestResultDto
{
    public string BrokerSymbol { get; set; } = string.Empty;
    public string? PiootooSymbol { get; set; }

    /// <summary>
    /// <c>true</c> = le specifiche sono cambiate ed e' nato uno scatto nuovo. Il primo invio di un
    /// simbolo e' sempre un cambiamento.
    /// </summary>
    public bool Changed { get; set; }

    /// <summary>Quali proprieta' sono cambiate, per non dover confrontare due file a mano.</summary>
    public List<string> ChangedProperties { get; set; } = [];

    public int SnapshotCount { get; set; }

    /// <summary>Valorizzato quando il simbolo e' stato scartato, con il motivo.</summary>
    public string? Rejected { get; set; }
}

/// <summary>
/// L'archivio di uno strumento presso un broker: la successione degli scatti, dal piu' vecchio al
/// piu' recente. E' il contenuto del file su disco.
/// </summary>
public sealed class SymbolInfoArchiveDto
{
    public string Broker { get; set; } = string.Empty;
    public string BrokerSymbol { get; set; } = string.Empty;

    /// <summary>L'ultimo nome Piootoo con cui questo strumento e' stato spinto, se mai lo e' stato.</summary>
    public string? PiootooSymbol { get; set; }

    public List<SymbolInfoSnapshotDto> Snapshots { get; set; } = [];
}

/// <summary>
/// Una rilevazione. Vale <b>da</b> <see cref="TakenUtc"/> <b>a</b> <see cref="LastSeenUtc"/>: due
/// rilevazioni identiche non producono due scatti, allungano il secondo estremo del primo.
///
/// <para>E' la sola forma onesta di un archivio alimentato da un bot che gira ogni giorno. Uno
/// scatto per rilevazione darebbe trecento copie identiche l'anno e renderebbe illeggibile l'unica
/// domanda che conta — <i>quando e' cambiato?</i> — che qui si legge dall'elenco stesso.</para>
/// </summary>
public sealed class SymbolInfoSnapshotDto
{
    public DateTime TakenUtc { get; set; }

    /// <summary>Ultima volta in cui queste stesse specifiche sono state viste.</summary>
    public DateTime LastSeenUtc { get; set; }

    public string? AccountNumber { get; set; }
    public string? BotVersion { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.Ordinal);
    public List<string> Warnings { get; set; } = [];
}
