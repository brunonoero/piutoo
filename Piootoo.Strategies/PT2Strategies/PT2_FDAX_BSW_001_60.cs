using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT2Strategies;

/// <summary>
/// PT2_FDAX_BSW_001_60 - BIASW su FDAX a 60 minuti, <b>S01</b> del dossier
/// <c>run-engine-v2/DOSSIER_PANIERE_001.md</c> (paniere del 14/09/2026, quattro strategie).
///
/// <para><b>Codice sorgente: S01 (run-engine-v2, DOSSIER_PANIERE_001).</b> E' l'identificativo con cui
/// questa strategia compare nel dossier: e' da li' che si risale a condizioni, filtri e parametri per un
/// controllo, senza riaprire i CSV a tentativi. La cella di ricerca e' <c>FDAX_1h</c>, famiglia
/// <c>fam01</c>, motore <c>BIASW</c>. La serie <c>PT2_*</c> nasce dal rifacimento dell'analisi: le
/// classi <c>PTS_*</c> restano e non descrivono queste righe.</para>
///
/// <para><b>Che cosa fa.</b> Bias settimanale: un solo ingresso market programmato a giorno e ora
/// fissi, con uscita programmata a giorno e ora fissi della settimana successiva. Il lato short e'
/// spento.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> cominciano alle <b>01:00 dell'orologio della
/// ricerca</b> (CET) e durano fino alla stessa ora del giorno dopo: e' quanto la tabella §2.1.1 del
/// dossier dichiara per FDAX (<c>session_start_hour = 1</c>) e lo risolve il calendario del simbolo,
/// non la classe. Gli orari di ingresso e uscita sono nell'orologio della ricerca, riportati
/// <b>verbatim</b>, mai convertiti nell'ora di borsa.</para>
///
/// <para><b>Etichetta della barra.</b> I run di <c>run-engine-v2</c> etichettano le candele
/// <b>all'inizio</b> (§2.6 del dossier), come il feed Piootoo e al contrario dei run da cui vengono le
/// <c>PTS_*</c>. Il motore Python (<c>bias_weekly.py</c>) confronta <c>le_time</c>/<c>lx_time</c> con
/// l'etichetta della barra e riempie all'apertura di quella barra: «MARKET alle 08:00 di lunedì
/// (apertura della barra da 60 minuti che chiude alle 09:00)» e' quindi <c>le_time = 08:00</c>, la
/// barra 08:00-09:00. La classe dichiara <c>ResearchLabelsBarsOnOpen = true</c> e riporta il numero
/// <b>verbatim</b>: e' l'engine a confrontarlo con l'apertura invece che con la chiusura. Senza
/// quella dichiarazione lo stesso 08:00 sarebbe letto come etichetta di chiusura e la strategia
/// entrerebbe un'ora prima, sulla barra 07:00-08:00. Vedi
/// <c>docs/domini/porting-da-report-sweep.md</c> §"L'etichetta della barra la dichiara la strategia".</para>
///
/// <para><b>Ciclo settimanale.</b></para>
/// <list type="bullet">
/// <item><description>LONG: MARKET all'apertura della barra <b>08:00-09:00 di lunedì</b> (etichetta 08:00), ora della ricerca (CET)</description></item>
/// <item><description>SHORT: spento - questa strategia non apre mai al ribasso</description></item>
/// <item><description>Il segnale nasce sulla barra prima di quella pianificata, come <c>next bar at market</c>, e si riempie all'apertura di quella pianificata</description></item>
/// <item><description>Se quella barra non esiste (festivo, mercato chiuso) la settimana salta</description></item>
/// <item><description>Al massimo una entrata per settimana e per direzione</description></item>
/// </list>
///
/// <para><b>Filtri pattern.</b> <i>Nessun filtro pattern</i>: il motore entra su ogni segnale
/// strutturale. I gate fast restano alle sentinelle 152 (sempre vero) e 153 (sempre falso), che non
/// filtrano nulla.</para>
///
/// <para><b>Quando puo' operare.</b></para>
/// <list type="bullet">
/// <item><description>Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno gia' parte della regola di entrata: la finestra e' dichiarata piena</description></item>
/// <item><description>Tiene la posizione <b>oltre la fine della sessione</b>: il motore BIASW non chiude mai per fine sessione e non c'e' un parametro che lo cambi</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Uscita LONG programmata: «lunedì alle 09:00, market alla chiusura della barra che termina a quell'ora», cioe' la barra <b>08:00-09:00 di lunedì</b>, etichetta 08:00: <c>lx_time = 08:00</c>, il dossier stampa l'istante della chiusura. E' la stessa barra dell'ingresso, quindi il motore la risolve alla <b>settimana successiva</b> (<c>ResolveScheduledExitUtc</c> prende la prima occorrenza <i>dopo</i> la barra di ingresso): la posizione dura una settimana, l'unica lettura compatibile con 166 trade in tre anni e mezzo e un drawdown di $99.782. L'engine interno esegue la chiusura al mark di quella barra, cioe' alla sua chiusura, come il dossier descrive. Se la barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana dopo.</description></item>
/// <item><description>E' l'uscita principale del motore: stop e target agiscono solo se scattano prima.</description></item>
/// <item><description>Stop loss: <b>$44.700</b> per contratto = <b>1.788,00 pt</b></description></item>
/// <item><description>Take profit: <b>nessuno</b></description></item>
/// <item><description>Nessun trailing, nessun breakeven, nessuna uscita a tempo</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> FDAX, 25 per punto (il dossier lo scrive in $, il registro
/// strumenti in EUR: la conversione in punti e' la stessa), tick 1 punto.</para>
///
/// <para><b>Metriche di validazione storica - non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$1.152</description></item>
/// <item><term>Fuori campione</term><description>$191.311 su 166 trade (24/01/2022 → 30/05/2025)</description></item>
/// <item><term>Drawdown</term><description>$99.782</description></item>
/// </list>
///
/// <para><b>Verifica del porting.</b> Lista trade di riferimento dichiarata dal dossier:
/// <c>FDAX_1h/consegna/trades/fam01_BIASW.csv</c>, che <b>non e' nel repository</b>: in
/// <c>run-engine-v2/</c> c'e' il solo dossier. Contano le <b>entrate</b>: timestamp e prezzo. Costi
/// del riferimento: $4,00 di commissione per trade e 1 tick di slippage per lato, che l'engine non
/// applica. Manca anche il datafeed <c>@FDAX_60</c>: va generato con <c>aggregate_flat_feed.py</c>
/// prima di qualunque backtest.</para>
/// </summary>
public sealed class PT2_FDAX_BSW_001_60 : BiasWeeklyEngine
{
    public override string Name => "PT2_FDAX_BSW_001_60";
    public override string Description =>
        "BIASW FDAX 60m: S01 di run-engine-v2/DOSSIER_PANIERE_001, long lunedì 08:00-09:00 → lunedì successivo 08:00-09:00 CET, solo long, multiday";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 60;

