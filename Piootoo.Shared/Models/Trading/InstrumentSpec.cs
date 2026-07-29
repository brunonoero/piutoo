namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Specifica di uno strumento: quanto vale, in denaro, un punto di prezzo per una unità di
/// quantità.
///
/// <para><b>Perché esiste.</b> È l'unico posto in cui il sistema sa tradurre denaro in punti.
/// Prima questa conoscenza era duplicata: una tabella hardcoded dentro
/// <c>PiootooTradingService</c> per stop loss e take profit, e un <c>DollarsPerPoint</c> di
/// sessione per il position sizing. Le due potevano dissentire senza che nulla lo segnalasse.</para>
///
/// <para><b>Contratto di riferimento vs contratto di esecuzione.</b> Le strategie portate da
/// EasyLanguage dichiarano il rischio in denaro sul contratto su cui l'originale girava (il
/// <i>riferimento</i>): <c>setstopcontract; setstoploss(2000)</c> significa $2.000 su un
/// contratto GC da 100 once. Quel valore va convertito in <b>punti</b> usando la spec del
/// contratto di riferimento, una volta sola. I punti sono la grandezza invariante: 20 punti
/// restano 20 punti su future, mini, micro e CFD. Solo la <i>quantità</i> viene poi riscalata
/// dal moltiplicatore dell'account. È questo che rende una strategia idempotente rispetto al
/// valore del contratto e del lotto.</para>
/// </summary>
public sealed record InstrumentSpec
{
    /// <summary>Simbolo canonico, senza '@' e in maiuscolo (es. <c>GC</c>).</summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Denaro per un punto di prezzo, per una unità di quantità, nella valuta
    /// <see cref="Currency"/>. Per GC (100 once, prezzo in $/oncia) vale 100.
    /// </summary>
    public required decimal PointValue { get; init; }

    /// <summary>Valuta in cui è espresso <see cref="PointValue"/>.</summary>
    public required string Currency { get; init; }

    /// <summary>Incremento minimo di prezzo. Serve ad arrotondare i livelli, non al P&amp;L.</summary>
    public decimal TickSize { get; init; } = 0.01m;

    /// <summary>Descrizione leggibile, usata nei messaggi di errore.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Converte un importo in denaro per contratto nella distanza equivalente in punti.
    /// È l'unica direzione di conversione ammessa: dal denaro dichiarato dalla strategia ai
    /// punti applicabili a qualunque strumento.
    /// </summary>
    public decimal MoneyToPoints(decimal moneyPerContract) => moneyPerContract / PointValue;

    /// <summary>Converte una distanza in punti nel denaro corrispondente per contratto.</summary>
    public decimal PointsToMoney(decimal points) => points * PointValue;
}
