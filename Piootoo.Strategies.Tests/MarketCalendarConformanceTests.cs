using Piootoo.Shared.Configuration;
using Piootoo.Shared.MarketData;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Passo 1 del refactor del layer barre: il calendario di mercato è diventato un <b>dato</b>
/// (<c>Piootoo.Shared/MarketData/market-calendars.json</c>) e <see cref="InstrumentRegistry"/> lo
/// inoltra invece di deciderlo.
///
/// <para><b>Perché questo test esiste.</b> Spostare una tabella da C# a JSON è il tipo di modifica
/// che non produce errori di compilazione e sbaglia in silenzio: un fuso trascritto male o un
/// <c>sessionStartHour</c> perso non fanno fallire nulla, spostano il confine di sessione di ore.
/// Le attese qui sotto sono la tabella <b>com'era prima</b> del passaggio al file, trascritte a
/// mano come copia indipendente: è quella copia a dimostrare che la migrazione non ha cambiato un
/// valore. Non vanno "aggiornate per far passare il test" — se divergono, è il file che ha
/// cambiato qualcosa.</para>
///
/// <para>Il test gemello <c>ResearchSessionStartConformanceTests</c> verifica la stessa tabella dal
/// lato delle strategie: che ogni <c>PTS_*</c> dichiari l'ora del proprio strumento. Qui si verifica
/// che l'ora dello strumento sia rimasta quella che era.</para>
/// </summary>
public sealed class MarketCalendarConformanceTests
{
    private const string CmeChicago = "America/Chicago";
    private const string NyComexNymex = "America/New_York";
    private const string EurexFrankfurt = "Europe/Berlin";
    private const string IceNewYork = "America/New_York";

