# 11 mercati su cTrader — specifica di implementazione

*Generato il 25 agosto 2026 da `run_20260814_1453, run_20260815_1021, run_20260819_0201, run_20260819_0659, run_20260819_1008, run_20260820_0012, run_20260820_0856, run_20260822_0403, run_20260822_0736, run_20260822_1249, run_20260823_0343, run_20260823_1535, run_20260824_1500, run_20260824_1550, run_20260824_1642, run_20260824_1847, run_20260824_1908, run_20260824_1935, run_20260824_2020, run_20260824_2133, run_20260824_2232`.*

**75 strategie univoche** da implementare come cBot, ricavate da 5 timeframe (15m, 1h, 30m, 4h, day). Ogni strategia è definita qui per intero: condizioni di entrata, filtri, uscite, e la lista trade con cui verificare il port. Non serve conoscere il trading per implementarle — serve rispettare le regole della sezione 2 alla lettera.

> **Le tre cose che fanno fallire un port.** In ordine di frequenza: le sessioni ricostruite male (§2.1), l'ordine lasciato vivo più di una barra (§2.2), e il backtest fatto su barre invece che su tick (§5).

---

## 1. Cosa si costruisce

75 cBot indipendenti. Ognuno opera su un solo strumento e un solo timeframe, con un contratto per posizione. Non comunicano fra loro.

