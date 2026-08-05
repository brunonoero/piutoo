using Piootoo.Core.Services;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Anagrafica minima degli account nel registro globale, per i test che fanno claim.
///
/// <para><c>TradingSessionService.ResolveAccountConversion</c> pretende che ogni numero di conto
/// configurato sulla sessione esista nel registro: senza anagrafica non sa risolvere capitale e
/// tabella di conversione simboli, e fallisce esplicitamente invece di operare 1 a 1. Un test che
/// polla un account deve quindi registrarlo, esattamente come farebbe l'operatore.</para>
///
/// <para>Gli account creati qui non hanno <c>SymbolConversionCode</c>: operano 1 a 1, così i test
/// sui lucchetti e sui limiti misurano quello che vogliono misurare senza che la conversione
/// alteri le quantità.</para>
/// </summary>
internal static class TestAccountRegistry
{
    internal const decimal DefaultBalance = 100_000m;

    /// <summary>Registra un account per ogni numero di conto distinto delle righe di gruppo.</summary>
    internal static void Register(WorkspaceService workspaces, IEnumerable<TradingGroupRow>? groups)
    {
        foreach (var row in (groups ?? [])
                     .Where(x => !string.IsNullOrWhiteSpace(x.AccountNumber))
                     .GroupBy(x => x.AccountNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                     .Select(g => g.First()))
        {
            Register(workspaces, row.AccountNumber, row.GroupId);
        }
    }

    /// <summary>Registra i numeri di conto indicati, tutti nello stesso gruppo.</summary>
    internal static void Register(WorkspaceService workspaces, params string[] accountNumbers)
    {
        foreach (var accountNumber in accountNumbers)
            Register(workspaces, accountNumber, groupId: string.Empty);
    }

    private static void Register(WorkspaceService workspaces, string accountNumber, string? groupId)
    {
        var number = accountNumber.Trim();
        if (workspaces.ListAccounts().Any(existing =>
                string.Equals(existing.AccountNumber?.Trim(), number, StringComparison.OrdinalIgnoreCase)))
            return;

        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = $"acc-{number}",
            AccountNumber = number,
            GroupId = groupId?.Trim() ?? string.Empty,
            InitialBalance = DefaultBalance,
            Enabled = true
        });
    }
}
