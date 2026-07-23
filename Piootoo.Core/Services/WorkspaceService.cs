using System.Text.Json;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>Gestisce workspace filesystem e il relativo masterfilter.json.</summary>
public sealed class WorkspaceService
{
    private const string MasterFilterFileName = "masterfilter.json";
    private readonly string _rootPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public WorkspaceService(PiootooSettings settings)
    {
        _rootPath = settings.GetWorkspacesPath();
        Directory.CreateDirectory(_rootPath);
    }

    public IReadOnlyList<WorkspaceInfo> List()
        => Directory.EnumerateDirectories(_rootPath)
            .Select(path =>
            {
                var id = Path.GetFileName(path);
                var filter = GetMasterFilter(id);
                return new WorkspaceInfo { Id = id, Name = filter.Name, StrategiesCount = filter.StrategiesFilter.Count };
            })
            .OrderBy(workspace => workspace.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public WorkspaceInfo Create(CreateWorkspaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Il nome del workspace è obbligatorio.");

        var id = ToId(request.Name);
        var path = GetWorkspacePath(id);
        if (Directory.Exists(path))
            throw new InvalidOperationException($"Il workspace '{id}' esiste già.");

        Directory.CreateDirectory(path);
        SaveMasterFilter(id, new WorkspaceMasterFilter
        {
            Name = request.Name.Trim(),
            StrategiesFilter = request.StrategiesFilter.Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList()
        });

        return new WorkspaceInfo { Id = id, Name = request.Name.Trim(), StrategiesCount = request.StrategiesFilter.Count };
    }

    public WorkspaceMasterFilter GetMasterFilter(string workspaceId)
    {
        var file = Path.Combine(GetExistingWorkspacePath(workspaceId), MasterFilterFileName);
        if (!File.Exists(file))
            return new WorkspaceMasterFilter { Name = workspaceId };

        return JsonSerializer.Deserialize<WorkspaceMasterFilter>(File.ReadAllText(file), _jsonOptions)
            ?? new WorkspaceMasterFilter { Name = workspaceId };
    }

    public WorkspaceMasterFilter SaveMasterFilter(string workspaceId, WorkspaceMasterFilter filter)
    {
        var path = GetExistingWorkspacePath(workspaceId);
        filter.Name = string.IsNullOrWhiteSpace(filter.Name) ? workspaceId : filter.Name.Trim();
        filter.StrategiesFilter = filter.StrategiesFilter.Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();
        AtomicFileWriter.WriteAllText(
            Path.Combine(path, MasterFilterFileName),
            JsonSerializer.Serialize(filter, _jsonOptions));
        return filter;
    }

    public string GetWorkspacePath(string workspaceId)
    {
        var id = ToId(workspaceId);
        var path = Path.GetFullPath(Path.Combine(_rootPath, id));
        if (!path.StartsWith(Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Workspace non valido.");
        return path;
    }

    public IReadOnlyList<WorkspaceBacktestInfo> ListBacktests(string workspaceId)
    {
        var workspacePath = GetExistingWorkspacePath(workspaceId);
        var backtestsPath = WorkspaceBacktestPaths.GetBacktestsPath(workspacePath);
        if (!Directory.Exists(backtestsPath))
            return Array.Empty<WorkspaceBacktestInfo>();

        return Directory.EnumerateDirectories(backtestsPath)
            .Select(path =>
            {
                var results = Directory.EnumerateFiles(path, "backtest_*.json", SearchOption.TopDirectoryOnly).Count();
                return new WorkspaceBacktestInfo
                {
                    FolderName = Path.GetFileName(path),
                    FullPath = path,
                    LastModifiedUtc = Directory.GetLastWriteTimeUtc(path),
                    ResultsCount = results
                };
            })
            .OrderByDescending(backtest => backtest.LastModifiedUtc)
            .ToList();
    }

    public string GetBacktestPath(string workspaceId, string folderName)
        => WorkspaceBacktestPaths.ResolveBacktestPath(GetExistingWorkspacePath(workspaceId), folderName);

    public void Delete(string workspaceId)
        => Directory.Delete(GetExistingWorkspacePath(workspaceId), recursive: true);

    private string GetExistingWorkspacePath(string workspaceId)
    {
        var path = GetWorkspacePath(workspaceId);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Workspace '{workspaceId}' non trovato.");
        return path;
    }

    private static string ToId(string value)
    {
        var id = string.Concat(value.Trim().ToLowerInvariant().Select(character =>
            char.IsLetterOrDigit(character) ? character : '-')).Trim('-');
        while (id.Contains("--", StringComparison.Ordinal)) id = id.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Nome workspace non valido.") : id;
    }
}
