using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore RAN: <b>ingresso casuale con seme fisso</b>, uscite vere. E' il controllo della serie
/// PT6EXO e di tutto il catalogo, non una strategia da piano (vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §"Il controllo").
///
/// <para><b>A cosa serve.</b> Con le stesse uscite di una strategia vera — stop, target, trailing,
/// breakeven, tenuta, ora di uscita — e un ingresso a caso con la stessa frequenza, cento semi danno la
/// distribuzione di cio' che quelle uscite producono da sole. Una strategia vale per il proprio
/// <i>segnale</i> solo se sta chiaramente sopra quella distribuzione; se ci sta dentro, il suo
/// guadagno viene dalla forma delle uscite e dal mercato, non dall'idea.</para>
///
/// <para><b>Il caso e' una funzione, non uno stato.</b> L'estrazione e' un hash di (seme, simbolo,
/// timeframe, istante di apertura della barra): la stessa barra da' la stessa decisione in backtest,
/// in sessione live, dopo un riavvio del server e con una finestra di storia di qualunque lunghezza.
/// Un <see cref="Random"/> con stato darebbe sequenze diverse appena cambia il numero di valutazioni
/// — e cambia sempre, fra backtest e live. Per lo stesso motivo il simbolo entra con un hash scritto
/// qui (FNV-1a) e non con <see cref="string.GetHashCode()"/>, che in .NET e' randomizzato per processo.</para>
///
/// <para>Il nome della strategia non entra nell'hash: un contenitore e una classe con gli stessi
/// parametri estraggono le stesse barre, cosi' un run si ripete da qualunque dei due.</para>
///
/// <para><b>Una posizione alla volta.</b> In posizione non nasce nulla: l'ingresso casuale conta le
/// barre flat, e la frequenza <see cref="EntryProbability"/> si tara su quelle.</para>
/// </summary>
public abstract class RandomEntryEngine : EasyEngineBase
{
    /// <summary>Seme dell'estrazione. Semi diversi = sequenze indipendenti sulle stesse barre.</summary>
    protected int Seed = 1;

    /// <summary>
    /// Probabilita' che una barra flat apra un ingresso, fra 0 e 1. Si sceglie in modo che il numero di
    /// trade sia quello della strategia che si vuole confrontare: con N trade su B barre flat, circa N/B.
    /// </summary>
    protected decimal EntryProbability = 0.05m;

    /// <summary>Lato consentito: 0 = a caso (meta' e meta'), 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    // Sali delle due estrazioni: la decisione di entrare e il lato non devono dipendere l'una
    // dall'altro, altrimenti con p piccola si entrerebbe quasi sempre dallo stesso lato.
    private const ulong EntrySalt = 0x9E3779B97F4A7C15UL;
    private const ulong SideSalt = 0xC2B2AE3D27D4EB4FUL;

    /// <summary>Un ingresso a mercato sulla barra successiva, se l'estrazione lo decide.</summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (EntryProbability < 0m || EntryProbability > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(EntryProbability), EntryProbability,
                $"{Name}: la probabilita' di ingresso deve stare fra 0 e 1.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        if (Draw(barTime, EntrySalt) >= (double)EntryProbability)
            return Hold(bar.Close, barTime);

        var side = Direction switch
        {
            1 => SignalType.Buy,
            2 => SignalType.Sell,
            _ => Draw(barTime, SideSalt) < 0.5 ? SignalType.Buy : SignalType.Sell
        };

        var entry = WithSessionExit(EntryMarketNextBar(
            side, bar.Close, data, barTime, side == SignalType.Buy ? "LE RAN" : "SE RAN"));

        return entry ?? Hold(bar.Close, barTime);
    }

    /// <summary>
    /// Estrazione uniforme in [0, 1) per questa barra: funzione pura di seme, simbolo, timeframe,
    /// istante di apertura e sale. E' pubblica perche' i test e gli studi devono poter dire quali
    /// barre un seme sceglie senza far girare un backtest.
    /// </summary>
    public double Draw(DateTime barOpenUtc, ulong salt)
    {
        var state = Mix((ulong)(uint)Seed ^ salt);
        state = Mix(state ^ SymbolHash(Symbol));
        state = Mix(state ^ (ulong)(uint)TimeframeMinutes);
        state = Mix(state ^ (ulong)barOpenUtc.Ticks);

        // I 53 bit alti come mantissa: uniforme in [0, 1) con la risoluzione piena di un double.
        return (state >> 11) * (1.0 / (1UL << 53));
    }

    /// <summary>Il finalizzatore di SplitMix64: ogni bit in ingresso sposta in media meta' dei bit in uscita.</summary>
    private static ulong Mix(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    /// <summary>FNV-1a a 64 bit sul simbolo normalizzato: stabile fra processi, a differenza di GetHashCode.</summary>
    private static ulong SymbolHash(string symbol)
    {
        var hash = 0xCBF29CE484222325UL;
        foreach (var character in symbol.Trim().TrimStart('@').ToUpperInvariant())
        {
            hash ^= character;
            hash *= 0x100000001B3UL;
        }

        return hash;
    }
}
