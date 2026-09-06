using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Lo spread denaro/lettera applicato all'ingresso, e la tabella misurata da cui esce.
///
/// <para>Sono test di <b>convenzione</b> come <see cref="TrailingStepAndGapFillTests"/>: guidano
/// <see cref="PiootooTradingService"/> con barre sintetiche e leggono il trade chiuso. Le barre sono
/// il lato <b>Bid</b> — e' quello che <c>MarketData.GetBars</c> consegna al cBot raccoglitore — e lo
/// spread sposta il solo prezzo di ingresso.</para>
///
/// <para>Il fatto che i test devono fissare non e' "l'ingresso costa qualcosa", che sarebbe ovvio,
/// ma quello di <c>docs/decisioni.md</c> 2026-08-06: <b>la perdita quando lo stop salta resta quella
/// dichiarata dalla strategia</b> — stop e target si spostano insieme all'ingresso — <b>mentre il
/// prezzo deve muoversi di meno per farlo saltare</b>. Stessa perdita, piu' stop.</para>
/// </summary>
public sealed class EntrySpreadTests
{
    private const string Code = "PTS_NQ_PCH_001_15";

    private static readonly DateTime IstanteSegnale = new(2024, 1, 2, 13, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime IstanteIngresso = IstanteSegnale.AddMinutes(15);

    /// <summary>
    /// Un long entra sull'Ask. La barra (Bid) apre a 100 e il livello dello stop buy e' 100, quindi
    /// senza spread si entra a 100; con due punti di spread si entra a 102, e lo stop dichiarato di
    /// 5 punti (100 dollari su NQ, 20 $/punto) sta a 97 invece che a 95.
    /// </summary>
    [Theory]
    [InlineData(0.0, 100.0, 95.0)]
    [InlineData(2.0, 102.0, 97.0)]
    public void UnLongEntraSullAskELoStopSiSpostaConLui(
        double spread, double ingressoAtteso, double stopAtteso)
    {
        var service = Motore(spread);
        ApriLong(service, (decimal)ingressoAtteso);

        // La barra scende fino a 94,9: prende lo stop in entrambi i casi, ma a livelli diversi.
        Barra(service, 2, open: 99m, high: 99m, low: 94.9m, close: 95m);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.StopLoss, trade.ExitReason);
        Assert.Equal((decimal)stopAtteso, trade.ExitPrice);

        // Il punto della decisione 2026-08-06: la perdita e' la stessa, cinque punti, con e senza
        // spread. Lo spread non rende piu' cara la singola perdita, rende piu' facile subirla.
        Assert.Equal(-5m, trade.ExitPrice - trade.EntryPrice);
    }

    /// <summary>
    /// Il costo dello spread e' il margine operativo che si perde, e si vede su una barra che
    /// <b>non</b> arriva allo stop senza spread e ci arriva con lo spread. Stesse identiche barre,
    /// un solo numero diverso: senza spread la posizione sopravvive, con due punti muore.
    /// </summary>
    [Theory]
    [InlineData(0.0, false)]
    [InlineData(2.0, true)]
    public void ConLoSpreadLoStopSaltaSuUnaBarraCheSenzaNonLoAvrebbeToccato(double spread, bool chiuso)
    {
        var service = Motore(spread);
        ApriLong(service, spread == 0d ? 100m : 102m);

        // Minimo 96,5: sopra lo stop a 95 (senza spread), sotto quello a 97 (con spread).
        Barra(service, 2, open: 99m, high: 99m, low: 96.5m, close: 98m);

        if (chiuso)
        {
            var trade = Assert.Single(service.GetClosedTrades());
            Assert.Equal(TradeExitReason.StopLoss, trade.ExitReason);
            Assert.Equal(97m, trade.ExitPrice);
        }
        else
        {
            Assert.Empty(service.GetClosedTrades());
        }
    }

