using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Models.Settings;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Controller per la gestione dei settings
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ILogger<SettingsController> _logger;
    private readonly IPiootooSettingsService _settingsService;

    public SettingsController(
        ILogger<SettingsController> logger,
        IPiootooSettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Ottiene l'elenco dei simboli disponibili
    /// </summary>
    [HttpGet("symbols")]
    public ActionResult<List<string>> GetAvailableSymbols()
    {
        try
        {
            var symbols = _settingsService.GetAvailableSymbols();
            return Ok(symbols);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei simboli");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene tutti i setup salvati
    /// </summary>
    [HttpGet("setups")]
    public ActionResult<List<PiootooSetup>> GetAllSetups()
    {
        try
        {
            var setups = _settingsService.GetAllSetups();
            return Ok(setups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei setup");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene un setup per ID
    /// </summary>
    [HttpGet("setups/{id}")]
    public ActionResult<PiootooSetup> GetSetupById(string id)
    {
        try
        {
            var setup = _settingsService.GetSetupById(id);
            if (setup == null)
            {
                return NotFound(new { error = $"Setup {id} non trovato" });
            }

            return Ok(setup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del setup");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Crea un nuovo setup
    /// </summary>
    [HttpPost("setups")]
    public ActionResult<PiootooSetup> CreateSetup([FromBody] PiootooSetup setup)
    {
        try
        {
            var created = _settingsService.CreateSetup(setup);
            return Ok(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione del setup");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Aggiorna un setup esistente
    /// </summary>
    [HttpPut("setups/{id}")]
    public ActionResult<PiootooSetup> UpdateSetup(string id, [FromBody] PiootooSetup setup)
    {
        try
        {
            if (id != setup.Id)
            {
                return BadRequest(new { error = "ID nel path non corrisponde all'ID nel body" });
            }

            var updated = _settingsService.UpdateSetup(setup);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del setup");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un setup
    /// </summary>
    [HttpDelete("setups/{id}")]
    public ActionResult DeleteSetup(string id)
    {
        try
        {
            var deleted = _settingsService.DeleteSetup(id);
            if (!deleted)
            {
                return NotFound(new { error = $"Setup {id} non trovato" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione del setup");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
