using Piootoo.Domain.Repositories;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <b>Il metro dell'aggregazione</b>, portato in C# dallo script
/// <c>piootoo-repository/timeframe-analisys/metro_aggregazione.py</c>: riaggrega dal minuto con il
/// layer e confronta con l'aggregato che il cBot ha già prodotto sullo stesso archivio.
///
/// <para><b>Perché è il test che conta.</b> Tutto il refactor poggia su una sola affermazione:
/// aggregare dal minuto lato server riproduce <i>esattamente</i> la griglia su cui le strategie
/// sono state trovate. Se è vera, il refactor sposta dove si aggrega senza spostare cosa esce; se è
/// falsa, ogni backtest dopo la migrazione parla di barre diverse da ogni backtest prima, e nessun
/// numero lo segnala. È un'affermazione che va misurata, non assunta, e ripetuta a ogni modifica
/// del layer — che è quello che questo test fa.</para>
///
/// <para><b>Si confronta l'interno, non i bordi.</b> Il primo bucket comune è troncato, perché il
/// journal a un minuto comincia più tardi del sessanta minuti da cui l'aggregato è nato; la coda
/// dipende da quando i due stream sono stati compattati l'ultima volta. In mezzo la corrispondenza
/// dev'essere <b>esatta</b> su OHLC e volume, e il test lo pretende.</para>
///
/// <para><b>Dipende dai dati.</b> Se l'archivio non è sul disco il test non fallisce: non c'è nulla
/// da misurare, e un rosso direbbe "il layer è rotto" invece di "mancano i dati". La riga di
/// diagnostica dice quale file mancava.</para>
/// </summary>
public sealed class BarAggregatorMetroTests(ITestOutputHelper output)
{
    private const string Broker = "FTMOPLATFORM";

    /// <summary>
    /// Uno stream per ancoraggio e per ordine di grandezza del timeframe: CC e KC aprono all'01:00
    /// dell'orologio della ricerca, SB pure ed è l'unico con il sabato. Sotto l'ora l'ancoraggio non
    /// conta, ma la griglia sì.
    /// </summary>
    public static TheoryData<string, int> Streams => new()
    {
        { "@CC", 240 },
        { "@CC", 60 },
        { "@CC", 15 },
        { "@CT", 240 },
        { "@SB", 240 },
        { "@FDAX", 240 },
    };

    /// <summary>
    /// Aggregati raccolti che <b>non</b> stanno su una griglia sola, e che quindi non sono
    /// confrontabili con niente — nemmeno con se stessi. Vedi
    /// <see cref="CollectedAggregatesSitOnTheirOwnGrid"/>: e' un elenco di file da rifare, non una
    /// tolleranza del layer.
    /// </summary>
    private static readonly string[] KnownMixedGridFeeds = [];

    [Theory]
    [MemberData(nameof(Streams))]
    public async Task AggregatingFromMinutesReproducesTheCollectedGrid(string symbol, int timeframe)
    {
        var root = Path.Combine(FindSolutionRoot(), "piootoo-repository", "datafeed-external", Broker);

        var minutesFile = Path.Combine(root, $"{symbol}_1.json");
        var referenceFile = Path.Combine(root, $"{symbol}_{timeframe}.json");
        if (!ArchiveIsPresent(minutesFile, referenceFile))
            return;

        var repository = new DataSourceRepository(root);
        var minutes = await repository.LoadAllDataAsync(symbol, "OneMinute");
        var reference = await repository.LoadAllDataAsync(symbol, BarTypeOf(timeframe));

        Assert.NotEmpty(minutes);
        Assert.NotEmpty(reference);

        var mine = new BarAggregator(MarketCalendarRegistry.Current.Get(symbol), timeframe)
            .Aggregate(minutes)
            .ToDictionary(bar => bar.Bar.DateTime, bar => bar.Bar);

        var theirs = reference.ToDictionary(bar => bar.DateTime, bar => bar);

        // L'interno: si scartano il primo bucket comune (troncato a sinistra dall'inizio del
        // journal a un minuto) e l'ultima giornata (dove i due stream possono essere stati
        // compattati in momenti diversi, e l'ultimo bucket e' comunque in formazione).
        var common = mine.Keys.Where(theirs.ContainsKey).OrderBy(key => key).ToArray();
        Assert.True(
            common.Length > 100,
            $"{symbol}/{timeframe}m: solo {common.Length} bucket in comune, troppo pochi per " +
            "misurare qualcosa. Probabilmente i due file non coprono lo stesso periodo.");

        var lastComparable = common[^1].AddDays(-1);
        var interior = common.Skip(1).Where(key => key < lastComparable).ToArray();

        var mismatches = new List<string>();
        foreach (var key in interior)
        {
            var a = mine[key];
            var b = theirs[key];
            if (a.Open == b.Open && a.High == b.High && a.Low == b.Low &&
                a.Close == b.Close && a.Volume == b.Volume)
            {
                continue;
            }

            if (mismatches.Count < 5)
            {
                mismatches.Add(
                    $"{key:yyyy-MM-dd HH:mm}Z  layer O{a.Open} H{a.High} L{a.Low} C{a.Close} V{a.Volume}" +
                    $"  |  file O{b.Open} H{b.High} L{b.Low} C{b.Close} V{b.Volume}");
            }
        }

        Assert.True(
            mismatches.Count == 0,
            $"{symbol}/{timeframe}m: {mismatches.Count} bucket diversi su {interior.Length} " +
            $"confrontati (ancoraggio {MarketCalendarRegistry.Current.Get(symbol).SessionStartHour:00}:00 " +
            $"{MarketCalendarRegistry.Current.Get(symbol).ResearchTimeZone}). Primi:\n" +
            string.Join("\n", mismatches));
    }

