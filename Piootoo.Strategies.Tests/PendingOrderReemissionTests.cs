using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La riemissione di un ordine non uccide l'ordine che sta ancora vivendo la propria barra.
///
/// <para><b>Perché questi test esistono.</b> Le strategie EasyLanguage riemettono il "next bar" a
/// ogni barra finché la condizione regge (<c>EasyEngineBase</c>: <c>ValidFromUtc = ExpiresAtUtc =
/// nextBar</c>). <c>EnqueuePendingOrder</c> indicizza su <c>positionKey|side|orderType</c>, quindi
/// il segnale nuovo — valido <i>dalla barra dopo</i> — prendeva il posto di quello valido
/// <i>adesso</i>. L'unica finestra che restava all'incumbent era la <c>TryFillPendingOrders</c> in
/// testa a <c>ProcessSignals</c>, larga quanto la barra dell'orologio.</para>
///
/// <para><b>Perché non si vedeva.</b> Finché l'orologio del portafoglio coincideva con il
/// timeframe della strategia quella finestra era la barra intera e il difetto non produceva
/// nessuna differenza. Con un orologio più fitto vale una frazione: a un minuto su una strategia a
/// 60, un sessantesimo. Misurato in <c>piootoo-repository/compare/compare-0022</c>:
/// <c>PTS_BTC_BIA_001_60</c> aveva 72 occasioni di riempimento sulle barre da 1 minuto
/// dell'archivio, il cBot ne ha prese 72 e il motore interno 9 — e 9 è esattamente il numero che
/// si ottiene contando solo i livelli toccati nel primo minuto della barra.</para>
///
/// <para>Le generazioni non si buttano: quella nuova aspetta il proprio turno e subentra quando
/// l'incumbent ha finito la propria barra, che è ciò che fa il broker (un ordine per lato,
/// cancellato e ripiazzato a ogni barra).</para>
/// </summary>
public class PendingOrderReemissionTests
{
    private const string Code = "PTS_TEST_60";

