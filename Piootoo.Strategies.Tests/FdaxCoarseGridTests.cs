using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Griglia grossa su <b>FDAX a 4 ore sul periodo lungo</b> (2014-07 → 2026-09).
///
/// <para><b>E' la cella che conta di piu', perche' e' l'unica che ha prodotto una strategia.</b>
/// <c>PT3B_FDAX_PCH_002_240</c> viene da qui, ma da una ricerca su <i>quattro</i> anni: campione
/// 2022-2025 e validazione 2025-2026. Questa griglia guarda gli stessi parametri su <b>dodici</b>,
/// con campione 2014-2021. Se il motore nudo su FDAX risulta sotto le soglie del metodo come su GC,
/// NQ e CL, allora la 002 e' una configurazione fortunata dentro una cella che non ha edge, e va
/// trattata come tale; se invece FDAX e' l'unica delle quattro a reggere, la 002 ha una spiegazione
/// e non solo dei numeri.</para>
///
/// <para><b>Veicolo <c>PT3B_FDAX_PCH_001_240</c>, che non e' un contenitore</b>: e' una strategia
/// vera, ma la griglia le sovrascrive ogni leva, e cio' che eredita — etichetta della barra
/// sull'apertura, fuso della finestra, tick — e' esattamente quello che serve a questa cella. E' la
/// stessa cosa che la griglia GC fa con <c>PTS_GC_PCH_004_240</c>.</para>
///
/// <para><b>Il denaro.</b> Un contratto FDAX e' 25 euro per punto. Il range medio della barra da 4 ore
/// e' 71 punti nel campione (1.775 per contratto) e 97 fuori (2.434), quindi la soglia del 15% passa
/// da 266 a 365: la volatilita' del DAX e' cresciuta molto meno di quella di oro e NQ, ed e' la
/// ragione per cui qui il confronto fra i due periodi e' piu' pulito che altrove.</para>
/// </summary>
public sealed class FdaxCoarseGridTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", ResearchStudy.Category)]
    public async Task CoarseGridOnFdaxOverTheLongPeriodMeasuredInAndOutOfSample()
    {
        var spec = new CoarseGridSpec(
            StrategyId: "PT3B_FDAX_PCH_001_240",
            Symbol: "@FDAX",
            TimeframeMinutes: 240,
            FeedBroker: "ICS",
            SpreadBrokers: ["ICS"],
            SwapBrokers: ["ICS"],
            // ICS stampa 38,46 di round turn su DE40, quindi 19,23 per lato.
            CommissionPerSide: 19.23m,
            StartUtc: new DateTime(2014, 7, 18, 0, 0, 0, DateTimeKind.Utc),
            SplitUtc: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Channels: [1, 20, 50],
            // Attorno ai 5.000 della 002 (200 punti), da un quinto al doppio.
            Stops: [1000, 2500, 5000, 8000, 12000],
            Targets: [0, 2500, 4500, 9000, 18000],
            // L'uscita alle 21 e' la leva che ha prodotto la 002: qui si misura se regge su dodici
            // anni o se valeva solo sul periodo in cui e' stata scelta.
            ExitHours: [-1, 21],
            Directions: [0, 1, 2],
            CsvName: "fdax-4h-griglia-grossa-lunga.csv");

        await CoarseGridStudy.RunAsync(spec, output);
    }
}
