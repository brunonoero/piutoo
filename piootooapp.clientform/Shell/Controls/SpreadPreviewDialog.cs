using System.Globalization;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;

namespace piootooapp.clientform.Shell.Controls;

/// <summary>Riga dell'anteprima: lo spread di un simbolo, già formattato per la griglia.</summary>
public sealed class SpreadPreviewRow
{
    public SpreadPreviewRow(SpreadSymbolInfo info, bool usedByRun)
    {
        Symbol = info.Symbol + (usedByRun ? string.Empty : "  (non nel run)");
        Points = info.Points;
        FallbackHours = info.HasHours ? info.FallbackHours : 0;

        if (!info.HasHours)
        {
            // Senza ore la riga non deve mostrare due celle vuote, che si leggerebbero come "misura
            // mancante": la misura c'è, è la costante, ed è quella che il simbolo paga.
            HourMinText = "costante per simbolo";
            HourMaxText = "costante per simbolo";
            Span = 1m;
            return;
        }

        HourMinText = Describe(info.HourMin, info.HourMinAt);
        HourMaxText = Describe(info.HourMax, info.HourMaxAt);

        // Il rapporto e non la differenza: è il numero che dice se la risoluzione oraria cambia
        // qualcosa per questo simbolo, e si legge uguale su @ES (0,6 punti) e su @CC (20).
        Span = info.HourMin > 0m ? info.HourMax / info.HourMin : 0m;
    }

    public string Symbol { get; }

    public decimal Points { get; }

    public string HourMinText { get; } = string.Empty;

    public string HourMaxText { get; } = string.Empty;

    public decimal Span { get; }

    public int FallbackHours { get; }

    private static string Describe(decimal value, int hour)
        => string.Format(
            CultureInfo.InvariantCulture, "{0:0.#####}  ({1:00}:00)", value, hour);
}

/// <summary>
/// Gli spread che il run applicherebbe, prima di lanciarlo.
///
/// <para>Esiste perché lo spread è l'unico parametro del backtest che non si scrive ma si
/// <b>trova</b>: viene da una misura sul disco, e fino a qui si vedeva solo a run finito, nella
/// scheda del report. Un broker sbagliato o un simbolo mai quotato costavano quindi un run intero
/// per essere scoperti.</para>
///
/// <para>I simboli elencati sono <b>tutti quelli misurati</b>, con i non usati dal run marcati:
/// vedere gli altri dice se la misura è quella giusta, ed è la stessa scelta della scheda nel
/// report HTML.</para>
/// </summary>
public partial class SpreadPreviewDialog : Form
{
    private readonly SortableBindingList<SpreadPreviewRow> _rows = new();

    public SpreadPreviewDialog(SpreadTableInfo table, IReadOnlyCollection<string> runSymbols)
    {
        InitializeComponent();
        ShellTheme.Apply(this);
        ShellGridHelper.ConfigureReadableGrids(this);
        _bindingSource.DataSource = _rows;
        _grid.EnableColumnSorting();

        _sourceLabel.Text =
            $"Misura: {table.Source}{Environment.NewLine}" +
            "Peggiora il solo prezzo di ingresso (long +spread, short −spread); " +
            "trigger, livelli e uscite restano sul prezzo del feed." +
            (table.Resolution == SpreadResolution.PerHour
                ? Environment.NewLine + "Risoluzione per ora UTC: si applica il valore dell'ora dell'ingresso. " +
                  "«Spread» è il ripiego delle ore che il broker non ha quotato, non il costo del run."
                : string.Empty);

        foreach (var symbol in table.Symbols)
        {
            _rows.Add(new SpreadPreviewRow(
                symbol,
                runSymbols.Count == 0 || runSymbols.Contains(symbol.Symbol, StringComparer.OrdinalIgnoreCase)));
        }

        _statusLabel.Text = $"{table.Symbols.Count} simboli misurati";

        _warningsBox.Text = table.Warnings.Count == 0
            ? "Nessun avviso: ogni simbolo del file ha una misura piena."
            : string.Join(Environment.NewLine, table.Warnings);
    }
}
