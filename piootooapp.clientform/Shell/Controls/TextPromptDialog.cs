namespace piootooapp.clientform.Shell.Controls;

/// <summary>Dialog a campo singolo, per le anagrafiche che non hanno un dettaglio proprio.</summary>
public partial class TextPromptDialog : Form
{
    public TextPromptDialog()
    {
        InitializeComponent();
        ShellTheme.Apply(this);
    }

    public string Prompt
    {
        get => _promptLabel.Text;
        set => _promptLabel.Text = value;
    }

    public string Value
    {
        get => _valueTextBox.Text.Trim();
        set => _valueTextBox.Text = value;
    }

    public string Placeholder
    {
        get => _valueTextBox.PlaceholderText;
        set => _valueTextBox.PlaceholderText = value;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        if (Value.Length == 0)
        {
            MessageBox.Show(this, "Il valore è obbligatorio.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }
    }
}
