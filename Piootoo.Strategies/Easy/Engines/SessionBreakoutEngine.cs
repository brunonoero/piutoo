using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Motore breakout sugli estremi delle ultime N sessioni.
///
/// <para>A ogni apertura di sessione fissa i livelli <c>hh</c> e <c>ll</c> sugli estremi delle
/// ultime <see cref="Sessions"/> sessioni chiuse e ricalcola l'ADX sui valori di sessione.
/// Durante la sessione, se <see cref="IncludeCurrentSession"/> è attivo, i due livelli si
/// allargano seguendo gli estremi già toccati. Finché la finestra oraria è aperta, l'ADX resta
/// sotto soglia e i gate di pattern passano, riemette a ogni barra uno stop buy su <c>hh</c> e
/// uno stop sell su <c>ll</c>.</para>
///
/// <para>Una direzione si disarma appena entra in posizione (<c>OKL</c>/<c>OKS</c>): un solo
/// ingresso per sessione e per verso, come nell'originale.</para>
///
/// <para>Copre <c>TOP_UA_287</c>; la stessa struttura serve 120, 298 e 736.</para>
/// </summary>
public abstract class SessionBreakoutEngine : EasyEngineBase
{
    // ------------------------------------------------------------------ livelli

    /// <summary>Numero di sessioni chiuse su cui calcolare massimo e minimo (<c>nSess</c>).</summary>
    protected int Sessions = 1;

    /// <summary>
    /// Se true i livelli inglobano anche gli estremi della sessione in corso
    /// (<c>levIncludeSess0</c>): il livello si allarga barra dopo barra.
    /// </summary>
    protected bool IncludeCurrentSession = true;

    // ------------------------------------------------------------------ filtro ADX

    /// <summary>Periodo ADX (<c>ADXLen</c>). 0 disattiva il filtro.</summary>
    protected int AdxLength;

    /// <summary>Soglia massima di ADX oltre la quale non si opera (<c>ADXTH</c>).</summary>
    protected decimal AdxThreshold = 100m;

    // ------------------------------------------------------------------ finestra oraria

    /// <summary>Inizio finestra operativa HHMM (<c>MyStartTime</c>).</summary>
    protected int StartTime;

    /// <summary>Fine finestra operativa HHMM, esclusa, come <c>tw()</c> (<c>MyEndTime</c>).</summary>
    protected int EndTime = 2359;

    /// <summary>Inizio della pausa in cui non si opera (<c>MyStartPause</c>).</summary>
    protected int PauseStart = -1;

    /// <summary>Fine della pausa (<c>MyEndPause</c>).</summary>
    protected int PauseEnd = -1;

    // ------------------------------------------------------------------ gate di pattern

    /// <summary>Pattern neutro richiesto (<c>PtnNeutYes</c>).</summary>
    protected int NeutralYes = 55;

    /// <summary>Secondo pattern neutro richiesto (<c>PtnNeutYes2</c>).</summary>
    protected int NeutralYes2 = 55;

    /// <summary>Pattern neutro che impedisce l'operatività (<c>PtnNeutNo</c>).</summary>
    protected int NeutralNo = 56;

    /// <summary>Pattern direzionale richiesto, con segno applicato al verso (<c>ptnDirYes</c>).</summary>
    protected int DirectionalYes = 52;

    /// <summary>Pattern direzionale che impedisce l'ingresso (<c>ptnDirNo</c>).</summary>
    protected int DirectionalNo = 53;

    /// <summary>Sessione della settimana da saltare per il long (<c>SkipSessL</c>). -1 = nessuna.</summary>
    protected int SkipSessionLong = -1;

    /// <summary>Sessione della settimana da saltare per lo short (<c>SkipSessS</c>).</summary>
    protected int SkipSessionShort = -1;

    // ------------------------------------------------------------------ stato di sessione

    private decimal _hh;
    private decimal _ll;
    private bool _okLong;
    private bool _okShort;
    private int _sessionOfWeek = -1;
    private bool _levelsReady;

