using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_VBO_001_1440 - VBO su NQ a giornaliero, <b>S11</b> del dossier
/// <c>run-engine/run-07-agosto/DOSSIER_PANIERE.md</c>.
///
/// <para><b>Codice sorgente: S11.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260822_0736</c>, famiglia
/// <c>fam06</c>, motore <c>VBO</c>.</para>
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
/// <item><description><c>VOL</c> = il <b>range della sessione precedente</b>, <c>H_d1 - L_d1</c> (<c>volatility_source = 1</c>)</description></item>
/// <item><description>LONG: stop buy a <b>O_d0 + 1 x VOL</b></description></item>
/// <item><description>SHORT: stop sell a <b>O_d0 - 1 x VOL</b></description></item>
/// <item><description><c>O_d0</c> e' l'apertura della sessione corrente: nota dalla prima barra, quindi il livello resta fisso per tutta la sessione</description></item>
/// <item><description><b>Solo long</b>: il lato short non opera mai (<c>direction = 1</c>)</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier e il segno lo applica il motore
/// per verso: si dichiarano una volta sola. Le sentinelle disattivano il gate - neutrale 55/56,
/// direzionale 52/53, fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra
/// nulla</b>, non e' un filtro con soglia altissima.</para>
/// <list type="bullet">
/// <item><description>deve essere VERO - neutrale <c>47</c></description></item>
/// <item><description>deve essere FALSO - neutrale <c>19</c></description></item>
/// <item><description>deve essere VERO - direzionale <c>52</c> <i>(sentinella: nessun filtro)</i></description></item>
/// <item><description>deve essere FALSO - direzionale <c>13</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Puo' restare aperta <b>oltre la sessione</b> (multiday): <c>intraday_only = 0</c>, quindi <c>IntradayOnly = false</c> esplicito</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b> - limite sul fill, non sull'emissione dello stop</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$1.000</b> per contratto = <b>50.00 pt</b></description></item>
/// <item><description>Take profit: <b>$10.000</b> = <b>500.00 pt</b></description></item>
/// <item><description>Trailing stop: <b>$4.000</b></description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$833</description></item>
/// <item><term>Fuori campione</term><description>$86.962 su 52 trade</description></item>
/// <item><term>Drawdown</term><description>$23.211</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260822_0736/consegna/trades/fam06_VBO.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_NQ_VBO_001_1440 : VolatilityBreakoutEngine
{
    public override string Name => "PTS_NQ_VBO_001_1440";
    public override string Description =>
        "VBO NQ giornaliero: S11 del dossier, run run_20260822_0736, finestra 24h, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 1440;

    /// <summary>Il motore VBO espone la valutazione comune: la sottoclasse la richiama.</summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public PTS_NQ_VBO_001_1440()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.Research(0, 2359);   // nessun filtro orario, finestra piena

        EntryOrderType = TradeOrderType.Stop;
        EntryLevel = VolatilityBreakoutLevel.SessionOpenAtrBand;
        VolatilitySource = 1;          // volatility_source (1 range d1, 2 ATR sessioni, 3 ATR barre)
        AtrLength = 0;               // atr_length; 0 quando la volatilita' e' il range d1
        AtrMultiplierLong = 1m;      // atr_mult_long
        AtrMultiplierShort = 1m;     // atr_mult_short
        Momentum = 0;                  // momentum (0 spento, 1 C_d1/C_d2, 2 O_d0/C_d1)
        Direction = 1;                 // direction (0 entrambi, 1 solo long, 2 solo short)
        SkipDay = -1;                  // skip_day (convenzione pandas, 0 = lunedi')

        NeutralYes = 47;      // ptn_neut_yes
        NeutralNo = 19;       // ptn_neut_no
        DirectionalYes = 52;  // ptn_dir_yes (il segno lo applica il motore per verso)
        DirectionalNo = 13;   // ptn_dir_no

        IntradayOnly = false;    // intraday_only = 0
        MaxEntriesPerSession = 1;      // una entrata per sessione e per direzione

        StopMoney = 1000;        // stop_loss, $ per contratto = 50.00 pt
        ProfitMoney = 10000;      // take_profit, $ per contratto = 500.00 pt
        TrailingStopMoney = 4000;  // trailing_stop, $ per contratto
        BreakEvenMoney = 0;        // nessun breakeven
        MaxBars = 0;            // max_bars = 0: nessuna uscita a tempo
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
    }
}
