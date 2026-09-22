# Catalogo Mercati e Strumenti — riferimento completo

Fonte sintetica: Unger Academy — *Portfolio Management* (PS), *Mastering Position Sizing* (MPS).

> Lo studio dei mercati è il prerequisito per la scelta degli strumenti da inserire in portafoglio.
> Ogni mercato ha caratteristiche operative proprie: tick size, BPV, sessione, liquidità,
> volatilità, $ATR di riferimento, range di stop tipici, alternative scalabili.

---

## 1. Tre famiglie di mercato (overview)

| Famiglia | Caratteristiche chiave | Position Sizing | Diversificazione |
|----------|------------------------|-----------------|------------------|
| **Forex** | Grande liquidità, non regolamentato, broker-sensitive | Eccellente (lotti decimali) | Attenzione a valuta "guida" |
| **Futures** | Diversificazione, costi alti per piccoli capitali | Impatto "lento" (contratti interi) | Grandi possibilità |
| **Azioni** | Diversificazione settoriale, short complesso | Limitato (commissioni minime) | Approccio di portafoglio |

---

## 2. Index Futures

### MiniSP500 (ES)
| Parametro | Valore |
|-----------|--------|
| Carattere principale | Mean Reverting |
| Liquidità | Molto elevata |
| Orari | Intera sessione |
| Tick size | 0.25 ($12.50) |
| Big Point Value | $50 |
| $ATR 45gg | $820 |
| Stop intraday tipico | $600 - $900 |
| Stop overnight tipico | $600 - $2.500 |
| Sviluppo sistemi | Buona |
| Alternative | MiniDow (meno liquido, leggermente più volatile) |

**Note operative**: il miniSP ha un forte carattere mean-reverting. Approcci breakout
funzionano peggio dei reversal. Il bias temporale è meno marcato che su altri mercati.

### DAX Future (FDAX)
| Parametro | Valore |
|-----------|--------|
| Carattere principale | Trending + BIAS |
| Liquidità | Elevata |
| Orari | Intera sessione (8-22 ora Roma) |
| Tick size | 0.50 (€12.50) |
| Big Point Value | €25 |
| $ATR 45gg | €2.850 |
| Stop intraday tipico | €1.000 - €2.500 |
| Stop overnight tipico | €1.000 - €5.000 |
| Sviluppo sistemi | Buona |
| Alternative | MiniDAX (1/5, tick doppio, meno liquido ma valido) |

**Market mover**: Disoccupazione USA (Nonfarm Payrolls primo venerdì), ZEW Index,
annunci BCE.

**⚠️ Trap della close**: le barre daily del DAX mostrano come close il *settlement close*,
**non** l'ultimo prezzo scambiato. **Per pattern correttamente costruiti usare barre da
1440 minuti, non daily.**

### EuroStoxx50 (FESX)
| Parametro | Valore |
|-----------|--------|
| Carattere principale | Varie |
| Liquidità | Elevata |
| Orari | Intera sessione |
| Stop intraday tipico | €200 - €500 |
| Stop overnight tipico | €400 - €1.000 |
| Sviluppo sistemi | Limitata |
| Alternative | MiniDAX |

**Note**: nonostante l'alta correlazione con DAX, l'EuroStoxx non è un mercato facile a
livello sistematico. Caratteristiche meno marcate, edge più sfuggente.

### MiniNasdaq (NQ)
| Parametro | Valore |
|-----------|--------|
| Carattere principale | Trending |
| Liquidità | Buona |
| Orari | Meglio sessione diurna |
| $ATR 45gg | $800 |
| Stop intraday tipico | $700 - $1.500 |
| Stop overnight tipico | $700 - $2.500 |
| Sviluppo sistemi | Scarsa |
| Alternative | MiniRussell (RTY) |

**Note**: il Nasdaq attira per la velocità ma le caratteristiche sono difficili da
sfruttare sistematicamente. Per operare velocità il MiniRussell è meglio strutturato.

---

## 3. Energy Futures