| ID | TF | Motore | Atteso/trade | P&L OOS | Drawdown | Trade | Equivalenti |
|---|---|---|---|---|---|---|---|
| [S01](#s01) | day | TF_M | $3,809 | $121,877 | $18,840 | 32 | — |
| [S02](#s02) | day | BO | $2,720 | $129,980 | $29,444 | 45 | day fam02-3 |
| [S03](#s03) | day | TF_M | $1,898 | $102,474 | $21,781 | 54 | day fam02-2 |
| [S04](#s04) | 4h | MAC | $1,543 | $99,091 | $17,444 | 46 | — |
| [S05](#s05) | day | TF_U | $1,477 | $223,041 | $23,721 | 151 | day fam04-2 |
| [S06](#s06) | 4h | TF_M | $1,193 | $147,874 | $17,341 | 124 | 4h fam01-2 |
| [S07](#s07) | day | TF_U | $1,102 | $109,104 | $17,572 | 99 | — |
| [S08](#s08) | 4h | PC | $965 | $330,157 | $15,704 | 342 | 4h fam02-2, 4h fam02-3, 4h fam02-5, 4h fam02-6, 4h fam02-7, 4h fam02-8, 4h fam02-9 |
| [S09](#s09) | 30m | TF_U | $889 | $176,500 | $20,054 | 150 | — |
| [S10](#s10) | 4h | VBO | $840 | $337,481 | $11,654 | 402 | — |
| [S11](#s11) | day | VBO | $833 | $86,962 | $23,211 | 52 | — |
| [S12](#s12) | 4h | BO | $811 | $87,321 | $25,308 | 51 | 4h fam01-2 |
| [S13](#s13) | 4h | TF_M | $781 | $114,096 | $25,482 | 146 | 4h fam02-2, 4h fam02-3 |
| [S14](#s14) | 30m | TF_M | $658 | $128,300 | $22,405 | 195 | — |
| [S15](#s15) | 4h | TF_M | $642 | $64,398 | $19,692 | 62 | — |
| [S16](#s16) | 4h | PC | $588 | $272,875 | $25,340 | 305 | 4h fam01-2 |
| [S17](#s17) | 1h | TF_M | $583 | $175,532 | $5,774 | 287 | — |
| [S18](#s18) | 4h | BO | $571 | $94,126 | $28,958 | 78 | 4h fam02-2 |
| [S19](#s19) | 4h | BO | $562 | $201,461 | $9,839 | 341 | — |
| [S20](#s20) | 30m | TF_M | $464 | $113,244 | $28,296 | 244 | — |
| [S21](#s21) | 15m | TF_M | $436 | $136,408 | $25,934 | 153 | — |
| [S22](#s22) | 1h | TF_M | $422 | $49,592 | $20,581 | 112 | — |
| [S23](#s23) | 1h | BIASW | $390 | $51,386 | $24,881 | 88 | 15m fam02 |
| [S24](#s24) | 30m | TF_M | $335 | $57,321 | $6,123 | 171 | — |
| [S25](#s25) | 15m | TF_M | $324 | $127,683 | $24,911 | 193 | 15m fam02-2 |
| [S26](#s26) | day | PC | $306 | $50,510 | $12,438 | 57 | day fam01-2 |
| [S27](#s27) | 1h | TF_M | $303 | $15,958 | $3,880 | 26 | — |
| [S28](#s28) | 15m | TF_M | $287 | $92,523 | $27,128 | 158 | — |
| [S29](#s29) | 1h | BO | $283 | $39,469 | $18,908 | 59 | — |
| [S30](#s30) | 4h | BO | $277 | $64,344 | $12,392 | 124 | — |
| [S31](#s31) | 15m | BO | $268 | $90,062 | $27,908 | 72 | — |
| [S32](#s32) | 15m | BO | $261 | $95,754 | $28,286 | 124 | — |
| [S33](#s33) | 15m | BO | $251 | $71,987 | $26,043 | 97 | — |
| [S34](#s34) | 1h | TF_U | $250 | $203,832 | $22,478 | 232 | — |
| [S35](#s35) | 1h | TF_M | $242 | $45,941 | $15,665 | 181 | — |
| [S36](#s36) | 4h | TF_M | $239 | $44,870 | $9,579 | 55 | 4h fam01-2 |
| [S37](#s37) | day | TF_M | $223 | $83,746 | $13,541 | 76 | day fam02-2, day fam02-3 |
| [S38](#s38) | 1h | PC | $215 | $60,204 | $19,202 | 81 | 1h fam01-2, 1h fam01-3 |
| [S39](#s39) | 30m | PC | $202 | $151,529 | $27,120 | 279 | — |
| [S40](#s40) | 30m | MAC | $199 | $39,344 | $17,164 | 186 | — |
| [S41](#s41) | 15m | BO | $198 | $70,891 | $21,070 | 121 | — |
| [S42](#s42) | 15m | BIASW | $193 | $57,538 | $15,424 | 81 | — |
| [S43](#s43) | 1h | PC | $181 | $46,003 | $8,602 | 93 | 1h fam02-2 |
| [S44](#s44) | 4h | TF_M | $180 | $117,630 | $17,718 | 215 | — |
| [S45](#s45) | day | TF_U | $176 | $122,200 | $23,176 | 153 | — |
| [S46](#s46) | 4h | TF_M | $162 | $77,981 | $15,059 | 141 | — |
| [S47](#s47) | 4h | TF_U | $160 | $144,102 | $11,156 | 222 | 4h fam04-2 |
| [S48](#s48) | 15m | TF_U | $156 | $85,485 | $29,837 | 155 | — |
| [S49](#s49) | 15m | BIASW | $148 | $93,316 | $20,886 | 171 | — |
| [S50](#s50) | 4h | TF_U | $146 | $96,168 | $20,924 | 197 | — |
| [S51](#s51) | 4h | PC | $145 | $93,240 | $10,886 | 410 | 4h fam02-2 |
| [S52](#s52) | 1h | RHL | $140 | $31,320 | $11,584 | 75 | — |
| [S53](#s53) | 30m | PC | $140 | $67,233 | $16,660 | 178 | 30m fam05-2, 30m fam05-3 |
| [S54](#s54) | day | TF_M | $132 | $105,327 | $18,502 | 162 | — |
| [S55](#s55) | 4h | VBO | $117 | $46,272 | $18,311 | 119 | — |
| [S56](#s56) | 4h | TF_U | $117 | $36,246 | $11,957 | 67 | — |
| [S57](#s57) | 4h | PC | $115 | $54,265 | $24,860 | 240 | — |
| [S58](#s58) | 4h | BIAS | $115 | $32,798 | $11,355 | 238 | — |
| [S59](#s59) | 4h | PC | $111 | $68,458 | $17,849 | 83 | 4h fam06-2 |
| [S60](#s60) | 15m | RBB_M | $110 | $104,240 | $27,622 | 520 | — |
| [S61](#s61) | 15m | TF_M | $104 | $5,629 | $2,341 | 33 | — |
| [S62](#s62) | 4h | PC | $102 | $53,332 | $16,988 | 333 | 4h fam03-2, 4h fam03-3 |
| [S63](#s63) | 4h | TF_U | $98 | $36,111 | $9,809 | 79 | — |
| [S64](#s64) | 4h | TF_U | $93 | $74,436 | $20,766 | 239 | — |
| [S65](#s65) | 1h | RHL | $92 | $21,820 | $7,516 | 80 | — |
| [S66](#s66) | 4h | TF_M | $92 | $66,422 | $21,471 | 212 | — |
| [S67](#s67) | 1h | TF_U | $91 | $68,525 | $24,928 | 215 | — |
| [S68](#s68) | 4h | TF_M | $90 | $94,460 | $7,560 | 345 | — |
| [S69](#s69) | 15m | TF_U | $86 | $91,035 | $29,723 | 300 | — |
| [S70](#s70) | 4h | PC | $82 | $100,640 | $24,969 | 165 | — |
| [S71](#s71) | 1h | PC | $77 | $34,473 | $15,623 | 163 | — |
| [S72](#s72) | 4h | BO | $71 | $114,200 | $17,007 | 305 | — |
| [S73](#s73) | 4h | BO | $63 | $57,433 | $6,102 | 173 | — |
| [S74](#s74) | 4h | TF_M | $52 | $15,985 | $2,613 | 115 | — |
| [S75](#s75) | 15m | TF_U | $50 | $50,526 | $21,924 | 286 | — |

La colonna **Equivalenti** elenca le strategie che emettono gli stessi ordini di entrata: trovate separatamente, ma sono lo stesso sistema. Se ne implementa una sola.

### Da dove vengono i numeri

| Timeframe | Righe approvate | Strategie | Univoche |
|---|---|---|---|
| 15m | 133 | 11 | 14 |
| 1h | 6 | 6 | 13 |
| 4h | 7 | 7 | 31 |
| 30m | 24 | 7 | 7 |
| day | 9 | 9 | 10 |

Le **righe approvate** includono la stessa strategia con stop e target diversi: non sono sistemi distinti e non compaiono qui. Le **univoche** restano dopo aver confrontato le entrate anche *fra* timeframe diversi.

---

## 2. Fondamenta comuni

Questa parte si scrive una volta e si riusa in tutti i 75 cBot. Ogni regola è vincolante: cambiarne una fa divergere il port dalla ricerca.

### 2.1 Sessioni

Le condizioni usano massimi e minimi di **sessione**, non di barra. Le sessioni si ricostruiscono dalle barre intraday, **non** si leggono dalle candele giornaliere del broker.

- Una sessione inizia all'ora indicata per quello strumento in §2.4 e dura fino all'inizio della successiva.
- `H_d1`, `L_d1`, `O_d1`, `C_d1` = massimo, minimo, apertura e chiusura della sessione **precedente**.
- `H_d2` … `H_d5` = le quattro sessioni ancora prima.
- `H_d0`, `L_d0`, `O_d0` = massimo, minimo e apertura della sessione **corrente**, sulle sole barre già **chiuse**.
- `HH5` = massimo di `H_d1..H_d5`; `LL5` = minimo di `L_d1..L_d5`.
- `close` (minuscolo) = chiusura della **barra** corrente, non della sessione.

### 2.2 Ciclo di vita dell'ordine

- Le condizioni si valutano **alla chiusura della barra**.
- L'ordine emesso vive **una sola barra**: alla barra successiva va cancellato e, se le condizioni reggono, ri-emesso. In cTrader: *cancel & replace* ad ogni `OnBar`.
- Nessuna condizione può usare il prezzo della barra su cui si entra.
- Al massimo **una entrata per sessione e per direzione**. Se la posizione si chiude dentro la stessa sessione, non si rientra sullo stesso lato.

### 2.3 Riempimento

- **Ordine stop** (rottura di un livello): riempie a `max(apertura, livello)` per il long, `min(apertura, livello)` per lo short. Se il prezzo apre già oltre il livello, il fill è all'apertura — mai al livello superato.
- **Ordine limite** (ritorno alla media): serve una penetrazione **stretta** del livello (`minimo < livello` per il long). Il semplice tocco non riempie.

### 2.4 Costi e unità per strumento

La ricerca esprime stop e target in **dollari per contratto**; cTrader li vuole in **punti**. Nelle schede i valori sono già convertiti — questa tabella serve per verificarli.

| Strumento | Sessione | 1 punto | 1 tick | Commissione | Slippage |
|---|---|---|---|---|---|
| **BP** | 00:00 CET | $62,500 | 0.0001 punti | $5.00 | 1 tick/lato |
| **BTC** | 00:00 CET | $5 | 5 punti | $15.00 | 1 tick/lato |
| **CL** | 00:00 CET | $1,000 | 0.01 punti | $6.00 | 1 tick/lato |
| **ES** | 00:00 CET | $50 | 0.25 punti | $4.00 | 1 tick/lato |
| **FDAX** | 01:00 CET | $25 | 1 punti | $4.00 | 1 tick/lato |
| **GC** | 00:00 CET | $100 | 0.1 punti | $6.00 | 1 tick/lato |
| **JY** | 00:00 CET | $125,000 | 5e-05 punti | $5.00 | 1 tick/lato |
| **NG** | 00:00 CET | $10,000 | 0.001 punti | $6.00 | 1 tick/lato |
| **NQ** | 00:00 CET | $20 | 0.25 punti | $4.00 | 1 tick/lato |
| **PL** | 00:00 CET | $50 | 0.1 punti | $6.00 | 1 tick/lato |
| **YM** | 00:00 CET | $5 | 1 punti | $4.00 | 1 tick/lato |

⚠ Un solo valore sbagliato in questa tabella e gli stop del cBot sono sbagliati di conseguenza: sono i numeri che convertono i dollari della ricerca nei punti del broker.

### 2.6 Orari

Le finestre orarie sono in **ora dei dati (CET)** e si valutano sulla chiusura della barra. Una finestra il cui orario di fine è minore di quello di inizio attraversa la mezzanotte e va gestita come tale.

---

## 3. Le condizioni di pattern

I filtri sono condizioni booleane sulle grandezze di sessione della §2.1. Qui tutte quelle usate dalle 75 strategie, già in forma di formula.

| Riferimento | Condizione |
|---|---|
| `` | `il motore entra su ogni segnale strutturale` |
| `direzionale -1` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25` |
| `direzionale -1` | `O_d0 - L_d0 > (O_d1 - L_d1) * 0.25` |
| `direzionale -10` | `(C_d1 < C_d2) E (C_d2 < C_d3) E (C_d3 < C_d4)` |
| `direzionale -10` | `(C_d1 > C_d2) E (C_d2 > C_d3) E (C_d3 > C_d4)` |
| `direzionale -14` | `C_d1 < O_d1` |
| `direzionale -14` | `C_d1 > O_d1` |
| `direzionale -2` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5` |
| `direzionale -2` | `O_d0 - L_d0 > (O_d1 - L_d1) * 0.5` |
| `direzionale -21` | `H_d0 > H_d1` |
| `direzionale -21` | `L_d0 < L_d1` |
| `direzionale -3` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.75` |
| `direzionale -3` | `O_d0 - L_d0 > (O_d1 - L_d1) * 0.75` |
| `direzionale -33` | `H_d1 > H_d5` |
| `direzionale -33` | `L_d1 < L_d5` |
| `direzionale -34` | `H_d1 < H_d5` |
| `direzionale -34` | `L_d1 > L_d5` |
| `direzionale -35` | `(H_d1 > H_d2) E (H_d1 > H_d3) E (H_d1 > H_d4)` |
| `direzionale -35` | `(L_d1 < L_d2) E (L_d1 < L_d3) E (L_d1 < L_d4)` |
| `direzionale -36` | `(H_d1 < H_d2) E (H_d1 < H_d3) E (H_d1 < H_d4)` |
| `direzionale -36` | `(L_d1 > L_d2) E (L_d1 > L_d3) E (L_d1 > L_d4)` |
| `direzionale -45` | `(C_d1 < O_d1) E (C_d2 < O_d2)` |
| `direzionale -45` | `(C_d1 > O_d1) E (C_d2 > O_d2)` |
| `direzionale -48` | `close < O_d0 * 1.005` |
| `direzionale -48` | `close > O_d0 * 0.995` |
| `direzionale -5` | `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5` |
| `direzionale -5` | `O_d0 - L_d0 > (O_d1 - L_d1) * 1.5` |
| `direzionale -9` | `H_d0 - O_d0 < H_d1 - O_d1` |
| `direzionale -9` | `O_d0 - L_d0 < O_d1 - L_d1` |
| `direzionale 1` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25` |
| `direzionale 1` | `O_d0 - L_d0 > (O_d1 - L_d1) * 0.25` |
| `direzionale 12` | `(H_d1 < H_d2) E (L_d1 < L_d2)` |
| `direzionale 12` | `(H_d1 > H_d2) E (L_d1 > L_d2)` |
| `direzionale 13` | `C_d1 < C_d2` |
| `direzionale 13` | `C_d1 > C_d2` |
| `direzionale 15` | `C_d1 < C_d2 * (1 - 0.005)` |
| `direzionale 15` | `C_d1 > C_d2 * (1 + 0.005)` |
| `direzionale 16` | `C_d1 < C_d2 * (1 - 0.01)` |
| `direzionale 16` | `C_d1 > C_d2 * (1 + 0.01)` |
| `direzionale 17` | `C_d1 < C_d2 * (1 - 0.015)` |
| `direzionale 17` | `C_d1 > C_d2 * (1 + 0.015)` |
| `direzionale 2` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5` |
| `direzionale 2` | `O_d0 - L_d0 > (O_d1 - L_d1) * 0.5` |
| `direzionale 21` | `H_d0 > H_d1` |
| `direzionale 21` | `L_d0 < L_d1` |
| `direzionale 28` | `H_d0 < H_d1 * (1 - 0.005)` |
| `direzionale 28` | `L_d0 > L_d1 * (1 + 0.005)` |
| `direzionale 29` | `H_d0 < H_d1 * (1 - 0.01)` |
| `direzionale 29` | `L_d0 > L_d1 * (1 + 0.01)` |
| `direzionale 30` | `H_d0 < H_d1 * (1 - 0.015)` |
| `direzionale 30` | `L_d0 > L_d1 * (1 + 0.015)` |
| `direzionale 31` | `H_d0 < H_d1 * (1 - 0.02)` |
| `direzionale 31` | `L_d0 > L_d1 * (1 + 0.02)` |
| `direzionale 33` | `H_d1 > H_d5` |
| `direzionale 33` | `L_d1 < L_d5` |
| `direzionale 34` | `H_d1 < H_d5` |
| `direzionale 34` | `L_d1 > L_d5` |
| `direzionale 35` | `(H_d1 > H_d2) E (H_d1 > H_d3) E (H_d1 > H_d4)` |
| `direzionale 35` | `(L_d1 < L_d2) E (L_d1 < L_d3) E (L_d1 < L_d4)` |
| `direzionale 37` | `(C_d1 < C_d2) E (C_d2 < C_d3) E (O_d0 < C_d1)` |
| `direzionale 37` | `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)` |
| `direzionale 38` | `C_d1 - L_d1 < 0.2 * (H_d1-L_d1)` |
| `direzionale 38` | `H_d1 - C_d1 < 0.2 * (H_d1-L_d1)` |
| `direzionale 39` | `O_d0 < L_d1` |
| `direzionale 39` | `O_d0 > H_d1` |
| `direzionale 44` | `H_d1 < H_d2` |
| `direzionale 44` | `L_d1 > L_d2` |
| `direzionale 45` | `(C_d1 < O_d1) E (C_d2 < O_d2)` |
| `direzionale 45` | `(C_d1 > O_d1) E (C_d2 > O_d2)` |
| `direzionale 46` | `(C_d1 < O_d1) E (C_d2 > O_d2)` |
| `direzionale 46` | `(C_d1 > O_d1) E (C_d2 < O_d2)` |
| `direzionale 47` | `close < O_d0 * 1.01` |
| `direzionale 47` | `close > O_d0 * 0.99` |
| `direzionale 48` | `close < O_d0 * 1.005` |
| `direzionale 48` | `close > O_d0 * 0.995` |
| `direzionale 49` | `close < O_d0` |
| `direzionale 49` | `close > O_d0` |
| `direzionale 50` | `close < O_d0 * 0.995` |
| `direzionale 50` | `close > O_d0 * 1.005` |
| `direzionale 51` | `close < O_d0 * 0.99` |
| `direzionale 51` | `close > O_d0 * 1.01` |
| `direzionale 6` | `H_d0 - O_d0 > (H_d1 - O_d1) * 2.0` |
| `direzionale 6` | `O_d0 - L_d0 > (O_d1 - L_d1) * 2.0` |
| `direzionale 7` | `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5` |
| `direzionale 7` | `O_d0 - L_d0 > (O_d1 - L_d1) * 2.5` |
| `direzionale 8` | `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0` |
| `direzionale 8` | `O_d0 - L_d0 > (O_d1 - L_d1) * 3.0` |
| `direzionale 9` | `H_d0 - O_d0 < H_d1 - O_d1` |
| `direzionale 9` | `O_d0 - L_d0 < O_d1 - L_d1` |
| `fast 1` | `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)` |
| `fast 100` | `L_d0 > L_d1` |
| `fast 101` | `L_d0 > L_d1 * (1 + 0.005)` |
| `fast 106` | `L_d1 < L_d5` |
| `fast 107` | `L_d1 > L_d5` |
| `fast 112` | `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)` |
| `fast 114` | `H_d1 - C_d1 < 0.2 * (H_d1-L_d1)` |
| `fast 115` | `C_d1 - L_d1 < 0.2 * (H_d1-L_d1)` |
| `fast 128` | `(H_d1-L_d1) < (H_d2 - L_d2 + H_d3 - L_d3) / 3` |
| `fast 130` | `(H_d2 > H_d1) E (L_d2 < L_d1)` |
| `fast 132` | `L_d1 > L_d2` |
| `fast 137` | `(C_d1 < O_d1) E (C_d2 > O_d2)` |
| `fast 139` | `(C_d1 < O_d1) E (C_d2 < O_d2)` |
| `fast 15` | `|O_d5-C_d1| < 2.0 * (H_d5-L_d1)` |
| `fast 150` | `close < O_d0 * 0.995` |
| `fast 2` | `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)` |
| `fast 21` | `|O_d5-C_d1| > 2.0 * (H_d5-L_d1)` |
| `fast 23` | `|O_d5-C_d1| < 0.1 * (HH5-LL5)` |
| `fast 25` | `|O_d5-C_d1| < 0.5 * (HH5-LL5)` |
| `fast 28` | `|O_d5-C_d1| > 0.25 * (HH5-LL5)` |
| `fast 31` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25` |
| `fast 32` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5` |
| `fast 34` | `H_d0 - O_d0 > (H_d1 - O_d1) * 1.0` |
| `fast 37` | `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5` |
| `fast 38` | `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0` |
| `fast 39` | `H_d0 - O_d0 < H_d1 - O_d1` |
| `fast 4` | `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)` |
| `fast 51` | `(H_d1 > H_d2) E (L_d1 > L_d2)` |
| `fast 52` | `(H_d1 < H_d2) E (L_d1 < L_d2)` |
| `fast 53` | `H_d0 > L_d0 * (1 + 0.005)` |
| `fast 58` | `H_d0 > L_d0 * (1 + 0.025)` |
| `fast 6` | `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)` |
| `fast 62` | `H_d0 < L_d0 * (1 + 0.01)` |
| `fast 63` | `H_d0 < L_d0 * (1 + 0.015)` |
| `fast 65` | `H_d0 < L_d0 * (1 + 0.025)` |
| `fast 67` | `C_d1 > C_d2` |
| `fast 7` | `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)` |
| `fast 73` | `C_d1 < C_d2 * (1 - 0.015)` |
| `fast 77` | `C_d1 > C_d2 * (1 + 0.005)` |
| `fast 79` | `C_d1 > C_d2 * (1 + 0.015)` |
| `fast 82` | `H_d0 > H_d1 * (1 + 0.0025)` |
| `fast 83` | `H_d0 > H_d1 * (1 + 0.005)` |
| `fast 85` | `H_d0 > H_d1 * (1 + 0.01)` |
| `fast 93` | `H_d1 > H_d5` |
| `fast 94` | `H_d1 < H_d5` |
| `neutrale 1` | `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)` |
| `neutrale 11` | `|O_d5-C_d1| < 0.5 * (H_d5-L_d1)` |
| `neutrale 12` | `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)` |
| `neutrale 13` | `|O_d5-C_d1| < 1.0 * (H_d5-L_d1)` |
| `neutrale 14` | `|O_d5-C_d1| < 1.5 * (H_d5-L_d1)` |
| `neutrale 15` | `|O_d5-C_d1| < 2.0 * (H_d5-L_d1)` |
| `neutrale 18` | `|O_d5-C_d1| > 0.75 * (H_d5-L_d1)` |
| `neutrale 19` | `|O_d5-C_d1| > 1.0 * (H_d5-L_d1)` |
| `neutrale 2` | `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)` |
| `neutrale 20` | `|O_d5-C_d1| > 1.5 * (H_d5-L_d1)` |
| `neutrale 23` | `|O_d5-C_d1| < 0.1 * (HH5-LL5)` |
| `neutrale 24` | `|O_d5-C_d1| < 0.25 * (HH5-LL5)` |
| `neutrale 25` | `|O_d5-C_d1| < 0.5 * (HH5-LL5)` |
| `neutrale 27` | `|O_d5-C_d1| > 0.9 * (HH5-LL5)` |
| `neutrale 28` | `|O_d5-C_d1| > 0.25 * (HH5-LL5)` |
| `neutrale 29` | `|O_d5-C_d1| > 0.5 * (HH5-LL5)` |
| `neutrale 3` | `|O_d1-C_d1| < 0.5 * (H_d1-L_d1)` |
| `neutrale 30` | `|O_d5-C_d1| > 0.75 * (HH5-LL5)` |
| `neutrale 31` | `(H_d0-L_d0) > L_d0 * 0.005` |
| `neutrale 32` | `(H_d0-L_d0) > L_d0 * 0.0075` |
| `neutrale 33` | `(H_d0-L_d0) > L_d0 * 0.01` |
| `neutrale 34` | `(H_d0-L_d0) > L_d0 * 0.015` |
| `neutrale 35` | `(H_d0-L_d0) > L_d0 * 0.02` |
| `neutrale 37` | `(H_d0-L_d0) > L_d0 * 0.03` |
| `neutrale 38` | `(H_d0-L_d0) < L_d0 * 0.005` |
| `neutrale 39` | `(H_d0-L_d0) < L_d0 * 0.0075` |
| `neutrale 4` | `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)` |
| `neutrale 45` | `(O_d0 < L_d1) O (O_d0 > H_d1)` |
| `neutrale 46` | `(H_d0 < H_d1) E (L_d0 > L_d1)` |
| `neutrale 47` | `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2` |
| `neutrale 48` | `((H_d1-L_d1) < (H_d2-L_d2)) E ((H_d2-L_d2) < (H_d3-L_d3))` |
| `neutrale 49` | `(H_d1 < H_d2) E (L_d1 > L_d2)` |
| `neutrale 5` | `|O_d1-C_d1| > 0.25 * (H_d1-L_d1)` |
| `neutrale 52` | `(H_d0 > H_d1) E (L_d0 < L_d1)` |
| `neutrale 53` | `(H_d1-L_d1) < (H_d2-L_d2)` |
| `neutrale 54` | `(H_d1-L_d1) > (H_d2-L_d2)` |
| `neutrale 6` | `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)` |
| `neutrale 7` | `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)` |
| `neutrale 8` | `|O_d1-C_d1| > 0.9 * (H_d1-L_d1)` |
| `neutrale 9` | `|O_d5-C_d1| < 0.1 * (H_d5-L_d1)` |

**deve essere VERO** = condizione requisito per entrare. **deve essere FALSO** = l'entrata è vietata quando la condizione si verifica. Le voci marcate *nessun filtro* sono assenti e non vanno implementate.

---

## 4. Le 75 strategie

### S01 · NQ day · Trend following, simmetrico  <a id='s01'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | day |
| Motore | TF_M |
| Atteso/trade | $3,809 |
| P&L fuori campione | $121,877 |
| Drawdown | $18,840 |
| Trade | 32 |
| Stop loss | 50.0 pt |
| Take profit | 750.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`
- deve essere FALSO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`

*Solo LONG*

- deve essere VERO — direzionale -3: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.75`
- deve essere FALSO — direzionale 45: `(C_d1 > O_d1) E (C_d2 > O_d2)`

*Solo SHORT*

- deve essere VERO — direzionale -3: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.75`
- deve essere FALSO — direzionale 45: `(C_d1 < O_d1) E (C_d2 < O_d2)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **50.00 pt**
- Take profit: **$15,000** = **750.00 pt**
- Uscita a tempo dopo **20 barre** (20.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260822_0736/consegna/trades/fam01_TF_M.csv`

---

### S02 · NQ day · Breakout su N sessioni  <a id='s02'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | day |
| Motore | BO |
| Atteso/trade | $2,720 |
| P&L fuori campione | $129,980 |
| Drawdown | $29,444 |
| Trade | 45 |
| Stop loss | 50.0 pt |
| Take profit | — |

**Ordine STOP sul canale a 1 sessioni**

- LONG: stop buy sul **massimo delle ultime 1 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 1 sessioni complete**

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 28: `|O_d5-C_d1| > 0.25 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale -3: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.75`
- deve essere FALSO — direzionale 6: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.0`

*Solo SHORT*

- deve essere VERO — direzionale -3: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.75`
- deve essere FALSO — direzionale 6: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.0`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **50.00 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **10 barre** (10.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260822_0736/consegna/trades/fam02_BO.csv`

> ⚠ **Non mettere su conti diversi** insieme a `day fam02-3`: emettono gli stessi ordini di entrata.

---

### S03 · NQ day · Trend following, simmetrico  <a id='s03'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | day |
| Motore | TF_M |
| Atteso/trade | $1,898 |
| P&L fuori campione | $102,474 |
| Drawdown | $21,781 |
| Trade | 54 |
| Stop loss | 50.0 pt |
| Take profit | — |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`
- deve essere FALSO — neutrale 35: `(H_d0-L_d0) > L_d0 * 0.02`

*Solo LONG*

- deve essere VERO — direzionale 44: `L_d1 > L_d2`
- deve essere FALSO — direzionale 29: `L_d0 > L_d1 * (1 + 0.01)`

*Solo SHORT*

- deve essere VERO — direzionale 44: `H_d1 < H_d2`
- deve essere FALSO — direzionale 29: `H_d0 < H_d1 * (1 - 0.01)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **50.00 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **10 barre** (10.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260822_0736/consegna/trades/fam03_TF_M.csv`

> ⚠ **Non mettere su conti diversi** insieme a `day fam02-2`: emettono gli stessi ordini di entrata.

---

### S04 · FDAX 4h · Incrocio di medie  <a id='s04'></a>

**SOLO LONG** — Incrocio di due medie mobili, senza pattern.

| | |
|---|---|
| Timeframe | 4h |
| Motore | MAC |
| Atteso/trade | $1,543 |
| P&L fuori campione | $99,091 |
| Drawdown | $17,444 |
| Trade | 46 |
| Stop loss | 120.0 pt |
| Take profit | — |

**Incrocio di medie mobili 5/24**

- Due medie mobili **semplici** sulla close: veloce a **5 barre**, lenta a **24 barre**.
- Segnale LONG: la veloce incrocia **sopra** la lenta. SHORT: incrocia **sotto**.
- Filtro gradiente: su 2 barre la veloce deve essersi mossa, in valore assoluto, almeno **2 volte** quanto la lenta nello stesso tratto.
- Filtro sulla sessione precedente: dev'essere di **indecisione** — `|C_d1 − O_d1| ≤ 0.5 × (H_d1 − L_d1)` — e **verde** (`C_d1 > O_d1`) perché il long operi, **rossa** perché operi lo short.
- Entrata **MARKET all'apertura della barra successiva** al segnale.
- Questo motore **non usa filtri pattern**.
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Nessun filtro pattern*

- —: `il motore entra su ogni segnale strutturale`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- **Nessun limite** al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta

**Uscite**

- Uscita su **incrocio inverso** delle due medie, eseguita sulla barra successiva al segnale.
- Uscita forzata all'**ultima barra del venerdì**: nessuna posizione resta aperta nel fine settimana.
- Sono le uscite principali del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$3,000** per contratto = **120.00 pt**
- Take profit: **nessuno**
- Trailing stop: **$4,000** = **160.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_1500/consegna/trades/fam01_MAC.csv`

---

### S05 · NQ day · Trend following, asimmetrico  <a id='s05'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | day |
| Motore | TF_U |
| Atteso/trade | $1,477 |
| P&L fuori campione | $223,041 |
| Drawdown | $23,721 |
| Trade | 151 |
| Stop loss | 50.0 pt |
| Take profit | 750.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 39: `H_d0 - O_d0 < H_d1 - O_d1`
- deve essere FALSO — fast 85: `H_d0 > H_d1 * (1 + 0.01)`

*Solo SHORT*

- deve essere VERO — fast 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`
- deve essere FALSO — fast 150: `close < O_d0 * 0.995`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **50.00 pt**
- Take profit: **$15,000** = **750.00 pt**
- Uscita a tempo dopo **20 barre** (20.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260822_0736/consegna/trades/fam04_TF_U.csv`

> ⚠ **Non mettere su conti diversi** insieme a `day fam04-2`: emettono gli stessi ordini di entrata.

---

### S06 · NQ 4h · Trend following, simmetrico  <a id='s06'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $1,193 |
| P&L fuori campione | $147,874 |
| Drawdown | $17,341 |
| Trade | 124 |
| Stop loss | 12.5 pt |
| Take profit | — |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 37: `(H_d0-L_d0) > L_d0 * 0.03`

*Solo LONG*

- deve essere VERO — direzionale 12: `(H_d1 > H_d2) E (L_d1 > L_d2)`
- deve essere FALSO — direzionale 16: `C_d1 > C_d2 * (1 + 0.01)`

*Solo SHORT*

- deve essere VERO — direzionale 12: `(H_d1 < H_d2) E (L_d1 < L_d2)`
- deve essere FALSO — direzionale 16: `C_d1 < C_d2 * (1 - 0.01)`

**Quando può operare**

- Opera solo fra **14:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **12.50 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **50 barre** (8.3 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1642/consegna/trades/fam01_TF_M.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam01-2`: emettono gli stessi ordini di entrata.

---

### S07 · NQ day · Trend following, asimmetrico  <a id='s07'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | day |
| Motore | TF_U |
| Atteso/trade | $1,102 |
| P&L fuori campione | $109,104 |
| Drawdown | $17,572 |
| Trade | 99 |
| Stop loss | 50.0 pt |
| Take profit | 300.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 32: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5`
- deve essere FALSO — fast 34: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.0`

*Solo SHORT*

- deve essere VERO — fast 128: `(H_d1-L_d1) < (H_d2 - L_d2 + H_d3 - L_d3) / 3`
- deve essere FALSO — fast 53: `H_d0 > L_d0 * (1 + 0.005)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **50.00 pt**
- Take profit: **$6,000** = **300.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260822_0736/consegna/trades/fam05_TF_U.csv`

---

### S08 · FDAX 4h · Price channel (Donchian)  <a id='s08'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $965 |
| P&L fuori campione | $330,157 |
| Drawdown | $15,704 |
| Trade | 342 |
| Stop loss | 10.0 pt |
| Take profit | 400.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre**
- SHORT: stop sell sul **minimo delle ultime 1 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- Opera solo se l'ATR di sessione a 14 periodi, convertito in dollari, è ≥ **$3,000**

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 8: `|O_d1-C_d1| > 0.9 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 47: `close > O_d0 * 0.99`
- deve essere FALSO — direzionale -5: `O_d0 - L_d0 > (O_d1 - L_d1) * 1.5`

*Solo SHORT*

- deve essere VERO — direzionale 47: `close < O_d0 * 1.01`
- deve essere FALSO — direzionale -5: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5`

**Quando può operare**

- Opera solo fra **07:00 e 13:00**, ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **10.00 pt**
- Take profit: **$10,000** = **400.00 pt**
- Trailing stop: **$2,000** = **80.00 pt**
- Breakeven a **$1,000** = **40.00 pt** di utile
- Uscita a tempo dopo **24 barre** (4.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1500/consegna/trades/fam02_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam02-2`, `4h fam02-3`, `4h fam02-5`, `4h fam02-6`, `4h fam02-7`, `4h fam02-8`, `4h fam02-9`: emettono gli stessi ordini di entrata.

---

### S09 · GC 30m · Trend following, asimmetrico  <a id='s09'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_U |
| Atteso/trade | $889 |
| P&L fuori campione | $176,500 |
| Drawdown | $20,054 |
| Trade | 150 |
| Stop loss | 17.5 pt |
| Take profit | 75.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 34: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.0`
- deve essere FALSO — fast 25: `|O_d5-C_d1| < 0.5 * (HH5-LL5)`

*Solo SHORT*

- deve essere VERO — fast 128: `(H_d1-L_d1) < (H_d2 - L_d2 + H_d3 - L_d3) / 3`
- deve essere FALSO — fast 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

**Quando può operare**

- Opera solo fra **16:00 e 08:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,750** per contratto = **17.50 pt**
- Take profit: **$7,500** = **75.00 pt**
- Uscita a tempo dopo **460 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260819_0201/consegna/trades/fam01_TF_U.csv`

---

### S10 · FDAX 4h · Volatility breakout  <a id='s10'></a>

**LONG + SHORT** — Rottura di un livello costruito sull'apertura più un multiplo di volatilità.

| | |
|---|---|
| Timeframe | 4h |
| Motore | VBO |
| Atteso/trade | $840 |
| P&L fuori campione | $337,481 |
| Drawdown | $11,654 |
| Trade | 402 |
| Stop loss | 10.0 pt |
| Take profit | 300.0 pt |

**Ordine STOP sull'apertura di sessione più un multiplo di volatilità**

- Sia `VOL` = l'**ATR a 500 barre** del timeframe, misurato fino alla barra precedente compresa
- LONG: stop buy a **O_d0 + 0.5 × VOL**
- SHORT: stop sell a **O_d0 − 0.3 × VOL**
- `O_d0` è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 14: `|O_d5-C_d1| < 1.5 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 52: `(H_d0 > H_d1) E (L_d0 < L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -9: `O_d0 - L_d0 < O_d1 - L_d1`

*Solo SHORT*

- deve essere VERO — direzionale -9: `H_d0 - O_d0 < H_d1 - O_d1`

**Quando può operare**

- Opera solo fra **07:00 e 14:00**, ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **10.00 pt**
- Take profit: **$7,500** = **300.00 pt**
- Breakeven a **$500** = **20.00 pt** di utile
- Uscita a tempo dopo **24 barre** (4.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1500/consegna/trades/fam02_VBO.csv`

---

### S11 · NQ day · Volatility breakout  <a id='s11'></a>

**SOLO LONG** — Rottura di un livello costruito sull'apertura più un multiplo di volatilità.

| | |
|---|---|
| Timeframe | day |
| Motore | VBO |
| Atteso/trade | $833 |
| P&L fuori campione | $86,962 |
| Drawdown | $23,211 |
| Trade | 52 |
| Stop loss | 50.0 pt |
| Take profit | 500.0 pt |

**Ordine STOP sull'apertura di sessione più un multiplo di volatilità**

- Sia `VOL` = il **range della sessione precedente**: `H_d1 − L_d1`
- LONG: stop buy a **O_d0 + 1 × VOL**
- SHORT: stop sell a **O_d0 − 1 × VOL**
- `O_d0` è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 19: `|O_d5-C_d1| > 1.0 * (H_d5-L_d1)`

*Solo LONG*

- deve essere FALSO — direzionale 13: `C_d1 > C_d2`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **50.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$4,000** = **200.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260822_0736/consegna/trades/fam06_VBO.csv`

---

### S12 · ES 4h · Breakout su N sessioni  <a id='s12'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 4h |
| Motore | BO |
| Atteso/trade | $811 |
| P&L fuori campione | $87,321 |
| Drawdown | $25,308 |
| Trade | 51 |
| Stop loss | 80.0 pt |
| Take profit | 200.0 pt |

**Ordine STOP sul canale a 3 sessioni**

- LONG: stop buy sul **massimo delle ultime 3 sessioni complete** + 2 tick (0.5 pt)
- SHORT: stop sell sul **minimo delle ultime 3 sessioni complete** − 2 tick (0.5 pt)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -2: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.5`
- deve essere FALSO — direzionale 33: `H_d1 > H_d5`

*Solo SHORT*

- deve essere VERO — direzionale -2: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5`
- deve essere FALSO — direzionale 33: `L_d1 < L_d5`

**Quando può operare**

- Opera solo fra **14:00 e 09:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$10,000** = **200.00 pt**
- Uscita a tempo dopo **48 barre** (8.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1847/consegna/trades/fam01_BO.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam01-2`: emettono gli stessi ordini di entrata.

---

### S13 · NQ 4h · Trend following, simmetrico  <a id='s13'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $781 |
| P&L fuori campione | $114,096 |
| Drawdown | $25,482 |
| Trade | 146 |
| Stop loss | 25.0 pt |
| Take profit | 500.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`

*Solo LONG*

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

**Quando può operare**

- Opera solo fra **18:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_1642/consegna/trades/fam02_TF_M.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam02-2`, `4h fam02-3`: emettono gli stessi ordini di entrata.

---

### S14 · NQ 30m · Trend following, simmetrico  <a id='s14'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_M |
| Atteso/trade | $658 |
| P&L fuori campione | $128,300 |
| Drawdown | $22,405 |
| Trade | 195 |
| Stop loss | 25.0 pt |
| Take profit | 500.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`
- deve essere FALSO — neutrale 33: `(H_d0-L_d0) > L_d0 * 0.01`

*Solo LONG*

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 17: `C_d1 > C_d2 * (1 + 0.015)`

*Solo SHORT*

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 17: `C_d1 < C_d2 * (1 - 0.015)`

**Quando può operare**

- Opera solo fra **14:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Uscita a tempo dopo **460 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260815_1021/consegna/trades/fam01_TF_M.csv`

---

### S15 · GC 4h · Trend following, simmetrico  <a id='s15'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $642 |
| P&L fuori campione | $64,398 |
| Drawdown | $19,692 |
| Trade | 62 |
| Stop loss | 50.0 pt |
| Take profit | 75.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 18: `|O_d5-C_d1| > 0.75 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -1: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.25`
- deve essere FALSO — direzionale 35: `(H_d1 > H_d2) E (H_d1 > H_d3) E (H_d1 > H_d4)`

*Solo SHORT*

- deve essere VERO — direzionale -1: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — direzionale 35: `(L_d1 < L_d2) E (L_d1 < L_d3) E (L_d1 < L_d4)`

**Quando può operare**

- Opera solo fra **10:00 e 13:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$5,000** per contratto = **50.00 pt**
- Take profit: **$7,500** = **75.00 pt**
- Uscita a tempo dopo **48 barre** (8.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1935/consegna/trades/fam01_TF_M.csv`

---

### S16 · BTC 4h · Price channel (Donchian)  <a id='s16'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $588 |
| P&L fuori campione | $272,875 |
| Drawdown | $25,340 |
| Trade | 305 |
| Stop loss | 350.0 pt |
| Take profit | 2,000.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre**
- SHORT: stop sell sul **minimo delle ultime 1 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- Opera solo se l'ATR di sessione a 14 periodi, convertito in dollari, è ≥ **$3,000**

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 45: `(O_d0 < L_d1) O (O_d0 > H_d1)`

*Solo LONG*

- deve essere VERO — direzionale 47: `close > O_d0 * 0.99`
- deve essere FALSO — direzionale 30: `L_d0 > L_d1 * (1 + 0.015)`

*Solo SHORT*

- deve essere VERO — direzionale 47: `close < O_d0 * 1.01`
- deve essere FALSO — direzionale 30: `H_d0 < H_d1 * (1 - 0.015)`

**Quando può operare**

- Opera solo fra **06:00 e 23:59**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,750** per contratto = **350.00 pt**
- Take profit: **$10,000** = **2,000.00 pt**
- Trailing stop: **$2,000** = **400.00 pt**
- Breakeven a **$1,000** = **200.00 pt** di utile
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_2232/consegna/trades/fam01_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam01-2`: emettono gli stessi ordini di entrata.

---

### S17 · NQ 1h · Trend following, simmetrico  <a id='s17'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $583 |
| P&L fuori campione | $175,532 |
| Drawdown | $5,774 |
| Trade | 287 |
| Stop loss | 12.5 pt |
| Take profit | 150.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 50: `close > O_d0 * 1.005`
- deve essere FALSO — direzionale 8: `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0`

*Solo SHORT*

- deve essere VERO — direzionale 50: `close < O_d0 * 0.995`
- deve essere FALSO — direzionale 8: `O_d0 - L_d0 > (O_d1 - L_d1) * 3.0`

**Quando può operare**

- Opera solo fra **14:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **12.50 pt**
- Take profit: **$3,000** = **150.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam01_TF_M.csv`

---

### S18 · ES 4h · Breakout su N sessioni  <a id='s18'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 4h |
| Motore | BO |
| Atteso/trade | $571 |
| P&L fuori campione | $94,126 |
| Drawdown | $28,958 |
| Trade | 78 |
| Stop loss | 80.0 pt |
| Take profit | 120.0 pt |

**Ordine STOP sul canale a 3 sessioni**

- LONG: stop buy sul **massimo delle ultime 3 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 3 sessioni complete**

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 14: `|O_d5-C_d1| < 1.5 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 34: `H_d1 < H_d5`
- deve essere FALSO — direzionale -35: `(L_d1 < L_d2) E (L_d1 < L_d3) E (L_d1 < L_d4)`

*Solo SHORT*

- deve essere VERO — direzionale 34: `L_d1 > L_d5`
- deve essere FALSO — direzionale -35: `(H_d1 > H_d2) E (H_d1 > H_d3) E (H_d1 > H_d4)`

**Quando può operare**

- Opera solo fra **14:00 e 09:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$6,000** = **120.00 pt**
- Uscita a tempo dopo **50 barre** (8.3 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1847/consegna/trades/fam02_BO.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam02-2`: emettono gli stessi ordini di entrata.

---

### S19 · FDAX 4h · Breakout su N sessioni  <a id='s19'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 4h |
| Motore | BO |
| Atteso/trade | $562 |
| P&L fuori campione | $201,461 |
| Drawdown | $9,839 |
| Trade | 341 |
| Stop loss | 10.0 pt |
| Take profit | 120.0 pt |

**Ordine STOP sul canale a 1 sessioni**

- LONG: stop buy sul **massimo in costruzione della sessione corrente**
- SHORT: stop sell sul **minimo in costruzione della sessione corrente**
- Il massimo/minimo corrente INCLUDE la barra in corso: l'ordine emesso alla barra i vive solo alla barra i+1, quindi non c'è look-ahead.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 15: `|O_d5-C_d1| < 2.0 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 30: `|O_d5-C_d1| > 0.75 * (HH5-LL5)`

*Solo LONG*

- deve essere FALSO — direzionale 6: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.0`

*Solo SHORT*

- deve essere FALSO — direzionale 6: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.0`

**Quando può operare**

- Opera solo fra **07:00 e 10:00**, ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **10.00 pt**
- Take profit: **$3,000** = **120.00 pt**
- Uscita a tempo dopo **10 barre** (1.7 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1500/consegna/trades/fam03_BO.csv`

---

### S20 · NQ 30m · Trend following, simmetrico  <a id='s20'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_M |
| Atteso/trade | $464 |
| P&L fuori campione | $113,244 |
| Drawdown | $28,296 |
| Trade | 244 |
| Stop loss | 250.0 pt |
| Take profit | 150.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 48: `((H_d1-L_d1) < (H_d2-L_d2)) E ((H_d2-L_d2) < (H_d3-L_d3))`

*Solo LONG*

- deve essere VERO — direzionale 50: `close > O_d0 * 1.005`
- deve essere FALSO — direzionale 7: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`

*Solo SHORT*

- deve essere VERO — direzionale 50: `close < O_d0 * 0.995`
- deve essere FALSO — direzionale 7: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.5`

**Quando può operare**

- Opera solo fra **02:00 e 01:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$5,000** per contratto = **250.00 pt**
- Take profit: **$3,000** = **150.00 pt**
- Uscita a tempo dopo **24 barre** (12 ore)

**Verifica** — lista trade di riferimento: `run_20260815_1021/consegna/trades/fam02_TF_M.csv`

---

### S21 · NQ 15m · Trend following, simmetrico  <a id='s21'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 15m |
| Motore | TF_M |
| Atteso/trade | $436 |
| P&L fuori campione | $136,408 |
| Drawdown | $25,934 |
| Trade | 153 |
| Stop loss | 150.0 pt |
| Take profit | 200.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 12: `(H_d1 > H_d2) E (L_d1 > L_d2)`
- deve essere FALSO — direzionale 39: `O_d0 > H_d1`

*Solo SHORT*

- deve essere VERO — direzionale 12: `(H_d1 < H_d2) E (L_d1 < L_d2)`
- deve essere FALSO — direzionale 39: `O_d0 < L_d1`

**Quando può operare**

- Opera solo fra **09:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$3,000** per contratto = **150.00 pt**
- Take profit: **$4,000** = **200.00 pt**
- Uscita a tempo dopo **368 barre** (3.8 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam01_TF_M.csv`

---

### S22 · NQ 1h · Trend following, simmetrico  <a id='s22'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $422 |
| P&L fuori campione | $49,592 |
| Drawdown | $20,581 |
| Trade | 112 |
| Stop loss | 125.0 pt |
| Take profit | 250.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 11: `|O_d5-C_d1| < 0.5 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

**Quando può operare**

- Opera solo fra **00:00 e 17:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,500** per contratto = **125.00 pt**
- Take profit: **$5,000** = **250.00 pt**
- Uscita a tempo dopo **48 barre** (2.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam02_TF_M.csv`

---

### S23 · ES 1h · Bias settimanale  <a id='s23'></a>

**LONG + SHORT** — Entra e esce a giorni/orari fissi della settimana.

| | |
|---|---|
| Timeframe | 1h |
| Motore | BIASW |
| Atteso/trade | $390 |
| P&L fuori campione | $51,386 |
| Drawdown | $24,881 |
| Trade | 88 |
| Stop loss | 100.0 pt |
| Take profit | 120.0 pt |

**Ciclo settimanale a giorno e ora fissi**

- LONG: **MARKET all'apertura della barra delle 02:00 di lunedì**
- SHORT: **spento** — questa strategia non apre mai al ribasso
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 106: `L_d1 < L_d5`
- deve essere FALSO — fast 130: `(H_d2 > H_d1) E (L_d2 < L_d1)`

**Quando può operare**

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

**Uscite**

- Uscita LONG: **lunedì alle 01:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$5,000** per contratto = **100.00 pt**
- Take profit: **$6,000** = **120.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260820_0012/consegna/trades/fam01_BIASW.csv`

> ⚠ **Non mettere su conti diversi** insieme a `15m fam02`: emettono gli stessi ordini di entrata.

---

### S24 · NQ 30m · Trend following, simmetrico  <a id='s24'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_M |
| Atteso/trade | $335 |
| P&L fuori campione | $57,321 |
| Drawdown | $6,123 |
| Trade | 171 |
| Stop loss | 12.5 pt |
| Take profit | 200.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale 50: `close > O_d0 * 1.005`
- deve essere FALSO — direzionale -36: `(H_d1 < H_d2) E (H_d1 < H_d3) E (H_d1 < H_d4)`

*Solo SHORT*

- deve essere VERO — direzionale 50: `close < O_d0 * 0.995`
- deve essere FALSO — direzionale -36: `(L_d1 > L_d2) E (L_d1 > L_d3) E (L_d1 > L_d4)`

**Quando può operare**

- Opera solo fra **09:00 e 19:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **12.50 pt**
- Take profit: **$4,000** = **200.00 pt**
- Uscita a tempo dopo **12 barre** (6 ore)

**Verifica** — lista trade di riferimento: `run_20260815_1021/consegna/trades/fam03_TF_M.csv`

---

### S25 · NQ 15m · Trend following, simmetrico  <a id='s25'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 15m |
| Motore | TF_M |
| Atteso/trade | $324 |
| P&L fuori campione | $127,683 |
| Drawdown | $24,911 |
| Trade | 193 |
| Stop loss | 125.0 pt |
| Take profit | 225.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`
- deve essere FALSO — neutrale 9: `|O_d5-C_d1| < 0.1 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 17: `C_d1 > C_d2 * (1 + 0.015)`

*Solo SHORT*

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 17: `C_d1 < C_d2 * (1 - 0.015)`

**Quando può operare**

- Opera solo fra **13:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,500** per contratto = **125.00 pt**
- Take profit: **$4,500** = **225.00 pt**
- Uscita a tempo dopo **644 barre** (6.7 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam02_TF_M.csv`

> ⚠ **Non mettere su conti diversi** insieme a `15m fam02-2`: emettono gli stessi ordini di entrata.

---

### S26 · ES day · Price channel (Donchian)  <a id='s26'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | day |
| Motore | PC |
| Atteso/trade | $306 |
| P&L fuori campione | $50,510 |
| Drawdown | $12,438 |
| Trade | 57 |
| Stop loss | 20.0 pt |
| Take profit | 200.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre**
- SHORT: stop sell sul **minimo delle ultime 1 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 48: `((H_d1-L_d1) < (H_d2-L_d2)) E ((H_d2-L_d2) < (H_d3-L_d3))`

*Solo LONG*

- deve essere FALSO — direzionale 44: `L_d1 > L_d2`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **20.00 pt**
- Take profit: **$10,000** = **200.00 pt**
- Trailing stop: **$2,000** = **40.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260822_1249/consegna/trades/fam01_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `day fam01-2`: emettono gli stessi ordini di entrata.

---

### S27 · BP 1h · Trend following, simmetrico  <a id='s27'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $303 |
| P&L fuori campione | $15,958 |
| Drawdown | $3,880 |
| Trade | 26 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 23: `|O_d5-C_d1| < 0.1 * (HH5-LL5)`
- deve essere FALSO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`

*Solo LONG*

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale -45: `(C_d1 < O_d1) E (C_d2 < O_d2)`

*Solo SHORT*

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale -45: `(C_d1 > O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **20:00 e 19:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$750** per contratto = **0.01 pt**
- Take profit: **$2,000** = **0.03 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260823_1535/consegna/trades/fam01_TF_M.csv`

---

### S28 · NQ 15m · Trend following, simmetrico  <a id='s28'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 15m |
| Motore | TF_M |
| Atteso/trade | $287 |
| P&L fuori campione | $92,523 |
| Drawdown | $27,128 |
| Trade | 158 |
| Stop loss | 200.0 pt |
| Take profit | 150.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 12: `(H_d1 > H_d2) E (L_d1 > L_d2)`
- deve essere FALSO — direzionale 21: `H_d0 > H_d1`

*Solo SHORT*

- deve essere VERO — direzionale 12: `(H_d1 < H_d2) E (L_d1 < L_d2)`
- deve essere FALSO — direzionale 21: `L_d0 < L_d1`

**Quando può operare**

- Opera solo fra **18:00 e 17:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$3,000** = **150.00 pt**
- Uscita a tempo dopo **368 barre** (3.8 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam03_TF_M.csv`

---

### S29 · NQ 1h · Breakout su N sessioni  <a id='s29'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 1h |
| Motore | BO |
| Atteso/trade | $283 |
| P&L fuori campione | $39,469 |
| Drawdown | $18,908 |
| Trade | 59 |
| Stop loss | 25.0 pt |
| Take profit | — |

**Ordine STOP sul canale a 4 sessioni**

- LONG: stop buy sul **massimo delle ultime 4 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 4 sessioni complete**

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 4: `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 32: `(H_d0-L_d0) > L_d0 * 0.0075`

*Solo LONG*

- deve essere VERO — direzionale 44: `L_d1 > L_d2`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — direzionale 44: `H_d1 < H_d2`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

**Quando può operare**

- Opera solo fra **22:00 e 21:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam03_BO.csv`

---

### S30 · NQ 4h · Breakout su N sessioni  <a id='s30'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 4h |
| Motore | BO |
| Atteso/trade | $277 |
| P&L fuori campione | $64,344 |
| Drawdown | $12,392 |
| Trade | 124 |
| Stop loss | 12.5 pt |
| Take profit | — |

**Ordine STOP sul canale a 4 sessioni**

- LONG: stop buy sul **massimo delle ultime 4 sessioni complete** + 5 tick (1.25 pt)
- SHORT: stop sell sul **minimo delle ultime 4 sessioni complete** − 5 tick (1.25 pt)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

**Quando può operare**

- Opera solo fra **18:00 e 13:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **12.50 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **12 barre** (2.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1642/consegna/trades/fam03_BO.csv`

---

### S31 · ES 15m · Breakout su N sessioni  <a id='s31'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 15m |
| Motore | BO |
| Atteso/trade | $268 |
| P&L fuori campione | $90,062 |
| Drawdown | $27,908 |
| Trade | 72 |
| Stop loss | 80.0 pt |
| Take profit | 120.0 pt |

**Ordine STOP sul canale a 3 sessioni**

- LONG: stop buy sul **massimo delle ultime 3 sessioni complete** + 2 tick (0.5 pt)
- SHORT: stop sell sul **minimo delle ultime 3 sessioni complete** − 2 tick (0.5 pt)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 34: `H_d1 < H_d5`
- deve essere FALSO — direzionale -48: `close < O_d0 * 1.005`

*Solo SHORT*

- deve essere VERO — direzionale 34: `L_d1 > L_d5`
- deve essere FALSO — direzionale -48: `close > O_d0 * 0.995`

**Quando può operare**

- Opera solo fra **03:00 e 02:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$6,000** = **120.00 pt**
- Uscita a tempo dopo **920 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260819_1008/consegna/trades/fam01_BO.csv`

---

### S32 · NQ 15m · Breakout su N sessioni  <a id='s32'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 15m |
| Motore | BO |
| Atteso/trade | $261 |
| P&L fuori campione | $95,754 |
| Drawdown | $28,286 |
| Trade | 124 |
| Stop loss | 250.0 pt |
| Take profit | 150.0 pt |

**Ordine STOP sul canale a 4 sessioni**

- LONG: stop buy sul **massimo delle ultime 4 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 4 sessioni complete**

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -1: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.25`
- deve essere FALSO — direzionale 38: `H_d1 - C_d1 < 0.2 * (H_d1-L_d1)`

*Solo SHORT*

- deve essere VERO — direzionale -1: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — direzionale 38: `C_d1 - L_d1 < 0.2 * (H_d1-L_d1)`

**Quando può operare**

- Opera solo fra **05:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$5,000** per contratto = **250.00 pt**
- Take profit: **$3,000** = **150.00 pt**
- Uscita a tempo dopo **644 barre** (6.7 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam04_BO.csv`

---

### S33 · NQ 15m · Breakout su N sessioni  <a id='s33'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 15m |
| Motore | BO |
| Atteso/trade | $251 |
| P&L fuori campione | $71,987 |
| Drawdown | $26,043 |
| Trade | 97 |
| Stop loss | 200.0 pt |
| Take profit | 125.0 pt |

**Ordine STOP sul canale a 5 sessioni**

- LONG: stop buy sul **massimo delle ultime 5 sessioni complete** + 2 tick (0.5 pt)
- SHORT: stop sell sul **minimo delle ultime 5 sessioni complete** − 2 tick (0.5 pt)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 48: `((H_d1-L_d1) < (H_d2-L_d2)) E ((H_d2-L_d2) < (H_d3-L_d3))`

*Solo LONG*

- deve essere VERO — direzionale 35: `(H_d1 > H_d2) E (H_d1 > H_d3) E (H_d1 > H_d4)`
- deve essere FALSO — direzionale 50: `close > O_d0 * 1.005`

*Solo SHORT*

- deve essere VERO — direzionale 35: `(L_d1 < L_d2) E (L_d1 < L_d3) E (L_d1 < L_d4)`
- deve essere FALSO — direzionale 50: `close < O_d0 * 0.995`

**Quando può operare**

- Opera solo fra **10:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$2,500** = **125.00 pt**
- Uscita a tempo dopo **644 barre** (6.7 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam05_BO.csv`

---

### S34 · NQ 1h · Trend following, asimmetrico  <a id='s34'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $250 |
| P&L fuori campione | $203,832 |
| Drawdown | $22,478 |
| Trade | 232 |
| Stop loss | 37.5 pt |
| Take profit | 500.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 32: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5`
- deve essere FALSO — fast 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`

*Solo SHORT*

- deve essere VERO — fast 38: `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0`
- deve essere FALSO — fast 137: `(C_d1 < O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **17:00 e 03:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$750** per contratto = **37.50 pt**
- Take profit: **$10,000** = **500.00 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam04_TF_U.csv`

---

### S35 · NQ 1h · Trend following, simmetrico  <a id='s35'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $242 |
| P&L fuori campione | $45,941 |
| Drawdown | $15,665 |
| Trade | 181 |
| Stop loss | 62.5 pt |
| Take profit | 200.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 16: `C_d1 > C_d2 * (1 + 0.01)`

*Solo SHORT*

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 16: `C_d1 < C_d2 * (1 - 0.01)`

**Quando può operare**

- Opera solo fra **21:00 e 14:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,250** per contratto = **62.50 pt**
- Take profit: **$4,000** = **200.00 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam05_TF_M.csv`

---

### S36 · YM 4h · Trend following, simmetrico  <a id='s36'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $239 |
| P&L fuori campione | $44,870 |
| Drawdown | $9,579 |
| Trade | 55 |
| Stop loss | 500.0 pt |
| Take profit | 800.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 5: `|O_d1-C_d1| > 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 18: `|O_d5-C_d1| > 0.75 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -33: `L_d1 < L_d5`
- deve essere FALSO — direzionale 31: `L_d0 > L_d1 * (1 + 0.02)`

*Solo SHORT*

- deve essere VERO — direzionale -33: `H_d1 > H_d5`
- deve essere FALSO — direzionale 31: `H_d0 < H_d1 * (1 - 0.02)`

**Quando può operare**

- Opera solo fra **06:00 e 09:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,500** per contratto = **500.00 pt**
- Take profit: **$4,000** = **800.00 pt**
- Uscita a tempo dopo **50 barre** (8.3 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1550/consegna/trades/fam01_TF_M.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam01-2`: emettono gli stessi ordini di entrata.

---

### S37 · ES day · Trend following, simmetrico  <a id='s37'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | day |
| Motore | TF_M |
| Atteso/trade | $223 |
| P&L fuori campione | $83,746 |
| Drawdown | $13,541 |
| Trade | 76 |
| Stop loss | 20.0 pt |
| Take profit | 120.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 28: `|O_d5-C_d1| > 0.25 * (HH5-LL5)`

*Solo LONG*

- deve essere FALSO — direzionale 51: `close > O_d0 * 1.01`

*Solo SHORT*

- deve essere FALSO — direzionale 51: `close < O_d0 * 0.99`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **20.00 pt**
- Take profit: **$6,000** = **120.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260822_1249/consegna/trades/fam02_TF_M.csv`

> ⚠ **Non mettere su conti diversi** insieme a `day fam02-2`, `day fam02-3`: emettono gli stessi ordini di entrata.

---

### S38 · GC 1h · Price channel (Donchian)  <a id='s38'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 1h |
| Motore | PC |
| Atteso/trade | $215 |
| P&L fuori campione | $60,204 |
| Drawdown | $19,202 |
| Trade | 81 |
| Stop loss | 22.5 pt |
| Take profit | 40.0 pt |

**Ordine STOP sul canale di Donchian a 30 barre**

- LONG: stop buy sul **massimo delle ultime 30 barre** + 2 tick (0.2 pt)
- SHORT: stop sell sul **minimo delle ultime 30 barre** − 2 tick (0.2 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 30: `|O_d5-C_d1| > 0.75 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale -14: `C_d1 < O_d1`
- deve essere FALSO — direzionale -21: `L_d0 < L_d1`

*Solo SHORT*

- deve essere VERO — direzionale -14: `C_d1 > O_d1`
- deve essere FALSO — direzionale -21: `H_d0 > H_d1`

**Quando può operare**

- Opera solo fra **06:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,250** per contratto = **22.50 pt**
- Take profit: **$4,000** = **40.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260819_0659/consegna/trades/fam01_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `1h fam01-2`, `1h fam01-3`: emettono gli stessi ordini di entrata.

---

### S39 · NQ 30m · Price channel (Donchian)  <a id='s39'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 30m |
| Motore | PC |
| Atteso/trade | $202 |
| P&L fuori campione | $151,529 |
| Drawdown | $27,120 |
| Trade | 279 |
| Stop loss | 125.0 pt |
| Take profit | 500.0 pt |

**Ordine STOP sul canale di Donchian a 50 barre**

- LONG: stop buy sul **massimo delle ultime 50 barre**
- SHORT: stop sell sul **minimo delle ultime 50 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere FALSO — neutrale 8: `|O_d1-C_d1| > 0.9 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 16: `C_d1 > C_d2 * (1 + 0.01)`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **14:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,500** per contratto = **125.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Breakeven a **$1,000** = **50.00 pt** di utile
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260815_1021/consegna/trades/fam04_PC.csv`

---

### S40 · CL 30m · Incrocio di medie  <a id='s40'></a>

**LONG + SHORT** — Incrocio di due medie mobili, senza pattern.

| | |
|---|---|
| Timeframe | 30m |
| Motore | MAC |
| Atteso/trade | $199 |
| P&L fuori campione | $39,344 |
| Drawdown | $17,164 |
| Trade | 186 |
| Stop loss | 1.0 pt |
| Take profit | — |

**Incrocio di medie mobili 20/50**

- Due medie mobili **semplici** sulla close: veloce a **20 barre**, lenta a **50 barre**.
- Segnale LONG: la veloce incrocia **sopra** la lenta. SHORT: incrocia **sotto**.
- Filtro gradiente: su 5 barre la veloce deve essersi mossa, in valore assoluto, almeno **2 volte** quanto la lenta nello stesso tratto.
- Filtro sulla sessione precedente: dev'essere di **indecisione** — `|C_d1 − O_d1| ≤ 0.5 × (H_d1 − L_d1)` — e **verde** (`C_d1 > O_d1`) perché il long operi, **rossa** perché operi lo short.
- Entrata **MARKET all'apertura della barra successiva** al segnale.
- Questo motore **non usa filtri pattern**.

**Filtri pattern**

*Nessun filtro pattern*

- —: `il motore entra su ogni segnale strutturale`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- **Nessun limite** al numero di entrate per sessione: dopo un'uscita un nuovo segnale riapre. Una sola posizione per volta

**Uscite**

- Uscita su **incrocio inverso** delle due medie, eseguita sulla barra successiva al segnale.
- Uscita forzata all'**ultima barra del venerdì**: nessuna posizione resta aperta nel fine settimana.
- Sono le uscite principali del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$1,000** per contratto = **1.00 pt**
- Take profit: **nessuno**
- Trailing stop: **$2,000** = **2.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260822_0403/consegna/trades/fam01_MAC.csv`

---

### S41 · NQ 15m · Breakout su N sessioni  <a id='s41'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 15m |
| Motore | BO |
| Atteso/trade | $198 |
| P&L fuori campione | $70,891 |
| Drawdown | $21,070 |
| Trade | 121 |
| Stop loss | 25.0 pt |
| Take profit | — |

**Ordine STOP sul canale a 5 sessioni**

- LONG: stop buy sul **massimo delle ultime 5 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 5 sessioni complete**

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

**Quando può operare**

- Opera solo fra **13:00 e 06:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **644 barre** (6.7 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam06_BO.csv`

---

### S42 · ES 15m · Bias settimanale  <a id='s42'></a>

**LONG + SHORT** — Entra e esce a giorni/orari fissi della settimana.

| | |
|---|---|
| Timeframe | 15m |
| Motore | BIASW |
| Atteso/trade | $193 |
| P&L fuori campione | $57,538 |
| Drawdown | $15,424 |
| Trade | 81 |
| Stop loss | 60.0 pt |
| Take profit | 90.0 pt |

**Ciclo settimanale a giorno e ora fissi**

- LONG: **MARKET all'apertura della barra delle 03:00 di venerdì**
- SHORT: **spento** — questa strategia non apre mai al ribasso
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 94: `H_d1 < H_d5`
- deve essere FALSO — fast 112: `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)`

**Quando può operare**

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

**Uscite**

- Uscita LONG: **venerdì alle 01:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$3,000** per contratto = **60.00 pt**
- Take profit: **$4,500** = **90.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260819_1008/consegna/trades/fam03_BIASW.csv`

---

### S43 · ES 1h · Price channel (Donchian)  <a id='s43'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 1h |
| Motore | PC |
| Atteso/trade | $181 |
| P&L fuori campione | $46,003 |
| Drawdown | $8,602 |
| Trade | 93 |
| Stop loss | 80.0 pt |
| Take profit | 150.0 pt |

**Ordine STOP sul canale di Donchian a 20 barre**

- LONG: stop buy sul **massimo delle ultime 20 barre**
- SHORT: stop sell sul **minimo delle ultime 20 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`
- deve essere FALSO — neutrale 38: `(H_d0-L_d0) < L_d0 * 0.005`

*Solo LONG*

- deve essere VERO — direzionale 49: `close > O_d0`
- deve essere FALSO — direzionale -45: `(C_d1 < O_d1) E (C_d2 < O_d2)`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **03:00 e 02:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$7,500** = **150.00 pt**
- Trailing stop: **$1,000** = **20.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260820_0012/consegna/trades/fam02_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `1h fam02-2`: emettono gli stessi ordini di entrata.

---

### S44 · NG 4h · Trend following, simmetrico  <a id='s44'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $180 |
| P&L fuori campione | $117,630 |
| Drawdown | $17,718 |
| Trade | 215 |
| Stop loss | 0.2 pt |
| Take profit | 0.4 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 4: `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`

*Solo LONG*

- deve essere FALSO — direzionale 7: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`

*Solo SHORT*

- deve essere FALSO — direzionale 7: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.5`

**Quando può operare**

- Opera solo fra **10:00 e 23:59**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,750** per contratto = **0.17 pt**
- Take profit: **$4,000** = **0.40 pt**
- Uscita a tempo dopo **50 barre** (8.3 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1908/consegna/trades/fam01_TF_M.csv`

---

### S45 · ES day · Trend following, asimmetrico  <a id='s45'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | day |
| Motore | TF_U |
| Atteso/trade | $176 |
| P&L fuori campione | $122,200 |
| Drawdown | $23,176 |
| Trade | 153 |
| Stop loss | 40.0 pt |
| Take profit | 120.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 4: `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)`
- deve essere FALSO — fast 139: `(C_d1 < O_d1) E (C_d2 < O_d2)`

*Solo SHORT*

- deve essere VERO — fast 62: `H_d0 < L_d0 * (1 + 0.01)`
- deve essere FALSO — fast 67: `C_d1 > C_d2`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,000** per contratto = **40.00 pt**
- Take profit: **$6,000** = **120.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260822_1249/consegna/trades/fam03_TF_U.csv`

---

### S46 · YM 4h · Trend following, simmetrico  <a id='s46'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $162 |
| P&L fuori campione | $77,981 |
| Drawdown | $15,059 |
| Trade | 141 |
| Stop loss | 800.0 pt |
| Take profit | 300.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 32: `(H_d0-L_d0) > L_d0 * 0.0075`
- deve essere FALSO — neutrale 6: `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -1: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.25`
- deve essere FALSO — direzionale -35: `(L_d1 < L_d2) E (L_d1 < L_d3) E (L_d1 < L_d4)`

*Solo SHORT*

- deve essere VERO — direzionale -1: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — direzionale -35: `(H_d1 > H_d2) E (H_d1 > H_d3) E (H_d1 > H_d4)`

**Quando può operare**

- Opera solo fra **06:00 e 23:59**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **800.00 pt**
- Take profit: **$1,500** = **300.00 pt**
- Uscita a tempo dopo **48 barre** (8.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1550/consegna/trades/fam02_TF_M.csv`

---

### S47 · NQ 4h · Trend following, asimmetrico  <a id='s47'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_U |
| Atteso/trade | $160 |
| P&L fuori campione | $144,102 |
| Drawdown | $11,156 |
| Trade | 222 |
| Stop loss | 25.0 pt |
| Take profit | 225.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 65: `H_d0 < L_d0 * (1 + 0.025)`
- deve essere FALSO — fast 101: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — fast 37: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`
- deve essere FALSO — fast 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

**Quando può operare**

- Opera solo fra **06:00 e 23:59**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **$4,500** = **225.00 pt**
- Uscita a tempo dopo **24 barre** (4.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1642/consegna/trades/fam04_TF_U.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam04-2`: emettono gli stessi ordini di entrata.

---

### S48 · NQ 15m · Trend following, asimmetrico  <a id='s48'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 15m |
| Motore | TF_U |
| Atteso/trade | $156 |
| P&L fuori campione | $85,485 |
| Drawdown | $29,837 |
| Trade | 155 |
| Stop loss | 200.0 pt |
| Take profit | 250.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 28: `|O_d5-C_d1| > 0.25 * (HH5-LL5)`
- deve essere FALSO — fast 77: `C_d1 > C_d2 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — fast 114: `H_d1 - C_d1 < 0.2 * (H_d1-L_d1)`
- deve essere FALSO — fast 39: `H_d0 - O_d0 < H_d1 - O_d1`

**Quando può operare**

- Opera solo fra **17:00 e 10:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$5,000** = **250.00 pt**
- Uscita a tempo dopo **368 barre** (3.8 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam07_TF_U.csv`

---

### S49 · ES 15m · Bias settimanale  <a id='s49'></a>

**LONG + SHORT** — Entra e esce a giorni/orari fissi della settimana.

| | |
|---|---|
| Timeframe | 15m |
| Motore | BIASW |
| Atteso/trade | $148 |
| P&L fuori campione | $93,316 |
| Drawdown | $20,886 |
| Trade | 171 |
| Stop loss | 60.0 pt |
| Take profit | 150.0 pt |

**Ciclo settimanale a giorno e ora fissi**

- LONG: **MARKET all'apertura della barra delle 11:00 di lunedì**
- SHORT: **MARKET all'apertura della barra delle 20:00 di giovedì**
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 65: `H_d0 < L_d0 * (1 + 0.025)`
- deve essere FALSO — fast 139: `(C_d1 < O_d1) E (C_d2 < O_d2)`

*Solo SHORT*

- deve essere VERO — fast 58: `H_d0 > L_d0 * (1 + 0.025)`
- deve essere FALSO — fast 73: `C_d1 < C_d2 * (1 - 0.015)`

**Quando può operare**

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

**Uscite**

- Uscita LONG: **lunedì alle 01:00**, market all'apertura di quella barra.
- Uscita SHORT: **lunedì alle 02:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$3,000** per contratto = **60.00 pt**
- Take profit: **$7,500** = **150.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260819_1008/consegna/trades/fam04_BIASW.csv`

---

### S50 · NG 4h · Trend following, asimmetrico  <a id='s50'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_U |
| Atteso/trade | $146 |
| P&L fuori campione | $96,168 |
| Drawdown | $20,924 |
| Trade | 197 |
| Stop loss | 0.1 pt |
| Take profit | 0.8 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 85: `H_d0 > H_d1 * (1 + 0.01)`
- deve essere FALSO — fast 23: `|O_d5-C_d1| < 0.1 * (HH5-LL5)`

*Solo SHORT*

- deve essere VERO — fast 39: `H_d0 - O_d0 < H_d1 - O_d1`
- deve essere FALSO — fast 51: `(H_d1 > H_d2) E (L_d1 > L_d2)`

**Quando può operare**

- Opera solo fra **14:00 e 23:59**, ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **0.10 pt**
- Take profit: **$7,500** = **0.75 pt**
- Uscita a tempo dopo **48 barre** (8.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1908/consegna/trades/fam02_TF_U.csv`

---

### S51 · GC 4h · Price channel (Donchian)  <a id='s51'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $145 |
| P&L fuori campione | $93,240 |
| Drawdown | $10,886 |
| Trade | 410 |
| Stop loss | 5.0 pt |
| Take profit | 100.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre**
- SHORT: stop sell sul **minimo delle ultime 1 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 31: `(H_d0-L_d0) > L_d0 * 0.005`
- deve essere FALSO — neutrale 6: `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)`

**Quando può operare**

- Opera solo fra **00:00 e 12:00**, ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$500** per contratto = **5.00 pt**
- Take profit: **$10,000** = **100.00 pt**
- Uscita a tempo dopo **24 barre** (4.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1935/consegna/trades/fam02_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam02-2`: emettono gli stessi ordini di entrata.

---

### S52 · GC 1h · Ritorno alla media sugli estremi  <a id='s52'></a>

**SOLO LONG** — Limite sugli estremi della sessione precedente.

| | |
|---|---|
| Timeframe | 1h |
| Motore | RHL |
| Atteso/trade | $140 |
| P&L fuori campione | $31,320 |
| Drawdown | $11,584 |
| Trade | 75 |
| Stop loss | 20.0 pt |
| Take profit | 50.0 pt |

**Ordine LIMITE sugli estremi della sessione precedente**

- LONG: limit buy a **L_d1** − 20 tick (2 pt) (minimo della sessione precedente)
- SHORT: limit sell a **H_d1** + 80 tick (8 pt) (massimo della sessione precedente)
- I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.
- Il fill richiede penetrazione stretta del livello (`minimo < livello` per il long): il semplice tocco NON riempie.
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -1: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — direzionale -5: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **13:00 e 12:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,000** per contratto = **20.00 pt**
- Take profit: **$5,000** = **50.00 pt**
- Uscita a tempo dopo **12 barre** (12 ore)

**Verifica** — lista trade di riferimento: `run_20260819_0659/consegna/trades/fam02_RHL.csv`

---

### S53 · NQ 30m · Price channel (Donchian)  <a id='s53'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 30m |
| Motore | PC |
| Atteso/trade | $140 |
| P&L fuori campione | $67,233 |
| Drawdown | $16,660 |
| Trade | 178 |
| Stop loss | 112.5 pt |
| Take profit | 500.0 pt |

**Ordine STOP sul canale di Donchian a 50 barre**

- LONG: stop buy sul **massimo delle ultime 50 barre** + 2 tick (0.5 pt)
- SHORT: stop sell sul **minimo delle ultime 50 barre** − 2 tick (0.5 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 3: `|O_d1-C_d1| < 0.5 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 46: `(C_d1 > O_d1) E (C_d2 < O_d2)`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **11:00 e 10:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,250** per contratto = **112.50 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Breakeven a **$500** = **25.00 pt** di utile
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260815_1021/consegna/trades/fam05_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `30m fam05-2`, `30m fam05-3`: emettono gli stessi ordini di entrata.

---

### S54 · ES day · Trend following, simmetrico  <a id='s54'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | day |
| Motore | TF_M |
| Atteso/trade | $132 |
| P&L fuori campione | $105,327 |
| Drawdown | $18,502 |
| Trade | 162 |
| Stop loss | 20.0 pt |
| Take profit | 80.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 3: `|O_d1-C_d1| < 0.5 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 34: `(H_d0-L_d0) > L_d0 * 0.015`

*Solo LONG*

- deve essere VERO — direzionale 48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 7: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`

*Solo SHORT*

- deve essere VERO — direzionale 48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 7: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.5`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **20.00 pt**
- Take profit: **$4,000** = **80.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260822_1249/consegna/trades/fam04_TF_M.csv`

---

### S55 · NQ 4h · Volatility breakout  <a id='s55'></a>

**LONG + SHORT** — Rottura di un livello costruito sull'apertura più un multiplo di volatilità.

| | |
|---|---|
| Timeframe | 4h |
| Motore | VBO |
| Atteso/trade | $117 |
| P&L fuori campione | $46,272 |
| Drawdown | $18,311 |
| Trade | 119 |
| Stop loss | 112.5 pt |
| Take profit | 375.0 pt |

**Ordine STOP sull'apertura di sessione più un multiplo di volatilità**

- Sia `VOL` = l'**ATR giornaliero a 100 periodi**: l'ATR calcolato sulla serie delle SESSIONI complete — non sulle barre del timeframe — e riportato su ogni barra della sessione corrente
- LONG: stop buy a **O_d0 + 0.3 × VOL**
- SHORT: stop sell a **O_d0 − 9.5 × VOL**
- `O_d0` è l'apertura della sessione corrente: è nota dalla prima barra, quindi il livello resta fisso per tutta la sessione.
- Filtro momentum: il long opera solo se **O_d0 > C_d1**, lo short solo se **O_d0 < C_d1**.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 28: `|O_d5-C_d1| > 0.25 * (HH5-LL5)`
- deve essere FALSO — neutrale 27: `|O_d5-C_d1| > 0.9 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale 2: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5`
- deve essere FALSO — direzionale -3: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.75`

*Solo SHORT*

- deve essere VERO — direzionale 2: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.5`
- deve essere FALSO — direzionale -3: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.75`

**Quando può operare**

- Opera solo fra **00:00 e 17:00**, ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,250** per contratto = **112.50 pt**
- Take profit: **$7,500** = **375.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Uscita a tempo dopo **24 barre** (4.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1642/consegna/trades/fam05_VBO.csv`

---

### S56 · JY 4h · Trend following, asimmetrico  <a id='s56'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_U |
| Atteso/trade | $117 |
| P&L fuori campione | $36,246 |
| Drawdown | $11,957 |
| Trade | 67 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere FALSO — fast 100: `L_d0 > L_d1`

*Solo SHORT*

- deve essere VERO — fast 132: `L_d1 > L_d2`
- deve essere FALSO — fast 115: `C_d1 - L_d1 < 0.2 * (H_d1-L_d1)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **0.01 pt**
- Take profit: **$3,000** = **0.02 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_2020/consegna/trades/fam01_TF_U.csv`

---

### S57 · ES 4h · Price channel (Donchian)  <a id='s57'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $115 |
| P&L fuori campione | $54,265 |
| Drawdown | $24,860 |
| Trade | 240 |
| Stop loss | 100.0 pt |
| Take profit | 150.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre** + 5 tick (1.25 pt)
- SHORT: stop sell sul **minimo delle ultime 1 barre** − 5 tick (1.25 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 39: `(H_d0-L_d0) < L_d0 * 0.0075`
- deve essere FALSO — neutrale 6: `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$5,000** per contratto = **100.00 pt**
- Take profit: **$7,500** = **150.00 pt**
- Breakeven a **$500** = **10.00 pt** di utile
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_1847/consegna/trades/fam03_PC.csv`

---

### S58 · YM 4h · Bias intraday  <a id='s58'></a>

**LONG + SHORT** — Entra e esce a orari fissi della sessione.

| | |
|---|---|
| Timeframe | 4h |
| Motore | BIAS |
| Atteso/trade | $115 |
| P&L fuori campione | $32,798 |
| Drawdown | $11,355 |
| Trade | 238 |
| Stop loss | 450.0 pt |
| Take profit | 1,000.0 pt |

**Entrata a barra fissa della sessione**

- LONG: **MARKET all'apertura della barra 1** della sessione
- SHORT: **MARKET all'apertura della barra 7** della sessione
- Le barre della sessione si contano da **0**: la prima barra dopo l'inizio sessione è la numero 0.
- I filtri pattern si valutano alla **chiusura della barra precedente** a quella di entrata.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 93: `H_d1 > H_d5`
- deve essere FALSO — fast 6: `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- **Non apre** posizioni LONG di lunedì
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Uscita **obbligatoria alla barra 6** della sessione per il LONG e alla barra **2** per lo SHORT, market all'apertura di quella barra.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$2,250** per contratto = **450.00 pt**
- Take profit: **$5,000** = **1,000.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_1550/consegna/trades/fam03_BIAS.csv`

---

### S59 · NQ 4h · Price channel (Donchian)  <a id='s59'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $111 |
| P&L fuori campione | $68,458 |
| Drawdown | $17,849 |
| Trade | 83 |
| Stop loss | 150.0 pt |
| Take profit | 500.0 pt |

**Ordine STOP sul canale di Donchian a 15 barre**

- LONG: stop buy sul **massimo delle ultime 15 barre**
- SHORT: stop sell sul **minimo delle ultime 15 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 13: `|O_d5-C_d1| < 1.0 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 1: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — direzionale 15: `C_d1 > C_d2 * (1 + 0.005)`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **10:00 e 00:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$3,000** per contratto = **150.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_1642/consegna/trades/fam06_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam06-2`: emettono gli stessi ordini di entrata.

---

### S60 · NQ 15m · Ritorno alla media su Bollinger, simmetrico  <a id='s60'></a>

**LONG + SHORT** — Compra in limite sulla banda inferiore, vende in limite sulla banda superiore. Il pattern direzionale è INVERTITO: il long cerca la fase ribassista, perché sta comprando il fondo.

| | |
|---|---|
| Timeframe | 15m |
| Motore | RBB_M |
| Atteso/trade | $110 |
| P&L fuori campione | $104,240 |
| Drawdown | $27,622 |
| Trade | 520 |
| Stop loss | 100.0 pt |
| Take profit | 500.0 pt |

**Ordine LIMITE sulle bande di Bollinger (10 barre, 2.5 deviazioni)**

- LONG: limit buy sulla **banda inferiore**, armato finché `close > banda_inf`
- SHORT: limit sell sulla **banda superiore**, armato finché `close < banda_sup`
- Il fill richiede penetrazione stretta del livello, non il semplice tocco.
- Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 53: `(H_d1-L_d1) < (H_d2-L_d2)`
- deve essere FALSO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 37: `(C_d1 < C_d2) E (C_d2 < C_d3) E (O_d0 < C_d1)`

*Solo SHORT*

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 37: `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)`

**Quando può operare**

- Opera solo fra **07:00 e 06:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,000** per contratto = **100.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam08_RBB_M.csv`

---

### S61 · BP 15m · Trend following, simmetrico  <a id='s61'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 15m |
| Motore | TF_M |
| Atteso/trade | $104 |
| P&L fuori campione | $5,629 |
| Drawdown | $2,341 |
| Trade | 33 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 20: `|O_d5-C_d1| > 1.5 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 45: `(C_d1 > O_d1) E (C_d2 > O_d2)`
- deve essere FALSO — direzionale -3: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.75`

*Solo SHORT*

- deve essere VERO — direzionale 45: `(C_d1 < O_d1) E (C_d2 < O_d2)`
- deve essere FALSO — direzionale -3: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.75`

**Quando può operare**

- Opera solo fra **23:00 e 22:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **0.02 pt**
- Take profit: **$500** = **0.01 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260823_0343/consegna/trades/fam01_TF_M.csv`

---

### S62 · GC 4h · Price channel (Donchian)  <a id='s62'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $102 |
| P&L fuori campione | $53,332 |
| Drawdown | $16,988 |
| Trade | 333 |
| Stop loss | 25.0 pt |
| Take profit | 100.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre** + 2 tick (0.2 pt)
- SHORT: stop sell sul **minimo delle ultime 1 barre** − 2 tick (0.2 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

**Filtri pattern**

*Filtro comune a long e short*

- deve essere FALSO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 9: `H_d0 - O_d0 < H_d1 - O_d1`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — direzionale 9: `O_d0 - L_d0 < O_d1 - L_d1`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

**Quando può operare**

- Opera solo fra **00:00 e 09:00**, ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,500** per contratto = **25.00 pt**
- Take profit: **$10,000** = **100.00 pt**
- Uscita a tempo dopo **24 barre** (4.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1935/consegna/trades/fam03_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `4h fam03-2`, `4h fam03-3`: emettono gli stessi ordini di entrata.

---

### S63 · JY 4h · Trend following, asimmetrico  <a id='s63'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_U |
| Atteso/trade | $98 |
| P&L fuori campione | $36,111 |
| Drawdown | $9,809 |
| Trade | 79 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 82: `H_d0 > H_d1 * (1 + 0.0025)`
- deve essere FALSO — fast 100: `L_d0 > L_d1`

*Solo SHORT*

- deve essere FALSO — fast 115: `C_d1 - L_d1 < 0.2 * (H_d1-L_d1)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **0.01 pt**
- Take profit: **$3,000** = **0.02 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_2020/consegna/trades/fam02_TF_U.csv`

---

### S64 · NG 4h · Trend following, asimmetrico  <a id='s64'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_U |
| Atteso/trade | $93 |
| P&L fuori campione | $74,436 |
| Drawdown | $20,766 |
| Trade | 239 |
| Stop loss | 0.1 pt |
| Take profit | 0.4 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 85: `H_d0 > H_d1 * (1 + 0.01)`
- deve essere FALSO — fast 23: `|O_d5-C_d1| < 0.1 * (HH5-LL5)`

*Solo SHORT*

- deve essere FALSO — fast 51: `(H_d1 > H_d2) E (L_d1 > L_d2)`

**Quando può operare**

- Opera solo fra **14:00 e 23:59**, ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **0.10 pt**
- Take profit: **$4,000** = **0.40 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_1908/consegna/trades/fam02_TF_U.csv`

---

### S65 · GC 1h · Ritorno alla media sugli estremi  <a id='s65'></a>

**SOLO LONG** — Limite sugli estremi della sessione precedente.

| | |
|---|---|
| Timeframe | 1h |
| Motore | RHL |
| Atteso/trade | $92 |
| P&L fuori campione | $21,820 |
| Drawdown | $7,516 |
| Trade | 80 |
| Stop loss | 20.0 pt |
| Take profit | 50.0 pt |

**Ordine LIMITE sugli estremi della sessione precedente**

- LONG: limit buy a **L_d1** − 20 tick (2 pt) (minimo della sessione precedente)
- SHORT: limit sell a **H_d1** + 80 tick (8 pt) (massimo della sessione precedente)
- I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.
- Il fill richiede penetrazione stretta del livello (`minimo < livello` per il long): il semplice tocco NON riempie.
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`
- deve essere FALSO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`

*Solo LONG*

- deve essere FALSO — direzionale -5: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **13:00 e 12:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,000** per contratto = **20.00 pt**
- Take profit: **$5,000** = **50.00 pt**
- Uscita a tempo dopo **12 barre** (12 ore)

**Verifica** — lista trade di riferimento: `run_20260819_0659/consegna/trades/fam03_RHL.csv`

---

### S66 · YM 4h · Trend following, simmetrico  <a id='s66'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $92 |
| P&L fuori campione | $66,422 |
| Drawdown | $21,471 |
| Trade | 212 |
| Stop loss | 250.0 pt |
| Take profit | 800.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 49: `(H_d1 < H_d2) E (L_d1 > L_d2)`

*Solo LONG*

- deve essere FALSO — direzionale 31: `L_d0 > L_d1 * (1 + 0.02)`

*Solo SHORT*

- deve essere FALSO — direzionale 31: `H_d0 < H_d1 * (1 - 0.02)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,250** per contratto = **250.00 pt**
- Take profit: **$4,000** = **800.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_1550/consegna/trades/fam04_TF_M.csv`

---

### S67 · NQ 1h · Trend following, asimmetrico  <a id='s67'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $91 |
| P&L fuori campione | $68,525 |
| Drawdown | $24,928 |
| Trade | 215 |
| Stop loss | 200.0 pt |
| Take profit | 250.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 107: `L_d1 > L_d5`
- deve essere FALSO — fast 83: `H_d0 > H_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — fast 21: `|O_d5-C_d1| > 2.0 * (H_d5-L_d1)`
- deve essere FALSO — fast 39: `H_d0 - O_d0 < H_d1 - O_d1`

**Quando può operare**

- Opera solo fra **16:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$5,000** = **250.00 pt**
- Uscita a tempo dopo **46 barre** (1.9 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam06_TF_U.csv`

---

### S68 · NG 4h · Trend following, simmetrico  <a id='s68'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $90 |
| P&L fuori campione | $94,460 |
| Drawdown | $7,560 |
| Trade | 345 |
| Stop loss | 0.0 pt |
| Take profit | 0.5 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 53: `(H_d1-L_d1) < (H_d2-L_d2)`
- deve essere FALSO — neutrale 8: `|O_d1-C_d1| > 0.9 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 2: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5`
- deve essere FALSO — direzionale 7: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`

*Solo SHORT*

- deve essere VERO — direzionale 2: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.5`
- deve essere FALSO — direzionale 7: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.5`

**Quando può operare**

- Opera solo fra **00:00 e 17:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **0.03 pt**
- Take profit: **$5,000** = **0.50 pt**
- Uscita a tempo dopo **10 barre** (1.7 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1908/consegna/trades/fam03_TF_M.csv`

---

### S69 · NQ 15m · Trend following, asimmetrico  <a id='s69'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 15m |
| Motore | TF_U |
| Atteso/trade | $86 |
| P&L fuori campione | $91,035 |
| Drawdown | $29,723 |
| Trade | 300 |
| Stop loss | 62.5 pt |
| Take profit | — |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 31: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — fast 52: `(H_d1 < H_d2) E (L_d1 < L_d2)`

*Solo SHORT*

- deve essere VERO — fast 52: `(H_d1 < H_d2) E (L_d1 < L_d2)`
- deve essere FALSO — fast 15: `|O_d5-C_d1| < 2.0 * (H_d5-L_d1)`

**Quando può operare**

- Opera solo fra **17:00 e 07:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,250** per contratto = **62.50 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **184 barre** (1.9 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam09_TF_U.csv`

---

### S70 · NQ 4h · Price channel (Donchian)  <a id='s70'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $82 |
| P&L fuori campione | $100,640 |
| Drawdown | $24,969 |
| Trade | 165 |
| Stop loss | 200.0 pt |
| Take profit | 500.0 pt |

**Ordine STOP sul canale di Donchian a 10 barre**

- LONG: stop buy sul **massimo delle ultime 10 barre** + 2 tick (0.5 pt)
- SHORT: stop sell sul **minimo delle ultime 10 barre** − 2 tick (0.5 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 3: `|O_d1-C_d1| < 0.5 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`

*Solo LONG*

- deve essere FALSO — direzionale -45: `(C_d1 < O_d1) E (C_d2 < O_d2)`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **06:00 e 23:59**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_1642/consegna/trades/fam07_PC.csv`

---

### S71 · ES 1h · Price channel (Donchian)  <a id='s71'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 1h |
| Motore | PC |
| Atteso/trade | $77 |
| P&L fuori campione | $34,473 |
| Drawdown | $15,623 |
| Trade | 163 |
| Stop loss | 80.0 pt |
| Take profit | 150.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre**
- SHORT: stop sell sul **minimo delle ultime 1 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 29: `|O_d5-C_d1| > 0.5 * (HH5-LL5)`
- deve essere FALSO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`

*Solo LONG*

- deve essere FALSO — direzionale 16: `C_d1 > C_d2 * (1 + 0.01)`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **08:00 e 10:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$7,500** = **150.00 pt**
- Trailing stop: **$2,000** = **40.00 pt**
- Breakeven a **$500** = **10.00 pt** di utile
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260820_0012/consegna/trades/fam03_PC.csv`

---

### S72 · YM 4h · Breakout su N sessioni  <a id='s72'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 4h |
| Motore | BO |
| Atteso/trade | $71 |
| P&L fuori campione | $114,200 |
| Drawdown | $17,007 |
| Trade | 305 |
| Stop loss | 50.0 pt |
| Take profit | 1,000.0 pt |

**Ordine STOP sul canale a 2 sessioni**

- LONG: stop buy sul **massimo in costruzione della sessione corrente** + 5 tick (5 pt)
- SHORT: stop sell sul **minimo in costruzione della sessione corrente** − 5 tick (5 pt)
- Il massimo/minimo corrente INCLUDE la barra in corso: l'ordine emesso alla barra i vive solo alla barra i+1, quindi non c'è look-ahead.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 5: `|O_d1-C_d1| > 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale -10: `(C_d1 < C_d2) E (C_d2 < C_d3) E (C_d3 < C_d4)`

*Solo SHORT*

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale -10: `(C_d1 > C_d2) E (C_d2 > C_d3) E (C_d3 > C_d4)`

**Quando può operare**

- Opera solo fra **14:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **50.00 pt**
- Take profit: **$5,000** = **1,000.00 pt**
- Uscita a tempo dopo **35 barre** (5.8 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1550/consegna/trades/fam05_BO.csv`

---

### S73 · YM 4h · Breakout su N sessioni  <a id='s73'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 4h |
| Motore | BO |
| Atteso/trade | $63 |
| P&L fuori campione | $57,433 |
| Drawdown | $6,102 |
| Trade | 173 |
| Stop loss | 50.0 pt |
| Take profit | 600.0 pt |

**Ordine STOP sul canale a 1 sessioni**

- LONG: stop buy sul **massimo in costruzione della sessione corrente** + 2 tick (2 pt)
- SHORT: stop sell sul **minimo in costruzione della sessione corrente** − 2 tick (2 pt)
- Il massimo/minimo corrente INCLUDE la barra in corso: l'ordine emesso alla barra i vive solo alla barra i+1, quindi non c'è look-ahead.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 25: `|O_d5-C_d1| < 0.5 * (HH5-LL5)`
- deve essere FALSO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -33: `L_d1 < L_d5`
- deve essere FALSO — direzionale 7: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`

*Solo SHORT*

- deve essere VERO — direzionale -33: `H_d1 > H_d5`
- deve essere FALSO — direzionale 7: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.5`

**Quando può operare**

- Opera solo fra **14:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **50.00 pt**
- Take profit: **$3,000** = **600.00 pt**
- Uscita a tempo dopo **12 barre** (2.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260824_1550/consegna/trades/fam06_BO.csv`

---

### S74 · PL 4h · Trend following, simmetrico  <a id='s74'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $52 |
| P&L fuori campione | $15,985 |
| Drawdown | $2,613 |
| Trade | 115 |
| Stop loss | 5.0 pt |
| Take profit | 50.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 6: `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 19: `|O_d5-C_d1| > 1.0 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 38: `H_d1 - C_d1 < 0.2 * (H_d1-L_d1)`
- deve essere FALSO — direzionale 50: `close > O_d0 * 1.005`

*Solo SHORT*

- deve essere VERO — direzionale 38: `C_d1 - L_d1 < 0.2 * (H_d1-L_d1)`
- deve essere FALSO — direzionale 50: `close < O_d0 * 0.995`

**Quando può operare**

- Opera solo fra **00:00 e 08:00**, ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **5.00 pt**
- Take profit: **$2,500** = **50.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260824_2133/consegna/trades/fam01_TF_M.csv`

---

### S75 · NQ 15m · Trend following, asimmetrico  <a id='s75'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 15m |
| Motore | TF_U |
| Atteso/trade | $50 |
| P&L fuori campione | $50,526 |
| Drawdown | $21,924 |
| Trade | 286 |
| Stop loss | 87.5 pt |
| Take profit | 125.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 63: `H_d0 < L_d0 * (1 + 0.015)`
- deve essere FALSO — fast 79: `C_d1 > C_d2 * (1 + 0.015)`

*Solo SHORT*

- deve essere VERO — fast 37: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`
- deve essere FALSO — fast 137: `(C_d1 < O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **17:00 e 03:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,750** per contratto = **87.50 pt**
- Take profit: **$2,500** = **125.00 pt**
- Uscita a tempo dopo **48 barre** (12 ore)

**Verifica** — lista trade di riferimento: `run_20260814_1453/consegna/trades/fam10_TF_U.csv`

---

## 5. Come si verifica un port

Il port è corretto quando le **entrate** coincidono — timestamp e prezzo. I P&L sono una conseguenza.

1. Backtest sullo stesso periodo della lista di riferimento, **su dati tick**.
2. Confronta le entrate: devono coincidere al minuto e al prezzo.
3. Se le **entrate** non coincidono, il problema è nelle condizioni o nella ricostruzione delle sessioni (§2.1). Isola stampando `H_d1` e `L_d1` per qualche giorno.
4. Se le entrate coincidono ma i **P&L** no, il problema è nelle uscite o nei costi (§2.3, §2.4).

> ⚠ **Il backtest su barre non è attendibile per queste strategie.** Su dati a barre il simulatore di cTrader valuta lo stop anche contro la barra d'ingresso, percorso pre-entrata incluso, e chiude a prezzi mai esistiti. Su un port già fatto, 201 trade su 359 uscivano nello stesso minuto del fill, con slippage medio di 23 punti oltre lo stop. Usare **Tick data (accurate)**.

---

## 6. Vincolo operativo

Le 75 strategie sono univoche: nessuna coppia condivide più del 70% degli ordini di entrata. È questo che rende lecito farle girare su conti separati — due sistemi che mandano gli stessi ordini sono copy trading, e presso una prop firm costano il conto.

Queste coppie restano sotto soglia ma sono le più vicine. Se i conti separati sono pochi, non accoppiare queste:

| Coppia | Entrate in comune |
|---|---|
| `S08` ↔ `S10` | 0.70 |
| `S50` ↔ `S64` | 0.69 |
| `S02` ↔ `S03` | 0.69 |
| `S39` ↔ `S53` | 0.69 |
| `S08` ↔ `S19` | 0.66 |
| `S12` ↔ `S18` | 0.63 |
| `S10` ↔ `S19` | 0.62 |
| `S21` ↔ `S28` | 0.57 |

Il vincolo si misura sulle **entrate**, non sulla correlazione dei risultati: due strategie possono avere P&L molto diversi e mandare gli stessi ordini.

*Dati fuori campione dal 02/06/2021 al 30/05/2025. Criteri di selezione in `METODO_RICERCA.md`.*