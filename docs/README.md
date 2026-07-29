# Documentazione Piootoo

Indice. Ogni file copre un concetto autoconsistente, non un progetto della
solution. Stato: **stabile** = contenuto verificato contro il codice,
**bozza** = solo scheletro/titoli, da scrivere.

## Da leggere per primo

- [`PROGETTO.md`](PROGETTO.md) — descrizione del progetto: cosa fa il sistema,
  moduli, flussi, invarianti da non rompere, trappole note. *Stabile.*
- [`verifica-codice-2026-07-27.md`](verifica-codice-2026-07-27.md) — audit del
  codice con evidenze e riferimenti puntuali. *Stabile.*

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
- [`domini/titano-rotation.md`](domini/titano-rotation.md) — filtro Titano:
  formule, isteresi, sizing, calendario, artefatti, API. *Stabile.*
- [`domini/position-sizing.md`](domini/position-sizing.md) — calcolo di
  `FinalQuantity` (Titano × ATR × rischio portfolio). *Stabile.*
- [`domini/trading-sessions-api.md`](domini/trading-sessions-api.md) — ciclo
  di vita di una sessione, endpoint, autorità di execution. *Stabile.*
- [`domini/trading-plans.md`](domini/trading-plans.md) — configurazioni operative
  riutilizzabili e apertura idempotente delle sessioni dal cBot. *Stabile.*
- [`domini/strategie-catalogo.md`](domini/strategie-catalogo.md) —
  `ITradingStrategy`, catalogo, generazione da EasyLanguage. *Bozza.*
- [`domini/motori-strategie.md`](domini/motori-strategie.md) — regole comuni
  dei motori Unger e guida di porting, inclusa la specifica `TF_M` NQ 60.
  *Stabile.*
- [`domini/feed-worker.md`](domini/feed-worker.md) — `FeedRunner`/
  `FeedWorker`, invio barre chiuse. *Bozza.*

## Client

- [`client/workspace-console.md`](client/workspace-console.md) — console
  WinForms (`piootooapp.clientform`), i suoi quattro tab. *Bozza.*
- [`client/titano-client.md`](client/titano-client.md) — client filtro/setup
  strategie (`piootoo.titanoclient`). *Bozza.*

## Decisioni

- [`decisioni.md`](decisioni.md) — log breve delle scelte fatte e perché.

## Convenzioni

- Un file per concetto, nome file in kebab-case, prosa tecnica compatta senza
  liste puntate salvo enumerazioni reali (endpoint, sequenze, campi).
- Ogni file di dominio chiude con "Riferimenti codice" verso le classi
  rilevanti, per non dover ripetere firme/parametri che cambiano nel codice.
- Quando una regola tocca più domini (es. sizing dentro le sessioni di
  trading), si linka l'altro documento invece di duplicare il contenuto.
