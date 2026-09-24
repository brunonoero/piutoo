using System.Collections.Frozen;

namespace Piootoo.Shared.Configuration;

/// <summary>
/// Allargamento dello stop protettivo dichiarato dalle strategie, applicato in <b>un punto solo</b>
/// — <c>StatelessEasyStrategyBase.EnrichSignal</c>, l'unico passaggio che ogni segnale di ingresso
/// attraversa, qualunque sia il motore che l'ha prodotto, e <b>solo alle strategie elencate qui</b>.
///
/// <para><b>Perche' esiste.</b> Il confronto fra gli stop veri del cBot e la barra da un minuto nello
/// stesso minuto dice che l'archivio di <c>datafeed-external/</c> e' la serie <b>Bid</b>: sui long
/// nessuna uscita cade fuori dal range della barra, su tutti e quattordici i simboli, mentre sugli
/// short ci cade il 100% su NG, il 94% su CC, il 92% su KC e il 24% su NQ, e la distanza e'
/// esattamente lo spread misurato. Uno short si copre sull'Ask, quindi il suo stop scatta uno spread
/// prima. Su parecchie strategie portate dalla ricerca lo spread misurato e' <b>maggiore della
/// distanza di stop dichiarata</b>: uno stop cosi' stretto non e' eseguibile su un conto vero.</para>
///
/// <para><b>Perche' non vale per tutte.</b> Il confronto fra le due gambe sullo stesso periodo,
/// stesso feed e stesso piano dice che il fattore applicato a tutto il catalogo <b>peggiora</b>
/// l'equity complessiva e raddoppia il drawdown: su 96 strategie con trade, 56 peggiorano e 31
/// migliorano. L'allargamento e' una correzione, non una taratura da estendere per simmetria: dove
/// lo stop e' gia' eseguibile, allargarlo cambia la strategia senza motivo. Da qui l'elenco.</para>
///
/// <para><b>Cosa fa e cosa non fa.</b> Moltiplica la sola distanza di stop. Target, breakeven e
/// trailing restano quelli della ricerca: allargare anche loro sarebbe una strategia diversa, non la
/// stessa strategia resa eseguibile. Il rapporto rischio/rendimento del porting cambia di
/// conseguenza, ed e' voluto.</para>
///
/// <para><b>Come si spegne.</b> <see cref="Enabled"/>, e basta quello: a interruttore spento ogni
/// strategia torna alla distanza di stop della ricerca, elenco compreso.</para>
///
/// <para><b>Dove si vede.</b> Il segnale nasce gia' con lo stop allargato, quindi <c>signals.json</c>,
/// <c>trades.json</c>, la console e l'intent che arriva al cBot portano tutti lo stesso numero: non
/// esiste uno stop "dichiarato" diverso da quello eseguito. Il fattore e' dichiarato nel log di
/// avvio del backtest e in <c>backtest-summary.json</c> come <see cref="EffectiveMultiplier"/>, e
/// <b>accanto ci sta l'elenco</b>: con un allargamento selettivo il solo fattore non descrive piu'
/// il run, e due run che allargano strategie diverse non sono confrontabili.</para>
/// </summary>
public static class StopMoneyPolicy
{
    /// <summary>
    /// <b>L'interruttore.</b> Accende e spegne l'allargamento per tutto il catalogo, da qui e da
    /// nessun altro posto. Spento, <see cref="WidenedStrategies"/> non conta piu' nulla e ogni
    /// strategia emette la distanza di stop della ricerca tale e quale.
    ///
    /// <para>Sta accanto al fattore e non nella configurazione del server perche' i due sono la
    /// stessa decisione: cambiarli cambia quali posizioni sopravvivono, quindi vanno letti insieme
    /// e versionati insieme. Chi confronta due run non deve andare a cercare quale
    /// <c>appsettings</c> girava quel giorno — se lo trova scritto in
    /// <see cref="EffectiveMultiplier"/>, dentro l'artefatto del run.</para>
    /// </summary>
    public const bool Enabled = true;

    /// <summary>
    /// Fattore applicato alla distanza di stop, quando <see cref="Enabled"/> e' acceso e la
    /// strategia sta in <see cref="WidenedStrategies"/>. 1 = nessun allargamento.
    ///
    /// <para><b>Dal 11/09/2026 vale 1.</b> La misura di compare-0033 (3.789 fill sui tick FTMO)
    /// dice che il rapporto <c>spread / stop della ricerca</c> supera il 10% solo su 15 delle 88
    /// strategie del piano, e che 23 delle 33 in elenco stanno sotto il 5%: l'elenco era stato scelto
    /// per effetto sull'equity, non per spread. La decisione e' di togliere dal piano le strategie con
    /// lo stop vicino allo spread invece di allargarlo, e di far girare tutte le altre con lo stop
    /// della ricerca. Il meccanismo, l'elenco e la dichiarazione negli artefatti restano: sono la
    /// leva per rimisurare. Tabella in <c>compare-0034/spread-su-stop-ricerca.md</c>.</para>
    /// </summary>
    public const decimal Multiplier = 1m;