### Crude Oil (CL)
| Parametro | Valore |
|-----------|--------|
| Carattere principale | Trending + BIAS |
| Liquidità | Buona |
| Orari | Intera sessione (18:00 - 17:00 NY) |
| Tick size | 0.01 ($10) |
| Big Point Value | $1.000 |
| $ATR 45gg | $1.100 |
| Stop intraday tipico | $1.000 - $1.500 |
| Stop overnight tipico | $1.500 - $3.500 |
| Sviluppo sistemi | Buona |
| Alternative | MiniCrude (1/2, valida per capitali minori) |

**Market mover**: EIA Petroleum Status Report (settimanale ~10:30 NY), notizie politiche
e macroeconomiche.

**Note**: forte BIAS temporale (es. short 19:00-12:00, long 12:00-17:00).
Mercato che premia approcci trend-following classici e bias structurati.

### Heating Oil (HO)
| Parametro | Valore |
|-----------|--------|
| Carattere principale | Trending |
| Liquidità | Buona diurna, scarsa notturna |
| Stop intraday tipico | $1.000 - $1.500 |
| Stop overnight tipico | $1.500 - $3.500 |
| Sviluppo sistemi | Buona |
| Alternative | CL (più liquido), RB (più impulsivo) |

### Gasoline (RB)
Analogo a HO, leggermente più impulsivo.

### Natural Gas (NG)
| Parametro | Valore |
|-----------|--------|
| Carattere principale | Varie (alta volatilità imprevedibile) |
| Liquidità | Buona |
| $ATR 45gg | $1.000 |
| Stop intraday | $1.000 - $1.500 |
| Stop overnight | $1.500 - $5.000 |
| Alternative | MiniNG (illiquido, non proponibile) |

**Note**: mercato peculiare con caratteristiche stagionali e shock notturni.
Pattern meno classici, richiede approccio dedicato.

---

## 4. Metals

### Gold (GC)
| Parametro | Valore |
|-----------|--------|
| Carattere principale | Trending + BIAS |
| Liquidità | Buona |
| Tick size | 0.10 ($10) |
| Big Point Value | $100 |
| $ATR 45gg | $1.300 |
| Stop intraday | $1.000 - $2.000 |
| Stop overnight | $1.500 - $5.000 |
| Sviluppo sistemi | Buona |
| Alternative | MicroGold (1/10, valida per piccoli capitali) |

### Silver (SI)
$ATR $1.350 — stop intraday $1.500-$2.500. Sviluppo limitato. No alternative reali.

### Copper (HG)
$ATR $1.350 — varie caratteristiche. Sviluppo medio.

### Platinum (PL)
$ATR $820 — trending. Correlato a Gold. Meglio sessione diurna.

---

## 5. Grains

| Strumento | Carattere | $ATR | Stop intra | Stop over | Sviluppo |
|-----------|----------|------|-----------|-----------|----------|
| Corn (ZC) | Varie | $1.300 | $500-$1.000 | $700-$2.000 | Scarsa |
| Soybeans (ZS) | Varie | $1.350 | $700-$1.600 | $1.000-$2.000 | Media |
| Wheat (ZW) | Varie | $1.350 | $700-$1.600 | $1.000-$2.000 | Limitata |

**Note**: i grani hanno stagionalità marcata. No alternative real-time scalabili.

---

## 6. Bonds

| Strumento | Carattere | Liquidità | Stop intra | Sviluppo |
|-----------|----------|-----------|-----------|----------|
| EuroBund (FGBL) | Varie | Elevata | €500-€1.000 | Media |
| 30Y T-Bond (ZB) | Varie | Elevata | $700-$1.600 | Media |
| 10Y T-Note (ZN) | Varie | Elevata | $700-$1.600 | Media |

**Alternative**: Bobl, BTP Future (potenzialmente interessante in relazione al Bund),
OAT Future. Ultrabond solo "per dovere", non realmente utile.

---

## 7. Meats

