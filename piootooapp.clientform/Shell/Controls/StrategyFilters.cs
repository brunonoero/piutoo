namespace piootooapp.clientform.Shell.Controls;

/// <summary>
/// I due filtri comuni alle schermate che elencano strategie — <b>simbolo</b> e <b>inizia con</b> — in
/// un posto solo, cosi' le tre schermate (elenco strategie, masterfilter del workspace, tab Strategie
/// del piano) rispondono allo stesso modo agli stessi controlli.
///
/// <para><b>Inizia con</b> e' un prefisso libero dell'Id di classe, senza distinzione fra maiuscole e
/// minuscole: «PT3» trova le <c>PT3B_*</c>, «PTS_NQ» le traduzioni dei dossier su NQ. Fino al
/// 23/09/2026 al suo posto c'era una combo «Serie» con un elenco chiuso di prefissi (PT3B, PTS,
/// tutte): ogni serie nuova andava aggiunta a mano, e una dimenticata spariva dalle schermate. Il
/// prefisso libero si compone con la ricerca della schermata — «PT3» qui e «DAX» nella ricerca danno
/// tutte le PT3* sul DAX.</para>
///
/// <para><b>Il simbolo</b> si legge dalle strategie caricate, non da un'anagrafica: la combo elenca
/// solo i simboli che compaiono davvero nell'elenco, con «tutti» in testa.</para>
/// </summary>
public static class StrategyFilters
{
    public const string AllSymbols = "(tutti)";

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

    /// <summary>Il prefisso scritto nella casella «inizia con», o <c>null</c> se e' vuota.</summary>
    public static string? WantedPrefix(TextBox box)
    {
        var prefix = box.Text.Trim();
        return prefix.Length == 0 ? null : prefix;
    }

    /// <summary>Se l'Id di classe inizia con il prefisso; <c>null</c> vuol dire nessun filtro.</summary>
    public static bool StartsWithPrefix(string? strategyId, string? wantedPrefix) =>
        wantedPrefix is null
        || (strategyId ?? string.Empty).Trim().StartsWith(wantedPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Se una strategia passa i due filtri; <c>null</c> da un lato vuol dire nessun filtro da quel lato.</summary>
    public static bool Passes(string strategyId, string symbol, string? wantedSymbol, string? wantedPrefix) =>
        (wantedSymbol is null || string.Equals(symbol?.Trim(), wantedSymbol, StringComparison.OrdinalIgnoreCase))
        && StartsWithPrefix(strategyId, wantedPrefix);
}
