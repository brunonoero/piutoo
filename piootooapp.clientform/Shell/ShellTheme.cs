namespace piootooapp.clientform.Shell;

internal enum ShellThemeKind
{
    Blue,
    Green,
    Orange,
}

/// <summary>Coppia sfondo/testo di una zona della shell.</summary>
internal readonly record struct ShellZone(Color Background, Color Foreground);

/// <summary>
/// Palette della console selezionabile da menu (Blu/Verde/Arancione). Lo schema è "chiaro con
/// accenti": l'area di lavoro è una superficie chiara e neutra con testo scuro — così griglie,
/// campi e form restano leggibili — mentre il colore della palette resta l'accento e tinge le
/// zone di cornice: header (menu + barra server) nella tonalità più scura, navigazione a sinistra
/// in tonalità intermedia, e i pulsanti / intestazioni di colonna / selezioni nel colore base.
/// Cambiare palette cambia solo la tinta dell'accento, non la struttura.
/// </summary>
internal static class ShellTheme
{
    private static readonly Dictionary<ShellThemeKind, Color> Accents = new()
    {
        [ShellThemeKind.Blue] = ColorTranslator.FromHtml("#2563EB"),
        [ShellThemeKind.Green] = ColorTranslator.FromHtml("#1F9D55"),
        [ShellThemeKind.Orange] = ColorTranslator.FromHtml("#D97706"),
    };

    // Superfici neutre condivise da tutte le palette.
    private static readonly Color SurfaceColor = ColorTranslator.FromHtml("#F4F6F9");
    private static readonly Color CardColor = Color.White;
    private static readonly Color BorderColor = ColorTranslator.FromHtml("#D5DCE5");
    private static readonly Color InkColor = ColorTranslator.FromHtml("#1F2733");
    private static readonly Color MutedInkColor = ColorTranslator.FromHtml("#5B6672");

    public static ShellThemeKind Current { get; private set; } = ShellThemeKind.Blue;

    public static Color Accent => Accents[Current];

    /// <summary>Sfondo dell'area di lavoro: chiaro, neutro, uguale per tutte le palette.</summary>
    public static Color Background => SurfaceColor;

    public static Color Surface => SurfaceColor;

    public static Color Card => CardColor;

    public static Color Border => BorderColor;

    public static Color Ink => InkColor;

    public static Color MutedInk => MutedInkColor;

    public static Color HeaderBackground => Darken(Accent, 0.40);

    public static Color MenuBackground => Darken(Accent, 0.18);

    public static Color ButtonBackground => Lighten(Accent, 0.10);

    public static Color ButtonHoverBackground => Lighten(Accent, 0.26);

    public static Color ButtonPressedBackground => Darken(Accent, 0.12);

    /// <summary>Pulsante disabilitato: fondo grigio neutro, mai accento con testo nero sopra.</summary>
    public static Color ButtonDisabledBackground => ColorTranslator.FromHtml("#E3E8EF");

    public static Color ButtonDisabledForeground => ColorTranslator.FromHtml("#98A2B3");

    /// <summary>Tinta chiarissima dell'accento: righe selezionate, evidenziazioni.</summary>
    public static Color AccentTint => Blend(Accent, Color.White, 0.86);

    public static Color TextColor => Color.White;

    public static ShellZone HeaderZone => new(HeaderBackground, Color.White);

    public static ShellZone MenuZone => new(MenuBackground, Color.White);

    public static ShellZone WorkspaceZone => new(SurfaceColor, InkColor);

    public static void SetTheme(ShellThemeKind kind)
    {
        Current = kind;
    }

    /// <summary>Applica lo stile dell'area di lavoro (screen, dialog) al sottoalbero indicato.</summary>
    public static void Apply(Control root)
    {
        ApplyRecursive(root, WorkspaceZone);
    }

    /// <summary>Applica una zona specifica (header, navigazione, area di lavoro) al sottoalbero indicato.</summary>
    public static void ApplyZone(Control root, ShellZone zone)
    {
        ApplyRecursive(root, zone);
    }

