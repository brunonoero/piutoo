using System.Globalization;
using System.Text;

namespace Piootoo.Core.Services;

/// <summary>
/// Estremi di un feed piatto <c>@SYM_{minuti}.json</c> letti <b>senza deserializzarlo</b>.
///
/// <para>Un feed di questo repository arriva a centinaia di migliaia di barre e a decine di MB:
/// aprire venti file interi per riempire venti righe di elenco costerebbe piu' del backtest che
/// l'elenco serve a preparare — e' la stessa regola che vale per gli artefatti di workspace
/// (<c>.cursor/rules/piutoo-console-screens.mdc</c>, "Prestazioni").</para>
///
/// <para>Il formato aiuta: l'intestazione precede l'array e <c>candleCount</c> lo segue, quindi la
/// prima barra sta nei primi byte e l'ultima negli ultimi. Si leggono due finestre di 64 KB e si
/// cerca in ciascuna la chiave <c>dateTime</c>, che e' ASCII: e' una ricerca di sottostringa, non
/// un parser, quindi non dipende da come il file e' indentato.</para>
///
/// <para>Gli istanti restituiti sono <b>come stampati nel file</b>, non ancora in UTC vero: la
/// <c>Z</c> nei timestamp non e' una garanzia (vedi <c>FeedClockRegistry</c>) e la conversione
/// spetta a chi conosce l'orologio dichiarato dal feed.</para>
/// </summary>
internal static class FlatFeedProbe
{
    /// <summary>Quanto si legge in testa e in coda. Una barra sta in poche centinaia di byte.</summary>
    private const int WindowBytes = 64 * 1024;

    private const string DateTimeKey = "\"dateTime\"";
    private const string CandlesKey = "\"candles\"";
    private const string CountKey = "\"candleCount\"";

    /// <summary>Estremi dichiarati dal file, con il motivo quando non si leggono.</summary>
    internal readonly record struct Range(
        DateTime? First,
        DateTime? Last,
        int? CandleCount,
        string? Problem);

    internal static Range Read(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var length = stream.Length;
            if (length == 0)
                return new Range(null, null, null, "File vuoto.");

            var head = ReadWindow(stream, 0, (int)Math.Min(length, WindowBytes));
            var tail = length <= WindowBytes
                ? head
                : ReadWindow(stream, length - WindowBytes, WindowBytes);

            // La prima barra si cerca dopo l'array: l'intestazione ha campi di data propri
            // (lastUpdate) che non sono barre.
            var candlesAt = head.IndexOf(CandlesKey, StringComparison.OrdinalIgnoreCase);
            var first = candlesAt < 0 ? null : FirstDateAfter(head, candlesAt);
            var last = LastDate(tail);

            if (first == null || last == null)
            {
                return new Range(
                    first,
                    last,
                    ReadCandleCount(tail),
                    candlesAt < 0
                        ? "Il file non ha un array 'candles': non e' un feed piatto."
                        : "Nessuna barra nel file.");
            }

            return new Range(first, last, ReadCandleCount(tail), null);
        }
        catch (IOException error)
        {
            return new Range(null, null, null, $"File non leggibile: {error.Message}");
        }
        catch (UnauthorizedAccessException error)
        {
            return new Range(null, null, null, $"File non leggibile: {error.Message}");
        }
    }

    private static string ReadWindow(FileStream stream, long offset, int count)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        var buffer = new byte[count];
        var read = stream.ReadAtLeast(buffer, count, throwOnEndOfStream: false);

        // Le chiavi cercate sono ASCII: un carattere multibyte tagliato al bordo della finestra
        // diventa un carattere di sostituzione e non sposta nulla.
        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    private static DateTime? FirstDateAfter(string window, int from)
    {
        var at = window.IndexOf(DateTimeKey, from, StringComparison.OrdinalIgnoreCase);
        return at < 0 ? null : ValueAt(window, at);
    }

    private static DateTime? LastDate(string window)
    {
        var at = window.LastIndexOf(DateTimeKey, StringComparison.OrdinalIgnoreCase);
        return at < 0 ? null : ValueAt(window, at);
    }

    /// <summary>Valore della coppia chiave/valore che inizia in <paramref name="keyAt"/>.</summary>
    private static DateTime? ValueAt(string window, int keyAt)
    {
        var open = window.IndexOf('"', keyAt + DateTimeKey.Length);
        if (open < 0)
            return null;

        var close = window.IndexOf('"', open + 1);
        if (close < 0)
            return null; // valore tagliato dal bordo della finestra: meglio niente di una data monca

        // RoundtripKind per non spostare l'orario: la 'Z' finale dice come e' stampato il file,
        // e se sia vera lo decide l'orologio dichiarato del feed, non questo parser.
        return DateTime.TryParse(
            window.AsSpan(open + 1, close - open - 1),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified)
            : null;
    }

    private static int? ReadCandleCount(string window)
    {
        var at = window.LastIndexOf(CountKey, StringComparison.OrdinalIgnoreCase);
        if (at < 0)
            return null;

        var colon = window.IndexOf(':', at + CountKey.Length);
        if (colon < 0)
            return null;

        var start = colon + 1;
        while (start < window.Length && char.IsWhiteSpace(window[start]))
            start++;

        var end = start;
        while (end < window.Length && char.IsDigit(window[end]))
            end++;

        return int.TryParse(
            window.AsSpan(start, end - start),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var count)
            ? count
            : null;
    }
}
