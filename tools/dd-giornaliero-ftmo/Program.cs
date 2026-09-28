// Drawdown giornaliero alla FTMO: saldo a mezzanotte di Praga contro l'equity piu' bassa della giornata,
// con le posizioni aperte valutate al minimo (long) o al massimo (short) di ogni minuto del feed FTMO.
// Uso: ddgiornaliero "<trades.json>|<etichetta>|<scala>" ...
using System.Globalization;
using System.Text.Json;

const string FeedDir = @"C:\piootoo-dev\piootoo-repository\datafeed-external\FTMO";
var prague = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
var feeds = new Dictionary<string, SortedList<long, (double H, double L, double C)>>();

Console.WriteLine($"{"piano",-30} {"trade",5} {"netto",9} {"DD chiuso",9} {"DD equity",9} {"gg chiuso",9} {"gg FTMO",9} {"giorno",-10} {"gg>2,5k",7} {"min vs 100k",11}");
foreach (var arg in args)
{
    var parts = arg.Split('|');
    var scale = parts.Length > 2 ? double.Parse(parts[2], CultureInfo.InvariantCulture) : 1.0;
    var trades = LoadTrades(parts[0]);
    var r = Measure(trades, scale);
    Console.WriteLine($"{parts[1],-30} {trades.Count,5} {r.Net,9:N0} {r.ClosedDd,9:N0} {r.EquityDd,9:N0} {r.WorstClosedDay,9:N0} {r.WorstFtmoDay,9:N0} {r.WorstDay,-10} {r.DaysOver2500,7} {r.MinVsStart,11:N0}");
}

Result Measure(List<Trade> trades, double scale)
{
    var from = trades.Min(t => t.Entry); var to = trades.Max(t => t.Exit);
    foreach (var s in trades.Select(t => t.Symbol).Distinct()) LoadFeed(s, from, to);

    // valore di un punto per lotto, dal trade stesso o dalla mediana del simbolo
    var perLot = trades.Where(t => Math.Abs(t.ExitPrice - t.EntryPrice) > 1e-9 && t.Qty > 0)
        .GroupBy(t => t.Symbol)
        .ToDictionary(g => g.Key, g => Median(g.Select(t => t.Gross / ((t.ExitPrice - t.EntryPrice) * t.Dir * t.Qty))));

    var minutes = new SortedSet<long>();
    foreach (var s in trades.Select(t => t.Symbol).Distinct())
        foreach (var k in feeds[s].Keys) if (k >= Floor(from) && k <= Floor(to)) minutes.Add(k);

    var byExit = trades.OrderBy(t => t.Exit).ToList();
    int nextExit = 0; double closed = 0, peakEq = 0, eqDd = 0, minVsStart = 0;
    string? day = null; double dayStart = 0, worstFtmo = 0; string worstDay = "";
    var dayLoss = new Dictionary<string, double>();
    var lastClose = new Dictionary<string, double>();
    foreach (var m in minutes)
    {
        var mt = DateTimeOffset.FromUnixTimeSeconds(m).UtcDateTime;
        var d = TimeZoneInfo.ConvertTimeFromUtc(mt, prague).ToString("yyyy-MM-dd");
        while (nextExit < byExit.Count && byExit[nextExit].Exit < mt) closed += byExit[nextExit++].Net * scale;
        if (d != day) { day = d; dayStart = closed; }
        foreach (var s in feeds.Keys) if (feeds[s].TryGetValue(m, out var b)) lastClose[s] = b.C;

        double floating = 0;
        foreach (var t in trades)
        {
            if (t.Entry >= mt.AddMinutes(1) || t.Exit < mt) continue;
            if (!perLot.TryGetValue(t.Symbol, out var ppl)) continue;
            double px;
            bool entryMinute = Floor(t.Entry) == m, exitMinute = Floor(t.Exit) == m;
            if (exitMinute) px = t.ExitPrice;
            else if (entryMinute) px = t.EntryPrice;
            else if (feeds[t.Symbol].TryGetValue(m, out var bar)) px = t.Dir > 0 ? bar.L : bar.H;
            else if (lastClose.TryGetValue(t.Symbol, out var c)) px = c;
            else continue;
            floating += (px - t.EntryPrice) * t.Dir * t.Qty * ppl * scale;
        }
        var eq = closed + floating;
        if (eq > peakEq) peakEq = eq;
        eqDd = Math.Max(eqDd, peakEq - eq);
        minVsStart = Math.Min(minVsStart, eq);
        var loss = dayStart - eq;
        if (!dayLoss.TryGetValue(d, out var prev) || loss > prev) dayLoss[d] = loss;
        if (loss > worstFtmo) { worstFtmo = loss; worstDay = d; }
    }

    // sui soli trade chiusi, per giornata di Praga
    var closedDays = trades.GroupBy(t => TimeZoneInfo.ConvertTimeFromUtc(t.Exit, prague).Date).OrderBy(g => g.Key)
        .Select(g => g.Sum(t => t.Net) * scale).ToList();
    double c2 = 0, pk = 0, cdd = 0;
    foreach (var v in closedDays) { c2 += v; pk = Math.Max(pk, c2); cdd = Math.Max(cdd, pk - c2); }
    return new Result(c2, cdd, eqDd, -closedDays.Min(), worstFtmo, worstDay, dayLoss.Values.Count(v => v > 2500), minVsStart);
}

void LoadFeed(string symbol, DateTime from, DateTime to)
{
    if (feeds.ContainsKey(symbol)) return;
    var list = new SortedList<long, (double, double, double)>();
    // intervallo fisso: la cache serve a tutti i run passati sulla riga di comando
    long a = Floor(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc)), z = Floor(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
    foreach (var line in File.ReadLines(Path.Combine(FeedDir, $"@{symbol}_1.json")))
    {
        if (!line.StartsWith("{\"timestamp\"")) continue;
        using var doc = JsonDocument.Parse(line.TrimEnd(',', ']', '}', ' ') + "}");
        var e = doc.RootElement;
        var ts = e.GetProperty("timestamp").GetInt64();
        if (ts < a || ts > z) continue;
        list[ts] = (e.GetProperty("high").GetDouble(), e.GetProperty("low").GetDouble(), e.GetProperty("close").GetDouble());
    }
    feeds[symbol] = list;
    Console.Error.WriteLine($"  feed {symbol}: {list.Count} minuti");
}

static long Floor(DateTime t) => new DateTimeOffset(t, TimeSpan.Zero).ToUnixTimeSeconds() / 60 * 60;
static double Median(IEnumerable<double> v) { var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }

static List<Trade> LoadTrades(string path)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    return doc.RootElement.EnumerateArray().Select(e => new Trade(
        e.GetProperty("symbol").GetString()!,
        e.GetProperty("direction").GetString() == "Buy" ? 1 : -1,
        e.GetProperty("quantity").GetDouble(),
        e.GetProperty("entryTimeUtc").GetDateTime().ToUniversalTime(),
        e.GetProperty("exitTimeUtc").GetDateTime().ToUniversalTime(),
        e.GetProperty("entryPrice").GetDouble(),
        e.GetProperty("exitPrice").GetDouble(),
        e.GetProperty("grossProfit").GetDouble(),
        e.GetProperty("netProfit").GetDouble())).ToList();
}

record Trade(string Symbol, int Dir, double Qty, DateTime Entry, DateTime Exit, double EntryPrice, double ExitPrice, double Gross, double Net);
record Result(double Net, double ClosedDd, double EquityDd, double WorstClosedDay, double WorstFtmoDay, string WorstDay, int DaysOver2500, double MinVsStart);
