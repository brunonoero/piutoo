using Piootoo.Shared.Models;

namespace Piootoo.Core.Services;

/// <summary>
/// Finestra scorrevole su una serie di candele già ordinata cronologicamente.
///
/// Il loop di backtest avanza con un orologio monotono crescente e a ogni barra chiede alla
/// strategia le ultime N candele disponibili. Farlo con
/// <c>Where(...).OrderByDescending(...).Take(n).OrderBy(...)</c> costa O(totale candele) per
/// chiamata, per ogni strategia, per ogni barra: su serie lunghe è il costo dominante
/// dell'intero backtest.
///
/// Questa classe sfrutta due proprietà che valgono sempre nel loop: la serie è ordinata e il
/// tempo richiesto non torna mai indietro. Mantiene quindi un indice che avanza e restituisce
/// una copia degli ultimi N elementi: costo O(N) invece di O(totale), con N = RequiredCandles.
///
/// Non è thread-safe: un cursore appartiene a un singolo job di backtest.
/// </summary>
public sealed class CandleWindowCursor
{
    private static readonly OhlcvData[] EmptyWindow = [];

    private readonly OhlcvData[] _candles;

    /// <summary>Numero di candele con timestamp &lt;= all'ultimo istante richiesto.</summary>
    private int _available;

    /// <summary>Ultimo istante richiesto, per riconoscere una richiesta fuori ordine.</summary>
    private DateTime _lastRequestedUtc = DateTime.MinValue;

    public CandleWindowCursor(OhlcvData[] candles)
    {
        _candles = candles ?? EmptyWindow;
    }

    public int TotalCandles => _candles.Length;

    public DateTime? FirstBarUtc => _candles.Length == 0 ? null : _candles[0].DateTime;

    public DateTime? LastBarUtc => _candles.Length == 0 ? null : _candles[^1].DateTime;

    /// <summary>
    /// Avanza il cursore fino a <paramref name="upToUtc"/> incluso e restituisce le ultime
    /// <paramref name="maxCandles"/> candele disponibili, in ordine cronologico.
    /// Richieste ripetute con lo stesso istante sono idempotenti.
    /// </summary>
    public OhlcvData[] Window(DateTime upToUtc, int maxCandles)
    {
        var available = Advance(upToUtc);
        if (available == 0 || maxCandles <= 0)
            return EmptyWindow;

        var take = Math.Min(maxCandles, available);
        var start = available - take;
        var window = new OhlcvData[take];
        Array.Copy(_candles, start, window, 0, take);
        return window;
    }

    /// <summary>
    /// Ultima candela disponibile a <paramref name="upToUtc"/>, senza allocare la finestra.
    /// Utile per i controlli preliminari (candela stale, prezzo di mark-to-market).
    /// </summary>
    public OhlcvData? LastCandle(DateTime upToUtc)
    {
        var available = Advance(upToUtc);
        return available == 0 ? null : _candles[available - 1];
    }

    /// <summary>Quante candele sono disponibili fino all'istante indicato.</summary>
    public int AvailableAt(DateTime upToUtc) => Advance(upToUtc);

    private int Advance(DateTime upToUtc)
    {
        if (_candles.Length == 0) return 0;

        if (upToUtc < _lastRequestedUtc)
        {
            // Il loop non dovrebbe mai tornare indietro; se succede si riparte da zero invece di
            // restituire una finestra che include il futuro.
            _available = 0;
        }

        _lastRequestedUtc = upToUtc;

        while (_available < _candles.Length && _candles[_available].DateTime <= upToUtc)
            _available++;

        return _available;
    }
}
