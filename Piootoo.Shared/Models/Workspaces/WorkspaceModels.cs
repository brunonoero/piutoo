namespace Piootoo.Shared.Models.Workspaces;

public sealed class WorkspaceMasterFilter
{
    public string Name { get; set; } = string.Empty;
    public List<string> StrategiesFilter { get; set; } = new();
}

public sealed class WorkspaceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StrategiesCount { get; set; }
}

public sealed class WorkspaceBacktestInfo
{
    public string FolderName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime LastModifiedUtc { get; set; }
    public int ResultsCount { get; set; }
    public bool HasResults => ResultsCount > 0;
}

public sealed class CreateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> StrategiesFilter { get; set; } = new();
}
