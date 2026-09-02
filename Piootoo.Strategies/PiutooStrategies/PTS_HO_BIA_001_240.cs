using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_HO_BIA_001_240 - BIAS su HO a 4 ore, <b>S34</b> del dossier
/// <c>run-engine/run-08-settembre/DOSSIER_PANIERE (1).md</c>.
///
/// <para><b>Codice sorgente: S34.</b> E' l'identificativo con cui questa strategia compare
/// nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un controllo,
/// senza riaprire i CSV a tentativi. La riga di ricerca e' <c>HO_4h</c>, famiglia
/// <c>fam01</c>, motore <c>BIAS</c>.</para>
///
/// <para><b>Che cosa fa.</b> Bias intraday con ingresso a breakout: dentro una finestra di barre
/// della sessione, uno stop sugli estremi rolling delle ultime N barre chiuse.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 -> 00:00, come il motore Python che taglia con
/// <c>(timestamp - 1 min - session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>
/// (tabella §2.1 del dossier: HO parte a 00:00 CET). Gli indici di barra si contano da quel
/// confine, non dall'ora NYMEX.</para>
///
/// <para><b>Livelli di ingresso.</b></para>
/// <list type="bullet">
/// <item><description>LONG: stop buy sul <b>massimo delle 2 barre precedenti</b></description></item>
/// <item><description>SHORT: stop sell sul <b>minimo della barra precedente</b></description></item>
/// <item><description>La finestra LONG e' <b>[1, 4)</b> barre di sessione, la SHORT <b>[1, 4)</b>: si arma alla barra di partenza e solo se i pattern sono veri in quel momento, poi resta attiva fino alla fine della finestra</description></item>
/// <item><description>Le barre della sessione si contano da <b>0</b>: la prima barra dopo l'inizio sessione e' la numero 0 (<c>BarCountStartsAt = 0</c>)</description></item>
/// <item><description>Gli estremi rolling si leggono su barre <b>gia' chiuse</b>: l'ordine emesso alla barra i vive solo alla barra i+1</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> I numeri sono quelli del dossier. Le sentinelle disattivano il
/// gate - fast 152/153 - quindi un gate lasciato alla sentinella <b>non filtra nulla</b>.</para>
/// <list type="bullet">
/// <item><description>LONG deve essere VERO - fast <c>103</c>: <c>L_d0 &gt; L_d1 * (1 + 0.015)</c></description></item>
/// <item><description>LONG deve essere FALSO - fast <c>33</c>: <c>H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.75</c></description></item>
/// <item><description>SHORT deve essere VERO - fast <c>105</c>: <c>L_d0 &gt; L_d1 * (1 + 0.025)</c></description></item>
/// <item><description>SHORT deve essere FALSO - fast <c>137</c>: <c>(C_d1 &lt; O_d1) E (C_d2 &gt; O_d2)</c></description></item>
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
/// <para><b>Uscite.</b> L'uscita principale e' <b>obbligatoria alla barra 4</b> della sessione
/// per il LONG e alla barra <b>4</b> per lo SHORT, market all'apertura di quella barra; stop e
/// target agiscono solo se scattano prima.</para>
/// <list type="bullet">
/// <item><description>Stop loss: <b>$250</b> per contratto = <b>0,01 pt</b></description></item>
/// <item><description>Take profit: <b>$4.000</b> = <b>0,10 pt</b></description></item>
/// <item><description>Nessun trailing stop</description></item>
/// <item><description>Nessun breakeven</description></item>
/// <item><description>Nessuna uscita a tempo (<c>max_bars = 0</c>)</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> HO, $42.000 per punto, tick 0,0001.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$341</description></item>
/// <item><term>Fuori campione</term><description>$219.699 su 492 trade</description></item>
/// <item><term>Drawdown</term><description>$8.429</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento:
/// <c>HO_4h/consegna/trades/fam01_BIAS.csv</c>. Contano le <b>entrate</b>: timestamp e prezzo.
/// Il riferimento addebita commissione per trade e <b>1 tick di slippage per lato</b>, che
/// l'engine non applica: va rettificato al confronto, non compensato sul livello.</para>
///
/// <para>ATTENZIONE: <b>non mettere su conti diversi</b> insieme a <c>4h fam01-2</c> e
/// <c>4h fam01-3</c> del dossier: emettono gli stessi ordini di entrata, e due sistemi che
/// mandano gli stessi ordini sono copy trading.</para>
/// </summary>
public sealed class PTS_HO_BIA_001_240 : BiasBarCountEngine
{
    public override string Name => "PTS_HO_BIA_001_240";
    public override string Description =>
        "BIAS HO 4 ore: S34 del dossier, run HO_4h, finestra 24h, uscita a indice di barra";
    public override string Symbol => "@HO";
    public override int TimeframeMinutes => 240;

    public PTS_HO_BIA_001_240()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Il BIAS non ha finestra operativa: gli indici di barra sono la regola di entrata. La
        // finestra e' dichiarata piena, nell'orologio della ricerca, perche' ogni PTS deve
        // dichiarare l'orologio in cui legge gli orari.
        TradingWindow = ZonedWindow.Research(0, 2359);   // nessun filtro orario, finestra piena

        EntryType = BiasEntryType.BreakoutStop;   // entrytype = 2
        PatternLibrary = EasyPatternLibrary.Fast;
        BarCountStartsAt = 0;          // le barre di sessione si contano da 0

        ArmBarLong = 1;                // inizio finestra LONG (inclusa)
        EndLong = 4;                   // fine finestra LONG (esclusa)
        ArmBarShort = 1;               // inizio finestra SHORT (inclusa)
        EndShort = 4;                  // fine finestra SHORT (esclusa)
        BreakoutBarsHigh = 2;          // massimo delle 2 barre precedenti
        BreakoutBarsLow = 1;           // minimo della barra precedente
        ExitBarLong = 4;               // barra di uscita LONG
        ExitBarShort = 4;              // barra di uscita SHORT

        PatternLongYes = 103;          // gate fast del long
        PatternLongNo = 33;            // gate fast che impedisce il long
        PatternShortYes = 105;         // gate fast dello short
        PatternShortNo = 137;          // gate fast che impedisce lo short
        NotEntryDayLong = 3;           // niente LONG di giovedi' (0 = lunedi')
        NotEntryDayShort = 3;          // niente SHORT di giovedi'

        StopMoney = 250;         // stop_loss, $ per contratto = 0,01 pt
        ProfitMoney = 4000;      // take_profit, $ per contratto = 0,10 pt
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
