using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il finanziamento oltre il rollover, tarato sui numeri veri.
///
/// <para>La misura viene dalla scheda di DE40 presso ICS — swap long −50,54 pip, short −12,15 pip,
/// pip da 0,1 punti, rollover alle 21:00, triplo il venerdi', weekend disabilitato — ed e' stata
/// verificata contro la history di 1.317 trade del backtest su tick: ogni long che attraversa il
/// rollover paga esattamente $126,35 e ogni short $30,38, con un contratto da 25 € a punto.</para>
/// </summary>
public sealed class SwapSpecTests
{
    // 50,54 pip × 0,1 punti = 5,054 punti; 12,15 pip × 0,1 = 1,215 punti.
    private static readonly SwapSpec Dax = new("@FDAX", 5.054m, 1.215m, new TimeOnly(21, 0));

    private const decimal PuntoInDenaro = 25m;

    /// <summary>
    /// Il caso che ha fatto scoprire tutto: ingresso al mattino, uscita a fine sessione — l'01:00
    /// di Roma, cioe' le 23:59 UTC — e una notte intera di finanziamento per tre ore di posizione
    /// oltre il rollover.
    /// </summary>
    [Fact]
    public void UnIntradayCheChiudeDopoIlRolloverPagaUnaNotte()
    {
        var ingresso = new DateTime(2022, 1, 3, 8, 0, 0, DateTimeKind.Utc);   // lunedi'
        var uscita = new DateTime(2022, 1, 3, 23, 59, 0, DateTimeKind.Utc);

        Assert.Equal(126.35m, Dax.PointsFor(ingresso, uscita, isLong: true) * PuntoInDenaro, 2);
        Assert.Equal(30.38m, Dax.PointsFor(ingresso, uscita, isLong: false) * PuntoInDenaro, 2);
    }

    /// <summary>Chiudere PRIMA del rollover non costa nulla: e' la differenza che decide la strategia.</summary>
    [Fact]
    public void ChiuderePrimaDelRolloverNonCostaNulla()
    {
        var ingresso = new DateTime(2022, 1, 3, 8, 0, 0, DateTimeKind.Utc);
        var uscita = new DateTime(2022, 1, 3, 20, 59, 0, DateTimeKind.Utc);

        Assert.Equal(0m, Dax.PointsFor(ingresso, uscita, isLong: true));
    }

    /// <summary>
    /// Il venerdi' sera non si paga, anche uscendo dopo il rollover: il triplo esiste ma tocca solo
    /// chi sopravvive al fine settimana. Misurato sulla history: 88 long su 91 entrati di venerdi'
    /// e usciti dopo le 21:00 non hanno pagato niente.
    /// </summary>
    [Fact]
    public void IlVenerdiSeraNonSiPagaSeSiChiudeSubito()
    {
        var ingresso = new DateTime(2022, 1, 7, 8, 0, 0, DateTimeKind.Utc);   // venerdi'
        var uscita = new DateTime(2022, 1, 7, 23, 59, 0, DateTimeKind.Utc);

        Assert.Equal(0m, Dax.PointsFor(ingresso, uscita, isLong: true));
    }

    /// <summary>Chi attraversa il fine settimana paga il triplo, e una volta sola.</summary>
    [Fact]
    public void AttraversareIlFineSettimanaCostaIlTriplo()
    {
        var ingresso = new DateTime(2022, 1, 7, 8, 0, 0, DateTimeKind.Utc);   // venerdi'
        var uscita = new DateTime(2022, 1, 10, 10, 0, 0, DateTimeKind.Utc);   // lunedi'

        // Tre notti per il venerdi' (sabato e domenica non si addebitano affatto) piu' nessun'altra:
        // il rollover del lunedi' cade dopo la chiusura.
        Assert.Equal(379.05m, Dax.PointsFor(ingresso, uscita, isLong: true) * PuntoInDenaro, 2);
    }

    /// <summary>Una multiday infrasettimanale paga una notte per ogni rollover attraversato.</summary>
    [Fact]
    public void UnaMultidayPagaUnaNottePerRollover()
    {
        var ingresso = new DateTime(2022, 1, 3, 8, 0, 0, DateTimeKind.Utc);   // lunedi'
        var uscita = new DateTime(2022, 1, 6, 10, 0, 0, DateTimeKind.Utc);    // giovedi'

        // Rollover di lunedi', martedi' e mercoledi': tre notti.
        Assert.Equal(3 * 126.35m, Dax.PointsFor(ingresso, uscita, isLong: true) * PuntoInDenaro, 2);
    }

    /// <summary>
    /// Senza misura non si inventa un costo: un simbolo che il broker non documenta vale zero, come
    /// i run sui future del vendor, dove lo swap non esiste affatto.
    /// </summary>
    [Fact]
    public void SenzaTassoNonSiAddebitaNulla()
    {
        var vuoto = new SwapSpec("@NQ", 0m, 0m, new TimeOnly(21, 0));
        var ingresso = new DateTime(2022, 1, 3, 8, 0, 0, DateTimeKind.Utc);
        var uscita = new DateTime(2022, 1, 5, 8, 0, 0, DateTimeKind.Utc);

        Assert.Equal(0m, vuoto.PointsFor(ingresso, uscita, isLong: true));
    }
}
