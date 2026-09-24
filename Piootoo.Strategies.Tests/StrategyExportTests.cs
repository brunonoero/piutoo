using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Strategies;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'export della scheda di una strategia (<see cref="StrategyExportService"/>).
///
/// <para>Cosa proteggono questi test: che l'export contenga davvero le tre cose per cui esiste — i
/// <b>numeri</b> della traduzione, i <b>commenti</b> di conversione, e il <b>motore Python</b> di
/// provenienza. Ognuna delle tre arriva da un meccanismo che può rompersi in silenzio: la
/// riflessione sui campi <c>protected</c>, le risorse incorporate dell'assembly, la mappa
/// motore→file Python. Un export che perde uno dei tre resta un JSON valido e sembra completo.</para>
/// </summary>
public sealed class StrategyExportTests
{
    /// <summary>
    /// La strategia di riferimento dei test: PC su FDAX 4 ore. Fino al 24/09/2026 era
    /// <c>PTS_ES_PCH_001_60</c>, eliminata con la serie PTS.
    /// </summary>
    private const string StrategiaCampione = "PT3B_FDAX_PCH_002_240";

    private static StrategyExportService CreateService() =>
        new(new PiootooSettings { BasePath = Path.Combine(FindRepositoryRoot(), "piootoo-repository") });

    [Fact]
    public void Export_PortaIParametriDelMotoreLettiDallIstanza()
    {
        var export = CreateService().Build(StrategiaCampione);

        // I parametri sono campi protected impostati nel costruttore: se la riflessione smette di
        // vederli l'export resta valido e diventa inutile, perche' e' proprio questa la parte da
        // confrontare con il report di sweep.
        Assert.Equal(1, Assert.Contains("ChannelBars", export.Parameters).Value);
        Assert.Equal(5000, Assert.Contains("StopMoney", export.Parameters).Value);
        Assert.Equal(4500, Assert.Contains("ProfitMoney", export.Parameters).Value);
        Assert.Equal(12, Assert.Contains("MaxBars", export.Parameters).Value);
        Assert.Equal(-1, Assert.Contains("SkipDay", export.Parameters).Value);
        Assert.Equal(true, Assert.Contains("IntradayOnly", export.Parameters).Value);

        // La provenienza del parametro conta: dice se il numero e' una scelta di questa strategia,
        // del motore, o della base comune.
        Assert.Equal("PriceChannelEngine", export.Parameters["ChannelBars"].DeclaredIn);
        Assert.Equal("EasyEngineBase", export.Parameters["IntradayOnly"].DeclaredIn);
    }

    [Fact]
    public void Export_PortaLaFinestraOperativaConIlProprioFuso()
    {
        var export = CreateService().Build(StrategiaCampione);

        // Gli orari della ricerca sono riportati verbatim con il loro fuso: esportarli come due
        // interi nudi rimetterebbe chi legge davanti alla conversione a mano che il progetto ha
        // gia' pagato una volta. Vedi docs/domini/orari-di-sessione-e-fusi.md.
        var finestra = Assert.IsType<ZonedWindow>(export.Parameters["TradingWindow"].Value);
        Assert.Equal(new TimeOnly(3, 0), finestra.Start);
        Assert.Equal(new TimeOnly(18, 0), finestra.End);
        Assert.Equal(InstrumentClock.Research, finestra.Clock);
    }

    [Fact]
    public void Export_NonRipeteLIdentitaFraIParametri()
    {
        var export = CreateService().Build(StrategiaCampione);

        Assert.DoesNotContain("Name", export.Parameters.Keys);
        Assert.DoesNotContain("Symbol", export.Parameters.Keys);
        Assert.DoesNotContain("TimeframeMinutes", export.Parameters.Keys);

        Assert.Equal("PT3B_FDAX_PCH_002_240", export.Identity.Id);
        Assert.Equal("PT3B_FDAX_PCH_002_240", export.Identity.ExecutionCode);
        Assert.Equal("@FDAX", export.Identity.Symbol);
        Assert.Equal(240, export.Identity.TimeframeMinutes);
        Assert.False(export.Identity.Overnight, "La PC FDAX 4h e' intraday: IntradayOnly = true.");
    }

