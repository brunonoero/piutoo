using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services.Compare;

/// <summary>
/// Confronto fra un run del cBot e un backtest interno avviato dalla console: crea la cartella
/// <c>compare-NNNN</c> successiva, ci scrive gli artefatti dei due run con i nomi di
/// <see cref="WorkspaceService.WriteCompareArtifacts"/> e una traccia di <c>esito.md</c>, poi fa
/// girare <see cref="CompareRunner"/> in background.
///
/// <para><b>La coppia si controlla prima di creare la cartella.</b> Lo strumento confronta un motore
/// contro l'altro sullo stesso feed: un run del cBot e un backtest interno sullo stesso broker, con
/// il summary dell'interno. Una coppia diversa non darebbe un errore ma un report sbagliato, quindi si
/// rifiuta con parole sue e senza lasciare una cartella a meta'.</para>
///
/// <para><b>log.txt e l'export Events non li puo' mettere il server:</b> li produce cTrader. Senza, il
/// report salta spread, esiti reali e verifica delle uscite; si copiano a mano nella cartella e si
/// rilancia lo strumento da riga di comando.</para>
///
/// <para>Un'analisi alla volta: sul log di un anno lo strumento tiene in memoria centinaia di
/// megabyte, e due confronti insieme non finiscono prima.</para>
/// </summary>
public sealed class CompareService
{
    public const string FolderPrefix = "compare-";
    public const string OutcomeFileName = "esito.md";

