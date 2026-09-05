using System.ComponentModel;

namespace piootooapp.clientform.Shell.Controls;

/// <summary>
/// Barra comandi delle liste di anagrafica: titolo, filtro testuale e i tre pulsanti
/// standard. Il controllo non conosce il modello: espone eventi e la schermata decide.
/// </summary>
public partial class EntityToolbar : UserControl
{
    private bool _deleteAllowed;

    public EntityToolbar()
    {
        InitializeComponent();
    }

    private bool _exportAllowed;

    public event EventHandler? CreateRequested;

    public event EventHandler? DeleteRequested;

    /// <summary>
    /// Comando facoltativo che porta fuori dalla console ciò che la lista mostra (per ora l'export
    /// della scheda di una strategia). Nascosto salvo che la schermata lo accenda, così le liste
    /// che non esportano nulla restano com'erano.
    /// </summary>
    public event EventHandler? ExportRequested;

    public event EventHandler? RefreshRequested;

    public event EventHandler? FilterChanged;

    [Category("Piootoo"), DefaultValue("")]
    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    [Category("Piootoo"), DefaultValue("Nuovo")]
    public string CreateButtonText
    {
        get => _createButton.Text;
        set => _createButton.Text = value;
    }

    [Category("Piootoo"), DefaultValue("Filtra…")]
    public string FilterPlaceholder
    {
        get => _filterBox.PlaceholderText;
        set => _filterBox.PlaceholderText = value;
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FilterText => _filterBox.Text.Trim();

    [Category("Piootoo"), DefaultValue(true)]
    public bool CanCreate
    {
        get => _createButton.Visible;
        set => _createButton.Visible = value;
    }

    [Category("Piootoo"), DefaultValue(true)]
    public bool CanDelete
    {
        get => _deleteButton.Visible;
        set => _deleteButton.Visible = value;
    }

    [Category("Piootoo"), DefaultValue(false)]
    public bool CanExport
    {
        get => _exportButton.Visible;
        set => _exportButton.Visible = value;
    }

    [Category("Piootoo"), DefaultValue("Esporta…")]
    public string ExportButtonText
    {
        get => _exportButton.Text;
        set => _exportButton.Text = value;
    }

    /// <summary>Abilita o disabilita l'eliminazione in base alla selezione corrente.</summary>
    public void SetDeleteEnabled(bool enabled)
    {
        _deleteAllowed = enabled;
        _deleteButton.Enabled = enabled && _deleteButton.Visible;
    }

    /// <summary>Abilita o disabilita l'export in base alla selezione corrente.</summary>
    public void SetExportEnabled(bool enabled)
    {
        _exportAllowed = enabled;
        _exportButton.Enabled = enabled && _exportButton.Visible;
    }

    /// <summary>Disabilita i comandi durante una chiamata al server.</summary>
    public void SetBusy(bool busy)
    {
        if (IsDisposed)
        {
            return;
        }

        _createButton.Enabled = !busy && _createButton.Visible;
        _deleteButton.Enabled = !busy && _deleteAllowed && _deleteButton.Visible;
        _exportButton.Enabled = !busy && _exportAllowed && _exportButton.Visible;
        _refreshButton.Enabled = !busy;

        // Il cursore di attesa va sulla schermata, non sulla barra. La barra è alta quaranta
        // pixel in cima: mentre si aspetta il mouse è sopra la griglia, ed è lì che deve
        // vedersi qualcosa. `UseWaitCursor` si propaga ai figli, `Cursor` no.
        ((Control?)Parent ?? this).UseWaitCursor = busy;
    }

    public void ClearFilter() => _filterBox.Clear();

    private void OnCreateClick(object? sender, EventArgs e) => CreateRequested?.Invoke(this, EventArgs.Empty);

    private void OnDeleteClick(object? sender, EventArgs e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

    private void OnExportClick(object? sender, EventArgs e) => ExportRequested?.Invoke(this, EventArgs.Empty);

    private void OnRefreshClick(object? sender, EventArgs e) => RefreshRequested?.Invoke(this, EventArgs.Empty);

    private void OnFilterTextChanged(object? sender, EventArgs e) => FilterChanged?.Invoke(this, EventArgs.Empty);
}
