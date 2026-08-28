using Piootoo.Shared.Models.Strategies;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Scheda di una strategia del catalogo. Sola lettura: la sorgente è codice compilato.</summary>
public partial class StrategyDetailScreen : UserControl, IShellScreen
{
    private StrategyCatalogItem? _strategy;
    private ShellContext? _context;

    public StrategyDetailScreen()
    {
        InitializeComponent();
    }

    public string ScreenTitle => _strategy?.Name is { Length: > 0 } name ? name : "Strategia";

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell.</summary>
    public void SetStrategy(StrategyCatalogItem strategy) => _strategy = strategy;

    public void Initialize(ShellContext context) => _context = context;

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_strategy == null)
        {
            return Task.CompletedTask;
        }

        _toolbar.Title = _strategy.Name;
        _idTextBox.Text = _strategy.Id;
        _nameTextBox.Text = _strategy.Name;
        _codeTextBox.Text = _strategy.Code;
        _symbolTextBox.Text = _strategy.Symbol;
        _timeframeTextBox.Text = _strategy.TimeframeMinutes > 0 ? $"{_strategy.TimeframeMinutes} minuti" : "—";
        _barTypeTextBox.Text = _strategy.BarType;
        _typeTextBox.Text = _strategy.Type;
        _activeTextBox.Text = _strategy.IsActive ? "sì" : "no";
        // Dichiarazione della strategia, non permesso: il piano che la esegue puo' troncarla.
        _holdingTextBox.Text = _strategy.Overnight
            ? (_strategy.Overweek
                ? "overnight + overweek (il piano puo' troncarla)"
                : "overnight (il piano puo' troncarla)")
            : "intraday: chiude a fine sessione";
        _sourceTextBox.Text = _strategy.SourceFileName;
        _descriptionTextBox.Text = string.IsNullOrWhiteSpace(_strategy.Description)
            ? "(nessuna descrizione nel catalogo)"
            : _strategy.Description;

        _context?.Navigation.SetStatus(
            $"'{_strategy.Name}' selezionabile nel masterfilter con l'id di classe '{_strategy.Id}'.");
        return Task.CompletedTask;
    }

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();
}
