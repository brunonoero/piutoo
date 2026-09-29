namespace Piootoo.Core.Planning;

/// <summary>Un regime di uno dei due assi, con il nome che il resoconto mostra.</summary>
public readonly record struct RegimeKey(string Axis, string Name)
{
    public static readonly IReadOnlyList<RegimeKey> All =
    [
        new("direzione", "trend-su"), new("direzione", "trend-giu"), new("direzione", "laterale"),
        new("volatilita'", "calma"), new("volatilita'", "normale"), new("volatilita'", "agitata")
    ];

    public static IEnumerable<RegimeKey> Of(DayRegime regime)
    {
        yield return All[regime.Direction switch
        {
            DirectionRegime.TrendUp => 0,
            DirectionRegime.TrendDown => 1,
            _ => 2
        }];
        yield return All[regime.Volatility switch
        {
            VolatilityRegime.Calm => 3,
            VolatilityRegime.Normal => 4,
            _ => 5
        }];
    }
}

/// <summary>Una strategia (o un piano) in un regime: trade entrati in quel regime e il loro netto.</summary>
public sealed record RegimeCell(RegimeKey Regime, int Trades, decimal Net)
{
    public decimal AverageTrade => Trades > 0 ? Net / Trades : 0m;
}

/// <summary>Una riga del controllo di un piano: il piano in un regime e quante sue strategie ci perdono.</summary>
public sealed record PlanRegimeRow(RegimeKey Regime, int Trades, decimal Net, int MembersMeasured, int MembersLosing)
{
    /// <summary>Il piano perde in questo regime.</summary>
    public bool PlanLoses => Net < 0m;

    /// <summary>Almeno due strategie misurabili, e tutte in perdita: perdono insieme.</summary>
    public bool AllLose => MembersMeasured >= 2 && MembersLosing == MembersMeasured;
}

/// <summary>Il controllo di un piano: una riga per regime, e i trade rimasti senza etichetta.</summary>
public sealed record PlanRegimeProfile(int PlanNumber, IReadOnlyList<PlanRegimeRow> Rows, int UnlabeledTrades)
{
    public IEnumerable<PlanRegimeRow> Warnings => Rows.Where(r => r.PlanLoses || r.AllLose);
}

/// <summary>Tutto il controllo per regime: le fonti, i piani, il profilo delle candidate.</summary>
public sealed record PlanRegimeReport(
    string DatafeedRoot,
    IReadOnlyDictionary<string, SymbolRegimes> Symbols,
    IReadOnlyDictionary<string, string> MissingSymbols,
    int MinTradesPerMember,
    IReadOnlyList<PlanRegimeProfile> Plans,
    IReadOnlyDictionary<string, IReadOnlyList<RegimeCell>> Candidates);

/// <summary>
/// <b>Un piano non deve perdere tutto nello stesso regime di mercato.</b> Il controllo si fa dopo la
/// costruzione e non la cambia: la scorrelazione giornaliera e' il vincolo, il regime e' la verifica di
/// cio' che ne e' uscito. Non diventa un vincolo, ne' un interruttore sulle strategie: il profilo per regime
/// di una strategia non persiste da un tratto della storia al successivo
/// (<c>ricerca/regimi-2026-09-29/esito.md</c>), quello di un piano scorrelato si'.
///
/// <para>Ogni trade prende il regime del <b>proprio</b> simbolo nel giorno di sessione in cui entra (vedi
/// <see cref="MarketRegimeClassifier"/>). Per ogni piano e regime: trade, netto, e quante strategie con
/// almeno <c>minTradesPerMember</c> trade in quel regime ci perdono. Un regime e' segnalato quando il piano
/// ci perde, o quando ci perdono tutte le sue strategie misurabili (almeno due).</para>
/// </summary>
public static class PlanRegimeCheck
{
    public static PlanRegimeReport Evaluate(
        PlanBuilderResult result,
        IEnumerable<PlanTrade> trades,
        string datafeedRoot,
        IReadOnlyDictionary<string, SymbolRegimes> symbols,
        IReadOnlyDictionary<string, string> missing,
        int minTradesPerMember = 10)
    {
        var candidates = result.Candidates.Select(c => c.Code).ToHashSet(StringComparer.Ordinal);
        var cells = new Dictionary<string, Dictionary<RegimeKey, (int Trades, decimal Net)>>(StringComparer.Ordinal);
        var unlabeled = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var trade in trades)
        {
            if (!candidates.Contains(trade.StrategyCode))
                continue;

            var regime = trade.EntryUtc is { } entry && symbols.TryGetValue(trade.Symbol, out var labels)
                ? labels.At(entry)
                : null;
            if (regime is null)
            {
                unlabeled[trade.StrategyCode] = unlabeled.GetValueOrDefault(trade.StrategyCode) + 1;
                continue;
            }

            if (!cells.TryGetValue(trade.StrategyCode, out var byRegime))
                cells[trade.StrategyCode] = byRegime = new Dictionary<RegimeKey, (int, decimal)>();
            foreach (var key in RegimeKey.Of(regime.Value))
            {
                var (count, net) = byRegime.GetValueOrDefault(key);
                byRegime[key] = (count + 1, net + trade.NetProfit);
            }
        }

        IReadOnlyList<RegimeCell> CellsOf(string code) =>
            RegimeKey.All.Select(key =>
            {
                var (count, net) = cells.TryGetValue(code, out var byRegime) ? byRegime.GetValueOrDefault(key) : default;
                return new RegimeCell(key, count, net);
            }).ToList();

        var profiles = result.Candidates.ToDictionary(c => c.Code, c => CellsOf(c.Code), StringComparer.Ordinal);

        var plans = result.Plans.Select(plan =>
        {
            var rows = RegimeKey.All.Select(key =>
            {
                var memberCells = plan.Members.Select(m => profiles[m.Code].Single(c => c.Regime == key)).ToList();
                var measured = memberCells.Where(c => c.Trades >= minTradesPerMember).ToList();
                return new PlanRegimeRow(
                    key,
                    memberCells.Sum(c => c.Trades),
                    memberCells.Sum(c => c.Net),
                    measured.Count,
                    measured.Count(c => c.Net < 0m));
            }).ToList();

            return new PlanRegimeProfile(plan.Number, rows, plan.Members.Sum(m => unlabeled.GetValueOrDefault(m.Code)));
        }).ToList();

        return new PlanRegimeReport(datafeedRoot, symbols, missing, minTradesPerMember, plans, profiles);
    }
}
