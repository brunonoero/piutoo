using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_YM_RHL_002_60</b> — RHL su YM 1h, codice della ricerca <c>YM-1H-RHL-be711d</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 100 trade, netto $134,602; broker (27/08/2025 → 09/09/2026)
/// 6 trade, netto $12,791. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/YM-1H-RHL-be711d.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ritorno sugli estremi di ieri.* Aspetta che il prezzo scenda sotto il minimo di ieri e compra li', scommettendo che sia un eccesso da cui rimbalzera'.</para>
/// <para>Da sapere. Lo scostamento e' in TICK, quindi e' una distanza fissa: su un mercato che cambia volatilita' nel tempo la stessa strategia diventa un'altra cosa. E' il motivo per cui l'oro con questo motore si comporta in modo diverso sul broker rispetto alla sua storia.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $134,602 su 100 trade · $1,346 a trade · drawdown $23,380 · netto/DD 5.76 · anni in perdita 2 su 12 · notti a mercato 1.36 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $62,806 su 100 trade, drawdown $10,294.</para>
/// <para><b>Ordine LIMITE sugli estremi della sessione precedente</b></para>
/// <para>· LONG: limit buy a L_d1 − 0.10 × ATR50 (minimo della sessione precedente)</para>
/// <para>· SHORT: limit sell a H_d1 + 0.50 × ATR50 (massimo della sessione precedente)</para>
/// <para>· I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.</para>
/// <para>· Il fill richiede penetrazione stretta del livello ('minimo &lt; livello' per il long): il semplice tocco NON riempie.</para>
/// <para>· Solo long: il lato short non opera mai.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 33: '(H_d0-L_d0) &gt; L_d0 * 0.01'</para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 29: 'H_d0 &lt; H_d1 * (1 - 0.01)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 29: 'L_d0 &gt; L_d1 * (1 + 0.01)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 14:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 14:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 2 giornate (46 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.50 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_YM_RHL_002_60 : Pt5DavRhlEngine
{
    public override string Name => "PT5DAV_YM_RHL_002_60";

    public override string Description => "RHL YM 1h, ricerca PT5DAV YM-1H-RHL-be711d";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "YM-1H-RHL-be711d";

    public PT5DAV_YM_RHL_002_60()
    {
        TradingWindow = ResearchWindow(-1, 14); // start_hour -1, end_hour 14, verbatim
        LongOffsetAtr = 0.1m;                   // long_offset_atr
        ShortOffsetAtr = 0.5m;                  // short_offset_atr
        Direction = 1;                          // direction: 0 entrambe, 1 long, 2 short
        NeutralYes = 33;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 29;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 2 giornate)
        MaxBars = 46;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.5m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
