using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_NQ_RHL_001_60</b> — RHL su NQ 1h, codice della ricerca <c>NQ-1H-RHL-8f783f</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>limit</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>overnight 2 giornate</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 210 trade, netto 571,019; fuori campione 06/2022 → 05/2025 netto
/// 20,342; broker (16/09/2025 → 09/09/2026) 17 trade, netto 88,501.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/NQ-1H-RHL-8f783f.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ritorno sugli estremi di ieri.* Aspetta che il prezzo scenda sotto il minimo di ieri e compra li', scommettendo che sia un eccesso da cui rimbalzera'.</para>
/// <para>Da sapere. Lo scostamento e' in TICK, quindi e' una distanza fissa: su un mercato che cambia volatilita' nel tempo la stessa strategia diventa un'altra cosa. E' il motivo per cui l'oro con questo motore si comporta in modo diverso sul broker rispetto alla sua storia.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $571,019 su 210 trade · $2,719 a trade · drawdown $86,774 · netto/DD 6.58 · anni in perdita 2 su 14 · notti a mercato 2.28 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $145,308 su 210 trade, drawdown $46,253.</para>
/// <para><b>Ordine LIMITE sugli estremi della sessione precedente</b></para>
/// <para>· LONG: limit buy a L_d1 − 0.02 × ATR50 (minimo della sessione precedente)</para>
/// <para>· SHORT: limit sell a H_d1 + 0.05 × ATR50 (massimo della sessione precedente)</para>
/// <para>· I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.</para>
/// <para>· Il fill richiede penetrazione stretta del livello ('minimo &lt; livello' per il long): il semplice tocco NON riempie.</para>
/// <para>· Solo long: il lato short non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 28: '|O_d5-C_d1| &gt; 0.25 * (HH5-LL5)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 02:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 02:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 2 giornate (46 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.25 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT8DAV_NQ_RHL_001_60 : Pt5DavRhlEngine
{
    public override string Name => "PT8DAV_NQ_RHL_001_60";

    public override string Description => "RHL NQ 1h, ricerca v5.1 NQ-1H-RHL-8f783f";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "NQ-1H-RHL-8f783f";

    public PT8DAV_NQ_RHL_001_60()
    {
        TradingWindow = ResearchWindow(-1, 2);  // start_hour -1, end_hour 2, verbatim
        LongOffsetAtr = 0.02m;                  // long_offset_atr
        ShortOffsetAtr = 0.05m;                 // short_offset_atr
        Direction = 1;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 28;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 2 giornate)
        MaxBars = 46;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 1.25m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
