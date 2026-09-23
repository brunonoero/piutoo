---
name: sweep-cella
description: >
  Lanciare e rileggere una ricerca a fasi con piootoo-sweep su una cella Piootoo: costi del
  paniere, --engine, --fix per cercare dentro una regione, split, soglie di ammissibilità, e la
  checklist di rilettura del resoconto. Usala quando si chiede di "lanciare la sweep", cercare
  filtri o pattern dentro una cella, o leggere un resoconto in ricerca/*-peggior-tratto*.md.
---

# Sweep: cercare dentro una cella

**Bozza del 23/09/2026.** La sweep a fasi ha già fallito tre volte per cause poi corrette
(orologio veloce, pavimento dei trade, spread costante): ogni regola qui sotto viene da una di quelle.
Codice: `Piootoo.Core/Optimization/Sweep/`, `Piootoo.Sweep/Program.cs`, `tools/sweep-paniere.ps1`.
Perché: `docs/domini/ricerca-parametri.md`.

## Quando

Solo dentro una cella che la griglia grossa (`/griglia-grossa`) ha detto avere un edge, o per
misurare una configurazione già scelta (`--params`, sola validazione). Una sweep su una cella senza
edge trova rumore con un punteggio alto: è successo su NQ 4h due volte.

## Il lancio

Da `tools/sweep-paniere.ps1`, che porta i costi giusti e nomina il resoconto con le ipotesi dentro:

- **Costi del paniere**: spread **per ora** e peggiore fra ICS e FTMO (`--spread-broker
  ICS,FTMO --spread-per-hour`), swap peggiore (`--swap-broker ICS,FTMO`), commissione per
  lato. Con lo spread costante la sweep sceglie la notte: misurato due volte.
- **Orologio al minuto** su ogni fase (default). Il veloce ordinava rumore, Spearman 0,021.
- **Ammissibilità**: `--min-trades` (250 su 4h, 3 anni), `--min-profit-factor 1.25`,
  `--min-average-trade` tarato sul costo per trade (150 su DE40, 120 su NQ 15: il 15% del range
  medio della barra). Una fase senza ammissibili non è un errore: i semi restano e si prosegue.
- **Criterio** `worst-period` (il peggiore dei quattro tratti): `net-over-dd` premiava la fortuna.
- **Motore**: `--engine PC | TFU | BIASW`. Un motore nuovo è uno `SweepSpace` nuovo, traduzione
  **verbatim** del `.py` in `easy_engine_py/` più le deviazioni dichiarate (`ExitHour`, `StopAtr`).
- **Regione**: `--fix "ChannelBars=20;Direction=1;ExitHour=21"` quando una misura più grossa ha già
  detto dove sta l'edge. Il resoconto scrive `Spazio: regione fissata`: una finalista trovata lì non
  si confronta con una dello spazio intero.
- **Fasi pattern**: `--split-pattern-phases` sotto l'ora (152 × 153 combinazioni non sono
  eseguibili). È una deviazione dichiarata: le coppie che rendono solo insieme non si trovano.
- **Tenuta**: `--flat-utc 20:45` se si opererà con un piano che vieta l'overnight. Cercare con una
  tenuta e operare con un'altra valida un'altra strategia.
- **Una corsa alla volta**: usa tutti i core. Log accanto al resoconto.

Il `--fix` e `--params` vogliono interi (`-1`, `-18` compresi).

## Rileggere il resoconto

Prima dei numeri, le domande che il 22/09 hanno fatto cadere tre conclusioni su quattro:

1. **Stesso split, stessi costi, stesso orologio** del riferimento con cui si confronta? Se no, il
   confronto non vale.
2. **Quante trade hanno le finaliste?** Se stanno sul pavimento (250-264 con `--min-trades 250`), il
   criterio ha scelto il conteggio, non la strategia.
3. **Le finaliste sono una configurazione in N varianti** di trailing e breakeven? Allora è una
   finalista, non N.
4. **Un parametro scelto è inerte** sulla configurazione (ExitHour con IntradayOnly = 0, MaxBars con
   IntradayOnly = 1 su 4h)? Il resoconto non lo dice: va controllato.
5. **Fuori campione batte dentro ovunque?** È il regime del periodo, non il motore. Confronta con il
   motore nudo della stessa cella nella griglia grossa: se il nudo fa lo stesso, i filtri non valgono.
6. **Il riferimento da battere non è lo zero**: è la regione nuda, o la finalista precedente, sullo
   stesso split.

Poi `/lettura-risultati` per il verdetto, e `/promuovi-finalista` solo se regge. `Survivor = null`
è un esito legittimo: non si abbassano le soglie finché qualcosa passa.

## Dove non applicarla

- Su una cella che la griglia ha bocciato.
- Con lo spread costante, con l'orologio veloce, o con `net-over-dd`: sono le tre vie già misurate
  false.
- In parallelo a un'altra corsa.
