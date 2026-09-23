using System.Globalization;
using System.Text;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Brokers;

namespace Piootoo.Core.Services;

/// <summary>
/// Scrive le righe di swap di un broker a partire dalle schede che il raccoglitore registra ogni
/// giorno (<see cref="SymbolInfoStore"/>), nel file che il backtest legge (<see cref="SwapTable"/>).
///
/// <para><b>Perche'.</b> Fino al 23/09/2026 la tabella si compilava a mano dalla scheda di cTrader,
/// un simbolo alla volta. Le schede ora arrivano da sole: ricopiarle a mano era il passaggio in cui
/// un numero sbagliato non lo vede nessuno.</para>
///
/// <para><b>Mediana pesata, non ultimo valore.</b> Lo swap di un indice CFD porta dentro, il giorno
/// dello stacco, l'aggiustamento per il dividendo: su EU50 la scheda del 23/09/2026 dava +3,56 e
/// -4,71 punti a notte, il 27% annuo sullo short, contro un tasso base di -1,13 e -0,01. Un giorno
/// cosi' non e' il costo di una posizione. La mediana degli ultimi <see cref="WindowDays"/> giorni,
/// pesata per quanto a lungo ciascuna scheda e' rimasta in vigore, lo lascia fuori appena ci sono
/// abbastanza giorni; con una scheda sola il valore e' quello, e il sorgente della riga lo dice.</para>
///
/// <para><b>Le righe scritte a mano vincono.</b> Una riga il cui <c>source</c> non comincia per
/// <c>auto:</c> e' una decisione presa guardando la scheda, e il calcolo non la sovrascrive: il suo
/// simbolo resta fuori dalle righe automatiche. Le righe <c>auto:</c> si rigenerano tutte a ogni
/// giro.</para>
///
/// <para><b>Cosa non sa fare.</b> Lo swap in percentuale del nozionale (le cripto FTMO, 30% annuo)
/// non si esprime in punti fissi a notte, che e' la sola forma che <see cref="SwapSpec"/> conosce:
/// quei simboli restano senza riga e un run che li chiede con lo swap fallisce all'avvio, come deve.
/// E l'ora del rollover non sta nella scheda: si prende dalle righe manuali dello stesso broker, e
/// senza nemmeno una non si scrive niente.</para>
/// </summary>
public sealed class SwapFromSymbolInfo
{
    public const int WindowDays = 30;
    public const string AutoPrefix = "auto:";

    public const string Header =
        "broker,symbol,longPipsPerNight,shortPipsPerNight,pipInPoints,rolloverUtc,tripleDay,source";

    private readonly string _root;
    private readonly SymbolInfoStore _symbolInfo;
    private readonly object _gate = new();

    public SwapFromSymbolInfo(PiootooSettings settings, SymbolInfoStore symbolInfo)
    {
        _root = settings.GetSwapPath();
        _symbolInfo = symbolInfo;
    }

