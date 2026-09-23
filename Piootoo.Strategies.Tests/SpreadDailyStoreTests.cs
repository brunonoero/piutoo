using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le giornate di spread che il raccoglitore manda ogni notte: il server le archivia e riscrive la
/// finestra mobile degli ultimi trenta giorni nei due CSV che il backtest legge. Gli istogrammi si
/// sommano esattamente, quindi la finestra vale quanto una misura sull'intero periodo.
/// </summary>
public sealed class SpreadDailyStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "piootoo-spread-daily", Guid.NewGuid().ToString("N"));
    private readonly PiootooSettings _settings;
    private readonly SpreadDailyStore _store;
    private readonly SpreadMeasurementStore _measurements;

    public SpreadDailyStoreTests()
    {
        _settings = new PiootooSettings
        {
            Workspaces = Path.Combine(_root, "workspaces"),
            SpreadPath = Path.Combine(_root, "spread")
        };
        var workspaces = new WorkspaceService(_settings);
        workspaces.CreateBroker(new TradingBroker { Code = "FTMO", Name = "FTMO" });
        workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = "conto-ftmo", AccountNumber = "4001", BrokerCode = "FTMO", InitialBalance = 100_000m
        });
        _measurements = new SpreadMeasurementStore(_settings, workspaces);
        _store = new SpreadDailyStore(_settings, workspaces, _measurements);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Due giorni sommati danno la mediana della loro unione, non la media delle due mediane: e' la
    /// ragione per cui si archiviano istogrammi. Un giorno a 1 tick (100 osservazioni) e uno a 3 tick
    /// (300) hanno mediane 1 e 3; l'unione ha mediana 3.
    /// </summary>
    [Fact]
    public void TheWindowIsTheExactDistributionOfTheDaysTogether()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        _store.Ingest(Request(Day("@FESX", yesterday.AddDays(-1), 10, (100, 100))));
        _store.Ingest(Request(Day("@FESX", yesterday, 10, (300, 300))));

        var table = SpreadTable.Load(_settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerHour);

        // 300 tick da 0,01 = 3,00 punti.
        Assert.Equal(3.00m, table.Points["FESX"]);
        Assert.Equal(3.00m, table.PointsByHour["FESX"][10]);
    }

    /// <summary>La finestra e' mobile: un giorno piu' vecchio di trenta dall'ultimo raccolto esce.</summary>
    [Fact]
    public void ADayOlderThanTheWindowLeavesTheMeasurement()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        _store.Ingest(Request(Day("@FESX", yesterday.AddDays(-SpreadDailyStore.WindowDays), 10, (500, 1000))));
        _store.Ingest(Request(Day("@FESX", yesterday, 10, (100, 10))));

        var table = SpreadTable.Load(_settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol);

        Assert.Equal(1.00m, table.Points["FESX"]);
    }

    /// <summary>
    /// Rimandare un giorno lo sostituisce: rimisurarlo e' il modo di correggerlo, e sommarlo due volte
    /// raddoppierebbe il suo peso nella finestra.
    /// </summary>
    [Fact]
    public void ResendingADayReplacesIt()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        _store.Ingest(Request(Day("@FESX", yesterday, 10, (500, 50))));
        _store.Ingest(Request(Day("@FESX", yesterday, 10, (200, 50))));

        var table = SpreadTable.Load(_settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol);

        Assert.Equal(2.00m, table.Points["FESX"]);
        Assert.Equal(["@FESX"], _store.GetStatus("4001", yesterday.AddDays(-5)).Days.Keys);
    }

    /// <summary>
    /// I simboli che la consegna non tocca restano: e' la stessa unione delle misure del bot, e una
    /// giornata di EU50 non deve togliere ai run lo spread di NQ.
    /// </summary>
    [Fact]
    public void ADailyWindowKeepsTheSymbolsItDoesNotTouch()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        _store.Ingest(Request(Day("@NQ", yesterday, 14, (150, 20))));
        _store.Ingest(Request(Day("@FESX", yesterday, 10, (100, 20))));

        var table = SpreadTable.Load(_settings.GetSpreadPath(), "FTMO", SpreadStatistic.Median, SpreadResolution.PerSymbol);

        Assert.Equal(1.50m, table.Points["NQ"]);
        Assert.Equal(1.00m, table.Points["FESX"]);
    }

    /// <summary>
    /// Un giorno senza tick (sabato, festivo) si registra ma non scrive una misura: una riga di celle
    /// vuote cancellerebbe quella vera. Registrarlo serve a non farlo rimisurare ogni notte.
    /// </summary>
    [Fact]
    public void ADayWithoutTicksIsRecordedButWritesNoMeasurement()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var empty = new SpreadDayDto { Symbol = "@FESX", BrokerSymbol = "EU50.cash", DayUtc = yesterday, TickSize = 0.01m, Digits = 2 };

        var response = _store.Ingest(Request(empty));

        Assert.Null(response.SymbolFile);
        Assert.Contains("@FESX", _store.GetStatus("4001", yesterday).Days.Keys);
        Assert.False(Directory.EnumerateFiles(Path.Combine(_settings.GetSpreadPath(), "FTMO"), "*.csv").Any());
    }

    /// <summary>Il giorno in corso non e' finito: misurarlo darebbe mezza giornata per una intera.</summary>
    [Fact]
    public void TodayIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            _store.Ingest(Request(Day("@FESX", DateTime.UtcNow.Date, 10, (100, 10)))));
    }

    /// <summary>Stesso algoritmo del bot: il primo valore che copre il quantile, senza interpolare.</summary>
    [Fact]
    public void PercentilesCountWithoutInterpolating()
    {
        var histogram = new SpreadHistogram();
        histogram.Add(1, 5);
        histogram.Add(2, 4);
        histogram.Add(9, 1);

        Assert.Equal(1, histogram.Percentile(0.50));
        Assert.Equal(2, histogram.Percentile(0.90));
        Assert.Equal(9, histogram.Percentile(0.99));
        Assert.Equal(2.2d, histogram.Mean, 6);
    }

    private static SpreadDailyRequest Request(params SpreadDayDto[] days) =>
        new() { AccountNumber = "4001", BotVersion = "test", Days = days.ToList() };

    private static SpreadDayDto Day(string symbol, DateTime day, int hour, (int Ticks, long Count) bin) => new()
    {
        Symbol = symbol,
        BrokerSymbol = symbol == "@NQ" ? "US100.cash" : "EU50.cash",
        DayUtc = DateTime.SpecifyKind(day, DateTimeKind.Utc),
        TickSize = 0.01m,
        PipSize = 1m,
        Digits = 2,
        FirstTickUtc = DateTime.SpecifyKind(day.AddHours(hour), DateTimeKind.Utc),
        LastTickUtc = DateTime.SpecifyKind(day.AddHours(hour + 1), DateTimeKind.Utc),
        Hours = [new SpreadHourDto { Hour = hour, Bins = [[bin.Ticks, bin.Count]] }]
    };
}
