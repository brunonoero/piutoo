namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Su quale asse della misura si legge lo spread: una costante per simbolo, oppure il valore
/// dell'ora in cui l'ingresso avviene.
///
/// <para><b>Perche' e' separato da <see cref="SpreadStatistic"/>.</b> Le due scelte rispondono a
/// domande diverse e si combinano: la statistica dice <i>quale colonna</i> del file diventa il
/// numero (p50, media, p90), la risoluzione dice <i>quale riga</i>. <c>PiootooSpreadDumpBot</c>
/// scrive entrambi i file con le stesse colonne, quindi "p90 dell'ora" e' una domanda legittima
/// tanto quanto "p50 dell'ora", e infilare la risoluzione nella statistica avrebbe raddoppiato
/// l'enum per dire una cosa sola.</para>
///
/// <para><b>Perche' l'ora conta.</b> Lo spread di un simbolo varia dentro la giornata piu' di
/// quanto vari fra un mese e l'altro: la sessione liquida costa un tick, la riapertura della
/// domenica sera e le news costano dieci volte tanto. Una strategia opera dentro la propria
/// <c>TradingWindow</c> e paga lo spread di <i>quelle</i> ore, quindi la costante per simbolo e'
/// un compromesso fra strategie che non entrano mai nello stesso momento. Vedi
/// <c>docs/domini/spread-e-costo-di-transazione.md</c>.</para>
/// </summary>
public enum SpreadResolution
{
    /// <summary>
    /// Una costante per simbolo, dal file <c>spread-by-symbol</c>. E' il default: e' la misura che
    /// esiste sempre, e resta quella giusta quando le strategie di un run operano tutte nelle
    /// stesse ore.
    /// </summary>
    PerSymbol = 0,

    /// <summary>
    /// Il valore dell'ora UTC dell'ingresso, dal file <c>spread-by-hour</c> della <b>stessa</b>
    /// misura. Le ore che il broker non ha quotato — mercato chiuso, celle vuote nel file —
    /// ripiegano sulla costante per simbolo e lo dichiarano fra gli avvisi: zero sarebbe uno spread
    /// misurato e nullo, e un ingresso li' gratis non e' un dato ma un buco.
    /// </summary>
    PerHour = 1
}
