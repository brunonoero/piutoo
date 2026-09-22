# compare-0047 — cbot-cfd-ICS contro interno-cfd-ICS

Cartella creata dalla console il 17/09/2026 12:48 UTC. Report dello strumento in `analisi/report.md`.

Per spread ai fill, esiti reali, ritardo degli intent e verifica delle uscite copia qui `log.txt` del cBot e l'export
Events di cTrader (`.xlsx`), poi rilancia lo strumento:

    dotnet run -c Release --project piootoo-repository/compare/strumento-confronto -- <questa cartella>

## Le due gambe

| lato | backtest | slug | serie di prezzi | versione | piano |
|---|---|---|---|---|---|
| cBot | `v2-ics-best2-bt-20201230-0000-v7.5.3-20260917-1243` | `cbot-cfd-ICS` | CFD ICS (datafeed-external/ICS) | 7.5.3 (client 7.5.2) | V2-ICS-BEST2 **com'era alle 12:43** |
| interno | `backtest-20260917-1245` | `interno-cfd-ICS` | CFD ICS (datafeed-external/ICS) | 7.5.3 | V2-ICS-BEST2 **com'era alle 12:46** |

## Esito

Finestra comune (ingressi): 30/12/2020 → 28/08/2021. Interno 190 trade, +104.146 $/contratto;
cBot 318 trade, −20.352 $/contratto. Divario **−124.498 $/contratto**, e cambia **segno**: non è
deriva, è un portafoglio diverso.

**Le due gambe non hanno eseguito lo stesso portafoglio.** Su tre strategie con trade, solo una è
comune a entrambi i lati. Il confronto motore-contro-motore, in questa cartella, riguarda quella
sola strategia; il resto misura due configurazioni diverse.

## Scomposizione

| quota | causa | Δ netto/ctr | % del divario |
|---|---|---:|---:|
| 1 | `PT2_FDAX_BSW_001_60` esiste solo nell'interno (32 trade) | −51.827 | 41,6% |
| 2 | `PT2_NQ_PCH_001_240` esiste solo sul cBot (156 trade) | −42.760 | 34,3% |
| 3 | `PT2_NQ_PCH_002_30`, l'unica comune | −29.911 | 24,0% |
| | **totale** | **−124.498** | 100% |

**Quota 1 — FDAX non è mai arrivato al server.** `session-summary.json` del run cBot:
`historyBarsHighWater = 0` sugli stream `FDAX/60` e `FDAX/240`, `everEvaluable = false` su entrambe
le strategie FDAX, zero intent emessi. Nessuna istanza cBot su FDAX ha spinto barre, quindi il
server ha saltato in silenzio per tutto il run (`history.Count < RequiredCandles`, 144 barre per
`PT2_FDAX_BSW_001_60`). Non è una divergenza di esecuzione: la strategia non è stata *eseguita*.
Sull'interno quelle 32 barre valgono +51.827, tutte uscite per `TimeExit`.

**Quota 2 — le due gambe hanno letto due versioni diverse dello stesso piano.**
`workspaces/v02-001/plans/plans.json`: V2-ICS-BEST2 creato alle **12:42:55**, aggiornato alle
**12:45:10** con `DisabledStrategies = [PT2_FDAX_PCH_001_240, PT2_NQ_PCH_001_240]`.
La sessione cBot nasce alle **12:43:12** — 17 secondi dopo la creazione, prima dell'update — e
infatti opera `PT2_NQ_PCH_001_240` (384 intent, 157 riempiti). Il backtest interno parte alle
**12:46:15**, dopo l'update, e la dichiara in `strategiesDisabledByPlan`. Quella strategia perde
42.760 $/contratto ed è proprio quella che il piano ha poi spento.

**Quota 3 — `PT2_NQ_PCH_002_30`, l'unico confronto vero.** Interno +52.319, cBot +22.408.

| voce | interno | cBot | Δ |
|---|---:|---:|---:|
| lordo/ctr | 52.951 | 36.328 | −16.623 |
| commissioni/ctr | 632 | 2.569 | −1.937 |
| swap/ctr | 0 | −11.351 | −11.351 |
| **netto/ctr** | **52.319** | **22.408** | **−29.911** |

- **Swap −11.351**: l'interno non modella lo swap, il cBot lo paga su un CFD tenuto overnight
  (`AllowOvernight: true`). Sono 83,5 $/trade/ctr di media sull'intero run cBot — su una strategia
  a 30 minuti che esce spesso per `MaxBars`, vale più della differenza di prezzo.
