using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_NQ_RHL_002_15</b> — RHL su NQ 15m, codice della ricerca <c>NQ-15M-RHL-873f51</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 525 trade, netto $973,362; broker (27/08/2025 → 09/09/2026)
/// 44 trade, netto $71,568. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/NQ-15M-RHL-873f51.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ritorno sugli estremi di ieri.* Aspetta che il prezzo scenda sotto il minimo di ieri e compra li', scommettendo che sia un eccesso da cui rimbalzera'.</para>
/// <para>Da sapere. Lo scostamento e' in TICK, quindi e' una distanza fissa: su un mercato che cambia volatilita' nel tempo la stessa strategia diventa un'altra cosa. E' il motivo per cui l'oro con questo motore si comporta in modo diverso sul broker rispetto alla sua storia.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $973,362 su 525 trade · $1,854 a trade · drawdown $103,901 · netto/DD 9.37 · anni in perdita 1 su 14 · notti a mercato 2.87 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $325,922 su 525 trade, drawdown $68,513.</para>
/// <para><b>Ordine LIMITE sugli estremi della sessione precedente</b></para>
/// <para>· LONG: limit buy a L_d1 (minimo della sessione precedente)</para>
/// <para>· SHORT: limit sell a H_d1 + 0.02 × ATR50 (massimo della sessione precedente)</para>
/// <para>· I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.</para>
/// <para>· Il fill richiede penetrazione stretta del livello ('minimo &lt; livello' per il long): il semplice tocco NON riempie.</para>
/// <para>· Solo long: il lato short non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 01:00 e 14:00, CET: ordini emessi sulle barre che chiudono fra le 01:00 e le 14:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di venerdì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 3 giornate (276 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 2.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.30 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT5DAV_NQ_RHL_002_15 : Pt5DavRhlEngine
{
    public override string Name => "PT5DAV_NQ_RHL_002_15";

    public override string Description => "RHL NQ 15m, ricerca PT5DAV NQ-15M-RHL-873f51";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 15;

    public override string ResearchCode => "NQ-15M-RHL-873f51";

    public PT5DAV_NQ_RHL_002_15()
    {
        TradingWindow = ResearchWindow(1, 14);  // start_hour 1, end_hour 14, verbatim
        LongOffsetAtr = 0m;                     // long_offset_atr
        ShortOffsetAtr = 0.02m;                 // short_offset_atr
        Direction = 1;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 4;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 3 giornate)
        MaxBars = 276;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 2.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1.3m;                       // take_profit_atr
    }
}
