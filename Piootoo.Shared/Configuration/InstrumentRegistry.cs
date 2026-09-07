using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Shared.Configuration;

/// <summary>
/// Sorgente unica delle specifiche strumento.
///
/// <para><b>Due conoscenze diverse, due sorgenti.</b> Questo registro tiene la parte
/// <i>economica</i> — quanto vale un punto, in che valuta, con che tick — che è una proprietà del
/// contratto e cambia solo quando l'exchange cambia le specifiche. La parte di <i>calendario</i>
/// — in che orologio tornano gli orari, da che ora comincia la sessione, in che giorni ce n'è una
/// — sta invece in <see cref="MarketCalendarRegistry"/>, cioè in un file dati, perché la stessa
/// conoscenza serviva anche al Python e ai tre cBot e in C# non potevano vederla. Vedi
/// <c>docs/domini/layer-barre-e-calendario.md</c>.</para>
///
/// <para><b>Nessun fallback silenzioso.</b> La versione precedente di questa conoscenza viveva
/// dentro <c>PiootooTradingService.ContractPointValues</c> e restituiva <c>1</c> per ogni simbolo
/// sconosciuto. Su HG (25.000 $/punto) uno stop di $1.000 diventava 1.000 punti anziché 0,04:
/// mai colpito, per l'intero backtest, senza un solo messaggio. Qui un simbolo sconosciuto è un
/// errore esplicito, coerente con l'invariante già adottata per i datafeed mancanti.</para>
///
/// <para><b>Come estendere.</b> Aggiungi la voce qui sotto solo dopo aver verificato la
/// dimensione del contratto e l'unità di quotazione sul sito dell'exchange, <b>e</b> la voce
/// corrispondente in <c>MarketData/market-calendars.json</c>. Un valore sbagliato qui è invisibile
/// a valle: falsa stop, target, P&amp;L ed equity insieme, mantenendo numeri plausibili. I simboli
/// che non sono ancora stati verificati sono deliberatamente assenti.</para>
/// </summary>
public static class InstrumentRegistry
{
    /// <summary>
    /// La sola parte economica di uno strumento. Il calendario arriva da
    /// <see cref="MarketCalendarRegistry"/> e viene unito in <see cref="BuildSpecs"/>.
    /// </summary>
    private sealed record Contract(
        string Symbol,
        decimal PointValue,
        string Currency,
        decimal TickSize,
        string Description);

