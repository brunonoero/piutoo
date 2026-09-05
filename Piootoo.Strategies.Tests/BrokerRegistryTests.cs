using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'anagrafica broker: chi quota gli strumenti su cui i conti operano.
///
/// <para>Il punto che questi test tengono fermo è che la tabella dei simboli venga <b>dal broker</b>
/// e che un piano non possa mescolare broker diversi. Due broker non producono la stessa serie di
/// barre per lo stesso simbolo: un run che li somma non corrisponde a nessuno dei due conti, e
/// prima non c'era niente che lo impedisse.</para>
/// </summary>
public sealed class BrokerRegistryTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-broker", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>La tabella di un conto è quella del suo broker, non quella scritta sul conto.</summary>
    [Fact]
    public void LaTabellaDeiSimboliVieneDalBroker()
    {
        var workspaces = NewWorkspaces();
        workspaces.CreateSymbolConversion(new SymbolConversion
        {
            Code = "cfd-oro",
            Name = "CFD oro",
            Mappings = [new AccountSymbolMapping { Symbol = "@GC", AccountSymbol = "XAUUSD", Enabled = true }]
        });
        workspaces.CreateBroker(new TradingBroker
        {
            Code = "FTMO", Name = "FTMO", SymbolConversionCode = "cfd-oro"
        });

        var account = workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-ftmo", AccountNumber = "1001", BrokerCode = "FTMO", InitialBalance = 100_000m
        });

        var conversion = workspaces.ResolveConversionForAccount(account);

        Assert.Equal("cfd-oro", conversion.Code);
        Assert.Equal("XAUUSD", Assert.Single(conversion.Mappings).AccountSymbol);
    }

    /// <summary>
    /// Un conto che non dichiara ancora un broker continua a usare la tabella scritta su di sé.
    /// Ripiegare su "nessuna conversione" cambierebbe ogni size di ogni conto non ancora migrato,
    /// e lo farebbe in silenzio.
    /// </summary>
    [Fact]
    public void SenzaBroker_ValeLaTabellaScrittaSulConto()
    {
        var workspaces = NewWorkspaces();
        workspaces.CreateSymbolConversion(new SymbolConversion
        {
            Code = "storica",
            Name = "storica",
            Mappings = [new AccountSymbolMapping { Symbol = "@NQ", AccountSymbol = "NAS100", Enabled = true }]
        });

        var account = workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-vecchio", AccountNumber = "2001",
            SymbolConversionCode = "storica", InitialBalance = 100_000m
        });

        Assert.Equal("NAS100", Assert.Single(
            workspaces.ResolveConversionForAccount(account).Mappings).AccountSymbol);
    }

    /// <summary>Un broker referenziato da un conto non si elimina: il conto resterebbe senza tabella.</summary>
    [Fact]
    public void UnBrokerInUsoNonSiElimina()
    {
        var workspaces = NewWorkspaces();
        workspaces.CreateBroker(new TradingBroker { Code = "ICS", Name = "ICS" });
        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-ics", AccountNumber = "3001", BrokerCode = "ICS", InitialBalance = 100_000m
        });

        var errore = Assert.Throws<InvalidOperationException>(() => workspaces.DeleteBroker("ICS"));

        Assert.Contains("conto-ics", errore.Message);
    }

    /// <summary>
    /// Un piano opera su un broker solo: un conto di un altro broker è rifiutato al salvataggio,
    /// non scoperto a mercato aperto guardando i prezzi.
    /// </summary>
    [Fact]
    public void UnPianoNonMescolaDueBroker()
    {
        var workspaces = NewWorkspaces();
        workspaces.CreateBroker(new TradingBroker { Code = "ICS", Name = "ICS" });
        workspaces.CreateBroker(new TradingBroker { Code = "FTMO", Name = "FTMO" });
        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-ics", AccountNumber = "3001", BrokerCode = "ICS", InitialBalance = 100_000m
        });
        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-ftmo", AccountNumber = "4001", BrokerCode = "FTMO", InitialBalance = 100_000m
        });

        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"broker-{Guid.NewGuid():N}",
            StrategiesFilter = [StrategyFactory.GetRegisteredStrategies().First().Id]
        });
        var plans = new TradingPlanService(workspaces);

        var errore = Assert.Throws<ArgumentException>(() => plans.Save(workspace.Id, new SaveTradingPlanRequest
        {
            Code = "PLANMIX",
            Name = "piano misto",
            BrokerCode = "ICS",
            Accounts = ["3001", "4001"]
        }));

        Assert.Contains("FTMO", errore.Message);

        // Con i soli conti del broker dichiarato il piano si salva.
        var salvato = plans.Save(workspace.Id, new SaveTradingPlanRequest
        {
            Code = "PLANICS",
            Name = "piano ICS",
            BrokerCode = "ICS",
            Accounts = ["3001"]
        });

        Assert.Equal("ICS", salvato.BrokerCode);
    }

    /// <summary>Un broker dichiarato ma inesistente non passa: sarebbe un piano senza tabella dei simboli.</summary>
    [Fact]
    public void UnBrokerInesistenteFaFallireIlSalvataggio()
    {
        var workspaces = NewWorkspaces();
        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto", AccountNumber = "5001", InitialBalance = 100_000m
        });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"broker-{Guid.NewGuid():N}",
            StrategiesFilter = [StrategyFactory.GetRegisteredStrategies().First().Id]
        });

        var errore = Assert.Throws<ArgumentException>(() =>
            new TradingPlanService(workspaces).Save(workspace.Id, new SaveTradingPlanRequest
            {
                Code = "PLANGHOST",
                Name = "piano fantasma",
                BrokerCode = "NONESISTE",
                Accounts = ["5001"]
            }));

        Assert.Contains("NONESISTE", errore.Message);
    }

    private WorkspaceService NewWorkspaces() =>
        new(new PiootooSettings { Workspaces = _root });
}
