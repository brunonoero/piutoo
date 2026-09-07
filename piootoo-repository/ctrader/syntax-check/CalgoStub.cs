// Stub minimo dell'API cAlgo, SOLO per far compilare il sorgente del cBot fuori da cTrader e
// verificarne la sintassi e i tipi. Non e' un'emulazione: nessun metodo fa niente.
using System;
using System.Collections.Generic;

namespace cAlgo.API
{
    public enum AccessRights { None, FullAccess }
    public enum TimeZones { UTC }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RobotAttribute : Attribute
    {
        public TimeZones TimeZone { get; set; }
        public AccessRights AccessRights { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ParameterAttribute : Attribute
    {
        public ParameterAttribute() { }
        public ParameterAttribute(string name) { }
        public object DefaultValue { get; set; }
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
        public string Group { get; set; }
    }

    public sealed class TimeFrame
    {
        public static readonly TimeFrame Minute = new TimeFrame();
        public static readonly TimeFrame Minute2 = new TimeFrame();
        public static readonly TimeFrame Minute3 = new TimeFrame();
        public static readonly TimeFrame Minute5 = new TimeFrame();
        public static readonly TimeFrame Minute10 = new TimeFrame();
        public static readonly TimeFrame Minute15 = new TimeFrame();
        public static readonly TimeFrame Minute30 = new TimeFrame();
        public static readonly TimeFrame Hour = new TimeFrame();
        public static readonly TimeFrame Hour2 = new TimeFrame();
        public static readonly TimeFrame Hour4 = new TimeFrame();
        public static readonly TimeFrame Hour6 = new TimeFrame();
        public static readonly TimeFrame Hour12 = new TimeFrame();
        public static readonly TimeFrame Daily = new TimeFrame();
        public static readonly TimeFrame Weekly = new TimeFrame();
    }

    public sealed class DataSeries
    {
        public double this[int index] { get { return 0; } }
    }

    public sealed class TimeSeries
    {
        public DateTime this[int index] { get { return default(DateTime); } }
    }

    public sealed class BarOpenedEventArgs { public Bars Bars { get; set; } }

    public sealed class Bars
    {
        public int Count { get { return 0; } }
        public TimeSeries OpenTimes { get { return null; } }
        public DataSeries OpenPrices { get { return null; } }
        public DataSeries HighPrices { get { return null; } }
        public DataSeries LowPrices { get { return null; } }
        public DataSeries ClosePrices { get { return null; } }
        public DataSeries TickVolumes { get { return null; } }
        public int LoadMoreHistory() { return 0; }
        public event Action<BarOpenedEventArgs> BarOpened;
    }

    public sealed class SymbolTickEventArgs
    {
        public string SymbolName { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }

    public sealed class Symbol
    {
        public string Name { get { return null; } }
        public event Action<SymbolTickEventArgs> Tick;
    }

    public sealed class Symbols { public Symbol GetSymbol(string name) { return null; } }
    public sealed class MarketData { public Bars GetBars(TimeFrame tf, string symbol) { return null; } }
    public sealed class Server { public DateTime Time { get { return default(DateTime); } } public DateTime TimeInUtc { get { return default(DateTime); } } }
    public sealed class Account { public string BrokerName { get { return null; } } public long Number { get { return 0; } } }
    public sealed class Timer { public void Start(TimeSpan interval) { } public void Stop() { } }

    public abstract class Robot
    {
        protected Symbols Symbols { get { return null; } }
        protected MarketData MarketData { get { return null; } }
        protected Server Server { get { return null; } }
        protected Account Account { get { return null; } }
        protected Timer Timer { get { return null; } }
        protected string SymbolName { get { return null; } }
        protected void Print(string format, params object[] args) { }
        protected void Print(object value) { }
        public virtual void Stop() { }
        protected virtual void OnStart() { }
        protected virtual void OnTimer() { }
        protected virtual void OnStop() { }
        protected virtual void OnTick() { }
    }
}
