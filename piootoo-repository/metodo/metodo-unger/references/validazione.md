# Validazione e valutazione di un sistema — riferimento completo

Fonte sintetica: Unger Academy — *Trading System Evaluation* (TSE), *Trading System Setup 2* (TSS2).

---

## 1. Le caratteristiche di un sistema utilizzabile

Un sistema può essere statisticamente eccellente e **inutilizzabile psicologicamente**.
La valutazione attraversa due dimensioni:

### Tecnico-pratiche
- **Automatizzabile o no**: regole completamente codificabili? Discrezionalità nascosta?
- **Veridicità del backtest**: simulatore corretto su ordini limit/stop? Slippage realistico?
- **Replicabilità degli ordini**: in real-time si possono mettere gli stessi ordini con lo stesso eseguito?
- **Frequenza operativa**: troppo bassa → commissioni e tempo morto; troppo alta → costi e stress

### Psicologiche
- **Approccio operativo**: serve presenza al monitor? si può lasciar girare?
- **Vincite e perdite**: distribuzione percentuale e dimensioni
- **Drawdown e RunUp**: intensità e frequenza delle escursioni
- **Frequenza operativa**: anche da prospettiva del trader — riesce a stare fermo nei periodi flat?

> Esempio classico: una strategia con 30% WR e payoff 5:1 è matematicamente vincente ma
> psicologicamente molto difficile (sequenze di 5-6 perdite frequenti).

---

## 2. Le metriche fondamentali

### 2.1 Net Profit
- Base solida su cui costruire i prossimi passi
- Se troppo basso: aumentare contratti (se possibile) o approfondire (pochi trade?)
- **Sempre considerare i costi**: una strategia che lavora poco rischia di essere sopraffatta da commissioni e slippage

### 2.2 Periodical Profits
- Distribuzione anno per anno
- Non tutti gli anni devono essere positivi, ma una buona distribuzione lungo l'intero arco è ideale
- **Allerta**: un anno "da favola" che falsa il totale → fragilità (anno irripetibile?)

### 2.3 Drawdown
- Il peggior DD è il parametro più importante
- Quanti DD "simili" ci sono stati? Quanto spesso?
- **Dove sono i DD più gravi?** Periodi recenti = più preoccupante che periodi remoti
- Confrontare DD col Net Profit (NP/DD ratio) e col mercato (DD/range medio)

### 2.4 Numero di trade
- Almeno ~12/anno se non è un sistema "raro" by design
- IS con < 50 trade → numeri inaffidabili, indipendentemente da quanto sembrino buoni
- Troppi trade su TF brevi: rischio di overfitting al rumore

### 2.5 Durata dei trade
- Più tempo a mercato = più rischio (gap, eventi notturni, slippage)
- Valutare insieme all'**average trade**: trade lungo deve avere avg trade maggiore

### 2.6 Outlier
- Profitti concentrati in pochi trade outlier = strategia fragile (perdi il prossimo outlier → perdi la strategia)
- Outlier negativi vanno misurati separatamente: possono "svuotare il conto"
- Approccio: ripetere il backtest senza il top 5% e bottom 5% — quanto cambia?

### 2.7 Average Trade
Soglie di buon senso:
- **Almeno 6-7 tick** dello strumento
- **~15% del range monetario medio** della giornata
- **Più alto se durata trade è lunga** (compensa la presenza prolungata a mercato)

Esempio CL: tick = $10, range medio = $1100. Avg trade minimo target: 60-70 USD, idealmente 150+.

---

## 3. In-Sample / Out-of-Sample / Forward

### 3.1 Processo
```
   Sviluppo IS    →    Validazione OOS    →    Validazione Forward    →    LIVE
                       (saved data)              (paper trading 2+ mesi)
```

### 3.2 Regole pratiche
- OOS o Forward possono essere saltati — ma è una scelta che riduce robustezza
- L'IS può essere scelto anche su periodi non consecutivi (es. anni alternati)
- IS può essere più lungo o più corto dell'OOS — non c'è regola fissa
- Quanto migliore l'IS, **tanto maggiore il rischio di calo OOS** (overfit)
- Risultati basati su outlier sono **molto più difficili** da validare in OOS

### 3.3 Cosa cercare nell'OOS
- Equity OOS positiva, anche se inferiore all'IS
- Direzione del DD e dei guadagni simile all'IS
- Win rate, avg trade, profit factor che non cambiano drasticamente
- Se il sistema OOS è significativamente **migliore** dell'IS → bias o look-ahead

### 3.4 Forward validation
Anche se OOS è ok, il Forward (paper trading 2-3 mesi) serve a:
- Scoprire errori nascosti nel codice (logica di entrata/uscita non replicata correttamente)
- Verificare slippage e commissioni reali vs simulati
- Confermare che lo stack tecnologico funziona in real-time
- Dare confidenza prima di andare con denaro reale

