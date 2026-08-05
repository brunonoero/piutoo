using System.Text;
using Piootoo.Shared.Models.Optimization;

namespace piootooapp.clientform.Shell.Screens;

/// <summary>
/// Traduce un <see cref="TitanoRotationSetup"/> in prosa, e ne elenca le incoerenze.
///
/// <para>Esiste perché trenta numeri corretti non dicono cosa farà il sistema. Un tooltip spiega
/// un parametro alla volta; il comportamento nasce dalla loro combinazione — la soglia di rientro
/// ha senso solo relativamente a quella di uscita, la finestra di misura solo relativamente alla
/// cadenza di rotazione. Le frasi qui sotto sono la stessa configurazione detta nell'ordine in cui
/// la decisione avviene.</para>
///
/// <para>È una classe statica pura, senza dipendenze da WinForms: è la parte che vale la pena
/// testare, e i test non devono aprire una finestra.</para>
/// </summary>
public static class TitanoSetupSummary
{
    /// <summary>Quanti giorni dura un periodo di rotazione. Serve a rapportare le finestre di misura alla cadenza.</summary>
    private static int PeriodDays(TitanoRotationPeriod period) => period switch
    {
        TitanoRotationPeriod.Weekly => 7,
        TitanoRotationPeriod.Biweekly => 14,
        _ => 30
    };

    private static string PeriodNoun(TitanoRotationPeriod period, int count) => period switch
    {
        TitanoRotationPeriod.Weekly => count == 1 ? "settimana" : "settimane",
        TitanoRotationPeriod.Biweekly => count == 1 ? "quindicina" : "quindicine",
        _ => count == 1 ? "mese" : "mesi"
    };

    private static string Pct(decimal fraction) => (fraction * 100m).ToString("0.##") + "%";

    /// <summary>Cosa farà questa configurazione, in quattro frasi.</summary>
    public static string Describe(TitanoRotationSetup s)
    {
        var text = new StringBuilder();

        var cadence = s.RotationPeriod switch
        {
            TitanoRotationPeriod.Weekly => "Ogni lunedì",
            TitanoRotationPeriod.Biweekly => "Ogni due settimane",
            _ => "A ogni inizio mese"
        };

        text.Append(cadence)
            .Append(" Titano rivede il portafoglio guardando gli ultimi ")
            .Append(s.ShortWindowDays)
            .Append(" giorni (e ")
            .Append(s.LongWindowDays)
            .Append(" per il trend di fondo). ");

        text.Append("Una strategia è ammessa se supera almeno ")
            .Append(s.MinimumPassingFilters)
            .Append(" voti su 5. ");

        text.Append("Si spegne se la perdita dal picco supera ")
            .Append(Pct(s.MaximumCurrentDrawdown))
            .Append(", e può rientrare solo quando è tornata sotto ")
            .Append(Pct(s.ReenableMaximumCurrentDrawdown))
            .Append(", non prima di ")
            .Append(s.CooldownPeriodsAfterOff)
            .Append(' ')
            .Append(PeriodNoun(s.RotationPeriod, s.CooldownPeriodsAfterOff))
            .Append(". Oltre ")
            .Append(Pct(s.HardStopDrawdown))
            .Append(" il blocco è definitivo e si toglie solo a mano. ");

        if (s.CrossSectionalSizing)
        {
            text.Append("Alle strategie accese va una size fra ")
                .Append(Pct(s.MinimumAllocationMultiplier))
                .Append(" e ")
                .Append(Pct(s.MaximumAllocationMultiplier))
                .Append(" di quella base, assegnata per posizione in classifica dentro il periodo")
                .Append(s.AllocationStep > 0 ? $" e arrotondata a passi di {Pct(s.AllocationStep)}." : ".");
        }
        else
        {
            text.Append("La size viene dagli scaglioni assoluti della categoria 6, non dalla classifica.");
        }

        return text.ToString();
    }

    /// <summary>
    /// Le incoerenze che il server non rifiuta ma che producono un manifest diverso da quello che
    /// si crede di aver chiesto. Sono avvisi, non errori: nessuno di questi impedisce di salvare.
    /// </summary>
    public static IReadOnlyList<string> Warnings(TitanoRotationSetup s)
    {
        var warnings = new List<string>();

        if (s.ReenableMaximumCurrentDrawdown >= s.MaximumCurrentDrawdown)
        {
            warnings.Add(
                $"Nessuna isteresi: la soglia di rientro ({Pct(s.ReenableMaximumCurrentDrawdown)}) non è più " +
                $"severa di quella di uscita ({Pct(s.MaximumCurrentDrawdown)}). Una strategia al confine " +
                "rischia di accendersi e spegnersi a ogni periodo.");
        }

        if (s.HardStopDrawdown <= s.MaximumCurrentDrawdown)
        {
            warnings.Add(
                $"Il blocco definitivo ({Pct(s.HardStopDrawdown)}) non è oltre la soglia di spegnimento " +
                $"({Pct(s.MaximumCurrentDrawdown)}): scatterebbe insieme a quella, rendendo ogni spegnimento " +
                "irreversibile. Il server rifiuterà questa combinazione.");
        }

        if (s.LongWindowDays < s.ShortWindowDays)
        {
            warnings.Add(
                $"La finestra lunga ({s.LongWindowDays} giorni) è più corta di quella breve " +
                $"({s.ShortWindowDays}). Il server rifiuterà questa combinazione.");
        }

        // Il rapporto fra finestra di misura e cadenza è la trappola di calibrazione più comune:
        // si imposta la rotazione settimanale credendo di aver comprato reattività settimanale.
        var periodDays = PeriodDays(s.RotationPeriod);
        var ratio = s.ShortWindowDays / (double)periodDays;
        if (ratio >= 8)
        {
            warnings.Add(
                $"Reattività molto più lenta della cadenza: la finestra breve copre ~{ratio:0} periodi, " +
                $"quindi l'ultimo periodo pesa circa un {ratio:0}esimo della misura. Ruoti spesso ma decidi " +
                "piano. Per una reattività allineata alla cadenza servono all'incirca " +
                $"{periodDays * 3}-{periodDays * 5} giorni di finestra breve.");
        }

        if (s.CrossSectionalSizing)
        {
            warnings.Add(
                "Con 'Alloca per classifica' attivo, i tre parametri della categoria 6 (score di " +
                "spegnimento, score di riaccensione, scaglioni) non hanno alcun effetto: modificarli " +
                "cambia l'identificativo del run ma non il manifest.");
        }

        if (s.MaximumAllocationMultiplier <= s.MinimumAllocationMultiplier)
        {
            warnings.Add(
                "Allocazione minima e massima coincidono o sono invertite: tutte le strategie accese " +
                "riceveranno la stessa size e la classifica non avrà alcun effetto.");
        }

        if (s.MinimumPassingFilters >= 5)
        {
            warnings.Add(
                "Servono tutti e cinque i voti: basta un solo criterio marginalmente fuori soglia per " +
                "escludere una strategia. Aspettati un portafoglio spesso vuoto.");
        }

        if (s.MinimumTrades <= 1)
        {
            warnings.Add(
                "Con un solo trade minimo, una strategia può essere giudicata su un campione che non " +
                "significa nulla. Su rotazioni frequenti conviene alzarlo.");
        }

        return warnings;
    }
}