| Strumento | Carattere | Liquidità | Stop intra | Sessione |
|-----------|----------|-----------|-----------|----------|
| Live Cattle (LE) | Varie | Media | $400-$1.000 | Breve |
| Feeder Cattle (GF) | Varie | Media | $500-$1.500 | Breve |
| Lean Hogs (HE) | Varie | Media | $400-$1.000 | Breve |

**Note**: sessioni brevi (4-5 ore), correlazione tra LE e GF. Le carni sono
"tra i meno puliti" — il Live Cattle è considerato il migliore della categoria.

---

## 8. Softs

| Strumento | Carattere | $ATR | Stop intra | Note |
|-----------|----------|------|-----------|------|
| Cocoa (CC) | Varie | $520 | $400-$1.000 | Peculiare |
| Sugar (SB) | Varie | $580 | $500-$1.000 | Peculiare |
| Coffee (KC) | Varie | $1.150 | $700-$1.500 | Peculiare |

**Note**: ognuno con caratteristiche proprie, no alternative reali.

---

## 9. FX Futures e Forex

### FX Futures CME
| Strumento | $ATR | Carattere |
|-----------|------|-----------|
| EurFX (6E) | $900 | Varie |
| BritishPound (6B) | $630 | Trending |
| Yen (6J) | $950 | Trending |
| Aussie (6A) | varie | Varie |
| NZD (6N) | $600 | Varie |
| Swiss Franc (6S) | $800 | Trending |
| Canadian (6C) | $450 | Varie |

### Forex
**Vantaggi:**
- Migliore scalabilità (lotti decimalizzabili)
- Eccellente per Position Sizing su capitali limitati
- Coppie con caratteristiche TF o MR (es. EURUSD MR, USDJPY trending)
- Diversificazione possibile ma occhio alla **valuta "guida"** del movimento

**Svantaggi:**
- Non regolamentato (importante scelta del broker per evitare frodi)
- Sessione 24/5 — definire bene la "giornata" di riferimento

**Riferimento sessione Forex:**
- Sessioni: Sydney → Tokyo → Londra → New York
- Per backtest sistematico: allinearsi alla close del future regolamentato (EuroFX su CME)
- Chicago time: 16:00-16:00 = sessione completa 24h
- Adattare a fuso orario del data feed:
  - NY time → 17:00-17:00
  - Londra → 22:00-22:00
  - Europa centrale → 23:00-23:00

---

## 10. Tabella riassuntiva $ATR 45gg

| Strumento | $ATR | Famiglia | Strumento | $ATR | Famiglia |
|-----------|------|----------|-----------|------|----------|
| Cocoa | 520 | Softs | Corn | 1.300 | Grains |
| Sugar | 580 | Softs | Soybeans | 1.350 | Grains |
| Coffee | 1.150 | Softs | Wheat | 1.350 | Grains |
| Lean Hogs | 610 | Meats | DAX | €2.850 | Index |
| Live Cattle | 640 | Meats | MiniSP500 | 820 | Index |
| Feeder Cattle | 1.050 | Meats | MiniND | 800 | Index |
| Crude Oil | 1.100 | Energy | EuroStoxx | €350 | Index |
| Heating Oil | 1.500 | Energy | Bund | €750 | Bonds |
| Gasoline | 1.700 | Energy | 30Y T-Bond | 1.250 | Bonds |
| Natural Gas | 1.000 | Energy | 10Y T-Note | 500 | Bonds |
| Gold | 1.300 | Metals | BP Future | 630 | FX |
| Silver | 1.350 | Metals | EurUsd | 900 | FX |
| Copper | 1.350 | Metals | JPY | 950 | FX |
| Platinum | 820 | Metals | NZD | 600 | FX |

---

## 11. Liquidità ranking

