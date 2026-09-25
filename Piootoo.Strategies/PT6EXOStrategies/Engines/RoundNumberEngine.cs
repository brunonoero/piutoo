using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore RNM: <b>numeri tondi</b>, una delle bizzarre del catalogo. I livelli sono i multipli di
/// <see cref="RoundStep"/> punti (100, 500, 1000): su quei prezzi si accumulano ordini, e il prezzo che
/// ci arriva vicino o ne viene attratto (magnete) o ci rimbalza (muro). Vedi
/// <c>docs/domini/catalogo-idee-pt6exo.md</c> §RNM. Come ogni bizzarra, vale solo se batte con
/// chiarezza il controllo a ingresso casuale (RAN) con le stesse uscite.
///
/// <para><b>Trappola: ha senso solo sul prezzo vero.</b> Il feed interno e' la serie continua
/// <b>aggiustata</b> ai rollover, dove un livello tondo non e' tondo: ogni rollover sposta all'indietro
/// tutti i prezzi di una quantita' qualunque, e il 18.000 della serie aggiustata di un anno fa era un
/// prezzo qualsiasi sul contratto che si scambiava allora, dove nessun ordine stava. RNM si ricerca <b>solo sul feed di un
/// broker</b> (<c>DatafeedBroker</c>), cioe' sul prezzo del CFD che il conto esegue; una cella trovata
/// sul feed interno non dice niente.</para>
///
/// <para><b>I livelli.</b> Aritmetica decimale: <c>base = Floor(chiusura / passo) × passo</c>. Il livello
/// sopra e' il primo multiplo <b>strettamente</b> sopra la chiusura (<c>base + passo</c>), quello sotto il
/// primo <b>strettamente</b> sotto (<c>base</c>, o <c>base − passo</c> se la chiusura sta esattamente su
/// un livello). Una chiusura sul livello e' gia' arrivata: non e' attratta da quello, e non ci mette un
/// muro. La vicinanza e' <see cref="DistanceTicks"/> × <see cref="TickSize"/>, estremo compreso.</para>
///
/// <para><b><see cref="Mode"/> 0, magnete.</b> Chiusura entro la distanza <b>sotto</b> il livello di
/// sopra: long a mercato sulla barra dopo, verso il livello; lo specchio sopra il livello di sotto e'
/// short. Con <see cref="TargetAtLevel"/> il target e' il livello stesso: la distanza dalla chiusura
/// della barra di segnale, l'unico prezzo noto quando l'ordine nasce, in denaro per contratto. Il fill
/// all'apertura dopo la sposta di quanto e' il gap. Due zone che si toccano (<c>2 × distanza</c> almeno
/// pari al passo) armerebbero un long e uno short a mercato sulla stessa barra: e' un errore di
/// configurazione.</para>
///
/// <para><b><see cref="Mode"/> 1, muro.</b> Limit sell sul livello di sopra, se la chiusura gli e' entro
/// la distanza, e limit buy sul livello di sotto, alle stesse condizioni: validi una barra, in OCO se
/// sono armati insieme. Un'apertura gia' oltre il livello rende il limit un livello scavalcato, e vale
/// la politica del segnale (<c>CrossedLevelPolicy</c>, di default scartato): il muro e' gia' stato
/// sfondato.</para>
///
/// <para>Una posizione alla volta: in posizione non nasce nulla.</para>
/// </summary>
public abstract class RoundNumberEngine : EasyEngineBase
{
    /// <summary>Passo dei livelli tondi, in punti.</summary>
    protected decimal RoundStep = 100m;

    /// <summary>0 = magnete (a mercato verso il livello), 1 = muro (limit sul livello).</summary>
    protected int Mode;

    /// <summary>Distanza massima dal livello, in tick, estremo compreso.</summary>
    protected int DistanceTicks = 20;

    /// <summary>Con il magnete: 1 = target sul livello, 0 = le uscite comuni della base.</summary>
    protected int TargetAtLevel = 1;

    /// <summary>Dimensione del tick dello strumento, per <see cref="DistanceTicks"/>.</summary>
    protected decimal TickSize = 1m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        var maxDistance = DistanceTicks * TickSize;
        if (RoundStep <= 0m || TickSize <= 0m || DistanceTicks < 1 || Mode is < 0 or > 1 ||
            TargetAtLevel is < 0 or > 1 || Direction is < 0 or > 2 || (Mode == 0 && 2m * maxDistance >= RoundStep))
        {
            throw new ArgumentOutOfRangeException(nameof(RoundStep),
                $"{Name}: configurazione RNM incoerente (passo {RoundStep}, distanza {DistanceTicks} tick da {TickSize}, " +
                $"modo {Mode}, target {TargetAtLevel}, lato {Direction}); servono passo e tick positivi, distanza " +
                "almeno 1 tick, modo 0 o 1, target 0 o 1, lato 0, 1 o 2 e, col magnete, due volte la distanza " +
                "sotto il passo.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        var close = bar.Close;
        var floor = Math.Floor(close / RoundStep) * RoundStep;
        var levelAbove = floor + RoundStep;
        var levelBelow = floor == close ? floor - RoundStep : floor;
        var nearAbove = levelAbove - close <= maxDistance;
        var nearBelow = close - levelBelow <= maxDistance;

        var entries = new List<TradeSignal>(2);

        if (Mode == 0)
        {
            if (Direction != 2 && nearAbove)
                AddEntry(entries, Magnet(SignalType.Buy, levelAbove - close, data, barTime, "LE RNM"));
            if (Direction != 1 && nearBelow)
                AddEntry(entries, Magnet(SignalType.Sell, close - levelBelow, data, barTime, "SE RNM"));
        }
        else
        {
            if (Direction != 1 && nearAbove)
                AddEntry(entries, WithSessionExit(EntryLimitNextBar(SignalType.Sell, levelAbove, data, barTime, "SE RNM muro")));
            if (Direction != 2 && nearBelow)
                AddEntry(entries, WithSessionExit(EntryLimitNextBar(SignalType.Buy, levelBelow, data, barTime, "LE RNM muro")));
        }

        return Combine(entries, Hold(close, barTime));
    }

    /// <summary>Ingresso a mercato verso il livello, con il target sul livello se dichiarato.</summary>
    private TradeSignal? Magnet(SignalType side, decimal distance, OhlcvData[] data, DateTime barTime, string reason)
    {
        var signal = EntryMarketNextBar(side, data[^1].Close, data, barTime, reason);

        if (TargetAtLevel != 0)
            signal.TakeProfitMoneyPerFutureContract = Math.Round(distance * InstrumentRegistry.PointValue(Symbol), 2);

        return WithSessionExit(signal);
    }
}
