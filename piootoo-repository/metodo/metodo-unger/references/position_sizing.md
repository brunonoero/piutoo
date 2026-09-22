# Position Sizing — riferimento completo

Fonte sintetica: Unger Academy — *Mastering Position Sizing* (MPS), *Portfolio Management* (PS).

> "Calcolare QUANTO utilizzare per ogni posizione."
>
> Money Management completo = **Stop Loss + Take Profit dinamici + RR Ratio + Position Sizing**.

---

## 1. Modello: Contratto Singolo

Il modello più semplice — ogni trade con 1 contratto, indipendentemente dal capitale.

**Quando si usa:**
- Sviluppo e validazione iniziale (equity comparabili tra strategie)
- Trader che ritirano i profitti regolarmente
- Confronto tra modelli alternativi su stessa base

**Limiti:**
- Non scala l'equity nei periodi positivi (perde l'effetto compounding)
- Non si riduce automaticamente nei DD (no risk management dinamico)

---

## 2. Modello: Size legata ai Margini

```
contracts = floor(equity / margin_required)
```

**Quando si usa:** raro, considerato troppo aggressivo.
Il margine richiesto dallo strumento è ben sotto il rischio reale del trade.

---

## 3. Modello: Percent f (%f)

```
              equity × f%
contracts = ┌────────────┐
            └    WCS     ┘
```

dove WCS è il **Worst Case Scenario** scelto.

### 3.1 Formula

```python
def percent_f(equity: float, f_pct: float, wcs: float) -> int:
    """Es: percent_f(100_000, 2, 1500) → 1 contratto (100k * 2% / 1500 = 1.33)"""
    return int(equity * f_pct / 100 / wcs)
```

### 3.2 Worst Case Scenario alternatives

| WCS | Caratteristica | Tipico per |
|-----|---------------|-----------|
| **Stop Loss della strategia** | aggressivo, si usa il livello programmato | sistemi con SL chiaro e affidabile |
| **Peggior perdita storica** | conservativo, riflette eseguiti reali (gap, slippage) | sistemi maturi con storico significativo |
| **Media degli N peggiori trade** | bilanciato | quando il peggiore è anomalo |
| **Worst Day** | considera anche eventi multi-trade nel giorno | sistemi multi-entry intraday |
| **Worst Week / Month** | molto conservativo | sistemi swing/positional |
| **Max DD storico** (→ Secure f) | il più conservativo | sistemi con DD lunghi e profondi |

### 3.3 Esempi numerici

```
Capitale = 100.000 EUR, WCS = 1.500 EUR, Rischio = 2%
→ 100.000 × 2% = 2000 EUR (max perdita per operazione)
→ 2000 / 1500 = 1.333 → 1 contratto

Capitale = 250.000 EUR, WCS = 1.500 EUR, Rischio = 2%
→ 250.000 × 2% = 5000 EUR
→ 5000 / 1500 = 3.33 → 3 contratti

Capitale = 50.000 EUR, WCS = 1.500 EUR, Rischio = 2%
→ 50.000 × 2% = 1000 EUR
→ 1000 / 1500 = 0.66 → impossibile operare!
```

### 3.4 Pro e contro

| Pro | Contro |
|-----|--------|
| Semplice da capire e implementare | Cresce esponenzialmente con l'equity |
| Si adatta alle fluttuazioni dell'equity | DD percentuale può diventare consistente |
| Funziona bene in portafoglio | Sopra una certa equity diventa molto aggressivo |

### 3.5 Progressione tipica

| Capitale | Rischio (2%) | Contratti | Trade -275 → cap |
|----------|--------------|-----------|------------------|
| 50.000 | 1.000 | 3 | 49.175 |
| 52.100 | 1.042 | 3 | 51.275 |
| 54.800 | 1.096 | 3 | 53.975 |
| 56.900 | 1.138 | 4 | 55.800 |

---

## 4. Modello: Fixed Ratio (Ryan Jones)

```
                  ┌  1 + sqrt(1 + 8 * (E - Ei) / Delta)  ┐
contracts = floor │  ─────────────────────────────────── │
                  └                  2                   ┘
```

### 4.1 Formula

