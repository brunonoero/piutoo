using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HK_BIA_001_15 - BIAS su HK a 15 minuti, <b>S108</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S108.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>run_20260831_0158</c>, famiglia
/// <c>fam01</c>, motore <c>BIAS</c>.</para>
///
/// <para><b>Che cosa fa.</b> Bias intraday con ingresso a breakout: dentro una finestra di barre
/// della sessione, uno stop sugli estremi rolling delle ultime N barre chiuse.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern vanno da
/// <b>01:00 a 01:00</b> nell'orologio della ricerca (<c>session_start_hour = 1</c>, tabella §2.1
/// del dossier), come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c>. Gli indici di barra si contano
/// da quel confine, non dall'ora di Hong Kong.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle 2 barre precedenti</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo delle 5 barre precedenti</b></description></item>
/// <item><description>La finestra LONG e' <b>[5, 46)</b> barre di sessione, la SHORT <b>[5, 46)</b>: si arma alla barra di partenza e solo se i pattern sono veri in quel momento, poi resta attiva fino alla fine della finestra</description></item>
/// <item><description>Le barre della sessione si contano da <b>0</b>: la prima barra dopo l'inizio sessione e' la numero 0 (<c>BarCountStartsAt = 0</c>)</description></item>
/// <item><description>Gli estremi rolling si leggono su barre <b>gia' chiuse</b>: l'ordine emesso alla barra i vive solo alla barra i+1</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier. Le sentinelle disattivano il
/// gate - fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra nulla</b>.</para>
/// <list type="bullet">
/// <item><description>LONG deve essere VERO - fast <c>147</c>: <c>close &lt; O_d0 * 1.01</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>117</c>: <c>O_d0 &lt; L_d1</c></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>28</c>: <c>|O_d5-C_d1| &gt; 0.25 * (HH5-LL5)</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>49</c>: <c>(C_d1 &gt; C_d2) E (C_d2 &gt; C_d3) E (C_d3 &gt; C_d4) E (C_d4 &gt; C_d5)</c></description></item>
/// </list>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario: opera su tutte le 24 ore</description></item>
/// <item><description><b>Non apre</b> posizioni LONG di giovedi' (<c>not_entry_day_long = 3</c>, convenzione pandas 0 = lunedi')</description></item>
/// <item><description><b>Non apre</b> posizioni SHORT di giovedi' (<c>not_entry_day_short = 3</c>)</description></item>
/// <item><description>Chiude tutto a <b>fine sessione</b>: l'uscita e' all'indice di barra dichiarato</description></item>
/// <item><description>Al massimo <b>una entrata per sessione e per direzione</b>: la direzione si disarma appena entra in posizione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> L'uscita principale e' <b>obbligatoria alla barra 69</b> della sessione
/// per il LONG e alla barra <b>91</b> per lo SHORT, market all'apertura di quella barra; stop e
/// target agiscono solo se scattano prima.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$5.000</b> per contratto = <b>780,03 pt</b></description></item>
/// <item><description>Take profit: <b>$4.500</b> = <b>702,03 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> HK, $6,41 per punto, tick 1. Il valore per punto e' il
/// solo del registro che dipende da un cambio (HKD 50 per punto a 7,8 HKD/USD): se l'HKD uscisse
/// dalla banda, stop e target andrebbero rimisurati. E' con questo cambio che tornano le
/// conversioni del dossier: $5.000 = 780,03 pt e $4.500 = 702,03 pt.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$74</description></item>
/// <item><term>Fuori campione</term><description>$69.026 su 620 trade</description></item>
/// <item><term>Drawdown</term><description>$17.700</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>run_20260831_0158/consegna/trades/fam01_BIAS.csv</c>. Contano le <b>entrate</b>: timestamp
/// e prezzo. Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>,
/// che l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>15m fam01-2</c> del
/// dossier: emettono gli stessi ordini di entrata, e due sistemi che mandano gli stessi
/// ordini sono copy trading.</para>
/// </summary>
public sealed class PTS_HK_BIA_001_15 : BiasBarCountEngine
{
    public override string Name => "PTS_HK_BIA_001_15";
    public override string Description =>
        "BIAS HK 15m: S108 del dossier, run run_20260831_0158, finestra 24h, uscita a indice di barra";
    public override string Symbol => "@HK";
    public override int TimeframeMinutes => 15;

    public PTS_HK_BIA_001_15()
    {
        // session_start_hour = 1 (tabella §2.1 del dossier): la sessione va da 01:00 a 01:00
        // nell'orologio della ricerca. Su HK la fascia 00:00-01:00 ha barre, e il taglio stretto
        // t > 0100 di OHLCMulti5 le assegna alla sessione precedente - esattamente come il
        // (timestamp - 1 min - 1h).normalize() del motore Python.
        Session = ZonedWindow.ResearchSession(1);
        Contracts = 1;

        // Il BIAS non ha finestra operativa: gli indici di barra sono la regola di entrata. La
        // finestra e' dichiarata piena, nell'orologio della ricerca, perche' ogni PTS deve
        // dichiarare l'orologio in cui legge gli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);   // nessun filtro orario, finestra piena

        EntryType = BiasEntryType.BreakoutStop;   // entrytype = 2
        PatternLibrary = EasyPatternLibrary.Fast;
        BarCountStartsAt = 0;          // le barre di sessione si contano da 0

        ArmBarLong = 5;                // inizio finestra LONG (inclusa)
        EndLong = 46;                  // fine finestra LONG (esclusa)
        ArmBarShort = 5;               // inizio finestra SHORT (inclusa)
        EndShort = 46;                 // fine finestra SHORT (esclusa)
        BreakoutBarsHigh = 2;          // massimo delle 2 barre precedenti
        BreakoutBarsLow = 5;           // minimo delle 5 barre precedenti
        ExitBarLong = 69;              // barra di uscita LONG
        ExitBarShort = 91;             // barra di uscita SHORT

        PatternLongYes = 147;          // gate fast del long
        PatternLongNo = 117;           // gate fast che impedisce il long
        PatternShortYes = 28;          // gate fast dello short
        PatternShortNo = 49;           // gate fast che impedisce lo short
        NotEntryDayLong = 3;           // niente LONG di giovedi' (0 = lunedi')
        NotEntryDayShort = 3;          // niente SHORT di giovedi'

        StopMoney = 5000;        // stop_loss, $ per contratto = 780,03 pt
        ProfitMoney = 4500;      // take_profit, $ per contratto = 702,03 pt
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
        if (parameters.TryGetValue("EndLong", out var endLong))
            EndLong = Convert.ToInt32(endLong);
        if (parameters.TryGetValue("EndShort", out var endShort))
            EndShort = Convert.ToInt32(endShort);
    }
}
