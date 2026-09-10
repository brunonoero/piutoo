namespace Piootoo.Shared.Configuration;

/// <summary>
/// Allargamento dello stop protettivo dichiarato dalle strategie, applicato in <b>un punto solo</b>
/// — <c>StatelessEasyStrategyBase.EnrichSignal</c>, l'unico passaggio che ogni segnale di ingresso
/// attraversa, qualunque sia il motore che l'ha prodotto.
///
/// <para><b>Perche' esiste.</b> Il confronto fra gli stop veri del cBot e la barra da un minuto nello
/// stesso minuto dice che l'archivio di <c>datafeed-external/</c> e' la serie <b>Bid</b>: sui long
/// nessuna uscita cade fuori dal range della barra, su tutti e quattordici i simboli, mentre sugli
/// short ci cade il 100% su NG, il 94% su CC, il 92% su KC e il 24% su NQ, e la distanza e'
/// esattamente lo spread misurato. Uno short si copre sull'Ask, quindi il suo stop scatta uno spread
/// prima. Su parecchie strategie portate dalla ricerca lo spread misurato e' <b>maggiore della
/// distanza di stop dichiarata</b>: uno stop cosi' stretto non e' eseguibile su un conto vero.</para>
///
/// <para><b>Cosa fa e cosa non fa.</b> Moltiplica la sola distanza di stop. Target, breakeven e
/// trailing restano quelli della ricerca: allargare anche loro sarebbe una strategia diversa, non la
/// stessa strategia resa eseguibile. Il rapporto rischio/rendimento del porting cambia di
/// conseguenza, ed e' voluto.</para>
///
/// <para><b>Dove si vede.</b> Il segnale nasce gia' con lo stop allargato, quindi <c>signals.json</c>,
/// <c>trades.json</c>, la console e l'intent che arriva al cBot portano tutti lo stesso numero: non
/// esiste uno stop "dichiarato" diverso da quello eseguito. Il moltiplicatore e' dichiarato nel log
/// di avvio del backtest e in <c>backtest-summary.json</c>, perche' due run con moltiplicatori
/// diversi non sono confrontabili.</para>
/// </summary>
public static class StopMoneyPolicy
{
    /// <summary>
    /// Fattore applicato alla distanza di stop dichiarata dalla strategia. 1 = nessun allargamento.
    /// </summary>
    public const decimal Multiplier = 3m;

    /// <summary>
    /// Applica <see cref="Multiplier"/> a uno stop in denaro per contratto. Lascia intatto tutto
    /// cio' che non e' uno stop attivo: <c>null</c> e i valori non positivi significano "nessuno
    /// stop" e moltiplicarli non li renderebbe uno stop.
    /// </summary>
    public static decimal? Widen(decimal? stopMoneyPerContract) =>
        stopMoneyPerContract is > 0m
            ? stopMoneyPerContract.Value * Multiplier
            : stopMoneyPerContract;
}
