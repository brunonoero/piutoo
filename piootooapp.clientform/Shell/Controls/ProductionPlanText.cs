using System.Text;
using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Controls;

/// <summary>
/// Un piano di produzione a parole, in un punto solo: la stessa scheda nell'anteprima della
/// promozione e nel dettaglio del broker workspace, cosi' cio' che si conferma e' cio' che poi si legge.
/// </summary>
internal static class ProductionPlanText
{
    public static string Describe(TradingPlan plan)
    {
        var text = new StringBuilder();
        void Line(string label, object? value) => text.Append($"{label,-16}{value}\n");

        Line("Piano", plan.Name.Length > 0 && plan.Name != plan.Code ? $"{plan.Code} — {plan.Name}" : plan.Code);
        Line("Broker", plan.BrokerCode);
        Line("Stato", plan.RetiredUtc is { } retired ? $"ritirato il {retired:yyyy-MM-dd HH:mm} UTC" : "attivo");
        Line("Conti", string.Join(", ", plan.Accounts));
        Line("Size (k)", $"{plan.SizeMultiplier:0.###}");
        Line("Commissione", $"{plan.CommissionPerContract:0.##} per contratto");
        Line("Tenuta", DescribeHolding(plan.Holding));
        Line("Concorrenza", plan.MaxConcurrentTrades > 0
            ? $"massimo {plan.MaxConcurrentTrades} per conto ({plan.ConcurrencyCountMode})"
            : "nessun tetto");

        var strategies = plan.EnabledStrategies ?? [];
        Line("Strategie", strategies.Count);
        foreach (var id in strategies)
        {
            var weight = plan.StrategyWeights.TryGetValue(id, out var value) ? value : 1m;
            text.Append($"  {id,-34} peso {weight:0.###}\n");
        }

        if (plan.Provenance is { } provenance)
        {
            text.Append('\n');
            Line("Best plan", provenance.BestPlanId);
            Line("Da workspace", $"{provenance.SourceWorkspaceId}, piano {provenance.SourcePlanCode}");
            Line("Backtest", provenance.BacktestFolder);
            Line("Run del (UTC)", $"{provenance.RunCreatedUtc:yyyy-MM-dd HH:mm}");
            Line("Promosso (UTC)", $"{provenance.PromotedUtc:yyyy-MM-dd HH:mm}");
            if (!string.IsNullOrEmpty(provenance.PreviousPlanCode))
            {
                Line("Sostituisce", provenance.PreviousPlanCode);
            }
        }

        return text.ToString();
    }

    private static string DescribeHolding(AccountHoldingPolicy holding)
        => $"overnight {(holding.AllowOvernight ? "si'" : "no")}, overweek {(holding.AllowOverweek ? "si'" : "no")}, " +
           $"flat di sessione {holding.SessionFlatUtc:HH:mm} UTC per {holding.SessionFlatWindowMinutes} min";
}
