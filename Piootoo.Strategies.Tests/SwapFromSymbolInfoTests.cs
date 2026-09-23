using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Brokers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le righe di swap scritte dal server a partire dalle schede del raccoglitore. Il caso che le
/// giustifica e' EU50 del 23/09/2026: la scheda di quel giorno portava l'aggiustamento per il
/// dividendo, il 27% annuo sullo short, e copiarla nella tabella avrebbe fatto pagare a ogni short di
/// ogni run un costo che nessun conto paga.
/// </summary>
public sealed class SwapFromSymbolInfoTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "piootoo-swap-auto", Guid.NewGuid().ToString("N"));
    private readonly PiootooSettings _settings;
    private readonly SymbolInfoStore _symbolInfo;
    private readonly SwapFromSymbolInfo _swap;
    private readonly DateTime _now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    public SwapFromSymbolInfoTests()
    {
        _settings = new PiootooSettings
        {
            SwapPath = Path.Combine(_root, "swap"),
            SymbolInfoPath = Path.Combine(_root, "symbol-info")
        };
        _symbolInfo = new SymbolInfoStore(_settings);
        _swap = new SwapFromSymbolInfo(_settings, _symbolInfo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Un giorno anomalo fra molti normali non sposta la mediana: la riga porta il tasso base, e il
    /// backtest lo legge come costo in punti.
    /// </summary>
    [Fact]
    public async Task AOneDayDividendAdjustmentDoesNotBecomeTheCost()
    {
        WriteManualRows("FTMO,@FDAX,-4.5288,-0.0457,1,20:59,Friday,scheda GER40.cash cTrader 2026-09-21");
        for (var day = 1; day <= 9; day++)
            await Snapshot("EU50.cash", "@FESX", _now.AddDays(-10 + day), "-1.13", "-0.01", pipValue: day);
        await Snapshot("EU50.cash", "@FESX", _now.AddDays(-0.5), "3.56", "-4.71", pipValue: 99);

        var result = _swap.Rebuild("FTMO", _now);

        Assert.Equal(["@FESX"], result.Auto);
        var table = SwapTable.Load(_settings.GetSwapPath(), "FTMO");
        Assert.Equal(1.13m, table.For("FESX").LongPointsPerNight);
        Assert.Equal(0.01m, table.For("FESX").ShortPointsPerNight);
        Assert.Equal(new TimeOnly(20, 59), table.For("FESX").RolloverUtc);
        // La riga scritta a mano c'e' ancora, com'era.
        Assert.Equal(4.5288m, table.For("FDAX").LongPointsPerNight);
    }

    /// <summary>Una riga scritta a mano vince: il suo simbolo non riceve una riga automatica.</summary>
    [Fact]
    public async Task AManualRowWinsOverTheSymbolInfo()
    {
        WriteManualRows("FTMO,@FESX,-1.1327,-0.0114,1,20:59,Friday,pagina simboli FTMO 2026-09-23");
        await Snapshot("EU50.cash", "@FESX", _now.AddDays(-1), "3.56", "-4.71", pipValue: 1);

        var result = _swap.Rebuild("FTMO", _now);

        Assert.Empty(result.Auto);
        Assert.Equal(["@FESX"], result.Manual);
        Assert.Equal(0.0114m, SwapTable.Load(_settings.GetSwapPath(), "FTMO").For("FESX").ShortPointsPerNight);
    }

    /// <summary>
    /// Le righe automatiche si rigenerano: un secondo giro con schede diverse non lascia la riga
    /// vecchia accanto alla nuova.
    /// </summary>
    [Fact]
    public async Task AutomaticRowsAreRegeneratedNotAppended()
    {
        WriteManualRows("FTMO,@FDAX,-4.5288,-0.0457,1,20:59,Friday,scheda GER40.cash cTrader 2026-09-21");
        await Snapshot("UK100.cash", "@Z", _now.AddDays(-2), "-1.47", "-0.84", pipValue: 1);
        _swap.Rebuild("FTMO", _now);
        _swap.Rebuild("FTMO", _now);

        var lines = File.ReadAllLines(Directory.EnumerateFiles(Path.Combine(_settings.GetSwapPath(), "FTMO")).Single());
        Assert.Single(lines, line => line.StartsWith("FTMO,@Z,", StringComparison.Ordinal));
    }

    /// <summary>
    /// Lo swap in percentuale del nozionale non si esprime in punti fissi: niente riga, e il motivo
    /// va detto, perche' un run che chiede lo swap su ETH fallira' all'avvio.
    /// </summary>
    [Fact]
    public async Task PercentageSwapGetsNoRowAndSaysWhy()
    {
        WriteManualRows("FTMO,@FDAX,-4.5288,-0.0457,1,20:59,Friday,scheda GER40.cash cTrader 2026-09-21");
        await Snapshot("ETHUSD", "@ETH", _now.AddDays(-1), "-30", "-30", pipValue: 1, type: "Percentage");

        var result = _swap.Rebuild("FTMO", _now);

        Assert.Empty(result.Auto);
        Assert.Contains(result.Warnings, warning => warning.Contains("@ETH") && warning.Contains("Percentage"));
    }

    /// <summary>
    /// Senza nemmeno una riga scritta a mano l'ora del rollover non si sa — la scheda non la dichiara —
    /// e il calcolo non scrive niente invece di inventarla.
    /// </summary>
    [Fact]
    public async Task WithoutAManualRowTheRolloverIsUnknownAndNothingIsWritten()
    {
        await Snapshot("EU50.cash", "@FESX", _now.AddDays(-1), "-1.13", "-0.01", pipValue: 1);

        var result = _swap.Rebuild("FTMO", _now);

        Assert.Null(result.File);
        Assert.NotEmpty(result.Warnings);
    }

    private void WriteManualRows(params string[] rows)
    {
        var folder = Path.Combine(_settings.GetSwapPath(), "FTMO");
        Directory.CreateDirectory(folder);
        File.WriteAllLines(Path.Combine(folder, "FTMO_swap-by-symbol.csv"),
            new[] { "# test", SwapFromSymbolInfo.Header }.Concat(rows));
    }

    private Task Snapshot(string brokerSymbol, string piootooSymbol, DateTime takenUtc, string swapLong, string swapShort,
        int pipValue, string type = "Pips") =>
        _symbolInfo.IngestAsync(new SymbolInfoIngestRequestDto
        {
            Broker = "FTMO",
            AccountNumber = "4001",
            TakenUtc = takenUtc,
            Symbols =
            [
                new SymbolInfoDto
                {
                    BrokerSymbol = brokerSymbol,
                    PiootooSymbol = piootooSymbol,
                    Properties = new Dictionary<string, string>
                    {
                        ["SwapCalculationType"] = type,
                        ["SwapLong"] = swapLong,
                        ["SwapShort"] = swapShort,
                        ["PipSize"] = "1",
                        ["PipValue"] = pipValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["Swap3DaysRollover"] = "Friday"
                    }
                }
            ]
        });
}
