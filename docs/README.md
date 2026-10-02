# Documentazione Piootoo

Indice. Ogni file copre un concetto autoconsistente, non un progetto della
solution. Stato: **stabile** = contenuto verificato contro il codice,
**bozza** = solo scheletro/titoli, da scrivere, **storia** = descrive codice che
non esiste più, si tiene per il perché.

> Titano è stato rimosso dalla piattaforma il 03/09/2026 (vedi `decisioni.md`).
> I file marcati *Storia* qui sotto lo descrivono e non valgono più come
> riferimento sul codice.

## Da leggere per primo

- [`lavori-in-corso.md`](lavori-in-corso.md) — stato dei lavori aperti, cosa non è ancora
  compilato, questioni da decidere. **Deperibile**: le voci chiuse si cancellano, la motivazione
  resta in `decisioni.md`. Da leggere prima di riprendere un lavoro a metà.

- [`registro-piani.md`](registro-piani.md) — scheda di ogni piano di trading: strategie, size,
  conto, ultimi backtest. **Si aggiorna a ogni piano creato o modificato.**

- [`../piootoo-repository/web/`](../piootoo-repository/web/LEGGIMI.md) — il sito per chi opera:
  **manuale d'uso** (`manuale.html`) e **piani per broker** (`piani.html`: piani promossi, strategie,
  compatibilità fra conti dello stesso broker). La pagina dei piani si rigenera dal disco con
  `python aggiorna-dati.py`; il manuale si corregge a mano quando cambia una schermata.

- [`PROGETTO.md`](PROGETTO.md) — descrizione del progetto: cosa fa il sistema,
  moduli, flussi, invarianti da non rompere, trappole note. *Stabile.*
- [`verifica-codice-2026-07-27.md`](verifica-codice-2026-07-27.md) — audit del
  codice con evidenze e riferimenti puntuali. *Stabile.*
- [`verifica-backtest-sizing-titano-2026-07-29.md`](verifica-backtest-sizing-titano-2026-07-29.md)
  — verifica riproducibile PTS, lotti decimali e applicazione Titano nei due
  engine. *Storia (Titano rimosso).*
- [`verifica-offset-conversione-2026-09-08.md`](verifica-offset-conversione-2026-09-08.md)
  — confronto a tre fra sorgenti EasyLanguage, motori Python e engine C#: quali
  offset di etichetta, fuso e ancoraggio sono compensati e quali no. *Stabile.*

## Architettura

- [`architettura/overview.md`](architettura/overview.md) — mappa dei moduli
  della solution e flusso dati end-to-end. *Bozza, superato da `PROGETTO.md` §2.*

## Domini

- [`domini/workspaces-e-masterfilter.md`](domini/workspaces-e-masterfilter.md)
  — cos'è un workspace, `masterfilter.json`. *Bozza.*
- [`domini/account-e-conversione-symbol.md`](domini/account-e-conversione-symbol.md)
  — anagrafica broker, conti, tabella di conversione symbol, moltiplicatore
  contratto, effetto su size e `signals.json`. *Stabile.*
- [`domini/backtesting.md`](domini/backtesting.md) — `signals.json`/
  `trades.json`, contratto cross-engine. *Bozza.*
- [`domini/best-plans.md`](domini/best-plans.md) — backtest messi in evidenza,
  fotografati fuori dai workspace in `best-plans/`: cosa si copia, da dove viene
  la curva, cifre per anno. *Bozza.*
- [`domini/broker-workspace.md`](domini/broker-workspace.md) — i piani in
  produzione, uno per broker, promossi solo da best plan con le strategie attive
  dichiarate; il risolutore unico dei piani e le fasi di implementazione.
  *Stabile per le fasi 1-5; la migrazione dei piani sui conti e' da fare.*
- [`domini/orologio-barre-e-fill.md`](domini/orologio-barre-e-fill.md) —
  orologio sintetico del loop, buchi del feed, fill fantasma e come validare i
  fill di un run. *Stabile.*
- [`domini/overnight-e-overweek.md`](domini/overnight-e-overweek.md) — chi
  decide se una posizione resta aperta oltre la sessione o oltre il fine
  settimana: la gerarchia piano → motore → strategia, dove si dichiara e dove
  si vede. *Stabile.*
- [`domini/layer-barre-e-calendario.md`](domini/layer-barre-e-calendario.md) —
  il layer unico che normalizza le barre per backtest interno, backtest su feed
  di broker e realtime: calendario di mercato come dato, pipeline a stadi,
  aggregazione ancorata, orologio a barre con logica future sul fine settimana.
  *Migrazione in corso: il §7 dice cosa e' gia' fatto e cosa no.*
