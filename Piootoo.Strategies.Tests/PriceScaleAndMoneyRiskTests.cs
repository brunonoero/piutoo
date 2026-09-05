using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il confine dell'account per la specifica di uscita: la scala di prezzo e il rischio in denaro.
///
/// <para>Fino al 25/08/2026 la tabella di conversione dell'account toccava la sola quantità, e il
/// denaro dichiarato dalla ricerca moriva sul server nella divisione che lo trasformava in punti.
/// Le due cose insieme rendevano invisibile un errore di catena: un conto che eseguiva un decimo
/// del contratto di riferimento rischiava un decimo del denaro dichiarato, e nessuno dei due lati
/// aveva in mano i numeri per accorgersene.</para>
///
/// <para>Le due modifiche sono deliberatamente indipendenti. <c>PriceScale</c> converte le
/// distanze di prezzo e <b>non</b> tocca il denaro: il denaro è il rischio per contratto future di
/// riferimento e resta quello qualunque strumento lo esegua. Il default è 1 ovunque, quindi nessun
/// run esistente cambia comportamento — ed è la prima cosa che questi test verificano.</para>
/// </summary>
public sealed class PriceScaleAndMoneyRiskTests : IDisposable
{
    private const decimal StopMoney = 1_000m;
    private const decimal TargetMoney = 2_000m;
    private const decimal TrailingMoney = 600m;
    private const decimal BreakEvenMoney = 400m;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-pricescale-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // --------------------------------------------------- la tabella di conversione, da sola

    [Fact]
    public void PriceScale_DefaultsToOne_WhenTheRowDoesNotDeclareIt()
    {
        // Il default vive sul modello: una riga scritta prima che il campo esistesse si rilegge
        // come scala 1, che è il motivo per cui questa modifica non cambia nessun run.
        var conversion = Conversion("scala-assente", new AccountSymbolMapping
        {
            Symbol = "@NQ", AccountSymbol = "USDTEC", ContractMultiplier = 1m, Enabled = true
        });

        Assert.Equal(1m, conversion.GetPriceScale("@NQ"));
    }

    [Fact]
    public void PriceScale_OfAnUnmappedSymbol_IsOne()
        => Assert.Equal(1m, AccountSymbolConversion.Identity.GetPriceScale("@NQ"));

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void NonPositivePriceScale_IsReadAsOne(int scala)
    {
        // Stesso trattamento del moltiplicatore di contratto: una scala mancante o assurda vale 1.
        // Zero qui azzererebbe stop e target di ogni segnale, in silenzio, che è il modo peggiore
        // in cui un errore di configurazione può manifestarsi.
        var conversion = Conversion("scala-assurda", new AccountSymbolMapping
        {
            Symbol = "@NQ", AccountSymbol = "USDTEC",
            ContractMultiplier = 1m, PriceScale = scala, Enabled = true
        });

        Assert.Equal(1m, conversion.GetPriceScale("@NQ"));
    }

    [Fact]
    public void PriceScale_IsSeparateFromTheContractMultiplier()
    {
        // I due fattori descrivono cose diverse — quanto si compra, in che unità è quotato — e non
        // devono collassare l'uno sull'altro: è tutto il punto di averne due.
        var conversion = Conversion("due-fattori", new AccountSymbolMapping
        {
            Symbol = "@NQ", AccountSymbol = "USDTEC",
            ContractMultiplier = 0.1m, PriceScale = 10m, Enabled = true
        });

        Assert.Equal(0.1m, conversion.GetContractMultiplier("@NQ"));
        Assert.Equal(10m, conversion.GetPriceScale("@NQ"));
    }

    // ------------------------------------------------------------------ il claim, end to end

    [Fact]
    public void WithoutAConversionTable_TheClaimedDistancesAreUnchanged()
    {
        var (sessions, descriptor, template) = ClaimSetup(conversionCode: null);
        var claimed = Claim(sessions, descriptor);

        Assert.Equal(1m, claimed.PriceScale);
        Assert.Equal(template.StopLoss, claimed.StopLoss);
        Assert.Equal(template.TakeProfit, claimed.TakeProfit);
        Assert.Equal(template.TrailingStop, claimed.TrailingStop);
        Assert.Equal(template.BreakEven, claimed.BreakEven);
    }

    [Fact]
    public void PriceScale_ScalesEveryDistanceOfTheExitSpec()
    {
        var (sessions, descriptor, template) = ClaimSetup(conversionCode: "scala-dieci", priceScale: 10m);
        var claimed = Claim(sessions, descriptor);

        Assert.Equal(10m, claimed.PriceScale);
        // Tutte e quattro: uno stop scalato e un trailing no è una posizione con due unità di
        // misura addosso, ed è peggio di entrambe le convenzioni prese per intero.
        Assert.Equal(template.StopLoss!.Value * 10m, claimed.StopLoss!.Value);
        Assert.Equal(template.TakeProfit!.Value * 10m, claimed.TakeProfit!.Value);
        Assert.Equal(template.TrailingStop!.Value * 10m, claimed.TrailingStop!.Value);
        Assert.Equal(template.BreakEven!.Value * 10m, claimed.BreakEven!.Value);
    }

