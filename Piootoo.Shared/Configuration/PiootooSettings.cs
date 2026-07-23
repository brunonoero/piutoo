namespace Piootoo.Shared.Configuration;

/// <summary>
/// Configurazione dei path per Piootoo
/// </summary>
public class PiootooSettings
{
    public string BasePath { get; set; } = string.Empty;
    public string RepositoryPath { get; set; } = string.Empty;
    public string SettingsPath { get; set; } = string.Empty;
    public string Workspaces { get; set; } = string.Empty;
    public string StrategiesPath { get; set; } = string.Empty;

    /// <summary>
    /// Risolve i path sostituendo [BasePath] con il valore effettivo
    /// </summary>
    public void ResolvePaths()
    {
        if (!string.IsNullOrEmpty(BasePath))
        {
            RepositoryPath = ResolvePath(RepositoryPath);
            SettingsPath = ResolvePath(SettingsPath);
            Workspaces = ResolvePath(Workspaces);
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
    /// Ottiene il path completo dei settings
    /// </summary>
    public string GetSettingsPath() => ResolvePath(SettingsPath);

    /// <summary>Ottiene la cartella radice dei workspace applicativi.</summary>
    public string GetWorkspacesPath() => ResolvePath(Workspaces);

    /// <summary>
    /// Ottiene il path completo delle strategie
    /// </summary>
    public string GetStrategiesPath() => ResolvePath(StrategiesPath);
}