    public static void ApplyMenuStrip(MenuStrip menuStrip)
    {
        menuStrip.Renderer = new ShellToolStripRenderer();
        menuStrip.BackColor = HeaderBackground;
        menuStrip.ForeColor = Color.White;
        menuStrip.Padding = new Padding(6, 2, 6, 2);
        foreach (ToolStripItem item in menuStrip.Items)
        {
            // Voci di primo livello sulla barra scura: testo bianco. I figli aprono su un
            // menu a tendina chiaro e vogliono invece testo scuro.
            item.ForeColor = Color.White;
            ApplyDropDownItems(item);
        }
    }

    public static void ApplyStatusStrip(StatusStrip statusStrip)
    {
        statusStrip.Renderer = new ShellToolStripRenderer();
        statusStrip.BackColor = CardColor;
        statusStrip.ForeColor = MutedInkColor;
        foreach (ToolStripItem item in statusStrip.Items)
        {
            item.ForeColor = MutedInkColor;
        }
    }

    private static void ApplyDropDownItems(ToolStripItem item)
    {
        if (item is not ToolStripDropDownItem parent)
        {
            return;
        }

        parent.DropDown.BackColor = CardColor;
        foreach (ToolStripItem child in parent.DropDownItems)
        {
            child.BackColor = CardColor;
            child.ForeColor = InkColor;
            ApplyDropDownItems(child);
        }
    }

    private static void ApplyRecursive(Control control, ShellZone zone)
    {
        var onDarkZone = IsDark(zone.Background);

        switch (control)
        {
            case Button button:
                StyleButton(button);
                break;
            case TextBox or ComboBox or NumericUpDown or DateTimePicker or ListBox:
                control.BackColor = CardColor;
                control.ForeColor = InkColor;
                if (control is TextBox textBox)
                {
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.FlatStyle = FlatStyle.Flat;
                }
                else if (control is ListBox listBox)
                {
                    listBox.BorderStyle = BorderStyle.FixedSingle;
                }

                break;
            case DataGridView grid:
                StyleGrid(grid);
                break;
            case GroupBox groupBox:
                groupBox.BackColor = zone.Background;
                groupBox.ForeColor = onDarkZone ? Color.White : Accent;
                break;
            case Label or CheckBox or RadioButton or LinkLabel:
                control.BackColor = zone.Background;
                if (IsGrayscale(control.ForeColor))
                {
                    control.ForeColor = zone.Foreground;
                }

                if (control is LinkLabel link)
                {
                    link.LinkColor = onDarkZone ? Color.White : Accent;
                    link.ActiveLinkColor = ButtonHoverBackground;
                }

                break;
            case Form or UserControl or Panel or TabPage or FlowLayoutPanel or TableLayoutPanel or SplitterPanel:
                control.BackColor = zone.Background;
                control.ForeColor = zone.Foreground;
                break;
            case TreeView treeView:
                treeView.BackColor = zone.Background;
                treeView.ForeColor = zone.Foreground;
                treeView.BorderStyle = BorderStyle.None;
                treeView.FullRowSelect = true;
                treeView.HideSelection = false;
                treeView.ShowLines = false;
                treeView.ShowPlusMinus = false;
                treeView.ItemHeight = Math.Max(treeView.ItemHeight, 26);
                treeView.Indent = 18;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyRecursive(child, zone);
        }
    }

    private static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ButtonHoverBackground;
        button.FlatAppearance.MouseDownBackColor = ButtonPressedBackground;
        button.UseVisualStyleBackColor = false;

