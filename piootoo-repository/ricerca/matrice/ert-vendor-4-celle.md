# ERT sul feed del vendor, 4 celle (lette il 29/09/2026)

Motore ERT (`TrendEfficiencyEngine`, `RC_ERT`): entra quando l'efficiency ratio con segno, scalato per
√N, passa 2 (il trend temporaneo si accende) e esce quando torna a 0,5. Leva `ErBars` 10/20/40/80, stop e
target in decimi di ATR (8/15/25, 0/15/30), uscita alle 21 o no, intraday o un giorno, long/short/entrambi:
324 combinazioni per cella. Feed del **vendor** (future), campione 2008-01 → 2022-01, verifica → 2025-06,
spread e swap FTMO, commissione 0, 250 trade minimi in campione. Lanciate a mano in parallelo ad altri
studi (51, 53, 100 e 120 minuti). CSV `{sim}-{tf}-ert-vendor.csv`, log in `log/matrice-*-ert-vendor.log`.

| cella | ammissibili | OOS in utile ≥ 3/4 | equilibrate | robuste | sopra soglia | verdetto |
|---|---:|---:|---:|---:|---:|---|
| ERT FDAX 4h | 82 | 40 | 2 | 0 | 0 | no |
| ERT NQ 4h | 27 | 6 | 0 | 0 | 0 | no |
| ERT FDAX 1h | 185 | 40 | 33 | **12** | 0 | la sola che risponde, sotto soglia |
| ERT NQ 1h | 24 | 6 | 0 | 0 | 0 | no |

Soglie di average trade (15% del range medio della barra, sul campione 2008-2021): FDAX 4h 267, NQ 4h 150,
FDAX 1h 150, NQ 1h 73.

## Cella per cella

- **FDAX 4h**: tante combinazioni in utile ma di poco; le due equilibrate (ER 10, solo short, un giorno)
  fanno +42-50k in 14 anni su ~290 trade (145-170 a trade contro 267) e cadono sull'outlier. Con ER 40 il
  fuori campione e' −74k in media.
- **NQ 4h**: la migliore in campione fa +19k in 14 anni e perde −61k fuori. A 4 ore il motore entra poco
  (circa 20 trade l'anno): il trend "pulito" con soglia 2 e' raro.
- **NQ 1h**: 24 ammissibili, nessuna equilibrata; la finestra lunga (80) e' la migliore dentro e perde
  fuori (−29/−37k).
- **FDAX 1h**: vedi sotto.

## ERT FDAX 1h

33 equilibrate e **12 robuste**, tutte con **ER 10** e **solo short**; 4 finestre su 4 in utile fuori
campione per la maggior parte. La migliore per UngerFit: ER 10, stop 1,5 ATR, target 1,5 ATR, un giorno,
short: IS +123.140 su 793 trade (DD 30.932), OOS +73.477 su 272 (DD 29.840), 4/4.

**Cosa regge.** Il risultato sta su una regione, non su un punto: ER 10 short e' in utile dentro e fuori
con ogni stop, con e senza uscita alle 21, intraday e un giorno. E' **short** su un DAX che fra il 2008 e
il 2025 e' piu' che triplicato, quindi non e' esposizione al mercato: e' il motore che trova i tratti
di ribasso veloce e li segue. Per la scorrelazione con i piani, quasi tutti long sugli indici, e' il
profilo giusto.

**Cosa non regge.**

- **Average trade 90-155 contro soglia 150**: solo due configurazioni arrivano alla soglia, e di un soffio.
- **UngerFit 0,48-0,72** (serve 1).
- **Sconto walk-forward**: 155 × 0,2-0,5 = 31-78, lontano dalla soglia.
- **P(edge) 12% dentro e 5% fuori** contro il rumore di 324 prove: non si distingue dal migliore di 324
  tentativi a caso.
- ER 20/40/80 perdono fuori campione in media (−12k/−37k): la regione buona e' una sola.

## Decisione

- **Nessuna classe ERT.** A 4 ore e su NQ il motore non ha un edge; su FDAX 1h ne ha uno troppo sottile per
  i costi veri.
- **ERT FDAX 1h short, ER 10** resta l'unico spunto. Se si riapre, non e' con una classe ma con una
  **sweep dentro la cella** (`sweep-cella`), per vedere se una soglia di ingresso diversa da 2 o un'uscita
  di regime diversa da 0,5 alzano l'average trade sopra soglia senza perdere la regione; poi il RAN solo
  short con le stesse uscite. Non ha priorita' sulle 44 celle del resto del catalogo in coda.
