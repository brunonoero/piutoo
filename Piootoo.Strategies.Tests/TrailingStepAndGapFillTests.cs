using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le due convenzioni di uscita nate dal confronto <c>compare-0005</c> fra backtest interno e conto
/// vero cTrader (voce del 2026-08-28 in <c>docs/decisioni.md</c>). Nessuna delle due cambia quali
/// ingressi il motore fa: cambiano il prezzo e l'istante a cui li chiude, che e' esattamente dove i
/// due sistemi si scostavano — le uscite in trailing valevano 2.605 $/trade sul conto vero contro
/// 522 nel backtest.
///
/// <para>Sono test di <b>convenzione</b>, non di strategia: guidano
/// <see cref="PiootooTradingService"/> con barre sintetiche e leggono il trade chiuso. Le distanze
/// sono dichiarate in denaro per contratto e il motore le converte al valore punto del simbolo — su
/// NQ $20 per punto, quindi $200 sono 10 punti di trailing e $100 sono 5 punti di stop.</para>
/// </summary>
public sealed class TrailingStepAndGapFillTests
{
    private const string Code = "PTS_NQ_PCH_001_15";

    private static readonly DateTime IstanteSegnale = new(2024, 1, 2, 13, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime IstanteIngresso = IstanteSegnale.AddMinutes(15);

    /// <summary>
    /// Il picco favorevole segue il prezzo a scatti di una frazione della distanza di trailing,
    /// come fa il cBot col proprio passo minimo.
    ///
    /// <para>Stesse identiche barre, un solo numero diverso. A passo <b>spento</b> — il
    /// comportamento pre-3.11.0 — il picco insegue anche il miglioramento da mezzo punto della
    /// terza barra, lo stop sale a 95,5 e il ritracciamento successivo lo prende. A passo
    /// <b>acceso</b> quel mezzo punto non muove niente (serve un punto intero, un decimo di 10), lo
    /// stop resta a 95 e la posizione sopravvive una barra in piu'. E' l'intera differenza fra i due
    /// motori: l'engine era pessimista perche' ricalcolava il livello dal picco a ogni barra.</para>
    /// </summary>
    [Theory]
    [InlineData(0.10, 95.0, 5)] // passo di un punto: il picco resta a 105, uscita sulla quinta barra
    [InlineData(0.0, 95.5, 4)]  // nessun passo: il picco sale a 105,5, uscita sulla quarta
    public void IlTrailingSegueIlPiccoAScatti(double passo, double uscitaAttesa, int barraDiUscita)
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        service.TrailingMinStepFraction = (decimal)passo;

        // Stop loss volutamente larghissimo (50 punti, livello 50): non deve mai essere lui il
        // livello protettivo, altrimenti il test non parlerebbe piu' del trailing.
        ApriLong(service, stopDollari: 1000m, trailingDollari: 200m);

        // 105: il picco sale in entrambi i casi, cinque punti sono oltre qualunque passo.
        Barra(service, 2, open: 100m, high: 105m, low: 100m, close: 105m);
        // 105,5: mezzo punto. Sotto il passo da un punto, sopra il passo spento.
        Barra(service, 3, open: 105m, high: 105.5m, low: 105m, close: 105.5m);
        // Ritracciamento a 95,2: prende lo stop a 95,5 e non quello a 95.
        Barra(service, 4, open: 105.5m, high: 105.5m, low: 95.2m, close: 95.4m);
        // Ritracciamento a 94,9: prende anche lo stop a 95.
        Barra(service, 5, open: 95.4m, high: 95.6m, low: 94.9m, close: 95m);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.TrailingStop, trade.ExitReason);
        Assert.Equal((decimal)uscitaAttesa, trade.ExitPrice);
        Assert.Equal(IstanteSegnale.AddMinutes(15 * barraDiUscita), trade.ExitDate);
    }

    /// <summary>
    /// Uno stop protettivo su una barra che <b>apre</b> gia' oltre il livello si serve
    /// all'apertura: al livello, in quel momento, non c'era nessuno. L'ingresso applicava gia'
    /// questa convenzione (<c>max(open, livello)</c>), l'uscita no, ed erano asimmetrici.
    /// </summary>
    [Theory]
    // Gap: la barra apre a 90, cinque punti sotto lo stop a 95, e li' si riempie.
    [InlineData(90.0, 91.0, 89.0, 90.0, 90.0)]
    // Nessun gap: la barra apre sopra e attraversa il livello, che si riempie al livello.
    [InlineData(98.0, 98.0, 94.0, 95.0, 95.0)]
    public void LoStopOriginaleDiUnLongSiRiempieAllAperturaSullaBarraInGap(
        double open, double high, double low, double close, double uscitaAttesa)
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        ApriLong(service, stopDollari: 100m, trailingDollari: null); // 5 punti: livello 95
        Barra(service, 2, (decimal)open, (decimal)high, (decimal)low, (decimal)close);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.StopLoss, trade.ExitReason);
        Assert.Equal((decimal)uscitaAttesa, trade.ExitPrice);
    }

    /// <summary>Lo stesso dal lato corto: la barra apre sopra lo stop e lo stop si serve li'.</summary>
    [Fact]
    public void LoStopOriginaleDiUnoShortSiRiempieAllAperturaSullaBarraInGap()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        // Stop di vendita a 100 con il mercato a 101: il livello e' dal lato giusto.
        service.ProcessSignals(
            [SegnaleDiIngresso(SignalType.Sell, stopDollari: 100m)],
            Prezzi(101m),
            Barre(IstanteSegnale, 101m, 102m, 101m, 101m),
            IstanteSegnale);
        service.UpdateMarketPrices(
            Prezzi(100m), Barre(IstanteIngresso, 101m, 101m, 100m, 100m), IstanteIngresso);

        var posizione = service.GetExecutionSnapshot(Code, "NQ", IstanteIngresso).Position;
        Assert.NotNull(posizione);
        Assert.Equal(100m, posizione!.EntryPrice); // livello protettivo a 105

        Barra(service, 2, open: 110m, high: 111m, low: 109m, close: 110m);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.StopLoss, trade.ExitReason);
        Assert.Equal(110m, trade.ExitPrice);
    }

    /// <summary>
    /// Il gap vale per lo <b>stop originale</b> e per nessun altro livello. Un trailing puo' essere
    /// nato dall'estremo della barra in corso — e' il caso di
    /// <c>PtsPriceChannelTests.Engine_ClosesLongAtTrailingStopFromFavorableHigh</c>, dove lo stop
    /// nasce dal massimo di 170 di una barra aperta a 101 — quindi confrontarlo con la propria
    /// apertura lo farebbe riempire a un prezzo che precede il livello stesso.
    /// </summary>
    [Fact]
    public void IlTrailingNonSiRiempieAllApertura()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        ApriLong(service, stopDollari: 1000m, trailingDollari: 200m);

        // Picco a 120: il trailing sale a 110 e la barra non lo tocca (minimo 111).
        Barra(service, 2, open: 112m, high: 120m, low: 111m, close: 118m);
        // La barra dopo apre a 100, dieci punti sotto il trailing. Si esce comunque a 110.
        Barra(service, 3, open: 100m, high: 101m, low: 99m, close: 100m);

        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.TrailingStop, trade.ExitReason);
        Assert.Equal(110m, trade.ExitPrice);
    }

    /// <summary>
    /// Apre un long a 100 con uno stop buy servito sulla seconda barra e lascia la posizione
    /// aperta: le barre successive le mette il test.
    /// </summary>
    private static void ApriLong(
        PiootooTradingService service, decimal stopDollari, decimal? trailingDollari)
    {
        service.ProcessSignals(
            [SegnaleDiIngresso(SignalType.Buy, stopDollari, trailingDollari)],
            Prezzi(99m),
            Barre(IstanteSegnale, 99m, 99m, 98m, 99m),
            IstanteSegnale);

        service.UpdateMarketPrices(
            Prezzi(100m), Barre(IstanteIngresso, 99m, 100m, 99m, 100m), IstanteIngresso);

        var posizione = service.GetExecutionSnapshot(Code, "NQ", IstanteIngresso).Position;
        Assert.NotNull(posizione);
        Assert.Equal(100m, posizione!.EntryPrice);
    }

    private static void Barra(
        PiootooTradingService service, int indice, decimal open, decimal high, decimal low, decimal close)
    {
        var istante = IstanteSegnale.AddMinutes(15 * indice);
        service.UpdateMarketPrices(Prezzi(close), Barre(istante, open, high, low, close), istante);
    }

    private static TradeSignal SegnaleDiIngresso(
        SignalType tipo, decimal stopDollari, decimal? trailingDollari = null) => new()
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
            StopLossMoneyPerFutureContract = stopDollari,
            TrailingStopMoneyPerFutureContract = trailingDollari
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
