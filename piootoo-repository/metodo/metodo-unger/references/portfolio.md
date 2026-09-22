# Portfolio Management — riferimento completo

Fonte sintetica: Unger Academy — *Portfolio Management* (PS).

> Il portafoglio è l'**ultimo tassello** della via verso il successo. Una strategia singola,
> anche eccellente, espone a rischio idiosincratico (mercato, regime). Il portafoglio è
> protezione attiva.

---

## 1. Le insidie tipiche

Errori ricorrenti nello sviluppo che portano a portafogli "finti diversificati":

### 1.1 Stesso modello su mercati correlati
Si sviluppa un buon sistema su DAX → si applica a CAC40, EuroStoxx, FTSEMIB. Risultato:
4 strategie, ma posizioni fortemente correlate (gli indici europei si muovono insieme).

### 1.2 Over-filtering
Si forzano filtri su filtri "per ripulire l'equity line" su mercati dove non c'è edge.
Risultato: strategia overfit che funziona solo sul campione storico specifico.

### 1.3 Concentrazione su un mercato
Si sviluppano diverse strategie su uno stesso strumento. Anche se i motori sono diversi,
il rischio è che cambiate caratteristiche del mercato facciano soffrire tutte insieme.

### 1.4 Diversi motori ma stessi orari
Es. DAX BIAS + DAX Reversal che operano entrambi nelle prime ore della giornata.
Quando il DAX è "fuori fase" → entrambi i sistemi soffrono.

### 1.5 Sistemi "incrociati"
Una strategia TF che usa un filtro derivato da un'analisi BIAS. In portafoglio col
sistema BIAS originale → forte correlazione mascherata da diversità apparente.

---

## 2. Quando la diversificazione funziona

Diversi sistemi cooperano se:
- Operano su **mercati strutturalmente diversi** (Index, Energy, Metals, Grains, Bonds, Softs, FX)
- Hanno **logiche di ingresso diverse** (TF, Counter, BIAS, Reversal)
- Entrano in **momenti diversi** della giornata
- Hanno **frequenze operative comparabili** (no mix scalping + long-term TF)
- Non condividono **filtri derivati uno dall'altro**

> **Test diagnostico** dopo la combinazione: sovrapporre le curve di DrawDown delle singole
> strategie. Episodi isolati di DD coincidente sono inevitabili; **ricorrenze sistematiche**
> sono campanello d'allarme — le strategie soffrono insieme.

---

## 3. Caratteristiche dei sistemi confrontabili

### 3.1 Frequenza operativa
Un sistema con 1 trade al mese **non si confronta** con uno con 10 trade/settimana.
Anche se la combinazione funziona bene, è difficile valutarne la reale contribuzione
reciproca. Il sistema "raro" può al massimo essere considerato un **satellite**.

### 3.2 Scala temporale (intraday vs swing)
Confrontare scalping con long-term TF richiede di ragionare sulla **open equity**
(P&L delle posizioni aperte), non sulla close-to-close. Sistemi con orizzonti diversi
generano fluttuazioni di equity diverse.

### 3.3 "Peso" dei sistemi
Non mescolare sistemi su strumenti con $ATR molto diversi senza correzione.
Es: Silver ($ATR 1350) + Cocoa ($ATR 520) — il Cocoa pesa meno in portafoglio
a meno di aumentarne i contratti (ma questa scelta va valutata nel Position Sizing).

---

## 4. Strutture di portafoglio

### 4.1 Monomercato
Più sistemi su uno stesso strumento. Esempio DAX: BIAS + 1st Hour + Medium Term.

**Quando funziona:** strumenti con liquidità elevata, varietà di pattern temporali sfruttabili.
**Quando fallisce:** quando le strategie operano negli stessi momenti o sotto stessi shock.

### 4.2 Monosettore
Più strumenti dello stesso settore. Esempio Energy: CL BIAS + HO Trend + RB Intraday.

**Pro:** focus su una macro-narrativa (es. energia), espertise concentrata.
**Contro:** correlazione settoriale durante shock.

