using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using cAlgo.API;
using cAlgo.API.Internals;
using File = System.IO.File;

namespace cAlgo.Robots
{
    // ------------------------------------------------------------------------------------------
    // PiootooSymbolMultiplierBot
    //
    // cBot di utilita', non di trading. Legge dal broker le specifiche dei symbol CFD elencati in
    // parametro e calcola il ContractMultiplier da mettere nella tabella di conversione symbol
    // (accounts/symbol-conversions.json, vedi docs/domini/account-e-conversione-symbol.md).
    //
    // LA REGOLA DEL MOLTIPLICATORE
    //
    //   ContractMultiplier = PointValue(future) / PointValue(1 lotto CFD)
    //
    // dove entrambi i valori sono espressi nella VALUTA DI QUOTAZIONE dello strumento, non nella
    // valuta del conto. Per un CFD il valore di un punto di prezzo per un lotto e' semplicemente la
    // dimensione del lotto in unita' del sottostante (Symbol.LotSize): il controvalore e'
    // unita' x prezzo, quindi la sua derivata rispetto al prezzo e' il numero di unita'.
    //
    //   USTEC   LotSize 1        -> 1 $/punto per lotto,       @NQ   20 $/punto     -> 20
    //   DE40    LotSize 1        -> 1 EUR/punto per lotto,     @FDAX 25 EUR/punto   -> 25
    //   XTIUSD  LotSize 100      -> 100 $/punto per lotto,     @CL   1000 $/punto   -> 10
    //   XAUUSD  LotSize 100      -> 100 $/punto per lotto,     @GC   100 $/punto    -> 1
    //   GBPUSD  LotSize 100.000  -> 100.000 $/punto per lotto, @BP   62.500 $/punto -> 0,625
    //
    // Il bot riporta ANCHE il valore punto in valuta conto (ricavato da TickValue), utile per capire
    // quanto pesa davvero una posizione sul conto, ma il moltiplicatore da mettere in tabella e'
    // quello in valuta di quotazione: e' l'unico che non cambia quando si muove il cambio.
    //
    // Non piazza ordini e non apre sessioni Piootoo: attende qualche secondo che arrivino i tick
    // (senza quotazione TickValue e PipValue possono essere zero), scrive i file e si ferma da solo.
    //
    // Output:
    //   symbol-multipliers.json          analisi completa: specifiche broker, valori calcolati,
    //                                    avvisi, piu' il dump per reflection di ogni proprieta'
    //   symbol-multipliers.mappings.json solo l'array "Mappings", da incollare dentro una voce di
    //                                    accounts/symbol-conversions.json
    //
    // Vedi anche PiootooSymbolInfoDumpBot: quello scarica *tutti* i symbol del conto senza calcoli,
    // questo lavora su un elenco mirato e produce i numeri della tabella di conversione.
    // ------------------------------------------------------------------------------------------

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooSymbolMultiplierBot : Robot
    {
        // Elenco "future=CFD:pointValue:valuta". I point value replicano
        // Piootoo.Shared/Configuration/InstrumentRegistry.cs: se li cambi qui senza cambiarli la',
        // i due mondi divergono in silenzio, che e' esattamente cio' che il registro esiste per evitare.
        private const string DefaultMap =
            "@BP=GBPUSD:62500:USD," +
            "@BTC=BTCUSD:5:USD," +
            "@CL=XTIUSD:1000:USD," +
            "@EC=EURUSD:125000:USD," +
            "@ES=US500:50:USD," +
            "@FDAX=DE40:25:EUR," +
            "@GC=XAUUSD:100:USD," +
            "@NG=XNGUSD:10000:USD," +
            "@NQ=USTEC:20:USD," +
            "@PL=XPTUSD:50:USD," +
            "@YM=US30:5:USD";

        [Parameter("Mappa future=CFD:pointValue:valuta", DefaultValue = DefaultMap, Group = "Strumenti")]
        public string SymbolMap { get; set; }

        [Parameter("Symbol extra da ispezionare (CSV, senza calcolo)", DefaultValue = "", Group = "Strumenti")]
        public string ExtraSymbols { get; set; }

        [Parameter("Secondi di attesa quotazioni", DefaultValue = 5, MinValue = 0, MaxValue = 120, Group = "Strumenti")]
        public int WaitSeconds { get; set; }

        [Parameter("Cartella di output", DefaultValue = "", Group = "Output")]
        public string OutputFolder { get; set; }

        [Parameter("Nome file", DefaultValue = "symbol-multipliers.json", Group = "Output")]
        public string FileName { get; set; }

        [Parameter("Codice tabella di conversione", DefaultValue = "cfd-ctrader", Group = "Output")]
        public string ConversionCode { get; set; }

        private readonly List<string> _warnings = new List<string>();
        private List<MapEntry> _entries;

        protected override void OnStart()
        {
            try
            {
                _entries = ParseMap(SymbolMap);
                foreach (var extra in SplitCsv(ExtraSymbols))
                    _entries.Add(new MapEntry { AccountSymbol = extra });

                if (_entries.Count == 0)
                {
                    Print("PiootooSymbolMultiplierBot: nessun symbol da analizzare, controlla i parametri.");
                    Stop();
                    return;
                }

                // GetSymbol iscrive lo strumento: chiedendoli tutti adesso, l'attesa che segue vale
                // per tutti insieme invece che per il primo soltanto.
                foreach (var entry in _entries)
                    TryResolve(entry);

                if (WaitSeconds <= 0)
                {
                    RunDump();
                    return;
                }

                Print("PiootooSymbolMultiplierBot: attendo {0}s che arrivino le quotazioni ({1} symbol richiesti).",
                    WaitSeconds, _entries.Count);
                Timer.Start(TimeSpan.FromSeconds(WaitSeconds));
            }
            catch (Exception ex)
            {
                Print("PiootooSymbolMultiplierBot: errore fatale in avvio: {0}", ex);
                Stop();
            }
        }

        protected override void OnTimer()
        {
            Timer.Stop();
            RunDump();
        }

        private void RunDump()
        {
            try
            {
                var accountCurrency = SafeAccountCurrency();
                var rows = new List<Dictionary<string, object>>();
                var mappings = new List<Dictionary<string, object>>();

                foreach (var entry in _entries)
                {
                    var symbol = TryResolve(entry);
                    if (symbol == null)
                    {
                        _warnings.Add(string.Format("Symbol '{0}' non esiste su questo conto.{1}",
                            entry.AccountSymbol, SuggestNames(entry.AccountSymbol)));
                        rows.Add(new Dictionary<string, object>
                        {
                            ["AccountSymbol"] = entry.AccountSymbol,
                            ["FuturesSymbol"] = entry.FuturesSymbol,
                            ["Found"] = false,
                            ["Candidates"] = FindCandidates(entry.AccountSymbol),
                        });
                        continue;
                    }

                    Dictionary<string, object> mapping;
                    rows.Add(Analyze(entry, symbol, accountCurrency, out mapping));
                    if (mapping != null)
                        mappings.Add(mapping);
                }

                var payload = new Dictionary<string, object>
                {
                    ["GeneratedUtc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    ["Broker"] = SafeBrokerName(),
                    ["AccountCurrency"] = accountCurrency,
                    ["Regola"] = "ContractMultiplier = PointValue(future) / LotSize(CFD), entrambi in valuta di quotazione",
                    ["Warnings"] = _warnings,
                    ["Symbols"] = rows,
                    ["Mappings"] = mappings,
                };

                var path = ResolveOutputPath(FileName, "symbol-multipliers.json");
                WriteJson(path, payload);

                var baseName = Path.GetFileNameWithoutExtension(ResolveFileName(FileName, "symbol-multipliers.json"));
                var mappingsPath = ResolveOutputPath(baseName + ".mappings.json", "symbol-multipliers.mappings.json");
                WriteJson(mappingsPath, new Dictionary<string, object>
                {
                    ["Code"] = ConversionCode,
                    ["Name"] = ConversionCode,
                    ["RoundingMode"] = BuildRoundingMode(rows),
                    ["Mappings"] = mappings,
                });

                PrintTable(rows);
                foreach (var warning in _warnings)
                    Print("PiootooSymbolMultiplierBot: ATTENZIONE {0}", warning);

                Print("PiootooSymbolMultiplierBot: scritti {0} symbol su {1}", rows.Count, path);
                Print("PiootooSymbolMultiplierBot: mappings pronti da incollare su {0}", mappingsPath);
            }
            catch (Exception ex)
            {
                Print("PiootooSymbolMultiplierBot: errore fatale: {0}", ex);
            }
            finally
            {
                Stop();
            }
        }

        // ---------------------------------------------------------------------------------------
        // Analisi di un singolo symbol
        // ---------------------------------------------------------------------------------------

        private Dictionary<string, object> Analyze(MapEntry entry, Symbol symbol, string accountCurrency,
            out Dictionary<string, object> mapping)
        {
            mapping = null;

            var lotSize = Read(() => symbol.LotSize, 0d);
            var tickSize = Read(() => symbol.TickSize, 0d);
            var tickValue = Read(() => symbol.TickValue, 0d);
            var pipSize = Read(() => symbol.PipSize, 0d);
            var pipValue = Read(() => symbol.PipValue, 0d);
            var quoteAsset = Read(() => symbol.QuoteAsset != null ? symbol.QuoteAsset.Name : null, (string)null);
            var baseAsset = Read(() => symbol.BaseAsset != null ? symbol.BaseAsset.Name : null, (string)null);

            var volMin = Read(() => symbol.VolumeInUnitsMin, 0d);
            var volMax = Read(() => symbol.VolumeInUnitsMax, 0d);
            var volStep = Read(() => symbol.VolumeInUnitsStep, 0d);
            var lotsMin = ToLots(symbol, volMin, lotSize);
            var lotsStep = ToLots(symbol, volStep, lotSize);
            var lotsMax = ToLots(symbol, volMax, lotSize);

            // Valore di un punto di prezzo per UN lotto:
            //  - in valuta di quotazione: la dimensione del lotto in unita' del sottostante;
            //  - in valuta conto: ricavato dal tick, che il broker converte gia' al cambio corrente.
            var pointValueQuote = lotSize;
            var pointValueAccount = tickSize > 0 ? tickValue / tickSize * lotSize : 0d;
            double? impliedFx = pointValueQuote > 0 && pointValueAccount > 0
                ? pointValueAccount / pointValueQuote
                : (double?)null;

            var row = new Dictionary<string, object>
            {
                ["AccountSymbol"] = symbol.Name,
                ["Found"] = true,
                ["FuturesSymbol"] = entry.FuturesSymbol,
                ["Description"] = Read(() => symbol.Description, (string)null),
                ["BaseAsset"] = baseAsset,
                ["QuoteAsset"] = quoteAsset,
                ["Digits"] = Read(() => symbol.Digits, 0),
                ["TickSize"] = tickSize,
                ["PipSize"] = pipSize,
                ["LotSize"] = lotSize,
                ["VolumeInUnitsMin"] = volMin,
                ["VolumeInUnitsStep"] = volStep,
                ["VolumeInUnitsMax"] = volMax,
                ["LotsMin"] = lotsMin,
                ["LotsStep"] = lotsStep,
                ["LotsMax"] = lotsMax,
                ["TickValueAccountCcy"] = tickValue,
                ["PipValueAccountCcy"] = pipValue,
                ["Commission"] = Read(() => symbol.Commission, 0d),
                ["CommissionType"] = Read(() => symbol.CommissionType.ToString(), (string)null),
                ["SwapLong"] = Read(() => symbol.SwapLong, 0d),
                ["SwapShort"] = Read(() => symbol.SwapShort, 0d),
                ["DynamicLeverage"] = Read(() => DescribeLeverage(symbol), (string)null),
                ["Bid"] = Read(() => symbol.Bid, 0d),
                ["Ask"] = Read(() => symbol.Ask, 0d),
                ["Spread"] = Read(() => symbol.Spread, 0d),
                ["PointValuePerLotQuoteCcy"] = Round(pointValueQuote),
                ["PointValuePerLotAccountCcy"] = Round(pointValueAccount),
                ["AccountCurrency"] = accountCurrency,
                ["ImpliedFxQuoteToAccount"] = impliedFx.HasValue ? (object)Round(impliedFx.Value) : null,
                ["Raw"] = DumpAllProperties(symbol),
            };

            if (tickValue <= 0)
                _warnings.Add(string.Format(
                    "'{0}': TickValue nullo, la quotazione non e' ancora arrivata. Aumenta l'attesa o avvia a mercato aperto: il valore punto in valuta conto non e' affidabile.",
                    symbol.Name));

            if (entry.FuturesSymbol == null)
            {
                row["ContractMultiplier"] = null;
                row["Note"] = "Symbol ispezionato senza future di riferimento: nessun moltiplicatore calcolato.";
                return row;
            }

            row["FuturesPointValue"] = entry.FuturesPointValue;
            row["FuturesCurrency"] = entry.FuturesCurrency;

            if (pointValueQuote <= 0)
            {
                _warnings.Add(string.Format("'{0}': LotSize nullo o non leggibile, moltiplicatore non calcolabile.", symbol.Name));
                row["ContractMultiplier"] = null;
                return row;
            }

            // Il rapporto vale solo se future e CFD sono quotati nella stessa valuta: altrimenti porta
            // dentro un cambio, e il moltiplicatore andrebbe rivisto a ogni movimento FX.
            if (!string.IsNullOrEmpty(quoteAsset) && !string.IsNullOrEmpty(entry.FuturesCurrency) &&
                !string.Equals(quoteAsset, entry.FuturesCurrency, StringComparison.OrdinalIgnoreCase))
            {
                _warnings.Add(string.Format(
                    "'{0}': il future {1} e' quotato in {2} ma il CFD in {3}. Il rapporto viene scritto lo stesso, ma va verificato a mano: mescola due valute.",
                    symbol.Name, entry.FuturesSymbol, entry.FuturesCurrency, quoteAsset));
                row["QuoteCurrencyMatchesFutures"] = false;
            }
            else
            {
                row["QuoteCurrencyMatchesFutures"] = true;
            }

            var multiplier = entry.FuturesPointValue / pointValueQuote;
            row["ContractMultiplier"] = Round(multiplier);

            // Minimo e passo restano per riga, espressi nei contratti del broker (lotti).
            // L'arrotondamento NO: e' una proprieta' della tabella, decisa dal broker una volta
            // sola, e viene scritto sull'oggetto SymbolConversion (vedi BuildRoundingMode).
            // Scriverlo per riga e' la forma deprecata dal 24/08/2026.
            mapping = new Dictionary<string, object>
            {
                ["Symbol"] = entry.FuturesSymbol,
                ["AccountSymbol"] = symbol.Name,
                ["ContractMultiplier"] = Round(multiplier),
                // PriceScale resta 1: le distanze sono in punti, invarianti fra future e CFD. Serve
                // diverso da 1 solo se il broker quota il sottostante in un'altra unita', e questo
                // il bot non lo puo' dedurre senza il prezzo del future accanto: va confrontato a mano
                // fra Bid qui sotto e l'ultima barra del datafeed.
                ["PriceScale"] = 1,
                ["MinimumQuantity"] = lotsMin > 0 ? Round(lotsMin) : 1d,
                ["QuantityStep"] = lotsStep > 0 ? Round(lotsStep) : 1d,
                ["Enabled"] = true,
            };

            return row;
        }

        // L'arrotondamento descrive il BROKER, non lo strumento: una tabella mappa un conto solo, e
        // un conto non e' a contratti interi per l'oro e a frazioni per il petrolio. Basta quindi un
        // symbol con passo frazionario perche' l'intera tabella sia BrokerVolumeStep (1); solo se
        // nessuno accetta frazioni siamo davanti a un broker a contratti interi (FuturesContracts = 0).
        private static int BuildRoundingMode(List<Dictionary<string, object>> rows)
        {
            foreach (var row in rows)
            {
                object step;
                if (row.TryGetValue("LotsStep", out step) && step is double && (double)step > 0 && (double)step < 1)
                    return 1;
            }

            return 0;
        }

        // ---------------------------------------------------------------------------------------
        // Risoluzione symbol
        // ---------------------------------------------------------------------------------------

        private Symbol TryResolve(MapEntry entry)
        {
            if (entry.Resolved != null)
                return entry.Resolved;

            if (string.IsNullOrWhiteSpace(entry.AccountSymbol))
                return null;

            try
            {
                entry.Resolved = Symbols.GetSymbol(entry.AccountSymbol);
            }
            catch (Exception)
            {
                entry.Resolved = null;
            }

            return entry.Resolved;
        }

        // I nomi cambiano da broker a broker (USOIL.cash, GBPUSDp, DE40.f): quando l'esatto non c'e'
        // conviene dire subito quali nomi gli assomigliano, invece di far ricercare a mano.
        private List<string> FindCandidates(string requested)
        {
            if (string.IsNullOrWhiteSpace(requested))
                return new List<string>();

            var needle = OnlyAlphanumeric(requested);
            if (needle.Length < 3)
                return new List<string>();

            return Symbols
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Where(n => OnlyAlphanumeric(n).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(n => n.Length)
                .Take(5)
                .ToList();
        }

        private string SuggestNames(string requested)
        {
            var candidates = FindCandidates(requested);
            return candidates.Count == 0
                ? string.Empty
                : " Forse: " + string.Join(", ", candidates) + ".";
        }

        private static string OnlyAlphanumeric(string value) =>
            new string(value.Where(char.IsLetterOrDigit).ToArray());

        // ---------------------------------------------------------------------------------------
        // Parsing parametri
        // ---------------------------------------------------------------------------------------

        private List<MapEntry> ParseMap(string raw)
        {
            var result = new List<MapEntry>();
            foreach (var token in SplitCsv(raw))
            {
                var sides = token.Split('=');
                if (sides.Length != 2)
                {
                    _warnings.Add(string.Format("Voce '{0}' ignorata: serve il formato future=CFD:pointValue:valuta.", token));
                    continue;
                }

                var right = sides[1].Split(':');
                if (right.Length < 2)
                {
                    _warnings.Add(string.Format("Voce '{0}' ignorata: manca il point value del future.", token));
                    continue;
                }

                double pointValue;
                if (!double.TryParse(right[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out pointValue) || pointValue <= 0)
                {
                    _warnings.Add(string.Format("Voce '{0}' ignorata: point value '{1}' non valido.", token, right[1]));
                    continue;
                }

                result.Add(new MapEntry
                {
                    FuturesSymbol = sides[0].Trim(),
                    AccountSymbol = right[0].Trim(),
                    FuturesPointValue = pointValue,
                    FuturesCurrency = right.Length > 2 ? right[2].Trim() : "USD",
                });
            }

            return result;
        }

        private static IEnumerable<string> SplitCsv(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Enumerable.Empty<string>();

            return raw
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0);
        }

        // ---------------------------------------------------------------------------------------
        // Output
        // ---------------------------------------------------------------------------------------

        private void PrintTable(List<Dictionary<string, object>> rows)
        {
            Print("PiootooSymbolMultiplierBot: future | CFD | lotSize | valore punto per lotto (conto) | moltiplicatore | min lotti | passo lotti");
            foreach (var row in rows)
            {
                object found;
                if (row.TryGetValue("Found", out found) && found is bool && !(bool)found)
                {
                    Print("  {0,-6} | {1,-10} | NON TROVATO", Show(row, "FuturesSymbol"), Show(row, "AccountSymbol"));
                    continue;
                }

                Print("  {0,-6} | {1,-10} | {2,10} | {3,14} | {4,10} | {5,6} | {6,6}",
                    Show(row, "FuturesSymbol"), Show(row, "AccountSymbol"), Show(row, "LotSize"),
                    Show(row, "PointValuePerLotAccountCcy"), Show(row, "ContractMultiplier"),
                    Show(row, "LotsMin"), Show(row, "LotsStep"));
            }
        }

        private static string Show(Dictionary<string, object> row, string key)
        {
            object value;
            if (!row.TryGetValue(key, out value) || value == null)
                return "-";

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string ResolveFileName(string candidate, string fallback) =>
            string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;

        private string ResolveOutputPath(string candidate, string fallback)
        {
            var folder = OutputFolder;
            if (string.IsNullOrWhiteSpace(folder))
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiootooSymbolMultiplierBot");

            Directory.CreateDirectory(folder);
            return Path.Combine(folder, ResolveFileName(candidate, fallback));
        }

        private static void WriteJson(string path, object payload)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

            var temporary = path + ".tmp";
            File.WriteAllText(temporary, json);
            if (File.Exists(path))
                File.Replace(temporary, path, null);
            else
                File.Move(temporary, path);
        }

        // ---------------------------------------------------------------------------------------
        // Helper
        // ---------------------------------------------------------------------------------------

        // Le proprieta' di Symbol possono lanciare quando manca la quotazione: leggerle una a una
        // dietro un try tiene in piedi il resto della riga invece di perdere tutto il symbol.
        private static T Read<T>(Func<T> reader, T fallback)
        {
            try
            {
                return reader();
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static double ToLots(Symbol symbol, double volumeInUnits, double lotSize)
        {
            var lots = Read(() => symbol.VolumeInUnitsToQuantity(volumeInUnits), 0d);
            if (lots > 0)
                return Round(lots);

            return lotSize > 0 ? Round(volumeInUnits / lotSize) : 0d;
        }

        private static string DescribeLeverage(Symbol symbol)
        {
            var tiers = symbol.DynamicLeverage;
            if (tiers == null || tiers.Count == 0)
                return null;

            return string.Join(" | ", tiers.Select(t => string.Format(
                CultureInfo.InvariantCulture, "fino a {0}: 1:{1}", t.Volume, t.Leverage)));
        }

        private string SafeAccountCurrency() =>
            Read(() => Account.Asset != null ? Account.Asset.Name : Account.Currency, "?");

        private string SafeBrokerName() =>
            Read(() => Account.BrokerName, "?");

        private static double Round(double value) => Math.Round(value, 8, MidpointRounding.AwayFromZero);

        // Dump completo per reflection: il calcolo sopra usa una manciata di proprieta', ma quando un
        // numero non torna serve poter guardare tutto quello che il broker espone senza ricompilare.
        private static Dictionary<string, object> DumpAllProperties(Symbol symbol)
        {
            var result = new Dictionary<string, object>();
            var properties = symbol.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal);

            foreach (var property in properties)
            {
                object value;
                try
                {
                    value = property.GetValue(symbol);
                }
                catch (Exception ex)
                {
                    value = "<errore lettura: " + ex.Message + ">";
                }

                result[property.Name] = NormalizeForJson(value);
            }

            return result;
        }

        private static object NormalizeForJson(object value)
        {
            if (value == null)
                return null;

            var type = value.GetType();
            if (type.IsPrimitive || value is string || value is decimal || value is DateTime || value is DateTimeOffset || value is TimeSpan)
                return value;

            if (type.IsEnum)
                return value.ToString();

            return value.ToString();
        }

        private sealed class MapEntry
        {
            public string FuturesSymbol { get; set; }
            public string AccountSymbol { get; set; }
            public double FuturesPointValue { get; set; }
            public string FuturesCurrency { get; set; }
            public Symbol Resolved { get; set; }
        }
    }
}
