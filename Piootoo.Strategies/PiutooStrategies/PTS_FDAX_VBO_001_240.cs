using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_FDAX_VBO_001_240 - VBO su FDAX a 4 ore, <b>S10</b> del dossier
/// <c>run-engine/run-07-agosto/DOSSIER_PANIERE.md</c>.
///
/// <para><b>Codice sorgente: S10.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260824_1500</c>, famiglia
/// <c>fam02</c>, motore <c>VBO</c>.</para>
///
/// <para><b>Che cosa fa.</b> Breakout di un livello costruito sull'apertura di sessione piu' un multiplo di volatilita'.</para>
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
/// <item><description><c>VOL</c> = l'<b>ATR a 500 barre del timeframe</b>, misurato fino alla barra precedente compresa (<c>volatility_source = 3</c>)</description></item>
/// <item><description>LONG: stop buy a <b>O_d0 + 0.5 x VOL</b></description></item>
/// <item><description>SHORT: stop sell a <b>O_d0 - 0.3 x VOL</b></description></item>
/// <item><description><c>O_d0</c> e' l'apertura della sessione corrente: nota dalla prima barra, quindi il livello resta fisso per tutta la sessione</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier e il segno lo applica il motore
/// per verso: si dichiarano una volta sola. Le sentinelle disattivano il gate - neutrale 55/56,
/// direzionale 52/53, fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra
/// nulla</b>, non e' un filtro con soglia altissima.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>14</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>52</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>-9</c></description></item>
/// <item><description>deve essere FALSO - direzionale <c>53</c> <i>(sentinella: nessun filtro)</i></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra <b>07:00 e 14:00</b>, ora della ricerca (CET)</description></item>
/// <item><description><b>Non apre</b> posizioni di venerdi' (<c>skip_day = 4</c>, convenzione pandas 0 = lunedi')</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b> (nessun overnight): <c>intraday_only = 1</c></description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>10.00 pt</b></description></item>
/// <item><description>Take profit: <b>$7.500</b> = <b>300.00 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Breakeven a <b>$500</b> di utile</description></item>
/// <item><description>Uscita a tempo dopo <b>24 barre</b></description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> FDAX, 25 EUR per punto, tick 1.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$840</description></item>
/// <item><term>Fuori campione</term><description>$337.481 su 402 trade</description></item>
/// <item><term>Drawdown</term><description>$11.654</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260824_1500/consegna/trades/fam02_VBO.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_FDAX_VBO_001_240 : VolatilityBreakoutEngine
{
    public override string Name => "PTS_FDAX_VBO_001_240";
    public override string Description =>
        "VBO FDAX 4 ore: S10 del dossier, run run_20260824_1500, finestra 07:00-14:00 CET, intraday";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 240;

    /// <summary>Il motore VBO espone la valutazione comune: la sottoclasse la richiama.</summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public PTS_FDAX_VBO_001_240()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(7, 14);   // start_hour 7, end_hour 14

        EntryOrderType = TradeOrderType.Stop;
        EntryLevel = VolatilityBreakoutLevel.SessionOpenAtrBand;
        VolatilitySource = 3;          // volatility_source (1 range d1, 2 ATR sessioni, 3 ATR barre)
        AtrLength = 500;               // atr_length; 0 quando la volatilita' e' il range d1
        AtrMultiplierLong = 0.5m;      // atr_mult_long
        AtrMultiplierShort = 0.3m;     // atr_mult_short
        Momentum = 0;                  // momentum (0 spento, 1 C_d1/C_d2, 2 O_d0/C_d1)
        Direction = 0;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        SkipDay = 4;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 14;      // ptn_neut_yes
        NeutralNo = 52;       // ptn_neut_no
        DirectionalYes = -9;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = 53;   // ptn_dir_no

        IntradayOnly = true;    // intraday_only = 1
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 250;        // stop_loss, $ per contratto = 10.00 pt
        ProfitMoney = 7500;      // take_profit, $ per contratto = 300.00 pt
        TrailingStopMoney = 0;     // nessun trailing
        BreakEvenMoney = 500;     // breakeven, $ di utile
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
        if (parameters.TryGetValue("PtnNeutYes", out var neutYes))
            NeutralYes = Convert.ToInt32(neutYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutNo))
            NeutralNo = Convert.ToInt32(neutNo);
        if (parameters.TryGetValue("PtnDirYes", out var dirYes))
            DirectionalYes = Convert.ToInt32(dirYes);
        if (parameters.TryGetValue("PtnDirNo", out var dirNo))
            DirectionalNo = Convert.ToInt32(dirNo);
        if (parameters.TryGetValue("AtrLength", out var atrLength))
            AtrLength = Convert.ToInt32(atrLength);
        if (parameters.TryGetValue("AtrMultiplierLong", out var multLong))
            AtrMultiplierLong = Convert.ToDecimal(multLong);
        if (parameters.TryGetValue("AtrMultiplierShort", out var multShort))
            AtrMultiplierShort = Convert.ToDecimal(multShort);
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
