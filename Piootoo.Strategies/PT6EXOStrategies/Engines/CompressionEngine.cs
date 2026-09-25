using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT6EXOStrategies.Engines;

/// <summary>
/// Motore NRX: <b>rottura di una compressione</b>. Dopo una barra stretta si piazzano due stop sui suoi
/// estremi, in OCO: il mercato che si e' fermato riparte, e non si sa da che parte. Gli ingressi nascono
/// nei momenti morti, non dopo un'espansione come nei VBO: e' la ragione per cui sta nella serie PT6EXO.
/// Vedi <c>docs/domini/catalogo-idee-pt6exo.md</c> §NRX.
///
/// <para><b>La barra di compressione</b>, scelta da <see cref="CompressionKind"/>:</para>
/// <list type="bullet">
///   <item><c>0</c> — NR-N: il range (massimo meno minimo) e' <b>strettamente</b> minore di quello di
///   ognuna delle <see cref="LookbackBars"/> − 1 barre prima (NR4, NR7). Un pareggio non e' una
///   compressione: su un tratto di barre tutte uguali ogni barra sarebbe "la piu' stretta" e l'OCO
///   nascerebbe a ogni barra, cioe' non ci sarebbe piu' un evento.</item>
///   <item><c>1</c> — inside bar: massimo non oltre il massimo precedente e minimo non sotto il minimo
///   precedente.</item>
/// </list>
/// <para>Una barra senza range (massimo uguale al minimo) non e' mai una compressione: i due stop
/// cadrebbero sullo stesso prezzo.</para>
///
/// <para><b>Validita'.</b> La barra di compressione e' una barra <b>chiusa</b>, e l'OCO vale dalla barra
/// dopo per <see cref="ValidBars"/> barre. Il motore non tiene stato: a ogni barra cerca la compressione
/// fra le ultime <see cref="ValidBars"/> barre, <b>quella appena chiusa compresa</b> — la compressione
/// appena chiusa arma gli stop per la prima barra valida —, e se ce n'e' piu' d'una usa la piu' recente,
/// che e' la compressione in corso. Gli ordini sono "next bar" e si riemettono a ogni barra finche' la
/// compressione resta nella finestra: e' la validita' di M barre scritta senza memoria.</para>
///
/// <para><b>Ordini.</b> Buy stop al massimo della compressione piu' <see cref="OffsetTicks"/> tick, sell
/// stop al minimo meno lo stesso margine, combinati come OCO: il fill di una gamba cancella l'altra, e
/// da quel momento il gate di posizione non riemette nulla. Se la posizione si chiude mentre la
/// compressione e' ancora valida gli stop si riarmano, anche sul lato gia' eseguito: il limite di fill
/// per sessione vale <b>per lato</b> e resta la leva comune <see cref="EasyEngineBase.MaxEntriesPerSession"/>
/// a dire quante volte.</para>
///
/// <para><b>Livello gia' scavalcato.</b> Con <see cref="ValidBars"/> oltre 1 il prezzo puo' aver gia'
/// superato uno dei due estremi senza riempire l'ordine (un gap, oppure un'uscita dopo il fill): lo stop
/// sarebbe dalla parte sbagliata del mercato. Il motore non lo controlla, perche' la regola sta in un
/// punto solo: il segnale porta il default di <c>CrossedLevelPolicy</c> (<c>Reject</c>) e motore interno e
/// cBot scartano l'ordine allo stesso modo.</para>
/// </summary>
public abstract class CompressionEngine : EasyEngineBase
{
    /// <summary>Tipo di compressione: 0 = NR-N, 1 = inside bar.</summary>
    protected int CompressionKind;

    /// <summary>Barre fra cui la compressione NR-N deve essere la piu' stretta, lei compresa (7 = NR7).</summary>
    protected int LookbackBars = 7;

    /// <summary>Barre, dopo quella di compressione, su cui gli stop restano validi.</summary>
    protected int ValidBars = 1;

    /// <summary>Margine degli stop oltre gli estremi della compressione, in tick.</summary>
    protected int OffsetTicks;

    /// <summary>Dimensione del tick dello strumento, per <see cref="OffsetTicks"/>.</summary>
    protected decimal TickSize = 1m;

    /// <summary>Lato consentito: 0 = entrambi, 1 = solo long, 2 = solo short.</summary>
    protected int Direction;

    /// <summary>Questo motore chiude a fine sessione quando <c>intraday_only = 1</c>.</summary>
    protected override bool AppliesSessionExit => SessionExitFromIntradayOnly;

    /// <inheritdoc />
    protected override bool AppliesSessionExitDeclared => true;

    /// <summary>
    /// La compressione piu' vecchia ancora valida e le barre con cui si confronta: per NR-N le
    /// <see cref="LookbackBars"/> − 1 prima, per l'inside bar la barra prima.
    /// </summary>
    public override int RequiredCandles => Math.Max(
        base.RequiredCandles,
        Math.Max(1, ValidBars) + Math.Max(2, LookbackBars));

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (CompressionKind is < 0 or > 1 || LookbackBars < 2 || ValidBars < 1 || OffsetTicks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CompressionKind),
                $"{Name}: configurazione NRX incoerente (tipo {CompressionKind}, barre {LookbackBars}, " +
                $"validita' {ValidBars}, margine {OffsetTicks}); servono tipo 0 o 1, almeno 2 barre di " +
                "confronto, validita' di almeno una barra e margine non negativo.");
        }

        if (data is null || data.Length < RequiredCandles)
            return Hold(data is { Length: > 0 } ? data[^1].Close : 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;

        if (CurrentMP != 0 || InDeclaredWindow(barTime) == false)
            return Hold(bar.Close, barTime);

        // Dalla piu' recente alla piu' vecchia: la prima trovata e' la compressione in corso.
        var compression = -1;
        for (var index = data.Length - 1; index >= data.Length - ValidBars; index--)
        {
            if (IsCompression(data, index))
            {
                compression = index;
                break;
            }
        }

        if (compression < 0)
            return Hold(bar.Close, barTime);

        var offset = OffsetTicks * TickSize;
        var entries = new List<TradeSignal>(2);

        if (Direction != 2)
        {
            AddEntry(entries, WithSessionExit(EntryStopNextBar(
                SignalType.Buy, data[compression].High + offset, data, barTime, "LE NRX")));
        }

        if (Direction != 1)
        {
            AddEntry(entries, WithSessionExit(EntryStopNextBar(
                SignalType.Sell, data[compression].Low - offset, data, barTime, "SE NRX")));
        }

        return Combine(entries, Hold(bar.Close, barTime));
    }

    /// <summary>La barra <paramref name="index"/> e' una compressione del tipo dichiarato.</summary>
    private bool IsCompression(OhlcvData[] data, int index)
    {
        var candidate = data[index];
        var range = candidate.High - candidate.Low;
        if (range <= 0m)
            return false;

        if (CompressionKind == 1)
        {
            var previous = data[index - 1];
            return candidate.High <= previous.High && candidate.Low >= previous.Low;
        }

        for (var other = index - LookbackBars + 1; other < index; other++)
        {
            if (data[other].High - data[other].Low <= range)
                return false;
        }

        return true;
    }
}
