# Documentazione Piootoo

Indice. Ogni file copre un concetto autoconsistente, non un progetto della
solution. Stato: **stabile** = contenuto verificato contro il codice,
**bozza** = solo scheletro/titoli, da scrivere.

## Da leggere per primo

- [`lavori-in-corso.md`](lavori-in-corso.md) — stato dei lavori aperti, cosa non è ancora
  compilato, questioni da decidere. **Deperibile**: le voci chiuse si cancellano, la motivazione
  resta in `decisioni.md`. Da leggere prima di riprendere un lavoro a metà.

- [`PROGETTO.md`](PROGETTO.md) — descrizione del progetto: cosa fa il sistema,
  moduli, flussi, invarianti da non rompere, trappole note. *Stabile.*
- [`verifica-codice-2026-07-27.md`](verifica-codice-2026-07-27.md) — audit del
  codice con evidenze e riferimenti puntuali. *Stabile.*
- [`verifica-backtest-sizing-titano-2026-07-29.md`](verifica-backtest-sizing-titano-2026-07-29.md)
  — verifica riproducibile PTS, lotti decimali e applicazione Titano nei due
  engine. *Stabile.*

## Architettura

- [`architettura/overview.md`](architettura/overview.md) — mappa dei moduli
  della solution e flusso dati end-to-end. *Bozza, superato da `PROGETTO.md` §2.*

## Domini

- [`domini/workspaces-e-masterfilter.md`](domini/workspaces-e-masterfilter.md)
  — cos'è un workspace, `masterfilter.json`. *Bozza.*
- [`domini/account-e-conversione-symbol.md`](domini/account-e-conversione-symbol.md)
  — account di workspace, tabella di conversione symbol, moltiplicatore
  contratto, effetto su size e `signals.json`. *Stabile.*
- [`domini/backtesting.md`](domini/backtesting.md) — `signals.json`/
  `trades.json`, contratto cross-engine. *Bozza.*
- [`domini/orologio-barre-e-fill.md`](domini/orologio-barre-e-fill.md) —
  orologio sintetico del loop, buchi del feed, fill fantasma e come validare i
  fill di un run. *Stabile.*
- [`domini/orari-di-sessione-e-fusi.md`](domini/orari-di-sessione-e-fusi.md) —
  in che orologio vanno letti gli orari di sessione delle sorgenti
  EasyLanguage, come si accerta il fuso di un feed, cosa costa sbagliarlo, cosa
  controllare quando si aggiunge una strategia o un simbolo. *Stabile, con una
  migrazione ancora aperta segnalata nel file.*
- [`domini/datafeed-generazione.md`](domini/datafeed-generazione.md) — come si
  genera un timeframe mancante dai CSV storici e come si verifica il
  risultato. *Stabile.*
- [`domini/parita-riferimento-esterno.md`](domini/parita-riferimento-esterno.md)
  — confronto fra una strategia portata e il suo riferimento esterno: cosa è
  confrontabile, procedura, cause di divergenza in ordine di impatto. *Stabile.*
- [`domini/titano-rotation.md`](domini/titano-rotation.md) — filtro Titano:
  formule, isteresi, sizing, calendario, artefatti, API. *Stabile.*
- [`domini/position-sizing.md`](domini/position-sizing.md) — calcolo di
  `FinalQuantity` (Titano × ATR × rischio portfolio). *Stabile.*
- [`domini/trading-sessions-api.md`](domini/trading-sessions-api.md) — ciclo
  di vita di una sessione, endpoint, autorità di execution. *Stabile.*
- [`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md)
  — come un cBot consegna le candele al server: riscaldamento all'avvio, finestra
  corta a regime, perché la sovrapposizione impedisce i buchi e come si
  diagnostica una sessione che non produce segnali. *Stabile.*
- [`domini/distribuzione-multi-account.md`](domini/distribuzione-multi-account.md)
  — secondo layer di filtro dopo Titano: gruppi, slot, budget di concorrenza per
  account (trasversale ai simboli) e le due modalità di conteggio, con matrice di
  esempi verificati. *Stabile.*
- [`domini/trading-plans.md`](domini/trading-plans.md) — configurazioni operative
  riutilizzabili e apertura idempotente delle sessioni dal cBot. *Stabile.*
- [`domini/cbot-realtime-backtest-titano.md`](domini/cbot-realtime-backtest-titano.md)
  — guida operativa ai cBot cTrader nei tre scenari (backtest puro, backtest con
  Titano, realtime): parametri, `open-plan`, matrice modalità, troubleshooting.
  *Stabile.*
- [`domini/strategie-catalogo.md`](domini/strategie-catalogo.md) —
  `ITradingStrategy`, catalogo, generazione da EasyLanguage. *Bozza.*
- [`domini/motori-strategie.md`](domini/motori-strategie.md) — regole comuni
  dei motori Unger e guida di porting, inclusa la specifica `TF_M` NQ 60.
  *Stabile.*
- [`domini/porting-da-report-sweep.md`](domini/porting-da-report-sweep.md) —
  come si legge un run di ottimizzazione esterno, mappa parametri → campi del
  motore, trappole verificate e procedura di verifica contro il report.
  *Stabile.*
- [`domini/mappa-strategie-pts.md`](domini/mappa-strategie-pts.md) — da quale
  run e da quale riga approvata viene ogni classe `PTS_*`, sigle dei motori,
  strategie disabilitate perché doppioni, motori senza più sottoclassi.
  *Stabile.*
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
