namespace Piootoo.Shared.MarketData;

/// <summary>
/// Il calendario di mercato di un simbolo: in che orologi vanno letti i suoi orari, da che ora
/// comincia la sua sessione, in che giorni ne ha una, e — quando saranno noti — le fasi, i festivi
/// e le chiusure anticipate.
///
/// <para><b>Perché è un dato e non codice.</b> La stessa conoscenza viveva in sette posti: la
/// tabella C# di <c>InstrumentRegistry</c>, <c>SESSION_START_HOUR</c> in
/// <c>aggregate_flat_feed.py</c>, e una copia per ciascuno dei tre cBot, più la ricostruzione
/// implicita dentro <c>EasyLib</c> e <c>EasyEngineBase</c>. Nessuna sbagliata da sola, tutte libere
/// di divergere — e quando divergono non danno un errore, danno barre <i>diverse</i>. Vedi
/// <c>docs/domini/layer-barre-e-calendario.md</c> §1.</para>
/// </summary>
public sealed record SymbolCalendar
{
    /// <summary>Simbolo canonico, senza '@' e in maiuscolo.</summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Fuso IANA in cui tornano gli orari di sessione dichiarati dalle sorgenti EasyLanguage.
    ///
    /// <para><b>Non è "dove ha sede la borsa".</b> Metalli ed energia stanno su New York pur essendo
    /// prodotti CME Group, perché le loro sorgenti scrivono <c>1800</c>→<c>1700</c>, che è la
    /// sessione in ora di New York; gli indici scrivono <c>1700</c>→<c>1600</c>, la stessa sessione
    /// in ora di Chicago. Il campo conserva il fuso che rende vera la coppia così com'è scritta,
    /// invece di riscrivere i numeri di cinquantacinque sorgenti a mano.</para>
    /// </summary>
    public required string ExchangeTimeZone { get; init; }

    /// <summary>
    /// Fuso IANA in cui i run di ricerca hanno scritto le finestre operative. È CET <b>per ogni
    /// simbolo</b>, e per questo non si deduce da <see cref="ExchangeTimeZone"/>: sono due orologi
    /// diversi e il sistema lo sa già (<c>ZonedWindow.ResearchTimeZone</c>).
    /// </summary>
    public required string ResearchTimeZone { get; init; }

    /// <summary>
    /// Ora locale, nell'orologio della ricerca, in cui comincia una sessione di questo strumento.
    /// È la colonna "Inizio sessione" della §2.4 del dossier del paniere: <b>01:00 per FDAX, CC,
    /// CT, KC e SB</b>, 00:00 per tutti gli altri.
    ///
    /// <para>È l'ancoraggio dei bucket oltre l'ora, oltre che il taglio di <c>d0..d5</c>. Conta solo
    /// da 4h in su: sotto l'ora i due ancoraggi coincidono comunque, perché lo scarto di un fuso è
    /// un numero intero di ore. Un ancoraggio sbagliato non dà barre sbagliate: dà barre diverse, e
    /// nessun controllo a valle se ne accorge.</para>
    /// </summary>
    public required int SessionStartHour { get; init; }

    /// <summary>
    /// I giorni in cui questo strumento ha una sessione, letti sul giorno di calendario
    /// dell'orologio della ricerca. <c>null</c> = <b>non dichiarato</b>, e chi legge non deve
    /// inventarlo: è la differenza fra "non lo sappiamo" e "non ne ha".
    ///
    /// <para>Serve perché un feed CFD quota anche quando il future è chiuso. Dove la ricerca non ha
    /// quella sessione, il feed ne crea una mai esistita: non genera trade, ma spezza la sessione e
    /// con l'uscita di fine sessione chiude posizioni ancora valide. Il dossier lo misura sul DAX,
    /// <b>11% del P&amp;L</b>.</para>
    ///
    /// <para><b>Non è "ignorare la domenica".</b> Sui CME la sessione domenicale è <i>vera</i> nelle
    /// settimane in cui l'ora legale europea e americana sono sfasate. Riscontro sul feed del
    /// vendor: <c>@NQ_240</c> ha 68 barre domenicali in diciannove anni, <c>@FDAX_240</c> zero.</para>
    /// </summary>
    public IReadOnlySet<DayOfWeek>? SessionDays { get; init; }