### 4.3 Multi-settore
Portafoglio costruito attraversando i settori. Esempio:
```
Index:        DAX Trend / miniSP Counter
Metals:       Gold BIAS / Silver Medium / Copper Swing
Energy:       CL BIAS / HO Trend / RB Intraday
Bonds:        Bund Medium / 30Y Counter
Currencies:   FXFader Group
Softs:        Feeder Cattle Breakout / Lean Hogs Swing
Grains:       ZS Trend / ZC Medium
```

**Pro:** vera decorrelazione strutturale, robustezza in vari regimi.
**Contro:** richiede capitale significativo e infrastruttura più complessa.

> **Regola pratica:** avere molte strategie **non significa doverle usare tutte** se queste
> non sono distribuite bene su vari mercati.

---

## 5. Capitale di riferimento

Quando si calcolano i contratti, a quale capitale si fa riferimento?

### 5.1 Capitale ripartito (fixed o frazione fissa)
**Capitale fissato per sottoconti:** alla prima spartizione si fissa il capitale per ogni
strategia e si lavora come sottoconti separati.

**Frazione fissa:** si ricalcola periodicamente la suddivisione al variare del totale —
ogni frazione mantiene la proporzione originale.

| Pro | Contro |
|-----|--------|
| Semplicità di gestione | Spesso limita il capitale disponibile |
| Conservativo | A capitale fissato → sovraesposizione su grandi profitti |
| Focus per strategia | A frazione fissa → trascina le perdite tra strategie |
| Può portare al limite per strategia | Difficile con > 4-5 strategie |

### 5.2 Riferimento unico (consigliato per portafogli)
Tutto il capitale è disponibile per ogni strategia. Tre opzioni operative:

1. **Equity con P&L in essere** — quella più comoda. Esempio:
   ```
   Capitale 100k, short DAX in profitto di 4k
   → riferimento: 104.000
   ```

2. **Equity depurata dei margini** delle posizioni aperte. Esempio:
   ```
   Capitale 100k, short DAX (margine 23k)
   → riferimento: 77.000
   ```

3. **Equity depurata di margini + stop loss virtuali**. Esempio:
   ```
   Capitale 100k, short DAX (margine 23k, SL 2.5k)
   → riferimento: 74.500
   ```

> **Consiglio Unger**: opzione 1 con percentuali di rischio meno aggressive.
> Più comoda da calcolare in real-time, semplifica la gestione.

---

## 6. Correlazione

### 6.1 Perché le formule classiche servono poco

La correlazione tradizionale (Pearson su close daily) è di **scarsa utilità** per strategie
sistematiche dinamiche con ingressi a orari/direzioni variabili.

**Il vero problema:** non è la correlazione media, è la **correlazione in stress** — quando
shock di mercato attivano correlazioni altrimenti deboli:
- Brexit → correlazione negativa indici USA vs Gold (di solito debole)
- Elezioni USA → correlazione tra asset risk-off
- COVID 2020 → correlazione di tutto con tutto

Anche gli stop loss possono mettere al riparo da disastri assoluti, ma **fenomeni di questo
tipo possono inquietare** e richiedere disinvestimento emotivo.

### 6.2 Approccio pratico

Misurare la correlazione dei **P&L daily delle strategie** (non dei prezzi sottostanti).

```python
def strategy_correlation(daily_pnls: pd.DataFrame) -> pd.DataFrame:
    """
    daily_pnls: DataFrame con una colonna per strategia, indice datetime.
    Restituisce matrice di correlazione.
    """
    return daily_pnls.corr()
```

### 6.3 Fattore correttivo

Se due strategie mostrano correlazione > 0.75, dividere il numero di contratti calcolato
dal position sizing per `(1 + correlation)`:

```python
def correlation_adjusted_size(base_size: int, correlation: float, threshold: float = 0.75) -> int:
    if correlation > threshold:
        return max(1, int(base_size / (1 + correlation)))
    return base_size
```

Esempio:
- 2 strategie con correlazione 0.8
- Base size: 4 contratti ciascuna
- Adjusted: `4 / (1 + 0.8) = 2.22` → 2 contratti ciascuna

### 6.4 La regola del buon senso

