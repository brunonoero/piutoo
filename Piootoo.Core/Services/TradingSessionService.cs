using System.Collections.Concurrent;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Piootoo.Shared.Utilities;

namespace Piootoo.Core.Services;

public interface IStrategyEvaluationService
{
    IReadOnlyList<TradeSignal> Evaluate(
        IReadOnlyList<ITradingStrategy> strategies,
        ClosedBar closedBar,
        IReadOnlyList<OhlcvData> history,
        Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot);
}

public sealed class StrategyEvaluationService : IStrategyEvaluationService
{
    public IReadOnlyList<TradeSignal> Evaluate(
        IReadOnlyList<ITradingStrategy> strategies,
        ClosedBar closedBar,
        IReadOnlyList<OhlcvData> history,
        Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
    {
        var result = new List<TradeSignal>();
        foreach (var strategy in strategies.Where(s =>
                     Normalize(s.Symbol) == Normalize(closedBar.Symbol) &&
                     s.TimeframeMinutes == closedBar.TimeframeMinutes))
        {
            if (history.Count < strategy.RequiredCandles)
                continue;

            var signal = strategy.Evaluate(new StrategyEvaluationRequest
            {
                Ohlcv = history.ToArray(),
                BarTimeUtc = closedBar.BarTimeUtc,
                Execution = executionSnapshot(strategy)
            });
            if (signal?.RuntimeState is not null)
                signal.StrategyCode = string.IsNullOrWhiteSpace(signal.StrategyCode) ? strategy.Name : signal.StrategyCode;
            if (signal is null || signal.Type == SignalType.Hold)
                continue;

            Prepare(signal, strategy, closedBar);
            result.Add(signal);
            if (signal.CompanionSignals is null) continue;
            foreach (var companion in signal.CompanionSignals)
            {
                Prepare(companion, strategy, closedBar);
                result.Add(companion);
            }
        }
        return result;
    }

    private static void Prepare(TradeSignal signal, ITradingStrategy strategy, ClosedBar bar)
    {
        signal.Date = bar.BarTimeUtc;
        signal.Symbol = string.IsNullOrWhiteSpace(signal.Symbol) ? Normalize(bar.Symbol) : Normalize(signal.Symbol);
        signal.StrategyCode = string.IsNullOrWhiteSpace(signal.StrategyCode) ? strategy.Name : signal.StrategyCode;
        signal.StrategyName = string.IsNullOrWhiteSpace(signal.StrategyName) ? strategy.Name : signal.StrategyName;
    }

    private static string Normalize(string value) => value.Trim().TrimStart('@').ToUpperInvariant();
}

public interface ITradingSessionService
{
    TradingSessionDescriptor Create(CreateTradingSessionRequest request);
    TradingSessionDescriptor OpenFromPlan(OpenTradingPlanSessionRequest request);

    /// <summary>
    /// Elenco leggero di tutte le sessioni vive nel processo, incluse quelle aperte da un cBot: senza
    /// questo la console non ha modo di scoprirle, perché le tiene solo <c>_sessions</c> in RAM.
    /// </summary>
    IReadOnlyList<TradingSessionSummary> ListSessions();
    TradingSessionDescriptor SetStatus(string sessionId, string token, TradingSessionStatus status);
    PushBarsResponse PushBars(PushBarsRequest request);

    /// <summary>
    /// Variante di <see cref="PushBars"/> in cui il client invia, per ogni stream, l'intera finestra
    /// di candele che le strategie richiedono. Il server accoda quelle che non ha e valuta solo
    /// l'ultima: è così che una sessione nuova parte già "calda" invece di scartare in silenzio le
    /// prime <c>RequiredCandles</c> barre del run.
    /// </summary>
    PushBarWindowResponse PushBarWindow(PushBarWindowRequest request);
    IReadOnlyList<OrderIntent> GetIntents(string sessionId, string token, long after = 0);
    IReadOnlyList<PersistedSignal> GetPersistedSignals(string sessionId, string token);
    IReadOnlyList<PersistedTrade> GetPersistedTrades(string sessionId, string token);

    /// <summary>
    /// Log diagnostico di rotazione (una riga per barra) per sessioni collegate a un run Titano: per
    /// ciascuna strategia del masterfilter riporta se è stata inclusa nella valutazione, lo stato/motivo
    /// Titano corrente e i segnali effettivamente generati. Pensato per verificare che le strategie
    /// eseguano (o non eseguano) trade coerentemente con la rotazione, e per individuare bug.
    /// </summary>
    IReadOnlyList<RotationLogEntry> GetRotationLog(string sessionId, string token);
    TradingSessionSnapshot ApplyReport(string sessionId, ExecutionReportRequest request);

    TradingSessionSnapshot GetSnapshot(string sessionId, string token);

    /// <summary>
    /// Gli ultimi eventi della sessione dopo <paramref name="since"/>, per il monitor della
    /// console. Complementare a <see cref="GetSnapshot"/>: lo snapshot dice cos'è aperto adesso,
    /// questo dice cosa è successo e perché — in particolare quale filtro ha svuotato un claim,
    /// che non è uno stato e quindi nello snapshot non compare.
    /// </summary>
    SessionActivityResponse GetActivity(string sessionId, string token, long since = 0);

    /// <summary>
    /// Copia i trade (e i signal) di una sessione in una cartella di backtest del workspace, così
    /// che possano essere usati come campione sorgente da <c>TitanoRotationService</c>.
    /// </summary>
    PromoteSessionToBacktestResult PromoteToBacktest(string sessionId, PromoteSessionToBacktestRequest request);
    void CancelIntent(string sessionId, string token, string intentId);

    /// <summary>Configura (sostituendola interamente) la mappa account -> gruppo per l'anti copy-trading. Solo ExternalBroker.</summary>
    void SetAccountGroups(string sessionId, string token, IReadOnlyList<AccountGroupMapping> accounts);

    /// <summary>Legge la mappa account -> gruppo corrente.</summary>
    IReadOnlyList<AccountGroupMapping> GetAccountGroups(string sessionId, string token);

    /// <summary>Configura gruppi, account e profilo Titano per gruppo. Solo ExternalBroker.</summary>
    void SetTradingGroups(string sessionId, string token, IReadOnlyList<TradingGroupRow> rows);

    /// <summary>Legge la configurazione gruppi/account/Titano corrente.</summary>
    IReadOnlyList<TradingGroupRow> GetTradingGroups(string sessionId, string token);

    /// <summary>
    /// Chiamata dal cBot di un singolo account: restituisce il prossimo segnale da eseguire (chiusura di
    /// una posizione già assegnata, oppure un nuovo ingresso libero nel gruppo, in ordine di priorità),
    /// oppure nessun segnale se l'account è già occupato o non c'è nulla di disponibile.
    /// </summary>
    AccountSignalResponse GetNextSignalForAccount(string sessionId, string token, string accountNumber);
    AccountSignalResponse PollSignalForAccount(
        string sessionId, string accountNumber, AccountSignalPollRequest request);

    /// <summary>
    /// Registra un intent di chiusura (<see cref="OrderIntentKind.Close"/>) per una posizione che un
    /// client ExternalBroker ha già chiuso applicando la specifica di uscita ricevuta con l'intent di
    /// ingresso (SL/TP nativi, CloseAtUtc, MaxBarsInPosition). Richiede che la posizione
    /// StrategyCode/Symbol (eventualmente per account, se sono configurati gruppi) risulti aperta lato
    /// sessione. Il client referenzia l'IntentId restituito nel normale ApplyReport.
    /// </summary>
    OrderIntent CreateExternalCloseIntent(string sessionId, CreateExternalCloseIntentRequest request);
}

public sealed class TradingSessionService : ITradingSessionService
{
    private sealed class Session
    {
        public required string Id { get; init; }
        public required string Token { get; init; }
        public required string WorkspaceId { get; init; }
        public string? PlanCode { get; init; }
        public string? ExecutionKey { get; init; }
        public required ExecutionMode Mode { get; init; }
        public required decimal InitialCapital { get; init; }
        public required List<ITradingStrategy> Strategies { get; init; }
        public required PiootooTradingService SimulatedEngine { get; init; }
        public required TradingJsonStore Store { get; init; }

        /// <summary>
        /// Run esplicito, valorizzato solo dal percorso non-piano (<see cref="CreateTradingSessionRequest.TitanoRunId"/>,
        /// usato da test e sessioni create a mano). Le sessioni aperte da piano lo lasciano null: il
        /// run effettivo si risolve sempre come "l'ultimo per questa cartella" al momento di ogni barra.
        /// </summary>
        public string? PinnedTitanoRunId { get; init; }
        public string? TitanoBacktestFolder { get; init; }
        public TitanoFilterMode TitanoMode { get; init; }
        public ClientRunMode ClientRunMode { get; init; }

        /// <summary>
        /// Applica MaxConcurrentTrades nella distribuzione multi-account. Indipendente da
        /// <see cref="TitanoMode"/>: vedi <c>docs/domini/distribuzione-multi-account.md</c> §4.
        /// </summary>
        public bool EnforceConcurrencyLimits { get; init; }

        /// <summary>
        /// Profilo dichiarato dal cBot all'apertura. Non governa nulla a runtime — al momento
        /// dell'apertura si è già risolto in <see cref="TitanoMode"/> e
        /// <see cref="EnforceConcurrencyLimits"/> — ma va conservato per poterlo mostrare a chart e
        /// nei log: sapere *come* è configurato un run è meno utile che sapere *quale* run è.
        /// </summary>
        public TradingRunProfile RunProfile { get; set; }

        public required PositionSizingConfig PositionSizing { get; init; }
        public required Dictionary<string, InstrumentMetadata> InstrumentMetadata { get; init; }

