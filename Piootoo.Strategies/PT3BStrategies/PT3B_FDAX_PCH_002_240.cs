using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT3BStrategies;

/// <summary>
/// <b><see cref="PT3B_FDAX_PCH_001_240"/> con l'uscita prima del rollover.</b> Identica in tutto —
/// canale, pattern, finestra, stop, target, limite di barre — tranne un campo: chiude alle
/// <b>21:00</b> dell'orologio della ricerca invece che a fine sessione.
///
/// <para><b>Perche' esiste come classe separata.</b> Isola una variabile sola. La 001 resta
/// confrontabile con i run gia' archiviati, e la differenza fra le due misura l'effetto
/// dell'uscita e nient'altro. La finalista della ricerca completa — dove anche canale, pattern e
/// stop vengono riscelti addosso alla durata nuova — sara' una terza classe, non questa.</para>
///
/// <para><b>Il costo che toglie.</b> La sessione della ricerca e' il giorno di calendario europeo:
/// per FDAX finisce alle 00:59, cioe' <i>dopo</i> il rollover del broker (21:00 UTC su ICS, 20:59
/// su FTMO). La 001 e' dichiarata intraday ma lo attraversa ogni giorno e paga il finanziamento
/// come una multiday. Misurato sulla validazione 2025-01 → 2026-09, feed ICS, spread e swap ICS:</para>
///
/// <list type="table">
///   <listheader><term></term><description>001 (fine sessione) → 002 (21:00)</description></listheader>
///   <item><term>trade</term><description>501 → 475</description></item>
///   <item><term>lordo</term><description>$50.680 → <b>$75.050</b></description></item>
///   <item><term>swap</term><description><b>$16.759 → $0</b> (201 trade su 501 lo pagavano)</description></item>
///   <item><term>netto</term><description>$14.653 → <b>$56.782</b></description></item>
/// </list>
///
/// <para>Solo 17k dei 42k di miglioramento sono swap risparmiato: il <b>lordo sale di 24k</b>,
/// perche' le ore fra le 21 e le 00:59 non erano soltanto care — erano in perdita. Sono le ore in
/// cui il future e' chiuso e quota il solo CFD. Con l'uscita a fine sessione la configurazione
/// veniva scartata dalla validazione (1 finestra su 4 in utile); con questa passa, quattro finestre
/// su quattro e tenuta del 190%.</para>
///
/// <para><b>Perche' le 21:00 e non un orario con un margine esplicito.</b> L'ora e' scritta
/// nell'orologio della ricerca, che per FDAX e' <c>Europe/Rome</c>, mentre il rollover e' dichiarato
/// in UTC: le 21:00 locali sono le 19:00 UTC d'estate e le 20:00 d'inverno, quindi il margine sul
/// rollover va da <b>due ore a 59 minuti</b> e non serve toglierne altro. La conferma e' nella
/// misura stessa: zero swap su 475 trade, su un periodo che attraversa entrambe le stagioni. La
/// chiusura non aspetta la barra — il cBot valuta <c>CloseAtUtc</c> a ogni giro di
/// <c>OnTimer</c>, non solo su <c>OnBar</c> — quindi il ritardo e' di secondi, non di ore.</para>
///
/// <para><b>Etichetta della barra.</b> Come la 001: <c>ResearchLabelsBarsOnOpen = true</c>, perche'
/// la ricerca ha girato istanziando <c>PT2_FDAX_PCH_001_240</c>. Cambiarla sposta la finestra di
/// quattro ore e ribalta il segno del risultato.</para>
///
/// <para><b>Cosa resta da fare.</b> Il backtest a tick in cTrader e una sessione
/// <c>ExternalBroker</c>, che e' la stessa verifica che manca alla 001. Il confronto interessante e'
/// fra le due, sullo stesso periodo e con la stessa modalita' dati — vedi
/// <c>compare/compare-0048/esito.md</c> su cosa succede quando le modalita' non coincidono.</para>
/// </summary>
public sealed class PT3B_FDAX_PCH_002_240 : PriceChannelEngine
{
    public override string Name => "PT3B_FDAX_PCH_002_240";

    public override string Description =>
        "PC FDAX 4 ore come PT3B_FDAX_PCH_001_240, ma chiude alle 21:00 invece che a fine sessione: " +
        "niente rollover, niente swap, e le ore notturne fuori dal trade";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 240;

    public PT3B_FDAX_PCH_002_240()
    {
        Contracts = 1;

        // Ereditata dalla classe con cui la ricerca ha girato: gli orari sotto si confrontano con
        // l'APERTURA della barra. Vedi la nota nel commento della classe.
        ResearchLabelsBarsOnOpen = true;

        // start_hour 3, end_hour 18, verbatim dall'ottimizzatore e nell'orologio della ricerca.
        TradingWindow = ZonedWindow.ResearchHours(3, 18);

        ChannelBars = 1;               // canale sulla sola barra di segnale, inclusa
        OffsetTicks = 0;               // nessun buffer oltre il livello
        TickSize = 1m;                 // tick FDAX: 1 punto
        Direction = 0;                 // entrambe le direzioni
        DvolMin = 0m;                  // nessun filtro di volatilita' daily
        SkipDay = -1;                  // nessun giorno escluso

        NeutralYes = 44;               // richiesto: highD0 < lowD0 * 1,03
        NeutralNo = 52;                // vietato:   highD0 > highD1 && lowD0 < lowD1
        DirectionalYes = 52;           // sentinella sempre vera
        DirectionalNo = -18;           // vietato: sessione precedente mossa del 2% contro il breakout

        IntradayOnly = true;           // chiude dentro la sessione

        // L'UNICA differenza dalla 001. 21:00 nell'orologio della ricerca (Europe/Rome per FDAX):
        // 19:00 UTC d'estate, 20:00 d'inverno, contro un rollover alle 20:59/21:00 UTC.
        // Non e' cablata nel motore e non e' una costante del simbolo: e' un parametro di QUESTA
        // strategia, che la ricerca sceglie con ExitHour. Vedi EasyEngineBase.SessionExitTime.
        SessionExitTime = new TimeOnly(21, 0);

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
        if (parameters.TryGetValue("ExitHour", out var exitHour))
            SessionExitTime = Convert.ToInt32(exitHour) < 0
                ? null
                : new TimeOnly(Convert.ToInt32(exitHour), 0);
        if (parameters.TryGetValue("SkipDay", out var skipDay))
            SkipDay = Convert.ToInt32(skipDay);
        if (parameters.TryGetValue("DvolMin", out var dvolMin))
            DvolMin = Convert.ToDecimal(dvolMin);
    }
}
