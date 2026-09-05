using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il moltiplicatore di size del piano: <c>k</c> entra nel fattore di conversione dell'account, e la
/// quantità che arriva al client è <c>k × dimensione del conto × conversione del simbolo</c>.
///
/// <para>Il punto che questi test tengono fermo è che <c>k</c> venga applicato <b>una volta sola</b>.
/// I punti in cui una quantità esce verso il client sono due — il clone del claim e l'intent già
/// assegnato dell'esecuzione diretta — e in mezzo c'è un template che parte dalla stessa
/// <c>FinalQuantity</c>: applicarlo anche lì lo porterebbe a <c>k²</c>, e un fattore quadratico su
/// una size non si vede finché non si contano i contratti di un conto vero.</para>
/// </summary>
public sealed class SizeMultiplierTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-size-k", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Il conto di test ha lo stesso capitale di riferimento delle strategie, quindi
    /// <c>BalanceScale</c> vale 1 e la quantità reclamata è esattamente la size della strategia per
    /// <c>k</c>: se il rapporto fra i due claim non è <c>k</c>, il moltiplicatore non sta arrivando
    /// dove deve.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void IlMoltiplicatoreDelPianoScalaLaQuantitaReclamata(int k)
    {
        var baseline = ClaimedQuantity(sizeMultiplier: 1m);
        var scalata = ClaimedQuantity(sizeMultiplier: k);

        Assert.True(baseline > 0m, "il claim di riferimento non ha prodotto quantità: test inutile");
        Assert.Equal(baseline * k, scalata);
    }

    /// <summary>
    /// La conferma che <c>k</c> non entra due volte: con un moltiplicatore di 3 la quantità è tripla,
    /// non nove volte. È il caso che il template — che passa dallo stesso <c>AddIntent</c> del
    /// percorso diretto — renderebbe silenziosamente sbagliato.
    /// </summary>
    [Fact]
    public void IlMoltiplicatoreNonVieneApplicatoDueVolte()
    {
        var baseline = ClaimedQuantity(sizeMultiplier: 1m);

        var scalata = ClaimedQuantity(sizeMultiplier: 3m);

        Assert.Equal(baseline * 3m, scalata);
        Assert.NotEqual(baseline * 9m, scalata);
    }

    /// <summary>
    /// Sotto il minimo il piano non si salva: un moltiplicatore che azzera le size dopo
    /// l'arrotondamento è uno spegnimento travestito da configurazione.
    /// </summary>
    [Fact]
    public void SottoIlMinimo_IlPianoVieneRifiutato()
    {
        var (workspaces, workspace, _) = NewWorkspace();
        var plans = new TradingPlanService(workspaces);

        var errore = Assert.Throws<ArgumentException>(() => plans.Save(workspace.Id, Plan(0.05m)));

        Assert.Contains("SizeMultiplier", errore.Message);
    }

    /// <summary>
    /// Un <c>plans.json</c> scritto prima che il campo esistesse lo presenta a zero: deve tornare 1,
    /// non azzerare le size di ogni piano già configurato al primo avvio dopo l'aggiornamento.
    /// </summary>
    [Fact]
    public void UnPianoSenzaMoltiplicatore_ValeUno()
    {
        var (workspaces, workspace, _) = NewWorkspace();
        var plans = new TradingPlanService(workspaces);

        var salvato = plans.Save(workspace.Id, Plan(sizeMultiplier: 0m));

        Assert.Equal(1m, salvato.SizeMultiplier);
        Assert.Equal(1m, plans.Get(workspace.Id, "PLANSIZE").SizeMultiplier);
    }

    /// <summary>La quantità del primo intent reclamato da un account, con il piano indicato.</summary>
    private decimal ClaimedQuantity(decimal sizeMultiplier)
    {
        var (workspaces, workspace, strategy) = NewWorkspace();
        var plans = new TradingPlanService(workspaces);

        // Il codice piano e' globale su tutti i workspace della stessa radice: due chiamate nello
        // stesso test collidono se riusano lo stesso codice.
        var code = $"PLANSIZE{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        plans.Save(workspace.Id, Plan(sizeMultiplier, code));

        var sessions = new TradingSessionService(
            workspaces, plans, new UnSegnalePerBarra(), positionSizing: new PositionSizingService());

        var descriptor = sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = code,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = $"run-{Guid.NewGuid():N}"
        });

        var barTime = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
            .AddMinutes(strategy.TimeframeMinutes);
        sessions.PushBars(new PushBarsRequest
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
        });

        var response = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.NotNull(response.Intent);
        return response.Intent!.FinalQuantity;
    }

    private static SaveTradingPlanRequest Plan(decimal sizeMultiplier, string code = "PLANSIZE") => new()
    {
        Code = code,
        Name = "Piano size",
        AccountNumber = "1001",
        SizeMultiplier = sizeMultiplier
    };

    private (WorkspaceService Workspaces, WorkspaceInfo Workspace, StrategyDefinition Strategy) NewWorkspace()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategy = StrategyFactory.GetRegisteredStrategies()
            .First(x => !string.IsNullOrWhiteSpace(x.Symbol) && x.TimeframeMinutes > 0);

        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"size-{Guid.NewGuid():N}",
            StrategiesFilter = [strategy.Id]
        });
        TestAccountRegistry.Register(workspaces, "1001");
        return (workspaces, workspace, strategy);
    }

    /// <summary>Un ingresso a mercato per barra: serve solo a far nascere un intent da reclamare.</summary>
    private sealed class UnSegnalePerBarra : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar bar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> execution)
            => strategies
                .Where(strategy => string.Equals(
                    strategy.Symbol.Trim().TrimStart('@'),
                    bar.Symbol.Trim().TrimStart('@'),
                    StringComparison.OrdinalIgnoreCase))
                .Select(strategy => new TradeSignal
                {
                    Date = bar.BarTimeUtc,
                    Type = SignalType.Buy,
                    Price = bar.Bar.Close,
                    Symbol = bar.Symbol,
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Quantity = 1m,
                    OrderType = TradeOrderType.Market,
                    ValidFromUtc = bar.BarTimeUtc
                })
                .ToList();
    }
}