        /// <summary>
        /// Account che esegue direttamente gli intent di <c>POST /bars</c>, cioè le sessioni aperte
        /// con <c>DistributeToAccounts=false</c>. Qui non c'è claim, quindi la conversione
        /// dell'account va applicata al momento in cui l'intent nasce. Null nelle sessioni
        /// distribuite, dove il conto si conosce solo al claim.
        /// </summary>
        public string? DirectAccountNumber { get; set; }
        public decimal PeakEquity { get; set; }
        public TradingSessionStatus Status { get; set; }
        public required DateTime CreatedAtUtc { get; init; }
        public object Gate { get; } = new();
        public Dictionary<string, List<OhlcvData>> History { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, long> LastSequence { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> BarKeys { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ReportIds { get; } = new(StringComparer.Ordinal);
        public List<OrderIntent> Intents { get; } = [];
        public List<RotationLogEntry> RotationLog { get; } = [];

        /// <summary>
        /// Buffer circolare degli ultimi eventi della sessione, per il monitor della console.
        /// Vedi <see cref="SessionActivityEntry"/> per il perche' non basti lo snapshot.
        ///
        /// <para>E' una lista e non una coda perche' il client legge per progressivo e non per
        /// posizione: serve poter rispondere "dammi tutto dopo il 412" con una ricerca binaria,
        /// non consumare la coda.</para>
        /// </summary>
        public List<SessionActivityEntry> Activity { get; } = [];

        /// <summary>Progressivo dell'ultimo evento registrato. Cresce e non torna mai indietro.</summary>
        public long ActivitySequence { get; set; }

        /// <summary>
        /// Ultimo motivo di claim negato registrato, per account. Un claim negato si ripete a ogni
        /// poll — ogni due secondi in live, ogni barra in backtest — e senza deduplica riempirebbe
        /// il buffer di righe identiche, buttando fuori proprio gli eventi rari che si stanno
        /// cercando. Si registra il cambio di motivo, non la ripetizione.
        /// </summary>
        public Dictionary<string, string> LastRefusalByAccount { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, TradingPositionSnapshot> ExternalPositions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, (DateTime EntryTimeUtc, string IntentId, decimal? StopLoss, decimal? TakeProfit)>
            ExternalPositionDetails { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<PersistedTrade> ExternalTrades { get; } = [];
        public int Entries { get; set; }
        public int Fills { get; set; }
        public DateTime? LastEvaluatedBarTimeUtc { get; set; }
        public int IntentSequence { get; set; }

        // --- Distribuzione multi-account / anti copy-trading (solo ExecutionMode.ExternalBroker) ---

        /// <summary>Mappa AccountNumber -> GroupId configurata dal tab Trading Session.</summary>
        public Dictionary<string, string> AccountGroups { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> AccountMaxConcurrentTrades { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Cosa conta <c>MaxConcurrentTrades</c> per ogni account: parametro del piano, non
        /// convenzione del server. Assente = <c>PositionsAndPendingOrders</c>, il default storico.
        /// </summary>
        public Dictionary<string, ConcurrencyCountMode> AccountConcurrencyCountMode { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Tabella di conversione per AccountNumber, risolta al primo poll dell'account: il
        /// registro account sta su disco e il poll è per barra e per conto.
        /// </summary>
        public Dictionary<string, AccountSymbolConversion> AccountConversions { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Profilo Titano per GroupId (RotationSetupId, run, flag apply).</summary>
        public Dictionary<string, GroupTitanoProfile> GroupProfiles { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Template di segnali di apertura non ancora reclamati: ogni gruppo può reclamarne una copia indipendente.</summary>
        public List<OrderIntent> EntryTemplates { get; } = [];

        /// <summary>Per ogni template (IntentId), l'insieme dei gruppi che ne hanno già ricevuto una copia.</summary>
        public Dictionary<string, HashSet<string>> TemplateClaimedGroups { get; } = new(StringComparer.Ordinal);

        /// <summary>Slot occupato per (gruppo, strategia, simbolo): quale account lo detiene e con quale IntentId.</summary>
        public Dictionary<string, (string AccountNumber, string IntentId)> GroupStrategySlots { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        // Non esiste più un lucchetto (account, simbolo). Il tetto di concorrenza è per account e
        // trasversale ai simboli — vedi AccountMaxConcurrentTrades e CountInFlightForAccount —
        // mentre l'unicità (strategia, simbolo) è garantita da AccountHasEntryInFlight, che è una
        // guardia di identità e non un vincolo di concorrenza.

        /// <summary>Posizione "canonica" (Symbol|StrategyCode) usata per alimentare la valutazione strategie in modalità multi-account,
        /// indipendente da quale account specifico la detiene realmente.</summary>
        public Dictionary<string, TradingPositionSnapshot> CanonicalPositions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> StrategyHolderCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class GroupTitanoProfile
    {
        public string? RotationSetupId { get; init; }
        public string? TitanoBacktestFolder { get; init; }
        public bool ApplyTitanoFilters { get; init; } = true;
    }

    private readonly record struct ResolvedGroupTitano(
        string? RotationSetupId,
        string? TitanoBacktestFolder,
        bool ApplyTitanoFilters);

    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _planExecutions =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly WorkspaceService _workspaces;
    private readonly TradingPlanService _plans;
    private readonly IStrategyEvaluationService _evaluation;
    private readonly TitanoRotationService? _titano;
    private readonly IPositionSizingService _positionSizing;

    public TradingSessionService(
        WorkspaceService workspaces, TradingPlanService plans, IStrategyEvaluationService evaluation,
        TitanoRotationService? titano = null, IPositionSizingService? positionSizing = null)
    {
        _workspaces = workspaces;
        _plans = plans;
        _evaluation = evaluation;
        _titano = titano;
        _positionSizing = positionSizing ?? new PositionSizingService();
    }

    public TradingSessionService(
        WorkspaceService workspaces, IStrategyEvaluationService evaluation,
        TitanoRotationService? titano = null, IPositionSizingService? positionSizing = null)
        : this(workspaces, new TradingPlanService(workspaces), evaluation, titano, positionSizing)
    {
    }

    public TradingSessionDescriptor Create(CreateTradingSessionRequest request)
        => CreateCore(request, null, null);

    public IReadOnlyList<TradingSessionSummary> ListSessions()
        => _sessions.Values
            .OrderByDescending(session => session.CreatedAtUtc)
            .Select(session => new TradingSessionSummary
            {
                SessionId = session.Id,
                SessionToken = session.Token,
                WorkspaceId = session.WorkspaceId,
                PlanCode = session.PlanCode,
                ExecutionKey = session.ExecutionKey,
                ExecutionMode = session.Mode,
                Status = session.Status,
                ClientRunMode = session.ClientRunMode,
                TitanoMode = session.TitanoMode,
                CreatedAtUtc = session.CreatedAtUtc,
                LastBarTimeUtc = session.LastEvaluatedBarTimeUtc
            })
            .ToList();

    public TradingSessionDescriptor OpenFromPlan(OpenTradingPlanSessionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ClientRunMode == ClientRunMode.Unknown)
            throw new ArgumentException("Il cBot deve dichiarare Backtest oppure Realtime.");
        if (string.IsNullOrWhiteSpace(request.ExecutionKey))
            throw new ArgumentException("ExecutionKey è obbligatoria.");

        // Il profilo si valida prima di qualunque altra cosa: è quello che decide se il run
        // produrrà il campione sorgente o una simulazione filtrata, e aprire il run sbagliato non
        // dà errore, dà numeri plausibili che nessuno rimetterà più in discussione.
        var runProfile = request.RunProfile ?? TradingRunProfile.DalPiano;
        if (runProfile != TradingRunProfile.DalPiano && request.ClientRunMode != ClientRunMode.Backtest)
            throw new ArgumentException(
                $"Il profilo '{runProfile}' vale solo in backtest, ma il cBot dichiara " +
                $"{request.ClientRunMode}. In realtime usa '{TradingRunProfile.DalPiano}': " +
                "la configurazione operativa la porta il piano.");

        var plan = _plans.Resolve(request.PlanCode);
        if (plan.Groups.Count == 0)
            throw new InvalidOperationException($"Il piano '{plan.Code}' non contiene righe gruppo/account.");

        var account = string.IsNullOrWhiteSpace(request.AccountNumber)
            ? plan.AccountNumber
            : request.AccountNumber.Trim();
        var accountRow = plan.Groups.FirstOrDefault(row =>
                             row.AccountNumber.Equals(account, StringComparison.OrdinalIgnoreCase))
                         ?? throw new ArgumentException(
                             $"L'account '{account}' non appartiene al piano '{plan.Code}'.");

        // In distribuzione la sessione è condivisa fra gli account del piano, quindi la chiave non
        // include l'account. In esecuzione diretta gli intent sono già assegnati e li consuma un
        // solo cBot: due account sulla stessa sessione eseguirebbero gli stessi segnali due volte.
        //
        // Il profilo entra nella chiave, altrimenti lo stesso cBot rilanciato dopo aver cambiato
        // profilo riprenderebbe la sessione precedente e continuerebbe a girare con il Titano e i
        // lucchetti del run vecchio, senza dirlo. Si accoda solo quando non è DalPiano, così le
        // chiavi delle sessioni già in corso restano quelle di prima e la ripresa non si rompe.
        var profileSuffix = runProfile == TradingRunProfile.DalPiano ? string.Empty : $"|{runProfile}";
        var executionKey = request.DistributeToAccounts
            ? $"{plan.Code}|{request.ClientRunMode}|{request.ExecutionKey.Trim()}{profileSuffix}"
            : $"{plan.Code}|{request.ClientRunMode}|{request.ExecutionKey.Trim()}{profileSuffix}|Direct|{account}";
        if (_planExecutions.TryGetValue(executionKey, out var existingId) &&
            _sessions.TryGetValue(existingId, out var existing))
        {
            lock (existing.Gate)
            {
                existing.Status = TradingSessionStatus.Running;
                Persist(existing);
                // Anche su riconnessione il descriptor deve riportare il simbolo del broker di
                // QUESTO account: in distribuzione la sessione è condivisa e Describe(existing) da
                // solo non saprebbe quale conversione applicare (vedi sotto, stessa apertura).
                return Describe(existing, ResolveAccountConversion(existing, account), account);
            }
        }

        // Titano di sessione dalla riga primaria (prima con run, altrimenti la prima): i profili
        // delle altre righe restano applicati da SetTradingGroups e prevalgono nel claim. In
        // esecuzione diretta non esiste claim, e l'unica riga che descrive l'esecuzione è quella
        // dell'account che ha aperto la sessione.
        var primary = request.DistributeToAccounts
            ? TradingPlanService.SelectPrimaryRow(plan.Groups)
            : accountRow;
        // Il profilo, quando è dichiarato, PREVALE sul piano: è il cBot a sapere che run sta
        // aprendo, e il piano resta la fonte di tutto il resto (workspace, sizing, strumenti,
        // cartella del run Titano). Senza profilo si ricade sul piano, com'era prima.
        var titanoMode = runProfile switch
        {
            TradingRunProfile.BacktestSorgente => TitanoFilterMode.Disabled,
            // Filtro statico: le strategie restano quelle del masterfilter, esattamente come nel
            // sorgente. Fra i due cambiano solo i lucchetti, poco più sotto.
            TradingRunProfile.BacktestStaticFilter => TitanoFilterMode.Disabled,
            TradingRunProfile.BacktestTitano => TitanoFilterMode.BacktestRotationFile,
            _ => !primary.ApplyTitanoFilters
                ? TitanoFilterMode.Disabled
                : request.ClientRunMode == ClientRunMode.Backtest
                    ? TitanoFilterMode.BacktestRotationFile
                    : TitanoFilterMode.Realtime
        };

        // Un backtest Titano senza rotazioni non è un backtest Titano: girerebbe come un run senza
        // filtro e la differenza si vedrebbe solo confrontando due trades.json mesi dopo.
        if (runProfile == TradingRunProfile.BacktestTitano &&
            string.IsNullOrWhiteSpace(primary.TitanoBacktestFolder))
            throw new ArgumentException(
                $"Il profilo '{TradingRunProfile.BacktestTitano}' richiede le rotazioni storiche, ma la " +
                $"riga primaria del piano '{plan.Code}' non indica alcuna cartella di run Titano. " +
                $"Valorizza TitanoBacktestFolder, oppure apri il run con " +
                $"'{TradingRunProfile.BacktestStaticFilter}' (stessi lucchetti, strategie dal " +
                $"masterfilter) o '{TradingRunProfile.BacktestSorgente}' (nessun lucchetto).");

        // MaxConcurrentTrades è applicato solo da GetNextSignalForAccount, cioè dal percorso di
        // claim. Senza gruppi quel percorso non esiste e il limite non avrebbe alcun punto di
        // applicazione: eseguire lo stesso il piano significherebbe operare senza il limite che
        // dichiara, quindi si rifiuta l'apertura invece di ignorarlo in silenzio.
        //
        // I profili espliciti DICHIARANO i lucchetti e il piano non li contraddice: è il senso di
        // averli nominati invece di dedurli da una combinazione di flag. Il piano decide ancora
        // tutto il resto (workspace, sizing, strumenti, cartella Titano) e continua a decidere i
        // lucchetti quando il profilo è DalPiano.
        //
        // BacktestSorgente li spegne: il campione sorgente deve contenere ogni segnale che le
        // strategie hanno prodotto, e un piano con EnforceConcurrencyLimits=true non deve poterlo
        // mutilare di nascosto.
        //
        // BacktestStaticFilter e BacktestTitano li accendono, per la ragione simmetrica. Fino al
        // 15/08/2026 solo il sorgente era blindato e gli altri ricadevano sul piano: un
        // EnforceConcurrencyLimits=false rendeva BacktestTitano un run senza lucchetti che
        // continuava a chiamarsi Titano, e la differenza si vedeva solo confrontando due
        // trades.json — esattamente lo scenario che la riga sopra dice di voler evitare per il
        // sorgente. I due profili condividono i lucchetti proprio perché la sola differenza fra
        // loro sia il filtro, statico contro dinamico: altrimenti il confronto non isola niente.
        var enforceConcurrency = runProfile switch
        {
            TradingRunProfile.BacktestSorgente => false,
            TradingRunProfile.BacktestStaticFilter => true,
            TradingRunProfile.BacktestTitano => true,
            _ => plan.EnforceConcurrencyLimits
                 ?? DefaultEnforceConcurrencyLimits(request.ClientRunMode, titanoMode)
        };
        if (!request.DistributeToAccounts && enforceConcurrency && accountRow.MaxConcurrentTrades > 0)
            throw new ArgumentException(
                $"Il piano '{plan.Code}' dichiara MaxConcurrentTrades={accountRow.MaxConcurrentTrades} " +
                $"per l'account '{account}', ma in esecuzione diretta il limite non è applicabile: " +
                "è governato dalla distribuzione multi-account. Azzera MaxConcurrentTrades, " +
                "disattiva EnforceConcurrencyLimits, oppure usa un cBot che reclama i segnali " +
                "da GET /accounts/{n}/signals.");

        var descriptor = CreateCore(new CreateTradingSessionRequest
        {
            WorkspaceId = plan.WorkspaceId,
            ExecutionMode = ExecutionMode.ExternalBroker,
            // Nessun capitale: una sessione da piano è sempre ExternalBroker, dove il saldo è del
            // broker e la size di ogni account viene dal suo InitialBalance (BalanceScale) al claim.
            CommissionPerContract = plan.CommissionPerContract,
            ClientSessionToken = Convert.ToHexString(Guid.NewGuid().ToByteArray()),
            TitanoBacktestFolder = primary.TitanoBacktestFolder,
            TitanoMode = titanoMode,
            ClientRunMode = request.ClientRunMode,
            // Il valore già risolto, non quello del piano: qui il profilo ha eventualmente
            // prevalso, e ripassare il nullable farebbe ricalcolare a CreateCore il default,
            // perdendo l'override.
            EnforceConcurrencyLimits = enforceConcurrency,
            PositionSizing = plan.PositionSizing
        }, plan.Code, request.ExecutionKey.Trim());
        AccountSymbolConversion conversion;
        lock (_sessions[descriptor.SessionId].Gate)
        {
            var opened = _sessions[descriptor.SessionId];
            // Conservato per la diagnostica: a runtime il profilo si è già risolto in TitanoMode e
            // EnforceConcurrencyLimits, ma senza di lui il descriptor non saprebbe dire quale run è.
            opened.RunProfile = runProfile;
            if (request.DistributeToAccounts)
                // SetTradingGroups azzera session.AccountConversions: va chiamato PRIMA di risolvere
                // la conversione di questo account, altrimenti la cache verrebbe svuotata subito dopo.
                SetTradingGroups(descriptor.SessionId, descriptor.SessionToken, plan.Groups);
            else
                opened.DirectAccountNumber = account;
            // Risolta subito e non alla prima barra: un conto senza anagrafica deve far fallire
            // l'apertura, non ogni push a sessione avviata. Anche in distribuzione, perché il
            // descriptor restituito a QUESTO cBot deve riportare il nome simbolo del SUO broker,
            // anche se la sessione è condivisa fra più account con tabelle di conversione diverse
            // (prima di questo fix Describe usava sempre Identity fuori da esecuzione diretta,
            // quindi il bot leggeva/pushava barre col simbolo Piootoo invece di quello convertito).
            conversion = ResolveAccountConversion(opened, account);
        }
        SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);
        _planExecutions[executionKey] = descriptor.SessionId;
        return Describe(_sessions[descriptor.SessionId], conversion, account);
    }

    /// <summary>
    /// Decide dove la sessione persiste i propri artefatti.
    ///
    /// <para>Una sessione di <b>backtest</b> aperta da piano scrive direttamente sotto
    /// <c>&lt;workspace&gt;/backtests/</c>, cioè dove <c>TitanoRotationService</c> cerca il campione
    /// sorgente. Prima finiva in <c>sessions/&lt;guid&gt;/</c> e serviva una copia esplicita per
    /// renderla utilizzabile: due alberi per lo stesso artefatto, con un passaggio manuale in mezzo
    /// che era facile dimenticare — e dimenticarlo non dava errore, dava una rotazione calcolata su
    /// un campione vecchio.</para>
    ///
    /// <para>Le sessioni <b>realtime</b> restano sotto <c>sessions/</c>: non sono campioni e non
    /// vanno confuse con i backtest. Prendono però un nome parlante al posto del GUID, perché una
    /// cartella che non dice a quale piano appartenga non è ispezionabile né ripulibile.</para>
    /// </summary>
    private string ResolveSessionDirectory(
        CreateTradingSessionRequest request, string? planCode, string? executionKey, string sessionId)
    {
        var workspacePath = _workspaces.GetWorkspacePath(request.WorkspaceId);

        // Senza piano non c'è un nome stabile da usare: resta il GUID, che almeno è univoco.
        if (string.IsNullOrWhiteSpace(planCode) || string.IsNullOrWhiteSpace(executionKey))
            return Path.Combine(workspacePath, "sessions", sessionId);

        var folderName = SanitizeFolderName($"{planCode}-{executionKey}");
        return request.ClientRunMode == ClientRunMode.Backtest
            ? WorkspaceBacktestPaths.ResolveBacktestPath(workspacePath, folderName)
            : Path.Combine(workspacePath, "sessions", folderName);
    }

    /// <summary>
    /// Riduce piano ed execution key a un nome di cartella. Non è solo cosmetica: il nome finisce
    /// in un path, e caratteri come <c>|</c> o <c>/</c> lo romperebbero o lo farebbero uscire dalla
    /// cartella prevista.
    /// </summary>
    private static string SanitizeFolderName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string((value ?? string.Empty)
            .Select(character => invalid.Contains(character) || character == ' ' ? '-' : character)
            .ToArray())
            .Trim('-');
        return cleaned.Length == 0 ? "sessione" : cleaned;
    }

    private TradingSessionDescriptor CreateCore(
        CreateTradingSessionRequest request, string? planCode, string? executionKey)
    {
        if (!string.IsNullOrWhiteSpace(request.TitanoRunId) &&
            string.IsNullOrWhiteSpace(request.TitanoBacktestFolder))
            throw new ArgumentException("TitanoRunId richiede TitanoBacktestFolder.");

        // Le modalità filtrate non possono degradare in silenzio a "nessun filtro": senza rotazione
        // la sessione eseguirebbe tutto il masterfilter, cioè l'opposto di quanto richiesto. Il run
        // non si richiede più esplicitamente: si risolve "l'ultimo per questa cartella" qui stesso,
        // così un run mai generato fa fallire l'apertura invece della prima barra.
        if (request.TitanoMode != TitanoFilterMode.Disabled && string.IsNullOrWhiteSpace(request.TitanoBacktestFolder))
            throw new ArgumentException(
                $"La modalità {request.TitanoMode} richiede TitanoBacktestFolder. " +
                "Usa TitanoFilterMode.Disabled per eseguire senza filtro Titano.");

        var pinnedTitanoRunId = string.IsNullOrWhiteSpace(request.TitanoRunId) ? null : request.TitanoRunId.Trim();
        if (request.TitanoMode != TitanoFilterMode.Disabled &&
            ResolveRunIdForFolder(pinnedTitanoRunId, request.WorkspaceId, request.TitanoBacktestFolder) is null)
            throw new ArgumentException(
                $"Nessun run Titano trovato per la cartella '{request.TitanoBacktestFolder}': esegui prima una rotazione.");

        RequireCoherentRunMode(request.TitanoMode, request.ClientRunMode);

        var filter = _workspaces.GetMasterFilter(request.WorkspaceId);
        if (filter.StrategiesFilter.Count == 0)
            throw new ArgumentException("Il masterfilter del workspace è vuoto.");

        var definitions = StrategyFactory.GetRegisteredStrategies();
        var byId = definitions.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var invalid = filter.StrategiesFilter.Where(id => !byId.ContainsKey(id)).ToArray();
        if (invalid.Length != 0)
            throw new ArgumentException(
                "ID strategia non eseguibili nel masterfilter: " +
                string.Join("; ", invalid.Select(StrategyFactory.DescribeUnusableId)));

        var strategies = filter.StrategiesFilter.Select(id =>
        {
            var d = byId[id];
            return StrategyFactory.CreateStrategy(d.Id, d.Symbol, d.TimeframeMinutes, d.Parameters)
                   ?? throw new InvalidOperationException($"Impossibile creare la strategia '{id}'.");
        }).ToList();
        // Il valore punto è del contratto Piootoo, non del piano: viene dal registro strumenti, che
        // lancia sui simboli non verificati (errore esplicito voluto, vedi PROGETTO.md §7). La
        // granularità di volume (min/step/rounding) non è più qui: per ExternalBroker è quella del
        // broker, applicata al claim da AccountSymbolConversion, quindi qui resta "differita" per non
        // arrotondare due volte (vedi ApplyGroupAllocation/PositionSizingService). Per il motore
        // interno, senza claim, resta il contratto intero.
        var instrumentMetadata = strategies.Select(x => Normalize(x.Symbol)).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(symbol => symbol, symbol => new InstrumentMetadata
            {
                Symbol = symbol,
                DollarsPerPoint = InstrumentRegistry.PointValue(symbol),
                MinimumQuantity = 1m,
                QuantityStep = 1m,
                RoundingMode = request.ExecutionMode == ExecutionMode.ExternalBroker
                    ? QuantityRoundingMode.Deferred
                    : QuantityRoundingMode.FuturesContracts
            }, StringComparer.OrdinalIgnoreCase);

        var engine = new PiootooTradingService();
        engine.Initialize(request.InitialCapital, request.CommissionPerContract);
        var sessionId = Guid.NewGuid().ToString("N");
        var sessionDirectory = ResolveSessionDirectory(request, planCode, executionKey, sessionId);

        // Una sessione di backtest scrive sotto backtests/ accanto ai run del motore interno:
        // senza marcatore le due origini sarebbero indistinguibili in elenco.
        if (request.ClientRunMode == ClientRunMode.Backtest && !string.IsNullOrWhiteSpace(planCode))
        {
            WorkspaceService.WriteBacktestOrigin(sessionDirectory, new BacktestOriginInfo
            {
                Origin = BacktestOrigin.ExternalBroker,
                CreatedUtc = DateTime.UtcNow,
                PlanCode = planCode,
                ExecutionKey = executionKey,
                SessionId = sessionId
            });
        }
        var store = new TradingJsonStore(sessionDirectory);
        store.Initialize();
        var session = new Session
        {
            Id = sessionId,
            Token = string.IsNullOrWhiteSpace(request.ClientSessionToken)
                ? Convert.ToHexString(Guid.NewGuid().ToByteArray())
                : request.ClientSessionToken,
            WorkspaceId = request.WorkspaceId,
            PlanCode = planCode,
            ExecutionKey = executionKey,
            Mode = request.ExecutionMode,
            InitialCapital = request.InitialCapital,
            Strategies = strategies,
            SimulatedEngine = engine,
            Store = store,
            PinnedTitanoRunId = pinnedTitanoRunId,
            TitanoBacktestFolder = request.TitanoBacktestFolder,
            TitanoMode = request.TitanoMode,
            ClientRunMode = request.ClientRunMode,
            EnforceConcurrencyLimits = request.EnforceConcurrencyLimits
                ?? DefaultEnforceConcurrencyLimits(request.ClientRunMode, request.TitanoMode),
            PositionSizing = ResolvePositionSizing(request.ExecutionMode, request.PositionSizing),
            InstrumentMetadata = instrumentMetadata,
            PeakEquity = request.InitialCapital,
            Status = TradingSessionStatus.Created,
            CreatedAtUtc = DateTime.UtcNow
        };
        _sessions[session.Id] = session;
        return Describe(session);
    }

    /// <summary>
    /// I freni di portafoglio del sizing (drawdown dal picco, esposizione lorda) sono calcolati sul
    /// capitale e sull'equity della sessione. In <see cref="ExecutionMode.ExternalBroker"/> il server
    /// non possiede né l'uno né l'altra — l'equity è del broker e ogni account ha il proprio saldo —
    /// quindi si disattivano invece di girare su un denominatore fittizio: il rischio di portafoglio
    /// live è governato dal broker (<c>PiootooRiskGuardianBot</c>), coerentemente con l'invariante
    /// "il server decide <i>cosa</i>, il broker decide <i>se e a che prezzo</i>".
    ///
    /// <para>Restano attivi il moltiplicatore di allocazione Titano e il freno per volatilità di
    /// mercato, che dipendono dalle barre e non dal capitale. Vedi <c>docs/decisioni.md</c>
    /// (2026-08-05).</para>
    /// </summary>
    private static PositionSizingConfig ResolvePositionSizing(
        ExecutionMode mode, PositionSizingConfig requested)
    {
        if (mode == ExecutionMode.ServerSimulated || !requested.PortfolioRisk.Enabled)
            return requested;

        return new PositionSizingConfig
        {
            ClampMultipliersToUnitInterval = requested.ClampMultipliersToUnitInterval,
            MarketVolatility = requested.MarketVolatility,
            PortfolioRisk = new PortfolioRiskSizingConfig
            {
                Enabled = false,
                MaximumDrawdown = requested.PortfolioRisk.MaximumDrawdown,
                MaximumGrossExposure = requested.PortfolioRisk.MaximumGrossExposure,
                EnableAggressiveModules = requested.PortfolioRisk.EnableAggressiveModules,
                FractionalFactor = requested.PortfolioRisk.FractionalFactor,
                MaximumMultiplier = requested.PortfolioRisk.MaximumMultiplier
            }
        };
    }

    /// <summary>
    /// Rifiuta le combinazioni modalità Titano / contesto di esecuzione che non possono essere
    /// corrette. Non è pignoleria: entrambe producono risultati plausibili ma sbagliati, e il primo
    /// segnale del problema arriverebbe dai numeri, non da un errore.
    ///
    /// Con <see cref="ClientRunMode.Unknown"/> non si verifica nulla: il client non ha dichiarato il
    /// contesto e inventarne uno sarebbe peggio che lasciare la responsabilità a chi configura.
    /// </summary>
    private static void RequireCoherentRunMode(TitanoFilterMode titanoMode, ClientRunMode runMode)
    {
        if (runMode == ClientRunMode.Unknown) return;

        if (titanoMode == TitanoFilterMode.Realtime && runMode == ClientRunMode.Backtest)
            throw new ArgumentException(
                "TitanoFilterMode.Realtime non è utilizzabile da un client in backtest: la rotazione " +
                "'corrente' verrebbe applicata a barre storiche e, oltre la fine del manifest, resterebbe " +
                "congelata sull'ultimo periodo calcolato — cioè look-ahead. Usa BacktestRotationFile per " +
                "filtrare con le rotazioni calcolate offline, oppure Disabled per non filtrare.");

        if (titanoMode == TitanoFilterMode.BacktestRotationFile && runMode == ClientRunMode.Realtime)
            throw new ArgumentException(
                "TitanoFilterMode.BacktestRotationFile non è utilizzabile in tempo reale: il manifest " +
                "copre l'intervallo del backtest da cui è stato generato, quindi il tempo live ne esce " +
                "quasi subito e la sessione si fermerebbe alla prima barra scoperta. Usa Realtime.");
    }

    public TradingSessionDescriptor SetStatus(string sessionId, string token, TradingSessionStatus status)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            session.Status = status;
            Persist(session);
            return Describe(session);
        }
    }

    public PushBarsResponse PushBars(PushBarsRequest request)
    {
        var session = Get(request.SessionId, request.SessionToken);
        lock (session.Gate)
        {
            if (session.Status != TradingSessionStatus.Running)
                throw new InvalidOperationException("La sessione non è in esecuzione.");

            var accepted = 0;
            var duplicates = 0;
            var emitted = new List<OrderIntent>();
            foreach (var bar in request.Bars)
            {
                ValidateBar(bar);
                if (!session.BarKeys.Add(bar.IdempotencyKey))
                {
                    duplicates++;
                    continue;
                }

                var stream = StreamKey(bar.Symbol, bar.TimeframeMinutes);
                if (session.LastSequence.TryGetValue(stream, out var last) && bar.Sequence <= last)
                {
                    session.BarKeys.Remove(bar.IdempotencyKey);
                    throw new ArgumentException($"Barra out-of-order per {stream}: sequence {bar.Sequence}, ultima {last}.");
                }
                session.LastSequence[stream] = bar.Sequence;
                accepted++;

                var normalizedBar = CloneUtc(bar);
                if (!session.History.TryGetValue(stream, out var history))
                    session.History[stream] = history = [];
                history.Add(normalizedBar.Bar);

                EvaluateClosedBar(session, normalizedBar, history, emitted);
            }
            Persist(session);
            return new PushBarsResponse { AcceptedBars = accepted, DuplicateBars = duplicates, Intents = emitted };
        }
    }

    /// <summary>
    /// Riceve, per ogni stream, la finestra di candele che le strategie di quello stream richiedono.
    /// Il server accoda alla propria storia le candele che non ha (è il riscaldamento: alla prima
    /// finestra di un run entrano tutte) e valuta la sola ultima candela.
    ///
    /// <para>Idempotenza e ordinamento restano sulla barra da valutare: rispedire la stessa finestra
    /// è un duplicato, spedirne una che finisce prima dell'ultima già valutata è un errore, esattamente
    /// come per <see cref="PushBars"/>. Le candele più vecchie non sono soggette a quel controllo
    /// perché per definizione arrivano già viste.</para>
    ///
    /// <para><b>Le candele restano in RAM.</b> La sessione non le scrive nel datafeed del workspace:
    /// <c>TradingJsonStore</c> persiste signal, trade e rotation-log, non barre. Raccogliere il feed
    /// da cTrader e salvarlo su disco è compito di un cBot dedicato, non di questa strada.</para>
    /// </summary>
    public PushBarWindowResponse PushBarWindow(PushBarWindowRequest request)
    {
        var session = Get(request.SessionId, request.SessionToken);
        lock (session.Gate)
        {
            if (session.Status != TradingSessionStatus.Running)
                throw new InvalidOperationException("La sessione non è in esecuzione.");

            var accepted = 0;
            var duplicates = 0;
            var backfilled = 0;
            var emitted = new List<OrderIntent>();
            var streams = new List<StreamHistoryStatus>();

            foreach (var window in request.Windows)
            {
                if (window.Candles is null || window.Candles.Count == 0)
                    throw new ArgumentException(
                        $"Finestra vuota per {window.Symbol}/{window.TimeframeMinutes}m: " +
                        "l'ultima candela è la barra da valutare e non può mancare.");

                var closedBar = new ClosedBar
                {
                    Symbol = window.Symbol,
                    TimeframeMinutes = window.TimeframeMinutes,
                    BarTimeUtc = window.Candles[^1].DateTime,
                    Sequence = window.Sequence,
                    IdempotencyKey = window.IdempotencyKey,
                    Bar = window.Candles[^1]
                };
                ValidateBar(closedBar);

                var stream = StreamKey(window.Symbol, window.TimeframeMinutes);
                if (!session.History.TryGetValue(stream, out var history))
                    session.History[stream] = history = [];

                // Validazioni della finestra prima di toccare qualsiasi stato della sessione: una
                // finestra rifiutata non deve lasciare dietro di sé una idempotency key consumata o
                // una sequence avanzata, altrimenti il rinvio corretto verrebbe scambiato per replay.
                var previousUtc = DateTime.MinValue;
                foreach (var candle in window.Candles)
                {
                    RequireUtc(candle.DateTime, $"{stream}: DateTime della candela");
                    if (candle.DateTime <= previousUtc)
                        throw new ArgumentException(
                            $"Finestra non ordinata per {stream}: {candle.DateTime:O} non è successiva a {previousUtc:O}.");
                    previousUtc = candle.DateTime;
                }

                var lastKnownUtc = history.Count == 0 ? (DateTime?)null : history[^1].DateTime;

                // La finestra deve SOVRAPPORSI alla storia già presente: se comincia dopo l'ultima
                // candela nota, fra le due c'è un buco che nessuno colmerà più, e le strategie
                // girerebbero su una serie bucata senza che nulla lo segnali. Il criterio è la
                // sovrapposizione e non l'aritmetica sui timestamp perché gli stream hanno buchi
                // legittimi — fine settimana, festivi, mercati chiusi — che una differenza in minuti
                // scambierebbe per barre perse.
                if (lastKnownUtc is { } lastKnown && window.Candles[0].DateTime > lastKnown)
                    throw new ArgumentException(
                        $"Buco nella storia di {stream}: la finestra parte da {window.Candles[0].DateTime:O} " +
                        $"ma il server è fermo a {lastKnown:O}. Il client deve includere almeno una candela " +
                        "già nota, oppure ricaricare dal broker abbastanza storia da coprire l'intervallo.");

                // Riscaldamento: si accoda e basta. Niente idempotency key consumata e niente sequence
                // avanzata, perché la stessa barra può tornare più tardi come barra da valutare e in
                // quel momento non deve sembrare un replay.
                if (!window.EvaluateLastCandle)
                {
                    backfilled += Backfill(history, window.Candles, lastKnownUtc);
                    streams.Add(BuildStreamStatus(session, window.Symbol, window.TimeframeMinutes, history.Count, evaluated: 0));
                    continue;
                }

                if (!session.BarKeys.Add(closedBar.IdempotencyKey))
                {
                    duplicates++;
                    streams.Add(BuildStreamStatus(session, window.Symbol, window.TimeframeMinutes, history.Count, evaluated: 0));
                    continue;
                }

                if (session.LastSequence.TryGetValue(stream, out var last) && window.Sequence <= last)
                {
                    session.BarKeys.Remove(closedBar.IdempotencyKey);
                    throw new ArgumentException(
                        $"Finestra out-of-order per {stream}: sequence {window.Sequence}, ultima {last}.");
                }
                session.LastSequence[stream] = window.Sequence;
                accepted++;

                backfilled += Math.Max(0, Backfill(history, window.Candles, lastKnownUtc) - 1);

                // La sequence è passata ma la candela finale non è entrata: vuol dire che il client
                // numera le sequence in modo scollegato dagli orari delle barre. Valutare comunque
                // significherebbe rivalutare una barra vecchia con una chiave nuova.
                if (history[^1].DateTime != closedBar.BarTimeUtc)
                    throw new ArgumentException(
                        $"Finestra incoerente per {stream}: l'ultima candela è {closedBar.BarTimeUtc:O} " +
                        $"ma la storia arriva già a {history[^1].DateTime:O}. Sequence e orari di barra " +
                        "devono crescere insieme.");

                var evaluatedBar = new ClosedBar
                {
                    Symbol = closedBar.Symbol,
                    TimeframeMinutes = closedBar.TimeframeMinutes,
                    BarTimeUtc = DateTime.SpecifyKind(closedBar.BarTimeUtc, DateTimeKind.Utc),
                    Sequence = closedBar.Sequence,
                    IdempotencyKey = closedBar.IdempotencyKey,
                    Bar = history[^1]
                };
                var evaluated = EvaluateClosedBar(session, evaluatedBar, history, emitted);
                streams.Add(BuildStreamStatus(session, window.Symbol, window.TimeframeMinutes, history.Count, evaluated));
            }

            Persist(session);
            return new PushBarWindowResponse
            {
                AcceptedBars = accepted,
                DuplicateBars = duplicates,
                BackfilledBars = Math.Max(0, backfilled),
                Intents = emitted,
                Streams = streams,
                ClaimableIntents = CountClaimableIntents(session)
            };
        }
    }

    /// <summary>
    /// Accoda alla storia le sole candele più recenti dell'ultima già presente e restituisce quante
    /// ne ha aggiunte. È così che il client può rispedire una finestra sovrapposta a ogni barra senza
    /// duplicare nulla, e che la prima finestra di un run entra tutta.
    /// </summary>
    private static int Backfill(List<OhlcvData> history, IReadOnlyList<OhlcvData> candles, DateTime? lastKnownUtc)
    {
        var added = 0;
        foreach (var candle in candles)
        {
            if (lastKnownUtc is { } known && candle.DateTime <= known)
                continue;
            history.Add(candle);
            added++;
        }
        return added;
    }

    /// <summary>
    /// Quante strategie del masterfilter insistono su questo stream, quante ne ha valutate il server
    /// e quante ha saltato per storia insufficiente. È la risposta a "perché non arrivano segnali".
    /// </summary>
    private StreamHistoryStatus BuildStreamStatus(
        Session session, string symbol, int timeframeMinutes, int historyBars, int evaluated)
    {
        var onStream = session.Strategies
            .Where(s => Normalize(s.Symbol) == Normalize(symbol) && s.TimeframeMinutes == timeframeMinutes)
            .ToArray();
        return new StreamHistoryStatus
        {
            Symbol = Normalize(symbol),
            TimeframeMinutes = timeframeMinutes,
            HistoryBars = historyBars,
            RequiredCandles = onStream.Length == 0 ? 0 : onStream.Max(s => s.RequiredCandles),
            EvaluatedStrategies = evaluated,
            SkippedForInsufficientHistory = onStream.Count(s => historyBars < s.RequiredCandles)
        };
    }

    /// <summary>
    /// Corpo comune a <see cref="PushBars"/> e <see cref="PushBarWindow"/>: aggiorna i prezzi di
    /// mercato, risolve la rotazione Titano, valuta le strategie dello stream, dimensiona e traduce i
    /// segnali in intent. Restituisce quante strategie sono state effettivamente valutate.
    /// </summary>
    private int EvaluateClosedBar(
        Session session, ClosedBar normalizedBar, List<OhlcvData> history, List<OrderIntent> emitted)
    {
        var bar = normalizedBar;

        // La barra nuova rende definitivamente morti i template della barra precedente: si buttano
        // qui invece di lasciarli in lista e scartarli a ogni claim.
        PurgeExpiredTemplates(session, bar.BarTimeUtc);

        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            { [Normalize(bar.Symbol)] = bar.Bar.Close };
        var bars = new Dictionary<string, OhlcvData>(StringComparer.OrdinalIgnoreCase)
            { [Normalize(bar.Symbol)] = normalizedBar.Bar };

        // Ordering autorevole: prima exit/pending, poi valutazione, infine intent.
        if (session.Mode == ExecutionMode.ServerSimulated)
            session.SimulatedEngine.UpdateMarketPrices(prices, bars, bar.BarTimeUtc);

        IReadOnlyList<ITradingStrategy> evaluationStrategies = session.Strategies;
        var allocations = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        TitanoEffectiveStrategies? effective = null;
        string? rotationNote = null;
        if (!string.IsNullOrWhiteSpace(session.TitanoBacktestFolder))
        {
            var service = _titano ?? throw new InvalidOperationException("Servizio Titano non disponibile.");
            var runId = ResolveRunIdForFolder(session, session.TitanoBacktestFolder);

            // In Disabled la rotazione non filtra niente: si risolve solo per lasciarne traccia nel
            // rotation-log. Se non esiste ancora un run per quella cartella non c'è nulla da
            // registrare, e pretenderlo bloccherebbe proprio il run che deve generarlo: il campione
            // sorgente di Titano nasce da una sessione Disabled che punta alla cartella dove i suoi
            // trade verranno promossi. L'apertura della sessione applica già la stessa regola
            // (vedi CreateCore), quindi senza questa il piano si apriva e poi falliva a ogni barra.
            if (runId is null && session.TitanoMode == TitanoFilterMode.Disabled)
            {
                rotationNote = "modalità Disabled senza run Titano per la cartella: nessun filtro da applicare";
            }
            else
            {
                if (runId is null)
                    throw new InvalidOperationException(
                        $"Nessun run Titano trovato per la cartella '{session.TitanoBacktestFolder}': " +
                        "esegui prima una rotazione.");
                effective = service.Resolve(session.WorkspaceId, session.TitanoBacktestFolder,
                    runId, bar.BarTimeUtc, session.TitanoMode);
                foreach (var state in effective.StrategyStates)
                    allocations[state.StrategyCode] = state.AllocationMultiplier;

                if (session.TitanoMode == TitanoFilterMode.Disabled)
                {
                    // Rotazione risolta e registrata, ma non applicata: le allocazioni restano
                    // neutre e tutte le strategie del masterfilter vengono valutate. È il run che
                    // produce i trade su cui l'analisi Titano calcolerà le rotazioni.
                    allocations.Clear();
                    rotationNote = "modalità Disabled: rotazione risolta solo a scopo diagnostico, nessun filtro applicato";
                }
                else if (!effective.HasActivePeriod)
                {
                    // Nessun periodo copre questa barra. In Realtime il fallback sull'ultimo periodo
                    // è già stato tentato dentro Resolve, quindi qui siamo davvero scoperti: è un
                    // manifest non allineato all'intervallo che si sta eseguendo. Fermarsi è meglio
                    // che eseguire senza filtri una sessione che l'utente ha chiesto filtrata.
                    throw new InvalidOperationException(
                        $"Nessun periodo Titano copre la barra {bar.BarTimeUtc:O}: il manifest '{runId}' " +
                        $"copre {effective.ManifestFromUtc:O} → {effective.ManifestToUtc:O}. " +
                        "Rigenera la rotazione su un backtest che copra questo intervallo, oppure " +
                        "esegui la sessione in modalità Disabled.");
                }
                else
                {
                    evaluationStrategies = session.Strategies
                        .Where(x => effective.EffectiveStrategies.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
                        .ToArray();

                    if (effective.UsedLatestPeriod)
                        rotationNote =
                            $"barra {bar.BarTimeUtc:O} oltre la fine del manifest ({effective.ManifestToUtc:O}): " +
                            "applicata la rotazione dell'ultimo periodo calcolato. Rigenera l'analisi Titano.";
                }
            }
        }
        var signals = _evaluation.Evaluate(
            evaluationStrategies,
            normalizedBar,
            history,
            strategy => GetExecution(session, strategy, bar.BarTimeUtc));

        if (effective is not null)
            session.RotationLog.Add(BuildRotationLogEntry(
                session, bar.BarTimeUtc, effective, evaluationStrategies, signals, rotationNote));
        var sized = new Dictionary<TradeSignal, PositionSizingResult>();
        foreach (var signal in signals)
        {
            var multiplier = allocations.TryGetValue(signal.StrategyCode, out var value) ? value : 1m;
            var snapshot = Snapshot(session);
            session.PeakEquity = Math.Max(session.PeakEquity, snapshot.Equity);
            var result = _positionSizing.Calculate(new PositionSizingRequest
            {
                BaseQuantity = signal.Quantity, StrategyEquityMultiplier = multiplier,
                Instrument = session.InstrumentMetadata[Normalize(signal.Symbol)],
                Config = session.PositionSizing, AvailableBars = history,
                TimestampUtc = bar.BarTimeUtc, InitialCapital = session.InitialCapital,
                Equity = snapshot.Equity, PeakEquity = session.PeakEquity,
                GrossExposureFraction = session.InitialCapital <= 0 ? 1m :
                    session.ExternalPositions.Values.Sum(x => x.Quantity * x.EntryPrice) / session.InitialCapital
            });
            sized[signal] = result;
            signal.Quantity = result.FinalQuantity;
        }
        session.LastEvaluatedBarTimeUtc = bar.BarTimeUtc;
        var multiAccount = session.AccountGroups.Count > 0;
        foreach (var signal in signals)
        {
            if (signal.RuntimeState is not null)
                session.SimulatedEngine.CaptureStrategyRuntimeState(
                    signal.StrategyCode, signal.Symbol, signal.RuntimeState);
            var result = sized.GetValueOrDefault(signal);

            // Un ExitOnly chiude la posizione opposta già confermata dal broker; non viene
            // dimensionato né trasformato in un template di ingresso.
            if (signal.ExitOnly && session.Mode == ExecutionMode.ExternalBroker)
            {
                emitted.AddRange(CreateExitOnlyCloseIntents(session, signal));
                continue;
            }

            if (multiAccount)
            {
                // Template non assegnato: resta disponibile finché non viene reclamato da un
                // account libero di un gruppo (vedi GetNextSignalForAccount).
                var template = AddIntent(session, signal, result, addToIntents: false);
                if (result?.Reason is not null) template.Status = OrderIntentStatus.Cancelled;
                else session.EntryTemplates.Add(template);
                RecordActivity(session, SessionActivityKind.IntentCreato,
                    result?.Reason is { } scartato
                        ? $"scartato dal sizing: {scartato}"
                        : $"{template.Side} {template.OrderType} @ {template.Price:0.#####} qty {template.FinalQuantity:0.####}",
                    strategyCode: template.StrategyCode, symbol: template.Symbol, intentId: template.IntentId);
                emitted.Add(template);
                continue;
            }

            var intent = AddIntent(session, signal, result, conversion: ResolveDirectConversion(session));
            if (result?.Reason is not null) intent.Status = OrderIntentStatus.Cancelled;

            // Simbolo non operativo sul conto che esegue: l'intent resta come traccia ma
            // non deve essere eseguito.
            if (intent.FinalQuantity <= 0 && session.DirectAccountNumber is not null)
                intent.Status = OrderIntentStatus.Cancelled;

            // Limite di fill per sessione. In ExternalBroker è l'unico punto in cui può
            // essere applicato: il motore simulato che lo verifica al fill
            // (PiootooTradingService) qui non decide niente. L'intent resta in sessione come
            // traccia di audit ma non viene consegnato al client, altrimenti un client che
            // ignora Status lo eseguirebbe comunque.
            if (session.Mode == ExecutionMode.ExternalBroker &&
                MaxEntriesPerSessionReached(session, intent, accountNumber: null))
            {
                intent.Status = OrderIntentStatus.Cancelled;
                continue;
            }

            emitted.Add(intent);
        }

        var executableSignals = signals.Where(x => x.Quantity > 0).ToList();
        if (session.Mode == ExecutionMode.ServerSimulated && executableSignals.Count != 0)
        {
            session.SimulatedEngine.ProcessSignals(executableSignals, prices, bars, bar.BarTimeUtc);
            foreach (var intent in emitted.Where(i => i.Status == OrderIntentStatus.Pending))
                intent.Status = OrderIntentStatus.Filled;
        }

        // "Valutate" sono le strategie di questo stream che avevano abbastanza storia: le altre
        // StrategyEvaluationService le salta in silenzio, ed è esattamente il silenzio da spiegare.
        return evaluationStrategies.Count(s =>
            Normalize(s.Symbol) == Normalize(bar.Symbol) &&
            s.TimeframeMinutes == bar.TimeframeMinutes &&
            history.Count >= s.RequiredCandles);
    }

    public IReadOnlyList<OrderIntent> GetIntents(string sessionId, string token, long after = 0)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return session.Intents.Skip((int)Math.Max(0, after)).ToArray();
    }

    public IReadOnlyList<PersistedSignal> GetPersistedSignals(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return session.Store.ReadSignals();
    }

    public IReadOnlyList<PersistedTrade> GetPersistedTrades(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return session.Store.ReadTrades();
    }

    public IReadOnlyList<RotationLogEntry> GetRotationLog(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return session.Store.ReadRotationLog();
    }

    public TradingSessionSnapshot ApplyReport(string sessionId, ExecutionReportRequest request)
    {
        var session = Get(sessionId, request.SessionToken);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("Gli execution report sono ammessi solo in ExternalBroker.");
            var report = request.Report;
            RequireUtc(report.EventTimeUtc, nameof(report.EventTimeUtc));
            if (!session.ReportIds.Add(report.ReportId))
                return Snapshot(session);
            var intent = session.Intents.SingleOrDefault(x => x.IntentId == report.IntentId)
                         ?? throw new KeyNotFoundException($"Intent '{report.IntentId}' non trovato.");
            if (report.CumulativeFilledQuantity < intent.FilledQuantity || report.CumulativeFilledQuantity > intent.Quantity)
                throw new ArgumentException("CumulativeFilledQuantity non valida.");

            var delta = report.CumulativeFilledQuantity - intent.FilledQuantity;
            intent.FilledQuantity = report.CumulativeFilledQuantity;
            intent.ExternalOrderId = report.ExternalOrderId ?? intent.ExternalOrderId;
            intent.Status = report.Status switch
            {
                ExecutionReportStatus.Accepted => OrderIntentStatus.Accepted,
                ExecutionReportStatus.PartiallyFilled => OrderIntentStatus.PartiallyFilled,
                ExecutionReportStatus.Filled => OrderIntentStatus.Filled,
                ExecutionReportStatus.Rejected => OrderIntentStatus.Rejected,
                _ => OrderIntentStatus.Cancelled
            };

            RecordActivity(session,
                intent.IsClose ? SessionActivityKind.PosizioneChiusa : SessionActivityKind.EsitoEsecuzione,
                report.FillPrice is { } prezzo
                    ? $"{intent.Status} @ {prezzo:0.#####} qty {report.CumulativeFilledQuantity:0.####}"
                    : $"{intent.Status}",
                intent.AssignedAccountNumber ?? string.Empty,
                intent.AssignedGroupId ?? string.Empty,
                intent.StrategyCode, intent.Symbol, intent.IntentId);

            if (!intent.IsClose && intent.FilledQuantity == 0 &&
                intent.Status is OrderIntentStatus.Rejected or OrderIntentStatus.Cancelled &&
                intent.AssignedAccountNumber is { } rejectedAccount)
            {
                // Ingresso mai eseguito (rifiutato/annullato dal broker): libera subito lo slot di
                // gruppo, altrimenti resterebbe bloccato per sempre. Il budget di concorrenza
                // dell'account non ha niente da liberare: si ricalcola a ogni poll dagli intent
                // ancora Pending, e questo ha appena smesso di esserlo.
                if (session.AccountGroups.TryGetValue(rejectedAccount, out var freedGroupId))
                    session.GroupStrategySlots.Remove(SlotKey(freedGroupId, intent.StrategyCode, intent.Symbol));
            }

            if (delta > 0)
            {
                session.Fills++;
                var accountNumber = intent.AssignedAccountNumber;
                // Legacy (nessun gruppo configurato): chiave invariata rispetto al comportamento storico.
                // Multi-account: chiave per-account, così più account possono detenere indipendentemente
                // la stessa strategia/simbolo senza sovrascriversi a vicenda.
                var key = accountNumber is null
                    ? $"{intent.Symbol}|{intent.StrategyCode}"
                    : $"{accountNumber}|{intent.Symbol}|{intent.StrategyCode}";
                var canonicalKey = $"{intent.Symbol}|{intent.StrategyCode}";

                if (intent.IsClose)
                {
                    if (session.ExternalPositions.TryGetValue(key, out var position) &&
                        session.ExternalPositionDetails.TryGetValue(key, out var details))
                    {
                        var exitPrice = report.FillPrice ?? intent.Price;
                        var gross = position.Direction == SignalType.Buy
                            ? (exitPrice - position.EntryPrice) * delta
                            : (position.EntryPrice - exitPrice) * delta;
                        session.ExternalTrades.Add(new PersistedTrade
                        {
                            TradeId = report.ReportId,
                            OrderId = report.ExternalOrderId,
                            IntentId = intent.IntentId,
                            CorrelationId = details.IntentId,
                            SessionId = session.Id,
                            StrategyCode = intent.StrategyCode,
                            StrategyName = intent.StrategyCode,
                            Symbol = intent.Symbol,
                            Direction = position.Direction,
                            Quantity = delta,
                            EntryTimeUtc = details.EntryTimeUtc,
                            ExitTimeUtc = report.EventTimeUtc,
                            EntryPrice = position.EntryPrice,
                            ExitPrice = exitPrice,
                            ExitReason = "ExternalBrokerCloseFill",
                            GrossProfit = gross,
                            NetProfit = gross - report.Commission,
                            Commission = report.Commission,
                            StopLoss = details.StopLoss,
                            TakeProfit = details.TakeProfit,
                            AccountNumber = accountNumber
                        });
                    }
                    session.ExternalPositions.Remove(key);
                    session.ExternalPositionDetails.Remove(key);

                    if (accountNumber != null)
                    {
                        // Libera lo slot di gruppo: la coppia (strategia, simbolo) torna disponibile
                        // per un nuovo ingresso. Il budget di concorrenza dell'account si libera da
                        // sé, perché la posizione appena chiusa non sarà più nello snapshot broker.
                        if (session.AccountGroups.TryGetValue(accountNumber, out var groupId))
                            session.GroupStrategySlots.Remove(SlotKey(groupId, intent.StrategyCode, intent.Symbol));

                        if (session.StrategyHolderCounts.TryGetValue(canonicalKey, out var count) && count > 0)
                        {
                            count--;
                            if (count <= 0)
                            {
                                session.StrategyHolderCounts.Remove(canonicalKey);
                                session.CanonicalPositions.Remove(canonicalKey);
                            }
                            else session.StrategyHolderCounts[canonicalKey] = count;
                        }
                    }
                }
                else
                {
                    if (!session.ExternalPositions.ContainsKey(key)) session.Entries++;
                    var snapshot = new TradingPositionSnapshot
                    {
                        StrategyCode = intent.StrategyCode,
                        Symbol = intent.Symbol,
                        Direction = intent.Side,
                        Quantity = report.CumulativeFilledQuantity,
                        EntryPrice = report.FillPrice ?? intent.Price,
                        AccountNumber = accountNumber ?? string.Empty
                    };
                    session.ExternalPositions[key] = snapshot;
                    session.ExternalPositionDetails[key] =
                        (report.EventTimeUtc, intent.IntentId, intent.StopLoss, intent.TakeProfit);

                    if (accountNumber != null)
                    {
                        var holders = session.StrategyHolderCounts.GetValueOrDefault(canonicalKey);
                        session.StrategyHolderCounts[canonicalKey] = holders + 1;
                        if (holders == 0)
                        {
                            // Primo holder in assoluto per questa strategia/simbolo: diventa il riferimento
                            // canonico usato dalla valutazione strategie (GetExecution).
                            session.CanonicalPositions[canonicalKey] = snapshot;
                        }
                    }
                }
            }
            Persist(session);
            return Snapshot(session);
        }
    }

    public TradingSessionSnapshot GetSnapshot(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return Snapshot(session);
    }

    /// <summary>
    /// Quanti eventi tiene il buffer circolare di una sessione. Copre abbondantemente il ritmo di
    /// un monitor che polla ogni pochi secondi; oltre non serve, perche' il registro durevole sono
    /// <c>signals.json</c> e <c>trades.json</c> e non questo.
    /// </summary>
    private const int ActivityCapacity = 500;

    /// <summary>
    /// Registra un evento nel buffer della sessione. Va chiamato con <c>session.Gate</c> gia'
    /// preso: tutti i punti che lo usano stanno gia' dentro il lock del claim o della barra.
    ///
    /// <para>Non fa I/O e non serializza niente: e' un <c>Add</c> con una potatura in testa. E'
    /// deliberato — questo metodo viene chiamato dentro il ciclo del claim, che in backtest gira
    /// per ogni barra e ogni account.</para>
    /// </summary>
    private static void RecordActivity(
        Session session,
        SessionActivityKind kind,
        string detail,
        string accountNumber = "",
        string groupId = "",
        string strategyCode = "",
        string symbol = "",
        string intentId = "")
    {
        session.Activity.Add(new SessionActivityEntry
        {
            Sequence = ++session.ActivitySequence,
            // L'orologio della sessione e' l'ultima barra valutata, non l'ora di sistema: in un
            // replay storico le due cose distano mesi, e un monitor che mostrasse l'ora di sistema
            // su un backtest del 2025 sarebbe illeggibile. Fallback a UtcNow prima della prima barra.
            TimestampUtc = session.LastEvaluatedBarTimeUtc ?? DateTime.UtcNow,
            Kind = kind,
            AccountNumber = accountNumber,
            GroupId = groupId,
            StrategyCode = strategyCode,
            Symbol = symbol,
            Detail = detail,
            IntentId = intentId
        });

        if (session.Activity.Count > ActivityCapacity)
            session.Activity.RemoveRange(0, session.Activity.Count - ActivityCapacity);
    }

    /// <summary>
    /// Registra un claim negato solo quando il MOTIVO cambia rispetto all'ultimo gia' registrato
    /// per quell'account.
    ///
    /// <para>Senza questa guardia il buffer si riempie di "nessun segnale per la barra corrente"
    /// ripetuto a ogni poll, e gli eventi rari — quelli per cui il monitor esiste — vengono spinti
    /// fuori dalla finestra prima che qualcuno li veda. E' la stessa deduplica che il server
    /// applica gia' al proprio log (<c>LastClaimRefusal</c> nel controller).</para>
    /// </summary>
    private static void RecordRefusal(Session session, string accountNumber, string groupId, string stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return;
        if (session.LastRefusalByAccount.TryGetValue(accountNumber, out var precedente) && precedente == stage)
            return;

        session.LastRefusalByAccount[accountNumber] = stage;
        RecordActivity(session, SessionActivityKind.ClaimNegato, stage, accountNumber, groupId);
    }

    /// <summary>
    /// Gli eventi della sessione dopo <paramref name="since"/>. Il client passa il progressivo
    /// dell'ultimo evento che ha gia' mostrato e riceve solo il nuovo.
    ///
    /// <para><see cref="SessionActivityResponse.Troncato"/> dice che fra <paramref name="since"/> e
    /// il primo evento ancora in buffer c'e' un buco: il chiamante ha pollato troppo lentamente e
    /// il buffer circolare ha gia' buttato quello che gli manca. Dichiararlo e' il punto — una
    /// griglia con un buco silenzioso e' peggio di una che lo ammette.</para>
    /// </summary>
    public SessionActivityResponse GetActivity(string sessionId, string token, long since = 0)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            var entries = session.Activity.Where(e => e.Sequence > since).ToList();

            // Il buco c'e' solo se il client aveva gia' visto qualcosa (since > 0) e il buffer non
            // parte da dove lui si era fermato. Alla prima chiamata (since = 0) ricevere un buffer
            // gia' potato e' normale, non una perdita.
            var primoInBuffer = session.Activity.Count > 0 ? session.Activity[0].Sequence : 0;
            var troncato = since > 0 && primoInBuffer > since + 1;

            return new SessionActivityResponse
            {
                LastSequence = session.ActivitySequence,
                Troncato = troncato,
                Entries = entries
            };
        }
    }

    /// <summary>
    /// Promuove i trade di una sessione a campione sorgente per Titano.
    ///
    /// <para>Una sessione scrive in <c>&lt;workspace&gt;/sessions/&lt;id&gt;/</c>, le rotazioni
    /// leggono <c>&lt;workspace&gt;/backtests/&lt;cartella&gt;/trades.json</c>: senza questo
    /// passaggio un backtest eseguito dall'engine cTrader produce i trade ma non può alimentare
    /// Titano. Si copiano anche i signal, che servono a ricostruire cosa il server aveva deciso
    /// prima che il broker eseguisse.</para>
    ///
    /// <para>Zero trade è un errore e non una cartella vuota: una rotazione su un campione vuoto
    /// non fallisce, produce un manifest che disabilita tutto — esattamente il tipo di risultato
    /// plausibile e sbagliato che il progetto tratta come inaccettabile.</para>
    /// </summary>
    public PromoteSessionToBacktestResult PromoteToBacktest(string sessionId, PromoteSessionToBacktestRequest request)
    {
        var session = Get(sessionId, request.SessionToken);

        IReadOnlyList<PersistedTrade> trades;
        IReadOnlyList<PersistedSignal> signals;
        lock (session.Gate)
        {
            trades = session.Store.ReadTrades();
            signals = session.Store.ReadSignals();
        }

        if (trades.Count == 0)
        {
            throw new InvalidOperationException(
                $"La sessione '{sessionId}' non ha trade chiusi: promuoverla darebbe un campione vuoto, " +
                "e una rotazione su un campione vuoto disabilita tutte le strategie senza segnalare nulla.");
        }

        var destination = _workspaces.GetBacktestPath(session.WorkspaceId, request.BacktestFolderName);
        if (Directory.Exists(destination) && !request.OverwriteExisting)
        {
            throw new InvalidOperationException(
                $"Il backtest '{request.BacktestFolderName}' esiste già nel workspace '{session.WorkspaceId}'. " +
                "Conferma esplicitamente la sostituzione: i run Titano già calcolati portano l'hash del " +
                "trades.json di origine, e cambiarlo sotto di loro li rende non riproducibili.");
        }

        Directory.CreateDirectory(destination);
        var target = new TradingJsonStore(destination);
        target.Initialize();
        target.WriteTrades(trades);
        if (signals.Count > 0)
            target.UpsertSignals(signals);

        return new PromoteSessionToBacktestResult
        {
            WorkspaceId = session.WorkspaceId,
            BacktestFolderName = request.BacktestFolderName,
            TradeCount = trades.Count,
            SignalCount = signals.Count
        };
    }

    public void CancelIntent(string sessionId, string token, string intentId)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            var intent = session.Intents.SingleOrDefault(x => x.IntentId == intentId)
                         ?? throw new KeyNotFoundException($"Intent '{intentId}' non trovato.");
            if (intent.Status is OrderIntentStatus.Filled or OrderIntentStatus.Rejected or OrderIntentStatus.Cancelled)
                throw new InvalidOperationException("L'intent non è cancellabile.");
            intent.Status = OrderIntentStatus.Cancelled;
            Persist(session);
        }
    }

    public void SetAccountGroups(string sessionId, string token, IReadOnlyList<AccountGroupMapping> accounts)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("I gruppi account sono configurabili solo per sessioni ExternalBroker.");
            if (accounts.Any(a => string.IsNullOrWhiteSpace(a.AccountNumber) || string.IsNullOrWhiteSpace(a.GroupId)))
                throw new ArgumentException("AccountNumber e GroupId sono obbligatori per ogni voce.");
            var duplicated = accounts.GroupBy(a => a.AccountNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicated != null)
                throw new ArgumentException($"Account '{duplicated.Key}' configurato più di una volta.");

            session.AccountGroups.Clear();
            session.AccountMaxConcurrentTrades.Clear();
            session.AccountConcurrencyCountMode.Clear();
            session.AccountConversions.Clear();
            foreach (var mapping in accounts)
                session.AccountGroups[mapping.AccountNumber.Trim()] = mapping.GroupId.Trim();
            Persist(session);
        }
    }

