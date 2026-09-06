using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un ordine <c>next bar</c> vive la barra <b>che arriva davvero</b>, non l'istante che
/// <c>EasyLib.EstimateNextBarUtc</c> ha proiettato.
///
/// <para><b>Perché questi test esistono.</b> Al momento del segnale la barra successiva non esiste
/// ancora, quindi <c>ValidFromUtc</c> nasce come <c>barTime + timeframe</c>. Attraverso un buco
/// della serie — fine settimana, festività, pausa di sessione — quella proiezione cade dove non
/// c'è nessuna barra, e con la sola scadenza a orologio l'ordine moriva prima che la barra che
/// <c>next bar</c> nomina fosse arrivata: nasceva e spariva senza che una sola barra lo guardasse.
/// Misurato sui feed interni, le barre seguite da un buco sono il <b>20,2%</b> su <c>@NQ_1440</c>
/// (è sistematicamente il segnale della sessione di venerdì, quello che deve operare sul lunedì),
/// il 15,0% su <c>@FDAX_240</c>, il 3,9% sui 4h CME e circa il 2% sugli intraday.</para>
///
/// <para>La regressione opposta è altrettanto importante ed è coperta da
/// <see cref="PendingOrderBarLifetimeTests"/>: senza buchi l'ordine deve continuare a morire dopo
/// la propria barra, altrimenti si riapre il fill fantasma.</para>
/// </summary>
public sealed class PendingOrderAcrossGapTests
{
    private const string Code = "PTS_TEST_60";

    // Chiusura del fine settimana: ultima barra oraria del venerdì, prima della domenica sera.
    private static readonly DateTime UltimaDelVenerdi = new(2025, 2, 7, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ProiezioneNelVuoto = new(2025, 2, 7, 21, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PrimaDellaDomenica = new(2025, 2, 9, 23, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Il caso che il motore perdeva: il livello viene superato sulla prima barra dopo il buco,
    /// che è esattamente la barra su cui l'ordine deve vivere.
    /// </summary>
    [Fact]
    public void AttraversoIlBuco_LOrdineViveLaPrimaBarraCheArrivaDavvero()
    {
        var service = Armato();

        // Tick dentro il vuoto: nessuna barra per il simbolo, quindi niente da riempire e niente
        // da far scadere. Sono più di due giorni di orologio.
        for (var t = ProiezioneNelVuoto; t < PrimaDellaDomenica; t = t.AddHours(1))
        {
            service.UpdateMarketPrices(Prices(15_900m), SenzaBarre(), t);
            Assert.Equal(1, service.PendingOrdersCount);
        }

        // Riapertura: il livello viene superato e l'ordine è ancora vivo.
        service.UpdateMarketPrices(
            Prices(16_060m),
            Barre(PrimaDellaDomenica, open: 16_000m, high: 16_070m, low: 15_990m, close: 16_060m),
            PrimaDellaDomenica);

        var position = service.GetExecutionSnapshot(Code, "NQ", PrimaDellaDomenica).Position;
        Assert.NotNull(position);
        Assert.Equal(16_050m, position!.EntryPrice);
    }

    /// <summary>
    /// E finita <b>quella</b> barra l'ordine è morto lo stesso: il buco sposta l'inizio del
    /// conteggio, non lo toglie.
    /// </summary>
    [Fact]
    public void DopoLaPrimaBarraDelDopoBuco_LOrdineNonEsistePiu()
    {
        var service = Armato();

        service.UpdateMarketPrices(Prices(15_900m), SenzaBarre(), ProiezioneNelVuoto);

        // Prima barra dopo il buco: il livello non viene raggiunto, l'ordine sopravvive.
        service.UpdateMarketPrices(
            Prices(15_950m),
            Barre(PrimaDellaDomenica, open: 15_900m, high: 15_960m, low: 15_890m, close: 15_950m),
            PrimaDellaDomenica);
        Assert.Equal(1, service.PendingOrdersCount);

        // Barra seguente: la propria ora è finita, il livello viene ampiamente superato e non
        // deve succedere niente.
        var seguente = PrimaDellaDomenica.AddHours(1);
        service.UpdateMarketPrices(
            Prices(16_200m),
            Barre(seguente, open: 15_955m, high: 16_210m, low: 15_950m, close: 16_200m),
            seguente);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", seguente).Position);
        Assert.Equal(0, service.PendingOrdersCount);
    }

    /// <summary>
    /// <c>MaxBarsInPosition</c> conta BARRE, non tick dell'orologio: i tick del buco non consumano
    /// la vita della posizione. Contarli faceva chiudere per <c>MaxBars</c> molto prima delle N
    /// barre dichiarate, e la distorsione cresceva con la lunghezza del buco — che dopo la
    /// rimozione del salto del fine settimana è di due giorni.
    /// </summary>
    [Fact]
    public void MaxBars_ContaLeBarreNonITickDellOrologio()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var ingresso = new TradeSignal
        {
            Date = UltimaDelVenerdi,
            Type = SignalType.Buy,
            Price = 16_000m,
            Symbol = "NQ",
            StrategyName = Code,
            StrategyCode = Code,
            Quantity = 1,
            OrderType = TradeOrderType.Market,
            TimeframeMinutes = 60,
            MaxBarsInPosition = 2
        };
        service.ProcessSignals(
            [ingresso],
            Prices(16_000m),
            Barre(UltimaDelVenerdi, 16_000m, 16_010m, 15_990m, 16_000m),
            UltimaDelVenerdi);
        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", UltimaDelVenerdi).Position);

        // Cinquanta tick senza barre: la posizione non deve consumare nemmeno una barra.
        var t = UltimaDelVenerdi;
        for (var i = 0; i < 50; i++)
        {
            t = t.AddHours(1);
            service.UpdateMarketPrices(Prices(16_000m), SenzaBarre(), t);
        }
        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", t).Position);

        // Prima barra vera dopo il buco: una barra consumata su due.
        t = t.AddHours(1);
        service.UpdateMarketPrices(Prices(16_005m), Barre(t, 16_000m, 16_010m, 15_995m, 16_005m), t);
        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", t).Position);

        // Seconda barra vera: MaxBars raggiunto.
        t = t.AddHours(1);
        service.UpdateMarketPrices(Prices(16_007m), Barre(t, 16_005m, 16_012m, 16_000m, 16_007m), t);
        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", t).Position);
    }

    private static PiootooTradingService Armato()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        service.ProcessSignals(
            [StopEntry()],
            Prices(15_900m),
            Barre(UltimaDelVenerdi, open: 15_900m, high: 15_910m, low: 15_890m, close: 15_900m),
            UltimaDelVenerdi);
        Assert.Equal(1, service.PendingOrdersCount);
        return service;
    }

    private static TradeSignal StopEntry() => new()
    {
        Date = UltimaDelVenerdi,
        Type = SignalType.Buy,
        Price = 16_050m,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1,
        OrderType = TradeOrderType.Stop,
        // "Next bar" di una strategia a 60 minuti: la proiezione cade alle 21:00 del venerdì,
        // dove il feed non ha nessuna barra.
        ValidFromUtc = ProiezioneNelVuoto,
        ExpiresAtUtc = ProiezioneNelVuoto,
        TimeframeMinutes = 60
    };

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> SenzaBarre() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, OhlcvData> Barre(
        DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData
            {
                DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1
            }
        };
}
