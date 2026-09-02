using Piootoo.Shared.Models.Trading;

namespace Piootoo.Shared.Configuration;

/// <summary>
/// Sorgente unica delle specifiche strumento.
///
/// <para><b>Nessun fallback silenzioso.</b> La versione precedente di questa conoscenza viveva
/// dentro <c>PiootooTradingService.ContractPointValues</c> e restituiva <c>1</c> per ogni simbolo
/// sconosciuto. Su HG (25.000 $/punto) uno stop di $1.000 diventava 1.000 punti anziché 0,04:
/// mai colpito, per l'intero backtest, senza un solo messaggio. Qui un simbolo sconosciuto è un
/// errore esplicito, coerente con l'invariante già adottata per i datafeed mancanti.</para>
///
/// <para><b>Come estendere.</b> Aggiungi la voce qui sotto solo dopo aver verificato la
/// dimensione del contratto e l'unità di quotazione sul sito dell'exchange. Un valore sbagliato
/// qui è invisibile a valle: falsa stop, target, P&amp;L ed equity insieme, mantenendo numeri
/// plausibili. I simboli che non sono ancora stati verificati sono deliberatamente assenti.</para>
/// </summary>
public static class InstrumentRegistry
{
    // Fusi in cui tornano gli orari di sessione dichiarati dalle sorgenti. La scelta non è "dove ha
    // sede la borsa" ma "in quale orologio la coppia start/end coincide con la sessione reale", ed è
    // per questo che metalli ed energia stanno su New York pur essendo prodotti CME Group: le loro
    // sorgenti scrivono 1800->1700, che è la sessione in ora di New York, mentre quelle degli indici
    // scrivono 1700->1600, la stessa sessione in ora di Chicago.
    private const string CmeChicago = "America/Chicago";    // sorgenti CME: 1700 -> 1600
    private const string NyComexNymex = "America/New_York"; // sorgenti COMEX/NYMEX: 1800 -> 1700
    private const string EurexFrankfurt = "Europe/Berlin";  // sorgenti Eurex: 0800 -> 2200
    private const string IceNewYork = "America/New_York";   // softs ICE US: 0400 -> 1400
    private const string HkexHongKong = "Asia/Hong_Kong";   // HKEX: 0915 -> 0300

    private static readonly Dictionary<string, InstrumentSpec> Specs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // --- Indici USA -------------------------------------------------------------
            ["ES"] = new() { Symbol = "ES", PointValue = 50m, Currency = "USD", TickSize = 0.25m, SessionTimeZone = CmeChicago, Description = "E-mini S&P 500" },
            ["MES"] = new() { Symbol = "MES", PointValue = 5m, Currency = "USD", TickSize = 0.25m, SessionTimeZone = CmeChicago, Description = "Micro E-mini S&P 500" },
            ["NQ"] = new() { Symbol = "NQ", PointValue = 20m, Currency = "USD", TickSize = 0.25m, SessionTimeZone = CmeChicago, Description = "E-mini Nasdaq-100" },
            ["MNQ"] = new() { Symbol = "MNQ", PointValue = 2m, Currency = "USD", TickSize = 0.25m, SessionTimeZone = CmeChicago, Description = "Micro E-mini Nasdaq-100" },
            ["YM"] = new() { Symbol = "YM", PointValue = 5m, Currency = "USD", TickSize = 1m, SessionTimeZone = CmeChicago, Description = "E-mini Dow" },
            ["MYM"] = new() { Symbol = "MYM", PointValue = 0.5m, Currency = "USD", TickSize = 1m, SessionTimeZone = CmeChicago, Description = "Micro E-mini Dow" },
            ["RTY"] = new() { Symbol = "RTY", PointValue = 50m, Currency = "USD", TickSize = 0.1m, SessionTimeZone = CmeChicago, Description = "E-mini Russell 2000" },
            ["M2K"] = new() { Symbol = "M2K", PointValue = 5m, Currency = "USD", TickSize = 0.1m, SessionTimeZone = CmeChicago, Description = "Micro E-mini Russell 2000" },

