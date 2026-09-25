using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore HOD: <b>deriva oraria pura</b>. Entra a un'ora fissa e tiene per un numero fisso di ore; non
/// guarda livelli, canali ne' pattern. E' la famiglia della serie PT6EXO piu' lontana dal catalogo,
/// che legge tutto dalla geometria del prezzo. Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §HOD.
///
/// <para><b>L'orologio.</b> <see cref="EntryTime"/> e' un orario locale nell'orologio dichiarato da
/// <see cref="ScheduleClock"/>: quello della ricerca (Roma, per ogni simbolo) oppure l'ora di borsa
/// dello strumento, con il fuso preso dal calendario del simbolo. <b>Mai UTC</b>: una deriva "delle
/// 15:30 di New York" letta in UTC si sposterebbe di un'ora due volte l'anno, e nelle settimane in cui
/// l'ora legale americana ed europea non sono allineate anche rispetto a Roma. Anche l'uscita si conta
/// in ora locale: <see cref="HoldHours"/> dopo l'ingresso sull'orologio del mercato, cosi' un'uscita
/// "alle 16" resta alle 16 anche la notte del cambio d'ora.</para>
///
/// <para><b>Quando nasce l'ordine.</b> Come <c>BiasWeeklyEngine</c>, sulla barra <b>prima</b> di quella
/// pianificata: la strategia viene valutata alla chiusura della barra, e l'ingresso a mercato
/// "next bar" apre esattamente all'orario dichiarato. La barra pianificata e' quella la cui
/// <b>apertura</b> cade su <see cref="EntryTime"/>, qualunque sia l'etichetta della ricerca: un orario
/// che non e' l'apertura di una barra del timeframe (le 09:30 su barre orarie) non scatta mai.
/// Un'apertura proiettata in un giorno senza sessione (il sabato, dall'ultima barra del venerdi') non
/// e' un ingresso: altrimenti l'ordine aspetterebbe la prima barra vera e aprirebbe il lunedi' a un
/// orario che nessuno ha dichiarato.</para>
///
/// <para><b>Il verso.</b> Con <see cref="MomentumMode"/> spento lo dichiara <see cref="Direction"/>
/// (1 long, 2 short). Acceso, lo decide il segno del movimento delle ultime <see cref="MomentumBars"/>
/// barre: 1 = lo segue, 2 = va contro; <see cref="Direction"/> a 1 o 2 tiene solo quel lato. E' la
/// variante "entra alle H solo se la notte e' salita".</para>
/// </summary>
public abstract class HourOfDayEngine : EasyEngineBase
{
    /// <summary>Orario locale di ingresso: apertura della barra su cui si entra.</summary>
    protected TimeOnly EntryTime = new(10, 0);

    /// <summary>
    /// L'ora piena di ingresso come la scrive una griglia: un intero 0..23. E' il solo punto in cui
    /// l'ora entra come numero; da qui in poi e' un <see cref="TimeOnly"/>.
    /// </summary>
    protected int EntryHour
    {
        set => EntryTime = new TimeOnly(value, 0);
    }

    /// <summary>Ore di tenuta in ora locale. 0 = nessuna uscita a tempo propria, restano le uscite comuni.</summary>
    protected int HoldHours = 4;

    /// <summary>In quale orologio sono scritti <see cref="EntryTime"/> e la tenuta.</summary>
    protected InstrumentClock ScheduleClock = InstrumentClock.Research;

    /// <summary>
    /// Lato: 1 long, 2 short. Con <see cref="MomentumMode"/> acceso, 0 = entrambi e 1/2 tengono solo quel
    /// lato; spento, 0 non dice in che verso entrare ed e' un errore di configurazione.
    /// </summary>
    protected int Direction = 1;

    /// <summary>0 = verso fisso da <see cref="Direction"/>; 1 = segue il movimento recente; 2 = va contro.</summary>
    protected int MomentumMode;

    /// <summary>Barre su cui si misura il movimento recente: chiusura di segnale contro chiusura di N barre prima.</summary>
    protected int MomentumBars = 1;

