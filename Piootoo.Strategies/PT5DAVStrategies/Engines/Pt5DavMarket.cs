using Piootoo.Shared.Configuration;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// Orari del mercato come li ha usati la ricerca PT5DAV (consegna v5.0 del 23/09/2026,
/// <c>piootoo-repository/PT5DAV/CONVENZIONI.md</c> §1, §2 e §6), per simbolo.
///
/// <para><b>Perche' non stanno nel calendario.</b> Il calendario di mercato
/// (<c>marketdata/market-calendars.json</c>) descrive il <i>future</i> e serve a tutte le serie;
/// questi sono gli orari del <b>CFD</b> misurati sul broker, e la fascia del DAX che la ricerca v5.0
/// ha adottato dal 14/09/2026 (<c>ORARIO_MERCATO.md</c>). Spostarli nel calendario cambierebbe le
/// barre e le sessioni delle PT3B su FDAX, che sono state trovate con l'ancoraggio all'01:00.
/// Valgono quindi per le sole PT5DAV, e stanno qui per non avere una seconda copia per classe.</para>
/// </summary>
public static class Pt5DavMarket
{
    /// <summary>Il fuso in cui la ricerca ha scritto gli orari del CFD.</summary>
    public const string NewYorkTimeZone = "America/New_York";

    /// <summary>
    /// Orari del CFD in ora di New York: quando apre, quando chiude, e l'ora entro cui una strategia
    /// intraday deve essere uscita. <see cref="Opens"/> a <c>null</c> = sempre aperto (BP, BTC).
    /// </summary>
    public sealed record CfdHours(TimeOnly? Opens, TimeOnly? Closes, TimeOnly IntradayLimit)
    {
        /// <summary>
        /// Vero se il CFD e' aperto all'orario di New York indicato. La fascia chiusa e'
        /// <c>[Closes, Opens)</c>, anche quando attraversa la mezzanotte (CC, KC).
        /// </summary>
        public bool IsOpenAt(TimeOnly newYorkTime)
        {
            if (Opens is not { } opens || Closes is not { } closes)
                return true;

            return opens < closes
                ? newYorkTime >= opens && newYorkTime < closes
                : newYorkTime >= opens || newYorkTime < closes;
        }
    }

    /// <summary>
    /// La tabella di CONVENZIONI.md §6. Un simbolo che non c'e' fa fallire: un orario del CFD
    /// inventato sposterebbe ogni uscita intraday senza produrre un messaggio.
    /// </summary>
    public static CfdHours Hours(string symbol) => InstrumentRegistry.Normalize(symbol) switch
    {
        "ES" or "NQ" or "YM" or "FDAX" or "GC" or "CL" =>
            new CfdHours(new TimeOnly(18, 5), new TimeOnly(16, 50), new TimeOnly(16, 50)),
        "BP" or "BTC" =>
            new CfdHours(null, null, new TimeOnly(17, 0)),
        "CC" =>
            new CfdHours(new TimeOnly(4, 50), new TimeOnly(13, 30), new TimeOnly(13, 30)),
        "KC" =>
            new CfdHours(new TimeOnly(4, 20), new TimeOnly(13, 30), new TimeOnly(13, 30)),
        var other => throw new KeyNotFoundException(
            $"PT5DAV: nessun orario del CFD per '{other}'. La ricerca v5.0 copre BP, BTC, CC, CL, ES, " +
            "FDAX, GC, KC, NQ e YM (CONVENZIONI.md §6).")
    };

    /// <summary>
    /// Vero se il CFD quota anche il fine settimana, e sabato e domenica sono quindi sessioni vere:
    /// solo BTC (CONVENZIONI.md §6, "BTC tratta 7 giorni su 7"). Per gli altri le barre della
    /// domenica sera sono la riapertura della settimana e stanno nella sessione del lunedi'.
    /// </summary>
    public static bool TradesOnWeekends(string symbol) => InstrumentRegistry.Normalize(symbol) == "BTC";

    /// <summary>
    /// La fascia del DAX della ricerca v5.0: le barre esistono solo fra le 08:00 e le 22:00 di Roma,
    /// e la sessione comincia alle 08:00 (<c>ORARIO_MERCATO.md</c>). <c>null</c> per gli altri
    /// simboli, dove la ricerca non taglia le barre.
    /// </summary>
    public static (TimeOnly Opens, TimeOnly Closes)? ResearchMarketHours(string symbol) =>
        InstrumentRegistry.Normalize(symbol) == "FDAX"
            ? (new TimeOnly(8, 0), new TimeOnly(22, 0))
            : null;
}
