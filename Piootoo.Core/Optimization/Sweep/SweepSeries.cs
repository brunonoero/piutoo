using Piootoo.Core.Services;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// Le barre di un simbolo, caricate una volta sola e riusate da ogni combinazione della sweep.
///
/// <para><b>Perche' esiste.</b> Una sweep sequenziale alla Unger su un motore solo sono decine di
/// migliaia di run. Passare dal servizio di backtest significherebbe, per ognuno, rileggere il JSON
/// del feed, ricostruire i cursori e scrivere gli artefatti: il feed di @NQ_240 e' 3 MB, quello da
/// un minuto 1,2 GB. Qui il caricamento e la pulizia avvengono una volta e ogni run parte da un
/// array gia' in memoria.</para>
///
/// <para><b>La pulizia e' quella del backtest, non una sua semplificazione</b>: stessi due passaggi
/// e nello stesso ordine — i giorni senza sessione (<see cref="SessionGrid.DropNonSessionDays"/>) e
/// poi le barre fuori dalla finestra di negoziazione (<see cref="SessionMask.DropOutsideWindow"/>,
/// che non tocca la serie da un minuto perche' e' il feed di rischio). Una sweep che ottimizzasse su
/// una serie diversa da quella del backtest troverebbe parametri che il backtest non riproduce, ed
/// e' esattamente l'errore che questo runner esiste per non commettere.</para>
/// </summary>
public sealed class SweepSeries
{
    private readonly Dictionary<int, OhlcvData[]> _byTimeframe = new();

    private SweepSeries(string symbol, DateTime startUtc, DateTime endUtc, string? broker)
    {
        Symbol = symbol;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Broker = broker;
    }

    /// <summary>Simbolo normalizzato, come lo scrivono le strategie.</summary>
    public string Symbol { get; }

    /// <summary>Estremi del run richiesto. Le serie contengono anche il riscaldamento prima di <see cref="StartUtc"/>.</summary>
    public DateTime StartUtc { get; }

    /// <summary>Fine del run richiesto.</summary>
    public DateTime EndUtc { get; }

    /// <summary>Archivio da cui le barre vengono: <c>null</c> = feed interno, altrimenti il broker.</summary>
    public string? Broker { get; }

    /// <summary>I timeframe caricati, in minuti.</summary>
    public IReadOnlyCollection<int> Timeframes => _byTimeframe.Keys;

    /// <summary>Le barre di un timeframe. Vuoto se non e' stato caricato.</summary>
    public OhlcvData[] Bars(int timeframeMinutes) =>
        _byTimeframe.TryGetValue(timeframeMinutes, out var bars) ? bars : [];

    /// <summary>Quante barre sono state scartate dalla pulizia, per timeframe. Serve al resoconto.</summary>
    public Dictionary<int, (int NonSessionDays, int OutsideWindow)> Dropped { get; } = [];

    /// <summary>
    /// Carica i timeframe richiesti per un simbolo. Il <paramref name="warmupDays"/> e' il
    /// riscaldamento prima dell'inizio del run: e' in giorni di <b>calendario</b> e non in barre,
    /// come nel servizio di backtest, perche' i future hanno pause e fine settimana e N barre
    /// coprono molto piu' di N × timeframe minuti.
    /// </summary>
    public static async Task<SweepSeries> LoadAsync(
        IPiootooDataFeedService dataFeed,
        string symbol,
        IEnumerable<int> timeframesMinutes,
        DateTime startUtc,
        DateTime endUtc,
        string? broker = null,
        double warmupDays = 120d)
    {
        var normalized = StrategyKeys.NormalizeSymbol(symbol);
        var series = new SweepSeries(normalized, startUtc, endUtc, broker);
        MarketCalendarRegistry.Current.TryGet(normalized, out var calendar);

        foreach (var timeframe in timeframesMinutes.Distinct().OrderBy(tf => tf))
        {
            var loaded = await dataFeed.GetCandlesRangeAsync(
                symbol, startUtc.AddDays(-Math.Max(30d, warmupDays)), endUtc, timeframe, broker);

            var bars = loaded;
            var nonSession = 0;
            var outsideWindow = 0;
            if (calendar is not null)
            {
                bars = new SessionGrid(calendar).DropNonSessionDays(loaded, out nonSession);
                if (timeframe > 1)
                    bars = new SessionMask(calendar).DropOutsideWindow(bars, timeframe, out outsideWindow);
            }

            series._byTimeframe[timeframe] = bars;
            series.Dropped[timeframe] = (nonSession, outsideWindow);
        }

        return series;
    }
}