- **Commissioni −1.937**: 15,9 $/trade/ctr sul cBot contro 4,0 dell'interno.
- **Lordo −16.623**: di questi, **−11.395 stanno sui 117 trade abbinati** (≈ −97/trade ≈ 4,9 punti
  NQ per round trip, dell'ordine dello spread ICS — l'interno ha girato con `spreadSource: nessuno`).
  I restanti −5.228 vengono dall'insieme diverso di trade: 41 solo-interno, 45 solo-cBot.

Dei 117 abbinati, i 73 usciti per stop su entrambi i lati danno **+9.014 a favore del cBot**: gli
stop concordano. Il rosso è concentrato sui 43 dove l'interno esce per `MaxBars` e il cBot per
`LocalExit:Closed` con Δ uscita **0 minuti** — stessa chiusura, stesso istante, −621/trade, cioè
swap e commissioni, non timing.

## Aperto

- **28 ingressi cBot su 156 fuori finestra** su `PT2_NQ_PCH_001_240` (finestra 12:00-16:00, barra
  di segnale etichettata 20:00, ingressi fra le 18:00 e le 21:00 UTC — §3 del report). Sospetto di
  etichettatura del bucket 4h sul bot, ma l'interno ha 0 trade su quella strategia e non c'è
  controparte con cui verificarlo. Va isolato in un confronto dove quella strategia è accesa su
  entrambi i lati, o chiuso guardando il `log.txt` del cBot.
- **Quanto pesa lo swap sul portafoglio buono.** Un CFD con `AllowOvernight` paga lo swap e il
  backtest interno non lo sa. Su `PT2_NQ_PCH_002_30` si mangia il 22% del netto interno. Aperto se
  vada modellato o solo dichiarato.

## Chiuso

- **Non è il moltiplicatore di stop.** `fillConventions.stopMoneyMultiplier = 1` nel summary
  interno, e `PT2_NQ_PCH_002_30` non è in `stopMoneyWidenedStrategies`. Lo stop eseguito è quello
  della ricerca su entrambi i lati.
- **Non è il feed.** Stessa serie `datafeed-external/ICS` per entrambi, `coversRequestedRange: true`
  su tutte e quattro le coppie (simbolo, timeframe), `barsOffGrid: 0`, 0 fill dell'interno su minuti
  senza barra e 0 ingressi cBot su minuti senza barra (§2g del report).
- **Non è il timing degli ingressi.** Sui 117 abbinati: Δ ingresso ≤ 1 min su 71, ≤ 5 min su 93,
  mediana −0,3 min.
- **Non è il timing delle uscite.** 112 abbinati su 117 escono entro 1 minuto l'uno dall'altro.
- **Non è la finestra weekend / holding.** Stessa `holding` su entrambi i lati
  (`sessionFlatUtc 20:45`, weekend 20:45→23:00), 0 chiusure per flat weekend.
- **Non sono gli stop.** Rapporto perdita realizzata / stop dichiarato = 1,00 mediano sull'interno,
  0 trade oltre 1,1; sui 73 abbinati usciti per stop su entrambi i lati il cBot fa *meglio* di 9.014.

## Cosa torna

- Ingresso e uscita di `PT2_NQ_PCH_002_30` coincidono al minuto sulla larga maggioranza dei trade:
  a parità di strategia accesa, i due motori generano e chiudono gli stessi trade.
- Prezzo di ingresso cBot peggiore dell'interno di 0,2 punti mediani sui long (n=117): lo scarto è
  quello atteso da uno spread pagato contro un run senza spread.
- Calendario e griglia: 0 barre fuori griglia, 0 ingressi in giorno saltato su entrambi i lati,
  ancoraggio sessione corretto per simbolo (FDAX 01:00, NQ 00:00).

## Trappole di misura di questa cartella

- **Il piano è cambiato fra i due run.** `plans.json` porta un solo `UpdatedUtc`: la versione che la
  sessione cBot ha letto non è più sul disco. Un confronto è valido solo se piano e sessione non
  sono stati toccati nel mezzo — qui 2 minuti e 15 secondi sono bastati a cambiare il portafoglio.
  Prima di confrontare, verifica `plan.UpdatedUtc` contro `CreatedUtc` di **entrambi** i run.
- **`everEvaluable: false` non lascia traccia in `trades.json`.** Le due strategie FDAX non hanno
  prodotto nulla e negli artefatti del cBot sembrano identiche a una strategia che non ha mai avuto
  un segnale. Solo `session-summary.json` distingue i due casi: qui `historyBarsHighWater = 0` dice
  che mancavano le *barre*, non i segnali.
- **Il netto del cBot include lo swap, quello dell'interno no.** Confrontare i due netti senza prima
  scomporre lordo / commissioni / swap attribuisce al motore una differenza che è di costo.
- **Il divario cambia segno e non è deriva.** La tabella mensile §7 mostra il cBot sotto fin dal
  primo mese: la causa è strutturale (portafogli diversi), non un accumulo progressivo.
