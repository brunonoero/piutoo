using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il flat di fine settimana è una regola del CONTO, non della strategia: lo decide il piano
/// (<c>AccountHoldingPolicy.AllowOverweek</c>) e non guarda cosa la strategia ha dichiarato. Su una
/// strategia che chiude da sé a fine sessione è innocuo; su una che resta aperta per giorni le
/// taglia i trade a metà.
///
/// <para>La differenza non si vede da nessuna parte, perché l'uscita ha comunque un prezzo e un
/// motivo plausibili: si scopre solo confrontando con la lista di trade della ricerca. Sul
/// confronto del 26/08/2026, su <c>PTS_GC_TFU_001_30</c> le 25 uscite di fine settimana valevano
/// 479 punti — il 97% dello scarto di quel porting. Questi test tengono in piedi la riga di
/// diagnostica che lo dice a voce alta su ogni run.</para>
/// </summary>
public class WeekEndFlatDiagnosticsTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "piootoo-diag-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// I numeri veri del confronto: PCH multiday (nessuna uscita a tempo) al 62%, TFU multiday al
    /// 43%, le due RHL intraday al 12% e 6% — queste ultime non vanno segnalate, perché il flat le
    /// sfiora e basta.
    /// </summary>
    [Fact]
    public void SegnalaSoloLeStrategieCheIlFlatTagliaDavvero()
    {
        using var logger = new BacktestDiagnosticsLogger(_dir, "job-1");
        Registra(logger, "PTS_GC_PCH_001_60", 60, trades: 39, weekEnd: 24);
        Registra(logger, "PTS_GC_TFU_001_30", 30, trades: 67, weekEnd: 29);
        Registra(logger, "PTS_GC_RHL_001_60", 60, trades: 32, weekEnd: 4);
        Registra(logger, "PTS_GC_RHL_002_60", 60, trades: 35, weekEnd: 2);

        var riga = Assert.Single(Diagnosi(logger), d => d.StartsWith("[fine settimana]", StringComparison.Ordinal));

        Assert.Contains("24 trade su 39", riga);
        Assert.Contains("PTS_GC_PCH_001_60", riga);
        Assert.Contains("29 trade su 67", riga);
        Assert.Contains("PTS_GC_TFU_001_30", riga);
        // Le intraday restano fuori: il flat non è ciò che le chiude.
        Assert.DoesNotContain("PTS_GC_RHL_001_60", riga);
        Assert.DoesNotContain("PTS_GC_RHL_002_60", riga);
        // La più colpita per prima: è quella da guardare.
        Assert.True(riga.IndexOf("PTS_GC_PCH_001_60", StringComparison.Ordinal)
                    < riga.IndexOf("PTS_GC_TFU_001_30", StringComparison.Ordinal));
    }

    /// <summary>
    /// Col flat spento non esistono uscite <c>WeekEnd</c>, quindi la diagnosi non ha di che
    /// parlare: non serve passarle il flag, si spegne da sola.
    /// </summary>
    [Fact]
    public void SenzaUsciteDiFineSettimana_NonDiceNulla()
    {
        using var logger = new BacktestDiagnosticsLogger(_dir, "job-2");
        Registra(logger, "PTS_GC_TFU_001_30", 30, trades: 67, weekEnd: 0);

        Assert.DoesNotContain(Diagnosi(logger), d => d.StartsWith("[fine settimana]", StringComparison.Ordinal));
    }

    /// <summary>
    /// Una manciata di chiusure tecniche su un campione grande non è un difetto di progetto: la
    /// soglia esiste per non trasformare la diagnosi in rumore che nessuno legge più.
    /// </summary>
    [Fact]
    public void PocheChiusureSuTantiTrade_NonSonoUnaSegnalazione()
    {
        using var logger = new BacktestDiagnosticsLogger(_dir, "job-3");
        Registra(logger, "PTS_GC_RHL_001_60", 60, trades: 100, weekEnd: 4);

        Assert.DoesNotContain(Diagnosi(logger), d => d.StartsWith("[fine settimana]", StringComparison.Ordinal));
    }

    /// <summary>
    /// La policy del conto entra nel summary: due run con permessi o orari diversi non sono
    /// confrontabili, e chi li rilegge mesi dopo non ha altro modo di accorgersene.
    /// </summary>
    [Fact]
    public void LaPolicyDelContoRestaScrittaNelSummary()
    {
        using var logger = new BacktestDiagnosticsLogger(_dir, "job-4");
        var summary = logger.Complete(new BacktestRunSummary
        {
            JobId = "job-4",
            Holding = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtcHhmm = 2045 }
        });

        Assert.NotNull(summary.Holding);
        Assert.False(summary.Holding!.AllowOvernight);
        Assert.Equal(2045, summary.Holding.SessionFlatUtcHhmm);
        Assert.Equal(2045, summary.Holding.WeekEnd.FromUtcHhmm);
    }

    /// <summary>
    /// La riga gemella per l'altro asse: il flat di SESSIONE imposto dal piano, che è un
    /// troncamento diverso dal fine settimana e non va confuso con la deadline della strategia.
    /// Nessuna soglia qui — un solo trade tagliato dal conto è già una divergenza dalla ricerca.
    /// </summary>
    [Fact]
    public void SegnalaIlTroncamentoDiSessioneImpostoDalPiano()
    {
        using var logger = new BacktestDiagnosticsLogger(_dir, "job-5");
        Registra(logger, "PTS_GC_TFU_001_30", 30, trades: 40, tagliati: 12,
            motivo: TradeExitReason.SessionFlat);

        var riga = Assert.Single(Diagnosi(logger), d => d.StartsWith("[fine sessione]", StringComparison.Ordinal));

        Assert.Contains("12 trade su 40", riga);
        Assert.Contains("PTS_GC_TFU_001_30", riga);
        // Le due righe parlano di due tagli diversi e non devono sovrapporsi.
        Assert.DoesNotContain(Diagnosi(logger), d => d.StartsWith("[fine settimana]", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> Diagnosi(BacktestDiagnosticsLogger logger) =>
        logger.Complete(new BacktestRunSummary { JobId = "x" }).Diagnostics;

    private static void Registra(
        BacktestDiagnosticsLogger logger, string code, int timeframe, int trades, int weekEnd)
        => Registra(logger, code, timeframe, trades, weekEnd, TradeExitReason.WeekEnd);

    private static void Registra(
        BacktestDiagnosticsLogger logger, string code, int timeframe, int trades, int tagliati,
        TradeExitReason motivo)
    {
        logger.RegisterStrategy(code, code, "GC", timeframe);
        var t0 = new DateTime(2022, 1, 3, 12, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < trades; i++)
            logger.LogExit(new PositionClosedEvent
            {
                StrategyCode = code,
                StrategyName = code,
                Symbol = "GC",
                Direction = SignalType.Buy,
                EntryTimeUtc = t0.AddDays(i),
                ExitTimeUtc = t0.AddDays(i).AddHours(6),
                EntryPrice = 1800m,
                ExitPrice = 1805m,
                Contracts = 1m,
                ExitReason = i < tagliati ? motivo : TradeExitReason.StopLoss
            });
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
