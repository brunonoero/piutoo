using Piootoo.Shared.Models.Trading;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un conto della prova, con il tetto di concorrenza che il piano gli imporrebbe.
///
/// <para>Ha preso il posto di <c>TradingGroupRow</c> nei test dopo la rimozione dei gruppi
/// (<c>docs/decisioni.md</c> 2026-09-05): un conto è un destinatario e basta. Il tetto resta sulla
/// riga solo per comodità di scrittura dei test — nel dominio è uno per sessione, dichiarato dal
/// piano, e <see cref="Apply"/> lo prende dalla prima riga.</para>
/// </summary>
internal sealed record TestAccountRow(
    string AccountNumber,
    int MaxConcurrentTrades = 0,
    ConcurrencyCountMode CountMode = ConcurrencyCountMode.PositionsAndPendingOrders);

internal static class TestSessionAccounts
{
    /// <summary>I soli numeri di conto, nell'ordine dichiarato.</summary>
    internal static IReadOnlyList<string> Numbers(IEnumerable<TestAccountRow>? rows) =>
        (rows ?? []).Select(row => row.AccountNumber).ToArray();

    /// <summary>Il tetto della sessione: quello della prima riga, come fa il piano.</summary>
    internal static int MaxConcurrentTrades(IReadOnlyList<TestAccountRow>? rows) =>
        rows is { Count: > 0 } ? rows[0].MaxConcurrentTrades : 0;

    /// <inheritdoc cref="MaxConcurrentTrades"/>
    internal static ConcurrencyCountMode CountMode(IReadOnlyList<TestAccountRow>? rows) =>
        rows is { Count: > 0 } ? rows[0].CountMode : ConcurrencyCountMode.PositionsAndPendingOrders;
}