    [Fact]
    public void PriceScale_DoesNotTouchTheDeclaredMoney()
    {
        // La distinzione che giustifica l'intera modifica: la scala è una proprietà della quotazione
        // dello strumento, il denaro è il rischio della strategia. Scalare il secondo con la prima
        // significherebbe dire che cambiando broker cambia quanto si è disposti a perdere.
        var (sessions, descriptor, _) = ClaimSetup(conversionCode: "scala-dieci", priceScale: 10m);
        var claimed = Claim(sessions, descriptor);

        Assert.Equal(StopMoney, claimed.StopLossMoneyPerFutureContract);
        Assert.Equal(TargetMoney, claimed.TakeProfitMoneyPerFutureContract);
        Assert.Equal(TrailingMoney, claimed.TrailingStopMoneyPerFutureContract);
        Assert.Equal(BreakEvenMoney, claimed.BreakEvenMoneyPerFutureContract);
    }

    [Fact]
    public void TheMoneyAndItsDivisor_ReconstructThePointsOfTheTemplate()
    {
        // Il client riceve punti, denaro e divisore: con i tre numeri può rifare il conto del
        // server e dire se il rischio che sta per mettere a mercato è quello dichiarato. È
        // esattamente la verifica che prima non era possibile fare da nessuna parte.
        var (_, _, template) = ClaimSetup(conversionCode: null);

        Assert.True(template.ReferenceDollarsPerPoint > 0m);
        Assert.Equal(StopMoney / template.ReferenceDollarsPerPoint, template.StopLoss!.Value);
        Assert.Equal(TargetMoney / template.ReferenceDollarsPerPoint, template.TakeProfit!.Value);
        Assert.Equal(TrailingMoney / template.ReferenceDollarsPerPoint, template.TrailingStop!.Value);
        Assert.Equal(BreakEvenMoney / template.ReferenceDollarsPerPoint, template.BreakEven!.Value);
    }

    // ------------------------------------------------------------------------------ helper

    private static AccountSymbolConversion Conversion(string code, params AccountSymbolMapping[] mappings)
        => AccountSymbolConversion.FromAccount(
            new WorkspaceAccount { Id = code, Name = code, InitialBalance = AccountSymbolConversion.ReferenceBalance },
            new SymbolConversion { Code = code, Name = code, Mappings = [.. mappings] });

    private static OrderIntent Claim(TradingSessionService sessions, TradingSessionDescriptor descriptor)
    {
        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        Assert.NotNull(claimed);
        return claimed!;
    }

    /// <summary>
    /// Sessione a un account e un segnale, con la specifica di uscita dichiarata in denaro come la
    /// dichiarano i motori portati da EasyLanguage. Restituisce anche il template, perché le
    /// asserzioni interessanti sono rapporti fra template e claim, non valori assoluti: il valore
    /// punto del simbolo del catalogo è un dettaglio del registro strumenti, non di questo test.
    /// </summary>
    private (TradingSessionService Sessions, TradingSessionDescriptor Descriptor, OrderIntent Template)
        ClaimSetup(string? conversionCode, decimal priceScale = 1m)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"pricescale-{Guid.NewGuid():N}", StrategiesFilter = [strategy.Id]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        if (conversionCode is not null)
        {
            workspaces.CreateSymbolConversion(new SymbolConversion
            {
                Code = conversionCode,
                Name = conversionCode,
                RoundingMode = QuantityRoundingMode.BrokerVolumeStep,
                Mappings =
                [
                    new AccountSymbolMapping
                    {
                        Symbol = strategy.Symbol,
                        AccountSymbol = "USDTEC",
                        ContractMultiplier = 1m,
                        PriceScale = priceScale,
                        // Nessun vincolo di granularità: qui si misurano gli stop, non la size.
                        MinimumQuantity = 0m,
                        QuantityStep = 0m,
                        Enabled = true
                    }
                ]
            });
        }

        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "acc-1001",
            AccountNumber = "1001",
            GroupId = "g1",
            // Capitale pari al riferimento: la scala di capitale resta 1 e non entra nel conto.
            InitialBalance = AccountSymbolConversion.ReferenceBalance,
            SymbolConversionCode = conversionCode ?? string.Empty,
            Enabled = true
        });

        var sessions = new TradingSessionService(
            workspaces, new MoneyRiskEvaluationService(), new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            EnforceConcurrencyLimits = false,
            MaxConcurrentTrades = 5
        });

        sessions.SetSessionAccounts(descriptor.SessionId, descriptor.SessionToken, ["1001"]);
        sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);

        var barTime = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var pushed = sessions.PushBars(new PushBarsRequest
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

        var template = Assert.Single(pushed.Intents);
        return (sessions, descriptor, template);
    }

    /// <summary>Un solo segnale per barra, con l'uscita dichiarata in denaro per contratto.</summary>
    private sealed class MoneyRiskEvaluationService : IStrategyEvaluationService
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
                    Price = closedBar.Bar.Close,
                    StopLossMoneyPerFutureContract = StopMoney,
                    TakeProfitMoneyPerFutureContract = TargetMoney,
                    TrailingStopMoneyPerFutureContract = TrailingMoney,
                    BreakEvenMoneyPerFutureContract = BreakEvenMoney
                }
            ];
        }
    }
}
