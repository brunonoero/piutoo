using System.ComponentModel;
using System.Globalization;
using Piootoo.Shared.Models.Optimization;
using piootooapp.clientform.Shell.Screens;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Copre i tre meccanismi su cui poggia la leggibilità della schermata di setup Titano: il livello
/// Base/Avanzato che pilota il filtro del <c>PropertyGrid</c>, la conversione frazione↔percentuale,
/// e il riepilogo in prosa con i suoi avvisi.
///
/// <para>Sono cose che si rompono in silenzio. Un parametro senza livello sparisce dalla vista Base
/// e nessuno se ne accorge finché non viene salvato al proprio default; un avviso che non scatta
/// lascia passare una configurazione incoerente. Da qui i test.</para>
/// </summary>
public class TitanoSetupUiTests
{
    private static IEnumerable<PropertyDescriptor> VisibleProperties() =>
        TypeDescriptor.GetProperties(typeof(TitanoRotationSetup))
            .Cast<PropertyDescriptor>()
            .Where(x => x.IsBrowsable);

    [Fact]
    public void OgniParametroVisibileDichiaraIlProprioLivello()
    {
        var senzaLivello = VisibleProperties()
            .Where(x => x.Attributes[typeof(TitanoLevelAttribute)] is null)
            .Select(x => x.Name)
            .ToList();

        Assert.True(
            senzaLivello.Count == 0,
            "Questi parametri non hanno [TitanoLevel] e sparirebbero dalla vista Base senza segnalarlo: " +
            string.Join(", ", senzaLivello));
    }

    [Fact]
    public void LaVistaBaseRestaBreveAbbastanzaDaEssereLetta()
    {
        var basici = VisibleProperties()
            .Where(x => x.Attributes[typeof(TitanoLevelAttribute)] is TitanoLevelAttribute
            {
                Level: TitanoParameterLevel.Base
            })
            .ToList();

        // Il numero esatto non è un contratto; il punto è che la vista Base non torni a essere
        // l'elenco completo con un altro nome. Oltre una dozzina di voci ha perso il suo scopo.
        Assert.InRange(basici.Count, 6, 12);
    }

    [Fact]
    public void IlLivelloSiConfrontaPerValore_AltrimentiIlFiltroDelGridNonTrovaNulla()
    {
        // BrowsableAttributes confronta gli attributi per valore: senza Equals ridefinito il filtro
        // costruito nella schermata non corrisponderebbe mai a quello sulla proprietà.
        Assert.Equal(
            new TitanoLevelAttribute(TitanoParameterLevel.Base),
            new TitanoLevelAttribute(TitanoParameterLevel.Base));
        Assert.NotEqual(
            new TitanoLevelAttribute(TitanoParameterLevel.Base),
            new TitanoLevelAttribute(TitanoParameterLevel.Avanzato));
    }

    [Fact]
    public void LeFrazioniSonoEsposteComePercentualiERilette()
    {
        var converter = new PercentTypeConverter();
        var it = CultureInfo.GetCultureInfo("it-IT");

        Assert.Equal("15 %", converter.ConvertTo(null, it, 0.15m, typeof(string)));
        Assert.Equal("2,5 %", converter.ConvertTo(null, it, 0.025m, typeof(string)));

        Assert.Equal(0.15m, converter.ConvertFrom(null, it, "15"));
        Assert.Equal(0.15m, converter.ConvertFrom(null, it, "15 %"));
        // Virgola o punto: chi digita sul tastierino numerico non sceglie il separatore.
        Assert.Equal(0.125m, converter.ConvertFrom(null, it, "12,5"));
        Assert.Equal(0.125m, converter.ConvertFrom(null, it, "12.5"));
    }

    [Fact]
    public void LaConversioneEUnAndataERitornoEsatto()
    {
        var converter = new PercentTypeConverter();
        var it = CultureInfo.GetCultureInfo("it-IT");

        foreach (var fraction in new[] { 0m, 0.05m, 0.10m, 0.15m, 0.25m, 0.35m, 1m })
        {
            var text = (string)converter.ConvertTo(null, it, fraction, typeof(string))!;
            Assert.Equal(fraction, converter.ConvertFrom(null, it, text));
        }
    }

