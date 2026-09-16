using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT2Strategies;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le due letture non ovvie del dossier di <c>run-engine-v2</c>, provate sul motore invece che
/// dichiarate nel commento della classe (vedi <c>docs/domini/porting-da-report-sweep.md</c>
/// §"Il dossier di run-engine-v2").
///
/// <para><b>L'etichetta.</b> I run di <c>run-engine-v2</c> etichettano le barre all'<b>inizio</b>
/// (§2.6 del dossier), al contrario di quelli delle <c>PTS_*</c>: ogni PT2 dichiara
/// <c>ResearchLabelsBarsOnOpen</c> e riporta gli orari verbatim, e l'engine li confronta con
/// l'apertura.</para>
///
/// <para><b>BIASW.</b> «MARKET alle 08:00 di lunedì (apertura della barra da 60 minuti che chiude
/// alle 09:00)» e' <c>le_time = 08:00</c>, la barra 08:00-09:00; il segnale nasce sulla barra prima.
/// Ingresso e uscita sono sulla stessa barra della settimana, quindi l'uscita e' la stessa barra del
/// lunedì <i>successivo</i>.</para>
///
/// <para><b>PC.</b> <c>ResearchHours(12, 16)</c> verbatim, letta sull'apertura: sulla 4h ancorata a
/// mezzanotte entrano le barre 12:00-16:00 e 16:00-20:00 di Roma, non quelle che <i>chiudono</i> fra
/// le 12:00 e le 16:00 come sarebbe per una PTS.</para>
///
/// <para>Le date sono di settembre 2025, ora legale europea: Roma = UTC+2.</para>
/// </summary>
public sealed class Pt2ScheduleAndWindowTests
{
    // Lunedì 8 settembre 2025. La barra 08:00-09:00 di Roma apre alle 06:00 UTC.
    private static readonly DateTime MondayEntryBarOpenUtc = new(2025, 9, 8, 6, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(typeof(PT2_FDAX_BSW_001_60))]
    [InlineData(typeof(PT2_NQ_PCH_001_240))]
    [InlineData(typeof(PT2_FDAX_PCH_001_240))]
    [InlineData(typeof(PT2_NQ_PCH_002_30))]
    public void EveryPt2DeclaresOpenLabelledBars(Type type)
    {
        var strategy = (Easy.Engines.EasyEngineBase)Activator.CreateInstance(type)!;
        Assert.True(strategy.ResearchLabelsBarsOnOpen,
            $"{type.Name}: viene da run-engine-v2, che etichetta le barre all'inizio (§2.6), e deve dichiararlo.");
    }

    [Fact]
    public void BiasWeekly_EntersOnTheBarLabelled0800_SignalBornOnTheBarBefore()
    {
        var strategy = new PT2_FDAX_BSW_001_60();
        var bars = HourlyBarsUntil(MondayEntryBarOpenUtc.AddHours(-1));

        var signal = Evaluate(strategy, bars, "FDAX");

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(MondayEntryBarOpenUtc, signal.ValidFromUtc);
        Assert.Equal(44700m, signal.StopLossMoneyPerFutureContract);
        Assert.Null(signal.TakeProfitMoneyPerFutureContract);
        Assert.Null(signal.CompanionSignals);
    }

    /// <summary>
    /// Il rollover settimanale: uscita e ingresso cadono sulla stessa barra, quindi la posizione
    /// della settimana prima e' ancora aperta quando nasce il segnale (sulla barra precedente). La
    /// ricerca esce e rientra alla stessa apertura, un trade a settimana: il segnale deve nascere
    /// anche in posizione. Senza, la strategia entrava una settimana si' e una no (84 trade contro
    /// 166 della scheda).
    /// </summary>
    [Fact]
    public void BiasWeekly_RollsOverWhileStillInPosition()
    {
        var strategy = new PT2_FDAX_BSW_001_60();
        var bars = HourlyBarsUntil(MondayEntryBarOpenUtc.AddHours(-1));

        var signal = Evaluate(strategy, bars, "FDAX", new StrategyPositionSnapshot
        {
            Direction = SignalType.Buy,
            EntryPrice = bars[0].Open,
            EntryTimeUtc = MondayEntryBarOpenUtc.AddDays(-7),
            Contracts = 1
        });

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(MondayEntryBarOpenUtc, signal.ValidFromUtc);
        Assert.Equal(MondayEntryBarOpenUtc.AddDays(7), signal.CloseAtUtc);
    }

