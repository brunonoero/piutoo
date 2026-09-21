using System.Globalization;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services;

/// <summary>
/// Le misure di finanziamento di un broker, lette dai CSV di <c>piootoo-repository/swap/</c>.
///
/// <para>Stessa impostazione di <see cref="SpreadTable"/>, e per la stessa ragione: lo swap e' una
/// <b>misura</b> presa dalle specifiche del simbolo presso quel broker, non un parametro del run.
/// Il file si compila a mano dalla scheda del simbolo in cTrader — swap long, swap short, pip
/// position, swap time, 3-day swaps — perche' quei valori non passano dall'API.</para>
///
/// <para><b>Un simbolo che il file non misura fa fallire il run</b> che dichiara il broker, come il
/// datafeed mancante: un costo che non si conosce non vale zero, e girare senza saperlo significa
/// ottimizzare contro un conto che non esiste.</para>
/// </summary>
public sealed class SwapTable
{
    public const string FilePattern = "*swap-by-symbol*.csv";

    private SwapTable(string broker, string filePath, IReadOnlyDictionary<string, SwapSpec> specs)
    {
        Broker = broker;
        FilePath = filePath;
        Specs = specs;
    }

    public string Broker { get; }
    public string FilePath { get; }
    public IReadOnlyDictionary<string, SwapSpec> Specs { get; }

    public string Describe() =>
        $"{Broker} · {Specs.Count} simboli · {Path.GetFileName(FilePath)}";

