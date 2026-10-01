using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_YM_MAC_002_240</b> — MAC su YM 4h, codice della ricerca <c>YM-4H-MAC-0f5aea</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>nessun limite</b>, tenuta: <b>del motore</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 559 trade, netto 188,199; fuori campione 06/2022 → 05/2025 netto
/// 12,967; broker (16/09/2025 → 09/09/2026) 48 trade, netto -3,877.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/YM-4H-MAC-0f5aea.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Incrocio di due medie mobili.* Il classico: compra quando la media veloce passa sopra la lenta, vende quando passa sotto.</para>
/// <para>Da sapere. E' l'unico motore che tiene la posizione la notte per costruzione, e l'unico che non usa pattern. Esce sull'incrocio inverso, e sugli intraday anche all'ultima barra del venerdi'. Non esce mai a fine sessione normale.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $188,199 su 559 trade · $337 a trade · drawdown $47,255 · netto/DD 3.98 · anni in perdita 3 su 14 · notti a mercato 2.19 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $102,851 su 559 trade, drawdown $28,123.</para>
/// <para><b>Incrocio di medie mobili 5/24</b></para>
/// <para>· Due medie mobili semplici sulla close: veloce a 5 barre, lenta a 24 barre.</para>
/// <para>· Segnale LONG: la veloce incrocia sopra la lenta. SHORT: incrocia sotto.</para>
/// <para>· Filtro gradiente: su 5 barre la veloce deve essersi mossa, in valore assoluto, almeno 1.2 volte quanto la lenta nello stesso tratto.</para>
/// <para>· Filtro sulla sessione precedente: dev'essere di indecisione — '|C_d1 − O_d1| ≤ 1 × (H_d1 − L_d1)' — e verde ('C_d1 &gt; O_d1') perché il long operi, rossa perché operi lo short.</para>
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
/// <para>· Stop loss: 0.80 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_YM_MAC_002_240 : Pt5DavMovingAverageCrossoverEngine
{
    public override string Name => "PT8DAV_YM_MAC_002_240";

    public override string Description => "MAC YM 4h, ricerca v5.1 YM-4H-MAC-0f5aea";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "YM-4H-MAC-0f5aea";

    public PT8DAV_YM_MAC_002_240()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        FastPeriod = 5;                         // fast
        SlowPeriod = 24;                        // slow
        GradientPeriod = 5;                     // gradient_length
        GradientFactor = 1.2m;                  // gradient_factor
        DailyFactor = 1m;                       // daily_factor
        Direction = 0;                          // direction: 0 entrambe, 1 long, 2 short
        StopAtr = 0.8m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
