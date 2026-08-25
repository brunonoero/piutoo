namespace Piootoo.Shared;

/// <summary>
/// Versione del contratto Piootoo, condivisa fra il server e il cBot distribuito.
///
/// <para><b>Perché una costante e non l'assembly version.</b> Il numero deve valere per due
/// artefatti che non condividono una build: il server è .NET e si compila da questa solution, il
/// cBot vive in <c>piootoo-repository/ctrader/</c> e lo compila cTrader, che non referenzia le
/// nostre assembly. Non esiste un punto unico da cui entrambi possano leggerlo a compile time: la
/// sincronia è manuale ed è per questo che è scritta qui, in chiaro, invece di essere dedotta.</para>
///
/// <para><b>Cosa aggiornare a ogni release.</b> Due punti, sempre insieme:</para>
/// <list type="number">
///   <item><description><see cref="Current"/> (questo file) — lato server;</description></item>
///   <item><description><c>PiootooDistributedExecutionBot.BotVersion</c> in
///   <c>piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs</c> — lato cBot.</description></item>
/// </list>
///
/// <para>Gli altri cBot (<c>PiootooDirectExecutionBot</c>, <c>PiootooBarCycleTestBot</c>) hanno una
/// versione propria e non seguono questa: non parlano HTTP con il server, quindi non c'è un
/// contratto comune di cui il numero sia la sintesi.</para>
///
/// <para>Il disallineamento non è bloccante ed è volutamente solo diagnostico: server e cBot
/// stampano la propria versione all'avvio, e il confronto si fa leggendo i due log. Un blocco
/// lato server significherebbe che un aggiornamento del server ferma i bot in esecuzione, che è
/// peggio del problema che risolverebbe.</para>
/// </summary>
public static class PiootooVersion
{
    /// <summary>Versione corrente del server. Vedi la nota della classe: va mossa insieme a quella del cBot.</summary>
    public const string Current = "3.5.0";
}
