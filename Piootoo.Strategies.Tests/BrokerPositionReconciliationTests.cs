using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Riconciliazione fra <c>ExternalPositions</c> e lo snapshot di posizioni aperte che il cBot manda
/// a ogni poll.
///
/// <para><b>Cosa protegge.</b> Una posizione chiusa dal broker per conto suo — stop loss nativo,
/// stop out, chiusura manuale — non passa da nessun intent del server. Se il client non riesce a
/// riportarla, la voce resta in <c>ExternalPositions</c>; e siccome <c>AccountHasEntryInFlight</c>
/// ci legge dentro ed è un lucchetto sempre attivo, quella coppia (strategia, simbolo) non apre più
/// niente per il resto del run. Sul run GC del 2014 è costato tre strategie su quattro.</para>
///
/// <para><b>Perché la mappatura dei simboli è il punto.</b> Le chiavi di <c>ExternalPositions</c>
/// portano il simbolo Piootoo, lo snapshot porta quello del broker: il cBot riempie
/// <c>Symbol</c> con <c>Position.SymbolName</c>. Confrontarli direttamente non produce un errore,
/// produce <b>zero corrispondenze</b> — cioè una riconciliazione che non riconcilia mai e non lo
/// dice. È esattamente come è nata rotta la prima versione, e per questo qui l'account ha sempre una
/// tabella di conversione con due nomi diversi: un test che opera 1 a 1 non vedrebbe il bug.</para>
/// </summary>
public sealed class BrokerPositionReconciliationTests : IDisposable
{
    private const string BrokerSymbol = "XAUUSD";
    private const string Account = "1001";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-reconcile-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void PosizioneSparitaDalBroker_VieneChiusaNeiRegistri_ELaStrategiaTornaLibera()
    {
        var (sessions, descriptor, strategy) = Session();
        var intent = OpenPosition(sessions, descriptor);

        // Il broker la conferma: da qui in poi la sua assenza vuol dire qualcosa.
        var confirmed = Poll(sessions, descriptor, WithPosition(intent.StrategyCode));
        Assert.Single(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);
        Assert.Null(confirmed.Intent);

        // Sparita dallo snapshot: chiusa dal broker senza che nessuno l'abbia riportata.
        Poll(sessions, descriptor, NoPositions());

        Assert.Empty(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);

        // E la strategia torna a poter operare: è questo che il bug impediva.
        sessions.PushBars(Bars(descriptor, strategy, Utc(2026, 1, 6)));
        var again = Poll(sessions, descriptor, NoPositions());
        Assert.NotNull(again.Intent);
        Assert.Equal(intent.StrategyCode, again.Intent!.StrategyCode);
    }

    [Fact]
    public void PosizioneMaiConfermataDalBroker_NonVieneCancellata()
    {
        // La corsa vera: il poll che arriva fra il report di fill e la registrazione della posizione
        // sulla piattaforma. Senza la conferma preventiva, la riconciliazione cancellerebbe una
        // posizione appena aperta e il server perderebbe di vista un'esposizione reale.
        var (sessions, descriptor, _) = Session();
        OpenPosition(sessions, descriptor);

        Poll(sessions, descriptor, NoPositions());

        Assert.Single(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);
    }

    [Fact]
    public void LoSnapshotUsaIlSimboloDelBroker_ELaPosizioneRestaConfermata()
    {
        // Guardia diretta sul bug di mappatura: lo snapshot dichiara XAUUSD, la posizione sul server
        // è sul simbolo Piootoo. Se il confronto tornasse a farsi sui nomi grezzi, questo poll non
        // confermerebbe niente e quello successivo non avrebbe nulla da riconciliare — il test
        // sopra passerebbe comunque, questo no.
        var (sessions, descriptor, _) = Session();
        var intent = OpenPosition(sessions, descriptor);

        var position = Assert.Single(
            sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);
        Assert.NotEqual(BrokerSymbol, position.Symbol);

        Poll(sessions, descriptor, WithPosition(intent.StrategyCode));
        Poll(sessions, descriptor, NoPositions());

        Assert.Empty(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);
    }

    [Fact]
    public void SnapshotWithADifferentPositionOnTheSamePair_StillClosesTheRegisteredOne()
    {
        // La coppia (simbolo, strategia) c'è ancora nello snapshot, ma è un'ALTRA posizione: la
        // nostra è chiusa e va tolta. Confrontando le coppie la risposta è "c'è", e la voce resta
        // nei registri per sempre — è la posizione fantasma che questa riconciliazione esiste per
        // uccidere, e che il confronto per coppia non vede quando la strategia rientra subito.
        var (sessions, descriptor, _) = Session();
        var intent = OpenPosition(sessions, descriptor, brokerPositionId: "77");

        Poll(sessions, descriptor, WithPosition(intent.StrategyCode, positionId: "77"));
        Poll(sessions, descriptor, WithPosition(intent.StrategyCode, positionId: "78"));

        Assert.Empty(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);
    }

