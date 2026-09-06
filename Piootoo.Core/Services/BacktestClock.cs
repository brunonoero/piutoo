namespace Piootoo.Core.Services;

/// <summary>
/// L'orologio del loop di backtesting: su quale barra il motore avanza, e quindi da quale barra
/// esce il prezzo di riempimento.
///
/// <para><b>Perche' e' una convenzione di fill e non un dettaglio di prestazione.</b> Il motore
/// riempie sulla barra che sta in <c>currentBars</c>, cioe' la piu' fitta caricata per quel
/// simbolo. Su una barra da sessanta minuti che contiene sia lo stop protettivo sia il target,
/// quale dei due scatti prima non e' un dato: e' la convenzione <c>ProtectiveBeforeTarget</c>,
/// dichiarata nel summary proprio perche' e' una scelta. Con un orologio a un minuto quella
/// domanda ha una risposta misurata, e con essa i trigger dei pending e il mark-to-market.</para>
///
/// <para>Sta fuori dal servizio perche' la regola e' di dominio e va verificabile da sola: le tre
/// condizioni sotto sono tutte casi che, passando, darebbero un run apparentemente normale e
/// silenziosamente sbagliato.</para>
/// </summary>
public static class BacktestClock
{
    /// <summary>
    /// Il timeframe dell'orologio. <paramref name="requested"/> null = il minimo fra le strategie,
    /// che e' il comportamento di sempre.
    /// </summary>
    /// <param name="strategies">Nome e timeframe di ogni strategia del run, per nominare chi rompe la regola.</param>
    /// <exception cref="ArgumentException">
    /// L'orologio non e' positivo; e' piu' lento del timeframe piu' corto del run; oppure non
    /// divide il timeframe di qualche strategia.
    /// </exception>
    public static int Resolve(
        int? requested,
        int strategyMinTimeframe,
        IEnumerable<(string Name, int TimeframeMinutes)> strategies)
    {
        if (requested is not { } clock)
            return strategyMinTimeframe;

        if (clock <= 0)
        {
            throw new ArgumentException(
                $"L'orologio del backtest dev'essere un numero di minuti positivo: ricevuto {clock}.",
                nameof(requested));
        }

        // Un orologio piu' lento delle strategie non le farebbe mai valutare: ShouldEvaluateStrategy
        // confronta il timeframe della strategia con il tick del loop, e un tick piu' largo non cade
        // mai dentro la barra della strategia.
        if (clock > strategyMinTimeframe)
        {
            throw new ArgumentException(
                $"L'orologio del backtest ({clock} minuti) e' piu' lungo del timeframe piu' corto del " +
                $"run ({strategyMinTimeframe} minuti): le strategie piu' fitte non verrebbero mai valutate.",
                nameof(requested));
        }

        // Una strategia il cui timeframe non e' multiplo del tick viene saltata in silenzio
        // (ShouldEvaluateStrategy esce su 'tf % min != 0') e il run gira senza di lei senza un
        // errore. E' il caso peggiore: un backtest che sembra completo e ha meno strategie.
        var indivisibili = strategies
            .Where(strategy => strategy.TimeframeMinutes % clock != 0)
            .Select(strategy => $"{strategy.Name} ({strategy.TimeframeMinutes}m)")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (indivisibili.Count > 0)
        {
            throw new ArgumentException(
                $"L'orologio del backtest ({clock} minuti) non divide il timeframe di " +
                $"{indivisibili.Count} strategie: {string.Join(", ", indivisibili.Take(10))}" +
                (indivisibili.Count > 10 ? ", …" : string.Empty) +
                ". Quelle strategie non verrebbero mai valutate e il run girerebbe senza dirlo.",
                nameof(requested));
        }

        return clock;
    }
}