    /// <summary>
    /// Le fasi della giornata, ciascuna con il proprio ancoraggio. <b>Oggi vuote per tutti</b>: il
    /// formato le prevede perché aggiungerle dopo vorrebbe dire rileggere ogni consumatore, ma
    /// popolarle senza una fonte verificata introdurrebbe numeri inventati.
    ///
    /// <para>Le fasi <b>etichettano</b>; la finestra di <see cref="TradingWindows"/> <b>decide</b>.
    /// Sono due mestieri diversi e per questo non sono lo stesso campo: un giorno <c>premarket</c> o
    /// <c>containsSettlement</c> sarà un'informazione che la strategia legge, non un motivo per cui
    /// una barra sparisce.</para>
    /// </summary>
    public IReadOnlyList<MarketPhase> Phases { get; init; } = [];

    /// <summary>
    /// Quando questo strumento negozia davvero. Vuoto = <b>non dichiarato</b>, e la maschera lascia
    /// passare tutto invece di spegnere lo strumento.
    ///
    /// <para>Più di una voce solo dove una borsa cambia orario nel corso dell'anno. L'ora legale
    /// <b>non</b> si dichiara qui: la risolve <see cref="ExchangeTimeZone"/>. Vedi
    /// <see cref="TradingWindow"/>.</para>
    /// </summary>
    public IReadOnlyList<TradingWindow> TradingWindows { get; init; } = [];

    /// <summary>
    /// Giorni di chiusura totale della borsa. <b>Oggi vuoti per tutti</b>: non esiste ancora una
    /// fonte verificata (il <c>eurex_2026.csv</c> della spec non è sul disco). Un elenco inventato
    /// qui eliminerebbe sessioni vere.
    /// </summary>
    public IReadOnlyList<DateOnly> Holidays { get; init; } = [];

    /// <summary>
    /// Chiusure anticipate, come <c>"MM-dd"</c> → orario locale <c>"HH:mm"</c>. Oggi vuote.
    /// </summary>
    public IReadOnlyDictionary<string, string> EarlyClose { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Ragione della scelta, quando non è ovvia. Facoltativa, solo documentazione.</summary>
    public string? Note { get; init; }

    /// <summary>Vero quando il calendario dichiara di sapere in che giorni c'è sessione.</summary>
    public bool DeclaresSessionDays => SessionDays is not null;

    /// <summary>
    /// Vero se in <paramref name="researchDay"/> — un giorno di calendario letto nell'orologio
    /// della ricerca — questo strumento ha una sessione. Quando i giorni non sono dichiarati la
    /// risposta è <c>null</c>: non si inventa.
    /// </summary>
    public bool? HasSessionOn(DayOfWeek researchDay) => SessionDays?.Contains(researchDay);
}

/// <summary>
/// Una fase della giornata di negoziazione, con il proprio riferimento orario.
///
/// <para><b>Perché ogni fase dichiara il proprio ancoraggio.</b> È la prima trappola segnalata
/// dalla spec del refactor: sul FDAX l'apertura notturna è agganciata a Singapore ed è fissa alle
/// 00:15 UTC, mentre chiusura e sessione cash seguono l'orologio di Berlino. Modellare tutto in
/// locale sbaglia l'apertura per metà anno; modellare tutto in UTC sbaglia tutto il resto.</para>
/// </summary>
/// <param name="Name">Nome della fase (<c>asian</c>, <c>cash</c>, …).</param>
/// <param name="StartHhmm">Orario di inizio, <c>HHMM</c>, nell'orologio di <paramref name="StartAnchor"/>.</param>
/// <param name="StartAnchor">In che orologio è scritto <paramref name="StartHhmm"/>.</param>
/// <param name="EndHhmm">Orario di fine, <c>HHMM</c>, nell'orologio di <paramref name="EndAnchor"/>.</param>
/// <param name="EndAnchor">In che orologio è scritto <paramref name="EndHhmm"/>.</param>
public sealed record MarketPhase(
    string Name,
    int StartHhmm,
    PhaseAnchor StartAnchor,
    int EndHhmm,
    PhaseAnchor EndAnchor);

/// <summary>L'orologio in cui è scritto un orario di fase.</summary>
public enum PhaseAnchor
{
    /// <summary>Ora locale della borsa (<see cref="SymbolCalendar.ExchangeTimeZone"/>).</summary>
    Local = 0,

