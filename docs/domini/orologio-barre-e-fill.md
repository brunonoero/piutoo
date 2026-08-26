# Orologio del backtest, buchi del feed e fill fantasma

Il loop di backtest avanza su un orologio sintetico, non sulle barre del feed:
scandisce il minimo timeframe fra le strategie coinvolte dall'inizio alla fine
dell'intervallo richiesto, un tick alla volta. Il feed invece ha buchi — notte,
weekend, festivi, giorni mancanti, e soprattutto la coda dell'intervallo
richiesto quando questo va oltre l'ultima barra disponibile. Su quei tick non
esiste una barra "corrente", e il cursore risponde con l'ultima barra chiusa,
che può essere di ore o di mesi prima.

Da qui nascono i **fill fantasma**: trade registrati a un istante in cui nessuno
ha scambiato, a un prezzo che il mercato non ha mai stampato. Non sono un caso di
laboratorio e non sono neutri, sono sistematicamente in perdita: un breakout che
non è avvenuto viene comunque comprato al livello dello stop, e da lì il prezzo
non ha nessun motivo di proseguire. Su `PTS_NQ_PCH_001_15` erano il **45% dei trade**,
e la loro rimozione porta lo stesso run da $56 a $33.270 di net profit e dal 19%
al 36% di trade vincenti.

## Due usi diversi della stessa barra

La distinzione che tiene in piedi tutto è che una barra serve a due cose
incompatibili, e solo una delle due tollera un dato stantio.

Il **prezzo di mark-to-market** (`currentPrices`) deve essere sempre l'ultimo
prezzo noto, anche vecchio: senza di esso stop loss, take profit, break even e
time exit non potrebbero essere valutati affatto, e una posizione aperta il
venerdì resterebbe cieca fino al lunedì.

La **barra di esecuzione** (`currentBars`) è quella su cui si decide se un
trigger è stato toccato e a che prezzo si riempie. Deve appartenere al tick
corrente: usare una barra vecchia significa far scattare un ordine sui prezzi di
un intervallo diverso da quello che l'orologio sta rappresentando.

`PiootooBacktestingService.BelongsToCurrentTick` è il predicato che separa i due
casi: una barra appartiene al tick se è stata chiusa entro l'ampiezza del tick
stesso. Il prezzo di mark entra sempre, la barra di esecuzione solo se fresca.

## Come nasce un fill fantasma

La catena, prima delle correzioni, era questa.

1. Su un tick senza barra nuova la strategia veniva valutata di nuovo sull'ultima
   barra chiusa, vedeva le stesse condizioni e riemetteva **lo stesso** ordine
   stop, con `ValidFromUtc` ed `ExpiresAtUtc` stimati su una "barra successiva"
   che nel frattempo era già passata.
2. `ProcessSignals` chiedeva a `RequiresDeferredExecution` se l'intent andasse
   messo in attesa. Un intent **scaduto** non è né futuro né stop pendente:
   cadeva nel ramo "eseguibile subito".
3. L'esecuzione immediata senza una barra da cui leggere i prezzi ripiegava sul
   prezzo del segnale, cioè esattamente il livello dello stop.

Il risultato è un ingresso al livello del trigger, a un timestamp senza barra,
senza che il mercato lo abbia mai raggiunto. Le strategie breakout sono le più
esposte, perché ripubblicano l'ordine a ogni barra finché restano flat: ogni
tick vuoto è un'occasione in più di riempirsi da solo.

Il segnale d'allarme tipico è una concentrazione anomala di ingressi su un
singolo orario, spesso l'ultimo timestamp presente nel feed.

## Le tre guardie

**Un intent scaduto si scarta, non si esegue.** In `ProcessSignals`, se
`currentTime > ExpiresAtUtc` il segnale viene abbandonato. La barra su cui era
valido è passata e non sappiamo a che prezzo l'avrebbe riempito: eseguirlo al
suo livello è inventare un prezzo. La verifica di scadenza è stata quindi tolta
da `RequiresDeferredExecution`, che ora risponde solo alla domanda
"questo intent è per il futuro?".

**Un intent next-bar si riempie solo su una barra abbastanza recente.**
`CanExecuteOnBar` pretende che la barra candidata sia chiusa a partire da
`ValidFromUtc`. Vale per stop, limit e market non-`ExitOnly`; le uscite ne sono
escluse, perché una chiusura va eseguita anche su dati stantii.

**Solo le barre fresche entrano in `currentBars`.** È la guardia a monte, quella
che evita di dover ripetere il controllo in ogni ramo di esecuzione.

