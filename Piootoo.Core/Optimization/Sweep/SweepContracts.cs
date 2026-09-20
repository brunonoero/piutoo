using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// Una combinazione da misurare: la strategia, i suoi parametri e le condizioni del conto.
///
/// <para>I parametri sono quelli che la classe espone dal proprio <c>Initialize</c> — le stesse
/// chiavi dei report della ricerca (<c>Direction</c>, <c>ChannelBars</c>, <c>StartHour</c>…). La
/// sweep non conosce i campi del motore e non deve conoscerli: cambia solo ciò che la classe ha
/// dichiarato di saper ricevere.</para>
/// </summary>
/// <param name="StrategyId">Id di catalogo, cioe' il nome della classe.</param>
/// <param name="Parameters">Parametri per <c>Initialize</c>. Vuoto = la strategia come e' dichiarata.</param>
public sealed record SweepJob(
    string StrategyId,
    IReadOnlyDictionary<string, object>? Parameters = null)
{
    /// <summary>Capitale iniziale del conto. Non entra nel sizing: il backtest resta a un contratto.</summary>
    public decimal InitialCapital { get; init; } = 1_000_000m;

    /// <summary>Commissione per contratto e per lato.</summary>
    public decimal CommissionPerContract { get; init; } = 4m;

    /// <summary>Tenuta del conto. Il default e' quello storico: overnight libero, fine settimana piatto.</summary>
    public AccountHoldingPolicy Holding { get; init; } = AccountHoldingPolicy.Default;

    /// <summary>
    /// Spread in punti per simbolo, come <c>BacktestingRequest.SpreadPoints</c>: peggiora il solo
    /// prezzo di ingresso e non tocca trigger, livelli ne' uscite.
    /// </summary>
    public IReadOnlyDictionary<string, decimal>? SpreadPoints { get; init; }

    /// <summary>
    /// Spread per <b>ora UTC</b> dell'ingresso, ventiquattro valori per simbolo. Quando c'e', vince
    /// sulla costante di <see cref="SpreadPoints"/>; le ore che il broker non ha quotato ripiegano
    /// su quella.
    ///
    /// <para><b>Perche' una ricerca ne ha bisogno piu' di un backtest.</b> Un backtest misura una
    /// strategia i cui orari sono gia' decisi; una sweep <i>sceglie</i> gli orari, e sceglie anche
    /// in base al costo. Con una costante giornaliera le fasce a spread largo sembrano economiche
    /// quanto le altre, e la ricerca ci si infila. Misurato su FDAX in agosto 2026: 1,13-1,33 punti
    /// fra le 07 e le 19 UTC, 2,93-3,33 fra le 22 e le 05 — due volte e mezzo, sulla stessa
    /// giornata. Una finestra notturna valutata alla mediana giornaliera paga meno di un terzo del
    /// costo vero.</para>
    /// </summary>
    public IReadOnlyDictionary<string, decimal[]>? SpreadPointsByHour { get; init; }

    /// <summary>
    /// Scarta gli ordini il cui livello e' gia' stato scavalcato, come fa il cBot. Acceso e' cio'
    /// che fa il conto vero; spento e' la parita' con il motore di ricerca, che quegli ordini li
    /// esegue. <b>Il default e' spento</b> perche' una sweep confronta configurazioni fra loro e il
    /// riferimento contro cui la si tara e' la ricerca — ma resta una scelta che cambia i numeri, e
    /// due risultati che non concordano su questo non sono confrontabili.
    /// </summary>
    public bool RejectWrongSideLevels { get; init; }

    /// <summary>
    /// Timeframe dell'orologio del run. <c>null</c> = quello della strategia, che e' il percorso
    /// veloce della ricerca. Un valore piu' fitto (tipicamente 1) e' il percorso di verifica: le
    /// barre di quel timeframe devono essere caricate in <see cref="SweepSeries"/>.
    ///
    /// <para><b>I due percorsi non danno lo stesso numero</b>, ed e' voluto: con l'orologio al
    /// minuto stop e target vengono valutati dentro la barra della strategia invece che sulla sua
    /// chiusura. Il percorso veloce serve a <i>ordinare</i> le configurazioni, quello fitto a
    /// misurare le finaliste. Confrontare un risultato dell'uno con uno dell'altro non ha senso.</para>
    /// </summary>
    public int? ClockTimeframeMinutes { get; init; }
}

/// <summary>Il risultato di una combinazione. Metriche sui trade <b>chiusi</b>.</summary>
public sealed record SweepOutcome
{
    /// <summary>La combinazione misurata.</summary>
    public required SweepJob Job { get; init; }

    /// <summary>Nome di esecuzione della strategia (<c>StrategyCode</c>), che non e' l'Id.</summary>
    public required string StrategyCode { get; init; }

    public int Trades { get; init; }
    public int Winners { get; init; }
    public int Losers { get; init; }

    /// <summary>Utile netto dei trade chiusi, commissioni comprese.</summary>
    public decimal NetProfit { get; init; }

    /// <summary>
    /// Drawdown massimo dell'equity dei trade <b>chiusi</b>, in denaro. Non e' il drawdown del
    /// backtest ufficiale, che e' mark-to-market e quindi piu' profondo: qui l'equity si muove solo
    /// quando un trade si chiude. Va usato per confrontare configurazioni fra loro, non per
    /// dichiarare il rischio di una strategia.
    /// </summary>
    public decimal MaxClosedTradeDrawdown { get; init; }

    /// <summary>Somma dei profitti diviso somma delle perdite. <c>null</c> se non ci sono perdite.</summary>
    public decimal? ProfitFactor { get; init; }

    /// <summary>Utile medio per trade.</summary>
    public decimal AverageTrade => Trades > 0 ? NetProfit / Trades : 0m;

    /// <summary>Quota di trade in utile.</summary>
    public decimal WinRate => Trades > 0 ? (decimal)Winners / Trades : 0m;

    /// <summary>Posizioni ancora aperte alla fine: il loro P&amp;L non e' in <see cref="NetProfit"/>.</summary>
    public int OpenAtEnd { get; init; }

    /// <summary>Quante volte la strategia e' stata valutata. Zero = la sweep sta misurando il nulla.</summary>
    public int Evaluations { get; init; }

    /// <summary>Quanti intent di ingresso ha emesso, companion compresi.</summary>
    public int Signals { get; init; }

    /// <summary>Durata della misura.</summary>
    public TimeSpan Elapsed { get; init; }

    /// <summary>
    /// I trade chiusi, in ordine di uscita. Servono al secondo livello della ricerca: il vincolo del
    /// paniere si misura sulle <b>entrate</b> — due strategie che mandano gli stessi ordini sono
    /// copy trading — e senza la lista non si puo' calcolare quanta parte ne condividono.
    /// </summary>
    public IReadOnlyList<Piootoo.Shared.Models.TradingResult> ClosedTrades { get; init; } = [];

    public override string ToString() =>
        $"{StrategyCode}: {Trades} trade, netto {NetProfit:N0}, DD {MaxClosedTradeDrawdown:N0}, " +
        $"PF {(ProfitFactor.HasValue ? ProfitFactor.Value.ToString("N2") : "-")}, {Elapsed.TotalMilliseconds:N0} ms";
}
