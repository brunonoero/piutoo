using Piootoo.Domain.Repositories;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;

namespace Piootoo.Core.Planning;

/// <summary>Direzione del mercato di un giorno.</summary>
public enum DirectionRegime
{
    TrendUp,
    TrendDown,
    Sideways
}

/// <summary>Volatilita' del mercato di un giorno, rispetto al suo ultimo anno.</summary>
public enum VolatilityRegime
{
    Calm,
    Normal,
    Agitated
}

/// <summary>Il regime di un giorno di sessione, sui due assi.</summary>
public readonly record struct DayRegime(DirectionRegime Direction, VolatilityRegime Volatility);

/// <summary>
/// Le etichette di regime di un simbolo, per giorno di sessione. <see cref="At"/> risponde per un istante:
/// il giorno di sessione che lo contiene, o l'ultimo etichettato nei quattro giorni prima (un festivo, un
/// fine settimana), altrimenti niente.
/// </summary>
public sealed class SymbolRegimes
{
    private const int MaxCarryDays = 4;
    private readonly SessionGrid _grid;
    private readonly SortedList<DateTime, DayRegime> _days;

    public SymbolRegimes(string symbol, string source, SessionGrid grid, SortedList<DateTime, DayRegime> days)
    {
        Symbol = symbol;
        Source = source;
        _grid = grid;
        _days = days;
    }

    public string Symbol { get; }

    /// <summary>Il file da cui vengono le barre, per il resoconto.</summary>
    public string Source { get; }

    public int Count => _days.Count;

    public DateTime? FirstDay => _days.Count > 0 ? _days.Keys[0] : null;

    public DateTime? LastDay => _days.Count > 0 ? _days.Keys[^1] : null;

    public DayRegime? At(DateTime instantUtc)
    {
        if (_days.Count == 0)
            return null;

        var day = _grid.SessionDayOf(instantUtc);
        if (_days.TryGetValue(day, out var exact))
            return exact;

        // Ultimo giorno etichettato non successivo: ricerca binaria sulle chiavi ordinate.
        var keys = _days.Keys;
        int lo = 0, hi = keys.Count;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (keys[mid] <= day) lo = mid + 1;
            else hi = mid;
        }

        if (lo == 0)
            return null;
        var previous = keys[lo - 1];
        return (day - previous).TotalDays <= MaxCarryDays ? _days.Values[lo - 1] : null;
    }
}

/// <summary>
/// <b>Il regime di mercato di ogni giorno di sessione</b>, calcolato con le sole barre giornaliere chiuse
/// <b>prima</b> di quel giorno: e' cio' che si sa quando la strategia entra.
///
/// <para><b>Direzione.</b> Efficiency ratio a <see cref="EfficiencyDays"/> giorni: spostamento netto delle
/// chiusure su somma degli spostamenti. Il terzile alto del simbolo, e almeno
/// <see cref="MinTrendEfficiency"/>, e' trend (su o giu' secondo il segno); il resto e' laterale. Il terzile
/// e' sull'intera storia del simbolo: e' una soglia di scala, non un'informazione sul giorno.</para>
///
/// <para><b>Volatilita'.</b> ATR a <see cref="AtrDays"/> giorni in rapporto al prezzo, come percentile sugli
/// ultimi <see cref="VolatilityWindowDays"/> giorni: sotto un terzo calma, sopra due terzi agitata.</para>
///
/// <para><b>A cosa serve.</b> A <b>descrivere</b>, non a spegnere: sul paniere 2012-2025 spegnere le
/// strategie nei regimi che la storia diceva dannosi toglieva guadagno a ogni taglio provato
/// (<c>ricerca/regimi-2026-09-29/esito.md</c>). Il plan-builder lo usa per verificare che un piano non
/// perda tutto nello stesso regime.</para>
/// </summary>
public static class MarketRegimeClassifier
{
    public const int EfficiencyDays = 50;
    public const int AtrDays = 20;
    public const int VolatilityWindowDays = 250;
    public const int MinVolatilityDays = 60;
    public const double MinTrendEfficiency = 0.15;

    /// <summary>I timeframe da cui si ricavano le barre giornaliere, in ordine di preferenza.</summary>
    private static readonly int[] SourceTimeframes = [1440, 240, 60, 30, 15];