    /// <summary>
    /// Nell'interno il layer non deve nemmeno <b>mancare</b> bucket che il file ha, né inventarne.
    /// Un bucket in più o in meno non si vede confrontando gli OHLC dei comuni, ed è il difetto che
    /// un ancoraggio sbagliato produce per primo.
    /// </summary>
    [Theory]
    [MemberData(nameof(Streams))]
    public async Task TheLayerProducesTheSameBucketsAsTheFile(string symbol, int timeframe)
    {
        var root = Path.Combine(FindSolutionRoot(), "piootoo-repository", "datafeed-external", Broker);
        var minutesFile = Path.Combine(root, $"{symbol}_1.json");
        var referenceFile = Path.Combine(root, $"{symbol}_{timeframe}.json");
        if (!ArchiveIsPresent(minutesFile, referenceFile))
            return;

        var repository = new DataSourceRepository(root);
        var minutes = await repository.LoadAllDataAsync(symbol, "OneMinute");
        var reference = await repository.LoadAllDataAsync(symbol, BarTypeOf(timeframe));

        var mine = new BarAggregator(MarketCalendarRegistry.Current.Get(symbol), timeframe)
            .Aggregate(minutes)
            .Select(bar => bar.Bar.DateTime)
            .ToHashSet();

        var theirs = reference.Select(bar => bar.DateTime).ToHashSet();

        var from = mine.Min() > theirs.Min() ? mine.Min() : theirs.Min();
        var to = (mine.Max() < theirs.Max() ? mine.Max() : theirs.Max()).AddDays(-1);

        var onlyMine = mine.Where(key => key > from && key < to && !theirs.Contains(key))
            .OrderBy(key => key).ToArray();
        var onlyTheirs = theirs.Where(key => key > from && key < to && !mine.Contains(key))
            .OrderBy(key => key).ToArray();

        // Barre che il layer produce e il file non ha sono LECITE: l'aggregato raccolto ha buchi
        // dove il suo backfill si e' interrotto, mentre il minuto no. Su @FDAX_240 e' una barra
        // sola, il 01/09/2026. Si riportano ma non si pretende che siano zero.
        if (onlyMine.Length > 0)
        {
            output.WriteLine(
                $"{symbol}/{timeframe}m: il layer produce {onlyMine.Length} bucket che il file " +
                $"non ha ({Preview(onlyMine)}). Sono buchi dell'aggregato raccolto, non del minuto.");
        }

        // Il contrario invece e' un difetto: una barra del riferimento che il layer non sa
        // riprodurre significa che le due griglie non coincidono.
        Assert.True(
            onlyTheirs.Length == 0,
            $"{symbol}/{timeframe}m: {onlyTheirs.Length} barre del file NON sono riproducibili dal " +
            $"minuto fra {from:yyyy-MM-dd} e {to:yyyy-MM-dd} ({Preview(onlyTheirs)}). " +
            "Le due griglie non coincidono.");
    }

