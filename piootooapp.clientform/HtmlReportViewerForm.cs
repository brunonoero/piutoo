using System.Diagnostics;
using Microsoft.Web.WebView2.WinForms;

namespace piootooapp.clientform;

internal sealed class HtmlReportViewerForm : Form
{
    private readonly WebView2 _browser = new()
    {
        Dock = DockStyle.Fill
    };
    private readonly string _temporaryHtmlPath;

    private HtmlReportViewerForm(string title, string temporaryHtmlPath, Uri sourceUri)
    {
        _temporaryHtmlPath = temporaryHtmlPath;
        Text = title;
        Width = 1180;
        Height = 820;
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterParent;

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(6)
        };
        var closeButton = new Button { Text = "Chiudi", AutoSize = true };
        closeButton.Click += (_, _) => Close();
        var externalButton = new Button { Text = "Apri nel browser", AutoSize = true };
        externalButton.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = sourceUri.ToString(),
            UseShellExecute = true
        });
        toolbar.Controls.Add(closeButton);
        toolbar.Controls.Add(externalButton);

        Controls.Add(_browser);
        Controls.Add(toolbar);
        Shown += async (_, _) => await InitializeBrowserAsync();
        FormClosed += (_, _) =>
        {
            try { File.Delete(_temporaryHtmlPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        };
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            await _browser.EnsureCoreWebView2Async();
            _browser.Source = new Uri(_temporaryHtmlPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile inizializzare WebView2: {ex.Message}",
                "Visualizzatore HTML",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    public static async Task ShowFromUriAsync(
        IWin32Window owner,
        HttpClient httpClient,
        Uri reportUri,
        string title,
        CancellationToken cancellationToken = default)
    {
        var html = await httpClient.GetStringAsync(reportUri, cancellationToken);
        var temporaryHtmlPath = Path.Combine(Path.GetTempPath(), $"piootoo-report-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(temporaryHtmlPath, html, cancellationToken);
        using var viewer = new HtmlReportViewerForm(title, temporaryHtmlPath, reportUri);
        viewer.ShowDialog(owner);
    }
}
