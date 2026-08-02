using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Libreria di funzioni helper per convertire funzioni EasyLanguage in C#
/// 
/// GUIDA ALLA CONVERSIONE:
/// =======================
/// 
/// 1. INPUT/VARIABLES:
///    - input: MySize(1) → private int _mySize = 1;
///    - var: OkLong(true) → private bool _okLong = true;
/// 
/// 2. FUNZIONI BUILT-IN:
///    - HighestFC(h, n) → Highest(data, n, d => d.High)
///    - LowestFC(l, n) → Lowest(data, n, d => d.Low)
///    - openD(0) → GetDailyOpen(data, 0)
///    - highD(0) → GetDailyHigh(data, 0)
///    - lowD(0) → GetDailyLow(data, 0)
///    - closeD(0) → GetDailyClose(data, 0)
///    - dayofweek(date) → currentDate.DayOfWeek
///    - time → currentDate.Hour * 100 + currentDate.Minute (formato HHMM)
///    - date → currentDate.Date
/// 
/// 3. TRADE SIGNALS:
///    - buy MySize contracts → return new TradeSignal { Type = SignalType.Buy, Quantity = _mySize }
///    - sellshort MySize contracts → return new TradeSignal { Type = SignalType.Sell, Quantity = _mySize }
///    - sell/buytocover → return new TradeSignal { Type = SignalType.Sell/Buy } (per chiudere posizione opposta)
///    - setstoploss/setprofittarget → NON gestito qui, sarà gestito dal TradingService
/// 
/// 4. CONDIZIONI:
///    - if MP = +1 → if (currentPosition == PositionType.Long)
///    - if MP = -1 → if (currentPosition == PositionType.Short)
///    - if MP = 0 → if (currentPosition == PositionType.None)
/// 
/// 5. ARRAY E FUNZIONI:
///    - array: ohlcValues[23](0) → decimal[] ohlcValues = new decimal[24]
///    - Funzioni f_* → Metodi statici in EasyLib
/// </summary>
public static class EasyLib
{
    /// <summary>
    /// Calcola OHLC per le ultime 5 sessioni più la sessione corrente,
    /// allineato a f__OHLCMulti5 (isBarTimeEndTime = true).
    /// Restituisce true se la barra corrente è l'inizio di una nuova sessione.
    /// </summary>
    public static bool OHLCMulti5(int sessionStartTime, int sessionEndTime, OhlcvData[] data, DateTime currentDate, out decimal[] ohlcValues)
    {
        ohlcValues = new decimal[24];

        var bars = data
            .Where(d => d.DateTime <= currentDate)
            .OrderBy(d => d.DateTime)
            .ToArray();

        if (bars.Length == 0)
            return false;

        bool oneDaySession = sessionStartTime < sessionEndTime;
        decimal actO = 0, actH = 0, actL = 0, actC = 0;
        int actDayIdx = 0;
        var pastOpen = new decimal[20];
        var pastHigh = new decimal[20];
        var pastLow = new decimal[20];
        var pastClose = new decimal[20];
        bool initialized = false;
        bool lastIsStartOfSession = false;

        for (int i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];
            int t = GetHhmm(bar.DateTime);
            int prevT = i > 0 ? GetHhmm(bars[i - 1].DateTime) : t;
            var day = bar.DateTime.Date;
            var prevDay = i > 0 ? bars[i - 1].DateTime.Date : day;

            // isBarTimeEndTime = true (default EasyLanguage)
            bool timeStarted = t > sessionStartTime;
            bool timeNotEnded = t <= sessionEndTime;
            bool prevTimeLessSTime = prevT <= sessionStartTime;

            bool inSessionTime = oneDaySession
                ? timeStarted && timeNotEnded
                : timeStarted || timeNotEnded;

            bool isStartOfSession = inSessionTime && timeStarted && prevTimeLessSTime;

            if (!oneDaySession)
            {
                // sessione overnight: giorno nuovo e ancora prima dello start
                isStartOfSession = isStartOfSession || (inSessionTime && day != prevDay && prevTimeLessSTime);
                // split se manca un giorno di calendario
                isStartOfSession = isStartOfSession || (inSessionTime && day > prevDay.AddDays(1));
            }
            else
            {
                isStartOfSession = isStartOfSession || (inSessionTime && day != prevDay);
            }

            if (!initialized)
            {
                actO = bar.Open;
                actH = bar.High;
                actL = bar.Low;
                actC = bar.Close;
                initialized = true;
            }

            pastOpen[actDayIdx] = actO;
            pastHigh[actDayIdx] = actH;
            pastLow[actDayIdx] = actL;
            pastClose[actDayIdx] = actC;

            if (inSessionTime)
            {
                actL = Math.Min(actL, bar.Low);
                actH = Math.Max(actH, bar.High);
                actC = bar.Close;
            }

            if (isStartOfSession)
            {
                actO = bar.Open;
                actH = bar.High;
                actL = bar.Low;
                actC = bar.Close;
                actDayIdx = (actDayIdx + 1) % 20;
            }

            lastIsStartOfSession = isStartOfSession;
        }

        ohlcValues[0] = actO;
        ohlcValues[1] = actH;
        ohlcValues[2] = actL;
        ohlcValues[3] = actC;

        for (int dayRef = 1; dayRef <= 5; dayRef++)
        {
            int retIdx = (20 + actDayIdx - dayRef) % 20;
            int baseIdx = dayRef * 4;
            ohlcValues[baseIdx] = pastOpen[retIdx];
            ohlcValues[baseIdx + 1] = pastHigh[retIdx];
            ohlcValues[baseIdx + 2] = pastLow[retIdx];
            ohlcValues[baseIdx + 3] = pastClose[retIdx];
        }

