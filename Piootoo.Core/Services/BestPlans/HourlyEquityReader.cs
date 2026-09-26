using System.Text.Json;
using Piootoo.Shared.Models.BestPlans;

namespace Piootoo.Core.Services.BestPlans;

/// <summary>
/// Legge la curva di equity globale (<c>HourlyResults</c>) da un file di risultato
/// <c>backtest_*.json</c> senza deserializzarlo.
/// </summary>
/// <remarks>
/// <para>Il file arriva a 160 MB, e la gran parte e' <c>StrategyResults</c>, l'equity minuto per
/// minuto di ogni strategia. <c>HourlyResults</c> viene prima — e' l'ottava proprieta' di
/// <c>BacktestingResult</c> — quindi il lettore scorre il file a token, tiene data, equity e drawdown
/// di ogni riga e si ferma alla chiusura dell'array: il resto del file non si legge.</para>
///
/// <para>Le righe a equity zero si scartano come fa il report HTML: sono tick dell'orologio prima che
/// il motore abbia un'equity.</para>
/// </remarks>
public static class HourlyEquityReader
{
    private const int InitialBufferSize = 1 << 16;

    public sealed record Series(decimal? InitialCapital, List<BestPlanEquityPoint> Points);

    public static Series Read(string resultPath)
    {
        using var stream = new FileStream(
            resultPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1,
            FileOptions.SequentialScan);

        var parser = new Parser();
        var buffer = new byte[InitialBufferSize];
        var buffered = 0;
        var state = new JsonReaderState();
        var isFinalBlock = false;

        while (true)
        {
            if (!isFinalBlock)
            {
                var read = stream.Read(buffer, buffered, buffer.Length - buffered);
                if (read == 0)
                    isFinalBlock = true;
                buffered += read;
            }

            var reader = new Utf8JsonReader(buffer.AsSpan(0, buffered), isFinalBlock, state);
            if (parser.Consume(ref reader) || isFinalBlock)
                break;

            state = reader.CurrentState;
            var consumed = (int)reader.BytesConsumed;
            Buffer.BlockCopy(buffer, consumed, buffer, 0, buffered - consumed);
            buffered -= consumed;

            // Un token piu' lungo del buffer intero: si allarga invece di girare a vuoto.
            if (buffered == buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);
        }

        return new Series(parser.InitialCapital, parser.Points);
    }

    private sealed class Parser
    {
        private enum Field { None, DateTime, Equity, Drawdown }

        private string? _topProperty;
        private bool _inHourly;
        private Field _field;
        private DateTime _time;
        private decimal _equity;
        private decimal _drawdown;

        public decimal? InitialCapital { get; private set; }

        public List<BestPlanEquityPoint> Points { get; } = new();

        /// <summary>Consuma i token disponibili. True quando l'array delle righe si e' chiuso.</summary>
        public bool Consume(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                var depth = reader.CurrentDepth;
                var token = reader.TokenType;

                if (!_inHourly)
                {
                    if (depth != 1)
                        continue;

                    if (token == JsonTokenType.PropertyName)
                    {
                        _topProperty = reader.GetString();
                        continue;
                    }

                    if (_topProperty == "InitialCapital" && token == JsonTokenType.Number)
                        InitialCapital = reader.GetDecimal();
                    else if (_topProperty == "HourlyResults" && token == JsonTokenType.StartArray)
                        _inHourly = true;

                    continue;
                }

                switch (token)
                {
                    case JsonTokenType.EndArray when depth == 1:
                        return true;
                    case JsonTokenType.StartObject when depth == 2:
                        _field = Field.None;
                        _time = default;
                        _equity = 0m;
                        _drawdown = 0m;
                        break;
                    case JsonTokenType.EndObject when depth == 2:
                        if (_equity != 0m)
                            Points.Add(new BestPlanEquityPoint
                            {
                                TimeUtc = _time,
                                Equity = _equity,
                                DrawdownPercent = Math.Abs(_drawdown)
                            });
                        break;
                    case JsonTokenType.PropertyName when depth == 3:
                        _field = reader.ValueTextEquals("DateTime"u8) ? Field.DateTime
                            : reader.ValueTextEquals("Equity"u8) ? Field.Equity
                            : reader.ValueTextEquals("Drawdown"u8) ? Field.Drawdown
                            : Field.None;
                        break;
                    case JsonTokenType.String when depth == 3 && _field == Field.DateTime:
                        _time = reader.GetDateTime().ToUniversalTime();
                        break;
                    case JsonTokenType.Number when depth == 3 && _field == Field.Equity:
                        _equity = reader.GetDecimal();
                        break;
                    case JsonTokenType.Number when depth == 3 && _field == Field.Drawdown:
                        _drawdown = reader.GetDecimal();
                        break;
                }
            }

            return false;
        }
    }
}
