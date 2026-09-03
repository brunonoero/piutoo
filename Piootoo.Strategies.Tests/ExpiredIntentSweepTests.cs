using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un ingresso reclamato che scade senza che il client ne riporti mai l'esito.
///
/// <para><b>Il difetto.</b> Restava <c>Pending</c> per sempre, e con lui restavano chiusi i due
/// lucchetti che lo riguardano — <c>AccountHasEntryInFlight</c> sul ramo pendente e lo slot di
/// gruppo — quindi quella (strategia, simbolo, lato) non riceveva piu' un solo ordine fino a fine
/// sessione. Misurato su una sonda a sei barre con un segnale per barra: <b>un intent in tutto il
/// run</b>, e dalla seconda barra in poi il claim rispondeva «l'account ha gia' un ingresso in corso
/// per quella strategia su quel simbolo e lato». A lucchetti accesi l'intent veniva annullato ma lo
/// slot no, e il blocco restava con un altro messaggio.</para>
///
/// <para><b>Il vincolo che questi test difendono</b>, ed e' il motivo per cui girano in tutte e due
/// le configurazioni: la spazzata vive su <c>EvaluateClosedBar</c>, il corpo comune di
/// <c>PushBars</c> e <c>PushBarWindow</c>, non sul percorso del claim. Il claim ha cadenze diverse
/// nei due mondi — timer ogni due secondi in realtime, solo eventi locali in backtest, voce del
/// 26/08/2026 — quindi una spazzata li' renderebbe il comportamento della sessione dipendente da chi
/// la esegue. La barra invece e' la stessa per entrambi. Se qualcuno rimette il controllo di
/// scadenza dentro <c>GetNextSignalForAccount</c>, questi test continuano a passare ma il vincolo e'
/// rotto: e' scritto qui perche' resti scritto da qualche parte.</para>
/// </summary>
public sealed class ExpiredIntentSweepTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-scad-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Theory]
    [InlineData(true)]   // lucchetti di concorrenza accesi: bloccava lo slot di gruppo
    [InlineData(false)]  // spenti: bloccava l'intent Pending, e nessuno lo spazzava mai
    public void UnIngressoScaduto_NonBloccaLeBarreSuccessive(bool lucchetti)
    {
        var (sessions, d) = Session(lucchetti);
        var t0 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        PushBar(sessions, d, t0);
        var primo = Claim(sessions, d);
        Assert.NotNull(primo);

        // Due barre senza un solo execution report: l'ordine e' scaduto e nessuno lo ha riportato.
        PushBar(sessions, d, t0.AddMinutes(15));
        PushBar(sessions, d, t0.AddMinutes(30));

        Assert.Equal(OrderIntentStatus.Cancelled, Intent(sessions, d, primo!.IntentId).Status);

        var secondo = Claim(sessions, d);
        Assert.NotNull(secondo);
        Assert.NotEqual(primo.IntentId, secondo!.IntentId);
        Assert.Equal(primo.StrategyCode, secondo.StrategyCode);
        Assert.Equal(primo.Side, secondo.Side);
    }

    [Fact]
    public void LaSpazzataNonDipendeDalClaim_QuindiNonDivergeFraRealtimeEBacktest()
    {
        // La differenza fra i due mondi e' quante volte il client polla. Qui non polla affatto fra
        // una barra e l'altra — il caso estremo del backtest — e l'intent deve scadere lo stesso:
        // se la spazzata dipendesse dal claim, questo intent resterebbe Pending per sempre.
        var (sessions, d) = Session(lucchetti: false);
        var t0 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        PushBar(sessions, d, t0);
        var primo = Claim(sessions, d);
        Assert.NotNull(primo);

        PushBar(sessions, d, t0.AddMinutes(15));
        PushBar(sessions, d, t0.AddMinutes(30));

        Assert.Equal(OrderIntentStatus.Cancelled, Intent(sessions, d, primo!.IntentId).Status);
    }

    [Fact]
    public void UnIngressoRiempito_NonVieneToccatoDallaSpazzata()
    {
        // Un riempimento e' esposizione vera: la sua fine la decide il broker, non una finestra di
        // validita'. Senza questo controllo la spazzata chiuderebbe posizioni aperte.
        var (sessions, d) = Session(lucchetti: false);
        var t0 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        PushBar(sessions, d, t0);
        var primo = Claim(sessions, d);
        Assert.NotNull(primo);

        sessions.ApplyReport(d.SessionId, new ExecutionReportRequest
        {
            SessionToken = d.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = "r-1", IntentId = primo!.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = primo.Quantity,
                FillPrice = 100m, EventTimeUtc = t0
            }
        });

        PushBar(sessions, d, t0.AddMinutes(15));
        PushBar(sessions, d, t0.AddMinutes(30));

        Assert.Equal(OrderIntentStatus.Filled, Intent(sessions, d, primo.IntentId).Status);
    }

    // ------------------------------------------------------------------------------ helper

    private (TradingSessionService, TradingSessionDescriptor) Session(bool lucchetti)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"scad-{Guid.NewGuid():N}", StrategiesFilter = [strategyId]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        var groups = new List<TradingGroupRow>
        {
            new()
            {
                GroupId = "g1", AccountNumber = "1001", MaxConcurrentTrades = 1,
                ConcurrencyCountMode = ConcurrencyCountMode.PositionsAndPendingOrders,
            }
        };
        TestAccountRegistry.Register(workspaces, groups);

        var sessions = new TradingSessionService(
            workspaces, new UnIngressoPerBarra(), new PositionSizingService());
        var d = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            EnforceConcurrencyLimits = lucchetti
        });
        sessions.SetTradingGroups(d.SessionId, d.SessionToken, groups);
        sessions.SetStatus(d.SessionId, d.SessionToken, TradingSessionStatus.Running);
        return (sessions, d);
    }

    private static void PushBar(
        TradingSessionService sessions, TradingSessionDescriptor d, DateTime quando)
    {
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        sessions.PushBars(new PushBarsRequest
        {
            SessionId = d.SessionId, SessionToken = d.SessionToken,
            Bars =
            [
                new ClosedBar
                {
                    Symbol = strategy.Symbol, TimeframeMinutes = strategy.TimeframeMinutes,
                    BarTimeUtc = quando, Sequence = quando.Ticks,
                    IdempotencyKey = $"bar-{quando:O}",
                    Bar = new OhlcvData
                    {
                        DateTime = quando, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1
                    }
                }
            ]
        });
    }

    private static OrderIntent? Claim(TradingSessionService sessions, TradingSessionDescriptor d) =>
        sessions.GetNextSignalForAccount(d.SessionId, d.SessionToken, "1001").Intent;

    private static OrderIntent Intent(
        TradingSessionService sessions, TradingSessionDescriptor d, string intentId) =>
        sessions.GetIntents(d.SessionId, d.SessionToken).First(i => i.IntentId == intentId);

    /// <summary>Un ingresso per barra, valido solo per la barra successiva.</summary>
    private sealed class UnIngressoPerBarra : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];
            var prossima = closedBar.BarTimeUtc.AddMinutes(strategy.TimeframeMinutes);
            return
            [
                new TradeSignal
                {
                    StrategyCode = strategy.Name, StrategyName = strategy.Name,
                    Symbol = strategy.Symbol, Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy, Quantity = 4m, Price = closedBar.Bar.High,
                    OrderType = TradeOrderType.Stop,
                    ValidFromUtc = prossima, ExpiresAtUtc = prossima
                }
            ];
        }
    }
}