    private static readonly Regex FolderNamePattern =
        new(@"^compare-(\d{4,})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly WorkspaceService _workspaces;
    private readonly string _compareRoot;
    private readonly Func<string, string?, string?, TextWriter?, string> _runner;
    private readonly ConcurrentDictionary<string, CompareJob> _jobs = new();
    private readonly object _folderGate = new();
    private readonly SemaphoreSlim _runGate = new(1, 1);

    public CompareService(WorkspaceService workspaces, PiootooSettings settings)
        : this(workspaces, settings.GetComparePath(), CompareRunner.Run)
    {
    }

    /// <summary>La radice dei confronti e l'analisi si passano da fuori nei test.</summary>
    public CompareService(
        WorkspaceService workspaces,
        string compareRoot,
        Func<string, string?, string?, TextWriter?, string> runner)
    {
        _workspaces = workspaces;
        _compareRoot = Path.GetFullPath(compareRoot);
        _runner = runner;
    }

    public string CompareRoot => _compareRoot;

    /// <summary>
    /// Il numero della prossima cartella: uno piu' del piu' alto fra le <c>compare-NNNN</c> esistenti.
    /// Le cartelle con altri nomi (<c>last-backtest</c>, <c>strumento-confronto</c>) non contano.
    /// </summary>
    public int NextFolderNumber()
    {
        if (!Directory.Exists(_compareRoot))
            return 1;

        return Directory.EnumerateDirectories(_compareRoot)
            .Select(path => FolderNamePattern.Match(Path.GetFileName(path)))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    /// <exception cref="ArgumentException">Richiesta incompleta, o lo stesso run due volte.</exception>
    /// <exception cref="DirectoryNotFoundException">Workspace o backtest inesistenti.</exception>
    /// <exception cref="InvalidOperationException">La coppia non e' confrontabile.</exception>
    public CompareJob Start(StartCompareRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkspaceId))
            throw new ArgumentException("WorkspaceId e' obbligatorio.");
        if (string.IsNullOrWhiteSpace(request.FirstBacktest) || string.IsNullOrWhiteSpace(request.SecondBacktest))
            throw new ArgumentException("Servono due backtest da confrontare.");
        if (string.Equals(request.FirstBacktest.Trim(), request.SecondBacktest.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Un backtest non si confronta con se stesso.");

        var first = ReadRun(request.WorkspaceId, request.FirstBacktest.Trim());
        var second = ReadRun(request.WorkspaceId, request.SecondBacktest.Trim());

        var cbot = first.Origin.Origin == BacktestOrigin.ExternalBroker ? first : second;
        var internalRun = ReferenceEquals(cbot, first) ? second : first;
        if (cbot.Origin.Origin != BacktestOrigin.ExternalBroker || internalRun.Origin.Origin != BacktestOrigin.Internal)
            throw new InvalidOperationException(
                $"Il confronto mette un run del cBot contro un backtest interno: '{first.Name}' e' " +
                $"{first.Origin.RunSlug}, '{second.Name}' e' {second.Origin.RunSlug}.");

        var cbotBroker = cbot.Origin.ResolvedPriceSource.Broker;
        var internalSource = internalRun.Origin.ResolvedPriceSource;
        if (internalSource.Kind != PriceSourceKind.BrokerCfd ||
            !string.Equals(internalSource.Broker, cbotBroker, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Il backtest interno '{internalRun.Name}' non gira sul feed del broker del cBot " +
                $"({internalRun.Origin.RunSlug} contro {cbot.Origin.RunSlug}): lo strumento confronta i due " +
                "motori sulla stessa serie di prezzi, e con feed diversi misurerebbe la somma delle due differenze.");

        if (!File.Exists(Path.Combine(internalRun.Path, BacktestDiagnosticsSchema.SummaryFileName)))
            throw new InvalidOperationException(
                $"Il backtest interno '{internalRun.Name}' non ha {BacktestDiagnosticsSchema.SummaryFileName}: " +
                "e' un run interrotto, e lo strumento ne legge convenzioni di fill e piano.");

        var folder = CreateFolder();
        try
        {
            _workspaces.WriteCompareArtifacts(request.WorkspaceId, cbot.Name, folder);
            _workspaces.WriteCompareArtifacts(request.WorkspaceId, internalRun.Name, folder);
            WriteOutcomeDraft(folder, cbot, internalRun);
        }
        catch
        {
            Directory.Delete(folder, recursive: true);
            throw;
        }

        var job = new CompareJob
        {
            FolderName = Path.GetFileName(folder),
            FolderPath = folder,
            CbotBacktest = cbot.Name,
            InternalBacktest = internalRun.Name,
            ProgressMessage = "In coda"
        };
        _jobs[job.JobId] = job;
        _ = Task.Run(() => ExecuteAsync(job, cbotBroker!));
        return Snapshot(job);
    }

    public CompareJob? GetStatus(string jobId)
        => _jobs.TryGetValue(jobId, out var job) ? Snapshot(job) : null;

    /// <summary>Il <c>report.md</c> di una cartella di confronto.</summary>
    /// <exception cref="ArgumentException">Il nome non e' quello di una cartella <c>compare-NNNN</c>.</exception>
    /// <exception cref="FileNotFoundException">Il report non c'e' (analisi fallita o ancora in corso).</exception>
    public string GetReport(string folderName)
    {
        if (!FolderNamePattern.IsMatch(folderName ?? string.Empty))
            throw new ArgumentException($"'{folderName}' non e' il nome di una cartella di confronto.");

        var path = Path.Combine(_compareRoot, folderName!, "analisi", "report.md");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Nessun report in '{folderName}'.", path);

        return File.ReadAllText(path, Encoding.UTF8);
    }

    private RunInfo ReadRun(string workspaceId, string folderName)
    {
        var path = _workspaces.GetBacktestPath(workspaceId, folderName);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Backtest '{folderName}' non trovato nel workspace '{workspaceId}'.");

        var origin = WorkspaceService.ReadBacktestOrigin(path);
        if (origin is null || !origin.IdentifiesRun)
            throw new InvalidOperationException(
                $"Il backtest '{folderName}' non dichiara motore e serie di prezzi " +
                $"({(origin is null ? $"manca {BacktestOriginInfo.FileName}" : origin.RunSlug)}): non e' confrontabile.");

        return new RunInfo(folderName, path, origin);
    }

    private string CreateFolder()
    {
        lock (_folderGate)
        {
            Directory.CreateDirectory(_compareRoot);
            for (var number = NextFolderNumber(); ; number++)
            {
                var path = Path.Combine(_compareRoot, FolderPrefix + number.ToString("D4", CultureInfo.InvariantCulture));
                if (Directory.Exists(path))
                    continue;

                Directory.CreateDirectory(path);
                return path;
            }
        }
    }

    private static void WriteOutcomeDraft(string folder, RunInfo cbot, RunInfo internalRun)
    {
        var name = Path.GetFileName(folder);
        var o = new StringBuilder();
        o.AppendLine($"# {name} — {cbot.Origin.RunSlug} contro {internalRun.Origin.RunSlug}");
        o.AppendLine();
        o.AppendLine($"Cartella creata dalla console il {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC. Report dello strumento in `analisi/report.md`.");
        o.AppendLine("Da completare a mano a fine analisi: scheletro e regole in `compare/README.md`.");
        o.AppendLine();
        o.AppendLine("Per spread ai fill, esiti reali, ritardo degli intent e verifica delle uscite copia qui `log.txt` del cBot e l'export");
        o.AppendLine("Events di cTrader (`.xlsx`), poi rilancia lo strumento:");
        o.AppendLine();
        o.AppendLine("    dotnet run -c Release --project piootoo-repository/compare/strumento-confronto -- <questa cartella>");
        o.AppendLine();
        o.AppendLine("## Le due gambe");
        o.AppendLine();
        o.AppendLine("| lato | backtest | slug | serie di prezzi | versione | piano |");
        o.AppendLine("|---|---|---|---|---|---|");
        o.AppendLine(Leg("cBot", cbot));
        o.AppendLine(Leg("interno", internalRun));
        o.AppendLine();
        foreach (var section in new[] { "Esito", "Scomposizione", "Aperto", "Chiuso", "Cosa torna", "Trappole di misura di questa cartella" })
        {
            o.AppendLine($"## {section}");
            o.AppendLine();
        }

        File.WriteAllText(Path.Combine(folder, OutcomeFileName), o.ToString(), new UTF8Encoding(false));

        static string Leg(string side, RunInfo run)
            => $"| {side} | `{run.Name}` | `{run.Origin.RunSlug}` | {run.Origin.ResolvedPriceSource.FeedLabel} | " +
               $"{run.Origin.EngineVersion ?? "-"} | {run.Origin.PlanCode ?? "-"} |";
    }

    private async Task ExecuteAsync(CompareJob job, string broker)
    {
        await _runGate.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (job)
            {
                job.Status = BacktestingJobStatus.Running;
                job.ProgressMessage = "Analisi in corso";
            }

            using var progress = new ProgressWriter(job);
            _runner(job.FolderPath, null, broker, progress);

            lock (job)
            {
                job.Status = BacktestingJobStatus.Completed;
                job.ProgressMessage = "Confronto completato";
            }
        }
        catch (Exception exception)
        {
            lock (job)
            {
                job.Status = BacktestingJobStatus.Failed;
                job.ErrorMessage = exception.Message;
                job.ProgressMessage = "Confronto fallito";
            }
        }
        finally
        {
            lock (job)
                job.CompletedAt = DateTime.UtcNow;
            _runGate.Release();
        }
    }

    private static CompareJob Snapshot(CompareJob job)
    {
        lock (job)
        {
            return new CompareJob
            {
                JobId = job.JobId,
                Status = job.Status,
                FolderName = job.FolderName,
                FolderPath = job.FolderPath,
                CbotBacktest = job.CbotBacktest,
                InternalBacktest = job.InternalBacktest,
                ProgressMessage = job.ProgressMessage,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage
            };
        }
    }

    private sealed record RunInfo(string Name, string Path, BacktestOriginInfo Origin);

    /// <summary>Le righe dello strumento diventano il messaggio di avanzamento del job.</summary>
    private sealed class ProgressWriter(CompareJob job) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
        }

        public override void WriteLine(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            lock (job)
                job.ProgressMessage = value;
        }
    }
}