---

## 4. Stabilità dei parametri

Una strategia con parametri instabili è solitamente overfit. Verificare:

### 4.1 Heatmap 2D
Ottimizzare due parametri congiuntamente, plotare il risultato come heatmap.
- **Plateau**: zona ampia di valori vicini al massimo → stabile, robusto
- **Picco isolato**: massimo circondato da negativi → overfit, da rifiutare

### 4.2 Test di sensitività
Per ciascun parametro ottimale, testare valori a ±5%, ±10%, ±20%.
Se la metrica crolla a piccole variazioni → il valore "ottimale" è rumore campionario.

### 4.3 Scelta operativa
**Scegliere parametri al centro di un plateau, non nel massimo assoluto.**
Un parametro che funziona solo con stop=850 e crolla con 800 o 900 non è una proprietà
del mercato: è rumore del campione IS.

---

## 5. Equity Curve Trading

Lo studio dell'equity stessa diventa un segnale on/off.

```python
def equity_curve_filter(equity: pd.Series, ma_period: int = 30, mode="above") -> pd.Series:
    """
    Maschera per attivare trading solo quando equity è sopra (o sotto) la sua MA.
    mode='above': protezione da sistema morente — disattiva quando equity scende sotto MA.
    mode='below': contrarian — opera solo dopo DD (recovery play, più rischioso).
    """
    ma = equity.rolling(ma_period).mean()
    return equity > ma if mode == "above" else equity < ma
```

**Logica classica Unger**: operare solo quando l'equity è sopra la propria MA.
Aiuta a proteggersi in caso di sistema morente — quando l'equity comincia a non recuperare
la MA, il sistema viene messo in pausa, riducendo automaticamente l'esposizione.

---

## 6. Z-Score / Trade Dependency

I trade non sono indipendenti se la strategia ha una struttura. Lo Z-Score misura
la dipendenza tra trade consecutivi.

### 6.1 Formula
```
              N * (R - 0.5) - X
Z-Score = ─────────────────────────
            sqrt( X*(X-N) / (N-1) )

N = numero totale di trade
R = numero di stringhe (sequenze consecutive di W o L)
W = winners
L = losers
X = 2 * W * L
```

### 6.2 Interpretazione

| Z-Score | Livello confidenza | Comportamento |
|---------|-------------------|---------------|
| Z > +1.96 | 95% | **Alternanza** (più stringhe del caso) |
| Z > +2.58 | 99% | Alternanza forte |
| -1.64 < Z < +1.64 | < 90% | **Indeterminato** — non agire |
| Z < -1.96 | 95% | **Continuazione** (meno stringhe del caso) |
| Z < -2.58 | 99% | Continuazione forte |

### 6.3 Implicazioni operative

- **Z > +1.96**: dopo una vincita o serie di vincite → considerare di **saltare** il prossimo trade
- **Z < -1.96**: dopo una perdita → considerare di saltare fino a una vincita
- **|Z| < 1.64**: i trade sono indipendenti → non modificare l'operatività

> ⚠️ Lo Z-Score va calcolato su un campione esteso (≥ 100 trade). Su campioni piccoli
> qualunque deviazione può sembrare significativa.

---

## 7. Monte Carlo

### 7.1 Idea base — "stessi trade, ordine diverso"

I trade reali sono una particolare permutazione di una popolazione di esiti.
Permutando l'ordine 1000+ volte si ottiene la distribuzione di:
- Net Profit alternativi
- Max DD alternativi
- Lunghezza serie perdenti alternative

### 7.2 Implementazione

```python
def monte_carlo(trades: pd.Series, n_sim: int = 1000) -> pd.DataFrame:
    """
    Permuta n_sim volte la sequenza di trade. Restituisce per ogni simulazione:
    net_profit, max_dd, max_consec_losers.
    """
    results = []
    for _ in range(n_sim):
        shuffled = trades.sample(frac=1).reset_index(drop=True)
        cum = shuffled.cumsum()
        dd = (cum - cum.cummax()).min()
        # max consecutive losers
        losers = (shuffled < 0).astype(int)
        max_consec = losers.groupby((losers != losers.shift()).cumsum()).sum().max()
        results.append({"net_profit": cum.iloc[-1], "max_dd": dd,
                         "max_consec_losers": max_consec})
    return pd.DataFrame(results)
```

### 7.3 Cosa cercare

**Per validazione OOS**:
- Il DD reale OOS deve essere migliore (o non peggiore) del 95° percentile delle permutazioni
- Se peggiore: l'equity OOS è frutto di una sequenza fortunata, non di edge robusto

**Per stima "cosa aspettarsi"**:
- Distribuzione dei DD attesi a 1, 2, 3, 10 anni
- 5° percentile = ottimistico; 95° percentile = pessimistico; mediana = realistico
- Permette di definire un budget di DD massimo accettabile prima di andare live

