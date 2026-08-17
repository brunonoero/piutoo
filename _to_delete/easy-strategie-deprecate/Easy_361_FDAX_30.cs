using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_361 — Price Channel FDAX 30m con pattern, ADX daily e scadenza multiday.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_361_FDAX_30__7.txt</c>. L'ADX viene
/// aggiornato una sola volta alla nuova sessione 08:00–22:00 sui valori d1/d2 di
/// <c>_ohlcmulti5</c>; non è l'ATR-ratio della vecchia traduzione.</para>
/// </summary>
public sealed class Easy_361_FDAX_30 : PriceChannelEngine
{
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 30;

    public Easy_361_FDAX_30()
    {
        UseLegacyVariant = true;
        SessionStartTime = 800;
        SessionEndTime = 2200;
        Contracts = 1;
        ChannelBars = 20;

        StartTime = 1400;
        EndTime = 2100;
        TradingWindowInclusive = false; // tw(): fine esclusiva
        PauseStart = 1200;
        PauseEnd = 1200;
        NeutralYes = 3;
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 8;

        MaxEntriesPerSession = 2;
        AdxLength = 5;
        AdxThreshold = 90;
        UseSessionAdx = true;
        MaxDaysInTrade = 3;
        MaxDaysFlatTime = 2130;
        StopMoney = 1800;
        BreakEvenMoney = 0;
        ProfitMoney = 4800;
    }

    public override string Name => "TOP_UA_361";
    public override string Description => "Price Channel con ADX sessionale, FDAX 30m";
    public override string Symbol => _symbol;
    public override int TimeframeMinutes => _timeframeMinutes;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Symbol", out var symbol)) _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("TimeframeMinutes", out var timeframe)) _timeframeMinutes = Convert.ToInt32(timeframe);
        if (parameters.TryGetValue("MyContracts", out var contracts)) Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("SessionStartTimeA", out var sessionStart)) SessionStartTime = Convert.ToInt32(sessionStart);
        if (parameters.TryGetValue("SessionEndTimeA", out var sessionEnd)) SessionEndTime = Convert.ToInt32(sessionEnd);
        if (parameters.TryGetValue("MyStartTime", out var start)) StartTime = Convert.ToInt32(start);
        if (parameters.TryGetValue("MyEndTime", out var end)) EndTime = Convert.ToInt32(end);
        if (parameters.TryGetValue("MyStartPause", out var pauseStart)) PauseStart = Convert.ToInt32(pauseStart);
        if (parameters.TryGetValue("MyEndPause", out var pauseEnd)) PauseEnd = Convert.ToInt32(pauseEnd);
        if (parameters.TryGetValue("PtnNeutYes", out var neutralYes)) NeutralYes = Convert.ToInt32(neutralYes);
        if (parameters.TryGetValue("PtnNeutNo", out var neutralNo)) NeutralNo = Convert.ToInt32(neutralNo);
        if (parameters.TryGetValue("PtnDirYes", out var directionalYes)) DirectionalYes = Convert.ToInt32(directionalYes);
        if (parameters.TryGetValue("PtnDirNo", out var directionalNo)) DirectionalNo = Convert.ToInt32(directionalNo);
        if (parameters.TryGetValue("MaxEntriesPerDay", out var entries)) MaxEntriesPerSession = Convert.ToInt32(entries);
        if (parameters.TryGetValue("ADX_TH", out var adxThreshold)) AdxThreshold = Convert.ToDecimal(adxThreshold);
        if (parameters.TryGetValue("nBars", out var channelBars)) ChannelBars = Convert.ToInt32(channelBars);
        if (parameters.TryGetValue("MaxDaysInTrade", out var maxDays)) MaxDaysInTrade = Convert.ToInt32(maxDays);
        if (parameters.TryGetValue("FlatTime", out var flatTime)) MaxDaysFlatTime = Convert.ToInt32(flatTime);
        if (parameters.TryGetValue("MyStop", out var stop)) StopMoney = Convert.ToInt32(stop);
        if (parameters.TryGetValue("MyBreakEven", out var breakEven)) BreakEvenMoney = Convert.ToInt32(breakEven);
        if (parameters.TryGetValue("MyProfit", out var profit)) ProfitMoney = Convert.ToInt32(profit);
    }
}

