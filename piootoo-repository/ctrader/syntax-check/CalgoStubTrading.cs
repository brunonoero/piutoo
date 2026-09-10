// Seconda meta' dello stub cAlgo: la superficie di ESECUZIONE — posizioni, ordini, storico,
// grafico — che serve a PiootooDistributedExecutionBot e che il raccoglitore non tocca.
//
// Vale la stessa regola di CalgoStub.cs: le firme si copiano da cAlgo, non si inventano per far
// passare la build. Uno stub piu' permissivo del compilatore vero fa passare qui codice che dentro
// cTrader non compila, cioe' e' peggio di non avere lo stub. Nessun metodo fa niente.
using System;
using System.Collections;
using System.Collections.Generic;

namespace cAlgo.API
{
    public enum TradeType { Buy, Sell }

    public enum RoundingMode { Down, Up, ToNearest }

    public enum PositionCloseReason { Closed, StopLoss, TakeProfit, StopOut }

    public enum ErrorCode
    {
        TechnicalError, BadVolume, NoMoney, MarketClosed, Disconnected,
        EntityNotFound, Timeout, UnableToTrade, InvalidStopLossTakeProfit, InvalidRequest
    }

    public enum VerticalAlignment { Top, Center, Bottom }

    public enum HorizontalAlignment { Left, Center, Right }

    public struct Color
    {
        public static Color Red { get { return default(Color); } }
        public static Color LightGreen { get { return default(Color); } }
        public static Color OrangeRed { get { return default(Color); } }
    }

    public sealed class Position
    {
        public int Id { get { return 0; } }
        public string Label { get { return null; } }
        public string SymbolName { get { return null; } }
        public TradeType TradeType { get { return TradeType.Buy; } }
        public double EntryPrice { get { return 0; } }
        public double? StopLoss { get { return null; } }
        public double? TakeProfit { get { return null; } }
        public double VolumeInUnits { get { return 0; } }
        public double Quantity { get { return 0; } }
        public double GrossProfit { get { return 0; } }
        public double NetProfit { get { return 0; } }
        public double Swap { get { return 0; } }
        public double Commissions { get { return 0; } }
        public DateTime EntryTime { get { return default(DateTime); } }
    }

    public sealed class PositionOpenedEventArgs
    {
        public Position Position { get { return null; } }
    }

    public sealed class PositionClosedEventArgs
    {
        public Position Position { get { return null; } }
        public PositionCloseReason Reason { get { return PositionCloseReason.Closed; } }
    }

    public sealed class Positions : IEnumerable<Position>
    {
        public int Count { get { return 0; } }
        public Position this[int index] { get { return null; } }
        public IEnumerator<Position> GetEnumerator() { return null; }
        IEnumerator IEnumerable.GetEnumerator() { return null; }
        public event Action<PositionOpenedEventArgs> Opened;
        public event Action<PositionClosedEventArgs> Closed;
    }

    public sealed class PendingOrder
    {
        public int Id { get { return 0; } }
        public string Label { get { return null; } }
        public string SymbolName { get { return null; } }
        public TradeType TradeType { get { return TradeType.Buy; } }
        public double TargetPrice { get { return 0; } }
        public double VolumeInUnits { get { return 0; } }
        public DateTime? ExpirationTime { get { return null; } }
    }

    public sealed class PendingOrders : IEnumerable<PendingOrder>
    {
        public int Count { get { return 0; } }
        public PendingOrder this[int index] { get { return null; } }
        public IEnumerator<PendingOrder> GetEnumerator() { return null; }
        IEnumerator IEnumerable.GetEnumerator() { return null; }
    }

    public sealed class HistoricalTrade
    {
        public int PositionId { get { return 0; } }
        public string Label { get { return null; } }
        public string SymbolName { get { return null; } }
        public TradeType TradeType { get { return TradeType.Buy; } }
        public double EntryPrice { get { return 0; } }
        public double ClosingPrice { get { return 0; } }
        public DateTime EntryTime { get { return default(DateTime); } }
        public DateTime ClosingTime { get { return default(DateTime); } }
        public double VolumeInUnits { get { return 0; } }
        public double GrossProfit { get { return 0; } }
        public double NetProfit { get { return 0; } }
        public double Commissions { get { return 0; } }
        public double Swap { get { return 0; } }
    }

    public sealed class History : IEnumerable<HistoricalTrade>
    {
        public int Count { get { return 0; } }
        public HistoricalTrade this[int index] { get { return null; } }
        public IEnumerator<HistoricalTrade> GetEnumerator() { return null; }
        IEnumerator IEnumerable.GetEnumerator() { return null; }
    }

    public sealed class TradeResult
    {
        public bool IsSuccessful { get { return false; } }
        public Position Position { get { return null; } }
        public PendingOrder PendingOrder { get { return null; } }
        public ErrorCode? Error { get { return null; } }
    }

    public sealed class Chart
    {
        public void DrawStaticText(string name, string text,
            VerticalAlignment verticalAlignment, HorizontalAlignment horizontalAlignment, Color color) { }
    }
}
