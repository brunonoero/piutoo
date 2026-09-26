namespace Piootoo.Shared.Configuration;

/// <summary>
/// Configurazione dei path per Piootoo
/// </summary>
public class PiootooSettings
{
    public string BasePath { get; set; } = string.Empty;
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Cartella dei feed raccolti da un bot esterno, tenuta SEPARATA da <see cref="RepositoryPath"/>:
    /// stessa convenzione di nome (<c>@SYM_{minuti}.json</c>) e stesso formato, ma i due non si
    /// mescolano finche' non lo si decide. Il feed del vendor e quello del broker non hanno lo
    /// stesso bucket ne' lo stesso volume, e sovrascrivere il primo col secondo renderebbe non
    /// confrontabili tutti i backtest gia' fatti. Quando manca, vale <c>[BasePath]\datafeed-external</c>.
    /// </summary>
    public string ExternalRepositoryPath { get; set; } = string.Empty;
    /// <summary>
    /// Cartella delle misure di spread, una sottocartella per broker, con dentro i CSV
    /// <c>spread-by-symbol</c> prodotti da <c>PiootooSpreadDumpBot</c>. Separata da
    /// <see cref="ExternalRepositoryPath"/> di proposito: quella e' il feed che il server scrive, e
    /// mescolarci file di misura fa sembrare dati di feed quello che non lo e'. Quando manca, vale
    /// <c>[BasePath]\spread</c>.
    /// </summary>
    public string SpreadPath { get; set; } = string.Empty;

    /// <summary>
    /// Cartella delle misure di finanziamento overnight. Separata da <see cref="SpreadPath"/>
    /// perche' sono due misure diverse dello stesso broker, prese da fonti diverse — lo spread da
    /// un dump di tick, lo swap dalla scheda del simbolo — e tenerle insieme farebbe sembrare l'una
    /// un dettaglio dell'altra.
    /// </summary>
    public string SwapPath { get; set; } = string.Empty;

    /// <summary>
    /// Cartella delle specifiche che un broker dichiara sui propri strumenti — moltiplicatori,
    /// volumi, tariffe di finanziamento — raccolte da un cBot e tenute a <b>scatti datati</b>.
    /// E' la fonte da cui si derivano lo swap e la riconciliazione della tabella di conversione, e
    /// sta separata da entrambi perche' e' una <i>misura</i>: quei due file contengono anche
    /// decisioni, e un file che cambia per due ragioni non si legge piu' in un diff.
    /// </summary>
    public string SymbolInfoPath { get; set; } = string.Empty;

    /// <summary>
    /// Cartella dei confronti fra run (<c>compare-NNNN</c>), quella in cui il server crea i confronti
    /// avviati dalla console. Quando manca, vale <c>[BasePath]\compare</c>: e' la cartella del
    /// repository dati, dove i confronti fatti a mano stanno gia'.
    /// </summary>
    public string ComparePath { get; set; } = string.Empty;

    /// <summary>
    /// Cartella dei best plan, le fotografie dei backtest messi in evidenza. Sta fuori dai workspace
    /// perche' l'elenco e' trasversale e deve sopravvivere alla pulizia delle cartelle di backtest.
    /// Quando manca, vale <c>[BasePath]\best-plans</c>, accanto a <c>workspaces</c>.
    /// </summary>
    public string BestPlansPath { get; set; } = string.Empty;

    public string SettingsPath { get; set; } = string.Empty;
    public string Workspaces { get; set; } = string.Empty;
    public string Accounts { get; set; } = string.Empty;
    public string StrategiesPath { get; set; } = string.Empty;

