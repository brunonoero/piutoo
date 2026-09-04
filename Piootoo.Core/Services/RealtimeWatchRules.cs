using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services;

/// <summary>
/// Le regole del presidio realtime: da uno stato di sessione già raccolto ricava i rilievi, cioè
/// dove il server e cTrader rischiano di non corrispondere più.
///
/// <para><b>Perché stanno qui e non nella console.</b> Sono le stesse domande che si porrà la
/// riconciliazione descritta in <c>docs/domini/riavvio-del-server-e-ripresa-sessione.md</c> §4:
/// scriverle due volte significa vederle divergere. La schermata riceve verdetti già pronti e li
/// impagina.</para>
///
/// <para><b>Funzione pura.</b> Nessun accesso allo stato del servizio, nessun <c>UtcNow</c> letto
/// dentro: l'istante arriva come parametro, così le regole sono verificabili a tavolino su una
/// sessione costruita a mano.</para>
///
/// <para><b>Cosa non fanno.</b> Non affermano mai che una posizione <i>è</i> aperta. Il server non
/// vede cTrader; il massimo che si può dire è che lui la crede aperta e da quanto non lo verifica.
/// Ogni messaggio è scritto in quella forma di proposito.</para>
/// </summary>
public static class RealtimeWatchRules
{
    /// <summary>
    /// Quante volte il timeframe più fitto della sessione può passare senza una barra prima di
    /// chiamarlo flusso fermo. Tre e non uno: una barra in ritardo è normale — il cBot la spedisce
    /// alla chiusura, non all'apertura — e un presidio che grida a ogni ritardo non lo guarda più
    /// nessuno.
    /// </summary>
    public const int MoltiplicatoreFlussoFermo = 3;

