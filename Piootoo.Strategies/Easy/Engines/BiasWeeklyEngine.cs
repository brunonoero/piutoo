using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Motore riutilizzabile per i BIAS settimanali a ingresso market programmato.
///
/// <para>Segue <c>easy_engine_py/bias_weekly.py</c>: ogni verso ha un solo ingresso
/// programmato (giorno/orario Python, con lunedì = 0), eseguito all'apertura di quella
/// barra. Il filtro Fast è quindi valutato sulla barra precedente. L'uscita viene allegata
/// al segnale d'ingresso come <see cref="TradeSignal.CloseAtUtc"/> e non dipende da un
/// futuro segnale <c>LX</c>/<c>SX</c>.</para>
///
/// <para><b>Il segnale nasce sulla barra prima di quella pianificata</b> (dal 11/09/2026), come
/// <c>buy next bar at market</c>: valutando la barra <c>B</c> il motore chiede se la barra
/// <i>successiva</i> e' quella dell'ingresso e, se si', emette un market con
/// <c>ValidFromUtc</c> = apertura di quella barra. Prima il segnale nasceva <i>sulla</i> barra
/// pianificata con <c>ValidFromUtc</c> = la sua apertura: nel backtest funzionava perche' il
/// loop la vede al suo inizio, ma il server la vede solo quando il cBot la spinge chiusa, quindi
/// il cBot entrava sempre una barra dopo (compare-0033: tutti gli intent market a 15 e 60 minuti
/// in ritardo di una barra intera, −36 k per contratto sulle tre <c>PTS_ES_BSW_*</c>). Ora i due
/// motori generano il segnale nello stesso istante.</para>
///
/// <para><b>L'orario pianificato e' l'etichetta di chiusura della barra</b>, come lo scrive il
/// dossier e come dal 08/09/2026 leggono finestra e filtro del giorno (<c>ParamTime</c>,
/// <c>PythonWeekday</c>): la barra dell'ingresso e' quella la cui chiusura cade a <c>le_time</c>,
/// e si entra alla sua apertura. Lo stesso per <c>lx_time</c>: la deadline e' l'apertura della
/// barra che chiude a quell'ora. Con <c>LegacyBarOpenLabels</c> si torna al confronto
/// sull'apertura.</para>
///
/// <para>I gate Fast indipendenti long/short coprono la forma standard del motore Python; le
/// varianti storiche possono aggiungere gate Neutral, Directional e BaseSA, inclusi più divieti
/// per lo stesso verso. Ogni gate <c>yes</c> deve essere vero e ogni gate <c>no</c> falso.</para>
/// </summary>
public abstract class BiasWeeklyEngine : EasyEngineBase
{
    /// <summary>Famiglie di pattern presenti nelle varianti EasyLanguage BIASW.</summary>
    protected enum WeeklyPatternKind
    {
        Fast,
        NeutralFast,
        DirectionalFast,
        BaseSA2
    }

    /// <summary>
    /// Regola di pattern obbligatoria o di esclusione. I gate sono valutati sulla barra precedente,
    /// come l'ordine <c>next bar market</c> del sorgente EasyLanguage.
    /// </summary>
    protected sealed record WeeklyPatternRule(WeeklyPatternKind Kind, int Number, bool MustMatch);

    /// <summary>
    /// Programmazione di un ingresso settimanale. Una finestra è inclusiva e permette di riprodurre
    /// i template che tentano l'ingresso per quindici minuti, non solo a un singolo timestamp.
    /// </summary>
    protected sealed record WeeklySchedule(
        int EntryDay,
        TimeOnly EntryStartTime,
        TimeOnly EntryEndTime,
        int ExitDay,
        TimeOnly ExitTime,
        int SkipMonth = 0);

    // ------------------------------------------------------------------ abilitazione e calendario

    protected bool EnableLong = true;
    protected bool EnableShort = true;

    /// <summary>Giorno Python (0 = lunedì) dell'ingresso; -1 disabilita il verso.</summary>
    protected int EntryDayLong = -1;
    protected int EntryDayShort = -1;

    /// <summary>Orario del singolo ingresso long/short programmato, come etichetta di chiusura della barra.</summary>
    protected TimeOnly EntryTimeLong;
    protected TimeOnly EntryTimeShort;

    /// <summary>
    /// Programmazioni aggiuntive. Se vuote, il motore usa i campi singoli storici qui sopra per
    /// mantenere compatibili le prime strategie e i test del motore.
    /// </summary>
    protected IReadOnlyList<WeeklySchedule> LongSchedules = Array.Empty<WeeklySchedule>();
    protected IReadOnlyList<WeeklySchedule> ShortSchedules = Array.Empty<WeeklySchedule>();

    // ------------------------------------------------------------------ gate Fast

