# Orari di sessione, fusi orari e confini di giornata

Ogni strategia portata da EasyLanguage dichiara la propria sessione come due
numeri in formato `HHMM`, `SessionStartTime` e `SessionEndTime`. Da quei due
numeri discende quasi tutto ciò che la strategia guarda: gli OHLC di sessione
`d0..d5` di `OHLCMulti5`, la serie giornaliera di `BuildSessionSeries`, i
`highd1`/`lowd1` che gli engine Unger usano come livelli di breakout, il
conteggio dei giorni in posizione, i time exit, il latch di fine sessione. Se il
confine cade nel posto sbagliato non sbaglia un dettaglio: sbaglia il livello
che la strategia traderà.

Questo documento spiega in che orologio vanno letti quei numeri, come si
verifica l'orologio di un feed, e cosa controllare quando si aggiunge una
strategia o un simbolo nuovo.

## I numeri delle sorgenti sono in ora di borsa, uno per strumento

Non sono in UTC, e non sono nemmeno tutti nello stesso fuso: ogni sorgente usa
l'ora locale della borsa del proprio strumento. Le coppie dichiarate lo
mostrano senza margini di dubbio, perché ciascuna riproduce esattamente
l'orario reale della sua borsa in quel fuso.

| simbolo | start → end | corrisponde a |
|---|---|---|
| NQ, ES | `1700` → `1600` / `1559` | CME indici, ora di Chicago |
| EC, BP | `1700` → `1600` | valute CME, ora di Chicago |
| GC, CL, PL, HG | `1800` → `1700` / `1659` | COMEX e NYMEX, ora di New York |
| FDAX, FGBL | `0800` → `2200` | Eurex, ora di Francoforte |
| TSLA | `0930` → `1600` | Nasdaq, ora di New York |
| FC, LC | `0830` → `1305` | CME bestiame, ora di Chicago |

La prova decisiva è Feeder Cattle: `1305` è l'orario di chiusura del bestiame
al CME in ora di Chicago, un valore troppo specifico per essere una
coincidenza. E nessun fuso unico può spiegare insieme le `0800` di FDAX e le
`0930` di TSLA.

Va notato che NQ e GC descrivono lo **stesso istante** con numeri diversi:
17:00 a Chicago e 18:00 a New York sono la stessa riapertura Globex. È il
motivo per cui il registro conserva il fuso invece di riscrivere gli orari in
una convenzione comune — riscriverli vorrebbe dire reinterpretare a mano più di
cinquanta sorgenti, ed è esattamente il tipo di traduzione in cui si perde
fedeltà senza accorgersene.

## Perché un orario di sessione in UTC non può funzionare

È la soluzione che verrebbe voglia di adottare, perché toglierebbe ogni
equivoco: un solo orologio dappertutto. Non è esatta, e la ragione è l'ora
legale.

La riapertura di NQ è le 17:00 di Chicago tutto l'anno, ma in UTC cade alle
23:00 in ora solare e alle 22:00 in ora legale. Misurando sul feed riportato a
UTC vero dove cade la pausa giornaliera, le finestre ammesse per collocare il
confine risultano **adiacenti e disgiunte**: da 21:00 a 22:00 quando a Chicago
è ora legale (734 giorni su 740 osservati) e da 22:00 a 23:00 quando è ora
solare (401 su 402). L'intersezione è vuota, quindi nessun valore UTC fisso è
corretto tutto l'anno.

Il costo di sceglierne uno comunque non è di un'ora di dati ma di **una barra
per sessione**: con `2200` UTC, d'estate la sessione perde la propria barra di
apertura e d'inverno la precedente perde quella di chiusura. Poco rispetto
all'errore che si aveva prima, ma non zero — e `closed1` entra nei pattern
`PtnNeut`, quindi si sente.

La forma esatta è dichiarare la finestra come dato, `(fuso IANA, orario
locale)`, e convertire l'istante della barra solo al momento del confronto. Non
è un ritorno all'ambiguità, è il contrario: `1700` da solo è un numero di cui
nessuno sa il fuso, mentre `(America/Chicago, 1700)` si spiega da sé.

## Come si stabilisce in che orologio è un feed

Un CSV storico non dichiara quasi mai il proprio fuso, e l'etichetta `Z` nei
JSON del feed non è una garanzia: dice solo che qualcuno ha passato `UTC` allo
script di aggregazione. Due misure indipendenti lo determinano dai dati, e
conviene farle entrambe perché si controllano a vicenda.