    private static readonly Contract[] Contracts =
    [
        // --- Indici USA -------------------------------------------------------------
        new("ES", 50m, "USD", 0.25m, "E-mini S&P 500"),
        new("MES", 5m, "USD", 0.25m, "Micro E-mini S&P 500"),
        new("NQ", 20m, "USD", 0.25m, "E-mini Nasdaq-100"),
        new("MNQ", 2m, "USD", 0.25m, "Micro E-mini Nasdaq-100"),
        new("YM", 5m, "USD", 1m, "E-mini Dow"),
        new("MYM", 0.5m, "USD", 1m, "Micro E-mini Dow"),
        new("RTY", 50m, "USD", 0.1m, "E-mini Russell 2000"),
        new("M2K", 5m, "USD", 0.1m, "Micro E-mini Russell 2000"),

        // --- Indici europei ---------------------------------------------------------
        // Attenzione: PointValue in EUR. Il sistema non converte le valute: un portafoglio
        // misto EUR/USD somma grandezze non omogenee finché non esiste un layer FX.
        new("FDAX", 25m, "EUR", 1m, "DAX future"),
        new("FDXM", 5m, "EUR", 1m, "Mini-DAX future"),
        new("FDXS", 1m, "EUR", 1m, "Micro-DAX future"),
        new("FESX", 10m, "EUR", 1m, "Euro Stoxx 50 future"),
        new("FGBL", 1000m, "EUR", 0.01m, "Euro-Bund future"),

        // --- Metalli ----------------------------------------------------------------
        new("GC", 100m, "USD", 0.1m, "Gold, 100 once troy"),
        new("MGC", 10m, "USD", 0.1m, "Micro Gold, 10 once troy"),
        new("SI", 5000m, "USD", 0.005m, "Silver, 5.000 once troy"),
        new("HG", 25000m, "USD", 0.0005m, "Copper, 25.000 libbre ($/lb)"),
        new("PL", 50m, "USD", 0.1m, "Platinum, 50 once troy"),
        new("PA", 100m, "USD", 0.05m, "Palladium, 100 once troy"),

        // --- Energia ----------------------------------------------------------------
        new("CL", 1000m, "USD", 0.01m, "Crude Oil WTI, 1.000 barili"),
        new("MCL", 100m, "USD", 0.01m, "Micro Crude Oil, 100 barili"),
        new("NG", 10000m, "USD", 0.001m, "Natural Gas, 10.000 MMBtu"),
        new("RB", 42000m, "USD", 0.0001m, "RBOB Gasoline, 42.000 galloni ($/gal)"),

        // --- Cripto -----------------------------------------------------------------
        // CME BTC: contratto 5 bitcoin, $5 per punto di indice; tick 5,00 punti = $25.
        new("BTC", 5m, "USD", 5m, "Bitcoin future (CME BTC), 5 BTC"),

        // --- Softs ICE US ----------------------------------------------------------
        // Quotazione in centesimi per libbra per KC, CT e SB: il PointValue e' quindi il
        // dollaro per centesimo, non per punto indice. La verifica e' doppia — dimensione del
        // contratto dell'exchange e conversioni del dossier del paniere, che concordano:
        // su KC $250 di stop valgono 0,67 "punti" (250/375), su CT $3.000 valgono 6,00
        // (3000/500), su SB $2.250 valgono 2,01 (2250/1120).
        new("KC", 375m, "USD", 0.05m, "Coffee C, 37.500 libbre (centesimi/lb, $375 per centesimo)"),
        new("CT", 500m, "USD", 0.01m, "Cotton No.2, 50.000 libbre (centesimi/lb, $500 per centesimo)"),
        new("SB", 1120m, "USD", 0.01m, "Sugar No.11, 112.000 libbre (centesimi/lb, $1.120 per centesimo)"),
        // Cocoa e' quotato in dollari per tonnellata su un contratto da 10 tonnellate.
        new("CC", 10m, "USD", 1m, "Cocoa, 10 tonnellate ($/tonnellata)"),

        // --- Valute CME ------------------------------------------------------------
        // CME 6B: contratto £62.500, quotato USD per GBP; tick 0,0001 = $6,25.
        new("BP", 62500m, "USD", 0.0001m, "British Pound GBP/USD (CME 6B), £62.500"),
        // CME 6E: contratto €125.000, quotato USD per EUR; tick 0,00005 = $6,25.
        new("EC", 125000m, "USD", 0.00005m, "Euro FX EUR/USD (CME 6E), €125.000")
    ];

    private static readonly Lazy<Dictionary<string, InstrumentSpec>> LazySpecs = new(BuildSpecs);

    private static Dictionary<string, InstrumentSpec> Specs => LazySpecs.Value;

    /// <summary>
    /// Unisce la tabella economica con il calendario di mercato. Un contratto senza calendario è un
    /// errore esplicito: dedurre il fuso dal simbolo è precisamente ciò che il file esiste per
    /// impedire, e un ancoraggio scelto a caso non dà barre sbagliate, dà barre diverse.
    /// </summary>
    private static Dictionary<string, InstrumentSpec> BuildSpecs()
    {
        var calendar = MarketCalendarRegistry.Current;
        var specs = new Dictionary<string, InstrumentSpec>(StringComparer.OrdinalIgnoreCase);

        foreach (var contract in Contracts)
        {
            if (!calendar.TryGet(contract.Symbol, out var market))
            {
                throw new InstrumentSpecNotFoundException(
                    $"Il simbolo '{contract.Symbol}' è nel registro dei contratti ma non nel " +
                    $"calendario di mercato ({calendar.Origin}, spec {calendar.SpecVersion}). " +
                    "Aggiungilo in MarketData/market-calendars.json: senza calendario non si sa in " +
                    "che orologio leggere i suoi orari di sessione né da che ora la ricerca " +
                    "segmenta le sue sessioni.");
            }

            specs[contract.Symbol] = new InstrumentSpec
            {
                Symbol = contract.Symbol,
                PointValue = contract.PointValue,
                Currency = contract.Currency,
                TickSize = contract.TickSize,
                Description = contract.Description,
                SessionTimeZone = market.ExchangeTimeZone,
                ResearchSessionStartHour = market.SessionStartHour,
                SessionDays = market.SessionDays
            };
        }

        return specs;
    }