    /// <summary>Lunedì-venerdì: gli europei e i softs ICE, che la domenica non aprono mai.</summary>
    private static readonly DayOfWeek[] MonToFri =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    ];

    /// <summary>Lunedì-venerdì più la domenica: i CME, colonna <c>dom</c> a 48 (27 per BTC).</summary>
    private static readonly DayOfWeek[] MonToFriPlusSunday =
    [
        DayOfWeek.Sunday,
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    ];

    /// <summary>Lunedì-venerdì più il sabato: solo SB, colonna <c>sab</c> a 14.</summary>
    private static readonly DayOfWeek[] MonToFriPlusSaturday =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
        DayOfWeek.Saturday
    ];

    /// <summary>
    /// La tabella com'era in <c>InstrumentRegistry.Specs</c> prima del 07/09/2026: simbolo, fuso di
    /// borsa, ora di inizio sessione della ricerca, giorni di sessione (<c>null</c> = non
    /// dichiarati).
    /// </summary>
    public static TheoryData<string, string, int, DayOfWeek[]?> TableBeforeTheFile => new()
    {
        // --- Indici USA
        { "ES", CmeChicago, 0, MonToFriPlusSunday },
        { "MES", CmeChicago, 0, null },
        { "NQ", CmeChicago, 0, MonToFriPlusSunday },
        { "MNQ", CmeChicago, 0, null },
        { "YM", CmeChicago, 0, MonToFriPlusSunday },
        { "MYM", CmeChicago, 0, null },
        { "RTY", CmeChicago, 0, null },
        { "M2K", CmeChicago, 0, null },

        // --- Indici europei
        { "FDAX", EurexFrankfurt, 1, MonToFri },
        { "FDXM", EurexFrankfurt, 0, null },
        { "FDXS", EurexFrankfurt, 0, null },
        { "FESX", EurexFrankfurt, 0, null },
        { "FGBL", EurexFrankfurt, 0, null },

        // --- Metalli
        { "GC", NyComexNymex, 0, MonToFriPlusSunday },
        { "MGC", NyComexNymex, 0, null },
        { "SI", NyComexNymex, 0, null },
        { "HG", NyComexNymex, 0, null },
        { "PL", NyComexNymex, 0, MonToFriPlusSunday },
        { "PA", NyComexNymex, 0, null },

        // --- Energia
        { "CL", NyComexNymex, 0, MonToFriPlusSunday },
        { "MCL", NyComexNymex, 0, null },
        { "NG", NyComexNymex, 0, MonToFriPlusSunday },
        { "RB", NyComexNymex, 0, null },

        // --- Cripto
        { "BTC", CmeChicago, 0, MonToFriPlusSunday },

        // --- Softs ICE US
        { "KC", IceNewYork, 1, MonToFri },
        { "CT", IceNewYork, 1, MonToFri },
        { "SB", IceNewYork, 1, MonToFriPlusSaturday },
        { "CC", IceNewYork, 1, MonToFri },

        // --- Valute CME
        { "BP", CmeChicago, 0, MonToFriPlusSunday },
        { "EC", CmeChicago, 0, null },
    };

    [Theory]
    [MemberData(nameof(TableBeforeTheFile))]
    public void RegistryReturnsTheSameValuesAsBeforeTheFile(
        string symbol, string timeZone, int sessionStartHour, DayOfWeek[]? sessionDays)
    {
        var spec = InstrumentRegistry.Get(symbol);

        Assert.Equal(timeZone, spec.SessionTimeZone);
        Assert.Equal(sessionStartHour, spec.ResearchSessionStartHour);

        if (sessionDays is null)
        {
            Assert.True(
                spec.SessionDays is null,
                $"{symbol}: i giorni di sessione non erano dichiarati e ora lo sono " +
                $"({string.Join(",", spec.SessionDays ?? new HashSet<DayOfWeek>())}). " +
                "'Non dichiarato' e 'nessun giorno' non sono la stessa cosa: chi legge non deve " +
                "inventare i giorni di uno strumento che il dossier non copre.");
            return;
        }

        Assert.NotNull(spec.SessionDays);
        Assert.Equal(sessionDays.OrderBy(day => day), spec.SessionDays!.OrderBy(day => day));
    }

    [Theory]
    [MemberData(nameof(TableBeforeTheFile))]
    public void CalendarAndRegistryAgree(
        string symbol, string timeZone, int sessionStartHour, DayOfWeek[]? sessionDays)
    {
        _ = timeZone;
        _ = sessionStartHour;
        _ = sessionDays;

        var spec = InstrumentRegistry.Get(symbol);
        var market = MarketCalendarRegistry.Current.Get(symbol);

        Assert.Equal(market.ExchangeTimeZone, spec.SessionTimeZone);
        Assert.Equal(market.SessionStartHour, spec.ResearchSessionStartHour);
        Assert.Equal(market.SessionDays, spec.SessionDays);
    }

    /// <summary>
    /// Ogni contratto verificato deve avere un calendario. Un contratto senza calendario oggi
    /// fallisce alla costruzione del registro, quindi il test non aggiunge un controllo nuovo:
    /// aggiunge il <i>messaggio</i>, cioè dice quale simbolo manca invece di far esplodere il primo
    /// che qualcuno chiede.
    /// </summary>
    [Fact]
    public void EveryVerifiedContractHasACalendar()
    {
        var calendar = MarketCalendarRegistry.Current;
        var missing = InstrumentRegistry.RegisteredSymbols
            .Where(symbol => !calendar.TryGet(symbol, out _))
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Simboli nel registro dei contratti ma non in market-calendars.json: " +
            $"{string.Join(", ", missing)}.");
    }

    /// <summary>
    /// Il fuso della ricerca è <b>uno solo per tutti i simboli</b>, e non si deduce dal fuso di
    /// borsa: è la ragione per cui i due campi del calendario sono separati.
    ///
    /// <para>Dal 07/09/2026 il calendario è l'<b>unico</b> posto in cui quel fuso è scritto —
    /// <c>ZonedWindow</c> non lo duplica più, dichiara solo <i>quale</i> dei due orologi usare — e
    /// questo test è ciò che impedisce che diventi due cose diverse per simboli diversi. I run di
    /// ricerca scrivono le finestre in CET qualunque sia lo strumento: un simbolo con un
    /// <c>researchTz</c> diverso vorrebbe dire che quella premessa è caduta, e le finestre portate
    /// verbatim finirebbero lette nell'orologio sbagliato.</para>
    /// </summary>
    [Fact]
    public void EverySymbolSharesTheSameResearchClock()
    {
        var calendar = MarketCalendarRegistry.Current;
        var distinti = calendar.Symbols
            .Select(symbol => calendar.Get(symbol).ResearchTimeZone)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(zone => zone, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Europe/Rome" }, distinti);
    }

    /// <summary>
    /// I mercati che aprono all'01:00 nella ricerca sono esattamente quelli della §2.4 del dossier —
    /// HK e HO sono usciti dal paniere il 07/09/2026 con le loro PTS. Il controllo sta sul
    /// calendario e non sulle strategie: è il file a dover restare fedele al dossier, altrimenti
    /// <c>ResearchSessionStartConformanceTests</c> imporrebbe con precisione un numero sbagliato.
    /// </summary>
    [Fact]
    public void OnlyTheDossierMarketsOpenAtOneCet()
    {
        var calendar = MarketCalendarRegistry.Current;
        var openingAtOne = calendar.Symbols
            .Where(symbol => calendar.Get(symbol).SessionStartHour == 1)
            .OrderBy(symbol => symbol, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "CC", "CT", "FDAX", "KC", "SB" }, openingAtOne);
    }

    /// <summary>
    /// Le fasi, i festivi e le chiusure anticipate sono dichiarati nel formato ma non popolati.
    /// Il test fissa lo stato di fatto invece di lasciarlo implicito: quando arriveranno i dati
    /// (una fonte verificata per i festivi, la sessione a fasi del FDAX) questo test va aggiornato
    /// <i>insieme</i> a chi li consuma, non prima.
    /// </summary>
    [Fact]
    public void PhasesHolidaysAndEarlyClosesAreDeclaredAndEmpty()
    {
        var calendar = MarketCalendarRegistry.Current;
        foreach (var symbol in calendar.Symbols)
        {
            var market = calendar.Get(symbol);
            Assert.Empty(market.Phases);
            Assert.Empty(market.Holidays);
            Assert.Empty(market.EarlyClose);
        }
    }

    [Fact]
    public void EmbeddedCalendarDeclaresAVersion()
    {
        var calendar = MarketCalendarRegistry.LoadEmbedded();

        Assert.False(string.IsNullOrWhiteSpace(calendar.SpecVersion));
        Assert.NotEmpty(calendar.Symbols);
    }

    [Theory]
    [InlineData("@NQ")]
    [InlineData("nq")]
    [InlineData(" NQ ")]
    public void SymbolIsNormalizedAsInTheRegistry(string symbol)
    {
        Assert.Equal("NQ", MarketCalendar.Normalize(symbol));
        Assert.True(MarketCalendarRegistry.Current.TryGet(symbol, out _));
    }

    [Fact]
    public void UnknownSymbolThrowsInsteadOfFallingBack()
    {
        var error = Assert.Throws<MarketCalendarException>(
            () => MarketCalendarRegistry.Current.Get("XYZ"));

        Assert.Contains("XYZ", error.Message);
    }
}