La prima è il **picco di volume**. Su un indice americano il volume ha un
massimo netto all'apertura del cash di New York, le 09:30 locali. Si sommano i
volumi per slot orario su qualche centinaio di giorni e si guarda dove cade il
massimo. Sul feed @NQ cade sullo slot 15:30, con l'11,4% del volume totale su
400 giorni, quasi il doppio del secondo slot: sei ore avanti rispetto a New
York, cioè ora europea.

La seconda è la **pausa di manutenzione**. I future CME hanno un'interruzione
giornaliera di un'ora, 16:00–17:00 di Chicago per gli indici. Si cerca il buco
fra due barre consecutive e si guarda a che ora cade. Sul feed @NQ le barre
finiscono alle 23:00 e riprendono alle 00:00: sette ore avanti rispetto a
Chicago, di nuovo ora europea. Le due misure concordano.

Questo vale anche come procedura di collaudo per un feed nuovo: se le due
misure non concordano, o se il picco di volume non è dove dovrebbe, il feed non
è nel fuso che si crede.

## Quanto costa sbagliare

Non è un bias di secondo ordine, ed è utile avere l'ordine di grandezza in
mente. Segmentando le stesse 462.692 barre a 15 minuti di @NQ nei due modi —
`1700`/`1559` letti sull'orologio del feed contro gli stessi letti in ora di
Chicago — le sessioni riconosciute diventano 6.035 invece di 5.013, il 20% in
più, perché il taglio nel punto sbagliato spezza sessioni che non esistono. I
confini stanno 17:15→15:45 invece di 00:15→22:45. E sul livello che gli engine
Unger tradano più spesso, `highd1`/`lowd1`, i due modi coincidono solo nel
**2,8%** delle barre: lo scostamento mediano è di 20,8 punti, il 34,7%
dell'ampiezza della sessione precedente, e nel 66,5% delle barre supera un
quarto di quell'ampiezza.

## Il registro degli strumenti

`InstrumentSpec.SessionTimeZone` dichiara, per ogni simbolo, il fuso IANA in
cui i suoi orari di sessione sono corretti. Il campo è obbligatorio e segue la
stessa filosofia di `PointValue`: nessun fallback silenzioso, un simbolo senza
specifica verificata è un errore esplicito. Un fuso sbagliato qui sposta il
confine di ore senza produrre alcun messaggio.

`InstrumentRegistry` raggruppa i valori in tre costanti — `CmeChicago`,
`NyComexNymex`, `EurexFrankfurt` — così la ragione della scelta sta scritta una
volta invece che ripetuta su ogni riga. Attenzione che la scelta **non** è
"dove ha sede la borsa": metalli ed energia stanno su New York pur essendo
prodotti CME Group, perché le loro sorgenti scrivono `1800`→`1700`, che è la
sessione in ora di New York.

## L'orologio di sessione

`SessionClock` è l'unico punto del sistema in cui compare un fuso diverso da
UTC. Converte l'istante della barra in ora di borsa ed espone le tre cose che
servono alla segmentazione: `ToSessionTime`, `Hhmm` e `SessionDay`.

Il **giorno di calendario conta quanto l'ora**. Il cambio di giorno è una delle
condizioni che aprono una sessione nuova in `OHLCMulti5` e in `InSessionBars`,
e se lo si leggesse in UTC mentre l'ora è in ora di borsa cadrebbe nel mezzo
della sessione invece che nella pausa. Chi tocca quelle funzioni deve convertire
entrambi, non solo `Hhmm`.

L'istanza tiene in cache l'offset dell'ultimo giorno UTC visto, perché le barre
arrivano in ordine e senza cache si pagherebbe una ricerca sul fuso per ogni
barra e per ogni strategia — e queste funzioni girano sull'intera finestra a
ogni valutazione. Nei due giorni all'anno in cui l'ora legale cambia l'offset
non è costante dentro la giornata UTC: lì la cache si disattiva da sola, perché
altrimenti metà delle barre di quel giorno userebbe l'offset dell'altra metà.
L'istanza **non è thread-safe** per scelta, e va creata una per strategia; il
`TimeZoneInfo` sottostante è immutabile e condiviso.

Vale la pena sapere che .NET accetta gli identificatori IANA anche su Windows,
tramite ICU: non serve tradurli nei nomi di fuso Windows.

## L'orologio della macchina non è un dato del sistema

