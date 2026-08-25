using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Configuration;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PiutooStrategies;

/// <summary>
/// PTS_NQ_TFM_004_15 — TF_M su NQ a 15 minuti, famiglia 02 della consegna
/// <c>run_20260814_1453</c>.
///
/// <para><b>Codice sorgente: nessuno.</b> riga 2/8 di run_20260814_1453, disabilitata: doppione, non e' fra le univoche del dossier. Non ha quindi un
/// S-ID a cui riferirsi per il controllo contro la sorgente: la fonte resta il run
/// citato qui sopra.</para>
///
/// <para>Trend following simmetrico: stop buy su <c>H_d1</c>, stop sell su <c>L_d1</c>. Long e short
/// condividono i gate neutri e usano i direzionali a specchio.</para>
///
/// <para><b>Sessione e fuso.</b> Le sessioni <c>d0..d5</c> su cui girano i pattern sono il
/// <b>giorno di calendario europeo</b>, 00:00 → 00:00, come il motore Python che taglia con
/// <c>(timestamp − 1 min − session_start_hour).normalize()</c> e <c>session_start_hour = 0</c>.
/// Non è la sessione del broker, ed è una scelta di modello della ricerca che il port riproduce
/// tale e quale: le due coincidono quasi sempre — mezzanotte a Roma sono le 17:00 a Chicago — ma
/// non nelle settimane in cui l'ora legale americana ed europea non sono allineate. Lo stesso
/// confine governa il secchio di <c>MaxEntriesPerSession</c>, quindi vale per pattern e limite di
/// fill insieme.</para>
///
/// <para><b>Niente dipende da come è stampato il feed.</b> Sessione e finestra dichiarano il
/// proprio fuso e il confronto passa dall'istante assoluto della barra: il feed dichiara il suo
/// orologio in <c>datafeed/feed-clocks.json</c> e viene convertito a UTC vero al caricamento.
/// Vedi <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
///
/// <para><b>Filtri pattern.</b></para>
/// <para><b>Filtro comune a long e short</b></para>
/// <list type="bullet">
/// <item><description>deve essere VERO — neutrale 54: <c>(H_d1-L_d1) &gt; (H_d2-L_d2)</c></description></item>
/// <item><description>deve essere FALSO — neutrale 9: <c>|O_d5-C_d1| &lt; 0.1 * (H_d5-L_d1)</c></description></item>
/// </list>
/// <para><b>Solo LONG</b></para>
/// <list type="bullet">
/// <item><description>deve essere FALSO — direzionale 17: <c>C_d1 &gt; C_d2 * (1 + 0.015)</c></description></item>
/// </list>
/// <para><b>Solo SHORT</b></para>
/// <list type="bullet">
/// <item><description>deve essere FALSO — direzionale 17: <c>C_d1 &lt; C_d2 * (1 - 0.015)</c></description></item>
/// </list>
///
/// <para><b>Quando può operare.</b></para>
/// <list type="bullet">
/// <item><description>Opera solo fra 13:00 e 05:00 (a cavallo della mezzanotte), ora dei dati (CET)</description></item>
/// <item><description>Può restare aperta oltre la sessione (multiday)</description></item>
/// <item><description>Al massimo una entrata per sessione e per direzione</description></item>
/// </list>
///
/// <para><b>Uscite.</b> Sono autocontenute nel segnale di ingresso e vengono applicate
/// dall'engine o dal broker: la strategia non emette mai segnali di chiusura.</para>
/// <list type="bullet">
/// <item><description>Stop loss: $1,000 per contratto = 50.00 pt</description></item>
/// <item><description>Take profit: $6,000 = 300.00 pt</description></item>
/// <item><description>Nessuna uscita a tempo</description></item>
/// </list>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto, tick 0,25 punti.</para>
///
/// <para><b>Metriche di validazione storica — non sono garanzie di rendimento.</b></para>
/// <list type="table">
/// <listheader><term>Metrica</term><description>Valore</description></listheader>
/// <item><term>Atteso per trade</term><description>$147.0</description></item>
/// <item><term>Out-of-sample</term><description>$88,354 su 294 trade &#183; drawdown $23,408 &#183; profit factor 1.37 &#183; $301 per trade.</description></item>
/// <item><term>Plateau minimo</term><description>0.83</description></item>
/// <item><term>Efficienza Walk-Forward</term><description>0.49</description></item>
/// <item><term>Monte Carlo drawdown p95</term><description>$47,488</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><b>Disabilitata: doppione di <see cref="PTS_NQ_TFM_003_15"/>.</b> Il gate <c>ptn_dir_yes = 52</c> e' la sentinella sempre-vera del motore, quindi il filtro
/// direzionale si riduce al solo <c>ptn_dir_no = 17</c> — lo stesso della 003. Le due emettono
/// gli stessi ordini di entrata.</para>
///
/// <para>Il dossier <c>run-engine/run-01-agosto/dossier_ctrader_NQ.md</c> conta 15 strategie
/// univoche, non 18, perche' deduplica anche fra timeframe diversi: questa non compare, e la
/// capofila corrispondente e' <c>S05</c>.</para>
///
/// <para><b>Perche' resta nel repository.</b> Il sorgente documenta una riga approvata dalla
/// ricerca e serve a rifare il confronto con la capofila. Ma non deve finire in un masterfilter:
/// due sistemi che mandano gli stessi ordini su conti separati sono copy trading, e presso una
/// prop firm costano il conto (dossier §6). L'attributo la toglie dal catalogo lasciandola
/// istanziabile per nome.</para>
/// </remarks>
[StrategiaDisabilitata(
    "Doppione di PTS_NQ_TFM_003_15: stessi ordini di entrata. Vedi dossier_ctrader_NQ.md §6.")]
public sealed class PTS_NQ_TFM_004_15 : TfMirroredEngine
{
    public override string Name => "PTS_NQ_TFM_004_15";
    public override string Description =>
        "TF_M NQ 15m: famiglia 02 run 20260814, finestra 13:00–05:00 CET, multiday";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 15;

    public PTS_NQ_TFM_004_15()
    {
        // Confine di sessione del run: giorno di calendario europeo, come
        // (timestamp - 1 min - session_start_hour).normalize() del motore Python.
        // NON e' la sessione del broker: le due divergono nelle settimane di
        // disallineamento fra ora legale americana ed europea.
        Session = ZonedWindow.ResearchSession();
        Contracts = 1;

        // Finestra operativa: start_hour/end_hour del run, verbatim nell'orologio
        // della ricerca. Nessuna conversione: il fuso viaggia con il dato.
        TradingWindow = ZonedWindow.ResearchHours(13, 5);
        SkipDay = -1;   // skip_day (0 = lunedì, -1 = nessuno)

        NeutralYes = 54;     // ptn_neut_yes
        NeutralNo = 9;       // ptn_neut_no
        DirectionalYes = 52; // ptn_dir_yes, specchiato dal motore
        DirectionalNo = 17;  // ptn_dir_no, specchiato dal motore

        IntradayOnly = false; // intraday_only

        StopMoney = 1000;   // stop_loss, $ per contratto = 50.00 pt
        ProfitMoney = 6000; // take_profit, $ per contratto
        MaxBars = 0;        // max_bars  (0 = nessuna uscita a tempo)
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
        if (parameters.TryGetValue("StartHour", out var startHour))
            TradingWindow = TradingWindow! with { StartHhmm = Convert.ToInt32(startHour) * 100 };
        if (parameters.TryGetValue("EndHour", out var endHour))
            TradingWindow = TradingWindow! with { EndHhmm = Convert.ToInt32(endHour) * 100 };
    }
}
