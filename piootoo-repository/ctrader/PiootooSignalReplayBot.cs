using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using cAlgo.API;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooSignalReplayBot : Robot
    {
        private const string LabelPrefix = "PiootooSignalReplay";

        [Parameter("Signals File Path", DefaultValue = "")]
        public string SignalsFilePath { get; set; }

        [Parameter("Signal Symbol Override", DefaultValue = "")]
        public string SignalSymbolOverride { get; set; }

        [Parameter("Volume per Quantity", DefaultValue = 1.0, MinValue = 0.01)]
        public double VolumePerQuantity { get; set; }

        [Parameter("Close Opposite Signal", DefaultValue = true)]
        public bool CloseOppositeSignal { get; set; }

        [Parameter("Use Signal Price Filter", DefaultValue = false)]
        public bool UseSignalPriceFilter { get; set; }

        [Parameter("Max Entry Slippage (Pips)", DefaultValue = 5.0, MinValue = 0)]
        public double MaxEntrySlippagePips { get; set; }

        private readonly List<ReplaySignal> _signals = new();
        private readonly Dictionary<long, int> _positionEntryBars = new();
        private readonly Dictionary<long, ReplaySignal> _positionSignals = new();
        private int _nextSignalIndex;

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(SignalsFilePath))
            {
                Print("Signals File Path non impostato.");
                Stop();
                return;
            }

            if (!System.IO.File.Exists(SignalsFilePath))
            {
                Print("File segnali non trovato: {0}", SignalsFilePath);
                Stop();
                return;
            }

            var json = System.IO.File.ReadAllText(SignalsFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var loadedSignals = JsonSerializer.Deserialize<List<ReplaySignal>>(json, options) ?? new List<ReplaySignal>();
            var expectedSymbol = NormalizeSymbol(string.IsNullOrWhiteSpace(SignalSymbolOverride) ? SymbolName : SignalSymbolOverride);

            _signals.AddRange(loadedSignals
                .Where(signal => signal.Type == SignalType.Buy || signal.Type == SignalType.Sell)
                .Where(signal => NormalizeSymbol(signal.Symbol) == expectedSymbol)
                .OrderBy(signal => signal.Date)
                .ToList());

            Print("Caricati {0} segnali per {1} da {2}", _signals.Count, expectedSymbol, SignalsFilePath);
        }

        protected override void OnBar()
        {
            ProcessDueSignals(Bars.OpenTimes.LastValue);
            CloseExpiredPositions();
        }

        protected override void OnTick()
        {
            ProcessDueSignals(Server.Time);
            MoveStopsToBreakEven();
        }

        private void ProcessDueSignals(DateTime currentTime)
        {
            while (_nextSignalIndex < _signals.Count && _signals[_nextSignalIndex].Date <= currentTime)
            {
                var signal = _signals[_nextSignalIndex];
                _nextSignalIndex++;

                if (UseSignalPriceFilter && !IsEntryPriceAcceptable(signal))
                {
                    Print("Segnale scartato per slippage: {0} {1} price={2}", signal.Date, signal.Type, signal.Price);
                    continue;
                }

                ApplySignal(signal);
            }
        }

        private void ApplySignal(ReplaySignal signal)
        {
            var tradeType = signal.Type == SignalType.Buy ? TradeType.Buy : TradeType.Sell;
            var oppositeType = tradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
            var strategyCode = GetStrategyCode(signal);
            var label = MakeLabel(strategyCode);

            if (CloseOppositeSignal)
            {
                foreach (var position in Positions.FindAll(label, SymbolName, oppositeType))
                {
                    ClosePosition(position);
                    _positionEntryBars.Remove(position.Id);
                    _positionSignals.Remove(position.Id);
                }
            }

            if (signal.CloseOnly)
            {
                return;
            }

            if (Positions.Find(label, SymbolName, tradeType) != null)
            {
                return;
            }

            var rawVolume = Math.Max(1.0, (double)signal.Quantity * VolumePerQuantity);
            var volume = Symbol.NormalizeVolumeInUnits(rawVolume, RoundingMode.Down);
            var stopLossPips = ToPips(signal.StopLoss);
            var takeProfitPips = ToPips(signal.TakeProfit);

            var result = ExecuteMarketOrder(tradeType, SymbolName, volume, label, stopLossPips, takeProfitPips, signal.Reason);
            if (!result.IsSuccessful)
            {
                Print("Errore apertura posizione {0} {1}: {2}", signal.Date, signal.Type, result.Error);
                return;
            }

            if (result.Position != null && signal.MaxBarsInPosition.HasValue && signal.MaxBarsInPosition.Value > 0)
            {
                _positionEntryBars[result.Position.Id] = Bars.Count;
            }

            if (result.Position != null)
            {
                _positionSignals[result.Position.Id] = signal;
            }
        }

        private void CloseExpiredPositions()
        {
            foreach (var position in Positions
                .Where(position => position.SymbolName == SymbolName && position.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .ToList())
            {
                if (!_positionSignals.TryGetValue(position.Id, out var entrySignal) ||
                    !entrySignal.MaxBarsInPosition.HasValue ||
                    entrySignal.MaxBarsInPosition.Value <= 0)
                {
                    continue;
                }

                if (!_positionEntryBars.TryGetValue(position.Id, out var entryBarIndex))
                {
                    continue;
                }

                if (Bars.Count - entryBarIndex >= entrySignal.MaxBarsInPosition.Value)
                {
                    ClosePosition(position);
                    _positionEntryBars.Remove(position.Id);
                    _positionSignals.Remove(position.Id);
                }
            }
        }

        private void MoveStopsToBreakEven()
        {
            foreach (var position in Positions
                .Where(position => position.SymbolName == SymbolName && position.Label.StartsWith(LabelPrefix, StringComparison.Ordinal))
                .ToList())
            {
                if (!_positionSignals.TryGetValue(position.Id, out var signal) ||
                    !signal.BreakEven.HasValue ||
                    signal.BreakEven.Value <= 0)
                {
                    continue;
                }

                var breakEvenDistance = (double)signal.BreakEven.Value;
                if (position.TradeType == TradeType.Buy)
                {
                    var move = Symbol.Bid - position.EntryPrice;
                    if (move >= breakEvenDistance && (!position.StopLoss.HasValue || position.StopLoss.Value < position.EntryPrice))
                    {
                        ModifyPosition(position, position.EntryPrice, position.TakeProfit);
                    }
                }
                else
                {
                    var move = position.EntryPrice - Symbol.Ask;
                    if (move >= breakEvenDistance && (!position.StopLoss.HasValue || position.StopLoss.Value > position.EntryPrice))
                    {
                        ModifyPosition(position, position.EntryPrice, position.TakeProfit);
                    }
                }
            }
        }

        private bool IsEntryPriceAcceptable(ReplaySignal signal)
        {
            if (signal.Price <= 0)
            {
                return true;
            }

            var currentPrice = signal.Type == SignalType.Buy ? Symbol.Ask : Symbol.Bid;
            var distancePips = Math.Abs(currentPrice - (double)signal.Price) / Symbol.PipSize;
            return distancePips <= MaxEntrySlippagePips;
        }

        private double? ToPips(decimal? priceDistance)
        {
            if (!priceDistance.HasValue || priceDistance.Value <= 0)
            {
                return null;
            }

            return (double)priceDistance.Value / Symbol.PipSize;
        }

        private static string NormalizeSymbol(string symbol)
        {
            return (symbol ?? string.Empty).Trim().TrimStart('@').ToUpperInvariant();
        }

        private static string GetStrategyCode(ReplaySignal signal)
        {
            return !string.IsNullOrWhiteSpace(signal.StrategyCode)
                ? signal.StrategyCode
                : signal.StrategyName;
        }

        private static string MakeLabel(string strategyCode)
        {
            return $"{LabelPrefix}:{strategyCode}";
        }

        private enum SignalType
        {
            Buy = 0,
            Sell = 1,
            Hold = 2
        }

        private class ReplaySignal
        {
            public DateTime Date { get; set; }
            public SignalType Type { get; set; }
            public decimal Price { get; set; }
            public string Symbol { get; set; } = string.Empty;
            public string StrategyCode { get; set; } = string.Empty;
            public string StrategyName { get; set; } = string.Empty;
            public string Reason { get; set; }
            public decimal Quantity { get; set; } = 1m;
            public decimal? StopLoss { get; set; }
            public decimal? TakeProfit { get; set; }
            public decimal? BreakEven { get; set; }
            public int? MaxBarsInPosition { get; set; }
            public bool CloseOnly { get; set; }
        }
    }
}
