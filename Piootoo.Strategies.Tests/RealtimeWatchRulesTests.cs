using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models.Trading;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le regole del presidio realtime. Sono la parte che decide se qualcuno deve alzarsi e aprire
/// cTrader, quindi vanno verificate sui due errori opposti: tacere su una posizione scoperta, e
/// gridare su una situazione normale — il secondo è quello che rende inutile il primo, perché un
/// presidio che segnala sempre non lo guarda più nessuno.
///
/// <para>Vedi <c>docs/domini/riavvio-del-server-e-ripresa-sessione.md</c> §8.</para>
/// </summary>
public class RealtimeWatchRulesTests
{
    // Mercoledì 10 gennaio 2024, mercato aperto: fuori da ogni finestra di flat del fine settimana.
    private static readonly DateTime Adesso = new(2024, 1, 10, 15, 00, 0, DateTimeKind.Utc);

    [Fact]
    public void SenzaSessioniMaConUnPiano_ChiedeDiControllareCTrader()
    {
        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [], Adesso);

        var rilievo = Assert.Single(rilievi);
        Assert.Equal(RealtimeWatchFinding.SessioneAssente, rilievo.Finding);
        Assert.Equal(RealtimeWatchSeverity.Intervento, rilievo.Severity);
    }

    /// <summary>
    /// Un conto che nessun piano nomina non ha sessioni per progetto: segnalarlo come anomalia
    /// riempirebbe il presidio di righe rosse per i conti che non operano.
    /// </summary>
    [Fact]
    public void SenzaSessioniESenzaPiani_NonEUnAnomalia()
    {
        var rilievi = RealtimeWatchRules.Evaluate([], [], Adesso);

        var rilievo = Assert.Single(rilievi);
        Assert.Equal(RealtimeWatchFinding.NessunPianoPerIlConto, rilievo.Finding);
        Assert.Equal(RealtimeWatchSeverity.Ok, rilievo.Severity);
    }

    [Fact]
    public void SessioneViva_SenzaAnomalie_NonProduceRilievi()
    {
        var sessione = Sessione(minutiDiSilenzio: 5, posizioni: [Posizione(chiusuraPrevista: Adesso.AddHours(3))]);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        var rilievo = Assert.Single(rilievi);
        Assert.Equal(RealtimeWatchFinding.Presidiata, rilievo.Finding);
        Assert.Equal(RealtimeWatchSeverity.Ok, RealtimeWatchRules.Worst(rilievi));
    }

    [Fact]
    public void ChiusuraATempoPassata_ConPosizioneAncoraAperta_ChiedeIntervento()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            posizioni: [Posizione(chiusuraPrevista: Adesso.AddMinutes(-30))]);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        var rilievo = Assert.Single(rilievi, item =>
            item.Finding == RealtimeWatchFinding.ChiusuraAttesaNonAvvenuta);
        Assert.Equal(RealtimeWatchSeverity.Intervento, rilievo.Severity);
        Assert.Equal("PTS_NQ_TFM_001_60", rilievo.StrategyCode);
    }

    /// <summary>
    /// Il margine esiste perché il flat lo applica il client al proprio tick: senza, ogni chiusura
    /// regolare comparirebbe come rilievo nei secondi fra la deadline e il report.
    /// </summary>
    [Fact]
    public void ChiusuraATempoAppenaPassata_RestaDentroLaTolleranza()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            posizioni: [Posizione(chiusuraPrevista: Adesso.AddSeconds(-30))]);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        Assert.DoesNotContain(rilievi, item =>
            item.Finding == RealtimeWatchFinding.ChiusuraAttesaNonAvvenuta);
    }

    /// <summary>
    /// Senza <c>CloseAtUtc</c> la deadline la mette il conto: un piano che vieta l'overnight ha già
    /// tagliato ieri sera, quindi una posizione aperta stamattina non dovrebbe esistere.
    /// </summary>
    [Fact]
    public void PianoSenzaOvernight_PosizioneOltreIlFlat_ChiedeIntervento()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            posizioni: [Posizione(chiusuraPrevista: null, ingresso: Adesso.AddDays(-1))],
            holding: new AccountHoldingPolicy { AllowOvernight = false, SessionFlatUtcHhmm = 2045 });

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        var rilievo = Assert.Single(rilievi, item => item.Finding == RealtimeWatchFinding.OltreIlFlatDiConto);
        Assert.Equal(RealtimeWatchSeverity.Intervento, rilievo.Severity);
    }

    /// <summary>
    /// La soglia è un multiplo del timeframe più fitto, non un numero di minuti: quattro ore di
    /// silenzio su una sessione di sole strategie a 240 minuti sono la norma, su una a 5 minuti
    /// sono un cBot spento.
    /// </summary>
    [Theory]
    [InlineData(5, 20, true)]
    [InlineData(5, 10, false)]
    [InlineData(240, 300, false)]
    [InlineData(240, 900, true)]
    public void FlussoFermo_SiMisuraSulTimeframePiuFitto(int timeframe, int silenzio, bool atteso)
    {
        var sessione = Sessione(minutiDiSilenzio: silenzio, timeframeMinimo: timeframe);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        Assert.Equal(atteso, rilievi.Any(item => item.Finding == RealtimeWatchFinding.FlussoFermo));
    }

    /// <summary>
    /// Nel fine settimana le barre non arrivano perché non esistono. Segnalarlo trasformerebbe il
    /// presidio in un allarme fisso da venerdì sera a domenica.
    /// </summary>
    [Fact]
    public void FlussoFermo_NonSiSegnalaDentroLaFinestraDiFlatDelFineSettimana()
    {
        // Sabato 13 gennaio 2024: dentro la finestra di flat di default.
        var sabato = new DateTime(2024, 1, 13, 12, 0, 0, DateTimeKind.Utc);
        var sessione = Sessione(minutiDiSilenzio: 2000, timeframeMinimo: 5);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], sabato);

        Assert.DoesNotContain(rilievi, item => item.Finding == RealtimeWatchFinding.FlussoFermo);
    }

    /// <summary>
    /// Il caso che la prima stesura sbagliava: la guardia del fine settimana era condizionata a
    /// <c>AllowOverweek</c>, quindi proprio il conto che tiene il fine settimana — cioè l'unico che
    /// il sabato ha davvero posizioni aperte — si prendeva un <c>FlussoFermo</c> a gravità
    /// Intervento per due giorni di fila, ogni settimana.
    ///
    /// <para>Che il mercato sia chiuso è un fatto del calendario e non dipende da cosa il conto
    /// permette di tenere: le barre non arrivano perché non esistono.</para>
    /// </summary>
    [Fact]
    public void FlussoFermo_TaceNelWeekendAncheQuandoIlContoTieneLOverweek()
    {
        var sabato = new DateTime(2024, 1, 13, 12, 0, 0, DateTimeKind.Utc);
        var sessione = Sessione(
            minutiDiSilenzio: 2000,
            timeframeMinimo: 5,
            posizioni: [Posizione(chiusuraPrevista: null, ingresso: new DateTime(2024, 1, 11, 10, 0, 0, DateTimeKind.Utc))],
            holding: AccountHoldingPolicy.Unrestricted);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], sabato);

        Assert.DoesNotContain(rilievi, item => item.Finding == RealtimeWatchFinding.FlussoFermo);
        // E nemmeno il flat di conto: overweek permesso significa che quella posizione può stare lì.
        Assert.DoesNotContain(rilievi, item => item.Finding == RealtimeWatchFinding.OltreIlFlatDiConto);
    }

    /// <summary>
    /// Riavvio nel fine settimana con posizioni overweek: il rilievo di ripresa resta — le posizioni
    /// vengono comunque da un file e nessuno le ha confermate — ma non può chiedere un intervento,
    /// perché a mercato chiuso un cBot acceso e uno spento producono lo stesso silenzio.
    /// </summary>
    [Fact]
    public void RipresaNelWeekend_RestaUnAvvisoENonUnIntervento()
    {
        var sabato = new DateTime(2024, 1, 13, 12, 0, 0, DateTimeKind.Utc);
        var sessione = Sessione(
            minutiDiSilenzio: 2000,
            posizioni: [Posizione(chiusuraPrevista: null, ingresso: new DateTime(2024, 1, 11, 10, 0, 0, DateTimeKind.Utc))],
            holding: AccountHoldingPolicy.Unrestricted,
            ripresa: sabato.AddMinutes(-5));

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], sabato);

        var rilievo = Assert.Single(rilievi, item =>
            item.Finding == RealtimeWatchFinding.SessioneRipresaSenzaFlusso);
        Assert.Equal(RealtimeWatchSeverity.Attenzione, rilievo.Severity);
        Assert.Contains("mercato chiuso", rilievo.Message);
        Assert.Equal(RealtimeWatchSeverity.Attenzione, RealtimeWatchRules.Worst(rilievi));
    }

    /// <summary>La controprova: a mercato aperto la stessa ripresa chiede un intervento.</summary>
    [Fact]
    public void RipresaAMercatoAperto_ConPosizioni_ChiedeIntervento()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            posizioni: [Posizione(chiusuraPrevista: Adesso.AddHours(3))],
            ripresa: Adesso.AddMinutes(-5));

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        var rilievo = Assert.Single(rilievi, item =>
            item.Finding == RealtimeWatchFinding.SessioneRipresaSenzaFlusso);
        Assert.Equal(RealtimeWatchSeverity.Intervento, rilievo.Severity);
    }

    /// <summary>
    /// La scadenza si misura su <c>ExpiresAtUtc + TimeframeMinutes</c>: quel campo è l'inizio
    /// dell'ultima barra valida, non la sua fine. Confonderle dichiara scaduto un ordine di una
    /// strategia a 60 minuti dopo mezz'ora.
    /// </summary>
    [Fact]
    public void PendingDentroLaPropriaUltimaBarra_NonEScaduto()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            pendenti: [Pendente(scadenza: Adesso.AddMinutes(-30), timeframe: 60)]);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        Assert.DoesNotContain(rilievi, item => item.Finding == RealtimeWatchFinding.PendingScaduto);
    }

    [Fact]
    public void PendingOltreLaPropriaBarra_SenzaReport_ESegnalato()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            pendenti: [Pendente(scadenza: Adesso.AddMinutes(-90), timeframe: 60)]);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        var rilievo = Assert.Single(rilievi, item => item.Finding == RealtimeWatchFinding.PendingScaduto);
        Assert.Equal(RealtimeWatchSeverity.Attenzione, rilievo.Severity);
    }

    /// <summary>
    /// In esecuzione diretta il server non riceve mai lo stato del broker: dichiararlo è il modo di
    /// dire che l'elenco delle posizioni è memoria, non una lettura del conto.
    /// </summary>
    [Fact]
    public void EsecuzioneDiretta_ConPosizioni_DichiaraCheNessunoHaVerificato()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            posizioni: [Posizione(chiusuraPrevista: Adesso.AddHours(3))],
            riceveStatoBroker: false);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        Assert.Contains(rilievi, item => item.Finding == RealtimeWatchFinding.StatoBrokerMaiVerificato);
    }

    /// <summary>
    /// La conferma del broker non è una scadenza: vale anche su una posizione perfettamente in
    /// orario, e la stragrande maggioranza delle posizioni ha un <c>CloseAtUtc</c>. Se il controllo
    /// finisce in coda a quelli di scadenza diventa irraggiungibile proprio nel caso normale.
    /// </summary>
    [Fact]
    public void PosizioneInOrarioMaiVistaDalBroker_ESegnalata()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            posizioni: [Posizione(chiusuraPrevista: Adesso.AddHours(3), confermata: false)],
            riceveStatoBroker: true);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        var rilievo = Assert.Single(rilievi, item =>
            item.Finding == RealtimeWatchFinding.PosizioneMaiConfermata);
        Assert.Equal(RealtimeWatchSeverity.Attenzione, rilievo.Severity);
    }

    [Fact]
    public void SessioneFermaConPosizioni_ChiedeIntervento()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            posizioni: [Posizione(chiusuraPrevista: Adesso.AddHours(3))],
            stato: TradingSessionStatus.Stopped);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        var rilievo = Assert.Single(rilievi, item =>
            item.Finding == RealtimeWatchFinding.SessioneNonInEsecuzione);
        Assert.Equal(RealtimeWatchSeverity.Intervento, rilievo.Severity);
    }

    [Fact]
    public void IRilieviEscanoOrdinatiPerGravita()
    {
        var sessione = Sessione(
            minutiDiSilenzio: 5,
            posizioni: [Posizione(chiusuraPrevista: Adesso.AddMinutes(-30))],
            pendenti: [Pendente(scadenza: Adesso.AddMinutes(-90), timeframe: 60)],
            riceveStatoBroker: false);

        var rilievi = RealtimeWatchRules.Evaluate(["PIANO_A"], [sessione], Adesso);

        Assert.Equal(RealtimeWatchSeverity.Intervento, rilievi[0].Severity);
        Assert.Equal(RealtimeWatchSeverity.Intervento, RealtimeWatchRules.Worst(rilievi));
    }

    // ------------------------------------------------------------------------------- costruttori

    private static RealtimeWatchSession Sessione(
        double minutiDiSilenzio,
        IReadOnlyList<RealtimeWatchPosition>? posizioni = null,
        IReadOnlyList<RealtimeWatchPending>? pendenti = null,
        AccountHoldingPolicy? holding = null,
        TradingSessionStatus stato = TradingSessionStatus.Running,
        int timeframeMinimo = 60,
        bool riceveStatoBroker = true,
        DateTime? ripresa = null) => new()
    {
        SessionId = "sessione-0001",
        PlanCode = "PIANO_A",
        WorkspaceId = "ws",
        Status = stato,
        ExecutionMode = ExecutionMode.ExternalBroker,
        CreatedAtUtc = Adesso.AddDays(-2),
        LastBarUtc = Adesso.AddMinutes(-minutiDiSilenzio),
        LastEvaluatedBarUtc = Adesso.AddMinutes(-minutiDiSilenzio),
        MinTimeframeMinutes = timeframeMinimo,
        MinutiDallUltimaBarra = minutiDiSilenzio,
        Holding = holding ?? AccountHoldingPolicy.Default,
        RiceveStatoBroker = riceveStatoBroker,
        RipresaDaDumpAtUtc = ripresa,
        Posizioni = posizioni ?? [],
        Pendenti = pendenti ?? []
    };

    private static RealtimeWatchPosition Posizione(
        DateTime? chiusuraPrevista, DateTime? ingresso = null, bool confermata = true) => new()
    {
        StrategyCode = "PTS_NQ_TFM_001_60",
        Symbol = "@NQ",
        AccountSymbol = "US100",
        Direction = SignalType.Buy,
        Quantity = 1m,
        EntryPrice = 16500m,
        EntryTimeUtc = ingresso ?? Adesso.AddHours(-2),
        IntentId = "sessione-0001-0000000012",
        CloseAtUtc = chiusuraPrevista,
        BrokerConfermata = confermata
    };

    private static RealtimeWatchPending Pendente(DateTime scadenza, int timeframe) => new()
    {
        IntentId = "sessione-0001-0000000013",
        StrategyCode = "PTS_NQ_TFM_001_60",
        Symbol = "@NQ",
        Side = SignalType.Buy,
        Status = OrderIntentStatus.Pending,
        Price = 16550m,
        Quantity = 1m,
        TimeframeMinutes = timeframe,
        CreatedAtUtc = scadenza.AddMinutes(-timeframe),
        ExpiresAtUtc = scadenza
    };
}
