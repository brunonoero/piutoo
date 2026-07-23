namespace Piootoo.Core.Services;

/// <summary>Risoluzione sicura delle cartelle backtest contenute in un workspace.</summary>
public static class WorkspaceBacktestPaths
{
    public const string BacktestsDirectoryName = "backtests";

    public static string NormalizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Il nome del backtest è obbligatorio.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed is "." or ".." || trimmed.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Il nome del backtest non può contenere '..'.", nameof(name));

        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var normalized = string.Concat(trimmed.Select(character =>
            char.IsWhiteSpace(character) ? '-' :
            invalid.Contains(character) || character is '/' or '\\' ? '-' :
            char.ToLowerInvariant(character)));

        while (normalized.Contains("--", StringComparison.Ordinal))
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);

        normalized = normalized.Trim(' ', '.', '-');
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("Il nome del backtest non contiene caratteri validi.", nameof(name));

        if (normalized.Length > 80)
            normalized = normalized[..80].TrimEnd(' ', '.', '-');

        return normalized;
    }

    public static string GetBacktestsPath(string workspacePath)
        => EnsureChildPath(workspacePath, BacktestsDirectoryName);

    public static string ResolveBacktestPath(string workspacePath, string folderName)
        => EnsureChildPath(GetBacktestsPath(workspacePath), NormalizeFolderName(folderName));

    private static string EnsureChildPath(string parentPath, string childName)
    {
        var parent = Path.GetFullPath(parentPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(parent, childName));
        var prefix = parent + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Il percorso del backtest non è valido.", nameof(childName));
        return candidate;
    }
}
