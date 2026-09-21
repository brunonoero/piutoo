namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Il costo di finanziamento di una posizione tenuta oltre il rollover del broker, per un simbolo.
///
/// <para><b>Perche' esiste, e perche' e' arrivato tardi.</b> Una strategia che la ricerca considera
/// <i>intraday</i> puo' pagare il finanziamento lo stesso, e ogni giorno: basta che la sua fine
/// sessione cada dopo l'ora di rollover del broker. E' successo esattamente cosi' su
/// <c>PT3B_FDAX_PCH_001_240</c>, che chiude a fine sessione — l'01:00 di Roma — mentre ICS fa
/// rollover alle 21:00 UTC: 879 trade su 1.317 restavano aperti oltre il rollover per poche ore e
/// pagavano una notte intera. Nel backtest su tick di cTrader sono <b>$58.564</b> su $212.344 di
/// lordo, il 28%, che il motore interno non vedeva affatto.</para>
///
/// <para><b>E' una MISURA, non un parametro</b>, come lo spread: si legge dalle specifiche del
/// simbolo presso quel broker e non si sceglie. Due run con swap diverso non sono confrontabili, e
/// per questo il run lo dichiara nei propri artefatti.</para>
/// </summary>
/// <param name="Symbol">Simbolo normalizzato, come lo scrivono le strategie.</param>
/// <param name="LongPointsPerNight">
/// Punti addebitati a una posizione <b>long</b> per ogni rollover attraversato. Positivo = costo.
/// Su DE40/ICS vale 5,054 punti, cioe' i −50,54 pip della scheda per un pip da 0,1 punti.
/// </param>
/// <param name="ShortPointsPerNight">Come sopra per lo <b>short</b>: su DE40/ICS 1,215 punti.</param>
/// <param name="RolloverUtc">Ora del rollover. Su ICS le 21:00 UTC.</param>
/// <param name="TripleDay">
/// Il giorno a cui il broker addebita anche il fine settimana. <c>null</c> = nessun triplo.
///
/// <para>Il triplo si applica solo a chi <b>attraversa</b> il fine settimana: chi apre e chiude
/// dentro il venerdi' non paga nulla, ed e' il caso normale di una strategia intraday. Misurato
/// sulla history ICS: dei 91 long entrati di venerdi' e usciti dopo il rollover ne hanno pagato
/// <b>tre</b>, e quei tre al triplo.</para>
/// </param>
public sealed record SwapSpec(
    string Symbol,
    decimal LongPointsPerNight,
    decimal ShortPointsPerNight,
    TimeOnly RolloverUtc,
    DayOfWeek? TripleDay = DayOfWeek.Friday)
{
    /// <summary>Quanti punti costa tenere una posizione da <paramref name="fromUtc"/> a <paramref name="toUtc"/>.</summary>
    /// <param name="isLong">Il lato: i due tassi sono molto diversi — su DE40 il long paga quattro volte lo short.</param>
    public decimal PointsFor(DateTime fromUtc, DateTime toUtc, bool isLong)
    {
        var rate = isLong ? LongPointsPerNight : ShortPointsPerNight;
        if (rate <= 0m || toUtc <= fromUtc) return 0m;

        var total = 0m;
        // Si parte dal primo rollover successivo all'apertura e si avanza di giorno in giorno.
        var rollover = fromUtc.Date.Add(RolloverUtc.ToTimeSpan());
        if (rollover <= fromUtc) rollover = rollover.AddDays(1);

        while (rollover < toUtc)
        {
            total += rate * Multiplier(rollover, toUtc);
            rollover = rollover.AddDays(1);
        }

        return total;
    }

    /// <summary>
    /// Il moltiplicatore di un singolo rollover. Il fine settimana non si addebita mai, e il giorno
    /// del triplo lo si paga <b>solo restando aperti oltre il fine settimana</b>: chiudere il
    /// venerdi' sera, poche ore dopo il rollover, non costa niente.
    /// </summary>
    private decimal Multiplier(DateTime rollover, DateTime closeUtc)
    {
        var day = rollover.DayOfWeek;
        if (day is DayOfWeek.Saturday or DayOfWeek.Sunday) return 0m;
        if (TripleDay is null || day != TripleDay.Value) return 1m;

        // Due giorni dopo il rollover del venerdi' c'e' quello della domenica, che il broker non
        // addebita: se la posizione e' ancora aperta allora, il fine settimana lo ha attraversato.
        return closeUtc > rollover.AddDays(2) ? 3m : 0m;
    }
}
