using Piootoo.Shared.Models.Trading;

namespace Piootoo.Shared.Models.Workspaces;

public sealed class WorkspaceMasterFilter
{
    public string Name { get; set; } = string.Empty;
    public List<string> StrategiesFilter { get; set; } = new();
}

public sealed class WorkspaceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StrategiesCount { get; set; }
}

/// <summary>
/// Chi ha prodotto una cartella di backtest. Da quando le sessioni di backtest aperte da piano
/// scrivono anch'esse sotto <c>backtests/</c>, i due tipi convivono nello stesso albero: l'origine
/// va dichiarata alla creazione, non dedotta a posteriori dai file presenti.
/// </summary>
public enum BacktestOrigin
{
    /// <summary>Cartella precedente all'introduzione del marcatore.</summary>
    Unknown,

    /// <summary>Motore di backtesting del server (<c>PiootooBacktestingService</c>).</summary>
    Internal,

    /// <summary>Sessione di trading eseguita da un engine esterno, tipicamente un cBot cTrader.</summary>
    ExternalBroker
}

/// <summary>
/// Da quale serie di prezzi ha letto un run. Non è deducibile dai trade: un run interno sul
/// datafeed del vendor e uno sulle barre di un broker producono file strutturalmente identici,
/// stesse size e stesse commissioni, e i livelli si somigliano abbastanza da non poterci
/// scommettere sopra. Va dichiarata.
/// </summary>
public enum PriceSourceKind
{
    /// <summary>Run precedente all'introduzione del campo.</summary>
    Unknown,

    /// <summary>Datafeed interno (<c>piootoo-repository/datafeed/</c>), CSV del vendor.</summary>
    Futures,

    /// <summary>Barre CFD di un broker: <c>datafeed-external/{BROKER}/</c>, o il broker stesso.</summary>
    BrokerCfd
}

/// <summary>
/// La serie di prezzi di un run. <see cref="Broker"/> è valorizzato se e solo se
/// <see cref="Kind"/> è <see cref="PriceSourceKind.BrokerCfd"/>: due broker chiudono le stesse
/// candele su prezzi diversi, quindi "CFD" senza il nome non identifica niente.
/// </summary>
public sealed class RunPriceSource
{
    public PriceSourceKind Kind { get; set; } = PriceSourceKind.Unknown;
    public string? Broker { get; set; }

    public static RunPriceSource Futures() => new() { Kind = PriceSourceKind.Futures };

    public static RunPriceSource Cfd(string? broker) => new()
    {
        Kind = PriceSourceKind.BrokerCfd,
        Broker = string.IsNullOrWhiteSpace(broker) ? null : broker.Trim().ToUpperInvariant()
    };

    /// <summary>Interno: il broker del datafeed, null per il feed del vendor.</summary>
    public static RunPriceSource FromDatafeedBroker(string? datafeedBroker)
        => string.IsNullOrWhiteSpace(datafeedBroker) ? Futures() : Cfd(datafeedBroker);

    /// <summary>
    /// Come si dice a chi legge un report da quale archivio di barre viene il run. Il nome del
    /// broker c'e' sempre quando la serie e' un CFD: "CFD" da solo non identifica niente, ed e'
    /// proprio l'informazione che decide se due curve di equity sono confrontabili.
    /// </summary>
    public string FeedLabel => Kind switch
    {
        PriceSourceKind.Futures => "datafeed interno - futures del vendor (piootoo-repository/datafeed)",
        PriceSourceKind.BrokerCfd => string.IsNullOrWhiteSpace(Broker)
            ? "CFD di un broker non dichiarato"
            : $"CFD {Broker} (datafeed-external/{Broker})",
        _ => "non dichiarato dal run"
    };
}

