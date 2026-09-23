using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Lo stato dei dati di un broker, simbolo per simbolo: e' la domanda con cui la coda di ricerca
/// decide se una cella puo' partire, quindi un "pronto" sbagliato fa girare una griglia senza costi.
/// </summary>
public sealed class BrokerDataStatusServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "piootoo-data-status", Guid.NewGuid().ToString("N"));
    private readonly PiootooSettings _settings;
    private readonly BrokerDataStatusService _service;

    public BrokerDataStatusServiceTests()
    {
        _settings = new PiootooSettings
        {
            Workspaces = Path.Combine(_root, "workspaces"),
            ExternalRepositoryPath = Path.Combine(_root, "datafeed-external"),
            SpreadPath = Path.Combine(_root, "spread"),
            SwapPath = Path.Combine(_root, "swap"),
            SymbolInfoPath = Path.Combine(_root, "symbol-info")
        };
        var workspaces = new WorkspaceService(_settings);
        workspaces.CreateSymbolConversion(new SymbolConversion
        {
            Code = "cfd-test",
            Name = "CFD test",
            Mappings =
            [
                new AccountSymbolMapping { Symbol = "@FESX", AccountSymbol = "EU50.cash", Enabled = true },
                new AccountSymbolMapping { Symbol = "@FCE", AccountSymbol = "FRA40.cash", Enabled = true }
            ]
        });
        workspaces.CreateBroker(new TradingBroker { Code = "FTMO", Name = "FTMO", SymbolConversionCode = "cfd-test" });
        _service = new BrokerDataStatusService(_settings, workspaces, new SymbolInfoStore(_settings));

        var feeds = Path.Combine(_settings.GetExternalRepositoryPath(), "FTMO");
        Directory.CreateDirectory(feeds);
        File.WriteAllText(Path.Combine(feeds, "@FESX_1.json"),
            "{\"symbol\":\"@FESX\",\"candles\":[{\"dateTime\":\"2020-11-09T03:00:00Z\"},{\"dateTime\":\"2026-09-21T19:59:00Z\"}]}");
        File.WriteAllText(Path.Combine(feeds, "@FESX_240.json"),
            "{\"symbol\":\"@FESX\",\"source\":\"griglia(1m->240m, finestra 00:15Z-22:00L)\",\"candles\":[]}");
        File.WriteAllText(Path.Combine(feeds, "@FCE_1.json"),
            "{\"symbol\":\"@FCE\",\"candles\":[{\"dateTime\":\"2020-11-09T03:00:00Z\"}]}");

        var spread = Path.Combine(_settings.GetSpreadPath(), "FTMO");
        Directory.CreateDirectory(spread);
        File.WriteAllText(Path.Combine(spread, "FTMO_spread-by-symbol_20260801-20260901.csv"),
            SpreadMeasurementStore.SymbolHeader + "\n" +
            "FTMO,@FESX,EU50.cash,100,2026-08-01T00:00:00Z,2026-09-01T00:00:00Z,0.01,1,1.00,1.36,1.40,1.80,2.00,3.00,100,136,140.00,180,200,300,0,false,\n");

        var swap = Path.Combine(_settings.GetSwapPath(), "FTMO");
        Directory.CreateDirectory(swap);
        File.WriteAllText(Path.Combine(swap, "FTMO_swap-by-symbol.csv"),
            SwapFromSymbolInfo.Header + "\n" +
            "FTMO,@FESX,-1.1327,-0.0114,1,20:59,Friday,pagina simboli FTMO\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ASymbolWithBarsSpreadAndSwapIsReady()
    {
        var eu50 = _service.Get("FTMO").Symbols.Single(row => row.Symbol == "@FESX");

        Assert.True(eu50.Ready, string.Join("; ", eu50.Missing));
        Assert.Equal("EU50.cash", eu50.BrokerSymbol);
        Assert.Equal(new DateTime(2020, 11, 9, 3, 0, 0, DateTimeKind.Utc), eu50.MinuteFromUtc);
        Assert.Equal(new DateTime(2026, 9, 21, 19, 59, 0, DateTimeKind.Utc), eu50.MinuteToUtc);
        Assert.Equal([240], eu50.Aggregates);
        Assert.Equal("1.36", eu50.SpreadMedian);
        Assert.Equal("manuale", eu50.Swap);
    }

    /// <summary>Un simbolo senza spread ne' swap non e' pronto, e dice cosa gli manca.</summary>
    [Fact]
    public void AMissingCostIsNamed()
    {
        var fra40 = _service.Get("FTMO").Symbols.Single(row => row.Symbol == "@FCE");

        Assert.False(fra40.Ready);
        Assert.Contains("spread", fra40.Missing);
        Assert.Contains("swap", fra40.Missing);
    }
}
