using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// Price Channel su FDAX a 4 ore. <b>Prima strategia della serie PT3B</b>, che non viene da un
/// dossier di ricerca ma dall'ottimizzatore interno (<c>piootoo-sweep</c>): cio' che nelle PTS e
/// PT2 arrivava gia' scelto, qui e' stato cercato in casa.
///
/// <para><b>Come e' stata trovata.</b> Ricerca sequenziale a fasi sul feed <b>ICS</b> (CFD), sette
/// fasi nell'ordine del metodo Unger, campione <b>2014-07-17 → 2022-01-01</b> e validazione
/// <b>2022-01-01 → 2026-09-17</b>. La validazione non ha visto un solo dato del campione. Resoconto
/// completo in <c>piootoo-repository/ricerca/fdax-4h-ics-ftmo-per-ora.md</c>.</para>
///
/// <para><b>I numeri, fuori campione</b> (spread FTMO per ora, mediana; commissione $4 per contratto
/// e per lato): <b>1.327 trade, $153.781</b>, punteggio netto/drawdown 2,49, tenuta del 52% rispetto
/// al campione e <b>quattro finestre su quattro in utile</b>. In campione: 1.913 trade e $216.830.</para>
///
/// <para><b>La prova di resistenza.</b> Rimisurata con lo spread <b>p90</b> — il costo che si paga
/// una volta su dieci — invece della mediana, il fuori campione passa da $153.781 a $150.571: il
/// <b>2%</b>, con le stesse quattro finestre in utile. Non vive dentro il costo di transazione, ed e'
/// il motivo per cui e' stata promossa a classe mentre le altre tre celle della stessa ricerca sono
/// state scartate.</para>
///
/// <para><b>Perche' opera di giorno.</b> Una prima ricerca sulla stessa cella, con lo spread
/// dichiarato come costante giornaliera (1,23 punti), aveva scelto la finestra <b>00:00-06:00</b> —
/// cioe' proprio le ore in cui lo spread FTMO del DAX vale 2,93-3,33 punti e non 1,13. La sweep
/// sceglie anche in base al costo, e con un costo medio le ore care sembrano economiche. Pagando il
/// costo vero ora per ora, la ricerca si e' spostata sulle ore diurne e il risultato fuori campione
/// e' salito del 70% ($90.296 → $153.781). Vedi <c>docs/domini/spread-e-costo-di-transazione.md</c>.</para>
///
/// <para><b>Etichetta della barra: la eredita dalla classe di partenza.</b> La ricerca ha girato
/// istanziando <c>PT2_FDAX_PCH_001_240</c> e cambiandone i parametri, e quella classe dichiara
/// <c>ResearchLabelsBarsOnOpen = true</c>. Gli orari trovati sono quindi confrontati con
/// l'<b>apertura</b> della barra: <c>start_hour = 3</c> vuol dire "la barra che apre alle 03:00".
/// Questa classe deve dichiarare lo stesso, e la prima stesura non lo faceva: eseguita con
/// l'etichetta di chiusura, la finestra si sposta di quattro ore e il campione passa da $216.830 a
/// <b>meno</b> $2.527. E' l'errore che la verifica «la classe riproduce i numeri della ricerca»
/// esiste per intercettare, ed e' il motivo per cui quella verifica va fatta prima del backtest e
/// non dopo.</para>
///
/// <para>Corollario da ricordare quando si promuovera' un'altra finalista: la classe di partenza
/// della sweep non e' solo un contenitore di parametri. Porta con se' l'etichetta della barra e
/// l'ancoraggio di sessione, che dei parametri non fanno parte e che nessun resoconto stampa.</para>
///
/// <para><b>Cosa resta da fare.</b> Un backtest su dati tick in cTrader e una sessione
/// <c>ExternalBroker</c>: il feed ICS e' a barre, e nessun backtest a barre sa cosa fa il broker
/// dentro la barra dell'ingresso.</para>
/// </summary>
public sealed class PT3B_FDAX_PCH_001_240 : PriceChannelEngine
{
    public override string Name => "PT3B_FDAX_PCH_001_240";