    /// <summary>
    /// Risolve i path sostituendo [BasePath] con il valore effettivo
    /// </summary>
    public void ResolvePaths()
    {
        if (!string.IsNullOrEmpty(BasePath))
        {
            RepositoryPath = ResolvePath(RepositoryPath);
            ExternalRepositoryPath = ResolvePath(ExternalRepositoryPath);
            SpreadPath = ResolvePath(SpreadPath);
            SwapPath = ResolvePath(SwapPath);
            SymbolInfoPath = ResolvePath(SymbolInfoPath);
            ComparePath = ResolvePath(ComparePath);
            BestPlansPath = ResolvePath(BestPlansPath);
            SettingsPath = ResolvePath(SettingsPath);
            Workspaces = ResolvePath(Workspaces);
            Accounts = ResolvePath(Accounts);
            StrategiesPath = ResolvePath(StrategiesPath);
        }
    }

    private string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        return path.Replace("[BasePath]", BasePath);
    }

    /// <summary>
    /// Ottiene il path completo del repository datafeed
    /// </summary>
    public string GetRepositoryPath() => ResolvePath(RepositoryPath);

    /// <summary>
    /// Cartella dei feed esterni. Il default non e' configurato altrove di proposito: un server a
    /// cui manca la voce deve comunque avere un posto dove raccogliere, non rifiutare gli invii.
    /// </summary>
    public string GetExternalRepositoryPath()
        => string.IsNullOrWhiteSpace(ExternalRepositoryPath)
            ? Path.Combine(string.IsNullOrWhiteSpace(BasePath) ? "." : BasePath, "datafeed-external")
            : ResolvePath(ExternalRepositoryPath);

    /// <summary>
    /// Cartella delle misure di spread. Come per i feed esterni il default non e' configurato
    /// altrove: un server a cui manca la voce deve comunque avere un posto dove cercarle, e dire
    /// che non ci sono, invece di non sapere dove guardare.
    /// </summary>
    public string GetSpreadPath()
        => string.IsNullOrWhiteSpace(SpreadPath)
            ? Path.Combine(string.IsNullOrWhiteSpace(BasePath) ? "." : BasePath, "spread")
            : ResolvePath(SpreadPath);

    /// <summary>Cartella delle misure di swap. Il default e' <c>[BasePath]\swap</c>.</summary>
    public string GetSwapPath()
        => string.IsNullOrWhiteSpace(SwapPath)
            ? Path.Combine(string.IsNullOrWhiteSpace(BasePath) ? "." : BasePath, "swap")
            : ResolvePath(SwapPath);

    /// <summary>
    /// Cartella delle specifiche che ogni broker dichiara sui propri strumenti. Il default e'
    /// <c>[BasePath]\symbol-info</c>.
    /// </summary>
    public string GetSymbolInfoPath()
        => string.IsNullOrWhiteSpace(SymbolInfoPath)
            ? Path.Combine(string.IsNullOrWhiteSpace(BasePath) ? "." : BasePath, "symbol-info")
            : ResolvePath(SymbolInfoPath);

    /// <summary>Cartella dei confronti fra run. Il default e' <c>[BasePath]\compare</c>.</summary>
    public string GetComparePath()
        => string.IsNullOrWhiteSpace(ComparePath)
            ? Path.Combine(string.IsNullOrWhiteSpace(BasePath) ? "." : BasePath, "compare")
            : ResolvePath(ComparePath);

    /// <summary>Cartella dei best plan. Il default e' <c>[BasePath]\best-plans</c>.</summary>
    public string GetBestPlansPath()
        => string.IsNullOrWhiteSpace(BestPlansPath)
            ? Path.Combine(string.IsNullOrWhiteSpace(BasePath) ? "." : BasePath, "best-plans")
            : ResolvePath(BestPlansPath);

    /// <summary>
    /// Ottiene il path completo dei settings
    /// </summary>
    public string GetSettingsPath() => ResolvePath(SettingsPath);

    /// <summary>Ottiene la cartella radice dei workspace applicativi.</summary>
    public string GetWorkspacesPath() => ResolvePath(Workspaces);

    /// <summary>Ottiene la cartella del registro account globale.</summary>
    public string GetAccountsPath() => ResolvePath(Accounts);

    /// <summary>
    /// Ottiene il path completo delle strategie
    /// </summary>
    public string GetStrategiesPath() => ResolvePath(StrategiesPath);
}
