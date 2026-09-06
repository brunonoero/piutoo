using Piootoo.Core.Services;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Quali tick il loop di backtesting salta nel fine settimana.
///
/// <para><b>Il fatto da fissare</b> non è "il fine settimana si salta", che era la regola vecchia,
/// ma che il salto sia deciso dal <b>piano</b> e non dal calendario. La sessione della ricerca è il
/// giorno di calendario <b>europeo</b>: la riapertura del lunedì cade alle 22:00 (ora legale) o
/// 23:00 (ora solare) UTC di <b>domenica</b>. Saltare sabato e domenica UTC toglieva quindi al
/// motore l'apertura di ogni lunedì — niente valutazione, niente fill, niente mark-to-market — e su
/// BTC, che quota 24/7, due giorni pieni a settimana.</para>
///
/// <para>La misura: nel feed interno le barre di sabato/domenica UTC sono il <b>21,0%</b> di
/// <c>@NQ_1440</c> (sono tutti i lunedì), il 3,6% dei 4h CME e l'1,4% degli intraday; sul confronto
/// <c>compare-0021</c> il motore interno aveva <b>zero</b> trade nel fine settimana UTC contro 110
/// su 1.273 del cBot, sullo stesso feed e sulla stessa finestra, e sul giornaliero 3 lunedì contro
/// 12.</para>
/// </summary>
public sealed class WeekEndIterationTests
{
    private const int Tick = 60;

    private static readonly AccountHoldingPolicy TieneIlFineSettimana =
        AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = true };

    private static readonly AccountHoldingPolicy PiattoNelFineSettimana =
        AccountHoldingPolicy.Default with { AllowOvernight = true, AllowOverweek = false };

    // Venerdì 20:45 UTC è l'orario dichiarato da WeekEndFlatPolicy; la riapertura è domenica 23:00.
    private static readonly DateTime SabatoMezzogiorno = new(2025, 2, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DomenicaRiaperturaCme = new(2025, 2, 9, 23, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DomenicaOraLegale = new(2025, 6, 8, 22, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Con l'overweek concesso — la condizione dei run di parità con la ricerca — il fine settimana
    /// si percorre tutto. È il difetto corretto: prima questi tick non esistevano per il motore.
    /// </summary>
    [Theory]
    [MemberData(nameof(TuttoIlFineSettimana))]
    public void ConOverweekConcesso_NessunTickDelFineSettimanaVieneSaltato(DateTime istante)
        => Assert.False(PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(
            TieneIlFineSettimana, istante, Tick));

    /// <summary>
    /// La riapertura della domenica sera è il caso che costava di più: è l'apertura della sessione
    /// del lunedì europeo, e su un giornaliero è l'unico istante in cui quella barra esiste.
    /// </summary>
    [Fact]
    public void ConOverweekConcesso_LaRiaperturaDellaDomenicaSeraNonESaltata()
    {
        Assert.False(PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(
            TieneIlFineSettimana, DomenicaRiaperturaCme, Tick));
        Assert.False(PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(
            TieneIlFineSettimana, DomenicaOraLegale, Tick));
    }

    /// <summary>
    /// Quando è il piano a vietare l'overweek il conto deve essere piatto e senza ordini: quei tick
    /// non hanno niente da valutare e restano saltati.
    /// </summary>
    [Fact]
    public void ConOverweekVietato_ITickDentroLaFinestraSonoSaltati()
    {
        Assert.True(PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(
            PiattoNelFineSettimana, SabatoMezzogiorno, Tick));
        Assert.True(PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(
            PiattoNelFineSettimana, DomenicaOraLegale, Tick));
    }

    /// <summary>
    /// Ma il tick su cui il flat SCATTA no: è quello che chiude le posizioni e cancella i pending,
    /// in fondo al corpo del loop. Saltarlo lascerebbe il conto in posizione per tutto il fine
    /// settimana proprio nel profilo che lo vieta.
    /// </summary>
    [Fact]
    public void ConOverweekVietato_IlTickInCuiIlFlatScattaNonESaltato()
    {
        var scatto = new DateTime(2025, 2, 7, 21, 0, 0, DateTimeKind.Utc); // primo tick orario dopo le 20:45
        Assert.True(PiattoNelFineSettimana.WeekEnd.IsFlatTrigger(scatto, scatto.AddMinutes(-Tick)));
        Assert.False(PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(
            PiattoNelFineSettimana, scatto, Tick));
    }

    /// <summary>Fuori dalla finestra non si salta mai, quale che sia il profilo.</summary>
    [Fact]
    public void FuoriDallaFinestra_NonSiSaltaMai()
    {
        var mercoledi = new DateTime(2025, 2, 5, 14, 0, 0, DateTimeKind.Utc);
        Assert.False(PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(
            PiattoNelFineSettimana, mercoledi, Tick));
        Assert.False(PiootooBacktestingService.IterationIsSkippedByWeekEndFlat(
            TieneIlFineSettimana, mercoledi, Tick));
    }

    public static TheoryData<DateTime> TuttoIlFineSettimana
    {
        get
        {
            var data = new TheoryData<DateTime>();
            var t = new DateTime(2025, 2, 7, 21, 0, 0, DateTimeKind.Utc); // venerdì sera
            while (t < new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc))
            {
                data.Add(t);
                t = t.AddHours(3);
            }
            return data;
        }
    }
}
