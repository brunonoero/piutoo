using Piootoo.Core.Services;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La gerarchia overnight/overweek: <b>decide prima il piano</b>, e solo se il piano concede di
/// tenere decidono motore e strategia. In una riga: <c>tiene = pianoPermette &amp;&amp;
/// strategiaVuole</c>, con la strategia libera di chiudere prima e mai dopo.
///
/// <para>Questi test tengono ferma la composizione, perché è l'unico punto in cui le due parti si
/// incontrano: il backtest interno e la sessione che costruisce gli intent per il cBot chiamano lo
/// stesso <see cref="HoldingResolver"/>, e se le due chiamate divergessero tornerebbe la classe di
/// bug del 26/08/2026 — due motori che tagliano in istanti diversi senza che nulla lo dica.</para>
/// </summary>
public class HoldingPolicyTests
{
    private static readonly DateTime Barra = new(2026, 8, 27, 14, 0, 0, DateTimeKind.Utc);

    /// <summary>Piano permissivo: la deadline resta quella della strategia, chiunque essa sia.</summary>
    [Fact]
    public void ConOvernightConcesso_LaParolaRestaAllaStrategia()
    {
        var deadline = Barra.AddHours(3);

        var senzaDeadline = HoldingResolver.Resolve(null, Barra, AccountHoldingPolicy.Default);
        var conDeadline = HoldingResolver.Resolve(deadline, Barra, AccountHoldingPolicy.Default);

        Assert.Null(senzaDeadline.AtUtc);
        Assert.False(senzaDeadline.FromAccountPolicy);
        Assert.Equal(deadline, conDeadline.AtUtc);
        Assert.False(conDeadline.FromAccountPolicy);
    }

    /// <summary>
    /// Piano che vieta l'overnight su una strategia che non dichiara uscite: la posizione riceve
    /// comunque una deadline, ed è del conto. È il caso della prop che impone il flat.
    /// </summary>
    [Fact]
    public void SenzaOvernight_UnaMultidayRiceveLaDeadlineDelConto()
    {
        var piano = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtcHhmm = 2045 };

        var decisione = HoldingResolver.Resolve(null, Barra, piano);

