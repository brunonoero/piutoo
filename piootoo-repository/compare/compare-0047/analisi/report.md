# compare-0047 — cBot cTrader contro backtest interno (feed ICS)

Generato: 2026-09-17 12:48Z. Finestra di sovrapposizione usata per il confronto trade: `2020-12-30` → `2021-08-28` (ingressi).

## 0. Perimetro dei due run

| | interno | cBot |
|---|---|---|
| trade totali | 190 | 319 |
| primo ingresso | 2020-12-30 02:42 | 2020-12-30 02:42 |
| ultimo ingresso | 2021-08-27 02:03 | 2021-08-30 07:06 |
| trade nella sovrapposizione | 190 | 318 |
| contratti per trade | 1 valori: 1 | 1 valori: 1 |
| netto totale | 104,146 | -16,717 |
| netto per contratto, sovrapposizione | 104,146 | -20,352 |
| lordo per contratto, sovrapposizione | 104,906 | 11,248 |
| commissioni per contratto, sovrapposizione | 760 | 5,043 |
| swap, sovrapposizione (grezzo) | 0 | -26,557 |

Log cBot: 0 righe; chiusure 0, fill con spread 0, ingressi scartati 0, annullati 0, bracket riancorati 0, chiusure per flat weekend 0, righe non interpretate 0.
Chiusure del log senza trade corrispondente: 0; fill senza trade: 0.

### 0b. Commissioni per contratto e per trade (sovrapposizione)

| simbolo | int trade | int comm/trade/ctr | cBot trade | cBot comm/trade/ctr | cBot swap/trade/ctr | cBot lordo/ctr | cBot netto/ctr |
|---|---:|---:|---:|---:|---:|---:|---:|
| FDAX | 32 | 4.0 | 0 | 0.0 | 0.0 | 0 | 0 |
| NQ | 158 | 4.0 | 318 | 15.9 | -83.5 | 11,248 | -20,352 |


## 1. Differenze di trade per strategia (finestra di sovrapposizione)

Match = stessa strategia, stesso verso, ingresso entro una barra della strategia. Netto per contratto future.

| strategia | tf | int | cbot | match | solo int | solo cbot | Δentry mediano (min) | netto/ctr int | netto/ctr cbot | Δ netto/ctr | int in match (netto) | cbot in match (netto) | int in match (lordo) | cbot in match (lordo) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| PT2_FDAX_BSW_001_60 | 60 | 32 | 0 | 0 | 32 | 0 | - | 51,827 | 0 | -51,827 | 0 | 0 | 0 | 0 |
| PT2_NQ_PCH_001_240 | 240 | 0 | 156 | 0 | 0 | 156 | - | 0 | -42,760 | -42,760 | 0 | 0 | 0 | 0 |
| PT2_NQ_PCH_002_30 | 30 | 158 | 162 | 117 | 41 | 45 | -0.3 | 52,319 | 22,408 | -29,911 | 47,548 | 26,786 | 48,016 | 36,621 |
| **TOTALE** | | 190 | 318 | 117 | 73 | 201 | | 104,146 | -20,352 | -124,498 | 47,548 | 26,786 | 48,016 | 36,621 |

Trade abbinati: 117. Scarto di ingresso: ≤1 min 71, ≤5 min 93, ≤15 min 105, oltre 12.
Prezzo di ingresso cBot peggiore dell'interno (punti, positivo = cBot paga di più): long mediana 0.2 media 0.4949 (n=117); short mediana 0 media 0 (n=0).

### 1b. Perché un trade interno non esiste sul cBot

| causa | trade | netto/ctr interno di quei trade |
|---|---:|---:|
| nessun evento nel log (livello non toccato o intent non consegnato) | 44 | 54,804 |
| cBot già in posizione (nessuna inversione) | 29 | 1,794 |

Uscite per segnale opposto nell'interno (inversione che il cBot non fa): 0 trade chiusi così, netto/ctr 0.

Trade solo cBot: 201, di cui 13 mentre l'interno era già in posizione sulla stessa strategia; netto/ctr dei solo-cBot -47,138.

