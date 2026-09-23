using System.Text;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le misure di spread che il bot manda al server finiscono in <c>spread/{BROKER}/</c>, con la
/// cartella decisa dal registro dei broker, e <b>si uniscono</b> a quella che c'era: il caricatore
/// del backtest legge un file solo, e una misura parziale scritta accanto toglierebbe ai run lo
/// spread di tutti i simboli che non ha rimisurato.
/// </summary>
public sealed class SpreadMeasurementStoreTests : IDisposable
{
    private const string SymbolHeader =
        "broker,symbol,brokerSymbol,ticks,firstTickUtc,lastTickUtc,tickSize,pipSize," +
        "minSpread,p50Spread,avgSpread,p90Spread,p99Spread,maxSpread," +
        "minSpreadTicks,p50SpreadTicks,avgSpreadTicks,p90SpreadTicks,p99SpreadTicks,maxSpreadTicks," +
        "nonPositiveTicks,truncated,note";

    private const string HourHeader =
        "broker,symbol,hourUtc,ticks,tickSize," +
        "minSpread,p50Spread,avgSpread,p90Spread,p99Spread,maxSpread," +
        "minSpreadTicks,p50SpreadTicks,avgSpreadTicks,p90SpreadTicks,p99SpreadTicks,maxSpreadTicks," +
        "nonPositiveTicks";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "piootoo-spread-store", Guid.NewGuid().ToString("N"));
    private readonly PiootooSettings _settings;
    private readonly SpreadMeasurementStore _store;

    public SpreadMeasurementStoreTests()
    {
        _settings = new PiootooSettings
        {
            Workspaces = Path.Combine(_root, "workspaces"),
            SpreadPath = Path.Combine(_root, "spread")
        };
        var workspaces = new WorkspaceService(_settings);
        workspaces.CreateBroker(new TradingBroker { Code = "ICS", Name = "ICS" });
        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-ics", AccountNumber = "3001", BrokerCode = "ICS", InitialBalance = 100_000m
        });
        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-senza-broker", AccountNumber = "9001", InitialBalance = 100_000m
        });
        _store = new SpreadMeasurementStore(_settings, workspaces);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void TheMeasurementGoesIntoTheFolderOfTheAccountBroker()
    {
        var response = _store.Ingest(Measurement("3001", ("@NQ", 1.0m), ("@FDAX", 0.5m)));

        Assert.Equal("ICS", response.Broker);
        Assert.True(File.Exists(Path.Combine(_settings.GetSpreadPath(), "ICS", response.SymbolFile)));

        var table = SpreadTable.Load(_settings.GetSpreadPath(), "ICS", SpreadStatistic.Median, SpreadResolution.PerHour);
        Assert.Equal(0.5m, table.Points["FDAX"]);
        Assert.Equal(1.0m, table.PointsByHour["NQ"][10]);
    }

    /// <summary>
    /// Il caso che questa unione esiste per coprire: una misura dei soli simboli nuovi non deve
    /// togliere ai run quelli misurati prima, e un simbolo rimisurato prende costante e ore nuove
    /// insieme.
    /// </summary>
    [Fact]
    public void ANewMeasurementKeepsTheSymbolsItDidNotMeasure()
    {
        _store.Ingest(Measurement("3001", ("@NQ", 1.0m), ("@FDAX", 0.5m)));

        var response = _store.Ingest(Measurement("3001", ("@NQ", 1.5m), ("@FESX", 2.0m)));

        Assert.Equal(["@FDAX"], response.Kept);
        Assert.Equal(["@FESX", "@NQ"], response.Measured);

        var table = SpreadTable.Load(_settings.GetSpreadPath(), "ICS", SpreadStatistic.Median, SpreadResolution.PerHour);
        Assert.Equal(0.5m, table.Points["FDAX"]);
        Assert.Equal(1.5m, table.Points["NQ"]);
        Assert.Equal(2.0m, table.Points["FESX"]);
        Assert.Equal(1.5m, table.PointsByHour["NQ"][10]);
        Assert.Equal(0.5m, table.PointsByHour["FDAX"][10]);
    }

    [Fact]
    public void TheReplacedPairAndTheRawMeasurementGoToTheArchive()
    {
        _store.Ingest(Measurement("3001", ("@NQ", 1.0m)));
        var response = _store.Ingest(Measurement("3001", ("@FESX", 2.0m), from: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)));

        var folder = Path.Combine(_settings.GetSpreadPath(), "ICS");
        Assert.Single(Directory.GetFiles(folder, "*spread-by-symbol*.csv"));
        Assert.Single(Directory.GetFiles(folder, "*spread-by-hour*.csv"));
        Assert.Contains(response.Archived, path => path.Contains("sostituito-", StringComparison.Ordinal));
        Assert.Contains(response.Archived, path => path.Contains("misura-", StringComparison.Ordinal));
        Assert.All(response.Archived, path => Assert.True(File.Exists(Path.Combine(folder, path)), path));
    }

    [Fact]
    public void AnAccountWithoutBrokerIsRejectedAndNothingIsWritten()
    {
        var error = Assert.Throws<InvalidOperationException>(() => _store.Ingest(Measurement("9001", ("@NQ", 1.0m))));

        Assert.Contains("9001", error.Message);
        Assert.False(Directory.Exists(_settings.GetSpreadPath()) &&
                     Directory.EnumerateFiles(_settings.GetSpreadPath(), "*.csv", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public void AMeasurementWithDifferentColumnsIsNotMerged()
    {
        _store.Ingest(Measurement("3001", ("@NQ", 1.0m)));
        var request = Measurement("3001", ("@FESX", 2.0m));
        request.BySymbolCsv = request.BySymbolCsv.Replace(",note", ",altro", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => _store.Ingest(request));

        var table = SpreadTable.Load(_settings.GetSpreadPath(), "ICS", SpreadStatistic.Median);
        Assert.Equal(["NQ"], table.Points.Keys);
    }

    // ------------------------------------------------------------------------------ infrastruttura

    private static SpreadMeasurementRequest Measurement(
        string account, (string Symbol, decimal Spread) first, params (string Symbol, decimal Spread)[] others) =>
        Measurement(account, [first, .. others], from: null);

    private static SpreadMeasurementRequest Measurement(
        string account, (string Symbol, decimal Spread) only, DateTime from) =>
        Measurement(account, [only], from);

    private static SpreadMeasurementRequest Measurement(
        string account, (string Symbol, decimal Spread)[] rows, DateTime? from)
    {
        var start = from ?? new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var symbols = new StringBuilder("# Piootoo Spread Dump v3.0.0 - broker X\n# spread = ask - bid.\n")
            .Append(SymbolHeader).Append('\n');
        var hours = new StringBuilder("# Piootoo Spread Dump v3.0.0 - broker X\n").Append(HourHeader).Append('\n');

        foreach (var (symbol, spread) in rows)
        {
            var s = spread.ToString(System.Globalization.CultureInfo.InvariantCulture);
            symbols.Append($"X,{symbol},B{symbol.TrimStart('@')},1000,2026-08-01T00:00:00Z,2026-08-31T23:59:00Z,0.01,0.1,")
                .Append($"{s},{s},{s},{s},{s},{s},1,1,1,1,1,1,0,false,\n");
            for (var hour = 0; hour < 24; hour++)
                hours.Append($"X,{symbol},{hour},10,0.01,{s},{s},{s},{s},{s},{s},1,1,1,1,1,1,0\n");
        }

        return new SpreadMeasurementRequest
        {
            AccountNumber = account,
            BotVersion = "3.0.0",
            WindowFromUtc = start,
            WindowToUtc = start.AddDays(30),
            BySymbolCsv = symbols.ToString(),
            ByHourCsv = hours.ToString()
        };
    }
}
