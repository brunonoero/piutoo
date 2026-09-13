using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.MarketData;
using Piootoo.Strategies.Easy;
using Piootoo.Strategies.Easy.Engines;

#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8625, CS0618

namespace Piootoo.Core.Services.Compare;

/// <summary>
/// Confronto fra un run cBot cTrader e un backtest interno sullo stesso feed di broker: legge una
/// cartella <c>compare/compare-NNNN</c> e scrive <c>analisi/report.md</c> e i CSV.
///
/// <para>La cartella deve contenere <c>trades-cbot-*.json</c>, <c>trades-interno-*.json</c> e
/// <c>backtest-summary-*.json</c> (del run interno); facoltativi <c>run-*.json</c> (dichiara il broker
/// del feed), <c>log.txt</c> del cBot (senza, saltano spread, esiti e intent) e l'export "Events"
/// <c>*.xlsx</c> di cTrader (senza, salta la verifica delle uscite). Il feed a barre viene da
/// <c>datafeed-external/{broker}/</c>, risalendo dalla cartella compare; la finestra di confronto e'
/// la sovrapposizione degli ingressi delle due gambe.</para>
///
/// <para>Il corpo e' lo script di <c>compare/strumento-confronto</c> (commit 0877d43, nato per
/// compare-0033) portato qui senza cambiarne la logica, perche' lo usino sia la riga di comando sia
/// il server: il report deve restare identico riga per riga a quello dello script.</para>
/// </summary>
public static class CompareRunner
{
    /// <summary>Esegue il confronto e restituisce la cartella in cui ha scritto il report.</summary>
    /// <exception cref="DirectoryNotFoundException">Cartella compare o feed del broker assenti.</exception>
    /// <exception cref="FileNotFoundException">Manca uno dei file obbligatori.</exception>
    public static string Run(string compareFolder, string? outputFolder = null, string? feedBroker = null, TextWriter? output = null)
    {
        var console = output ?? TextWriter.Null;
        var ci = CultureInfo.InvariantCulture;
        if (!Directory.Exists(compareFolder))
            throw new DirectoryNotFoundException($"Cartella compare non trovata: {compareFolder}");

        var Cmp = Path.GetFullPath(compareFolder);
        var outDir = outputFolder ?? Path.Combine(Cmp, "analisi");
        Directory.CreateDirectory(outDir);

        var repositoryRoot = FindRepositoryRoot(Cmp);
        var broker = feedBroker ?? ReadBrokerFromRun(Cmp) ?? "FTMO";
        var Feed = Path.Combine(repositoryRoot, "datafeed-external", broker);
        if (!Directory.Exists(Feed))
            throw new DirectoryNotFoundException($"Feed non trovato: {Feed}");
        console.WriteLine($"cartella {Cmp}, feed {Feed}");

        // L'export eventi di cTrader e' uno zip: si scompatta accanto all'output.
        var xlsxDir = "";
        var xlsxFile = Directory.GetFiles(Cmp, "*.xlsx").FirstOrDefault();
        if (xlsxFile is not null)
        {
            xlsxDir = Path.Combine(outDir, "xlsx");
            if (!Directory.Exists(xlsxDir))
                System.IO.Compression.ZipFile.ExtractToDirectory(xlsxFile, xlsxDir);
        }

        string FirstFile(string pattern) =>
            Directory.GetFiles(Cmp, pattern).OrderBy(f => f).FirstOrDefault()
            ?? throw new FileNotFoundException($"Manca {pattern} in {Cmp}");

        // ----------------------------------------------------------------------------- trade
        var summary = JsonDocument.Parse(File.ReadAllText(FirstFile("backtest-summary-*.json")));

        // ----------------------------------------------------------------------------- moltiplicatori
        // Lotti broker per un contratto future: dalla tabella di conversione del piano, se il repository
        // la ha, altrimenti quella di cfd-ctrader-ftmo.
        var contractMultiplier = LoadContractMultipliers(repositoryRoot, summary) ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["BP"] = 0.625m, ["BTC"] = 5m, ["CC"] = 10m, ["CL"] = 10m, ["CT"] = 500m, ["ES"] = 50m,
            ["FDAX"] = 25m, ["GC"] = 1m, ["HK"] = 50m, ["HO"] = 420m, ["KC"] = 375m, ["NG"] = 10m,
            ["NQ"] = 20m, ["PL"] = 0.5m, ["SB"] = 1120m, ["YM"] = 5m,
        };

        var internalTrades = LoadTrades(FirstFile("trades-interno-*.json"), isCbot: false);
        var cbotTrades = LoadTrades(FirstFile("trades-cbot-*.json"), isCbot: true);
        console.WriteLine($"trade interni {internalTrades.Count}, cbot {cbotTrades.Count}");

        // Finestra di confronto: la sovrapposizione degli ingressi delle due gambe, a giorni interi.
        var overlapStart = new DateTime(Math.Max(internalTrades.Min(t => t.Entry).Date.Ticks, cbotTrades.Min(t => t.Entry).Date.Ticks), DateTimeKind.Utc);
        var overlapEnd = new DateTime(Math.Min(internalTrades.Max(t => t.Entry).Date.AddDays(1).Ticks, cbotTrades.Max(t => t.Entry).Date.AddDays(1).Ticks), DateTimeKind.Utc);
        if (summary.RootElement.TryGetProperty("requestedEndUtc", out var reqEnd) && reqEnd.ValueKind == JsonValueKind.String)
            overlapEnd = new DateTime(Math.Min(overlapEnd.Ticks, ParseUtc(reqEnd.GetString()!).Ticks), DateTimeKind.Utc);
        console.WriteLine($"sovrapposizione {overlapStart:yyyy-MM-dd} -> {overlapEnd:yyyy-MM-dd}");

        // ----------------------------------------------------------------------------- strategie
        var widened = summary.RootElement.GetProperty("fillConventions").GetProperty("stopMoneyWidenedStrategies")
            .EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stopMult = summary.RootElement.GetProperty("fillConventions").GetProperty("stopMoneyMultiplier").GetDecimal();
        var targetMult = summary.RootElement.GetProperty("fillConventions").GetProperty("targetMoneyMultiplier").GetDecimal();
        console.WriteLine($"stop x{stopMult}, target x{targetMult}, allargate {widened.Count}");

        var strategies = LoadStrategies(widened, stopMult, targetMult);
        console.WriteLine($"strategie nel catalogo: {strategies.Count}");

