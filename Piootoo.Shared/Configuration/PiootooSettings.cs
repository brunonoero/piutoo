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