/// <summary>
/// Marcatore scritto nella cartella del backtest alla creazione (<c>origin.json</c>). Serve a
/// distinguere un run interno da uno prodotto dall'engine esterno senza euristiche sui file
/// presenti: <c>backtest-summary.json</c> manca anche in un run interno interrotto, quindi usarlo
/// come indizio etichetterebbe come esterno un backtest che non lo è.
/// <para>
/// Motore (<see cref="Origin"/>) e prezzi (<see cref="PriceSource"/>) sono le due cose che
/// rendono due run non confrontabili, e insieme danno i tre tipi che <see cref="RunSlug"/>
/// nomina. Non c'è un campo "tipo" a parte: sarebbe una quarta cosa che può contraddire le
/// altre tre.
/// </para>
/// </summary>
public sealed class BacktestOriginInfo
{
    public const string FileName = "origin.json";

    public BacktestOrigin Origin { get; set; } = BacktestOrigin.Unknown;
    public DateTime CreatedUtc { get; set; }

    /// <summary>La serie di prezzi letta dal run. Assente nei run precedenti al campo.</summary>
    public RunPriceSource? PriceSource { get; set; }

    /// <summary>Versione del binario che ha eseguito il run (<c>PiootooVersion.Current</c>).</summary>
    public string? EngineVersion { get; set; }

    /// <summary>Valorizzati solo per l'origine esterna.</summary>
    public string? PlanCode { get; set; }
    public string? ExecutionKey { get; set; }
    public string? SessionId { get; set; }

    /// <summary>Il conto per cui la sessione è stata aperta. Solo per l'origine esterna.</summary>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// La serie di prezzi del run, con l'unica deduzione lecita gia' applicata: un run dell'engine
    /// esterno gira per definizione sui prezzi del broker, e nei marcatori scritti prima del campo
    /// l'ignoto e' il <i>nome</i> del broker, non il tipo di serie. Sta qui perche' <see
    /// cref="RunSlug"/> e i report devono dire la stessa cosa: due deduzioni scritte due volte
    /// finiscono per divergere.
    /// </summary>
    public RunPriceSource ResolvedPriceSource
        => PriceSource
           ?? (Origin == BacktestOrigin.ExternalBroker
               ? RunPriceSource.Cfd(null)
               : new RunPriceSource());

    /// <summary>
    /// Il nome del tipo di run, per i file di confronto: <c>interno-futures</c>,
    /// <c>interno-cfd-{BROKER}</c>, <c>cbot-cfd-{BROKER}</c>. Si copia negli artefatti esportati
    /// (<c>trades-&lt;slug&gt;.json</c>) così il tipo non lo digita nessuno a mano. Convenzione e
    /// trappole di misura in <c>piootoo-repository/compare/README.md</c>.
    /// </summary>
    public string RunSlug
    {
        get
        {
            var source = ResolvedPriceSource;
            var kind = source.Kind;
            var broker = source.Broker;
            var feed = kind switch
            {
                PriceSourceKind.Futures => "futures",
                PriceSourceKind.BrokerCfd => string.IsNullOrWhiteSpace(broker) ? "cfd" : $"cfd-{broker}",
                _ => "feed-sconosciuto"
            };
            var engine = Origin switch
            {
                BacktestOrigin.Internal => "interno",
                BacktestOrigin.ExternalBroker => "cbot",
                _ => "motore-sconosciuto"
            };
            return $"{engine}-{feed}";
        }
    }

    /// <summary>
    /// Vero quando <see cref="RunSlug"/> identifica davvero il run, e quindi si può stampare sul
    /// nome di un artefatto esportato. Un CFD senza il nome del broker non identifica niente — due
    /// broker chiudono le stesse candele su prezzi diversi — ed è la ragione per cui questo non
    /// coincide con "lo slug non contiene la parola sconosciuto".
    /// </summary>
    public bool IdentifiesRun
    {
        get
        {
            if (Origin is not (BacktestOrigin.Internal or BacktestOrigin.ExternalBroker))
                return false;

            return PriceSource?.Kind switch
            {
                PriceSourceKind.Futures => true,
                PriceSourceKind.BrokerCfd => !string.IsNullOrWhiteSpace(PriceSource.Broker),
                _ => false
            };
        }
    }

