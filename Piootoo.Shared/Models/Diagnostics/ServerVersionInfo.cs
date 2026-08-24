namespace Piootoo.Shared.Models.Diagnostics;

/// <summary>
/// Identità del processo server, esposta da <c>GET /api/v1/version</c>.
///
/// <para>Esiste perché la versione compilata nella console non dice quale server sta rispondendo:
/// la console si ricompila dalla solution, il server gira spesso da una cartella pubblicata a parte
/// (<c>publish_run</c>) che può essere di una build precedente. Il confronto ha senso solo fra il
/// numero compilato nel client e quello dichiarato a runtime dal server.</para>
/// </summary>
public sealed class ServerVersionInfo
{
    /// <summary>Versione del contratto Piootoo dichiarata dal server (<c>PiootooVersion.Current</c>).</summary>
    public required string Version { get; init; }

    /// <summary>Da quando questo processo è in piedi. Distingue un server riavviato da uno rimasto su.</summary>
    public required DateTime StartedAtUtc { get; init; }

    /// <summary>Cartella da cui il server sta girando: è ciò che spiega una versione inattesa.</summary>
    public required string ContentRootPath { get; init; }

    /// <summary>Ambiente ASP.NET (<c>Development</c>, <c>Production</c>…).</summary>
    public required string Environment { get; init; }
}