    /// <summary>
    /// Fattore applicato al <b>target</b>, alle stesse condizioni di <see cref="Multiplier"/>:
    /// stesso interruttore, stesso elenco, stesso punto di applicazione. <b>1 = il target della
    /// ricerca, intatto</b>, ed e' il default.
    ///
    /// <para><b>Perche' esiste.</b> Allargare il solo stop peggiora il rapporto rischio/rendimento
    /// per costruzione: si rischia il doppio o il triplo per lo stesso obiettivo. Tenerlo a 1 e'
    /// una scelta legittima — rende la strategia eseguibile senza cambiarne l'obiettivo — ma e' una
    /// scelta, e va potuta misurare contro l'alternativa che scala i due insieme e conserva il
    /// rapporto.</para>
    ///
    /// <para><b>Perche' e' un numero separato.</b> Perche' i due si misurano uno contro l'altro:
    /// con un solo fattore per entrambi non si potrebbe piu' riprodurre il comportamento attuale,
    /// che e' la baseline di ogni confronto.</para>
    /// </summary>
    public const decimal TargetMultiplier = 1m;

    /// <summary>
    /// Il fattore davvero in vigore: <see cref="Multiplier"/> a interruttore acceso, 1 a
    /// interruttore spento. E' questo, non <see cref="Multiplier"/>, che va dichiarato negli
    /// artefatti e atteso dai test: altrimenti a interruttore spento il run direbbe di aver
    /// allargato stop che non ha allargato. Da solo pero' non basta piu' a descrivere il run: va
    /// dichiarato insieme a <see cref="WidenedStrategies"/>.
    /// </summary>
    public static decimal EffectiveMultiplier => Enabled ? Multiplier : 1m;

    /// <summary>
    /// L'elenco, nell'ordine in cui e' stato deciso e non in ordine alfabetico: prima le strategie
    /// che con l'allargamento restano o tornano in utile, poi quelle che restano in perdita ma la
    /// riducono. Chi lo rilegge deve poter distinguere i due gruppi, perche' il secondo e' quello
    /// da rimettere in discussione per primo se il porting cambia.
    ///
    /// <para>Sta <b>prima</b> di <see cref="WidenedStrategies"/> e non in fondo alla classe perche'
    /// gli inizializzatori statici girano nell'ordine in cui sono scritti: dichiararlo dopo lo
    /// farebbe leggere ancora nullo.</para>
    ///
    /// <para><b>Vuoto dal 24/09/2026.</b> Conteneva 33 strategie della serie PTS, eliminata dal
    /// progetto quel giorno; con <see cref="Multiplier"/> a 1 non allargava gia' nulla dall'11/09.
    /// Il meccanismo resta come leva per rimisurare: una strategia nuova si aggiunge qui per
    /// <c>StrategyCode</c>.</para>
    /// </summary>
    private static readonly FrozenSet<string> Selection =
        Array.Empty<string>().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Le strategie a cui l'allargamento si applica, per <c>StrategyCode</c>. Confronto senza
    /// distinzione fra maiuscole e minuscole; qui l'ordine e' alfabetico, cosi' due artefatti si
    /// confrontano riga per riga.
    ///
    /// <para>Un codice scritto male non farebbe rumore: la strategia semplicemente non verrebbe
    /// allargata e il run sembrerebbe a posto. Per questo <c>StopMoneyPolicyConformanceTests</c>
    /// verifica che ogni codice dell'elenco esista davvero nel catalogo.</para>
    /// </summary>
    public static IReadOnlyList<string> WidenedStrategies { get; } =
        [.. Selection.Order(StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Se l'allargamento si applica a questa strategia: interruttore acceso <b>e</b> codice in
    /// elenco.
    /// </summary>
    public static bool AppliesTo(string? strategyCode) =>
        Enabled && !string.IsNullOrWhiteSpace(strategyCode) && Selection.Contains(strategyCode);

    /// <summary>
    /// Applica <see cref="Multiplier"/> allo stop in denaro per contratto della strategia indicata,
    /// se le si applica. Lascia intatto tutto cio' che non e' uno stop attivo: <c>null</c> e i
    /// valori non positivi significano "nessuno stop" e moltiplicarli non li renderebbe uno stop.
    /// </summary>
    public static decimal? Widen(string? strategyCode, decimal? stopMoneyPerContract) =>
        AppliesTo(strategyCode) && stopMoneyPerContract is > 0m
            ? stopMoneyPerContract.Value * Multiplier
            : stopMoneyPerContract;

    /// <summary>
    /// Il fattore del target davvero in vigore: <see cref="TargetMultiplier"/> a interruttore
    /// acceso, 1 a interruttore spento. Vale per gli artefatti la stessa regola dello stop.
    /// </summary>
    public static decimal EffectiveTargetMultiplier => Enabled ? TargetMultiplier : 1m;

    /// <summary>
    /// Applica <see cref="TargetMultiplier"/> al target in denaro per contratto della strategia
    /// indicata, se le si applica. Come per lo stop, <c>null</c> e i valori non positivi
    /// significano "nessun target" e restano tali.
    /// </summary>
    public static decimal? WidenTarget(string? strategyCode, decimal? targetMoneyPerContract) =>
        AppliesTo(strategyCode) && targetMoneyPerContract is > 0m
            ? targetMoneyPerContract.Value * TargetMultiplier
            : targetMoneyPerContract;
}