            // --- Indici europei ---------------------------------------------------------
            // Attenzione: PointValue in EUR. Il sistema non converte le valute: un portafoglio
            // misto EUR/USD somma grandezze non omogenee finché non esiste un layer FX.
            ["FDAX"] = new() { Symbol = "FDAX", PointValue = 25m, Currency = "EUR", TickSize = 1m, SessionTimeZone = EurexFrankfurt, Description = "DAX future" },
            ["FDXM"] = new() { Symbol = "FDXM", PointValue = 5m, Currency = "EUR", TickSize = 1m, SessionTimeZone = EurexFrankfurt, Description = "Mini-DAX future" },
            ["FDXS"] = new() { Symbol = "FDXS", PointValue = 1m, Currency = "EUR", TickSize = 1m, SessionTimeZone = EurexFrankfurt, Description = "Micro-DAX future" },
            ["FESX"] = new() { Symbol = "FESX", PointValue = 10m, Currency = "EUR", TickSize = 1m, SessionTimeZone = EurexFrankfurt, Description = "Euro Stoxx 50 future" },
            ["FGBL"] = new() { Symbol = "FGBL", PointValue = 1000m, Currency = "EUR", TickSize = 0.01m, SessionTimeZone = EurexFrankfurt, Description = "Euro-Bund future" },

            // --- Metalli ----------------------------------------------------------------
            ["GC"] = new() { Symbol = "GC", PointValue = 100m, Currency = "USD", TickSize = 0.1m, SessionTimeZone = NyComexNymex, Description = "Gold, 100 once troy" },
            ["MGC"] = new() { Symbol = "MGC", PointValue = 10m, Currency = "USD", TickSize = 0.1m, SessionTimeZone = NyComexNymex, Description = "Micro Gold, 10 once troy" },
            ["SI"] = new() { Symbol = "SI", PointValue = 5000m, Currency = "USD", TickSize = 0.005m, SessionTimeZone = NyComexNymex, Description = "Silver, 5.000 once troy" },
            ["HG"] = new() { Symbol = "HG", PointValue = 25000m, Currency = "USD", TickSize = 0.0005m, SessionTimeZone = NyComexNymex, Description = "Copper, 25.000 libbre ($/lb)" },
            ["PL"] = new() { Symbol = "PL", PointValue = 50m, Currency = "USD", TickSize = 0.1m, SessionTimeZone = NyComexNymex, Description = "Platinum, 50 once troy" },
            ["PA"] = new() { Symbol = "PA", PointValue = 100m, Currency = "USD", TickSize = 0.05m, SessionTimeZone = NyComexNymex, Description = "Palladium, 100 once troy" },

            // --- Energia ----------------------------------------------------------------
            ["CL"] = new() { Symbol = "CL", PointValue = 1000m, Currency = "USD", TickSize = 0.01m, SessionTimeZone = NyComexNymex, Description = "Crude Oil WTI, 1.000 barili" },
            ["MCL"] = new() { Symbol = "MCL", PointValue = 100m, Currency = "USD", TickSize = 0.01m, SessionTimeZone = NyComexNymex, Description = "Micro Crude Oil, 100 barili" },
            ["NG"] = new() { Symbol = "NG", PointValue = 10000m, Currency = "USD", TickSize = 0.001m, SessionTimeZone = NyComexNymex, Description = "Natural Gas, 10.000 MMBtu" },
            ["RB"] = new() { Symbol = "RB", PointValue = 42000m, Currency = "USD", TickSize = 0.0001m, SessionTimeZone = NyComexNymex, Description = "RBOB Gasoline, 42.000 galloni ($/gal)" },
            ["HO"] = new() { Symbol = "HO", PointValue = 42000m, Currency = "USD", TickSize = 0.0001m, SessionTimeZone = NyComexNymex, Description = "Heating Oil, 42.000 galloni ($/gal)" },

