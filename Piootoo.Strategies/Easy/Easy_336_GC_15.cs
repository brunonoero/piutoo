using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_336 — breakout degli estremi della sessione corrente GC 15m, con filtro ADX.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_336_GC_15__7.txt</c>. Il canale
/// Donchian a 155 barre è usato dall'uscita dinamica, non dall'ingresso: gli ordini di ingresso
/// sono stop su <c>HighD(0)</c>/<c>LowD(0)</c>.</para>
///
/// <para><b>Non eseguibile.</b> <c>UseDonchianTrailing=1</c> aggiorna lo stop di uscita a ogni
/// barra sul canale opposto. Il contratto corrente può dichiarare solo uscite autocontenute
/// all'ingresso, quindi la strategia è correttamente close-dependent ed esclusa dal catalogo.</para>
/// </summary>
public sealed class Easy_336_GC_15 : PriceChannelEngine
{
    private string _symbol = "@GC";
    private int _timeframeMinutes = 15;

    public Easy_336_GC_15()
    {
        UseLegacyVariant = true;
        SessionStartTime = 1800;
        SessionEndTime = 1700;
        Contracts = 1;

        ChannelBars = 155;  // MyLenght, canale usato dal trail
        UseCurrentSessionExtremesForEntries = true;
        UseDonchianTrailing = true;

        StartTime = 300;   // MyStartTrade
        EndTime = 1545;    // MyEndTrade
        TradingWindowInclusive = false;
        AdxLength = 5;     // ADXLenght
        AdxThreshold = 50; // MyADXLimit
        DailyFactorValue = 0.40m; // DF
        NotEntryDayShort = 5;     // MyNoShortDay
        StopMoney = 2500;
        ProfitMoney = 4300;
    }

    public override string Name => "TOP_UA_336";
    public override string Description => "Breakout HighD/LowD con ADX e trail Donchian, GC 15m";
    public override string Symbol => _symbol;
    public override int TimeframeMinutes => _timeframeMinutes;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Symbol", out var symbol)) _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("TimeframeMinutes", out var timeframe)) _timeframeMinutes = Convert.ToInt32(timeframe);
        if (parameters.TryGetValue("ADXLenght", out var adxLength)) AdxLength = Convert.ToInt32(adxLength);
        if (parameters.TryGetValue("MyADXLimit", out var adxLimit)) AdxThreshold = Convert.ToDecimal(adxLimit);
        if (parameters.TryGetValue("MyLenght", out var length)) ChannelBars = Convert.ToInt32(length);
        if (parameters.TryGetValue("UseDonchianTrailing", out var trail)) UseDonchianTrailing = Convert.ToInt32(trail) == 1;
        if (parameters.TryGetValue("MyStartTrade", out var start)) StartTime = Convert.ToInt32(start);
        if (parameters.TryGetValue("MyEndTrade", out var end)) EndTime = Convert.ToInt32(end);
        if (parameters.TryGetValue("MyStop", out var stop)) StopMoney = Convert.ToInt32(stop);
        if (parameters.TryGetValue("MyProfit", out var profit)) ProfitMoney = Convert.ToInt32(profit);
        if (parameters.TryGetValue("MyNoShortDay", out var noShortDay)) NotEntryDayShort = Convert.ToInt32(noShortDay);
        if (parameters.TryGetValue("DF", out var dailyFactor)) DailyFactorValue = Convert.ToDecimal(dailyFactor);
        if (parameters.TryGetValue("mycontracts", out var contracts)) Contracts = Convert.ToInt32(contracts);
    }
}
