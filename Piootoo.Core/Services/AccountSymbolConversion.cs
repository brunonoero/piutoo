using System.Diagnostics.CodeAnalysis;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>Riga risolta della tabella di conversione di un account.</summary>
public sealed record AccountSymbolConversionEntry(
    string AccountSymbol,
    decimal ContractMultiplier,
    bool Enabled);

/// <summary>
/// Tabella di conversione di un account in forma pronta per il loop caldo: il lookup avviene per
/// simbolo Piootoo normalizzato (senza <c>@</c>, maiuscolo), la stessa normalizzazione usata dal
/// backtest e dal motore di trading.
///
/// <para>Regole quando un simbolo non è in tabella: nessuna conversione (simbolo invariato,
/// moltiplicatore 1). Un simbolo mappato ma disabilitato non è operativo su quell'account e i suoi
/// segnali vengono scartati.</para>
/// </summary>
public sealed class AccountSymbolConversion
{
    private static readonly Dictionary<string, AccountSymbolConversionEntry> Empty =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, AccountSymbolConversionEntry> _entries;

    private AccountSymbolConversion(
        string accountId,
        string accountName,
        decimal initialBalance,
        Dictionary<string, AccountSymbolConversionEntry> entries)
    {
        AccountId = accountId;
        AccountName = accountName;
        InitialBalance = initialBalance;
        _entries = entries;
    }

    /// <summary>Conversione neutra: nessun account selezionato, tutto 1 a 1.</summary>
    public static AccountSymbolConversion Identity { get; } =
        new(string.Empty, string.Empty, 0m, Empty);

    public string AccountId { get; }
    public string AccountName { get; }
    public decimal InitialBalance { get; }
    public bool IsIdentity => _entries.Count == 0;

    public static AccountSymbolConversion FromAccount(WorkspaceAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var entries = new Dictionary<string, AccountSymbolConversionEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in account.SymbolMappings)
        {
            var key = NormalizeSymbol(mapping.Symbol);
            if (key.Length == 0) continue;
            entries[key] = new AccountSymbolConversionEntry(
                string.IsNullOrWhiteSpace(mapping.AccountSymbol) ? mapping.Symbol.Trim() : mapping.AccountSymbol.Trim(),
                mapping.ContractMultiplier <= 0 ? 1m : mapping.ContractMultiplier,
                mapping.Enabled);
        }

        return new AccountSymbolConversion(account.Id, account.Name, account.InitialBalance, entries);
    }

    /// <summary>Stessa normalizzazione del backtest: <c>@NQ</c> e <c>nq</c> collassano su <c>NQ</c>.</summary>
    public static string NormalizeSymbol(string? symbol)
        => symbol is null ? string.Empty : symbol.Trim().TrimStart('@').ToUpperInvariant();

    public bool TryGet(string? symbol, [MaybeNullWhen(false)] out AccountSymbolConversionEntry entry)
        => _entries.TryGetValue(NormalizeSymbol(symbol), out entry);

    /// <summary>Fattore di scala della size; 1 se il simbolo non è mappato.</summary>
    public decimal GetContractMultiplier(string? symbol)
        => TryGet(symbol, out var entry) ? entry.ContractMultiplier : 1m;

    /// <summary>Simbolo del broker; il simbolo originale se non è mappato.</summary>
    public string GetAccountSymbol(string? symbol)
        => TryGet(symbol, out var entry) ? entry.AccountSymbol : symbol?.Trim() ?? string.Empty;

    /// <summary>False solo se il simbolo è mappato ed è stato disabilitato sull'account.</summary>
    public bool IsSymbolEnabled(string? symbol)
        => !TryGet(symbol, out var entry) || entry.Enabled;
}
