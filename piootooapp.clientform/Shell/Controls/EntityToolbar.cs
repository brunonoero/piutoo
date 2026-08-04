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

    public event EventHandler? CreateRequested;

    public event EventHandler? DeleteRequested;

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

    /// <summary>Abilita o disabilita l'eliminazione in base alla selezione corrente.</summary>
    public void SetDeleteEnabled(bool enabled)
    {
        _deleteAllowed = enabled;
        _deleteButton.Enabled = enabled && _deleteButton.Visible;
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
        _refreshButton.Enabled = !busy;

        // Il cursore di attesa va sulla schermata, non sulla barra. La barra è alta quaranta
        // pixel in cima: mentre si aspetta il mouse è sopra la griglia, ed è lì che deve
        // vedersi qualcosa. `UseWaitCursor` si propaga ai figli, `Cursor` no.
        ((Control?)Parent ?? this).UseWaitCursor = busy;
    }

    public void ClearFilter() => _filterBox.Clear();

    private void OnCreateClick(object? sender, EventArgs e) => CreateRequested?.Invoke(this, EventArgs.Empty);

    private void OnDeleteClick(object? sender, EventArgs e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

    private void OnRefreshClick(object? sender, EventArgs e) => RefreshRequested?.Invoke(this, EventArgs.Empty);

    private void OnFilterTextChanged(object? sender, EventArgs e) => FilterChanged?.Invoke(this, EventArgs.Empty);
}