    public IReadOnlyList<AccountGroupMapping> GetAccountGroups(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
            return session.AccountGroups
                .Select(kv => new AccountGroupMapping { AccountNumber = kv.Key, GroupId = kv.Value })
                .OrderBy(x => x.GroupId).ThenBy(x => x.AccountNumber).ToArray();
    }

    public void SetTradingGroups(string sessionId, string token, IReadOnlyList<TradingGroupRow> rows)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("I gruppi sono configurabili solo per sessioni ExternalBroker.");
            ValidateTradingGroupRows(rows);

            session.AccountGroups.Clear();
            session.AccountMaxConcurrentTrades.Clear();
            session.AccountConcurrencyCountMode.Clear();
            session.AccountConversions.Clear();
            session.GroupProfiles.Clear();
            foreach (var group in rows.GroupBy(r => r.GroupId.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var sample = group.First();
                session.GroupProfiles[group.Key] = new GroupTitanoProfile
                {
                    RotationSetupId = string.IsNullOrWhiteSpace(sample.RotationSetupId)
                        ? null
                        : sample.RotationSetupId.Trim(),
                    TitanoBacktestFolder = string.IsNullOrWhiteSpace(sample.TitanoBacktestFolder)
                        ? null
                        : sample.TitanoBacktestFolder.Trim(),
                    ApplyTitanoFilters = sample.ApplyTitanoFilters
                };
                foreach (var row in group)
                {
                    session.AccountGroups[row.AccountNumber.Trim()] = group.Key;
                    session.AccountMaxConcurrentTrades[row.AccountNumber.Trim()] = row.MaxConcurrentTrades;
                    session.AccountConcurrencyCountMode[row.AccountNumber.Trim()] = row.ConcurrencyCountMode;
                }
            }
            Persist(session);
        }
    }