/// <summary>
/// Il lettore del formato: cosa accetta e cosa rifiuta. Sono tutti casi che, passando, darebbero un
/// calendario apparentemente valido e silenziosamente diverso da quello inteso.
/// </summary>
public sealed class MarketCalendarParsingTests
{
    private static string Document(string symbolBody) =>
        "{\"specVersion\":\"test.1\",\"symbols\":{\"NQ\":{" + symbolBody + "}}}";

    [Fact]
    public void SymbolWithoutSessionDaysStaysUndeclared()
    {
        var calendar = MarketCalendarRegistry.Parse(
            Document("""
                "exchangeTz":"America/Chicago","researchTz":"Europe/Rome","sessionStartHour":0
                """),
            "test");

        var nq = calendar.Get("NQ");
        Assert.Null(nq.SessionDays);
        Assert.False(nq.DeclaresSessionDays);
        Assert.Null(nq.HasSessionOn(DayOfWeek.Sunday));
    }

    [Fact]
    public void EmptySessionDaysListIsRejected()
    {
        var error = Assert.Throws<MarketCalendarException>(() => MarketCalendarRegistry.Parse(
            Document("""
                "exchangeTz":"America/Chicago","researchTz":"Europe/Rome","sessionStartHour":0,
                "sessionDays":[]
                """),
            "test"));

        Assert.Contains("vuoto", error.Message);
    }

