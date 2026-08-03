# Parità fra una strategia Piootoo e il suo riferimento esterno

Quando una strategia viene portata da un riferimento esterno — un run Python, un
report TradeStation, un backtest cTrader — la domanda "perché i risultati sono
diversi?" ha quasi sempre più di una risposta contemporaneamente. Questo
documento fissa cosa è lecito confrontare, in che ordine cercare le cause, e
quali divergenze sono difetti da correggere invece che differenze di convenzione.

## Cosa è confrontabile e cosa no

**I prezzi assoluti no.** I riferimenti Python della serie PTS girano su
contratti continui *back-adjusted*: NQ vale 4.607 nel 2012 e il datafeed Piootoo
ne dà 18.000 nel 2024 sullo stesso strumento. Non è un errore di nessuno dei
due — è il retro-aggiustamento dei rollover — ma rende inutile qualunque
confronto su livelli, e con esso su stop e target espressi in punti. Restano
confrontabili le distanze (SL, TP, trailing sono in dollari per contratto,
quindi invarianti) e la struttura dei trade.

**Le date sì, ed è da lì che si parte.** Il primo confronto utile è sulla
sequenza dei timestamp di ingresso: quanti ingressi in comune, quanti solo su un
lato, e su quali barre i due sistemi divergono per la prima volta. Una divergenza
che parte da un punto preciso e poi non recupera più è un problema di stato
(una posizione aperta da uno e non dall'altro); una divergenza diffusa e
simmetrica è un problema di dati o di condizione d'ingresso.

**La copertura del feed è il vincolo che decide se il confronto ha senso.**
Il datafeed `NQ` 15m copre oggi `2006-01-03 → 2025-05-30`, quindi contiene per
intero la storia dei run Python recenti e le metriche aggregate sono comparabili.
Non era vero fino al 2026-08-01, quando il feed partiva dal dicembre 2023: su un
campione di diciotto mesi una differenza di net profit non dimostrava niente. Il
primo controllo resta quello, `dataSources[].firstBarUtc` contro `first_trade`
del riferimento.

## Procedura

Verificare **quale codice ha prodotto il run** che si sta leggendo. È l'errore
che costa più tempo, perché non lascia tracce: `generatedAtUtc` nel
`backtest-summary.json` va confrontato con la data di modifica del sorgente della
strategia. Un run di stamattina su un file toccato dieci minuti dopo mostra il
comportamento di una versione che non esiste più — nel caso PTS_NQ_PCH_001 questo ha
prodotto una cartella con migliaia di segnali short su una strategia solo long,
indagati per un po' prima di accorgersi che il sorgente era cambiato.

Allineare l'**intervallo richiesto** alla copertura del datasource, e considerare
`coversRequestedRange: false` un blocco. I motivi sono in
[`orologio-barre-e-fill.md`](orologio-barre-e-fill.md).

Leggere il `backtest-summary.json` prima dei trade: valutazioni, segnali per
tipo, trade, cause di uscita. Tre incoerenze si vedono solo lì —
`sellSignals > 0` su una strategia solo long, `TimeExit` su una multiday,
`signalsWithoutExitSpec > 0` su una strategia che dichiara le uscite
all'ingresso.

Validare i fill contro il feed, come descritto in
[`orologio-barre-e-fill.md`](orologio-barre-e-fill.md). Un backtest con fill
fantasma non è confrontabile con niente: prima si ripulisce quello.

Solo allora confrontare i trade con il riferimento.

## Cause di divergenza, in ordine di impatto

**Fill fantasma e buchi del feed.** La più grossa vista finora, e l'unica che
produce trade interamente inventati. Documento dedicato:
[`orologio-barre-e-fill.md`](orologio-barre-e-fill.md).

**Default del motore non dichiarati dalla sottoclasse.** I motori Unger hanno
default che la specifica della strategia contraddice, e il caso ricorrente è
`IntradayOnly`, vero per default in `PriceChannelEngine`,
`SessionBreakoutEngine`, `TfEngines` e i due `ReversalBollingerBand`. Una
strategia con `intraday_only = 0` che non lo disattiva esce con `CloseAtUtc` a
fine sessione su ogni segnale, e diventa una strategia di sessione senza
violare nessun contratto: i test passano e si vede solo nei numeri. Su
`PTS_NQ_PCH_001_15` valeva 848 `TimeExit` su 2.012 trade. Corretto il 2026-08-02 per
PTS_NQ_PCH_001 e PTS_NQ_PCH_002 (voce in [`../decisioni.md`](../decisioni.md), regressione in
`Pts002PcTests`).

**Due percorsi direzionali nello stesso motore.** In `PriceChannelEngine`
convivono il percorso di parità Python, governato da `Direction`, e quello
legacy, governato da `EnableLong`/`EnableShort`. Rendere una strategia solo long
richiede coerenza su entrambi: `Direction = 1` blocca lo short sul primo, non sul
secondo.

**Convenzione di etichettatura delle barre.** Il datafeed Piootoo etichetta ogni
barra sull'apertura, `EasyLib.OHLCMulti5` la assume etichettata sulla chiusura.
È la divergenza più insidiosa perché non produce trade impossibili, produce trade
plausibili in momenti sbagliati: i timestamp del riferimento risultano spostati di
una barra, le barre della pausa di sessione non appartengono a nessuna sessione e
su di esse i gate leggono un `d0` stantio. Numeri misurati e stato della decisione
in [`porting-da-report-sweep.md`](porting-da-report-sweep.md).

**Granularità della finestra operativa.** Il riferimento confronta l'orario
completo con gli estremi `"HH:00"`, fine inclusa; confrontare le sole ore allarga
la finestra fino a `HH:59`. `PriceChannelEngine` è allineato,
`VolatilityBreakoutEngine`, `LevelFaderEngine` e `SessionBreakoutEngine` no.

**Granularità del feed a parità di timeframe nominale.** Due feed "15 minuti"
possono avere un numero di barre al giorno diverso, per sessioni, festivi o
buchi differenti. Per una strategia a canale la conseguenza è diretta: un
Donchian di 100 barre copre una finestra temporale diversa nei due sistemi,
quindi i livelli di trigger non sono gli stessi. Sul caso PTS_NQ_PCH_001 questa causa è
stata circoscritta: la serie è la stessa e i livelli ricalcolati coincidono, ma
resta un 18% di trade in cui il massimo di canale differisce.

**Convenzione di fill sulla barra di ingresso.** Lo scostamento di un tick fra
trade che coincidono per data è **slippage del riferimento**, non una nostra
imprecisione: su 925 trade PTS_NQ_PCH_001 il prezzo del report è esattamente un tick
sopra il livello ricalcolato in 490 casi. Il fill gap-aware Piootoo esegue uno
stop long a `max(open, livello)` senza slippage, e nel motore di esecuzione non
esiste un parametro per aggiungerlo, quindi la rettifica va fatta a mano: su NQ
$10 per trade fra ingresso e uscita.

**Commissioni.** Piootoo addebita `commissionPerContract` all'ingresso e
all'uscita, quindi il valore `2` produce $4 per trade round-turn, che coincide con
le assunzioni dei run Python. Se il riferimento ne applica altre, la differenza è
lineare nel numero di trade e va sottratta prima di confrontare i net profit.

**Momento di applicazione di `MaxEntriesPerSession`.** Il limite si applica al
**fill**, non all'invio: uno stop non eseguito può essere ripubblicato nella
stessa sessione. Un riferimento che conta gli ordini inviati produce meno
ingressi.

## Caso PTS_NQ_PCH_001_15 — 2026-08-02

Riferimento: `D:\Piootoo\davide\run_20260730_0005\trades\top01_PC.csv`. Primo
giro di verifica: workspace `pts`, cartella `verifica-fresh`,
`2024-01-01 → 2025-05-30`. Secondo giro, con il feed esteso al 2006: workspace
`pts-confronto`, cartelle `confronto-002-003-multiday` e `confronto-report-2012`.

Chiuse: le short spurie (sorgente cambiato dopo il run esaminato), i fill
fantasma (correzioni all'engine, tabella dei numeri in
[`orologio-barre-e-fill.md`](orologio-barre-e-fill.md)), `IntradayOnly`, la
granularità della finestra operativa, l'attribuzione dello scostamento di un tick
allo slippage del riferimento, e la copertura del datafeed.

Ancora aperta una sola causa, l'etichettatura delle barre, che da sola spiega 175
trade su 1.084 e $36.785 di utile non presenti nella fonte. Tabella del confronto
aggregato e procedura in
[`porting-da-report-sweep.md`](porting-da-report-sweep.md).

## Riferimenti codice

- `Piootoo.Strategies/Easy/Engines/PriceChannelEngine.cs`
- `Piootoo.Strategies/PiutooStrategies/PTS_NQ_PCH_001_15.cs`
- `Piootoo.Core/Services/PiootooBacktestingService.cs`
- `Piootoo.Domain/Repositories/DataSourceRepository.cs`
- `Piootoo.Strategies.Tests/Pts002PcTests.cs`