## 2. Uscite

### 2a. Motivo di uscita dichiarato in trades.json

| interno | n | | cBot | n |
|---|---:|---|---|---:|
| StopLoss | 94 | | LocalExit:StopLoss | 115 |
| MaxBars | 64 | | BrokerExit:TakeProfit | 78 |
| TimeExit | 32 | | LocalExit:Closed | 63 |
|  |  | | BrokerExit:StopLoss | 62 |

### 2b. Esito reale dal log del cBot (riga `Chiuso`)

| esito log | n | netto/ctr | di cui trades.json=Closed | StopLoss | TakeProfit |
|---|---:|---:|---:|---:|---:|
| (senza riga Chiuso) | 318 | -20,352 | 63 | 177 | 78 |

### 2c. Trade abbinati: motivo interno × esito cBot

| interno \ cBot | LocalExit:Closed | LocalExit:StopLoss | tot |
|---|---:|---:|---:|
| MaxBars | 43 | 1 | 44 |
| StopLoss | 0 | 73 | 73 |

Impatto sui trade abbinati (netto/ctr cBot − interno) per coppia di esiti, prime 12 per valore assoluto:

| interno | cBot | n | Δ netto/ctr totale | Δ medio | Δ uscita mediano (min) |
|---|---|---:|---:|---:|---:|
| MaxBars | LocalExit:Closed | 43 | -26,718 | -621 | 0 |
| StopLoss | LocalExit:StopLoss | 73 | 9,014 | 123 | 0 |
| MaxBars | LocalExit:StopLoss | 1 | -3,058 | -3,058 | -3517 |

### 2d. Stop loss: perdita realizzata contro distanza dichiarata (per contratto, lordo)

Solo trade usciti per stop (interno `StopLoss`; cBot esito `StopLoss` senza trailing). Rapporto = perdita lorda / stop dichiarato (allargato dove previsto). >1 = stop eseguito oltre il livello (gap, spread sugli short).

| simbolo | int n | int rapporto mediano | int oltre 1,1 | cBot n | cBot rapporto mediano | cBot oltre 1,1 | cBot short mediano | cBot long mediano |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| FDAX | 0 | NaN | 0 | 0 | NaN | 0 | NaN | NaN |
| NQ | 94 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |

### 2f. Trade abbinati: quanto distano le uscite

| |Δ uscita| | n | Σ Δ lordo/ctr (cBot − int) | Σ Δ netto/ctr | Δ netto medio |
|---|---:|---:|---:|---:|
| ≤ 1 min | 112 | -7,827 | -16,348 | -146 |
| 5–60 min | 3 | -673 | -1,495 | -498 |
| 1–24 h | 1 | 151 | 139 | 139 |
| > 24 h | 1 | -3,046 | -3,058 | -3,058 |

Nota: per le chiusure che il cBot scopre in ritardo (`BrokerExit`), l'esito nel log è dedotto dal segno del netto (`DeduceCloseReason`), quindi `MaxBars → TakeProfit` con Δ uscita 0 è la stessa chiusura etichettata in due modi.

### 2g. Riempimenti dell'interno su un minuto senza barra nel feed

Ingresso/uscita a un istante in cui `@SYM_1.json` non ha una barra: il prezzo viene dal mark-to-market, non dal mercato. Uscite tecniche (TimeExit, MaxBars, SessionFlat, WeekEnd, EndOfRun) escluse dal conteggio delle uscite.

| strategia | ingressi senza barra / tot | netto/ctr di quei trade | uscite SL/TP/trailing/BE senza barra | esempio |
|---|---:|---:|---:|---|
| **TOTALE** | 0 | 0 | | |

Per confronto, ingressi cBot il cui minuto non ha una barra nel feed raccolto: 0 su 319 (il cBot esegue sui tick di cTrader, il feed è la raccolta a barre dello stesso broker).

Bracket riancorato al fill (ingresso slittato rispetto al livello): 0 trade; slippage mediano NaN punti, costo totale per contratto 0.

