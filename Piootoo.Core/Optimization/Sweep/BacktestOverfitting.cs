using System.Numerics;
using Piootoo.Shared.Models;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// Il verdetto della validazione incrociata combinatoria (CSCV) su una griglia di configurazioni.
/// </summary>
/// <param name="Configurations">Le configurazioni messe a confronto.</param>
/// <param name="Periods">I periodi (mesi) della matrice.</param>
/// <param name="Blocks">In quanti blocchi contigui i periodi sono stati tagliati.</param>
/// <param name="Splits">Le partizioni provate: tutte le scelte di meta' dei blocchi come campione.</param>
/// <param name="Probability">
/// La probabilita' di sovra-adattamento (PBO): la quota di partizioni in cui la migliore in campione
/// finisce fuori campione <b>sotto la mediana</b> delle altre. 0,5 e' la selezione che non sceglie
/// niente, sotto 0,2 la selezione porta informazione.
/// </param>
/// <param name="MedianOutOfSamplePercentile">
/// Il percentile mediano che la migliore in campione occupa fuori campione (1 = prima, 0 = ultima).
/// </param>
/// <param name="LossProbability">
/// La quota di partizioni in cui la migliore in campione ha fuori campione uno Sharpe negativo.
/// </param>
public sealed record OverfittingResult(
    int Configurations,
    int Periods,
    int Blocks,
    int Splits,
    double Probability,
    double MedianOutOfSamplePercentile,
    double LossProbability);

/// <summary>
/// La probabilita' di sovra-adattamento di una selezione (Bailey, Borwein, Lopez de Prado e Zhu,
/// 2015), per validazione incrociata combinatoria simmetrica.
///
/// <para><b>Perche'.</b> Lo Sharpe deflazionato (<see cref="DeflatedSharpe"/>) chiede se <i>una</i>
/// configurazione batte il massimo di K prove senza edge, con un modello di rumore gaussiano e
/// indipendente. Il PBO non assume nessun modello: prende la griglia intera, e per ogni modo di
/// dividere la storia in due meta' guarda se la configurazione scelta sulla prima meta' resta
/// sopra la mediana sulla seconda. E' la domanda che la griglia fa davvero — «la migliore qui e'
/// ancora buona li'?» — ripetuta su migliaia di tagli invece che sul solo split dichiarato, che e'
/// un taglio fra tanti e puo' essere stato fortunato.</para>
///
/// <para><b>Il criterio</b> e' lo Sharpe del P&amp;L per periodo (media su deviazione standard dei
/// mesi, zero compresi), non il netto su drawdown della griglia: serve una misura che si sommi per
/// blocchi senza rifare i run. Misura quindi la stabilita' della <i>classifica</i>, non il verdetto
/// della griglia.</para>
///
/// <para><b>Limite.</b> I blocchi sono contigui e il P&amp;L di un mese dipende solo dai trade che
/// in quel mese si chiudono: una posizione a cavallo del confine fra due blocchi porta un po' di
/// informazione dall'uno all'altro, trascurabile per strategie che tengono giorni e non mesi.</para>
/// </summary>
public static class BacktestOverfitting
{
    /// <summary>Blocchi di default, come nell'articolo: C(16, 8) = 12.870 partizioni.</summary>
    public const int DefaultBlocks = 16;

    /// <summary>I periodi minimi per blocco: sotto, lo Sharpe di un blocco e' una misura di tre numeri.</summary>
    public const int MinPeriodsPerBlock = 3;

    /// <summary>
    /// La matrice del P&amp;L mensile di una configurazione: una voce per mese di calendario UTC da
    /// <paramref name="fromUtc"/> a <paramref name="toUtc"/>, ogni trade nel mese in cui si chiude.
    /// I mesi senza trade valgono zero: stare fuori mercato e' un esito della configurazione.
    /// </summary>
    public static double[] MonthlyProfit(IEnumerable<TradingResult> trades, DateTime fromUtc, DateTime toUtc)
    {
        var months = MonthCount(fromUtc, toUtc);
        var profit = new double[months];
        foreach (var trade in trades)
        {
            var index = MonthIndex(fromUtc, trade.ExitDate);
            if (index >= 0 && index < months)
                profit[index] += (double)trade.NetProfit;
        }

        return profit;
    }

    /// <summary>I mesi di calendario che il periodo tocca, estremi compresi.</summary>
    public static int MonthCount(DateTime fromUtc, DateTime toUtc) => Math.Max(0, MonthIndex(fromUtc, toUtc) + 1);

