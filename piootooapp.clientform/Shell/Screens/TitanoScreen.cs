using System.Globalization;
using System.Text;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Workspaces;
using piootooapp.clientform.Shell.Controls;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Setup ed esecuzione di una rotazione Titano. Il setup è riutilizzabile e non contiene il
/// contesto del run: workspace, cartella di backtest e finestra temporale stanno solo qui e
/// vengono uniti ai parametri al momento dell'esecuzione.
/// </summary>
public partial class TitanoScreen : UserControl, IShellScreen
{
    private ShellContext? _context;
    private TitanoRotationManifest? _lastManifest;
    private bool _suspendReload;

    /// <summary>
    /// Elenco dei backtest del workspace corrente. Sta qui e non in una combo perché la scelta
    /// passa da una modale con filtro: le cartelle sono troppe per un menu a tendina.
    /// </summary>
    private readonly List<WorkspaceBacktestInfo> _backtests = new();

    private WorkspaceBacktestInfo? _selectedBacktest;

    /// <summary>Evita che il ripopolamento della combo scateni un caricamento del setup.</summary>
    private bool _suspendSetupReload;
    private bool _isRunning;

    public TitanoScreen()
    {
        InitializeComponent();
        _periodCombo.Items.AddRange(Enum.GetNames<TitanoRotationPeriod>());
        _periodCombo.SelectedIndex = 0;
        _walkForwardCombo.Items.AddRange(Enum.GetNames<TitanoWalkForwardMode>());
        _walkForwardCombo.SelectedIndex = 0;
        _startPicker.Value = DateTime.UtcNow.Date.AddYears(-2);
        _endPicker.Value = DateTime.UtcNow.Date;
        ApplyDefaults(new TitanoRotationSetup());
    }

