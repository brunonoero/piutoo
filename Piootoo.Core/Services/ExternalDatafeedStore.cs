using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Datafeed;

namespace Piootoo.Core.Services;

/// <summary>
/// Colleziona su filesystem il datafeed raccolto da un bot esterno (cTrader), a pezzi e in ordine
/// qualsiasi, fino a ricostruire lo stesso artefatto che il repository sa gia' leggere:
/// <c>datafeed-external/{BROKER}/@SYM_{minuti}.json</c>, identico per forma e convenzione ai file
/// piatti di <c>datafeed/</c>.
///
/// <para><b>Perche' una cartella per broker.</b> Le barre dello stesso simbolo prese da due broker
/// diversi non sono la stessa serie: cambiano l'orario di sessione, il bucket in cui cade la barra
/// e il volume (che e' conteggio tick, non contratti). Mescolarle in un unico file darebbe un feed
/// che non corrisponde a nessuno dei due, e nessuno se ne accorgerebbe. Tenendole separate,
/// <c>datafeed-external/ICMARKETS</c> e' invece una cartella di feed completa a se' stante — ha il
/// proprio <c>feed-clocks.json</c> — e ci si puo' puntare <c>DataSourceRepository</c> direttamente
/// per fare un backtest su quei dati.</para>
///
/// <para><b>Perche' a pezzi.</b> Lo storico di uno strumento sono decine di migliaia di barre, e il
/// broker le consegna a blocchi. Una singola chiamata che le raccolga tutte va in timeout, e se
/// muore a meta' non lascia niente di riutilizzabile. Qui l'unita' e' il blocco: piccolo, autonomo,
/// idempotente. Si puo' completare un feed in cento invii, spalmati su piu' sessioni, riprendendo
/// da dove si era rimasti — <see cref="GetStatusAsync"/> dice al bot cosa c'e' gia'.</para>
///
/// <para><b>Perche' un journal.</b> Riscrivere il file piatto a ogni blocco costa quanto tutto il
/// feed gia' raccolto, e rende quadratico un backfill che e' lineare: e' la stessa trappola dei
/// checkpoint di <c>TradingJsonStore</c> (CLAUDE.md, "I checkpoint non riscrivono l'artefatto
/// intero"). I blocchi si accodano a <c>.pending/@SYM_{minuti}.jsonl</c> — append puro, niente
/// fsync — e il file piatto viene materializzato alla compattazione: a soglia, su richiesta
/// esplicita del bot a fine backfill, e sempre prima di rispondere a una lettura.</para>
///
/// <para><b>Sovrapposizioni e buchi.</b> La chiave di una barra e' il suo istante di apertura,
/// quindi due blocchi che si sovrappongono collassano da soli: la barra identica e' un duplicato e
/// non viene nemmeno scritta, quella diversa vince perche' e' arrivata dopo (rimandare un periodo
/// e' il modo di correggere barre sbagliate). I buchi non si "riempiono": si <i>dichiarano</i>, in
/// <see cref="ExternalFeedStatusDto.Gaps"/>, perche' inventare barre mancanti e' esattamente cio'
/// che un datafeed non deve fare — e il weekend, che e' un buco legittimo, viene marcato come tale
/// invece di essere confuso con storia persa.</para>
/// </summary>
public sealed class ExternalDatafeedStore
{
    /// <summary>Barre nel journal oltre le quali si compatta senza aspettare che lo chieda il bot.</summary>
    private const int JournalCompactThreshold = 20_000;

    /// <summary>Tetto ai buchi restituiti da una status: oltre, la lista e' rumore.</summary>
    private const int MaxReportedGaps = 200;

    /// <summary>Ragioni di scarto distinte riportate al chiamante.</summary>
    private const int MaxRejectReasons = 5;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static readonly Dictionary<int, string> CanonicalBarTypes = new()
    {
        [1] = "OneMinute",
        [5] = "FiveMinute",
        [15] = "FifteenMinute",
        [30] = "ThirtyMinute",
        [60] = "OneHour",
        [240] = "FourHour",
        [1440] = "Daily",
        [10080] = "Weekly"
    };

    private readonly string _root;
    private readonly ConcurrentDictionary<string, StreamState> _streams = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tickGates = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _manifestGate = new(1, 1);

    public ExternalDatafeedStore(PiootooSettings settings)
    {
        _root = settings.GetExternalRepositoryPath();
        Directory.CreateDirectory(_root);
    }

    public string RootPath => _root;

