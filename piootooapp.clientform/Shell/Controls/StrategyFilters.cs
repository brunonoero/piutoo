namespace piootooapp.clientform.Shell.Controls;

/// <summary>
/// I due filtri comuni alle schermate che elencano strategie — <b>simbolo</b> e <b>serie</b> — in
/// un posto solo, cosi' le tre schermate (elenco strategie, masterfilter del workspace, tab Strategie
/// del piano) rispondono allo stesso modo alle stesse combo.
///
/// <para><b>La serie</b> e' il prefisso dell'Id di classe: <c>PTS_*</c> sono le traduzioni dei dossier
/// di agosto e settembre, <c>PT2_*</c> quelle del paniere rifatto di <c>run-engine-v2/</c>
/// (16/09/2026), <c>PT3B_*</c> quelle nate dalla ricerca interna con <c>piootoo-sweep</c>
/// (21/09/2026). Le voci sono in ordine di eta', dalla piu' recente. Il default resta <b>PT2</b>:
/// cambiarlo mostrerebbe un elenco vuoto a chi apre un piano costruito su un'altra serie, che
/// sembra un guasto e non un filtro. La voce «tutte» resta per guardare l'intero catalogo e per le
/// classi fuori serie.</para>
///
/// <para><b>Il simbolo</b> si legge dalle strategie caricate, non da un'anagrafica: la combo elenca
/// solo i simboli che compaiono davvero nell'elenco, con «tutti» in testa.</para>
/// </summary>
public static class StrategyFilters
{
    public const string AllSymbols = "(tutti)";
    public const string AllSeries = "(tutte)";
    public const string SeriesPt3b = "PT3B";
    public const string SeriesPt2 = "PT2";
    public const string SeriesPts = "PTS";

    /// <summary>La serie a cui appartiene un Id di classe: il prefisso prima del primo <c>_</c>, oppure «altro».</summary>
    public static string SeriesOf(string? strategyId)
    {
        if (string.IsNullOrWhiteSpace(strategyId))
        {
            return "altro";
        }

        var underscore = strategyId.IndexOf('_');
        var prefix = underscore > 0 ? strategyId[..underscore] : strategyId;
        return prefix.ToUpperInvariant() switch
        {
            SeriesPt3b => SeriesPt3b,
            SeriesPt2 => SeriesPt2,
            SeriesPts => SeriesPts,
            _ => "altro"
        };
    }

    /// <summary>
    /// Riempie la combo delle serie una volta sola, in ordine di eta' e con PT2 selezionata.
    /// La selezione si dichiara per <b>valore</b> e non per indice: aggiungendo una serie in testa,
    /// un indice avrebbe cambiato il default in silenzio.
    /// </summary>
    public static void InitializeSeries(ComboBox combo)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Items.Clear();
        combo.Items.Add(SeriesPt3b);
        combo.Items.Add(SeriesPt2);
        combo.Items.Add(SeriesPts);
        combo.Items.Add(AllSeries);
        combo.SelectedItem = SeriesPt2;
    }

    /// <summary>
    /// Riempie la combo dei simboli con «tutti» piu' i simboli dati, ordinati e senza doppioni,
    /// conservando la selezione corrente se ancora presente. Va richiamata a ogni caricamento:
    /// l'elenco dei simboli e' quello delle strategie mostrate, non un'anagrafica.
    /// </summary>
    public static void SetSymbols(ComboBox combo, IEnumerable<string> symbols)
    {
        var current = SelectedSymbol(combo);
        var items = symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToList();

        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.BeginUpdate();
        combo.Items.Clear();
        combo.Items.Add(AllSymbols);
        foreach (var symbol in items)
        {
            combo.Items.Add(symbol);
        }

        var index = current is null
            ? 0
            : items.FindIndex(symbol => string.Equals(symbol, current, StringComparison.OrdinalIgnoreCase)) + 1;
        combo.SelectedIndex = index >= 0 ? index : 0;
        combo.EndUpdate();
    }

    /// <summary>Il simbolo scelto, o <c>null</c> per «tutti».</summary>
    public static string? SelectedSymbol(ComboBox combo) =>
        combo.SelectedItem is string symbol && symbol != AllSymbols ? symbol : null;

    /// <summary>La serie scelta, o <c>null</c> per «tutte».</summary>
    public static string? SelectedSeries(ComboBox combo) =>
        combo.SelectedItem is string series && series != AllSeries ? series : null;

    /// <summary>Se una strategia passa i due filtri; <c>null</c> da un lato vuol dire nessun filtro da quel lato.</summary>
    public static bool Passes(string strategyId, string symbol, string? wantedSymbol, string? wantedSeries) =>
        (wantedSymbol is null || string.Equals(symbol?.Trim(), wantedSymbol, StringComparison.OrdinalIgnoreCase))
        && (wantedSeries is null || string.Equals(SeriesOf(strategyId), wantedSeries, StringComparison.OrdinalIgnoreCase));
}