    public IReadOnlyList<TradingGroupRow> GetTradingGroups(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
            return BuildTradingGroupRows(session);
    }

    private static void ValidateTradingGroupRows(IReadOnlyList<TradingGroupRow> rows)
    {
        if (rows.Count == 0)
            throw new ArgumentException("Almeno una riga gruppo/account è obbligatoria.");

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.GroupId) || string.IsNullOrWhiteSpace(row.AccountNumber))
                throw new ArgumentException("GroupId e AccountNumber sono obbligatori per ogni riga.");
            if (row.MaxConcurrentTrades < 0)
                throw new ArgumentException(
                    $"MaxConcurrentTrades non può essere negativo per l'account '{row.AccountNumber}'.");
        }

        var duplicatedAccount = rows.GroupBy(r => r.AccountNumber.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicatedAccount != null)
            throw new ArgumentException($"Account '{duplicatedAccount.Key}' configurato più di una volta.");

        foreach (var group in rows.GroupBy(r => r.GroupId.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var signatures = group.Select(r => (
                RotationSetupId: (r.RotationSetupId ?? string.Empty).Trim(),
                TitanoBacktestFolder: (r.TitanoBacktestFolder ?? string.Empty).Trim(),
                r.ApplyTitanoFilters)).Distinct().ToArray();
            if (signatures.Length > 1)
                throw new ArgumentException(
                    $"Profilo Titano inconsistente tra le righe del gruppo '{group.Key}'.");
        }
    }

    private static IReadOnlyList<TradingGroupRow> BuildTradingGroupRows(Session session) =>
        session.AccountGroups
            .Select(kv =>
            {
                session.GroupProfiles.TryGetValue(kv.Value, out var profile);
                return new TradingGroupRow
                {
                    GroupId = kv.Value,
                    AccountNumber = kv.Key,
                    MaxConcurrentTrades = session.AccountMaxConcurrentTrades.GetValueOrDefault(kv.Key),
                    ConcurrencyCountMode = session.AccountConcurrencyCountMode.GetValueOrDefault(kv.Key),
                    RotationSetupId = profile?.RotationSetupId,
                    TitanoBacktestFolder = profile?.TitanoBacktestFolder,
                    ApplyTitanoFilters = profile?.ApplyTitanoFilters ?? true
                };
            })
            .OrderBy(x => x.GroupId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.AccountNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public AccountSignalResponse GetNextSignalForAccount(string sessionId, string token, string accountNumber)
        => GetNextSignalForAccount(sessionId, token, accountNumber, brokerState: null);

    public AccountSignalResponse PollSignalForAccount(
        string sessionId, string accountNumber, AccountSignalPollRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return GetNextSignalForAccount(sessionId, request.SessionToken, accountNumber, request);
    }

    private AccountSignalResponse GetNextSignalForAccount(
        string sessionId,
        string token,
        string accountNumber,
        AccountSignalPollRequest? brokerState)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("AccountNumber obbligatorio.");
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("La distribuzione multi-account è disponibile solo in modalità ExternalBroker.");
            if (session.Status != TradingSessionStatus.Running)
                return new AccountSignalResponse { Reason = "SessionNotRunning" };
            if (!session.AccountGroups.TryGetValue(accountNumber, out var groupId))
                throw new ArgumentException(
                    $"Account '{accountNumber}' non configurato per questa sessione. Aggiungilo nel tab Trading Session.");

            // 1) Le CHIUSURE assegnate a questo account si ripropongono sempre, e prima di tutto:
            //    sono ordini da eseguire, non segnali da distribuire, e perderne una lascia aperta
            //    una posizione che nessuno chiuderà più. Non consumano budget e non ne aspettano.
            var pendingForAccount = session.Intents
                .Where(i => string.Equals(i.AssignedAccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase)
                            && i.Status == OrderIntentStatus.Pending)
                .OrderBy(i => i.CreatedAtUtc)
                .ToList();

            var pendingClose = pendingForAccount.FirstOrDefault(i => i.Kind == OrderIntentKind.Close);
            if (pendingClose != null)
                return new AccountSignalResponse { Intent = pendingClose };

            // "Adesso" è l'ultima barra valutata, non l'ora di sistema: in un replay storico le due
            // cose distano mesi, e con DateTime.UtcNow ogni template con ExpiresAtUtc (cioè ogni
            // ordine "next bar" dei motori Unger) risultava scaduto prima di poter essere reclamato.
            // Il server generava i segnali, il claim rispondeva sempre NoSignal, e sul broker non
            // arrivava mai un ordine. Fallback all'ora di sistema solo prima della prima barra.
            //
            // L'orologio è l'ORA DI APERTURA dell'ultima barra valutata, non la sua chiusura: i
            // motori Unger dichiarano ExpiresAtUtc = barra successiva, mentre BiasWeeklyEngine usa la
            // barra corrente. Con l'apertura entrambe le convenzioni danno al template esattamente
            // una barra di vita; con la chiusura la seconda scadrebbe prima di poter essere
            // reclamata.
            //
            // Su una sessione multi-timeframe questo valore può arretrare — il 60m che chiude alle
            // 16:00 porta BarTimeUtc 15:00, dopo che il 15m ha già spinto le 15:45 — quindi il
            // confronto è conservativo: tiene in vita un template un po' più a lungo, non ne scarta
            // mai uno ancora valido. È il verso giusto in cui sbagliare.
            //
            // Sta qui e non più alla selezione del template perché serve anche alla ripresa
            // dell'intent bloccato, subito sotto.
            var now = session.LastEvaluatedBarTimeUtc ?? DateTime.UtcNow;

            // 2) Budget di concorrenza dell'account. Conta gli ingressi in volo SULL'INSIEME delle
            //    strategie e trasversalmente ai simboli: dieci vuol dire dieci, che stiano su un
            //    simbolo solo o su dieci diversi. Cosa sia "in volo" lo decide il piano con
            //    ConcurrencyCountMode, e il conteggio è deduplicato per IntentId — un ordine già
            //    piazzato è insieme un intent Pending sul server e un pending order sul broker, e
            //    sommare i due conteggi grezzi lo contava due volte.
            //
            //    Fino al 11/08/2026 qui c'era anche un lucchetto (account, simbolo) che, su una
            //    sessione a simbolo singolo, rendeva MaxConcurrentTrades inapplicabile: il tetto
            //    effettivo era 1 qualunque valore si impostasse, e la seconda strategia sullo
            //    stesso simbolo non arrivava mai a mercato. Vedi docs/decisioni.md.
            var countMode = session.AccountConcurrencyCountMode.GetValueOrDefault(accountNumber);
            var inFlight = CountInFlightForAccount(
                session, accountNumber, brokerState, countMode, out var openPositions, out var pendingOrders);
            var maxConcurrentTrades = session.AccountMaxConcurrentTrades.GetValueOrDefault(accountNumber);
            if (IsConcurrentTradeLimitActive(session) &&
                maxConcurrentTrades > 0 &&
                inFlight >= maxConcurrentTrades)
            {
                // Tetto pieno. Se l'account ha un ingresso ancora Pending glielo riproponiamo: è
                // l'unico modo di recuperare un claim la cui risposta si è persa in rete, e il
                // client lo riconosce come già inviato e smette di drenare. Senza budget residuo
                // non ci sarebbe comunque niente di nuovo da consegnargli.
                //
                // Ma è una RIPRESA, non una consegna nuova: questa strada non passa da
                // NarrowTemplates, quindi i due vincoli che decidono se quell'ingresso può ancora
                // andare a mercato vanno riverificati qui, o l'intent bloccato li scavalca entrambi.
                // Nei log del 06/08, 08/08 e 11/08 si vede l'effetto: ordini piazzati dopo che il
                // limite di ingressi per sessione era già stato dichiarato raggiunto per gli altri
                // template della stessa barra.
                //
                //  - la scadenza: un ingresso "next bar" ripreso barre dopo è un livello che la
                //    strategia non sostiene più;
                //  - il limite di ingressi per sessione: la strategia ne dichiara uno per sessione
                //    (per PTS_NQ_PCH_* la sessione è il giorno di calendario UTC) e un intent
                //    bloccato non è un'eccezione a quel tetto.
                //
                // Un intent che non supera i controlli va chiuso, non solo saltato: lasciarlo
                // Pending significa riproporlo a ogni claim per sempre e tenere occupati i lucchetti
                // che lo riguardano.
                var stalledEntry = pendingForAccount.FirstOrDefault(i => i.Kind == OrderIntentKind.Entry);
                if (stalledEntry != null)
                {
                    if (stalledEntry.ExpiresAtUtc.HasValue && stalledEntry.ExpiresAtUtc.Value < now)
                        stalledEntry.Status = OrderIntentStatus.Cancelled;
                    else if (MaxEntriesPerSessionReached(session, stalledEntry, accountNumber))
                        stalledEntry.Status = OrderIntentStatus.Cancelled;
                    else
                        return new AccountSignalResponse { Intent = stalledEntry };

                    Persist(session);
                }

                return new AccountSignalResponse
                {
                    Reason = "MaxConcurrentTradesExceeded",
                    OpenPositions = openPositions,
                    PendingOrders = pendingOrders,
                    InFlight = inFlight,
                    MaxConcurrentTrades = maxConcurrentTrades
                };
            }

            // 3) Selezione del template. L'orologio (`now`) è già stato risolto sopra: lo condivide
            // con la ripresa dell'intent bloccato, che applica lo stesso criterio di scadenza.
            var priorities = ComputeStrategyPriority(session, groupId);
            var conversion = ResolveAccountConversion(session, accountNumber);

            // I filtri sono applicati a stadi invece che in una sola catena LINQ per poter dire QUALE
            // ha svuotato la lista. Un claim che non restituisce niente è indistinguibile, dal client,
            // da "nessuna strategia ha prodotto un segnale": senza questa traccia ogni indagine
            // ricomincia dal rileggere il codice del claim.
            // I template delle barre passate sono già stati rimossi da PurgeExpiredTemplates: se la
            // lista è vuota vuol dire che per la barra corrente nessuna strategia ha prodotto un
            // segnale, non che ce n'erano di vecchi da scartare.
            var stage = "nessun segnale per la barra corrente";
            var candidates = session.EntryTemplates.Where(t => t.Status == OrderIntentStatus.Pending).ToList();

            candidates = NarrowTemplates(candidates, ref stage,
                // Un simbolo disabilitato sull'account non è operativo su quel conto: il template
                // resta disponibile per gli altri account invece di essere consumato qui.
                t => conversion.IsSymbolEnabled(t.Symbol),
                "simbolo non abilitato sulla tabella di conversione dell'account");
            candidates = NarrowTemplates(candidates, ref stage,
                t => !t.ExpiresAtUtc.HasValue || t.ExpiresAtUtc.Value >= now,
                // Niente orario della barra nel testo: il motivo viene deduplicato per stringa da
                // client e server, e un valore che cambia a ogni barra manderebbe a vuoto la
                // deduplica riempiendo entrambi i log di righe identiche nella sostanza.
                "template scaduti rispetto alla barra corrente");
            candidates = NarrowTemplates(candidates, ref stage,
                t => !(session.TemplateClaimedGroups.TryGetValue(t.IntentId, out var claimed)
                       && claimed.Contains(groupId)),
                $"già reclamati dal gruppo '{groupId}'");
            // Sempre attivo, in ogni profilo: un account non tiene DUE ingressi in corso della
            // stessa strategia sullo stesso simbolo. Non è un vincolo di concorrenza — è l'identità
            // della strategia: quel segnale è già in mano al broker, e un secondo ordine sarebbe
            // rischio doppio sullo stesso motivo di ingresso.
            //
            // Serve perché `MaxEntriesPerSession` si applica al FILL e non al claim: due template
            // di barre diverse reclamati prima che il primo riempia passano entrambi il controllo, e
            // su un run reale (PTS_NQ_PCH_002_15, 14/10/2024 13:15) hanno prodotto due stop order
            // riempiti allo stesso prezzo e due posizioni da 20 lotti sullo stesso segnale.
            // Con i lucchetti attivi il 4 lo copre già, ma è più largo — vale per tutto il gruppo —
            // e a lucchetti spenti non c'era più niente a fermare il doppione.
            candidates = NarrowTemplates(candidates, ref stage,
                t => !AccountHasEntryInFlight(session, accountNumber, t.StrategyCode, t.Symbol),
                "l'account ha già un ingresso in corso per quella strategia su quel simbolo");

            // Lucchetto 4: è un vincolo di CONCORRENZA, non di distribuzione, quindi segue
            // EnforceConcurrencyLimits insieme a MaxConcurrentTrades. Spento, ogni segnale della
            // barra diventa un intent e il campione sorgente è completo.
            //
            // Il lucchetto 3 (TemplateClaimedGroups, sopra) resta invece SEMPRE attivo: non limita
            // quanto si opera in parallelo, dice che un template è già stato servito a quel gruppo.
            // Spegnerlo non produrrebbe più segnali, produrrebbe lo stesso segnale all'infinito, e
            // il drenaggio del cBot non terminerebbe mai.
            //
            // Il lucchetto 5 (account, simbolo) non esiste più: quanto un account opera in
            // parallelo lo dice MaxConcurrentTrades e basta, sull'insieme delle strategie.
            if (IsConcurrentTradeLimitActive(session))
                candidates = NarrowTemplates(candidates, ref stage,
                    t => !session.GroupStrategySlots.ContainsKey(SlotKey(groupId, t.StrategyCode, t.Symbol)),
                    "slot (gruppo, strategia, simbolo) già occupato");
            candidates = NarrowTemplates(candidates, ref stage,
                // Il limite di fill per sessione è per account: un template già consumato da un
                // account resta disponibile per gli altri.
                t => !MaxEntriesPerSessionReached(session, t, accountNumber),
                // Il motivo porta i NUMERI e non il solo esito. Nei log del 06/08 e 08/08 questo
                // filtro ha lasciato passare un template di PTS_NQ_PCH_002_15 alle 17:00 UTC dopo
                // che la stessa strategia aveva gia' riempito nello stesso giorno di calendario,
                // e dal solo "limite raggiunto" non si puo' dire QUALE delle cinque condizioni del
                // conteggio non abbia fatto match: secchio del template, secchio dell'intent
                // riempito, FilledQuantity, account assegnato, o il limite stesso assente sul
                // template. Stampandoli si legge la differenza invece di dedurla.
                //
                // La deduplica per stringa (RecordRefusal) regge: i valori cambiano una volta al
                // giorno, non a ogni barra, quindi non genera la riga-per-barra che la nota del
                // filtro di scadenza dice di evitare.
                DescribeSessionLimit(session, candidates, accountNumber));
            candidates = NarrowTemplates(candidates, ref stage,
                t => IsTemplateEligibleForGroup(session, groupId, t),
                "escluso dalla rotazione Titano del gruppo");

            var template = candidates
                .OrderByDescending(t => priorities.GetValueOrDefault(t.StrategyCode, 0m))
                .ThenBy(t => t.CreatedAtUtc)
                .FirstOrDefault();

            if (template is null)
            {
                RecordRefusal(session, accountNumber, groupId, stage);
                return new AccountSignalResponse { Reason = "NoSignal", ReasonDetail = stage };
            }

            var claim = CloneForClaim(session, template, accountNumber, groupId);
            if (claim.FinalQuantity <= 0)
                return new AccountSignalResponse
                {
                    Reason = "NoSignal",
                    // Il caso più insidioso: il template esiste ed è idoneo, ma la conversione
                    // dell'account (BalanceScale, moltiplicatore contratto, arrotondamento del
                    // broker) lo riduce a zero contratti. Dal client è identico a "nessun segnale".
                    ReasonDetail =
                        $"{template.StrategyCode} {template.Symbol}: quantità azzerata dalla conversione " +
                        $"dell'account ({template.FinalQuantity:0.####} contratti Piootoo -> " +
                        $"{claim.FinalQuantity:0.####}). {claim.SizingReason}"
                };
            session.Intents.Add(claim);
            session.LastRefusalByAccount.Remove(accountNumber);
            RecordActivity(session, SessionActivityKind.ClaimServito,
                $"{claim.Side} {claim.OrderType} @ {claim.Price:0.#####} qty {claim.FinalQuantity:0.####}",
                accountNumber, groupId, claim.StrategyCode, claim.Symbol, claim.IntentId);
            if (!session.TemplateClaimedGroups.TryGetValue(template.IntentId, out var claimedGroups))
                session.TemplateClaimedGroups[template.IntentId] = claimedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            claimedGroups.Add(groupId);

            // Lo slot di gruppo si scrive solo se qualcuno lo leggerà. Con EnforceConcurrencyLimits
            // spento sarebbe un dizionario che cresce per tutta la durata del run senza mai essere
            // consultato, e che mostrerebbe nelle diagnostiche occupazioni che nessun filtro sta
            // applicando.
            if (IsConcurrentTradeLimitActive(session))
                session.GroupStrategySlots[SlotKey(groupId, claim.StrategyCode, claim.Symbol)] = (accountNumber, claim.IntentId);
            Persist(session);
            return new AccountSignalResponse { Intent = claim };
        }
    }

    /// <summary>
    /// Il limite di trade concorrenti è governato da un flag esplicito della sessione, non più
    /// dedotto da <see cref="TitanoFilterMode"/>. Vedi
    /// <c>CreateTradingSessionRequest.EnforceConcurrencyLimits</c> e
    /// <c>docs/domini/distribuzione-multi-account.md</c> §4.
    /// </summary>
    /// <summary>
    /// Toglie da <c>EntryTemplates</c> i template la cui finestra di validità è chiusa rispetto alla
    /// barra appena arrivata, e con essi la traccia di quali gruppi li avevano reclamati.
    ///
    /// <para>Un template scaduto non è "da filtrare al prossimo giro": è morto. Tenerlo in lista
    /// costava tre cose. La lista cresceva per tutta la durata del run — un template per segnale, mai
    /// rimosso — e ogni claim la riscorreva tutta. La diagnostica del claim continuava a parlare di
    /// template di barre vecchie ("2 template scartati") invece di dire la verità, cioè che per la
    /// barra corrente non c'era nessun segnale. E soprattutto rendeva legittimo il sospetto che un
    /// segnale di una barra passata potesse ancora essere eseguito: non poteva, perché il filtro di
    /// scadenza c'era, ma la sola lettura del codice non bastava a convincersene.</para>
    ///
    /// <para>Si rimuovono solo i template con una scadenza dichiarata e già passata. Quelli senza
    /// <c>ExpiresAtUtc</c> non hanno una finestra da far scadere, e su una sessione multi-timeframe
    /// un template del 60m deve sopravvivere alle barre del 15m che gli passano accanto.</para>
    /// </summary>
    private static void PurgeExpiredTemplates(Session session, DateTime barTimeUtc)
    {
        if (session.EntryTemplates.Count == 0)
            return;

        var expired = session.EntryTemplates
            .Where(t => t.ExpiresAtUtc.HasValue && t.ExpiresAtUtc.Value < barTimeUtc)
            .ToList();
        if (expired.Count == 0)
            return;

        foreach (var template in expired)
        {
            session.EntryTemplates.Remove(template);
            session.TemplateClaimedGroups.Remove(template.IntentId);
        }
    }

    /// <summary>
    /// Applica un filtro ai template e, se è lui a svuotare la lista, registra in
    /// <paramref name="stage"/> il motivo. Restituisce la lista precedente quando il filtro non
    /// lascia nulla, così i filtri successivi non lavorano su una lista vuota e il motivo
    /// riportato resta il PRIMO che ha davvero escluso tutto.
    /// </summary>
    private static List<OrderIntent> NarrowTemplates(
        List<OrderIntent> candidates, ref string stage, Func<OrderIntent, bool> predicate, string reason)
    {
        if (candidates.Count == 0)
            return candidates;

        var filtered = candidates.Where(predicate).ToList();
        if (filtered.Count != 0)
            return filtered;

        stage = $"{reason} ({candidates.Count} template scartati)";
        return filtered;
    }

    private static bool IsConcurrentTradeLimitActive(Session session)
        => session.EnforceConcurrencyLimits;

    /// <summary>
    /// Default storico del flag: attivo ovunque tranne nel run che produce il <c>trades.json</c>
    /// sorgente delle rotazioni (backtest senza filtro Titano).
    /// </summary>
    public static bool DefaultEnforceConcurrencyLimits(ClientRunMode runMode, TitanoFilterMode titanoMode)
        => !(runMode == ClientRunMode.Backtest && titanoMode == TitanoFilterMode.Disabled);

    private static int CountServerPositionsForAccount(Session session, string accountNumber)
        => session.ExternalPositions.Keys.Count(key =>
            key.StartsWith($"{accountNumber}|", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Quanti ingressi ha in volo l'account, sull'insieme delle strategie e trasversalmente ai
    /// simboli. È il numero che si confronta con <c>MaxConcurrentTrades</c>.
    ///
    /// <para><b>Deduplicato per IntentId</b>, ed è il punto delicato: un ordine stop già piazzato
    /// esiste contemporaneamente come intent <c>Pending</c> sul server e come pending order nello
    /// snapshot del broker. Sommare i due conteggi grezzi — come faceva il codice precedente —
    /// contava due volte ogni ordine a mercato e dimezzava di fatto il tetto configurato.</para>
    ///
    /// <para>I claim consegnati e non ancora comparsi sul broker entrano nel conto: senza di loro
    /// un drenaggio veloce reclamerebbe tutti i template della barra prima che il broker registri
    /// il primo ordine, e il tetto verrebbe sfondato dal ritardo di propagazione invece che da una
    /// decisione.</para>
    ///
    /// <para>In <see cref="ConcurrencyCountMode.PositionsOnly"/> il conto sono le sole posizioni
    /// riempite: ordini pendenti e claim non ancora piazzati non consumano budget, perché in quel
    /// modello un ordine stop non è esposizione ma un'opzione, e spegnerne uno significa perdere
    /// il breakout che forse era l'unico a partire. Il drenaggio resta comunque finito, perché
    /// <c>AccountHasEntryInFlight</c> ammette un solo ingresso in volo per (strategia, simbolo):
    /// il massimo di ordini contemporanei è il numero di strategie della sessione.</para>
    /// </summary>
    /// <param name="brokerState">
    /// Stato dichiarato dal cBot al poll. Null quando il claim arriva dalla vecchia GET senza corpo:
    /// in quel caso si ripiega sul conteggio server delle posizioni, che non porta IntentId e va
    /// quindi sommato invece che unito.
    /// </param>
    private static int CountInFlightForAccount(
        Session session,
        string accountNumber,
        AccountSignalPollRequest? brokerState,
        ConcurrencyCountMode countMode,
        out int openPositions,
        out int pendingOrders)
    {
        var positions = brokerState?.Positions;
        var orders = brokerState?.Orders;
        openPositions = positions?.Count ?? CountServerPositionsForAccount(session, accountNumber);
        pendingOrders = orders?.Count ?? 0;

        var identified = new HashSet<string>(StringComparer.Ordinal);
        // Esposizione reale che non porta un IntentId leggibile: label di formato precedente, o
        // fallback al conteggio server. Non è deduplicabile, quindi si somma. Meglio contare una
        // volta di troppo che consegnare un ingresso oltre il tetto.
        var anonymous = 0;

        if (positions is null)
            anonymous += openPositions;
        else
            foreach (var position in positions)
            {
                if (string.IsNullOrWhiteSpace(position.IntentId)) anonymous++;
                else identified.Add(position.IntentId);
            }

        if (countMode == ConcurrencyCountMode.PositionsOnly)
            return identified.Count + anonymous;

        if (orders is not null)
            foreach (var order in orders)
            {
                if (string.IsNullOrWhiteSpace(order.IntentId)) anonymous++;
                else identified.Add(order.IntentId);
            }

        foreach (var intent in session.Intents)
            if (intent.Kind == OrderIntentKind.Entry &&
                intent.Status == OrderIntentStatus.Pending &&
                string.Equals(intent.AssignedAccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase))
                identified.Add(intent.IntentId);

        return identified.Count + anonymous;
    }

    public OrderIntent CreateExternalCloseIntent(string sessionId, CreateExternalCloseIntentRequest request)
    {
        var session = Get(sessionId, request.SessionToken);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("Gli intent di chiusura esterni sono ammessi solo in ExternalBroker.");
            if (string.IsNullOrWhiteSpace(request.StrategyCode) || string.IsNullOrWhiteSpace(request.Symbol))
                throw new ArgumentException("StrategyCode e Symbol sono obbligatori.");

            var symbol = Normalize(request.Symbol);
            var accountNumber = string.IsNullOrWhiteSpace(request.AccountNumber) ? null : request.AccountNumber.Trim();
            if (session.AccountGroups.Count > 0 && accountNumber is null)
                throw new ArgumentException("AccountNumber obbligatorio quando la sessione ha gruppi account configurati.");

            var key = accountNumber is null
                ? $"{symbol}|{request.StrategyCode}"
                : $"{accountNumber}|{symbol}|{request.StrategyCode}";
            if (!session.ExternalPositions.TryGetValue(key, out var position))
                throw new KeyNotFoundException($"Nessuna posizione aperta per '{key}'.");

            var intent = CreateCloseIntent(
                session, request.StrategyCode, symbol, position, accountNumber,
                request.Quantity, string.IsNullOrWhiteSpace(request.Reason) ? "ClientLocalExit" : request.Reason,
                // Stessa ragione del claim: in un replay storico l'ora di sistema data la chiusura a
                // mesi di distanza dal trade che la genera, e i PersistedTrade finirebbero fuori
                // dall'intervallo del run — cioè fuori da qualunque periodo di rotazione Titano.
                session.LastEvaluatedBarTimeUtc ?? DateTime.UtcNow);
            Persist(session);
            return intent;
        }
    }

    private IReadOnlyList<OrderIntent> CreateExitOnlyCloseIntents(Session session, TradeSignal signal)
    {
        var symbol = Normalize(signal.Symbol);
        var closes = new List<OrderIntent>();
        foreach (var position in session.ExternalPositions.Values
                     .Where(position =>
                         string.Equals(position.StrategyCode, signal.StrategyCode, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(position.Symbol, symbol, StringComparison.OrdinalIgnoreCase) &&
                         IsOpposite(position.Direction, signal.Type)))
        {
            var accountNumber = string.IsNullOrWhiteSpace(position.AccountNumber) ? null : position.AccountNumber;
            if (HasPendingCloseIntent(session, signal.StrategyCode, symbol, accountNumber))
                continue;

            closes.Add(CreateCloseIntent(
                session, signal.StrategyCode, symbol, position, accountNumber,
                position.Quantity, string.IsNullOrWhiteSpace(signal.Reason) ? "StrategyExitOnly" : signal.Reason,
                signal.Date));
        }
        return closes;
    }

    private static bool HasPendingCloseIntent(
        Session session, string strategyCode, string symbol, string? accountNumber) =>
        session.Intents.Any(intent =>
            intent.IsClose &&
            intent.Status is OrderIntentStatus.Pending or OrderIntentStatus.Accepted or OrderIntentStatus.PartiallyFilled &&
            string.Equals(intent.StrategyCode, strategyCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(intent.Symbol, symbol, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(intent.AssignedAccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase));

    private OrderIntent CreateCloseIntent(
        Session session, string strategyCode, string symbol, TradingPositionSnapshot position,
        string? accountNumber, decimal requestedQuantity, string reason, DateTime createdAtUtc)
    {
        var quantity = requestedQuantity > 0 ? Math.Min(requestedQuantity, position.Quantity) : position.Quantity;

        // La quantità di una chiusura è quella della posizione aperta, quindi già convertita: qui
        // la tabella serve solo per il simbolo con cui il client riconosce l'intent.
        var conversion = accountNumber is null
            ? AccountSymbolConversion.Identity
            : ResolveAccountConversion(session, accountNumber);

        session.IntentSequence++;
        var intent = new OrderIntent
        {
            IntentId = $"{session.Id}-{session.IntentSequence:D10}",
            SessionId = session.Id,
            StrategyCode = strategyCode,
            StrategyName = strategyCode,
            Symbol = symbol,
            AccountSymbol = conversion.GetAccountSymbol(symbol),
            AccountId = conversion.AccountId,
            ContractMultiplier = conversion.GetContractMultiplier(symbol),
            AccountBalanceScale = conversion.BalanceScale,
            CreatedAtUtc = createdAtUtc,
            Side = position.Direction,
            OrderType = TradeOrderType.Market,
            Quantity = quantity,
            QuantityBeforeAccountConversion = quantity,
            BaseQuantity = quantity,
            FinalQuantity = quantity,
            Price = position.EntryPrice,
            Kind = OrderIntentKind.Close,
            Reason = reason,
            AssignedAccountNumber = accountNumber,
            Status = OrderIntentStatus.Pending
        };
        session.Intents.Add(intent);
        return intent;
    }

    private static bool IsOpposite(SignalType positionDirection, SignalType signalDirection) =>
        (positionDirection == SignalType.Buy && signalDirection == SignalType.Sell) ||
        (positionDirection == SignalType.Sell && signalDirection == SignalType.Buy);

    /// <summary>
    /// Priorità per strategia usata per decidere quale segnale offrire per primo quando un account libero
    /// ha più template di ingresso disponibili in contemporanea: usa il ranking Titano del gruppo (o della
    /// sessione come fallback), altrimenti il PnL netto live accumulato dalla strategia nella sessione.
    /// </summary>
    private Dictionary<string, decimal> ComputeStrategyPriority(Session session, string groupId)
    {
        var profile = ResolveGroupTitano(session, groupId);
        if (!string.IsNullOrWhiteSpace(profile.TitanoBacktestFolder) && _titano != null)
        {
            try
            {
                var runId = ResolveRunIdForFolder(session, profile.TitanoBacktestFolder);
                if (!string.IsNullOrWhiteSpace(runId))
                {
                    var effective = _titano.Resolve(
                        session.WorkspaceId, profile.TitanoBacktestFolder, runId,
                        session.LastEvaluatedBarTimeUtc ?? DateTime.UtcNow);
                    var map = effective.StrategyStates.ToDictionary(
                        s => s.StrategyCode, s => s.AllocationMultiplier, StringComparer.OrdinalIgnoreCase);
                    if (map.Count > 0) return map;
                }
            }
            catch (Exception)
            {
                // Rotazione non risolvibile (es. dati mancanti): fallback sul PnL live sotto.
            }
        }

        var pnl = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var trade in session.ExternalTrades)
            pnl[trade.StrategyCode] = pnl.GetValueOrDefault(trade.StrategyCode) + trade.NetProfit;
        return pnl;
    }

    private static ResolvedGroupTitano ResolveGroupTitano(Session session, string groupId)
    {
        session.GroupProfiles.TryGetValue(groupId, out var profile);
        var groupFolder = profile?.TitanoBacktestFolder;
        var usesGroupFolder = !string.IsNullOrWhiteSpace(groupFolder);
        return new ResolvedGroupTitano(
            profile?.RotationSetupId,
            usesGroupFolder ? groupFolder : session.TitanoBacktestFolder,
            // La MODALITÀ (dove si sta girando) è della sessione; il gruppo può solo scegliere se
            // subire o no il filtro della propria cartella. Un gruppo senza cartella propria eredita
            // quindi la decisione della sessione: filtrata in tutto tranne che in Disabled.
            usesGroupFolder ? profile!.ApplyTitanoFilters : session.TitanoMode != TitanoFilterMode.Disabled);
    }

    /// <summary>
    /// Run effettivo per una cartella: il pin esplicito della sessione se la cartella è la sua
    /// stessa (percorso non-piano, vedi <see cref="Session.PinnedTitanoRunId"/>), altrimenti sempre
    /// l'ultimo generato — così una rotazione nuova si applica dalla barra successiva senza
    /// riaprire la sessione.
    /// </summary>
    private string? ResolveRunIdForFolder(Session session, string? backtestFolder)
    {
        if (string.IsNullOrWhiteSpace(backtestFolder))
            return null;
        var pinned = string.Equals(backtestFolder, session.TitanoBacktestFolder, StringComparison.OrdinalIgnoreCase)
            ? session.PinnedTitanoRunId
            : null;
        return ResolveRunIdForFolder(pinned, session.WorkspaceId, backtestFolder);
    }

    private string? ResolveRunIdForFolder(string? pinnedRunId, string workspaceId, string? backtestFolder)
    {
        if (string.IsNullOrWhiteSpace(backtestFolder))
            return null;
        if (!string.IsNullOrWhiteSpace(pinnedRunId))
            return pinnedRunId;
        return _titano?.ResolveLatestRun(workspaceId, backtestFolder)?.RunId;
    }

    private TitanoEffectiveStrategies? TryResolveGroupTitano(Session session, string groupId)
    {
        var profile = ResolveGroupTitano(session, groupId);
        if (string.IsNullOrWhiteSpace(profile.TitanoBacktestFolder) || _titano is null)
            return null;

        var runId = ResolveRunIdForFolder(session, profile.TitanoBacktestFolder);
        if (string.IsNullOrWhiteSpace(runId))
            return null;

        return _titano.Resolve(
            session.WorkspaceId, profile.TitanoBacktestFolder, runId,
            session.LastEvaluatedBarTimeUtc ?? DateTime.UtcNow);
    }

    private bool IsTemplateEligibleForGroup(Session session, string groupId, OrderIntent template)
    {
        // Un intent di chiusura non è mai un template da reclamare (il server non ne emette), ma se
        // ne arrivasse uno non va comunque filtrato: chiudere una posizione aperta è sempre lecito.
        if (template.IsClose)
            return true;

        var profile = ResolveGroupTitano(session, groupId);
        if (!profile.ApplyTitanoFilters || string.IsNullOrWhiteSpace(profile.TitanoBacktestFolder))
            return true;

        var effective = TryResolveGroupTitano(session, groupId);
        if (effective is null)
            return true;

        if (!effective.HasActivePeriod)
            return true;

        if (!effective.EffectiveStrategies.Contains(template.StrategyCode, StringComparer.OrdinalIgnoreCase))
            return false;

        var allocation = effective.StrategyStates
            .FirstOrDefault(s => string.Equals(s.StrategyCode, template.StrategyCode, StringComparison.OrdinalIgnoreCase))
            ?.AllocationMultiplier ?? 0m;
        return allocation > 0m;
    }

    private decimal GetGroupStrategyAllocation(Session session, string groupId, string strategyCode)
    {
        var profile = ResolveGroupTitano(session, groupId);
        if (!profile.ApplyTitanoFilters || string.IsNullOrWhiteSpace(profile.TitanoBacktestFolder))
            return 1m;

        // OpenFromPlan associa la stessa cartella sia alla sessione sia al suo unico gruppo, quindi
        // risolvono sempre allo stesso run in un dato istante. PushBars ha già applicato quel
        // moltiplicatore nel PositionSizingService: riapplicarlo qui trasformerebbe 0,5 in 0,25. Il
        // claim deve scalare solo per la cartella di un gruppo diverso da quella della sessione.
        if (session.TitanoMode != TitanoFilterMode.Disabled &&
            string.Equals(profile.TitanoBacktestFolder, session.TitanoBacktestFolder, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var effective = TryResolveGroupTitano(session, groupId);
        if (effective is null || !effective.HasActivePeriod)
            return 1m;

        return effective.StrategyStates
            .FirstOrDefault(s => string.Equals(s.StrategyCode, strategyCode, StringComparison.OrdinalIgnoreCase))
            ?.AllocationMultiplier ?? 0m;
    }

    private static OrderIntent AddIntent(
        Session session, TradeSignal signal, PositionSizingResult? sizing, bool addToIntents = true,
        AccountSymbolConversion? conversion = null)
    {
        // Identità sui template: il conto si conosce solo al claim (vedi CloneForClaim).
        conversion ??= AccountSymbolConversion.Identity;
        var sizeFactor = conversion.IsSymbolEnabled(signal.Symbol)
            ? conversion.GetSizeFactor(signal.Symbol)
            : 0m;
        var quantityBeforeConversion = sizing?.FinalQuantity ?? signal.Quantity;

        // Arrotondamento alla granularità del broker (o al contratto intero se il simbolo non è
        // mappato, vedi AccountSymbolConversion.RoundQuantity): serve qui perché sui template
        // (conversion = Identity) è l'unico punto in cui un intent in esecuzione diretta — nessun
        // gruppo, nessun claim successivo — riceve mai un arrotondamento quando
        // RoundingMode è Deferred (ExternalBroker). Su un template vero e proprio l'arrotondamento
        // qui è quello di default (contratto intero) ed è coerente con quanto CloneForClaim farà poi
        // con la granularità reale del conto.
        var roundedQuantity = conversion.RoundQuantity(signal.Symbol, signal.Quantity * sizeFactor);
        var roundedFinalQuantity = conversion.RoundQuantity(signal.Symbol, quantityBeforeConversion * sizeFactor);

        var strategyTimeframe = session.Strategies
            .FirstOrDefault(strategy => string.Equals(
                strategy.Name, signal.StrategyCode, StringComparison.OrdinalIgnoreCase))
            ?.TimeframeMinutes ?? 0;
        var dollarsPerPoint = session.InstrumentMetadata.TryGetValue(Normalize(signal.Symbol), out var metadata)
            ? metadata.DollarsPerPoint
            : 1m;
        if (dollarsPerPoint <= 0m)
            dollarsPerPoint = 1m;

        var stopLossPoints = signal.StopLoss
            ?? (signal.StopLossMoneyPerFutureContract.HasValue
                ? signal.StopLossMoneyPerFutureContract.Value / dollarsPerPoint
                : null);
        var takeProfitPoints = signal.TakeProfit
            ?? (signal.TakeProfitMoneyPerFutureContract.HasValue
                ? signal.TakeProfitMoneyPerFutureContract.Value / dollarsPerPoint
                : null);
        var trailingStopPoints = signal.TrailingStopMoneyPerFutureContract.HasValue
            ? (decimal?)(signal.TrailingStopMoneyPerFutureContract.Value / dollarsPerPoint)
            : null;
        var breakEvenPoints = signal.BreakEvenMoneyPerFutureContract.HasValue
            ? (decimal?)(signal.BreakEvenMoneyPerFutureContract.Value / dollarsPerPoint)
            : signal.BreakEven;

        session.IntentSequence++;
        var intent = new OrderIntent
        {
            IntentId = $"{session.Id}-{session.IntentSequence:D10}",
            SessionId = session.Id,
            StrategyCode = signal.StrategyCode,
            StrategyName = signal.StrategyName,
            Symbol = Normalize(signal.Symbol),
            // Sui template la conversione è l'identità: il simbolo del broker e i fattori arrivano
            // in CloneForClaim, quando l'account è noto.
            AccountSymbol = conversion.GetAccountSymbol(Normalize(signal.Symbol)),
            AccountId = conversion.AccountId,
            ContractMultiplier = conversion.GetContractMultiplier(signal.Symbol),
            AccountBalanceScale = conversion.BalanceScale,
            CreatedAtUtc = signal.Date,
            Side = signal.Type,
            OrderType = signal.OrderType,
            Quantity = roundedQuantity,
            QuantityBeforeAccountConversion = quantityBeforeConversion,
            AllocationMultiplier = sizing?.StrategyEquityMultiplier ?? 1m,
            BaseQuantity = sizing?.BaseQuantity ?? signal.Quantity,
            StrategyEquityMultiplier = sizing?.StrategyEquityMultiplier ?? 1m,
            MarketVolatilityMultiplier = sizing?.MarketVolatilityMultiplier ?? 1m,
            PortfolioRiskMultiplier = sizing?.PortfolioRiskMultiplier ?? 1m,
            FinalQuantity = roundedFinalQuantity,
            SizingReason = sizeFactor == 1m
                ? sizing?.Reason
                : $"{sizing?.Reason} | conversione account: {sizeFactor:0.######}",
            Price = signal.Price,
            Kind = OrderIntentKind.Entry,
            // Specifica di uscita completa: e' l'unica cosa con cui il client chiudera' la posizione.
            StopLoss = stopLossPoints,
            TakeProfit = takeProfitPoints,
            BreakEven = breakEvenPoints,
            TrailingStop = trailingStopPoints,
            TimeframeMinutes = strategyTimeframe,
            MaxBarsInPosition = signal.MaxBarsInPosition,
            MaxEntriesPerSession = signal.MaxEntriesPerSession,
            EntrySessionStartUtc = signal.EntrySessionStartUtc,
            ValidFromUtc = signal.ValidFromUtc,
            ExpiresAtUtc = signal.ExpiresAtUtc,
            CloseAtUtc = signal.CloseAtUtc,
            TimeExitOnlyIfProfitBelowMoneyPerContract = signal.TimeExitOnlyIfProfitBelowMoneyPerContract,
            ProfitStallAfterUtc = signal.ProfitStallAfterUtc,
            Reason = signal.Reason
        };
        if (addToIntents) session.Intents.Add(intent);
        return intent;
    }

    /// <summary>
    /// Vero quando il limite di fill dichiarato dalla strategia per la sessione di
    /// <see cref="OrderIntent.EntrySessionStartUtc"/> è già stato raggiunto.
    ///
    /// <para>Si contano i <b>fill confermati</b>, non gli intent emessi: è la stessa semantica del
    /// motore simulato, dove uno stop non eseguito viene riemesso nella stessa sessione. Il
    /// conteggio è per account quando l'account è noto (multi-account), globale altrimenti.</para>
    /// </summary>
    /// <summary>
    /// Quante cose la sessione potrebbe consegnare a un claim: template di ingresso pendenti e non
    /// scaduti, più gli intent già assegnati e ancora pendenti. Zero significa che
    /// <see cref="GetNextSignalForAccount"/> risponderebbe a vuoto per qualunque account, ed è la
    /// garanzia su cui il cBot si permette di saltare il poll.
    ///
    /// <para>Deliberatamente <b>più largo</b> del claim vero: non applica i lucchetti di gruppo, il
    /// filtro Titano né la tabella di conversione dell'account, perché qui non si sta decidendo
    /// <i>chi</i> prende <i>cosa</i> — si sta solo dicendo se c'è qualcosa. Sbagliare per eccesso
    /// costa un poll a vuoto; sbagliare per difetto perde un segnale, ed è il verso in cui non si
    /// può sbagliare.</para>
    ///
    /// <para>"Adesso" è l'ultima barra valutata, come nel claim: in un replay storico l'ora di
    /// sistema dista mesi e farebbe risultare scaduto ogni template.</para>
    /// </summary>
    private static int CountClaimableIntents(Session session)
    {
        var now = session.LastEvaluatedBarTimeUtc ?? DateTime.UtcNow;
        var templates = session.EntryTemplates.Count(t =>
            t.Status == OrderIntentStatus.Pending &&
            (!t.ExpiresAtUtc.HasValue || t.ExpiresAtUtc.Value >= now));
        var assigned = session.Intents.Count(i =>
            i.Status == OrderIntentStatus.Pending && i.AssignedAccountNumber is not null);
        return templates + assigned;
    }

    /// <summary>
    /// L'account ha già un ingresso "in volo" per quella coppia (strategia, simbolo)? Conta sia un
    /// intent di ingresso ancora <c>Pending</c> — ordine piazzato sul broker e non ancora riempito o
    /// annullato — sia una posizione aperta.
    ///
    /// <para>È il complemento di <see cref="MaxEntriesPerSessionReached"/>, che conta i fill e quindi
    /// non vede gli ordini in attesa: fra il claim e il fill c'è una finestra in cui due template
    /// della stessa strategia, nati su barre diverse, sono entrambi ammissibili.</para>
    /// </summary>
    private static bool AccountHasEntryInFlight(
        Session session, string accountNumber, string strategyCode, string symbol)
    {
        var pending = session.Intents.Any(intent =>
            intent.Kind == OrderIntentKind.Entry &&
            intent.Status == OrderIntentStatus.Pending &&
            string.Equals(intent.AssignedAccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(intent.StrategyCode, strategyCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(intent.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
        if (pending)
            return true;

        return session.ExternalPositions.Values.Any(position =>
            string.Equals(position.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(position.StrategyCode, strategyCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(position.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MaxEntriesPerSessionReached(Session session, OrderIntent intent, string? accountNumber)
    {
        if (intent.MaxEntriesPerSession is not > 0 || intent.EntrySessionStartUtc is not { } sessionStart)
            return false;

        var fills = session.Intents.Count(x =>
            x.Kind == OrderIntentKind.Entry &&
            x.FilledQuantity > 0 &&
            x.EntrySessionStartUtc == sessionStart &&
            string.Equals(x.StrategyCode, intent.StrategyCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Symbol, intent.Symbol, StringComparison.OrdinalIgnoreCase) &&
            (accountNumber is null ||
             string.Equals(x.AssignedAccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase)));

        return fills >= intent.MaxEntriesPerSession.Value;
    }

    /// <summary>
    /// Il motivo di rifiuto del limite di ingressi per sessione, con dentro i NUMERI del conteggio
    /// invece del solo esito.
    ///
    /// <para><b>Perche' esiste.</b> <see cref="MaxEntriesPerSessionReached"/> confronta cinque cose
    /// — secchio del template, secchio dell'intent riempito, <c>FilledQuantity</c>, account
    /// assegnato e il tetto dichiarato dalla strategia — e restituisce un bool. Quando il verdetto
    /// e' quello sbagliato, dal log non si puo' dire quale confronto sia saltato, e le cinque
    /// ipotesi portano a cinque correzioni diverse. Il caso aperto: nei backtest del 06/08 e 08/08
    /// un template di <c>PTS_NQ_PCH_002_15</c> passa alle 17:00 UTC dopo che la stessa strategia ha
    /// gia' riempito nello stesso giorno di calendario, che e' il secchio dichiarato dal motore
    /// (<c>SessionKey</c> con SessionStartTime=0 restituisce la mezzanotte UTC).</para>
    ///
    /// <para>Oltre al conteggio nel secchio del template si stampano <b>i secchi in cui quella
    /// strategia ha davvero dei fill</b>: se non contengono il secchio del template, il
    /// disallineamento e' li' e si legge senza altre indagini. Un fill attribuito a un altro
    /// account viene marcato con <c>@numero</c>, cosi' anche quel caso si distingue.</para>
    ///
    /// <para>E' solo diagnostica: non cambia quali template passano. La deduplica per stringa di
    /// <see cref="RecordRefusal"/> regge, perche' questi valori cambiano una volta al giorno e non
    /// a ogni barra — il motivo per cui il filtro di scadenza, poco sopra, tiene invece il testo
    /// fisso.</para>
    /// </summary>
    private static string DescribeSessionLimit(
        Session session, IReadOnlyList<OrderIntent> candidates, string accountNumber)
    {
        const string testa = "limite di ingressi per sessione raggiunto";
        if (candidates.Count == 0)
            return testa;

        var dettagli = candidates
            .Select(t => new { t.StrategyCode, t.Symbol, Secchio = t.EntrySessionStartUtc, Tetto = t.MaxEntriesPerSession })
            .Distinct()
            .Select(c =>
            {
                var fillNelSecchio = session.Intents.Count(x =>
                    x.Kind == OrderIntentKind.Entry &&
                    x.FilledQuantity > 0 &&
                    x.EntrySessionStartUtc == c.Secchio &&
                    string.Equals(x.StrategyCode, c.StrategyCode, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Symbol, c.Symbol, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.AssignedAccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase));

                var secchiConFill = session.Intents
                    .Where(x => x.Kind == OrderIntentKind.Entry &&
                                x.FilledQuantity > 0 &&
                                string.Equals(x.StrategyCode, c.StrategyCode, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(x.Symbol, c.Symbol, StringComparison.OrdinalIgnoreCase))
                    .Select(x => Secchio(x.EntrySessionStartUtc) +
                                 (string.Equals(x.AssignedAccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase)
                                     ? string.Empty
                                     : $"@{x.AssignedAccountNumber ?? "n/d"}"))
                    .Distinct()
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                return $"{c.StrategyCode}/{c.Symbol} secchio {Secchio(c.Secchio)} " +
                       $"fill {fillNelSecchio}/{(c.Tetto?.ToString() ?? "n/d")}" +
                       (secchiConFill.Count == 0
                           ? ", nessun fill della strategia"
                           : $", fill in {string.Join(" ", secchiConFill)}");
            })
            .ToList();

        return $"{testa} ({string.Join(" | ", dettagli)})";

        static string Secchio(DateTime? valore) =>
            valore is { } v ? v.ToString("yyyy-MM-dd HH:mm") + "Z" : "nessuno";
    }

    /// <summary>
    /// Clona un template di ingresso in un intent concreto assegnato a un account/gruppo specifico,
    /// applicando l'allocazione Titano <b>del gruppo</b>: gruppi diversi possono avere run distinti e
    /// quindi ricevere lo stesso segnale con size diverse.
    ///
    /// <para>La quantità viene arrotondata per difetto al passo dello strumento e azzerata sotto la
    /// quantità minima: un intent con <c>FinalQuantity</c> a zero non viene consegnato all'account
    /// (vedi il chiamante). Meglio nessun ordine che un ordine di taglia non eseguibile.</para>
    ///
    /// <para>L'intera specifica di uscita viaggia con il clone: è l'unica cosa con cui il client
    /// chiuderà la posizione.</para>
    /// </summary>
    private OrderIntent CloneForClaim(Session session, OrderIntent template, string accountNumber, string groupId)
    {
        var groupAllocation = GetGroupStrategyAllocation(session, groupId, template.StrategyCode);
        var quantity = ApplyGroupAllocation(session, template.Symbol, template.FinalQuantity, groupAllocation);

        // La conversione dell'account è l'ultimo passaggio, ed è qui e non sul template perché
        // dipende dal conto: lo stesso segnale vale size diverse su conti con capitale o contratto
        // diversi. L'arrotondamento avviene dopo la conversione, con la granularità del broker
        // (MinimumQuantity/QuantityStep/RoundingMode sulla riga della tabella di conversione): prima
        // della conversione la quantità è ancora nei contratti Piootoo, dove quel passo non significa
        // nulla. Vedi docs/decisioni.md (2026-08-05).
        var conversion = ResolveAccountConversion(session, accountNumber);
        var enabled = conversion.IsSymbolEnabled(template.Symbol);
        var sizeFactor = conversion.GetSizeFactor(template.Symbol);
        var convertedQuantity = enabled
            ? conversion.RoundQuantity(template.Symbol, quantity * sizeFactor)
            : 0m;

        return new OrderIntent
        {
            IntentId = $"{template.IntentId}::{groupId}",
            SessionId = template.SessionId,
            StrategyCode = template.StrategyCode,
            StrategyName = template.StrategyName,
            Symbol = template.Symbol,
            AccountSymbol = conversion.GetAccountSymbol(template.Symbol),
            AccountId = conversion.AccountId,
            ContractMultiplier = conversion.GetContractMultiplier(template.Symbol),
            AccountBalanceScale = conversion.BalanceScale,
            CreatedAtUtc = template.CreatedAtUtc,
            Side = template.Side,
            OrderType = template.OrderType,
            Quantity = convertedQuantity,
            QuantityBeforeAccountConversion = quantity,
            AllocationMultiplier = template.AllocationMultiplier * groupAllocation,
            BaseQuantity = template.BaseQuantity,
            StrategyEquityMultiplier = template.StrategyEquityMultiplier * groupAllocation,
            MarketVolatilityMultiplier = template.MarketVolatilityMultiplier,
            PortfolioRiskMultiplier = template.PortfolioRiskMultiplier,
            FinalQuantity = convertedQuantity,
            SizingReason = BuildClaimSizingReason(template.SizingReason, groupId, groupAllocation, enabled, sizeFactor),
            Price = template.Price,
            Kind = OrderIntentKind.Entry,
            StopLoss = template.StopLoss,
            TakeProfit = template.TakeProfit,
            BreakEven = template.BreakEven,
            TrailingStop = template.TrailingStop,
            TimeframeMinutes = template.TimeframeMinutes,
            MaxBarsInPosition = template.MaxBarsInPosition,
            MaxEntriesPerSession = template.MaxEntriesPerSession,
            EntrySessionStartUtc = template.EntrySessionStartUtc,
            ValidFromUtc = template.ValidFromUtc,
            ExpiresAtUtc = template.ExpiresAtUtc,
            CloseAtUtc = template.CloseAtUtc,
            TimeExitOnlyIfProfitBelowMoneyPerContract = template.TimeExitOnlyIfProfitBelowMoneyPerContract,
            ProfitStallAfterUtc = template.ProfitStallAfterUtc,
            Reason = template.Reason,
            AssignedAccountNumber = accountNumber,
            AssignedGroupId = groupId
        };
    }

    /// <summary>
    /// Scala una quantità per l'allocazione di gruppo rispettando i vincoli dello strumento:
    /// arrotondamento per difetto al passo, zero sotto la quantità minima.
    ///
    /// <para>Se <see cref="InstrumentMetadata.RoundingMode"/> è <see cref="QuantityRoundingMode.Deferred"/>
    /// non si arrotonda qui: è il caso <see cref="ExecutionMode.ExternalBroker"/>, dove la quantità è
    /// ancora nei contratti Piootoo e la granularità che conta è quella del broker, applicata una
    /// sola volta dopo la conversione d'account (vedi <see cref="CloneForClaim"/>).</para>
    /// </summary>
    private decimal ApplyGroupAllocation(Session session, string symbol, decimal quantity, decimal allocation)
    {
        if (allocation >= 1m) return quantity;
        if (allocation <= 0m) return 0m;

        var scaled = quantity * allocation;
        if (!session.InstrumentMetadata.TryGetValue(Normalize(symbol), out var metadata) ||
            metadata.RoundingMode == QuantityRoundingMode.Deferred)
            return scaled;

        if (metadata.QuantityStep > 0)
            scaled = Math.Floor(scaled / metadata.QuantityStep) * metadata.QuantityStep;

        return scaled < metadata.MinimumQuantity ? 0m : scaled;
    }

    /// <summary>
    /// Tabella di conversione dell'account che opera con questo numero di conto, presa dal registro
    /// globale e memorizzata sulla sessione. Un conto configurato sulla sessione ma assente dal
    /// registro è un errore esplicito: senza tabella si opererebbe 1 a 1 con la size di un conto da
    /// un milione, che è il tipo di errore silenzioso che il progetto non ammette.
    /// </summary>
    private AccountSymbolConversion ResolveAccountConversion(Session session, string accountNumber)
    {
        if (session.AccountConversions.TryGetValue(accountNumber, out var cached))
            return cached;

        var account = _workspaces.ListAccounts().FirstOrDefault(candidate =>
            string.Equals(candidate.AccountNumber?.Trim(), accountNumber.Trim(), StringComparison.OrdinalIgnoreCase));
        if (account is null)
            throw new InvalidOperationException(
                $"Account '{accountNumber}' non presente nel registro account: impossibile risolvere " +
                "capitale e tabella di conversione simboli. Creane l'anagrafica prima di operare.");

        var mappings = _workspaces.ResolveSymbolConversionMappings(account.SymbolConversionCode);
        var conversion = AccountSymbolConversion.FromAccount(account, mappings);
        session.AccountConversions[accountNumber] = conversion;
        return conversion;
    }

    /// <summary>
    /// Conversione da applicare agli intent che nascono già assegnati (esecuzione diretta).
    /// Null-safe: nelle sessioni distribuite o simulate non c'è un conto a cui riferirsi.
    /// </summary>
    private AccountSymbolConversion? ResolveDirectConversion(Session session) =>
        session.DirectAccountNumber is { } accountNumber
            ? ResolveAccountConversion(session, accountNumber)
            : null;

    private static string? BuildClaimSizingReason(
        string? templateReason, string groupId, decimal groupAllocation, bool symbolEnabled, decimal sizeFactor)
    {
        var reason = groupAllocation == 1m
            ? templateReason
            : $"{templateReason} | allocazione gruppo {groupId}: {groupAllocation:0.###}";

        if (!symbolEnabled)
            return $"{reason} | simbolo non operativo sull'account";

        return sizeFactor == 1m
            ? reason
            : $"{reason} | conversione account: {sizeFactor:0.######}";
    }

    private static string SlotKey(string groupId, string strategyCode, string symbol) =>
        $"{groupId}|{strategyCode}|{Normalize(symbol)}";

    private static void Persist(Session session)
    {
        session.Store.WriteSignals(session.Intents.Select(intent => new PersistedSignal
        {
            SignalId = intent.IntentId,
            IntentId = intent.IntentId,
            CorrelationId = intent.IntentId,
            SessionId = session.Id,
            TimestampUtc = intent.CreatedAtUtc,
            StrategyCode = intent.StrategyCode,
            StrategyName = string.IsNullOrWhiteSpace(intent.StrategyName)
                ? intent.StrategyCode
                : intent.StrategyName,
            Symbol = intent.Symbol,
            AccountId = intent.AccountId,
            AccountSymbol = intent.AccountSymbol,
            ContractMultiplier = intent.ContractMultiplier,
            AccountBalanceScale = intent.AccountBalanceScale,
            Side = intent.Side,
            OrderType = intent.OrderType,
            TriggerPrice = intent.Price,
            Quantity = intent.Quantity,
            QuantityBeforeAccountConversion = intent.QuantityBeforeAccountConversion,
            BaseQuantity = intent.BaseQuantity,
            StrategyEquityMultiplier = intent.StrategyEquityMultiplier,
            MarketVolatilityMultiplier = intent.MarketVolatilityMultiplier,
            PortfolioRiskMultiplier = intent.PortfolioRiskMultiplier,
            FinalQuantity = intent.FinalQuantity,
            SizingReason = intent.SizingReason,
            ValidFromUtc = intent.ValidFromUtc,
            ExpiresAtUtc = intent.ExpiresAtUtc,
            StopLoss = intent.StopLoss,
            TakeProfit = intent.TakeProfit,
            BreakEven = intent.BreakEven,
            TrailingStop = intent.TrailingStop,
            TimeframeMinutes = intent.TimeframeMinutes,
            TimeExitUtc = intent.CloseAtUtc,
            Reason = intent.Reason,
            MaxBarsInPosition = intent.MaxBarsInPosition,
            IsClose = intent.IsClose,
            Status = intent.Status,
            FilledQuantity = intent.FilledQuantity,
            ExternalOrderId = intent.ExternalOrderId,
            AssignedAccountNumber = intent.AssignedAccountNumber,
            AssignedGroupId = intent.AssignedGroupId
        }));

        var trades = session.Mode == ExecutionMode.ExternalBroker
            ? session.ExternalTrades
            : session.SimulatedEngine.GetClosedTrades().Select((trade, index) => new PersistedTrade
            {
                TradeId = $"{session.Id}-trade-{index + 1:D10}",
                SessionId = session.Id,
                CorrelationId = session.Id,
                StrategyCode = trade.StrategyCode,
                StrategyName = trade.StrategyName,
                Symbol = trade.Symbol,
                Direction = trade.Direction,
                Quantity = trade.Quantity,
                EntryTimeUtc = TradingDateTime.ToFeedUtc(trade.EntryDate),
                ExitTimeUtc = TradingDateTime.ToFeedUtc(trade.ExitDate),
                EntryPrice = trade.EntryPrice,
                ExitPrice = trade.ExitPrice,
                ExitReason = trade.ExitReason.ToString(),
                GrossProfit = trade.GrossProfit,
                NetProfit = trade.NetProfit,
                Commission = trade.Commission
            }).ToList();
        session.Store.WriteTrades(trades);
        session.Store.WriteRotationLog(session.RotationLog);
    }

    /// <summary>
    /// Costruisce la riga di log diagnostico per la barra corrente, incrociando lo stato Titano
    /// (chi è stato incluso/escluso e perché) con i segnali effettivamente generati dalle strategie
    /// valutate. Serve a verificare che le esclusioni Titano corrispondano a strategie che non hanno
    /// generato trade, e viceversa che le strategie incluse si comportino come progettato.
    /// </summary>
    private static RotationLogEntry BuildRotationLogEntry(
        Session session, DateTime barTimeUtc, TitanoEffectiveStrategies effective,
        IReadOnlyList<ITradingStrategy> evaluationStrategies, IReadOnlyList<TradeSignal> signals,
        string? note = null)
    {
        var masterStrategies = session.Strategies.Select(s => s.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
        var evaluatedNames = evaluationStrategies.Select(s => s.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
        var skipped = masterStrategies
            .Where(x => !evaluatedNames.Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        var statesByCode = effective.StrategyStates.ToDictionary(x => x.StrategyCode, StringComparer.OrdinalIgnoreCase);
        var strategyStates = masterStrategies.Select(code =>
        {
            statesByCode.TryGetValue(code, out var state);
            return new RotationStrategyState
            {
                StrategyCode = code,
                Included = evaluatedNames.Contains(code, StringComparer.OrdinalIgnoreCase),
                AllocationMultiplier = state?.AllocationMultiplier ?? 0m,
                State = state?.State.ToString(),
                HardStopped = state?.HardStopped ?? false,
                CooldownRemaining = state?.CooldownRemaining ?? 0,
                Score = state?.Score ?? 0m,
                PassingFilters = state?.PassingFilters ?? 0,
                TotalFilters = state?.TotalFilters ?? 0,
                Reason = state?.Reason ?? "strategia assente dal run Titano corrente"
            };
        }).ToArray();

        return new RotationLogEntry
        {
            EntryId = $"{session.Id}-{barTimeUtc:yyyyMMddTHHmmssfffZ}",
            SessionId = session.Id,
            BarTimeUtc = barTimeUtc,
            TitanoRunId = effective.RunId,
            TitanoBacktestFolder = session.TitanoBacktestFolder,
            PeriodId = effective.PeriodId,
            MasterStrategies = masterStrategies,
            EvaluatedStrategies = evaluatedNames,
            SkippedByTitano = skipped,
            StrategyStates = strategyStates,
            SignalsEmitted = signals.Select(s => $"{s.StrategyCode}:{s.Type}").ToArray(),
            FiltersApplied = session.TitanoMode != TitanoFilterMode.Disabled && effective.HasActivePeriod,
            TitanoMode = session.TitanoMode,
            ClientRunMode = session.ClientRunMode,
            Note = note
        };
    }

    private static StrategyExecutionSnapshot GetExecution(Session session, ITradingStrategy strategy, DateTime time)
    {
        if (session.Mode == ExecutionMode.ServerSimulated)
            return session.SimulatedEngine.GetExecutionSnapshot(strategy.Name, strategy.Symbol, time);
        var key = $"{Normalize(strategy.Symbol)}|{strategy.Name}";
        // In modalità multi-account la valutazione strategie usa la posizione "canonica" (indipendente
        // da quale account la detiene realmente); in modalità legacy usa le posizioni dirette come prima.
        var positions = session.AccountGroups.Count > 0 ? session.CanonicalPositions : session.ExternalPositions;
        positions.TryGetValue(key, out var position);
        return new StrategyExecutionSnapshot
        {
            StrategyCode = strategy.Name,
            Symbol = Normalize(strategy.Symbol),
            BarTimeUtc = time,
            EntriesToday = session.Entries,
            Position = position is null ? null : new StrategyPositionSnapshot
            {
                Direction = position.Direction,
                EntryPrice = position.EntryPrice,
                EntryTimeUtc = time,
                Contracts = (int)position.Quantity
            }
        };
    }

    private Session Get(string id, string token)
    {
        if (!_sessions.TryGetValue(id, out var session)) throw new KeyNotFoundException($"Sessione '{id}' non trovata.");
        if (!CryptographicEquals(session.Token, token)) throw new UnauthorizedAccessException("Session token non valido.");
        return session;
    }

    private static bool CryptographicEquals(string left, string right) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(left),
            System.Text.Encoding.UTF8.GetBytes(right ?? string.Empty));

    private TradingSessionDescriptor Describe(Session session)
    {
        // In esecuzione diretta la conversione è già in cache (risolta all'apertura): il descriptor
        // dice al client con che nome vedrà sul proprio grafico ogni strumento del piano.
        var conversion = session.DirectAccountNumber is { } account
                         && session.AccountConversions.TryGetValue(account, out var resolved)
            ? resolved
            : AccountSymbolConversion.Identity;
        return Describe(session, conversion, session.DirectAccountNumber);
    }

    private TradingSessionDescriptor Describe(
        Session session, AccountSymbolConversion conversion, string? accountNumber = null) => new()
    {
        SessionId = session.Id,
        SessionToken = session.Token,
        WorkspaceId = session.WorkspaceId,
        PlanCode = session.PlanCode,
        ExecutionKey = session.ExecutionKey,
        ExecutionMode = session.Mode,
        Status = session.Status,
        // Informativo per il client (diagnosi locale): calcolato al momento, non congelato
        // sulla sessione, così riflette una rotazione più recente senza bisogno di riaprirla.
        TitanoRunId = ResolveRunIdForFolder(session, session.TitanoBacktestFolder),
        TitanoMode = session.TitanoMode,
        ClientRunMode = session.ClientRunMode,
        RunProfile = session.RunProfile,
        EnforceConcurrencyLimits = session.EnforceConcurrencyLimits,
        // Il limite è per account: senza destinatario noto (elenco sessioni, sessione distribuita
        // descritta fuori da un'apertura) si riporta 0, che il client legge come "non dichiarato".
        MaxConcurrentTrades = accountNumber is not null
            ? session.AccountMaxConcurrentTrades.GetValueOrDefault(accountNumber)
            : 0,
        // Il client la legge per sapere se, raggiunto il tetto sulle posizioni, deve cancellare i
        // propri ordini pendenti rimasti. È configurazione consegnata all'apertura, non un canale
        // di controllo: il server non gli dirà mai "cancella quell'ordine".
        ConcurrencyCountMode = accountNumber is not null
            ? session.AccountConcurrencyCountMode.GetValueOrDefault(accountNumber)
            : default,
        // Ordinate per simbolo/timeframe/codice: il pannello a chart le stampa così com'è, e un
        // ordine stabile rende confrontabili a colpo d'occhio due run diversi.
        Strategies = session.Strategies
            .Select(s => new TradingSessionStrategyInfo
            {
                StrategyCode = s.Name,
                Symbol = Normalize(s.Symbol),
                TimeframeMinutes = s.TimeframeMinutes
            })
            .OrderBy(s => s.Symbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.TimeframeMinutes)
            .ThenBy(s => s.StrategyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray(),
        PositionSizing = session.PositionSizing,
        InstrumentMetadata = session.InstrumentMetadata.Values.OrderBy(x => x.Symbol).ToArray(),
        Instruments = session.Strategies.GroupBy(s => Normalize(s.Symbol))
            .Select(g => new TradingInstrument
            {
                Symbol = g.Key,
                AccountSymbol = conversion.GetAccountSymbol(g.Key),
                TimeframesMinutes = g.Select(x => x.TimeframeMinutes).Distinct().Order().ToArray(),
                // Quanta storia serve al server per valutare quello stream: il client la usa per
                // sapere quante candele caricare dal broker e quanto profonda spedire la finestra.
                RequiredCandlesByTimeframe = g.GroupBy(x => x.TimeframeMinutes)
                    .ToDictionary(tf => tf.Key, tf => tf.Max(x => x.RequiredCandles))
            }).ToArray()
    };

    private static TradingSessionSnapshot Snapshot(Session session)
    {
        var simulation = session.SimulatedEngine.GetSnapshot();
        return new TradingSessionSnapshot
        {
            SessionId = session.Id,
            ExecutionMode = session.Mode,
            Status = session.Status,
            // In ExternalBroker il server non tiene il conto: balance ed equity sono del broker, e
            // dichiararli qui significherebbe inventare un numero (prima si ripeteva il capitale
            // iniziale, costante per tutta la sessione). Zero dice "non lo so", che è la verità.
            Balance = session.Mode == ExecutionMode.ServerSimulated ? simulation.Balance : 0m,
            Equity = session.Mode == ExecutionMode.ServerSimulated ? simulation.Equity : 0m,
            Entries = session.Mode == ExecutionMode.ExternalBroker ? session.Entries : 0,
            Fills = session.Mode == ExecutionMode.ExternalBroker ? session.Fills : 0,
            Positions = session.ExternalPositions.Values.ToArray(),
            PendingIntents = session.Intents.Where(x => x.Status is OrderIntentStatus.Pending
                or OrderIntentStatus.Accepted or OrderIntentStatus.PartiallyFilled).ToArray(),
            AccountGroups = session.AccountGroups
                .Select(kv => new AccountGroupMapping { AccountNumber = kv.Key, GroupId = kv.Value })
                .OrderBy(x => x.GroupId).ThenBy(x => x.AccountNumber).ToArray(),
            Groups = BuildTradingGroupRows(session)
        };
    }

    private static ClosedBar CloneUtc(ClosedBar bar)
    {
        bar.Bar.DateTime = bar.BarTimeUtc;
        return bar;
    }

    private static void ValidateBar(ClosedBar bar)
    {
        if (string.IsNullOrWhiteSpace(bar.Symbol) || bar.Symbol.Contains('/') || bar.Symbol.Contains('\\'))
            throw new ArgumentException("Symbol non valido.");
        if (bar.TimeframeMinutes <= 0 || bar.Sequence < 0 || string.IsNullOrWhiteSpace(bar.IdempotencyKey))
            throw new ArgumentException("Timeframe, sequence e idempotency key sono obbligatori.");
        RequireUtc(bar.BarTimeUtc, nameof(bar.BarTimeUtc));
    }

    private static void RequireUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc) throw new ArgumentException($"{name} deve essere UTC.");
    }

    private static string StreamKey(string symbol, int timeframe) => $"{Normalize(symbol)}|{timeframe}";
    private static string Normalize(string value) => value.Trim().TrimStart('@').ToUpperInvariant();
}
