using Piootoo.Shared.Models.Trading;

namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Richiesta di avvio backtesting.
/// StartDate e EndDate devono essere espressi in UTC (allineati al feed JSON).
/// </summary>
public class BacktestingRequest
{
    /// <summary>Workspace obbligatorio che determina il masterfilter e gli output.</summary>
    public string WorkspaceId { get; set; } = string.Empty;
    /// <summary>Nome normalizzato della sottocartella in workspace/backtests.</summary>
    public string BacktestFolderName { get; set; } = string.Empty;
    /// <summary>Consente di sostituire una cartella esistente solo dopo conferma esplicita.</summary>
    public bool OverwriteExistingBacktest { get; set; }
    public List<string> SelectedSymbols { get; set; } = new();
    public List<string> SelectedStrategyIds { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal CommissionPerContract { get; set; } = 2.0m;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Da quale archivio di barre legge il run. <b>Null o vuoto = datafeed interno</b>
    /// (<c>piootoo-repository/datafeed</c>); altrimenti il nome della cartella broker sotto
    /// <c>piootoo-repository/datafeed-external</c>, es. <c>RAWTRADINGLTD</c>.
    ///
    /// <para>Un run legge da <b>una sola</b> radice: le due strutture hanno lo stesso formato ma non
    /// gli stessi prezzi — l'interno viene dai CSV del vendor, l'esterno dalle barre che il broker
    /// ha davvero chiuso — e un backtest a cavallo delle due non corrisponderebbe a nessun conto.
    /// Per lo stesso motivo il valore finisce in <c>backtest-summary.json</c>: due run su feed
    /// diversi non sono confrontabili, e mesi dopo non c'e' altro modo di accorgersene.</para>
    ///
    /// <para>Un broker inesistente fa fallire l'avvio: vale la stessa regola del datafeed mancante,
    /// mai proseguire in silenzio.</para>
    /// </summary>
    public string? DatafeedBroker { get; set; }
    /// <summary>
    /// Cosa il conto simulato permette di tenere — la notte, il fine settimana — e a che ora taglia
    /// quando non lo permette.
    ///
    /// <para>E' <b>lo stesso tipo</b> che il piano porta in sessione e nel descriptor, e non un
    /// interruttore parallelo: un run e il live dello stesso piano sono confrontabili per
    /// costruzione, invece che per disciplina di chi compila la richiesta. Il precedente
    /// <c>CloseAllPositionsAtWeekEnd</c> e' oggi <see cref="AccountHoldingPolicy.AllowOverweek"/>
    /// rovesciato. Vedi <see cref="AccountHoldingPolicy"/>.</para>
    /// </summary>
    public AccountHoldingPolicy Holding { get; set; } = AccountHoldingPolicy.Default;

    /// <summary>
    /// Scarta i pending il cui livello e' gia' oltrepassato quando l'ordine nasce, come fa il cBot
    /// con <c>RejectWrongSideLevels</c>. Il perche' sta su
    /// <c>PiootooTradingService.RejectWrongSideLevels</c>; spegnerlo serve solo a misurare la
    /// fedelta' del porting rispetto al motore di ricerca, che quei livelli li riempie
    /// all'apertura.
    /// </summary>
    public bool RejectWrongSideLevels { get; set; } = true;

    /// <summary>
    /// Slippage in punti sul riempimento degli stop protettivi, per simbolo. Null o vuoto =
    /// nessuno slippage, che e' il comportamento storico del motore.
    ///
    /// <para>Vedi <c>PiootooTradingService.StopFillSlippagePoints</c> per la misura da cui
    /// escono i valori e per il motivo per cui sono un parametro del run e non una costante:
    /// dipendono dal broker e dal periodo.</para>
    /// </summary>
    public Dictionary<string, decimal>? StopFillSlippagePoints { get; set; }

    /// <summary>
    /// Quanto deve migliorare il picco favorevole prima che il trailing lo segua, in frazione
    /// della distanza di trailing. Stesso numero e stesso significato del parametro omonimo del
    /// cBot; il perche' sta su <c>PiootooTradingService.TrailingMinStepFraction</c>.
    ///
    /// <para>A <c>0</c> il trailing torna a inseguire ogni miglioramento, cioe' al comportamento
    /// pre-3.11.0. Come <see cref="RejectWrongSideLevels"/>, serve a <i>misurare</i> quanto vale
    /// la convenzione a parita' di ingressi — due run dello stesso periodo, un solo numero
    /// diverso — non a spegnerla in produzione. Il valore usato finisce nel log di avvio del job:
    /// senza, due cartelle di backtest con trailing diverso sono indistinguibili.</para>
    /// </summary>
    public decimal TrailingMinStepFraction { get; set; } = 0.10m;

    /// <summary>
    /// Conto di cui applicare l'<b>universo operativo</b>: le strategie su simboli che la sua tabella
    /// di conversione non prevede non vengono eseguite. Null o vuoto = nessun conto, il run gira
    /// sull'intero masterfilter.
    ///
    /// <para><b>Solo l'universo, non la size.</b> Il backtest interno resta neutro rispetto ai
    /// conti: capitale, <c>BalanceScale</c> e moltiplicatori di contratto non entrano da qui e
    /// restano fissi a 1. Il motivo e' quello di <c>docs/decisioni.md</c> (2026-08-05) e non e'
    /// cambiato: una size legata al conto farebbe dipendere il campione dal capitale invece che
    /// dalle strategie. Quello che cambia e' <i>quali</i> strategie girano, che e' una domanda
    /// diversa da <i>con che size</i>.</para>
    ///
    /// <para>Il conto non arriva dal piano di proposito: un piano puo' contenere piu' account, e
    /// l'universo operativo di un run e' quello di <b>un</b> conto — sceglierne uno per conto del
    /// piano significherebbe indovinare.</para>
    ///
    /// <para>Come <see cref="DatafeedBroker"/>, il valore finisce in <c>backtest-summary.json</c>:
    /// due run con universi diversi non sono confrontabili, e mesi dopo non c'e' altro modo di
    /// accorgersene. Un conto inesistente fa fallire l'avvio, non ripiega sul masterfilter intero.</para>
    /// </summary>
    public string? AccountNumber { get; set; }
}
