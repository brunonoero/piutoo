using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_KC_RHL_001_30</b> — RHL su KC 30m, codice della ricerca <c>KC-30M-RHL-568d96</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>limit</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>intraday</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 260 trade, netto 36,015; fuori campione 06/2022 → 05/2025 netto
/// 11,375; broker (16/09/2025 → 09/09/2026) 17 trade, netto 3,274.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/KC-30M-RHL-568d96.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ritorno sugli estremi di ieri.* Aspetta che il prezzo scenda sotto il minimo di ieri e compra li', scommettendo che sia un eccesso da cui rimbalzera'.</para>
/// <para>Da sapere. Lo scostamento e' in TICK, quindi e' una distanza fissa: su un mercato che cambia volatilita' nel tempo la stessa strategia diventa un'altra cosa. E' il motivo per cui l'oro con questo motore si comporta in modo diverso sul broker rispetto alla sua storia.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $36,015 su 260 trade · $139 a trade · drawdown $20,992 · netto/DD 1.72 · anni in perdita 5 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $27,631 su 260 trade, drawdown $10,948.</para>
/// <para><b>Ordine LIMITE sugli estremi della sessione precedente</b></para>
/// <para>· LONG: limit buy a L_d1 (minimo della sessione precedente)</para>
/// <para>· SHORT: limit sell a H_d1 (massimo della sessione precedente)</para>
/// <para>· I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.</para>
/// <para>· Il fill richiede penetrazione stretta del livello ('minimo &lt; livello' per il long): il semplice tocco NON riempie.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 11: '(C_d1 &lt; C_d2) E (C_d2 &lt; C_d3) E (C_d3 &lt; C_d4) E (C_d4 &lt; C_d5)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 11: '(C_d1 &gt; C_d2) E (C_d2 &gt; C_d3) E (C_d3 &gt; C_d4) E (C_d4 &gt; C_d5)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 04:20-13:30, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 13:30 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_KC_RHL_001_30 : Pt5DavRhlEngine
{
    public override string Name => "PT8DAV_KC_RHL_001_30";

    public override string Description => "RHL KC 30m, ricerca v5.1 KC-30M-RHL-568d96";

    public override string Symbol => "@KC";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "KC-30M-RHL-568d96";

    public PT8DAV_KC_RHL_001_30()
    {
        TradingWindow = ResearchWindow(-1, -1); // start_hour -1, end_hour -1, verbatim
        LongOffsetAtr = 0m;                     // long_offset_atr
        ShortOffsetAtr = 0m;                    // short_offset_atr
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 11;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
