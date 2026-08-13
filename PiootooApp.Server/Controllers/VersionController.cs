using Microsoft.AspNetCore.Mvc;
using Piootoo.Shared;
using Piootoo.Shared.Models.Diagnostics;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Identità del processo server. Endpoint diagnostico, senza token: chi arriva a parlare col server
/// deve poter sapere <i>con quale</i> server sta parlando prima ancora di aprire una sessione.
/// </summary>
[ApiController]
[Route("api/v1/version")]
public sealed class VersionController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ServerRuntime _runtime;

    public VersionController(IWebHostEnvironment environment, ServerRuntime runtime)
    {
        _environment = environment;
        _runtime = runtime;
    }

    [HttpGet]
    public ActionResult<ServerVersionInfo> Get() => Ok(new ServerVersionInfo
    {
        Version = PiootooVersion.Current,
        StartedAtUtc = _runtime.StartedAtUtc,
        ContentRootPath = _environment.ContentRootPath,
        Environment = _environment.EnvironmentName
    });
}
