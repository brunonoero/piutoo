using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Brokers;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Le specifiche che un broker dichiara sui propri strumenti, spinte da un cBot verso
/// <c>piootoo-repository/symbol-info/{BROKER}/</c>.
///
/// <para><b>Perche' passa dal server e non da un file copiato a mano.</b> Le stesse specifiche oggi
/// si ricopiano dalla scheda del simbolo in cTrader in tre posti diversi — il moltiplicatore in
/// <c>symbol-conversions.json</c>, il finanziamento nel CSV di swap, tick e pip altrove. Venti
/// strumenti per ogni broker nuovo, a mano, e' il punto in cui un numero sbagliato non lo vede
/// nessuno: nessun test puo' accorgersene, perche' il numero vero sta su uno schermo.</para>
///
/// <para>L'ingestione e' <b>idempotente per costruzione</b>: una rilevazione identica alla
/// precedente non crea uno scatto nuovo, allunga quello in corso. Il bot puo' quindi spedire a ogni
/// avvio senza tenere il conto di cosa ha gia' mandato.</para>
/// </summary>
[ApiController]
[Route("api/symbol-info")]
public class SymbolInfoController : ControllerBase
{
    private readonly SymbolInfoStore _store;
    private readonly SymbolConversionReconciler _reconciler;
    private readonly SwapFromSymbolInfo _swap;
    private readonly ILogger<SymbolInfoController> _logger;

    public SymbolInfoController(
        SymbolInfoStore store,
        SymbolConversionReconciler reconciler,
        SwapFromSymbolInfo swap,
        ILogger<SymbolInfoController> logger)
    {
        _store = store;
        _reconciler = reconciler;
        _swap = swap;
        _logger = logger;
    }

    /// <summary>Registra una rilevazione. Vedi la nota sulla classe per l'idempotenza.</summary>
    [HttpPost]
    public async Task<ActionResult<SymbolInfoIngestResponseDto>> Ingest(
        [FromBody] SymbolInfoIngestRequestDto request)
    {
        try
        {
            var response = await _store.IngestAsync(request);

            _logger.LogInformation(
                "[symbol-info] {Broker}: {Total} strumenti, {Changed} cambiati, {Unchanged} invariati, " +
                "{Rejected} scartati (rilevazione {TakenUtc:O}).",
                response.Broker, response.Symbols.Count, response.Changed, response.Unchanged,
                response.Rejected, response.TakenUtc);

            // I cambiamenti si dicono uno per uno: una tariffa di swap che si muove cambia il costo
            // di ogni run successivo, e scoprirlo da un contatore aggregato vorrebbe dire non
            // scoprirlo affatto.
            foreach (var symbol in response.Symbols.Where(entry => entry.Changed && entry.SnapshotCount > 1))
            {
                _logger.LogWarning(
                    "[symbol-info] {Broker}/{Symbol}: specifiche CAMBIATE — {Changes}",
                    response.Broker, symbol.BrokerSymbol, string.Join("; ", symbol.ChangedProperties));
            }

            // Le schede nuove aggiornano le righe automatiche di swap. Un calcolo che non riesce non
            // fa perdere la rilevazione, che e' gia' archiviata: si dice e basta.
            try
            {
                var swap = _swap.Rebuild(response.Broker);
                _logger.LogInformation(
                    "[swap] {Broker}: {Auto} righe automatiche, {Manual} a mano in {File}.",
                    swap.Broker, swap.Auto.Count, swap.Manual.Count, swap.File ?? "(nessun file)");
                foreach (var warning in swap.Warnings)
                    _logger.LogWarning("[swap] {Broker}: {Warning}", swap.Broker, warning);
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                _logger.LogWarning("[swap] {Broker}: righe automatiche NON aggiornate: {Error}", response.Broker, error.Message);
            }

            return Ok(response);
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Rilevazione non valida", detail: error.Message, statusCode: 400);
        }
        catch (InvalidDataException error)
        {
            return Problem(title: "Archivio non leggibile", detail: error.Message, statusCode: 409);
        }
    }

    /// <summary>
    /// La tabella di conversione delle size messa accanto a cio' che il broker dichiara.
    ///
    /// <para><b>Confronta e basta.</b> Quei numeri decidono quanti contratti va a mercato un
    /// segnale: una riga corretta in automatico sposterebbe il rischio di ogni strategia senza che
    /// nessuno abbia deciso nulla. Il rapporto dice cosa non torna, la correzione resta una scelta
    /// presa guardando.</para>
    /// </summary>
    [HttpGet("reconcile")]
    public ActionResult<SymbolConversionReconciliationDto> Reconcile(
        [FromQuery] string broker,
        [FromQuery] string conversion)
    {
        try
        {
            var report = _reconciler.Reconcile(broker, conversion);

            if (report.Divergent > 0)
            {
                _logger.LogWarning(
                    "[symbol-info] {Broker} vs tabella '{Conversion}': {Divergent} righe divergenti, " +
                    "{Confirmed} confermate, {Unverifiable} non verificabili.",
                    report.Broker, report.ConversionCode, report.Divergent, report.Confirmed,
                    report.Unverifiable);
            }

            return Ok(report);
        }
        catch (KeyNotFoundException error)
        {
            return Problem(title: "Tabella di conversione non trovata", detail: error.Message, statusCode: 404);
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Richiesta non valida", detail: error.Message, statusCode: 400);
        }
        catch (InvalidDataException error)
        {
            return Problem(title: "Archivio non leggibile", detail: error.Message, statusCode: 409);
        }
    }

    /// <summary>I broker che hanno almeno uno strumento archiviato.</summary>
    [HttpGet("brokers")]
    public ActionResult<IReadOnlyList<string>> GetBrokers() => Ok(_store.GetBrokers());

    /// <summary>
    /// Gli strumenti archiviati di un broker, con tutta la successione degli scatti: e' il file su
    /// disco, e chi lo legge deve poter vedere <i>quando</i> una specifica e' cambiata, non solo
    /// quale valga adesso.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<SymbolInfoArchiveDto>> GetArchives([FromQuery] string broker)
    {
        try
        {
            return Ok(_store.GetArchives(broker));
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Broker non valido", detail: error.Message, statusCode: 400);
        }
        catch (InvalidDataException error)
        {
            return Problem(title: "Archivio non leggibile", detail: error.Message, statusCode: 409);
        }
    }
}