Le uscite tecniche — `WeekEnd`, `EndOfRun` — restano legittimamente senza barra
corrispondente: non sono fill di mercato ma chiusure forzate al prezzo di mark.

## Quanto vive un pending, e su quale barra

`ExpiresAtUtc` è **l'inizio dell'ultima barra su cui l'ordine vive, non un
istante**. Un "next bar" nasce con `ValidFromUtc == ExpiresAtUtc` e vale per
tutta la barra che comincia lì: sul broker si vede come `Create` a T e `Cancel`
a T + timeframe.

L'orologio del backtest è però il timeframe **minimo** del portafoglio, e
`currentBars` tiene **una sola barra per simbolo**, quella della serie più fitta
(`markCursors` sceglie il timeframe più piccolo). Confrontare `ExpiresAtUtc` con
il tick corrente faceva quindi morire l'ordine di una strategia a 60 minuti dopo
il primo tick da 30: metà del suo range non veniva mai guardata. Per questo il
segnale dichiara `TimeframeMinutes` e la scadenza si misura su
`ExpiresAtUtc + TimeframeMinutes`; senza il campo si ricade sul confronto di
prima, che per le strategie al timeframe minimo dà lo stesso risultato.

L'effetto è più forte sui **limit**, che chiedono penetrazione stretta
(`bar.Low < livello`) e non il solo tocco: dimezzare la finestra dimezza le
occasioni. Nel confronto del 26/08/2026 le due strategie a limite a 60 minuti
avevano 29 fill interni contro 69 esterni, e il **54%** dei fill del broker
cadeva nella seconda mezz'ora dell'ora — esattamente la metà invisibile.

## Un livello già scavalcato non è un ordine

Uno stop buy sotto il prezzo corrente non è più il breakout che la strategia
aspettava: è un market al peggiore dei due prezzi. Il cBot lo scarta al
piazzamento (`RejectWrongSideLevels`, acceso di default) e il motivo compare nel
log come *livello … dal lato sbagliato*; l'engine interno invece riempiva, con
`Math.Max(bar.Open, livello)` — un prezzo reale, ma su un trade che nel conto
vero non esiste.

`PiootooTradingService.RejectWrongSideLevels`, anch'esso acceso di default,
allinea i due. La verifica avviene **una volta sola**, sulla prima barra su cui
l'ordine è attivo, perché è lì che l'ordine "nasce": dalla seconda in poi un
pending vivo che il mercato raggiunge è ciò che la strategia voleva. Il confronto
è con l'apertura della barra e usa la disuguaglianza **stretta**: un livello
esattamente sull'apertura è il breakout che comincia lì, il fill sarebbe comunque
l'apertura, e scartarlo toglierebbe trade sani.

Spegnerlo riporta la semantica di TradeStation — dove un ordine "next bar" a un
livello già superato si riempie all'apertura — e serve solo a misurare la fedeltà
del porting rispetto al motore di ricerca, non a stimare cosa farà il conto vero.
Il contatore finisce nel `backtest-log.jsonl` come `wrongSideLevelsRejected`: un
numero alto non è un difetto, è la misura di quanti ingressi il backtest avrebbe
preso e il broker no. Se è zero mentre il log del cBot scarta, i due non stanno
guardando lo stesso mercato.

## Evidenza — PTS_NQ_PCH_001_15, 2026-08-02

Due run sullo stesso workspace `pts`, feed `NQ` 15m che copre
`2023-12-04 → 2025-05-30`.

Il confronto pulito è fra due run con la **stessa richiesta** — inizio
2024-01-01, fine 2025-12-31, quindi sette mesi oltre l'ultima barra — a parità di
codice della strategia, uno prima e uno dopo le guardie.

| | `backtest-20260801-0831` | `backtest-20260802-0914` |
|---|---|---|
| | prima delle guardie | dopo |
| trade | 226 | 150 |
| vincenti | 42 (19%) | 54 (36%) |
| net profit | $56 | $33.270 |
| ingressi fuori da una barra reale | 101 | 0 |

**101 dei 226 trade — il 45% — erano fill fantasma**: ingressi presenti solo nel
run pre-guardie, che nel run corretto non esistono più. Il conto non è
simmetrico, perché 25 ingressi *compaiono* dopo la correzione: i fill fantasma
consumavano il budget di `MaxEntriesPerSession` e bloccavano ingressi legittimi
più tardi nella stessa sessione.