Fuori da `SessionClock` non esiste nessun fuso: "adesso" è `DateTime.UtcNow` e
nient'altro. Il fuso dell'host cambia fra la postazione di sviluppo, il server e
il container, e due volte l'anno cambia da solo — e quando entra in un calcolo
non produce un errore, produce un risultato plausibile e irriproducibile. Aveva
già fatto danni in tre punti: la cron del `DataFeedWorker`, che stabiliva a che
ora si scaricano le barre; la validità del setup di rotazione settimanale, che
scadeva con ore di anticipo o ritardo secondo l'host; e la finestra di default
di un'ottimizzazione, che delimita barre UTC.

L'unica eccezione legittima è la **presentazione**: la console WinForms mostra
gli istanti nell'ora di chi guarda lo schermo, e lì `ToLocalTime()` è corretto.
Tutto ciò che decide, calcola, nomina un artefatto o finisce su disco resta UTC.

`UtcOnlyConformanceTests` fa rispettare la regola sul sorgente, non sul
comportamento: su una macchina configurata su UTC — cioè quella dove il test
girerebbe — il codice sbagliato si comporta esattamente come quello giusto, e un
test sul comportamento non lo vedrebbe. Verifica anche che lo script di
generazione dei feed continui a pretendere `--source-timezone` esplicito.

## Quando aggiungi una strategia

Copia gli orari di sessione dalla sorgente EasyLanguage **così come sono**, senza
convertirli. Il loro fuso è quello dichiarato nel registro per il simbolo, e la
conversione avviene a valle.

Poi verifica che il registro conosca il simbolo. Se non lo conosce il backtest
si ferma con un errore esplicito, ed è voluto: prima di aggiungerlo, controlla
che la coppia start/end dichiarata dalla sorgente coincida con l'orario reale
della borsa **in quel fuso**. È la verifica, e costa due minuti.

Attenzione ai casi in cui la sorgente non usa la sessione piena. `Easy_244`
dichiara `0`→`2359` e `Easy_666` chiude alle `1715`: non sono errori, sono
scelte della sorgente, e vanno riportate tali e quali.

Infine, ricorda che ogni orario della strategia è in ora di borsa, non solo la
coppia di sessione: le finestre operative (`StartTrade`/`EndTrade`), gli orari
di uscita a tempo, i filtri sul giorno della settimana. Il giorno della
settimana in particolare è insidioso, perché un `dayofweek()` letto in UTC
cambia giorno nel mezzo della sessione serale americana.

## Stato della migrazione

**Il lavoro non è finito.** Al momento esistono e sono verificati
`SessionClock` e il campo `SessionTimeZone` sul registro. Non è ancora fatto il
passaggio dell'orologio dentro `EasyLib` — le tre funzioni di segmentazione, le
due finestre orarie, `IsSessionLastBar`, i quattro `GetDaily*` e `GroupByDay` —
né l'aggiornamento dei circa trentacinque punti di chiamata in motori e
strategie, né quello delle serie sintetiche dei test, che oggi costruiscono
barre a orari UTC coincidenti con gli orari di sessione.

C'è anche un punto in direzione inversa: `CombineDateAndHhmm` costruisce le
deadline `CloseAtUtc` dei time exit e dovrà tornare da ora di borsa a UTC.

Finché la migrazione non è completa, **i backtest @NQ archiviati sono calcolati
su sessioni sbagliate** e non sono confrontabili con quelli che verranno.

## Riferimenti codice

- `Piootoo.Shared/Configuration/SessionClock.cs` — conversione, cache
  dell'offset, gestione dei giorni di cambio d'ora.
- `Piootoo.Shared/Configuration/InstrumentRegistry.cs` — le tre costanti di
  fuso e `CreateSessionClock`.
- `Piootoo.Shared/Models/Trading/InstrumentSpec.cs` — `SessionTimeZone` e
  perché non è "la borsa dello strumento".
- `Piootoo.Strategies/Easy/EasyLib.cs` — `OHLCMulti5`, `BuildSessionSeries`,
  `LastBarOfPreviousSession`, `InSessionBars`, `TimeWindow`,
  `IsSessionLastBar`, `CombineDateAndHhmm`.
- `Piootoo.Strategies/Easy/Engines/EasyEngineBase.cs` — `BuildSessionOhlc`,
  `Hhmm`, `EasyDayOfWeek`.
- `Piootoo.Strategies.Tests/SessionClockTests.cs` — regressioni sull'ora
  legale.
- [`datafeed-generazione.md`](datafeed-generazione.md) — come si accerta
  l'orologio di un feed e come si generano i timeframe.
- [`motori-strategie.md`](motori-strategie.md) — regole comuni dei motori e
  guida di porting.
