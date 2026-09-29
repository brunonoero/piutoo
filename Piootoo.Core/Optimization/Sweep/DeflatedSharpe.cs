using Piootoo.Shared.Models;

namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// I momenti dei trade di una configurazione, per trade e non annualizzati: lo Sharpe per trade e'
/// la media divisa la deviazione standard del netto di ogni trade chiuso.
/// </summary>
public sealed record TradeMoments(int Trades, double Mean, double StdDev, double Skewness, double Kurtosis)
{
    public double Sharpe => StdDev > 0d ? Mean / StdDev : 0d;

    public static TradeMoments Of(IReadOnlyList<TradingResult> trades)
    {
        var count = trades.Count;
        if (count < 2)
            return new TradeMoments(count, count == 1 ? (double)trades[0].NetProfit : 0d, 0d, 0d, 3d);

        var values = new double[count];
        var sum = 0d;
        for (var index = 0; index < count; index++)
        {
            values[index] = (double)trades[index].NetProfit;
            sum += values[index];
        }

        var mean = sum / count;
        double m2 = 0d, m3 = 0d, m4 = 0d;
        foreach (var value in values)
        {
            var d = value - mean;
            var d2 = d * d;
            m2 += d2;
            m3 += d2 * d;
            m4 += d2 * d2;
        }

        m2 /= count;
        m3 /= count;
        m4 /= count;
        var stdDev = Math.Sqrt(m2 * count / (count - 1));
        var skewness = m2 > 0d ? m3 / Math.Pow(m2, 1.5) : 0d;
        var kurtosis = m2 > 0d ? m4 / (m2 * m2) : 3d;
        return new TradeMoments(count, mean, stdDev, skewness, kurtosis);
    }
}

/// <summary>
/// Il verdetto dello Sharpe deflazionato su una configurazione.
/// </summary>
/// <param name="Trials">Le prove fra cui la configurazione e' stata scelta.</param>
/// <param name="Sharpe">Lo Sharpe per trade misurato.</param>
/// <param name="NoiseSharpe">
/// Lo Sharpe per trade che il miglior risultato di <paramref name="Trials"/> prove raggiunge in media
/// con il solo rumore, su questo numero di trade.
/// </param>
/// <param name="NoiseAverageTrade">
/// La stessa soglia in denaro: l'average trade che il rumore regala al migliore, con la dispersione
/// dei trade di questa configurazione. E' il numero da mettere accanto all'average trade misurato.
/// </param>
/// <param name="Probability">
/// La probabilita' che lo Sharpe vero sia sopra quello del rumore: sotto 0,95 la configurazione non
/// si distingue dal massimo di tante prove senza edge.
/// </param>
public sealed record DeflatedSharpeResult(
    long Trials,
    int Trades,
    double Sharpe,
    double NoiseSharpe,
    decimal AverageTrade,
    decimal NoiseAverageTrade,
    double Probability)
{
    public const double Confidence = 0.95;

    public bool Distinguishable => Probability >= Confidence;
}

/// <summary>
/// Lo Sharpe deflazionato (Bailey e Lopez de Prado, 2014): quanto il migliore di K prove supera cio'
/// che il migliore di K prove farebbe senza nessun edge.
///
/// <para><b>Perche'.</b> Una sweep prova migliaia di combinazioni e tiene la migliore. Il massimo di
/// K misure rumorose sta sopra il valore vero di circa sqrt(2 ln K) deviazioni standard, e la
/// deviazione standard dello Sharpe stimato su T trade e' circa 1/sqrt(T): con 2.000 prove e 270
/// trade il rumore da solo regala uno Sharpe per trade di 0,2. Un profit factor minimo fisso non lo
/// vede, perche' vale uguale dopo 10 prove o dopo 100.000.</para>
///
/// <para><b>Le prove si contano grezze</b>: ogni run della ricerca, di ogni fase e dell'ablation. Le
/// prove di una sweep a fasi sono correlate fra loro, quindi il numero effettivo e' piu' basso e la
/// soglia che ne esce e' <b>prudente</b>, mai ottimista. Il riferimento di rumore e' quello di uno
/// Sharpe vero nullo (varianza dello stimatore 1/(T-1)), non la dispersione osservata fra le prove,
/// che su una sweep mescola configurazioni da 30 e da 3.000 trade.</para>
/// </summary>
public static class DeflatedSharpe
{
    private const double EulerGamma = 0.5772156649015329;

