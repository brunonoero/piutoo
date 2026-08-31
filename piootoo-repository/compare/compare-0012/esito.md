# compare-0012 — il portafoglio intero, interno-futures contro cBot

Analizzato il 2026-08-31. Report:
https://claude.ai/code/artifact/29168764-8454-457f-9953-2e2e42755c61

## Le due gambe

| | file | tipo | motore / prezzi | arco |
|---|---|---|---|---|
| INT | `trades-interno-futures.json` | `interno-futures` | `PiootooTradingService` su `datafeed/` (`@SYM` retro-aggiustati) | 17/06 → 30/12/2024 |
| EXT | `trades-cbot-cfd-ICS.json` | `cbot-cfd-ICS` | backtest del cBot in cTrader, sessione `b35ccef7…`, conto 1075035 (ICS) | 01/07 → 13/11/2024 |

Rinominati il 2026-08-31 secondo la convenzione dei tre tipi (`../README.md`).
**L'esterno è il backtest del cBot, non un run interno su feed di broker**: qui feed e
motore cambiano *insieme*, si misura la somma e non i due addendi. La gamba
`interno-cfd` non è ancora stata girata e non lo sarà finché manca l'archivio ICS sotto
`datafeed-external/` — l'unico presente è RAWTRADINGLTD, che è un'altra serie di prezzi.

## Esito

Finestra comune **2024-07-01 → 2024-11-13**: INT 992 trade / 128.385 USD contro EXT 968
/ 103.443 USD convertiti trade per trade. **Divario 24.942 USD, il 19,4%** — ma il segno
del gap mensile cambia tre volte (da −22.994 a +23.942): non è deriva sistematica.

Somiglianza alta: 778 coppie (78%), stessa famiglia d'uscita nel **97%**, target al
tick, stop alla stessa distanza, size 1:1. **NQ, metà del portafoglio, torna a 446 USD
su 68.000.** Il divario è tutto in coda: FDAX +21.861, YM +8.462, NG +7.796, contro ES
−9.968.

Scomposizione sulle 19.581 coppie, per famiglia d'uscita:

| famiglia | USD | lettura |
|---|---|---|
| BreakEven | −22.226 | 43 coppie, −517 l'una: l'interno chiude a pareggio, l'esterno resta dentro e arriva al target |
| TrailingStop | +13.451 | lo stesso modello di gestione disallineato, visto dall'altro lato: saldo netto −8.775 |
| TakeProfit | +11.682 | fra coppie che escono a target da entrambe le parti il saldo è −2.231: gli 11.682 sono cinque cammini che non toccano il target |
| StopLoss | +4.541 | |

**Lo slittamento sugli stop è crollato**: 405 coppie stop→stop, 7.542 USD in tutto; su
NQ mediana 0,51 punti, media 1,32, p90 2,92 = **26 USD a stop contro i 180 di
compare-0007**. In questo run l'interno riempie lo stop esattamente al livello
dichiarato (407 su 408): il modello di slittamento non era acceso.

Spaiati 214 INT / 190 EXT, ma **109 dei 214 hanno un solo-EXT di stessa strategia e lato
entro 24 ore** (mediana 39 minuti): è lo stesso trade a orario diverso, non un ingresso
perso.

## Aperto

- La gestione dinamica (break-even + trailing) è il divario residuo: è un solo modello
  disallineato che si presenta con due segni opposti.
- `ES_BSW_003_15` (19 contro 5) e `YM_BIA_001_240` (27 contro 6) l'esterno quasi non li
  prende; `FDAX_VBO_001_240` e `FDAX_MAC_001_240` non li prende affatto.
- Il confronto pulito richiede la gamba `interno-cfd-ICS`, cioè l'archivio di barre ICS.

## Trappole di misura di questa cartella

L'esterno è in **EUR** e il cambio implicito **si muove dentro il run** (0,899 → 0,943
fra luglio e novembre): va convertito trade per trade, una costante inventa migliaia di
dollari di divario. Il cambio implicito si ricava da `grossProfit / punti /
valore-punto-USD`.
