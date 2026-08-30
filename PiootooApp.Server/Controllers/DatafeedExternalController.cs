using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Datafeed;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Raccolta del datafeed spinto da un bot esterno (cBot cTrader) verso
/// <c>piootoo-repository/datafeed-external/{BROKER}/</c>.
///
/// <para>Il <b>codice broker</b> e' obbligatorio su ogni invio ed e' la sottocartella: le barre
/// dello stesso simbolo prese da broker diversi non sono la stessa serie, e mescolarle darebbe un
/// feed che non corrisponde a nessuno dei due. Il bot lo deduce dal conto e lo si puo' forzare.</para>
///
/// <para>Il contratto e' pensato per essere usato <b>a pezzi</b>: ogni <c>POST bars</c> porta un
/// blocco piccolo di un solo stream, e il server lo cuce a quello che ha gia'. Non esiste una
/// chiamata "importa tutto lo storico", ed e' voluto: sarebbe l'unica che puo' andare in timeout, e
/// morendo a meta' non lascerebbe niente di riutilizzabile.</para>
///
/// <para>Il flusso normale del bot e': <c>GET status</c> per sapere cosa manca -> tanti
/// <c>POST bars</c> quanti sono i blocchi -> <c>POST compact</c> a fine backfill.</para>
/// </summary>
[ApiController]
[Route("api/datafeed-external")]
public class DatafeedExternalController : ControllerBase
{
    private readonly ExternalDatafeedStore _store;
    private readonly TradingPlanService _plans;
    private readonly ILogger<DatafeedExternalController> _logger;

    public DatafeedExternalController(
        ExternalDatafeedStore store,
        TradingPlanService plans,
        ILogger<DatafeedExternalController> logger)
    {
        _store = store;
        _plans = plans;
        _logger = logger;
    }

    /// <summary>
    /// Gli strumenti che un piano tocca, per il bot che ne deve raccogliere il datafeed: coppie
    /// (simbolo, timeframe) e il nome di ogni simbolo sul conto indicato.
    ///
    /// <para>Vengono dal <b>masterfilter</b>, non dalla rotazione Titano corrente: il feed di uno
    /// strumento serve anche mentre le sue strategie sono spente, altrimenti alla riaccensione
    /// mancherebbe la storia della pausa. È una lettura pura — non apre sessioni e non tocca
    /// stato — così un raccoglitore non ha alcun effetto sull'operatività.</para>
    /// </summary>
    [HttpGet("plan-instruments")]
    public ActionResult<PlanDatafeedInstrumentsDto> GetPlanInstruments(
        [FromQuery] string planCode,
        [FromQuery] string? accountNumber = null)
    {
        try
        {
            return Ok(_plans.ResolveDatafeedInstruments(planCode, accountNumber));
        }
        catch (KeyNotFoundException error)
        {
            return Problem(title: "Piano non trovato", detail: error.Message, statusCode: 404);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            return Problem(title: "Piano non utilizzabile", detail: error.Message, statusCode: 400);
        }
    }

    /// <summary>
    /// Accoda uno o piu' blocchi di barre. Idempotente: rimandare lo stesso periodo produce
    /// duplicati contati e nessuna scrittura.
    /// </summary>
    [HttpPost("bars")]
    public async Task<ActionResult<IngestBarsResponseDto>> IngestBars([FromBody] IngestBarsRequestDto request)
    {
        try
        {
            var response = await _store.IngestBarsAsync(request);
            foreach (var stream in response.Streams)
            {
                _logger.LogInformation(
                    "[datafeed-external] {Broker}/{Symbol}_{Timeframe}: {Received} ricevute, {Accepted} nuove, " +
                    "{Updated} aggiornate, {Duplicates} duplicate, {Rejected} scartate (journal={Pending}{Compacted}).",
                    stream.Broker, stream.Symbol, stream.TimeframeMinutes, stream.Received, stream.Accepted,
                    stream.Updated, stream.Duplicates, stream.Rejected, stream.PendingJournalCandles,
                    stream.Compacted ? ", compattato" : string.Empty);
            }

            return Ok(response);
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Blocco di barre non valido", detail: error.Message, statusCode: 400);
        }
    }

    /// <summary>Accoda tick di un simbolo ai journal giornalieri.</summary>
    [HttpPost("ticks")]
    public async Task<ActionResult<IngestTicksResponseDto>> IngestTicks([FromBody] IngestTicksRequestDto request)
    {
        try
        {
            var response = await _store.IngestTicksAsync(request);
            _logger.LogInformation(
                "[datafeed-external] tick {Broker}/{Symbol}: {Received} ricevuti, {Accepted} scritti, " +
                "{Stale} sovrapposti, {Rejected} scartati (ultimo {Last:O}).",
                response.Broker, response.Symbol, response.Received, response.Accepted, response.Stale,
                response.Rejected, response.LastTickUtc);
            return Ok(response);
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Blocco di tick non valido", detail: error.Message, statusCode: 400);
        }
    }

    /// <summary>
    /// Copertura di uno stream: da quando a quando, quante barre, e dove sono i buchi. E' la
    /// chiamata con cui il bot decide cosa chiedere al broker e cosa saltare.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ExternalFeedStatusDto>> GetStatus(
        [FromQuery] string broker,
        [FromQuery] string symbol,
        [FromQuery] int timeframeMinutes,
        [FromQuery] int? gapToleranceMinutes = null)
    {
        try
        {
            return Ok(await _store.GetStatusAsync(broker, symbol, timeframeMinutes, gapToleranceMinutes));
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Richiesta non valida", detail: error.Message, statusCode: 400);
        }
    }

    /// <summary>Tutti i feed esterni raccolti finora. Senza <c>broker</c>, di tutti i broker.</summary>
    [HttpGet("index")]
    public async Task<ActionResult<ExternalFeedIndexDto>> GetIndex(
        [FromQuery] string? broker = null,
        [FromQuery] int? gapToleranceMinutes = null)
        => Ok(await _store.GetIndexAsync(broker, gapToleranceMinutes));

    /// <summary>
    /// Materializza i journal nei file piatti. Senza parametri compatta tutti gli stream: e' la
    /// chiamata da fare a mano quando un bot e' morto a meta' backfill e si vuole il file su disco.
    /// </summary>
    [HttpPost("compact")]
    public async Task<ActionResult<CompactExternalFeedsResponseDto>> Compact(
        [FromQuery] string? broker = null,
        [FromQuery] string? symbol = null,
        [FromQuery] int? timeframeMinutes = null)
    {
        try
        {
            return Ok(await _store.CompactAsync(broker, symbol, timeframeMinutes));
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Richiesta non valida", detail: error.Message, statusCode: 400);
        }
    }
}
