using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_BP_BBO_002_240</b> — BIAS_BO su BP 4h, codice della ricerca <c>BP-4H-BIASBO-66a0de</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>stop</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>del motore</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 1965 trade, netto 27,409; fuori campione 06/2022 → 05/2025 netto
/// 5,336; broker (16/09/2025 → 09/09/2026) 122 trade, netto -1,863.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/BP-4H-BIASBO-66a0de.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ora fissa, poi rottura.* Come il BIAS decide l'ora, ma non entra subito: da quell'ora in poi mette un ordine sulla rottura degli estremi recenti, e entra solo se il prezzo si muove.</para>
/// <para>Da sapere. Al massimo un ingresso per sessione per direzione: dopo il fill l'ordine non si riarma. Solo intraday.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $27,409 su 1965 trade · $14 a trade · drawdown $8,853 · netto/DD 3.10 · anni in perdita 4 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $625 su 1965 trade, drawdown $12,309.</para>
/// <para><b>Breakout dentro una finestra di barre della sessione</b></para>
/// <para>· LONG: stop buy sul massimo delle 5 barre precedenti</para>
/// <para>· SHORT: stop sell sul minimo delle 5 barre precedenti</para>
/// <para>· L'ordine LONG esiste solo dalla barra 1 (inclusa) alla barra 4 (esclusa) della sessione; lo SHORT dalla 1 alla 2.</para>
/// <para>· La finestra si arma alla sua barra di partenza, e solo se i filtri pattern sono veri in quel preciso momento. Una volta armata resta attiva fino a fine finestra, anche se i pattern smettono di essere veri.</para>
/// <para>· Se la barra di partenza è maggiore di quella di fine, la finestra attraversa il cambio di sessione.</para>
/// <para>· Gli estremi rolling si leggono su barre già chiuse.</para>
/// <para>· Le barre della sessione si contano da 0: la prima barra dopo l'inizio sessione è la numero 0.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Non apre posizioni LONG di lunedì</para>
/// <para>· Non apre posizioni SHORT di mercoledì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para><b>Uscite</b></para>
/// <para>· Uscita obbligatoria alla barra 2 della sessione per il LONG e alla barra 6 per lo SHORT, market all'apertura di quella barra.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: nessuno — la posizione si chiude solo con le uscite qui sopra</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_BP_BBO_002_240 : Pt5DavBiasBreakoutEngine
{
    public override string Name => "PT8DAV_BP_BBO_002_240";

    public override string Description => "BIAS_BO BP 4h, ricerca v5.1 BP-4H-BIASBO-66a0de";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "BP-4H-BIASBO-66a0de";

    public PT8DAV_BP_BBO_002_240()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        ArmBarLong = 1;                         // le_bar
        ExitBarLong = 2;                        // lx_bar
        EndLong = 4;                            // end_long
        ArmBarShort = 1;                        // se_bar
        ExitBarShort = 6;                       // sx_bar
        EndShort = 2;                           // end_short
        BreakoutBarsHigh = 5;                   // nhigh
        BreakoutBarsLow = 5;                    // nlow
        PatternLongYes = 152;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 152;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        NotEntryDayLong = 0;                    // not_le_day, pandas
        NotEntryDayShort = 2;                   // not_se_day, pandas
        StopAtr = 0m;                           // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