    /// <summary>
    /// Capitale iniziale dichiarato all'apertura della sessione.
    /// </summary>
    /// <remarks>
    /// Il piano non ha un capitale (vedi <c>docs/decisioni.md</c> 2026-08-05) e la sessione esterna
    /// non lascia altra traccia del proprio: senza questo campo il report ricostruito a posteriori
    /// dovrebbe inventarne uno, e percentuali e drawdown sarebbero riferiti a una base arbitraria.
    /// <c>null</c> nelle cartelle scritte prima della sua introduzione.
    /// </remarks>
    public decimal? InitialCapital { get; set; }
}

/// <summary>
/// Gli artefatti di un run impacchettati per un confronto, già rinominati con
/// <see cref="BacktestOriginInfo.RunSlug"/>. È uno zip e non i singoli file perché il client parla
/// solo HTTP: scompatta lui nella cartella scelta, e i nomi arrivano dal run invece che dalle dita
/// di chi copia.
/// </summary>
/// <param name="RunSlug">Il tipo di run, es. <c>interno-futures</c>.</param>
/// <param name="FileName">Nome proposto per l'archivio.</param>
/// <param name="Entries">Nomi dei file contenuti, per dire all'utente cosa ha preso.</param>
/// <param name="Content">L'archivio.</param>
public sealed record CompareExportBundle(
    string RunSlug,
    string FileName,
    IReadOnlyList<string> Entries,
    byte[] Content);

public sealed class WorkspaceBacktestInfo
{
    public string FolderName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime LastModifiedUtc { get; set; }
    public int ResultsCount { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public bool HasResults => ResultsCount > 0;

    public BacktestOrigin Origin { get; set; } = BacktestOrigin.Unknown;

    /// <summary>Piano che ha prodotto il run, per l'origine esterna.</summary>
    public string? PlanCode { get; set; }
}

public sealed class CreateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> StrategiesFilter { get; set; } = new();
}

/// <summary>
/// Riga della tabella di conversione di un account: traduce il simbolo Piootoo nel simbolo
/// usato dal broker e scala la size del contratto.
/// </summary>
public sealed class AccountSymbolMapping
{
    /// <summary>Simbolo Piootoo, come compare nelle strategie del catalogo (es. <c>@NQ</c>).</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Simbolo equivalente sull'account/broker (es. <c>USDTEC</c>).</summary>
    public string AccountSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Fattore moltiplicativo del contratto: 1 contratto Piootoo vale
    /// <c>ContractMultiplier</c> contratti sull'account. Es. 0.1 se il contratto del broker
    /// vale 100k contro 1M del contratto Piootoo. Deve essere maggiore di zero.
    /// </summary>
    public decimal ContractMultiplier { get; set; } = 1m;

    /// <summary>
    /// Fattore con cui convertire le distanze di prezzo (stop, target, trailing, break even)
    /// dichiarate dalle strategie nei punti dello strumento del broker: una distanza Piootoo di
    /// <c>d</c> punti vale <c>d * PriceScale</c> punti sull'account.
    ///
    /// <para>Vale 1 quasi sempre, perché i punti sono la grandezza invariante del contratto: 20
    /// punti restano 20 punti su future, mini, micro e CFD dello stesso sottostante. Serve solo
    /// dove il broker quota lo stesso sottostante in un'altra unità (es. un indice quotato in
    /// centesimi contro i punti interi del future), che è un cambio di unità di misura del prezzo
    /// e non del contratto — per questo è separato da <see cref="ContractMultiplier"/>, che scala
    /// invece la sola quantità.</para>
    ///
    /// <para>Un valore non positivo viene letto come 1: una scala mancante non deve azzerare gli
    /// stop in silenzio.</para>
    /// </summary>
    public decimal PriceScale { get; set; } = 1m;

    /// <summary>
    /// Quantità minima eseguibile su questo simbolo presso il broker dell'account, espressa nei
    /// contratti del broker. Sotto questa soglia l'intent vale zero e non viene consegnato: meglio
    /// nessun ordine che un ordine di taglia non eseguibile.
    /// </summary>
    public decimal MinimumQuantity { get; set; } = 1m;

    /// <summary>Passo di volume del broker; la quantità viene arrotondata per difetto a un suo multiplo.</summary>
    public decimal QuantityStep { get; set; } = 1m;

