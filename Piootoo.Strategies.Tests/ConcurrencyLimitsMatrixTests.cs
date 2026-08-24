using System.Collections.Concurrent;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Banco di prova dei limiti di concorrenza: <c>MaxConcurrentTrades</c> (per account) messo a
/// confronto con i tre lucchetti di gruppo descritti in
/// <c>docs/domini/distribuzione-multi-account.md</c> §2.
///
/// <para>Il punto che questi test isolano è che i due meccanismi vivono in passi diversi del claim e
/// non sono intercambiabili: il limite per account è il <b>passo 2</b> e risponde
/// <c>MaxConcurrentTradesExceeded</c> <i>prima</i> di guardare i template, quindi non li consuma; i
/// lucchetti di gruppo sono i passi successivi e rispondono <c>NoSignal</c>. Distinguere le due
/// risposte è l'unico modo per sapere quale vincolo è binding, ed è la diagnosi che il cBot fa in
/// produzione.</para>
///
/// <para>Il masterfilter prende una strategia per ciascuno dei primi simboli distinti del catalogo,
/// e il servizio di valutazione sintetico emette un segnale per la strategia del simbolo della
/// barra: una barra spinta = un template su quel simbolo. Serviva a poter osservare il limite per
/// account senza che il vecchio lucchetto (account, simbolo) lo anticipasse sempre; quel lucchetto
/// non esiste più dall'11/08/2026, ma la fixture multi-simbolo resta perché è anche il modo di
/// verificare che il limite <b>non</b> guardi il simbolo.</para>
/// </summary>
public sealed class ConcurrencyLimitsMatrixTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-limits-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // ------------------------------------------------- il limite per account, in isolamento

    [Fact]
    public void TheAccountLimitBlocksASymbolThatNoLockIsHolding()
    {
        // Due simboli liberi, nessun lucchetto chiuso su quello non ancora operato: l'unica cosa che
        // ferma il secondo ingresso è il limite dell'account.
        var f = New(symbols: 2, [Row("g1", "1001", maxConcurrent: 1)]);

        f.PushBar(0);
        f.PushBar(1);

        var first = f.Poll("1001");
        Assert.NotNull(first.Intent);
        f.Fill(first.Intent!);

        var second = f.Poll("1001");

        Assert.Null(second.Intent);
        Assert.Equal("MaxConcurrentTradesExceeded", second.Reason);
        // La risposta è diagnostica: il cBot deve poter dire quale limite ha morso e con che numeri.
        Assert.Equal(1, second.OpenPositions);
        Assert.Equal(0, second.PendingOrders);
        Assert.Equal(1, second.MaxConcurrentTrades);
    }

    [Fact]
    public void WithoutTheLimit_TheSameAccountRunsTwoSymbolsInParallel()
    {
        // Stessa sequenza con max = 0 (illimitato): niente tetto, quindi il secondo ingresso passa.
        // È la controprova del test precedente.
        var f = New(symbols: 2, [Row("g1", "1001", maxConcurrent: 0)]);

        f.PushBar(0);
        f.PushBar(1);

        var first = f.Poll("1001");
        Assert.NotNull(first.Intent);
        f.Fill(first.Intent!);

        var second = f.Poll("1001");

        Assert.NotNull(second.Intent);
        Assert.NotEqual(first.Intent!.Symbol, second.Intent!.Symbol);
    }

    [Fact]
    public void WithoutABrokerSnapshot_TheServerCountsItsOwnPositions()
    {
        // GetNextSignalForAccount senza dati del broker ricade su CountServerPositionsForAccount. È il
        // percorso dei test e dei client che non inviano lo stato: deve dare lo stesso esito.
        var f = New(symbols: 2, [Row("g1", "1001", maxConcurrent: 1)]);

        f.PushBar(0);
        f.PushBar(1);
        f.Fill(f.Poll("1001").Intent!);

        var blocked = f.Poll("1001");

        Assert.Equal("MaxConcurrentTradesExceeded", blocked.Reason);
        Assert.Equal(1, blocked.OpenPositions);
    }

    [Fact]
    public void TheBrokerSnapshotWins_AndPendingOrdersCountToo()
    {
        // Il server non sa nulla di ordini pendenti presso il broker: se il cBot li dichiara, entrano
        // nel conteggio insieme alle posizioni. Con max = 2 basta 1 + 1 per saturare.
        var f = New(symbols: 2, [Row("g1", "1001", maxConcurrent: 2)]);

        f.PushBar(0);
        f.PushBar(1);

        var blocked = f.PollWithBroker("1001", positions: 1, orders: 1);

        Assert.Null(blocked.Intent);
        Assert.Equal("MaxConcurrentTradesExceeded", blocked.Reason);
        Assert.Equal(1, blocked.OpenPositions);
        Assert.Equal(1, blocked.PendingOrders);

        // Un solo elemento dichiarato sta sotto il limite: il claim passa.
        var granted = f.PollWithBroker("1001", positions: 1, orders: 0);
        Assert.NotNull(granted.Intent);
    }

    // ------------------------------------------- limite per account vs lucchetti di gruppo

    [Fact]
    public void TheLimitIsPerAccount_TheSiblingOfTheSameGroupKeepsWorking()
    {
        // Il limite non è di gruppo: un account saturo non ferma il fratello. Se fosse di gruppo
        // questo test fallirebbe, ed è l'errore di lettura più frequente della tabella.
        var f = New(symbols: 2,
        [
            Row("g1", "1001", maxConcurrent: 1),
            Row("g1", "1002", maxConcurrent: 1)
        ]);

        f.PushBar(0);
        f.PushBar(1);

        var first = f.Poll("1001");
        f.Fill(first.Intent!);
        Assert.Equal("MaxConcurrentTradesExceeded", f.Poll("1001").Reason);

        var sibling = f.Poll("1002");

        Assert.NotNull(sibling.Intent);
        // Il template rifiutato per limite non era stato consumato: il passo 2 precede la selezione.
        Assert.NotEqual(first.Intent!.Symbol, sibling.Intent!.Symbol);
    }

    [Fact]
    public void TheGroupSlotBlocksEvenWhenTheAccountHasCapacity()
    {
        // Due template della stessa strategia/simbolo su barre successive, account con limite largo:
        // il secondo account del gruppo non prende nulla, e la ragione è NoSignal (lucchetto 4), non
        // MaxConcurrentTradesExceeded. La ragione distingue i due vincoli.
        var f = New(symbols: 1,
        [
            Row("g1", "1001", maxConcurrent: 10),
            Row("g1", "1002", maxConcurrent: 10)
        ]);

        f.PushBar(0);
        var claimed = f.Poll("1001").Intent;
        Assert.NotNull(claimed);
        f.Fill(claimed!);

        f.PushBar(0);                       // secondo template, stessa strategia e stesso simbolo

        var sibling = f.Poll("1002");

        Assert.Null(sibling.Intent);
        Assert.Equal("NoSignal", sibling.Reason);
    }

    [Fact]
    public void TheLimitIsNotSharedBetweenGroups()
    {
        // Fan-out: un account saturo in g1 non toglie nulla a g2, che riceve lo stesso template.
        var f = New(symbols: 1,
        [
            Row("g1", "1001", maxConcurrent: 1),
            Row("g2", "2001", maxConcurrent: 1)
        ]);

        f.PushBar(0);
        var first = f.Poll("1001");
        f.Fill(first.Intent!);
        Assert.Equal("MaxConcurrentTradesExceeded", f.Poll("1001").Reason);

        var other = f.Poll("2001");

        Assert.NotNull(other.Intent);
        Assert.Equal(first.Intent!.StrategyCode, other.Intent!.StrategyCode);
        Assert.NotEqual(first.Intent.IntentId, other.Intent.IntentId);
    }

    [Fact]
    public void ClosingThePosition_ReleasesBothTheLimitAndTheGroupSlot()
    {
        // Il ciclo completo: il limite si libera perché la posizione sparisce, il lucchetto 4/5 perché
        // ApplyReport di una chiusura li rimuove esplicitamente. Prima della chiusura nessuno dei due
        // è aperto, quindi un solo test copre entrambi i rilasci.
        var f = New(symbols: 1, [Row("g1", "1001", maxConcurrent: 1)]);

        f.PushBar(0);
        var entry = f.Poll("1001").Intent!;
        f.Fill(entry);
        f.PushBar(0);                       // template successivo sulla stessa strategia/simbolo
        Assert.Equal("MaxConcurrentTradesExceeded", f.Poll("1001").Reason);

        var close = f.Sessions.CreateExternalCloseIntent(f.Descriptor.SessionId, new CreateExternalCloseIntentRequest
        {
            SessionToken = f.Descriptor.SessionToken,
            StrategyCode = entry.StrategyCode,
            Symbol = entry.Symbol,
            AccountNumber = "1001"
        });

        // Finché la chiusura è pendente il poll ripropone quella, non un nuovo ingresso (passo 1).
        Assert.Equal(close.IntentId, f.Poll("1001").Intent!.IntentId);

        f.Fill(close);

        var afterClose = f.Poll("1001");
        Assert.NotNull(afterClose.Intent);
        Assert.Equal(OrderIntentKind.Entry, afterClose.Intent!.Kind);
    }

    [Fact]
    public void ARejectionFreesTheLimitImmediately_WithoutWaitingForAClose()
    {
        // Un ingresso rifiutato non apre posizione: né il conteggio del limite né i lucchetti devono
        // trattenerlo. Il template però resta consumato dal gruppo, quindi serve un secondo template.
        var f = New(symbols: 2, [Row("g1", "1001", maxConcurrent: 1)]);

        f.PushBar(0);
        f.PushBar(1);

        var first = f.Poll("1001").Intent!;
        f.Report(first, ExecutionReportStatus.Rejected, filled: 0m);

        var second = f.Poll("1001");

        Assert.NotNull(second.Intent);
        Assert.NotEqual(first.IntentId, second.Intent!.IntentId);
    }

    [Fact]
    public void AClaimNotYetPlacedStillCountsAgainstTheLimit()
    {
        // Un claim consegnato e non ancora comparso sul broker conta eccome: senza, un drenaggio
        // veloce sfonderebbe il tetto prima che il broker registri il primo ordine. A tetto pieno il
        // poll ripropone l'ingresso pendente invece di rispondere MaxConcurrentTradesExceeded — è
        // così che si recupera un claim la cui risposta si è persa in rete.
        var f = New(symbols: 2, [Row("g1", "1001", maxConcurrent: 1)]);

        f.PushBar(0);
        f.PushBar(1);

        var first = f.Poll("1001").Intent!;
        var again = f.Poll("1001");

        Assert.NotNull(again.Intent);
        Assert.Equal(first.IntentId, again.Intent!.IntentId);
        Assert.Null(again.Reason);
    }

    // ------------------------------------------------------------------ stress concorrente

    [Fact]
    public async Task ParallelPolls_NeverAssignTheSameTemplateTwiceInsideAGroup()
    {
        // Sei account dello stesso gruppo che pollano insieme su sei template di simboli diversi.
        // Invariante: ogni template è consumato una volta sola dal gruppo, quindi i sei account si
        // dividono i sei template uno a testa, senza duplicati né eccezioni.
        const int count = 6;
        var accounts = Enumerable.Range(1, count).Select(i => $"100{i}").ToArray();
        var f = New(symbols: count, accounts.Select(a => Row("g1", a, maxConcurrent: 0)).ToArray());

        for (var i = 0; i < count; i++) f.PushBar(i);

        var claims = new ConcurrentBag<OrderIntent>();
        var errors = new ConcurrentBag<Exception>();

        await Parallel.ForEachAsync(accounts, async (account, _) =>
        {
            await Task.Yield();
            for (var attempt = 0; attempt < 50; attempt++)
            {
                try
                {
                    var response = f.Poll(account);
                    if (response.Intent is null) continue;
                    claims.Add(response.Intent);
                    return;
                }
                catch (Exception ex) { errors.Add(ex); return; }
            }
        });

        Assert.Empty(errors);
        Assert.Equal(count, claims.Count);
        Assert.Equal(count, claims.Select(x => x.IntentId).Distinct().Count());
        // Un template per strategia: se due account avessero preso lo stesso, qui ci sarebbe un duplicato.
        Assert.Equal(count, claims.Select(x => x.StrategyCode).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(count, claims.Select(x => x.AssignedAccountNumber).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task ParallelPollsOfTheSameAccount_ProduceExactlyOneClaim()
    {
        // Il passo 1 legge e poi scrive: se non fosse dentro il lock della sessione, due poll
        // simultanei dello stesso account potrebbero reclamare due template diversi.
        var f = New(symbols: 4, [Row("g1", "1001", maxConcurrent: 0)]);
        for (var i = 0; i < 4; i++) f.PushBar(i);

        var responses = new ConcurrentBag<OrderIntent>();
        var errors = new ConcurrentBag<Exception>();

        await Parallel.ForEachAsync(Enumerable.Range(0, 32), async (_, _) =>
        {
            await Task.Yield();
            try
            {
                var response = f.Poll("1001");
                if (response.Intent is not null) responses.Add(response.Intent);
            }
            catch (Exception ex) { errors.Add(ex); }
        });

        Assert.Empty(errors);
        Assert.NotEmpty(responses);
        Assert.Single(responses.Select(x => x.IntentId).Distinct());
        // E in sessione esiste davvero un solo intent di ingresso: nessun claim orfano.
        var entries = f.Sessions
            .GetIntents(f.Descriptor.SessionId, f.Descriptor.SessionToken)
            .Where(x => x.Kind == OrderIntentKind.Entry)
            .ToArray();
        Assert.Single(entries);
    }

    [Fact]
    public async Task ParallelPollsAgainstTheLimit_NeverExceedIt()
    {
        // Molti account con limite 1 che pollano insieme mentre arrivano fill: il numero di posizioni
        // aperte per account non deve mai superare il limite dichiarato.
        const int accounts = 4;
        var rows = Enumerable.Range(1, accounts).Select(i => Row("g1", $"100{i}", maxConcurrent: 1)).ToArray();
        var f = New(symbols: 4, rows);
        for (var i = 0; i < 4; i++) f.PushBar(i);

        var errors = new ConcurrentBag<Exception>();
        var exceeded = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(rows.Select(r => r.AccountNumber), async (account, _) =>
        {
            await Task.Yield();
            for (var attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    var response = f.Poll(account);
                    if (response.Intent is { Kind: OrderIntentKind.Entry } intent &&
                        intent.Status == OrderIntentStatus.Pending)
                        f.Fill(intent);
                    if (response.Reason == "MaxConcurrentTradesExceeded") exceeded.Add(account);
                }
                catch (Exception ex) { errors.Add(ex); return; }
            }
        });

        Assert.Empty(errors);
        // Ogni account ha al massimo una posizione aperta, cioè il proprio limite.
        var positions = f.Sessions.GetSnapshot(f.Descriptor.SessionId, f.Descriptor.SessionToken).Positions
            .GroupBy(p => p.AccountNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        Assert.All(positions.Values, x => Assert.Equal(1, x));
        // E il limite si è fatto sentire: se nessuno lo avesse visto il test non starebbe misurando nulla.
        Assert.NotEmpty(exceeded);
    }

    [Fact]
    public async Task PushingBarsWhilePolling_KeepsTheGroupLocksConsistent()
    {
        // Scrittori e lettori insieme: PushBars crea template mentre due gruppi pollano. Invariante
        // strutturale: dentro un gruppo non esistono due claim pendenti sulla stessa
        // strategia/simbolo, che è esattamente ciò che il lucchetto 4 garantisce.
        var f = New(symbols: 1,
        [
            Row("g1", "1001", maxConcurrent: 0),
            Row("g1", "1002", maxConcurrent: 0),
            Row("g2", "2001", maxConcurrent: 0),
            Row("g2", "2002", maxConcurrent: 0)
        ]);

        var errors = new ConcurrentBag<Exception>();
        using var stop = new CancellationTokenSource();

        // I lettori partono per primi e sono materializzati subito: se nascessero dopo l'ultimo push
        // il test non osserverebbe nessuna sovrapposizione e passerebbe senza aver misurato niente.
        var readers = new[] { "1001", "1002", "2001", "2002" }.Select(account => Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                try { f.Poll(account); }
                catch (Exception ex) { errors.Add(ex); return; }
                Thread.Yield();
            }
        })).ToArray();

        var writer = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 40; i++) f.PushBar(0);
            }
            catch (Exception ex) { errors.Add(ex); }
            finally { stop.Cancel(); }
        });

        await Task.WhenAll(readers.Append(writer));

        Assert.Empty(errors);

        var pending = f.Sessions
            .GetIntents(f.Descriptor.SessionId, f.Descriptor.SessionToken)
            .Where(x => x.Kind == OrderIntentKind.Entry && x.Status == OrderIntentStatus.Pending)
            .ToArray();

        // Un solo claim per gruppo: g1 e g2 hanno due account ciascuno ma un solo slot
        // (strategia, simbolo) disponibile.
        Assert.All(
            pending.GroupBy(x => Group(x.AssignedAccountNumber!)),
            group => Assert.Single(group));
        Assert.True(pending.Length <= 2, $"claim pendenti attesi al massimo 2, trovati {pending.Length}");
    }

    private static string Group(string accountNumber) => accountNumber.StartsWith('1') ? "g1" : "g2";

    // ------------------------------------------------------------------------------ helper

    private static TradingGroupRow Row(string groupId, string account, int maxConcurrent) => new()
    {
        GroupId = groupId, AccountNumber = account,
        MaxConcurrentTrades = maxConcurrent, ApplyTitanoFilters = false
    };

    private Fixture New(int symbols, IReadOnlyList<TradingGroupRow> groups)
    {
        // Una strategia per simbolo distinto. Serviva a poter osservare il limite per account senza
        // che il vecchio lucchetto (account, simbolo) lo anticipasse; ora serve a verificare che il
        // limite conti sull'insieme e non per simbolo.
        var selected = StrategyFactory.GetRegisteredStrategies()
            .GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(symbols)
            .ToArray();
        Assert.Equal(symbols, selected.Length);

        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"limits-{Guid.NewGuid():N}",
            StrategiesFilter = selected.Select(x => x.Id).ToList()
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        TestAccountRegistry.Register(workspaces, groups);

        var sessions = new TradingSessionService(
            workspaces, new OneSignalPerBarEvaluationService(),
            new TitanoRotationService(workspaces), new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            TitanoMode = TitanoFilterMode.Disabled,
            EnforceConcurrencyLimits = true
        });
        sessions.SetTradingGroups(descriptor.SessionId, descriptor.SessionToken, groups);
        sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);

        return new Fixture(sessions, descriptor, selected);
    }

    private sealed class Fixture(
        TradingSessionService sessions,
        TradingSessionDescriptor descriptor,
        IReadOnlyList<StrategyDefinition> strategies)
    {
        private static readonly DateTime Origin = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        private int _bar;

        public TradingSessionService Sessions { get; } = sessions;
        public TradingSessionDescriptor Descriptor { get; } = descriptor;

        /// <summary>Spinge una barra sul simbolo indicato: produce esattamente un template.</summary>
        public void PushBar(int symbolIndex)
        {
            var strategy = strategies[symbolIndex];
            var barTime = Origin.AddMinutes(Interlocked.Increment(ref _bar) * 60);
            Sessions.PushBars(new PushBarsRequest
            {
                SessionId = Descriptor.SessionId,
                SessionToken = Descriptor.SessionToken,
                Bars =
                [
                    new ClosedBar
                    {
                        Symbol = strategy.Symbol,
                        TimeframeMinutes = strategy.TimeframeMinutes,
                        BarTimeUtc = barTime,
                        Sequence = barTime.Ticks,
                        IdempotencyKey = $"bar-{strategy.Symbol}-{barTime:O}",
                        Bar = new OhlcvData
                        {
                            DateTime = barTime, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1
                        }
                    }
                ]
            });
        }

        public AccountSignalResponse Poll(string account) =>
            Sessions.GetNextSignalForAccount(Descriptor.SessionId, Descriptor.SessionToken, account);

        public AccountSignalResponse PollWithBroker(string account, int positions, int orders) =>
            Sessions.PollSignalForAccount(Descriptor.SessionId, account, new AccountSignalPollRequest
            {
                SessionToken = Descriptor.SessionToken,
                Positions = Enumerable.Range(0, positions)
                    .Select(i => new BrokerPositionSnapshot { PositionId = $"p{i}" }).ToArray(),
                Orders = Enumerable.Range(0, orders)
                    .Select(i => new BrokerOrderSnapshot { OrderId = $"o{i}" }).ToArray()
            });

        public void Fill(OrderIntent intent) =>
            Report(intent, ExecutionReportStatus.Filled, intent.Quantity);

        public void Report(OrderIntent intent, ExecutionReportStatus status, decimal filled) =>
            Sessions.ApplyReport(Descriptor.SessionId, new ExecutionReportRequest
            {
                SessionToken = Descriptor.SessionToken,
                Report = new ExternalExecutionReport
                {
                    ReportId = $"r-{Guid.NewGuid():N}",
                    IntentId = intent.IntentId,
                    Status = status,
                    CumulativeFilledQuantity = filled,
                    FillPrice = 100m,
                    EventTimeUtc = Origin
                }
            });
    }

    /// <summary>
    /// Un segnale per barra, per la strategia del simbolo della barra. Il codice è quello reale del
    /// catalogo, così i lucchetti di gruppo lavorano su chiavi distinte simbolo per simbolo.
    /// </summary>
    private sealed class OneSignalPerBarEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault(x =>
                string.Equals(x.Symbol, closedBar.Symbol, StringComparison.OrdinalIgnoreCase));
            if (strategy is null) return [];

            return
            [
                new TradeSignal
                {
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Symbol = strategy.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy,
                    Quantity = 4m,
                    Price = closedBar.Bar.Close
                }
            ];
        }
    }
}