        Assert.Equal(new DateTime(2026, 8, 27, 20, 45, 0, DateTimeKind.Utc), decisione.AtUtc);
        Assert.True(decisione.FromAccountPolicy);
    }

    /// <summary>
    /// Il permesso non è un obbligo: una strategia che chiude prima del flat del conto non viene
    /// allungata fino a lì. Vince sempre la deadline più stretta.
    /// </summary>
    [Fact]
    public void SenzaOvernight_UnaIntradayChiudeComunquePrima()
    {
        var piano = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtcHhmm = 2045 };
        var fineSessione = new DateTime(2026, 8, 27, 16, 59, 0, DateTimeKind.Utc);

        var decisione = HoldingResolver.Resolve(fineSessione, Barra, piano);

        Assert.Equal(fineSessione, decisione.AtUtc);
        Assert.False(decisione.FromAccountPolicy);
    }

    /// <summary>
    /// Una deadline della strategia oltre il flat del conto viene troncata, e il troncamento è
    /// dichiarato: è ciò che distingue <c>SessionFlat</c> da <c>TimeExit</c> nei trade.
    /// </summary>
    [Fact]
    public void SenzaOvernight_UnaDeadlineOltreIlFlatVieneTroncata()
    {
        var piano = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtcHhmm = 2045 };

        var decisione = HoldingResolver.Resolve(Barra.AddDays(3), Barra, piano);

        Assert.Equal(new DateTime(2026, 8, 27, 20, 45, 0, DateTimeKind.Utc), decisione.AtUtc);
        Assert.True(decisione.FromAccountPolicy);
    }

    /// <summary>
    /// L'ordine nato dopo l'ora del flat appartiene alla giornata di trading successiva: la sua
    /// deadline è il flat del giorno dopo, non uno già passato che lo chiuderebbe all'apertura.
    /// </summary>
    [Fact]
    public void IlFlatSiMisuraSullaBarraDellOrdine_NonSulCalendario()
    {
        var piano = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtcHhmm = 2045 };
        var dopoIlFlat = new DateTime(2026, 8, 27, 23, 30, 0, DateTimeKind.Utc);

        var decisione = HoldingResolver.Resolve(null, dopoIlFlat, piano);

        Assert.Equal(new DateTime(2026, 8, 28, 20, 45, 0, DateTimeKind.Utc), decisione.AtUtc);
    }

    /// <summary>
    /// Overweek senza overnight non descrive alcun conto reale ed è quasi sempre una spunta
    /// dimenticata: va rifiutato, non risolto in silenzio.
    /// </summary>
    [Fact]
    public void OverweekSenzaOvernight_VieneRifiutato()
    {
        var piano = new AccountHoldingPolicy { AllowOvernight = false, AllowOverweek = true };

        Assert.Throws<InvalidOperationException>(piano.Validate);
    }

    /// <summary>
    /// L'elenco che alimenta l'avviso del dettaglio piano: solo le strategie che il piano taglia,
    /// ciascuna col taglio che le tocca. Una intraday non compare mai, qualunque sia il piano.
    /// </summary>
    [Fact]
    public void IConflittiElencanoSoloLeStrategieCheIlPianoTaglia()
    {
        var strategie = new[]
        {
            ("PTS_A", "PTS_A", StrategyHolding.Intraday),
            ("PTS_B", "PTS_B", StrategyHolding.Multiday)
        };

        var soloWeekend = HoldingResolver.FindConflicts(strategie, AccountHoldingPolicy.Default);
        var conflitto = Assert.Single(soloWeekend);
        Assert.Equal("PTS_B", conflitto.StrategyCode);
        Assert.True(conflitto.CutAtWeekEnd);
        Assert.False(conflitto.CutAtSessionFlat);

        var senzaOvernight = HoldingResolver.FindConflicts(
            strategie, AccountHoldingPolicy.Default with { AllowOvernight = false });
        var troncata = Assert.Single(senzaOvernight);
        Assert.True(troncata.CutAtSessionFlat);
        // Il taglio più stretto assorbe l'altro: dirli entrambi farebbe contare due volte lo
        // stesso trade nell'avviso.
        Assert.False(troncata.CutAtWeekEnd);

        Assert.Empty(HoldingResolver.FindConflicts(strategie, AccountHoldingPolicy.Unrestricted));
    }

    /// <summary>
    /// La guardia sul ramo di parita' daily. Il motore di ricerca non applica l'uscita di sessione
    /// su D1, quindi <c>SessionExitFromIntradayOnly</c> la disattiva a 1440 anche se la classe
    /// dichiara <c>IntradayOnly = true</c>: e' una regola di parita', non una deduzione.
    ///
    /// <para>Il punto e' che nessuno debba <b>dipendere</b> da quell'esenzione senza saperlo. Tutte
    /// e dieci le strategie daily a catalogo dichiarano gia' <c>IntradayOnly = false</c>, quindi il
    /// ramo e' inerte; se ne comparisse una che lo lascia a true, la sua tenuta sarebbe decisa dal
    /// timeframe invece che dal report della ricerca — ed e' qui che ci si accorge.</para>
    /// </summary>
    [Fact]
    public void LeStrategieDailyDelCatalogoNonDipendonoDallEsenzioneD1()
    {
        var sospette = StrategyFactory.GetRegisteredStrategies()
            .Where(strategy => strategy.TimeframeMinutes >= 1440)
            .Where(strategy => StrategyFactory.CreateStrategy(strategy.Id, strategy.Symbol, strategy.TimeframeMinutes)
                                   is Easy.Engines.EasyEngineBase engine
                               && engine.DependsOnDailySessionExitExemption)
            .Select(strategy => strategy.Id)
            .ToList();

        Assert.True(sospette.Count == 0,
            "Strategie daily che dichiarano IntradayOnly = true e restano multiday solo grazie " +
            "all'esenzione D1: " + string.Join(", ", sospette) +
            ". Dichiara IntradayOnly = false se il report della ricerca ha intraday_only = 0, " +
            "cosi' la tenuta la decide la strategia e non il timeframe.");
    }

    /// <summary>
    /// Il catalogo dichiara la tenuta di ogni strategia, ed è coerente: nessuna può tenere il fine
    /// settimana senza tenere la notte. È il dato su cui si reggono la colonna della griglia e
    /// l'avviso del piano.
    /// </summary>
    [Fact]
    public void OgniStrategiaDelCatalogoDichiaraUnaTenutaCoerente()
    {
        var incoerenti = StrategyFactory.GetRegisteredStrategies()
            .Where(strategy => strategy.Holding.Overweek && !strategy.Holding.Overnight)
            .Select(strategy => strategy.Id)
            .ToList();

        Assert.Empty(incoerenti);
    }
}