```python
import math
def fixed_ratio(equity: float, equity_init: float, delta: float) -> int:
    return int((1 + math.sqrt(1 + 8 * (equity - equity_init) / delta)) / 2)
```

### 4.2 Logica
- Si parte con **1 contratto** (indipendentemente dal capitale, salvo margini)
- Per passare a 2 contratti serve un profitto totale di Delta
- Per passare a 3 servono 3 × Delta (cumulato)
- Per passare a n servono Σ(i=1..n-1) i × Delta = n(n-1)/2 × Delta

### 4.3 Esempio progressione

Delta = 5.000 EUR, capitale iniziale 100.000:

| Contratti | Equity richiesta |
|-----------|------------------|
| 1 (start) | 100.000 |
| 2 | 105.000 (+5.000) |
| 3 | 115.000 (+10.000) |
| 4 | 130.000 (+15.000) |
| 5 | 150.000 (+20.000) |
| 6 | 175.000 (+25.000) |

### 4.4 Pro e contro

| Pro | Contro |
|-----|--------|
| Partenza aggressiva possibile | DD iniziale può essere notevole |
| Progressione decelera con la crescita | Scelta Delta è arbitraria |
| DD percentuali contenuti su grandi capitali | Sensibile ai parametri se non personalizzato |

---

## 5. Modello: Percent Volatility (%Vol, K. Van Tharp)

```
              equity × %Vol
contracts = ┌──────────────┐
            └  ATR × BPV   ┘
```

### 5.1 Formula

```python
def percent_volatility(equity: float, vol_pct: float, atr: float, bpv: float) -> int:
    return int(equity * vol_pct / 100 / (atr * bpv))
```

### 5.2 Calcolo ATR

L'ATR (Average True Range) si calcola sul periodo che ha senso rispetto alla durata media
del trade. Per overnight: ATR 20-45 giorni. Per intraday: ATR delle barre operative.

```
True Range:
TRi = max( Hi - Li, |Hi - Ci-1|, |Li - Ci-1| )

ATR(n) = media degli ultimi n TR
```

### 5.3 Esempio numerico

```
Capitale = 250.000 EUR
ATR = 4.576 punti
BPV = 1.000 USD
%Vol = 3%

250.000 × 3% = 7.500 EUR (max fluttuazione giornaliera attesa)
7.500 / (4.576 × 1.000) = 1.639 → 1 contratto
```

### 5.4 Pro e contro

| Pro | Contro |
|-----|--------|
| Lega la size alla volatilità corrente | Calcolo ATR varia tra implementazioni |
| Eccellente in portafoglio diversificato | Meno semplice da applicare in ogni forma |
| Naturale per portafogli multi-mercato | Adeguamento posizioni aperte al variare di ATR? |

---

## 6. Risk of Ruin

Probabilità che l'equity scenda al livello dove non si possa (o voglia) più operare.

### 6.1 Stima base (gambling, win = loss)

```
              ┌  1 - W  ┐ ^ (1 / f)
RoR  =        │  ─────  │
              └    W    ┘

W = winning %, f = % di rischio per operazione
```

Adatta al gambling: assume payoff 1:1 (avg_win = avg_loss).
Per avere RoR < 100% serve W > 0.5.

### 6.2 Stima completa (Perry Kaufman)

```
Step 1 — ritorno medio e quadratico medio:
   E  = Σ PLi × pi              # ritorno medio per trade atteso
   E² = Σ PLi² × pi             # ritorno quadratico medio

Step 2 — probabilità e capitale a rischio normalizzato:
   CA = capitale a rischio (da inizio al "Ruin")
   D  = CA / sqrt(E²)            # capitale a rischio normalizzato
   P  = 0.5 + E / (2 × sqrt(E²))  # probabilità di trade "favorevole"

Step 3 — Risk of Ruin:
   RoR = ((1 - P) / P) ^ D
```

### 6.3 Implementazione

```python
import numpy as np

def risk_of_ruin_kaufman(trades: pd.Series, capital_at_risk: float) -> float:
    """
    Trades: serie di P&L per trade.
    capital_at_risk: capitale che si è disposti a perdere (es. 30% equity).
    """
    pls = trades.values
    pi = np.ones(len(pls)) / len(pls)            # probabilità empiriche uniformi
    E = (pls * pi).sum()
    E2 = (pls**2 * pi).sum()
    if E2 <= 0:
        return 1.0
    P = 0.5 + E / (2 * np.sqrt(E2))
    D = capital_at_risk / np.sqrt(E2)
    if P <= 0 or P >= 1:
        return 0.0 if P >= 1 else 1.0
    return ((1 - P) / P) ** D
```

