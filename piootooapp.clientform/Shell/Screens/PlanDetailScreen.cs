using System.ComponentModel;
using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>Riga gruppo/account modificabile: <see cref="TradingGroupRow"/> è init-only.</summary>
public sealed class PlanGroupEditRow
{
    public string GroupId { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public int MaxConcurrentTrades { get; set; }

    public string RotationSetupId { get; set; } = string.Empty;

    public string TitanoRunId { get; set; } = string.Empty;

    public string TitanoBacktestFolder { get; set; } = string.Empty;

    public bool ApplyTitanoFilters { get; set; } = true;
}

/// <summary>Riga strumento modificabile: <see cref="InstrumentMetadata"/> è init-only.</summary>
public sealed class PlanInstrumentEditRow
{
    public string Symbol { get; set; } = string.Empty;

    public decimal DollarsPerPoint { get; set; } = 1m;

    public decimal MinimumQuantity { get; set; } = 1m;

    public decimal QuantityStep { get; set; } = 1m;

    public QuantityRoundingMode RoundingMode { get; set; } = QuantityRoundingMode.FuturesContracts;
}

/// <summary>
/// Dettaglio di un piano di trading. Il salvataggio riscrive il piano intero, quindi la schermata
/// deve esporre tutto ciò che il piano contiene: quello che non è modificabile qui verrebbe
/// riportato ai default alla prima modifica.
/// </summary>
public partial class PlanDetailScreen : UserControl, IShellScreen, IDirtyAware
{
    private readonly BindingList<PlanGroupEditRow> _groups = new();
    private readonly BindingList<PlanInstrumentEditRow> _instruments = new();
    private ShellContext? _context;
    private string _workspaceId = string.Empty;
    private string? _code;
    private bool _isNew;
    private bool _suspendDirtyTracking;
    private bool _isDirty;

    public PlanDetailScreen()
    {
        InitializeComponent();
        _groupsBindingSource.DataSource = _groups;
        _instrumentsBindingSource.DataSource = _instruments;
        _colRoundingMode.DataSource = Enum.GetValues<QuantityRoundingMode>();
        _enforceConcurrencyCombo.Items.AddRange(new object[]
        {
            "Default (come da storico)",
            "Sì, applica i limiti",
            "No, ignora i limiti"
        });
        _enforceConcurrencyCombo.SelectedIndex = 0;

        _groups.ListChanged += (_, _) => MarkDirty();
        _instruments.ListChanged += (_, _) => MarkDirty();
    }

    public string ScreenTitle => _isNew
        ? "Nuovo piano"
        : _code is { Length: > 0 } code ? $"Piano {code}" : "Piano";

    public bool HasUnsavedChanges => _isDirty;

    /// <summary>Va chiamato prima di aggiungere il controllo allo shell. <paramref name="code"/> null = nuovo piano.</summary>
    public void SetPlan(string workspaceId, string? code)
    {
        _workspaceId = workspaceId;
        _code = code;
        _isNew = string.IsNullOrEmpty(code);
    }

    public void Initialize(ShellContext context) => _context = context;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_context == null)
        {
            return;
        }

