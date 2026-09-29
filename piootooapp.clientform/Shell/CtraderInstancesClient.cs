using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace piootooapp.clientform.Shell;

/// <summary>Un'istanza di cBot su cTrader CLI come la descrive <c>conti-ctrader.ps1 -Json</c>.</summary>
public sealed class CtraderInstanceStatus
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("account")] public string Account { get; init; } = string.Empty;
    [JsonPropertyName("bot")] public string Bot { get; init; } = string.Empty;
    [JsonPropertyName("planCode")] public string? PlanCode { get; init; }
    [JsonPropertyName("enabled")] public bool Enabled { get; init; }
    [JsonPropertyName("running")] public bool Running { get; init; }
    [JsonPropertyName("processId")] public int? ProcessId { get; init; }
    [JsonPropertyName("startedAtUtc")] public DateTime? StartedAtUtc { get; init; }
    [JsonPropertyName("logPath")] public string LogPath { get; init; } = string.Empty;
}

/// <summary>
/// Le istanze dei cBot su cTrader CLI, lette e comandate attraverso <c>tools\conti-ctrader.ps1</c>.
/// È l'unica eccezione al "la console parla solo HTTP": i processi di cTrader girano su questa
/// macchina e il server non li conosce. La logica — avvio, Ctrl+C, verifica di OnStop — resta nello
/// script, che è anche quello che <c>aggiorna-cbot.ps1 -RestartInstances</c> usa: qui la si chiama
/// soltanto, così i due percorsi non possono divergere.
/// </summary>
public static class CtraderInstancesClient
{
    private const string ScriptName = "conti-ctrader.ps1";

    /// <summary>Lo script da <c>appsettings.json</c>, altrimenti risalendo dalla cartella dell'eseguibile
    /// fino a un <c>tools\conti-ctrader.ps1</c>. Null se non si trova.</summary>
    public static string? ResolveScript()
    {
        if (!string.IsNullOrWhiteSpace(ClientSettings.CtraderInstancesScript))
        {
            return File.Exists(ClientSettings.CtraderInstancesScript) ? ClientSettings.CtraderInstancesScript : null;
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "tools", ScriptName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static async Task<IReadOnlyList<CtraderInstanceStatus>> GetStatusAsync(CancellationToken cancellationToken)
    {
        var result = await RunAsync(new[] { "-Action", "Status", "-Json" }, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Describe());
        }

        return JsonSerializer.Deserialize<List<CtraderInstanceStatus>>(result.Output)
               ?? new List<CtraderInstanceStatus>();
    }

    /// <summary>Start, Stop o Restart su una sola istanza. Lo stop può durare fino al timeout dello
    /// script (30 secondi) più l'avvio: il chiamante tiene la schermata in attesa.</summary>
    public static Task<ScriptResult> RunActionAsync(string action, string instanceName, CancellationToken cancellationToken)
        => RunAsync(new[] { "-Action", action, "-Instance", instanceName }, cancellationToken);

    private static async Task<ScriptResult> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var script = ResolveScript()
                     ?? throw new InvalidOperationException(
                         $"{ScriptName} non trovato: imposta CtraderInstancesScript in appsettings.json della console.");

        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-OutputFormat", "Text", "-File", script })
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(ClientSettings.CtraderInstancesConfig))
        {
            start.ArgumentList.Add("-Config");
            start.ArgumentList.Add(ClientSettings.CtraderInstancesConfig);
        }

        using var process = Process.Start(start)
                            ?? throw new InvalidOperationException("powershell.exe non si è avviato.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new ScriptResult(process.ExitCode, await output, await error);
    }
}

public sealed record ScriptResult(int ExitCode, string Output, string Error)
{
    /// <summary>Il messaggio da mostrare: l'errore di PowerShell se c'è, altrimenti l'output.</summary>
    public string Describe()
    {
        var text = string.IsNullOrWhiteSpace(Error) ? Output : Error;
        // PowerShell accoda allo stesso errore posizione e categoria su righe successive: la prima
        // riga è il messaggio dello script, il resto è rumore per chi guarda la barra di stato.
        var first = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => line.Length > 0);
        return first ?? $"conti-ctrader.ps1 uscito con codice {ExitCode}";
    }
}
