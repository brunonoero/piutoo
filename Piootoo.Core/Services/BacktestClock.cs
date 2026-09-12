namespace Piootoo.Core.Services;

/// <summary>
/// L'orologio del loop di backtesting: su quale barra il motore avanza, e quindi da quale barra
/// esce il prezzo di riempimento. E' <b>sempre il minuto</b>, e non e' una scelta della richiesta.
///
/// <para><b>Perche' e' una convenzione di fill e non un dettaglio di prestazione.</b> Il motore
/// riempie sulla barra che sta in <c>currentBars</c>, cioe' la piu' fitta caricata per quel
/// simbolo. Su una barra da sessanta minuti che contiene sia lo stop protettivo sia il target,
/// quale dei due scatti prima non e' un dato: e' la convenzione <c>ProtectiveBeforeTarget</c>,
/// dichiarata nel summary proprio perche' e' una scelta. Con l'orologio a un minuto quella
/// domanda ha una risposta misurata, e con essa i trigger dei pending e il mark-to-market.</para>
///
/// <para><b>Perche' non si sceglie piu'.</b> Fino al 12/09/2026 la richiesta poteva chiedere
/// l'orologio al timeframe piu' corto delle strategie, ed era il default. Compare-0040 ha misurato
/// quanto costa: con l'orologio a 15 minuti le coppie di trade che coincidono al minuto con il cBot
/// scendono da 1.259 su 1.433 a 131 su 641, e le FDAX giornaliere entrano con 80-100 minuti di
/// scarto. Un run che non gira al minuto non e' confrontabile con il conto, quindi non e' un run
/// che serva. Il minuto divide ogni timeframe del catalogo, percio' nessuna strategia puo' essere
/// saltata dal tick del loop: la validazione che esisteva per gli altri orologi non ha piu' un
/// caso da coprire.</para>
///
/// <para>Il prezzo e' il feed: un simbolo del run senza <c>@SYM_1.json</c> fa fallire l'avvio come
/// qualsiasi datafeed mancante, invece di girare al proprio timeframe e mescolare due risoluzioni
/// di riempimento nello stesso run.</para>
/// </summary>
public static class BacktestClock
{
    /// <summary>Il timeframe dell'orologio, in minuti.</summary>
    public const int TimeframeMinutes = 1;
}
