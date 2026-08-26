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
///   <c>piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs</c> — lato cBot;</description></item>
///   <item><description><c>VersionPrefix</c> in <c>Directory.Build.props</c> — la versione con cui
///   vengono stampati gli assembly. Senza, MSBuild ci mette il proprio default <c>1.0.0.0</c> e i
///   binari non dicono più a quale release appartengono.</description></item>
/// </list>
///
/// <para><b>Non è a fiducia.</b> <c>VersioneDelProgettoTests</c> verifica che i tre numeri
/// coincidano: i primi due leggendo l'assembly e il sorgente del bot, il terzo dall'attributo
/// <c>AssemblyInformationalVersion</c>. Muoverne uno solo fa fallire la build dei test.</para>
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
    public const string Current = "3.9.0";
}
