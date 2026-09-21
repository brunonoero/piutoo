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

/// <summary>
/// Il punteggio del <b>peggiore</b> dei sotto-periodi del campione, non quello del totale.
///
/// <para><b>Il problema che risolve.</b> Su decine di migliaia di combinazioni, il massimo di
/// <see cref="NetOverDrawdownObjective"/> e' quasi sempre una configurazione che nel campione non ha
/// mai incontrato un brutto tratto — non perche' sia robusta, ma perche' fra tante qualcuna e'
/// fortunata. Misurato il 20/09/2026 sul feed ICS, campione 2014-2022 e validazione 2022-2026: su
/// NQ 4h la vincitrice aveva punteggio <b>26,19</b> in campione e <b>1,04</b> fuori, e su FDAX 1h
/// 16,10 contro 1,14. Le due celle promosse avevano invece i punteggi in campione piu' <i>bassi</i>.
/// Un massimo troppo bello non e' un risultato migliore: e' un avvertimento.</para>
///
/// <para><b>Come lo rompe.</b> Il campione viene diviso in <paramref name="SubPeriods"/> tratti
/// consecutivi e ognuno riceve il proprio punteggio; vince il <b>minimo</b>. Una configurazione che
/// deve tutto a un anno buono viene giudicata sull'anno cattivo, ed e' l'unico modo di distinguerla
/// da una che funziona sempre un po'. Non costa un run in piu': i sotto-periodi si ricavano dai
/// trade gia' chiusi.</para>
///
/// <para><b>Il pavimento sul drawdown.</b> Il denominatore non scende mai sotto la <b>peggiore
/// perdita singola</b> del tratto: un conto che ha incassato una perdita di X ha visto almeno X di
/// escursione negativa, e senza questo pavimento una configurazione con drawdown minuscolo — di
/// nuovo, quasi sempre fortuna — otteneva un punteggio enorme dividendo per quasi zero.</para>
///
/// <para>Un tratto con meno di <paramref name="MinTradesPerSubPeriod"/> trade vale <b>zero</b>, non
/// viene saltato: una strategia che opera solo in un pezzo del campione non e' una strategia buona
/// con un buco, e saltare i tratti vuoti la premierebbe.</para>
/// </summary>
/// <param name="MinProfitFactor">
/// Profit factor minimo perche' la configurazione sia ammissibile.
///
/// <para><b>Perche' serve, oltre al drawdown.</b> Il punteggio guarda il rapporto fra utile e
/// drawdown, e di quanto margine ci sia su <i>ogni trade</i> non sa niente: una configurazione con
/// profit factor 1,01 — i profitti superano le perdite dell'1% — puo' arrivare in cima a una fase
/// se il netto e' positivo e la curva e' liscia. Ma un margine dell'1% lo mangia il primo costo che
/// ci si e' dimenticati, e ce n'e' gia' uno noto: la fee di conversione del P&amp;L dello 0,70% che
/// FTMO dichiara e il motore non applica. Sotto questa soglia la configurazione non entra in
/// classifica affatto.</para>
/// </param>
/// <param name="MinAverageTrade">
/// Utile medio per trade minimo, in denaro.
///
/// <para>E' la stessa idea del rapporto <c>spread / distanza di stop</c> che il progetto gia' usa
/// per scegliere le coppie strategia/strumento: quello che conta non e' il target in se' — un
/// target largo abbassa il win rate e puo' peggiorare tutto — ma che l'utile medio sia <b>molto
/// piu' grande del costo per trade</b>. Su ICS/DE40 il costo e' ~$50 fra commissione e spread: una
/// configurazione che ne guadagna 78 ne lascia due terzi sul tavolo e muore al primo tick di
/// slippage, una che ne guadagna 300 no.</para>
/// </param>
public sealed record WorstSubPeriodObjective(
    int MinTrades = 50,
    int SubPeriods = 4,
    int MinTradesPerSubPeriod = 5,
    decimal MinAverageTrade = 0m,
    decimal MinProfitFactor = 0m) : ISweepObjective
{
    public decimal? Score(SweepOutcome outcome)
    {
        if (outcome.Trades < MinTrades) return null;
        if (outcome.AverageTrade < MinAverageTrade) return null;
        if (outcome.ClosedTrades.Count == 0 || SubPeriods < 1) return null;

        // Il profit factor si controlla sul complesso e non tratto per tratto: su un quarto di
        // campione e' troppo rumoroso per essere una soglia, mentre sull'intero dice quanto margine
        // ha la configurazione su ogni trade. Un profit factor nullo (nessuna perdita) passa: e'
        // raro e non e' un difetto.
        if (MinProfitFactor > 0m && outcome.ProfitFactor is { } pf && pf < MinProfitFactor)
            return null;

        var trades = outcome.ClosedTrades.OrderBy(trade => trade.ExitDate).ToArray();
        var from = trades[0].ExitDate;
        var to = trades[^1].ExitDate;
        if (to <= from) return null;

        var span = (to - from) / SubPeriods;
        var worst = decimal.MaxValue;

        for (var index = 0; index < SubPeriods; index++)
        {
            var start = from + span * index;
            var end = index == SubPeriods - 1 ? to.AddTicks(1) : start + span;
            var slice = trades.Where(trade => trade.ExitDate >= start && trade.ExitDate < end).ToArray();

            var score = slice.Length < MinTradesPerSubPeriod ? 0m : ScoreOf(slice);
            if (score < worst) worst = score;
        }

        return worst == decimal.MaxValue ? null : worst;
    }

    private static decimal ScoreOf(IReadOnlyList<Piootoo.Shared.Models.TradingResult> trades)
    {
        decimal net = 0m, peak = 0m, drawdown = 0m, worstTrade = 0m;
        foreach (var trade in trades)
        {
            net += trade.NetProfit;
            if (trade.NetProfit < worstTrade) worstTrade = trade.NetProfit;
            if (net > peak) peak = net;
            var gap = peak - net;
            if (gap > drawdown) drawdown = gap;
        }

        var floor = Math.Max(drawdown, -worstTrade);
        return floor > 0m ? net / floor : net;
    }

    public string Describe() =>
        $"peggiore di {SubPeriods} sotto-periodi (netto/drawdown con pavimento sulla perdita massima), " +
        $"almeno {MinTrades} trade e {MinTradesPerSubPeriod} per tratto" +
        (MinAverageTrade > 0m ? $", utile medio ≥ {MinAverageTrade:N0}" : string.Empty) +
        (MinProfitFactor > 0m ? $", profit factor ≥ {MinProfitFactor:N2}" : string.Empty);
}