    /// <summary>
    /// Codice broker ridotto a nome di cartella: maiuscolo, solo lettere, cifre, trattino e
    /// underscore. Non si usa la stringa com'e' perche' arriva da un bot, cioe' da fuori: un
    /// <c>..</c> dentro un percorso costruito con <see cref="Path.Combine"/> uscirebbe dal
    /// repository. Vuoto e' un errore esplicito e non un default, perche' un feed di cui non si sa
    /// da quale broker viene e' un feed che non si puo' confrontare con niente.
    /// </summary>
    public static string NormalizeBroker(string? broker)
    {
        if (string.IsNullOrWhiteSpace(broker))
        {
            throw new ArgumentException(
                "Codice broker mancante. Ogni feed esterno vive nella cartella del broker che lo ha " +
                "prodotto (es. 'ICMARKETS'): senza, due broker finirebbero nello stesso file.");
        }

        var builder = new StringBuilder(broker.Length);
        foreach (var character in broker.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
                builder.Append(character);
        }

        if (builder.Length == 0)
            throw new ArgumentException($"Codice broker '{broker}' non utilizzabile come nome di cartella.");

        return builder.ToString();
    }

    // -------------------------------------------------------------------------------------------
    // Ingestione barre
    // -------------------------------------------------------------------------------------------

    public async Task<IngestBarsResponseDto> IngestBarsAsync(IngestBarsRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Chunks.Count == 0)
            throw new ArgumentException("Nessun blocco da importare: 'chunks' e' vuoto.");

        var response = new IngestBarsResponseDto();
        foreach (var chunk in request.Chunks)
        {
            var result = await IngestChunkAsync(chunk, request.Compact);
            response.Streams.Add(result);
            response.TotalAccepted += result.Accepted;
            response.TotalDuplicates += result.Duplicates;
            response.TotalRejected += result.Rejected;
        }