    [Fact]
    public void Export_PortaIlContrattoPerLeggereIParametriInDenaro()
    {
        var export = CreateService().Build(StrategiaCampione);

        // Senza il valore del punto, "StopMoney = 5000" non e' confrontabile con i punti della
        // ricerca: sono 200 punti su FDAX e 100 su ES.
        var strumento = Assert.IsType<StrategyExportInstrument>(export.Instrument);
        Assert.Equal("FDAX", strumento.Symbol);
        Assert.Equal(25m, strumento.PointValue);
        Assert.Equal(1m, strumento.TickSize);
    }

    [Fact]
    public void Export_PortaIlSorgenteConICommentiDiConversione()
    {
        var export = CreateService().Build(StrategiaCampione);

        var sorgente = Assert.Single(export.Sources, document => document.Role == "strategy");
        Assert.True(sorgente.FromAssembly, "Il sorgente della strategia viene dall'assembly in esecuzione.");
        Assert.Contains("start_hour 3, end_hour 18", sorgente.Text);
        Assert.Contains("SessionExitTime = new TimeOnly(21, 0)", sorgente.Text);

        var motore = Assert.Single(export.Sources, document => document.Role == "engine");
        Assert.True(motore.FromAssembly);
        Assert.Contains("class PriceChannelEngine", motore.Text);
    }

    [Fact]
    public void Export_PortaIlMotorePythonDaCuiEStataTradotta()
    {
        var export = CreateService().Build(StrategiaCampione);

        Assert.Equal("PC", export.Conversion.EngineCode);
        Assert.Equal("PriceChannelEngine", export.Conversion.EngineClass);

        var python = Assert.Single(export.Sources, document => document.Role == "engine-python");
        Assert.Equal("python", python.Language);
        Assert.False(python.FromAssembly, "Il motore Python e' letto dal repository dati, non dall'assembly.");
        Assert.Contains("name = \"PC\"", python.Text);
    }

    // I tre casi sull'aggancio della scheda del dossier di settembre (impronta contro S-ID, impronta
    // ambigua, copertura del catalogo) sono stati tolti il 24/09/2026 con la serie PTS: erano le
    // sole strategie che quel dossier descriveva.

    /// <summary>
    /// Ogni motore C# che ha sottoclassi nel catalogo dev'essere nella mappa dei motori, altrimenti
    /// le sue strategie escono senza motore Python e senza sigla. È il modo in cui l'export si
    /// accorge di un motore nuovo: senza questo test la mancanza si vedrebbe solo aprendo il file.
    /// </summary>
    [Fact]
    public void Export_RiconosceIlMotoreDiOgniStrategiaDelCatalogo()
    {
        var service = CreateService();
        var senzaMotore = StrategyFactory.GetRegisteredStrategies()
            .Select(definizione => service.Build(definizione.Id))
            .Where(export => export.Conversion.EngineClass is null)
            .Select(export => export.Identity.Id)
            .ToList();

        Assert.True(
            senzaMotore.Count == 0,
            "Strategie senza un motore noto in StrategyExportService.EngineOrigins: " +
            string.Join(", ", senzaMotore));
    }

    /// <summary>
    /// Il dossier è citato per nome da tre punti (questo servizio, <c>tools/dossier-extract.py</c> e
    /// <c>docs/domini/mappa-strategie-pts.md</c>) e ogni edizione nuova li sposta tutti e tre. Qui
    /// fallisce quello che è rimasto indietro, invece di lasciare che l'export dica "scheda non
    /// trovata" su ogni strategia.
    /// </summary>
    [Fact]
    public void IlDossierDelPaniereCitatoDalServizioEsiste()
    {
        var dossier = Path.Combine(
            FindRepositoryRoot(), "piootoo-repository", StrategyExportService.DossierRelativePath);

        Assert.True(File.Exists(dossier), $"Dossier del paniere non trovato: {dossier}");
    }

    [Fact]
    public void Export_DiUnaStrategiaInesistenteNonRestituisceUnaSchedaVuota()
        => Assert.Throws<KeyNotFoundException>(() => CreateService().Build("PTS_NON_ESISTE_000_1"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                $"PiootooApp.sln non trovata risalendo da {AppContext.BaseDirectory}.");
    }
}