    [Fact]
    public void MissingSessionStartHourIsRejectedInsteadOfDefaultingToZero()
    {
        var error = Assert.Throws<MarketCalendarException>(() => MarketCalendarRegistry.Parse(
            Document("""
                "exchangeTz":"America/Chicago","researchTz":"Europe/Rome"
                """),
            "test"));

        Assert.Contains("sessionStartHour", error.Message);
    }

    [Fact]
    public void MisspelledDayIsRejected()
    {
        Assert.Throws<MarketCalendarException>(() => MarketCalendarRegistry.Parse(
            Document("""
                "exchangeTz":"America/Chicago","researchTz":"Europe/Rome","sessionStartHour":0,
                "sessionDays":["Lun"]
                """),
            "test"));
    }

    [Fact]
    public void SpecWithoutVersionIsRejected()
    {
        Assert.Throws<MarketCalendarException>(() => MarketCalendarRegistry.Parse(
            """{"symbols":{}}""", "test"));
    }

    [Fact]
    public void PhasesDeclareTheirOwnAnchor()
    {
        var calendar = MarketCalendarRegistry.Parse(
            Document("""
                "exchangeTz":"Europe/Berlin","researchTz":"Europe/Rome","sessionStartHour":1,
                "phases":[{"name":"asian","start":"00:15","startAnchor":"utc","end":"08:00","endAnchor":"local"}]
                """),
            "test");

        var phase = Assert.Single(calendar.Get("NQ").Phases);
        Assert.Equal("asian", phase.Name);
        Assert.Equal(15, phase.StartHhmm);
        Assert.Equal(PhaseAnchor.Utc, phase.StartAnchor);
        Assert.Equal(800, phase.EndHhmm);
        Assert.Equal(PhaseAnchor.Local, phase.EndAnchor);
    }

    [Fact]
    public void UnknownAnchorIsRejected()
    {
        Assert.Throws<MarketCalendarException>(() => MarketCalendarRegistry.Parse(
            Document("""
                "exchangeTz":"Europe/Berlin","researchTz":"Europe/Rome","sessionStartHour":1,
                "phases":[{"name":"x","start":"00:15","startAnchor":"singapore","end":"08:00"}]
                """),
            "test"));
    }

    [Fact]
    public void OverrideWithADifferentVersionIsRejected()
    {
        var path = Path.Combine(Path.GetTempPath(), $"market-calendars-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{"specVersion":"versione-inventata","symbols":{}}""");

        try
        {
            var error = Assert.Throws<MarketCalendarException>(
                () => MarketCalendarRegistry.LoadOverrideFile(path));

            Assert.Contains("versione-inventata", error.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OverrideWithTheSameVersionIsAccepted()
    {
        var embedded = MarketCalendarRegistry.LoadEmbedded();
        var path = Path.Combine(Path.GetTempPath(), $"market-calendars-{Guid.NewGuid():N}.json");
        File.WriteAllText(
            path,
            "{\"specVersion\":\"" + embedded.SpecVersion + "\",\"symbols\":{" +
            "\"NQ\":{\"exchangeTz\":\"America/Chicago\",\"researchTz\":\"Europe/Rome\"," +
            "\"sessionStartHour\":0}}}");

        try
        {
            var loaded = MarketCalendarRegistry.LoadOverrideFile(path);

            Assert.Equal(embedded.SpecVersion, loaded.SpecVersion);
            Assert.Equal(0, loaded.Get("NQ").SessionStartHour);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
