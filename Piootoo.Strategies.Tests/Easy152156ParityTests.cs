using System;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy;
using Xunit;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Tests;

public class EasySessionParityTests
{
    [Fact]
    public void OHLCMulti5_OvernightSession_BuildsFivePriorSessions()
    {
        var bars = BuildOvernightSessions(
            sessionCount: 6,
            barsPerSession: 4,
            start: new DateTime(2024, 1, 1, 17, 5, 0, DateTimeKind.Utc));

        var last = bars[^1].DateTime;
        OHLCMulti5(1700, 1559, bars, last, out var ohlc);

        Assert.True(ohlc[1] > 0);
        Assert.True(ohlc[2] > 0);
        Assert.True(ohlc[5] > 0); // highd1
        Assert.True(ohlc[6] > 0); // lowd1

        // highd0 deve essere il max della sola sessione corrente (non di tutto lo storico)
        var currentSessionStart = bars
            .Select((b, i) => (b, i))
            .Last(x =>
            {
                if (x.i == 0) return true;
                var t = GetHhmm(x.b.DateTime);
                var prevT = GetHhmm(bars[x.i - 1].DateTime);
                return t > 1700 && prevT <= 1700;
            }).i;

        var currentSessionHigh = bars.Skip(currentSessionStart).Where(InOvernightSession).Max(b => b.High);
        Assert.Equal(currentSessionHigh, ohlc[1]);

        // Deve attraversare mezzanotte: barre di almeno due date di calendario nello storico
        Assert.True(bars.Select(b => b.DateTime.Date).Distinct().Count() >= 2);
    }

    [Fact]
    public void PatternNeutralFast_23_UsesFiveSessionRange()
    {
        // body5d = |opend5 - closed1| = 1
        // range5d = 100 → body5d < 0.1 * range5d = true
        var ohlc = new decimal[24];
        ohlc[4] = 10; ohlc[5] = 20; ohlc[6] = 5; ohlc[7] = 11; // d1
        ohlc[8] = 10; ohlc[9] = 50; ohlc[10] = 5; ohlc[11] = 10; // d2
        ohlc[12] = 10; ohlc[13] = 50; ohlc[14] = 5; ohlc[15] = 10; // d3
        ohlc[16] = 10; ohlc[17] = 50; ohlc[18] = 5; ohlc[19] = 10; // d4
        ohlc[20] = 12; ohlc[21] = 105; ohlc[22] = 5; ohlc[23] = 10; // d5 high makes range5d=100

        Assert.True(PatternNeutralFast(23, ohlc));

        // body5d grande rispetto a range5d → false
        ohlc[20] = 200;
        Assert.False(PatternNeutralFast(23, ohlc));
    }

    [Fact]
    public void PatternNeutralFast_52_InsideOutsideSession()
    {
        var ohlc = new decimal[24];
        ohlc[1] = 120; ohlc[2] = 80; // d0
        ohlc[5] = 110; ohlc[6] = 90; // d1
        Assert.True(PatternNeutralFast(52, ohlc));

        ohlc[1] = 100;
        Assert.False(PatternNeutralFast(52, ohlc));
    }

    [Fact]
    public void TimeWindow_InclusiveVsExclusive()
    {
        var at1530 = new DateTime(2024, 1, 2, 15, 30, 0, DateTimeKind.Utc);
        Assert.True(TimeWindowInclusive(930, 1530, at1530));
        Assert.False(TimeWindow(930, 1530, at1530));
    }

    private static bool InOvernightSession(OhlcvData bar)
    {
        var t = bar.DateTime.Hour * 100 + bar.DateTime.Minute;
        return t > 1700 || t <= 1559;
    }

    private static OhlcvData[] BuildOvernightSessions(int sessionCount, int barsPerSession, DateTime start)
    {
        var bars = new List<OhlcvData>();
        // Parte da una sera (es. 17:05) e costruisce sessioni 17:xx + 09:xx del giorno dopo.
        var sessionEvening = start.Date.AddHours(17).AddMinutes(5);
        decimal price = 1000m;

        for (var session = 0; session < sessionCount; session++)
        {
            var evening = sessionEvening.AddDays(session);
            var morning = evening.Date.AddDays(1).AddHours(9).AddMinutes(30);

            var half = Math.Max(1, barsPerSession / 2);
            for (var i = 0; i < half; i++)
            {
                var ts = evening.AddMinutes(5 * i);
                bars.Add(MakeBar(ts, price, session));
                price = bars[^1].Close;
            }

            for (var i = 0; i < barsPerSession - half; i++)
            {
                var ts = morning.AddMinutes(5 * i);
                bars.Add(MakeBar(ts, price, session));
                price = bars[^1].Close;
            }
        }

        return bars.OrderBy(b => b.DateTime).ToArray();
    }

