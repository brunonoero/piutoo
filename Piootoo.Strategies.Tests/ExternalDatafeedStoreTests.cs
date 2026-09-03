using Piootoo.Core.Services;
using Piootoo.Domain.Repositories;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Datafeed;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La raccolta a pezzi del datafeed esterno. Le proprieta' che questi test difendono sono quattro, e
/// sono le cose che un collettore incrementale sbaglia: cucire blocchi arrivati in ordine qualsiasi,
/// non duplicare le sovrapposizioni, non spacciare per continuo un feed bucato, e non mescolare due
/// broker nello stesso file.
/// </summary>
public sealed class ExternalDatafeedStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-datafeed-external-tests", Guid.NewGuid().ToString("N"));

    private ExternalDatafeedStore CreateStore()
    {
        Directory.CreateDirectory(_root);
        return new ExternalDatafeedStore(new PiootooSettings { ExternalRepositoryPath = _root });
    }

    /// <summary>
    /// Il caso d'uso per cui esiste tutto il meccanismo: tre blocchi spediti al contrario, con la
    /// coda di ognuno sovrapposta al successivo, devono produrre UNA serie continua e ordinata.
    /// </summary>
    [Fact]
    public async Task StitchesOutOfOrderOverlappingChunksIntoOneOrderedSeries()
    {
        var store = CreateStore();
        var day = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        // Ordine di arrivo volutamente sbagliato, e ogni blocco copre due ore del precedente.
        await store.IngestBarsAsync(Request(Chunk("@NQ", 60, day.AddHours(12), 8)));
        await store.IngestBarsAsync(Request(Chunk("@NQ", 60, day, 14)));
        await store.IngestBarsAsync(Request(Chunk("@NQ", 60, day.AddHours(18), 6), compact: true));

        var status = await store.GetStatusAsync(Broker, "@NQ", 60);

        Assert.Equal(24, status.Coverage.TotalCandles);
        Assert.Equal(day, status.Coverage.FirstCandleUtc);
        Assert.Equal(day.AddHours(23), status.Coverage.LastCandleUtc);
        Assert.Equal(60, status.Coverage.DominantStepMinutes);
        Assert.Equal(0, status.GapCount);
    }

    /// <summary>
    /// Rimandare un periodo gia' spedito e' la norma, non l'eccezione: il bot lo fa a ogni riavvio
    /// e a ogni barra di regime. Deve costare zero scritture, non una riga in piu' nel feed.
    /// </summary>
    [Fact]
    public async Task ResendingTheSamePeriodIsCountedAsDuplicateAndWritesNothing()
    {
        var store = CreateStore();
        var start = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var first = await store.IngestBarsAsync(Request(Chunk("@ES", 15, start, 10)));
        var second = await store.IngestBarsAsync(Request(Chunk("@ES", 15, start, 10), compact: true));

        Assert.Equal(10, first.TotalAccepted);
        Assert.Equal(0, second.TotalAccepted);
        Assert.Equal(10, second.TotalDuplicates);
        Assert.Equal(10, (await store.GetStatusAsync(Broker, "@ES", 15)).Coverage.TotalCandles);
    }

    /// <summary>
    /// Rimandare un periodo con valori diversi e' il modo con cui si corregge una barra sbagliata:
    /// l'ultima arrivata vince. Se vincesse la prima, un feed nato storto resterebbe storto per
    /// sempre e l'unico rimedio sarebbe cancellare il file a mano.
    /// </summary>
    [Fact]
    public async Task LastChunkWinsOnTheSameBar()
    {
        var store = CreateStore();
        var start = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        await store.IngestBarsAsync(Request(Chunk("@GC", 60, start, 1, basePrice: 2000m)));
        var correction = await store.IngestBarsAsync(Request(Chunk("@GC", 60, start, 1, basePrice: 2500m), compact: true));

        Assert.Equal(1, correction.Streams[0].Updated);
        Assert.Equal(0, correction.Streams[0].Duplicates);

        var repository = new DataSourceRepository(Path.Combine(_root, Broker));
        var candles = await repository.LoadAllDataAsync("@GC", "OneHour");
        Assert.Equal(2500m, Assert.Single(candles).Open);
    }

    /// <summary>
    /// Un buco va dichiarato, mai riempito: inventare barre e' esattamente cio' che un datafeed non
    /// deve fare. E il fine settimana va distinto da una storia mancante, altrimenti il bot
    /// continuerebbe a richiedere al broker un periodo in cui il mercato era chiuso.
    /// </summary>
    [Fact]
    public async Task DeclaresGapsAndMarksTheWeekendOnes()
    {
        var store = CreateStore();
        var friday = new DateTime(2026, 3, 6, 20, 0, 0, DateTimeKind.Utc);

        await store.IngestBarsAsync(Request(Chunk("@CL", 60, friday, 2)));                     // ven 20:00, 21:00
        await store.IngestBarsAsync(Request(Chunk("@CL", 60, friday.AddDays(3), 2)));           // lun 20:00, 21:00
        await store.IngestBarsAsync(Request(Chunk("@CL", 60, friday.AddDays(3).AddHours(5), 2), compact: true)); // lun 01:00+

        var status = await store.GetStatusAsync(Broker, "@CL", 60);

        Assert.Equal(2, status.GapCount);
        var weekend = status.Gaps.Single(gap => gap.SpansWeekend);
        Assert.Equal(friday.AddHours(1), weekend.FromUtc);
        Assert.Equal(friday.AddDays(3), weekend.ToUtc);
        Assert.Contains(status.Gaps, gap => !gap.SpansWeekend);
    }

    /// <summary>
    /// Un buco lungo contiene per forza dei fine settimana, ma non e' un fine settimana: e' storia
    /// mancante. La distinzione non e' estetica — il bot raccoglitore <b>salta</b> i blocchi che
    /// cadono dentro un buco marcato weekend, quindi un feed fatto di tre barre vecchie piu' due
    /// mesi recenti veniva dichiarato coperto per gli anni in mezzo e non si riempiva mai, per
    /// quanti run gli si dessero. Caso reale: <c>@FDAX/240</c> su FTMO, 282 barre in tutto, buco di
    /// 1276 giorni fra il 2023-01-01 e il 2026-07-01, e ogni backtest su quel periodo trovava zero
    /// candele.
    /// </summary>
    [Fact]
    public async Task ALongGapIsMissingHistoryNotAWeekend()
    {
        var store = CreateStore();
        var old = new DateTime(2022, 12, 28, 8, 0, 0, DateTimeKind.Utc);

        await store.IngestBarsAsync(Request(Chunk("@FDAX", 240, old, 3)));
        await store.IngestBarsAsync(Request(Chunk("@FDAX", 240, new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc), 3),
            compact: true));

        var status = await store.GetStatusAsync(Broker, "@FDAX", 240);

        var gap = Assert.Single(status.Gaps);
        Assert.False(gap.SpansWeekend);
    }

    /// <summary>
    /// L'artefatto finale deve essere leggibile da <see cref="DataSourceRepository"/> senza
    /// conversioni: se il feed raccolto non fosse un feed come gli altri, non servirebbe a niente.
    /// Compreso il manifest degli orologi, senza il quale la lettura si rifiuta di partire.
    /// </summary>
    [Fact]
    public async Task ProducesAFeedTheRepositoryCanRead()
    {
        var store = CreateStore();
        var start = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

        await store.IngestBarsAsync(Request(Chunk("@NQ", 15, start, 4, basePrice: 21000m), compact: true));

        Assert.True(File.Exists(Path.Combine(_root, Broker, FeedClockRegistry.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(_root, Broker, "@NQ_15.json")));

        var repository = new DataSourceRepository(Path.Combine(_root, Broker));
        var candles = await repository.LoadAllDataAsync("@NQ", "FifteenMinute");

        Assert.Equal(4, candles.Count);
        Assert.Equal(start, candles[0].DateTime);
        Assert.Equal(21000m, candles[0].Open);
        Assert.Contains("@NQ", repository.GetAvailableSymbols());
    }

    /// <summary>
    /// Due broker, stesso simbolo, stesso istante: sono due serie diverse e devono restare due file
    /// diversi. Se finissero nello stesso, l'ultimo arrivato sovrascriverebbe la barra dell'altro —
    /// e il feed risultante non corrisponderebbe a nessuno dei due conti, senza che niente lo dica.
    /// </summary>
    [Fact]
    public async Task TwoBrokersNeverShareAFile()
    {
        var store = CreateStore();
        var start = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        await store.IngestBarsAsync(Request(Chunk("@NQ", 60, start, 3, basePrice: 21000m, broker: "ICMARKETS"), compact: true));
        await store.IngestBarsAsync(Request(Chunk("@NQ", 60, start, 3, basePrice: 21500m, broker: "Pepperstone Ltd"), compact: true));

        Assert.True(File.Exists(Path.Combine(_root, "ICMARKETS", "@NQ_60.json")));
        Assert.True(File.Exists(Path.Combine(_root, "PEPPERSTONELTD", "@NQ_60.json")));

        var first = await new DataSourceRepository(Path.Combine(_root, "ICMARKETS")).LoadAllDataAsync("@NQ", "OneHour");
        var second = await new DataSourceRepository(Path.Combine(_root, "PEPPERSTONELTD")).LoadAllDataAsync("@NQ", "OneHour");
        Assert.Equal(21000m, first[0].Open);
        Assert.Equal(21500m, second[0].Open);

        var index = await store.GetIndexAsync();
        Assert.Equal(2, index.Feeds.Count);
        Assert.Equal(new[] { "ICMARKETS", "PEPPERSTONELTD" }, index.Feeds.Select(feed => feed.Broker));

        // Con il filtro, solo il broker chiesto.
        Assert.Equal("ICMARKETS", Assert.Single((await store.GetIndexAsync("icmarkets")).Feeds).Broker);
    }

    /// <summary>
    /// Un invio senza codice broker si rifiuta invece di finire in una cartella di ripiego: un feed
    /// di cui non si sa da quale conto viene non e' confrontabile con niente, e scoprirlo mesi dopo
    /// significa buttare la raccolta.
    /// </summary>
    [Fact]
    public async Task RefusesAnIngestWithoutBrokerCode()
    {
        var store = CreateStore();
        var start = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            store.IngestBarsAsync(Request(Chunk("@NQ", 60, start, 1, broker: "   "))));

        // Trattino e underscore restano (sono nomi di cartella legittimi), gli spazi no.
        Assert.Equal("IC-MARKETS", ExternalDatafeedStore.NormalizeBroker("ic-markets "));
        Assert.Equal("ICMARKETS", ExternalDatafeedStore.NormalizeBroker("IC Markets"));

        // Nessun modo di uscire dalla cartella dei feed passando un percorso come codice broker.
        Assert.Equal("ETCPASSWD", ExternalDatafeedStore.NormalizeBroker("../../etc/passwd"));
    }

    /// <summary>
    /// Il journal e' un dettaglio interno, ma la status non deve dipendere dal fatto che sia stato
    /// compattato: un bot che si sente dire "non ho quelle barre" subito dopo averle spedite le
    /// rispedirebbe all'infinito.
    /// </summary>
    [Fact]
    public async Task StatusSeesCandlesStillInTheJournal()
    {
        var store = CreateStore();
        var start = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var ingest = await store.IngestBarsAsync(Request(Chunk("@YM", 60, start, 5)));
        Assert.False(ingest.Streams[0].Compacted);
        Assert.Equal(5, ingest.Streams[0].PendingJournalCandles);

        var status = await store.GetStatusAsync(Broker, "@YM", 60);

        Assert.Equal(5, status.Coverage.TotalCandles);
        Assert.Equal(0, status.PendingJournalCandles);
    }

    /// <summary>
    /// Una barra rotta si scarta, e si dice perche'. Il contrario — accettarla — mette nel feed un
    /// dato falso che nessuno distinguera' piu' da uno vero.
    /// </summary>
    [Fact]
    public async Task RejectsBrokenCandlesAndSaysWhy()
    {
        var store = CreateStore();
        var start = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var response = await store.IngestBarsAsync(Request(new ExternalBarChunkDto
        {
            Broker = Broker,
            Symbol = "@BP",
            TimeframeMinutes = 60,
            Candles = new List<ExternalCandleDto>
            {
                new() { DateTime = start, Open = 100, High = 90, Low = 95, Close = 99, Volume = 1 },      // high < low
                new() { DateTime = DateTime.SpecifyKind(start, DateTimeKind.Local), Open = 100, High = 101, Low = 99, Close = 100, Volume = 1 },
                new() { DateTime = start.AddHours(1), Open = 100, High = 101, Low = 99, Close = 100, Volume = 1 }
            }
        }, compact: true));

        Assert.Equal(1, response.TotalAccepted);
        Assert.Equal(2, response.TotalRejected);
        Assert.Equal(2, response.Streams[0].RejectReasons.Count);
    }

    /// <summary>
    /// I tick sono un flusso, non un artefatto: l'unica cosa che serve e' che due invii sovrapposti
    /// non li duplichino, e che il server sappia dire da dove riprendere.
    /// </summary>
    [Fact]
    public async Task TicksAreAppendedOnceAndResumePointIsReported()
    {
        var store = CreateStore();
        var start = new DateTime(2026, 3, 2, 9, 30, 0, DateTimeKind.Utc);
        var ticks = Enumerable.Range(0, 5)
            .Select(index => new ExternalTickDto { TimeUtc = start.AddSeconds(index), Bid = 100 + index, Ask = 101 + index })
            .ToList();

        var first = await store.IngestTicksAsync(new IngestTicksRequestDto { Broker = Broker, Symbol = "@NQ", Ticks = ticks });
        var second = await store.IngestTicksAsync(new IngestTicksRequestDto { Broker = Broker, Symbol = "@NQ", Ticks = ticks });

        Assert.Equal(5, first.Accepted);
        Assert.Equal(0, second.Accepted);
        Assert.Equal(5, second.Stale);
        Assert.Equal(start.AddSeconds(4), second.LastTickUtc);

        var file = Path.Combine(_root, Broker, "ticks", "@NQ", $"@NQ_ticks_{start:yyyyMMdd}.jsonl");
        Assert.Equal(5, File.ReadAllLines(file).Length);
    }

    // ---------------------------------------------------------------------------------------------

    private static IngestBarsRequestDto Request(ExternalBarChunkDto chunk, bool compact = false)
        => new() { Chunks = new List<ExternalBarChunkDto> { chunk }, Compact = compact };

    private const string Broker = "ICMARKETS";

    private static ExternalBarChunkDto Chunk(
        string symbol,
        int timeframeMinutes,
        DateTime startUtc,
        int count,
        decimal basePrice = 100m,
        string broker = Broker)
    {
        var chunk = new ExternalBarChunkDto { Broker = broker, Symbol = symbol, TimeframeMinutes = timeframeMinutes };
        for (var index = 0; index < count; index++)
        {
            var open = basePrice + index;
            chunk.Candles.Add(new ExternalCandleDto
            {
                DateTime = startUtc.AddMinutes(timeframeMinutes * index),
                Open = open,
                High = open + 5,
                Low = open - 5,
                Close = open + 1,
                Volume = 1000 + index
            });
        }

        return chunk;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
