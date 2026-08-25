using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_YM_BIA_001_240 - BIAS su YM a 4 ore, <b>S58</b> del dossier
/// <c>run-engine/run-07-agosto/DOSSIER_PANIERE.md</c>.
///
/// <para><b>Codice sorgente: S58.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260824_1550</c>, famiglia
/// <c>fam03</c>, motore <c>BIAS</c>.</para>
///
/// <para><b>Che cosa fa.</b> Bias intraday: entra ed esce a indici di barra fissi della sessione.</para>
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
/// <item><description>LONG: <b>MARKET all'apertura della barra 1</b> della sessione</description></item>
/// <item><description>SHORT: <b>MARKET all'apertura della barra 7</b> della sessione</description></item>
/// <item><description>Le barre della sessione si contano da <b>0</b>: la prima barra dopo l'inizio sessione e' la numero 0 (<c>BarCountStartsAt = 0</c>)</description></item>
/// <item><description>I filtri pattern si valutano alla <b>chiusura della barra precedente</b> a quella di entrata</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier e il segno lo applica il motore
/// per verso: si dichiarano una volta sola. Le sentinelle disattivano il gate - neutrale 55/56,
/// direzionale 52/53, fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra
/// nulla</b>, non e' un filtro con soglia altissima.</para>
/// <list type="bullet">
/// <item><description>LONG deve essere VERO - fast <c>93</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>6</c></description></item>
/// <item><description>SHORT: nessun gate dichiarato dal dossier, sentinelle fast 152/153</description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b>: l'uscita e' all'indice di barra dichiarato</description></item>
/// <item><description><b>Non apre</b> posizioni LONG di lunedi' (<c>not_entry_day_long = 0</c>)</description></item>
/// </list>
///
/// <para><b>Uscite.</b> L'uscita principale e' <b>obbligatoria alla barra 6</b> della sessione
/// per il LONG e alla barra <b>2</b> per lo SHORT, market all'apertura di quella barra;
/// stop e target agiscono solo se scattano prima.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$2.250</b> per contratto = <b>450.00 pt</b></description></item>
/// <item><description>Take profit: <b>$5.000</b> = <b>1.000,00 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> YM, $5 per punto, tick 1.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$115</description></item>
/// <item><term>Fuori campione</term><description>$32.798 su 238 trade</description></item>
/// <item><term>Drawdown</term><description>$11.355</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260824_1550/consegna/trades/fam03_BIAS.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_YM_BIA_001_240 : BiasBarCountEngine
{
    public override string Name => "PTS_YM_BIA_001_240";
    public override string Description =>
        "BIAS YM 4 ore: S58 del dossier, run run_20260824_1550, finestra 24h, intraday";
    public override string Symbol => "@YM";
    public override int TimeframeMinutes => 240;

    public PTS_YM_BIA_001_240()
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

        EntryType = BiasEntryType.MarketOnArmBar;
        PatternLibrary = EasyPatternLibrary.Fast;
        BarCountStartsAt = 0;          // le barre di sessione si contano da 0
        ArmBarLong = 1;                // barra di entrata LONG
        ArmBarShort = 7;               // barra di entrata SHORT
        ExitBarLong = 6;               // barra di uscita LONG
        ExitBarShort = 2;              // barra di uscita SHORT

        PatternLongYes = 93;           // gate fast del long
        PatternLongNo = 6;             // gate fast che impedisce il long
        PatternShortYes = 152;          // sentinella fast: nessun gate sullo short
        PatternShortNo = 153;           // sentinella fast: nessun gate sullo short
        NotEntryDayLong = 0;           // niente LONG di lunedi' (0 = lunedi')

        StopMoney = 2250;        // stop_loss, $ per contratto = 450.00 pt
        ProfitMoney = 5000;      // take_profit, $ per contratto = 1.000,00 pt
        TrailingStopMoney = 0;     // nessun trailing
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
        if (parameters.TryGetValue("ArmBarLong", out var armLong))
            ArmBarLong = Convert.ToInt32(armLong);
        if (parameters.TryGetValue("ArmBarShort", out var armShort))
            ArmBarShort = Convert.ToInt32(armShort);
    }
}
