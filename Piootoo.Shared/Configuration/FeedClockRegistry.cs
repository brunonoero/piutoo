using System.Text.Json;

namespace Piootoo.Shared.Configuration;

/// <summary>
/// Dichiara in che orologio è stampato ogni feed, e lo trasforma in un <see cref="SessionClock"/>.
///
/// <para><b>Perché serve.</b> Un file di barre non dichiara quasi mai il proprio fuso, e
/// l'etichetta <c>Z</c> nei JSON non è una garanzia: dice solo che qualcuno ha passato <c>UTC</c>
/// allo script di aggregazione. Sul feed <c>@NQ</c> di questo repository è successo esattamente
/// questo — i timestamp finiscono in <c>Z</c> ma sono ora europea. Due misure indipendenti sui
/// dati lo dimostrano: il picco di volume dell'apertura cash di New York cade sullo slot 15:30 e
/// la pausa di manutenzione CME sugli slot 23:15–23:45, <b>in entrambe le stagioni</b>. Se le
/// etichette fossero UTC vero, entrambe si sposterebbero di un'ora fra inverno ed estate.</para>
///
/// <para><b>Cosa cambia averlo.</b> Finché l'istante di una barra è una bugia, ogni conversione a
/// valle è sbagliata di un offset che dipende dalla stagione, e una strategia finisce per
/// dipendere da come è stampato il feed che riceve. Dichiarando il fuso qui, la conversione a UTC
/// vero avviene <b>una volta sola</b> al caricamento, e da lì in poi tutto il dominio ragiona su
/// istanti veri.</para>
///
/// <para><b>Nessun default silenzioso.</b> Stessa filosofia di <c>InstrumentSpec.PointValue</c>:
/// un feed senza dichiarazione è un errore esplicito, non un'assunzione. Assumere UTC è
/// precisamente l'errore che questo tipo esiste per impedire.</para>
/// </summary>
public sealed class FeedClockRegistry
{
    /// <summary>Nome del manifest, atteso nella radice della cartella dei feed.</summary>
    public const string ManifestFileName = "feed-clocks.json";

    private readonly Dictionary<string, SessionClock> _clocks;

    private FeedClockRegistry(Dictionary<string, SessionClock> clocks) => _clocks = clocks;

    /// <summary>
    /// Legge il manifest dalla radice dei feed. Il file deve esistere: se manca, il chiamante
    /// starebbe per interpretare timestamp di fuso ignoto, ed è meglio fermarsi.
    /// </summary>
    public static FeedClockRegistry Load(string datafeedRoot)
    {
        var manifestPath = Path.Combine(datafeedRoot, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            throw new FeedClockNotDeclaredException(
                $"Manifest '{ManifestFileName}' assente in '{datafeedRoot}'. Ogni feed deve " +
                "dichiarare in che orologio sono stampati i suoi timestamp: senza, le barre " +
                "verrebbero interpretate come UTC, che per i feed di questo repository è falso. " +
                "Vedi docs/domini/orari-di-sessione-e-fusi.md.");
        }

        ManifestDto? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ManifestDto>(
                File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException error)
        {
            throw new FeedClockNotDeclaredException(
                $"Manifest '{manifestPath}' illeggibile: {error.Message}", error);
        }

        var clocks = new Dictionary<string, SessionClock>(StringComparer.OrdinalIgnoreCase);
        foreach (var voce in manifest?.Orologi ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(voce.Value))
            {
                throw new FeedClockNotDeclaredException(
                    $"Il feed '{voce.Key}' in '{manifestPath}' ha un fuso vuoto.");
            }

            // Costruire l'orologio qui, e non alla prima lettura, fa fallire subito un fuso
            // scritto male invece che a metà di un backtest.
            clocks[Normalize(voce.Key)] = new SessionClock(voce.Value);
        }

        return new FeedClockRegistry(clocks);
    }

    /// <summary>Orologio del feed di <paramref name="symbol"/>. Simbolo non dichiarato = errore.</summary>
    public SessionClock For(string symbol)
    {
        if (_clocks.TryGetValue(Normalize(symbol), out var clock))
            return clock;

        throw new FeedClockNotDeclaredException(
            $"Il feed '{symbol}' non dichiara il proprio fuso in '{ManifestFileName}'. " +
            "Aggiungerlo dopo averlo accertato dai dati — la procedura (picco di volume e pausa " +
            "di manutenzione, confrontati fra inverno ed estate) è in " +
            "docs/domini/orari-di-sessione-e-fusi.md.");
    }

    /// <summary>Vero se il feed dichiara il proprio fuso.</summary>
    public bool IsDeclared(string symbol) => _clocks.ContainsKey(Normalize(symbol));

    private static string Normalize(string symbol) => symbol.TrimStart('@').ToUpperInvariant();

    private sealed class ManifestDto
    {
        public Dictionary<string, string>? Orologi { get; set; }
    }
}

/// <summary>Un feed sta per essere letto senza che il suo orologio sia dichiarato.</summary>
public sealed class FeedClockNotDeclaredException : InvalidOperationException
{
    public FeedClockNotDeclaredException(string message) : base(message) { }
    public FeedClockNotDeclaredException(string message, Exception inner) : base(message, inner) { }
}
