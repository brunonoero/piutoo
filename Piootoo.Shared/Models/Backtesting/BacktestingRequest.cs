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
    /// Spread denaro/lettera in punti dello strumento, per simbolo. Null o vuoto = nessuno spread,
    /// che e' il comportamento storico del motore.
    ///
    /// <para>Peggiora il solo <b>prezzo di ingresso</b> — long a <c>fill + spread</c>, short a
    /// <c>fill - spread</c> — e lascia dove sono trigger, livelli e uscite. Il perche', e perche' il
    /// trigger dei pending resta sul feed, stanno su <c>PiootooTradingService.SpreadPoints</c>.</para>
    ///
    /// <para>Come <see cref="StopFillSlippagePoints"/> e' una misura del broker e del periodo, non
    /// una costante: la produce <c>PiootooSpreadDumpBot</c> e va ricalibrata quando cambia l'uno o
    /// l'altro. Un valore negativo fa fallire l'avvio: uno spread negativo non e' un modello
    /// ottimista, e' un ingresso migliore del mercato.</para>
    ///
    /// <para>I simboli e i valori applicati finiscono nel log di avvio del job e in
    /// <c>backtest-summary.json</c>: due run con spread diverso non sono confrontabili, e mesi dopo
    /// non c'e' altro modo di accorgersene.</para>
    /// </summary>
    public Dictionary<string, decimal>? SpreadPoints { get; set; }

    /// <summary>
    /// Broker di cui caricare la misura di spread, da
    /// <c>piootoo-repository/spread/{BROKER}/*spread-by-symbol*.csv</c>. Null o vuoto = nessuna
    /// tabella: valgono i soli <see cref="SpreadPoints"/> scritti a mano, e senza nemmeno quelli il
    /// run gira senza spread come ha sempre fatto.
    ///
    /// <para>Il file lo produce <c>PiootooSpreadDumpBot</c> sui tick del conto vero. Si carica da
    /// li' e non si ricopiano i numeri nella richiesta perche' sono venti valori che nessuno
    /// trascrive due volte allo stesso modo, e perche' il file porta con se' quando e su che conto
    /// e' stato misurato — che e' cio' che rende un run rifacibile mesi dopo.</para>
    ///
    /// <para><b>Scelta indipendente da <see cref="DatafeedBroker"/> e da <see cref="PlanCode"/>.</b>
    /// Girare sul feed interno con lo spread di un broker vero e' esattamente il confronto che dice
    /// quanto costa quel broker; legare le due scelte lo renderebbe impossibile. Vale la stessa
    /// regola del datafeed: un broker senza misura fa fallire l'avvio, non ripiega su "nessuno
    /// spread".</para>
    ///
    /// <para>I valori della tabella sono un default per simbolo: un simbolo presente anche in
    /// <see cref="SpreadPoints"/> prende il valore scritto a mano, cosi' si puo' correggere un
    /// singolo strumento senza rifare la misura.</para>
    /// </summary>
    public string? SpreadBroker { get; set; }

    /// <summary>
    /// Quale numero della distribuzione misurata diventa lo spread del run. Conta solo quando
    /// <see cref="SpreadBroker"/> e' valorizzato: gli <see cref="SpreadPoints"/> scritti a mano sono
    /// gia' un numero scelto. Vedi <see cref="Piootoo.Shared.Models.Backtesting.SpreadStatistic"/>.
    /// </summary>
    public SpreadStatistic SpreadStatistic { get; set; } = SpreadStatistic.Median;

    /// <summary>
    /// Se lo spread e' una costante per simbolo o il valore dell'ora UTC dell'ingresso. Conta solo
    /// quando <see cref="SpreadBroker"/> e' valorizzato, e legge il file <c>spread-by-hour</c>
    /// della <b>stessa</b> misura — stesso broker, stessa finestra — non il piu' recente per conto
    /// proprio: due file di mesi diversi darebbero una costante di riferimento e delle ore che non
    /// vengono dagli stessi tick.
    ///
    /// <para>Sta nella richiesta e nel summary perche' due run con risoluzioni diverse non sono
    /// confrontabili, e gli spread applicati sono numeri: un numero non dice se viene dalla riga
    /// del simbolo o da quella delle 14 UTC. Vedi
    /// <see cref="Piootoo.Shared.Models.Backtesting.SpreadResolution"/>.</para>
    /// </summary>
    public SpreadResolution SpreadResolution { get; set; } = SpreadResolution.PerSymbol;

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
    /// Piano di cui il run riproduce le regole. Null o vuoto = nessun piano: il run gira
    /// sull'intero masterfilter con i parametri che questa richiesta porta, ed e' il run neutro di
    /// sempre.
    ///
    /// <para><b>Quando c'e' un piano, decide lui.</b> Il server ne prende l'universo operativo (i
    /// simboli che la tabella di conversione del suo broker prevede: le strategie sugli altri non
    /// girano affatto), le strategie che il piano tiene spente
    /// (<c>TradingPlan.DisabledStrategies</c>), la policy di tenuta (<see cref="Holding"/>) e la
    /// commissione per contratto. Quei tre campi della richiesta vengono <b>sovrascritti</b>, non
    /// composti: e' l'unico modo perche' un run e il live dello stesso piano siano confrontabili
    /// per costruzione invece che per disciplina di chi compila la richiesta. Il log di avvio del
    /// job e <c>backtest-summary.json</c> dichiarano i valori davvero applicati.</para>
    ///
    /// <para><b>Il piano e non un conto.</b> La tabella dei simboli e' una proprieta' del
    /// <i>broker</i> (<c>TradingBroker.SymbolConversionCode</c>) e tutti i conti di un piano sono
    /// di quel broker: l'universo e' lo stesso per tutti, quindi nominare un conto per ottenerlo
    /// significava scegliere a caso fra conti che davano la stessa risposta. Fino al 05/09/2026 qui
    /// c'era <c>AccountNumber</c>.</para>
    ///
    /// <para><b>Solo l'universo, non la size.</b> Il backtest interno resta neutro rispetto ai
    /// conti: capitale, <c>BalanceScale</c>, moltiplicatori di contratto e il
    /// <c>SizeMultiplier</c> del piano non entrano da qui e restano fissi a 1. Il motivo e' quello
    /// di <c>docs/decisioni.md</c> (2026-08-05) e non e' cambiato: una size legata al conto
    /// farebbe dipendere il campione dal capitale invece che dalle strategie. Quello che cambia e'
    /// <i>quali</i> strategie girano, che e' una domanda diversa da <i>con che size</i>.</para>
    ///
    /// <para><b>Il datafeed resta una scelta a parte</b> (<see cref="DatafeedBroker"/>): il broker
    /// del piano dice con che tabella si opera, non da quale archivio di barre si legge. Misurare
    /// lo stesso piano sul feed interno e su quello del suo broker e' esattamente il confronto che
    /// dice quanto vale lo spread, e legare le due scelte lo renderebbe impossibile.</para>
    ///
    /// <para>Come <see cref="DatafeedBroker"/>, il valore finisce in <c>backtest-summary.json</c>:
    /// due run con universi diversi non sono confrontabili, e mesi dopo non c'e' altro modo di
    /// accorgersene. Un piano inesistente fa fallire l'avvio, non ripiega sul masterfilter intero.</para>
    /// </summary>
    public string? PlanCode { get; set; }
}