    // Strategia a 60 minuti, orologio del portafoglio a 1 minuto.
    private static readonly DateTime SignalBar = new(2024, 1, 3, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OraA = new(2024, 1, 3, 16, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OraB = new(2024, 1, 3, 17, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OraC = new(2024, 1, 3, 18, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Il caso di compare-0022: il livello viene toccato al minuto 30 della propria ora, dopo che
    /// la strategia ha già riemesso l'ordine per l'ora successiva.
    /// </summary>
    [Fact]
    public void LaRiemissione_NonToglieAllIncumbent_IlRestoDellaPropriaBarra()
    {
        var service = Armato();

        // 16:00 — primo minuto dell'ora dell'ordine A. Il prezzo non tocca il livello, e nello
        // stesso tick la strategia riemette per le 17:00.
        service.ProcessSignals(
            [StopEntry(livello: 16_050m, nextBar: OraB)],
            Prezzi(16_010m),
            Barre(OraA, open: 16_000m, high: 16_020m, low: 15_995m, close: 16_010m),
            OraA);

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", OraA).Position);
        Assert.Equal(1, service.PendingOrdersCount);

        // 16:30 — il livello viene superato. L'ordine A è ancora dentro la propria ora.
        service.UpdateMarketPrices(
            Prezzi(16_060m),
            Barre(Minuto(OraA, 30), open: 16_012m, high: 16_070m, low: 16_010m, close: 16_060m),
            Minuto(OraA, 30));

        var posizione = service.GetExecutionSnapshot(Code, "NQ", Minuto(OraA, 30)).Position;
        Assert.NotNull(posizione);
        Assert.Equal(16_050m, posizione!.EntryPrice);
    }

    /// <summary>
    /// La generazione parcheggiata non si perde: quando l'incumbent ha finito la propria ora
    /// subentra lei, con il <b>proprio</b> livello, e vive a sua volta un'ora intera.
    /// </summary>
    [Fact]
    public void LaGenerazioneSuccessiva_SubentraQuandoLIncumbentHaFinito()
    {
        var service = Armato();

        // 16:00 — nessun tocco, e la strategia riemette con un livello DIVERSO per le 17:00.
        service.ProcessSignals(
            [StopEntry(livello: 16_100m, nextBar: OraB)],
            Prezzi(16_010m),
            Barre(OraA, open: 16_000m, high: 16_020m, low: 15_995m, close: 16_010m),
            OraA);

        // 16:30 — sempre nessun tocco: l'ordine A muore inutilizzato alle 17:00.
        service.UpdateMarketPrices(
            Prezzi(16_015m),
            Barre(Minuto(OraA, 30), open: 16_012m, high: 16_030m, low: 16_005m, close: 16_015m),
            Minuto(OraA, 30));
        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", Minuto(OraA, 30)).Position);

        // 17:00 — A è scaduto, subentra B. La strategia riemette ancora, per le 18:00.
        service.ProcessSignals(
            [StopEntry(livello: 16_100m, nextBar: OraC)],
            Prezzi(16_020m),
            Barre(OraB, open: 16_016m, high: 16_040m, low: 16_014m, close: 16_020m),
            OraB);
        Assert.Equal(1, service.PendingOrdersCount);

        // 17:45 — il livello di B viene superato: è B a riempirsi, al proprio livello.
        service.UpdateMarketPrices(
            Prezzi(16_120m),
            Barre(Minuto(OraB, 45), open: 16_042m, high: 16_130m, low: 16_040m, close: 16_120m),
            Minuto(OraB, 45));

        var posizione = service.GetExecutionSnapshot(Code, "NQ", Minuto(OraB, 45)).Position;
        Assert.NotNull(posizione);
        Assert.Equal(16_100m, posizione!.EntryPrice);
    }

    /// <summary>
    /// Il livello vecchio non sopravvive alla propria ora: finita quella, un tocco che riguarda
    /// solo lui non apre niente. È il fill fantasma di <c>orologio-barre-e-fill.md</c>, e la
    /// riemissione parcheggiata non deve reintrodurlo.
    /// </summary>
    [Fact]
    public void IlLivelloVecchio_NonSiRiempieNellOraDellaGenerazioneSuccessiva()
    {
        var service = Armato();

        service.ProcessSignals(
            [StopEntry(livello: 16_100m, nextBar: OraB)],
            Prezzi(16_010m),
            Barre(OraA, open: 16_000m, high: 16_020m, low: 15_995m, close: 16_010m),
            OraA);

        // 17:15 — il prezzo supera il livello VECCHIO (16.050) ma non quello nuovo (16.100).
        service.UpdateMarketPrices(
            Prezzi(16_060m),
            Barre(Minuto(OraB, 15), open: 16_016m, high: 16_070m, low: 16_014m, close: 16_060m),
            Minuto(OraB, 15));

        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", Minuto(OraB, 15)).Position);
    }

    /// <summary>
    /// Regressione: quando l'orologio coincide con il timeframe della strategia il comportamento
    /// non cambia di una virgola — ogni generazione vede la propria barra e una sola.
    /// </summary>
    [Fact]
    public void ConOrologioUgualeAlTimeframe_IlComportamentoNonCambia()
    {
        var service = Armato();

        // Un tick solo per ora, e la barra è quella da 60 minuti: A vede tutta la propria ora.
        service.ProcessSignals(
            [StopEntry(livello: 16_100m, nextBar: OraB)],
            Prezzi(16_060m),
            Barre(OraA, open: 16_000m, high: 16_070m, low: 15_995m, close: 16_060m),
            OraA);

        var posizione = service.GetExecutionSnapshot(Code, "NQ", OraA).Position;
        Assert.NotNull(posizione);
        Assert.Equal(16_050m, posizione!.EntryPrice);
    }

    /// <summary>Il servizio con l'ordine A (livello 16.050) già accodato per le 16:00.</summary>
    private static PiootooTradingService Armato()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        service.ProcessSignals(
            [StopEntry(livello: 16_050m, nextBar: OraA)],
            Prezzi(15_900m),
            Barre(SignalBar, open: 15_900m, high: 15_910m, low: 15_890m, close: 15_900m),
            SignalBar);
        Assert.Equal(1, service.PendingOrdersCount);
        return service;
    }

    private static DateTime Minuto(DateTime ora, int minuti) => ora.AddMinutes(minuti);

    private static TradeSignal StopEntry(decimal livello, DateTime nextBar) => new()
    {
        Date = nextBar.AddMinutes(-60),
        Type = SignalType.Buy,
        Price = livello,
        Symbol = "NQ",
        StrategyName = Code,
        StrategyCode = Code,
        Quantity = 1,
        OrderType = TradeOrderType.Stop,
        ValidFromUtc = nextBar,
        ExpiresAtUtc = nextBar,
        TimeframeMinutes = 60
    };

    private static Dictionary<string, decimal> Prezzi(decimal prezzo) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = prezzo };

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