> Mai sovraesporre la stessa **direzione strutturale**, anche con strategie diverse:
> Long DAX + Long miniSP + Short Gold → tutto risk-on, esposizione concentrata.

---

## 7. Filtering ricorrente delle strategie

In portafogli grandi (>10 sistemi) è impraticabile valutare ogni sistema manualmente.
Servono **filtri parametrici** che decidano automaticamente quale strategia tenere "accesa".

### 7.1 Perché dati daily

Si usano i P&L **giornalieri** (close-equity giornaliero) anziché i singoli trade:
- I trade vengono computati solo quando chiusi → finché aperti, possono creare problemi di valutazione
- I dati daily riflettono correttamente la open equity
- Più stabili dei trade per analisi temporali

> Esempio del problema: aprendo long con SL $5.000 overnight, se il giorno dopo è ancora
> aperto e ne apro un altro, il sistema "non sa" del primo. Se il primo è in profitto +$4k
> o in perdita -$4k, lo scenario è molto diverso.

### 7.2 Metriche generiche

```python
def long_term_filter(daily_pnl: pd.Series, lookback_months: int = 12) -> bool:
    """Profitto positivo nell'ultimo periodo lungo."""
    return daily_pnl.tail(lookback_months * 21).sum() > 0

def short_term_filter(daily_pnl: pd.Series, lookback_months: int = 4) -> bool:
    """Profitto positivo nell'ultimo periodo breve."""
    return daily_pnl.tail(lookback_months * 21).sum() > 0
```

### 7.3 Metodi di filtraggio classici

| Metodo | Condizioni |
|--------|-----------|
| **1** | Media(short) > soglia × Media(long) AND Periodo short > 0 |
| **2** | Periodi positivi in short > X% del totale OR periodo long > 0 |
| **3** | Metodo 1 o 2 + AvgGain ultimi N anni > MultoMaxDD × MaxDD ultimi N anni |
| **4** | Metodo 1 o 2 + Equity sopra propria MA a N periodi |
| **5** | Metodo 3 + Metodo 4 (più stringente) |

### 7.4 Quando i filtri funzionano male

Non tutti i filtri funzionano. Misurare quante volte il filtro decide bene:

- **Filtraggio OK > SogliaPos (es. 50%)**: usare il filtro
- **Filtraggio OK ∈ [50%, 60%]**: non usare il filtro (zona grigia)
- **Filtraggio OK < SogliaNeg (es. 40%)** → fa il contrario di cosa serve → considerare l'**uso opposto** del filtro

```python
def filter_quality(filter_decisions: pd.Series, actual_outcomes: pd.Series) -> float:
    """
    filter_decisions: True/False — se il filtro ha attivato la strategia.
    actual_outcomes: True/False — se il periodo era effettivamente profittevole.
    Restituisce % di decisioni giuste del filtro.
    """
    return (filter_decisions == actual_outcomes).mean()
```

### 7.5 Setup operativo Unger (preferenze personali)

| Parametro | Valore |
|-----------|--------|
| Finestra analisi | Mensile (primo sabato del mese) |
| Long Period | 12 mesi |
| Short Period | 4 mesi |
| Condizione attivazione | Long > 0 AND Short > 0 |
| Stabilità storica | AvgGain ultimi 3 anni > 2 × MaxDD ultimi 3 anni |
| Soglia filtri positiva | 40% |
| Soglia filtri negativa | 60% |
| Soglia correlazione | > 0.75 |
| Position Sizing | %f 1.25% su Worst Day |
| Limite DD singola strategia | non > 4% del capitale |

---

## 8. Flowchart operativo