    /// <summary>
    /// Il rollover vale solo quando uscita e ingresso coincidono: una BIASW con uscita su un'altra
    /// barra (ES: ingresso lunedì 02:00, uscita lunedì 01:00) in posizione resta ferma.
    /// </summary>
    [Fact]
    public void BiasWeekly_WithADifferentExitBarDoesNotEnterWhileInPosition()
    {
        var strategy = new PiutooStrategies.PTS_ES_BSW_001_60();
        strategy.Initialize(new Dictionary<string, object> { ["PtnLyYes"] = 152, ["PtnLyNo"] = 153 });
        // Barra 00:00-01:00 Roma di lunedì 8/9/2025 = 22:00Z di domenica: la prossima e' la 02:00 (etichetta di chiusura).
        var signalBar = new DateTime(2025, 9, 7, 22, 0, 0, DateTimeKind.Utc);
        var bars = BarsUntil(new DateTime(2025, 8, 24, 22, 0, 0, DateTimeKind.Utc), signalBar, 60, weekdaysOnly: false);

        var flat = Evaluate(strategy, bars, "ES");
        var inPosition = Evaluate(strategy, bars, "ES", new StrategyPositionSnapshot
        {
            Direction = SignalType.Buy, EntryPrice = bars[0].Open, EntryTimeUtc = signalBar.AddDays(-7), Contracts = 1
        });

        Assert.Equal(SignalType.Buy, flat.Type);
        Assert.Equal(SignalType.Hold, inPosition.Type);
    }

    [Fact]
    public void BiasWeekly_ExitIsTheSameBarOfTheFollowingMonday()
    {
        var strategy = new PT2_FDAX_BSW_001_60();
        var bars = HourlyBarsUntil(MondayEntryBarOpenUtc.AddHours(-1));

        var signal = Evaluate(strategy, bars, "FDAX");

        // Stessa barra (etichetta 09:00 di lunedì) sette giorni dopo: la posizione dura una
        // settimana, non un'ora. La deadline e' l'apertura di quella barra.
        Assert.Equal(MondayEntryBarOpenUtc.AddDays(7), signal.CloseAtUtc);
    }

    /// <summary>
    /// Il segnale nasce su UNA barra sola, quella che precede la 08:00-09:00 di Roma. Sulle vicine
    /// non nasce: una barra prima punterebbe alla 07:00-08:00 (quella che, letta con l'etichetta di
    /// chiusura come una PTS, sarebbe la «08:00»); sulla barra pianificata stessa, e su quella dopo,
    /// la barra successiva apre alle 09:00 o alle 10:00.
    /// </summary>
    [Theory]
    [InlineData(-1)] // segnale sulla barra 06:00-07:00 Roma: la prossima apre alle 07:00
    [InlineData(1)]  // sulla barra pianificata stessa: la prossima apre alle 09:00
    [InlineData(2)]  // sulla barra dopo: la prossima apre alle 10:00
    public void BiasWeekly_DoesNotEnterOnNeighbouringBars(int hoursFromSignalBar)
    {
        var strategy = new PT2_FDAX_BSW_001_60();
        var bars = HourlyBarsUntil(MondayEntryBarOpenUtc.AddHours(-1 + hoursFromSignalBar));

        var signal = Evaluate(strategy, bars, "FDAX");

        Assert.Equal(SignalType.Hold, signal.Type);
    }

