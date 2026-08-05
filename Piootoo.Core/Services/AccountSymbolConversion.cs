using System.Diagnostics.CodeAnalysis;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>Riga risolta della tabella di conversione di un account.</summary>
public sealed record AccountSymbolConversionEntry(
    string AccountSymbol,
    decimal ContractMultiplier,
    bool Enabled,
    decimal MinimumQuantity,
    decimal QuantityStep,
    QuantityRoundingMode RoundingMode);

/// <summary>
/// Tabella di conversione di un account in forma pronta per il loop caldo: il lookup avviene per
/// simbolo Piootoo normalizzato (senza <c>@</c>, maiuscolo), la stessa normalizzazione usata dal
/// backtest e dal motore di trading.
///
/// <para>Regole quando un simbolo non è in tabella: nessuna conversione (simbolo invariato,
/// moltiplicatore 1). Un simbolo mappato ma disabilitato non è operativo su quell'account e i suoi
/// segnali vengono scartati.</para>
///
/// <para>La size dell'account è il prodotto di due fattori indipendenti: la
/// <see cref="BalanceScale"/>, che rapporta il capitale del conto al milione di riferimento delle
/// strategie, e il <c>ContractMultiplier</c> della riga, che rapporta il contratto del broker a
/// quello Piootoo. Il primo è una proprietà del conto, il secondo dello strumento: tenerli
/// separati permette di cambiare il capitale senza ricalcolare a mano tutte le righe.</para>
/// </summary>
public sealed class AccountSymbolConversion
{
    /// <summary>
    /// Capitale rispetto al quale sono dimensionate le quantità dichiarate dalle strategie: un
    /// segnale da un contratto vale un contratto su un conto da un milione. È lo stesso numero
    /// proposto come capitale iniziale del backtest interno, e vive in un solo posto proprio perché
    /// i due usi devono restare d'accordo.
    /// </summary>
    public const decimal ReferenceBalance = TradingConventions.StrategyReferenceBalance;

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

    /// <summary>
    /// Rapporto fra il capitale del conto e <see cref="ReferenceBalance"/>: un conto da 250.000
    /// opera un quarto della size dichiarata dalle strategie. Un balance non valorizzato vale 1
    /// (nessuna scala) invece che zero: azzerare tutte le size su un dato mancante nasconderebbe
    /// l'errore di configurazione dietro un run senza trade.
    /// </summary>
    public decimal BalanceScale => InitialBalance > 0 ? InitialBalance / ReferenceBalance : 1m;

    public bool IsIdentity => _entries.Count == 0 && BalanceScale == 1m;

    /// <summary>
    /// <paramref name="mappings"/> è la tabella già risolta dal codice di conversione dell'account
    /// (<see cref="WorkspaceAccount.SymbolConversionCode"/>) nel registro globale delle
    /// <c>SymbolConversion</c>: vuota se l'account non ne referenzia una.
    /// </summary>
    public static AccountSymbolConversion FromAccount(
        WorkspaceAccount account, IReadOnlyList<AccountSymbolMapping> mappings)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(mappings);

        var entries = new Dictionary<string, AccountSymbolConversionEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in mappings)
        {
            var key = NormalizeSymbol(mapping.Symbol);
            if (key.Length == 0) continue;
            entries[key] = new AccountSymbolConversionEntry(
                string.IsNullOrWhiteSpace(mapping.AccountSymbol) ? mapping.Symbol.Trim() : mapping.AccountSymbol.Trim(),
                mapping.ContractMultiplier <= 0 ? 1m : mapping.ContractMultiplier,
                mapping.Enabled,
                mapping.MinimumQuantity <= 0 ? 1m : mapping.MinimumQuantity,
                mapping.QuantityStep <= 0 ? 1m : mapping.QuantityStep,
                mapping.RoundingMode);
        }

        return new AccountSymbolConversion(account.Id, account.Name, account.InitialBalance, entries);
    }

    /// <summary>Stessa normalizzazione del backtest: <c>@NQ</c> e <c>nq</c> collassano su <c>NQ</c>.</summary>
    public static string NormalizeSymbol(string? symbol)
        => symbol is null ? string.Empty : symbol.Trim().TrimStart('@').ToUpperInvariant();

    public bool TryGet(string? symbol, [MaybeNullWhen(false)] out AccountSymbolConversionEntry entry)
        => _entries.TryGetValue(NormalizeSymbol(symbol), out entry);

    /// <summary>Rapporto fra contratto broker e contratto Piootoo; 1 se il simbolo non è mappato.</summary>
    public decimal GetContractMultiplier(string? symbol)
        => TryGet(symbol, out var entry) ? entry.ContractMultiplier : 1m;

    /// <summary>
    /// Fattore complessivo con cui scalare la quantità di un segnale su questo account:
    /// capitale del conto per contratto del broker.
    /// </summary>
    public decimal GetSizeFactor(string? symbol) => BalanceScale * GetContractMultiplier(symbol);

    /// <summary>Simbolo del broker; il simbolo originale se non è mappato.</summary>
    public string GetAccountSymbol(string? symbol)
        => TryGet(symbol, out var entry) ? entry.AccountSymbol : symbol?.Trim() ?? string.Empty;

    /// <summary>False solo se il simbolo è mappato ed è stato disabilitato sull'account.</summary>
    public bool IsSymbolEnabled(string? symbol)
        => !TryGet(symbol, out var entry) || entry.Enabled;

    /// <summary>
    /// Arrotonda una quantità già convertita nei contratti del broker alla granularità di volume di
    /// questo account/simbolo: passo di volume per difetto, zero sotto la quantità minima (meglio
    /// nessun ordine che un ordine di taglia non eseguibile).
    ///
    /// <para>Se il simbolo non è mappato non c'è una granularità dichiarata dal broker: si applica
    /// comunque il default <see cref="QuantityRoundingMode.FuturesContracts"/> (contratto intero,
    /// minimo 1) invece di lasciare passare una quantità frazionaria. "Nessuna conversione" vale per
    /// simbolo e moltiplicatore (vedi <see cref="GetContractMultiplier"/>), non per la granularità:
    /// un future non diventa negoziabile a frazioni di contratto solo perché manca la riga in
    /// tabella.</para>
    /// </summary>
    public decimal RoundQuantity(string? symbol, decimal quantity)
    {
        if (quantity <= 0m) return quantity;

        var (step, minimum) = TryGet(symbol, out var entry)
            ? (entry.RoundingMode == QuantityRoundingMode.FuturesContracts
                ? Math.Max(1m, entry.QuantityStep)
                : entry.QuantityStep, entry.MinimumQuantity)
            : (1m, 1m);
        var rounded = step > 0m ? Math.Floor(quantity / step) * step : quantity;
        return rounded < minimum ? 0m : rounded;
    }
}