    /// <summary>UTC, cioè un orario che non si muove con l'ora legale locale.</summary>
    Utc = 1
}

/// <summary>
/// L'insieme dei calendari, con la versione della spec che li ha prodotti.
///
/// <para><b>La versione non è decorativa.</b> È ciò che un aggregato derivato dichiara in testa per
/// dire su quale griglia è nato: due file identici nella forma ma prodotti con ancoraggi diversi
/// non sono confrontabili e senza questo non si distinguerebbero. Vedi
/// <c>docs/domini/layer-barre-e-calendario.md</c> §6.</para>
/// </summary>
public sealed class MarketCalendar
{
    private readonly Dictionary<string, SymbolCalendar> _symbols;

    public MarketCalendar(string specVersion, string origin, IEnumerable<SymbolCalendar> symbols)
    {
        if (string.IsNullOrWhiteSpace(specVersion))
            throw new ArgumentException("La spec del calendario deve dichiarare una versione.", nameof(specVersion));

        SpecVersion = specVersion;
        Origin = origin;
        _symbols = new Dictionary<string, SymbolCalendar>(StringComparer.OrdinalIgnoreCase);
        foreach (var symbol in symbols)
        {
            if (!_symbols.TryAdd(symbol.Symbol, symbol))
            {
                throw new MarketCalendarException(
                    $"Il calendario '{origin}' dichiara due volte il simbolo '{symbol.Symbol}'.");
            }
        }
    }

    /// <summary>Versione della spec, es. <c>2026-09-07.1</c>.</summary>
    public string SpecVersion { get; }

    /// <summary>Da dove viene questo calendario, per i messaggi di errore.</summary>
    public string Origin { get; }

    /// <summary>Simboli dichiarati, per diagnostica e test di copertura.</summary>
    public IReadOnlyCollection<string> Symbols => _symbols.Keys;

    /// <summary>Normalizza un simbolo alla chiave canonica (senza '@', maiuscolo).</summary>
    public static string Normalize(string symbol) =>
        symbol.Trim().TrimStart('@').ToUpperInvariant();

    public bool TryGet(string symbol, out SymbolCalendar calendar) =>
        _symbols.TryGetValue(Normalize(symbol), out calendar!);

    /// <summary>
    /// Calendario del simbolo. Lancia se non è dichiarato: è voluto, ed è la stessa regola di
    /// <c>InstrumentRegistry.Get</c>. Un fuso o un ancoraggio scelti a caso spostano il confine di
    /// sessione di ore senza produrre alcun messaggio.
    /// </summary>
    public SymbolCalendar Get(string symbol)
    {
        if (TryGet(symbol, out var calendar))
            return calendar;

        throw new MarketCalendarException(
            $"Nessun calendario di mercato per il simbolo '{symbol}' (chiave " +
            $"'{Normalize(symbol)}') nella spec {SpecVersion} di '{Origin}'. Aggiungilo dopo aver " +
            "verificato in che orologio tornano i suoi orari di sessione e da che ora la ricerca " +
            "segmenta le sue sessioni: un ancoraggio sbagliato non produce barre sbagliate, " +
            "produce barre diverse, e nessun controllo a valle se ne accorge.");
    }
}

/// <summary>Calendario mancante, malformato o incoerente.</summary>
public sealed class MarketCalendarException(string message, Exception? inner = null)
    : Exception(message, inner);