    private static OhlcvData MakeBar(DateTime ts, decimal price, int session)
        => new()
        {
            DateTime = ts,
            Open = price,
            High = price + 2 + session,
            Low = price - 1,
            Close = price + 1,
            Volume = 1
        };
}

public class Easy152IntentTests
{
    [Fact]
    public void Easy152_EmitsNextBarStop_NotSameBarFill()
    {
        var strategy = new Easy_152_NQ_5();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["PtnDirYes"] = 52,
            ["PtnNeutYes"] = 55,
            ["PtnNeutNo"] = 99,
            ["PtnLY"] = 41,
            ["PtnLN"] = 42,
            ["PtnSY"] = 41,
            ["PtnSN"] = 42,
            ["MyTrigger"] = 1,
            ["mydayNolong"] = 9,
            ["mydayNoshort"] = 9
        });

        var bars = BuildTradingBarsInWindow(120, new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc), 5, 1000, 1500);
        // Forza highd0 raggiungibile: l'intent non deve comunque dipendere dal touch corrente
        bars[^1].High = bars.Max(b => b.High) + 50;

        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "NQ",
                BarTimeUtc = bars[^1].DateTime,
                EntriesToday = 0
            }
        });

        if (signal.Type == SignalType.Hold && (signal.CompanionSignals is null || signal.CompanionSignals.Count == 0))
        {
            Assert.Fail($"Expected stop intent, got Hold. Reason={signal.Reason}");
        }

        Assert.True(signal.Type is SignalType.Buy or SignalType.Sell);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.NotNull(signal.ValidFromUtc);
        Assert.Equal(bars[^1].DateTime.AddMinutes(5), signal.ValidFromUtc);
        Assert.Equal(1200m, signal.StopLossMoneyPerFutureContract);
    }

    [Fact]
    public void Easy152_UsesEngineEntriesToday()
    {
        var strategy = new Easy_152_NQ_5();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["PtnDirYes"] = 52,
            ["PtnNeutYes"] = 55,
            ["PtnNeutNo"] = 99,
            ["PtnLY"] = 41,
            ["PtnLN"] = 42,
            ["MaxTradesPerDay"] = 1,
            ["mydayNolong"] = 9,
            ["mydayNoshort"] = 9
        });

        var bars = BuildTradingBarsInWindow(120, new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc), 5, 1000, 1500);
        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "NQ",
                BarTimeUtc = bars[^1].DateTime,
                EntriesToday = 1
            }
        });

        Assert.Equal(SignalType.Hold, signal.Type);
    }

    private static OhlcvData[] BuildTradingBarsInWindow(int count, DateTime start, int minutes, int windowStart, int windowEnd)
    {
        var bars = new List<OhlcvData>();
        var cursor = start;
        decimal price = 15000m;
        while (bars.Count < count)
        {
            var t = cursor.Hour * 100 + cursor.Minute;
            if (t < windowStart || t >= windowEnd)
            {
                cursor = cursor.Date.AddDays(t >= windowEnd ? 1 : 0).AddHours(windowStart / 100).AddMinutes(windowStart % 100);
                continue;
            }

            bars.Add(new OhlcvData
            {
                DateTime = cursor,
                Open = price,
                High = price + 5,
                Low = price - 5,
                Close = price + 1,
                Volume = 10
            });
            price += 1;
            cursor = cursor.AddMinutes(minutes);
        }

        return bars.ToArray();
    }
}

