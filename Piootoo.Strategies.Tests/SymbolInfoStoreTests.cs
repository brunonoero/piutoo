using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Brokers;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'archivio delle specifiche dichiarate da un broker. Le proprieta' difese qui sono quelle che un
/// archivio alimentato da un bot che gira ogni giorno sbaglia: moltiplicare scatti identici, perdere
/// la data in cui qualcosa e' cambiato, e lasciare che un nome di strumento arrivato da fuori esca
/// dalla cartella del repository.
/// </summary>
public sealed class SymbolInfoStoreTests : IDisposable
{
    private const string Broker = "ICS";

    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-symbol-info-tests", Guid.NewGuid().ToString("N"));

    private SymbolInfoStore CreateStore()
    {
        Directory.CreateDirectory(_root);
        return new SymbolInfoStore(new PiootooSettings { SymbolInfoPath = _root });
    }

    private static SymbolInfoIngestRequestDto Request(
        DateTime takenUtc,
        (string Key, string Value)[]? properties = null,
        string brokerSymbol = "USTEC",
        string? piootooSymbol = "@NQ")
    {
        properties ??= [];

        var symbol = new SymbolInfoDto
        {
            BrokerSymbol = brokerSymbol,
            PiootooSymbol = piootooSymbol
        };

        foreach (var (key, value) in properties)
            symbol.Properties[key] = value;

        return new SymbolInfoIngestRequestDto
        {
            Broker = Broker,
            AccountNumber = "2094690",
            BotVersion = "1.0.0",
            TakenUtc = takenUtc,
            Symbols = [symbol]
        };
    }

    /// <summary>
    /// Il caso normale di esercizio: il bot gira tutti i giorni e le specifiche non cambiano mai.
    /// Deve restare UN solo scatto, con il secondo estremo che avanza â€” altrimenti in un anno ci
    /// sarebbero trecento copie identiche e la domanda "quando e' cambiato?" non avrebbe risposta.
    /// </summary>
    [Fact]
    public async Task IdenticalReadingsExtendTheSnapshotInsteadOfAddingOne()
    {
        var store = CreateStore();
        var monday = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);

        await store.IngestAsync(Request(monday, properties: [("SwapLong", "-67.39")]));
        var second = await store.IngestAsync(Request(monday.AddDays(1), properties: [("SwapLong", "-67.39")]));
        var third = await store.IngestAsync(Request(monday.AddDays(2), properties: [("SwapLong", "-67.39")]));

        Assert.False(second.Symbols[0].Changed);
        Assert.Equal(1, third.Symbols[0].SnapshotCount);

