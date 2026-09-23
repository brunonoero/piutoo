using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Gli strumenti che un piano dichiara a un raccoglitore di datafeed. E' la chiamata con cui il
/// cBot smette di avere un elenco simboli scritto a mano, che e' la seconda lista destinata a
/// divergere dal masterfilter senza che nessuno se ne accorga.
/// </summary>
public sealed class PlanDatafeedInstrumentsTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-plan-instruments-tests", Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Il caso normale: le coppie (simbolo, timeframe) escono dal masterfilter, raggruppate per
    /// simbolo, e i timeframe sono quelli che le strategie usano davvero.
    /// </summary>
    [Fact]
    public void InstrumentsComeFromTheMasterFilterGroupedBySymbol()
    {
        var selected = StrategyFactory.GetRegisteredStrategies()
            .GroupBy(definition => definition.Symbol.Trim().TrimStart('@').ToUpperInvariant())
            .Where(group => group.Select(definition => definition.TimeframeMinutes).Distinct().Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .First()
            .ToArray();

        var plans = CreatePlan(selected.Select(definition => definition.Id).ToList());
        var resolved = plans.ResolveDatafeedInstruments("PLANFEED", accountNumber: null);

        var instrument = Assert.Single(resolved.Instruments);
        Assert.StartsWith("@", instrument.Symbol);
        Assert.Equal(
            selected.Select(definition => definition.TimeframeMinutes).Distinct().Order().ToArray(),
            instrument.TimeframesMinutes);
        Assert.Equal("PLANFEED", resolved.PlanCode);
    }

    /// <summary>
    /// Piu' simboli nel masterfilter = piu' strumenti, ordinati. Il raccoglitore li usa per aprire
    /// una serie ciascuno, quindi un ordine stabile rende confrontabili due avvii.
    /// </summary>
    [Fact]
    public void EverySymbolOfTheMasterFilterIsReported()
    {
        var selected = StrategyFactory.GetRegisteredStrategies()
            .GroupBy(definition => definition.Symbol.Trim().TrimStart('@').ToUpperInvariant())
            .Take(3)
            .Select(group => group.First())
            .ToArray();
        Assert.True(selected.Length > 1, "servono strategie su piu' simboli");

        var plans = CreatePlan(selected.Select(definition => definition.Id).ToList());
        var resolved = plans.ResolveDatafeedInstruments("PLANFEED", accountNumber: null);

        Assert.Equal(selected.Length, resolved.Instruments.Count);
        Assert.Equal(
            resolved.Instruments.Select(x => x.Symbol).OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
            resolved.Instruments.Select(x => x.Symbol));
        Assert.All(resolved.Instruments, x => Assert.False(string.IsNullOrWhiteSpace(x.AccountSymbol)));
    }

    /// <summary>
    /// Un piano inesistente non deve restituire una lista vuota — il bot partirebbe raccogliendo
    /// nulla e sembrerebbe funzionare. Deve essere un errore.
    /// </summary>
    [Fact]
    public void UnknownPlanIsAnError()
    {
        var plans = CreatePlan(StrategyFactory.GetRegisteredStrategies().Take(1)
            .Select(definition => definition.Id).ToList());

        Assert.Throws<KeyNotFoundException>(() => plans.ResolveDatafeedInstruments("NONESISTE", null));
    }

    /// <summary>
    /// Un conto senza anagrafica non blocca la raccolta: si usa il simbolo Piootoo cosi' com'e'.
    /// Aprire una sessione invece pretende l'anagrafica, perche' li serve anche il capitale — qui
    /// no, e fermare la raccolta per quel motivo sarebbe un costo senza contropartita.
    /// </summary>
    [Fact]
    public void UnknownAccountFallsBackToThePiootooSymbol()
    {
        var definition = StrategyFactory.GetRegisteredStrategies().First();
        var plans = CreatePlan(new List<string> { definition.Id });

        var resolved = plans.ResolveDatafeedInstruments("PLANFEED", "9999-inesistente");

        var instrument = Assert.Single(resolved.Instruments);
        Assert.Equal(instrument.Symbol, instrument.AccountSymbol);
    }

    /// <summary>
    /// L'elenco simboli senza piano: il nome sul broker viene dalla tabella del conto e la cartella
    /// dal suo broker, esattamente come con il piano. L'elenco dice solo quali simboli.
    /// </summary>
    [Fact]
    public void ListedSymbolsAreTranslatedByTheAccountTable()
    {
        var plans = CreateBrokerAccount();

        var resolved = plans.ResolveListedDatafeedInstruments("5001", ["@FESX", "SI"], [60, 1, 240, 60]);

        Assert.Equal("FTMO", resolved.DatafeedBroker);
        Assert.Equal(string.Empty, resolved.PlanCode);
        Assert.Equal(["@FESX", "@SI"], resolved.Instruments.Select(x => x.Symbol));
        Assert.Equal(["EU50.cash", "XAGUSD"], resolved.Instruments.Select(x => x.AccountSymbol));
        // Il minuto non e' un bersaglio di derivazione: e' la sorgente.
        Assert.All(resolved.Instruments, x => Assert.Equal([60, 240], x.TimeframesMinutes));
    }

    /// <summary>
    /// La forma esplicita <c>NOMEBROKER=@SIMBOLO</c> scavalca la tabella: e' quella del bot degli
    /// spread, e serve per un simbolo che la tabella non mappa ancora.
    /// </summary>
    [Fact]
    public void ExplicitBrokerNameIsTakenVerbatim()
    {
        var plans = CreateBrokerAccount();

        var resolved = plans.ResolveListedDatafeedInstruments("5001", ["ETHUSD=@ETH"], [240]);

        var instrument = Assert.Single(resolved.Instruments);
        Assert.Equal("@ETH", instrument.Symbol);
        Assert.Equal("ETHUSD", instrument.AccountSymbol);
    }

    /// <summary>
    /// Un simbolo che la tabella non mappa e uno fuori calendario sono errori, e il messaggio li
    /// nomina tutti e due: un raccoglitore che parte su meta' dell'elenco sembra funzionare.
    /// </summary>
    [Fact]
    public void UnmappedOrUncalendaredSymbolsAreRejectedTogether()
    {
        var plans = CreateBrokerAccount();

        var error = Assert.Throws<ArgumentException>(() =>
            plans.ResolveListedDatafeedInstruments("5001", ["@FESX", "@NQ", "XYZ.cash=@XYZ"], [60]));

        Assert.Contains("@NQ", error.Message);
        Assert.Contains("@XYZ", error.Message);
        Assert.DoesNotContain("@FESX", error.Message);
    }

    /// <summary>
    /// Senza un conto in anagrafica non c'e' una cartella in cui scrivere: a differenza del piano,
    /// che ripiega sul simbolo Piootoo, qui non c'e' nient'altro da cui dedurla.
    /// </summary>
    [Fact]
    public void UnknownAccountIsAnErrorForTheList()
    {
        var plans = CreateBrokerAccount();

        Assert.Throws<InvalidOperationException>(() =>
            plans.ResolveListedDatafeedInstruments("9999-inesistente", ["@FESX"], [60]));
    }

    private TradingPlanService CreateBrokerAccount()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        workspaces.CreateSymbolConversion(new SymbolConversion
        {
            Code = "cfd-test",
            Name = "CFD test",
            Mappings =
            [
                new AccountSymbolMapping { Symbol = "@FESX", AccountSymbol = "EU50.cash", Enabled = true },
                new AccountSymbolMapping { Symbol = "@SI", AccountSymbol = "XAGUSD", Enabled = true }
            ]
        });
        workspaces.CreateBroker(new TradingBroker { Code = "FTMO", Name = "FTMO", SymbolConversionCode = "cfd-test" });
        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-ftmo", AccountNumber = "5001", BrokerCode = "FTMO", InitialBalance = 100_000m
        });

        return new TradingPlanService(workspaces);
    }

    private TradingPlanService CreatePlan(List<string> strategyIds)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"feed-{Guid.NewGuid():N}",
            StrategiesFilter = strategyIds
        });
        TestAccountRegistry.Register(workspaces, "1001");

        var plans = new TradingPlanService(workspaces);
        plans.Save(workspace.Id, new SaveTradingPlanRequest
        {
            Code = "PLANFEED",
            Name = "Piano raccolta feed",
            AccountNumber = "1001"
        });

        return plans;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