        // Un pulsante disabilitato con lo sfondo d'accento verrebbe disegnato da WinForms con il
        // testo grigio scuro sul blu: illeggibile. Va spento anche il fondo, e va rifatto ogni
        // volta che cambia Enabled (Remove+Add evita di iscriversi due volte quando si ritema).
        button.EnabledChanged -= OnButtonEnabledChanged;
        button.EnabledChanged += OnButtonEnabledChanged;
        ApplyButtonState(button);
    }

    private static void OnButtonEnabledChanged(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            ApplyButtonState(button);
        }
    }

    private static void ApplyButtonState(Button button)
    {
        if (button.Enabled)
        {
            button.BackColor = ButtonBackground;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
        }
        else
        {
            button.BackColor = ButtonDisabledBackground;
            button.ForeColor = ButtonDisabledForeground;
            button.Cursor = Cursors.Default;
        }
    }

    // Le griglie sono il contenuto principale delle liste: intestazioni nel colore d'accento,
    // righe alternate appena tinte e selezione in tinta chiara per non perdere la leggibilità.
    private static void StyleGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = CardColor;
        grid.GridColor = BorderColor;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.RowHeadersVisible = false;
        grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, 32);

        grid.ColumnHeadersDefaultCellStyle.BackColor = Accent;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Accent;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);

        grid.DefaultCellStyle.BackColor = CardColor;
        grid.DefaultCellStyle.ForeColor = InkColor;
        grid.DefaultCellStyle.SelectionBackColor = AccentTint;
        grid.DefaultCellStyle.SelectionForeColor = InkColor;
        grid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
        grid.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F7F9FC");
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = AccentTint;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = InkColor;
        grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, 26);
    }

    private static Color Darken(Color color, double factor)
    {
        var r = (int)(color.R * (1 - factor));
        var g = (int)(color.G * (1 - factor));
        var b = (int)(color.B * (1 - factor));
        return Color.FromArgb(r, g, b);
    }

    private static Color Lighten(Color color, double factor)
    {
        return Blend(color, Color.White, factor);
    }

    /// <summary>Miscela <paramref name="color"/> verso <paramref name="target"/>: 0 = invariato, 1 = target.</summary>
    private static Color Blend(Color color, Color target, double amount)
    {
        var r = (int)(color.R + (target.R - color.R) * amount);
        var g = (int)(color.G + (target.G - color.G) * amount);
        var b = (int)(color.B + (target.B - color.B) * amount);
        return Color.FromArgb(r, g, b);
    }

    private static bool IsDark(Color color)
    {
        // Luminanza percettiva: sotto ~0.55 il testo bianco è la scelta leggibile.
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        return luminance < 0.55;
    }

    // Le etichette con un colore semantico (es. avviso "modifiche non salvate" in arancione,
    // errore in rosso) non vanno riverniciate: restano riconoscibili solo se non sono grigie/nere.
    private static bool IsGrayscale(Color color)
    {
        return Math.Abs(color.R - color.G) < 8
            && Math.Abs(color.G - color.B) < 8
            && Math.Abs(color.R - color.B) < 8;
    }

    /// <summary>
    /// Renderer piatto per menu e barra di stato: niente gradienti di sistema, barra scura in
    /// tinta con l'header, tendine chiare con evidenziazione nel colore d'accento.
    /// </summary>
    private sealed class ShellToolStripRenderer : ToolStripProfessionalRenderer
    {
        public ShellToolStripRenderer()
            : base(new ShellColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // Sulla barra scura il testo è bianco; dentro la tendina chiara è scuro, e quando la
            // voce è evidenziata torna bianco perché lo sfondo diventa l'accento.
            if (e.Item.Selected && e.Item.OwnerItem is not null)
            {
                e.TextColor = Color.White;
            }
            else if (e.Item.OwnerItem is null && e.ToolStrip is MenuStrip)
            {
                e.TextColor = Color.White;
            }

            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                base.OnRenderToolStripBorder(e);
            }
        }
    }

    private sealed class ShellColorTable : ProfessionalColorTable
    {
        public ShellColorTable()
        {
            UseSystemColors = false;
        }

        public override Color MenuStripGradientBegin => HeaderBackground;

        public override Color MenuStripGradientEnd => HeaderBackground;

        public override Color StatusStripGradientBegin => CardColor;

        public override Color StatusStripGradientEnd => CardColor;

        public override Color MenuItemSelected => Lighten(HeaderBackground, 0.25);

        public override Color MenuItemSelectedGradientBegin => Accent;

        public override Color MenuItemSelectedGradientEnd => Accent;

        public override Color MenuItemPressedGradientBegin => HeaderBackground;

        public override Color MenuItemPressedGradientEnd => HeaderBackground;

        public override Color MenuItemBorder => Accent;

        public override Color MenuBorder => BorderColor;

        public override Color ToolStripDropDownBackground => CardColor;

        public override Color ImageMarginGradientBegin => CardColor;

        public override Color ImageMarginGradientEnd => CardColor;

        public override Color ImageMarginGradientMiddle => CardColor;

        public override Color SeparatorDark => BorderColor;

        public override Color SeparatorLight => CardColor;
    }
}
