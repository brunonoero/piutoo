using System.ComponentModel;

namespace piootooapp.clientform.Shell.Controls;

/// <summary>Barra comandi delle schermate di dettaglio: indietro, titolo, stato modifiche, salva.</summary>
public partial class DetailToolbar : UserControl
{
    public DetailToolbar()
    {
        InitializeComponent();
    }

    public event EventHandler? BackRequested;

    public event EventHandler? SaveRequested;

    public event EventHandler? RevertRequested;

    [Category("Piootoo"), DefaultValue("")]
    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    /// <summary>Falso nelle schermate di primo livello, che non hanno una lista a cui tornare.</summary>
    [Category("Piootoo"), DefaultValue(true)]
    public bool CanGoBack
    {
        get => _backButton.Visible;
        set => _backButton.Visible = value;
    }

    /// <summary>Falso nelle schermate di sola lettura.</summary>
    [Category("Piootoo"), DefaultValue(true)]
    public bool CanSave
    {
        get => _buttons.Visible;
        set => _buttons.Visible = value;
    }

    public void SetDirty(bool dirty)
    {
        _dirtyLabel.Visible = dirty;
        _revertButton.Enabled = dirty;
    }

    public void SetBusy(bool busy)
    {
        if (IsDisposed)
        {
            return;
        }

        _saveButton.Enabled = !busy;
        _backButton.Enabled = !busy;

        // Come in <see cref="EntityToolbar.SetBusy"/>: il cursore di attesa vale per la
        // schermata, altrimenti si vede solo passando sopra la barra dei comandi.
        ((Control?)Parent ?? this).UseWaitCursor = busy;
    }

    private void OnBackClick(object? sender, EventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    private void OnSaveClick(object? sender, EventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);

    private void OnRevertClick(object? sender, EventArgs e) => RevertRequested?.Invoke(this, EventArgs.Empty);
}