    [Fact]
    public void IlRiepilogoNominaLeSoglieCheGovernanoLaDecisione()
    {
        var setup = new TitanoRotationSetup();

        var testo = TitanoSetupSummary.Describe(setup);

        Assert.Contains("15%", testo);   // spegnimento
        Assert.Contains("10%", testo);   // rientro
        Assert.Contains("35%", testo);   // blocco definitivo
        Assert.Contains("4 voti su 5", testo);
        Assert.Contains("settimane", testo);
    }

    [Fact]
    public void IlRiepilogoDichiaraQualeDelleDueModalitaDiSizingEInVigore()
    {
        var perClassifica = TitanoSetupSummary.Describe(new TitanoRotationSetup { CrossSectionalSizing = true });
        var aScaglioni = TitanoSetupSummary.Describe(new TitanoRotationSetup { CrossSectionalSizing = false });

        Assert.Contains("classifica", perClassifica);
        Assert.Contains("scaglioni", aScaglioni);
    }

    [Fact]
    public void SegnalaLIsteresiAssente()
    {
        var setup = new TitanoRotationSetup
        {
            MaximumCurrentDrawdown = 0.15m,
            ReenableMaximumCurrentDrawdown = 0.15m
        };

        Assert.Contains(TitanoSetupSummary.Warnings(setup), x => x.Contains("isteresi"));
    }

    [Fact]
    public void SegnalaIlBloccoDefinitivoNonOltreLaSogliaDiSpegnimento()
    {
        var setup = new TitanoRotationSetup
        {
            MaximumCurrentDrawdown = 0.35m,
            HardStopDrawdown = 0.30m
        };

        Assert.Contains(TitanoSetupSummary.Warnings(setup), x => x.Contains("blocco definitivo"));
    }

    [Fact]
    public void SegnalaLaReattivitaIncoerenteConLaCadenza()
    {
        // 90 giorni di finestra breve su rotazione settimanale: ~13 periodi, la trappola descritta
        // in docs/titano-analisi-parametri-e-audit-2026-07-31.md §1.2.
        var lenta = new TitanoRotationSetup
        {
            RotationPeriod = TitanoRotationPeriod.Weekly,
            ShortWindowDays = 90
        };
        Assert.Contains(TitanoSetupSummary.Warnings(lenta), x => x.Contains("Reattività"));

        var allineata = new TitanoRotationSetup
        {
            RotationPeriod = TitanoRotationPeriod.Weekly,
            ShortWindowDays = 28
        };
        Assert.DoesNotContain(TitanoSetupSummary.Warnings(allineata), x => x.Contains("Reattività"));
    }

    [Fact]
    public void SegnalaIParametriSenzaEffettoConIlSizingPerClassifica()
    {
        // È il difetto B3 dell'audit, che era stato corretto disabilitando i controlli nella vecchia
        // form a NumericUpDown. Il PropertyGrid non sa disabilitare per valore, quindi lo dice.
        var perClassifica = new TitanoRotationSetup { CrossSectionalSizing = true };
        Assert.Contains(TitanoSetupSummary.Warnings(perClassifica), x => x.Contains("non hanno alcun effetto"));

        var aScaglioni = new TitanoRotationSetup { CrossSectionalSizing = false };
        Assert.DoesNotContain(TitanoSetupSummary.Warnings(aScaglioni), x => x.Contains("non hanno alcun effetto"));
    }

    [Fact]
    public void UnaConfigurazioneCoerenteNonProduceAvvisiStrutturali()
    {
        // Nessun avviso su isteresi, hard stop, finestre o allocazione. Restano ammessi gli avvisi
        // informativi (parametri inerti, reattività), che qui sono spenti dalla calibrazione.
        var setup = new TitanoRotationSetup
        {
            RotationPeriod = TitanoRotationPeriod.Weekly,
            ShortWindowDays = 28,
            LongWindowDays = 365,
            MinimumTrades = 3,
            MinimumPassingFilters = 4,
            MaximumCurrentDrawdown = 0.15m,
            ReenableMaximumCurrentDrawdown = 0.10m,
            HardStopDrawdown = 0.35m,
            CrossSectionalSizing = false,
            MinimumAllocationMultiplier = 0.25m,
            MaximumAllocationMultiplier = 1m
        };

        Assert.Empty(TitanoSetupSummary.Warnings(setup));
    }
}
