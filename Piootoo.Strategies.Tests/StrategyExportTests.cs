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
    /// <summary>La strategia di riferimento dei test: PC su ES, con parametri di tutte le famiglie.</summary>
    private const string StrategiaCampione = "PTS_ES_PCH_001_60";

    private static StrategyExportService CreateService() =>
        new(new PiootooSettings { BasePath = Path.Combine(FindRepositoryRoot(), "piootoo-repository") });

    [Fact]
    public void Export_PortaIParametriDelMotoreLettiDallIstanza()
    {
        var export = CreateService().Build(StrategiaCampione);

        // I parametri sono campi protected impostati nel costruttore: se la riflessione smette di
        // vederli l'export resta valido e diventa inutile, perche' e' proprio questa la parte da
        // confrontare con il report di sweep.
        Assert.Equal(20, Assert.Contains("ChannelBars", export.Parameters).Value);
        Assert.Equal(4000, Assert.Contains("StopMoney", export.Parameters).Value);
        Assert.Equal(7500, Assert.Contains("ProfitMoney", export.Parameters).Value);
        Assert.Equal(1000, Assert.Contains("TrailingStopMoney", export.Parameters).Value);
        Assert.Equal(4, Assert.Contains("SkipDay", export.Parameters).Value);
        Assert.Equal(false, Assert.Contains("IntradayOnly", export.Parameters).Value);

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
        Assert.Equal(300, finestra.StartHhmm);
        Assert.Equal(200, finestra.EndHhmm);
        Assert.Equal(ZonedWindow.ResearchTimeZone, finestra.TimeZoneId);
    }

    [Fact]
    public void Export_NonRipeteLIdentitaFraIParametri()
    {
        var export = CreateService().Build(StrategiaCampione);

        Assert.DoesNotContain("Name", export.Parameters.Keys);
        Assert.DoesNotContain("Symbol", export.Parameters.Keys);
        Assert.DoesNotContain("TimeframeMinutes", export.Parameters.Keys);

        Assert.Equal("PTS_ES_PCH_001_60", export.Identity.Id);
        Assert.Equal("PTS_ES_PCH_001_60", export.Identity.ExecutionCode);
        Assert.Equal("@ES", export.Identity.Symbol);
        Assert.Equal(60, export.Identity.TimeframeMinutes);
        Assert.True(export.Identity.Overnight, "La PC ES 60m e' multiday: IntradayOnly = false.");
    }

    [Fact]
    public void Export_PortaIlContrattoPerLeggereIParametriInDenaro()
    {
        var export = CreateService().Build(StrategiaCampione);

        // Senza il valore del punto, "StopMoney = 4000" non e' confrontabile con i "200.0 pt" del
        // dossier: sono 80 punti su ES e 200 su NQ.
        var strumento = Assert.IsType<StrategyExportInstrument>(export.Instrument);
        Assert.Equal("ES", strumento.Symbol);
        Assert.Equal(50m, strumento.PointValue);
        Assert.Equal(0.25m, strumento.TickSize);
    }

    [Fact]
    public void Export_PortaIlSorgenteConICommentiDiConversione()
    {
        var export = CreateService().Build(StrategiaCampione);

        var sorgente = Assert.Single(export.Sources, document => document.Role == "strategy");
        Assert.True(sorgente.FromAssembly, "Il sorgente della strategia viene dall'assembly in esecuzione.");
        Assert.Contains("Codice sorgente", sorgente.Text);
        Assert.Contains("channel_len", sorgente.Text);

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

    /// <summary>
    /// La scheda si aggancia per <b>impronta numerica</b>, non per l'S-ID scritto nel sorgente.
    ///
    /// <para>Questo test è il motivo per cui l'aggancio non passa dall'S-ID: la classe campione
    /// dichiara <c>S43</c>, che nell'edizione corrente del dossier è una NQ 15m TF_M — un'altra
    /// strategia. Allegare quella scheda avrebbe prodotto un export che si legge come completo e
    /// descrive il run sbagliato. Vedi <c>docs/domini/mappa-strategie-pts.md</c>.</para>
    /// </summary>
    [Fact]
    public void Export_AgganciaLaSchedaPerImprontaENonPerLSIdDichiarato()
    {
        var export = CreateService().Build(StrategiaCampione);

        Assert.Equal("S43", export.Conversion.DeclaredDossierId);
        Assert.Equal("S63", export.Conversion.DossierId);

        var scheda = Assert.Single(export.Sources, document => document.Role == "dossier");
        Assert.StartsWith("### S63 · ES 1h", scheda.Text);
        Assert.Contains("| Motore | PC |", scheda.Text);
        Assert.Contains("Stop loss: **$4,000**", scheda.Text);

        Assert.Contains(export.Warnings, warning => warning.Contains("S43") && warning.Contains("S63"));
    }

    /// <summary>
    /// L'impronta è fatta di cinque numeri e nel dossier corrente quattro coppie di schede li
    /// condividono. Su quelle l'export non deve inventare un vincitore: allega entrambe e lo dice.
    /// </summary>
    [Fact]
    public void Export_ConImprontaAmbiguaAllegaTutteLeSchedeCandidate()
    {
        // GC 1h RHL stop $2.000 target $5.000: S78 e S97 coincidono anche su trailing e uscita a tempo.
        var export = CreateService().Build("PTS_GC_RHL_001_60");

        var schede = export.Sources.Where(document => document.Role == "dossier").ToList();
        Assert.Equal(2, schede.Count);
        Assert.Null(export.Conversion.DossierId);
        Assert.Contains(export.Warnings, warning => warning.Contains("2 schede"));
    }

    /// <summary>
    /// L'export deve trovare la scheda di quasi tutte le strategie del catalogo: se l'aggancio si
    /// rompesse — un formato del dossier cambiato, una sigla di motore nuova — i singoli export
    /// continuerebbero a uscire, solo senza la parte che spiega da dove vengono.
    /// </summary>
    [Fact]
    public void Export_TrovaLaSchedaDiRicercaPerQuasiTutteLeStrategie()
    {
        var service = CreateService();
        var totali = StrategyFactory.GetRegisteredStrategies();
        var senzaScheda = totali
            .Select(definizione => service.Build(definizione.Id))
            .Where(export => export.Sources.All(document => document.Role != "dossier"))
            .Select(export => export.Identity.Id)
            .ToList();

        Assert.True(
            senzaScheda.Count * 10 <= totali.Count,
            $"{senzaScheda.Count} strategie su {totali.Count} senza scheda di dossier: " +
            string.Join(", ", senzaScheda));
    }

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
