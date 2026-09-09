using System.Text;
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

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_strategy == null)
        {
            return;
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

        await LoadHoursAsync(cancellationToken);
    }

    /// <summary>
    /// La scheda oraria, che è l'unica parte di questa schermata a costare una chiamata al server:
    /// due dei tre piani orari vengono dal calendario di mercato, che la console non ha.
    ///
    /// <para>Un errore qui non svuota la schermata: il resto della scheda è già a video e ciò che
    /// manca lo dice il riquadro, invece di restare vuoto e sembrare "nessun orario dichiarato".</para>
    /// </summary>
    private async Task LoadHoursAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _strategy == null)
        {
            return;
        }

        _hoursTextBox.Text = "Lettura degli orari dal server…";
        _toolbar.SetBusy(true);
        try
        {
            var card = await _context.Services.Api.GetStrategyHoursAsync(_strategy.Id, cancellationToken);
            _hoursTextBox.Text = Render(card);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _hoursTextBox.Text = $"Orari non disponibili: {ex.Message}";
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    /// <summary>
    /// I tre piani orari, in ordine di quanto costa sbagliarli: il confine di sessione muove
    /// <c>d0..d5</c> e l'uscita di fine sessione, la finestra operativa decide solo se un ingresso
    /// nasce, la maschera del calendario toglie barre che il feed stampa comunque.
    /// </summary>
    private static string Render(StrategyHoursCard card)
    {
        var text = new StringBuilder();

        text.AppendLine("SESSIONE — taglia d0..d5, il secchio delle entrate e l'uscita di fine sessione");
        if (card.Session.TimeZoneId.Length == 0)
        {
            text.AppendLine("  non dichiarata (vedi avvisi in fondo)");
        }
        else
        {
            text.AppendLine($"  Finestra       {card.Session.Label}");
            text.AppendLine(
                $"  Ancoraggio     {card.SessionAnchorHour:00}:00 · " +
                (card.SessionAnchorOverrideReason is { Length: > 0 } reason
                    ? $"OVERRIDE della strategia (il calendario dice {card.CalendarSessionStartHour:00}:00) — {reason}"
                    : $"dal calendario di {card.Symbol}"));
            text.AppendLine($"  In UTC         gennaio {card.Session.WinterUtc}   ·   luglio {card.Session.SummerUtc}");
        }

        text.AppendLine();
        text.AppendLine("FINESTRA OPERATIVA — quando può nascere un ingresso");
        if (card.TradingWindow is { } window)
        {
            text.AppendLine($"  Finestra       {window.Label}");
            text.AppendLine($"  In UTC         gennaio {window.WinterUtc}   ·   luglio {window.SummerUtc}");
        }

        text.AppendLine($"  {(card.TradingWindow is null ? "Nessuna       " : "Effetto       ")} {card.TradingWindowNote}");

        text.AppendLine();
        text.AppendLine("NEGOZIAZIONE DELLO STRUMENTO — fuori da qui la barra non arriva alla strategia");
        if (card.InstrumentTradingWindows.Count == 0)
        {
            text.AppendLine("  non dichiarata: passa ogni barra del feed");
        }
        else
        {
            foreach (var instrumentWindow in card.InstrumentTradingWindows)
            {
                text.AppendLine($"  Negozia        {instrumentWindow.Label}");
                if (instrumentWindow.Source is { Length: > 0 } source)
                {
                    text.AppendLine($"                 ({source})");
                }
            }
        }

        text.AppendLine(
            "  Giorni         " +
            (card.DeclaresSessionDays
                ? $"{string.Join(" ", card.SessionDays)} (sessioni della ricerca)"
                : "non dichiarati dal calendario"));

        text.AppendLine();
        text.AppendLine("RISCALDAMENTO — sotto questa soglia il server salta la strategia in silenzio");
        text.AppendLine(
            $"  Barre          {card.RequiredCandles} a {card.TimeframeMinutes} minuti " +
            $"(≈ {card.RequiredCandlesInSessions:0.#} sessioni)");
        text.AppendLine($"  Tenuta         {card.HoldingLabel}");

        if (card.Warnings.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("AVVISI");
            foreach (var warning in card.Warnings)
            {
                text.AppendLine($"  · {warning}");
            }
        }

        return text.ToString();
    }

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();
}