---

## 7. Modelli avanzati

### 7.1 Reduced f

%f decrescente al crescere dell'equity. Più aggressivo all'inizio, più conservativo dopo.

| Equity | %f |
|--------|------|
| ≤ 2 × Ei | 10% |
| 2-3 × Ei | 7.5% |
| 3-4 × Ei | 5% |
| 4-7 × Ei | 2.5% |
| > 7 × Ei | 1% |

```python
def reduced_f(equity: float, equity_init: float, wcs: float) -> int:
    ratio = equity / equity_init
    if ratio <= 2:   f = 10
    elif ratio <= 3: f = 7.5
    elif ratio <= 4: f = 5
    elif ratio <= 7: f = 2.5
    else:            f = 1
    return int(equity * f / 100 / wcs)
```

**Filosofia**: si accetta più rischio quando il capitale è "fresco" (ipotetica fase di test
del mercato), si protegge il capitale accumulato.

### 7.2 Aggressive Ratio

Delta del Fixed Ratio decrescente al crescere dell'equity → più aggressivo coi profitti.

| Equity | Delta effettivo |
|--------|-----------------|
| ≤ 2 × Ei | Delta base |
| 2-3 × Ei | Delta × 0.75 |
| 3-4 × Ei | Delta × 0.50 |
| 4-7 × Ei | Delta × 0.35 |
| > 7 × Ei | Delta × 0.25 |

```python
def aggressive_ratio(equity: float, equity_init: float, base_delta: float) -> int:
    ratio = equity / equity_init
    if ratio <= 2:   d = base_delta
    elif ratio <= 3: d = base_delta * 0.75
    elif ratio <= 4: d = base_delta * 0.50
    elif ratio <= 7: d = base_delta * 0.35
    else:            d = base_delta * 0.25
    return int((1 + (1 + 8 * (equity - equity_init) / d) ** 0.5) / 2)
```

**Filosofia**: opposta di Reduced f — il rischio cresce con la crescita perché "abbiamo
testato la strategia e funziona".

### 7.3 Asymmetric Ratio (Ryan Jones)

I livelli di crescita dei contratti sono fissati salendo, ma quando l'equity scende
i livelli si **avvicinano** (ratchet) per rallentare il DD.

Esempio Delta = 5000:
| Equity | Contratti |
|--------|-----------|
| da 50k a 55k | 1 |
| da 55k a 65k | 2 |
| da 65k a 80k | 3 |
| da 80k a 100k | 4 |
| **da 100k a 90k** | **4** (mantiene) |
| **da 90k a 77.5k** | **3** (livello compresso) |
| **da 77.5k a 65k** | **2** |
| sotto 65k | 1 |

### 7.4 Timid Bold Equity (Van Tharp)

Rischio Timid sul capitale di base, Bold sul "Market Money" (i guadagni).
Reset quando l'equity raggiunge soglie predefinite.

| Equity | Timid 2% | Bold 5% | Somma a rischio |
|--------|---------|---------|-----------------|
| 100.000 | 100k × 2% | — | 2.000 |
| 110.000 | 100k × 2% | 10k × 5% | 2.500 |
| 140.000 | 100k × 2% | 40k × 5% | 4.000 |
| **150.000 reset** | 150k × 2% | — | 3.000 |
| 180.000 | 150k × 2% | 30k × 5% | 4.500 |
| 200.000 | 150k × 2% | 50k × 5% | 5.500 |
| **250.000 reset** | 250k × 2% | — | 5.000 |

### 7.5 Mischiare le tecniche

Per portafogli complessi e periodi lunghi, **combinare** modelli può essere efficace:
- Fixed Ratio nella fase iniziale (crescita aggressiva)
- Passaggio a %f sopra una soglia (controllo del rischio)
- Equity Curve Trading come "interruttore" (vedi `validazione.md`)

---