    [Fact]
    public void SnapshotWithTheSamePositionId_KeepsThePositionOpen()
    {
        var (sessions, descriptor, _) = Session();
        var intent = OpenPosition(sessions, descriptor, brokerPositionId: "77");

        Poll(sessions, descriptor, WithPosition(intent.StrategyCode, positionId: "77"));
        Poll(sessions, descriptor, WithPosition(intent.StrategyCode, positionId: "77"));

        Assert.Single(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);
    }

    [Fact]
    public void CloseOfAPositionTheReconciliationHadDroppedIsAccountedAnyway()
    {
        // Il caso che è costato 123 trade nel confronto compare-0032: la riconciliazione dà la
        // posizione per sparita, il client la chiude davvero e chiede di registrarla, il server
        // risponde 404 e il P&L — già realizzato al broker — non entra in nessun artefatto.
        var (sessions, descriptor, _) = Session();
        var entry = OpenPosition(sessions, descriptor);

        Poll(sessions, descriptor, WithPosition(entry.StrategyCode));
        Poll(sessions, descriptor, NoPositions());
        Assert.Empty(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);

        var close = sessions.CreateExternalCloseIntent(descriptor.SessionId, new CreateExternalCloseIntentRequest
        {
            SessionToken = descriptor.SessionToken,
            StrategyCode = entry.StrategyCode,
            Symbol = entry.Symbol,
            AccountNumber = Account,
            Quantity = entry.Quantity,
            Reason = "LocalExit:StopLoss"
        });

        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{close.IntentId}",
                IntentId = close.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = close.Quantity,
                FillPrice = 110m,
                EventTimeUtc = Utc(2026, 1, 7)
            }
        });

        var trade = Assert.Single(sessions.GetPersistedTrades(descriptor.SessionId, descriptor.SessionToken));
        Assert.Equal(entry.StrategyCode, trade.StrategyCode);
        // L'ingresso viene dalla lapide: senza, il trade non sarebbe ricostruibile affatto.
        Assert.Equal(100m, trade.EntryPrice);
        Assert.Equal(110m, trade.ExitPrice);
        Assert.Equal("LocalExit:StopLoss", trade.ExitReason);

        // E la posizione non resta in piedi: la chiusura l'ha consumata.
        Assert.Empty(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);
    }

    [Fact]
    public void CloseOfAPositionThatWasNeverOpenIsStillRejected()
    {
        // La lapide serve a non buttare via un P&L reale, non ad accettare qualunque chiusura: una
        // strategia che non ha mai aperto niente non deve poter fabbricare un trade.
        var (sessions, descriptor, strategy) = Session();

        Assert.Throws<KeyNotFoundException>(() =>
            sessions.CreateExternalCloseIntent(descriptor.SessionId, new CreateExternalCloseIntentRequest
            {
                SessionToken = descriptor.SessionToken,
                StrategyCode = strategy.Name,
                Symbol = strategy.Symbol,
                AccountNumber = Account,
                Quantity = 1m,
                Reason = "LocalExit:StopLoss"
            }));
    }

    [Fact]
    public void ANewEntryOnTheSameKeyDiscardsTheTombstone()
    {
        // Riaperta la stessa strategia sullo stesso simbolo, la lapide vecchia non vale più: una
        // chiusura in ritardo che la servisse scriverebbe un trade con il prezzo di ingresso della
        // posizione precedente.
        var (sessions, descriptor, strategy) = Session();
        var entry = OpenPosition(sessions, descriptor);

        Poll(sessions, descriptor, WithPosition(entry.StrategyCode));
        Poll(sessions, descriptor, NoPositions());

        sessions.PushBars(Bars(descriptor, strategy, Utc(2026, 1, 6)));
        OpenPosition(sessions, descriptor, pushBars: false, fillPrice: 200m);

        var close = sessions.CreateExternalCloseIntent(descriptor.SessionId, new CreateExternalCloseIntentRequest
        {
            SessionToken = descriptor.SessionToken,
            StrategyCode = entry.StrategyCode,
            Symbol = entry.Symbol,
            AccountNumber = Account,
            Quantity = entry.Quantity,
            Reason = "LocalExit:StopLoss"
        });

        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{close.IntentId}",
                IntentId = close.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = close.Quantity,
                FillPrice = 210m,
                EventTimeUtc = Utc(2026, 1, 7)
            }
        });

        var trade = Assert.Single(sessions.GetPersistedTrades(descriptor.SessionId, descriptor.SessionToken));
        Assert.Equal(200m, trade.EntryPrice);
    }

    // ------------------------------------------------------------------------------ helper

    private static AccountSignalPollRequest NoPositions() => new() { SessionToken = string.Empty };

    private AccountSignalResponse Poll(
        TradingSessionService sessions, TradingSessionDescriptor descriptor, AccountSignalPollRequest request)
        => sessions.PollSignalForAccount(descriptor.SessionId, Account, new AccountSignalPollRequest
        {
            SessionToken = descriptor.SessionToken,
            Positions = request.Positions
        });

    private static AccountSignalPollRequest WithPosition(string strategyCode, string positionId = "1") => new()
    {
        SessionToken = string.Empty,
        Positions =
        [
            new BrokerPositionSnapshot
            {
                PositionId = positionId,
                // Il nome del BROKER, come lo manda il cBot: è il cuore del test.
                Symbol = BrokerSymbol,
                StrategyCode = strategyCode
            }
        ]
    };

    private OrderIntent OpenPosition(
        TradingSessionService sessions,
        TradingSessionDescriptor descriptor,
        string? brokerPositionId = null,
        bool pushBars = true,
        decimal fillPrice = 100m)
    {
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        if (pushBars)
            sessions.PushBars(Bars(descriptor, strategy, Utc(2026, 1, 5)));

        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, Account).Intent;
        Assert.NotNull(claimed);

        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{claimed!.IntentId}",
                IntentId = claimed.IntentId,
                // È da qui che il server prende l'id della posizione sul broker: il cBot lo dichiara
                // nel report di ingresso, e senza la riconciliazione può solo confrontare coppie.
                ExternalOrderId = brokerPositionId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = claimed.Quantity,
                FillPrice = fillPrice,
                EventTimeUtc = Utc(2026, 1, 5)
            }
        });

        return claimed;
    }

    private (TradingSessionService Sessions, TradingSessionDescriptor Descriptor, StrategyDefinition Strategy) Session()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"reconcile-{Guid.NewGuid():N}", StrategiesFilter = [strategy.Id]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        // Simbolo Piootoo e simbolo broker DIVERSI: senza questo il test opererebbe 1 a 1 e non
        // distinguerebbe una riconciliazione corretta da una che non trova mai niente.
        workspaces.CreateSymbolConversion(new SymbolConversion
        {
            Code = "cfd-oro",
            Name = "cfd-oro",
            RoundingMode = QuantityRoundingMode.BrokerVolumeStep,
            Mappings =
            [
                new AccountSymbolMapping
                {
                    Symbol = strategy.Symbol,
                    AccountSymbol = BrokerSymbol,
                    ContractMultiplier = 1m,
                    MinimumQuantity = 0m,
                    QuantityStep = 0m,
                    Enabled = true
                }
            ]
        });

        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = $"acc-{Account}",
            AccountNumber = Account,
            GroupId = "g1",
            InitialBalance = AccountSymbolConversion.ReferenceBalance,
            SymbolConversionCode = "cfd-oro",
            Enabled = true
        });

        var sessions = new TradingSessionService(
            workspaces, new SingleSignalEvaluationService(), new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            MaxConcurrentTrades = 5
        });
        sessions.SetSessionAccounts(descriptor.SessionId, descriptor.SessionToken, [Account]);
        sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);
        return (sessions, descriptor, strategy);
    }

    private static PushBarsRequest Bars(
        TradingSessionDescriptor descriptor, StrategyDefinition strategy, DateTime barTime) => new()
    {
        SessionId = descriptor.SessionId,
        SessionToken = descriptor.SessionToken,
        Bars =
        [
            new ClosedBar
            {
                Symbol = strategy.Symbol,
                TimeframeMinutes = strategy.TimeframeMinutes,
                BarTimeUtc = barTime,
                Sequence = barTime.Ticks,
                IdempotencyKey = $"bar-{barTime:O}",
                Bar = new OhlcvData
                {
                    DateTime = barTime, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1
                }
            }
        ]
    };

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Un segnale di ingresso per barra, col nome reale della strategia del catalogo.</summary>
    private sealed class SingleSignalEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];
            return
            [
                new TradeSignal
                {
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Symbol = strategy.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy,
                    Quantity = 1m,
                    Price = closedBar.Bar.Close
                }
            ];
        }
    }
}
