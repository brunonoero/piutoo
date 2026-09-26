using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.BestPlans;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services.BestPlans;

/// <summary>
/// I best plan: backtest messi in evidenza, fotografati in <c>[BasePath]\best-plans\{Id}\</c>.
/// </summary>
/// <remarks>
/// <para><b>La promozione copia, non rimanda.</b> Cifre per anno, curva di equity ridotta e strategie
/// finiscono in <c>best-plan.json</c>; <c>trades.json</c>, i summary, <c>origin.json</c>, il report HTML
/// e il piano come era al momento della promozione nella sottocartella <c>artifacts</c>. Il file di
/// risultato <c>backtest_*.json</c> resta fuori: arriva a 160 MB e cio' che serve se ne estrae qui.
/// Pulire le cartelle di backtest non tocca i best plan.</para>
///
/// <para><b>La curva viene da dove e' piu' fedele.</b> Un run interno ha l'equity mark-to-market nel
/// file di risultato, e si legge da li'; un run del cBot, o un interno interrotto prima di scriverlo,
/// ha solo i trade chiusi, e la curva e' quella realizzata. <see cref="BestPlan.EquitySource"/> lo
/// dichiara, perche' il drawdown delle due non e' la stessa misura.</para>
/// </remarks>
public sealed class BestPlanService
{
    private const string PlanSnapshotFileName = "plan.json";
    private const string RealizedEquity = "realizzata";
    private const string MarkToMarketEquity = "mark-to-market";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WorkspaceService _workspaces;
    private readonly TradingPlanService _plans;
    private readonly string _root;
    private readonly object _gate = new();

    public BestPlanService(WorkspaceService workspaces, TradingPlanService plans, PiootooSettings settings)
        : this(workspaces, plans, settings.GetBestPlansPath())
    {
    }

    /// <summary>Radice iniettata: per i test.</summary>
    public BestPlanService(WorkspaceService workspaces, TradingPlanService plans, string root)
    {
        _workspaces = workspaces;
        _plans = plans;
        _root = Path.GetFullPath(root);
    }

    public static string MakeId(string workspaceId, string backtestFolder)
        => $"{workspaceId}__{backtestFolder}";

    /// <summary>
    /// Tutti i best plan, dal piu' recente. La curva arriva ridotta alla miniatura: il dettaglio la
    /// chiede intera con <see cref="Get"/>. Una cartella illeggibile si salta, non svuota l'elenco.
    /// </summary>
    public IReadOnlyList<BestPlan> List()
    {
        if (!Directory.Exists(_root))
            return Array.Empty<BestPlan>();

        var result = new List<BestPlan>();
        foreach (var directory in Directory.EnumerateDirectories(_root))
        {
            var plan = TryRead(directory);
            if (plan is null)
                continue;

            plan.Equity = BestPlanPerformance.Downsample(plan.Equity, BestPlanPerformance.SparklineColumns / 5);
            result.Add(plan);
        }

        return result.OrderByDescending(plan => plan.PromotedUtc).ToList();
    }

    public BestPlan Get(string id)
    {
        var directory = ResolveDirectory(id);
        return TryRead(directory)
               ?? throw new DirectoryNotFoundException($"Best plan '{id}' non trovato.");
    }

    public void Delete(string id)
    {
        var directory = ResolveDirectory(id);
        lock (_gate)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Best plan '{id}' non trovato.");
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Il report HTML copiato alla promozione.</summary>
    public string GetHtmlReportPath(string id)
    {
        var artifacts = Path.Combine(ResolveDirectory(id), BestPlan.ArtifactsDirectoryName);
        var report = Directory.Exists(artifacts)
            ? new DirectoryInfo(artifacts).EnumerateFiles("*.html").FirstOrDefault()
            : null;
        return report?.FullName
               ?? throw new FileNotFoundException($"Il best plan '{id}' non ha un report HTML.");
    }

    /// <summary>
    /// Fotografa il backtest e lo aggiunge ai best plan.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">Workspace o backtest inesistente.</exception>
    /// <exception cref="InvalidOperationException">
    /// Il backtest e' gia' un best plan e <see cref="PromoteBestPlanRequest.Overwrite"/> e' falso,
    /// oppure non ha ne' una curva di equity ne' trade chiusi da cui ricostruirla.
    /// </exception>
    public BestPlan Promote(PromoteBestPlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkspaceId) || string.IsNullOrWhiteSpace(request.BacktestFolder))
            throw new ArgumentException("Workspace e cartella di backtest sono obbligatori.");

        var workspaceId = request.WorkspaceId.Trim();
        var folder = request.BacktestFolder.Trim();
        var backtestPath = _workspaces.GetBacktestPath(workspaceId, folder);
        if (!Directory.Exists(backtestPath))
            throw new DirectoryNotFoundException($"Backtest '{folder}' non trovato nel workspace '{workspaceId}'.");

        var id = MakeId(workspaceId, folder);
        var directory = ResolveDirectory(id);

