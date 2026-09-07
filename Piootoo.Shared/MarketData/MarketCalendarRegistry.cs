using System.Reflection;
using System.Text.Json;

namespace Piootoo.Shared.MarketData;

/// <summary>
/// Da dove viene il calendario di mercato: una risorsa incorporata nell'assembly, con override
/// facoltativo su disco.
///
/// <para><b>Perché incorporato e non solo su disco.</b> Il calendario serve a test, server e client,
/// e alcuni di questi non hanno un <c>piootoo-repository/</c> sotto mano. Un percorso obbligatorio
/// avrebbe reso il caricamento un punto di rottura in tre ambienti diversi per una tabella che
/// cambia due volte l'anno. La risorsa incorporata è quindi la sorgente, e il file su disco serve a
/// correggerla senza ricompilare.</para>
///
/// <para><b>Perché l'override deve dichiarare la stessa versione.</b> Un file scritto contro una
/// forma diversa da quella che questo codice sa leggere verrebbe caricato lo stesso, con i campi
/// nuovi ignorati in silenzio: esattamente il tipo di divergenza che spostare il calendario in un
/// dato serve a eliminare. Versione diversa = errore all'avvio.</para>
///
/// <para><b>Perché <see cref="Initialize"/> rifiuta un calendario tardivo.</b> Cambiare il
/// calendario dopo che qualcuno l'ha già letto significa avere due griglie nello stesso processo, e
/// nessuna delle due sa dell'altra. Meglio non partire.</para>
/// </summary>
public static class MarketCalendarRegistry
{
    /// <summary>Nome del file di override, sotto <c>piootoo-repository/settings/</c>.</summary>
    public const string OverrideFileName = "market-calendars.json";

    private const string EmbeddedResourceName = "Piootoo.Shared.MarketData.market-calendars.json";

    private static readonly object Gate = new();
    private static MarketCalendar? _current;
    private static bool _observed;

    /// <summary>
    /// Il calendario in vigore. Alla prima lettura risolve la risorsa incorporata, se
    /// <see cref="Initialize"/> non ne ha già fornito un altro.
    /// </summary>
    public static MarketCalendar Current
    {
        get
        {
            lock (Gate)
            {
                _observed = true;
                return _current ??= LoadEmbedded();
            }
        }
    }

