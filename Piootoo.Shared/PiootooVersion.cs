namespace Piootoo.Shared;

/// <summary>
/// Versione del progetto Piootoo, condivisa fra server, console WinForms e cBot distribuito.
///
/// <para><b>Perché una costante e non l'assembly version.</b> Il numero deve valere per due
/// artefatti che non condividono una build: il server è .NET e si compila da questa solution, il
/// cBot vive in <c>piootoo-repository/ctrader/</c> e lo compila cTrader, che non referenzia le
/// nostre assembly. Non esiste un punto unico da cui entrambi possano leggerlo a compile time: la
/// sincronia è manuale ed è per questo che è scritta qui, in chiaro, invece di essere dedotta.</para>
///
/// <para><b>Cosa aggiornare a ogni release.</b> Tre punti, sempre insieme:</para>
/// <list type="number">
///   <item><description><see cref="Current"/> (questo file) — lato server <b>e console</b>: la
///   console WinForms compila contro questa assembly, quindi la sua versione è questa e non può
///   divergere per costruzione;</description></item>
///   <item><description><c>PiootooDistributedExecutionBot.BotVersion</c> in
///   <c>piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs</c> — lato cBot. Solo se cambia
///   il <see cref="Contract"/>: una fix di patch non obbliga a ricompilare e ridistribuire i bot
///   (vedi sotto);</description></item>
///   <item><description><c>VersionPrefix</c> in <c>Directory.Build.props</c> — la versione con cui
///   vengono stampati gli assembly. Senza, MSBuild ci mette il proprio default <c>1.0.0.0</c> e i
///   binari non dicono più a quale release appartengono.</description></item>
/// </list>
///
/// <para><b>Cosa significa "disallineato".</b> Il numero è <c>major.minor.patch</c> ma il
/// contratto è <c>major.minor</c>: la patch è per le correzioni che non cambiano ciò che le tre
/// parti si dicono. Portare una fix da 3.11.0 a 3.11.1 significa poter aggiornare il server (o la
/// console, o il bot) da solo, senza che gli altri due la segnalino come incompatibile — e senza
/// dover ridistribuire i cBot su ogni macchina per un numero che non cambia nulla per loro. È
/// <see cref="IsSameContract"/> a dirlo, ed è quello che guardano console e cBot: un salto di
/// minor invece resta un disallineamento, perché lì il contratto è cambiato davvero.</para>
///
/// <para><b>Non è a fiducia.</b> <c>VersioneDelProgettoTests</c> verifica che i tre numeri stiano
/// insieme: assembly e costante devono coincidere <b>esattamente</b> (stessa build, non c'è motivo
/// perché divergano), il sorgente del cBot deve dichiarare almeno lo stesso contratto. Muoverne uno
/// solo fa fallire la build dei test.</para>
///
/// <para><c>PiootooBarCycleTestBot</c> ha una versione propria e non segue questa: non parla HTTP
/// con il server, quindi non c'è un contratto comune di cui il numero sia la sintesi.
/// <c>PiootooDirectExecutionBot</c> invece <b>un client HTTP ce l'ha</b> (parametro
/// <c>API Base Url</c>) pur portando una versione propria (1.4.0): o segue anche lui questo
/// numero, o va detto perché non deve. Voce aperta, non risolta qui.</para>
///
/// <para>Il disallineamento non è bloccante ed è volutamente solo diagnostico: server e cBot
/// stampano la propria versione all'avvio, e il confronto si fa leggendo i due log. Un blocco
/// lato server significherebbe che un aggiornamento del server ferma i bot in esecuzione, che è
/// peggio del problema che risolverebbe.</para>
/// </summary>
public static class PiootooVersion
{
    /// <summary>Versione corrente del server. Vedi la nota della classe: va mossa insieme a quella del cBot.</summary>
    public const string Current = "5.1.1";

    /// <summary>
    /// Parte del numero che vale come contratto: <c>major.minor</c>. È ciò che console e cBot
    /// confrontano con il server — la patch è per le fix, e non deve far comparire un avviso di
    /// incompatibilità che non c'è.
    /// </summary>
    public static string Contract => ContractOf(Current);

    /// <summary>
    /// <c>major.minor</c> di una versione qualsiasi. Un numero che non ha due parti torna
    /// invariato: meglio confrontare per intero una stringa che non si sa leggere, che dichiarare
    /// allineato ciò che non si è capito.
    /// </summary>
    public static string ContractOf(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        var parts = version.Trim().Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : version.Trim();
    }

    /// <summary>
    /// True se l'altra versione parla lo stesso contratto della nostra: differiscono al più per la
    /// patch.
    /// </summary>
    public static bool IsSameContract(string? otherVersion)
        => otherVersion is not null
           && string.Equals(ContractOf(otherVersion), Contract, StringComparison.Ordinal);
}
