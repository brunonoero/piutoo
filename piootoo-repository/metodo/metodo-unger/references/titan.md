# Titan — Portfolio Management Unger Academy (riferimento operativo)

> Fonte: "MANUALE TITAN ITA.pdf" (Unger Academy, 43 pagine) in `Mat_didattico/`.
> Titan è il software con cui Unger Academy implementa in pratica il **4° tassello del metodo
> (Portfolio)**: riceve i profitti giornalieri di ogni strategia, decide periodo per periodo quali
> strategie accendere/spegnere e con quanti contratti operare, e restituisce la composizione del
> portafoglio per il **prossimo** periodo di trading. È il riferimento funzionale per `portfolio.py`.

## Indice

1. [Cos'è Titan](#1-cosè-titan)
2. [Export dei dati dalla piattaforma](#2-export-dei-dati-dalla-piattaforma)
3. [Lista strategie e parametri per strategia](#3-lista-strategie-e-parametri-per-strategia)
4. [Settings: capitale, rischio e formula MinCap](#4-settings-capitale-rischio-e-formula-mincap)
5. [I filtri (selezione on/off delle strategie)](#5-i-filtri-selezione-onoff-delle-strategie)
6. [Correlazione e arrotondamenti](#6-correlazione-e-arrotondamenti)
7. [Money Management e degrado](#7-money-management-e-degrado)
8. [Parametri di default e consigliati](#8-parametri-di-default-e-consigliati)
9. [Pipeline di esecuzione e i 3 portafogli](#9-pipeline-di-esecuzione-e-i-3-portafogli)
10. [Output e analisi dei risultati](#10-output-e-analisi-dei-risultati)
11. [Comparatore di backtest](#11-comparatore-di-backtest)
12. [FAQ — il rationale di Unger](#12-faq--il-rationale-di-unger)
13. [Implicazioni per il port Python](#13-implicazioni-per-il-port-python)

---

## 1. Cos'è Titan

Programma standalone (Windows, runtime MATLAB MCR R2018a) che riceve come input i **profitti
giornalieri** di ogni strategia (export da MultiCharts/TradeStation) e, a ogni ricalcolo periodico
(settimanale o mensile), decide:

- **con quali strategie operare** e quali spegnere (filtri + ranking);
- **quanti contratti** allocare a ciascuna (position sizing + correlazione);
- quali strategie **non hanno capitale sufficiente** (eliminate via MinCap).

Confronta sempre 3 portafogli: **Filtered** (filtri + ranking + sizing), **Unfiltered** (tutti i
sistemi attivi, solo sizing), **OneContract** (tutti con 1 contratto, nessun sizing). Output
grafici: equity daily/monthly/annual, drawdown, margini usati, istogramma profitti.

I criteri di ranking analizzati: performance breve vs lungo periodo (periodi più lunghi per
strategie con pochi trade), DD vs profitti medi, equity vs propria media, periodi in
guadagno/perdita, performance dell'equity filtrata vs originale.

## 2. Export dei dati dalla piattaforma

- Funzione consigliata: **`WriteDailiesCTitanReports`** (la vecchia `WriteDailies` resta
  utilizzabile anche da indicatori; la nuova SOLO da segnali). Scrive direttamente nella cartella
  report di Titan un file per strategia: **`Strumento_NomeStrategia.txt`**.
- Contenuto file (una riga per giorno): data, **daily profit calcolato su open equity** (include
  posizioni aperte, non solo trade chiusi), contratti correnti, gap overnight in $, true range
  giornaliero in $, numero trade cumulati. Giorni senza operatività riempiti con zeri.
- Attivazione: input `TitanExportMode = 1` nella strategia. Primo parametro = data inizio export
  in **formato EasyLanguage YYYMMDD** con YYY = anno−1900 (es. 15/03/2009 → `1090315`).
- Verificare che il percorso output nella funzione punti a una cartella esistente, poi compilare.
- **TradeStation**: versione apposita + libreria `fast_appender.dll` da copiare nella cartella
  programma (es. `C:\Program Files (x86)\TradeStation 9.5\Program`) e riavviare la piattaforma.
- Quantità di export standard: **1 contratto** per i futures, **100.000 contratti (1 lotto)** per
  il forex. Niente piramidazioni o uscite differenziate con quantità variabili.
- **Applicare sempre stima slippage/commissioni** prima dell'export: backtest più veritieri e
  strategie su strumenti diversi confrontabili.
- Valute compatibili: EUR, USD, GBP, JPY, AUD, CAD, NZD, CHF. Strumenti in altre valute (es. TRY):
  convertire in piattaforma prima dell'export.
- Consiglio operativo: workspace dedicati con tutti i grafici già configurati per velocizzare
  l'aggiornamento periodico dei `.txt`.

## 3. Lista strategie e parametri per strategia

File **`TradingSystems.ttslist`** = lista delle strategie analizzate (gestibile da **Strategy
Manager**; liste multiple selezionabili da Settings → Current list). Creazione: import `.csv` da
vecchie versioni, `Add strats from reports` (con auto-riconoscimento del sottostante = stringa
prima del primo `-` nel nome file, es. `@ES_Reversal_1.txt` → `@ES`), `Add list from folder`
(ATTENZIONE: cancella la lista precedente) o manuale. Operazioni: reassign margins, add/remove
strategy, clear, sort, `Save list as` (nessun salvataggio automatico delle modifiche).

Parametri per strategia:

| Parametro | Significato |
|-----------|-------------|
| **System Name** | Nome file report ESATTO, senza estensione |
| **Forex Multiplier** | Frazione di lotto come unità di sizing: 1 = lotto intero (futures: sempre 1), 10 = mini, 100 = micro. Output Titan decimale: 0,48 con export a 100.000 = 48.000 contratti |
| **Currency** | Valuta dello strumento (tutte le analisi convertite in USD ai tassi dei Settings) |
| **Active** | 1 = importata, 0 = ignorata. Funziona SOLO con `LoadOnlyActive` attivo nei Settings |
| **Minimum Contracts** | Minimo contratti (0/vuoto = nessun limite). Interagisce con `ForceMinCont`: OFF → il minimo si applica solo se la strategia avrebbe comunque passato test di performance/rischio/correlazione; ON → forzatura incondizionata |
| **Maximum Contracts** | Massimo contratti, indipendente da altri settaggi |
| **Filter Bypass** | 1 = esente dai filtri, va direttamente al ranking (o al sizing) |
| **Margin in USD** | Margine broker per 1 contratto (forex: per lotto intero). Differenziabile intraday/multiday (es. DAX: 5.000 intraday, 28.000 multiday) |
| **Underlying, Category, Timeframe** | Es. DAX / reversal-breakout-none / intraday-multiday. OBBLIGATORI per lo Strategy Ranking |

## 4. Settings: capitale, rischio e formula MinCap

**Generali**: Starting/Finishing Date (Titan carica SEMPRE tutto lo storico dei `.txt`, le date
limitano solo i risultati mostrati), Report Folder, Output Folder, Current list, **Preset**
(set di impostazioni salvabili/richiamabili; tutti i parametri tranne generali e principali).

**Principali dell'esecuzione**:

- `StartingCapital`: capitale iniziale in valuta locale;
- `DailyRisk`: % capitale a rischio sul **giorno peggiore** della strategia (WCS) — è il %f su
  Worst Day del metodo;
- `MaxDD`: % capitale dedicata a ogni strategia rispetto al suo Max DD;
- `MaxTotalDD`: % capitale a rischio sull'intero portafoglio (99 = filtro disattivato);
- `MarginDD`: moltiplicatore dei margini da aggiungere al Max DD;
- `leverage`: leva sul capitale iniziale (default 1).

**Capitale minimo per strategia** (elimina le strategie che non se lo possono permettere):

```
CapWCS      = giorno peggiore / DailyRisk%
CapMaxDD    = MaxDD strategia / MaxDD%     (rettificato per n. candidati e MaxTotalDD)
CapMarginDD = margine + MaxDD storico × MarginDD

MinCap = (CapWCS + CapMaxDD + CapMarginDD) / 3     ← MEDIA, non il più severo (scelta deliberata)
```

**Altri**: `ReadSaved` (ricarica il file temporaneo `TitanData` dell'esecuzione precedente;
disattivare per rendere effettive modifiche a MinCont/MaxCont), `LoadOnlyActive`, `ShowOne`
(mostra anche il portafoglio OneContract), `ShowGraphs`/`SaveGraphs`, `SaveAggregEquities`
(equity aggregate per sottostante), `SaveAllSysEquities` (equity singole), `Excel export`,
`ForceMinCont` (vedi §3). **Parametri valutari**: tasso di ogni cambio vs USD + valuta del
capitale iniziale (`Own currency`).

## 5. I filtri (selezione on/off delle strategie)

### Filtro base short/long period
- `Periodicity`: ogni settimana / fine mese / **1° sabato del mese** (default Unger) / ultima
  domenica / ogni anno;
- `long`, `short`: numero periodi (disattivabili singolarmente);
- `th`: soglia del test **short × th > long**;
- `MinTrades`: minimo trade per attendibilità dello short period — se mancano, si pesca più
  indietro nello storico (0 = disattivato).

### Filtri alternativi
- `alternate` (sullo short period) + `MinPosPeriods`: % di periodi in guadagno richiesta.

### Filtro media equity curve
- `avgEquity > 0`: spegne se l'equity è sotto la propria media. Media su **giorni naturali**,
  non sul numero di trade.

### Filtro rendimento/rischio medio annuale
- `avgGDDYrs` (anni testati), `avgGDD` (min guadagno medio annuo / DD), `MinYearTrades`,
  `degrade` (degrado ammesso vs stesso test sull'equity non filtrata).

### Soglie di utilità dei filtri (meta-filtro)
- `thPassP`: usa i filtri se si sono dimostrati utili ≥ thPassP% dei periodi;
- `thPassN`: **inverte la logica** se hanno funzionato male > thPassN% dei periodi (0 = off).

### Filtro Average Trade (degrado)
- Confronta avg trade recente vs avg trade su tutto lo storico fino a quel momento:
  `AvgTradeFilter`, `AvgTradePeriod`, `AvgTradeTH` (% minima accettabile), `AvgTradeMinTrades`,
  `Filter mode` (degrado storico / valore fisso da tabella strategie / combinazione). Lavora IN
  AGGIUNTA ai filtri precedenti.

### Filtro ultimo picco
- Blocca i sistemi senza nuovi massimi dell'equity negli ultimi N periodi (DD = 0 a fine giornata):
  `EnablePeakTest`, `PeakPeriod`, `PeakMinTrades`.

### Filtro deviazione standard (Bollinger sull'equity)
- Bande di Bollinger sulla curva dei profitti: `DevNumUp`/`DevNumDn` (n. deviazioni, con checkbox
  per filtrare sopra la banda sup. / sotto la banda inf.), `DevPeriod` (giorni).

### Strategy Ranking
- Tetto al numero di sistemi "simili" (stesso **Underlying + Category + Timeframe**): i meno
  performanti sono eliminati prima del sizing, per ridurre perdite simultanee da affinità.
- Metriche: NetProfit, NetProfit/MaxDD, MaxDD. Parametri: `Ranking Mode`, `PickingPeriod`,
  `PickingMinTrades`, `RankMaxPerCat`, `SkipCat` (categoria esclusa dal ranking, per sistemi
  non categorizzabili).

### Tetti di emergenza
- **MonthStop** + `MonStopCash`: tetto monetario alle perdite mensili del portafoglio; controllo a
  fine giornata (il limite può quindi essere ecceduto intraday), sospende fino al ricalcolo
  successivo.
- **SuspendForDD** + `SuspAlsoStd` (anche sulle equity non filtrate), `DDTH` (% di sforamento del
  DD vs max storico mai recuperato), `DDRestartTH` (% per riattivare; 0 = riattiva al nuovo picco).
  Anche qui controllo a fine giornata.

## 6. Correlazione e arrotondamenti

- `corMode`: **Classic** (correlazione statistica) oppure **AU formula** (formula di Unger):
  rapporto tra i giorni in cui la coppia di sistemi ha guadagnato o perso INSIEME e il totale dei
  giorni in cui entrambi hanno operato.
- Calcolata sulle **equity line giornaliere** (non sui trade chiusi).
- `corTH`: soglia oltre cui due sistemi sono "correlati" → i contratti di entrambi vengono divisi
  per **(1 + coefficiente di correlazione)**.
- `tradeLMT`: minimo decimale di contratti perché una strategia sia candidata al portafoglio;
- `shiftC`: soglia decimale oltre cui si arrotonda all'intero successivo;
- `cLoops`: cicli di ricalcolo nella ricerca delle strategie tradabili.

## 7. Money Management e degrado

- `VolPotSize`: attiva il **Percent Volatility** usando l'ATR contenuto nei `.txt` (funziona SOLO
  con `MoneyMgt = 0`, cioè senza ricapitalizzazione);
- `MoneyMgt`: aggiorna il capitale mese per mese (capitalizzazione di utili/perdite nel backtest);
- **Degrado dei trade storici** (solo per backtest): `EnableDegrade`, `InitialDeg` ($ per trade),
  `DegressiveDegMode` (degrado maggiore all'inizio dello storico — zavorra la parte in-sample —
  ridotto costantemente ogni anno).

## 8. Parametri di default e consigliati

| Parametro | Default | Consigliato |
|-----------|---------|-------------|
| Periodicity | Mensile (1° sabato) | |
| long / short | 12 / 4 mesi (attivi) | |
| th | 0 | |
| MinTrades | 16 | ≈ ≥4 trade/mese |
| alternate / MinPosPeriods | 0 / 50 | |
| avgEquity | disattivato | |
| avgGDDYrs / avgGDD | 3 / 2 | |
| MinYearTrades | 23 | ≈ ≥1,5 trade/mese |
| degrade | 0,5 | |
| thPassP / thPassN | 40 / 60 | |
| AvgTradeFilter | disattivato | AvgTradeTH 25–50 (default 40), period 4, min trades 16 |
| EnableDevTest | disattivato | DevPeriod 100–200 (default 150), DevNumUp/Dn 1, solo test DN attivo |
| EnablePeakTest | disattivato | PeakPeriod 6–12 mesi, min trades 12 |
| EnableRanking | disattivato | RankMaxPerCat 1–4 secondo capitale (default 3), picking 6 periodi / 16 trade |
| SuspendForDD | disattivato | DDTH 70, DDRestartTH 0 |
| **DailyRisk** | **1,25** | 1–2 |
| **MaxDD** | **4** | 4–10 |
| MaxTotalDD | 99 | |
| MarginDD | 2 | 2–4 |
| ForceMinCont | disattivato | disattivato |
| corMode / corTH | 2 (AU) / 0,75 | corTH 0,5–0,75 |
| tradeLMT / shiftC / cLoops | 0,375 / 0,75 / 2 | shiftC 0,7–0,99 |
| leverage | 1 | 1 |
| EnableDegrade | disattivato | disattivato |

## 9. Pipeline di esecuzione e i 3 portafogli

Ordine di esecuzione (`Titan.exe`): 1. caricamento parametri → 2. caricamento dati giornalieri →
3. degrado equity → 4. filtri + ranking → 5. correlazioni → 6. position sizing → 7. sospensioni
per DD / MonthStop → 8. output backtest + strategie del periodo attuale → 9. grafici → 10.
salvataggio grafici → 11. salvataggio equity per mercato e singole.

**Portafoglio Filtered** (step): analisi short period (convalida vs long, filtri alternate) →
filtro media equity (opz.) → filtro avgGDD (opz.) → test utilità filtri positivo/negativo →
filtro Average Trade (opz.) → test ultimo picco (opz.) → test deviazione standard (opz.) →
Strategy Ranking (opz.) → Position Sizing (%DailyRisk, %MaxDD, margini) → correlazioni e
correzione contratti → forzature min/max → output.

**Unfiltered**: solo sizing + correlazioni + forzature. **OneContract**: tutti i sistemi,
1 contratto, nessun filtro né sizing.

## 10. Output e analisi dei risultati

**A video** (sezione Backtest): indicatori di performance, **strategie da tradare nel periodo**
(Filtered e Unfiltered, contratti, margini, correlazioni), equity daily/monthly/annual, Period
Analysis.

**Su file** (cartella Output):
- **TitanLog**: trascrizione dell'esecuzione (debug errori; in caso di crash resta nella dir
  principale);
- **Results.xlsx** (con Excel export): fogli `TradeLive` (portafogli del periodo, contratti anche
  decimali, esito 0/1 di OGNI test per strategia), `HistoricalFiltered` e
  `HistoricalFilteredTable` (composizioni storiche del portafoglio a ogni ricalcolo — mostra come
  Titan si è adattato nel tempo), `Strats details` (NP, WCS, MaxDD, Filtered Profit/MaxDD),
  `MinCapPerStrategyFiltered` (capitale necessario per sistema, sia "Portfolio" = nel contesto
  attuale sia "Alone" = come unica strategia; avg trade recente e storico), `Periodical Analysis`
  (risultati annuali/mensili/giornalieri dei 3 portafogli + differenza Filtered−Unfiltered),
  `Equity Curves`, `Settings`;
- **Graphs** (.jpg), **Equities_Aggregates** (per sottostante), **Equities_SingleStrats**.

## 11. Comparatore di backtest

Ogni esecuzione genera un file `.tBT` in `Backtests/` (nome = timestamp, rinominabile). La sezione
"Backtest comparison" carica **fino a 5 file**, mostra i settaggi di ciascuno (bottone "i") e
confronta indici di performance, curve dei profitti e risultati giornalieri/mensili/annuali dei
preset. Utile per A/B di configurazioni filtri/rischio.

## 12. FAQ — il rationale di Unger

Principi emersi dalle risposte di Unger nel manuale (da rispettare anche nel port):

- **Filtri non vitali, tranne short>long**: i filtri positivi/negativi aiutano ma non sono
  essenziali; short>long va tenuto perché contiene il DD e "tiene tranquille" le strategie nuove
  potenzialmente sovra-ottimizzate. Concentrarsi su **UNO solo** tra filtro media equity e
  avgGDD, non entrambi.
- **Strategie piatte negli ultimi anni**: effetto OOS (il passato sviluppato rende sempre meglio
  del futuro) + calo generalizzato di volatilità. Il primo si migliora con l'esperienza, il
  secondo è ciclico.
- **MinCap = media dei 3 criteri, non il più severo**: scelta deliberata di semplicità visto il
  numero di vincoli.
- **% MaxDD dipende dal numero di strategie**: con ~20 sistemi una % alta (es. 20%) è accettabile;
  con centinaia serve scendere (es. 4%). Si può ridurre la % man mano che i profitti crescono.
- **Quando attivare un sistema nuovo**: non aspettare troppo — metterlo nel paniere e lasciare che
  i filtri lo gestiscano. Un OOS che copre ~lo short period di Titan + ~1 mese di demo è
  sufficiente.
- **Disciplina sui filtri**: non "adattare" i filtri per far entrare una strategia che piace
  (sottovalutiamo l'impatto psicologico delle perdite di una scelta forzata). Se serve, cambiare
  il moltiplicatore OVUNQUE (es. avgGDD 2 → 1,5) e valutare il risultato, non fare eccezioni.
- **Trade aperti al ricalcolo (riallineamento del lunedì)**: esiste la curva **"filtered with
  gap"** che contabilizza i gap della routine weekend (differenze minime, che si compensano nel
  tempo — per i conti fa fede la curva filtered). Il P&L è calcolato **sulla differenza di equity
  giornaliera, non sui trade**: le strategie staccate si chiudono all'apertura del lunedì; le
  strategie riattivate non-flat si riallineano aprendo la posizione all'apertura del lunedì.

## 13. Implicazioni per il port Python

Il contratto dati di Titan è la specifica di riferimento per `portfolio.py`:

1. **Input**: serie giornaliera per strategia (data, daily P&L su open equity, contratti, gap $,
   true range $, n. trade cumulati) — esattamente ciò che il simulatore Python già produce;
   replicare l'export = un writer con quel formato.
2. **P&L su differenza di equity giornaliera**, mai sui trade chiusi: semplifica accensioni e
   spegnimenti a metà trade (§12).
3. Ordine della pipeline: filtri → ranking → MinCap → sizing → correzione per correlazione →
   arrotondamenti (§9); il basket Unfiltered e OneContract come benchmark interni.
4. Formula MinCap (§4), correlazione AU e correzione `(1+corr)` (§6), default §8 come parametri
   iniziali (DailyRisk 1,25%, MaxDD 4%, corTH 0,75, mensile 1° sabato, long 12 / short 4).
5. Confronto di parità: dare in pasto a Titan i report esportati dalle strategie Python e
   verificare che `portfolio.py` produca la stessa composizione di portafoglio.
