// Drawdown giornaliero alla FTMO: saldo a mezzanotte di Praga contro l'equity piu' bassa della giornata,
// con le posizioni aperte valutate al minimo (long) o al massimo (short) di ogni minuto del feed FTMO.
// Colonna "gg equity": la regola sull'equity con l'ora del reset ignota (Fintokei, 30/09/2026). Per ognuna
// delle 24 ore UTC possibili la giornata parte dal maggiore fra saldo ed equity a quell'ora, e si tiene
// la perdita peggiore su tutte: un piano che ci sta sotto ci sta qualunque sia l'ora vera.
// Uso: ddgiornaliero "<trades.json>|<etichetta>|<scala>" ...
using System.Globalization;
using System.Text.Json;

const string FeedDir = @"C:\piootoo-dev\piootoo-repository\datafeed-external\FTMO";
var prague = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
var feeds = new Dictionary<string, SortedList<long, (double H, double L, double C)>>();

// Il feed si legge una volta sola, sull'intervallo di tutti i run della riga di comando.
var runs = args.Select(arg => arg.Split('|')).Select(parts => (Parts: parts, Trades: LoadTrades(parts[0]))).ToList();
var feedFrom = runs.Min(r => r.Trades.Min(t => t.Entry)).AddDays(-2);
var feedTo = runs.Max(r => r.Trades.Max(t => t.Exit)).AddDays(2);

Console.WriteLine($"{"piano",-30} {"trade",5} {"netto",9} {"DD chiuso",9} {"DD equity",9} {"gg chiuso",9} {"gg FTMO",9} {"giorno",-10} {"gg equity",9} {"giorno",-16} {"gg>2,5k",7} {"min vs 100k",11}");
foreach (var (parts, trades) in runs)
{
    var scale = parts.Length > 2 ? double.Parse(parts[2], CultureInfo.InvariantCulture) : 1.0;
    var r = Measure(trades, scale);
    Console.WriteLine($"{parts[1],-30} {trades.Count,5} {r.Net,9:N0} {r.ClosedDd,9:N0} {r.EquityDd,9:N0} {r.WorstClosedDay,9:N0} {r.WorstFtmoDay,9:N0} {r.WorstDay,-10} {r.WorstEquityDay,9:N0} {r.WorstEquityWhen,-16} {r.DaysOver2500,7} {r.MinVsStart,11:N0}");
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
    var byEntry = trades.OrderBy(t => t.Entry).ToList();
    var open = new List<Trade>();
    var nextEntry = 0;
    int nextExit = 0; double closed = 0, peakEq = 0, eqDd = 0, minVsStart = 0;
    string? day = null; double dayStart = 0, worstFtmo = 0; string worstDay = "";
    var dayLoss = new Dictionary<string, double>();
    var lastClose = new Dictionary<string, double>();
    // Regola sull'equity: una giornata per ogni ora UTC di reset possibile.
    var resetDay = new long[24];
    Array.Fill(resetDay, long.MinValue);
    var resetStart = new double[24];
    double worstEquity = 0; string worstEquityWhen = "";
    foreach (var m in minutes)
    {
        var mt = DateTimeOffset.FromUnixTimeSeconds(m).UtcDateTime;
        var d = TimeZoneInfo.ConvertTimeFromUtc(mt, prague).ToString("yyyy-MM-dd");
        while (nextExit < byExit.Count && byExit[nextExit].Exit < mt) closed += byExit[nextExit++].Net * scale;
        if (d != day) { day = d; dayStart = closed; }
        foreach (var s in feeds.Keys) if (feeds[s].TryGetValue(m, out var b)) lastClose[s] = b.C;

        // solo le posizioni aperte in questo minuto: entrate prima della sua fine, non ancora uscite
        while (nextEntry < byEntry.Count && byEntry[nextEntry].Entry < mt.AddMinutes(1)) open.Add(byEntry[nextEntry++]);
        open.RemoveAll(t => t.Exit < mt);
        double floating = 0;
        foreach (var t in open)
        {
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

        for (var h = 0; h < 24; h++)
        {
            var key = (m - h * 3600L) / 86400L;
            if (key != resetDay[h]) { resetDay[h] = key; resetStart[h] = Math.Max(closed, eq); }
            var equityLoss = resetStart[h] - eq;
            if (equityLoss > worstEquity) { worstEquity = equityLoss; worstEquityWhen = $"{mt:yyyy-MM-dd} h{h:00}"; }
        }
    }

    // sui soli trade chiusi, per giornata di Praga
    var closedDays = trades.GroupBy(t => TimeZoneInfo.ConvertTimeFromUtc(t.Exit, prague).Date).OrderBy(g => g.Key)
        .Select(g => g.Sum(t => t.Net) * scale).ToList();
    double c2 = 0, pk = 0, cdd = 0;
    foreach (var v in closedDays) { c2 += v; pk = Math.Max(pk, c2); cdd = Math.Max(cdd, pk - c2); }
    return new Result(c2, cdd, eqDd, -closedDays.Min(), worstFtmo, worstDay, worstEquity, worstEquityWhen,
        dayLoss.Values.Count(v => v > 2500), minVsStart);
}

void LoadFeed(string symbol, DateTime from, DateTime to)
{
    if (feeds.ContainsKey(symbol)) return;
    var list = new SortedList<long, (double, double, double)>();
    // intervallo comune a tutti i run passati sulla riga di comando: la cache serve a tutti
    long a = Floor(feedFrom), z = Floor(feedTo);
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
record Result(double Net, double ClosedDd, double EquityDd, double WorstClosedDay, double WorstFtmoDay, string WorstDay,
    double WorstEquityDay, string WorstEquityWhen, int DaysOver2500, double MinVsStart);
