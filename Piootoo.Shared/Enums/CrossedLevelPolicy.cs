namespace Piootoo.Shared.Enums;

/// <summary>
/// Cosa fare di un ordine Stop o Limit il cui livello e' gia' superato quando l'ordine nasce: uno
/// stop buy sotto l'Ask, uno stop sell sopra il Bid, un limit buy sopra l'Ask, un limit sell sotto
/// il Bid. Lo dichiara la strategia sul segnale; motore interno e cBot lo eseguono allo stesso modo.
///
/// <para><b>Perche' esiste.</b> Il livello nasce da barre gia' chiuse (il massimo di ieri, un
/// canale) e il segnale alla chiusura di una barra, ma il prezzo puo' averlo superato prima: su
/// <c>PT5DAV_GC_TFU_001_15</c> il 03/09/2026 l'oro rompe il massimo di ieri alle 01:00 UTC e la
/// strategia emette lo stop buy alle 08:00, 31 punti sotto il prezzo. Il broker quell'ordine non
/// lo accetta. La ricerca Python lo riempie all'apertura della barra dopo, ed e' cosi' che la serie
/// PT5DAV e' stata misurata per tredici anni: su 81 trade di quella strategia sull'anno broker, 71
/// sono di questo tipo.</para>
/// </summary>
public enum CrossedLevelPolicy
{
    /// <summary>
    /// L'ordine non nasce. E' la regola di sempre (<c>RejectWrongSideLevels</c>), con cui sono state
    /// validate le serie portate da TradeStation: li' il breakout gia' avvenuto non e' il trade che la
    /// strategia aspettava.
    /// </summary>
    Reject = 0,

    /// <summary>
    /// L'ordine diventa un ingresso a mercato all'apertura della barra su cui e' valido, con gli
    /// stessi stop, target e uscite: e' la semantica del simulatore della ricerca PT5DAV.
    /// </summary>
    Market = 1
}