    /// <summary>
    /// Tolleranza su una scadenza già passata. Il flat e le uscite a tempo li applica il client al
    /// proprio tick, e fra la deadline e il report di chiusura passa qualche istante: senza questo
    /// margine ogni chiusura regolare comparirebbe come rilievo per qualche secondo.
    /// </summary>
    public static readonly TimeSpan TolleranzaScadenza = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Valuta il conto. <paramref name="piani"/> sono i codici piano che lo nominano: servono a
    /// distinguere "nessuna sessione perché non deve averne" da "nessuna sessione e invece
    /// dovrebbe", che è il caso tipico dopo un riavvio del server.
    /// </summary>
    public static IReadOnlyList<RealtimeWatchItem> Evaluate(
        IReadOnlyList<string> piani,
        IReadOnlyList<RealtimeWatchSession> sessioni,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(piani);
        ArgumentNullException.ThrowIfNull(sessioni);

        var rilievi = new List<RealtimeWatchItem>();

        if (sessioni.Count == 0)
        {
            rilievi.Add(piani.Count == 0
                ? new RealtimeWatchItem
                {
                    Finding = RealtimeWatchFinding.NessunPianoPerIlConto,
                    Severity = RealtimeWatchSeverity.Ok,
                    Message = "Nessun piano di trading nomina questo conto.",
                    Action = string.Empty
                }
                : new RealtimeWatchItem
                {
                    Finding = RealtimeWatchFinding.SessioneAssente,
                    Severity = RealtimeWatchSeverity.Intervento,
                    Message = $"Il conto è nei piani {string.Join(", ", piani)} ma il server non ha " +
                              "nessuna sessione realtime viva per lui. Le sessioni stanno in RAM: " +
                              "un riavvio del server le fa sparire tutte.",
                    Action = "Aprire cTrader e controllare le posizioni con label Piootoo: nessuna " +
                             "di quelle è governata dal server finché il cBot non riapre la sessione."
                });

            return rilievi;
        }

        foreach (var sessione in sessioni)
        {
            ValutaSessione(sessione, nowUtc, rilievi);
        }

        if (rilievi.Count == 0)
        {
            rilievi.Add(new RealtimeWatchItem
            {
                Finding = RealtimeWatchFinding.Presidiata,
                Severity = RealtimeWatchSeverity.Ok,
                Message = $"{sessioni.Count} sessione/i in esecuzione, flusso di barre recente, " +
                          "nessuna posizione oltre la propria scadenza.",
                Action = string.Empty
            });
        }

        return rilievi
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Finding)
            .ThenBy(item => item.StrategyCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>La gravità massima fra i rilievi: il semaforo del conto.</summary>
    public static RealtimeWatchSeverity Worst(IReadOnlyList<RealtimeWatchItem> rilievi) =>
        rilievi.Count == 0 ? RealtimeWatchSeverity.Ok : rilievi.Max(item => item.Severity);

    private static void ValutaSessione(
        RealtimeWatchSession sessione, DateTime nowUtc, List<RealtimeWatchItem> rilievi)
    {
        var conPosizioni = sessione.Posizioni.Count > 0;

        if (sessione.Status != TradingSessionStatus.Running)
        {
            rilievi.Add(new RealtimeWatchItem
            {
                Finding = RealtimeWatchFinding.SessioneNonInEsecuzione,
                Severity = conPosizioni ? RealtimeWatchSeverity.Intervento : RealtimeWatchSeverity.Attenzione,
                SessionId = sessione.SessionId,
                Message = $"Sessione in stato {sessione.Status} con {sessione.Posizioni.Count} " +
                          $"posizione/i aperte per il server: le barre non vengono valutate e " +
                          "nessuna uscita decisa dal server può partire.",
                Action = conPosizioni
                    ? "Far ripartire la sessione dal cBot, oppure chiudere le posizioni a mano su cTrader."
                    : "Verificare se la sessione va fatta ripartire."
            });
        }

        ValutaRipresa(sessione, nowUtc, rilievi);
        ValutaFlusso(sessione, nowUtc, conPosizioni, rilievi);

        if (!sessione.RiceveStatoBroker && conPosizioni)
        {
            rilievi.Add(new RealtimeWatchItem
            {
                Finding = RealtimeWatchFinding.StatoBrokerMaiVerificato,
                Severity = RealtimeWatchSeverity.Attenzione,
                SessionId = sessione.SessionId,
                Message = "Sessione in esecuzione diretta: il client non manda mai al server lo " +
                          "stato del broker, quindi le posizioni qui elencate sono quelle che il " +
                          "server ricorda, mai verificate contro cTrader.",
                Action = "Confrontare a occhio l'elenco con le posizioni aperte su cTrader."
            });
        }

        foreach (var posizione in sessione.Posizioni)
        {
            ValutaPosizione(sessione, posizione, nowUtc, rilievi);
        }

        foreach (var pendente in sessione.Pendenti)
        {
            ValutaPendente(sessione, pendente, nowUtc, rilievi);
        }
    }

    /// <summary>
    /// Una sessione ripresa dal dump è una sessione le cui posizioni nessuno ha ancora confermato:
    /// vengono da un file, non da una lettura del conto. Il rilievo però **scade da solo** appena
    /// arriva una barra successiva alla ripresa, perché da lì in poi il cBot sta parlando e la
    /// sessione è viva come le altre. Senza quella condizione sarebbe una riga permanente, e una
    /// riga permanente in un presidio è rumore che nasconde le altre.
    /// </summary>
    private static void ValutaRipresa(
        RealtimeWatchSession sessione, DateTime nowUtc, List<RealtimeWatchItem> rilievi)
    {
        if (sessione.RipresaDaDumpAtUtc is not { } ripresa) return;
        if (sessione.LastBarUtc is { } ultima && ultima > ripresa) return;

        // A mercato chiuso l'assenza di barre non dice niente su nessuno: un cBot sano e uno spento
        // producono lo stesso silenzio, e nessuna azione è possibile prima della riapertura. Il
        // rilievo resta — le posizioni vengono comunque da un file e nessuno le ha confermate — ma
        // scende ad Attenzione e chiede la cosa giusta, cioè di guardare lunedì.
        var mercatoChiuso = sessione.Holding.WeekEnd.IsInsideWindow(nowUtc);

        rilievi.Add(new RealtimeWatchItem
        {
            Finding = RealtimeWatchFinding.SessioneRipresaSenzaFlusso,
            Severity = sessione.Posizioni.Count > 0 && !mercatoChiuso
                ? RealtimeWatchSeverity.Intervento
                : RealtimeWatchSeverity.Attenzione,
            SessionId = sessione.SessionId,
            Message = $"Sessione ripresa da dump alle {ripresa:yyyy-MM-dd HH:mm} UTC dopo un riavvio " +
                      $"del server, e da allora non è arrivata nessuna barra" +
                      (mercatoChiuso ? " (mercato chiuso: era atteso)" : string.Empty) +
                      $". Le {sessione.Posizioni.Count} posizione/i elencate vengono dal file, non " +
                      "da una lettura del conto.",
            Action = mercatoChiuso
                ? "Alla riapertura verificare che le barre ricomincino ad arrivare: fino ad allora " +
                  "il silenzio non distingue un cBot acceso da uno spento."
                : "Verificare che il cBot sia acceso e stia spingendo le barre; finché tace, " +
                  "il server non valuta nessuna uscita."
        });
    }

    /// <summary>
    /// Il flusso fermo si misura sul timeframe più fitto del portafoglio, non su un numero di
    /// minuti fisso: su una sessione di sole strategie a 240 minuti quattro ore di silenzio sono
    /// la norma, su una a 5 minuti sono un cBot spento.
    ///
    /// <para>Dentro la finestra di flat del fine settimana non si segnala: il mercato è chiuso e le
    /// barre non arrivano perché non esistono, non perché il client è morto.</para>
    /// </summary>
    private static void ValutaFlusso(
        RealtimeWatchSession sessione, DateTime nowUtc, bool conPosizioni, List<RealtimeWatchItem> rilievi)
    {
        // La finestra di fine settimana dice che il MERCATO è chiuso, e quel fatto non dipende da
        // cosa il conto permette di tenere: le barre non arrivano perché non esistono. La guardia
        // non va quindi condizionata a AllowOverweek — un conto che tiene il fine settimana è
        // esattamente quello che ha posizioni aperte il sabato, cioè quello a cui il rilievo
        // uscirebbe con gravità Intervento per due giorni di fila, ogni settimana. Un presidio che
        // suona sempre non lo guarda più nessuno.
        if (sessione.Holding.WeekEnd.IsInsideWindow(nowUtc))
        {
            return;
        }

        if (sessione.MinutiDallUltimaBarra is not { } minuti)
        {
            rilievi.Add(new RealtimeWatchItem
            {
                Finding = RealtimeWatchFinding.FlussoFermo,
                Severity = RealtimeWatchSeverity.Attenzione,
                SessionId = sessione.SessionId,
                Message = "La sessione non ha mai ricevuto una barra chiusa: nessuna strategia è " +
                          "stata valutata da quando è stata aperta.",
                Action = "Verificare che il cBot sia avviato e che i suoi stream corrispondano al piano."
            });
            return;
        }

        var soglia = Math.Max(1, sessione.MinTimeframeMinutes) * MoltiplicatoreFlussoFermo;
        if (minuti <= soglia)
        {
            return;
        }

        rilievi.Add(new RealtimeWatchItem
        {
            Finding = RealtimeWatchFinding.FlussoFermo,
            Severity = conPosizioni ? RealtimeWatchSeverity.Intervento : RealtimeWatchSeverity.Attenzione,
            SessionId = sessione.SessionId,
            Message = $"Ultima barra ricevuta {minuti:0} minuti fa, su un timeframe minimo di " +
                      $"{sessione.MinTimeframeMinutes} minuti (soglia {soglia}). Il server è cieco: " +
                      $"non valuta uscite e non emette intent, e ha {sessione.Posizioni.Count} " +
                      "posizione/i aperte in carico.",
            Action = conPosizioni
                ? "Verificare che il cBot giri; nel frattempo le uscite dipendono solo da SL/TP " +
                  "nativi e dal bot, non dal server."
                : "Verificare che il cBot giri."
        });
    }

    private static void ValutaPosizione(
        RealtimeWatchSession sessione, RealtimeWatchPosition posizione, DateTime nowUtc,
        List<RealtimeWatchItem> rilievi)
    {
        var etichetta = $"{posizione.StrategyCode} su {posizione.Symbol}";
        var suCTrader = string.IsNullOrWhiteSpace(posizione.AccountSymbol)
            ? posizione.Symbol
            : posizione.AccountSymbol;

        // La conferma del broker si valuta per prima e da sola: non è una scadenza, è una domanda
        // sull'esistenza della posizione, e vale anche — anzi soprattutto — su una posizione
        // perfettamente in orario. Metterla in coda ai controlli di scadenza la rendeva
        // irraggiungibile per ogni posizione con un CloseAtUtc, cioè per quasi tutte.
        if (sessione.RiceveStatoBroker && !posizione.BrokerConfermata)
        {
            rilievi.Add(new RealtimeWatchItem
            {
                Finding = RealtimeWatchFinding.PosizioneMaiConfermata,
                Severity = RealtimeWatchSeverity.Attenzione,
                SessionId = sessione.SessionId,
                StrategyCode = posizione.StrategyCode,
                Symbol = posizione.Symbol,
                IntentId = posizione.IntentId,
                Message = $"{etichetta}: il server l'ha aperta su execution report ma non l'ha mai " +
                          "vista negli snapshot di posizione che il cBot manda a ogni poll.",
                Action = $"Se {suCTrader} non risulta aperta su cTrader, il server ne sta tenendo " +
                         "occupato lo slot di strategia per niente."
            });
        }

        // Una scadenza esplicita ha la precedenza sulla policy di conto: CloseAtUtc è già il
        // risultato della gerarchia piano → motore → strategia (HoldingResolver), quindi
        // segnalarla e poi ripetere il flat sarebbe lo stesso rilievo scritto due volte.
        if (posizione.CloseAtUtc is { } chiusura)
        {
            if (chiusura + TolleranzaScadenza < nowUtc)
            {
                rilievi.Add(new RealtimeWatchItem
                {
                    Finding = RealtimeWatchFinding.ChiusuraAttesaNonAvvenuta,
                    Severity = RealtimeWatchSeverity.Intervento,
                    SessionId = sessione.SessionId,
                    StrategyCode = posizione.StrategyCode,
                    Symbol = posizione.Symbol,
                    IntentId = posizione.IntentId,
                    Message = $"{etichetta}: l'uscita a tempo era prevista alle " +
                              $"{chiusura:yyyy-MM-dd HH:mm} UTC e per il server la posizione è " +
                              "ancora aperta. L'uscita a tempo la applica il cBot, non il server: " +
                              "se il bot è stato riavviato può averla persa.",
                    Action = $"Controllare {suCTrader} su cTrader e chiudere a mano se è ancora aperta."
                });
            }

            return;
        }

        var holding = sessione.Holding;

        if (!holding.AllowOvernight)
        {
            var flat = holding.ResolveSessionFlatUtc(posizione.EntryTimeUtc);
            if (flat + TolleranzaScadenza < nowUtc)
            {
                rilievi.Add(new RealtimeWatchItem
                {
                    Finding = RealtimeWatchFinding.OltreIlFlatDiConto,
                    Severity = RealtimeWatchSeverity.Intervento,
                    SessionId = sessione.SessionId,
                    StrategyCode = posizione.StrategyCode,
                    Symbol = posizione.Symbol,
                    IntentId = posizione.IntentId,
                    Message = $"{etichetta}: il piano non permette l'overnight e il flat di " +
                              $"sessione era alle {flat:yyyy-MM-dd HH:mm} UTC, ma per il server la " +
                              "posizione è aperta da " +
                              $"{posizione.EntryTimeUtc:yyyy-MM-dd HH:mm} UTC.",
                    Action = $"Controllare {suCTrader} su cTrader: il conto dovrebbe essere piatto."
                });
                return;
            }
        }

        if (!holding.AllowOverweek && holding.WeekEnd.IsInsideWindow(nowUtc))
        {
            rilievi.Add(new RealtimeWatchItem
            {
                Finding = RealtimeWatchFinding.OltreIlFlatDiConto,
                Severity = RealtimeWatchSeverity.Intervento,
                SessionId = sessione.SessionId,
                StrategyCode = posizione.StrategyCode,
                Symbol = posizione.Symbol,
                IntentId = posizione.IntentId,
                Message = $"{etichetta}: siamo dentro la finestra di flat del fine settimana " +
                          $"({holding.Describe()}) e per il server la posizione è ancora aperta.",
                Action = $"Controllare {suCTrader} su cTrader: il conto dovrebbe essere piatto."
            });
        }
    }

    /// <summary>
    /// La scadenza di un pending si misura su <c>ExpiresAtUtc + TimeframeMinutes</c>, non su
    /// <c>ExpiresAtUtc</c>: quel campo è l'inizio dell'ultima barra su cui l'ordine è valido, non
    /// la sua fine. È la stessa convenzione di <c>docs/domini/orologio-barre-e-fill.md</c>, e
    /// confonderle fa dichiarare scaduto un ordine di una strategia a 60 minuti dopo mezz'ora.
    /// </summary>
    private static void ValutaPendente(
        RealtimeWatchSession sessione, RealtimeWatchPending pendente, DateTime nowUtc,
        List<RealtimeWatchItem> rilievi)
    {
        if (pendente.ExpiresAtUtc is not { } scadenza)
        {
            return;
        }

        var fine = scadenza.AddMinutes(Math.Max(1, pendente.TimeframeMinutes));
        if (fine + TolleranzaScadenza >= nowUtc)
        {
            return;
        }

        rilievi.Add(new RealtimeWatchItem
        {
            Finding = RealtimeWatchFinding.PendingScaduto,
            Severity = RealtimeWatchSeverity.Attenzione,
            SessionId = sessione.SessionId,
            StrategyCode = pendente.StrategyCode,
            Symbol = pendente.Symbol,
            IntentId = pendente.IntentId,
            Message = $"{pendente.StrategyCode} su {pendente.Symbol}: ordine ancora " +
                      $"{pendente.Status} per il server, valido fino alle {fine:yyyy-MM-dd HH:mm} " +
                      "UTC e mai completato da un execution report.",
            Action = "Se l'ordine è ancora a mercato su cTrader va cancellato: al fill aprirebbe " +
                     "una posizione che il server non sorveglia."
        });
    }
}
