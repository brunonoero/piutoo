using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_FDAX_MAC_001_240 - MAC su FDAX a 4 ore, <b>S04</b> del dossier
/// <c>run-engine/run-07-agosto/DOSSIER_PANIERE.md</c>.
///
/// <para><b>Codice sorgente: S04.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260824_1500</c>, famiglia
/// <c>fam01</c>, motore <c>MAC</c>.</para>
///
/// <para><b>Che cosa fa.</b> Incrocio di due medie mobili, senza filtri pattern.</para>
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
/// <item><description>Due medie mobili <b>semplici</b> sulla close: veloce a <b>5 barre</b>, lenta a <b>24 barre</b></description></item>
/// <item><description>Segnale LONG: la veloce incrocia <b>sopra</b> la lenta; SHORT: incrocia <b>sotto</b></description></item>
/// <item><description>Filtro gradiente: su 2 barre la veloce deve essersi mossa, in valore assoluto, almeno <b>2 volte</b> quanto la lenta nello stesso tratto</description></item>
/// <item><description>Filtro sulla sessione precedente: corpo/range &lt;= 0,5 e sessione <b>verde</b> perche' il long operi, <b>rossa</b> perche' operi lo short</description></item>
/// <item><description>Entrata <b>MARKET all'apertura della barra successiva</b> al segnale</description></item>
/// <item><description><b>Solo long</b>: il lato short non opera mai</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier e il segno lo applica il motore
/// per verso: si dichiarano una volta sola. Le sentinelle disattivano il gate - neutrale 55/56,
/// direzionale 52/53, fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra
/// nulla</b>, non e' un filtro con soglia altissima.</para>
/// <list type="bullet">
/// <item><description>Questo motore <b>non usa filtri pattern</b> (<c>UsePatternFilter = false</c>)</description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore</description></item>
/// <item><description>Nessun giorno escluso (<c>skip_day = -1</c>)</description></item>
/// <item><description>Tiene la posizione <b>oltre la fine della sessione</b>: questo motore non chiude mai per fine sessione, e non c'e' un parametro che lo cambi</description></item>
/// <item><description><b>Nessun limite</b> al numero di entrate per sessione; una sola posizione per volta</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Oltre a stop e trailing dichiarati sul segnale, il motore chiude sul
/// <b>crossover inverso</b> (barra successiva) e alla <b>chiusura della sessione di venerdi'</b>:
/// sono quelle le uscite principali, stop e target agiscono solo se scattano prima.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$3.000</b> per contratto = <b>120.00 pt</b></description></item>
/// <item><description>Take profit: <b>nessuno</b> (<c>take_profit = 0</c>)</description></item>
/// <item><description>Trailing stop: <b>$4.000</b></description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> FDAX, 25 EUR per punto, tick 1.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$1.543</description></item>
/// <item><term>Fuori campione</term><description>$99.091 su 46 trade</description></item>
/// <item><term>Drawdown</term><description>$17.444</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260824_1500/consegna/trades/fam01_MAC.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
/// </summary>
public sealed class PTS_FDAX_MAC_001_240 : MovingAverageCrossoverEngine
{
    public override string Name => "PTS_FDAX_MAC_001_240";
    public override string Description =>
        "MAC FDAX 4 ore: S04 del dossier, run run_20260824_1500, finestra 24h, multiday";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 240;

    public PTS_FDAX_MAC_001_240()
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

        AverageType = MovingAverageType.Simple;
        FastPeriod = 5;                // media veloce
        SlowPeriod = 24;               // media lenta
        Direction = MovingAverageCrossoverDirection.LongOnly;
        RequireFlatPosition = true;    // una sola posizione per volta
        MaxEntriesPerDay = 0;          // nessun limite di entrate per sessione

        GradientPeriod = 2;            // barre del filtro gradiente
        GradientFactor = 2m;           // rapporto minimo fra pendenza veloce e lenta
        UseDailyFilter = true;         // setup della sessione precedente
        DailyBodyFactor = 0.5m;        // |C_d1 - O_d1| <= 0,5 x (H_d1 - L_d1)

        UsePatternFilter = false;      // questo motore non usa filtri pattern

        StopMoney = 3000;        // stop_loss, $ per contratto = 120.00 pt
        ProfitMoney = 0;         // take_profit = 0: nessun target
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
        if (parameters.TryGetValue("FastPeriod", out var fastPeriod))
            FastPeriod = Convert.ToInt32(fastPeriod);
        if (parameters.TryGetValue("SlowPeriod", out var slowPeriod))
            SlowPeriod = Convert.ToInt32(slowPeriod);
    }
}
