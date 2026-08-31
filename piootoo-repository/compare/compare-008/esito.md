# compare-008 — portafoglio 2024, interno contro sessione ExternalBroker

Le due gambe del confronto registrato come **compare-0007** (2026-08-28): quella
cartella tiene solo il `backtest-summary.json` del run interno, byte per byte identico a
`internal/backtest-summary.json` di qui (stesso `jobId 5ee64c1e…`). Gli eventi cTrader
del run esterno sono l'`.xlsx` nella cartella padre.

## Le due gambe

| | cartella | `Origin` | arco (dai trade) | trade | codici | saldo grezzo |
|---|---|---|---|---|---|---|
| INT | `internal/` | `Internal` | 02/01 → 30/12/2024 | 2.425 | 75 | +618.273 USD |
| EXT | `external/` | `ExternalBroker`, piano `ALL-76`, sessione `762fdf26…` | 02/01 → 08/11/2024 | 1.928 | 70 | −30.519 **EUR non convertiti** |

I saldi grezzi non sono confrontabili così: valuta diversa, cambio che si muove dentro
il run, archi diversi. Vanno ristretti alla finestra comune e convertiti trade per
trade. Dal summary interno: capitale iniziale 1.000.000, `finalEquity` 1.620.522,
max drawdown 10,96%, 902 vinti / 1.523 persi, 3 posizioni aperte alla fine,
`holding` = overnight sì, overweek no, flat di sessione 20:45 UTC, fine settimana
20:45 → 23:00.

## Esito

Il divario, **YM escluso, vale −523.614 USD** e si scompone così:

| causa | USD | quota |
|---|---|---|
| slippage sugli stop, **misurato dagli eventi cTrader** | −186.991 | 36% |
| 111 uscite a tempo/flat in meno × 1.354 | −150.278 | 29% |
| 25 target in meno × 4.072 | −101.801 | 19% |
| resto | −84.544 | 16% |

I conteggi della scomposizione (EXT 1.830 posizioni contro INT 1.932) sono su finestra
comune e su posizioni, non sui trade grezzi della tabella sopra: non aspettarti che
tornino a occhio.

**A — slippage sugli stop: chiuso e calibrato.** 1.315 eventi `Stop Loss Hit` negli
eventi cTrader, **1.277 (97%) riempiono peggio del livello**. Costo per stop: NQ 180 USD
(816 stop, mediana 3,70 punti, p90 20,35), FDAX 136 (il 20,4% della distanza di stop),
ES 95, GC 90, YM 66, PL 65, CL 46, NG 27 — l'88% del divario unitario sulle uscite
protettive. Il modello da implementare è una costante per simbolo sul riempimento degli
stop; per riprodurre anche la coda serve la distribuzione, non la media.

## Aperto

**B — 102 posizioni che l'esterno non apre.** Le mancanti sono i vincenti: protettive
**+34**, tempo/flat **−111**, target **−25**; se fossero gli stessi trade con esito
peggiore le protettive salirebbero di ~136. Lato ingressi, dagli eventi cTrader: 58.351
ordini creati, 56.434 cancellati, **1.916 riempiti, il 3,3%**. Nel log del bot, **3.401
ingressi distinti rifiutati** perché il livello era già superato
(`RejectWrongSideLevels`), 19 al giorno su 179 giorni, concentrati su `NQ_TFU_002_15`
(369), `NQ_TFU_003_15` (252), `NQ_TFM_007_30` (249), `NQ_TFU_001_15` (241),
`ES_SBO_001_15` (198), `GC_TFU_001_30` (186).

**La misura che manca**: `WrongSideLevelsRejected` del run interno, dentro il
`backtest-summary.json`. L'engine ha lo stesso filtro acceso di default, ma decide su
`bar.Open` mentre il bot decide su Bid/Ask live.

## Cosa torna e non va più indagato

Orologi allineati (shift 0 batte ±1h e la variante DST); flat del venerdì alle 20:45 da
entrambe le parti; flat giornaliero a mezzanotte di Roma; distanze di stop e target
identiche; **size 1:1** (l'esterno *non* gira a 1/10); nessuna inversione di segno; il
target rende uguale per trade (+4.096 contro +4.072).

## Trappole di misura di questa cartella — non rifarle

- **Il classificatore per prezzo distorce le famiglie.** `LocalExit:StopLoss` di cAlgo
  significa "un qualsiasi stop protettivo è stato colpito": va confrontato con
  `StopLoss + TrailingStop + BreakEven` interni **insieme**. Confrontando solo le uscite
  esterne che somigliano allo stop originale nascono 247 trade "senza controparte" che
  sono un artefatto, e da lì venivano sia il "63% del gap sono perdite oltre lo stop"
  sia il "trailing 5× meglio sull'esterno": entrambi falsi.
- **"Una perdita di 13,6× lo stop non è ricostruibile da OHLC a 15 minuti": falso.** 312
  perdite su 313 stanno dentro il range delle barre che l'engine ha già; in mediana la
  perdita è il 42% del range. I dati ci sono, manca il modello di fill.
- **Slippage come "distanza realizzata EXT meno distanza realizzata INT"**: mescola
  l'esecuzione con il fatto che i due stop stanno a livelli assoluti diversi. Misurarlo
  contro la distanza **dichiarata**, o dagli eventi cTrader (`SL` contro `Prezzo di
  chiusura`).
- **Sommare solo le righe in perdita** di una tabella causale × causale sovrastima di
  4-5 volte.

Report delle iterazioni precedenti sullo stesso materiale: compare-0004
https://claude.ai/code/artifact/946ef9c5-f272-4166-9976-55f08bc43905 · compare-0005
https://claude.ai/code/artifact/fe4d7737-0606-4298-9218-179031d16d18