    /// <summary>
    /// Carica il file piu' recente del broker. Solleva se la cartella o il file non ci sono: vale la
    /// regola del datafeed mancante, mai proseguire in silenzio.
    /// </summary>
    public static SwapTable Load(string root, string broker)
    {
        if (string.IsNullOrWhiteSpace(broker))
            throw new ArgumentException("Il broker della tabella swap non puo' essere vuoto.", nameof(broker));

        var name = broker.Trim();
        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar) ||
            name.Contains(':') || name == "." || name == "..")
        {
            throw new ArgumentException($"Nome di broker non valido: '{broker}'.", nameof(broker));
        }

        // Una sottocartella per broker, come per lo spread: il NOME DEL FILE non conta, conta la
        // cartella. Serve perche' i due nomi possono non coincidere — cTrader scrive l'entita'
        // legale ("RAWTRADINGLTD") mentre il sistema conosce il broker con il proprio codice
        // ("ICS") — e il nome del file resta cosi' la traccia di chi ha prodotto la misura.
        var cartella = Path.Combine(root, name);
        if (!Directory.Exists(cartella))
        {
            var presenti = Directory.Exists(root)
                ? Directory.EnumerateDirectories(root).Select(Path.GetFileName).ToList()
                : [];

            throw new DirectoryNotFoundException(
                $"Nessuna misura di swap per il broker {name}: manca la cartella {cartella}" +
                (presenti.Count > 0 ? $" (ci sono: {string.Join(", ", presenti)})" : string.Empty) +
                ". Il file si compila dalla scheda del simbolo in cTrader — swap long/short, " +
                "pip position, swap time, 3-day swaps.");
        }

        var file = new DirectoryInfo(cartella)
            .EnumerateFiles(FilePattern, SearchOption.TopDirectoryOnly)
            .OrderByDescending(candidate => candidate.LastWriteTimeUtc)
            .FirstOrDefault()?.FullName
            ?? throw new FileNotFoundException(
                $"In {cartella} non c'e' nessun file '{FilePattern}'.");

        var specs = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var riga in File.ReadLines(file))
        {
            if (string.IsNullOrWhiteSpace(riga) || riga.StartsWith('#')) continue;

            var campi = riga.Split(',');
            if (campi.Length < 7 || campi[0].Equals("broker", StringComparison.OrdinalIgnoreCase)) continue;

            var simbolo = StrategyKeys.NormalizeSymbol(campi[1].Trim());
            var longPips = Decimal(campi[2]);
            var shortPips = Decimal(campi[3]);
            var pipInPoints = Decimal(campi[4]);
            var rollover = TimeOnly.ParseExact(campi[5].Trim(), "HH\\:mm", CultureInfo.InvariantCulture);
            var triplo = Enum.TryParse<DayOfWeek>(campi[6].Trim(), ignoreCase: true, out var giorno)
                ? giorno
                : (DayOfWeek?)null;

            // La scheda scrive il costo come numero NEGATIVO; SwapSpec vuole punti positivi = costo.
            // Un valore positivo sulla scheda sarebbe un credito: lo si porta a zero invece di
            // regalarlo al backtest, perche' un credito incassato dipende dal conto e dal momento.
            specs[simbolo] = new SwapSpec(
                simbolo,
                Math.Max(0m, -longPips * pipInPoints),
                Math.Max(0m, -shortPips * pipInPoints),
                rollover,
                triplo);
        }

        if (specs.Count == 0)
            throw new InvalidOperationException($"La tabella swap {file} non contiene righe valide.");

        return new SwapTable(name, file, specs);

        static decimal Decimal(string campo) =>
            decimal.Parse(campo.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Il costo <b>peggiore</b> fra piu' broker, voce per voce.
    ///
    /// <para><b>Perche' voce per voce e non "il broker piu' caro".</b> Non esiste il broker piu'
    /// caro: su @FDAX, ICS costa piu' di FTMO sullo swap (5,05 punti per notte contro 4,53 sul long,
    /// 1,22 contro 0,05 sullo short) e molto meno sullo spread (0,50 contro 1,23). Scegliere un
    /// broker solo lascerebbe fuori meta' del costo peggiore.</para>
    ///
    /// <para>Il rollover peggiore e' il <b>piu' presto</b>: anticiparlo di un minuto — FTMO fa
    /// rollover alle 20:59 e ICS alle 21:00 — allarga la finestra in cui una posizione lo attraversa.
    /// Il giorno del triplo si prende dal primo che lo dichiara: nessuno dei due, finora, ne ha uno
    /// diverso.</para>
    ///
    /// <para><b>A cosa serve.</b> Una strategia cercata sui costi di un broker vive dentro il listino
    /// di quel broker: se il listino cambia, o se si cambia prop firm, non e' piu' la strategia che
    /// si era validata. Cercare sul peggiore da' configurazioni che dove si opera renderanno di piu'
    /// di quanto promesso, mai di meno — ed e' la stessa logica del criterio sul peggior
    /// sotto-periodo, applicata al costo invece che al tempo.</para>
    /// </summary>
    public static IReadOnlyDictionary<string, SwapSpec> Worst(IEnumerable<SwapTable> tables)
    {
        var peggiore = new Dictionary<string, SwapSpec>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in tables)
        {
            foreach (var (symbol, spec) in table.Specs)
            {
                if (!peggiore.TryGetValue(symbol, out var corrente))
                {
                    peggiore[symbol] = spec;
                    continue;
                }

                peggiore[symbol] = corrente with
                {
                    LongPointsPerNight = Math.Max(corrente.LongPointsPerNight, spec.LongPointsPerNight),
                    ShortPointsPerNight = Math.Max(corrente.ShortPointsPerNight, spec.ShortPointsPerNight),
                    RolloverUtc = corrente.RolloverUtc <= spec.RolloverUtc ? corrente.RolloverUtc : spec.RolloverUtc,
                    TripleDay = corrente.TripleDay ?? spec.TripleDay
                };
            }
        }

        return peggiore;
    }

    /// <summary>
    /// La misura di un simbolo. Solleva se manca: vedi la nota sulla classe.
    /// </summary>
    public SwapSpec For(string symbol)
    {
        var key = StrategyKeys.NormalizeSymbol(symbol);
        if (Specs.TryGetValue(key, out var spec)) return spec;

        throw new KeyNotFoundException(
            $"{Broker} non misura lo swap di {key}: aggiungilo a {Path.GetFileName(FilePath)} " +
            "dalla scheda del simbolo, oppure lancia il run senza broker di swap.");
    }
}
