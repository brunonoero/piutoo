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

        // Il file puo' stare nella cartella del broker o direttamente nella radice, come i CSV di
        // spread gia' presenti.
        var candidati = new List<string>();
        var cartella = Path.Combine(root, name);
        if (Directory.Exists(cartella))
            candidati.AddRange(Directory.GetFiles(cartella, FilePattern));
        if (Directory.Exists(root))
            candidati.AddRange(Directory.GetFiles(root, name + FilePattern));

        var file = candidati
            .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
            .FirstOrDefault();

        if (file is null)
        {
            throw new FileNotFoundException(
                $"Nessuna misura di swap per il broker {name}: cercato '{FilePattern}' in " +
                $"{cartella} e in {root}. Compilala dalla scheda del simbolo in cTrader " +
                "(swap long/short, pip position, swap time, 3-day swaps).");
        }

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