        var archive = Assert.Single(store.GetArchives(Broker));
        var snapshot = Assert.Single(archive.Snapshots);
        Assert.Equal(monday, snapshot.TakenUtc);
        Assert.Equal(monday.AddDays(2), snapshot.LastSeenUtc);
    }

    /// <summary>
    /// Il motivo per cui l'archivio esiste: quando il broker muove una tariffa, la data del
    /// cambiamento resta scritta, e il valore di prima non si perde. Su una tariffa di swap questo
    /// e' cio' che distingue un backtest ripetibile da uno che cambia risultato senza spiegazione.
    /// </summary>
    [Fact]
    public async Task AChangedSpecificationOpensANewSnapshotAndKeepsTheOldOne()
    {
        var store = CreateStore();
        var before = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);
        var after = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc);

        await store.IngestAsync(Request(before, properties: [("SwapLong", "-67.39")]));
        var response = await store.IngestAsync(Request(after, properties: [("SwapLong", "-71.20")]));

        Assert.True(response.Symbols[0].Changed);
        Assert.Equal(1, response.Changed);
        Assert.Contains("SwapLong: -67.39 -> -71.20", response.Symbols[0].ChangedProperties);

        var archive = Assert.Single(store.GetArchives(Broker));
        Assert.Equal(2, archive.Snapshots.Count);
        Assert.Equal("-67.39", archive.Snapshots[0].Properties["SwapLong"]);
        Assert.Equal("-71.20", archive.Snapshots[1].Properties["SwapLong"]);
    }

    /// <summary>
    /// Una proprieta' che l'API smette di esporre e' un cambiamento quanto un valore diverso:
    /// tacerla farebbe sembrare stabile un archivio che ha perso un campo, e il campo perso e'
    /// proprio quello che poi si legge come zero.
    /// </summary>
    [Fact]
    public async Task APropertyThatDisappearsIsReportedAsAChange()
    {
        var store = CreateStore();
        var before = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);

        await store.IngestAsync(Request(before, properties: [("SwapLong", "-67.39"), ("Swap3DaysRule", "Friday")]));
        var response = await store.IngestAsync(Request(before.AddDays(1), properties: [("SwapLong", "-67.39")]));

        Assert.True(response.Symbols[0].Changed);
        Assert.Contains("Swap3DaysRule: Friday -> assente", response.Symbols[0].ChangedProperties);
    }

    /// <summary>
    /// Bid, Ask e Spread cambiano a ogni tick e non sono specifiche: se entrassero nel confronto,
    /// ogni rilevazione aprirebbe uno scatto nuovo e l'archivio diventerebbe un feed di prezzi.
    /// </summary>
    [Fact]
    public async Task QuotesDoNotCountAsAChangeAndAreNotArchived()
    {
        var store = CreateStore();
        var first = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);

        await store.IngestAsync(Request(first, properties: [("SwapLong", "-67.39"), ("Bid", "24150.5")]));
        var response = await store.IngestAsync(
            Request(first.AddHours(1), properties: [("SwapLong", "-67.39"), ("Bid", "24210.0")]));

        Assert.False(response.Symbols[0].Changed);

        var archive = Assert.Single(store.GetArchives(Broker));
        Assert.DoesNotContain("Bid", Assert.Single(archive.Snapshots).Properties.Keys);
    }

    /// <summary>
    /// Le specifiche IN VIGORE a una data: e' la lettura che serve a un backtest, che gira su un
    /// periodo passato. Quando la rilevazione e' tutta successiva al periodo â€” il caso normale oggi,
    /// dove si misura nel 2026 e si fa girare il 2023 â€” si usa la prima, ma il chiamante deve
    /// saperlo: e' un'ipotesi, non una misura.
    /// </summary>
    [Fact]
    public async Task SnapshotAtReturnsTheOneInForceAndFlagsAMeasureTakenLater()
    {
        var store = CreateStore();
        var september = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);
        var october = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc);

        await store.IngestAsync(Request(september, properties: [("SwapLong", "-67.39")]));
        await store.IngestAsync(Request(october, properties: [("SwapLong", "-71.20")]));

        var archive = Assert.Single(store.GetArchives(Broker));

        var inForce = store.SnapshotAt(archive, new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc), out var later);
        Assert.False(later);
        Assert.Equal("-67.39", inForce!.Properties["SwapLong"]);

        var backtest2023 = store.SnapshotAt(archive, new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc), out later);
        Assert.True(later);
        Assert.Equal("-67.39", backtest2023!.Properties["SwapLong"]);
    }

    /// <summary>
    /// Il nome dello strumento arriva da un bot, cioe' da fuori: un <c>..</c> dentro un percorso
    /// costruito con Path.Combine scriverebbe fuori dal repository. I punti invece si tengono,
    /// perche' i simboli veri li contengono (<c>US100.cash</c>).
    /// </summary>
    [Fact]
    public void SymbolFileNameKeepsDotsAndRefusesTraversal()
    {
        Assert.Equal("US100.cash", SymbolInfoStore.NormalizeSymbolFileName("US100.cash"));
        Assert.Equal("USTEC", SymbolInfoStore.NormalizeSymbolFileName(" USTEC "));

        // I separatori cadono e i punti di testa e coda si tolgono: cio' che resta non puo' risalire.
        Assert.Equal("etcpasswd", SymbolInfoStore.NormalizeSymbolFileName("../../etc/passwd"));
        Assert.Throws<ArgumentException>(() => SymbolInfoStore.NormalizeSymbolFileName(".."));
        Assert.Throws<ArgumentException>(() => SymbolInfoStore.NormalizeSymbolFileName("   "));
    }

    /// <summary>
    /// Uno strumento sbagliato non deve costare la rilevazione degli altri: in un giro da venti
    /// simboli, uno con un nome inutilizzabile ne farebbe perdere diciannove buoni.
    /// </summary>
    [Fact]
    public async Task OneUnusableSymbolDoesNotLoseTheOthers()
    {
        var store = CreateStore();
        var request = Request(new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc));
        request.Symbols.Insert(0, new SymbolInfoDto { BrokerSymbol = "..", PiootooSymbol = "@BAD" });

        var response = await store.IngestAsync(request);

        Assert.Equal(1, response.Rejected);
        Assert.Equal(1, response.Changed);
        Assert.Single(store.GetArchives(Broker));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}