### 7.4 Lunghezza simulazione

Se la strategia ha 75 trade/anno, simulare:
- Serie di 75 trade → distribuzione 1-year
- Serie di 150 trade → distribuzione 2-year
- Serie di 750 trade → distribuzione 10-year

Permette di confrontare il proprio storico col distribution range a quel orizzonte.

### 7.5 Limiti

- MC assume **indipendenza dei trade** — se Z-Score è significativo, il MC sovrastima la variabilità
- MC non simula **regime change** — se il mercato cambia carattere, la distribuzione storica non è più rappresentativa
- MC basato su trade chiusi non vede il **rischio di liquidità** (gap, eventi estremi)

---

## 8. Riottimizzazione periodica & Walk-Forward

### 8.1 Riottimizzazione manuale
Periodicamente (es. ogni 6-12 mesi):
1. Rilanciare l'ottimizzazione su un periodo allungato (incluso l'ultimo periodo live)
2. Confrontare i nuovi parametri con quelli attuali
3. Verificare metriche di valutazione (cap. 2)
4. Se i parametri sono cambiati ma la curva non migliora → non cambiarli (rumore)
5. Se i nuovi parametri migliorano stabilmente → migrazione graduale

### 8.2 Walk-Forward Optimization
Quando il processo di riottimizzazione diventa sistematico e si trasla continuamente,
si parla di **Walk-Forward Optimization (WFO)**:

```
Window 1:  [─── IS ───][─OOS─]
Window 2:           [─── IS ───][─OOS─]
Window 3:                    [─── IS ───][─OOS─]
                                       ...

Risultato live = concatenazione degli OOS
```

### 8.3 Implementazione WFO

```python
def walk_forward(df, run_fn, is_pct: float = 0.7, n_windows: int = 5):
    """
    Divide il dataset in N finestre. Su ciascuna: ottimizza su is_pct di IS,
    valida sul resto come OOS. Restituisce equity OOS concatenate.
    """
    n = len(df); window = n // n_windows
    results = []; chosen_params_log = []
    for i in range(n_windows):
        start = i * window; end = start + window
        split = start + int(window * is_pct)
        is_df = df.iloc[start:split]
        oos_df = df.iloc[split:end]
        best_params = run_fn(is_df, mode="optimize")
        chosen_params_log.append(best_params)
        oos_result = run_fn(oos_df, mode="run", params=best_params)
        results.append(oos_result)
    return pd.concat(results), chosen_params_log
```

### 8.4 Analisi dei parametri WFO

Oltre all'equity OOS, è cruciale guardare ai **parametri scelti finestra per finestra**:
- Se cambiano drasticamente → instabilità, sistema fragile
- Se restano simili → stabilità, scelta robusta
- Se hanno un pattern (es. stop sempre crescente) → drift del mercato

---

## 9. Influenza del time frame

Dal corso TSE, una distinzione fondamentale:

### Time frame brevi (minuti)
- Il **rumore di fondo** crea situazioni molto variabili da mercato a mercato
- Ogni mercato dev'essere studiato per sé
- Gli studi sui pattern hanno senso (sfruttano la microstruttura)
- Strategia da un mercato spesso **non applicabile** ad un altro

### Time frame lunghi (daily, 1440min)
- Si può usare un'**ottica di prezzo** valida per tutti i mercati
- Stesso motore applicabile a panieri ampi
- Logiche di puro prezzo, indipendenti dal singolo mercato
- Si tengono tutti i sistemi del paniere — alcuni andranno male oggi, bene domani

> Differenze pratiche dipendono da: tick value, volatilità, range medi, BPV, lunghezza session,
> sovrapposizione con altri mercati. Es. DAX su 30min: le azioni sottostanti aprono alle 8.00
> mentre il Future apre alle 9.00 → microstruttura unica del DAX.

---

## 10. Checklist finale prima di andare live

- [ ] Net Profit positivo IS + OOS
- [ ] Numero trade IS ≥ 50; OOS ≥ 20
- [ ] Avg trade ≥ 6-7 tick AND ≥ 15% range medio
- [ ] Drawdown OOS non peggiore del 95° percentile MC
- [ ] Z-Score nel range [-1.64, +1.64] (o gestito esplicitamente)
- [ ] Parametri al centro di un plateau (non picco isolato)
- [ ] OOS non significativamente migliore di IS (sospetto look-ahead)
- [ ] Win rate < 80% se trade < 50 (altrimenti sospetto artefatto)
- [ ] Forward validation paper trading ≥ 2 mesi superato
- [ ] Sistema replicabile in real-time (ordini, slippage, commissioni testati)
- [ ] Sistema sopportabile psicologicamente (test su lunghe perdite passate)
- [ ] Sistema scelto secondo decision table di `position_sizing.md`
- [ ] Sistema non correlato con altri in portafoglio — vedi `portfolio.md`
