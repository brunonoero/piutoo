using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// BIASW della ricerca PT5DAV: ciclo settimanale a giorno e ora fissi. Entra a mercato un certo
/// giorno a una certa ora e esce un altro giorno a un'altra ora; tiene la posizione oltre la
/// sessione per costruzione.
///
/// <para><b>Orari, misurati sui trade delle cinque BIASW</b> (<c>entrata_inizio = 1</c> in tutte):</para>
/// <list type="bullet">
///   <item><b>ingresso</b> all'apertura della barra che <b>apre</b> a <c>le_time</c> del giorno
///   <c>le_day</c> (pandas, 0 = lunedi'), con i pattern letti alla chiusura della barra prima.
///   NQ-4H: lunedi' 04:00, CL-30M: mercoledi' 07:00, 676 e 632 ingressi tutti li'. La scheda scrive
///   una barra prima ("MARKET alle 00:00, apertura della barra che chiude alle 04:00"): vale il
///   trade;</item>
///   <item><b>uscita</b> alla chiusura della barra che <b>termina</b> a <c>lx_time</c> del giorno
///   <c>lx_day</c>: NQ-4H giovedi' alla chiusura della 00:00-04:00, CL-30M venerdi' alla chiusura
///   della 22:30-23:00, ES-15M giovedi' alla chiusura della 00:45-01:00.</item>
/// </list>
///
/// <para><b>Cosa non si replica.</b> La ricerca salta la settimana se la barra d'ingresso non esiste
/// (festivo) e rinvia l'uscita di una settimana se manca quella d'uscita. Qui l'ingresso resta un
/// market "next bar", che il motore esegue sulla prima barra vera, e l'uscita e' una deadline che
/// scatta al primo prezzo utile: nei festivi i due comportamenti divergono.</para>
/// </summary>
public abstract class Pt5DavBiasWeeklyEngine : Pt5DavEngineBase
{
    /// <summary><c>le_day</c>, pandas 0 = lunedi'; -1 = long spento.</summary>
    protected int EntryDayLong = -1;

    /// <summary><c>le_time</c>: apertura della barra d'ingresso long.</summary>
    protected TimeOnly EntryTimeLong;

    /// <summary><c>lx_day</c>.</summary>
    protected int ExitDayLong;

    /// <summary><c>lx_time</c>: chiusura della barra d'uscita long.</summary>
    protected TimeOnly ExitTimeLong;

    /// <summary><c>se_day</c>; -1 = short spento.</summary>
    protected int EntryDayShort = -1;

    /// <summary><c>se_time</c>.</summary>
    protected TimeOnly EntryTimeShort;

    /// <summary><c>sx_day</c>.</summary>
    protected int ExitDayShort;

    /// <summary><c>sx_time</c>.</summary>
    protected TimeOnly ExitTimeShort;

    /// <summary><c>ptn_ly_yes</c>/<c>ptn_ly_no</c>/<c>ptn_sy_yes</c>/<c>ptn_sy_no</c>, libreria fast.</summary>
    protected int PatternLongYes = 152, PatternLongNo = 153, PatternShortYes = 152, PatternShortNo = 153;

    protected Pt5DavBiasWeeklyEngine()
    {
        IntradayOnly = false;
    }

    protected override bool ApplyResearchParameter(string key, object value)
    {
        switch (key)
        {
            case "le_day": EntryDayLong = ResearchInt(value); return true;
            case "le_time": EntryTimeLong = TimeFromLegacyHhmm(ResearchInt(value)); return true;
            case "lx_day": ExitDayLong = ResearchInt(value); return true;
            case "lx_time": ExitTimeLong = TimeFromLegacyHhmm(ResearchInt(value)); return true;
            case "se_day": EntryDayShort = ResearchInt(value); return true;
            case "se_time": EntryTimeShort = TimeFromLegacyHhmm(ResearchInt(value)); return true;
            case "sx_day": ExitDayShort = ResearchInt(value); return true;
            case "sx_time": ExitTimeShort = TimeFromLegacyHhmm(ResearchInt(value)); return true;
            case "ptn_ly_yes": PatternLongYes = ResearchInt(value); return true;
            case "ptn_ly_no": PatternLongNo = ResearchInt(value); return true;
            case "ptn_sy_yes": PatternShortYes = ResearchInt(value); return true;
            case "ptn_sy_no": PatternShortNo = ResearchInt(value); return true;
            // La settimana della ricerca parte sempre dall'inizio della barra (entrata_inizio = 1).
            case "entrata_inizio": return ResearchInt(value) == 1;
            // Il motore non ha finestra oraria ne' tenuta variabile: la consegna le lascia vuote.
            case "start_hour" or "end_hour" or "intraday_only" or "max_bars": return false;            default: return base.ApplyResearchParameter(key, value);
        }
    }

    protected override TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc)
    {
        var barTime = bar.DateTime;
        var nextBar = EasyLib.EstimateNextBarUtc(data, barTime, TimeframeMinutes);
        var entries = new List<TradeSignal>(2);

        if (IsScheduled(nextBar, EntryDayLong, EntryTimeLong) &&
            EasyLib.PatternFast(PatternLongYes, ohlc) && !EasyLib.PatternFast(PatternLongNo, ohlc))
        {
            AddEntry(entries, WithScheduledExit(Finish(
                EntryMarketNextBar(SignalType.Buy, bar.Close, data, barTime, "LE BIASW"), oneEntryPerSessionPerSide: false),
                ExitDayLong, ExitTimeLong));
        }

        if (IsScheduled(nextBar, EntryDayShort, EntryTimeShort) &&
            EasyLib.PatternFast(PatternShortYes, ohlc) && !EasyLib.PatternFast(PatternShortNo, ohlc))
        {
            AddEntry(entries, WithScheduledExit(Finish(
                EntryMarketNextBar(SignalType.Sell, bar.Close, data, barTime, "SE BIASW"), oneEntryPerSessionPerSide: false),
                ExitDayShort, ExitTimeShort));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>La barra che apre in <paramref name="barOpenUtc"/> e' quella del giorno e dell'ora d'ingresso.</summary>
    private bool IsScheduled(DateTime barOpenUtc, int day, TimeOnly time) =>
        day >= 0 &&
        ((int)Clock.SessionDay(barOpenUtc).DayOfWeek + 6) % 7 == day &&
        Clock.TimeOfDay(barOpenUtc) == time;

    /// <summary>
    /// L'uscita alla chiusura della barra che termina a <paramref name="time"/> del giorno
    /// <paramref name="day"/>, la prima dopo l'ingresso.
    /// </summary>
    private TradeSignal? WithScheduledExit(TradeSignal? signal, int day, TimeOnly time)
    {
        if (signal is null)
            return null;

        var fill = signal.ValidFromUtc!.Value;
        var candidate = Clock.SessionDay(fill);
        for (var step = 0; step < 15; step++, candidate = candidate.AddDays(1))
        {
            if (DaysSinceMonday(candidate) != day)
                continue;

            var exitUtc = Clock.ToUtc(candidate.Add(time.ToTimeSpan()));
            if (exitUtc > fill.AddMinutes(TimeframeMinutes))
            {
                signal.CloseAtUtc = exitUtc.AddMinutes(-1);
                return signal;
            }
        }

        return signal;
    }
}
