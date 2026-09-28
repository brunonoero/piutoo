using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Controls;

/// <summary>
/// Nasce un piano di produzione: da un best plan (promozione) o da un piano di produzione con conti
/// nuovi (duplica). Il piano non si compone qui — strategie, pesi e tenuta vengono dal run misurato —
/// si scelgono solo codice, nome e conti.
///
/// <para><b>Prima si verifica, poi si conferma.</b> <i>Verifica</i> chiede al server il piano che
/// nascerebbe senza scriverlo (<c>DryRun</c>): le regole del broker workspace — una strategia in un
/// solo piano attivo, piano non cambiato dopo il run — rispondono li', con le parole del
/// server. La conferma si abilita solo dopo una verifica riuscita e si spegne a ogni modifica dei
/// campi: si conferma sempre cio' che si e' appena visto.</para>
/// </summary>
public partial class ProductionPlanDialog : Form
{
    private Func<bool, Task<TradingPlan>>? _submit;

    public ProductionPlanDialog()
    {
        InitializeComponent();
        ShellTheme.Apply(this);
    }

    /// <summary>
    /// La chiamata al server, con i valori correnti del dialog. Il parametro e' <c>dryRun</c>.
    /// </summary>
    public void SetSubmit(Func<bool, Task<TradingPlan>> submit) => _submit = submit;

    public string Intro
    {
        get => _introLabel.Text;
        set => _introLabel.Text = value;
    }

    public string ConfirmText
    {
        get => _okButton.Text;
        set => _okButton.Text = value;
    }

    public string PlanCode
    {
        get => _codeBox.Text.Trim();
        set => _codeBox.Text = value;
    }

    public string PlanName
    {
        get => _nameBox.Text.Trim();
        set => _nameBox.Text = value;
    }

    /// <summary>Numeri di conto separati da virgola o spazio.</summary>
    public List<string> Accounts =>
        _accountsBox.Text
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    public string AccountsText
    {
        get => _accountsBox.Text;
        set => _accountsBox.Text = value;
    }

    public string AccountsHint
    {
        get => _accountsBox.PlaceholderText;
        set => _accountsBox.PlaceholderText = value;
    }

    /// <summary>Il piano creato, dopo la conferma.</summary>
    public TradingPlan? Result { get; private set; }

    private void OnFieldChanged(object? sender, EventArgs e)
    {
        _okButton.Enabled = false;
        _previewBox.Text = "Verifica di nuovo prima di confermare.";
    }

    private async void OnVerifyClick(object? sender, EventArgs e)
    {
        if (_submit == null)
        {
            return;
        }

        SetBusy(true);
        _previewBox.Text = "Verifica in corso…";
        try
        {
            var plan = await _submit(true);
            _previewBox.Text = ProductionPlanText.Describe(plan).Replace("\n", Environment.NewLine);
            _okButton.Enabled = true;
        }
        catch (Exception ex)
        {
            _previewBox.Text = "Non si può creare:" + Environment.NewLine + Environment.NewLine + ex.Message;
            _okButton.Enabled = false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnOkClick(object? sender, EventArgs e)
    {
        if (_submit == null)
        {
            return;
        }

        SetBusy(true);
        try
        {
            Result = await _submit(false);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        _verifyButton.Enabled = !busy;
        _cancelButton.Enabled = !busy;
        if (busy)
        {
            _okButton.Enabled = false;
        }
    }
}