    /// <summary>
    /// Simboli citati dal catalogo strategie ma non ancora verificati. Sono elencati a parte per
    /// dare un messaggio d'errore utile invece di un generico "sconosciuto": l'unità di
    /// quotazione di valute e agricoli (dollari o centesimi) è la fonte di errore più frequente e
    /// va confermata sulla specifica dell'exchange prima di inserirli sopra.
    /// </summary>
    private static readonly Dictionary<string, string> KnownButUnverified =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["JY"] = "Japanese Yen (6J) — quotazione in unità di 0,000001, particolarmente insidiosa",
            ["AD"] = "Australian Dollar (6A)",
            ["CD"] = "Canadian Dollar (6C)",
            ["C"] = "Corn — verificare se il feed quota in centesimi o dollari per bushel",
            ["LC"] = "Live Cattle — quotazione in centesimi per libbra",
            ["FC"] = "Feeder Cattle — quotazione in centesimi per libbra",
            ["LH"] = "Lean Hogs — quotazione in centesimi per libbra",
            ["ETHUSDT"] = "Cripto: definire la dimensione del lotto del broker",
            ["HO"] = "Heating Oil — ritirato dal paniere il 07/09/2026 insieme alle sue otto PTS",
            ["HK"] = "Hang Seng — ritirato dal paniere il 07/09/2026 insieme alle sue cinque PTS",
        };

    /// <summary>Normalizza un simbolo alla chiave canonica (senza '@', maiuscolo).</summary>
    public static string Normalize(string symbol) =>
        symbol.Trim().TrimStart('@').ToUpperInvariant();

    public static bool TryGet(string symbol, out InstrumentSpec spec) =>
        Specs.TryGetValue(Normalize(symbol), out spec!);

    /// <summary>
    /// Spec dello strumento. Lancia se il simbolo non è verificato: è voluto. Meglio un backtest
    /// che si ferma di un backtest che produce numeri plausibili e sbagliati.
    /// </summary>
    public static InstrumentSpec Get(string symbol)
    {
        var key = Normalize(symbol);
        if (Specs.TryGetValue(key, out var spec))
            return spec;

        var hint = KnownButUnverified.TryGetValue(key, out var note)
            ? $" Nota: {note}."
            : string.Empty;

        throw new InstrumentSpecNotFoundException(
            $"Nessuna specifica per il simbolo '{symbol}' (chiave '{key}').{hint} " +
            $"Aggiungila in {nameof(InstrumentRegistry)} dopo aver verificato dimensione del " +
            "contratto e unità di quotazione sull'exchange, e il suo calendario in " +
            "MarketData/market-calendars.json: un PointValue sbagliato falsa stop, target e P&L " +
            "senza produrre alcun errore visibile.");
    }

    /// <summary>Denaro per punto, per una unità di quantità.</summary>
    public static decimal PointValue(string symbol) => Get(symbol).PointValue;

    /// <summary>Dimensione del tick dello strumento. Serve ai motori che devono decidere se un
    /// livello e' distinguibile da un altro: sotto il tick il confronto e' un pareggio.</summary>
    public static decimal TickSize(string symbol) => Get(symbol).TickSize;

    /// <summary>
    /// Orologio in cui leggere gli orari di sessione del simbolo. Va creato <b>uno per strategia</b>
    /// e non condiviso: l'istanza tiene in cache l'offset dell'ultimo giorno visto e non è
    /// thread-safe, come il motore che la ospita.
    /// </summary>
    public static SessionClock CreateSessionClock(string symbol) =>
        new(Get(symbol).SessionTimeZone);

    /// <summary>Simboli verificati, per diagnostica e test di copertura del catalogo.</summary>
    public static IReadOnlyCollection<string> RegisteredSymbols => Specs.Keys;
}

/// <summary>Simbolo privo di specifica verificata.</summary>
public sealed class InstrumentSpecNotFoundException(string message) : Exception(message);
