using Piootoo.Core.Services;
using Piootoo.Shared.Models.Brokers;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il confronto fra la tabella di conversione delle size e cio' che il broker dichiara.
///
/// <para>La regola verificata e' <c>moltiplicatore = valore punto del future / dimensione del lotto
/// del broker</c>: su USTEC, lotto da 1 unita' contro le 20 $/punto di <c>@NQ</c>, fa 20. I numeri
/// usati qui sono quelli veri delle schede ICS del 21/09/2026.</para>
///
/// <para>Quello che questi test difendono davvero e' il <b>non</b> fare: il riconciliatore non deve
/// mai dichiarare verificata una riga su cui non ha abbastanza informazioni, perche' una size
/// sbagliata e' l'errore piu' caro del sistema.</para>
/// </summary>
public sealed class SymbolConversionReconcilerTests
{
    private const string Broker = "ICS";

    private static SymbolInfoArchiveDto Archive(
        string brokerSymbol,
        string? piootooSymbol,
        params (string Key, string Value)[] properties)
    {
        var snapshot = new SymbolInfoSnapshotDto
        {
            TakenUtc = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc),
            LastSeenUtc = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc)
        };

        foreach (var (key, value) in properties)
            snapshot.Properties[key] = value;

        return new SymbolInfoArchiveDto
        {
            Broker = Broker,
            BrokerSymbol = brokerSymbol,
            PiootooSymbol = piootooSymbol,
            Snapshots = [snapshot]
        };
    }

    private static SymbolConversion Table(params AccountSymbolMapping[] mappings) =>
        new()
        {
            Code = "cfd-ctrader-ics",
            Name = "CFD ctrader ICS",
            Mappings = [.. mappings]
        };

    private static AccountSymbolMapping Mapping(
        string symbol,
        string accountSymbol,
        decimal multiplier,
        decimal minimum = 0.1m,
        decimal step = 0.01m) =>
        new()
        {
            Symbol = symbol,
            AccountSymbol = accountSymbol,
            ContractMultiplier = multiplier,
            MinimumQuantity = minimum,
            QuantityStep = step,
            PriceScale = 1m,
            Enabled = true
        };

    /// <summary>Il caso buono: la tabella dice 20, il broker dichiara un lotto da 1 unita', @NQ vale 20 $/punto.</summary>
    [Fact]
    public void AMultiplierThatMatchesTheBrokerSpecIsConfirmed()
    {
        var report = SymbolConversionReconciler.Reconcile(
            Broker,
            Table(Mapping("@NQ", "USTEC", 20m)),
            [Archive("USTEC", "@NQ", ("LotSize", "1"), ("VolumeInUnitsMin", "0.1"), ("VolumeInUnitsStep", "0.01"))]);

        var row = Assert.Single(report.Rows);
        Assert.Empty(row.Findings);
        Assert.Equal(20m, row.MeasuredMultiplier);
        Assert.Equal(1, report.Confirmed);
        Assert.Equal(0, report.Divergent);
    }

    /// <summary>
    /// Il caso per cui il rapporto esiste: qualcuno ha scritto 2 dove va 20, e nessun test del
    /// motore puo' accorgersene — un backtest con size dieci volte piu' piccole sembra solo una
    /// strategia meno redditizia.
    /// </summary>
    [Fact]
    public void AWrongMultiplierIsReportedWithBothNumbers()
    {
        var report = SymbolConversionReconciler.Reconcile(
            Broker,
            Table(Mapping("@NQ", "USTEC", 2m)),
            [Archive("USTEC", "@NQ", ("LotSize", "1"))]);

        var row = Assert.Single(report.Rows);
        Assert.Equal(1, report.Divergent);
        Assert.Contains("la tabella usa 2", row.Findings[0]);
        Assert.Contains("la rilevazione dice 20", row.Findings[0]);
    }

    /// <summary>
    /// Su un lotto grande il moltiplicatore e' una frazione, e li' lo scarto va misurato in
    /// relativo: <c>@BP</c> vale 0,625 e una tolleranza assoluta lo confonderebbe con 0,624.
    /// </summary>
    [Fact]
    public void FractionalMultipliersAreComparedRelatively()
    {
        var confirmed = SymbolConversionReconciler.Reconcile(
            Broker,
            Table(Mapping("@BP", "GBPUSD", 0.625m)),
            [Archive("GBPUSD", "@BP", ("LotSize", "100000"))]);

        Assert.Empty(Assert.Single(confirmed.Rows).Findings);

        var divergent = SymbolConversionReconciler.Reconcile(
            Broker,
            Table(Mapping("@BP", "GBPUSD", 0.624m)),
            [Archive("GBPUSD", "@BP", ("LotSize", "100000"))]);

        Assert.Equal(1, divergent.Divergent);
    }

    /// <summary>
    /// I volumi: l'API li dichiara in UNITA' di sottostante, la tabella in LOTTI. Senza dividere
    /// per il lotto ogni riga su uno strumento con lotto grande sembrerebbe divergente.
    /// </summary>
    [Fact]
    public void VolumesAreComparedInLotsAndNotInUnits()
    {
        var report = SymbolConversionReconciler.Reconcile(
            Broker,
            Table(Mapping("@BP", "GBPUSD", 0.625m, minimum: 0.01m, step: 0.01m)),
            [Archive("GBPUSD", "@BP",
                ("LotSize", "100000"),
                ("VolumeInUnitsMin", "1000"),
                ("VolumeInUnitsStep", "1000"))]);

        var row = Assert.Single(report.Rows);
        Assert.Equal(0.01m, row.MeasuredMinimumQuantity);
        Assert.Equal(0.01m, row.MeasuredQuantityStep);
        Assert.Empty(row.Findings);
    }

    /// <summary>
    /// Senza rilevazione non si conferma e non si smentisce: si dice che manca. Una riga che sparisse
    /// dal rapporto si leggerebbe come "non c'era niente da dire", che e' il contrario.
    /// </summary>
    [Fact]
    public void AMappingWithoutAnyReadingIsUnverifiableAndSaysSo()
    {
        var report = SymbolConversionReconciler.Reconcile(
            Broker,
            Table(Mapping("@NQ", "USTEC", 20m)),
            []);

        var row = Assert.Single(report.Rows);
        Assert.True(row.Unverifiable);
        Assert.Equal(1, report.Unverifiable);
        Assert.Equal(0, report.Confirmed);
        Assert.Contains("nessuna rilevazione", row.Findings[0]);
    }

    /// <summary>
    /// Con <c>PriceScale</c> diverso da 1 il broker quota in un'altra unita' di prezzo e la regola
    /// non vale: meglio dichiararsi incompetenti che restituire un numero che sembra una verifica.
    /// </summary>
    [Fact]
    public void ADifferentPriceScaleMakesTheRowUnverifiable()
    {
        var mapping = Mapping("@NQ", "USTEC", 20m);
        mapping.PriceScale = 100m;

        var report = SymbolConversionReconciler.Reconcile(
            Broker, Table(mapping), [Archive("USTEC", "@NQ", ("LotSize", "1"))]);

        var row = Assert.Single(report.Rows);
        Assert.True(row.Unverifiable);
        Assert.Null(row.MeasuredMultiplier);
        Assert.Contains("PriceScale", row.Findings[0]);
    }

    /// <summary>
    /// Gli strumenti che il broker offre e la tabella non mappa non sono un errore — un conto ne ha
    /// centinaia — ma su un broker nuovo sono l'elenco da cui la tabella si costruisce.
    /// </summary>
    [Fact]
    public void ArchivedSymbolsMissingFromTheTableAreListedApart()
    {
        var report = SymbolConversionReconciler.Reconcile(
            Broker,
            Table(Mapping("@NQ", "USTEC", 20m)),
            [Archive("USTEC", "@NQ", ("LotSize", "1")), Archive("DE40", null, ("LotSize", "1"))]);

        Assert.Equal("DE40", Assert.Single(report.ArchivedButNotMapped));
        Assert.Single(report.Rows);
    }
}
