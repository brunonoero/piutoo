using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_TFU_001_240</b> — TF_U su BTC 4h, codice della ricerca <c>BTC-4H-TFU-82f8b9</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 612 trade, netto $1,159,738; broker (27/08/2025 → 09/09/2026)
/// 138 trade, netto $-172,238. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-4H-TFU-82f8b9.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Trend following con i due lati indipendenti.* Stesso ingresso del TF_M — rottura degli estremi di ieri — ma long e short hanno filtri propri, scelti separatamente.</para>
/// <para>Da sapere. Avendo due insiemi di filtri invece di uno, lo spazio di ricerca e' piu' grande: piu' facile trovare qualcosa che funziona per caso. E il filtro di un lato puo' essere messo su "sempre falso": quel lato si spegne e la strategia opera in una sola direzione.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $1,159,738 su 612 trade · $1,895 a trade · drawdown $327,278 · netto/DD 3.54 · anni in perdita 2 su 8 · notti a mercato 2.91 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $476,419 su 612 trade, drawdown $163,804.</para>
/// <para><b>Ordine STOP sugli estremi della sessione precedente</b></para>
/// <para>· LONG: stop buy a H_d1 (massimo della sessione precedente)</para>
/// <para>· SHORT: stop sell a L_d1 (minimo della sessione precedente)</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Non apre posizioni di lunedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 10 giornate (60 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BTC_TFU_001_240 : Pt5DavTfUnmirroredEngine
{
    public override string Name => "PT5DAV_BTC_TFU_001_240";

    public override string Description => "TF_U BTC 4h, ricerca PT5DAV BTC-4H-TFU-82f8b9";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "BTC-4H-TFU-82f8b9";

    public PT5DAV_BTC_TFU_001_240()
    {
        TradingWindow = ResearchWindow(-1, -1); // start_hour -1, end_hour -1, verbatim
        FastYesLong = 152;                      // ptn_ly_yes
        FastNoLong = 153;                       // ptn_ly_no
        FastYesShort = 152;                     // ptn_sy_yes
        FastNoShort = 153;                      // ptn_sy_no
        SkipDay = 0;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 10 giornate)
        MaxBars = 60;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
