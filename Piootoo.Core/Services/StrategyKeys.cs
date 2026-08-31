using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Core.Services;

/// <summary>
/// Chiave con cui il backtest e i suoi report identificano una strategia: <c>SYMBOL|StrategyCode</c>.
/// </summary>
/// <remarks>
/// La stessa strategia su due simboli è due serie distinte, quindi il simbolo fa parte della chiave.
/// Il codice è <c>StrategyCode</c> e non <c>Id</c> (vedi CLAUDE.md, "Id ≠ Name"): è ciò che finisce
/// in <c>signals.json</c> e <c>trades.json</c>, cioè l'unica cosa che un run esterno riporta.
/// Vive qui e non dentro un servizio perché la usano sia il loop di backtest sia i generatori di
/// report: due implementazioni della stessa chiave avrebbero raggruppato in modo diverso gli stessi
/// trade.
/// </remarks>
public static class StrategyKeys
{
    public static string NormalizeSymbol(string symbol)
    {
        return symbol.Trim().TrimStart('@').ToUpperInvariant();
    }

    public static string NormalizeSymbolWithPrefix(string symbol)
    {
        var normalized = NormalizeSymbol(symbol);
        return string.IsNullOrEmpty(normalized) ? normalized : $"@{normalized}";
    }

    public static string MakeStrategyKey(string symbol, string strategyCode)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var normalizedStrategyCode = strategyCode.Trim();

        if (string.IsNullOrEmpty(normalizedSymbol))
        {
            return normalizedStrategyCode;
        }

        if (string.IsNullOrEmpty(normalizedStrategyCode))
        {
            return normalizedSymbol;
        }

        return $"{normalizedSymbol}|{normalizedStrategyCode}";
    }

    /// <summary>Codice della strategia, con il nome come ripiego per i risultati storici che non l'avevano.</summary>
    public static string CodeOf(StrategyHourlyResult result)
    {
        return !string.IsNullOrWhiteSpace(result.StrategyCode)
            ? result.StrategyCode
            : result.StrategyName;
    }

    /// <inheritdoc cref="CodeOf(StrategyHourlyResult)"/>
    public static string CodeOf(StrategyInfo info)
    {
        return !string.IsNullOrWhiteSpace(info.StrategyCode)
            ? info.StrategyCode
            : info.Name;
    }
}
