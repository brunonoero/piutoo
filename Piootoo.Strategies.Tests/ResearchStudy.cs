using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'interruttore degli <b>studi di ricerca</b>: classi che vivono nel progetto di test ma non sono
/// test. Misurano, non asseriscono; girano per ore sul feed vero di <c>piootoo-repository</c>; e
/// scrivono i propri risultati in <c>piootoo-repository/ricerca/</c>.
///
/// <para><b>Perche' serve.</b> Il 22/09/2026 un <c>dotnet test</c> senza argomenti non e' finito in
/// quaranta minuti, e lo studio interrotto a meta' aveva gia' riscritto un CSV tracciato da git. La
/// stessa suite senza gli studi passa in poco piu' di tre minuti. Uno studio che parte perche'
/// qualcuno ha lanciato la suite non e' una ricerca: e' un run che nessuno sta guardando, su un file
/// che qualcun altro sta leggendo.</para>
///
/// <para><b>Perche' una variabile d'ambiente e non un filtro.</b> Un filtro va ricordato, e chi non
/// lo ricorda paga le ore. Qui il default e' spento e non si puo' dimenticare: la variabile la mette
/// chi lo studio lo vuole davvero. E' la stessa convenzione gia' in uso per
/// <c>PIOOTOO_PERSIST_ALL_INTENTS</c>, che accende la persistenza integrale degli intent in
/// sessione.</para>
///
/// <para><b>Perche' anche il trait.</b> La variabile decide <i>se</i> uno studio gira, il trait
/// serve a nominarli come gruppo (<c>--filter "Category=Studio"</c>) e a non trascinare gli altri
/// mille test in un giro che ne vuole quattro. Uno studio nuovo porta entrambi.</para>
///
/// <para>A interruttore spento lo studio <b>esce subito</b> e il test risulta verde, come gia'
/// succede quando il feed non c'e': in xUnit 2.9 non esiste uno skip deciso a runtime, e la riga
/// stampata dice che non ha misurato nulla. Vedi <c>docs/lavori-in-corso.md</c>.</para>
/// </summary>
public static class ResearchStudy
{
    /// <summary>Metti <c>1</c> (o <c>true</c>) per far girare gli studi.</summary>
    public const string EnvironmentVariable = "PIOOTOO_STUDI";

    /// <summary>Valore del trait che raggruppa gli studi: <c>--filter "Category=Studio"</c>.</summary>
    public const string Category = "Studio";

    /// <summary>
    /// Se gli studi sono accesi. Qualunque valore diverso da <c>1</c> e <c>true</c> vale spento,
    /// compresa la variabile vuota: un interruttore che si accende per errore non e' un interruttore.
    /// </summary>
    public static bool IsEnabled
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(EnvironmentVariable)?.Trim();
            return value is not null &&
                   (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Vero quando lo studio <b>non</b> deve girare, dopo aver scritto il perche' nell'output del
    /// test. Si usa come prima riga dello studio: <c>if (ResearchStudy.IsSkipped(output)) return;</c>
    /// </summary>
    public static bool IsSkipped(ITestOutputHelper output)
    {
        if (IsEnabled) return false;

        output.WriteLine(
            $"studio saltato: {EnvironmentVariable} non e' impostata. " +
            $"Per farlo girare: $env:{EnvironmentVariable}='1' prima di dotnet test. " +
            "Gira per ore sul feed vero e riscrive i file in piootoo-repository/ricerca.");
        return true;
    }
}