    public PT2_FDAX_BSW_001_60()
    {
        // Il BIASW non ha finestra operativa: giorno e ora di ingresso sono gia' la regola di
        // entrata. La finestra e' dichiarata piena, nell'orologio della ricerca, perche' ogni
        // strategia del catalogo deve dichiarare l'orologio in cui legge gli orari
        // (StrategyClockConformanceTests).
        TradingWindow = ZonedWindow.AllDay;

        // I run di run-engine-v2 etichettano le barre all'INIZIO (§2.6 del dossier): le_time e
        // lx_time vanno confrontati con l'apertura della barra, non con la chiusura come per le
        // PTS_*. Si dichiara qui e si riportano i numeri verbatim: mai convertirli a mano.
        ResearchLabelsBarsOnOpen = true;

        Contracts = 1;

        EnableLong = true;
        EnableShort = false;

        EntryDayLong = 0;                     // le_day: 0 = lunedi' (convenzione pandas)
        EntryTimeLong = new TimeOnly(8, 0);   // le_time: 08:00, la barra 08:00-09:00 (etichetta = apertura)
        ExitDayLong = 0;                      // lx_day: lunedi'
        // lx_time: 08:00, la stessa barra 08:00-09:00 («chiusura della barra che termina alle
        // 09:00»), risolta alla settimana successiva perche' coincide con la barra di ingresso.
        ExitTimeLong = new TimeOnly(8, 0);

        EntryDayShort = -1;                   // se_day: lato short spento
        ExitDayShort = -1;

        FastYesLong = 152;                    // ptn_ly_yes: sentinella sempre vera (nessun filtro)
        FastNoLong = 153;                     // ptn_ly_no:  sentinella sempre falsa (nessun filtro)
        FastYesShort = 152;                   // lato spento
        FastNoShort = 153;

        StopMoneyLong = 44700m;               // stop_loss, $ per contratto = 1.788,00 pt
        ProfitMoneyLong = 0m;                 // take_profit: nessuno
        StopMoneyShort = 0m;
        ProfitMoneyShort = 0m;
        BreakEvenMoneyLong = 0m;
        BreakEvenMoneyShort = 0m;
        TrailingMoneyLong = 0m;
        TrailingMoneyShort = 0m;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("StopLoss", out var stopLoss))
            StopMoneyLong = Convert.ToDecimal(stopLoss);
        if (parameters.TryGetValue("TakeProfit", out var takeProfit))
            ProfitMoneyLong = Convert.ToDecimal(takeProfit);
        if (parameters.TryGetValue("EntryDayLong", out var entryDayLong))
            EntryDayLong = Convert.ToInt32(entryDayLong);
        if (parameters.TryGetValue("EntryTimeLong", out var entryTimeLong))
            EntryTimeLong = TimeFromLegacyHhmm(entryTimeLong);
        if (parameters.TryGetValue("ExitDayLong", out var exitDayLong))
            ExitDayLong = Convert.ToInt32(exitDayLong);
        if (parameters.TryGetValue("ExitTimeLong", out var exitTimeLong))
            ExitTimeLong = TimeFromLegacyHhmm(exitTimeLong);
        if (parameters.TryGetValue("PtnLyYes", out var lyYes))
            FastYesLong = Convert.ToInt32(lyYes);
        if (parameters.TryGetValue("PtnLyNo", out var lyNo))
            FastNoLong = Convert.ToInt32(lyNo);
    }
}