    /// <summary>
    /// Lo stesso dal lato corto, rovesciato: si entra sul Bid e l'uscita si valuta sull'Ask, che qui
    /// diventa un ingresso spostato di <c>-spread</c>. Il P&amp;L e gli istanti sono gli stessi del
    /// modello a due lati, e il conto della perdita torna identico.
    /// </summary>
    [Theory]
    [InlineData(0.0, 100.0, 105.0)]
    [InlineData(2.0, 98.0, 103.0)]
    public void UnoShortEntraSulBidELoStopSiSpostaConLui(
        double spread, double ingressoAtteso, double stopAtteso)
    {
        var service = Motore(spread);

        service.ProcessSignals(
            [SegnaleDiIngresso(SignalType.Sell)],
            Prezzi(101m),
            Barre(IstanteSegnale, 101m, 102m, 101m, 101m),
            IstanteSegnale);
        service.UpdateMarketPrices(
            Prezzi(100m), Barre(IstanteIngresso, 101m, 101m, 100m, 100m), IstanteIngresso);

        var posizione = service.GetExecutionSnapshot(Code, "NQ", IstanteIngresso).Position;
        Assert.NotNull(posizione);
        Assert.Equal((decimal)ingressoAtteso, posizione!.EntryPrice);

        Barra(service, 2, open: 101m, high: 105.1m, low: 101m, close: 105m);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.StopLoss, trade.ExitReason);
        Assert.Equal((decimal)stopAtteso, trade.ExitPrice);
        Assert.Equal(-5m, trade.EntryPrice - trade.ExitPrice);
    }

    /// <summary>
    /// Un simbolo senza misura non paga niente: la tabella e' un elenco di quello che si e'
    /// misurato, non una regola che vale per tutti. E' il caso che il report deve rendere visibile —
    /// una strategia che gira gratis perche' il suo strumento non e' mai stato misurato.
    /// </summary>
    [Fact]
    public void UnSimboloNonMisuratoEntraAlPrezzoDelFeed()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        service.SpreadPoints["@ES"] = 1.5m;

        ApriLong(service, 100m);
    }

    // ---------------------------------------------------------------------------------------
    // La tabella misurata
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Il caricamento del CSV di <c>PiootooSpreadDumpBot</c>: righe di commento saltate, statistica
    /// scelta per colonna, simboli normalizzati, <c>truncated</c> segnalato senza fermare il run.
    /// </summary>
    [Theory]
    [InlineData(SpreadStatistic.Median, 2.0, 0.5)]
    [InlineData(SpreadStatistic.Mean, 3.25, 0.75)]
    [InlineData(SpreadStatistic.P90, 6.0, 1.25)]
    public void LaTabellaLeggeLaColonnaDellaStatisticaChiesta(
        SpreadStatistic statistica, double nq, double es)
    {
        using var radice = new CartellaTemporanea();
        radice.ScriviMisura("FTMO", MisuraDiEsempio);

        var tabella = SpreadTable.Load(radice.Path, "FTMO", statistica);

        // Le chiavi sono normalizzate come nel resto del motore (StrategyKeys.NormalizeSymbol):
        // niente '@', maiuscole. E' quello che ApplySpread cerca, e quello che il report affianca
        // ai simboli del run.
        Assert.Equal((decimal)nq, tabella.Points["NQ"]);
        Assert.Equal((decimal)es, tabella.Points["ES"]);
        Assert.Equal("FTMO", tabella.Broker);
        Assert.Contains("FTMO", tabella.Describe());

        // @GC ha truncated=true e @CL non ha misura: due avvisi, nessuna eccezione.
        Assert.Contains(tabella.Warnings, warning => warning.StartsWith("GC", StringComparison.Ordinal));
        Assert.Contains(tabella.Warnings, warning => warning.StartsWith("CL", StringComparison.Ordinal));
        Assert.False(tabella.Points.ContainsKey("CL"));
    }

    /// <summary>
    /// Un broker senza misura fa fallire l'avvio: e' la stessa regola del datafeed mancante. Un run
    /// che ripiegasse in silenzio su "nessuno spread" sarebbe indistinguibile da uno con lo spread,
    /// e varrebbe un'altra cosa.
    /// </summary>
    [Fact]
    public void UnBrokerSenzaMisuraNonRipiegaSuNessunoSpread()
    {
        using var radice = new CartellaTemporanea();
        radice.ScriviMisura("FTMO", MisuraDiEsempio);

        Assert.Throws<DirectoryNotFoundException>(
            () => SpreadTable.Load(radice.Path, "ICMARKETS", SpreadStatistic.Median));
    }

    /// <summary>Un file della versione vecchia del bot non ha la colonna: si dice quale manca.</summary>
    [Fact]
    public void UnFileSenzaLaColonnaAttesaSpiegaCosaManca()
    {
        using var radice = new CartellaTemporanea();
        radice.ScriviMisura("FTMO", "broker,symbol,ticks,avgSpread\nFTMO,@NQ,10,2.0\n");

        var errore = Assert.Throws<InvalidDataException>(
            () => SpreadTable.Load(radice.Path, "FTMO", SpreadStatistic.Median));
        Assert.Contains("p50Spread", errore.Message, StringComparison.Ordinal);
    }

    /// <summary>Uno spread negativo non e' una misura, e' un feed rotto: si rifiuta.</summary>
    [Fact]
    public void UnoSpreadNegativoNelFileFermaIlCaricamento()
    {
        using var radice = new CartellaTemporanea();
        radice.ScriviMisura("FTMO", "broker,symbol,p50Spread\nFTMO,@NQ,-1\n");

        Assert.Throws<InvalidDataException>(
            () => SpreadTable.Load(radice.Path, "FTMO", SpreadStatistic.Median));
    }

    /// <summary>
    /// Uscita di <c>PiootooSpreadDumpBot</c> ridotta alle colonne che il caricatore legge, con le
    /// righe di commento che il bot scrive in testa.
    /// </summary>
    private const string MisuraDiEsempio =
        "# Piootoo Spread Dump v2.0.0 — broker FTMO (conto 123), finestra UTC 2026-08-01 -> 2026-08-31\n" +
        "# spread = ask - bid.\n" +
        "broker,symbol,brokerSymbol,ticks,p50Spread,avgSpread,p90Spread,truncated,note\n" +
        "FTMO,@NQ,NAS100,1000,2.0,3.25,6.0,false,\n" +
        "FTMO,@ES,SP500,900,0.5,0.75,1.25,false,\n" +
        "FTMO,@GC,GOLD,800,0.3,0.4,0.6,true,\n" +
        "FTMO,@CL,OIL,0,,,,false,nessun tick nella finestra\n";

    private sealed class CartellaTemporanea : IDisposable
    {
        public CartellaTemporanea()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "piootoo-spread-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void ScriviMisura(string broker, string contenuto)
        {
            var cartella = System.IO.Path.Combine(Path, broker);
            Directory.CreateDirectory(cartella);
            File.WriteAllText(
                System.IO.Path.Combine(cartella, $"{broker}_spread-by-symbol_20260801-20260831.csv"),
                contenuto);
        }

        /// <summary>Il file gemello per ora, con la stessa finestra nel nome se non se ne chiede un'altra.</summary>
        public void ScriviMisuraPerOra(string broker, string contenuto, string finestra = "20260801-20260831")
        {
            var cartella = System.IO.Path.Combine(Path, broker);
            Directory.CreateDirectory(cartella);
            File.WriteAllText(
                System.IO.Path.Combine(cartella, $"{broker}_spread-by-hour_{finestra}.csv"),
                contenuto);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Una cartella temporanea che resta non fa fallire un test.
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    // La risoluzione per ora
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Con la tabella oraria si paga il valore dell'<b>ora dell'ingresso</b>, non un riassunto della
    /// giornata. Le altre 23 ore portano un numero molto diverso apposta: se il motore leggesse la
    /// costante, il massimo o la prima casella, il test lo vedrebbe.
    /// </summary>
    [Theory]
    [InlineData(3.0, 103.0)]
    [InlineData(0.0, 100.0)]
    public void SiPagaLoSpreadDellOraDellIngresso(double spreadDelleTredici, double ingressoAtteso)
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        // La costante c'e' e vale altro: con le ore non deve contare per un simbolo che le ha.
        service.SpreadPoints["NQ"] = 7m;
        service.SpreadPointsByHour["NQ"] = Ore(spreadDelleTredici, resto: 9m);

        // L'ingresso cade alle 13:15 UTC, cioe' nel bucket delle 13.
        Assert.Equal(13, IstanteIngresso.Hour);
        ApriLong(service, (decimal)ingressoAtteso);
    }

    /// <summary>
    /// Un simbolo senza righe orarie paga la costante: la tabella per ora non e' un interruttore del
    /// run ma una risoluzione piu' fine dove la misura ce l'ha, e un simbolo scoperto non deve
    /// diventare gratis — sarebbe il buco che somiglia a un regalo.
    /// </summary>
    [Fact]
    public void UnSimboloSenzaOrePagaLaCostante()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        service.SpreadPoints["NQ"] = 2m;
        service.SpreadPointsByHour["ES"] = Ore(9d, resto: 9m);

        ApriLong(service, 102m);
    }

    /// <summary>
    /// Il caricamento del file per ora: valori dell'ora giusta, ore senza misura ripiegate sulla
    /// costante del simbolo e dichiarate, simboli senza righe orarie lasciati alla costante.
    /// </summary>
    [Fact]
    public void LaTabellaPerOraLeggeIlFileGemelloERipiegaSulleOreVuote()
    {
        using var radice = new CartellaTemporanea();
        radice.ScriviMisura("FTMO", MisuraDiEsempio);
        radice.ScriviMisuraPerOra("FTMO", MisuraOrariaDiEsempio);

        var tabella = SpreadTable.Load(
            radice.Path, "FTMO", SpreadStatistic.Median, SpreadResolution.PerHour);

        var ore = tabella.PointsByHour["NQ"];
        Assert.Equal(24, ore.Length);
        Assert.Equal(1.0m, ore[13]);
        Assert.Equal(12.0m, ore[22]);

        // Le 22 ore senza misura prendono la costante per simbolo (2,0 per NQ), non zero.
        Assert.Equal(2.0m, ore[3]);
        Assert.Contains(tabella.Warnings, warning => warning.Contains("22 ore su 24", StringComparison.Ordinal));

        // ES e GC hanno la costante ma nessuna riga oraria: restano fuori dalla tabella per ora, e
        // ApplySpread li serve dalla costante.
        Assert.False(tabella.PointsByHour.ContainsKey("ES"));
        Assert.Equal(0.5m, tabella.Points["ES"]);
        Assert.Contains(tabella.Warnings, warning => warning.StartsWith("ES: nessuna riga", StringComparison.Ordinal));

        Assert.Equal(SpreadResolution.PerHour, tabella.Resolution);
        Assert.Contains("per ora UTC", tabella.Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Il file per ora e' quello <b>gemello</b>, non il piu' recente: i due escono dallo stesso run
    /// del bot e la costante di ripiego deve venire dagli stessi tick delle ore. Qui accanto c'e' un
    /// file per ora di un'altra finestra, scritto dopo: non deve vincere, o il run mescolerebbe due
    /// misure senza dirlo.
    /// </summary>
    [Fact]
    public void IlFilePerOraEQuelloDellaStessaMisuraNonIlPiuRecente()
    {
        using var radice = new CartellaTemporanea();
        radice.ScriviMisura("FTMO", MisuraDiEsempio);
        radice.ScriviMisuraPerOra("FTMO", MisuraOrariaDiEsempio);
        radice.ScriviMisuraPerOra(
            "FTMO",
            "broker,symbol,hourUtc,p50Spread\nFTMO,@NQ,13,99.0\n",
            finestra: "20260901-20260930");

        var tabella = SpreadTable.Load(
            radice.Path, "FTMO", SpreadStatistic.Median, SpreadResolution.PerHour);

        Assert.Equal(1.0m, tabella.PointsByHour["NQ"][13]);
    }

    /// <summary>
    /// Chiedere le ore senza il file delle ore fa fallire l'avvio: e' la regola del datafeed
    /// mancante. Ripiegare in silenzio sulla costante darebbe un run che <i>dichiara</i> la
    /// risoluzione oraria e non l'ha applicata, cioe' il caso peggiore — un summary che mente.
    /// </summary>
    [Fact]
    public void SenzaIlFilePerOraIlRunNonRipiegaSullaCostante()
    {
        using var radice = new CartellaTemporanea();
        radice.ScriviMisura("FTMO", MisuraDiEsempio);

        var errore = Assert.Throws<FileNotFoundException>(
            () => SpreadTable.Load(radice.Path, "FTMO", SpreadStatistic.Median, SpreadResolution.PerHour));
        Assert.Contains("spread-by-hour", errore.Message, StringComparison.Ordinal);
    }

    /// <summary>La risoluzione per simbolo non tocca il file per ora: resta il comportamento di prima.</summary>
    [Fact]
    public void LaRisoluzionePerSimboloNonLeggeLeOre()
    {
        using var radice = new CartellaTemporanea();
        radice.ScriviMisura("FTMO", MisuraDiEsempio);

        var tabella = SpreadTable.Load(radice.Path, "FTMO", SpreadStatistic.Median);

        Assert.Empty(tabella.PointsByHour);
        Assert.Equal(SpreadResolution.PerSymbol, tabella.Resolution);
    }

    /// <summary>
    /// Ventiquattro ore con un valore diverso in quella dell'ingresso: e' l'impalcatura di
    /// <see cref="SiPagaLoSpreadDellOraDellIngresso"/>.
    /// </summary>
    private static decimal[] Ore(double allIngresso, decimal resto)
    {
        var ore = new decimal[24];
        for (var ora = 0; ora < ore.Length; ora++)
            ore[ora] = resto;

        ore[IstanteIngresso.Hour] = (decimal)allIngresso;
        return ore;
    }

    /// <summary>
    /// Uscita di <c>PiootooSpreadDumpBot</c> per ora, ridotta: solo <c>@NQ</c>, e solo due ore
    /// quotate. Le altre 22 sono righe assenti, come nel file vero quando il mercato e' chiuso.
    /// </summary>
    private const string MisuraOrariaDiEsempio =
        "# Piootoo Spread Dump v2.0.0 — broker FTMO, finestra UTC 2026-08-01 -> 2026-08-31\n" +
        "# hourUtc = ora UTC di apertura del bucket (0..23).\n" +
        "broker,symbol,hourUtc,ticks,p50Spread,avgSpread,p90Spread\n" +
        "FTMO,@NQ,13,500,1.0,1.2,2.0\n" +
        "FTMO,@NQ,22,80,12.0,13.5,20.0\n";

    // ---------------------------------------------------------------------------------------
    // La scheda nel report
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Il report elenca <b>tutti</b> i simboli del run, non i soli misurati: un simbolo senza spread
    /// e' un'assenza di misura, e una riga mancante si leggerebbe come "non c'era niente da dire".
    /// E' esattamente il caso da vedere — una strategia che gira gratis perche' il suo strumento non
    /// e' mai stato misurato.
    /// </summary>
    [Fact]
    public void IlReportElencaAncheISimboliNonMisurati()
    {
        var html = ScriviReport(new BacktestSpreadReportInfo(
            "FTMO · p50Spread · misura.csv (2026-08-31)",
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["NQ"] = 2m }));

        Assert.Contains("Spread applicato all'ingresso", html, StringComparison.Ordinal);
        // A pezzi: HtmlEncode trasforma il separatore "·" in un'entita' numerica.
        Assert.Contains("FTMO", html, StringComparison.Ordinal);
        Assert.Contains("p50Spread", html, StringComparison.Ordinal);
        Assert.Contains("<td>NQ</td><td>2</td>", html, StringComparison.Ordinal);
        Assert.Contains("non misurato", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un run senza spread lo dichiara invece di tacere: il silenzio si leggerebbe come "lo spread
    /// c'era, non l'abbiamo scritto", ed e' il malinteso che rende due cartelle indistinguibili.
    /// </summary>
    [Fact]
    public void IlReportDichiaraAncheLAssenzaDiSpread()
    {
        var vuoto = new BacktestSpreadReportInfo("nessuno", new Dictionary<string, decimal>());

        Assert.Contains("Spread: nessuno", ScriviReport(vuoto), StringComparison.Ordinal);
    }

    /// <summary>
    /// Nessuna informazione NON e' "nessuno spread": e' il report ricostruito da un run dell'engine
    /// esterno, dove i trade vengono da un conto vero e lo spread l'ha pagato il broker. Li' il
    /// report tace, perche' dichiarare "nessuno" sarebbe falso.
    /// </summary>
    [Fact]
    public void IlReportRicostruitoNonDichiaraNientePerLoSpread()
    {
        Assert.DoesNotContain("Spread", ScriviReport(spread: null), StringComparison.Ordinal);
    }

    /// <summary>Scrive un report su un run vuoto con due simboli e ne restituisce l'HTML.</summary>
    private static string ScriviReport(BacktestSpreadReportInfo? spread)
    {
        var risultato = new BacktestingResult
        {
            SetupName = "spread",
            StrategiesInfo =
            [
                new StrategyInfo { Name = Code, StrategyCode = Code, Symbol = "@NQ" },
                new StrategyInfo { Name = "PTS_ES_TFM_001_60", StrategyCode = "PTS_ES_TFM_001_60", Symbol = "@ES" }
            ]
        };

        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "piootoo-report-" + Guid.NewGuid().ToString("N") + ".html");
        try
        {
            BacktestHtmlReport.Write(path, risultato, [], spread: spread);
            return File.ReadAllText(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---------------------------------------------------------------------------------------
    // Impalcatura
    // ---------------------------------------------------------------------------------------

    private static PiootooTradingService Motore(double spread)
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        if (spread > 0d)
            service.SpreadPoints["NQ"] = (decimal)spread;

        return service;
    }

    /// <summary>
    /// Apre un long con uno stop buy a 100 servito sulla seconda barra e verifica il prezzo di
    /// ingresso: e' li' che lo spread si vede, ed e' il presupposto di tutto il resto.
    /// </summary>
    private static void ApriLong(PiootooTradingService service, decimal ingressoAtteso)
    {
        service.ProcessSignals(
            [SegnaleDiIngresso(SignalType.Buy)],
            Prezzi(99m),
            Barre(IstanteSegnale, 99m, 99m, 98m, 99m),
            IstanteSegnale);

        service.UpdateMarketPrices(
            Prezzi(100m), Barre(IstanteIngresso, 100m, 100m, 99m, 100m), IstanteIngresso);

        var posizione = service.GetExecutionSnapshot(Code, "NQ", IstanteIngresso).Position;
        Assert.NotNull(posizione);
        Assert.Equal(ingressoAtteso, posizione!.EntryPrice);
    }

    private static void Barra(
        PiootooTradingService service, int indice, decimal open, decimal high, decimal low, decimal close)
    {
        var istante = IstanteSegnale.AddMinutes(15 * indice);
        service.UpdateMarketPrices(Prezzi(close), Barre(istante, open, high, low, close), istante);
    }

    private static TradeSignal SegnaleDiIngresso(SignalType tipo) => new()
    {
        Date = IstanteSegnale,
        Type = tipo,
        Price = 100m,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1m,
        OrderType = TradeOrderType.Stop,
        ValidFromUtc = IstanteIngresso,
        ExpiresAtUtc = IstanteIngresso,
        // 100 dollari su NQ, che vale 20 $/punto: cinque punti di stop.
        StopLossMoneyPerFutureContract = 100m
    };

    private static Dictionary<string, decimal> Prezzi(decimal prezzo) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = prezzo };

    private static Dictionary<string, OhlcvData> Barre(
        DateTime istante, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData
            {
                DateTime = istante, Open = open, High = high, Low = low, Close = close, Volume = 1m
            }
        };
}
