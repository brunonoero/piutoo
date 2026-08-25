using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_TFU_008_240 - TF_U su NQ a 4 ore, <b>S47</b> del dossier
/// <c>run-engine/run-07-agosto/DOSSIER_PANIERE.md</c>.
///
/// <para><b>Codice sorgente: S47.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260824_1642</c>, famiglia
/// <c>fam04</c>, motore <c>TF_U</c>.</para>
///
/// <para><b>Che cosa fa.</b> Trend following asimmetrico: il filtro del long e quello dello short sono indipendenti.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non e' la sessione del broker: le due coincidono quasi sempre, ma non nelle settimane in cui
/// l'ora legale americana ed europea non sono allineate. Gli orari della finestra operativa sono
/// riportati <b>verbatim</b> dalla ricerca, mai convertiti nell'ora di borsa del simbolo.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo della sessione precedente</b> (H_d1)</description></item>
/// <item><description>SHORT: stop sell sul <b>minimo della sessione precedente</b> (L_d1)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier e il segno lo applica il motore
/// per verso: si dichiarano una volta sola. Le sentinelle disattivano il gate - neutrale 55/56,
/// direzionale 52/53, fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra
/// nulla</b>, non e' un filtro con soglia altissima.</para>
/// <list type="bullet">
/// <item><description>LONG deve essere VERO - fast <c>65</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>101</c></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>37</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>7</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>06:00 e 23:59</b>, ora della ricerca (CET)</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$500</b> per contratto = <b>25.00 pt</b></description></item>
/// <item><description>Take profit: <b>$4.500</b> = <b>225.00 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Uscita a tempo dopo <b>24 barre</b></description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$160</description></item>
/// <item><term>Fuori campione</term><description>$144.102 su 222 trade</description></item>
/// <item><term>Drawdown</term><description>$11.156</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260824_1642/consegna/trades/fam04_TF_U.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>4h fam04-2</c> del
/// dossier: emettono gli stessi ordini di entrata, e due sistemi che mandano gli stessi
/// ordini sono copy trading.</para>
/// </summary>
public sealed class PTS_NQ_TFU_008_240 : TfUnmirroredEngine
{
    public override string Name => "PTS_NQ_TFU_008_240";
    public override string Description =>
        "TF_U NQ 4 ore: S47 del dossier, run run_20260824_1642, finestra 06:00-23:59 CET, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 240;

    public PTS_NQ_TFU_008_240()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.Research(600, 2359);   // start_hour 6, fine a 23:59

        FastYesLong = 65;     // ptn_fast_yes_long
        FastNoLong = 101;      // ptn_fast_no_long
        FastYesShort = 37;    // ptn_fast_yes_short
        FastNoShort = 7;     // ptn_fast_no_short
        SkipDay = -1;         // skip_day (convenzione pandas, 0 = lunedi')

        IntradayOnly = false;    // intraday_only = 0

        StopMoney = 500;        // stop_loss, $ per contratto = 25.00 pt
        ProfitMoney = 4500;      // take_profit, $ per contratto = 225.00 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 24;           // max_bars
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
        if (parameters.TryGetValue("FastYesLong", out var fastYesLong))
            FastYesLong = Convert.ToInt32(fastYesLong);
        if (parameters.TryGetValue("FastNoLong", out var fastNoLong))
            FastNoLong = Convert.ToInt32(fastNoLong);
        if (parameters.TryGetValue("FastYesShort", out var fastYesShort))
            FastYesShort = Convert.ToInt32(fastYesShort);
        if (parameters.TryGetValue("FastNoShort", out var fastNoShort))
            FastNoShort = Convert.ToInt32(fastNoShort);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
