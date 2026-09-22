using System.Reflection;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// I <b>contenitori di ricerca</b>: classi che danno a uno studio un simbolo, un timeframe e un
/// <c>Initialize</c> che legge ogni leva, con i pattern alle sentinelle e parametri che nessuna
/// validazione ha visto.
///
/// <para><b>Il difetto che questi test chiudono.</b> Fino al 22/09/2026 la distinzione viveva nel
/// solo commento XML, che nessuna schermata mostra: nel catalogo un contenitore era identico a una
/// finalista. <c>PT3B_NQ_PCH_001_15</c> e' finito cosi' in un piano eseguito, dove ha prodotto 334
/// trade e −17.612 su un conto vero.</para>
///
/// <para>Ora e' un dato dichiarato, e il server rifiuta in due punti: il salvataggio del
/// masterfilter e l'apertura della sessione. Il secondo esiste perche' i masterfilter scritti prima
/// del primo controllo sono ancora su disco.</para>
/// </summary>
public sealed class ResearchContainerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-cont-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Il catalogo dichiara la proprieta', e le classi che oggi sono contenitori la portano.
    /// </summary>
    [Fact]
    public void TheKnownContainersAreDeclaredInTheCatalog()
    {
        var completo = StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true);
        var containers = completo
            .Where(strategy => strategy.IsResearchContainer)
            .Select(strategy => strategy.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        Assert.Contains("PT3B_NQ_PCH_001_15", containers);
        Assert.Contains("PT3B_CL_PCH_001_30", containers);
        Assert.DoesNotContain("PT3B_FDAX_PCH_002_240", containers);

        // E l'elenco di default — quello fra cui si sceglie — non ne contiene nessuno: e' la ragione
        // per cui i test che pescano "una strategia qualunque" non ne prendono mai uno.
        var selezionabili = StrategyFactory.GetRegisteredStrategies();
        Assert.DoesNotContain(selezionabili, strategy => strategy.IsResearchContainer);
        Assert.Equal(containers.Count, completo.Count - selezionabili.Count);
    }

    /// <summary>
    /// La guardia contro la dimenticanza: una classe con tutti e quattro i gate di pattern alle
    /// <b>sentinelle</b> (55/56 neutrali, 52/53 direzionali) non filtra niente, e nel catalogo di
    /// oggi questo succede solo ai contenitori. Se ne compare una che non si dichiara, o e' un
    /// contenitore a cui e' sfuggita la dichiarazione, o e' una strategia vera che la ricerca ha
    /// lasciato senza pattern.
    ///
    /// <para><b>La regola non e' una legge di natura, ed e' gia' stata falsa.</b> Fino al 22/09/2026
    /// il catalogo conteneva <c>PT2_NQ_PCH_002_30</c>: gate spenti, finestra a giornata piena,
    /// <c>SkipDay</c> a −1 e <c>DvolMin</c> a zero, cioe' la stessa configurazione di
    /// <c>PT3B_NQ_PCH_001_15</c>, eppure una strategia portata da un run che per quella cella non
    /// aveva scelto alcun pattern. La serie PT2 e' stata rimossa e il controesempio con lei. Se ne
    /// ricompare uno, la risposta giusta e' <b>togliere questo test</b>, non marcare contenitore una
    /// strategia vera per farlo tacere.</para>
    /// </summary>
    [Fact]
    public void AStrategyWithBlankPatternsDeclaresItselfAContainer()
    {
        var sospette = new List<string>();

        foreach (var definition in StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true))
        {
            if (definition.IsResearchContainer) continue;

            var instance = StrategyFactory.CreateStrategy(
                definition.Id, definition.Symbol, definition.TimeframeMinutes);
            if (instance is null) continue;

            if (HasBlankPatterns(instance))
                sospette.Add(definition.Id);
        }

        Assert.True(sospette.Count == 0,
            "Classi con i pattern alle sentinelle che non si dichiarano contenitori di ricerca: " +
            string.Join(", ", sospette) +
            ". Se sono contenitori, dichiara IsResearchContainer. Se invece sono strategie vere a cui " +
            "la ricerca non ha dato pattern, questo test non ha piu' fondamento e va tolto: vedi la " +
            "nota sul caso PT2_NQ_PCH_002_30.");
    }

    /// <summary>
    /// I quattro gate letti per riflessione, perche' sono protetti. Una classe che non li ha — un
    /// motore diverso dal Price Channel — non e' sospetta e torna <c>false</c>.
    /// </summary>
    private static bool HasBlankPatterns(ITradingStrategy instance)
    {
        var sentinelle = new Dictionary<string, int>
        {
            ["NeutralYes"] = 55, ["NeutralNo"] = 56, ["DirectionalYes"] = 52, ["DirectionalNo"] = 53
        };

        foreach (var (nome, atteso) in sentinelle)
        {
            var field = Campo(instance.GetType(), nome);
            if (field is null) return false;
            if (field.GetValue(instance) is not int valore || valore != atteso) return false;
        }

        return true;
    }

    private static FieldInfo? Campo(Type? type, string name)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            var field = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field is not null) return field;
        }

        return null;
    }

    /// <summary>
    /// <b><c>SessionExitTime</c> la legge solo il Price Channel</b>, e una classe di un altro motore
    /// che la dichiarasse non otterrebbe niente — in silenzio.
    ///
    /// <para>Il campo sta su <c>EasyEngineBase</c>, quindi lo ereditano tutti e dieci i motori, ma
    /// <c>PriceChannelEngine.WithPythonSettings</c> e' l'unico che lo usa: <c>TfEngineBase</c> chiude
    /// su <c>SessionEnd</c> e basta. Non e' un dettaglio: l'uscita prima del rollover e' la leva che
    /// ha prodotto <c>PT3B_FDAX_PCH_002_240</c> e che quattro griglie indipendenti hanno confermato,
    /// e chi la applicasse a una trend following otterrebbe gli stessi numeri di prima concludendo
    /// che «su questa non serve». La griglia TF del 22/09 ci ha perso meta' corsa: 250 combinazioni
    /// che ne misuravano 25.</para>
    ///
    /// <para>Finche' la leva non e' portata su <c>EasyEngineBase</c> per tutti i motori, questo test
    /// impedisce che una classe la dichiari dove non fa niente.</para>
    /// </summary>
    [Fact]
    public void OnlyThePriceChannelDeclaresASessionExitTime()
    {
        var sospette = new List<string>();

        foreach (var definition in StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true))
        {
            var instance = StrategyFactory.CreateStrategy(
                definition.Id, definition.Symbol, definition.TimeframeMinutes);
            if (instance is null or Piootoo.Strategies.Easy.Engines.PriceChannelEngine) continue;

            var field = Campo(instance.GetType(), "SessionExitTime");
            if (field?.GetValue(instance) is not null)
                sospette.Add(definition.Id);
        }

        Assert.True(sospette.Count == 0,
            "Classi non Price Channel che dichiarano SessionExitTime, dove il motore non la legge: " +
            string.Join(", ", sospette) +
            ". O si porta la leva su EasyEngineBase per tutti i motori, o quella dichiarazione non " +
            "fa niente e va tolta. Vedi ricerca/nq-4h-tf-griglia-grossa.md.");
    }

    /// <summary>Un masterfilter con dentro un contenitore non si salva, e l'errore dice quale.</summary>
    [Fact]
    public void AMasterFilterWithAContainerIsRejected()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"cont-{Guid.NewGuid():N}",
            StrategiesFilter = ["PT3B_FDAX_PCH_002_240"]
        });

        var errore = Assert.Throws<InvalidOperationException>(() =>
            workspaces.SaveMasterFilter(workspace.Id, new WorkspaceMasterFilter
            {
                Name = workspace.Id,
                StrategiesFilter = ["PT3B_FDAX_PCH_002_240", "PT3B_NQ_PCH_001_15"]
            }));

        Assert.Contains("PT3B_NQ_PCH_001_15", errore.Message);
        // Il masterfilter sul disco non e' stato toccato: il rifiuto viene prima della scrittura.
        Assert.Equal(["PT3B_FDAX_PCH_002_240"], workspaces.GetMasterFilter(workspace.Id).StrategiesFilter);
    }

    /// <summary>
    /// Anche la creazione del workspace passa dallo stesso controllo: e' la stessa porta, e lasciarla
    /// aperta significherebbe poter creare in un colpo cio' che non si puo' salvare in due.
    /// </summary>
    [Fact]
    public void AWorkspaceCreatedWithAContainerIsRejected()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });

        Assert.Throws<InvalidOperationException>(() => workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"cont-create-{Guid.NewGuid():N}",
            StrategiesFilter = ["PT3B_CL_PCH_001_30"]
        }));
    }

    /// <summary>Un masterfilter senza contenitori si salva come prima: il controllo non tocca il caso normale.</summary>
    [Fact]
    public void AMasterFilterWithoutContainersIsSavedAsBefore()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"ok-{Guid.NewGuid():N}",
            StrategiesFilter = ["PT3B_FDAX_PCH_001_240"]
        });

        var salvato = workspaces.SaveMasterFilter(workspace.Id, new WorkspaceMasterFilter
        {
            Name = workspace.Id,
            StrategiesFilter = ["PT3B_FDAX_PCH_001_240", "PT3B_FDAX_PCH_002_240"]
        });

        Assert.Equal(2, salvato.StrategiesFilter.Count);
    }
}