    /// <summary>Giorno escluso, letto sull'orologio dell'orario: 0 = lunedi' … 4 = venerdi'; -1 = nessuno.</summary>
    protected int SkipDay = -1;

    private SessionClock? _scheduleClockInstance;
    private InstrumentClock _scheduleClockResolved;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <inheritdoc />
    public override int RequiredCandles => Math.Max(base.RequiredCandles, MomentumMode != 0 ? MomentumBars + 1 : 0);

    /// <summary>
    /// L'orologio dell'orario, con il fuso dal calendario del simbolo. Si ricostruisce se
    /// <see cref="ScheduleClock"/> cambia dopo la prima lettura: la cache viaggia con i campi della
    /// strategia, e una letta prima di <c>Initialize</c> sarebbe quella sbagliata.
    /// </summary>
    private SessionClock LocalClock
    {
        get
        {
            if (_scheduleClockInstance is null || _scheduleClockResolved != ScheduleClock)
            {
                var calendar = MarketCalendarRegistry.Current.Get(Symbol);
                _scheduleClockInstance = new SessionClock(ScheduleClock == InstrumentClock.Exchange
                    ? calendar.ExchangeTimeZone
                    : calendar.ResearchTimeZone);
                _scheduleClockResolved = ScheduleClock;
            }

            return _scheduleClockInstance;
        }
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (MomentumMode == 0 && Direction is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(Direction), Direction,
                $"{Name}: senza MomentumMode il verso lo dichiara Direction, che deve valere 1 o 2.");
        }

        if (MomentumMode is < 0 or > 2 || MomentumBars < 1 || HoldHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MomentumMode),
                $"{Name}: MomentumMode {MomentumMode}, MomentumBars {MomentumBars}, HoldHours {HoldHours} non validi.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        if (CurrentMP != 0)
            return Hold(bar.Close, barTime);

        // L'apertura della barra su cui si entrerebbe, proiettata sul timeframe dichiarato.
        var entryOpen = barTime.AddMinutes(TimeframeMinutes);
        if (LocalClock.TimeOfDay(entryOpen) != EntryTime || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // La barra pianificata deve esistere: in un giorno di sessione e dentro la finestra di
        // negoziazione del simbolo. Il giorno da solo non basta — la domenica e' una sessione di NQ,
        // ma solo dalla sera — e un ordine su una barra che non c'e' aprirebbe alla prima vera.
        // Si controlla solo qui, una volta al giorno, non a ogni barra.
        if (Grid.IsSessionDay(Grid.SessionDayOf(entryOpen)) == false ||
            SessionMask.For(Symbol).Overlaps(entryOpen, TimeframeMinutes) == false)
        {
            return Hold(bar.Close, barTime);
        }

        if (SkipDay >= 0 && ((int)LocalClock.SessionDay(entryOpen).DayOfWeek + 6) % 7 == SkipDay)
            return Hold(bar.Close, barTime);

        var side = ResolveSide(data);
        if (side is null)
            return Hold(bar.Close, barTime);

        var signal = WithSessionExit(EntryMarketNextBar(side.Value, bar.Close, data, barTime,
            side == SignalType.Buy ? "LE HOD" : "SE HOD"));
        if (signal is null)
            return Hold(bar.Close, barTime);

        if (HoldHours > 0)
        {
            // Tenuta in ora locale: l'orario di uscita resta quello anche la notte del cambio d'ora.
            var exit = LocalClock.ToUtc(LocalClock.ToSessionTime(entryOpen).AddHours(HoldHours));
            if (signal.CloseAtUtc is not { } earlier || exit < earlier)
                signal.CloseAtUtc = exit;
        }

        return signal;
    }

    private SignalType? ResolveSide(OhlcvData[] data)
    {
        if (MomentumMode == 0)
            return Direction == 1 ? SignalType.Buy : SignalType.Sell;

        var move = data[^1].Close - data[^(MomentumBars + 1)].Close;
        if (move == 0m)
            return null;

        var up = move > 0m;
        var side = (MomentumMode == 1) == up ? SignalType.Buy : SignalType.Sell;

        return Direction switch
        {
            1 when side != SignalType.Buy => null,
            2 when side != SignalType.Sell => null,
            _ => side
        };
    }
}