    /// <summary>
    /// Etichetta i giorni di una serie di barre giornaliere gia' ordinate. La chiave e' il giorno di
    /// sessione della barra (<see cref="SessionGrid.SessionDayOf"/>); l'etichetta del giorno <c>i</c> usa
    /// le barre fino a <c>i-1</c>.
    /// </summary>
    public static SortedList<DateTime, DayRegime> Classify(IReadOnlyList<OhlcvData> daily, SessionGrid grid)
    {
        var n = daily.Count;
        var closes = new double[n];
        var trueRange = new double[n];
        for (var i = 0; i < n; i++)
        {
            var bar = daily[i];
            closes[i] = (double)bar.Close;
            trueRange[i] = i == 0
                ? (double)(bar.High - bar.Low)
                : Math.Max((double)bar.High, closes[i - 1]) - Math.Min((double)bar.Low, closes[i - 1]);
        }

        var efficiency = new double?[n];
        var sign = new int[n];
        var atrRatio = new double?[n];
        for (var i = 0; i < n; i++)
        {
            if (i >= EfficiencyDays)
            {
                double path = 0;
                for (var k = i - EfficiencyDays + 1; k <= i; k++)
                    path += Math.Abs(closes[k] - closes[k - 1]);
                var move = closes[i] - closes[i - EfficiencyDays];
                efficiency[i] = path > 0 ? Math.Abs(move) / path : 0;
                sign[i] = move > 0 ? 1 : -1;
            }

            if (i >= AtrDays && closes[i] > 0)
            {
                double sum = 0;
                for (var k = i - AtrDays + 1; k <= i; k++)
                    sum += trueRange[k];
                atrRatio[i] = sum / AtrDays / closes[i];
            }
        }

        var known = efficiency.Where(e => e.HasValue).Select(e => e!.Value).OrderBy(e => e).ToArray();
        var trendThreshold = known.Length > 0
            ? Math.Max(known[Math.Min(known.Length - 1, known.Length * 2 / 3)], MinTrendEfficiency)
            : double.MaxValue;

        var labels = new SortedList<DateTime, DayRegime>();
        for (var i = 1; i < n; i++)
        {
            var j = i - 1; // l'ultimo giorno chiuso prima di questo
            if (efficiency[j] is not { } er || atrRatio[j] is not { } atr)
                continue;

            var window = 0;
            var below = 0;
            for (var k = Math.Max(0, j - VolatilityWindowDays + 1); k <= j; k++)
            {
                if (atrRatio[k] is not { } other) continue;
                window++;
                if (other < atr) below++;
            }

            if (window < MinVolatilityDays)
                continue;

            var direction = er >= trendThreshold
                ? sign[j] > 0 ? DirectionRegime.TrendUp : DirectionRegime.TrendDown
                : DirectionRegime.Sideways;
            var rank = (double)below / window;
            var volatility = rank < 1.0 / 3 ? VolatilityRegime.Calm
                : rank > 2.0 / 3 ? VolatilityRegime.Agitated
                : VolatilityRegime.Normal;

            labels[grid.SessionDayOf(daily[i].DateTime)] = new DayRegime(direction, volatility);
        }

        return labels;
    }

    /// <summary>
    /// Carica le etichette dei simboli da una cartella di datafeed (<c>datafeed\</c> o
    /// <c>datafeed-external\{BROKER}\</c>): la barra giornaliera se c'e', altrimenti la piu' larga fra
    /// 240, 60, 30 e 15, piegata sul giorno di sessione del calendario. Un simbolo senza calendario o
    /// senza feed resta fuori, con il motivo in <paramref name="missing"/>: e' un controllo, non
    /// un'esecuzione, e il resoconto lo dichiara.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, SymbolRegimes>> LoadAsync(
        string datafeedRoot, IEnumerable<string> symbols, IDictionary<string, string> missing)
    {
        var repository = new DataSourceRepository(datafeedRoot);
        var result = new Dictionary<string, SymbolRegimes>(StringComparer.OrdinalIgnoreCase);
        foreach (var symbol in symbols.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!MarketCalendarRegistry.Current.TryGet(symbol, out var calendar))
            {
                missing[symbol] = "nessun calendario di mercato";
                continue;
            }

            var timeframe = SourceTimeframes.FirstOrDefault(tf =>
                File.Exists(Path.Combine(datafeedRoot, $"@{symbol.TrimStart('@').ToUpperInvariant()}_{tf}.json")));
            if (timeframe == 0)
            {
                missing[symbol] = $"nessun feed da 15 a 1440 minuti in '{datafeedRoot}'";
                continue;
            }

            var grid = new SessionGrid(calendar);
            var bars = await repository.LoadAllDataAsync(symbol, timeframe.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var inSession = grid.DropNonSessionDays(bars, out _);
            // Senza maschera oraria: per il regime conta il giorno, e la maschera sulle barre gia' piegate
            // ha una semantica propria (SessionMask.DropOutsideWindow) che qui non serve.
            var daily = new BarAggregator(grid, 1440, mask: null).Aggregate(inSession).Select(b => b.Bar).ToList();
            result[symbol] = new SymbolRegimes(symbol, $"@{symbol.TrimStart('@').ToUpperInvariant()}_{timeframe}", grid, Classify(daily, grid));
        }

        return result;
    }
}