        lock (_gate)
        {
            if (Directory.Exists(directory) && !request.Overwrite)
                throw new InvalidOperationException(
                    $"Il backtest '{folder}' {BestPlan.AlreadyPromotedMessage}. Promuoverlo di nuovo sostituisce la fotografia.");

            var plan = Snapshot(id, workspaceId, folder, backtestPath);

            // Si costruisce accanto e si sostituisce alla fine: una promozione che fallisce a meta'
            // non deve lasciare una cartella a mezzo, ne' cancellare la fotografia precedente.
            var staging = directory + ".tmp-" + Guid.NewGuid().ToString("N")[..8];
            Directory.CreateDirectory(staging);
            try
            {
                CopyArtifacts(plan, workspaceId, backtestPath, Path.Combine(staging, BestPlan.ArtifactsDirectoryName));
                AtomicFileWriter.WriteAllText(
                    Path.Combine(staging, BestPlan.FileName),
                    JsonSerializer.Serialize(plan, Json));

                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
                Directory.Move(staging, directory);
            }
            catch
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, recursive: true);
                throw;
            }

            // Un best plan nomina il piano che l'ha prodotto: se il piano potesse ancora cambiare,
            // lo stesso codice finirebbe per indicare una configurazione diversa da quella misurata.
            // Si blocca dopo la copia, cosi' una promozione fallita non lascia un piano bloccato.
            if (TryGetPlan(workspaceId, plan.PlanCode) is { Locked: false })
                _plans.Lock(workspaceId, plan.PlanCode);

            return plan;
        }
    }

    private BestPlan Snapshot(string id, string workspaceId, string folder, string backtestPath)
    {
        var info = _workspaces.ListBacktests(workspaceId)
            .FirstOrDefault(item => string.Equals(item.FolderName, folder, StringComparison.OrdinalIgnoreCase));
        var origin = WorkspaceService.ReadBacktestOrigin(backtestPath);
        var summary = ReadJson(Path.Combine(backtestPath, BacktestDiagnosticsSchema.SummaryFileName));
        var sessionSummary = ReadJson(Path.Combine(backtestPath, SessionRunSummarySchema.FileName));
        var trades = new TradingJsonStore(backtestPath).ReadTrades();

        var resultFile = new DirectoryInfo(backtestPath)
            .EnumerateFiles("backtest_*.json")
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        HourlyEquityReader.Series? series = null;
        if (resultFile is not null)
        {
            series = HourlyEquityReader.Read(resultFile.FullName);
            if (series.Points.Count == 0)
                series = null;
        }

        var capital = ReadDecimal(summary, "initialCapital")
                      ?? series?.InitialCapital
                      ?? origin?.InitialCapital
                      ?? ExternalBacktestReportService.FallbackInitialCapital;

        List<BestPlanEquityPoint> points;
        string equitySource;
        if (series is not null)
        {
            points = series.Points;
            points.Sort((left, right) => left.TimeUtc.CompareTo(right.TimeUtc));
            equitySource = MarkToMarketEquity;
        }
        else
        {
            if (trades.Count == 0)
                throw new InvalidOperationException(
                    $"Il backtest '{folder}' non ha ne' una curva di equity ne' trade chiusi: non c'e' niente da mettere in evidenza.");
            points = BestPlanPerformance.EquityFromTrades(trades, capital);
            equitySource = RealizedEquity;
        }

        var planCode = ReadString(summary, "planCode") ?? origin?.PlanCode ?? info?.PlanCode ?? string.Empty;
        var plan = new BestPlan
        {
            Id = id,
            PlanCode = planCode,
            PlanName = TryGetPlan(workspaceId, planCode)?.Name ?? string.Empty,
            WorkspaceId = workspaceId,
            WorkspaceName = _workspaces.List().FirstOrDefault(item => item.Id == workspaceId)?.Name ?? workspaceId,
            BacktestFolder = folder,
            Origin = origin?.Origin ?? info?.Origin ?? BacktestOrigin.Unknown,
            PriceSource = origin?.ResolvedPriceSource.FeedLabel ?? string.Empty,
            Version = (origin?.Origin == BacktestOrigin.ExternalBroker ? origin.ClientVersion : origin?.EngineVersion)
                      ?? string.Empty,
            ExecutedUtc = origin?.CreatedUtc is { } created && created != default
                ? created
                : ReadDateTime(summary, "generatedAtUtc") ?? Directory.GetCreationTimeUtc(backtestPath),
            StartUtc = info?.StartDateUtc ?? ReadDateTime(summary, "requestedStartUtc") ?? points.FirstOrDefault()?.TimeUtc,
            EndUtc = info?.EndDateUtc ?? ReadDateTime(summary, "requestedEndUtc") ?? points.LastOrDefault()?.TimeUtc,
            PromotedUtc = DateTime.UtcNow,
            EquitySource = equitySource
        };

        BestPlanPerformance.Fill(plan, points, capital, trades);
        plan.Strategies = BuildStrategies(trades, summary, sessionSummary);
        plan.Equity = BestPlanPerformance.Downsample(points, BestPlanPerformance.StoredEquityColumns);
        return plan;
    }

    /// <summary>
    /// Le strategie del run: quelle con trade, dai trade stessi, piu' quelle che i summary dichiarano
    /// e che non hanno chiuso niente — una strategia del piano rimasta ferma e' un'informazione.
    /// </summary>
    private static List<BestPlanStrategy> BuildStrategies(
        IReadOnlyList<PersistedTrade> trades, JsonElement? summary, JsonElement? sessionSummary)
    {
        var byCode = new Dictionary<string, BestPlanStrategy>(StringComparer.OrdinalIgnoreCase);
        foreach (var trade in trades)
        {
            var code = string.IsNullOrWhiteSpace(trade.StrategyCode) ? trade.StrategyName : trade.StrategyCode;
            if (!byCode.TryGetValue(code, out var row))
            {
                row = new BestPlanStrategy
                {
                    StrategyCode = code,
                    Symbol = trade.Symbol,
                    TimeframeMinutes = TimeframeFromCode(code)
                };
                byCode[code] = row;
            }

            row.Trades++;
            row.NetProfit += trade.NetProfit;
            if (trade.NetProfit > 0)
                row.WinningTrades++;
        }

        foreach (var source in new[] { summary, sessionSummary })
        {
            if (source is not { } root
                || !root.TryGetProperty("strategies", out var strategies)
                || strategies.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var entry in strategies.EnumerateArray())
            {
                var code = ReadString(entry, "strategyCode");
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                if (!byCode.TryGetValue(code, out var row))
                {
                    row = new BestPlanStrategy { StrategyCode = code };
                    byCode[code] = row;
                }

                if (string.IsNullOrEmpty(row.Symbol))
                    row.Symbol = ReadString(entry, "symbol") ?? string.Empty;
                if (ReadDecimal(entry, "timeframeMinutes") is { } minutes)
                    row.TimeframeMinutes = (int)minutes;
            }
        }

        return byCode.Values
            .OrderBy(row => row.Symbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.StrategyCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void CopyArtifacts(BestPlan plan, string workspaceId, string backtestPath, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var name in new[]
                 {
                     BacktestOriginInfo.FileName,
                     BacktestDiagnosticsSchema.SummaryFileName,
                     SessionRunSummarySchema.FileName
                 })
        {
            var source = Path.Combine(backtestPath, name);
            if (File.Exists(source))
            {
                File.Copy(source, Path.Combine(destination, name));
                plan.Artifacts.Add(name);
            }
        }

        // Attraverso lo store: durante il run i trade stanno anche nel journal .jsonl affiancato, e
        // copiare il solo array darebbe un file che sembra completo e non lo e'.
        var trades = new TradingJsonStore(backtestPath).ReadTrades();
        new TradingJsonStore(destination).WriteTrades(trades);
        plan.Artifacts.Add(TradingPersistenceSchema.TradesFileName);

        var report = new DirectoryInfo(backtestPath)
            .EnumerateFiles("*.html")
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();
        if (report is not null)
        {
            report.CopyTo(Path.Combine(destination, report.Name));
            plan.Artifacts.Add(report.Name);
            plan.HasHtmlReport = true;
        }

        // Il piano come e' oggi, non come era al momento del run: e' dichiarato nel nome del file
        // accanto alla data di promozione, e resta l'unica traccia se il piano cambia o sparisce.
        if (TryGetPlan(workspaceId, plan.PlanCode) is { } tradingPlan)
        {
            AtomicFileWriter.WriteAllText(
                Path.Combine(destination, PlanSnapshotFileName),
                JsonSerializer.Serialize(tradingPlan, Json));
            plan.Artifacts.Add(PlanSnapshotFileName);
        }
    }

    private TradingPlan? TryGetPlan(string workspaceId, string planCode)
    {
        if (string.IsNullOrWhiteSpace(planCode))
            return null;
        try
        {
            return _plans.Get(workspaceId, planCode);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private string ResolveDirectory(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id del best plan obbligatorio.");

        var path = Path.GetFullPath(Path.Combine(_root, id));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(path) != id)
            throw new ArgumentException($"Id del best plan non valido: '{id}'.");
        return path;
    }

    private static BestPlan? TryRead(string directory)
    {
        var path = Path.Combine(directory, BestPlan.FileName);
        if (!File.Exists(path))
            return null;
        try
        {
            return JsonSerializer.Deserialize<BestPlan>(File.ReadAllText(path), Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement? ReadJson(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement? root, string name)
        => root is { } element && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal? ReadDecimal(JsonElement? root, string name)
        => root is { } element && element.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var parsed)
            ? parsed
            : null;

    private static DateTime? ReadDateTime(JsonElement? root, string name)
        => root is { } element && element.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.String && value.TryGetDateTime(out var parsed)
            ? parsed.ToUniversalTime()
            : null;

    /// <summary>Timeframe dal suffisso del codice (<c>PT5DAV_NQ_RHL_001_30</c> → 30), zero se non c'e'.</summary>
    private static int TimeframeFromCode(string code)
    {
        var separator = code.LastIndexOf('_');
        return separator >= 0 && int.TryParse(code[(separator + 1)..], out var minutes) ? minutes : 0;
    }
}
