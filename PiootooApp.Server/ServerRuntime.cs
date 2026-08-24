namespace PiootooApp.Server;

/// <summary>
/// Dati del processo server che non cambiano dopo l'avvio.
///
/// <para>Registrato in <c>Program</c> come istanza già costruita: se fosse un singleton risolto
/// pigramente, <see cref="StartedAtUtc"/> sarebbe l'istante della prima richiesta e non dell'avvio —
/// plausibile e sbagliato, cioè il tipo di dato che porta fuori strada proprio quando lo si guarda
/// per capire se il server è stato riavviato.</para>
/// </summary>
public sealed class ServerRuntime
{
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
}