    /// <summary>
    /// Rigenera le righe automatiche del broker e restituisce cosa ha fatto, simbolo per simbolo.
    /// Non solleva per un simbolo che non si puo' calcolare: lo dice e passa al successivo.
    /// </summary>
    public SwapRebuildResult Rebuild(string broker, DateTime? nowUtc = null)
    {
        var name = ExternalDatafeedStore.NormalizeBroker(broker);
        var now = nowUtc ?? DateTime.UtcNow;
        var result = new SwapRebuildResult { Broker = name };

        lock (_gate)
        {
            var folder = Path.Combine(_root, name);
            var file = Directory.Exists(folder)
                ? new DirectoryInfo(folder).EnumerateFiles(SwapTable.FilePattern, SearchOption.TopDirectoryOnly)
                    .OrderByDescending(candidate => candidate.LastWriteTimeUtc)
                    .FirstOrDefault()?.FullName
                : null;

            var lines = file is null ? [] : File.ReadAllLines(file).ToList();
            var comments = lines.Where(line => line.StartsWith('#')).ToList();
            var manual = lines
                .Where(line => !line.StartsWith('#') && !string.IsNullOrWhiteSpace(line) &&
                               !line.StartsWith("broker,", StringComparison.OrdinalIgnoreCase))
                .Where(line => !SourceOf(line).StartsWith(AutoPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var manualSymbols = manual.Select(SymbolOf).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var rollover = manual.Select(RolloverOf).FirstOrDefault(value => value is not null);
            if (rollover is null)
            {
                result.Warnings.Add(
                    $"{name}: nessuna riga di swap scritta a mano da cui prendere l'ora del rollover, che la " +
                    "scheda non dichiara. Scrivine una dalla scheda di un simbolo e le altre si calcoleranno.");
                return result;
            }

            var autoRows = new List<string>();
            foreach (var archive in _symbolInfo.GetArchives(name))
            {
                if (string.IsNullOrWhiteSpace(archive.PiootooSymbol) || archive.Snapshots.Count == 0)
                    continue;

                var symbol = "@" + archive.PiootooSymbol.Trim().TrimStart('@').ToUpperInvariant();
                if (manualSymbols.Contains(symbol))
                {
                    result.Manual.Add(symbol);
                    continue;
                }

                var row = BuildRow(name, symbol, archive, rollover, now, out var skipped);
                if (row is null)
                {
                    result.Warnings.Add($"{symbol}: {skipped}");
                    continue;
                }

                autoRows.Add(row);
                result.Auto.Add(symbol);
            }

            const string autoNote =
                "# Le righe con source 'auto:' le scrive il server dalle schede del raccoglitore (mediana pesata degli ultimi 30 giorni); le altre sono scritte a mano e vincono.";
            if (!comments.Contains(autoNote, StringComparer.Ordinal))
                comments.Add(autoNote);

            var text = new StringBuilder();
            foreach (var line in comments)
                text.Append(line).Append('\n');
            text.Append(Header).Append('\n');
            foreach (var line in manual)
                text.Append(line).Append('\n');
            foreach (var line in autoRows.Order(StringComparer.OrdinalIgnoreCase))
                text.Append(line).Append('\n');

            Directory.CreateDirectory(folder);
            var target = file ?? Path.Combine(folder, $"{name}_swap-by-symbol.csv");
            AtomicFileWriter.WriteAllText(target, text.ToString());
            result.File = Path.GetFileName(target);
        }

        return result;
    }

    private static string? BuildRow(
        string broker, string symbol, SymbolInfoArchiveDto archive, string rollover, DateTime now, out string skipped)
    {
        skipped = string.Empty;
        var latest = archive.Snapshots[^1];

        var type = Property(latest, "SwapCalculationType");
        if (!string.Equals(type, "Pips", StringComparison.OrdinalIgnoreCase))
        {
            skipped = $"swap di tipo '{type}', non in punti: non esprimibile nella tabella, nessuna riga.";
            return null;
        }

        var pip = Decimal(Property(latest, "PipSize"));
        if (pip is null or <= 0)
        {
            skipped = "la scheda non dichiara PipSize: senza non si sa quanti punti vale un pip.";
            return null;
        }

        var since = now.AddDays(-WindowDays);
        var inWindow = new List<(SymbolInfoSnapshotDto Snapshot, double Weight)>();
        for (var index = 0; index < archive.Snapshots.Count; index++)
        {
            var snapshot = archive.Snapshots[index];
            var end = index + 1 < archive.Snapshots.Count ? archive.Snapshots[index + 1].TakenUtc : snapshot.LastSeenUtc;
            if (end < since || !string.Equals(Property(snapshot, "SwapCalculationType"), "Pips", StringComparison.OrdinalIgnoreCase))
                continue;

            var start = snapshot.TakenUtc < since ? since : snapshot.TakenUtc;
            // Una scheda vista una volta sola vale almeno un'ora: con peso zero una rilevazione unica
            // non conterebbe niente, e la tabella resterebbe vuota proprio all'inizio.
            inWindow.Add((snapshot, Math.Max(1d, (end - start).TotalHours)));
        }

        if (inWindow.Count == 0)
        {
            skipped = $"nessuna scheda negli ultimi {WindowDays} giorni.";
            return null;
        }

        var longPips = WeightedMedian(inWindow, "SwapLong");
        var shortPips = WeightedMedian(inWindow, "SwapShort");
        if (longPips is null || shortPips is null)
        {
            skipped = "SwapLong o SwapShort non leggibili nelle schede.";
            return null;
        }

        var triple = Property(latest, "Swap3DaysRollover");
        var from = inWindow.Min(entry => entry.Snapshot.TakenUtc);
        var to = inWindow.Max(entry => entry.Snapshot.LastSeenUtc);
        var source = string.Format(CultureInfo.InvariantCulture,
            "{0} mediana pesata di {1} schede {2} dal {3:yyyy-MM-dd} al {4:yyyy-MM-dd}",
            AutoPrefix, inWindow.Count, archive.BrokerSymbol, from, to);

        return string.Join(',',
            broker,
            symbol,
            Format(longPips.Value),
            Format(shortPips.Value),
            Format(pip.Value),
            rollover,
            string.IsNullOrWhiteSpace(triple) ? string.Empty : triple.Trim(),
            source);
    }

    private static decimal? WeightedMedian(List<(SymbolInfoSnapshotDto Snapshot, double Weight)> entries, string property)
    {
        var values = new List<(decimal Value, double Weight)>();
        foreach (var (snapshot, weight) in entries)
        {
            var value = Decimal(Property(snapshot, property));
            if (value is not null)
                values.Add((value.Value, weight));
        }

        if (values.Count == 0)
            return null;

        values.Sort((left, right) => left.Value.CompareTo(right.Value));
        var half = values.Sum(entry => entry.Weight) / 2d;
        var cumulative = 0d;
        foreach (var (value, weight) in values)
        {
            cumulative += weight;
            if (cumulative >= half)
                return value;
        }

        return values[^1].Value;
    }

    private static string Property(SymbolInfoSnapshotDto snapshot, string name) =>
        snapshot.Properties.TryGetValue(name, out var value) ? value : string.Empty;

    private static decimal? Decimal(string text) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static string Format(decimal value) => (value / 1.0000000000000000000000000000m).ToString(CultureInfo.InvariantCulture);

    private static string SymbolOf(string line)
    {
        var cells = line.Split(',');
        return cells.Length > 1 ? "@" + cells[1].Trim().TrimStart('@').ToUpperInvariant() : string.Empty;
    }

    private static string? RolloverOf(string line)
    {
        var cells = line.Split(',');
        return cells.Length > 5 && TimeOnly.TryParseExact(cells[5].Trim(), "HH\\:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? cells[5].Trim()
            : null;
    }

    /// <summary>Il campo <c>source</c> e' l'ultimo e puo' contenere virgole: e' tutto cio' che
    /// viene dopo il settimo separatore.</summary>
    private static string SourceOf(string line)
    {
        var index = -1;
        for (var separators = 0; separators < 7; separators++)
        {
            index = line.IndexOf(',', index + 1);
            if (index < 0)
                return string.Empty;
        }

        return line[(index + 1)..].Trim();
    }
}

public sealed class SwapRebuildResult
{
    public string Broker { get; set; } = string.Empty;
    public string? File { get; set; }
    public List<string> Auto { get; set; } = new();
    public List<string> Manual { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