```
                ┌──────────────────────┐
                │  Long Term > 0 ?     │
                └──────────┬───────────┘
                           │ No → strategia SPENTA
                           │ Yes
                           ▼
                ┌──────────────────────┐
                │  Short Term > 0 ?    │
                └──────────┬───────────┘
                           │ No → strategia SPENTA
                           │ Yes
                           ▼
                ┌──────────────────────────────────┐
                │  AvgGain 3y > 2 × MaxDD 3y ?     │
                └──────────┬───────────────────────┘
                           │ No → strategia SPENTA
                           │ Yes
                           ▼
                ┌──────────────────────┐
                │  Calcolo contratti   │
                │  %f 1.25% Worst Day  │
                └──────────┬───────────┘
                           ▼
                ┌──────────────────────────────────┐
                │  MaxDD con tali contratti > 4% ? │
                └──────────┬───────────────────────┘
                           │ Yes → diminuire contratti per limitare DD
                           │ No
                           ▼
                ┌──────────────────────────────────┐
                │  Correlazione con altre > 0.75 ? │
                └──────────┬───────────────────────┘
                           │ Yes → dividere contratti per (1 + corr)
                           │ No
                           ▼
                  ┌─────────────────┐
                  │  STRATEGIA ON   │
                  │  con N contratti│
                  └─────────────────┘
```

---

## 9. Implementazione Python del portfolio runner

```python
import pandas as pd
from dataclasses import dataclass
from typing import Callable

@dataclass
class StrategyState:
    name: str
    daily_pnl: pd.Series
    max_dd_3y: float
    contracts: int = 0
    active: bool = False

def evaluate_portfolio(
    strategies: list[StrategyState],
    capital: float,
    long_months: int = 12,
    short_months: int = 4,
    gain_dd_multiplier: float = 2.0,
    f_pct: float = 1.25,
    dd_limit_pct: float = 4.0,
    correlation_threshold: float = 0.75,
) -> dict:
    """
    Implementa il flowchart §8 sul portafoglio di strategie.
    Restituisce {name: contracts} per le strategie attive.
    """
    active_strategies = []

    # Step 1-3: filtri di base
    for s in strategies:
        long_pnl = s.daily_pnl.tail(long_months * 21).sum()
        short_pnl = s.daily_pnl.tail(short_months * 21).sum()
        avg_gain_3y = s.daily_pnl.tail(3 * 252).sum() / 3
        if long_pnl > 0 and short_pnl > 0 and avg_gain_3y > gain_dd_multiplier * s.max_dd_3y:
            active_strategies.append(s)

    if not active_strategies:
        return {}

    # Step 4: calcolo contratti (esempio %f su Worst Day)
    for s in active_strategies:
        worst_day = s.daily_pnl.min()
        if worst_day >= 0:
            s.contracts = 1
        else:
            s.contracts = max(1, int(capital * f_pct / 100 / abs(worst_day)))

    # Step 5: limite DD per strategia
    for s in active_strategies:
        dd_with_contracts = s.max_dd_3y * s.contracts
        if dd_with_contracts > capital * dd_limit_pct / 100:
            # riduce contratti per stare sotto il limite
            s.contracts = max(1, int(capital * dd_limit_pct / 100 / s.max_dd_3y))

    # Step 6: correlazione
    pnls = pd.DataFrame({s.name: s.daily_pnl for s in active_strategies})
    corr = pnls.corr()
    for i, s in enumerate(active_strategies):
        max_corr = 0
        for j, t in enumerate(active_strategies):
            if i != j and abs(corr.iloc[i, j]) > max_corr:
                max_corr = abs(corr.iloc[i, j])
        if max_corr > correlation_threshold:
            s.contracts = max(1, int(s.contracts / (1 + max_corr)))

    return {s.name: s.contracts for s in active_strategies}
```

---

## 10. Linee guida finali

- **Verifica sempre la curva DD aggregata** dopo aver combinato strategie: l'effetto cooperativo
  è il vero benefit, l'amplificazione è il rischio nascosto.
- **Mai più di 4-5 strategie sulla stessa direzione strutturale** (es. tutte long indici, tutte short bond).
- Il portafoglio è **un livello sopra** la singola strategia — modello di sizing e regole di
  attivazione si applicano al portafoglio nel suo complesso.
- Periodicamente (es. ogni mese) ri-eseguire la valutazione di portafoglio: alcune strategie
  vanno spente, altre riattivate. Vedi `validazione.md` per il framework di valutazione.
- Vedi `position_sizing.md` per i modelli di dimensionamento da usare nel portafoglio.
- Vedi `mercati.md` per scegliere strumenti complementari nel portafoglio.