    public override string Description =>
        "PC FDAX 4 ore: ricerca interna su feed ICS 2014-2022, canale 1 barra, finestra 03:00-18:00 CET, " +
        "neutrale 44 richiesto e 52 vietato, direzionale -18 vietato, intraday, uscita a 12 barre";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 240;

    public PT3B_FDAX_PCH_001_240()
    {
        Contracts = 1;

        // Ereditata dalla classe con cui la ricerca ha girato: gli orari sotto si confrontano con
        // l'APERTURA della barra. Vedi la nota nel commento della classe.
        ResearchLabelsBarsOnOpen = true;

        // start_hour 3, end_hour 18, verbatim dall'ottimizzatore e nell'orologio della ricerca.
        // Sulla 4h ancorata all'01:00 di Roma sono le barre che APRONO alle 05:00, 09:00, 13:00 e
        // 17:00 locali: quattro bucket su sei, tutti dentro l'orario di borsa europeo.
        TradingWindow = ZonedWindow.ResearchHours(3, 18);

        ChannelBars = 1;               // canale sulla sola barra di segnale, inclusa
        OffsetTicks = 0;               // nessun buffer oltre il livello
        TickSize = 1m;                 // tick FDAX: 1 punto
        Direction = 0;                 // entrambe le direzioni
        DvolMin = 0m;                  // nessun filtro di volatilita' daily
        SkipDay = -1;                  // nessun giorno escluso

        // Pattern, con i numeri della libreria e la formula accanto: un id da solo non si verifica.
        NeutralYes = 44;               // richiesto: highD0 < lowD0 * 1,03 — sessione stretta, meno del 3% di escursione
        NeutralNo = 52;                // vietato:   highD0 > highD1 && lowD0 < lowD1 — outside bar sulla sessione precedente
        DirectionalYes = 52;           // sentinella sempre vera: nessun filtro direzionale richiesto
        DirectionalNo = -18;           // vietato: per il LONG closeD1 < closeD2 - 2%, per lo SHORT closeD1 > closeD2 + 2%
                                       // (il motore applica il segno al lato) — niente ingressi dopo una sessione
                                       // che si e' gia' mossa del 2% contro la direzione del breakout

        IntradayOnly = true;           // chiude a fine sessione
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 5000;              // $ per contratto = 200 punti FDAX
        ProfitMoney = 4500;            // $ per contratto = 180 punti
        TrailingStopMoney = 0;
        BreakEvenMoney = 0;
        MaxBars = 12;                  // uscita a tempo dopo 12 barre da 4 ore
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("StopLoss", out var stopLoss))
            StopMoney = Convert.ToInt32(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
            ProfitMoney = Convert.ToInt32(takeProfit);
        if (parameters.TryGetValue("TrailingStop", out var trailing))
            TrailingStopMoney = Convert.ToInt32(trailing);
        if (parameters.TryGetValue("BreakEven", out var breakEven))
            BreakEvenMoney = Convert.ToInt32(breakEven);
        if (parameters.TryGetValue("MaxBars", out var maxBars))
            MaxBars = Convert.ToInt32(maxBars);
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("ChannelBars", out var channelBars))
            ChannelBars = Convert.ToInt32(channelBars);
        if (parameters.TryGetValue("OffsetTicks", out var offsetTicks))
            OffsetTicks = Convert.ToInt32(offsetTicks);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { Start = ResearchHourOrOff(startHour, TimeOnly.MinValue) };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { End = ResearchHourOrOff(endHour, ZonedWindow.EndOfDay) };
        if (parameters.TryGetValue("Direction", out var direction))
            Direction = Convert.ToInt32(direction);
        if (parameters.TryGetValue("IntradayOnly", out var intradayOnly))
            IntradayOnly = Convert.ToInt32(intradayOnly) != 0;
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            SkipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("DvolMin", out var dvolMin))
            DvolMin = Convert.ToDecimal(dvolMin);
    }
}