    /// <summary>Gate <c>ptn_ly_yes</c>/<c>ptn_ly_no</c> del motore Python.</summary>
    protected int FastYesLong = 152;
    protected int FastNoLong = 153;

    /// <summary>Gate <c>ptn_sy_yes</c>/<c>ptn_sy_no</c> del motore Python.</summary>
    protected int FastYesShort = 152;
    protected int FastNoShort = 153;

    /// <summary>Gate aggiuntivi, inclusi i secondi divieti dei template originali.</summary>
    protected IReadOnlyList<WeeklyPatternRule> LongPatternRules = Array.Empty<WeeklyPatternRule>();
    protected IReadOnlyList<WeeklyPatternRule> ShortPatternRules = Array.Empty<WeeklyPatternRule>();

    // ------------------------------------------------------------------ calendario di uscita

    /// <summary>Giorno Python (0 = lunedì) e orario dell'uscita long; giorno -1 disabilita la deadline.</summary>
    protected int ExitDayLong = -1;
    protected TimeOnly ExitTimeLong;

    /// <summary>Giorno Python (0 = lunedì) e orario dell'uscita short; giorno -1 disabilita la deadline.</summary>
    protected int ExitDayShort = -1;
    protected TimeOnly ExitTimeShort;

    // ------------------------------------------------------------------ uscite monetarie, per verso

    protected decimal StopMoneyLong;
    protected decimal StopMoneyShort;
    protected decimal ProfitMoneyLong;
    protected decimal ProfitMoneyShort;
    protected decimal BreakEvenMoneyLong;
    protected decimal BreakEvenMoneyShort;
    protected decimal TrailingMoneyLong;
    protected decimal TrailingMoneyShort;

    // MaxEntriesPerSession è dichiarato in EasyEngineBase.

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        // La barra pianificata e' la PROSSIMA: questa (B) e' quella su cui il segnale nasce, e i
        // gate si leggono sulle barre fino a B inclusa — lo shift(1) del Python rispetto alla
        // barra dell'ingresso. Il timeframe e' quello dichiarato, non dedotto dalla serie:
        // attraverso un buco la distanza fra le ultime due barre e' il buco.
        var entryBarUtc = EasyLib.EstimateNextBarUtc(data, barTime, TimeframeMinutes);
        BuildSessionOhlc(data, barTime, out var ohlc);

        var entries = new List<TradeSignal>(2);
        if (CanEnterLong(entryBarUtc, ohlc, out var longSchedule))
            entries.Add(BuildEntry(SignalType.Buy, bar, barTime, entryBarUtc, longSchedule));

        if (CanEnterShort(entryBarUtc, ohlc, out var shortSchedule))
            entries.Add(BuildEntry(SignalType.Sell, bar, barTime, entryBarUtc, shortSchedule));