    // Stato dell'ADX ricorsivo: i quattro accumulatori di iADXOnArray devono sopravvivere fra le
    // sessioni, altrimenti la media mobile riparte da zero e il filtro è privo di significato.
    private decimal _adxValue;
    private decimal _adx0;
    private decimal _adx1;
    private decimal _adx2;
    private decimal _adx3;

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);

        if (isStartOfSession)
        {
            UpdateAdx(ohlc);
            ResetLevels(ohlc);
            _okLong = true;
            _okShort = true;

            // L'originale sposta di uno l'indice di giornata quando la sessione attraversa la
            // mezzanotte, perché la sessione "di lunedì" comincia domenica sera.
            _sessionOfWeek = SessionStartTime > SessionEndTime
                ? EasyDayOfWeek(barTime) + 1
                : EasyDayOfWeek(barTime);
        }

        if (!_levelsReady)
            return Hold(bar.Close, barTime, "Livelli di sessione non ancora inizializzati");

        if (IncludeCurrentSession)
        {
            _hh = Math.Max(_hh, bar.High);
            _ll = Math.Min(_ll, bar.Low);
        }

        // Una direzione già in posizione resta disarmata fino alla prossima apertura.
        if (CurrentMP == 1) _okLong = false;
        if (CurrentMP == -1) _okShort = false;

        if (!InTradingWindow(barTime) || !PassesNeutralGates(ohlc))
            return Hold(bar.Close, barTime);

        if (AdxLength > 0 && _adxValue >= AdxThreshold)
            return Hold(bar.Close, barTime, "ADX oltre soglia");

        var entries = new List<TradeSignal>(2);

        if (_okLong &&
            EasyLib.PatternDirectionalFast(+DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(+DirectionalNo, ohlc) &&
            _sessionOfWeek != SkipSessionLong)
        {
            entries.Add(EntryStopNextBar(SignalType.Buy, _hh, data, barTime, "LE"));
        }

        if (_okShort &&
            EasyLib.PatternDirectionalFast(-DirectionalYes, ohlc) &&
            !EasyLib.PatternDirectionalFast(-DirectionalNo, ohlc) &&
            _sessionOfWeek != SkipSessionShort)
        {
            entries.Add(EntryStopNextBar(SignalType.Sell, _ll, data, barTime, "SE"));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private void ResetLevels(decimal[] ohlc)
    {
        if (Sessions <= 1)
        {
            _hh = ohlc[5];  // highd1
            _ll = ohlc[6];  // lowd1
        }
        else
        {
            _hh = decimal.MinValue;
            _ll = decimal.MaxValue;
            for (var s = 1; s <= Math.Min(Sessions, 5); s++)
            {
                _hh = Math.Max(_hh, ohlc[1 + s * 4]);
                _ll = Math.Min(_ll, ohlc[2 + s * 4]);
            }
        }

        _levelsReady = _hh > decimal.MinValue && _ll < decimal.MaxValue && _hh > 0m && _ll > 0m;
    }

    private void UpdateAdx(decimal[] ohlc)
    {
        if (AdxLength <= 0) return;

        var calc = new[] { _adx0, _adx1, _adx2, _adx3 };
        _adxValue = EasyLib.iADXOnArray(
            AdxLength,
            ohlc[5], ohlc[6], ohlc[7],      // high/low/close della sessione d1
            ohlc[9], ohlc[10], ohlc[11],    // high/low/close della sessione d2
            ref calc) * 100m;

        _adx0 = calc[0];
        _adx1 = calc[1];
        _adx2 = calc[2];
        _adx3 = calc[3];
    }

    private bool InTradingWindow(DateTime barTime)
    {
        // tw() ha fine esclusiva: replicarla è importante perché la variante inclusiva esiste
        // altrove nella stessa libreria e le due differiscono di una barra sul bordo.
        if (!EasyLib.TimeWindow(StartTime, EndTime, barTime))
            return false;

        if (PauseStart < 0 || PauseEnd < 0)
            return true;

        var t = Hhmm(barTime);
        return t < PauseStart || t > PauseEnd;
    }

    private bool PassesNeutralGates(decimal[] ohlc) =>
        EasyLib.PatternNeutralFast(NeutralYes, ohlc) &&
        EasyLib.PatternNeutralFast(NeutralYes2, ohlc) &&
        !EasyLib.PatternNeutralFast(NeutralNo, ohlc);
}
