using System.Text.Json;
using Piootoo.Shared.MarketData;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il metro della maschera: quante barre toglie davvero dall'archivio del broker, simbolo per
/// simbolo.
///
/// <para><b>Perché è un test e non uno script.</b> I numeri sono il contratto di questo
/// cambiamento: dicono che la maschera morde dove deve — BTC quota il sabato, FDAX l'ora prima
/// dell'apertura Eurex — e che <b>non</b> morde dove il broker rispetta già l'orario del future.
/// Undici simboli su quattordici perdono zero barre, ed è quel numero a dimostrare che la finestra
/// dichiarata è quella vera e non una plausibile: una finestra sbagliata di un'ora comincerebbe a
/// togliere barre a simboli che oggi ne perdono nessuna.</para>
/// </summary>
public sealed class SessionMaskArchiveTests(ITestOutputHelper output)
{
    private const string Broker = "FTMOPLATFORM";

    /// <summary>
    /// Quota massima di barre da un minuto che la maschera può togliere, per simbolo. I valori sono
    /// quelli misurati l'08/09/2026 sull'archivio 2025-07-01 → 2026-09-08, arrotondati per eccesso:
    /// servono a far fallire uno spostamento della finestra, non a fissare il conteggio esatto, che
    /// cambia a ogni raccolta.
    /// </summary>
    private static readonly Dictionary<string, double> MaxRemovedFraction = new()
    {
        ["NQ"] = 0.001, ["ES"] = 0.001, ["YM"] = 0.001, ["GC"] = 0.001, ["CL"] = 0.001,
        ["NG"] = 0.001, ["PL"] = 0.001, ["CC"] = 0.001, ["KC"] = 0.001, ["SB"] = 0.001,
        ["CT"] = 0.001,
        ["BP"] = 0.045,     // CFD FX: gira 24/5 continuo e non ha la pausa CME
        ["FDAX"] = 0.125,   // l'ora prima dell'apertura Eurex e quella dopo la chiusura
        ["BTC"] = 0.290     // CFD 24/7 contro un future CME: sabato intero piu' la pausa quotidiana
    };

    [Fact]
    public void MaskRemovesOnlyWhatTheBrokerQuotesOutsideFuturesHours()
    {
        var root = Path.Combine(FindSolutionRoot(), "piootoo-repository", "datafeed-external", Broker);
        if (!Directory.Exists(root))
        {
            output.WriteLine($"METRO NON ESEGUITO: archivio '{root}' assente.");
            return;
        }

        var misurati = 0;
        foreach (var (symbol, limite) in MaxRemovedFraction.OrderBy(entry => entry.Key))
        {
            var file = Path.Combine(root, $"@{symbol}_1.json");
            if (!File.Exists(file))
            {
                output.WriteLine($"  {symbol,-5} archivio al minuto assente, non misurato.");
                continue;
            }

            var mask = SessionMask.For(symbol);
            var (total, removed) = Count(file, mask);
            if (total == 0)
                continue;

            var fraction = (double)removed / total;
            output.WriteLine(
                $"  {symbol,-5} {total,8} barre, tolte {removed,7} ({fraction,6:P1}), limite {limite:P1}");

            Assert.True(
                fraction <= limite,
                $"{symbol}: la maschera toglie il {fraction:P1} delle barre, oltre il {limite:P1} " +
                "misurato. Una finestra spostata di un'ora si vede esattamente cosi'.");

            misurati++;
        }

        Assert.True(misurati > 0, "Nessuno stream misurato: l'archivio c'e' ma non ha file al minuto.");
    }

    private static (int Total, int Removed) Count(string path, SessionMask mask)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("candles", out var candles))
            return (0, 0);

        var total = 0;
        var removed = 0;
        foreach (var candle in candles.EnumerateArray())
        {
            if (!candle.TryGetProperty("dateTime", out var when) ||
                !when.TryGetDateTime(out var instant))
            {
                continue;
            }

            total++;
            if (mask.IsOpen(DateTime.SpecifyKind(instant, DateTimeKind.Utc)) == false)
                removed++;
        }

        return (total, removed);
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                $"PiootooApp.sln non trovata risalendo da {AppContext.BaseDirectory}.");
    }
}