        return Combine(entries, Hold(bar.Close, barTime));
    }

    private bool CanEnterLong(DateTime entryBarUtc, decimal[] ohlc, out WeeklySchedule schedule)
    {
        schedule = FindSchedule(LongSchedules, EntryDayLong, EntryTimeLong, ExitDayLong, ExitTimeLong, entryBarUtc);
        return EnableLong &&
               CurrentMP != 1 &&
               IsInScheduledEntry(entryBarUtc, schedule) &&
               PassesGates(FastYesLong, FastNoLong, LongPatternRules, ohlc);
    }

    private bool CanEnterShort(DateTime entryBarUtc, decimal[] ohlc, out WeeklySchedule schedule)
    {
        schedule = FindSchedule(ShortSchedules, EntryDayShort, EntryTimeShort, ExitDayShort, ExitTimeShort, entryBarUtc);
        return EnableShort &&
               CurrentMP != -1 &&
               IsInScheduledEntry(entryBarUtc, schedule) &&
               PassesGates(FastYesShort, FastNoShort, ShortPatternRules, ohlc);
    }

    /// <param name="bar">La barra di segnale, l'ultima chiusa.</param>
    /// <param name="barTime">Apertura della barra di segnale.</param>
    /// <param name="entryBarUtc">Apertura della barra pianificata, su cui il market si esegue.</param>
    private TradeSignal BuildEntry(
        SignalType side, OhlcvData bar, DateTime barTime, DateTime entryBarUtc, WeeklySchedule schedule)
    {
        var signal = new TradeSignal
        {
            Date = barTime,
            Type = side,
            // Prezzo di riferimento: l'engine riempie all'apertura effettiva della barra pianificata.
            Price = bar.Close,
            StrategyName = Name,
            Quantity = Contracts,
            OrderType = TradeOrderType.Market,
            ValidFromUtc = entryBarUtc,
            ExpiresAtUtc = entryBarUtc,
            TimeframeMinutes = TimeframeMinutes,
            Reason = side == SignalType.Buy ? "LE_BIASW" : "SE_BIASW"
        };

        var isLong = side == SignalType.Buy;
        signal.StopLossMoneyPerFutureContract = ValueForSide(StopMoneyLong, StopMoneyShort, isLong);
        signal.TakeProfitMoneyPerFutureContract = ValueForSide(ProfitMoneyLong, ProfitMoneyShort, isLong);
        signal.BreakEvenMoneyPerFutureContract = ValueForSide(BreakEvenMoneyLong, BreakEvenMoneyShort, isLong);
        signal.TrailingStopMoneyPerFutureContract = ValueForSide(TrailingMoneyLong, TrailingMoneyShort, isLong);

        var exitDay = schedule.ExitDay;
        if (exitDay >= 0)
            signal.CloseAtUtc = ResolveScheduledExitUtc(entryBarUtc, exitDay, schedule.ExitTime);

        if (MaxEntriesPerSession > 0)
        {
            signal.MaxEntriesPerSession = MaxEntriesPerSession;
            signal.EntrySessionStartUtc = GetSessionStartUtc(entryBarUtc);
        }

        return signal;
    }

    private static decimal? ValueForSide(decimal longValue, decimal shortValue, bool isLong)
    {
        var value = isLong ? longValue : shortValue;
        return value > 0m ? value : null;
    }

    private static bool PassesGates(
        int fastYes,
        int fastNo,
        IReadOnlyList<WeeklyPatternRule> rules,
        decimal[] ohlc)
    {
        if (!EasyLib.PatternFast(fastYes, ohlc) || EasyLib.PatternFast(fastNo, ohlc))
            return false;

        foreach (var rule in rules)
        {
            var matches = Pattern(rule.Kind, rule.Number, ohlc);
            if (matches != rule.MustMatch)
                return false;
        }

        return true;
    }

    private static bool Pattern(WeeklyPatternKind kind, int number, decimal[] ohlc) => kind switch
    {
        WeeklyPatternKind.Fast => EasyLib.PatternFast(number, ohlc),
        WeeklyPatternKind.NeutralFast => EasyLib.PatternNeutralFast(number, ohlc),
        WeeklyPatternKind.DirectionalFast => EasyLib.PatternDirectionalFast(number, ohlc),
        WeeklyPatternKind.BaseSA2 => EasyLib.PtnBaseSA2(number, ohlc),
        _ => false
    };

    private WeeklySchedule FindSchedule(
        IReadOnlyList<WeeklySchedule> schedules,
        int entryDay,
        TimeOnly entryTime,
        int exitDay,
        TimeOnly exitTime,
        DateTime barTime)
    {
        if (schedules.Count == 0)
            return new WeeklySchedule(entryDay, entryTime, entryTime, exitDay, exitTime);

        foreach (var schedule in schedules)
        {
            if (IsInScheduledEntry(barTime, schedule))
                return schedule;
        }

        return new WeeklySchedule(-1, TimeOnly.MinValue, TimeOnly.MinValue, -1, TimeOnly.MinValue);
    }

    /// <summary>
    /// Le leg (ingresso o uscita, per verso) il cui istante programmato <b>non esiste</b> nella
    /// serie fornita: giorno della settimana e orario per cui il feed non ha una sola barra.
    ///
    /// <para><b>Perche' serve.</b> Il BIASW entra ed esce a giorno e ora fissi con un confronto
    /// esatto: se quella barra non c'e', la leg non esiste e il motore non se ne accorge — non e'
    /// uno skip, non e' un errore, e' <i>niente</i>. Misurato su compare-0017: <c>@HO_60</c> non ha
    /// nessuna barra fra le 22:00 e le 23:00 di Roma, quindi gli ingressi LONG di
    /// <c>PTS_HO_BSW_001_60</c> e <c>PTS_HO_BSW_002_60</c> (marted&#236; 23:00) hanno una barra
    /// disponibile su trentacinque settimane e producono <b>zero</b> segnali; l'uscita LONG di
    /// <c>PTS_HO_BSW_003_60</c> (venerd&#236; 23:00) non ne ha nessuna. Otto mesi di run senza un
    /// segnale e senza una riga di diagnostica.</para>
    ///
    /// <para>Il conteggio passa dall'orologio del motore, non dall'ora grezza della barra: e' lo
    /// stesso confronto che fa <see cref="IsInScheduledEntry"/>, altrimenti la verifica
    /// risponderebbe a una domanda diversa da quella che l'engine si pone.</para>
    /// </summary>
    /// <param name="data">La serie completa del proprio stream, gia' ordinata.</param>
    public IReadOnlyList<string> UnreachableScheduleLegs(OhlcvData[] data)
    {
        if (data is null || data.Length == 0)
            return Array.Empty<string>();

        var legs = new List<(string Nome, int Giorno, TimeOnly Orario)>(4);
        if (EnableLong && EntryDayLong >= 0) legs.Add(("ingresso LONG", EntryDayLong, EntryTimeLong));
        if (EnableLong && ExitDayLong >= 0) legs.Add(("uscita LONG", ExitDayLong, ExitTimeLong));
        if (EnableShort && EntryDayShort >= 0) legs.Add(("ingresso SHORT", EntryDayShort, EntryTimeShort));
        if (EnableShort && ExitDayShort >= 0) legs.Add(("uscita SHORT", ExitDayShort, ExitTimeShort));
        if (legs.Count == 0)
            return Array.Empty<string>();

        var conteggi = new int[legs.Count];
        foreach (var bar in data)
        {
            // Stessa etichetta con cui IsInScheduledEntry giudica la barra: la chiusura.
            var giorno = PythonWeekday(bar.DateTime);
            var orario = ParamTime(bar.DateTime);
            for (var index = 0; index < legs.Count; index++)
            {
                if (legs[index].Giorno == giorno && legs[index].Orario == orario)
                    conteggi[index]++;
            }
        }

        var irraggiungibili = new List<string>();
        for (var index = 0; index < legs.Count; index++)
        {
            if (conteggi[index] == 0)
            {
                irraggiungibili.Add(
                    $"{legs[index].Nome} ({GiornoPython(legs[index].Giorno)} {legs[index].Orario:HH\\:mm})");
            }
        }

        return irraggiungibili;
    }

    private static string GiornoPython(int giorno) => giorno switch
    {
        0 => "lun", 1 => "mar", 2 => "mer", 3 => "gio", 4 => "ven", 5 => "sab", 6 => "dom",
        _ => "?"
    };

    /// <summary>
    /// Se la barra che apre in <paramref name="entryBarUtc"/> e' quella pianificata: giorno e
    /// orario si leggono sulla sua <b>etichetta di chiusura</b> (<see cref="EasyEngineBase.ParamTime"/>,
    /// <see cref="EasyEngineBase.PythonWeekday"/>), come per la finestra operativa degli altri motori.
    /// </summary>
    private bool IsInScheduledEntry(DateTime entryBarUtc, WeeklySchedule schedule)
    {
        if (schedule.EntryDay < 0 || PythonDayOfWeek(entryBarUtc) != schedule.EntryDay)
            return false;

        var orario = ParamTime(entryBarUtc);
        return orario >= schedule.EntryStartTime &&
               orario <= schedule.EntryEndTime &&
               (schedule.SkipMonth == 0 || LabelDay(entryBarUtc).Month != schedule.SkipMonth);
    }

    /// <summary>Il giorno con cui la ricerca chiama una barra: quello della chiusura, salvo etichettatura legacy.</summary>
    private DateTime LabelDay(DateTime barOpenUtc) =>
        LegacyBarOpenLabels ? Clock.SessionDay(barOpenUtc) : Clock.BarLabelDay(barOpenUtc, TimeframeMinutes);

    private DateTime GetSessionStartUtc(DateTime barTime)
    {
        var sessionStart = Clock.SessionInstantUtc(barTime, SessionStart);
        if (SessionStart > SessionEnd && TimeOfDay(barTime) < SessionStart)
            sessionStart = Clock.SessionInstantUtc(barTime.AddDays(-1), SessionStart);
        return sessionStart;
    }

    /// <summary>
    /// Trova la prima occorrenza dell'orario di uscita nel giorno Python richiesto, fino a sette
    /// giorni dopo l'ingresso. Gestisce sia le uscite nella stessa settimana sia quelle della
    /// settimana successiva (per esempio venerdì → lunedì).
    ///
    /// <para><c>lx_time</c> e' l'etichetta di chiusura della barra di uscita, come <c>le_time</c>
    /// per l'ingresso: la deadline e' quindi l'apertura di quella barra, un timeframe prima
    /// dell'istante dichiarato. Con <c>LegacyBarOpenLabels</c> resta l'istante stesso.</para>
    /// </summary>
    protected DateTime ResolveScheduledExitUtc(DateTime entryBarTime, int exitDay, TimeOnly exitTime)
    {
        for (var offset = 0; offset <= 7; offset++)
        {
            var giorno = entryBarTime.AddDays(offset);
            if (PythonDayOfWeek(giorno) != exitDay)
                continue;

            var candidate = Clock.SessionInstantUtc(giorno, exitTime);
            if (!LegacyBarOpenLabels)
                candidate = candidate.AddMinutes(-TimeframeMinutes);
            if (candidate > entryBarTime)
                return candidate;
        }

        throw new InvalidOperationException("Impossibile risolvere la deadline BIASW.");
    }

    /// <summary>Convenzione del motore Python: lunedì = 0 … domenica = 6.</summary>
    private int PythonDayOfWeek(DateTime time) => PythonWeekday(time);
}