        return response;
    }

    private async Task<ExternalStreamIngestResultDto> IngestChunkAsync(ExternalBarChunkDto chunk, bool forceCompact)
    {
        var broker = NormalizeBroker(chunk.Broker);
        var symbol = NormalizeSymbol(chunk.Symbol);
        var timeframe = chunk.TimeframeMinutes;
        if (timeframe <= 0)
            throw new ArgumentException($"Timeframe non valido per '{symbol}': {timeframe} minuti.");

        var result = new ExternalStreamIngestResultDto
        {
            Broker = broker,
            Symbol = symbol,
            TimeframeMinutes = timeframe,
            Received = chunk.Candles.Count
        };

        await EnsureFeedClockDeclaredAsync(broker, symbol);

        var state = GetState(broker, symbol, timeframe);
        await state.Gate.WaitAsync();
        try
        {
            await state.EnsureIndexLoadedAsync();

            var lines = new StringBuilder();
            var reasons = new HashSet<string>(StringComparer.Ordinal);

            foreach (var candle in chunk.Candles)
            {
                if (!TryConvert(candle, out var flat, out var reason))
                {
                    result.Rejected++;
                    if (reasons.Count < MaxRejectReasons)
                        reasons.Add(reason);
                    continue;
                }

                var key = flat.DateTime.Ticks;
                var fingerprint = Fingerprint(flat);
                if (state.Index!.TryGetValue(key, out var known))
                {
                    if (known == fingerprint)
                    {
                        // La sovrapposizione fra due blocchi finisce qui: non si scrive niente.
                        result.Duplicates++;
                        continue;
                    }

                    result.Updated++;
                }
                else
                {
                    result.Accepted++;
                }

                state.Index[key] = fingerprint;
                lines.Append(JsonSerializer.Serialize(flat, Json)).Append('\n');
                state.PendingJournalCandles++;
            }

            result.RejectReasons.AddRange(reasons);

            if (lines.Length > 0)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(state.JournalPath)!);
                // Append puro, niente fsync: qui si sta dentro il ciclo di invio del bot, e la
                // durabilita' la garantisce la compattazione. Un journal perso si ri-spedisce.
                await File.AppendAllTextAsync(state.JournalPath, lines.ToString(), Encoding.UTF8);
            }

            if (!string.IsNullOrWhiteSpace(chunk.Source))
                state.Source = chunk.Source;

            result.PendingJournalCandles = state.PendingJournalCandles;

            if (forceCompact || state.PendingJournalCandles >= JournalCompactThreshold)
            {
                var candles = await CompactLockedAsync(state);
                result.Compacted = true;
                result.PendingJournalCandles = 0;
                result.Coverage = BuildCoverage(candles);
            }

            return result;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    // -------------------------------------------------------------------------------------------
    // Stato e copertura
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Cosa il server ha gia' di uno stream. E' la chiamata con cui il bot decide da dove
    /// ripartire: senza, ogni riavvio ricomincerebbe il backfill dall'inizio.
    /// </summary>
    public async Task<ExternalFeedStatusDto> GetStatusAsync(
        string broker,
        string symbol,
        int timeframeMinutes,
        int? gapToleranceMinutes = null)
    {
        var normalizedBroker = NormalizeBroker(broker);
        var normalized = NormalizeSymbol(symbol);
        if (timeframeMinutes <= 0)
            throw new ArgumentException($"Timeframe non valido: {timeframeMinutes} minuti.");

        var state = GetState(normalizedBroker, normalized, timeframeMinutes);
        await state.Gate.WaitAsync();
        try
        {
            await state.EnsureIndexLoadedAsync();

            // Si compatta prima di rispondere: una status che ignora il journal direbbe al bot che
            // gli mancano barre che ha appena spedito, e lo farebbe rispedire all'infinito.
            var candles = state.PendingJournalCandles > 0
                ? await CompactLockedAsync(state)
                : await ReadFlatCandlesAsync(state.FlatPath);

            return BuildStatus(state, candles, gapToleranceMinutes);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    /// <summary>
    /// Tutti i feed esterni presenti, con la loro copertura. Senza <paramref name="broker"/> elenca
    /// tutti i broker raccolti.
    /// </summary>
    public async Task<ExternalFeedIndexDto> GetIndexAsync(string? broker = null, int? gapToleranceMinutes = null)
    {
        var index = new ExternalFeedIndexDto { RootPath = _root };
        var filter = string.IsNullOrWhiteSpace(broker) ? null : NormalizeBroker(broker);

        foreach (var (feedBroker, symbol, timeframe) in EnumerateFeeds())
        {
            if (filter != null && !string.Equals(filter, feedBroker, StringComparison.OrdinalIgnoreCase))
                continue;

            index.Feeds.Add(await GetStatusAsync(feedBroker, symbol, timeframe, gapToleranceMinutes));
        }

        index.Feeds = index.Feeds
            .OrderBy(feed => feed.Broker, StringComparer.OrdinalIgnoreCase)
            .ThenBy(feed => feed.Symbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(feed => feed.TimeframeMinutes)
            .ToList();
        return index;
    }

    /// <summary>
    /// Materializza il journal nel file piatto. Il bot la chiama a fine backfill di uno stream; per
    /// il resto il server decide da solo. Senza argomenti compatta tutto.
    /// </summary>
    public async Task<CompactExternalFeedsResponseDto> CompactAsync(string? broker, string? symbol, int? timeframeMinutes)
    {
        var filter = string.IsNullOrWhiteSpace(broker) ? null : NormalizeBroker(broker);
        var targets = filter is null || symbol is null || timeframeMinutes is null
            ? EnumerateFeeds()
                .Where(feed => filter is null || string.Equals(filter, feed.Broker, StringComparison.OrdinalIgnoreCase))
                .ToList()
            : new List<(string Broker, string Symbol, int TimeframeMinutes)>
                { (filter, NormalizeSymbol(symbol), timeframeMinutes.Value) };

        var response = new CompactExternalFeedsResponseDto();
        foreach (var (feedBroker, feedSymbol, feedTimeframe) in targets)
        {
            var state = GetState(feedBroker, feedSymbol, feedTimeframe);
            await state.Gate.WaitAsync();
            try
            {
                await state.EnsureIndexLoadedAsync();
                var candles = await CompactLockedAsync(state);
                response.Streams.Add(new ExternalStreamIngestResultDto
                {
                    Broker = feedBroker,
                    Symbol = feedSymbol,
                    TimeframeMinutes = feedTimeframe,
                    Compacted = true,
                    Coverage = BuildCoverage(candles)
                });
            }
            finally
            {
                state.Gate.Release();
            }
        }

        return response;
    }

    // -------------------------------------------------------------------------------------------
    // Ingestione tick
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Accoda tick a file giornalieri <c>{BROKER}/ticks/@SYM/@SYM_ticks_yyyyMMdd.jsonl</c>. I tick non si
    /// compattano in un artefatto: sono un flusso, e l'unica cosa che serve e' che due invii
    /// sovrapposti non li duplichino. Per questo si tiene <c>lastTickUtc</c> per simbolo e si
    /// scarta tutto cio' che non lo supera: e' anche il punto da cui il bot riprende.
    /// </summary>
    public async Task<IngestTicksResponseDto> IngestTicksAsync(IngestTicksRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var broker = NormalizeBroker(request.Broker);
        var symbol = NormalizeSymbol(request.Symbol);

        var response = new IngestTicksResponseDto
        {
            Broker = broker,
            Symbol = symbol,
            Received = request.Ticks.Count
        };

        await EnsureFeedClockDeclaredAsync(broker, symbol);

        var gate = _tickGates.GetOrAdd($"{broker}/{symbol}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var directory = Path.Combine(_root, broker, "ticks", symbol);
            Directory.CreateDirectory(directory);

            var statePath = Path.Combine(directory, $"{symbol}_ticks_state.json");
            var state = await ReadTickStateAsync(statePath);

            var reasons = new HashSet<string>(StringComparer.Ordinal);
            var buffers = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);

            // Ordinati per tempo: un blocco puo' arrivare disordinato, e il filtro di monotonia
            // scarterebbe come "vecchio" un tick che invece e' solo fuori sequenza nel payload.
            foreach (var tick in request.Ticks.OrderBy(tick => tick.TimeUtc))
            {
                if (!TryValidateTick(tick, out var reason))
                {
                    response.Rejected++;
                    if (reasons.Count < MaxRejectReasons)
                        reasons.Add(reason);
                    continue;
                }

                var fingerprint = HashCode.Combine(tick.Bid, tick.Ask);
                if (state.LastTickUtc is { } last)
                {
                    if (tick.TimeUtc < last)
                    {
                        response.Stale++;
                        continue;
                    }

                    // Stesso istante e stessi prezzi dell'ultimo scritto: e' il ri-invio dello
                    // stesso blocco, non un secondo tick sul medesimo millisecondo.
                    if (tick.TimeUtc == last && fingerprint == state.LastFingerprint)
                    {
                        response.Stale++;
                        continue;
                    }
                }

                var fileName = $"{symbol}_ticks_{tick.TimeUtc:yyyyMMdd}.jsonl";
                if (!buffers.TryGetValue(fileName, out var buffer))
                    buffers[fileName] = buffer = new StringBuilder();

                buffer.Append('{')
                    .Append("\"t\":\"").Append(tick.TimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)).Append("\",")
                    .Append("\"bid\":").Append(tick.Bid.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append("\"ask\":").Append(tick.Ask.ToString(CultureInfo.InvariantCulture))
                    .Append("}\n");

                state.LastTickUtc = tick.TimeUtc;
                state.LastFingerprint = fingerprint;
                response.Accepted++;
            }

            foreach (var (fileName, buffer) in buffers)
            {
                await File.AppendAllTextAsync(Path.Combine(directory, fileName), buffer.ToString(), Encoding.UTF8);
                response.Files.Add(fileName);
            }

            if (response.Accepted > 0)
            {
                if (!string.IsNullOrWhiteSpace(request.Source))
                    state.Source = request.Source;
                await WriteTickStateAsync(statePath, state);
            }

            response.RejectReasons.AddRange(reasons);
            response.LastTickUtc = state.LastTickUtc;
            return response;
        }
        finally
        {
            gate.Release();
        }
    }

    // -------------------------------------------------------------------------------------------
    // Compattazione e I/O del file piatto
    // -------------------------------------------------------------------------------------------

    /// <summary>Da chiamare con il gate dello stream gia' preso.</summary>
    private async Task<List<FlatCandleDto>> CompactLockedAsync(StreamState state)
    {
        var merged = new SortedDictionary<long, FlatCandleDto>();
        foreach (var candle in await ReadFlatCandlesAsync(state.FlatPath))
            merged[candle.DateTime.Ticks] = candle;

        if (File.Exists(state.JournalPath))
        {
            // In ordine di arrivo: l'ultima versione di una barra vince, perche' rimandare un
            // periodo e' il modo con cui si corregge una barra sbagliata.
            foreach (var line in await File.ReadAllLinesAsync(state.JournalPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var candle = JsonSerializer.Deserialize<FlatCandleDto>(line, Json);
                if (candle is not null)
                    merged[candle.DateTime.Ticks] = candle;
            }
        }

        var candles = merged.Values.ToList();
        state.LastUpdate = DateTime.UtcNow;
        WriteFlatFeed(state, candles);

        if (File.Exists(state.JournalPath))
            File.Delete(state.JournalPath);
        state.PendingJournalCandles = 0;

        state.Index = candles.ToDictionary(candle => candle.DateTime.Ticks, Fingerprint);
        return candles;
    }

    /// <summary>
    /// Scrive il file piatto con lo stesso layout di <c>datafeed/@SYM_{minuti}.json</c>: intestazione
    /// su una riga e una candela per riga. Non e' estetica — e' cio' che rende leggibile un diff e
    /// ispezionabile con <c>head</c> un file da decine di megabyte.
    /// </summary>
    private void WriteFlatFeed(StreamState state, List<FlatCandleDto> candles)
    {
        var barType = CanonicalBarTypes.TryGetValue(state.TimeframeMinutes, out var known)
            ? known
            : state.TimeframeMinutes.ToString(CultureInfo.InvariantCulture);

        // Artefatto finale: qui la scrittura durabile ci sta, ed e' fuori dal loop di ingestione.
        AtomicFileWriter.Write(state.FlatPath, stream =>
        {
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1 << 16, leaveOpen: true);
            writer.Write("{\"symbol\":");
            writer.Write(JsonSerializer.Serialize(state.Symbol, Json));
            writer.Write(",\"barType\":");
            writer.Write(JsonSerializer.Serialize(barType, Json));
            writer.Write(",\"timeframeMinutes\":");
            writer.Write(state.TimeframeMinutes.ToString(CultureInfo.InvariantCulture));
            writer.Write(",\"barEnd\":null,\"lastUpdate\":\"");
            writer.Write((state.LastUpdate ?? DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            writer.Write("\",\"source\":");
            writer.Write(JsonSerializer.Serialize(state.Source ?? "external-sync", Json));
            writer.Write(",\"candles\":[\n");

            for (var i = 0; i < candles.Count; i++)
            {
                writer.Write(JsonSerializer.Serialize(candles[i], Json));
                writer.Write(i == candles.Count - 1 ? "\n" : ",\n");
            }

            writer.Write("]}");
            writer.Flush();
        }, durable: true);
    }

    private static async Task<List<FlatCandleDto>> ReadFlatCandlesAsync(string path)
    {
        if (!File.Exists(path))
            return new List<FlatCandleDto>();

        await using var stream = File.OpenRead(path);
        var feed = await JsonSerializer.DeserializeAsync<FlatFeedFileDto>(stream, Json);
        return feed?.Candles ?? new List<FlatCandleDto>();
    }

    // -------------------------------------------------------------------------------------------
    // Copertura, buchi
    // -------------------------------------------------------------------------------------------

    private ExternalFeedStatusDto BuildStatus(
        StreamState state,
        List<FlatCandleDto> candles,
        int? gapToleranceMinutes)
    {
        var coverage = BuildCoverage(candles);
        var step = coverage.DominantStepMinutes ?? state.TimeframeMinutes;
        var tolerance = gapToleranceMinutes ?? Math.Max(step * 2, step + 1);

        var status = new ExternalFeedStatusDto
        {
            Broker = state.Broker,
            Symbol = state.Symbol,
            TimeframeMinutes = state.TimeframeMinutes,
            FilePath = state.FlatPath,
            Coverage = coverage,
            LastUpdateUtc = state.LastUpdate,
            Source = state.Source,
            PendingJournalCandles = state.PendingJournalCandles,
            GapToleranceMinutes = tolerance
        };

        var gaps = new List<ExternalFeedGapDto>();
        for (var i = 1; i < candles.Count; i++)
        {
            var previous = candles[i - 1].DateTime;
            var current = candles[i].DateTime;
            var minutes = (int)Math.Round((current - previous).TotalMinutes);
            if (minutes <= tolerance)
                continue;

            gaps.Add(new ExternalFeedGapDto
            {
                FromUtc = previous,
                ToUtc = current,
                MinutesMissing = minutes,
                EstimatedMissingCandles = step > 0 ? Math.Max(0, minutes / step - 1) : 0,
                SpansWeekend = SpansWeekend(previous, current)
            });
        }

        status.GapCount = gaps.Count;
        status.GapsTruncated = gaps.Count > MaxReportedGaps;
        status.Gaps = gaps
            .OrderByDescending(gap => gap.MinutesMissing)
            .Take(MaxReportedGaps)
            .OrderBy(gap => gap.FromUtc)
            .ToList();

        return status;
    }

    private static ExternalFeedCoverageDto BuildCoverage(List<FlatCandleDto> candles)
    {
        var coverage = new ExternalFeedCoverageDto { TotalCandles = candles.Count };
        if (candles.Count == 0)
            return coverage;

        coverage.FirstCandleUtc = candles[0].DateTime;
        coverage.LastCandleUtc = candles[^1].DateTime;

        // Il passo si DEDUCE dai dati invece di assumerlo dal timeframe dichiarato: un giornaliero
        // di broker apre alle 22:00 o alle 23:00 UTC, non a mezzanotte, e assumere 1440 allineati
        // all'epoch farebbe comparire un buco per ogni giornata.
        var steps = new Dictionary<int, int>();
        for (var i = 1; i < candles.Count; i++)
        {
            var minutes = (int)Math.Round((candles[i].DateTime - candles[i - 1].DateTime).TotalMinutes);
            if (minutes <= 0)
                continue;

            steps[minutes] = steps.TryGetValue(minutes, out var count) ? count + 1 : 1;
        }

        if (steps.Count > 0)
            coverage.DominantStepMinutes = steps.OrderByDescending(entry => entry.Value).First().Key;

        return coverage;
    }

    /// <summary>
    /// Un buco e' "di fine settimana" — mercato chiuso, non storia mancante — solo se contiene un
    /// sabato o una domenica <b>e</b> nessun giorno feriale intero. Il secondo pezzo non e' un
    /// dettaglio: senza, bastava che un buco contenesse un weekend qualsiasi per essere assolto, e
    /// un buco di tre anni ne contiene centocinquanta. Il bot raccoglitore salta i buchi cosi'
    /// marcati (<c>IsAlreadyCovered</c>), quindi un feed con tre barre del 2022 e due mesi del 2026
    /// veniva dichiarato coperto per tutto quello che c'e' in mezzo e non si riempiva mai — per
    /// quanto lo si rilanciasse.
    ///
    /// <para>Il confronto e' sui giorni <i>interi</i> compresi fra le due barre: il venerdi' della
    /// barra prima e il lunedi' di quella dopo sono per definizione parziali, e contarli farebbe
    /// passare per storia mancante ogni normale chiusura del fine settimana.</para>
    /// </summary>
    private static bool SpansWeekend(DateTime fromUtc, DateTime toUtc)
    {
        var weekendSeen = false;

        for (var day = fromUtc.Date.AddDays(1); day <= toUtc.Date.AddDays(-1); day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                weekendSeen = true;
            else
                return false; // un giorno feriale intero senza barre e' storia mancante, punto.
        }

        if (weekendSeen)
            return true;

        // Buco corto che non contiene alcun giorno intero (es. venerdi' 21:00 -> sabato 01:00):
        // e' di fine settimana se uno dei due estremi cade nel weekend.
        return fromUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
               toUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    // -------------------------------------------------------------------------------------------
    // Manifest degli orologi
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Il feed esterno deve dichiarare il proprio fuso come qualunque altro: <see cref="FeedClockRegistry"/>
    /// si rifiuta di leggere una cartella senza manifest, ed e' la regola che impedisce di prendere
    /// per UTC dei timestamp che non lo sono. Qui il fuso e' noto e vale <c>UTC</c> davvero — la
    /// piattaforma cTrader espone gli orari delle barre in UTC e il bot li spedisce cosi' — quindi
    /// il manifest si scrive da solo, ma non si sovrascrive mai una voce gia' presente: se qualcuno
    /// l'ha corretta a mano, ha piu' ragione di questo codice.
    /// </summary>
    private async Task EnsureFeedClockDeclaredAsync(string broker, string symbol)
    {
        var key = symbol.TrimStart('@').ToUpperInvariant();
        // Un manifest per cartella broker, non uno solo alla radice: la cartella di un broker deve
        // poter essere passata cosi' com'e' a DataSourceRepository, che il manifest lo cerca li'.
        var path = Path.Combine(_root, broker, FeedClockRegistry.ManifestFileName);

        await _manifestGate.WaitAsync();
        try
        {
            var manifest = File.Exists(path)
                ? JsonSerializer.Deserialize<ClockManifestDto>(await File.ReadAllTextAsync(path), Json)
                : null;
            manifest ??= new ClockManifestDto();
            manifest.Orologi ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (manifest.Orologi.ContainsKey(key))
                return;

            manifest.Orologi[key] = "UTC";
            manifest.Nota = new[]
            {
                "Generato da ExternalDatafeedStore. I feed di questa cartella arrivano da un bot",
                "raccoglitore cTrader, che spedisce gli orari delle barre in UTC vero: la piattaforma",
                "li espone gia' cosi' (Bars.OpenTimes / Server.TimeInUtc), quindi qui UTC non e'",
                "un'assunzione ma il fuso dichiarato dalla sorgente.",
                "Correggere a mano una voce e' lecito: il codice non sovrascrive quelle esistenti."
            };

            var ordered = new ClockManifestDto
            {
                Nota = manifest.Nota,
                Orologi = manifest.Orologi
                    .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase)
            };

            AtomicFileWriter.WriteAllText(
                path,
                JsonSerializer.Serialize(ordered, new JsonSerializerOptions(Json) { WriteIndented = true }));
        }
        finally
        {
            _manifestGate.Release();
        }
    }

    // -------------------------------------------------------------------------------------------
    // Supporto
    // -------------------------------------------------------------------------------------------

    private IEnumerable<(string Broker, string Symbol, int TimeframeMinutes)> EnumerateFeeds()
    {
        if (!Directory.Exists(_root))
            yield break;

        foreach (var brokerDirectory in Directory.EnumerateDirectories(_root)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var broker = Path.GetFileName(brokerDirectory);

            foreach (var path in Directory.EnumerateFiles(brokerDirectory, "@*.json", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var parsed = ParseFlatFileName(Path.GetFileNameWithoutExtension(path));
                if (parsed is { } feed)
                    yield return (broker, feed.Symbol, feed.TimeframeMinutes);
            }

            // Uno stream che ha solo journal (blocchi arrivati, mai compattati) esiste comunque:
            // e' il caso del bot morto a meta' backfill, ed e' proprio quello che si vuole vedere.
            var pending = Path.Combine(brokerDirectory, ".pending");
            if (!Directory.Exists(pending))
                continue;

            foreach (var path in Directory.EnumerateFiles(pending, "@*.jsonl", SearchOption.TopDirectoryOnly))
            {
                var parsed = ParseFlatFileName(Path.GetFileNameWithoutExtension(path));
                if (parsed is { } feed &&
                    !File.Exists(Path.Combine(brokerDirectory, $"{feed.Symbol}_{feed.TimeframeMinutes}.json")))
                {
                    yield return (broker, feed.Symbol, feed.TimeframeMinutes);
                }
            }
        }
    }

    private static (string Symbol, int TimeframeMinutes)? ParseFlatFileName(string fileNameWithoutExtension)
    {
        if (!fileNameWithoutExtension.StartsWith('@'))
            return null;

        var lastUnderscore = fileNameWithoutExtension.LastIndexOf('_');
        if (lastUnderscore <= 1 || lastUnderscore == fileNameWithoutExtension.Length - 1)
            return null;

        return int.TryParse(fileNameWithoutExtension[(lastUnderscore + 1)..], out var minutes)
            ? (fileNameWithoutExtension[..lastUnderscore], minutes)
            : null;
    }

    private StreamState GetState(string broker, string symbol, int timeframeMinutes)
        => _streams.GetOrAdd(
            $"{broker}/{symbol}_{timeframeMinutes}",
            _ => new StreamState(_root, broker, symbol, timeframeMinutes));

    private static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Simbolo mancante.");

        return "@" + symbol.Trim().TrimStart('@').ToUpperInvariant();
    }

    /// <summary>
    /// Converte e valida una candela in arrivo. Si scarta solo cio' che e' <i>rotto</i> — un OHLC
    /// incoerente, un prezzo non positivo, un istante non UTC — e mai cio' che e' solo inatteso: in
    /// particolare NON si pretende l'allineamento all'epoch, perche' i timeframe alti del broker
    /// aprono all'orario di sessione e rifiutarli svuoterebbe il feed giornaliero.
    /// </summary>
    private static bool TryConvert(ExternalCandleDto candle, out FlatCandleDto flat, out string reason)
    {
        flat = default!;

        if (candle.DateTime.Kind != DateTimeKind.Utc)
        {
            reason = "istante non UTC (atteso un timestamp con suffisso 'Z')";
            return false;
        }

        if (candle.DateTime.Year < 1990 || candle.DateTime > DateTime.UtcNow.AddDays(1))
        {
            reason = $"istante fuori intervallo plausibile ({candle.DateTime:O})";
            return false;
        }

        if (candle.Open <= 0 || candle.High <= 0 || candle.Low <= 0 || candle.Close <= 0)
        {
            reason = "prezzo non positivo";
            return false;
        }

        if (candle.High < candle.Low ||
            candle.High < Math.Max(candle.Open, candle.Close) ||
            candle.Low > Math.Min(candle.Open, candle.Close))
        {
            reason = "OHLC incoerente (high/low non contengono open/close)";
            return false;
        }

        if (candle.Volume < 0)
        {
            reason = "volume negativo";
            return false;
        }

        flat = new FlatCandleDto
        {
            Timestamp = new DateTimeOffset(candle.DateTime).ToUnixTimeSeconds(),
            DateTime = candle.DateTime,
            DateTimeFormatted = candle.DateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Open = candle.Open,
            High = candle.High,
            Low = candle.Low,
            Close = candle.Close,
            Volume = candle.Volume
        };
        reason = string.Empty;
        return true;
    }

    private static bool TryValidateTick(ExternalTickDto tick, out string reason)
    {
        if (tick.TimeUtc.Kind != DateTimeKind.Utc)
        {
            reason = "istante non UTC (atteso un timestamp con suffisso 'Z')";
            return false;
        }

        if (tick.TimeUtc.Year < 1990 || tick.TimeUtc > DateTime.UtcNow.AddDays(1))
        {
            reason = $"istante fuori intervallo plausibile ({tick.TimeUtc:O})";
            return false;
        }

        if (tick.Bid <= 0 || tick.Ask <= 0)
        {
            reason = "bid/ask non positivo";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static int Fingerprint(FlatCandleDto candle)
        => HashCode.Combine(candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);

    private static async Task<TickStateDto> ReadTickStateAsync(string path)
    {
        if (!File.Exists(path))
            return new TickStateDto();

        try
        {
            return JsonSerializer.Deserialize<TickStateDto>(await File.ReadAllTextAsync(path), Json)
                   ?? new TickStateDto();
        }
        catch (JsonException)
        {
            // Uno stato illeggibile non deve impedire di raccogliere tick: si riparte da zero, al
            // costo di qualche duplicato in coda al file del giorno.
            return new TickStateDto();
        }
    }

    private static Task WriteTickStateAsync(string path, TickStateDto state)
    {
        AtomicFileWriter.WriteAllText(path, JsonSerializer.Serialize(state, Json), durable: false);
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------------------------
    // Stato per stream
    // -------------------------------------------------------------------------------------------

    private sealed class StreamState
    {
        public StreamState(string root, string broker, string symbol, int timeframeMinutes)
        {
            Broker = broker;
            Symbol = symbol;
            TimeframeMinutes = timeframeMinutes;
            FlatPath = Path.Combine(root, broker, $"{symbol}_{timeframeMinutes}.json");
            JournalPath = Path.Combine(root, broker, ".pending", $"{symbol}_{timeframeMinutes}.jsonl");
        }

        public string Broker { get; }
        public string Symbol { get; }
        public int TimeframeMinutes { get; }
        public string FlatPath { get; }
        public string JournalPath { get; }
        public SemaphoreSlim Gate { get; } = new(1, 1);

        /// <summary>Istante barra -> impronta OHLCV. Evita di rileggere il file a ogni blocco.</summary>
        public Dictionary<long, int>? Index { get; set; }

        public int PendingJournalCandles { get; set; }
        public DateTime? LastUpdate { get; set; }
        public string? Source { get; set; }

        public async Task EnsureIndexLoadedAsync()
        {
            if (Index is not null)
                return;

            var index = new Dictionary<long, int>();
            if (File.Exists(FlatPath))
            {
                await using var stream = File.OpenRead(FlatPath);
                var feed = await JsonSerializer.DeserializeAsync<FlatFeedFileDto>(stream, Json);
                LastUpdate = feed?.LastUpdate;
                Source ??= feed?.Source;
                foreach (var candle in feed?.Candles ?? new List<FlatCandleDto>())
                    index[candle.DateTime.Ticks] = Fingerprint(candle);
            }

            var pending = 0;
            if (File.Exists(JournalPath))
            {
                foreach (var line in await File.ReadAllLinesAsync(JournalPath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var candle = JsonSerializer.Deserialize<FlatCandleDto>(line, Json);
                    if (candle is null)
                        continue;

                    index[candle.DateTime.Ticks] = Fingerprint(candle);
                    pending++;
                }
            }

            PendingJournalCandles = pending;
            Index = index;
        }
    }

    private sealed class FlatFeedFileDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string BarType { get; set; } = string.Empty;
        public int TimeframeMinutes { get; set; }
        public DateTime? BarEnd { get; set; }
        public DateTime? LastUpdate { get; set; }
        public string? Source { get; set; }
        public List<FlatCandleDto> Candles { get; set; } = new();
    }

    private sealed class FlatCandleDto
    {
        public long Timestamp { get; set; }
        public DateTime DateTime { get; set; }
        public string DateTimeFormatted { get; set; } = string.Empty;
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }

    private sealed class ClockManifestDto
    {
        [JsonPropertyName("_nota")]
        public string[]? Nota { get; set; }

        public Dictionary<string, string>? Orologi { get; set; }
    }

    private sealed class TickStateDto
    {
        public DateTime? LastTickUtc { get; set; }
        public int LastFingerprint { get; set; }
        public string? Source { get; set; }
    }
}