            // --- Cripto -----------------------------------------------------------------
            // CME BTC: contratto 5 bitcoin, $5 per punto di indice; tick 5,00 punti = $25.
            ["BTC"] = new() { Symbol = "BTC", PointValue = 5m, Currency = "USD", TickSize = 5m, SessionTimeZone = CmeChicago, Description = "Bitcoin future (CME BTC), 5 BTC" },

            // --- Softs ICE US ----------------------------------------------------------
            // Quotazione in centesimi per libbra per KC, CT e SB: il PointValue e' quindi il
            // dollaro per centesimo, non per punto indice. La verifica e' doppia — dimensione del
            // contratto dell'exchange e conversioni del dossier del paniere, che concordano:
            // su KC $250 di stop valgono 0,67 "punti" (250/375), su CT $3.000 valgono 6,00
            // (3000/500), su SB $2.250 valgono 2,01 (2250/1120).
            ["KC"] = new() { Symbol = "KC", PointValue = 375m, Currency = "USD", TickSize = 0.05m, SessionTimeZone = IceNewYork, Description = "Coffee C, 37.500 libbre (centesimi/lb, $375 per centesimo)" },
            ["CT"] = new() { Symbol = "CT", PointValue = 500m, Currency = "USD", TickSize = 0.01m, SessionTimeZone = IceNewYork, Description = "Cotton No.2, 50.000 libbre (centesimi/lb, $500 per centesimo)" },
            ["SB"] = new() { Symbol = "SB", PointValue = 1120m, Currency = "USD", TickSize = 0.01m, SessionTimeZone = IceNewYork, Description = "Sugar No.11, 112.000 libbre (centesimi/lb, $1.120 per centesimo)" },
            // Cocoa e' quotato in dollari per tonnellata su un contratto da 10 tonnellate.
            ["CC"] = new() { Symbol = "CC", PointValue = 10m, Currency = "USD", TickSize = 1m, SessionTimeZone = IceNewYork, Description = "Cocoa, 10 tonnellate ($/tonnellata)" },

            // --- Indici asiatici -------------------------------------------------------
            // Hang Seng: il contratto vale HKD 50 per punto indice. Il valore qui e' in USD al
            // cambio fisso della banda HKD (7,8), cioe' i $6,41 con cui il dossier del paniere
            // converte i propri stop: $3.000 = 468,02 punti e $250 = 39,00. La tabella §2.4 del
            // dossier arrotonda a "$6", che non torna con le sue stesse conversioni.
            // ⚠ E' l'unico strumento del registro il cui PointValue dipende da un cambio: se un
            // giorno l'HKD uscisse dalla banda, stop e target di queste strategie andrebbero
            // rimisurati, e la strada corretta sarebbe dichiararlo in HKD con un layer FX.
            ["HK"] = new() { Symbol = "HK", PointValue = 6.41m, Currency = "USD", TickSize = 1m, SessionTimeZone = HkexHongKong, Description = "Hang Seng future, HKD 50 per punto convertiti a 7,8 HKD/USD" },

            // --- Valute CME ------------------------------------------------------------
            // CME 6B: contratto £62.500, quotato USD per GBP; tick 0,0001 = $6,25.
            ["BP"] = new() { Symbol = "BP", PointValue = 62500m, Currency = "USD", TickSize = 0.0001m, SessionTimeZone = CmeChicago, Description = "British Pound GBP/USD (CME 6B), £62.500" },
            // CME 6E: contratto €125.000, quotato USD per EUR; tick 0,00005 = $6,25.
            ["EC"] = new() { Symbol = "EC", PointValue = 125000m, Currency = "USD", TickSize = 0.00005m, SessionTimeZone = CmeChicago, Description = "Euro FX EUR/USD (CME 6E), €125.000" },
        };

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
            "contratto e unità di quotazione sull'exchange: un PointValue sbagliato falsa stop, " +
            "target e P&L senza produrre alcun errore visibile.");
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