        // ----------------------------------------------------------------------------- feed (barre della strategia)
        var feeds = new Dictionary<string, Bar[]>(StringComparer.OrdinalIgnoreCase);
        Bar[] FeedFor(string symbol, int tf)
        {
            var key = $"{symbol}_{tf}";
            if (feeds.TryGetValue(key, out var bars)) return bars;
            var path = Path.Combine(Feed, $"@{symbol}_{tf}.json");
            if (!File.Exists(path)) { feeds[key] = Array.Empty<Bar>(); return feeds[key]; }
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var list = new List<Bar>();
            foreach (var c in doc.RootElement.GetProperty("candles").EnumerateArray())
            {
                list.Add(new Bar(
                    DateTime.Parse(c.GetProperty("dateTime").GetString()!, ci, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                    c.GetProperty("open").GetDecimal(), c.GetProperty("high").GetDecimal(),
                    c.GetProperty("low").GetDecimal(), c.GetProperty("close").GetDecimal()));
            }
            list.Sort((a, b) => a.Open.CompareTo(b.Open));
            feeds[key] = list.ToArray();
            return feeds[key];
        }

        // ----------------------------------------------------------------------------- feed a un minuto (solo gli istanti)
        var minuteSets = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);
        foreach (var sym in internalTrades.Select(t => t.Symbol).Distinct())
        {
            var path = Path.Combine(Feed, $"@{sym}_1.json");
            if (!File.Exists(path)) continue;
            var set = new HashSet<long>();
            var bytes = File.ReadAllBytes(path);
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { AllowTrailingCommas = true });
            var expect = false;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName) { expect = reader.ValueTextEquals("dateTime"); continue; }
                if (expect && reader.TokenType == JsonTokenType.String)
                {
                    var dt = DateTime.Parse(reader.GetString()!, ci, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                    set.Add(dt.Ticks / TimeSpan.TicksPerMinute);
                }
                expect = false;
            }
            minuteSets[sym] = set;
        }
        console.WriteLine($"feed 1m caricati: {minuteSets.Count} simboli");
        bool HasMinuteBar(string sym, DateTime t) => minuteSets.TryGetValue(sym, out var s) && s.Contains(t.Ticks / TimeSpan.TicksPerMinute);

        // ----------------------------------------------------------------------------- log cBot
        var logPath = Path.Combine(Cmp, "log.txt");
        var log = File.Exists(logPath) ? ParseLog(logPath) : new LogData();
        if (!File.Exists(logPath)) console.WriteLine("log.txt assente: spread, esiti e intent del cBot non disponibili");
        console.WriteLine($"log: chiusure {log.Closes.Count}, fill {log.Fills.Count}, scarti {log.Rejects.Count}, annullati {log.Cancels.Count}, bracket {log.Brackets.Count}");

        // join log → trade cbot
        JoinLog(cbotTrades, log);

        // ----------------------------------------------------------------------------- xlsx cTrader
        List<XPosition> xpos = new();
        if (!string.IsNullOrEmpty(xlsxDir) && Directory.Exists(xlsxDir))
        {
            xpos = ParseXlsx(xlsxDir);
            console.WriteLine($"xlsx: posizioni {xpos.Count}");
            JoinXlsx(cbotTrades, xpos, contractMultiplier);
        }

        // ----------------------------------------------------------------------------- confronto trade
        var intOverlap = internalTrades.Where(t => t.Entry >= overlapStart && t.Entry < overlapEnd).ToList();
        var cbOverlap = cbotTrades.Where(t => t.Entry >= overlapStart && t.Entry < overlapEnd).ToList();
        MatchTrades(intOverlap, cbOverlap, strategies);

        // ----------------------------------------------------------------------------- report
        var sb = new StringBuilder();
        WriteReport(sb);
        File.WriteAllText(Path.Combine(outDir, "report.md"), sb.ToString(), Encoding.UTF8);
        WriteCsvs();
        console.WriteLine("fatto: " + outDir);
        return outDir;

        // ============================================================================================
        // FUNZIONI
        // ============================================================================================

        // La radice di piootoo-repository: la prima cartella, risalendo, che contiene datafeed-external.
        static string FindRepositoryRoot(string start)
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
                if (Directory.Exists(Path.Combine(dir.FullName, "datafeed-external")))
                    return dir.FullName;
            throw new DirectoryNotFoundException($"Nessuna cartella datafeed-external risalendo da {start}");
        }

        // Il broker del feed lo dichiara run-cbot-*.json (PriceSource.Broker).
        static string? ReadBrokerFromRun(string cmp)
        {
            var run = Directory.GetFiles(cmp, "run-cbot-*.json").OrderBy(f => f).FirstOrDefault()
                      ?? Directory.GetFiles(cmp, "run-interno-*.json").OrderBy(f => f).FirstOrDefault();
            if (run is null) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(run));
            return doc.RootElement.TryGetProperty("PriceSource", out var ps) && ps.TryGetProperty("Broker", out var b)
                ? b.GetString()
                : null;
        }

        // Lotti broker per contratto future dalla tabella di conversione che il summary dichiara
        // (planUniverse.symbolConversionCode), letta da accounts/symbol-conversions.json.
        Dictionary<string, decimal>? LoadContractMultipliers(string repositoryRoot, JsonDocument summary)
        {
            if (!summary.RootElement.TryGetProperty("planUniverse", out var pu) ||
                !pu.TryGetProperty("symbolConversionCode", out var codeEl) || codeEl.ValueKind != JsonValueKind.String)
                return null;
            var code = codeEl.GetString();
            var path = Path.Combine(repositoryRoot, "accounts", "symbol-conversions.json");
            if (!File.Exists(path)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            // Il file e' un oggetto che avvolge l'elenco delle tabelle (o, nelle versioni vecchie, l'elenco stesso).
            var tables = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement
                : doc.RootElement.EnumerateObject().Select(p => p.Value).FirstOrDefault(v => v.ValueKind == JsonValueKind.Array);
            if (tables.ValueKind != JsonValueKind.Array) return null;
            foreach (var table in tables.EnumerateArray())
            {
                if (!string.Equals(table.GetProperty("Code").GetString(), code, StringComparison.OrdinalIgnoreCase)) continue;
                var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in table.GetProperty("Mappings").EnumerateArray())
                    map[m.GetProperty("Symbol").GetString()!.TrimStart('@').ToUpperInvariant()] = m.GetProperty("ContractMultiplier").GetDecimal();
                console.WriteLine($"conversione {code}: {map.Count} simboli");
                return map;
            }
            return null;
        }

        List<Trade> LoadTrades(string path, bool isCbot)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var list = new List<Trade>();
            var i = 0;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                var t = new Trade
                {
                    Idx = i++,
                    IsCbot = isCbot,
                    Strategy = e.GetProperty("strategyCode").GetString()!,
                    Symbol = e.GetProperty("symbol").GetString()!.TrimStart('@').ToUpperInvariant(),
                    Dir = e.GetProperty("direction").GetString() == "Buy" ? 1 : -1,
                    Qty = e.GetProperty("quantity").GetDecimal(),
                    Entry = ParseUtc(e.GetProperty("entryTimeUtc").GetString()!),
                    Exit = ParseUtc(e.GetProperty("exitTimeUtc").GetString()!),
                    EntryPx = e.GetProperty("entryPrice").GetDecimal(),
                    ExitPx = e.GetProperty("exitPrice").GetDecimal(),
                    Reason = e.GetProperty("exitReason").GetString() ?? "",
                    Gross = e.GetProperty("grossProfit").GetDecimal(),
                    Net = e.GetProperty("netProfit").GetDecimal(),
                    Commission = e.GetProperty("commission").GetDecimal(),
                    Swap = e.TryGetProperty("swap", out var sw) && sw.ValueKind == JsonValueKind.Number ? sw.GetDecimal() : 0m,
                };
                t.Contracts = isCbot ? t.Qty / contractMultiplier[t.Symbol] : t.Qty;
                list.Add(t);
            }
            return list;
        }

        DateTime ParseUtc(string s) => DateTime.Parse(s, ci, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        Dictionary<string, StratInfo> LoadStrategies(HashSet<string> widenedSet, decimal stopM, decimal targetM)
        {
            var result = new Dictionary<string, StratInfo>(StringComparer.OrdinalIgnoreCase);
            var asm = typeof(EasyEngineBase).Assembly;
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || !type.Name.StartsWith("PTS_", StringComparison.Ordinal)) continue;
                if (!typeof(ITradingStrategy).IsAssignableFrom(type)) continue;
                if (type.GetConstructor(Type.EmptyTypes) is null) continue;
                object inst;
                try { inst = Activator.CreateInstance(type)!; } catch (Exception ex) { console.WriteLine($"  {type.Name}: {ex.Message}"); continue; }
                var s = (ITradingStrategy)inst;
                var eng = inst as EasyEngineBase;
                var info = new StratInfo
                {
                    Code = s.Name,
                    Symbol = s.Symbol.TrimStart('@').ToUpperInvariant(),
                    Tf = s.TimeframeMinutes,
                    Engine = type.BaseType?.Name ?? "?",
                    Holding = s.Holding.ToString(),
                };
                if (eng is not null)
                {
                    info.Window = eng.TradingWindow;
                    info.Session = eng.Session;
                    info.WindowClock = (SessionClock)GetMember(eng, "WindowClock")!;
                    info.Clock = (SessionClock)GetMember(eng, "Clock")!;
                    info.StopMoney = (int)(GetMember(eng, "StopMoney") ?? 0);
                    info.ProfitMoney = (int)(GetMember(eng, "ProfitMoney") ?? 0);
                    info.BreakEvenMoney = (int)(GetMember(eng, "BreakEvenMoney") ?? 0);
                    info.TrailingMoney = (int)(GetMember(eng, "TrailingStopMoney") ?? 0);
                    info.MaxBars = (int)(GetMember(eng, "MaxBars") ?? 0);
                    info.MaxEntries = (int)(GetMember(eng, "MaxEntriesPerSession") ?? 0);
                    info.IntradayOnly = (bool)(GetMember(eng, "IntradayOnly") ?? false);
                    var skip = GetMember(eng, "SkipDay");
                    info.SkipDay = skip is int sd ? sd : -1;
                }
                info.PointValue = InstrumentRegistry.PointValue(info.Symbol);
                info.Grid = new SessionGrid(MarketCalendarRegistry.Current.Get(info.Symbol));
                var widenedHere = widenedSet.Contains(info.Code);
                info.Widened = widenedHere;
                info.StopMoneyEff = info.StopMoney * (widenedHere ? stopM : 1m);
                info.ProfitMoneyEff = info.ProfitMoney * (widenedHere ? targetM : 1m);
                info.StopPoints = info.PointValue > 0 ? info.StopMoneyEff / info.PointValue : 0m;
                info.TargetPoints = info.PointValue > 0 ? info.ProfitMoneyEff / info.PointValue : 0m;
                result[info.Code] = info;
            }
            return result;
        }

        object? GetMember(object obj, string name)
        {
            for (var t = obj.GetType(); t is not null; t = t.BaseType)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (f is not null) return f.GetValue(obj);
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (p is not null) return p.GetValue(obj);
            }
            return null;
        }

        LogData ParseLog(string path)
        {
            var d = new LogData();
            var reClose = new Regex(@"^(?<ts>\d\d/\d\d/\d{4} \d\d:\d\d:\d\d\.\d{3}) \| \w+ \| Chiuso (?<strat>\S+) (?<sym>\S+) (?<side>Buy|Sell): (?<entry>[\d.]+) -> (?<exit>[\d.]+) qty (?<qty>[\d.]+) \| esito (?<esito>.+?) \| lordo (?<gross>-?[\d.]+) commissioni (?<comm>-?[\d.]+) swap (?<swap>-?[\d.]+) netto (?<net>-?[\d.]+) \((?<pc>-?[\d.]+)/contratto\) \| MFE (?<mfe>-?[\d.]+) MAE (?<mae>-?[\d.]+) punti \| (?<min>\d+) min \((?<bars>\d+) barre\) \| trailing (?<tr>\d+)x", RegexOptions.Compiled);
            var reFill = new Regex(@"^(?<ts>\S+ \S+) \| \w+ \| Fill (?<strat>\S+) (?<sym>\S+): spread (?<spread>[\d.]+) punti(?: su stop (?<stop>[\d.]+) = (?<pct>[\d.]+)% del respiro)?", RegexOptions.Compiled);
            var reReject = new Regex(@"^(?<ts>\S+ \S+) \| \w+ \| Ingresso (?<sym>\S+)/(?<strat>\S+) scartato(?: per slippage)?[: ](?<motivo>.+?)\.?$", RegexOptions.Compiled);
            var reCancel = new Regex(@"^(?<ts>\S+ \S+) \| \w+ \| Ingresso (?<sym>\S+)/(?<strat>\S+) annullato: (?<motivo>.+?)\.?$", RegexOptions.Compiled);
            var reBracket = new Regex(@"^(?<ts>\S+ \S+) \| \w+ \| Bracket riancorato al fill (?<sym>\S+)/(?<strat>\S+): ingresso (?<fill>[\d.]+) \(richiesto (?<req>[\d.]+)\)", RegexOptions.Compiled);
            var reFlat = new Regex(@"chiusa per il flat di fine settimana", RegexOptions.Compiled);
            var reCap = new Regex(@"limite di ingressi per sessione raggiunto \((?<strat>\S+)/(?<sym>\S+) (?<side>Buy|Sell)", RegexOptions.Compiled);
            var reErr = new Regex(@"\| (Error|Info|Trade) \| (?<msg>(Errore|ATTENZIONE|Impossibile).*)$", RegexOptions.Compiled);
            var reNum = new Regex(@"[0-9][0-9.,:\-]*", RegexOptions.Compiled);
            var reNoIntent = new Regex(@"Nessun intent per l'account: (?<motivo>[^(]+)", RegexOptions.Compiled);
            var reIntent = new Regex(@"^(?<ts>\S+ \S+) \| \w+ \| Intent (?<sym>\S+)/(?<strat>\S+) (?<side>Buy|Sell) (?<type>Stop|Limit|Market): .*\| ritardo (?<rit>-?[\d.]+)s \| attesa (?<att>-?[\d.]+)s", RegexOptions.Compiled);

            using var rd = new StreamReader(path, Encoding.UTF8);
            string? line;
            long n = 0;
            while ((line = rd.ReadLine()) is not null)
            {
                n++;
                if (line.Length < 30) continue;
                Match m;
                if (line.Contains("| Chiuso PTS_"))
                {
                    m = reClose.Match(line);
                    if (m.Success)
                    {
                        d.Closes.Add(new LogClose(Ts(m), m.Groups["strat"].Value, m.Groups["sym"].Value,
                            m.Groups["side"].Value == "Buy" ? 1 : -1, Dec(m, "entry"), Dec(m, "exit"), Dec(m, "qty"),
                            m.Groups["esito"].Value.Trim(), Dec(m, "gross"), Dec(m, "comm"), Dec(m, "swap"), Dec(m, "net"),
                            Dec(m, "mfe"), Dec(m, "mae"), int.Parse(m.Groups["min"].Value), int.Parse(m.Groups["bars"].Value),
                            int.Parse(m.Groups["tr"].Value)));
                    }
                    else d.Unparsed++;
                    continue;
                }
                if (line.Contains("| Intent ") && line.Contains("| attesa "))
                {
                    m = reIntent.Match(line);
                    if (m.Success)
                        d.Intents.Add(new LogIntent(Ts(m), m.Groups["strat"].Value, m.Groups["sym"].Value, m.Groups["type"].Value,
                            double.Parse(m.Groups["rit"].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups["att"].Value, CultureInfo.InvariantCulture)));
                    continue;
                }
                if (line.Contains("| Fill PTS_"))
                {
                    m = reFill.Match(line);
                    if (m.Success)
                        d.Fills.Add(new LogFill(Ts(m), m.Groups["strat"].Value, m.Groups["sym"].Value, Dec(m, "spread"),
                            m.Groups["stop"].Success ? Dec(m, "stop") : null));
                    else d.Unparsed++;
                    continue;
                }
                if (line.Contains(" scartato"))
                {
                    m = reReject.Match(line);
                    if (m.Success)
                    {
                        var motivo = m.Groups["motivo"].Value;
                        var cls = motivo.Contains("lato sbagliato") ? "lato sbagliato"
                            : motivo.StartsWith("attivazione") ? "attivazione oltre la barra"
                            : motivo.StartsWith("spread") ? "spread sullo stop"
                            : motivo.Contains("dal mercato") ? "livello troppo lontano"
                            : motivo.Contains("flat") ? "finestra flat weekend"
                            : motivo.Contains("slippage") || line.Contains("per slippage") ? "slippage market"
                            : "altro";
                        d.Rejects.Add(new LogEvent(Ts(m), m.Groups["strat"].Value, m.Groups["sym"].Value, cls, motivo));
                    }
                    else d.Unparsed++;
                    continue;
                }
                if (line.Contains(" annullato: "))
                {
                    m = reCancel.Match(line);
                    if (m.Success)
                    {
                        var motivo = m.Groups["motivo"].Value;
                        var cls = motivo.Contains("posizione") ? "posizione già aperta" : motivo.Contains("massimo") ? "tetto posizioni" : "altro";
                        d.Cancels.Add(new LogEvent(Ts(m), m.Groups["strat"].Value, m.Groups["sym"].Value, cls, motivo));
                    }
                    else d.Unparsed++;
                    continue;
                }
                if (line.Contains("Bracket riancorato"))
                {
                    m = reBracket.Match(line);
                    if (m.Success)
                        d.Brackets.Add(new LogBracket(Ts(m), m.Groups["strat"].Value, m.Groups["sym"].Value, Dec(m, "fill"), Dec(m, "req")));
                    continue;
                }
                if (reFlat.IsMatch(line)) { d.FlatCloses++; continue; }
                if (line.Contains("limite di ingressi per sessione"))
                {
                    m = reCap.Match(line);
                    if (m.Success) Inc(d.SessionCap, m.Groups["strat"].Value + " " + m.Groups["side"].Value);
                    continue;
                }
                if (line.Contains("Nessun intent per l'account"))
                {
                    m = reNoIntent.Match(line);
                    if (m.Success) Inc(d.NoIntent, m.Groups["motivo"].Value.Trim());
                    continue;
                }
                m = reErr.Match(line);
                if (m.Success)
                {
                    var key = reNum.Replace(m.Groups["msg"].Value, "#");
                    if (key.Length > 110) key = key[..110];
                    Inc(d.Warnings, key);
                }
            }
            d.Lines = n;
            return d;

            static DateTime Ts(Match m) => DateTime.ParseExact(m.Groups["ts"].Value, "dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            static decimal Dec(Match m, string g) => decimal.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture);
        }

        void Inc<TK>(Dictionary<TK, int> d, TK k) where TK : notnull { d[k] = d.TryGetValue(k, out var v) ? v + 1 : 1; }

        void JoinLog(List<Trade> trades, LogData d)
        {
            var byStrat = trades.GroupBy(t => t.Strategy).ToDictionary(g => g.Key, g => g.OrderBy(t => t.Entry).ToList(), StringComparer.OrdinalIgnoreCase);
            // chiusure → per strategia, match su exit time (±10 s) e prezzo di ingresso
            foreach (var c in d.Closes)
            {
                if (!byStrat.TryGetValue(c.Strategy, out var list)) continue;
                Trade? best = null; double bestD = double.MaxValue;
                foreach (var t in list)
                {
                    if (t.LogClose is not null) continue;
                    if (t.Dir != c.Dir) continue;
                    var dd = Math.Abs((t.Exit - c.Ts).TotalSeconds);
                    if (dd > 10) continue;
                    if (Math.Abs(t.EntryPx - c.EntryPx) > 0.0001m * Math.Max(1m, t.EntryPx)) continue;
                    if (dd < bestD) { bestD = dd; best = t; }
                }
                if (best is not null) best.LogClose = c; else d.ClosesUnmatched++;
            }
            foreach (var f in d.Fills)
            {
                if (!byStrat.TryGetValue(f.Strategy, out var list)) continue;
                Trade? best = null; double bestD = double.MaxValue;
                foreach (var t in list)
                {
                    if (t.SpreadAtFill is not null) continue;
                    var dd = Math.Abs((t.Entry - f.Ts).TotalSeconds);
                    if (dd > 10) continue;
                    if (dd < bestD) { bestD = dd; best = t; }
                }
                if (best is not null) { best.SpreadAtFill = f.Spread; best.StopAtFill = f.Stop; } else d.FillsUnmatched++;
            }
            // intent → trade: l'ultimo intent della stessa strategia ricevuto prima del fill, entro due barre
            var intentsByStrat = d.Intents.GroupBy(i => i.Strategy).ToDictionary(g => g.Key, g => g.OrderBy(i => i.Ts).ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (var t in trades)
            {
                if (!intentsByStrat.TryGetValue(t.Strategy, out var list)) continue;
                var tf = strategies.TryGetValue(t.Strategy, out var s) ? s.Tf : 60;
                var lo = t.Entry.AddMinutes(-2 * tf - 1);
                LogIntent? best = null;
                foreach (var i in list)
                {
                    if (i.Ts > t.Entry) break;
                    if (i.Ts >= lo) best = i;
                }
                if (best is not null) { t.IntentAttesa = best.Attesa; t.IntentType = best.Type; }
            }
            foreach (var b in d.Brackets)
            {
                if (!byStrat.TryGetValue(b.Strategy, out var list)) continue;
                Trade? best = null; double bestD = double.MaxValue;
                foreach (var t in list)
                {
                    if (t.RequestedEntry is not null) continue;
                    var dd = Math.Abs((t.Entry - b.Ts).TotalSeconds);
                    if (dd > 10) continue;
                    if (Math.Abs(t.EntryPx - b.Fill) > 0.0001m * Math.Max(1m, t.EntryPx)) continue;
                    if (dd < bestD) { bestD = dd; best = t; }
                }
                if (best is not null) best.RequestedEntry = b.Requested;
            }
        }

        List<XPosition> ParseXlsx(string dir)
        {
            // shared strings
            var shared = new List<string>();
            using (var xr = XmlReader.Create(Path.Combine(dir, "xl", "sharedStrings.xml")))
            {
                while (xr.Read())
                {
                    if (xr.NodeType == XmlNodeType.Element && xr.Name == "si")
                    {
                        var sbx = new StringBuilder();
                        using var sub = xr.ReadSubtree();
                        while (sub.Read())
                            if (sub.NodeType == XmlNodeType.Element && sub.Name == "t") sbx.Append(sub.ReadElementContentAsString());
                        shared.Add(sbx.ToString());
                    }
                }
            }
            var positions = new Dictionary<long, XPosition>();
            var header = new Dictionary<string, string>();
            using (var xr = XmlReader.Create(Path.Combine(dir, "xl", "worksheets", "sheet1.xml")))
            {
                while (xr.Read())
                {
                    if (xr.NodeType != XmlNodeType.Element || xr.Name != "row") continue;
                    var rowNum = int.Parse(xr.GetAttribute("r")!);
                    var cells = new Dictionary<string, string>();
                    using (var sub = xr.ReadSubtree())
                    {
                        while (sub.Read())
                        {
                            if (sub.NodeType != XmlNodeType.Element || sub.Name != "c") continue;
                            var r = sub.GetAttribute("r")!;
                            var col = new string(r.TakeWhile(char.IsLetter).ToArray());
                            var type = sub.GetAttribute("t");
                            string? v = null;
                            using (var cs = sub.ReadSubtree())
                                while (cs.Read())
                                    if (cs.NodeType == XmlNodeType.Element && cs.Name == "v") v = cs.ReadElementContentAsString();
                            if (v is null) continue;
                            cells[col] = type == "s" ? shared[int.Parse(v)] : v;
                        }
                    }
                    if (rowNum == 1) { foreach (var kv in cells) header[kv.Key] = kv.Value; continue; }
                    string G(string name) { var col = header.FirstOrDefault(h => h.Value == name).Key; return col is not null && cells.TryGetValue(col, out var s) ? s : ""; }
                    var ev = G("Event");
                    if (!long.TryParse(G("Position ID"), out var pid) || pid <= 0) continue;
                    if (!positions.TryGetValue(pid, out var p)) { p = new XPosition { Id = pid }; positions[pid] = p; }
                    var time = ParseXTime(G("Time (UTC+0)"));
                    switch (ev)
                    {
                        case "Create Position":
                            p.OpenTime = time; p.Type = G("Type"); p.VolumeLots = XNum(G("Volume"));
                            p.EntryPx = XNum(G("Entry price")); p.Sl = XNum(G("SL")); p.Tp = XNum(G("TP"));
                            break;
                        case "Position Modified (S/L)":
                        case "Position Modified (T/P)":
                        case "Position Modified (S/L, T/P)":
                            p.Modifications++;
                            break;
                        case "Stop Loss Hit":
                        case "Take Profit Hit":
                        case "Position closed":
                            p.CloseTime = time; p.CloseEvent = ev; p.ClosePx = XNum(G("Closing price"));
                            p.Gross = XNum(G("Gross profit")); p.Balance = XNum(G("Balance"));
                            break;
                    }
                }
            }
            return positions.Values.OrderBy(p => p.OpenTime).ToList();

            static DateTime ParseXTime(string s) =>
                DateTime.TryParseExact(s, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var t) ? t : DateTime.MinValue;
            static decimal? XNum(string s)
            {
                if (string.IsNullOrWhiteSpace(s) || s == "-") return null;
                var clean = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
                return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
            }
        }

        void JoinXlsx(List<Trade> trades, List<XPosition> positions, Dictionary<string, decimal> mult)
        {
            var sorted = trades.OrderBy(t => t.Entry).ToList();
            foreach (var p in positions)
            {
                if (p.OpenTime == DateTime.MinValue) continue;
                var dir = p.Type == "Buy" ? 1 : -1;
                Trade? best = null; double bestD = double.MaxValue;
                foreach (var t in sorted)
                {
                    if (t.XPos is not null || t.Dir != dir) continue;
                    var dd = (t.Entry - p.OpenTime).TotalSeconds;
                    if (dd < -60 || dd > 120) continue;
                    if (p.EntryPx is { } px && Math.Abs(t.EntryPx - px) > 0.0001m * Math.Max(1m, t.EntryPx)) continue;
                    // piu' strategie entrano sullo stesso livello nello stesso minuto: la chiusura distingue
                    var score = Math.Abs(dd);
                    if (p.CloseTime != DateTime.MinValue)
                    {
                        var dc = Math.Abs((t.Exit - p.CloseTime).TotalSeconds);
                        if (dc > 120) continue;
                        score += dc;
                        if (p.ClosePx is { } cp && Math.Abs(t.ExitPx - cp) > 0.0001m * Math.Max(1m, t.ExitPx)) score += 1000;
                    }
                    if (score < bestD) { bestD = score; best = t; }
                }
                if (best is not null) { best.XPos = p; p.Trade = best; }
            }
        }

        void MatchTrades(List<Trade> ints, List<Trade> cbs, Dictionary<string, StratInfo> strats)
        {
            var cbByKey = cbs.GroupBy(t => t.Strategy + "|" + t.Dir).ToDictionary(g => g.Key, g => g.OrderBy(t => t.Entry).ToList());
            foreach (var t in ints.OrderBy(t => t.Entry))
            {
                var tf = strats.TryGetValue(t.Strategy, out var s) ? s.Tf : 60;
                var tol = TimeSpan.FromMinutes(tf + 1);
                if (!cbByKey.TryGetValue(t.Strategy + "|" + t.Dir, out var cands)) continue;
                Trade? best = null; var bestD = TimeSpan.MaxValue;
                foreach (var c in cands)
                {
                    if (c.Match is not null) continue;
                    var dd = (c.Entry - t.Entry).Duration();
                    if (dd > tol) continue;
                    if (dd < bestD) { bestD = dd; best = c; }
                }
                if (best is not null) { t.Match = best; best.Match = t; }
            }
        }

        // ----------------------------------------------------------------------------- orari a testo
        // La fine di giornata (ZonedWindow.EndOfDay) si scrive 24:00: e' la mezzanotte successiva.
        static string TimeText(TimeOnly? time) =>
            time is null ? "-" : time.Value == ZonedWindow.EndOfDay ? "24:00" : time.Value.ToString("HH\\:mm");
        static string WindowText(ZonedWindow w) => $"{TimeText(w.Start)}-{TimeText(w.End)}";

        // ----------------------------------------------------------------------------- verifiche per trade
        (bool inWindow, TimeOnly? label, bool skipped, string note) WindowCheck(Trade t, StratInfo s)
        {
            var bars = FeedFor(s.Symbol, s.Tf);
            if (bars.Length == 0) return (true, null, false, "feed assente");
            // barra che contiene il fill
            DateTime bucket;
            try { bucket = s.Grid.BucketStartUtc(t.Entry, s.Tf); } catch { bucket = t.Entry; }
            var idx = Array.BinarySearch(bars.Select(b => b.Open).ToArray(), bucket);
            if (idx < 0) idx = ~idx - 1; // ultima barra con apertura <= bucket
            if (idx <= 0) return (true, null, false, "prima barra");
            var fillBar = bars[idx];
            var prev = bars[idx - 1];
            // se il fill cade in un buco (nessuna barra apre nel bucket) la barra di segnale è comunque la precedente
            var label = s.WindowClock.BarLabelTime(prev.Open, s.Tf);
            var inWin = s.Window is null || EasyLib.TimeWindowInclusive(s.Window.Start, s.Window.End, label);
            var skipped = s.SkipDay >= 0 && (((int)s.WindowClock.BarLabelDay(prev.Open, s.Tf).DayOfWeek + 6) % 7) == s.SkipDay;
            var note = fillBar.Open != bucket ? "fill fuori griglia" : "";
            return (inWin, label, skipped, note);
        }

        // ----------------------------------------------------------------------------- report
        void WriteReport(StringBuilder o)
        {
            o.AppendLine($"# {Path.GetFileName(Cmp)} — cBot cTrader contro backtest interno (feed {broker})");
            o.AppendLine();
            o.AppendLine($"Generato: {DateTime.UtcNow:yyyy-MM-dd HH:mm}Z. Finestra di sovrapposizione usata per il confronto trade: `{overlapStart:yyyy-MM-dd}` → `{overlapEnd:yyyy-MM-dd}` (ingressi).");
            o.AppendLine();

            // ---- 0. setup
            o.AppendLine("## 0. Perimetro dei due run");
            o.AppendLine();
            o.AppendLine("| | interno | cBot |");
            o.AppendLine("|---|---|---|");
            o.AppendLine($"| trade totali | {internalTrades.Count} | {cbotTrades.Count} |");
            o.AppendLine($"| primo ingresso | {internalTrades.Min(t => t.Entry):yyyy-MM-dd HH:mm} | {cbotTrades.Min(t => t.Entry):yyyy-MM-dd HH:mm} |");
            o.AppendLine($"| ultimo ingresso | {internalTrades.Max(t => t.Entry):yyyy-MM-dd HH:mm} | {cbotTrades.Max(t => t.Entry):yyyy-MM-dd HH:mm} |");
            o.AppendLine($"| trade nella sovrapposizione | {intOverlap.Count} | {cbOverlap.Count} |");
            o.AppendLine($"| contratti per trade | {internalTrades.Select(t => t.Contracts).Distinct().Count()} valori: {string.Join(",", internalTrades.Select(t => t.Contracts).Distinct().Take(5))} | {cbotTrades.Select(t => t.Contracts).Distinct().Count()} valori: {string.Join(",", cbotTrades.Select(t => Math.Round(t.Contracts, 4)).Distinct().Take(5))} |");
            o.AppendLine($"| netto totale | {internalTrades.Sum(t => t.Net):N0} | {cbotTrades.Sum(t => t.Net):N0} |");
            o.AppendLine($"| netto per contratto, sovrapposizione | {intOverlap.Sum(t => t.NetPerContract):N0} | {cbOverlap.Sum(t => t.NetPerContract):N0} |");
            o.AppendLine($"| lordo per contratto, sovrapposizione | {intOverlap.Sum(t => t.GrossPerContract):N0} | {cbOverlap.Sum(t => t.GrossPerContract):N0} |");
            o.AppendLine($"| commissioni per contratto, sovrapposizione | {intOverlap.Sum(t => t.Commission / t.Contracts):N0} | {cbOverlap.Sum(t => t.Commission / t.Contracts):N0} |");
            o.AppendLine($"| swap, sovrapposizione (grezzo) | {intOverlap.Sum(t => t.Swap):N0} | {cbOverlap.Sum(t => t.Swap):N0} |");
            o.AppendLine();
            o.AppendLine($"Log cBot: {log.Lines:N0} righe; chiusure {log.Closes.Count}, fill con spread {log.Fills.Count}, ingressi scartati {log.Rejects.Count}, annullati {log.Cancels.Count}, bracket riancorati {log.Brackets.Count}, chiusure per flat weekend {log.FlatCloses}, righe non interpretate {log.Unparsed}.");
            o.AppendLine($"Chiusure del log senza trade corrispondente: {log.ClosesUnmatched}; fill senza trade: {log.FillsUnmatched}.");
            o.AppendLine();
            o.AppendLine("### 0b. Commissioni per contratto e per trade (sovrapposizione)");
            o.AppendLine();
            o.AppendLine("| simbolo | int trade | int comm/trade/ctr | cBot trade | cBot comm/trade/ctr | cBot swap/trade/ctr | cBot lordo/ctr | cBot netto/ctr |");
            o.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");
            foreach (var sym in intOverlap.Select(t => t.Symbol).Union(cbOverlap.Select(t => t.Symbol)).Distinct().OrderBy(x => x))
            {
                var a = intOverlap.Where(t => t.Symbol == sym).ToList();
                var b = cbOverlap.Where(t => t.Symbol == sym).ToList();
                o.AppendLine($"| {sym} | {a.Count} | {(a.Count > 0 ? a.Average(t => t.Commission / t.Contracts) : 0):N1} | {b.Count} | {(b.Count > 0 ? b.Average(t => t.Commission / t.Contracts) : 0):N1} | {(b.Count > 0 ? b.Average(t => t.Swap / t.Contracts) : 0):N1} | {b.Sum(t => t.GrossPerContract):N0} | {b.Sum(t => t.NetPerContract):N0} |");
            }
            o.AppendLine();
            // FDAX: conversione EUR->USD
            var fdaxPairs = intOverlap.Where(t => t.Match is not null && t.Symbol == "FDAX" && Math.Abs((t.Match!.Exit - t.Exit).TotalMinutes) <= 2 && t.Gross != 0 && Math.Sign(t.Gross) == Math.Sign(t.Match!.Gross)).ToList();
            if (fdaxPairs.Count > 0)
                o.AppendLine($"FDAX: rapporto lordo cBot / lordo interno sulle coppie con stessa uscita (n={fdaxPairs.Count}): mediana {Median(fdaxPairs.Select(t => (double)(t.Match!.GrossPerContract / t.GrossPerContract)).ToList()):0.000} — il conto cTrader è in USD e GER40 quota in EUR, l'interno usa 25 EUR/punto senza conversione.");
            o.AppendLine();

            // ---- 1. differenze trade per strategia
            o.AppendLine("## 1. Differenze di trade per strategia (finestra di sovrapposizione)");
            o.AppendLine();
            o.AppendLine("Match = stessa strategia, stesso verso, ingresso entro una barra della strategia. Netto per contratto future.");
            o.AppendLine();
            o.AppendLine("| strategia | tf | int | cbot | match | solo int | solo cbot | Δentry mediano (min) | netto/ctr int | netto/ctr cbot | Δ netto/ctr | int in match (netto) | cbot in match (netto) | int in match (lordo) | cbot in match (lordo) |");
            o.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            var perStrat = new List<StratRow>();
            var allCodes = intOverlap.Select(t => t.Strategy).Union(cbOverlap.Select(t => t.Strategy)).Distinct().OrderBy(x => x).ToList();
            foreach (var code in allCodes)
            {
                var a = intOverlap.Where(t => t.Strategy == code).ToList();
                var b = cbOverlap.Where(t => t.Strategy == code).ToList();
                var matched = a.Where(t => t.Match is not null).ToList();
                var deltas = matched.Select(t => (t.Match!.Entry - t.Entry).TotalMinutes).OrderBy(x => x).ToList();
                var row = new StratRow
                {
                    Code = code, Tf = strategies.TryGetValue(code, out var s) ? s.Tf : 0,
                    NInt = a.Count, NCbot = b.Count, Matched = matched.Count,
                    OnlyInt = a.Count - matched.Count, OnlyCbot = b.Count - matched.Count,
                    MedianDelta = deltas.Count > 0 ? deltas[deltas.Count / 2] : double.NaN,
                    NetInt = a.Sum(t => t.NetPerContract), NetCbot = b.Sum(t => t.NetPerContract),
                    NetIntMatched = matched.Sum(t => t.NetPerContract), NetCbotMatched = matched.Sum(t => t.Match!.NetPerContract),
                    GrossIntMatched = matched.Sum(t => t.GrossPerContract), GrossCbotMatched = matched.Sum(t => t.Match!.GrossPerContract),
                };
                perStrat.Add(row);
                o.AppendLine($"| {code} | {row.Tf} | {row.NInt} | {row.NCbot} | {row.Matched} | {row.OnlyInt} | {row.OnlyCbot} | {(double.IsNaN(row.MedianDelta) ? "-" : row.MedianDelta.ToString("0.0", ci))} | {row.NetInt:N0} | {row.NetCbot:N0} | {row.NetCbot - row.NetInt:N0} | {row.NetIntMatched:N0} | {row.NetCbotMatched:N0} | {row.GrossIntMatched:N0} | {row.GrossCbotMatched:N0} |");
            }
            o.AppendLine($"| **TOTALE** | | {perStrat.Sum(r => r.NInt)} | {perStrat.Sum(r => r.NCbot)} | {perStrat.Sum(r => r.Matched)} | {perStrat.Sum(r => r.OnlyInt)} | {perStrat.Sum(r => r.OnlyCbot)} | | {perStrat.Sum(r => r.NetInt):N0} | {perStrat.Sum(r => r.NetCbot):N0} | {perStrat.Sum(r => r.NetCbot - r.NetInt):N0} | {perStrat.Sum(r => r.NetIntMatched):N0} | {perStrat.Sum(r => r.NetCbotMatched):N0} | {perStrat.Sum(r => r.GrossIntMatched):N0} | {perStrat.Sum(r => r.GrossCbotMatched):N0} |");
            o.AppendLine();

            // distribuzione Δentry
            var allMatched = intOverlap.Where(t => t.Match is not null).ToList();
            var dAbs = allMatched.Select(t => Math.Abs((t.Match!.Entry - t.Entry).TotalMinutes)).ToList();
            o.AppendLine($"Trade abbinati: {allMatched.Count}. Scarto di ingresso: ≤1 min {dAbs.Count(x => x <= 1)}, ≤5 min {dAbs.Count(x => x <= 5)}, ≤15 min {dAbs.Count(x => x <= 15)}, oltre {dAbs.Count(x => x > 15)}.");
            var pxLong = allMatched.Where(t => t.Dir > 0).Select(t => t.Match!.EntryPx - t.EntryPx).ToList();
            var pxShort = allMatched.Where(t => t.Dir < 0).Select(t => t.EntryPx - t.Match!.EntryPx).ToList();
            o.AppendLine($"Prezzo di ingresso cBot peggiore dell'interno (punti, positivo = cBot paga di più): long mediana {MedianD(pxLong):0.####} media {Avg(pxLong):0.####} (n={pxLong.Count}); short mediana {MedianD(pxShort):0.####} media {Avg(pxShort):0.####} (n={pxShort.Count}).");
            o.AppendLine();

            // ---- 1b. cause dei solo-interno
            o.AppendLine("### 1b. Perché un trade interno non esiste sul cBot");
            o.AppendLine();
            var onlyInt = intOverlap.Where(t => t.Match is null).ToList();
            var causes = new Dictionary<string, int>();
            var causesByStrat = new Dictionary<string, Dictionary<string, int>>();
            var cbSorted = cbotTrades.OrderBy(t => t.Entry).ToList();
            foreach (var t in onlyInt)
            {
                var tf = strategies.TryGetValue(t.Strategy, out var s) ? s.Tf : 60;
                var lo = t.Entry.AddMinutes(-tf - 1); var hi = t.Entry.AddMinutes(tf + 1);
                string cause;
                var rej = log.Rejects.FirstOrDefault(r => r.Strategy == t.Strategy && r.Ts >= lo && r.Ts <= hi);
                var can = log.Cancels.FirstOrDefault(r => r.Strategy == t.Strategy && r.Ts >= lo && r.Ts <= hi);
                var cbOpen = cbSorted.Any(c => c.Strategy == t.Strategy && c.Entry < t.Entry && c.Exit > t.Entry);
                var intPrevOpp = t.Reason == "OppositeSignal";
                if (rej is not null) cause = "cBot scartato: " + rej.Class;
                else if (can is not null) cause = "cBot annullato: " + can.Class;
                else if (cbOpen) cause = "cBot già in posizione (nessuna inversione)";
                else cause = "nessun evento nel log (livello non toccato o intent non consegnato)";
                t.Cause = cause;
                Inc(causes, cause);
                if (!causesByStrat.TryGetValue(t.Strategy, out var cd)) causesByStrat[t.Strategy] = cd = new();
                Inc(cd, cause);
            }
            o.AppendLine("| causa | trade | netto/ctr interno di quei trade |");
            o.AppendLine("|---|---:|---:|");
            foreach (var kv in causes.OrderByDescending(k => k.Value))
                o.AppendLine($"| {kv.Key} | {kv.Value} | {onlyInt.Where(t => t.Cause == kv.Key).Sum(t => t.NetPerContract):N0} |");
            o.AppendLine();
            o.AppendLine("Uscite per segnale opposto nell'interno (inversione che il cBot non fa): " + intOverlap.Count(t => t.Reason == "OppositeSignal") + " trade chiusi così, netto/ctr " + intOverlap.Where(t => t.Reason == "OppositeSignal").Sum(t => t.NetPerContract).ToString("N0") + ".");
            o.AppendLine();
            var onlyCb = cbOverlap.Where(t => t.Match is null).ToList();
            var intSorted = internalTrades.OrderBy(t => t.Entry).ToList();
            int cbOnlyIntOpen = 0;
            foreach (var c in onlyCb)
                if (intSorted.Any(i => i.Strategy == c.Strategy && i.Entry < c.Entry && i.Exit > c.Entry)) { c.Cause = "interno già in posizione"; cbOnlyIntOpen++; } else c.Cause = "interno flat: livello non toccato / scartato";
            o.AppendLine($"Trade solo cBot: {onlyCb.Count}, di cui {cbOnlyIntOpen} mentre l'interno era già in posizione sulla stessa strategia; netto/ctr dei solo-cBot {onlyCb.Sum(t => t.NetPerContract):N0}.");
            o.AppendLine();

            // ---- 2. uscite
            o.AppendLine("## 2. Uscite");
            o.AppendLine();
            o.AppendLine("### 2a. Motivo di uscita dichiarato in trades.json");
            o.AppendLine();
            o.AppendLine("| interno | n | | cBot | n |");
            o.AppendLine("|---|---:|---|---|---:|");
            var ri = intOverlap.GroupBy(t => t.Reason).OrderByDescending(g => g.Count()).ToList();
            var rc = cbOverlap.GroupBy(t => t.Reason).OrderByDescending(g => g.Count()).ToList();
            for (var i = 0; i < Math.Max(ri.Count, rc.Count); i++)
                o.AppendLine($"| {(i < ri.Count ? ri[i].Key : "")} | {(i < ri.Count ? ri[i].Count().ToString() : "")} | | {(i < rc.Count ? rc[i].Key : "")} | {(i < rc.Count ? rc[i].Count().ToString() : "")} |");
            o.AppendLine();
            o.AppendLine("### 2b. Esito reale dal log del cBot (riga `Chiuso`)");
            o.AppendLine();
            o.AppendLine("| esito log | n | netto/ctr | di cui trades.json=Closed | StopLoss | TakeProfit |");
            o.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (var g in cbOverlap.GroupBy(t => t.LogClose?.Esito ?? "(senza riga Chiuso)").OrderByDescending(g => g.Count()))
                o.AppendLine($"| {g.Key} | {g.Count()} | {g.Sum(t => t.NetPerContract):N0} | {g.Count(t => t.Reason.EndsWith("Closed"))} | {g.Count(t => t.Reason.EndsWith("StopLoss"))} | {g.Count(t => t.Reason.EndsWith("TakeProfit"))} |");
            o.AppendLine();
            o.AppendLine("### 2c. Trade abbinati: motivo interno × esito cBot");
            o.AppendLine();
            var pairs = allMatched.Select(t => (i: t.Reason, c: t.Match!.LogClose?.Esito ?? t.Match!.Reason)).ToList();
            var cReasons = pairs.Select(p => p.c).Distinct().OrderBy(x => x).ToList();
            o.AppendLine("| interno \\ cBot | " + string.Join(" | ", cReasons) + " | tot |");
            o.AppendLine("|---|" + string.Join("", cReasons.Select(_ => "---:|")) + "---:|");
            foreach (var ir in pairs.Select(p => p.i).Distinct().OrderBy(x => x))
                o.AppendLine($"| {ir} | " + string.Join(" | ", cReasons.Select(c => pairs.Count(p => p.i == ir && p.c == c))) + $" | {pairs.Count(p => p.i == ir)} |");
            o.AppendLine();
            // stesso trade, esiti diversi: impatto
            o.AppendLine("Impatto sui trade abbinati (netto/ctr cBot − interno) per coppia di esiti, prime 12 per valore assoluto:");
            o.AppendLine();
            o.AppendLine("| interno | cBot | n | Δ netto/ctr totale | Δ medio | Δ uscita mediano (min) |");
            o.AppendLine("|---|---|---:|---:|---:|---:|");
            foreach (var g in allMatched.GroupBy(t => (t.Reason, t.Match!.LogClose?.Esito ?? t.Match!.Reason))
                         .Select(g => (g.Key, n: g.Count(), d: g.Sum(t => t.Match!.NetPerContract - t.NetPerContract), dm: Median(g.Select(t => (t.Match!.Exit - t.Exit).TotalMinutes).ToList())))
                         .OrderByDescending(x => Math.Abs(x.d)).Take(12))
                o.AppendLine($"| {g.Key.Item1} | {g.Key.Item2} | {g.n} | {g.d:N0} | {g.d / g.n:N0} | {g.dm:0} |");
            o.AppendLine();

            // 2d. stop: distanza realizzata vs dichiarata
            o.AppendLine("### 2d. Stop loss: perdita realizzata contro distanza dichiarata (per contratto, lordo)");
            o.AppendLine();
            o.AppendLine("Solo trade usciti per stop (interno `StopLoss`; cBot esito `StopLoss` senza trailing). Rapporto = perdita lorda / stop dichiarato (allargato dove previsto). >1 = stop eseguito oltre il livello (gap, spread sugli short).");
            o.AppendLine();
            o.AppendLine("| simbolo | int n | int rapporto mediano | int oltre 1,1 | cBot n | cBot rapporto mediano | cBot oltre 1,1 | cBot short mediano | cBot long mediano |");
            o.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (var sym in allCodes.Select(c => strategies.TryGetValue(c, out var s) ? s.Symbol : "?").Distinct().OrderBy(x => x))
            {
                var si = intOverlap.Where(t => t.Reason == "StopLoss" && strategies.TryGetValue(t.Strategy, out var s) && s.Symbol == sym && s.StopMoneyEff > 0).Select(t => (double)(-t.GrossPerContract / strategies[t.Strategy].StopMoneyEff)).ToList();
                var sc = cbOverlap.Where(t => t.LogClose?.Esito == "StopLoss" && (t.LogClose?.Trailing ?? 0) == 0 && strategies.TryGetValue(t.Strategy, out var s) && s.Symbol == sym && s.StopMoneyEff > 0).ToList();
                var scr = sc.Select(t => (double)(-t.GrossPerContract / strategies[t.Strategy].StopMoneyEff)).ToList();
                var scS = sc.Where(t => t.Dir < 0).Select(t => (double)(-t.GrossPerContract / strategies[t.Strategy].StopMoneyEff)).ToList();
                var scL = sc.Where(t => t.Dir > 0).Select(t => (double)(-t.GrossPerContract / strategies[t.Strategy].StopMoneyEff)).ToList();
                o.AppendLine($"| {sym} | {si.Count} | {Median(si):0.00} | {si.Count(x => x > 1.1)} | {scr.Count} | {Median(scr):0.00} | {scr.Count(x => x > 1.1)} | {Median(scS):0.00} | {Median(scL):0.00} |");
            }
            o.AppendLine();
            // 2f. tempi di uscita
            o.AppendLine("### 2f. Trade abbinati: quanto distano le uscite");
            o.AppendLine();
            o.AppendLine("| |Δ uscita| | n | Σ Δ lordo/ctr (cBot − int) | Σ Δ netto/ctr | Δ netto medio |");
            o.AppendLine("|---|---:|---:|---:|---:|");
            foreach (var (label, lo, hi) in new[] { ("≤ 1 min", 0.0, 1.0), ("1–5 min", 1.0, 5.0), ("5–60 min", 5.0, 60.0), ("1–24 h", 60.0, 1440.0), ("> 24 h", 1440.0, double.MaxValue) })
            {
                var g = allMatched.Where(t => { var d = Math.Abs((t.Match!.Exit - t.Exit).TotalMinutes); return d > lo && d <= hi || (lo == 0.0 && d <= hi); }).ToList();
                if (g.Count == 0) continue;
                o.AppendLine($"| {label} | {g.Count} | {g.Sum(t => t.Match!.GrossPerContract - t.GrossPerContract):N0} | {g.Sum(t => t.Match!.NetPerContract - t.NetPerContract):N0} | {g.Average(t => t.Match!.NetPerContract - t.NetPerContract):N0} |");
            }
            o.AppendLine();
            o.AppendLine("Nota: per le chiusure che il cBot scopre in ritardo (`BrokerExit`), l'esito nel log è dedotto dal segno del netto (`DeduceCloseReason`), quindi `MaxBars → TakeProfit` con Δ uscita 0 è la stessa chiusura etichettata in due modi.");
            o.AppendLine();

            // 2g. fill senza barra (interno)
            o.AppendLine("### 2g. Riempimenti dell'interno su un minuto senza barra nel feed");
            o.AppendLine();
            o.AppendLine("Ingresso/uscita a un istante in cui `@SYM_1.json` non ha una barra: il prezzo viene dal mark-to-market, non dal mercato. Uscite tecniche (TimeExit, MaxBars, SessionFlat, WeekEnd, EndOfRun) escluse dal conteggio delle uscite.");
            o.AppendLine();
            o.AppendLine("| strategia | ingressi senza barra / tot | netto/ctr di quei trade | uscite SL/TP/trailing/BE senza barra | esempio |");
            o.AppendLine("|---|---:|---:|---:|---|");
            int nbTot = 0;
            foreach (var g in internalTrades.GroupBy(t => t.Strategy).OrderBy(g => g.Key))
            {
                var noBar = g.Where(t => !HasMinuteBar(t.Symbol, t.Entry)).ToList();
                var noBarExit = g.Count(t => t.Reason is "StopLoss" or "TakeProfit" or "TrailingStop" or "BreakEven" or "OppositeSignal" && !HasMinuteBar(t.Symbol, t.Exit));
                if (noBar.Count == 0 && noBarExit == 0) continue;
                nbTot += noBar.Count;
                foreach (var t in noBar) t.NoBarEntry = true;
                var ex = noBar.FirstOrDefault();
                o.AppendLine($"| {g.Key} | {noBar.Count}/{g.Count()} | {noBar.Sum(t => t.NetPerContract):N0} | {noBarExit} | {(ex is null ? "" : $"{ex.Side} {ex.Entry:yyyy-MM-dd HH:mm} @ {ex.EntryPx} → {ex.Reason}")} |");
            }
            o.AppendLine($"| **TOTALE** | {nbTot} | {internalTrades.Where(t => t.NoBarEntry).Sum(t => t.NetPerContract):N0} | | |");
            o.AppendLine();
            var cbNoBar = cbotTrades.Count(t => !HasMinuteBar(t.Symbol, new DateTime(t.Entry.Ticks - t.Entry.Ticks % TimeSpan.TicksPerMinute, DateTimeKind.Utc)));
            o.AppendLine($"Per confronto, ingressi cBot il cui minuto non ha una barra nel feed raccolto: {cbNoBar} su {cbotTrades.Count} (il cBot esegue sui tick di cTrader, il feed è la raccolta a barre dello stesso broker).");
            o.AppendLine();

            // bracket
            var slip = cbotTrades.Where(t => t.RequestedEntry is not null).Select(t => (t, pts: t.Dir * (t.EntryPx - t.RequestedEntry!.Value))).ToList();
            o.AppendLine($"Bracket riancorato al fill (ingresso slittato rispetto al livello): {slip.Count} trade; slippage mediano {Median(slip.Select(x => (double)x.pts).ToList()):0.###} punti, costo totale per contratto {slip.Sum(x => x.pts * strategies[x.t.Strategy].PointValue):N0}.");
            o.AppendLine();
            if (xpos.Count > 0)
            {
                o.AppendLine("### 2e. Verifica contro l'export eventi di cTrader");
                o.AppendLine();
                var withX = cbotTrades.Where(t => t.XPos is not null).ToList();
                o.AppendLine($"Posizioni nell'export: {xpos.Count}, chiuse {xpos.Count(p => p.CloseEvent is not null)}; trade cBot abbinati a una posizione: {withX.Count} su {cbotTrades.Count}; posizioni senza trade registrato: {xpos.Count(p => p.Trade is null)}.");
                var unreg = xpos.Where(p => p.Trade is null && p.CloseEvent is not null).ToList();
                if (unreg.Count > 0)
                    o.AppendLine($"Le posizioni non registrate valgono lordo {unreg.Sum(p => p.Gross ?? 0):N0} (prime: {string.Join("; ", unreg.Take(8).Select(p => $"#{p.Id} {p.Type} {p.OpenTime:yyyy-MM-dd HH:mm} {p.EntryPx} → {p.ClosePx} {p.CloseEvent} {p.Gross}"))}).");
                o.AppendLine();
                o.AppendLine("| evento di chiusura cTrader | n | trades.json StopLoss | TakeProfit | Closed | prezzo uscita diverso | lordo diverso |");
                o.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
                foreach (var g in withX.GroupBy(t => t.XPos!.CloseEvent ?? "(aperta)"))
                    o.AppendLine($"| {g.Key} | {g.Count()} | {g.Count(t => t.Reason.EndsWith("StopLoss"))} | {g.Count(t => t.Reason.EndsWith("TakeProfit"))} | {g.Count(t => t.Reason.EndsWith("Closed"))} | {g.Count(t => t.XPos!.ClosePx is { } cp && Math.Abs(cp - t.ExitPx) > 0.00001m * Math.Max(1m, t.ExitPx))} | {g.Count(t => t.XPos!.Gross is { } gp && Math.Abs(gp - t.Gross) > 0.02m)} |");
                o.AppendLine();
                var lastBal = xpos.Where(p => p.Balance is not null).OrderBy(p => p.CloseTime).LastOrDefault();
                o.AppendLine($"Saldo finale cTrader: {lastBal?.Balance:N2} (partenza 100.000); somma lordi export {xpos.Sum(p => p.Gross ?? 0):N2}; somma netti trades.json {cbotTrades.Sum(t => t.Net):N2}.");
                o.AppendLine();
            }

            // ---- 3. trading window
            o.AppendLine("## 3. Finestra operativa (TradingWindow) e giorno saltato");
            o.AppendLine();
            o.AppendLine("Per ogni trade si ricava la barra di segnale (la barra della strategia che precede quella del fill) e si etichetta come fa il motore (`BarLabelTime` sull'orologio della finestra). Fuori finestra = un ingresso che la strategia non avrebbe dovuto emettere.");
            o.AppendLine();
            o.AppendLine("| strategia | finestra | fuso | int fuori/tot | cBot fuori/tot | int giorno saltato | cBot giorno saltato | fill fuori griglia int/cBot |");
            o.AppendLine("|---|---|---|---:|---:|---:|---:|---:|");
            var winTotals = new int[6];
            foreach (var code in allCodes)
            {
                if (!strategies.TryGetValue(code, out var s)) { o.AppendLine($"| {code} | ? | | | | | | |"); continue; }
                int oi = 0, oc = 0, si2 = 0, sc2 = 0, gi = 0, gc = 0;
                var ai = intOverlap.Where(t => t.Strategy == code).ToList();
                var bc = cbOverlap.Where(t => t.Strategy == code).ToList();
                foreach (var t in ai) { var r = WindowCheck(t, s); t.WindowLabel = r.label; t.OutOfWindow = !r.inWindow; t.SkippedDay = r.skipped; if (!r.inWindow) oi++; if (r.skipped) si2++; if (r.note != "") gi++; }
                foreach (var t in bc) { var r = WindowCheck(t, s); t.WindowLabel = r.label; t.OutOfWindow = !r.inWindow; t.SkippedDay = r.skipped; if (!r.inWindow) oc++; if (r.skipped) sc2++; if (r.note != "") gc++; }
                winTotals[0] += oi; winTotals[1] += oc; winTotals[2] += si2; winTotals[3] += sc2; winTotals[4] += gi; winTotals[5] += gc;
                var win = s.Window is null ? "(non dichiarata)" : WindowText(s.Window);
                o.AppendLine($"| {code} | {win} | {s.Window?.Clock.ToString() ?? "-"} | {oi}/{ai.Count} | {oc}/{bc.Count} | {si2} | {sc2} | {gi}/{gc} |");
            }
            o.AppendLine($"| **TOTALE** | | | {winTotals[0]} | {winTotals[1]} | {winTotals[2]} | {winTotals[3]} | {winTotals[4]}/{winTotals[5]} |");
            o.AppendLine();
            var oow = intOverlap.Concat(cbOverlap).Where(t => t.OutOfWindow).Take(25).ToList();
            if (oow.Count > 0)
            {
                o.AppendLine("Esempi fuori finestra (primi 25):");
                o.AppendLine();
                o.AppendLine("| lato | strategia | ingresso UTC | etichetta barra di segnale | finestra |");
                o.AppendLine("|---|---|---|---:|---|");
                foreach (var t in oow)
                    o.AppendLine($"| {(t.IsCbot ? "cBot" : "int")} | {t.Strategy} | {t.Entry:yyyy-MM-dd HH:mm} | {TimeText(t.WindowLabel)} | {WindowText(strategies[t.Strategy].Window!)} |");
                o.AppendLine();
            }

            // ---- 4. sessione
            o.AppendLine("## 4. Sessione: ancoraggio e un fill per sessione per lato");
            o.AppendLine();
            o.AppendLine("| strategia | sessione (ancoraggio) | max ingressi/sessione | int sessioni con >max per lato | cBot sessioni con >max per lato | int ingressi weekend UTC | cBot ingressi weekend UTC |");
            o.AppendLine("|---|---|---:|---:|---:|---:|---:|");
            int sessViolInt = 0, sessViolCb = 0;
            foreach (var code in allCodes)
            {
                if (!strategies.TryGetValue(code, out var s)) continue;
                var ai = intOverlap.Where(t => t.Strategy == code).ToList();
                var bc = cbOverlap.Where(t => t.Strategy == code).ToList();
                int vi = 0, vc = 0;
                if (s.MaxEntries > 0)
                {
                    vi = ai.GroupBy(t => (t.Dir, s.Grid.SessionDayOf(t.Entry))).Count(g => g.Count() > s.MaxEntries);
                    vc = bc.GroupBy(t => (t.Dir, s.Grid.SessionDayOf(t.Entry))).Count(g => g.Count() > s.MaxEntries);
                }
                sessViolInt += vi; sessViolCb += vc;
                var we = ai.Count(t => t.Entry.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
                var wc = bc.Count(t => t.Entry.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
                o.AppendLine($"| {code} | {WindowText(s.Session)} | {s.MaxEntries} | {vi} | {vc} | {we} | {wc} |");
            }
            o.AppendLine($"| **TOTALE** | | | {sessViolInt} | {sessViolCb} | | |");
            o.AppendLine();
            o.AppendLine("Nel log del cBot il server ha rifiutato template per `limite di ingressi per sessione raggiunto` (conteggio righe, per strategia e lato, prime 15):");
            o.AppendLine();
            foreach (var kv in log.SessionCap.OrderByDescending(k => k.Value).Take(15))
                o.AppendLine($"- {kv.Key}: {kv.Value}");
            o.AppendLine();

            // ---- 5. spread
            o.AppendLine("## 5. Spread");
            o.AppendLine();
            var internalSpreadSource = summary.RootElement.TryGetProperty("fillConventions", out var fcSpread)
                && fcSpread.TryGetProperty("spreadSource", out var spreadSourceEl)
                && spreadSourceEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(spreadSourceEl.GetString())
                ? spreadSourceEl.GetString()!
                : "nessuno";
            o.AppendLine("Spread dell'interno, dal summary: `" + internalSpreadSource + "`. Il cBot lo paga sui tick: costo per trade ≈ spread al fill × valore punto × contratti (una volta per round trip). Lo stop è quello eseguito (allargato ×" + stopMult + " dove previsto).");
            o.AppendLine();
            o.AppendLine("| strategia | simbolo | fill con spread | spread medio (punti) | stop (punti) | spread/stop | costo spread tot (per contratto, $) | netto/ctr cBot | netto/ctr senza spread | costo/trade ($/ctr) |");
            o.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|");
            var spreadRows = new List<(string code, decimal ratio, decimal cost, decimal net, int n)>();
            foreach (var code in cbotTrades.Select(t => t.Strategy).Distinct().OrderBy(x => x))
            {
                var s = strategies.TryGetValue(code, out var ss) ? ss : null;
                var ts = cbotTrades.Where(t => t.Strategy == code).ToList();
                var withSpread = ts.Where(t => t.SpreadAtFill is not null).ToList();
                var avgSpread = withSpread.Count > 0 ? withSpread.Average(t => t.SpreadAtFill!.Value) : 0m;
                var pv = s?.PointValue ?? 0m;
                var cost = ts.Sum(t => (t.SpreadAtFill ?? avgSpread) * pv); // per contratto
                var stopPts = s?.StopPoints ?? 0m;
                var ratio = stopPts > 0 ? avgSpread / stopPts : 0m;
                var net = ts.Sum(t => t.NetPerContract);
                spreadRows.Add((code, ratio, cost, net, ts.Count));
                o.AppendLine($"| {code} | {s?.Symbol} | {withSpread.Count}/{ts.Count} | {avgSpread:0.###} | {stopPts:0.##} | {ratio:P1} | {cost:N0} | {net:N0} | {net + cost:N0} | {(ts.Count > 0 ? cost / ts.Count : 0):N0} |");
            }
            o.AppendLine($"| **TOTALE** | | | | | | {spreadRows.Sum(r => r.cost):N0} | {spreadRows.Sum(r => r.net):N0} | {spreadRows.Sum(r => r.net + r.cost):N0} | |");
            o.AppendLine();
            o.AppendLine("### 5b. Strategie con stop più stretto dello spread, o quasi");
            o.AppendLine();
            o.AppendLine("| strategia | spread/stop | stop (punti) | trade cBot | netto/ctr cBot | commento |");
            o.AppendLine("|---|---:|---:|---:|---:|---|");
            foreach (var r in spreadRows.Where(r => r.ratio >= 0.10m).OrderByDescending(r => r.ratio))
            {
                var s = strategies[r.code];
                var cm = r.ratio >= 1m ? "**stop sotto lo spread: non eseguibile**" : r.ratio >= 0.5m ? "stop di poco sopra lo spread" : r.ratio >= 0.2m ? "oltre il tetto del 20% del cBot (filtro spento nel profilo sorgente)" : "10-20%";
                o.AppendLine($"| {r.code} | {r.ratio:P1} | {s.StopPoints:0.##} | {r.n} | {r.net:N0} | {cm} |");
            }
            o.AppendLine();
            // effetto "più stop": abbinati in cui l'interno non esce per stop e il cbot sì
            var moreStops = allMatched.Count(t => t.Reason != "StopLoss" && (t.Match!.LogClose?.Esito == "StopLoss"));
            var fewerStops = allMatched.Count(t => t.Reason == "StopLoss" && (t.Match!.LogClose?.Esito != "StopLoss"));
            o.AppendLine($"Sui trade abbinati: {moreStops} volte il cBot esce per stop dove l'interno no; {fewerStops} il contrario.");
            o.AppendLine();

            // ---- 6. curva crescente
            o.AppendLine("## 6. Strategie con curva di equity crescente");
            o.AppendLine();
            var cbotSpan = cbotTrades.Count > 0 ? $"{cbotTrades.Min(t => t.Entry):dd/MM/yyyy} → {cbotTrades.Max(t => t.Entry):dd/MM/yyyy}" : "nessun trade";
            var internalSpan = internalTrades.Count > 0 ? $"{internalTrades.Min(t => t.Entry):dd/MM/yyyy} → {internalTrades.Max(t => t.Entry):dd/MM/yyyy}" : "nessun trade";
            o.AppendLine("Metriche su netto per contratto, trade ordinati per uscita. R² = fit lineare della curva cumulata sull'indice dei trade. Crescente = netto > 0, R² ≥ 0,6, chiusura ad almeno il 70% del picco, ≥ 8 trade. Il cBot è calcolato sull'intero run (ingressi " + cbotSpan + "), l'interno sull'intero run (ingressi " + internalSpan + ").");
            o.AppendLine();
            o.AppendLine("| strategia | cBot n | cBot netto/ctr | PF | R² | DD max/ctr | fine/picco | **cBot** | int n | int netto/ctr | PF | R² | DD max/ctr | fine/picco | **int** |");
            o.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---|");
            var rising = new List<(string code, bool cb, bool it)>();
            foreach (var code in cbotTrades.Select(t => t.Strategy).Union(internalTrades.Select(t => t.Strategy)).Distinct().OrderBy(x => x))
            {
                var mc = Curve(cbotTrades.Where(t => t.Strategy == code).ToList());
                var mi = Curve(internalTrades.Where(t => t.Strategy == code).ToList());
                rising.Add((code, mc.rising, mi.rising));
                o.AppendLine($"| {code} | {mc.n} | {mc.net:N0} | {mc.pf:0.00} | {mc.r2:0.00} | {mc.dd:N0} | {mc.endPeak:0.00} | {(mc.rising ? "SÌ" : "no")} | {mi.n} | {mi.net:N0} | {mi.pf:0.00} | {mi.r2:0.00} | {mi.dd:N0} | {mi.endPeak:0.00} | {(mi.rising ? "SÌ" : "no")} |");
            }
            o.AppendLine();
            o.AppendLine("**Crescenti sul cBot:** " + string.Join(", ", rising.Where(r => r.cb).Select(r => r.code)));
            o.AppendLine();
            o.AppendLine("**Crescenti sull'interno:** " + string.Join(", ", rising.Where(r => r.it).Select(r => r.code)));
            o.AppendLine();
            o.AppendLine("**Crescenti su entrambi:** " + string.Join(", ", rising.Where(r => r.cb && r.it).Select(r => r.code)));
            o.AppendLine();

            // ---- 7. equity aggregata per mese
            o.AppendLine("## 7. Equity aggregata per mese (netto per contratto, sovrapposizione)");
            o.AppendLine();
            o.AppendLine("| mese (uscita) | interno | cBot | Δ |");
            o.AppendLine("|---|---:|---:|---:|");
            var months = intOverlap.Concat(cbOverlap).Select(t => new DateTime(t.Exit.Year, t.Exit.Month, 1)).Distinct().OrderBy(x => x);
            decimal ci2 = 0, cc2 = 0;
            foreach (var m in months)
            {
                var a = intOverlap.Where(t => t.Exit.Year == m.Year && t.Exit.Month == m.Month).Sum(t => t.NetPerContract);
                var b = cbOverlap.Where(t => t.Exit.Year == m.Year && t.Exit.Month == m.Month).Sum(t => t.NetPerContract);
                ci2 += a; cc2 += b;
                o.AppendLine($"| {m:yyyy-MM} | {a:N0} (cum {ci2:N0}) | {b:N0} (cum {cc2:N0}) | {b - a:N0} |");
            }
            o.AppendLine();

            // ---- 9. ritardo degli intent
            o.AppendLine("## 9. Quando arrivano gli intent al cBot (righe `Intent ... attesa`)");
            o.AppendLine();
            o.AppendLine("`attesa` = ValidFrom − ora del server: negativo = l'intent arriva dopo l'inizio della barra su cui è valido. Per tipo di ordine e timeframe della strategia.");
            o.AppendLine();
            o.AppendLine("| tipo | tf | intent | in orario (≤ 10 s) | 10 s – 5 min | 5 min – 1 barra | ≥ 1 barra (in ritardo di una barra intera) | esempio strategie ≥ 1 barra |");
            o.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");
            foreach (var g in log.Intents.GroupBy(i => (i.Type, tf: strategies.TryGetValue(i.Strategy, out var s) ? s.Tf : 0)).OrderBy(g => g.Key.Type).ThenBy(g => g.Key.tf))
            {
                var tfSec = g.Key.tf * 60.0;
                var late = g.Select(i => -i.Attesa).ToList();
                var lateBar = g.Where(i => -i.Attesa >= tfSec - 1).ToList();
                o.AppendLine($"| {g.Key.Type} | {g.Key.tf} | {g.Count()} | {late.Count(x => x <= 10)} | {late.Count(x => x > 10 && x <= 300)} | {late.Count(x => x > 300 && x < tfSec - 1)} | {lateBar.Count} | {string.Join(", ", lateBar.GroupBy(i => i.Strategy).OrderByDescending(x => x.Count()).Take(4).Select(x => $"{x.Key} ({x.Count()})"))} |");
            }
            o.AppendLine();
            o.AppendLine("### 9b. Trade cBot nati da un intent arrivato in ritardo di almeno una barra");
            o.AppendLine();
            var lateTrades = cbotTrades.Where(t => t.IntentAttesa is { } a && strategies.TryGetValue(t.Strategy, out var s) && -a >= s.Tf * 60.0 - 1).ToList();
            o.AppendLine($"Trade cBot con intent risolto: {cbotTrades.Count(t => t.IntentAttesa is not null)} su {cbotTrades.Count}; nati da intent in ritardo ≥ 1 barra: {lateTrades.Count}, netto/ctr {lateTrades.Sum(t => t.NetPerContract):N0}; di questi senza corrispondente interno: {lateTrades.Count(t => t.Entry >= overlapStart && t.Entry < overlapEnd && t.Match is null)}.");
            o.AppendLine();
            o.AppendLine("| strategia | trade da intent stantio | netto/ctr | non abbinati |");
            o.AppendLine("|---|---:|---:|---:|");
            foreach (var g in lateTrades.GroupBy(t => t.Strategy).OrderByDescending(g => g.Count()).Take(15))
                o.AppendLine($"| {g.Key} | {g.Count()} | {g.Sum(t => t.NetPerContract):N0} | {g.Count(t => t.Match is null)} |");
            o.AppendLine();
            // stop davvero anticipati
            var realEarlyStops = allMatched.Where(t => (t.Match!.LogClose?.Esito == "StopLoss") && t.Reason is not ("StopLoss" or "BreakEven" or "TrailingStop") && (t.Exit - t.Match!.Exit).TotalMinutes > 5).ToList();
            o.AppendLine($"Trade abbinati in cui il cBot esce per stop più di 5 minuti PRIMA dell'uscita interna (che non era uno stop): {realEarlyStops.Count}, Δ netto/ctr {realEarlyStops.Sum(t => t.Match!.NetPerContract - t.NetPerContract):N0}. Per strategia: {string.Join(", ", realEarlyStops.GroupBy(t => t.Strategy).OrderByDescending(g => g.Count()).Take(8).Select(g => $"{g.Key} {g.Count()}"))}.");
            o.AppendLine();
            if (xpos.Count > 0)
            {
                o.AppendLine("### 9c. Scostamenti fra trades.json e l'export cTrader (esempi)");
                o.AppendLine();
                var diffs = cbotTrades.Where(t => t.XPos?.ClosePx is { } cp && Math.Abs(cp - t.ExitPx) > 0.00001m * Math.Max(1m, t.ExitPx)).ToList();
                o.AppendLine($"Trade con prezzo di uscita diverso dall'export: {diffs.Count}; differenza mediana in punti {Median(diffs.Select(t => (double)Math.Abs(t.XPos!.ClosePx!.Value - t.ExitPx)).ToList()):0.####}; per simbolo: {string.Join(", ", diffs.GroupBy(t => t.Symbol).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}"))}.");
                o.AppendLine();
                o.AppendLine("| strategia | uscita trades.json | prezzo trades.json | prezzo export | lordo trades.json | lordo export | evento export |");
                o.AppendLine("|---|---|---:|---:|---:|---:|---|");
                foreach (var t in diffs.Take(10))
                    o.AppendLine($"| {t.Strategy} | {t.Exit:yyyy-MM-dd HH:mm:ss} | {t.ExitPx} | {t.XPos!.ClosePx} | {t.Gross} | {t.XPos!.Gross} | {t.XPos!.CloseEvent} |");
                o.AppendLine();
                var unreg = xpos.Where(p => p.Trade is null).ToList();
                o.AppendLine($"Posizioni dell'export senza trade in trades.json: {unreg.Count} — chiuse fra {unreg.Min(p => p.CloseTime):yyyy-MM-dd HH:mm} e {unreg.Max(p => p.CloseTime):yyyy-MM-dd HH:mm}, lordo totale {unreg.Sum(p => p.Gross ?? 0):N2}.");
                o.AppendLine();
            }

            // ---- 8. log: avvisi
            o.AppendLine("## 8. Avvisi ed errori nel log del cBot (messaggi normalizzati, prime 25)");
            o.AppendLine();
            foreach (var kv in log.Warnings.OrderByDescending(k => k.Value).Take(25))
                o.AppendLine($"- {kv.Value,7}  {kv.Key}");
            o.AppendLine();
            o.AppendLine("Ingressi scartati per classe:");
            foreach (var kv in log.Rejects.GroupBy(r => r.Class).OrderByDescending(g => g.Count()))
                o.AppendLine($"- {kv.Count(),7}  {kv.Key}");
            o.AppendLine();
            o.AppendLine("Ingressi annullati per classe:");
            foreach (var kv in log.Cancels.GroupBy(r => r.Class).OrderByDescending(g => g.Count()))
                o.AppendLine($"- {kv.Count(),7}  {kv.Key}");
            o.AppendLine();
            o.AppendLine("`Nessun intent per l'account` per motivo:");
            foreach (var kv in log.NoIntent.OrderByDescending(k => k.Value))
                o.AppendLine($"- {kv.Value,7}  {kv.Key}");
            o.AppendLine();
            o.AppendLine("Ingressi scartati `lato sbagliato` per strategia (prime 15):");
            foreach (var kv in log.Rejects.Where(r => r.Class == "lato sbagliato").GroupBy(r => r.Strategy).OrderByDescending(g => g.Count()).Take(15))
                o.AppendLine($"- {kv.Count(),7}  {kv.Key}");
            o.AppendLine();
        }

        (int n, decimal net, double pf, double r2, decimal dd, double endPeak, bool rising) Curve(List<Trade> ts)
        {
            if (ts.Count == 0) return (0, 0, 0, 0, 0, 0, false);
            var ordered = ts.OrderBy(t => t.Exit).ToList();
            decimal cum = 0, peak = 0, dd = 0, gp = 0, gl = 0;
            var ys = new List<double>();
            foreach (var t in ordered)
            {
                cum += t.NetPerContract; ys.Add((double)cum);
                if (cum > peak) peak = cum;
                if (peak - cum > dd) dd = peak - cum;
                if (t.NetPerContract > 0) gp += t.NetPerContract; else gl -= t.NetPerContract;
            }
            var n = ys.Count;
            var xs = Enumerable.Range(1, n).Select(i => (double)i).ToList();
            var mx = xs.Average(); var my = ys.Average();
            var sxy = xs.Zip(ys).Sum(p => (p.First - mx) * (p.Second - my));
            var sxx = xs.Sum(x => (x - mx) * (x - mx));
            var syy = ys.Sum(y => (y - my) * (y - my));
            var slope = sxx > 0 ? sxy / sxx : 0;
            var r2 = sxx > 0 && syy > 0 ? (sxy * sxy) / (sxx * syy) : 0;
            var endPeak = peak > 0 ? (double)(cum / peak) : 0;
            var pf = gl > 0 ? (double)(gp / gl) : (gp > 0 ? 99 : 0);
            var rising = cum > 0 && slope > 0 && r2 >= 0.6 && endPeak >= 0.7 && n >= 8;
            return (n, cum, pf, r2, dd, endPeak, rising);
        }

        static double Median(List<double> xs)
        {
            if (xs.Count == 0) return double.NaN;
            var s = xs.OrderBy(x => x).ToList();
            return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
        }
        static decimal MedianD(List<decimal> xs) => xs.Count == 0 ? 0 : (decimal)Median(xs.Select(x => (double)x).ToList());
        static decimal Avg(List<decimal> xs) => xs.Count == 0 ? 0 : xs.Average();

        void WriteCsvs()
        {
            var sbm = new StringBuilder();
            sbm.AppendLine("strategy;dir;int_entry;cb_entry;delta_min;int_entry_px;cb_entry_px;int_exit;cb_exit;int_exit_px;cb_exit_px;int_reason;cb_reason;cb_esito;int_net_ctr;cb_net_ctr;cb_spread;cb_mfe;cb_mae;cb_bars");
            foreach (var t in intOverlap.Where(t => t.Match is not null).OrderBy(t => t.Entry))
            {
                var c = t.Match!;
                sbm.AppendLine(string.Join(";", t.Strategy, t.Side, t.Entry.ToString("s"), c.Entry.ToString("s"), (c.Entry - t.Entry).TotalMinutes.ToString("0.0", ci),
                    t.EntryPx.ToString(ci), c.EntryPx.ToString(ci), t.Exit.ToString("s"), c.Exit.ToString("s"), t.ExitPx.ToString(ci), c.ExitPx.ToString(ci),
                    t.Reason, c.Reason, c.LogClose?.Esito, t.NetPerContract.ToString("0.00", ci), c.NetPerContract.ToString("0.00", ci),
                    c.SpreadAtFill?.ToString(ci), c.LogClose?.Mfe.ToString(ci), c.LogClose?.Mae.ToString(ci), c.LogClose?.Bars.ToString()));
            }
            File.WriteAllText(Path.Combine(outDir, "trade-abbinati.csv"), sbm.ToString(), Encoding.UTF8);

            var sbo = new StringBuilder();
            sbo.AppendLine("lato;strategy;dir;entry;entry_px;exit;exit_px;reason;net_ctr;causa;fuori_finestra;etichetta");
            foreach (var t in intOverlap.Where(t => t.Match is null).Concat(cbOverlap.Where(t => t.Match is null)).OrderBy(t => t.Entry))
                sbo.AppendLine(string.Join(";", t.IsCbot ? "cbot" : "int", t.Strategy, t.Side, t.Entry.ToString("s"), t.EntryPx.ToString(ci), t.Exit.ToString("s"), t.ExitPx.ToString(ci), t.LogClose?.Esito ?? t.Reason, t.NetPerContract.ToString("0.00", ci), t.Cause, t.OutOfWindow, TimeText(t.WindowLabel)));
            File.WriteAllText(Path.Combine(outDir, "trade-non-abbinati.csv"), sbo.ToString(), Encoding.UTF8);

            var sbs = new StringBuilder();
            sbs.AppendLine("code;symbol;tf;engine;window;windowClock;session;holding;intradayOnly;stopMoney;stopMoneyEff;profitMoney;profitMoneyEff;breakEven;trailing;maxBars;maxEntries;skipDay;pointValue;stopPoints;widened");
            foreach (var s in strategies.Values.OrderBy(s => s.Code))
                sbs.AppendLine(string.Join(";", s.Code, s.Symbol, s.Tf, s.Engine, s.Window is null ? "" : WindowText(s.Window), s.Window?.Clock, WindowText(s.Session), s.Holding, s.IntradayOnly, s.StopMoney, s.StopMoneyEff, s.ProfitMoney, s.ProfitMoneyEff, s.BreakEvenMoney, s.TrailingMoney, s.MaxBars, s.MaxEntries, s.SkipDay, s.PointValue, s.StopPoints.ToString("0.####", ci), s.Widened));
            File.WriteAllText(Path.Combine(outDir, "strategie.csv"), sbs.ToString(), Encoding.UTF8);
        }
    }
}

// ============================================================================================
// TIPI
// ============================================================================================

sealed record Bar(DateTime Open, decimal O, decimal H, decimal L, decimal C);
sealed record LogClose(DateTime Ts, string Strategy, string Symbol, int Dir, decimal EntryPx, decimal ExitPx, decimal Qty, string Esito, decimal Gross, decimal Comm, decimal Swap, decimal Net, decimal Mfe, decimal Mae, int Minutes, int Bars, int Trailing);
sealed record LogFill(DateTime Ts, string Strategy, string Symbol, decimal Spread, decimal? Stop);
sealed record LogEvent(DateTime Ts, string Strategy, string Symbol, string Class, string Motivo);
sealed record LogBracket(DateTime Ts, string Strategy, string Symbol, decimal Fill, decimal Requested);
sealed record LogIntent(DateTime Ts, string Strategy, string Symbol, string Type, double Ritardo, double Attesa);

sealed class LogData
{
    public List<LogIntent> Intents = new();
    public List<LogClose> Closes = new();
    public List<LogFill> Fills = new();
    public List<LogEvent> Rejects = new();
    public List<LogEvent> Cancels = new();
    public List<LogBracket> Brackets = new();
    public Dictionary<string, int> Warnings = new();
    public Dictionary<string, int> SessionCap = new();
    public Dictionary<string, int> NoIntent = new();
    public int FlatCloses, Unparsed, ClosesUnmatched, FillsUnmatched;
    public long Lines;
}

sealed class XPosition
{
    public long Id; public DateTime OpenTime; public string Type = ""; public decimal? VolumeLots, EntryPx, Sl, Tp;
    public DateTime CloseTime; public string? CloseEvent; public decimal? ClosePx, Gross, Balance; public int Modifications;
    public Trade? Trade;
}

sealed class StratInfo
{
    public string Code = ""; public string Symbol = ""; public int Tf; public string Engine = ""; public string Holding = "";
    public ZonedWindow? Window; public ZonedWindow Session = null!; public SessionClock WindowClock = null!; public SessionClock Clock = null!; public SessionGrid Grid = null!;
    public int StopMoney, ProfitMoney, BreakEvenMoney, TrailingMoney, MaxBars, MaxEntries, SkipDay = -1; public bool IntradayOnly, Widened;
    public decimal PointValue, StopMoneyEff, ProfitMoneyEff, StopPoints, TargetPoints;
}

sealed class StratRow
{
    public string Code = ""; public int Tf, NInt, NCbot, Matched, OnlyInt, OnlyCbot; public double MedianDelta;
    public decimal NetInt, NetCbot, NetIntMatched, NetCbotMatched, GrossIntMatched, GrossCbotMatched;
}

sealed class Trade
{
    public int Idx; public bool IsCbot;
    public string Strategy = ""; public string Symbol = ""; public int Dir; public decimal Qty, Contracts;
    public DateTime Entry, Exit; public decimal EntryPx, ExitPx; public string Reason = "";
    public decimal Gross, Net, Commission, Swap;
    public string Side => Dir > 0 ? "Buy" : "Sell";
    public decimal GrossPerContract => Contracts > 0 ? Gross / Contracts : 0;
    public decimal NetPerContract => Contracts > 0 ? Net / Contracts : 0;
    public Trade? Match;
    public LogClose? LogClose; public decimal? SpreadAtFill, StopAtFill, RequestedEntry; public XPosition? XPos;
    public string? Cause; public bool OutOfWindow, SkippedDay, NoBarEntry; public TimeOnly? WindowLabel;
    public double? IntentAttesa; public string? IntentType;
}
