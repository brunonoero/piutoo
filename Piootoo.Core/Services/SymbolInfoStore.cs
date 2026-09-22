using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Brokers;

namespace Piootoo.Core.Services;

/// <summary>
/// L'archivio delle specifiche che un broker dichiara sui propri strumenti, sotto
/// <c>piootoo-repository/symbol-info/{BROKER}/{SIMBOLO}.json</c>.
///
/// <para><b>Un file per strumento, con dentro la successione degli scatti.</b> Gli strumenti si
/// rilevano uno per volta e cambiano uno per volta: un file unico per broker si riscriverebbe
/// tutto a ogni rilevazione e il suo diff non direbbe piu' quale strumento e' cambiato.</para>
///
/// <para><b>La chiave e' il nome sul BROKER</b>, non quello Piootoo. Su un broker nuovo la tabella
/// di conversione non esiste ancora, quindi <c>@NQ</c> non e' ancora nessuno: l'archivio registra
/// cio' che il broker dichiara, e la mappatura resta una decisione che vive in
/// <c>symbol-conversions.json</c>. Il nome Piootoo si annota quando il bot lo conosce, come
/// promemoria, non come chiave.</para>
///
/// <para><b>Scrive solo quando qualcosa cambia.</b> Il bot gira ogni giorno; le specifiche cambiano
/// due volte l'anno. Uno scatto per rilevazione darebbe trecento copie identiche e seppellirebbe
/// l'unica domanda che conta — <i>quando e' cambiato?</i> — sotto il rumore. Una rilevazione
/// identica alla precedente allunga <c>LastSeenUtc</c> dello scatto in corso, che e' l'informazione
/// vera: "queste specifiche sono state viste fino a ieri".</para>
/// </summary>
public sealed class SymbolInfoStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>
    /// Proprieta' che cambiano a ogni tick e non sono specifiche: tenerle farebbe nascere uno scatto
    /// nuovo ogni volta che il bot gira, cioe' esattamente il rumore che l'archivio esiste per
    /// evitare. Il confronto le ignora e il file non le porta.
    ///
    /// <para>Si scartano per <b>nome</b> e non per euristica sul valore: un prezzo che per caso non
    /// si muove resterebbe dentro, e l'archivio non sarebbe piu' riproducibile.</para>
    /// </summary>
    private static readonly HashSet<string> Volatile = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ask", "Bid", "Spread", "Time", "ServerTime"
    };

    private readonly string _root;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.OrdinalIgnoreCase);

    public SymbolInfoStore(PiootooSettings settings)
    {
        _root = settings.GetSymbolInfoPath();
        Directory.CreateDirectory(_root);
    }

    public string RootPath => _root;

    /// <summary>
    /// Accoda una rilevazione. Idempotente per costruzione: rimandare le stesse specifiche non
    /// produce uno scatto nuovo, quindi il bot puo' spedire a ogni avvio senza doversi ricordare
    /// cosa ha gia' mandato.
    /// </summary>
    public async Task<SymbolInfoIngestResponseDto> IngestAsync(SymbolInfoIngestRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var broker = ExternalDatafeedStore.NormalizeBroker(request.Broker);
        if (request.Symbols.Count == 0)
            throw new ArgumentException("Nessuno strumento da registrare: 'symbols' e' vuoto.");

        // L'istante e' UNO per tutta la rilevazione e non uno per simbolo: gli strumenti di un giro
        // sono stati letti nello stesso momento, e datarli separatamente farebbe sembrare successivi
        // cambiamenti che sono simultanei.
        var takenUtc = Normalize(request.TakenUtc ?? DateTime.UtcNow);

        var response = new SymbolInfoIngestResponseDto { Broker = broker, TakenUtc = takenUtc };

        foreach (var symbol in request.Symbols)
        {
            SymbolInfoIngestResultDto result;
            try
            {
                result = await IngestSymbolAsync(broker, takenUtc, request, symbol);
            }
            catch (ArgumentException error)
            {
                // Uno strumento sbagliato non costa la rilevazione degli altri: in un giro da venti
                // simboli, uno con un nome inutilizzabile non deve far perdere gli altri diciannove.
                result = new SymbolInfoIngestResultDto
                {
                    BrokerSymbol = symbol.BrokerSymbol,
                    PiootooSymbol = symbol.PiootooSymbol,
                    Rejected = error.Message
                };
            }

            response.Symbols.Add(result);

            if (result.Rejected is not null)
                response.Rejected++;
            else if (result.Changed)
                response.Changed++;
            else
                response.Unchanged++;
        }

        return response;
    }

    private async Task<SymbolInfoIngestResultDto> IngestSymbolAsync(
        string broker,
        DateTime takenUtc,
        SymbolInfoIngestRequestDto request,
        SymbolInfoDto symbol)
    {
        var fileName = NormalizeSymbolFileName(symbol.BrokerSymbol);
        var path = Path.Combine(_root, broker, fileName + ".json");

        var gate = _gates.GetOrAdd($"{broker}/{fileName}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var archive = Read(path) ?? new SymbolInfoArchiveDto
            {
                Broker = broker,
                BrokerSymbol = symbol.BrokerSymbol.Trim()
            };

            if (!string.IsNullOrWhiteSpace(symbol.PiootooSymbol))
                archive.PiootooSymbol = symbol.PiootooSymbol.Trim();

            var properties = Clean(symbol.Properties);
            var last = archive.Snapshots.Count == 0 ? null : archive.Snapshots[^1];
            var changed = last is null ? null : Diff(last.Properties, properties);

            var result = new SymbolInfoIngestResultDto
            {
                BrokerSymbol = archive.BrokerSymbol,
                PiootooSymbol = archive.PiootooSymbol
            };

            if (last is not null && changed!.Count == 0)
            {
                // Stesse specifiche: non nasce uno scatto, si allunga quello in corso. Il secondo
                // estremo va PROTETTO dal tornare indietro — una rilevazione vecchia rispedita non
                // deve accorciare la validita' di uno scatto.
                if (takenUtc > last.LastSeenUtc)
                    last.LastSeenUtc = takenUtc;

                result.Changed = false;
                result.SnapshotCount = archive.Snapshots.Count;
                Write(path, archive);
                return result;
            }

            archive.Snapshots.Add(new SymbolInfoSnapshotDto
            {
                TakenUtc = takenUtc,
                LastSeenUtc = takenUtc,
                AccountNumber = request.AccountNumber,
                BotVersion = request.BotVersion,
                Properties = properties,
                Warnings = symbol.Warnings
            });

            // Gli scatti si tengono in ordine di rilevazione anche quando arrivano fuori ordine: chi
            // legge "le specifiche in vigore il giorno X" scorre l'elenco, e un elenco disordinato
            // gli darebbe la risposta sbagliata senza sembrare rotto.
            archive.Snapshots.Sort((left, right) => left.TakenUtc.CompareTo(right.TakenUtc));

            result.Changed = true;
            result.ChangedProperties = changed ?? [];
            result.SnapshotCount = archive.Snapshots.Count;
            Write(path, archive);
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Gli strumenti archiviati di un broker, con l'ultimo scatto di ciascuno.</summary>
    public IReadOnlyList<SymbolInfoArchiveDto> GetArchives(string broker)
    {
        var name = ExternalDatafeedStore.NormalizeBroker(broker);
        var folder = Path.Combine(_root, name);
        if (!Directory.Exists(folder))
            return [];

        var archives = new List<SymbolInfoArchiveDto>();
        foreach (var path in Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
        {
            var archive = Read(path);
            if (archive is not null)
                archives.Add(archive);
        }

        return archives
            .OrderBy(archive => archive.BrokerSymbol, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>I broker che hanno almeno uno strumento archiviato.</summary>
    public IReadOnlyList<string> GetBrokers()
    {
        if (!Directory.Exists(_root))
            return [];

        return Directory.EnumerateDirectories(_root)
            .Select(folder => Path.GetFileName(folder)!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Le specifiche <b>in vigore</b> a un istante: l'ultimo scatto non successivo a quella data.
    ///
    /// <para>Quando non ce n'e' nessuno — la rilevazione e' tutta posteriore al periodo chiesto, che
    /// e' il caso normale di un backtest sul 2023 misurato oggi — si restituisce il <b>primo</b>
    /// scatto, ma la cosa va detta da chi chiama: applicare a un periodo tariffe rilevate dopo e'
    /// un'ipotesi, non una misura, e un run che la fa deve dichiararla.</para>
    /// </summary>
    public SymbolInfoSnapshotDto? SnapshotAt(SymbolInfoArchiveDto archive, DateTime whenUtc, out bool isLaterThanAsked)
    {
        isLaterThanAsked = false;
        if (archive.Snapshots.Count == 0)
            return null;

        SymbolInfoSnapshotDto? inForce = null;
        foreach (var snapshot in archive.Snapshots)
        {
            if (snapshot.TakenUtc <= whenUtc)
                inForce = snapshot;
        }

        if (inForce is not null)
            return inForce;

        isLaterThanAsked = true;
        return archive.Snapshots[0];
    }

    /// <summary>
    /// Nome di file da un simbolo di broker. I simboli veri contengono punti (<c>US100.cash</c>),
    /// quindi il punto si tiene; tutto il resto fuori da lettere, cifre, punto, trattino e
    /// underscore no, perche' il nome arriva da un bot e un <c>..</c> uscirebbe dal repository.
    /// </summary>
    public static string NormalizeSymbolFileName(string? brokerSymbol)
    {
        if (string.IsNullOrWhiteSpace(brokerSymbol))
            throw new ArgumentException("Nome dello strumento mancante: senza, la rilevazione non e' attribuibile.");

        var builder = new StringBuilder(brokerSymbol.Length);
        foreach (var character in brokerSymbol.Trim())
        {
            if (char.IsLetterOrDigit(character) || character is '.' or '-' or '_')
                builder.Append(character);
        }

        var name = builder.ToString().Trim('.');
        if (name.Length == 0)
            throw new ArgumentException($"Nome dello strumento '{brokerSymbol}' non utilizzabile come nome di file.");

        return name;
    }

    /// <summary>
    /// Quali proprieta' sono cambiate fra due rilevazioni, comprese quelle comparse e sparite: una
    /// proprieta' che l'API smette di esporre e' un cambiamento quanto un valore diverso, e tacerlo
    /// farebbe sembrare stabile un archivio che non lo e' piu'.
    /// </summary>
    private static List<string> Diff(
        IReadOnlyDictionary<string, string> previous,
        IReadOnlyDictionary<string, string> current)
    {
        var changed = new List<string>();

        foreach (var (key, value) in current)
        {
            if (!previous.TryGetValue(key, out var old))
                changed.Add($"{key}: assente -> {value}");
            else if (!string.Equals(old, value, StringComparison.Ordinal))
                changed.Add($"{key}: {old} -> {value}");
        }

        foreach (var (key, old) in previous)
        {
            if (!current.ContainsKey(key))
                changed.Add($"{key}: {old} -> assente");
        }

        changed.Sort(StringComparer.Ordinal);
        return changed;
    }

    private static Dictionary<string, string> Clean(IReadOnlyDictionary<string, string>? properties)
    {
        var cleaned = new Dictionary<string, string>(StringComparer.Ordinal);
        if (properties is null)
            return cleaned;

        foreach (var (key, value) in properties)
        {
            if (string.IsNullOrWhiteSpace(key) || Volatile.Contains(key))
                continue;

            cleaned[key.Trim()] = value ?? string.Empty;
        }

        return cleaned;
    }

    private static SymbolInfoArchiveDto? Read(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<SymbolInfoArchiveDto>(File.ReadAllText(path), Json);
        }
        catch (JsonException error)
        {
            throw new InvalidDataException(
                $"{path} non e' un archivio di specifiche leggibile: {error.Message}. " +
                "Va riparato o rimosso a mano: sovrascriverlo perderebbe la storia dei cambiamenti.",
                error);
        }
    }

    private static void Write(string path, SymbolInfoArchiveDto archive)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // durable: l'archivio si scrive una volta al giorno per simbolo, non e' un checkpoint dentro
        // un loop, e perderlo per un riavvio significherebbe perdere la data di un cambiamento.
        AtomicFileWriter.WriteAllText(path, JsonSerializer.Serialize(archive, Json));
    }

    /// <summary>
    /// Tutto il dominio e' UTC: un istante che arriva da fuori con un altro <c>Kind</c> viene
    /// <b>convertito</b>, non ri-etichettato, perche' ri-etichettarlo sposterebbe la rilevazione di
    /// ore senza che niente lo segnali.
    /// </summary>
    private static DateTime Normalize(DateTime instant) =>
        instant.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(instant, DateTimeKind.Utc)
            // Un istante con offset ("+02:00") deserializzato come ora della macchina: riportarlo
            // a UTC e' l'unica conversione che restituisce l'istante originale. Su un istante gia'
            // UTC e' l'identita'.
            : instant.ToUniversalTime();
}