    /// <summary>
    /// <b>Ogni barra di un aggregato raccolto deve stare sulla griglia dichiarata dal proprio
    /// simbolo.</b> Il controllo e' banale — l'etichetta di una barra deve coincidere con l'inizio
    /// del bucket che la contiene — ma e' l'unico che vede il difetto peggiore che questo archivio
    /// possa avere: due griglie mescolate nello <b>stesso</b> file.
    ///
    /// <para><b>Come succede.</b> La chiave di deduplica e' l'istante di apertura della barra.
    /// Due raccolte con ancoraggi diversi producono etichette diverse per lo stesso periodo,
    /// quindi non si sovrascrivono: si <i>sommano</i>. Il file che ne esce ha il doppio delle
    /// barre, tutte plausibili, meta' su una griglia che nessuna strategia ha mai visto — e
    /// guardando il file non si distinguono.</para>
    ///
    /// <para><b>Trovato cosi' il 07/09/2026:</b> <c>@KC_240.json</c> conteneva 1.760 barre, cioe'
    /// l'unione <i>esatta</i> dell'ancoraggio 00:00 e dell'ancoraggio 01:00, 880 ciascuno e zero
    /// sovrapposizioni; <c>@KC_1440.json</c> e <c>@CT_1440.json</c> 295 su 590. Gli altri softs con
    /// lo stesso ancoraggio — CC, SB — erano puliti, quindi era una raccolta fatta prima che il cBot
    /// imparasse la tabella §2.4, non un difetto dei simboli.</para>
    ///
    /// <para><b>Rifatti lo stesso giorno</b> dal minuto, che e' il dato autorevole, con
    /// <c>POST api/datafeed-external/rebuild-from-minutes</c>. La lista e' quindi vuota: se torna a
    /// popolarsi, un file e' stato raccolto con l'ancoraggio sbagliato e le sue barre non
    /// corrispondono a nessun run.</para>
    /// </summary>
    [Fact]
    public void CollectedAggregatesSitOnTheirOwnGrid()
    {
        var root = Path.Combine(FindSolutionRoot(), "piootoo-repository", "datafeed-external", Broker);
        if (!Directory.Exists(root))
        {
            output.WriteLine($"METRO NON ESEGUITO: archivio assente in '{root}'.");
            return;
        }

        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(root, "@*_*.json").OrderBy(path => path))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var separator = name.LastIndexOf('_');
            if (separator < 0 || !int.TryParse(name[(separator + 1)..], out var timeframe))
                continue;

            // Il minuto e' il dato di partenza: non ha una griglia da rispettare.
            if (timeframe <= 1 || !SessionGrid.DividesTheDay(timeframe))
                continue;

            var symbol = name[..separator];
            if (!MarketCalendarRegistry.Current.TryGet(symbol, out var calendar))
                continue; // simbolo fuori dal paniere: non c'e' una griglia attesa

            var grid = new SessionGrid(calendar);
            var bars = new DataSourceRepository(root)
                .LoadAllDataAsync(symbol, BarTypeOf(timeframe)).GetAwaiter().GetResult();

            var offGrid = bars.Count(bar => grid.BucketStartUtc(bar.DateTime, timeframe) != bar.DateTime);
            if (offGrid == 0)
                continue;

            offenders.Add($"{Path.GetFileName(file)}");
            output.WriteLine(
                $"{Path.GetFileName(file)}: {offGrid} barre su {bars.Count} NON sono su un confine " +
                $"di bucket dell'ancoraggio {calendar.SessionStartHour:00}:00 {calendar.ResearchTimeZone}.");
        }

        Assert.True(
            offenders.OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(KnownMixedGridFeeds.OrderBy(name => name, StringComparer.Ordinal)),
            $"Aggregati fuori griglia: [{string.Join(", ", offenders)}]. " +
            $"Attesi (difetti noti, da rifare dal minuto): [{string.Join(", ", KnownMixedGridFeeds)}]. " +
            "Se un file e' stato rigenerato va tolto da KnownMixedGridFeeds; se ne compare uno nuovo, " +
            "e' stato raccolto con l'ancoraggio sbagliato e le sue barre non corrispondono a nessun run.");
    }

    /// <summary>
    /// Il metro misura il layer contro dati veri, e senza dati non misura niente. Un rosso qui
    /// direbbe "il layer e' rotto" invece di "mancano i file", quindi l'assenza si dichiara
    /// nell'output del test e non si trasforma in un fallimento. xUnit 2.9 non ha
    /// <c>Assert.Skip</c>: la riga stampata e' l'unico modo per non passare in silenzio.
    /// </summary>
    private bool ArchiveIsPresent(string minutesFile, string referenceFile)
    {
        if (File.Exists(minutesFile) && File.Exists(referenceFile))
            return true;

        output.WriteLine(
            $"METRO NON ESEGUITO: archivio assente. Servono '{minutesFile}' e '{referenceFile}'. " +
            "Il layer non e' stato confrontato con nessun dato reale.");
        return false;
    }

    private static string Preview(IReadOnlyList<DateTime> keys) =>
        keys.Count == 0
            ? "-"
            : string.Join(", ", keys.Take(3).Select(key => key.ToString("yyyy-MM-dd HH:mm")));

    private static string BarTypeOf(int timeframeMinutes) => timeframeMinutes switch
    {
        1 => "OneMinute",
        5 => "FiveMinute",
        15 => "FifteenMinute",
        30 => "ThirtyMinute",
        60 => "OneHour",
        240 => "FourHour",
        1440 => "Daily",
        _ => throw new ArgumentOutOfRangeException(nameof(timeframeMinutes), timeframeMinutes, null)
    };

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                $"PiootooApp.sln non trovata risalendo da {AppContext.BaseDirectory}.");
    }
}