    /// <summary>Il valore atteso del massimo di K normali standard indipendenti. Zero per K = 1.</summary>
    public static double ExpectedMaxOfNormals(long trials)
    {
        if (trials <= 1) return 0d;
        return (1d - EulerGamma) * NormalQuantile(1d - 1d / trials) +
               EulerGamma * NormalQuantile(1d - 1d / (trials * Math.E));
    }

    public static DeflatedSharpeResult Evaluate(IReadOnlyList<TradingResult> trades, long trials) =>
        Evaluate(TradeMoments.Of(trades), trials);

    public static DeflatedSharpeResult Evaluate(TradeMoments moments, long trials)
    {
        trials = Math.Max(1, trials);
        if (moments.Trades < 3 || moments.StdDev <= 0d)
            return new DeflatedSharpeResult(trials, moments.Trades, moments.Sharpe, double.NaN,
                (decimal)moments.Mean, 0m, 0d);

        var degrees = moments.Trades - 1d;
        var noise = ExpectedMaxOfNormals(trials) / Math.Sqrt(degrees);
        var sharpe = moments.Sharpe;

        // Code grasse e asimmetria allargano l'errore dello Sharpe stimato: e' il termine che
        // distingue un edge fatto di tanti trade simili da uno fatto di pochi colpi grossi.
        var spread = 1d - moments.Skewness * sharpe + (moments.Kurtosis - 1d) / 4d * sharpe * sharpe;
        if (spread <= 0d) spread = 1d;

        var probability = NormalCdf((sharpe - noise) * Math.Sqrt(degrees) / Math.Sqrt(spread));
        return new DeflatedSharpeResult(
            trials, moments.Trades, sharpe, noise,
            (decimal)moments.Mean, (decimal)(noise * moments.StdDev), probability);
    }

    /// <summary>Funzione di ripartizione della normale standard (Abramowitz-Stegun 7.1.26, errore sotto 1,5e-7).</summary>
    public static double NormalCdf(double x)
    {
        var z = Math.Abs(x) / Math.Sqrt(2d);
        var t = 1d / (1d + 0.3275911 * z);
        var erf = 1d - ((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t *
            Math.Exp(-z * z);
        return x >= 0d ? 0.5 * (1d + erf) : 0.5 * (1d - erf);
    }

    /// <summary>Quantile della normale standard (algoritmo di Acklam, errore relativo sotto 1,2e-9).</summary>
    public static double NormalQuantile(double p)
    {
        if (p <= 0d) return double.NegativeInfinity;
        if (p >= 1d) return double.PositiveInfinity;

        double[] a = [-3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02, 1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00];
        double[] b = [-5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02, 6.680131188771972e+01, -1.328068155288572e+01];
        double[] c = [-7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00, -2.549732539343734e+00, 4.374664141464968e+00, 2.938163982698783e+00];
        double[] d = [7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00, 3.754408661907416e+00];
        const double low = 0.02425;

        if (p < low)
        {
            var q = Math.Sqrt(-2d * Math.Log(p));
            return (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                   ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1d);
        }

        if (p > 1d - low)
        {
            var q = Math.Sqrt(-2d * Math.Log(1d - p));
            return -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                   ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1d);
        }

        var r = p - 0.5;
        var s = r * r;
        return (((((a[0] * s + a[1]) * s + a[2]) * s + a[3]) * s + a[4]) * s + a[5]) * r /
               (((((b[0] * s + b[1]) * s + b[2]) * s + b[3]) * s + b[4]) * s + 1d);
    }
}
