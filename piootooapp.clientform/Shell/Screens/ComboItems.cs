using Piootoo.Shared.Models.Workspaces;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Voci condivise dalle combo delle schermate operative.</summary>
public sealed class WorkspaceComboItem
{
    public WorkspaceComboItem(WorkspaceInfo info) => Info = info;

    public WorkspaceInfo Info { get; }

    public override string ToString() => $"{Info.Name}  ({Info.Id})";
}

public sealed class BacktestComboItem
{
    public BacktestComboItem(WorkspaceBacktestInfo info) => Info = info;

    public WorkspaceBacktestInfo Info { get; }

    public override string ToString()
        => $"{Info.FolderName}  ·  {Info.LastModifiedUtc:yyyy-MM-dd HH:mm} UTC" +
           (Info.ResultsCount > 0 ? $"  ·  {Info.ResultsCount} risultati" : "  ·  nessun risultato");
}

public sealed class AccountComboItem
{
    public AccountComboItem(WorkspaceAccount? account) => Account = account;

    /// <summary>Null è la voce "nessuna conversione", cioè il run gira 1 a 1.</summary>
    public WorkspaceAccount? Account { get; }

    public override string ToString()
        => Account == null
            ? "(nessuna conversione)"
            : $"{Account.Name}  ·  {Account.SymbolMappings.Count} simboli" +
              (string.IsNullOrWhiteSpace(Account.GroupId) ? string.Empty : $"  ·  gruppo {Account.GroupId}");
}
