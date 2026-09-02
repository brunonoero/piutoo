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
    // PiootooSymbolMultiplierBotFtmo
    //
    // Variante FTMO del PiootooSymbolMultiplierBot, da eseguire su un conto cTrader FTMO.
    // Rispetto all'originale (tarato sui nomi IC Markets) cambia tre cose, e solo quelle:
    //
    //   1. ogni future porta un ELENCO di nomi alternativi separati da '|'. FTMO ha ereditato dal
    //      mondo MT4/MT5 i nomi con suffisso ('US100.cash', 'GER40.cash', 'USOIL.cash'), ma il
    //      listino cTrader non e' garantito identico: il bot prova i nomi in ordine e tiene il
    //      primo che esiste, invece di far fallire tutta la riga per un suffisso.
    //   2. se nessun alias esiste, e con "Adotta symbol simile" acceso, adotta l'unico symbol del
    //      conto che assomiglia al nome chiesto - segnalandolo come tale, mai in silenzio.
    //   3. scrive anche l'elenco COMPLETO dei symbol del conto: quando un nome non c'e', la
    //      risposta sta nel listino, non in un altro run.
    //
    // Il conto misurato il 2026-09-02 (17188650, "FTMO Platform") e' in USD, non in EUR come dice
    // accounts.json per gli account del gruppo FTMO. Per il moltiplicatore non cambia nulla, perche'
    // si calcola in valuta di QUOTAZIONE (vedi sotto); cambia il valore punto in valuta conto, che
    // infatti resta un dato informativo. L'unico strumento quotato in un'altra valuta e' GER40.cash,
    // in EUR: la conversione la fa il broker, non il sistema.
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
    // Output (cartella di default: %APPDATA%/PiootooSymbolMultiplierBot/ftmo):
    //   symbol-multipliers-ftmo.json          analisi completa: specifiche broker, valori calcolati,
    //                                         avvisi, elenco dei symbol del conto, piu' il dump per
    //                                         reflection di ogni proprieta'
    //   symbol-multipliers-ftmo.mappings.json la voce di conversione GIA' COMPLETA (Code, Name,
    //                                         RoundingMode, Mappings, date): si incolla dentro
    //                                         l'array "Conversions" del symbol-conversions.json in
    //                                         uso (symbol-convertion/ e' la copia di lavoro, il
    //                                         registro vivo e' accounts/), accanto a "cfd-mt4-ftmo"
    //                                         e senza ritoccare i numeri a mano.
    //
    // COME SI ESEGUE
    //   cTrader -> Automate -> nuovo cBot, incollare questo file, Build, poi Add Instance su un
    //   simbolo qualsiasi del conto FTMO (il bot non guarda il simbolo del grafico: se li iscrive
    //   da solo) e Start a mercato APERTO. Serve mercato aperto perche' senza quotazione TickValue
    //   e' zero e il valore punto in valuta conto non e' leggibile; il moltiplicatore, che dipende
    //   dal solo LotSize, esce comunque. Il bot si ferma da solo finita l'attesa.
    //
    // Vedi anche PiootooSymbolInfoDumpBot: quello scarica *tutti* i symbol del conto senza calcoli,
    // questo lavora su un elenco mirato e produce i numeri della tabella di conversione.
    // ------------------------------------------------------------------------------------------

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooSymbolMultiplierBotFtmo : Robot
    {
        // Elenco "future=CFD[|alias|alias]:pointValue:valuta[:prezzoFutureDiRiferimento]".
        //
        // I point value replicano Piootoo.Shared/Configuration/InstrumentRegistry.cs: se li cambi
        // qui senza cambiarli la', i due mondi divergono in silenzio, che e' esattamente cio' che il
        // registro esiste per evitare.
        //
        // L'elenco copre i simboli che hanno ALMENO UNA STRATEGIA in catalogo, e solo quelli: una
        // riga di tabella su uno strumento che nessuno tradera' e' un numero che nessuno verifica.
        // Fra parentesi le strategie PTS al 2026-09-02.
        //
        // Fuori resta @JY (8 strategie): il suo PointValue non e' nel registro ma in
        // InstrumentRegistry.KnownButUnverified, quindi il moltiplicatore non e' calcolabile - e il
        // future 6J quota yen in dollari mentre il CFD USDJPY quota dollari in yen, che e' un
        // inverso, non una scala: non lo sistema ne' PriceScale ne' ContractMultiplier.
        //
        // Gli alias sono in ordine di fiducia: primo il nome VERIFICATO sul conto FTMO cTrader
        // (run del 2026-09-02, conto 17188650, "FTMO Platform"), poi le varianti degli altri broker
        // per non ripartire da zero se il listino cambia. Il bot tiene il primo che esiste e scrive
        // nel mapping il nome vero, non l'alias. FTMO nomina i commodity con suffisso '.c' e gli
        // indici con '.cash'; il gas e' NATGAS.cash, non NGAS.
        //
        // Il quarto campo, opzionale, e' un prezzo di riferimento del future: qui sono le chiusure
        // dell'ultima barra di piootoo-repository/datafeed (maggio 2025), quindi indicative - servono
        // solo a far accorgere il bot di un fattore dieci, non a misurare uno scostamento. Dove il
        // datafeed interno non ha lo strumento il campo resta vuoto: sono proprio gli agricoli e
        // l'heating oil, cioe' i casi in cui centesimi contro dollari fa la differenza, e la scala
        // va guardata sul Bid che il bot stampa.
        private const string DefaultMap =
            "@BP=GBPUSD:62500:USD:1.3454," +                                  // 2 strategie
            "@BTC=BTCUSD|BTCUSD.cash|Bitcoin:5:USD:105060," +                 // 4
            "@CC=COCOA.c|COCOA.cash|COCOA:10:USD," +                          // 4  ($/tonnellata)
            "@CL=USOIL.cash|USOIL|XTIUSD|WTI:1000:USD:60.79," +               // 1
            "@CT=COTTON.c|COTTON.cash|COTTON:500:USD," +                      // 1  (centesimi/lb)
            "@ES=US500.cash|US500|SPX500|SP500:50:USD:5913.75," +             // 13
            "@FDAX=GER40.cash|GER40|DE40|DAX40|GERMANY40:25:EUR:24090," +     // 7  (quotato in EUR)
            "@GC=XAUUSD:100:USD:3313.1," +                                    // 9
            "@HK=HK50.cash|HK50|HSI|HKG33:6.41:USD," +                        // 5  (CFD in HKD: attenzione)
            "@HO=HEATOIL.c|HEATOIL.cash|HEATINGOIL:42000:USD," +              // 8  ($/gallone)
            "@KC=COFFEE.c|COFFEE.cash|COFFEE:375:USD," +                      // 1  (centesimi/lb)
            "@NG=NATGAS.cash|NATGAS.c|NGAS.cash|NGAS|XNGUSD:10000:USD:3.462," +  // 10
            "@NQ=US100.cash|US100|USTEC|NAS100:20:USD:21369," +               // 40
            "@PL=XPTUSD:50:USD:1053.5," +                                     // 1
            "@SB=SUGAR.c|SUGAR.cash|SUGAR:1120:USD," +                        // 1  (centesimi/lb)
            "@YM=US30.cash|US30|DJI30|WS30:5:USD:42278";                      // 9

        [Parameter("Mappa future=CFD|alias:pointValue:valuta[:prezzoFuture]", DefaultValue = DefaultMap, Group = "Strumenti")]
        public string SymbolMap { get; set; }

        [Parameter("Symbol extra da ispezionare (CSV, senza calcolo)", DefaultValue = "", Group = "Strumenti")]
        public string ExtraSymbols { get; set; }

        [Parameter("Secondi di attesa quotazioni", DefaultValue = 15, MinValue = 0, MaxValue = 120, Group = "Strumenti")]
        public int WaitSeconds { get; set; }

        // Adottare un nome per somiglianza e' comodo ma resta un'ipotesi: il bot lo fa solo se il
        // candidato e' UNO, e lo marca (ResolvedBy = "somiglianza") sia nella riga sia negli avvisi.
        // Con piu' candidati preferisce non scegliere e lasciare la riga vuota.
        [Parameter("Adotta symbol simile se il nome esatto manca", DefaultValue = true, Group = "Strumenti")]
        public bool ResolveBySimilarity { get; set; }

        [Parameter("Cartella di output", DefaultValue = "", Group = "Output")]
        public string OutputFolder { get; set; }

        [Parameter("Nome file", DefaultValue = "symbol-multipliers-ftmo.json", Group = "Output")]
        public string FileName { get; set; }

        // Codice nuovo, non "cfd-mt4-ftmo": FTMO su cTrader e' un altro listino, e due conti con
        // nomi, passi e minimi diversi non possono condividere la stessa tabella.
        [Parameter("Codice tabella di conversione", DefaultValue = "cfd-ctrader-ftmo", Group = "Output")]
        public string ConversionCode { get; set; }

        [Parameter("Nome tabella di conversione", DefaultValue = "CFD-ctrader-ftmo", Group = "Output")]
        public string ConversionName { get; set; }

        [Parameter("Elenca tutti i symbol del conto", DefaultValue = true, Group = "Output")]
        public bool DumpAccountSymbols { get; set; }

        private readonly List<string> _warnings = new List<string>();
        private List<MapEntry> _entries;

        protected override void OnStart()
        {
            try
            {
                _entries = ParseMap(SymbolMap);
                foreach (var extra in SplitCsv(ExtraSymbols))
                    _entries.Add(new MapEntry { Requested = extra, Aliases = SplitAliases(extra) });

                if (_entries.Count == 0)
                {
                    Print("PiootooSymbolMultiplierBot-FTMO: nessun symbol da analizzare, controlla i parametri.");
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

                Print("PiootooSymbolMultiplierBot-FTMO: attendo {0}s che arrivino le quotazioni ({1} symbol richiesti).",
                    WaitSeconds, _entries.Count);
                Timer.Start(TimeSpan.FromSeconds(WaitSeconds));
            }
            catch (Exception ex)
            {
                Print("PiootooSymbolMultiplierBot-FTMO: errore fatale in avvio: {0}", ex);
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
                        _warnings.Add(string.Format("Nessuno dei nomi '{0}' esiste su questo conto.{1}",
                            entry.Requested, SuggestNames(entry)));
                        rows.Add(new Dictionary<string, object>
                        {
                            ["AccountSymbol"] = entry.Requested,
                            ["RequestedAliases"] = entry.Aliases,
                            ["FuturesSymbol"] = entry.FuturesSymbol,
                            ["Found"] = false,
                            ["Candidates"] = FindCandidates(entry.Aliases),
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
                    ["AccountNumber"] = Read(() => Account.Number.ToString(CultureInfo.InvariantCulture), "?"),
                    ["AccountCurrency"] = accountCurrency,
                    ["ConversionCode"] = ConversionCode,
                    ["Regola"] = "ContractMultiplier = PointValue(future) / LotSize(CFD), entrambi in valuta di quotazione",
                    ["Warnings"] = _warnings,
                    ["Symbols"] = rows,
                    ["Mappings"] = mappings,
                    // Il listino intero: quando un nome non c'e', la risposta e' qui dentro e non in
                    // un secondo run a mercato chiuso.
                    ["AccountSymbols"] = DumpAccountSymbols ? ListAccountSymbols() : null,
                };

                var path = ResolveOutputPath(FileName, "symbol-multipliers-ftmo.json");
                WriteJson(path, payload);

                // La voce esce completa di date, nella stessa forma delle altre di
                // symbol-conversions.json: si incolla dentro "Conversions" senza aggiungere campi a mano.
                var nowUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                var baseName = Path.GetFileNameWithoutExtension(ResolveFileName(FileName, "symbol-multipliers-ftmo.json"));
                var mappingsPath = ResolveOutputPath(baseName + ".mappings.json", "symbol-multipliers-ftmo.mappings.json");
                WriteJson(mappingsPath, new Dictionary<string, object>
                {
                    ["Code"] = ConversionCode,
                    ["Name"] = string.IsNullOrWhiteSpace(ConversionName) ? ConversionCode : ConversionName,
                    ["RoundingMode"] = BuildRoundingMode(rows),
                    ["Mappings"] = mappings,
                    ["CreatedUtc"] = nowUtc,
                    ["UpdatedUtc"] = nowUtc,
                });

                PrintTable(rows);
                foreach (var warning in _warnings)
                    Print("PiootooSymbolMultiplierBot-FTMO: ATTENZIONE {0}", warning);

                Print("PiootooSymbolMultiplierBot-FTMO: scritti {0} symbol su {1}", rows.Count, path);
                Print("PiootooSymbolMultiplierBot-FTMO: mappings pronti da incollare su {0}", mappingsPath);
            }
            catch (Exception ex)
            {
                Print("PiootooSymbolMultiplierBot-FTMO: errore fatale: {0}", ex);
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
                ["Requested"] = entry.Requested,
                ["MatchedAlias"] = entry.MatchedAlias,
                ["ResolvedBy"] = entry.ResolvedBy,
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

            var priceScale = SuggestPriceScale(entry, symbol, row);

            // Minimo e passo restano per riga, espressi nei contratti del broker (lotti).
            // L'arrotondamento NO: e' una proprieta' della tabella, decisa dal broker una volta
            // sola, e viene scritto sull'oggetto SymbolConversion (vedi BuildRoundingMode).
            // Scriverlo per riga e' la forma deprecata dal 24/08/2026.
            mapping = new Dictionary<string, object>
            {
                ["Symbol"] = entry.FuturesSymbol,
                ["AccountSymbol"] = symbol.Name,
                ["ContractMultiplier"] = Round(multiplier),
                // PriceScale vale 1 quasi sempre: le distanze sono in punti, invarianti fra future e
                // CFD. Serve diverso da 1 solo dove il broker quota il sottostante in un'altra unita',
                // e questo il bot lo puo' dedurre soltanto se gli si passa un prezzo di riferimento
                // del future nel quarto campo della mappa; senza, resta 1 e va confrontato a mano fra
                // il Bid della riga e l'ultima barra del datafeed.
                ["PriceScale"] = priceScale,
                ["MinimumQuantity"] = lotsMin > 0 ? Round(lotsMin) : 1d,
                ["QuantityStep"] = lotsStep > 0 ? Round(lotsStep) : 1d,
                ["Enabled"] = true,
            };

            return row;
        }

        // Confronto fra il prezzo del CFD e un prezzo di riferimento del future: se il rapporto e'
        // una potenza di dieci, il broker quota lo stesso sottostante in un'altra unita' e la scala
        // delle distanze non e' 1. Un rapporto vicino a 1 ma non uguale non e' una scala: e' la
        // differenza fra scadenza e cash, e va lasciata stare.
        private double SuggestPriceScale(MapEntry entry, Symbol symbol, Dictionary<string, object> row)
        {
            if (entry.FuturesReferencePrice <= 0)
                return 1d;

            var bid = Read(() => symbol.Bid, 0d);
            if (bid <= 0)
            {
                _warnings.Add(string.Format(
                    "'{0}': prezzo di riferimento indicato ma Bid non disponibile, PriceScale lasciato a 1.", symbol.Name));
                return 1d;
            }

            var ratio = bid / entry.FuturesReferencePrice;
            row["FuturesReferencePrice"] = entry.FuturesReferencePrice;
            row["BidVsFutureRatio"] = Round(ratio);

            var scale = Math.Pow(10, Math.Round(Math.Log10(ratio)));
            row["PriceScaleSuggested"] = scale;

            if (Math.Abs(scale - 1d) > 1e-9)
                _warnings.Add(string.Format(
                    "'{0}': il CFD quota {1} contro {2} del future, cioe' un fattore {3}. PriceScale scritto a {3}: verificalo prima di usarlo, sposta stop e target.",
                    symbol.Name, Round(bid), entry.FuturesReferencePrice, scale));
            else if (ratio < 0.5 || ratio > 2)
                _warnings.Add(string.Format(
                    "'{0}': Bid {1} lontano dal riferimento {2} ma non di una potenza di dieci. Non e' una scala di prezzo: o il riferimento e' vecchio, o il CFD non e' lo stesso sottostante.",
                    symbol.Name, Round(bid), entry.FuturesReferencePrice));

            return scale;
        }

        // Il listino del conto, in ordine: e' la risposta alla domanda "come si chiama qui?".
        private List<string> ListAccountSymbols()
        {
            return Symbols
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        // Prova gli alias in ordine e tiene il primo che esiste. Solo se nessuno esiste, e se il
        // parametro lo consente, guarda i nomi simili: un candidato solo viene adottato (marcato), da
        // due in su il bot non sceglie, perche' indovinare fra US100 e US100.f cambia il conto.
        private Symbol TryResolve(MapEntry entry)
        {
            if (entry.Resolved != null || entry.Attempted)
                return entry.Resolved;

            entry.Attempted = true;

            foreach (var alias in entry.Aliases)
            {
                var symbol = GetSymbolOrNull(alias);
                if (symbol == null)
                    continue;

                entry.Resolved = symbol;
                entry.MatchedAlias = alias;
                entry.ResolvedBy = alias == entry.Aliases[0] ? "nome esatto" : "alias";
                return entry.Resolved;
            }

            if (!ResolveBySimilarity)
                return null;

            var candidates = FindCandidates(entry.Aliases);
            if (candidates.Count != 1)
                return null;

            var guessed = GetSymbolOrNull(candidates[0]);
            if (guessed == null)
                return null;

            entry.Resolved = guessed;
            entry.MatchedAlias = candidates[0];
            entry.ResolvedBy = "somiglianza";
            _warnings.Add(string.Format(
                "'{0}': nessuno dei nomi chiesti esiste, adottato '{1}' perche' e' l'unico che gli assomiglia. Verificalo nel listino prima di usare la tabella.",
                entry.Requested, candidates[0]));

            return entry.Resolved;
        }

        private Symbol GetSymbolOrNull(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            try
            {
                return Symbols.GetSymbol(name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // I nomi cambiano da broker a broker (USOIL.cash, GBPUSDp, DE40.f): quando nessun alias
        // esiste conviene dire subito quali nomi gli assomigliano, invece di far cercare a mano.
        // La ricerca gira su TUTTI gli alias: 'US100' non compare in 'USTEC', ma 'USTEC' si'.
        private List<string> FindCandidates(IEnumerable<string> requestedNames)
        {
            var result = new List<string>();
            foreach (var requested in requestedNames)
            {
                if (string.IsNullOrWhiteSpace(requested))
                    continue;

                var needle = OnlyAlphanumeric(requested);
                if (needle.Length < 3)
                    continue;

                foreach (var name in Symbols)
                {
                    if (string.IsNullOrWhiteSpace(name) || result.Contains(name) || IsRetiredSymbol(name))
                        continue;

                    if (OnlyAlphanumeric(name).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                        result.Add(name);
                }
            }

            return result.OrderBy(n => n.Length).Take(5).ToList();
        }

        // Su FTMO ogni symbol ha un gemello dismesso ('COCOA.c' e 'COCOA.c_removed'), piu' qualche
        // residuo di import demo. Contarli come candidati faceva sempre due, e con due candidati il
        // bot per prudenza non sceglie: cosi' il fallback per somiglianza non scattava MAI proprio
        // sul broker per cui esiste. Non sono strumenti negoziabili: fuori dalla ricerca.
        private static bool IsRetiredSymbol(string name)
        {
            return name.IndexOf("_removed", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("(Demo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string SuggestNames(MapEntry entry)
        {
            var candidates = FindCandidates(entry.Aliases);
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

                var aliases = SplitAliases(right[0]);
                if (aliases.Count == 0)
                {
                    _warnings.Add(string.Format("Voce '{0}' ignorata: manca il nome del symbol sul conto.", token));
                    continue;
                }

                double referencePrice = 0d;
                if (right.Length > 3)
                    double.TryParse(right[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out referencePrice);

                result.Add(new MapEntry
                {
                    FuturesSymbol = sides[0].Trim(),
                    Requested = right[0].Trim(),
                    Aliases = aliases,
                    FuturesPointValue = pointValue,
                    FuturesCurrency = right.Length > 2 ? right[2].Trim() : "USD",
                    FuturesReferencePrice = referencePrice,
                });
            }

            return result;
        }

        private static List<string> SplitAliases(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            return raw
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();
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
            Print("PiootooSymbolMultiplierBot-FTMO: future | CFD | come risolto | lotSize | valore punto per lotto (conto) | moltiplicatore | min lotti | passo lotti");
            foreach (var row in rows)
            {
                object found;
                if (row.TryGetValue("Found", out found) && found is bool && !(bool)found)
                {
                    Print("  {0,-6} | {1,-14} | NON TROVATO | candidati: {2}",
                        Show(row, "FuturesSymbol"), Show(row, "AccountSymbol"), ShowList(row, "Candidates"));
                    continue;
                }

                Print("  {0,-6} | {1,-14} | {2,-12} | {3,10} | {4,14} | {5,10} | {6,6} | {7,6}",
                    Show(row, "FuturesSymbol"), Show(row, "AccountSymbol"), Show(row, "ResolvedBy"),
                    Show(row, "LotSize"), Show(row, "PointValuePerLotAccountCcy"), Show(row, "ContractMultiplier"),
                    Show(row, "LotsMin"), Show(row, "LotsStep"));
            }
        }

        private static string ShowList(Dictionary<string, object> row, string key)
        {
            object value;
            if (!row.TryGetValue(key, out value) || !(value is List<string>))
                return "-";

            var list = (List<string>)value;
            return list.Count == 0 ? "nessuno" : string.Join(", ", list);
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
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiootooSymbolMultiplierBot", "ftmo");

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
            // Testo chiesto dall'utente, alias compresi: e' quello che finisce nella riga quando il
            // symbol non esiste, cosi' l'avviso dice cosa e' stato cercato davvero.
            public string Requested { get; set; }
            public List<string> Aliases { get; set; }
            public string FuturesSymbol { get; set; }
            public double FuturesPointValue { get; set; }
            public string FuturesCurrency { get; set; }
            public double FuturesReferencePrice { get; set; }
            public string MatchedAlias { get; set; }
            public string ResolvedBy { get; set; }
            public bool Attempted { get; set; }
            public Symbol Resolved { get; set; }
        }
    }
}