        _suspendDirtyTracking = true;
        _toolbar.SetBusy(true);
        try
        {
            _workspaceTextBox.Text = _workspaceId;
            if (_isNew)
            {
                _toolbar.Title = "Nuovo piano";
                _codeTextBox.ReadOnly = false;
                ResetToDefaults();
                _context.Navigation.SetStatus($"Nuovo piano nel workspace '{_workspaceId}'.");
                return;
            }

            var plan = await _context.Services.Plans.GetAsync(_workspaceId, _code!, cancellationToken);
            _toolbar.Title = $"Piano {plan.Code}";
            _codeTextBox.ReadOnly = true;
            Fill(plan);
            _context.Navigation.SetStatus(
                $"Piano '{plan.Code}' con {plan.Groups.Count} righe gruppo/account, " +
                $"aggiornato il {plan.UpdatedUtc:yyyy-MM-dd HH:mm} UTC.");
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
            _suspendDirtyTracking = false;
            SetDirty(false);
            _toolbar.SetBusy(false);
        }
    }

    private void ResetToDefaults()
    {
        _codeTextBox.Text = string.Empty;
        _nameTextBox.Text = string.Empty;
        _maxConcurrentInput.Value = 0;
        _initialCapitalInput.Value = 100_000m;
        _commissionInput.Value = 2m;
        _applyTitanoCheckBox.Checked = false;
        _rotationSetupTextBox.Text = string.Empty;
        _titanoRunTextBox.Text = string.Empty;
        _titanoFolderTextBox.Text = string.Empty;
        _enforceConcurrencyCombo.SelectedIndex = 0;

        _clampMultipliersCheckBox.Checked = true;
        _volatilityEnabledCheckBox.Checked = false;
        _atrPeriodsInput.Value = 14;
        _targetRiskInput.Value = 1_000m;
        _portfolioRiskEnabledCheckBox.Checked = false;
        _maxDrawdownInput.Value = 0.20m;
        _maxGrossExposureInput.Value = 1m;
        _cppiEnabledCheckBox.Checked = false;
        _cppiFloorInput.Value = 0.80m;
        _cppiMultiplierInput.Value = 1m;
        _aggressiveModulesCheckBox.Checked = false;
        _fractionalFactorInput.Value = 0.25m;
        _maximumMultiplierInput.Value = 1m;

        _groups.Clear();
        _instruments.Clear();
    }

    private void Fill(TradingPlan plan)
    {
        _codeTextBox.Text = plan.Code;
        _nameTextBox.Text = plan.Name;
        _maxConcurrentInput.Value = plan.MaxConcurrentTrades;
        _initialCapitalInput.Value = Clamp(_initialCapitalInput, plan.InitialCapital);
        _commissionInput.Value = Clamp(_commissionInput, plan.CommissionPerContract);
        _applyTitanoCheckBox.Checked = plan.ApplyTitanoFilters;
        _rotationSetupTextBox.Text = plan.RotationSetupId ?? string.Empty;
        _titanoRunTextBox.Text = plan.TitanoRunId ?? string.Empty;
        _titanoFolderTextBox.Text = plan.TitanoBacktestFolder ?? string.Empty;
        _enforceConcurrencyCombo.SelectedIndex = plan.EnforceConcurrencyLimits switch
        {
            null => 0,
            true => 1,
            false => 2
        };

        var sizing = plan.PositionSizing;
        _clampMultipliersCheckBox.Checked = sizing.ClampMultipliersToUnitInterval;
        _volatilityEnabledCheckBox.Checked = sizing.MarketVolatility.Enabled;
        _atrPeriodsInput.Value = Math.Clamp(sizing.MarketVolatility.AtrPeriods, 1, 1000);
        _targetRiskInput.Value = Clamp(_targetRiskInput, sizing.MarketVolatility.TargetRiskDollars);
        _portfolioRiskEnabledCheckBox.Checked = sizing.PortfolioRisk.Enabled;
        _maxDrawdownInput.Value = Clamp(_maxDrawdownInput, sizing.PortfolioRisk.MaximumDrawdown);
        _maxGrossExposureInput.Value = Clamp(_maxGrossExposureInput, sizing.PortfolioRisk.MaximumGrossExposure);
        _cppiEnabledCheckBox.Checked = sizing.PortfolioRisk.EnableCppi;
        _cppiFloorInput.Value = Clamp(_cppiFloorInput, sizing.PortfolioRisk.CppiFloorFraction);
        _cppiMultiplierInput.Value = Clamp(_cppiMultiplierInput, sizing.PortfolioRisk.CppiMultiplier);
        _aggressiveModulesCheckBox.Checked = sizing.PortfolioRisk.EnableAggressiveModules;
        _fractionalFactorInput.Value = Clamp(_fractionalFactorInput, sizing.PortfolioRisk.FractionalFactor);
        _maximumMultiplierInput.Value = Clamp(_maximumMultiplierInput, sizing.PortfolioRisk.MaximumMultiplier);

        _groups.RaiseListChangedEvents = false;
        _groups.Clear();
        foreach (var group in plan.Groups)
        {
            _groups.Add(new PlanGroupEditRow
            {
                GroupId = group.GroupId,
                AccountNumber = group.AccountNumber,
                MaxConcurrentTrades = group.MaxConcurrentTrades,
                RotationSetupId = group.RotationSetupId ?? string.Empty,
                TitanoRunId = group.TitanoRunId ?? string.Empty,
                TitanoBacktestFolder = group.TitanoBacktestFolder ?? string.Empty,
                ApplyTitanoFilters = group.ApplyTitanoFilters
            });
        }

        _groups.RaiseListChangedEvents = true;
        _groups.ResetBindings();

        _instruments.RaiseListChangedEvents = false;
        _instruments.Clear();
        foreach (var instrument in plan.Instruments)
        {
            _instruments.Add(new PlanInstrumentEditRow
            {
                Symbol = instrument.Symbol,
                DollarsPerPoint = instrument.DollarsPerPoint,
                MinimumQuantity = instrument.MinimumQuantity,
                QuantityStep = instrument.QuantityStep,
                RoundingMode = instrument.RoundingMode
            });
        }

        _instruments.RaiseListChangedEvents = true;
        _instruments.ResetBindings();
    }

    private static decimal Clamp(NumericUpDown input, decimal value)
        => Math.Clamp(value, input.Minimum, input.Maximum);

    private void MarkDirty()
    {
        if (!_suspendDirtyTracking)
        {
            SetDirty(true);
        }
    }

    private void SetDirty(bool dirty)
    {
        _isDirty = dirty;
        _toolbar.SetDirty(dirty);
    }

    private void OnFieldChanged(object? sender, EventArgs e) => MarkDirty();

    private void OnBackRequested(object? sender, EventArgs e) => _context?.Navigation.GoBack();

    private async void OnRevertRequested(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnAddGroupClick(object? sender, EventArgs e)
        => _groups.Add(new PlanGroupEditRow { GroupId = "gruppo", AccountNumber = string.Empty });

    private void OnRemoveGroupClick(object? sender, EventArgs e)
    {
        if (_groupsGrid.CurrentRow?.Index is { } index && index >= 0 && index < _groups.Count)
        {
            _groups.RemoveAt(index);
        }
    }

    private void OnAddInstrumentClick(object? sender, EventArgs e)
        => _instruments.Add(new PlanInstrumentEditRow());

    private void OnRemoveInstrumentClick(object? sender, EventArgs e)
    {
        if (_instrumentsGrid.CurrentRow?.Index is { } index && index >= 0 && index < _instruments.Count)
        {
            _instruments.RemoveAt(index);
        }
    }

    private async void OnSaveRequested(object? sender, EventArgs e)
    {
        if (_context == null)
        {
            return;
        }

        var code = _codeTextBox.Text.Trim();
        if (code.Length == 0)
        {
            MessageBox.Show(this, "Il codice del piano è obbligatorio.", "Piano",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var validGroups = _groups
            .Where(row => !string.IsNullOrWhiteSpace(row.GroupId) && !string.IsNullOrWhiteSpace(row.AccountNumber))
            .ToList();
        if (validGroups.Count == 0)
        {
            MessageBox.Show(
                this,
                "Serve almeno una riga gruppo/account completa: è la configurazione canonica del piano.",
                "Piano",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var request = new SaveTradingPlanRequest
        {
            Code = code,
            Name = _nameTextBox.Text.Trim() is { Length: > 0 } name ? name : code,
            Groups = validGroups.Select(row => new TradingGroupRow
            {
                GroupId = row.GroupId.Trim(),
                AccountNumber = row.AccountNumber.Trim(),
                MaxConcurrentTrades = row.MaxConcurrentTrades,
                RotationSetupId = NullIfEmpty(row.RotationSetupId),
                TitanoRunId = NullIfEmpty(row.TitanoRunId),
                TitanoBacktestFolder = NullIfEmpty(row.TitanoBacktestFolder),
                ApplyTitanoFilters = row.ApplyTitanoFilters
            }).ToList(),
            MaxConcurrentTrades = (int)_maxConcurrentInput.Value,
            RotationSetupId = NullIfEmpty(_rotationSetupTextBox.Text),
            TitanoRunId = NullIfEmpty(_titanoRunTextBox.Text),
            TitanoBacktestFolder = NullIfEmpty(_titanoFolderTextBox.Text),
            ApplyTitanoFilters = _applyTitanoCheckBox.Checked,
            EnforceConcurrencyLimits = _enforceConcurrencyCombo.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            },
            InitialCapital = _initialCapitalInput.Value,
            CommissionPerContract = _commissionInput.Value,
            PositionSizing = new PositionSizingConfig
            {
                ClampMultipliersToUnitInterval = _clampMultipliersCheckBox.Checked,
                MarketVolatility = new MarketVolatilitySizingConfig
                {
                    Enabled = _volatilityEnabledCheckBox.Checked,
                    AtrPeriods = (int)_atrPeriodsInput.Value,
                    TargetRiskDollars = _targetRiskInput.Value
                },
                PortfolioRisk = new PortfolioRiskSizingConfig
                {
                    Enabled = _portfolioRiskEnabledCheckBox.Checked,
                    MaximumDrawdown = _maxDrawdownInput.Value,
                    MaximumGrossExposure = _maxGrossExposureInput.Value,
                    EnableCppi = _cppiEnabledCheckBox.Checked,
                    CppiFloorFraction = _cppiFloorInput.Value,
                    CppiMultiplier = _cppiMultiplierInput.Value,
                    EnableAggressiveModules = _aggressiveModulesCheckBox.Checked,
                    FractionalFactor = _fractionalFactorInput.Value,
                    MaximumMultiplier = _maximumMultiplierInput.Value
                }
            },
            Instruments = _instruments
                .Where(row => !string.IsNullOrWhiteSpace(row.Symbol))
                .Select(row => new InstrumentMetadata
                {
                    Symbol = row.Symbol.Trim(),
                    DollarsPerPoint = row.DollarsPerPoint,
                    MinimumQuantity = row.MinimumQuantity,
                    QuantityStep = row.QuantityStep,
                    RoundingMode = row.RoundingMode
                }).ToList()
        };

        _toolbar.SetBusy(true);
        try
        {
            var saved = await _context.Services.Plans.SaveAsync(_workspaceId, request);
            _code = saved.Code;
            _isNew = false;
            _codeTextBox.ReadOnly = true;
            _suspendDirtyTracking = true;
            Fill(saved);
            _suspendDirtyTracking = false;
            SetDirty(false);
            _toolbar.Title = $"Piano {saved.Code}";
            _context.Navigation.SetStatus($"Piano '{saved.Code}' salvato.");
        }
        catch (Exception ex)
        {
            _context.Navigation.SetError(ex.Message);
            MessageBox.Show(this, ex.Message, "Errore di salvataggio", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _toolbar.SetBusy(false);
        }
    }

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