    /// <summary>
    /// Il calendario incorporato nell'assembly, riletto da capo. Serve ai test che devono
    /// confrontare l'override con l'originale senza toccare <see cref="Current"/>.
    /// </summary>
    public static MarketCalendar LoadEmbedded()
    {
        var assembly = typeof(MarketCalendarRegistry).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new MarketCalendarException(
                $"La risorsa incorporata '{EmbeddedResourceName}' non è nell'assembly " +
                $"{assembly.GetName().Name}. Verificare che il .csproj la dichiari come " +
                "<EmbeddedResource>: senza calendario il sistema non sa dove cadono i confini di " +
                "sessione di alcun simbolo.");

        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd(), $"risorsa incorporata di {assembly.GetName().Name}");
    }

    /// <summary>
    /// Impone un calendario diverso da quello incorporato. Va chiamato all'avvio, <b>prima</b> che
    /// qualunque codice abbia letto <see cref="Current"/>.
    /// </summary>
    /// <exception cref="MarketCalendarException">Il calendario è già stato letto.</exception>
    public static void Initialize(MarketCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        lock (Gate)
        {
            if (_observed)
            {
                throw new MarketCalendarException(
                    $"Il calendario di mercato è già stato letto ('{_current?.Origin}'): non può " +
                    $"essere sostituito con '{calendar.Origin}' a processo avviato. Due griglie " +
                    "nello stesso processo darebbero barre diverse a chi ha letto prima e a chi " +
                    "legge dopo, senza che nulla lo segnali. Chiamare Initialize all'avvio.");
            }

            _current = calendar;
        }
    }

    /// <summary>
    /// Applica l'override <c>{settingsPath}/market-calendars.json</c> se esiste — cioè la stessa
    /// cartella <c>piootoo-repository/settings/</c> in cui vivono già le altre tabelle di
    /// configurazione. Restituisce <c>true</c> se è stato applicato, <c>false</c> se il file non
    /// c'è: in quel caso resta in vigore il calendario incorporato, che è il caso normale.
    /// </summary>
    /// <exception cref="MarketCalendarException">
    /// Il file esiste ma è malformato, oppure dichiara una <c>specVersion</c> diversa da quella
    /// incorporata.
    /// </exception>
    public static bool InitializeFromSettingsFolder(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            return false;

        var path = Path.Combine(settingsPath, OverrideFileName);
        if (!File.Exists(path))
            return false;

        Initialize(LoadOverrideFile(path));
        return true;
    }

    /// <summary>
    /// Legge e convalida un file di override <b>senza applicarlo</b>. Separato da
    /// <see cref="InitializeFromRepository"/> perché la convalida è la parte che vale la pena
    /// verificare da sola, e farlo attraverso lo stato globale del processo renderebbe il test
    /// dipendente dall'ordine in cui gira.
    /// </summary>
    /// <exception cref="MarketCalendarException">
    /// Il file è malformato, oppure dichiara una <c>specVersion</c> diversa da quella incorporata.
    /// </exception>
    public static MarketCalendar LoadOverrideFile(string path)
    {
        var embedded = LoadEmbedded();
        var over = Parse(File.ReadAllText(path), path);

        if (!string.Equals(over.SpecVersion, embedded.SpecVersion, StringComparison.Ordinal))
        {
            throw new MarketCalendarException(
                $"L'override del calendario '{path}' dichiara la spec {over.SpecVersion}, ma " +
                $"questa build ne sa leggere la {embedded.SpecVersion}. Un file scritto contro " +
                "una forma diversa verrebbe caricato con i campi nuovi ignorati in silenzio: " +
                "allinea il file, oppure aggiorna il calendario incorporato e la sua versione.");
        }

        return over;
    }

    /// <summary>Interpreta il documento JSON di un calendario.</summary>
    public static MarketCalendar Parse(string json, string origin)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
        catch (JsonException error)
        {
            throw new MarketCalendarException($"Il calendario '{origin}' non è JSON valido.", error);
        }

        using (document)
        {
            var root = document.RootElement;

            if (!root.TryGetProperty("specVersion", out var version) ||
                version.ValueKind != JsonValueKind.String)
            {
                throw new MarketCalendarException(
                    $"Il calendario '{origin}' non dichiara 'specVersion'. La versione non è " +
                    "decorativa: è ciò che un aggregato derivato porta in testa per dire su quale " +
                    "griglia è nato.");
            }

            if (!root.TryGetProperty("symbols", out var symbols) ||
                symbols.ValueKind != JsonValueKind.Object)
            {
                throw new MarketCalendarException(
                    $"Il calendario '{origin}' non dichiara l'oggetto 'symbols'.");
            }

            var parsed = new List<SymbolCalendar>();
            foreach (var entry in symbols.EnumerateObject())
                parsed.Add(ParseSymbol(entry.Name, entry.Value, origin));

            return new MarketCalendar(version.GetString()!, origin, parsed);
        }
    }

    private static SymbolCalendar ParseSymbol(string symbol, JsonElement element, string origin)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new MarketCalendarException($"'{symbol}' in '{origin}' non è un oggetto.");

        var exchangeTz = RequiredString(element, "exchangeTz", symbol, origin);
        var researchTz = RequiredString(element, "researchTz", symbol, origin);

        if (!element.TryGetProperty("sessionStartHour", out var startHour) ||
            startHour.ValueKind != JsonValueKind.Number ||
            !startHour.TryGetInt32(out var hour) ||
            hour is < 0 or > 23)
        {
            throw new MarketCalendarException(
                $"'{symbol}' in '{origin}' non dichiara 'sessionStartHour' come ora valida (0-23). " +
                "È l'ancoraggio della sessione e dei bucket oltre l'ora: un valore assente non " +
                "vale zero, perché zero è a sua volta un ancoraggio.");
        }

        return new SymbolCalendar
        {
            Symbol = MarketCalendar.Normalize(symbol),
            ExchangeTimeZone = exchangeTz,
            ResearchTimeZone = researchTz,
            SessionStartHour = hour,
            SessionDays = ParseSessionDays(element, symbol, origin),
            Phases = ParsePhases(element, symbol, origin),
            Holidays = ParseHolidays(element, symbol, origin),
            EarlyClose = ParseEarlyClose(element),
            Note = element.TryGetProperty("note", out var note) && note.ValueKind == JsonValueKind.String
                ? note.GetString()
                : null
        };
    }

    /// <summary>
    /// I giorni di sessione. <b>Assente resta assente</b>: la proprietà è nullable proprio perché
    /// "non dichiarato" e "nessun giorno" sono cose diverse, e trattare il primo come il secondo
    /// spegnerebbe uno strumento invece di lasciarlo passare.
    /// </summary>
    private static IReadOnlySet<DayOfWeek>? ParseSessionDays(
        JsonElement element, string symbol, string origin)
    {
        if (!element.TryGetProperty("sessionDays", out var days))
            return null;

        if (days.ValueKind != JsonValueKind.Array)
            throw new MarketCalendarException($"'sessionDays' di '{symbol}' in '{origin}' non è un array.");

        var parsed = new HashSet<DayOfWeek>();
        foreach (var day in days.EnumerateArray())
        {
            var text = day.GetString();
            if (text is null || !TryParseDay(text, out var value))
            {
                throw new MarketCalendarException(
                    $"'{text}' non è un giorno valido in 'sessionDays' di '{symbol}' ('{origin}'). " +
                    "Attesi: Sun, Mon, Tue, Wed, Thu, Fri, Sat.");
            }

            parsed.Add(value);
        }

        if (parsed.Count == 0)
        {
            throw new MarketCalendarException(
                $"'sessionDays' di '{symbol}' in '{origin}' è un array vuoto. Uno strumento senza " +
                "alcun giorno di sessione non esiste: per dire 'non lo sappiamo' si omette il campo.");
        }

        return parsed;
    }

    private static bool TryParseDay(string text, out DayOfWeek day)
    {
        switch (text.Trim().ToUpperInvariant())
        {
            case "SUN": day = DayOfWeek.Sunday; return true;
            case "MON": day = DayOfWeek.Monday; return true;
            case "TUE": day = DayOfWeek.Tuesday; return true;
            case "WED": day = DayOfWeek.Wednesday; return true;
            case "THU": day = DayOfWeek.Thursday; return true;
            case "FRI": day = DayOfWeek.Friday; return true;
            case "SAT": day = DayOfWeek.Saturday; return true;
            default: day = default; return false;
        }
    }

    private static IReadOnlyList<MarketPhase> ParsePhases(
        JsonElement element, string symbol, string origin)
    {
        if (!element.TryGetProperty("phases", out var phases) ||
            phases.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<MarketPhase>();
        foreach (var phase in phases.EnumerateArray())
        {
            parsed.Add(new MarketPhase(
                RequiredString(phase, "name", symbol, origin),
                ParseHhmm(RequiredString(phase, "start", symbol, origin), symbol, origin),
                ParseAnchor(phase, "startAnchor", symbol, origin),
                ParseHhmm(RequiredString(phase, "end", symbol, origin), symbol, origin),
                ParseAnchor(phase, "endAnchor", symbol, origin)));
        }

        return parsed;
    }

    private static PhaseAnchor ParseAnchor(
        JsonElement element, string property, string symbol, string origin)
    {
        if (!element.TryGetProperty(property, out var anchor) || anchor.ValueKind != JsonValueKind.String)
            return PhaseAnchor.Local;

        return anchor.GetString()!.Trim().ToUpperInvariant() switch
        {
            "UTC" => PhaseAnchor.Utc,
            "LOCAL" => PhaseAnchor.Local,
            var other => throw new MarketCalendarException(
                $"'{other}' non è un ancoraggio valido per '{property}' di '{symbol}' ('{origin}'). " +
                "Attesi: 'local' oppure 'utc'. Ogni fase dichiara il proprio perché non tutte hanno " +
                "lo stesso riferimento: sul FDAX l'apertura notturna è fissa in UTC mentre la " +
                "chiusura segue l'orologio di Berlino.")
        };
    }

    private static int ParseHhmm(string text, string symbol, string origin)
    {
        var parts = text.Split(':');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var hours) &&
            int.TryParse(parts[1], out var minutes) &&
            hours is >= 0 and <= 23 && minutes is >= 0 and <= 59)
        {
            return hours * 100 + minutes;
        }

        throw new MarketCalendarException(
            $"'{text}' non è un orario valido per '{symbol}' ('{origin}'). Atteso 'HH:mm'.");
    }

    private static IReadOnlyList<DateOnly> ParseHolidays(
        JsonElement element, string symbol, string origin)
    {
        if (!element.TryGetProperty("holidays", out var holidays) ||
            holidays.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<DateOnly>();
        foreach (var holiday in holidays.EnumerateArray())
        {
            var text = holiday.GetString();
            if (text is null || !DateOnly.TryParse(text, out var date))
            {
                throw new MarketCalendarException(
                    $"'{text}' non è una data valida in 'holidays' di '{symbol}' ('{origin}'). " +
                    "Atteso 'yyyy-MM-dd'.");
            }

            parsed.Add(date);
        }

        return parsed;
    }

    private static IReadOnlyDictionary<string, string> ParseEarlyClose(JsonElement element)
    {
        var parsed = new Dictionary<string, string>(StringComparer.Ordinal);
        if (element.TryGetProperty("earlyClose", out var early) &&
            early.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in early.EnumerateObject())
                parsed[entry.Name] = entry.Value.GetString() ?? string.Empty;
        }

        return parsed;
    }

    private static string RequiredString(
        JsonElement element, string property, string symbol, string origin)
    {
        if (element.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(value.GetString()))
        {
            return value.GetString()!;
        }

        throw new MarketCalendarException(
            $"'{symbol}' in '{origin}' non dichiara '{property}'.");
    }
}