public class Easy156IntentTests
{
    [Fact]
    public void Easy156_EmitsStopWithMoneyRiskAndSessionClose()
    {
        var strategy = new Easy_156_NQ_15();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["MyPtnLY"] = 152,
            ["MyPtnLN"] = 999,
            ["MyPtnSY"] = 152,
            ["MyPtnSN"] = 999
        });

        var bars = BuildTradingBarsInWindow(120, new DateTime(2024, 1, 3, 11, 0, 0, DateTimeKind.Utc), 15, 1000, 1500);
        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "NQ",
                BarTimeUtc = bars[^1].DateTime
            }
        });

        Assert.True(signal.Type is SignalType.Buy or SignalType.Sell);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(1750m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(4500m, signal.TakeProfitMoneyPerFutureContract);
        Assert.NotNull(signal.ValidFromUtc);
        Assert.Equal(bars[^1].DateTime.AddMinutes(15), signal.ValidFromUtc);
        Assert.NotNull(signal.CloseAtUtc);
    }

    private static OhlcvData[] BuildTradingBarsInWindow(int count, DateTime start, int minutes, int windowStart, int windowEnd)
    {
        var bars = new List<OhlcvData>();
        var cursor = start;
        decimal price = 15000m;
        while (bars.Count < count)
        {
            var t = cursor.Hour * 100 + cursor.Minute;
            if (t < windowStart || t >= windowEnd)
            {
                cursor = cursor.Date.AddDays(t >= windowEnd ? 1 : 0).AddHours(windowStart / 100).AddMinutes(windowStart % 100);
                continue;
            }

            bars.Add(new OhlcvData
            {
                DateTime = cursor,
                Open = price,
                High = price + 8,
                Low = price - 8,
                Close = price + 2,
                Volume = 10
            });
            price += 2;
            cursor = cursor.AddMinutes(minutes);
        }

        return bars.ToArray();
    }
}

public class EasyReplayValidationTests
{
    [Fact]
    public void Replay_Easy152_IntentsAreAlwaysNextBarStopOrHoldOrDeferredExit()
    {
        var strategy = new Easy_152_NQ_5();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["PtnDirYes"] = 52,
            ["PtnNeutYes"] = 55,
            ["PtnNeutNo"] = 99,
            ["PtnLY"] = 41,
            ["PtnLN"] = 42,
            ["PtnSY"] = 41,
            ["PtnSN"] = 42,
            ["mydayNolong"] = 9,
            ["mydayNoshort"] = 9
        });

        var bars = BuildDenseSession(200, new DateTime(2024, 1, 2, 17, 5, 0, DateTimeKind.Utc), 5);
        var log = new List<string>();

        for (var i = 100; i < bars.Length; i++)
        {
            var window = bars.Take(i + 1).ToArray();
            var bar = window[^1];
            var signal = strategy.Evaluate(new StrategyEvaluationRequest
            {
                Ohlcv = window,
                BarTimeUtc = bar.DateTime,
                Execution = new StrategyExecutionSnapshot
                {
                    StrategyCode = strategy.Name,
                    Symbol = "NQ",
                    BarTimeUtc = bar.DateTime,
                    EntriesToday = 0
                }
            });

            foreach (var intent in Expand(signal))
            {
                if (intent.Type == SignalType.Hold)
                {
                    continue;
                }

                Assert.NotNull(intent.ValidFromUtc);
                Assert.True(intent.ValidFromUtc >= bar.DateTime.AddMinutes(5).AddMinutes(-1));
                // L'uscita di fine sessione e' a mercato; gli ingressi restano ordini stop.
                var isSessionExit = intent.Reason is not null &&
                                    intent.Reason.StartsWith("EOSess", StringComparison.Ordinal);
                if (!isSessionExit)
                {
                    Assert.Equal(TradeOrderType.Stop, intent.OrderType);
                }

                log.Add($"{bar.DateTime:O}|{intent.Type}|{intent.OrderType}|{intent.Price}|{intent.ValidFromUtc:O}|{intent.Reason}");
            }
        }

        // Il replay produce un log confrontabile barra-per-barra con TradeStation.
        Assert.NotEmpty(log);
        Assert.Contains(log, line => line.Contains("Stop", StringComparison.Ordinal));
    }

    private static IEnumerable<TradeSignal> Expand(TradeSignal signal)
    {
        yield return signal;
        if (signal.CompanionSignals is null)
        {
            yield break;
        }

        foreach (var companion in signal.CompanionSignals)
        {
            yield return companion;
        }
    }

    private static OhlcvData[] BuildDenseSession(int count, DateTime start, int minutes)
    {
        var bars = new List<OhlcvData>();
        var cursor = start;
        decimal price = 16000m;
        for (var i = 0; i < count; i++)
        {
            var t = cursor.Hour * 100 + cursor.Minute;
            if (t > 1559 && t <= 1700)
            {
                cursor = cursor.Date.AddHours(17).AddMinutes(5);
                if (cursor <= bars[^1].DateTime)
                {
                    cursor = bars[^1].DateTime.AddDays(1).Date.AddHours(17).AddMinutes(5);
                }
            }

            bars.Add(new OhlcvData
            {
                DateTime = cursor,
                Open = price,
                High = price + 10,
                Low = price - 10,
                Close = price + (i % 2 == 0 ? 3 : -2),
                Volume = 5
            });
            price = bars[^1].Close;
            cursor = cursor.AddMinutes(minutes);
        }

        return bars.ToArray();
    }
}
