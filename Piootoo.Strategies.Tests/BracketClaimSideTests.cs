using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il claim di una sessione esterna deve distinguere le due gambe di un bracket da un doppione.
///
/// <para><b>Il difetto che questi test bloccano.</b> I lucchetti del claim erano chiavati su
/// (strategia, simbolo) senza il lato. Un motore non simmetrico emette le due gambe sulla
/// <b>stessa barra</b> — stop buy sull'estremo alto, stop sell su quello basso — e il claim ne
/// serve una alla volta: servito il Buy, il Sell restava rifiutato finché il primo era
/// <c>Pending</c>, cioè per sempre, perché il Buy viene riemesso e rireclamato a ogni barra. Nel
/// confronto <c>compare-0009</c> (GC/XAUUSD, lug-nov 2024) valeva <b>47 long e zero short</b> su
/// <c>PTS_GC_PCH_004_240</c>, contro i 25/20 che il backtest fa <i>sugli stessi identici
/// segnali</i>: la strategia emette 127 buy e 127 sell nella finestra, sempre in coppia.</para>
///
/// <para>Il backtest non aveva il difetto perché non passa dal claim: il motore interno mette il
/// verso nella chiave del pending da sempre
/// (<c>PiootooTradingService.EnqueuePendingOrder</c>, «long e short stop possono coesistere»).
/// È lo stesso difetto corretto nel cBot il 26/08/2026 — <c>CancelStrategyPendingOrders</c> per
/// strategia <i>e lato</i> — di cui non era mai stato fatto il gemello lato server.</para>
///
/// <para><b>Cosa NON deve cambiare</b>, ed è metà del valore di questo file: il doppione dello
/// stesso lato resta bloccato (era il caso reale <c>PTS_NQ_PCH_002_15</c> del 14/10/2024, due stop
/// riempiti allo stesso prezzo), e la gamba opposta resta bloccata quando l'altra si è già
/// riempita, perché quello è l'OCO e non un vincolo di distribuzione.</para>
/// </summary>
public sealed class BracketClaimSideTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-bracket-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void BracketLegs_BothReachTheSameAccount()
    {
        // La regressione vera e propria: prima della correzione il secondo claim rispondeva
        // NoSignal con «l'account ha già un ingresso in corso», e la gamba short non arrivava mai
        // al broker.
        var (sessions, descriptor) = Session(Bracket, [Row("g1", "1001", maxConcurrent: 5)],
            enforceConcurrencyLimits: true);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var first = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        var second = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;

        Assert.NotNull(first);
        Assert.NotNull(second);
        // Stessa strategia e stesso simbolo: è proprio la coppia che il filtro cieco al lato
        // scambiava per un doppione.
        Assert.Equal(first!.StrategyCode, second!.StrategyCode);
        Assert.Equal(first.Symbol, second.Symbol);
        Assert.Equal(new[] { SignalType.Buy, SignalType.Sell }.OrderBy(x => x),
            new[] { first.Side, second.Side }.OrderBy(x => x));
    }

    [Fact]
    public void SameSideOnTwoBars_IsStillRefused()
    {
        // Il caso che il filtro esisteva per fermare, e che deve restare fermo: due template dello
        // stesso lato da barre diverse, reclamati prima che il primo riempia. MaxEntriesPerSession
        // non li vede, perché si applica al fill.
        var (sessions, descriptor) = Session(LongOnly, [Row("g1", "1001", maxConcurrent: 5)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));
        Assert.NotNull(sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 6)));
        var doppione = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.Null(doppione.Intent);
        Assert.Equal("NoSignal", doppione.Reason);
    }

    [Fact]
    public void OnceALegIsFilled_TheOppositeLegIsRefused()
    {
        // L'OCO: la posizione aperta blocca ENTRAMBI i lati, ed è l'unico punto in cui la cecità al
        // verso è la regola giusta. Senza, la strategia resterebbe long e short insieme.
        var (sessions, descriptor) = Session(Bracket, [Row("g1", "1001", maxConcurrent: 5)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));
        var gamba = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        Assert.NotNull(gamba);

        Fill(sessions, descriptor, gamba!);

        var opposta = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");
        Assert.Null(opposta.Intent);
        Assert.Equal("NoSignal", opposta.Reason);
    }

    // ------------------------------------------------------------------------------ helper

    /// <summary>Le due gambe di un bracket: stessa strategia, stesso simbolo, stessa barra.</summary>
    private static readonly SignalType[] Bracket = [SignalType.Buy, SignalType.Sell];

    /// <summary>Una gamba sola, per il caso di controllo del doppione.</summary>
    private static readonly SignalType[] LongOnly = [SignalType.Buy];

    private static TradingGroupRow Row(string groupId, string account, int maxConcurrent) => new()
    {
        GroupId = groupId, AccountNumber = account,
        MaxConcurrentTrades = maxConcurrent,
        ConcurrencyCountMode = ConcurrencyCountMode.PositionsAndPendingOrders,
    };

    private (TradingSessionService Sessions, TradingSessionDescriptor Descriptor) Session(
        SignalType[] sides,
        IReadOnlyList<TradingGroupRow> groups,
        bool? enforceConcurrencyLimits = null)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"bracket-{Guid.NewGuid():N}", StrategiesFilter = [strategyId]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();
        TestAccountRegistry.Register(workspaces, groups);

        var sessions = new TradingSessionService(
            workspaces, new BracketEvaluationService(sides), new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            EnforceConcurrencyLimits = enforceConcurrencyLimits
        });
        sessions.SetTradingGroups(descriptor.SessionId, descriptor.SessionToken, groups);
        sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);
        return (sessions, descriptor);
    }

    private static void Fill(
        TradingSessionService sessions, TradingSessionDescriptor descriptor, OrderIntent intent) =>
        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{intent.IntentId}", IntentId = intent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = intent.Quantity,
                FillPrice = 100m, EventTimeUtc = Utc(2026, 1, 5)
            }
        });

    private static PushBarsRequest Bars(TradingSessionDescriptor descriptor, DateTime barTime)
    {
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        return new PushBarsRequest
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
    }

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Emette una gamba per ciascun lato richiesto, tutte con lo <b>stesso codice strategia</b> e
    /// sullo stesso simbolo e sulla stessa barra: è il bracket che i motori non simmetrici
    /// producono, ed è esattamente la forma che i lucchetti del claim scambiavano per un doppione.
    ///
    /// <para>Il primo lato passa dal segnale primario e gli altri da <c>CompanionSignals</c>, come
    /// fa <c>EasyEngineBase.Combine</c>: l'ordine conta, perché il claim serve il primo della lista
    /// ed è così che il Buy finiva sempre davanti al Sell.</para>
    /// </summary>
    private sealed class BracketEvaluationService(SignalType[] sides) : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];

            return sides.Select(side => new TradeSignal
            {
                StrategyCode = strategy.Name,
                StrategyName = strategy.Name,
                Symbol = strategy.Symbol,
                Date = closedBar.BarTimeUtc,
                Type = side,
                Quantity = 4m,
                // Il livello sta dal lato giusto rispetto alla chiusura, come nel bracket vero.
                Price = side == SignalType.Buy ? closedBar.Bar.High : closedBar.Bar.Low,
                OrderType = TradeOrderType.Stop
            }).ToArray();
        }
    }
}