## 3. Finestra operativa (TradingWindow) e giorno saltato

Per ogni trade si ricava la barra di segnale (la barra della strategia che precede quella del fill) e si etichetta come fa il motore (`BarLabelTime` sull'orologio della finestra). Fuori finestra = un ingresso che la strategia non avrebbe dovuto emettere.

| strategia | finestra | fuso | int fuori/tot | cBot fuori/tot | int giorno saltato | cBot giorno saltato | fill fuori griglia int/cBot |
|---|---|---|---:|---:|---:|---:|---:|
| PT2_FDAX_BSW_001_60 | 00:00-24:00 | Research | 0/32 | 0/0 | 0 | 0 | 0/0 |
| PT2_NQ_PCH_001_240 | 12:00-16:00 | Research | 0/0 | 28/156 | 0 | 0 | 0/0 |
| PT2_NQ_PCH_002_30 | 00:00-24:00 | Research | 0/158 | 0/162 | 0 | 0 | 0/0 |
| **TOTALE** | | | 0 | 28 | 0 | 0 | 0/0 |

Esempi fuori finestra (primi 25):

| lato | strategia | ingresso UTC | etichetta barra di segnale | finestra |
|---|---|---|---:|---|
| cBot | PT2_NQ_PCH_001_240 | 2021-01-07 19:05 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-01-08 20:34 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-01-11 19:18 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-01-27 19:30 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-01-28 20:59 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-02-24 20:26 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-02-25 20:35 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-03-05 19:31 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-03-08 19:00 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-04-16 19:40 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-05-11 18:29 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-05-14 18:02 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-05-18 18:23 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-05-19 18:22 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-05-20 18:54 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-05-21 19:56 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-05-24 18:08 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-06-07 18:50 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-06-10 18:01 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-06-11 19:54 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-06-14 18:05 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-06-15 18:14 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-06-16 18:00 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-06-17 19:13 | 20:00 | 12:00-16:00 |
| cBot | PT2_NQ_PCH_001_240 | 2021-07-21 18:59 | 20:00 | 12:00-16:00 |

## 4. Sessione: ancoraggio e un fill per sessione per lato

| strategia | sessione (ancoraggio) | max ingressi/sessione | int sessioni con >max per lato | cBot sessioni con >max per lato | int ingressi weekend UTC | cBot ingressi weekend UTC |
|---|---|---:|---:|---:|---:|---:|
| PT2_FDAX_BSW_001_60 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PT2_NQ_PCH_001_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PT2_NQ_PCH_002_30 | 00:00-24:00 | 1 | 0 | 1 | 1 | 2 |
| **TOTALE** | | | 0 | 1 | | |

Nel log del cBot il server ha rifiutato template per `limite di ingressi per sessione raggiunto` (conteggio righe, per strategia e lato, prime 15):


## 5. Spread

Spread dell'interno, dal summary: `nessuno`. Il cBot lo paga sui tick: costo per trade ≈ spread al fill × valore punto × contratti (una volta per round trip). Lo stop è quello eseguito (allargato ×1 dove previsto).

| strategia | simbolo | fill con spread | spread medio (punti) | stop (punti) | spread/stop | costo spread tot (per contratto, $) | netto/ctr cBot | netto/ctr senza spread | costo/trade ($/ctr) |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PT2_NQ_PCH_001_240 | NQ | 0/156 | 0 | 229.75 | 0.0% | 0 | -42,760 | -42,760 | 0 |
| PT2_NQ_PCH_002_30 | NQ | 0/163 | 0 | 52.25 | 0.0% | 0 | 26,043 | 26,043 | 0 |
| **TOTALE** | | | | | | 0 | -16,717 | -16,717 | |

### 5b. Strategie con stop più stretto dello spread, o quasi

| strategia | spread/stop | stop (punti) | trade cBot | netto/ctr cBot | commento |
|---|---:|---:|---:|---:|---|

Sui trade abbinati: 0 volte il cBot esce per stop dove l'interno no; 73 il contrario.

## 6. Strategie con curva di equity crescente

Metriche su netto per contratto, trade ordinati per uscita. R² = fit lineare della curva cumulata sull'indice dei trade. Crescente = netto > 0, R² ≥ 0,6, chiusura ad almeno il 70% del picco, ≥ 8 trade. Il cBot è calcolato sull'intero run (ingressi 30/12/2020 → 30/08/2021), l'interno sull'intero run (ingressi 30/12/2020 → 27/08/2021).

| strategia | cBot n | cBot netto/ctr | PF | R² | DD max/ctr | fine/picco | **cBot** | int n | int netto/ctr | PF | R² | DD max/ctr | fine/picco | **int** |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---|
| PT2_FDAX_BSW_001_60 | 0 | 0 | 0.00 | 0.00 | 0 | 0.00 | no | 32 | 51,827 | 2.13 | 0.85 | 10,247 | 0.98 | SÌ |
| PT2_NQ_PCH_001_240 | 156 | -42,760 | 0.69 | 0.77 | 44,101 | -72.61 | no | 0 | 0 | 0.00 | 0.00 | 0 | 0.00 | no |
| PT2_NQ_PCH_002_30 | 163 | 26,043 | 1.28 | 0.72 | 11,819 | 1.00 | SÌ | 158 | 52,319 | 1.52 | 0.92 | 11,606 | 1.00 | SÌ |

**Crescenti sul cBot:** PT2_NQ_PCH_002_30

**Crescenti sull'interno:** PT2_FDAX_BSW_001_60, PT2_NQ_PCH_002_30

**Crescenti su entrambi:** PT2_NQ_PCH_002_30

## 7. Equity aggregata per mese (netto per contratto, sovrapposizione)

| mese (uscita) | interno | cBot | Δ |
|---|---:|---:|---:|
| 2020-12 | -1,167 (cum -1,167) | -1,360 (cum -1,360) | -193 |
| 2021-01 | 8,421 (cum 7,254) | -4,137 (cum -5,497) | -12,558 |
| 2021-02 | 5,877 (cum 13,130) | -5,311 (cum -10,808) | -11,188 |
| 2021-03 | 22,799 (cum 35,929) | -17,828 (cum -28,636) | -40,627 |
| 2021-04 | 21,839 (cum 57,768) | 8,894 (cum -19,743) | -12,945 |
| 2021-05 | 13,310 (cum 71,078) | 2,859 (cum -16,884) | -10,451 |
| 2021-06 | 14,121 (cum 85,199) | 4,483 (cum -12,401) | -9,638 |
| 2021-07 | 2,372 (cum 87,571) | -673 (cum -13,074) | -3,045 |
| 2021-08 | 16,576 (cum 104,146) | -7,278 (cum -20,352) | -23,854 |

## 9. Quando arrivano gli intent al cBot (righe `Intent ... attesa`)

`attesa` = ValidFrom − ora del server: negativo = l'intent arriva dopo l'inizio della barra su cui è valido. Per tipo di ordine e timeframe della strategia.

| tipo | tf | intent | in orario (≤ 10 s) | 10 s – 5 min | 5 min – 1 barra | ≥ 1 barra (in ritardo di una barra intera) | esempio strategie ≥ 1 barra |
|---|---:|---:|---:|---:|---:|---:|---|

### 9b. Trade cBot nati da un intent arrivato in ritardo di almeno una barra

Trade cBot con intent risolto: 0 su 319; nati da intent in ritardo ≥ 1 barra: 0, netto/ctr 0; di questi senza corrispondente interno: 0.

| strategia | trade da intent stantio | netto/ctr | non abbinati |
|---|---:|---:|---:|

Trade abbinati in cui il cBot esce per stop più di 5 minuti PRIMA dell'uscita interna (che non era uno stop): 0, Δ netto/ctr 0. Per strategia: .

## 8. Avvisi ed errori nel log del cBot (messaggi normalizzati, prime 25)


Ingressi scartati per classe:

Ingressi annullati per classe:

`Nessun intent per l'account` per motivo:

Ingressi scartati `lato sbagliato` per strategia (prime 15):

