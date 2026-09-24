using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_FDAX_MAC_002_30</b> — MAC su FDAX 30m, codice della ricerca <c>FDAX-30M-MAC-9c292b</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 928 trade, netto $484,303; broker (27/08/2025 → 09/09/2026)
/// 65 trade, netto $-56,439. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/FDAX-30M-MAC-9c292b.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Incrocio di due medie mobili.* Il classico: compra quando la media veloce passa sopra la lenta, vende quando passa sotto.</para>
/// <para>Da sapere. E' l'unico motore che tiene la posizione la notte per costruzione, e l'unico che non usa pattern. Esce sull'incrocio inverso, e sugli intraday anche all'ultima barra del venerdi'. Non esce mai a fine sessione normale.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $484,303 su 928 trade · $522 a trade · drawdown $64,332 · netto/DD 7.53 · anni in perdita 3 su 14 · notti a mercato 0.60 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $206,169 su 928 trade, drawdown $36,386.</para>
/// <para><b>Incrocio di medie mobili 20/24</b></para>
/// <para>· Due medie mobili semplici sulla close: veloce a 20 barre, lenta a 24 barre.</para>
/// <para>· Segnale LONG: la veloce incrocia sopra la lenta. SHORT: incrocia sotto.</para>
/// <para>· Filtro gradiente: su 1 barre la veloce deve essersi mossa, in valore assoluto, almeno 1.2 volte quanto la lenta nello stesso tratto.</para>
/// <para>· Filtro sulla sessione precedente: dev'essere di indecisione — '|C_d1 − O_d1| ≤ 0.5 × (H_d1 − L_d1)' — e verde ('C_d1 &gt; O_d1') perché il long operi, rossa perché operi lo short.</para>
/// <para>· Entrata MARKET all'apertura della barra successiva al segnale.</para>
/// <para>· Questo motore non usa filtri pattern.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Tiene la posizione oltre la fine della sessione: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi</para>
/// <para>· Nessun limite al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para><b>Uscite</b></para>
/// <para>· Uscita su incrocio inverso delle due medie, eseguita sulla barra successiva al segnale.</para>
/// <para>· Uscita forzata all'ultima barra del venerdì: nessuna posizione resta aperta nel fine settimana.</para>
/// <para>· Sono le uscite principali del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_FDAX_MAC_002_30 : Pt5DavMovingAverageCrossoverEngine
{
    public override string Name => "PT5DAV_FDAX_MAC_002_30";

    public override string Description => "MAC FDAX 30m, ricerca PT5DAV FDAX-30M-MAC-9c292b";

    public override string Symbol => "@FDAX";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "FDAX-30M-MAC-9c292b";

    public PT5DAV_FDAX_MAC_002_30()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        FastPeriod = 20;                        // fast
        SlowPeriod = 24;                        // slow
        GradientPeriod = 1;                     // gradient_length
        GradientFactor = 1.2m;                  // gradient_factor
        DailyFactor = 0.5m;                     // daily_factor
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
