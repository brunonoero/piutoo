using Piootoo.Core.Services;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'orologio del loop di backtesting.
///
/// <para>Il fatto da fissare non è "si può scegliere il tick", che sarebbe ovvio, ma <b>quali
/// combinazioni si rifiutano e perché</b>: le tre che qui falliscono darebbero altrimenti un run
/// apparentemente normale e silenziosamente sbagliato — un orologio più lento delle strategie, o
/// uno che ne salta una in silenzio. Un backtest che sembra completo e ha meno strategie è il
/// difetto peggiore che questo motore possa produrre.</para>
/// </summary>
public sealed class BacktestClockTests
{
    private static readonly (string Name, int TimeframeMinutes)[] Portafoglio =
    [
        ("PTS_NQ_PCH_001_15", 15),
        ("PTS_NQ_TFM_001_60", 60),
        ("PTS_ES_VBO_002_240", 240)
    ];

    /// <summary>
    /// Senza richiesta l'orologio è il timeframe più corto del run: è il comportamento che il
    /// motore ha sempre avuto, e non deve cambiare per il fatto che ora si possa forzare.
    /// </summary>
    [Fact]
    public void SenzaRichiestaLOrologioEIlTimeframePiuCorto()
        => Assert.Equal(15, BacktestClock.Resolve(null, 15, Portafoglio));

    /// <summary>Il minuto divide 15, 60 e 240: è la combinazione per cui l'opzione esiste.</summary>
    [Fact]
    public void IlMinutoEUnOrologioValidoPerUnPortafoglioNormale()
        => Assert.Equal(1, BacktestClock.Resolve(1, 15, Portafoglio));

    /// <summary>
    /// Un orologio uguale al minimo non è una forzatura: è lo stesso run di prima, e chiederlo
    /// esplicitamente non deve essere un errore.
    /// </summary>
    [Fact]
    public void UnOrologioUgualeAlMinimoEAccettato()
        => Assert.Equal(15, BacktestClock.Resolve(15, 15, Portafoglio));

    /// <summary>
    /// Più lento delle strategie le lascerebbe senza valutazione: il tick del loop non cadrebbe
    /// mai dentro la barra di una 15 minuti.
    /// </summary>
    [Fact]
    public void UnOrologioPiuLentoDelleStrategieNonParte()
    {
        var errore = Assert.Throws<ArgumentException>(
            () => BacktestClock.Resolve(60, 15, Portafoglio));

        Assert.Contains("piu' corto", errore.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Il caso silenzioso, ed è il motivo per cui questa validazione esiste: con un orologio da 4
    /// minuti la 15 non è divisibile e <c>ShouldEvaluateStrategy</c> la salterebbe senza un errore. Il run finirebbe "completo" con meno strategie di quelle chieste, e
    /// l'unico indizio sarebbe l'equity più bassa.
    /// </summary>
    [Fact]
    public void UnOrologioCheNonDivideUnTimeframeNominaLeStrategieCheSalterebbe()
    {
        var errore = Assert.Throws<ArgumentException>(
            () => BacktestClock.Resolve(4, 15, Portafoglio));

        Assert.Contains("PTS_NQ_PCH_001_15 (15m)", errore.Message, StringComparison.Ordinal);
        // 60 e 240 sono multipli di 4 e verrebbero valutate: nominarle sarebbe un errore opposto e
        // altrettanto costoso — si toglierebbe dal masterfilter una strategia che andava bene.
        Assert.DoesNotContain("PTS_NQ_TFM_001_60", errore.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("PTS_ES_VBO_002_240", errore.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void UnOrologioNonPositivoNonEUnTimeframe(int minuti)
        => Assert.Throws<ArgumentException>(() => BacktestClock.Resolve(minuti, 15, Portafoglio));
}