Il meccanismo si legge in un confronto interno al run pre-guardie: **50.139
valutazioni contro 35.469 candele** nel feed. La strategia è stata valutata più
volte di quante barre esistano, perché su ogni tick oltre la fine del feed
rivedeva la stessa ultima barra chiusa e riemetteva lo stesso ordine, che poi si
riempiva da sé.

Nel run corretto tutti i 150 ingressi cadono su una barra reale con prezzo dentro
il suo `[low, high]`, nessun ingresso è successivo all'ultima barra del feed, e
l'unica uscita senza barra è la chiusura `WeekEnd` attesa.

Restano invece i **segnali** riemessi: `signals.json` contiene 14.625 copie
identiche dello stesso segnale, tutte con `timestampUtc` dell'ultima barra del
feed, e il contatore `buySignals` sale da ~9.970 a 24.099. Le guardie agiscono al
momento del fill, non della valutazione: i segnali inutili non diventano trade ma
gonfiano l'artefatto (35 MB) e rendono i contatori di segnale illeggibili. È la
ragione pratica per allineare l'intervallo richiesto alla copertura del feed. Lo
stesso fenomeno esiste in scala ridotta su ogni buco del calendario anche nei run
allineati — 96 copie sulla barra del 2023-12-29, 19 su quella del 2024-07-03 —
dove è innocuo ma spiega perché i segnali totali superano i timestamp distinti.

## Verificare un run nuovo

Il `backtest-summary.json` risponde da solo a buona parte delle domande, ma tre
controlli vanno fatti a mano sui trade perché nessun contatore li copre.

Sul summary: `coversRequestedRange: false` va trattato come un blocco, non come
un avviso. Chiedere un intervallo più lungo del feed non fa fallire il run, ma
è la condizione che genera i tick vuoti; conviene allineare la richiesta a
`firstBarUtc`/`lastBarUtc` del datasource. Un `TimeExit` fra le cause di uscita
di una strategia dichiarata multiday significa `IntradayOnly` sbagliato (vedi
[`motori-strategie.md`](motori-strategie.md)). `sellSignals > 0` su una strategia
solo long significa che il ramo direzionale in uso non è quello che si crede.

Sui trade: ogni timestamp di ingresso deve avere una barra nel feed; il prezzo di
ingresso e quello di uscita devono cadere dentro il `[low, high]` di quella
barra, con l'eccezione delle chiusure tecniche; e la distribuzione degli ingressi
per ora non deve avere picchi su un orario singolo fuori dalla finestra
operativa.

Un rapporto segnali/trade molto alto invece è normale e non va inseguito: uno
stop breakout viene ripubblicato a ogni barra finché la strategia resta flat,
quindi 9.969 segnali per 151 trade è il comportamento corretto.

Attenzione a un dettaglio degli strumenti: PowerShell converte i timestamp UTC
nel fuso locale quando li interpreta come `DateTime`. Analizzando le ore degli
ingressi conviene estrarre le cifre dalla stringa, altrimenti si inseguono
ingressi "di sabato" che non esistono.

## Cosa resta scoperto

`IsStrategyCandleStale` è inerte sotto il giornaliero: ritorna `false` per
`timeframeMinutes < 1440`, quindi non protegge nessuna strategia intraday. La
protezione effettiva oggi è `BelongsToCurrentTick`; il contatore
`skippedStaleCandle` resta di conseguenza a zero sugli intraday e non va letto
come "nessuna barra stantia incontrata".

Resta aperto il punto già elencato in [`../PROGETTO.md`](../PROGETTO.md) §8: le
strategie con timeframe superiore al minimo del run sono valutate sull'orologio
sintetico e non sui confini reali della loro barra.

## Riferimenti codice

- `Piootoo.Core/Services/PiootooBacktestingService.cs` — loop, `currentPrices` /
  `currentBars`, `BelongsToCurrentTick`
- `Piootoo.Core/Services/PiootooTradingService.cs` — `ProcessSignals`,
  `RequiresDeferredExecution`, `TryFillPendingOrders`, `CanExecuteOnBar`,
  `ResolveFillPrice`
- `Piootoo.Shared/Models/TradeSignal.cs` — `ValidFromUtc`, `ExpiresAtUtc`
- `Piootoo.Strategies.Tests/PendingStopOrderTests.cs` — regressioni
  `GapInFeed_DoesNotFillStopWithoutABar`,
  `ExpiredStopIntent_IsDiscardedInsteadOfFilledAtItsLevel`
