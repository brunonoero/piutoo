namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Quale numero della distribuzione misurata diventa lo spread del run.
///
/// <para>La misura di <c>PiootooSpreadDumpBot</c> non e' un valore ma una distribuzione, e le tre
/// scelte rispondono a tre domande diverse. Sta nella richiesta e nel summary perche' due run con
/// statistiche diverse non sono confrontabili, ed e' l'unica traccia che li separa: gli spread
/// applicati sono numeri, e un numero non dice da quale coda della distribuzione viene.</para>
/// </summary>
public enum SpreadStatistic
{
    /// <summary>
    /// Mediana (<c>p50Spread</c>). E' lo spread che si paga <i>di solito</i>, ed e' il default:
    /// risponde a "queste strategie reggono il costo normale di questo broker?".
    /// </summary>
    Median = 0,

    /// <summary>
    /// Media (<c>avgSpread</c>). Comprende la riapertura della domenica sera e le news, cioe'
    /// istanti in cui nessuna strategia sta entrando: sovrastima il costo di un ingresso tipico ma
    /// e' la statistica giusta se si vuole che l'<i>equity</i> complessiva torni, per la stessa
    /// ragione per cui <c>StopFillSlippagePoints</c> usa medie e non mediane.
    /// </summary>
    Mean = 1,

    /// <summary>
    /// Novantesimo percentile (<c>p90Spread</c>). Il caso brutto: serve a chiedersi quanto resta
    /// di una strategia quando entra nel momento sbagliato, non a stimare il risultato atteso.
    /// </summary>
    P90 = 2
}