    /// <summary>
    /// <b>Obsoleta dal 24/08/2026:</b> l'arrotondamento è una proprietà della tabella, non della
    /// riga — vedi <see cref="SymbolConversion.RoundingMode"/>. Resta letta per migrare i file
    /// scritti prima di quella data e non viene più riscritta.
    ///
    /// <para>Stava qui perché la si era pensata come proprietà della coppia broker/strumento, ma
    /// una tabella di conversione descrive <i>un</i> broker: la granularità del volume la decide
    /// quello. Per riga significava solo poterla sbagliare N volte invece di una — ed è
    /// esattamente quello che è successo.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public QuantityRoundingMode? RoundingMode { get; set; }

    /// <summary>Se false il simbolo resta configurato ma non è operativo sull'account.</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>Account di trading globale, condiviso da tutti i workspace.</summary>
public sealed class WorkspaceAccount
{
    /// <summary>Slug derivato dal nome; identifica univocamente l'account nel registro globale.</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Codice account del broker, lo stesso usato in <c>AccountGroupMapping</c>.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Gruppo anti copy-trading (tipicamente la prop firm) usato dalle trading session.</summary>
    public string GroupId { get; set; } = string.Empty;

    public string Broker { get; set; } = string.Empty;

    public string Currency { get; set; } = "USD";

    /// <summary>Balance iniziale dell'account nella valuta indicata.</summary>
    public decimal InitialBalance { get; set; }

    public bool Enabled { get; set; } = true;

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Codice della tabella di conversione simboli associata (<see cref="SymbolConversion.Code"/>),
    /// dal registro globale. Vuoto = nessuna conversione, l'account opera 1 a 1.
    /// </summary>
    public string SymbolConversionCode { get; set; } = string.Empty;
}

/// <summary>
/// Tabella di conversione simboli nominata, definita nel registro globale (fuori da workspace e
/// account): un account la referenzia per <see cref="Code"/> invece di portarne una copia propria,
/// così più account possono condividere la stessa tabella.
/// </summary>
public sealed class SymbolConversion
{
    /// <summary>Identificativo univoco scelto dall'utente, stabile: è ciò che gli account referenziano.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Granularità del volume del broker descritto da questa tabella, applicata a <b>tutti</b> i
    /// suoi simboli: contratti interi per un broker future, passo di volume per un broker CFD.
    ///
    /// <para>Vive sulla tabella e non sulla riga perché è una proprietà del broker: una tabella
    /// mappa un conto solo, e un conto non è a contratti interi per l'oro e a frazioni per il
    /// petrolio. Fino al 24/08/2026 stava sulla singola riga, e la conseguenza è stata che un
    /// intero file di mappature CFD si è ritrovato con l'arrotondamento dei future e ha azzerato
    /// ogni quantità frazionaria senza che nessuna riga apparisse sbagliata.</para>
    ///
    /// <para>Il valore per riga, quando presente nei file vecchi, viene migrato in lettura
    /// (vedi <c>WorkspaceService.NormalizeSymbolConversion</c>).</para>
    /// </summary>
    public QuantityRoundingMode RoundingMode { get; set; } = QuantityRoundingMode.BrokerVolumeStep;

    /// <summary>Tabella di conversione, una riga per simbolo.</summary>
    public List<AccountSymbolMapping> Mappings { get; set; } = new();

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}

/// <summary>Contenuto del registro globale <c>accounts/symbol-conversions.json</c>.</summary>
public sealed class SymbolConversionsFile
{
    public List<SymbolConversion> Conversions { get; set; } = new();
}

/// <summary>Contenuto del registro globale <c>accounts/accounts.json</c>.</summary>
public sealed class WorkspaceAccountsFile
{
    public List<string> Groups { get; set; } = new();
    public List<WorkspaceAccount> Accounts { get; set; } = new();
}

/// <summary>Richiesta di creazione di un gruppo account globale.</summary>
public sealed class CreateAccountGroupRequest
{
    public string GroupId { get; set; } = string.Empty;
}