    /// <summary>Il primo giorno del mese <paramref name="index"/> contato da quello di <paramref name="fromUtc"/>.</summary>
    public static DateTime MonthStart(DateTime fromUtc, int index) =>
        new DateTime(fromUtc.Year, fromUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(index);

    private static int MonthIndex(DateTime fromUtc, DateTime atUtc) =>
        (atUtc.Year - fromUtc.Year) * 12 + atUtc.Month - fromUtc.Month;

    /// <summary>
    /// Il PBO di una griglia. <paramref name="profits"/> ha una riga per configurazione e, in ogni
    /// riga, il P&amp;L di ogni periodo nello stesso ordine. <c>null</c> se le configurazioni sono
    /// meno di due o i periodi non bastano per quattro blocchi: meglio nessun numero che un numero
    /// fatto su due mesi per meta'.
    /// </summary>
    public static OverfittingResult? Evaluate(IReadOnlyList<double[]> profits, int blocks = DefaultBlocks)
    {
        var configurations = profits.Count;
        if (configurations < 2) return null;

        var periods = profits[0].Length;
        if (profits.Any(row => row.Length != periods))
            throw new ArgumentException("ogni configurazione deve avere lo stesso numero di periodi.", nameof(profits));

        // Pari, e non piu' di quanti ne consentono i periodi minimi per blocco.
        blocks = Math.Min(blocks, periods / MinPeriodsPerBlock);
        blocks -= blocks % 2;
        if (blocks < 4) return null;

        // Somma, somma dei quadrati e numero di periodi per configurazione e blocco: lo Sharpe di
        // un'unione di blocchi si ricompone da questi tre senza ripassare i mesi.
        var sums = new double[configurations, blocks];
        var squares = new double[configurations, blocks];
        var counts = new int[blocks];
        for (var period = 0; period < periods; period++)
        {
            var block = (int)((long)period * blocks / periods);
            counts[block]++;
            for (var configuration = 0; configuration < configurations; configuration++)
            {
                var value = profits[configuration][period];
                sums[configuration, block] += value;
                squares[configuration, block] += value * value;
            }
        }

        var half = blocks / 2;
        var logits = new List<double>();
        var percentiles = new List<double>();
        var losses = 0;
        var inSample = new double[configurations];
        var outOfSample = new double[configurations];

        for (var mask = 0; mask < 1 << blocks; mask++)
        {
            if (BitOperations.PopCount((uint)mask) != half) continue;

            for (var configuration = 0; configuration < configurations; configuration++)
            {
                inSample[configuration] = Sharpe(sums, squares, counts, configuration, mask, inside: true);
                outOfSample[configuration] = Sharpe(sums, squares, counts, configuration, mask, inside: false);
            }

            var best = 0;
            for (var configuration = 1; configuration < configurations; configuration++)
                if (inSample[configuration] > inSample[best]) best = configuration;

            // Rango relativo fuori campione, con i pari a meta': (sotto + pari/2 + 1/2) / N sta in (0, 1)
            // e il logit resta finito anche per la prima e l'ultima.
            var below = 0;
            var ties = 0;
            for (var configuration = 0; configuration < configurations; configuration++)
            {
                if (configuration == best) continue;
                if (outOfSample[configuration] < outOfSample[best]) below++;
                else if (outOfSample[configuration] == outOfSample[best]) ties++;
            }

            var omega = (below + ties / 2d + 0.5d) / configurations;
            logits.Add(Math.Log(omega / (1d - omega)));
            percentiles.Add(omega);
            if (outOfSample[best] < 0d) losses++;
        }

        var splits = logits.Count;
        return new OverfittingResult(
            configurations,
            periods,
            blocks,
            splits,
            logits.Count(logit => logit <= 0d) / (double)splits,
            Median(percentiles),
            losses / (double)splits);
    }

    private static double Sharpe(double[,] sums, double[,] squares, int[] counts, int configuration, int mask, bool inside)
    {
        double sum = 0d, square = 0d;
        var count = 0;
        for (var block = 0; block < counts.Length; block++)
        {
            if (((mask >> block) & 1) == 1 != inside) continue;
            sum += sums[configuration, block];
            square += squares[configuration, block];
            count += counts[block];
        }

        if (count < 2) return 0d;
        var mean = sum / count;
        var variance = (square - count * mean * mean) / (count - 1);
        return variance > 1e-12 ? mean / Math.Sqrt(variance) : 0d;
    }

    private static double Median(List<double> values)
    {
        values.Sort();
        var middle = values.Count / 2;
        return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2d;
    }
}