    // Non è più una voce di menu: ci si arriva da "Nuova rotazione" nella lista dei run.
    public string ScreenTitle => "Nuova rotazione";

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null || _isRunning)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var previous = SelectedWorkspaceId;
            var workspaces = await _context.Services.Api.ListAsync(cancellationToken);

            _suspendReload = true;
            _workspaceCombo.Items.Clear();
            foreach (var workspace in workspaces)
            {
                _workspaceCombo.Items.Add(new WorkspaceComboItem(workspace));
            }

            var restored = FindWorkspaceIndex(previous);
            _workspaceCombo.SelectedIndex = restored >= 0
                ? restored
                : _workspaceCombo.Items.Count > 0 ? 0 : -1;

            var setups = await _context.Services.Titano.ListSetupsAsync(cancellationToken);
            _suspendSetupReload = true;
            _setupCombo.Items.Clear();
            foreach (var setup in setups.OrderBy(setup => setup.Name, StringComparer.OrdinalIgnoreCase))
            {
                _setupCombo.Items.Add(new SetupComboItem(setup));
            }

            _suspendSetupReload = false;

            _suspendReload = false;

            await ReloadBacktestsAsync(cancellationToken);
            _context.Navigation.SetStatus($"{setups.Count} setup di rotazione salvati.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _suspendReload = false;
            SetBusy(false);
        }
    }

    private sealed class SetupComboItem
    {
        public SetupComboItem(TitanoSetupInfo info) => Info = info;

        public TitanoSetupInfo Info { get; }

        public override string ToString()
            => Info.UpdatedAt is { } updated
                ? $"{Info.Name}  ·  {updated:yyyy-MM-dd HH:mm}"
                : Info.Name;
    }

    private string? SelectedWorkspaceId => (_workspaceCombo.SelectedItem as WorkspaceComboItem)?.Info.Id;

    private string? SelectedBacktestFolder => _selectedBacktest?.FolderName;

    private int FindWorkspaceIndex(string? workspaceId)
    {
        if (string.IsNullOrEmpty(workspaceId))
        {
            return -1;
        }

        for (var index = 0; index < _workspaceCombo.Items.Count; index++)
        {
            if (_workspaceCombo.Items[index] is WorkspaceComboItem item
                && string.Equals(item.Info.Id, workspaceId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void SetBusy(bool busy)
    {
        if (IsDisposed)
        {
            return;
        }

        _runButton.Enabled = !busy;
        _setupCombo.Enabled = !busy;
        _reloadButton.Enabled = !busy;
        _backtestPickButton.Enabled = !busy;
        _reportButton.Enabled = !busy && _lastManifest != null;
        Cursor = busy ? Cursors.AppStarting : Cursors.Default;
    }

    private async Task ReloadBacktestsAsync(CancellationToken cancellationToken)
    {
        if (_context == null || SelectedWorkspaceId is not { } workspaceId)
        {
            _backtests.Clear();
            SelectBacktest(null);
            return;
        }

        var backtests = await _context.Services.Api.ListBacktestsAsync(workspaceId, cancellationToken);
        _backtests.Clear();
        _backtests.AddRange(backtests.OrderByDescending(backtest => backtest.LastModifiedUtc));

        // La selezione precedente sopravvive a un ricarico se la cartella c'è ancora; altrimenti
        // vale il più recente, che è il caso d'uso normale (rotazione sul backtest appena fatto).
        var previous = _selectedBacktest?.FolderName;
        var restored = previous == null
            ? null
            : _backtests.FirstOrDefault(backtest =>
                string.Equals(backtest.FolderName, previous, StringComparison.OrdinalIgnoreCase));

        SelectBacktest(restored ?? _backtests.FirstOrDefault());
    }

    /// <summary>
    /// Nel form si legge il backtest scelto, non la lista: l'etichetta ripete cartella, origine e
    /// data perché scegliere un run dell'engine esterno al posto di quello interno non dà errore,
    /// dà numeri diversi.
    /// </summary>
    private void SelectBacktest(WorkspaceBacktestInfo? backtest)
    {
        _selectedBacktest = backtest;
        _backtestTextBox.Text = backtest == null
            ? string.Empty
            : $"{backtest.FolderName}  ·  {BacktestComboItem.DescribeOrigin(backtest)}  ·  " +
              $"{backtest.LastModifiedUtc:yyyy-MM-dd HH:mm} UTC";
    }

    private void OnPickBacktestClick(object? sender, EventArgs e)
    {
        if (SelectedWorkspaceId is null)
        {
            MessageBox.Show(this, "Scegli prima un workspace.", "Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_backtests.Count == 0)
        {
            MessageBox.Show(this, "Il workspace non contiene backtest.", "Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var chosen = BacktestPickerDialog.Pick(this, _backtests, _selectedBacktest?.FolderName);
        if (chosen != null)
        {
            SelectBacktest(chosen);
        }
    }

    private void ApplyDefaults(TitanoRotationSetup setup)
    {
        _setupNameTextBox.Text = setup.Name;
        _setupDescriptionTextBox.Text = setup.Description;
        _periodCombo.SelectedItem = setup.RotationPeriod.ToString();
        _minimumTradesInput.Value = setup.MinimumTrades;
        _shortWindowInput.Value = setup.ShortWindowDays;
        _longWindowInput.Value = setup.LongWindowDays;
        _movingAverageWindowInput.Value = setup.MovingAverageWindowDays;
        _minimumShortReturnInput.Value = Clamp(_minimumShortReturnInput, setup.MinimumShortReturn);
        _minimumLongReturnInput.Value = Clamp(_minimumLongReturnInput, setup.MinimumLongReturn);
        _minimumZScoreInput.Value = Clamp(_minimumZScoreInput, setup.MinimumZScore);
        _maximumZScoreInput.Value = Clamp(_maximumZScoreInput, setup.MaximumZScore);
        _maximumCurrentDrawdownInput.Value = Clamp(_maximumCurrentDrawdownInput, setup.MaximumCurrentDrawdown);
        _maximumObservedDrawdownInput.Value = Clamp(_maximumObservedDrawdownInput, setup.MaximumObservedDrawdown);
        _maximumVolatilityInput.Value = Clamp(_maximumVolatilityInput, setup.MaximumReturnVolatility);
        _requireEquityAboveMaCheckBox.Checked = setup.RequireEquityAboveMovingAverage;
        _reenableDrawdownInput.Value = Clamp(_reenableDrawdownInput, setup.ReenableMaximumCurrentDrawdown);
        _disableScoreInput.Value = Clamp(_disableScoreInput, setup.DisableCompositeScore);
        _reenableScoreInput.Value = Clamp(_reenableScoreInput, setup.ReenableCompositeScore);
        _minimumPassingFiltersInput.Value = setup.MinimumPassingFilters;
        _cooldownInput.Value = setup.CooldownPeriodsAfterOff;
        _minimumOnPeriodsInput.Value = setup.MinimumOnPeriods;
        _hardStopInput.Value = Clamp(_hardStopInput, setup.HardStopDrawdown);
        _crossSectionalCheckBox.Checked = setup.CrossSectionalSizing;
        _minimumAllocationInput.Value = Clamp(_minimumAllocationInput, setup.MinimumAllocationMultiplier);
        _maximumAllocationInput.Value = Clamp(_maximumAllocationInput, setup.MaximumAllocationMultiplier);
        _allocationStepInput.Value = Clamp(_allocationStepInput, setup.AllocationStep);
        _commissionInput.Value = Clamp(_commissionInput, setup.CommissionPerUnit);
        _slippageInput.Value = Clamp(_slippageInput, setup.SlippagePerUnit);
        _sizingTiersTextBox.Text = FormatSizingTiers(setup.SizingTiers);
        _calibrationPeriodsInput.Value = setup.CalibrationPeriods;
        _evaluationPeriodsInput.Value = setup.EvaluationPeriods;
        _walkForwardCombo.SelectedItem = setup.WalkForwardMode.ToString();
        ApplySizingModeAvailability();
    }

    private static decimal Clamp(NumericUpDown input, decimal value)
        => Math.Clamp(value, input.Minimum, input.Maximum);

    /// <summary>
    /// Con il sizing per percentile i tier assoluti e le soglie di score non entrano nel calcolo:
    /// lasciarli attivi farebbe credere di poterli regolare.
    /// </summary>
    private void ApplySizingModeAvailability()
    {
        var crossSectional = _crossSectionalCheckBox.Checked;
        _minimumAllocationInput.Enabled = crossSectional;
        _maximumAllocationInput.Enabled = crossSectional;
        _allocationStepInput.Enabled = crossSectional;
        _sizingTiersTextBox.Enabled = !crossSectional;
        _disableScoreInput.Enabled = !crossSectional;
        _reenableScoreInput.Enabled = !crossSectional;
    }

    private TitanoRotationSetup BuildSetup(string? id)
        => new()
        {
            Id = id ?? string.Empty,
            Name = _setupNameTextBox.Text.Trim(),
            Description = _setupDescriptionTextBox.Text.Trim(),
            RotationPeriod = Enum.Parse<TitanoRotationPeriod>(_periodCombo.SelectedItem?.ToString() ?? "Weekly"),
            MinimumTrades = (int)_minimumTradesInput.Value,
            ShortWindowDays = (int)_shortWindowInput.Value,
            LongWindowDays = (int)_longWindowInput.Value,
            MovingAverageWindowDays = (int)_movingAverageWindowInput.Value,
            MinimumShortReturn = _minimumShortReturnInput.Value,
            MinimumLongReturn = _minimumLongReturnInput.Value,
            MinimumZScore = _minimumZScoreInput.Value,
            MaximumZScore = _maximumZScoreInput.Value,
            MaximumCurrentDrawdown = _maximumCurrentDrawdownInput.Value,
            MaximumObservedDrawdown = _maximumObservedDrawdownInput.Value,
            MaximumReturnVolatility = _maximumVolatilityInput.Value,
            RequireEquityAboveMovingAverage = _requireEquityAboveMaCheckBox.Checked,
            ReenableMaximumCurrentDrawdown = _reenableDrawdownInput.Value,
            DisableCompositeScore = _disableScoreInput.Value,
            ReenableCompositeScore = _reenableScoreInput.Value,
            MinimumPassingFilters = (int)_minimumPassingFiltersInput.Value,
            CooldownPeriodsAfterOff = (int)_cooldownInput.Value,
            MinimumOnPeriods = (int)_minimumOnPeriodsInput.Value,
            HardStopDrawdown = _hardStopInput.Value,
            CrossSectionalSizing = _crossSectionalCheckBox.Checked,
            MinimumAllocationMultiplier = _minimumAllocationInput.Value,
            MaximumAllocationMultiplier = _maximumAllocationInput.Value,
            AllocationStep = _allocationStepInput.Value,
            CommissionPerUnit = _commissionInput.Value,
            SlippagePerUnit = _slippageInput.Value,
            SizingTiers = ParseSizingTiers(_sizingTiersTextBox.Text),
            CalibrationPeriods = (int)_calibrationPeriodsInput.Value,
            EvaluationPeriods = (int)_evaluationPeriodsInput.Value,
            WalkForwardMode = Enum.Parse<TitanoWalkForwardMode>(
                _walkForwardCombo.SelectedItem?.ToString() ?? "Rolling")
        };

    private TitanoRotationRequest BuildRequest(string workspaceId, string backtestFolder)
    {
        var setup = BuildSetup(null);
        var startUtc = DateTime.SpecifyKind(_startPicker.Value.Date, DateTimeKind.Utc);
        return new TitanoRotationRequest
        {
            WorkspaceId = workspaceId,
            BacktestFolder = backtestFolder,
            SetupName = setup.Name,
            Description = setup.Description,
            RotationPeriod = setup.RotationPeriod,
            StartUtc = startUtc,
            EndUtc = DateTime.SpecifyKind(_endPicker.Value.Date, DateTimeKind.Utc),
            BiweeklyAnchorUtc = startUtc,
            InitialCapital = _initialCapitalInput.Value,
            MinimumTrades = setup.MinimumTrades,
            ShortWindowDays = setup.ShortWindowDays,
            LongWindowDays = setup.LongWindowDays,
            MovingAverageWindowDays = setup.MovingAverageWindowDays,
            MinimumShortReturn = setup.MinimumShortReturn,
            MinimumLongReturn = setup.MinimumLongReturn,
            MinimumZScore = setup.MinimumZScore,
            MaximumZScore = setup.MaximumZScore,
            MaximumCurrentDrawdown = setup.MaximumCurrentDrawdown,
            MaximumObservedDrawdown = setup.MaximumObservedDrawdown,
            MaximumReturnVolatility = setup.MaximumReturnVolatility,
            RequireEquityAboveMovingAverage = setup.RequireEquityAboveMovingAverage,
            ReenableMaximumCurrentDrawdown = setup.ReenableMaximumCurrentDrawdown,
            DisableCompositeScore = setup.DisableCompositeScore,
            ReenableCompositeScore = setup.ReenableCompositeScore,
            MinimumPassingFilters = setup.MinimumPassingFilters,
            CooldownPeriodsAfterOff = setup.CooldownPeriodsAfterOff,
            MinimumOnPeriods = setup.MinimumOnPeriods,
            HardStopDrawdown = setup.HardStopDrawdown,
            CrossSectionalSizing = setup.CrossSectionalSizing,
            MinimumAllocationMultiplier = setup.MinimumAllocationMultiplier,
            MaximumAllocationMultiplier = setup.MaximumAllocationMultiplier,
            AllocationStep = setup.AllocationStep,
            CommissionPerUnit = setup.CommissionPerUnit,
            SlippagePerUnit = setup.SlippagePerUnit,
            SizingTiers = setup.SizingTiers,
            CalibrationPeriods = setup.CalibrationPeriods,
            EvaluationPeriods = setup.EvaluationPeriods,
            WalkForwardMode = setup.WalkForwardMode
        };
    }

    private static string FormatSizingTiers(IEnumerable<TitanoSizingTier> tiers)
        => string.Join("; ", tiers.Select(tier =>
            $"{tier.MinimumScore.ToString("0.##", CultureInfo.InvariantCulture)}=" +
            $"{tier.AllocationMultiplier.ToString("0.##", CultureInfo.InvariantCulture)}"));

    private static List<TitanoSizingTier> ParseSizingTiers(string value)
    {
        var tiers = new List<TitanoSizingTier>();
        foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = part.Split('=', StringSplitOptions.TrimEntries);
            if (pieces.Length != 2)
            {
                throw new InvalidOperationException(
                    $"Scaglione di sizing non valido: '{part}'. Formato atteso 'score=moltiplicatore'.");
            }

            tiers.Add(new TitanoSizingTier
            {
                MinimumScore = ParseDecimal(pieces[0]),
                AllocationMultiplier = ParseDecimal(pieces[1])
            });
        }

        return tiers;
    }

    private static decimal ParseDecimal(string value)
    {
        var text = value.Replace("%", string.Empty).Trim();
        var isPercent = value.Contains('%');
        if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            && !decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
        {
            throw new InvalidOperationException($"Valore numerico non valido: '{value}'.");
        }

        return isPercent ? parsed / 100m : parsed;
    }

    private void OnCrossSectionalChanged(object? sender, EventArgs e) => ApplySizingModeAvailability();

    private async void OnWorkspaceChanged(object? sender, EventArgs e)
    {
        if (!_suspendReload)
        {
            try
            {
                await ReloadBacktestsAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _context?.Navigation.SetError(ex.Message);
            }
        }
    }

    private async void OnReloadClick(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    /// <summary>
    /// Applica il setup scelto ai parametri della schermata. Non c'e' piu' un pulsante "carica":
    /// selezionare un setup e non vederlo applicato sarebbe una scelta senza effetto visibile.
    ///
    /// <para>I parametri restano modificabili perche' una rotazione una tantum e' un caso legittimo,
    /// ma le modifiche non si salvano piu' da qui: il setup e' un'anagrafica e si modifica in
    /// <em>Anagrafiche -> Setup Titano</em>. Averlo in due posti significherebbe due versioni della
    /// stessa ricetta, e i run porterebbero l'id di una e i parametri dell'altra.</para>
    /// </summary>
    private async void OnSetupSelected(object? sender, EventArgs e)
    {
        if (_context == null || _suspendSetupReload || _setupCombo.SelectedItem is not SetupComboItem selected)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var setup = await _context.Services.Titano.GetSetupAsync(selected.Info.Id);
            ApplyDefaults(setup);
            _context.Navigation.SetStatus(
                $"Parametri dal setup '{setup.Name}'. Le modifiche qui valgono per questa rotazione: " +
                "per cambiarlo davvero usa Anagrafiche -> Setup Titano.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnRunClick(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        if (SelectedWorkspaceId is not { } workspaceId || SelectedBacktestFolder is not { } folder)
        {
            MessageBox.Show(this, "Servono un workspace e una cartella di backtest.", "Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_endPicker.Value <= _startPicker.Value)
        {
            MessageBox.Show(this, "La data di fine deve essere successiva a quella di inizio.", "Titano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        TitanoRotationRequest request;
        try
        {
            request = BuildRequest(workspaceId, folder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Titano", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _isRunning = true;
        SetBusy(true);
        _resultsTextBox.Text = "Rotazione in corso…";
        try
        {
            var manifest = await _context.Services.Titano.RunRotationAsync(request);
            _lastManifest = manifest;
            _resultsTextBox.Text = Summarize(manifest);
            _context.Navigation.SetStatus(
                $"Run {manifest.RunId}: {manifest.Periods.Count} periodi generati.");
        }
        catch (Exception ex)
        {
            _resultsTextBox.Text = ex.Message;
            _context.Navigation.SetError(ex.Message);
        }
        finally
        {
            _isRunning = false;
            SetBusy(false);
        }
    }

    private static string Summarize(TitanoRotationManifest manifest)
    {
        var text = new StringBuilder();
        text.AppendLine($"Run {manifest.RunId}");
        text.AppendLine($"Generato il {manifest.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC");
        text.AppendLine($"Periodi: {manifest.Periods.Count}");
        if (manifest.TradesOutsideCoverage > 0)
        {
            text.AppendLine(
                $"Trade fuori dai periodi efficaci: {manifest.TradesOutsideCoverage} " +
                "(presenti nell'equity originale, assenti da quella filtrata).");
        }

        if (!string.IsNullOrWhiteSpace(manifest.WalkForwardNote))
        {
            text.AppendLine($"Walk-forward: {manifest.WalkForwardNote}");
        }

        text.AppendLine();
        foreach (var period in manifest.Periods)
        {
            text.AppendLine(
                $"— {period.PeriodId}  {period.EffectiveFromUtc:yyyy-MM-dd HH:mm} → " +
                $"{period.EffectiveToUtc:yyyy-MM-dd HH:mm} UTC");
            foreach (var strategy in period.Strategies.OrderBy(
                         state => state.StrategyCode, StringComparer.OrdinalIgnoreCase))
            {
                text.AppendLine(
                    $"   {strategy.StrategyCode,-24} {strategy.State,-12} " +
                    $"alloc {strategy.AllocationMultiplier,6:P0}  " +
                    $"voti {strategy.PassingFilters}/{strategy.TotalFilters}  " +
                    $"cooldown {strategy.CooldownRemaining}  {strategy.Reason}");
            }

            text.AppendLine();
        }

        foreach (var walkForward in manifest.WalkForward)
        {
            text.AppendLine(
                $"Walk-forward {walkForward.EvaluationPeriodId}: " +
                $"IS {walkForward.InSampleNetProfit:N2} / OOS {walkForward.OutOfSampleNetProfit:N2}" +
                (walkForward.InSampleOnlyImprovementWarning ? "  ⚠ migliora solo in-sample" : string.Empty) +
                (walkForward.EvaluationTruncated ? "  (finestra troncata)" : string.Empty));
        }

        return text.ToString();
    }

    private async void OnReportClick(object? sender, EventArgs e)
    {
        if (_context == null || _lastManifest == null
            || SelectedWorkspaceId is not { } workspaceId
            || SelectedBacktestFolder is not { } folder)
        {
            return;
        }

        try
        {
            var uri = _context.Services.Titano.GetReportUri(_lastManifest.RunId, workspaceId, folder);
            await HtmlReportViewerForm.ShowFromUriAsync(
                FindForm()!, _context.Services.Http, uri, "Report Titano");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Report Titano", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
