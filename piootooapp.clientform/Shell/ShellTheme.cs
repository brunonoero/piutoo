namespace piootooapp.clientform.Shell;

internal enum ShellThemeKind
{
    Blue,
    Green,
    Orange,
}

/// <summary>
/// Palette della console selezionabile da menu (Blu/Verde/Arancione). Ogni palette definisce un
/// colore base e ne derivano tre tonalità per distinguere le zone della shell — header (menu +
/// barra server) più scuro, menu di navigazione a tonalità intermedia, area di lavoro (schermate)
/// nel colore base — più il colore dei bottoni. Testo sempre bianco. I controlli di editing dati
/// (TextBox, ComboBox, DataGridView, TreeView, griglie) restano con lo stile nativo per leggibilità.
/// </summary>
internal static class ShellTheme
{
    private static readonly Dictionary<ShellThemeKind, (Color Background, Color ButtonBackground)> Palettes = new()
    {
        [ShellThemeKind.Blue] = (ColorTranslator.FromHtml("#007AFF"), ColorTranslator.FromHtml("#0580C7")),
        [ShellThemeKind.Green] = (ColorTranslator.FromHtml("#34C759"), ColorTranslator.FromHtml("#248A3D")),
        [ShellThemeKind.Orange] = (ColorTranslator.FromHtml("#FF9500"), ColorTranslator.FromHtml("#C77700")),
    };

    public static ShellThemeKind Current { get; private set; } = ShellThemeKind.Blue;

    public static Color Background => Palettes[Current].Background;

    public static Color ButtonBackground => Palettes[Current].ButtonBackground;

    public static Color HeaderBackground => Darken(Background, 0.45);

    public static Color MenuBackground => Darken(Background, 0.2);

    public static Color TextColor => Color.White;

    public static void SetTheme(ShellThemeKind kind)
    {
        Current = kind;
    }

    /// <summary>Applica il colore dell'area di lavoro (screen, dialog) al sottoalbero indicato.</summary>
    public static void Apply(Control root)
    {
        ApplyRecursive(root, Background);
    }

    /// <summary>Applica un colore di zona specifico (header, menu, area di lavoro) al sottoalbero indicato.</summary>
    public static void ApplyZone(Control root, Color zoneBackground)
    {
        ApplyRecursive(root, zoneBackground);
    }

    public static void ApplyMenuStrip(MenuStrip menuStrip)
    {
        menuStrip.BackColor = HeaderBackground;
        menuStrip.ForeColor = TextColor;
        foreach (ToolStripItem item in menuStrip.Items)
        {
            ApplyToolStripItem(item);
        }
    }

    public static void ApplyStatusStrip(StatusStrip statusStrip)
    {
        statusStrip.BackColor = HeaderBackground;
        foreach (ToolStripItem item in statusStrip.Items)
        {
            ApplyToolStripItem(item);
        }
    }

    private static void ApplyToolStripItem(ToolStripItem item)
    {
        item.ForeColor = TextColor;
        if (item is ToolStripMenuItem menuItem)
        {
            foreach (ToolStripItem child in menuItem.DropDownItems)
            {
                ApplyToolStripItem(child);
            }
        }
    }

    private static void ApplyRecursive(Control control, Color zoneBackground)
    {
        switch (control)
        {
            case Button button:
                button.BackColor = ButtonBackground;
                button.ForeColor = TextColor;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                break;
            case Label or CheckBox or RadioButton or GroupBox or LinkLabel:
                control.BackColor = zoneBackground;
                if (IsGrayscale(control.ForeColor))
                {
                    control.ForeColor = TextColor;
                }
                break;
            case Form or UserControl or Panel or TabPage or FlowLayoutPanel or TableLayoutPanel or SplitterPanel:
                control.BackColor = zoneBackground;
                control.ForeColor = TextColor;
                break;
            case TreeView treeView:
                treeView.BackColor = zoneBackground;
                treeView.ForeColor = TextColor;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyRecursive(child, zoneBackground);
        }
    }

    private static Color Darken(Color color, double factor)
    {
        var r = (int)(color.R * (1 - factor));
        var g = (int)(color.G * (1 - factor));
        var b = (int)(color.B * (1 - factor));
        return Color.FromArgb(r, g, b);
    }

    // Le etichette con un colore semantico (es. avviso "modifiche non salvate" in arancione,
    // errore in rosso) non vanno sbiancate: restano riconoscibili solo se non sono grigie/nere.
    private static bool IsGrayscale(Color color)
    {
        return Math.Abs(color.R - color.G) < 8
            && Math.Abs(color.G - color.B) < 8
            && Math.Abs(color.R - color.B) < 8;
    }
}
