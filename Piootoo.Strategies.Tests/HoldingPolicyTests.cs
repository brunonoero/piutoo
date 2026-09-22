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

    /// <summary>
    /// Piano che non vieta nulla: la deadline resta quella della strategia, chiunque essa sia — e se
    /// la strategia non ne ha, il segnale esce senza.
    ///
    /// <para>Il piano permissivo e' <see cref="AccountHoldingPolicy.Unrestricted"/>, non
    /// <c>Default</c>: il default vieta l'overweek, e da quando il divieto e' una deadline sul
    /// segnale quel piano una scadenza la impone eccome (vedi
    /// <see cref="SenzaOverweek_LaChiusuraSettimanaleFinisceSulSegnale"/>).</para>
    /// </summary>
    [Fact]
    public void ConTuttoConcesso_LaParolaRestaAllaStrategia()
    {
        var deadline = Barra.AddHours(3);

        var senzaDeadline = HoldingResolver.Resolve(null, Barra, AccountHoldingPolicy.Unrestricted);
        var conDeadline = HoldingResolver.Resolve(deadline, Barra, AccountHoldingPolicy.Unrestricted);

        Assert.Null(senzaDeadline.AtUtc);
        Assert.False(senzaDeadline.FromAccountPolicy);
        Assert.Equal(deadline, conDeadline.AtUtc);
        Assert.False(conDeadline.FromAccountPolicy);
    }

    /// <summary>
    /// Overnight concesso ma overweek no: il segnale esce con la chiusura del fine settimana gia'
    /// addosso, invece di affidarla a chi lo esegue.
    ///
    /// <para>Prima il fine settimana era solo una finestra applicata due volte — dal loop di
    /// backtest e dal cBot — su due orologi diversi. Portandola sul segnale l'istante e' deciso una
    /// volta sola: <see cref="Barra"/> e' giovedi', la deadline e' il venerdi' successivo all'ora
    /// dichiarata dal piano.</para>
    /// </summary>
    [Fact]
    public void SenzaOverweek_LaChiusuraSettimanaleFinisceSulSegnale()
    {
        var piano = AccountHoldingPolicy.Default with
        {
            AllowOvernight = true,
            AllowOverweek = false,
            WeekEnd = new WeekEndFlatPolicy(new TimeOnly(20, 45), new TimeOnly(23, 0))
        };

        var decisione = HoldingResolver.Resolve(null, Barra, piano);

        Assert.Equal(new DateTime(2026, 8, 28, 20, 45, 0, DateTimeKind.Utc), decisione.AtUtc);
        Assert.True(decisione.FromAccountPolicy);
    }

    /// <summary>
    /// Anche qui vince la scadenza piu' stretta: una strategia che chiude prima del venerdi' non
    /// viene allungata fino al fine settimana.
    /// </summary>
    [Fact]
    public void SenzaOverweek_UnaStrategiaCheChiudePrima_RestaLaSua()
    {
        var piano = AccountHoldingPolicy.Default with { AllowOverweek = false };
        var suo = Barra.AddHours(2);

        var decisione = HoldingResolver.Resolve(suo, Barra, piano);

        Assert.Equal(suo, decisione.AtUtc);
        Assert.False(decisione.FromAccountPolicy);
    }

    /// <summary>
    /// Con entrambi i divieti attivi vince il flat di sessione, che cade prima: la composizione e'
    /// il minimo, non l'ultimo divieto letto.
    /// </summary>
    [Fact]
    public void ConEntrambiIDivieti_VinceIlFlatDiSessione()
    {
        var piano = AccountHoldingPolicy.Default with
        {
            AllowOvernight = false,
            AllowOverweek = false,
            SessionFlatUtc = new TimeOnly(20, 45),
            WeekEnd = new WeekEndFlatPolicy(new TimeOnly(20, 45), new TimeOnly(23, 0))
        };

        var decisione = HoldingResolver.Resolve(null, Barra, piano);

        // Giovedi' 20:45, non venerdi': chi taglia ogni sera taglia prima del fine settimana.
        Assert.Equal(new DateTime(2026, 8, 27, 20, 45, 0, DateTimeKind.Utc), decisione.AtUtc);
        Assert.True(decisione.FromAccountPolicy);
    }

    /// <summary>
    /// Piano che vieta l'overnight su una strategia che non dichiara uscite: la posizione riceve
    /// comunque una deadline, ed è del conto. È il caso della prop che impone il flat.
    /// </summary>
    [Fact]
    public void SenzaOvernight_UnaMultidayRiceveLaDeadlineDelConto()
    {
        var piano = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtc = new TimeOnly(20, 45) };

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
        var piano = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtc = new TimeOnly(20, 45) };
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
        var piano = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtc = new TimeOnly(20, 45) };

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
        var piano = AccountHoldingPolicy.Default with { AllowOvernight = false, SessionFlatUtc = new TimeOnly(20, 45) };
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

    // ------------------------------------------------------------ la finestra del flat di sessione

    private static readonly AccountHoldingPolicy FlatAlle2045 = AccountHoldingPolicy.Default with
    {
        AllowOvernight = false,
        AllowOverweek = false,
        SessionFlatUtc = new TimeOnly(20, 45),
        SessionFlatWindowMinutes = 30
    };

    /// <summary>
    /// Il flat e' una finestra <c>[flat, flat + minuti)</c>: l'istante di inizio e' dentro, quello
    /// di fine e' fuori, e un secondo prima dell'inizio e' fuori. Il caso del secondo prima e'
    /// quello che un troncamento a intero sbaglia: <c>(int)(-0,5)</c> vale 0.
    /// </summary>
    [Theory]
    [InlineData(20, 44, 59, false)]
    [InlineData(20, 45, 0, true)]
    [InlineData(20, 45, 30, true)]
    [InlineData(21, 14, 59, true)]
    [InlineData(21, 15, 0, false)]
    [InlineData(23, 30, 0, false)]
    public void LaFinestraDelFlatIncludeLInizioEdEscludeLaFine(int ora, int minuto, int secondo, bool dentro)
    {
        var istante = new DateTime(2026, 8, 27, ora, minuto, secondo, DateTimeKind.Utc);

        Assert.Equal(dentro, FlatAlle2045.IsInsideSessionFlatWindow(istante));
    }

    /// <summary>Una finestra puo' passare la mezzanotte: la distanza dall'inizio si misura modulo un giorno.</summary>
    [Fact]
    public void LaFinestraDelFlatPuoPassareLaMezzanotte()
    {
        var piano = FlatAlle2045 with { SessionFlatUtc = new TimeOnly(23, 50), SessionFlatWindowMinutes = 30 };

        Assert.True(piano.IsInsideSessionFlatWindow(new DateTime(2026, 8, 27, 23, 55, 0, DateTimeKind.Utc)));
        Assert.True(piano.IsInsideSessionFlatWindow(new DateTime(2026, 8, 28, 0, 10, 0, DateTimeKind.Utc)));
        Assert.False(piano.IsInsideSessionFlatWindow(new DateTime(2026, 8, 28, 0, 20, 0, DateTimeKind.Utc)));
        Assert.Equal(new TimeOnly(0, 20), piano.SessionFlatUntilUtc);
    }

    /// <summary>
    /// Il trigger scatta una volta sola, sul primo tick che raggiunge o supera l'ora del flat: e'
    /// cio' che permette al backtest di cancellare i pending senza ripeterlo a ogni barra. E scatta
    /// anche con un orologio piu' largo della finestra — un tick a quattro ore salta le 20:45 ma
    /// arriva alle 00:00 — altrimenti su quei run il flat non esisterebbe.
    /// </summary>
    [Fact]
    public void IlTriggerDelFlatScattaUnaVoltaSolaAlPrimoTickCheRaggiungeLOra()
    {
        var prima = new DateTime(2026, 8, 27, 20, 30, 0, DateTimeKind.Utc);
        var flat = new DateTime(2026, 8, 27, 20, 45, 0, DateTimeKind.Utc);
        var dopo = new DateTime(2026, 8, 27, 21, 0, 0, DateTimeKind.Utc);

        Assert.True(FlatAlle2045.IsSessionFlatTrigger(flat, prima));
        Assert.False(FlatAlle2045.IsSessionFlatTrigger(dopo, flat));
        Assert.False(FlatAlle2045.IsSessionFlatTrigger(prima, prima.AddMinutes(-15)));

        // Orologio a quattro ore: il tick delle 00:00 e' il primo dopo le 20:45.
        var mezzanotte = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(FlatAlle2045.IsSessionFlatTrigger(mezzanotte, mezzanotte.AddHours(-4)));
        Assert.False(FlatAlle2045.IsSessionFlatTrigger(mezzanotte.AddHours(4), mezzanotte));
    }

    /// <summary>
    /// Il buco che la finestra chiude: un ingresso valido fra il flat e il rollover riceveva la
    /// deadline del giorno dopo e attraversava la notte. Ora non nasce. Un ingresso valido dopo la
    /// finestra nasce, e la sua deadline e' il flat del giorno dopo: nessun rollover in mezzo.
    /// </summary>
    [Fact]
    public void UnIngressoDentroLaFinestraDelFlatNonNasce()
    {
        var dentro = new DateTime(2026, 8, 27, 20, 45, 0, DateTimeKind.Utc);
        var dopo = new DateTime(2026, 8, 27, 21, 15, 0, DateTimeKind.Utc);

        Assert.True(HoldingResolver.BlocksEntry(dentro, FlatAlle2045));
        Assert.False(HoldingResolver.BlocksEntry(dopo, FlatAlle2045));
        Assert.Equal(
            new DateTime(2026, 8, 28, 20, 45, 0, DateTimeKind.Utc),
            HoldingResolver.Resolve(null, dopo, FlatAlle2045).AtUtc);
    }

    /// <summary>Con l'overnight permesso la finestra giornaliera non esiste: il piano non promette niente.</summary>
    [Fact]
    public void ConOvernightPermesso_LaFinestraDelFlatNonBloccaNiente()
    {
        var dentro = new DateTime(2026, 8, 27, 20, 45, 0, DateTimeKind.Utc);

        Assert.False(HoldingResolver.BlocksEntry(dentro, AccountHoldingPolicy.Unrestricted));
        Assert.False(HoldingResolver.BlocksEntry(dentro, AccountHoldingPolicy.Default));
        Assert.Empty(HoldingResolver.RolloversOutsideSessionFlatWindow(
            AccountHoldingPolicy.Unrestricted, [new SwapSpec("@FDAX", 5m, 1m, new TimeOnly(9, 0))]));
    }

    /// <summary>
    /// La finestra del fine settimana blocca allo stesso modo: e' la stessa regola su un altro
    /// asse, e con l'overweek concesso il venerdi' sera resta un venerdi' qualunque.
    /// </summary>
    [Fact]
    public void LaFinestraDelFineSettimanaBloccaGliIngressiSoloSeIlPianoVietaLOverweek()
    {
        var venerdiSera = new DateTime(2026, 8, 28, 21, 30, 0, DateTimeKind.Utc);

        Assert.True(HoldingResolver.BlocksEntry(venerdiSera, AccountHoldingPolicy.Default));
        Assert.False(HoldingResolver.BlocksEntry(venerdiSera, AccountHoldingPolicy.Unrestricted));
    }

    /// <summary>
    /// Il flat promette di non attraversare il rollover, e la promessa regge solo se il rollover
    /// cade dentro la finestra: prima dell'inizio lo pagano le posizioni ancora aperte, alla fine o
    /// dopo lo attraversa un ingresso nato appena la finestra si chiude.
    /// </summary>
    [Theory]
    [InlineData(20, 59, true)]
    [InlineData(21, 0, true)]
    [InlineData(20, 45, true)]
    [InlineData(20, 30, false)]
    [InlineData(21, 15, false)]
    [InlineData(22, 0, false)]
    public void IlRolloverDeveCadereDentroLaFinestraDelFlat(int ora, int minuto, bool coperto)
    {
        var rollover = new TimeOnly(ora, minuto);

        Assert.Equal(coperto, FlatAlle2045.SessionFlatWindowCoversRollover(rollover));

        var fuori = HoldingResolver.RolloversOutsideSessionFlatWindow(
            FlatAlle2045, [new SwapSpec("@GC", 0.6m, 0m, rollover)]);
        Assert.Equal(coperto ? 0 : 1, fuori.Count);
        if (!coperto) Assert.Contains("@GC", fuori[0]);
    }

    /// <summary>
    /// Una finestra vuota riaprirebbe in silenzio il buco fra flat e rollover; una di mezza giornata
    /// terrebbe il conto fermo per ore. Nessuna delle due descrive un conto.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(721)]
    public void UnaFinestraDelFlatFuoriMisuraVieneRifiutata(int minuti)
    {
        var piano = FlatAlle2045 with { SessionFlatWindowMinutes = minuti };

        Assert.Throws<InvalidOperationException>(piano.Validate);
        FlatAlle2045.Validate();
    }

    /// <summary>Il flat dentro la propria finestra e' "adesso", come per il fine settimana: una posizione li' non deve esistere.</summary>
    [Fact]
    public void DentroLaFinestra_IlFlatEAdesso()
    {
        var dentro = new DateTime(2026, 8, 27, 20, 50, 0, DateTimeKind.Utc);

        Assert.Equal(dentro, FlatAlle2045.ResolveSessionFlatUtc(dentro));
        Assert.Equal("flat di sessione 20:45 → 21:15 UTC", FlatAlle2045.Describe());
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