| Tier | Strumenti |
|------|-----------|
| **Molto liquidi** | miniSP, Bonds (FGBL, ZB, ZN), Forex, EuroStoxx, Corn |
| **Liquidi** | CL, DAX, FX Futures, Gold, Copper, NG, NQ, Sugar, Soybeans, Wheat |
| **Accettabili** | Cocoa, Lean Hogs, HO, FederCattle, Coffee, Live Cattle, Platinum, RB, Silver, Soy Oil |
| **Scarsi** | Cotton, Orange Juice |

---

## 12. Costo (BPV) ranking

| Categoria | Strumenti |
|-----------|-----------|
| **Molto costosi** | DAX, Metals (specialmente full-size) |
| **Costosi** | CL, FX Futures, HO, Coffee, NG, NQ, Platinum, RB |
| **Economici** | Cocoa, Lean Hogs, Live Cattle, British Pound |
| **Migliore** | Forex (scalabilità completa) |

---

## 13. Esempi di costruzione portafoglio

### 13.1 Monomercato CL
```
CL BIAS
CL Trend Following puro
CL Medium Term
```

### 13.2 Monomercato DAX
```
DAX 1st Hour
DAX BIAS
DAX Medium Term
```

### 13.3 Monosettore Energy
```
CL BIAS
HO Trend Overnight
RB Intraday
```

### 13.4 Monosettore Metals
```
Gold BIAS
Silver Progressive Stops
Platinum Trend
```

### 13.5 Portafoglio multi-settore (template)
```
Index:      DAX Trend OR BIAS  +  MiniSP Counter
Metals:     Gold BIAS  +  Silver Medium Term  +  Copper Swing
Energy:     CL BIAS  +  HO Trend  +  RB Intraday
Bonds:      Bund Medium Term  +  ZB Counter
Currencies: FXFader Group (paniere)
Softs:      Feeder Cattle Breakout  +  Lean Hogs Swing
Grains:     ZS Trend  +  ZC Medium Term
```

---

## 14. Implementazione Python — markets.py

```python
from dataclasses import dataclass

@dataclass(frozen=True)
class Instrument:
    symbol: str
    name: str
    family: str
    exchange: str
    tick_size: float
    big_point_value: float
    session_hours: str
    atr_45d_dollar: float
    stop_intraday: tuple
    stop_overnight: tuple
    liquidity: str
    main_character: str
    micro_alternative: str = None

INSTRUMENTS = {
    "ES": Instrument("ES", "MiniSP500", "Index", "CME", 0.25, 50, "17-16",
                      820, (600, 900), (600, 2500), "molto elevata", "mean reverting"),
    "FDAX": Instrument("FDAX", "DAX Future", "Index", "EUREX", 0.5, 25, "8-22",
                       2850, (1000, 2500), (1000, 5000), "elevata", "trending+bias",
                       "MiniDAX"),
    "CL": Instrument("CL", "Crude Oil", "Energy", "NYMEX", 0.01, 1000, "18-17NY",
                     1100, (1000, 1500), (1500, 3500), "buona", "trending+bias",
                     "MiniCrude"),
    "GC": Instrument("GC", "Gold", "Metals", "COMEX", 0.10, 100, "intera",
                     1300, (1000, 2000), (1500, 5000), "buona", "trending+bias",
                     "MicroGold"),
    # ... etc
}

def get_instrument(symbol: str) -> Instrument:
    return INSTRUMENTS[symbol]

def by_family(family: str) -> list[Instrument]:
    return [i for i in INSTRUMENTS.values() if i.family == family]
```

---

## 15. Linee guida finali

- **Mescolare mercati molto diversi** è la regola d'oro della diversificazione
- **Mescolare logiche di ingresso diverse** (TF, Counter, BIAS) all'interno dello stesso mercato
- **Cercare strategie con ingressi in momenti diversi** del giorno
- **Puntare sulle caratteristiche del mercato**: TF per trending markets, Counter per mean-reverting, BIAS per i mercati che lo manifestano
- **Avere molte strategie non significa doverle usare tutte** — quel che conta è la distribuzione
- Vedi `portfolio.md` per la combinazione e il filtering ricorrente
- Vedi `position_sizing.md` per il dimensionamento adatto al mercato