- [`domini/orari-di-sessione-e-fusi.md`](domini/orari-di-sessione-e-fusi.md) —
  in che orologio vanno letti gli orari di sessione delle sorgenti
  EasyLanguage, come si accerta il fuso di un feed, cosa costa sbagliarlo, cosa
  controllare quando si aggiunge una strategia o un simbolo. *Stabile, con una
  migrazione ancora aperta segnalata nel file.*
- [`domini/datafeed-generazione.md`](domini/datafeed-generazione.md) — come si
  genera un timeframe mancante dai CSV storici e come si verifica il
  risultato. *Stabile.*
- [`domini/raccolta-datafeed-esterno.md`](domini/raccolta-datafeed-esterno.md)
  — come si costruisce un datafeed dai dati di un broker cTrader: raccolta a
  blocchi, journal e compattazione, deduplica delle sovrapposizioni, buchi
  dichiarati, una cartella per broker. API `api/datafeed-external` e cBot
  `PiootooDatafeedSyncBot`. *Stabile.*
- [`domini/spread-e-costo-di-transazione.md`](domini/spread-e-costo-di-transazione.md)
  — come si misura lo spread di un broker (`PiootooSpreadDumpBot`, distribuzione
  per simbolo e per ora UTC) e come entra nel backtest: peggiora il solo prezzo
  di ingresso, il numero che conta è `spread / distanza di stop`. *Stabile.*
- [`domini/parita-riferimento-esterno.md`](domini/parita-riferimento-esterno.md)
  — confronto fra una strategia portata e il suo riferimento esterno: cosa è
  confrontabile, procedura, cause di divergenza in ordine di impatto. *Stabile.*
- [`domini/titano-rotation.md`](domini/titano-rotation.md) — filtro Titano:
  formule, isteresi, sizing, calendario, artefatti, API. *Storia (Titano rimosso).*
- [`domini/position-sizing.md`](domini/position-sizing.md) — calcolo di
  `FinalQuantity` (ATR × rischio portfolio). *Stabile.*
- [`domini/trading-sessions-api.md`](domini/trading-sessions-api.md) — ciclo
  di vita di una sessione, endpoint, autorità di execution. *Stabile.*
- [`domini/riavvio-del-server-e-ripresa-sessione.md`](domini/riavvio-del-server-e-ripresa-sessione.md)
  — cosa si perde quando il server si riavvia con una sessione realtime viva,
  cosa si dumpa e cosa si ricostruisce, la procedura di riallineamento con
  cTrader e il presidio dalla console. *Progetto per le fasi 1-4, stabile per il
  presidio (§8).*