## 8. Quale modello quando — decision table

| Nr Sistemi | Modello | Suggerito | Alternativa |
|-----------|---------|-----------|-------------|
| **< 5** | modelli simili | Fixed Ratio | %f su SL o Worst Day |
| **< 5** | modelli diversi | Personalizzato per sistema | %f su Worst Day |
| **5–10** | modelli simili | Fixed Ratio | %f su Worst Day |
| **5–10** | modelli diversi | %f su Worst Day | Fixed Ratio |
| **10–25** | modelli simili | %f su Worst Day | Fixed Ratio |
| **10–25** | modelli diversi | %f su Worst Day | Percent Volatility |
| **> 25** | indifferente | %f su Worst Day | Position Sizing "a mercato" |

### 8.1 Preferenza Unger
**%f su Worst Day**, tipicamente 1.25% sul capitale totale. Motivazioni:
- Amalgama bene sistemi con caratteristiche diverse (multi-frequency, multi-TF)
- Facile da applicare e calcolare
- Adattivo: si autoadegua al variare dell'equity
- Trasparente: il rischio per operazione è esplicito

---

## 9. Position Sizing "a mercato"

Quando ci sono molti sistemi sullo stesso strumento, il rischio è la **sovra-concentrazione**:
4 sistemi su DAX significano potenzialmente 4 posizioni aperte sullo stesso mercato.

Approccio empirico (no formula, buon senso ed esperienza):

```python
def market_aware_size(market_atr_dollar: float, n_active_systems: int) -> int:
    """
    Dimensionamento per fasce di volatilità con fattore correttivo sul numero
    di sistemi attivi sullo stesso strumento.
    """
    # Fascia 1: low ATR
    if market_atr_dollar < 400:
        base_contracts = 8; max_systems = 4
    elif market_atr_dollar < 800:
        base_contracts = 5; max_systems = 4
    elif market_atr_dollar < 1500:
        base_contracts = 3; max_systems = 4
    elif market_atr_dollar < 2500:
        base_contracts = 2; max_systems = 4
    else:
        base_contracts = 1; max_systems = 4

    if n_active_systems <= max_systems:
        return base_contracts
    # se troppi sistemi attivi, dimezza i contratti per sistema
    return max(1, base_contracts // 2)
```

> La tabella è solo un esempio. Va calibrata sul singolo trader / portafoglio.

---

## 10. Mastering — confronto sui medesimi dati

Dal corso MPS, confronto su portafoglio di 5 strategie diverse (Live Cattle Swing,
Lean Hogs Swing, miniSP500 Counter, Silver Swing Trend, Natural Gas Counter)
dal 2010 in poi, capitale iniziale $1.000.000:

| Modello | Net Profit | Max DD % | Caratteristica |
|---------|-----------|----------|----------------|
| Fixed Ratio (Delta=10k) | 6.948.191 | 17% | DD più contenuto, ma concentra su strategie ricche |
| Percent Volatility | 6.864.714 | 22.4% | Comportamento bilanciato |
| %f con Worst Day | 6.788.641 | 19% | Andamento più regolare anno per anno |
| %f con Stop Loss | 6.910.652 | 20.7% | Sensibile a strategie con SL stretti |

**Conclusione (preferenza Unger)**: il `%f su Worst Day` produce l'andamento più
regolare anno per anno e valorizza meglio la **cooperazione** tra strategie diverse,
mentre il Fixed Ratio tende a puntare sulle strategie più "ricche".

---

## 11. Linee guida finali

- **Mai** dimensionare sopra il punto di confort psicologico — un DD oltre la zona di
  comfort fa abbandonare la strategia, anche se matematicamente ottimale.
- **Equity Curve Trading + Position Sizing** è la combo più potente per longevità: si
  riduce automaticamente l'esposizione quando l'equity flette.
- **Mai joint optimization** tra parametri di trading e parametri di sizing — sono livelli
  decisionali separati.
- **Backtestare il sizing** è obbligatorio: lo stesso storico con sizing diversi può
  produrre traiettorie radicalmente diverse.
- Vedi `portfolio.md` per come il sizing si integra nella gestione di portafoglio.
- Vedi `validazione.md` per le metriche di valutazione del sistema dimensionato.