        return lastIsStartOfSession;
    }

    /// <summary>
    /// Aggrega la serie intraday nella serie di sessione completa, cioè il <c>data2</c> giornaliero
    /// che in EasyLanguage accompagna un grafico intraday (i sorgenti <c>..._1440_...</c>).
    ///
    /// <para>I confini di sessione sono gli stessi di <see cref="OHLCMulti5"/>: questa funzione
    /// estende quella logica dai soli d0..d5 a tutto lo storico disponibile, perché un ADX o un ATR
    /// su data2 hanno bisogno di molte più sessioni di sei.</para>
    ///
    /// <para><b>L'ultima barra è la sessione in corso, ancora in formazione.</b> È la semantica di
    /// TradeStation — su una barra intraday <c>c data2</c> vale la chiusura corrente del giorno che
    /// si sta formando, non quella dell'ultimo giorno chiuso — ed è la stessa convenzione di d0 in
    /// <see cref="OHLCMulti5"/>. Chi vuole solo sessioni chiuse deve scartare l'ultimo elemento,
    /// come fa l'originale quando scrive <c>[1] of data2</c>.</para>
    ///
    /// <para><b>La prima barra può essere troncata</b>, perché la finestra ricevuta comincia quasi
    /// sempre a metà di una sessione. Va tenuto presente nel dimensionare <c>RequiredCandles</c>:
    /// serve una sessione di margine oltre a quelle che l'indicatore consuma.</para>
    ///
    /// <para>Ogni barra aggregata è marcata con l'orario dell'<i>ultima</i> barra intraday che la
    /// compone, coerente con <c>isBarTimeEndTime = true</c>.</para>
    /// </summary>
    public static OhlcvData[] BuildSessionSeries(
        int sessionStartTime, int sessionEndTime, OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length == 0)
            return [];

        var (bars, count) = WindowUpTo(data, currentDate);
        if (count == 0)
            return [];

        var sessions = new List<OhlcvData>();
        OhlcvData? current = null;

        foreach (var (bar, startsNewSession) in InSessionBars(sessionStartTime, sessionEndTime, bars, count))
        {
            if (current is null || startsNewSession)
            {
                if (current is not null)
                    sessions.Add(current);

                current = new OhlcvData
                {
                    DateTime = bar.DateTime,
                    Open = bar.Open, High = bar.High, Low = bar.Low, Close = bar.Close,
                    Volume = bar.Volume
                };
                continue;
            }

            current.DateTime = bar.DateTime;
            current.High = Math.Max(current.High, bar.High);
            current.Low = Math.Min(current.Low, bar.Low);
            current.Close = bar.Close;
            current.Volume += bar.Volume;
        }

        if (current is not null)
            sessions.Add(current);

        return sessions.ToArray();
    }

    /// <summary>
    /// Ultima barra della sessione più recente fra quelle <b>concluse</b>, cioè il valore che in
    /// EasyLanguage si legge dopo un latch su <c>sessionlastbar</c>.
    ///
    /// <para>Serve alle sorgenti che scrivono <c>if sessionlastbar data2 then flag = c data2 &gt; o
    /// data2</c>: quel flag viene aggiornato una volta sola, alla chiusura della sessione, e resta
    /// valido per tutta la sessione seguente. Confrontare invece la barra precedente a ogni barra —
    /// come è facile fare tradendo l'originale — cambia la condizione di ingresso, perché il flag
    /// cambierebbe più volte al giorno.</para>
    ///
    /// <para>Restituisce <c>null</c> se nella finestra non c'è nemmeno una sessione conclusa.</para>
    /// </summary>
    public static OhlcvData? LastBarOfPreviousSession(
        int sessionStartTime, int sessionEndTime, OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length == 0)
            return null;

        var (bars, count) = WindowUpTo(data, currentDate);
        OhlcvData? lastInSession = null;
        OhlcvData? previousSessionLastBar = null;

        foreach (var (bar, startsNewSession) in InSessionBars(sessionStartTime, sessionEndTime, bars, count))
        {
            if (startsNewSession && lastInSession is not null)
                previousSessionLastBar = lastInSession;

            lastInSession = bar;
        }

        return previousSessionLastBar;
    }

    /// <summary>
    /// Scorre le barre in sessione segnalando quali aprono una sessione nuova.
    ///
    /// <para>I confronti con la barra precedente usano la barra che precede nella serie
    /// <i>completa</i>, anche se fuori sessione: è così che ragiona <see cref="OHLCMulti5"/>, e le
    /// tre funzioni devono segmentare in modo identico o d0..d5 e le serie derivate finirebbero per
    /// non parlare più della stessa sessione. La prima barra utile non viene mai segnalata come
    /// inizio: apre la sessione troncata da cui comincia la finestra.</para>
    /// </summary>
    private static IEnumerable<(OhlcvData Bar, bool StartsNewSession)> InSessionBars(
        int sessionStartTime, int sessionEndTime, OhlcvData[] bars, int count)
    {
        var oneDaySession = sessionStartTime < sessionEndTime;
        var first = true;

        for (var i = 0; i < count; i++)
        {
            var bar = bars[i];
            var t = GetHhmm(bar.DateTime);
            var prevT = i > 0 ? GetHhmm(bars[i - 1].DateTime) : t;
            var day = bar.DateTime.Date;
            var prevDay = i > 0 ? bars[i - 1].DateTime.Date : day;

            var timeStarted = t > sessionStartTime;
            var timeNotEnded = t <= sessionEndTime;
            var prevTimeLessSTime = prevT <= sessionStartTime;

            var inSessionTime = oneDaySession
                ? timeStarted && timeNotEnded
                : timeStarted || timeNotEnded;

            if (!inSessionTime)
                continue;

            var isStartOfSession = timeStarted && prevTimeLessSTime;
            isStartOfSession = oneDaySession
                ? isStartOfSession || day != prevDay
                : isStartOfSession ||
                  (day != prevDay && prevTimeLessSTime) ||
                  day > prevDay.AddDays(1);

            yield return (bar, !first && isStartOfSession);
            first = false;
        }
    }

    /// <summary>
    /// Finestra da segmentare, fino a <paramref name="currentDate"/> incluso.
    ///
    /// <para>Queste funzioni girano a ogni barra per ogni strategia, su finestre che possono contare
    /// migliaia di elementi: nel caso normale — serie già ordinata, come la consegna
    /// <c>CandleWindowCursor</c> — si restituisce l'array originale con la sola lunghezza utile,
    /// senza copiarlo né riordinarlo. L'ordinamento difensivo di <see cref="OHLCMulti5"/> resta come
    /// ripiego per una serie fuori ordine, così le tre funzioni continuano a segmentare allo stesso
    /// modo qualunque cosa riceva la prima.</para>
    /// </summary>
    private static (OhlcvData[] Bars, int Count) WindowUpTo(OhlcvData[] data, DateTime currentDate)
    {
        var count = 0;
        for (var i = 0; i < data.Length; i++)
        {
            if (i > 0 && data[i].DateTime < data[i - 1].DateTime)
            {
                var reordered = data
                    .Where(d => d.DateTime <= currentDate)
                    .OrderBy(d => d.DateTime)
                    .ToArray();
                return (reordered, reordered.Length);
            }

            if (data[i].DateTime <= currentDate)
                count = i + 1;
        }

        return (data, count);
    }

    public static int GetHhmm(DateTime dateTime) => dateTime.Hour * 100 + dateTime.Minute;

    /// <summary>Stima l'inizio della barra successiva dal timeframe dei dati.</summary>
    public static DateTime EstimateNextBarUtc(OhlcvData[] data, DateTime currentDate)
    {
        int minutes = GetTimeframeMinutes(data);
        return currentDate.AddMinutes(minutes);
    }

    /// <summary>Costruisce un DateTime UTC con orario HHMM sullo stesso giorno di riferimento.</summary>
    public static DateTime CombineDateAndHhmm(DateTime date, int hhmm)
    {
        int hour = hhmm / 100;
        int minute = hhmm % 100;
        return new DateTime(date.Year, date.Month, date.Day, hour, minute, 0, DateTimeKind.Utc);
    }
    
    /// <summary>
    /// Pattern Neutral Fast - determina se un pattern neutral è presente
    /// </summary>
    public static bool PatternNeutralFast(int numeroPattern, decimal[] ohlcValues)
    {
        if (ohlcValues == null || ohlcValues.Length < 24)
            return false;
        
        // Estrai valori OHLC dai giorni
        decimal opend0 = ohlcValues[0], highd0 = ohlcValues[1], lowd0 = ohlcValues[2], closed0 = ohlcValues[3];
        decimal opend1 = ohlcValues[4], highd1 = ohlcValues[5], lowd1 = ohlcValues[6], closed1 = ohlcValues[7];
        decimal opend2 = ohlcValues[8], highd2 = ohlcValues[9], lowd2 = ohlcValues[10], closed2 = ohlcValues[11];
        decimal opend3 = ohlcValues[12], highd3 = ohlcValues[13], lowd3 = ohlcValues[14], closed3 = ohlcValues[15];
        decimal opend4 = ohlcValues[16], highd4 = ohlcValues[17], lowd4 = ohlcValues[18], closed4 = ohlcValues[19];
        decimal opend5 = ohlcValues[20], highd5 = ohlcValues[21], lowd5 = ohlcValues[22], closed5 = ohlcValues[23];
        
        decimal body1d = Math.Abs(opend1 - closed1);
        decimal range1d = highd1 - lowd1;
        decimal body5d = Math.Abs(opend5 - closed1);
        decimal range5d = Math.Max(Math.Max(Math.Max(Math.Max(highd1, highd2), highd3), highd4), highd5) - 
                          Math.Min(Math.Min(Math.Min(Math.Min(lowd1, lowd2), lowd3), lowd4), lowd5);
        
        return numeroPattern switch
        {
            1 => body1d < 0.1m * range1d,
            2 => body1d < 0.25m * range1d,
            3 => body1d < 0.5m * range1d,
            4 => body1d < 0.75m * range1d,
            5 => body1d > 0.25m * range1d,
            6 => body1d > 0.5m * range1d,
            7 => body1d > 0.75m * range1d,
            8 => body1d > 0.9m * range1d,
            9 => body5d < 0.1m * (highd5 - lowd1),
            10 => body5d < 0.25m * (highd5 - lowd1),
            11 => body5d < 0.5m * (highd5 - lowd1),
            12 => body5d < 0.75m * (highd5 - lowd1),
            13 => body5d < 1m * (highd5 - lowd1),
            14 => body5d < 1.5m * (highd5 - lowd1),
            15 => body5d < 2m * (highd5 - lowd1),
            16 => body5d > 0.25m * (highd5 - lowd1),
            17 => body5d > 0.5m * (highd5 - lowd1),
            18 => body5d > 0.75m * (highd5 - lowd1),
            19 => body5d > 1m * (highd5 - lowd1),
            20 => body5d > 1.5m * (highd5 - lowd1),
            21 => body5d > 2m * (highd5 - lowd1),
            22 => body5d > 2.5m * (highd5 - lowd1),
            23 => body5d < 0.1m * range5d,
            24 => body5d < 0.25m * range5d,
            25 => body5d < 0.5m * range5d,
            26 => body5d < 0.75m * range5d,
            27 => body5d > 0.9m * range5d,
            28 => body5d > 0.25m * range5d,
            29 => body5d > 0.5m * range5d,
            30 => body5d > 0.75m * range5d,
            31 => highd0 > (lowd0 + lowd0 * 0.5m * 0.01m),
            32 => highd0 > (lowd0 + lowd0 * 0.75m * 0.01m),
            33 => highd0 > (lowd0 + lowd0 * 1m * 0.01m),
            34 => highd0 > (lowd0 + lowd0 * 1.5m * 0.01m),
            35 => highd0 > (lowd0 + lowd0 * 2m * 0.01m),
            36 => highd0 > (lowd0 + lowd0 * 2.5m * 0.01m),
            37 => highd0 > (lowd0 + lowd0 * 3m * 0.01m),
            38 => highd0 < (lowd0 + lowd0 * 0.5m * 0.01m),
            39 => highd0 < (lowd0 + lowd0 * 0.75m * 0.01m),
            40 => highd0 < (lowd0 + lowd0 * 1m * 0.01m),
            41 => highd0 < (lowd0 + lowd0 * 1.5m * 0.01m),
            42 => highd0 < (lowd0 + lowd0 * 2m * 0.01m),
            43 => highd0 < (lowd0 + lowd0 * 2.5m * 0.01m),
            44 => highd0 < (lowd0 + lowd0 * 3m * 0.01m),
            45 => opend0 < lowd1 || opend0 > highd1,
            46 => highd0 < highd1 && lowd0 > lowd1,
            47 => range1d < (((highd2 - lowd2) + (highd3 - lowd3)) / 3m),
            48 => range1d < (highd2 - lowd2) && (highd2 - lowd2) < (highd3 - lowd3),
            49 => highd1 < highd2 && lowd1 > lowd2,
            50 => highd1 < highd2 || lowd1 > lowd2,
            51 => highd1 > highd2 && lowd1 < lowd2,
            52 => highd0 > highd1 && lowd0 < lowd1,
            53 => range1d < (highd2 - lowd2),
            54 => range1d > (highd2 - lowd2),
            55 => true,
            _ => false
        };
    }
    
    /// <summary>
    /// PtnBaseSA2 - Pattern Base per strategie
    /// </summary>
    public static bool PtnBaseSA2(int numeroPattern, decimal[] ohlcValues)
    {
        if (ohlcValues == null || ohlcValues.Length < 24)
            return false;
        
        // Estrai valori OHLC
        decimal opend0 = ohlcValues[0], highd0 = ohlcValues[1], lowd0 = ohlcValues[2], closed0 = ohlcValues[3];
        decimal opend1 = ohlcValues[4], highd1 = ohlcValues[5], lowd1 = ohlcValues[6], closed1 = ohlcValues[7];
        decimal opend2 = ohlcValues[8], highd2 = ohlcValues[9], lowd2 = ohlcValues[10], closed2 = ohlcValues[11];
        decimal opend3 = ohlcValues[12], highd3 = ohlcValues[13], lowd3 = ohlcValues[14], closed3 = ohlcValues[15];
        decimal opend4 = ohlcValues[16], highd4 = ohlcValues[17], lowd4 = ohlcValues[18], closed4 = ohlcValues[19];
        decimal opend5 = ohlcValues[20], highd5 = ohlcValues[21], lowd5 = ohlcValues[22], closed5 = ohlcValues[23];
        
        return numeroPattern switch
        {
            1 => Math.Abs(opend1 - closed1) < 0.5m * (highd1 - lowd1),
            2 => Math.Abs(opend1 - closed5) < 0.5m * (highd5 - closed1),
            3 => Math.Abs(opend5 - closed1) < 0.5m * (Math.Max(Math.Max(Math.Max(Math.Max(highd1, highd2), highd3), highd4), highd5) - Math.Min(Math.Min(Math.Min(Math.Min(lowd1, lowd2), lowd3), lowd4), lowd5)),
            4 => (highd0 - opend0) > ((highd1 - opend1) * 1m),
            5 => (highd0 - opend0) > ((highd1 - opend1) * 1.5m),
            6 => (opend0 - lowd0) > ((opend1 - lowd1) * 1m),
            7 => (opend0 - lowd0) > ((opend1 - lowd1) * 1.5m),
            8 => closed1 > closed2 && closed2 > closed3 && closed3 > closed4,
            9 => closed1 < closed2 && closed2 < closed3 && closed3 < closed4,
            10 => highd1 > highd2 && lowd1 > lowd2,
            11 => highd1 < highd2 && lowd1 < lowd2,
            12 => highd0 > (lowd0 + lowd0 * 0.75m / 100m),
            13 => highd0 < (lowd0 + lowd0 * 0.75m / 100m),
            14 => closed1 > closed2,
            15 => closed1 < closed2,
            16 => closed1 < opend1,
            17 => closed1 > opend1,
            18 => closed1 < (closed2 - closed2 * 0.5m / 100m),
            19 => closed1 > (closed2 + closed2 * 0.5m / 100m),
            20 => highd0 > highd1,
            21 => highd1 > highd5,
            22 => lowd0 < lowd1,
            23 => lowd1 < lowd5,
            24 => (highd1 > highd2) && (highd1 > highd3) && (highd1 > highd4),
            25 => (highd1 < highd2) && (highd1 < highd3) && (highd1 < highd4),
            26 => (lowd1 < lowd2) && (lowd1 < lowd3) && (lowd1 < lowd4),
            27 => (lowd1 > lowd2) && (lowd1 > lowd3) && (lowd1 > lowd4),
            28 => closed1 > closed2 && closed2 > closed3 && opend0 > closed1,
            29 => closed1 < closed2 && closed2 < closed3 && opend0 < closed1,
            30 => (highd1 - closed1) < 0.20m * (highd1 - lowd1),
            31 => (closed1 - lowd1) < 0.20m * (highd1 - lowd1),
            32 => opend0 < lowd1 || opend0 > highd1,
            33 => opend0 < (closed1 - closed1 * 0.5m / 100m),
            34 => opend0 > (closed1 + closed1 * 0.5m / 100m),
            35 => highd0 < highd1 && lowd0 > lowd1,
            36 => (highd1 - lowd1) < (((highd2 - lowd2) + (highd3 - lowd3)) / 3m),
            37 => (highd1 - lowd1) < (highd2 - lowd2) && (highd2 - lowd2) < (highd3 - lowd3),
            38 => highd2 > highd1 && lowd2 < lowd1,
            39 => highd1 < highd2 || lowd1 > lowd2,
            40 => highd2 < highd1 && lowd2 > lowd1,
            41 => true,
            42 => false,
            _ => false
        };
    }
    
    /// <summary>
    /// Highest - trova il valore più alto negli ultimi N periodi
    /// </summary>
    public static decimal Highest(OhlcvData[] data, int periods, Func<OhlcvData, decimal> selector)
    {
        if (data == null || data.Length < periods)
            return 0;
        
        return data.Skip(Math.Max(0, data.Length - periods))
                   .Select(selector)
                   .Max();
    }
    
    /// <summary>
    /// Lowest - trova il valore più basso negli ultimi N periodi
    /// </summary>
    public static decimal Lowest(OhlcvData[] data, int periods, Func<OhlcvData, decimal> selector)
    {
        if (data == null || data.Length < periods)
            return 0;
        
        return data.Skip(Math.Max(0, data.Length - periods))
                   .Select(selector)
                   .Min();
    }
    
    /// <summary>
    /// HighestFC - Highest con from current bar (stesso di Highest)
    /// </summary>
    public static decimal HighestFC(OhlcvData[] data, int periods, Func<OhlcvData, decimal> selector)
    {
        return Highest(data, periods, selector);
    }
    
    /// <summary>
    /// LowestFC - Lowest con from current bar (stesso di Lowest)
    /// </summary>
    public static decimal LowestFC(OhlcvData[] data, int periods, Func<OhlcvData, decimal> selector)
    {
        return Lowest(data, periods, selector);
    }
    
    /// <summary>
    /// UAPtnBase - Pattern Base per strategie (stesso di PtnBaseSA2)
    /// </summary>
    public static bool UAPtnBase(int numeroPattern, decimal[] ohlcValues)
    {
        return PtnBaseSA2(numeroPattern, ohlcValues);
    }
    
    /// <summary>
    /// PatterndirectionalFast - Alias per PatternDirectionalFast
    /// </summary>
    public static bool PatterndirectionalFast(int numeroPattern, decimal[] ohlcValues)
    {
        return PatternDirectionalFast(numeroPattern, ohlcValues);
    }
    
    /// <summary>
    /// Raggruppa i dati per giorno e calcola OHLC giornalieri
    /// </summary>
    public static List<DailyOHLC> GroupByDay(OhlcvData[] data, DateTime currentDate)
    {
        var dailyGroups = data
            .Where(d => d.DateTime <= currentDate)
            .GroupBy(d => d.DateTime.Date)
            .OrderByDescending(g => g.Key)
            .Take(6) // Ultimi 6 giorni
            .Select(g => new DailyOHLC
            {
                DateTime = g.Key,
                Open = g.First().Open,
                High = g.Max(d => d.High),
                Low = g.Min(d => d.Low),
                Close = g.Last().Close
            })
            .ToList();
        
        return dailyGroups;
    }
    
    public class DailyOHLC
    {
        public DateTime DateTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }
    
    /// <summary>
    /// Conta il numero di entry oggi (semplificato - conta solo se non c'è posizione aperta)
    /// </summary>
    public static int EntriesToday(OhlcvData[] data, DateTime currentDate, bool hasOpenPosition)
    {
        // Implementazione semplificata - in realtà dovrebbe tracciare le entry per giorno
        // Per ora restituisce 0 se non c'è posizione aperta, 1 se c'è
        return hasOpenPosition ? 1 : 0;
    }
    
    /// <summary>
    /// GetDailyOpen - Ottiene l'open del giorno corrente (0) o dei giorni precedenti (1, 2, ecc.)
    /// </summary>
    public static decimal GetDailyOpen(OhlcvData[] data, DateTime currentDate, int daysAgo = 0)
    {
        var dailyData = GroupByDay(data, currentDate);
        if (daysAgo >= dailyData.Count) return 0;
        return dailyData[daysAgo].Open;
    }
    
    /// <summary>
    /// GetDailyHigh - Ottiene l'high del giorno corrente (0) o dei giorni precedenti
    /// </summary>
    public static decimal GetDailyHigh(OhlcvData[] data, DateTime currentDate, int daysAgo = 0)
    {
        var dailyData = GroupByDay(data, currentDate);
        if (daysAgo >= dailyData.Count) return 0;
        return dailyData[daysAgo].High;
    }
    
    /// <summary>
    /// GetDailyLow - Ottiene il low del giorno corrente (0) o dei giorni precedenti
    /// </summary>
    public static decimal GetDailyLow(OhlcvData[] data, DateTime currentDate, int daysAgo = 0)
    {
        var dailyData = GroupByDay(data, currentDate);
        if (daysAgo >= dailyData.Count) return 0;
        return dailyData[daysAgo].Low;
    }
    
    /// <summary>
    /// GetDailyClose - Ottiene il close del giorno corrente (0) o dei giorni precedenti
    /// </summary>
    public static decimal GetDailyClose(OhlcvData[] data, DateTime currentDate, int daysAgo = 0)
    {
        var dailyData = GroupByDay(data, currentDate);
        if (daysAgo >= dailyData.Count) return 0;
        return dailyData[daysAgo].Close;
    }
    
    /// <summary>
    /// PatternFast - Pattern generico per strategie (151 pattern supportati)
    /// </summary>
    public static bool PatternFast(int numeroPattern, decimal[] ohlcValues)
    {
        if (ohlcValues == null || ohlcValues.Length < 24)
            return false;
        
        decimal opend0 = ohlcValues[0], highd0 = ohlcValues[1], lowd0 = ohlcValues[2], closed0 = ohlcValues[3];
        decimal opend1 = ohlcValues[4], highd1 = ohlcValues[5], lowd1 = ohlcValues[6], closed1 = ohlcValues[7];
        decimal opend2 = ohlcValues[8], highd2 = ohlcValues[9], lowd2 = ohlcValues[10], closed2 = ohlcValues[11];
        decimal opend3 = ohlcValues[12], highd3 = ohlcValues[13], lowd3 = ohlcValues[14], closed3 = ohlcValues[15];
        decimal opend4 = ohlcValues[16], highd4 = ohlcValues[17], lowd4 = ohlcValues[18], closed4 = ohlcValues[19];
        decimal opend5 = ohlcValues[20], highd5 = ohlcValues[21], lowd5 = ohlcValues[22], closed5 = ohlcValues[23];
        
        decimal body1d = Math.Abs(opend1 - closed1);
        decimal range1d = highd1 - lowd1;
        decimal body5d = Math.Abs(opend5 - closed1);
        decimal range5d = Math.Max(Math.Max(Math.Max(Math.Max(highd1, highd2), highd3), highd4), highd5) - 
                          Math.Min(Math.Min(Math.Min(Math.Min(lowd1, lowd2), lowd3), lowd4), lowd5);
        decimal currentClose = closed0; // C = close corrente
        
        return numeroPattern switch
        {
            >= 1 and <= 4 => body1d < (numeroPattern switch { 1 => 0.1m, 2 => 0.25m, 3 => 0.5m, 4 => 0.75m, _ => 0 }) * range1d,
            >= 5 and <= 8 => body1d > (numeroPattern switch { 5 => 0.25m, 6 => 0.5m, 7 => 0.75m, 8 => 0.9m, _ => 0 }) * range1d,
            >= 9 and <= 15 => body5d < (numeroPattern switch { 9 => 0.1m, 10 => 0.25m, 11 => 0.5m, 12 => 0.75m, 13 => 1m, 14 => 1.5m, 15 => 2m, _ => 0 }) * (highd5 - lowd1),
            >= 16 and <= 21 => body5d > (numeroPattern switch { 16 => 0.25m, 17 => 0.5m, 18 => 0.75m, 19 => 1m, 20 => 1.5m, 21 => 2m, _ => 0 }) * (highd5 - lowd1),
            22 => body5d > 2.5m * (highd5 - lowd1),
            >= 23 and <= 26 => body5d < (numeroPattern switch { 23 => 0.1m, 24 => 0.25m, 25 => 0.5m, 26 => 0.75m, _ => 0 }) * range5d,
            >= 27 and <= 30 => body5d > (numeroPattern switch { 27 => 0.9m, 28 => 0.25m, 29 => 0.5m, 30 => 0.75m, _ => 0 }) * range5d,
            >= 31 and <= 38 => (highd0 - opend0) > ((highd1 - opend1) * (numeroPattern switch { 31 => 0.25m, 32 => 0.5m, 33 => 0.75m, 34 => 1m, 35 => 1.5m, 36 => 2m, 37 => 2.5m, 38 => 3m, _ => 0 })),
            39 => (highd0 - opend0) < (highd1 - opend1),
            40 => (opend0 - lowd0) < (opend1 - lowd1),
            >= 41 and <= 46 => (opend0 - lowd0) > ((opend1 - lowd1) * (numeroPattern switch { 41 => 0.5m, 42 => 1m, 43 => 1.5m, 44 => 2m, 45 => 2.5m, 46 => 3m, _ => 0 })),
            47 => closed1 > closed2 && closed2 > closed3 && closed3 > closed4,
            48 => closed1 < closed2 && closed2 < closed3 && closed3 < closed4,
            49 => closed1 > closed2 && closed2 > closed3 && closed3 > closed4 && closed4 > closed5,
            50 => closed1 < closed2 && closed2 < closed3 && closed3 < closed4 && closed4 < closed5,
            51 => highd1 > highd2 && lowd1 > lowd2,
            52 => highd1 < highd2 && lowd1 < lowd2,
            >= 53 and <= 59 => highd0 > (lowd0 + lowd0 * (numeroPattern switch { 53 => 0.5m, 54 => 0.75m, 55 => 1m, 56 => 1.5m, 57 => 2m, 58 => 2.5m, 59 => 3m, _ => 0 }) * 0.01m),
            >= 60 and <= 66 => highd0 < (lowd0 + lowd0 * (numeroPattern switch { 60 => 0.5m, 61 => 0.75m, 62 => 1m, 63 => 1.5m, 64 => 2m, 65 => 2.5m, 66 => 3m, _ => 0 }) * 0.01m),
            67 => closed1 > closed2,
            68 => closed1 < closed2,
            69 => closed1 < opend1,
            70 => closed1 > opend1,
            >= 71 and <= 76 => closed1 < (closed2 - closed2 * (numeroPattern switch { 71 => 0.5m, 72 => 1m, 73 => 1.5m, 74 => 2m, 75 => 2.5m, 76 => 3m, _ => 0 }) * 0.01m),
            >= 77 and <= 80 => closed1 > (closed2 + closed2 * (numeroPattern switch { 77 => 0.5m, 78 => 1m, 79 => 1.5m, 80 => 2m, _ => 0 }) * 0.01m),
            >= 81 and <= 86 => highd0 > (highd1 + highd1 * (numeroPattern switch { 81 => 0m, 82 => 0.25m, 83 => 0.5m, 84 => 0.75m, 85 => 1m, 86 => 1.5m, _ => 0 }) * 0.01m),
            >= 87 and <= 92 => highd0 < (highd1 - highd1 * (numeroPattern switch { 87 => 0m, 88 => 0.5m, 89 => 1m, 90 => 1.5m, 91 => 2m, 92 => 2.5m, _ => 0 }) * 0.01m),
            93 => highd1 > highd5,
            94 => highd1 < highd5,
            >= 95 and <= 99 => lowd0 < (lowd1 - lowd1 * (numeroPattern switch { 95 => 0m, 96 => 0.25m, 97 => 0.5m, 98 => 0.75m, 99 => 1m, _ => 0 }) * 0.01m),
            >= 100 and <= 105 => lowd0 > (lowd1 + lowd1 * (numeroPattern switch { 100 => 0m, 101 => 0.5m, 102 => 1m, 103 => 1.5m, 104 => 2m, 105 => 2.5m, _ => 0 }) * 0.01m),
            106 => lowd1 < lowd5,
            107 => lowd1 > lowd5,
            108 => (highd1 > highd2) && (highd1 > highd3) && (highd1 > highd4),
            109 => (highd1 < highd2) && (highd1 < highd3) && (highd1 < highd4),
            110 => (lowd1 < lowd2) && (lowd1 < lowd3) && (lowd1 < lowd4),
            111 => (lowd1 > lowd2) && (lowd1 > lowd3) && (lowd1 > lowd4),
            112 => closed1 > closed2 && closed2 > closed3 && opend0 > closed1,
            113 => closed1 < closed2 && closed2 < closed3 && opend0 < closed1,
            114 => (highd1 - closed1) < 0.20m * range1d,
            115 => (closed1 - lowd1) < 0.20m * range1d,
            116 => opend0 < lowd1 || opend0 > highd1,
            117 => opend0 < lowd1,
            118 => opend0 > highd1,
            >= 119 and <= 122 => opend0 < (closed1 - closed1 * (numeroPattern switch { 119 => 0.25m, 120 => 0.5m, 121 => 0.75m, 122 => 1m, _ => 0 }) * 0.01m),
            >= 123 and <= 126 => opend0 > (closed1 + closed1 * (numeroPattern switch { 123 => 0.25m, 124 => 0.5m, 125 => 0.75m, 126 => 1m, _ => 0 }) * 0.01m),
            127 => highd0 < highd1 && lowd0 > lowd1,
            128 => range1d < (((highd2 - lowd2) + (highd3 - lowd3)) / 3m),
            129 => range1d < (highd2 - lowd2) && (highd2 - lowd2) < (highd3 - lowd3),
            130 => highd2 > highd1 && lowd2 < lowd1,
            131 => highd1 < highd2,
            132 => lowd1 > lowd2,
            133 => highd1 < highd2 || lowd1 > lowd2,
            134 => highd2 < highd1 && lowd2 > lowd1,
            135 => highd0 > highd1 && lowd0 < lowd1,
            136 => closed1 > opend1 && closed2 > opend2,
            137 => closed1 < opend1 && closed2 > opend2,
            138 => closed1 > opend1 && closed2 < opend2,
            139 => closed1 < opend1 && closed2 < opend2,
            140 => (highd1 - lowd1) < (highd2 - lowd2),
            141 => (highd1 - lowd1) > (highd2 - lowd2),
            >= 142 and <= 146 => currentClose > opend0 * (numeroPattern switch { 142 => 0.99m, 143 => 0.995m, 144 => 1m, 145 => 1.005m, 146 => 1.01m, _ => 0 }),
            >= 147 and <= 151 => currentClose < opend0 * (numeroPattern switch { 147 => 1.01m, 148 => 1.005m, 149 => 1m, 150 => 0.995m, 151 => 0.99m, _ => 0 }),
            152 => true,
            _ => false
        };
    }
    
    /// <summary>
    /// PatternDirectionalFast - Pattern direzionale per strategie (supporta valori positivi e negativi)
    /// </summary>
    public static bool PatternDirectionalFast(int numeroPattern, decimal[] ohlcValues)
    {
        if (ohlcValues == null || ohlcValues.Length < 24)
            return false;
        
        decimal opend0 = ohlcValues[0], highd0 = ohlcValues[1], lowd0 = ohlcValues[2], closed0 = ohlcValues[3];
        decimal opend1 = ohlcValues[4], highd1 = ohlcValues[5], lowd1 = ohlcValues[6], closed1 = ohlcValues[7];
        decimal opend2 = ohlcValues[8], highd2 = ohlcValues[9], lowd2 = ohlcValues[10], closed2 = ohlcValues[11];
        decimal opend3 = ohlcValues[12], highd3 = ohlcValues[13], lowd3 = ohlcValues[14], closed3 = ohlcValues[15];
        decimal opend4 = ohlcValues[16], highd4 = ohlcValues[17], lowd4 = ohlcValues[18], closed4 = ohlcValues[19];
        decimal opend5 = ohlcValues[20], highd5 = ohlcValues[21], lowd5 = ohlcValues[22], closed5 = ohlcValues[23];
        
        decimal range1d = highd1 - lowd1;
        decimal currentClose = closed0;

        // Il segno di numeroPattern sceglie il VERSO (positivo = long, negativo = short), il valore
        // assoluto sceglie il PATTERN. Il dispatch va quindi fatto sul valore assoluto: fatto sul
        // valore con segno, un pattern negativo non entrava in nessun case (sono tutti range
        // positivi) e cadeva nel default `false`, rendendo irraggiungibili tutti i rami short
        // scritti qui sotto. Le strategie che usano pattern direzionali negativi non emettevano mai
        // un segnale — vedi TOP_UA_303 (-47 e -9), TOP_UA_416, TOP_UA_695, TOP_UA_851, TOP_UA_940.
        var magnitude = Math.Abs(numeroPattern);

        return magnitude switch
        {
            >= 1 and <= 8 => numeroPattern > 0 
                ? (highd0 - opend0) > ((highd1 - opend1) * (Math.Abs(numeroPattern) switch { 1 => 0.25m, 2 => 0.5m, 3 => 0.75m, 4 => 1m, 5 => 1.5m, 6 => 2m, 7 => 2.5m, 8 => 3m, _ => 0 }))
                : (opend0 - lowd0) > ((opend1 - lowd1) * (Math.Abs(numeroPattern) switch { 1 => 0.25m, 2 => 0.5m, 3 => 0.75m, 4 => 1m, 5 => 1.5m, 6 => 2m, 7 => 2.5m, 8 => 3m, _ => 0 })),
            9 => numeroPattern > 0 ? (highd0 - opend0) < (highd1 - opend1) : (opend0 - lowd0) < (opend1 - lowd1),
            >= 10 and <= 12 => numeroPattern > 0
                ? numeroPattern switch
                {
                    10 => closed1 > closed2 && closed2 > closed3 && closed3 > closed4,
                    11 => closed1 > closed2 && closed2 > closed3 && closed3 > closed4 && closed4 > closed5,
                    12 => highd1 > highd2 && lowd1 > lowd2,
                    _ => false
                }
                : Math.Abs(numeroPattern) switch
                {
                    10 => closed1 < closed2 && closed2 < closed3 && closed3 < closed4,
                    11 => closed1 < closed2 && closed2 < closed3 && closed3 < closed4 && closed4 < closed5,
                    12 => highd1 < highd2 && lowd1 < lowd2,
                    _ => false
                },
            >= 13 and <= 14 => numeroPattern > 0
                ? numeroPattern switch { 13 => closed1 > closed2, 14 => closed1 > opend1, _ => false }
                : Math.Abs(numeroPattern) switch { 13 => closed1 < closed2, 14 => closed1 < opend1, _ => false },
            >= 15 and <= 20 => numeroPattern > 0
                ? closed1 > (closed2 + closed2 * (Math.Abs(numeroPattern) switch { 15 => 0.5m, 16 => 1m, 17 => 1.5m, 18 => 2m, 19 => 2.5m, 20 => 3m, _ => 0 }) * 0.01m)
                : closed1 < (closed2 - closed2 * (Math.Abs(numeroPattern) switch { 15 => 0.5m, 16 => 1m, 17 => 1.5m, 18 => 2m, 19 => 2.5m, 20 => 3m, _ => 0 }) * 0.01m),
            >= 21 and <= 26 => numeroPattern > 0
                ? highd0 > (highd1 + highd1 * (Math.Abs(numeroPattern) switch { 21 => 0m, 22 => 0.25m, 23 => 0.5m, 24 => 0.75m, 25 => 1m, 26 => 1.5m, _ => 0 }) * 0.01m)
                : lowd0 < (lowd1 - lowd1 * (Math.Abs(numeroPattern) switch { 21 => 0m, 22 => 0.25m, 23 => 0.5m, 24 => 0.75m, 25 => 1m, 26 => 1.5m, _ => 0 }) * 0.01m),
            >= 27 and <= 32 => numeroPattern > 0
                ? lowd0 > (lowd1 + lowd1 * (Math.Abs(numeroPattern) switch { 27 => 0m, 28 => 0.5m, 29 => 1m, 30 => 1.5m, 31 => 2m, 32 => 2.5m, _ => 0 }) * 0.01m)
                : highd0 < (highd1 - highd1 * (Math.Abs(numeroPattern) switch { 27 => 0m, 28 => 0.5m, 29 => 1m, 30 => 1.5m, 31 => 2m, 32 => 2.5m, _ => 0 }) * 0.01m),
            >= 33 and <= 34 => numeroPattern > 0
                ? numeroPattern switch { 33 => highd1 > highd5, 34 => highd1 < highd5, _ => false }
                : Math.Abs(numeroPattern) switch { 33 => lowd1 < lowd5, 34 => lowd1 > lowd5, _ => false },
            >= 35 and <= 36 => numeroPattern > 0
                ? numeroPattern switch
                {
                    35 => highd1 > highd2 && highd1 > highd3 && highd1 > highd4,
                    36 => lowd1 > lowd2 && lowd1 > lowd3 && lowd1 > lowd4,
                    _ => false
                }
                : Math.Abs(numeroPattern) switch
                {
                    35 => lowd1 < lowd2 && lowd1 < lowd3 && lowd1 < lowd4,
                    36 => highd1 < highd2 && highd1 < highd3 && highd1 < highd4,
                    _ => false
                },
            37 => numeroPattern > 0
                ? closed1 > closed2 && closed2 > closed3 && opend0 > closed1
                : closed1 < closed2 && closed2 < closed3 && opend0 < closed1,
            38 => numeroPattern > 0
                ? (highd1 - closed1) < 0.20m * range1d
                : (closed1 - lowd1) < 0.20m * range1d,
            39 => numeroPattern > 0 ? opend0 > highd1 : opend0 < lowd1,
            >= 40 and <= 43 => numeroPattern > 0
                ? opend0 > (closed1 + closed1 * (Math.Abs(numeroPattern) switch { 40 => 0.25m, 41 => 0.5m, 42 => 0.75m, 43 => 1m, _ => 0 }) * 0.01m)
                : opend0 < (closed1 - closed1 * (Math.Abs(numeroPattern) switch { 40 => 0.25m, 41 => 0.5m, 42 => 0.75m, 43 => 1m, _ => 0 }) * 0.01m),
            44 => numeroPattern > 0 ? lowd1 > lowd2 : highd1 < highd2,
            >= 45 and <= 46 => numeroPattern > 0
                ? numeroPattern switch
                {
                    45 => closed1 > opend1 && closed2 > opend2,
                    46 => closed1 > opend1 && closed2 < opend2,
                    _ => false
                }
                : Math.Abs(numeroPattern) switch
                {
                    45 => closed1 < opend1 && closed2 < opend2,
                    46 => closed1 < opend1 && closed2 > opend2,
                    _ => false
                },
            >= 47 and <= 51 => numeroPattern > 0
                ? currentClose > opend0 * (Math.Abs(numeroPattern) switch { 47 => 0.99m, 48 => 0.995m, 49 => 1m, 50 => 1.005m, 51 => 1.01m, _ => 0 })
                : currentClose < opend0 * (Math.Abs(numeroPattern) switch { 47 => 1.01m, 48 => 1.005m, 49 => 1m, 50 => 0.995m, 51 => 0.99m, _ => 0 }),
            52 => true,
            _ => false
        };
    }
    
    /// <summary>
    /// iADXOnArray - Calcola ADX usando valori OHLC da array
    /// </summary>
    public static decimal iADXOnArray(int adxPeriod, decimal curHigh, decimal curLow, decimal curClose, 
        decimal prevHigh, decimal prevLow, decimal prevClose, ref decimal[] calcValues)
    {
        if (calcValues == null || calcValues.Length < 4)
            calcValues = new decimal[4];
        
        decimal atrVal = Math.Max(Math.Abs(curHigh - curLow), Math.Abs(curHigh - prevClose));
        atrVal = Math.Max(atrVal, Math.Abs(curLow - prevClose));
        decimal atrValAvg = calcValues[1] - (calcValues[1] / adxPeriod) + atrVal;
        
        decimal pdm = curHigh - prevHigh;
        decimal mdm = prevLow - curLow;
        if (pdm < 0) pdm = 0;
        if (mdm < 0) mdm = 0;
        
        if (pdm == mdm || atrVal == 0)
        {
            pdm = 0;
            mdm = 0;
        }
        else
        {
            if (pdm < mdm) pdm = 0;
            else if (mdm < pdm) mdm = 0;
        }
        
        decimal pdmAvg = calcValues[2] - (calcValues[2] / adxPeriod) + pdm;
        decimal mdmAvg = calcValues[3] - (calcValues[3] / adxPeriod) + mdm;
        
        decimal pdi = 0, mdi = 0;
        if (atrValAvg != 0)
        {
            pdi = (pdmAvg / atrValAvg) * 100;
            mdi = (mdmAvg / atrValAvg) * 100;
        }
        
        decimal dx = 0;
        if (pdi + mdi != 0)
            dx = Math.Abs((pdi - mdi) / (pdi + mdi));
        
        decimal adxVal = (calcValues[0] * (adxPeriod - 1) + dx) / adxPeriod;
        
        calcValues[0] = adxVal;
        calcValues[1] = atrValAvg;
        calcValues[2] = pdmAvg;
        calcValues[3] = mdmAvg;
        
        return calcValues[0];
    }
    
    /// <summary>
    /// Calcola True Range per una barra
    /// TrueRange = Max(High - Low, Abs(High - PreviousClose), Abs(Low - PreviousClose))
    /// </summary>
    public static decimal TrueRange(OhlcvData current, OhlcvData? previous)
    {
        if (previous == null)
            return current.High - current.Low;
        
        decimal tr1 = current.High - current.Low;
        decimal tr2 = Math.Abs(current.High - previous.Close);
        decimal tr3 = Math.Abs(current.Low - previous.Close);
        
        return Math.Max(tr1, Math.Max(tr2, tr3));
    }
    
    /// <summary>
    /// Calcola Average True Range (ATR) con media semplice
    /// </summary>
    public static decimal AvgTrueRange(OhlcvData[] data, int periods) =>
        AvgTrueRange(data, periods, barsAgo: 0);

    /// <summary>
    /// ATR arretrato di <paramref name="barsAgo"/> barre, cioè l'EasyLanguage
    /// <c>AvgTrueRange(periods)[barsAgo]</c>. Serve ai filtri che confrontano la barra corrente con
    /// la media della volatilità <i>precedente</i>, escludendola dalla media.
    /// </summary>
    public static decimal AvgTrueRange(OhlcvData[] data, int periods, int barsAgo)
    {
        if (data == null || periods <= 0 || barsAgo < 0)
            return 0;

        // Il true range della prima barra della media ha bisogno della barra che la precede.
        int end = data.Length - 1 - barsAgo;
        if (end < periods)
            return 0;

        decimal sum = 0;
        for (int i = end - periods + 1; i <= end; i++)
        {
            OhlcvData? prev = i > 0 ? data[i - 1] : null;
            sum += TrueRange(data[i], prev);
        }

        return sum / periods;
    }
    
    /// <summary>
    /// Time Window - verifica se l'ora corrente è nel range specificato (fine esclusiva, come tw()).
    /// Gestisce anche il caso in cui startTime > endTime (sessione che attraversa la mezzanotte).
    /// </summary>
    public static bool TimeWindow(int startTime, int endTime, DateTime currentDate)
    {
        var currentTime = GetHhmm(currentDate);
        
        if (startTime > endTime)
        {
            // Sessione che attraversa la mezzanotte (es. 1700-1600)
            return currentTime >= startTime || currentTime < endTime;
        }

        // Sessione normale (es. 0900-1300) — fine esclusiva come f_tw
        return currentTime >= startTime && currentTime < endTime;
    }

    /// <summary>
    /// Finestra oraria con estremi inclusivi, come la logica inline di Easy 152.
    /// </summary>
    public static bool TimeWindowInclusive(int startTime, int endTime, DateTime currentDate)
    {
        var currentTime = GetHhmm(currentDate);

        if (startTime > endTime)
        {
            return currentTime >= startTime || currentTime <= endTime;
        }

        return currentTime >= startTime && currentTime <= endTime;
    }
    
    /// <summary>
    /// twBars - verifica se mycount è nel range tra startBar e endBar
    /// Gestisce anche il caso in cui startBar > endBar
    /// </summary>
    public static bool TwBars(int startBar, int endBar, int mycount)
    {
        if (startBar > endBar)
        {
            return mycount >= startBar || mycount < endBar;
        }
        else
        {
            return mycount >= startBar && mycount < endBar;
        }
    }
    
    /// <summary>
    /// Verifica se è l'ultima barra della sessione (semplificato)
    /// </summary>
    public static bool IsSessionLastBar(OhlcvData[] data, DateTime currentDate, int sessionStartTime, int sessionEndTime)
    {
        if (data == null || data.Length == 0)
            return false;
        
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;
        var nextBarTime = currentDate.AddMinutes(GetTimeframeMinutes(data)).Hour * 100 + 
                          currentDate.AddMinutes(GetTimeframeMinutes(data)).Minute;
        
        // Se la prossima barra sarebbe fuori dalla sessione, questa è l'ultima
        if (sessionStartTime > sessionEndTime)
        {
            // Sessione che attraversa mezzanotte
            return (currentTime >= sessionStartTime && nextBarTime < sessionStartTime && nextBarTime >= sessionEndTime) ||
                   (currentTime < sessionEndTime && nextBarTime >= sessionEndTime);
        }
        else
        {
            // Sessione normale
            return currentTime >= sessionStartTime && currentTime < sessionEndTime && 
                   (nextBarTime >= sessionEndTime || nextBarTime < sessionStartTime);
        }
    }
    
    /// <summary>
    /// Stima il timeframe in minuti dai dati
    /// </summary>
    private static int GetTimeframeMinutes(OhlcvData[] data)
    {
        if (data == null || data.Length < 2)
            return 60; // Default
        
        var diff = data[data.Length - 1].DateTime - data[data.Length - 2].DateTime;
        return (int)diff.TotalMinutes;
    }
}
