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

/// <summary>
/// Voce generica id + etichetta per le combo che devono poter esporre anche un valore già
/// persistito ma non più presente nella lista corrente. Scartarlo in silenzio riscriverebbe
/// l'entità azzerando un riferimento che il resto del sistema considera ancora valido.
/// </summary>
public sealed class ValueComboItem
{
    private ValueComboItem(string? id, string display)
    {
        Id = id;
        Display = display;
    }

    /// <summary>Null è la voce "nessuno".</summary>
    public string? Id { get; }

    public string Display { get; }

    public static ValueComboItem None(string label) => new(null, label);

    /// <summary>
    /// Voce "vuota" per le celle di griglia: l'id è stringa vuota e non null, perché il binding
    /// di <c>DataGridViewComboBoxColumn</c> confronta il valore della cella con il ValueMember.
    /// </summary>
    public static ValueComboItem Blank(string label) => new(string.Empty, label);

    public static ValueComboItem Of(string id, string display) => new(id, display);

    public static ValueComboItem Missing(string id) => new(id, $"{id}  ·  (non più presente)");

    public override string ToString() => Display;
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