    [Theory]
    [InlineData(10, true)]  // 12:00-16:00 Roma, apre alle 12:00: dentro
    [InlineData(14, true)]  // 16:00-20:00 Roma, apre alle 16:00: dentro (estremo incluso)
    [InlineData(6, false)]  // 08:00-12:00 Roma, apre alle 08:00: fuori, anche se CHIUDE dentro la finestra (sarebbe dentro per una PTS)
    [InlineData(18, false)] // 20:00-00:00 Roma, apre alle 20:00: fuori
    public void PriceChannel_WindowIsReadOnTheOpeningLabel(int signalBarOpenHourUtc, bool expectsEntry)
    {
        var strategy = new PT2_NQ_PCH_001_240();
        var signalBar = new DateTime(2025, 9, 9, signalBarOpenHourUtc, 0, 0, DateTimeKind.Utc); // martedì
        var bars = FourHourBarsUntil(signalBar);

        var signal = Evaluate(strategy, bars, "NQ");

        if (!expectsEntry)
        {
            Assert.Equal(SignalType.Hold, signal.Type);
            return;
        }

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(bars[^1].High, signal.Price);            // canale a 1 barra, nessun offset
        Assert.Equal(signalBar.AddHours(4), signal.ValidFromUtc);
        Assert.Equal(5, signal.MaxBarsInPosition);
        Assert.Equal(4595m, signal.StopLossMoneyPerFutureContract);
        Assert.Null(signal.TakeProfitMoneyPerFutureContract);
        Assert.Null(signal.CloseAtUtc);                        // multiday: nessuna chiusura di sessione

        var sell = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Sell, sell.Type);
        Assert.Equal(bars[^1].Low, sell.Price);
    }

    [Fact]
    public void PriceChannel_Fdax_IsIntradayWithNeutral7Forbidden()
    {
        var strategy = new PT2_FDAX_PCH_001_240();
        Assert.Equal(StrategyHolding.Intraday, strategy.Holding);

        // Barra 09:00-13:00 di Roma (07:00 UTC): la sessione FDAX e' ancorata alle 01:00 di Roma,
        // quindi la chiusura di fine sessione e' all'ultimo minuto prima delle 01:00 del giorno dopo.
        var signalBar = new DateTime(2025, 9, 9, 7, 0, 0, DateTimeKind.Utc);
        var bars = FourHourBarsUntil(signalBar, anchorHourUtc: 23);

        var signal = Evaluate(strategy, bars, "FDAX");

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(bars[^1].High + 10m, signal.Price);       // + 10 tick da 1 punto
        Assert.Equal(new DateTime(2025, 9, 9, 22, 59, 0, DateTimeKind.Utc), signal.CloseAtUtc);
        Assert.Equal(3575m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(5800m, signal.TakeProfitMoneyPerFutureContract);
        Assert.Null(signal.MaxBarsInPosition);

        var sell = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(bars[^1].Low - 10m, sell.Price);
    }

    [Fact]
    public void PriceChannel_Nq30_IsLongOnlyWith46BarsTimeExit()
    {
        var strategy = new PT2_NQ_PCH_002_30();
        Assert.Equal(StrategyHolding.Multiday, strategy.Holding);

        var signalBar = new DateTime(2025, 9, 9, 12, 0, 0, DateTimeKind.Utc);
        var bars = HalfHourBarsUntil(signalBar);

        var signal = Evaluate(strategy, bars, "NQ");

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Null(signal.CompanionSignals);                  // solo long
        Assert.Equal(bars[^10..].Max(bar => bar.High), signal.Price); // canale a 10 barre
        Assert.Equal(46, signal.MaxBarsInPosition);
        Assert.Equal(1045m, signal.StopLossMoneyPerFutureContract);
        Assert.Null(signal.TakeProfitMoneyPerFutureContract);
    }

    // ------------------------------------------------------------------ helper

    private static TradeSignal Evaluate(
        Easy.Engines.EasyEngineBase strategy, OhlcvData[] bars, string symbol, StrategyPositionSnapshot? position = null) =>
        strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = symbol,
                BarTimeUtc = bars[^1].DateTime,
                Position = position,
                EntriesToday = 0
            }
        });

    /// <summary>Barre orarie dal lunedì 25/08/2025, solo lun-ven, fino alla barra indicata inclusa.</summary>
    private static OhlcvData[] HourlyBarsUntil(DateTime lastBarOpenUtc) =>
        BarsUntil(new DateTime(2025, 8, 25, 0, 0, 0, DateTimeKind.Utc), lastBarOpenUtc, 60, weekdaysOnly: true);

    /// <summary>
    /// Barre da 4 ore allineate alla griglia della ricerca (00, 04, 08, ... di Roma, cioe' 22, 02,
    /// 06, ... UTC in ora legale), dal 24/08/2025, tutti i giorni, fino alla barra indicata inclusa.
    /// Con <paramref name="anchorHourUtc"/> = 23 la griglia e' quella ancorata all'01:00 di Roma (FDAX).
    /// </summary>
    private static OhlcvData[] FourHourBarsUntil(DateTime lastBarOpenUtc, int anchorHourUtc = 22) =>
        BarsUntil(new DateTime(2025, 8, 24, anchorHourUtc, 0, 0, DateTimeKind.Utc), lastBarOpenUtc, 240, weekdaysOnly: false);

    private static OhlcvData[] HalfHourBarsUntil(DateTime lastBarOpenUtc) =>
        BarsUntil(new DateTime(2025, 8, 31, 22, 0, 0, DateTimeKind.Utc), lastBarOpenUtc, 30, weekdaysOnly: false);

    private static OhlcvData[] BarsUntil(DateTime firstOpenUtc, DateTime lastOpenUtc, int minutes, bool weekdaysOnly)
    {
        var bars = new List<OhlcvData>();
        decimal price = 10_000m;
        for (var cursor = firstOpenUtc; cursor <= lastOpenUtc; cursor = cursor.AddMinutes(minutes))
        {
            if (weekdaysOnly && cursor.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            // Corpo piccolo rispetto al range: il pattern neutrale 7 (corpo > 0,75 × range) resta
            // falso su ogni sessione, cosi' il divieto di PT2_FDAX_PCH_001_240 non scatta.
            bars.Add(new OhlcvData
            {
                DateTime = cursor,
                Open = price,
                High = price + 20m,
                Low = price - 20m,
                Close = price + 1m,
                Volume = 10
            });
            price += 1m;
        }

        Assert.Equal(lastOpenUtc, bars[^1].DateTime);
        return bars.ToArray();
    }
}