- [`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md)
  — come un cBot consegna le candele al server: riscaldamento all'avvio, finestra
  corta a regime, perché la sovrapposizione impedisce i buchi e come si
  diagnostica una sessione che non produce segnali. *Stabile.*
- [`domini/distribuzione-multi-account.md`](domini/distribuzione-multi-account.md)
  — chi esegue quale segnale: gruppi, slot, budget di concorrenza per
  account (trasversale ai simboli) e le due modalità di conteggio, con matrice di
  esempi verificati. *Stabile.*
- [`domini/trading-plans.md`](domini/trading-plans.md) — configurazioni operative
  riutilizzabili e apertura idempotente delle sessioni dal cBot. *Stabile.*
- [`domini/cbot-realtime-backtest-titano.md`](domini/cbot-realtime-backtest-titano.md)
  — guida operativa ai cBot cTrader nei tre scenari (backtest puro, backtest con
  Titano, realtime): parametri, `open-plan`, matrice modalità, troubleshooting.
  *Storia per la parte Titano; il resto (parametri, `open-plan`) vale ancora.*
- [`domini/strategie-catalogo.md`](domini/strategie-catalogo.md) —
  `ITradingStrategy`, catalogo, generazione da EasyLanguage. *Bozza.*
- [`domini/motori-strategie.md`](domini/motori-strategie.md) — regole comuni
  dei motori Unger e guida di porting, inclusa la specifica `TF_M` NQ 60.
  *Stabile.*
- [`domini/porting-da-report-sweep.md`](domini/porting-da-report-sweep.md) —
  come si legge un run di ottimizzazione esterno, mappa parametri → campi del
  motore, trappole verificate e procedura di verifica contro il report.
  *Stabile.*
- [`domini/ricerca-parametri.md`](domini/ricerca-parametri.md) — l'ottimizzatore
  **interno** (`piootoo-sweep`): fasi, griglie, i due orologi e perché il percorso
  veloce non può scegliere gli stop, il criterio sul peggior tratto, la validazione
  fuori campione e il costo peggiore fra più broker. È da qui che vengono le
  `PT3B_*`, mentre il file sopra copre i parametri che arrivano da fuori. *Bozza.*
- [`domini/mappa-strategie-pt5dav.md`](domini/mappa-strategie-pt5dav.md) — la serie
  `PT5DAV_*` dalla ricerca v5.0 fatta su un altro server: perimetro (le 135 che non
  tengono più di una notte), motori propri e base comune (ATR50 di Wilder, sessione
  della ricerca, orari del CFD), regole misurate riconciliando il pilota NQ con i
  trade Python, elenco classe → codice. *Bozza: riconciliato il solo NQ.*
- [`domini/mappa-strategie-pt8dav.md`](domini/mappa-strategie-pt8dav.md) — la serie
  `PT8DAV_*`: la consegna v5.1 della stessa ricerca (111 strategie, parametri scelti
  fino al 2022 e filtro sui tre anni dopo), sugli stessi motori delle PT5DAV. Cosa
  promette la selezione, le regole dei motori corrette riconciliando (ATR di Wilder
  della VBO, ordini e cambio di sessione, `max_bars` e ordini sul DAX, BIASW, MAC),
  il DAX a 4 ore con le barre piegate dal motore, l'ATR50 non rodato della prova
  sul broker della ricerca, differenze note, elenco classe → codice con i trade
  ritrovati. *Bozza: manca la verifica del cBot per le FDAX a 4 ore.*
- [`domini/catalogo-idee-pt6exo.md`](domini/catalogo-idee-pt6exo.md) — la serie
  `PT6EXO_*`: famiglie di motori nuove, scelte per essere scorrelate dal catalogo
  (orologio, volume, forma della barra, regime, calendario, idee bizzarre), il
  controllo a ingresso casuale, come si compongono i piani, ordine di lavoro.
  *Bozza: scritti tutti i motori (XMK solo per la sweep), nessuno ancora misurato.*
- [`domini/catalogo-pattern-classici.md`](domini/catalogo-pattern-classici.md) — la serie
  `PT7CLP_*`: figure classiche dell'analisi tecnica (bandiera, doppio massimo, testa e
  spalle, triangoli) tradotte in regole in ATR, ingresso sulla rottura della conferma.
  *Bozza: scritta la bandiera (FLG), in coda la griglia.*
- [`domini/catalogo-armonici.md`](domini/catalogo-armonici.md) — la serie `PTARM_*`:
  figure armoniche X-A-B-C-D (Gartley, Bat, Butterfly, Crab) con i rapporti di
  Fibonacci fissati nella classe, pivot confermati, limit su D, controllo a rapporti
  falsati. *Bozza: scritte le quattro figure, in coda la griglia.*
- [`domini/studio-apertura.md`](domini/studio-apertura.md) — lo studio dell'apertura di
  borsa: opening range (breakout e fade) e bandiera in una finestra in ora di borsa, tutto
  in `Piootoo.Strategies/OpeningStudy/` per toglierlo in un colpo. *Bozza: scritto, in coda
  la griglia.*
- [`domini/mappa-strategie-pts.md`](domini/mappa-strategie-pts.md) — da quale
  run e da quale riga approvata veniva ogni classe `PTS_*`. **Serie eliminata dal
  progetto il 24/09/2026.** *Storia: non descrive più il codice.*
- [`domini/mappa-strategie-pt2.md`](domini/mappa-strategie-pt2.md) — la serie
  `PT2_*`, porting del paniere rifatto di `run-engine-v2/DOSSIER_PANIERE_001.md`:
  scheda → classe e parametri. **Serie rimossa dal progetto il 22/09/2026**
  perché nessuna delle quattro classi aveva superato la validazione ai costi
  veri. *Storia: non descrive più il codice.*
- [`domini/feed-worker.md`](domini/feed-worker.md) — `FeedRunner`/
  `FeedWorker`, invio barre chiuse. *Bozza.*

## Client

- [`client/workspace-console.md`](client/workspace-console.md) — console
  WinForms (`piootooapp.clientform`), i suoi quattro tab. *Bozza.*

## Decisioni

- [`decisioni.md`](decisioni.md) — log breve delle scelte fatte e perché.

## Convenzioni

- Un file per concetto, nome file in kebab-case, prosa tecnica compatta senza
  liste puntate salvo enumerazioni reali (endpoint, sequenze, campi).
- Ogni file di dominio chiude con "Riferimenti codice" verso le classi
  rilevanti, per non dover ripetere firme/parametri che cambiano nel codice.
- Quando una regola tocca più domini (es. sizing dentro le sessioni di
  trading), si linka l'altro documento invece di duplicare il contenuto.
