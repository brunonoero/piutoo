namespace Piootoo.Core.Optimization.Sweep;

/// <summary>
/// Come si giudica una configurazione. Sostituibile di proposito: la funzione obiettivo e' la scelta
/// piu' discutibile di tutta la ricerca, e va potuta cambiare senza toccare l'ottimizzatore.
/// </summary>
public interface ISweepObjective
{
    /// <summary>
    /// Il punteggio, o <c>null</c> se la configurazione non e' ammissibile — che non e' la stessa
    /// cosa di "punteggio basso": una configurazione con tre trade non e' cattiva, e' non misurata,
    /// e tenerla in classifica con un punteggio qualsiasi significa farla vincere per caso.
    /// </summary>
    decimal? Score(SweepOutcome outcome);

    /// <summary>Come si chiama, per il resoconto del run.</summary>
    string Describe();
}

/// <summary>
/// L'obiettivo di default: <b>utile netto diviso drawdown</b>, con una soglia minima di trade.
///
/// <para><b>Dichiarazione onesta.</b> Il file dell'ottimizzatore Python (<c>optimizer.py</c>) non e'
/// nel repository: del punteggio che la ricerca usava si sa dai commenti di <c>base.py</c> che
/// esiste — vi si parla di un "punteggio q" e di plateau analysis sulla top 10 — ma non la formula.
/// Questa quindi <b>non</b> e' la funzione della ricerca riprodotta: e' una scelta, e va giudicata
/// come tale. Premia il rendimento per unita' di rischio invece del P&amp;L assoluto, che e' il
/// principio del metodo — una curva ripida e profonda non e' operabile su un conto vero — ed evita
/// di eleggere configurazioni con pochissimi trade, che e' il modo piu' rapido per ottenere un
/// overfit che sembra ottimo.</para>
///
/// <para>Il drawdown e' quello dei trade chiusi (<see cref="SweepOutcome.MaxClosedTradeDrawdown"/>),
/// piu' ottimista di quello mark-to-market del backtest: serve a ordinare, non a dichiarare il
/// rischio di una strategia.</para>
/// </summary>
/// <param name="MinTrades">
/// Sotto questa soglia la configurazione non e' ammissibile. Trenta e' poco per dichiarare una
/// strategia buona e abbastanza per non eleggerne una nata dal caso.
/// </param>
/// <param name="MinAverageTrade">
/// Utile medio per trade minimo. Zero lascia passare tutto cio' che e' in utile; alzarlo a qualche
/// multiplo del costo di transazione e' il filtro che il metodo raccomanda quando lo spread e'
/// vicino alla distanza di stop.
/// </param>
public sealed record NetOverDrawdownObjective(int MinTrades = 30, decimal MinAverageTrade = 0m) : ISweepObjective
{
    public decimal? Score(SweepOutcome outcome)
    {
        // Non ammissibile vuol dire NON MISURATA, non "cattiva". Una configurazione con pochi trade
        // non si puo' giudicare; una in perdita si', e va giudicata — altrimenti una fase in cui
        // nessuna combinazione e' ancora in utile non produce alcun ordinamento e la ricerca si
        // ferma li'. Succede per davvero: la prima fase del BIAS settimanale ottimizza i GIORNI con
        // gli orari ancora al default, e a quell'ora puo' non funzionare niente. Con le perdite
        // escluse la sweep restava ai default e girava a vuoto per tutte le fasi successive.
        //
        // Un punteggio negativo ordina correttamente (meno peggio in alto) e non puo' vincere
        // contro una configurazione in utile. Che il FUORI CAMPIONE in perdita sia un motivo di
        // scarto resta vero, ma e' una decisione della validazione, non dell'ordinamento.
        if (outcome.Trades < MinTrades) return null;
        if (outcome.AverageTrade < MinAverageTrade) return null;

        // Un drawdown nullo su un numero di trade sopra soglia e' possibile e non e' un errore:
        // vale il netto stesso, senza dividere per un epsilon che gonfierebbe il punteggio.
        return outcome.MaxClosedTradeDrawdown > 0m
            ? outcome.NetProfit / outcome.MaxClosedTradeDrawdown
            : outcome.NetProfit;
    }

    /// <summary>
    /// Se un punteggio descrive una configurazione in <b>utile</b>. Serve alla validazione, che deve
    /// scartare un fuori campione in perdita anche quando il punteggio esiste ed e' ordinabile.
    /// </summary>
    public static bool IsProfitable(decimal? score) => score is > 0m;

    public string Describe() =>
        $"netto/drawdown, almeno {MinTrades} trade" +
        (MinAverageTrade > 0m ? $" e utile medio ≥ {MinAverageTrade:N0}" : string.Empty);
}
