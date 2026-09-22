---
name: metodo-unger
description: >
  Esperto del Metodo Unger per il trading algoritmico implementato in Python.
  Usa questa skill ogni volta che l'utente chiede di analizzare, creare, modificare, testare o ottimizzare
  strategie algoritmiche secondo il metodo Unger: motori (TF, Mean Reversion, BIAS, Breakout, Level Fader),
  pattern (NeutralFast, DirectionalFast, PatternFast, UAPtnBase, PtnPSpecchio), filtri temporali e tecnici,
  risk management, valutazione metriche, position sizing avanzato (Fixed Ratio, %f, %Vol, Reduced f,
  Aggressive Ratio, Asymmetric Ratio, Timid Bold), portfolio management (filtering, correlation,
  strategy on/off), gestione portafoglio con il software Titan di Unger Academy (export
  WriteDailies/WriteDailiesCTitanReports, filtri short/long period, MinCap, ranking, correlazione
  AU, portafogli Filtered/Unfiltered/OneContract), catalogo mercati (futures
  Index/Energy/Metals/Grains/Bonds/Meats/Softs/FX),
  Walk-Forward, Monte Carlo, Z-Score, Equity Curve Trading, backtesting con vectorbt.
  Triggera anche quando l'utente mostra codice EasyLanguage TOP_UA da tradurre in Python
  e quando chiede di Titan, dei suoi report giornalieri o del filtering periodico del portafoglio.
---

# Metodo Unger — Implementazione Python

Sei un esperto di trading algoritmico e Python. Il metodo Unger è una **disciplina completa** che
abbraccia quattro tasselli: **Strategia → Valutazione → Position Sizing → Portfolio**. Implementato
come libreria Python modulare.

Il codice di riferimento EasyLanguage originale è in `o_decoded_scripts/` (solo per consultare la logica).
Il codice Python va scritto e salvato in `/Users/davide/Documents/COWORK/ALGO-UNGER/`.
Il materiale didattico Unger Academy (TSS2, TSE, PS, MPS) è in `Mat_didattico/`.

---

## ⛔ LEGGE ZERO — LOOK-AHEAD BIAS: LA PRIMA DOMANDA DA FARE SEMPRE

> **"Sto usando dati futuri?"**
> Questa domanda va posta PRIMA di scrivere qualsiasi segnale, pattern, filtro o entrata.
> Un backtest con look-ahead bias non è un backtest: è una simulazione di onniscienza.
> I risultati sembrano perfetti perché lo sono — ma in modo impossibile da replicare live.

### Definizione operativa

**Look-ahead bias** = usare al momento della decisione di entrata/uscita informazioni
che in realtà non erano ancora disponibili a quel timestamp.

Si manifesta in **due forme distinte**, entrambe letali:

---

**TIPO 1 — Look-ahead sul PREZZO di entrata** (il più noto)

Un ordine stop viene "eseguito" a un livello già superato molto prima che il segnale scattasse.

```
Scenario: pattern usa H_d0 > H_d1 * 1.015, entrata stop a H_d1
Realtà:   H_d0 ha già superato H_d1 di molto → il fill a H_d1 è impossibile
Effetto:  win rate e avg trade gonfiati artificialmente
Segnale:  OOS >> IS, win rate > 80% con pochi trade
```

Questo bug si è già manifestato sul primo test GC (pattern dy=23, win rate 89.5%).
**Regola:** con entrata stop su H_d1/L_d1, i pattern non devono usare H_d0/L_d0/C_d0.

---

**TIPO 2 — Look-ahead sulla SELEZIONE dei trade** (il più insidioso)

Il prezzo di entrata è realistico, ma il criterio che decide SE entrare usa dati futuri.
Il simulatore entra al prezzo giusto, ma solo nei trade che già sa che andranno bene.

```
Scenario: pattern_fast(144) = close[i] > open[i], entrata market a open[i]
Realtà:   decidi di comprare oggi all'apertura usando la chiusura di oggi
Effetto:  selezioni automaticamente solo i giorni rialzisti → win rate artificioso
Segnale:  profit factor > 5, max drawdown ridicolmente basso, OOS ancora migliore di IS
```

Questo bug si è manifestato su BIAS daily GC (pattern 144, UngerScore 10.903, CAGR 42%).
**Regola:** su dati daily, qualsiasi dato d0 (open, high, low, close) è futuro rispetto all'apertura.

---

### Tabella di sicurezza per timeframe

| Dato usato nel pattern | Intraday (N barre/sessione) | Daily (1 barra = 1 giorno) |
|------------------------|----------------------------|---------------------------|
| `O_d1, H_d1, L_d1, C_d1` (ieri completato) | ✅ sempre sicuro | ✅ sempre sicuro |
| `O_d0` (apertura sessione corrente) | ✅ sicuro (nota dalla prima barra) | ✅ sicuro (è l'open di oggi) |
| `H_d0, L_d0` (max/min sessione in costruzione) | ⚠️ sicuro solo se entry è su prezzo corrente | ❌ **LOOK-AHEAD** — high/low di oggi non noti all'open |
| `C_d0` (close corrente = `df["close"]`) | ⚠️ sicuro solo se entry è sulla barra SUCCESSIVA | ❌ **LOOK-AHEAD** — close di oggi non nota all'open |

### Checklist obbligatoria prima di ogni segnale

Prima di finalizzare qualsiasi `generate_signals()`, verificare punto per punto:

```
□ 1. Quale dato usa ogni pattern? (d0? d1? df["close"]?)
□ 2. Su quale barra scatta l'entrata? (stessa barra del segnale, o barra successiva?)
□ 3. Se il timeframe è daily: nessun dato d0 tranne O_d0
□ 4. Se il timeframe è intraday con entry market sulla stessa barra: nessun C_d0/H_d0/L_d0
□ 5. Se il timeframe è intraday con entry stop su H_d1/L_d1: nessun H_d0/L_d0/C_d0
□ 6. I risultati sembrano "troppo belli"? → sospettare look-ahead prima di qualsiasi altra spiegazione
```

### Pattern fast con look-ahead su daily (da escludere dallo spazio parametri)

```python
# Su dati DAILY questi pattern usano dati non disponibili all'open → VIETATI
FAST_LOOKAHEAD_ON_DAILY = (
    set(range(31, 47))   |   # usa H_d0/L_d0 (range sessione corrente)
    set(range(53, 67))   |   # usa H_d0/L_d0
    set(range(81, 107))  |   # usa H_d0/L_d0
    {116, 117, 118}      |   # usa O_d0 vs L_d1/H_d1 (ma O_d0 è safe — eccezione)
    set(range(119, 152))     # usa O_d0/C_d0 vs C_d1 e df["close"] vs O_d0
)
# Pattern 116-118 usano O_d0 che è safe, ma per semplicità escluderli tutti >= 81 è più sicuro.
# Pattern 144 specifico: df["close"] > O_d0 = close oggi > open oggi → LOOK-AHEAD DAILY
```

### Fix per BIAS su daily — shift obbligatorio

Se si usano pattern che toccano d0 in un engine daily, il segnale va sempre shiftato di 1 barra:

```python
# In generate_signals(), dopo aver calcolato mask_long e mask_short:
if self.is_daily:
    mask_long  = mask_long.shift(1).fillna(False)   # ieri decide → oggi entro
    mask_short = mask_short.shift(1).fillna(False)
```

Senza questo shift, stai usando la close di oggi per entrare all'open di oggi.

### Segnali di allarme nei risultati (tutti e tre insieme = look-ahead quasi certo)

| Segnale | Soglia di allarme |
|---------|------------------|
| Win rate IS | > 75% su > 50 trade |
| OOS avg trade / IS avg trade | > 2× |
| OOS profit factor / IS profit factor | > 1.5× |
| Max drawdown / Net profit | < 1% |
| CAGR IS | > 50% su 5+ anni |

---

## 0. FILOSOFIA FONDANTE — Prima di tutto il resto

> **"I mercati non si interpretano, si testano."** — Andrea Unger

Questo non è uno slogan: è il vincolo operativo che governa ogni decisione del metodo.
Applicarlo correttamente significa:

**Non si sceglie per intuizione.** Un pattern non viene incluso perché "ha senso" o perché
"sembra correlato al mercato." Viene testato sistematicamente insieme al motore e accettato
solo se i numeri lo supportano su dati che non ha mai visto (out-of-sample).

**Non si modifica senza backtest.** Cambiare un parametro perché "sembra meglio" è
interpretazione travestita da ottimizzazione. Ogni modifica va ri-testata formalmente.

**Non si difende ciò che smette di funzionare.** Una strategia che perde edge
nel mercato attuale si sostituisce, non si "aggiusta" in modo discrezionale.

**I pattern sono classificatori di stato, non predizioni.** `PatternNeutralFast(3)` non dice
"il mercato salirà." Dice "ieri il mercato era in questo stato." Il motore poi chiede:
"in questo stato, la mia logica di entrata funzionava storicamente?" Il mercato risponde
con le statistiche. Nessuno interpreta nulla.

**Implicazioni pratiche per lo sviluppo in Python:**
- Ogni ipotesi si esprime in codice testabile, non in commenti
- L'ottimizzazione è esaustiva (sweep su tutto lo spazio) o quasi-esaustiva, non "provo qualche valore"
- La validazione è sempre IS + OOS, mai solo IS
- La metrica di selezione deve penalizzare la fragilità, non solo massimizzare il rendimento
- Il walk-forward non è un'opzione: è la firma di credibilità della strategia
- La minimizzazione dei parametri è protezione attiva dall'overfitting

**Perché la semplicità di Unger è anti-overfitting per costruzione:**
I pattern del metodo hanno zero parametri interni: sono soglie fisse (0.10, 0.25, 0.50...) ricavate
dalla struttura OHLC. Non si ottimizzano. Si usano o non si usano. Questo è il vero freno
all'overfitting: non è un controllo esterno aggiunto dopo, è nella struttura del metodo stesso.
I parametri liberi di una strategia Unger completa sono tipicamente 4–6 (stop, target, orari, pattern).

> **Regola operativa**: il vantaggio di Python non è poter aggiungere complessità.
> È poter fare una validazione più rigorosa della stessa semplicità.

---

## 1. LA VIA VERSO IL SUCCESSO — I quattro tasselli

Il successo di un trading sistematico secondo Unger non dipende solo dalla qualità della strategia.
Dipende dall'integrazione coerente di **quattro pilastri**:

```
   STRATEGIA  ──►  VALUTAZIONE  ──►  POSITION SIZING  ──►  PORTFOLIO
   (trigger,      (metriche,         (Fixed Ratio,        (filtering,
    pattern,      stabilità,         %f, %Vol,            correlation,
    filtri,       robustezza)        Reduced f, ...)      risk dynamic)
    risk)
```

Una strategia eccellente con position sizing aggressivo a contratto singolo, in portafoglio
con altre strategie correlate, può fallire. Una strategia mediocre ben dimensionata in un
portafoglio diversificato può prosperare. **L'integrazione conta più del singolo elemento.**

| Pilastro | Obiettivo | Domanda chiave |
|----------|-----------|----------------|
| Strategia | Generare edge statistico | Funziona su OOS? |
| Valutazione | Decidere se è davvero utilizzabile | È stabile, replicabile, sopportabile? |
| Position Sizing | Dimensionare il rischio | Quanti contratti per ogni trade? |
| Portfolio | Combinare strategie | I sistemi cooperano o soffrono insieme? |

Approfondimenti completi:
- Valutazione → `references/validazione.md`
- Position Sizing → `references/position_sizing.md`
- Portfolio → `references/portfolio.md`
- Mercati → `references/mercati.md`

---

## 2. ARCHITETTURA E STACK

```
STRATEGIA = MOTORE + PATTERN + FILTRI + RISK MANAGEMENT
PORTAFOGLIO = N × STRATEGIE + FILTRO ATTIVO/SPENTO + POSITION SIZING + CORRELATION
```

Ogni componente è indipendente e componibile. Si implementano come funzioni/classi Python pure
che operano su DataFrame pandas con colonne standard OHLCV + colonne di sessione calcolate.

### Stack tecnologico
- **pandas / numpy**: manipolazione dati, calcolo indicatori, pattern
- **vectorbt**: backtesting vettorializzato e ottimizzazione parametri
- **scipy/statsmodels**: Monte Carlo, Z-Score, statistiche
- **Dati**: CSV locali, yfinance, API broker (ib_insync per Interactive Brokers)

### Struttura moduli
```
unger/
├── session.py        # Calcolo OHLC multi-sessione (equivalente _OHLCMulti5)
├── patterns.py       # Pattern (NeutralFast, DirectionalFast, Fast, UAPtnBase, Specchio)
├── engines.py        # Motori (TF, MR, BIAS, Breakout, LevelFader, Reversal)
├── filters.py        # Filtri temporali, ADX, ATR, BB
├── risk.py           # Stop, target, trailing, breakeven
├── sizing.py         # Position Sizing: %f, Fixed Ratio, %Vol, Reduced f, Asymmetric, etc.
├── evaluation.py     # Metriche, Equity Curve Trading, Z-Score, Monte Carlo
├── portfolio.py      # Aggregazione strategie, filtering ricorrente, correlation
├── markets.py        # Catalogo strumenti: tick, BPV, $ATR, stop tipici, liquidità
└── backtest.py       # Runner vectorbt + ottimizzazione + Walk Forward
```

Per generare lo scheletro completo di questi file, leggi → `references/codice_base.md`

---

## 3. CALCOLO OHLC DI SESSIONE (`session.py`)

Equivalente Python di `_OHLCMulti5` ("la funzione di Michael"). Opera su un DataFrame intraday
con indice DatetimeIndex.

```python
def calc_session_ohlc(df, session_start: str, session_end: str, n_sessions: int = 5) -> pd.DataFrame:
    """
    Calcola OHLC delle ultime N sessioni completate + sessione corrente in costruzione.
    Restituisce colonne: O_d0..C_d0, O_d1..C_d1, ..., O_d5..C_d5
    is_session_start = True sulla prima barra della nuova sessione.

    Gestisce sessioni a cavallo di mezzanotte (futures USA 17:00–16:59 Chicago time).
    Mettendo session_start == session_end la funzione lavora sulle 24 ore (Forex).
    """
```

**Colonne restituite** (layout di ohlcValues[] in EasyLanguage):

| Colonne Python | Equivalente EL | Descrizione |
|----------------|---------------|-------------|
| `O_d0, H_d0, L_d0, C_d0` | ohlcValues[0..3] | Sessione corrente in corso |
| `O_d1, H_d1, L_d1, C_d1` | ohlcValues[4..7] | Sessione precedente (completata) |
| `O_d2..C_d2` | ohlcValues[8..11] | 2 sessioni fa |
| ... | ... | ... |
| `O_d5..C_d5` | ohlcValues[20..23] | 5 sessioni fa |
| `is_session_start` | return value | True sulla prima barra |

### Definire le sessioni correttamente

L'orario di Exchange Time è critico: la maggior parte dei mercati USA è a **Chicago (GMT-6)**
o **New York (GMT-5)**, EUREX a **Francoforte (GMT+1)**, LSE a **Londra (GMT)**.

**Forex 24/5**: scegliere come riferimento un mercato regolamentato sovrapposto. Per EurUsd
usare la sessione del future EuroFX (chiusura 23:00 Italia / 16:00 Chicago).

**DAX trap**: la close giornaliera mostrata sui chart daily è il *settlement close*, non l'ultimo
prezzo scambiato. **Per pattern correttamente costruiti usare barre da 1440 minuti, non daily.**

Per il codice completo → `references/codice_base.md` § Session OHLC

---

## 4. PATTERN (`patterns.py`)

Le librerie di pattern sono funzioni Python che ricevono il DataFrame con le colonne di sessione
e restituiscono una Series booleana (True = pattern verificato, barra per barra).

> Filosofia Unger sui pattern: si codifica una serie di pattern, alcuni direzionali, altri di
> volatilità. La numerazione non ha significato semantico, segue l'ordine di codifica storico.
> I pattern sono filtri statistici, non predizioni.

---

### ⚠️ Look-Ahead Bias — vedi Legge Zero in cima al documento

La trattazione completa (Tipo 1, Tipo 2, tabella di sicurezza per timeframe, checklist,
pattern vietati su daily, fix shift obbligatorio, segnali di allarme) è nella
**sezione "LEGGE ZERO"** all'inizio di questo documento. Quella sezione va letta prima
di qualsiasi altra cosa.

Reminder sintetico per i pattern:

```python
# ✅ SICURO su tutti i timeframe: pattern su sessioni passate (d1..d5)
pattern_neutral(df, n)       # usa H_d1, L_d1 — tutto completato ieri
pattern_directional(df, n)   # usa C_d1, H_d1, L_d1, O_d0 (apertura nota)

# ❌ VIETATO su daily: qualsiasi pattern che usa H_d0, L_d0, C_d0, df["close"]
# → high/low/close di oggi non sono noti all'apertura di oggi

# ⚠️ SU INTRADAY: sicuro solo se l'entrata avviene sulla barra SUCCESSIVA al segnale
#   (shift obbligatorio se si usano C_d0/H_d0/L_d0 con entry market)
```

**Sample size minimo:** almeno 50 trade su IS prima di considerare un pattern.

---

### Convenzione chiamata

```python
# MIRRORED: stesso pattern per entrambe le direzioni, segno per direzionale
mask_long = (
    pattern_neutral(df, ptn_neut_yes) &
    ~pattern_neutral(df, ptn_neut_no) &
    pattern_directional(df, +ptn_dir_yes) &
    ~pattern_directional(df, +ptn_dir_no)
)
mask_short = (
    pattern_neutral(df, ptn_neut_yes) &
    ~pattern_neutral(df, ptn_neut_no) &
    pattern_directional(df, -ptn_dir_yes) &
    ~pattern_directional(df, -ptn_dir_no)
)

# UNMIRRORED: pattern completamente indipendenti per long e short
mask_long  = pattern_fast(df, ptn_ly) & ~pattern_fast(df, ptn_ln)
mask_short = pattern_fast(df, ptn_sy) & ~pattern_fast(df, ptn_sn)
```

---

### `pattern_neutral(df, n)` → Series[bool]
**55 pattern senza direzione.** Uguale per long e short. `n=55` → sempre True, `n=56` → sempre False.

| n | Condizione Python | Cosa misura |
|---|------------------|-------------|
| 1 | `body1d < 0.10 * range1d` | corpo piccolo (doji) |
| 2 | `body1d < 0.25 * range1d` | corpo piccolo |
| 3 | `body1d < 0.50 * range1d` | corpo medio-piccolo |
| 4 | `body1d < 0.75 * range1d` | corpo non dominante |
| 5 | `body1d > 0.25 * range1d` | corpo presente |
| 6 | `body1d > 0.50 * range1d` | corpo dominante |
| 7 | `body1d > 0.75 * range1d` | barra molto direzionale |
| 8 | `body1d > 0.90 * range1d` | quasi marubozu |
| 9–15 | `body5d < X * (H_d5 - L_d1)` | mov. netto piccolo su 5 sess. (X: 0.1..2) |
| 16–22 | `body5d > X * (H_d5 - L_d1)` | mov. netto grande su 5 sess. (X: 0.25..2.5) |
| 23–26 | `body5d < X * range5d` | compressione su 5 sess. |
| 27–30 | `body5d > X * range5d` | espansione su 5 sess. |
| 31–37 | `H_d0 > L_d0 * (1 + X/100)` | ampiezza range corrente > X% |
| 38–44 | `H_d0 < L_d0 * (1 + X/100)` | ampiezza range corrente < X% |
| 45 | `(O_d0 < L_d1) \| (O_d0 > H_d1)` | gap di apertura |
| 46 | `(H_d0 < H_d1) & (L_d0 > L_d1)` | inside bar d0 in d1 |
| 47 | `range1d < (range_d2 + range_d3) / 3` | barra compressa vs media |
| 48 | `range1d < range_d2` e `range_d2 < range_d3` | compressione 3 barre |
| 49 | `(H_d1 < H_d2) & (L_d1 > L_d2)` | inside bar ieri in l'altro ieri |
| 50 | `(H_d1 < H_d2) \| (L_d1 > L_d2)` | parzialmente dentro range d2 |
| 51 | `(H_d0 > H_d1) & (L_d0 < L_d1)` | outside bar d0 vs d1 |
| 52 | stessa logica 51 | outside bar (variante) |
| 53 | `range1d < range_d2` | ieri più stretto di l'altro ieri |
| 54 | `range1d > range_d2` | ieri più ampio di l'altro ieri |
| **55** | `True` | **pass-through (nessun filtro)** |
| **>55** | `False` | **disabilitatore (usato come ptn_no)** |

Variabili interne: `body1d = abs(O_d1 - C_d1)`, `range1d = H_d1 - L_d1`,
`body5d = abs(O_d5 - C_d1)`, `range5d = max(H d1..d5) - min(L d1..d5)`

### `pattern_directional(df, n)` → Series[bool]
**52 pattern con direzione.** `+n` per long, `-n` per short (logica speculare).
`n=+52` o `n=-52` → sempre True; `n=+53` o `n=-53` → sempre False.

Ogni pattern `+n` misura una condizione rialzista; `-n` misura la stessa condizione ribassista.

| ±n | +n (long) | -n (short) |
|----|-----------|------------|
| ±1–8 | `H_d0 - O_d0 > (H_d1 - O_d1) * k` | `O_d0 - L_d0 > (O_d1 - L_d1) * k` (k: 0.25..3) |
| ±9 | `H_d0 - O_d0 < H_d1 - O_d1` | `O_d0 - L_d0 < O_d1 - L_d1` |
| ±10 | `C_d1 > C_d2 > C_d3 > C_d4` | `C_d1 < C_d2 < C_d3 < C_d4` |
| ±11 | 4 chiusure consecutive crescenti | 4 chiusure decrescenti |
| ±12 | `H_d1 > H_d2` e `L_d1 > L_d2` | `H_d1 < H_d2` e `L_d1 < L_d2` |
| ±13 | `C_d1 > C_d2` | `C_d1 < C_d2` |
| ±14 | `C_d1 > O_d1` (barra verde) | `C_d1 < O_d1` (barra rossa) |
| ±15–20 | `C_d1 > C_d2 * (1 + X/100)` | `C_d1 < C_d2 * (1 - X/100)` (X: 0.5..3) |
| ±21–26 | `H_d0 > H_d1 * (1 + X/100)` | `L_d0 < L_d1 * (1 - X/100)` |
| ±27–32 | `L_d0 > L_d1 * (1 + X/100)` | `H_d0 < H_d1 * (1 - X/100)` |
| ±33 | `H_d1 > H_d5` | `L_d1 < L_d5` |
| ±34 | `H_d1 < H_d5` | `L_d1 > L_d5` |
| ±35 | `H_d1 > H_d2, H_d3, H_d4` | `L_d1 < L_d2, L_d3, L_d4` |
| ±36 | `L_d1 > L_d2, L_d3, L_d4` | `H_d1 < H_d2, H_d3, H_d4` |
| ±37 | `C_d1 > C_d2 > C_d3` e `O_d0 > C_d1` | ribasso + gap apertura giù |
| ±38 | `H_d1 - C_d1 < 0.20 * range1d` (close vicino al max) | `C_d1 - L_d1 < 0.20 * range1d` |
| ±39 | `O_d0 > H_d1` | `O_d0 < L_d1` |
| ±40–43 | `O_d0 > C_d1 * (1 + X/100)` | `O_d0 < C_d1 * (1 - X/100)` |
| ±44 | `L_d1 > L_d2` | `H_d1 < H_d2` |
| ±45 | `C_d1 > O_d1` e `C_d2 > O_d2` | 2 barre verdi consecutive |
| ±46 | `C_d1 > O_d1` e `C_d2 < O_d2` | verde dopo rossa |
| ±47–51 | `C > O_d0 * k` (momentum intrabar) | `C < O_d0 * k` |
| **±52** | `True` | `True` |
| **±53** | `False` | `False` |

### Altre librerie pattern

- **`pattern_fast(df, n)`**: 152 pattern unmirrored. `n=152` → True. I primi 30 sono identici a
  NeutralFast 1–30. I successivi coprono wick, sequenze, range, High/Low, open vs close.
- **`pattern_uaptnbase(df, n)`**: 42 pattern. Libreria base storica. `n=41` → True, `n=42` → False.
- **`pattern_specchio(df, n)`**: 63 pattern ±n. Versione speculare di UAPtnBase con pattern avanzati
  (41–61): sequenze verdi/rosse, espansioni volatilità. `n=+62` → True. Usato in LevelFader.

Dettaglio completo → `references/codice_base.md` § Pattern*

> ⚠️ **Pattern SA — stesse formule, API vecchia** (verificato 2026-08-19, diff caso per caso):
> `PtnBaseSA` e `PtnBaseSA2` sono **identiche a `UAPtnBase`** (42 casi su 42),
> `PatternNeutralFastSa` a `PatternNeutralFast` (55/55), `PatternDirectionalFastSa` a
> `PatternDirectionalFast` (104/104). Cambia solo come entrano i dati: `PtnBaseSA` e le
> `*Sa` leggono `OpenS()/HighS()` dal grafico, le altre ricevono l'array `ohlcValues`.
> **Non c'è niente da riscrivere in Python**: `pattern_uaptnbase` / `pattern_fast` le coprono
> già, con la stessa convenzione delle sentinelle (41 = sempre vero, 42 = sempre falso).
> Una TOP_UA che usa i pattern SA **non va scartata per questo**: si porta mappando i numeri.
> Unica cautela: se usa `PtnBaseSA` (senza il 2) i pattern sono calcolati sulla sessione del
> grafico, quindi va verificato che coincida con `session_start_hour` del registry.
>
> ⚠️ **`PtnBaseP` è invece un'altra libreria**: 69 casi, e i numeri **41/42 sono pattern veri,
> non sentinelle**. Mapparla su `uaptnbase` produce una strategia diversa in silenzio.
>
> Triage delle TOP_UA coinvolte → `TRIAGE_PATTERN_SA.md` nella radice del progetto.

---

## 5. MOTORI (`engines.py`)

Ogni motore riceve il DataFrame (con colonne di sessione + pattern) e restituisce due Series
booleane: `entries_long` e `entries_short`.

Il metodo Unger identifica tre famiglie strategiche fondamentali:

| Famiglia | Quando funziona | Ingresso tipico | Mercati tipici |
|----------|-----------------|------------------|----------------|
| **Trend Following** | mercati con trend di fondo | ordine stop su rottura massimi/minimi | DAX, CL, GC, ZN |
| **Counter-Trend / Mean Reversion** | mercati che tornano su valori medi | ordine limit dopo ritracciamento | ES (miniSP), EurUsd |
| **Swing / BIAS** | cicli intraday / settimanali ricorrenti | ordine market in finestre temporali | CL, DAX, FX |

```python
def engine_trend_following(df, mirrored=True, **kwargs) -> tuple[pd.Series, pd.Series]:
    """Trigger LONG: close supera H_d1 (ordine stop su H_d1). Short speculare."""

def engine_mean_reversion_bb(df, length=20, num_devs=2, **kwargs) -> tuple[pd.Series, pd.Series]:
    """LONG: close crossover banda inferiore BB. SHORT: crossunder banda superiore."""

def engine_bias(df, le_bar, se_bar, lx_bar, sx_bar, entry_type=1, **kwargs) -> tuple[pd.Series, pd.Series]:
    """
    entry_type 1: entrata market alla N-esima barra di sessione
    entry_type 2: breakout dentro finestra temporale BIAS
    entry_type 3: ritracciamento (limit) dentro finestra temporale BIAS

    ⚠️ GRIGLIA SCALATA AL TIMEFRAME: le_bar/se_bar/lx_bar/sx_bar sono indici di barra
    DENTRO la sessione → vanno scalati su bars_per_session (~23h/tf), NON una lista
    fissa. Una griglia fissa (es. [1..16]) tarata su 60m (~23 barre) su 30m/15m sonda
    solo l'apertura e NON raggiunge le entrate tardo-sessione (Unger usa barra 21-36)
    → il motore sembra "senza edge" ma è solo cieco. Verificato: griglia fissa = net
    -53k / 0 sopravvissuti; griglia scalata = +187k IS, 4 sopravvissuti OOS, WF 5/5,
    se_bar=9 converge con Unger 460(=10)/960(=8).
    NB entry market: il simulatore entra all'OPEN della barra-segnale (bar_num==le_bar)
    → equivale al "buy next bar at market" di EL (che parte da mycount==le_bar): nessun
    off-by-one. Pattern: usare la libreria moderna (PatternFast/UAPtnBase su ohlcValues),
    MAI i pattern *SA (PtnBaseSA/PtnBaseSA2/FastSa = legacy OpenS()/HighS(), obsoleti).
    """

def engine_breakout_n_sessions(df, n_sess=1, lev_include_sess0=False, **kwargs):
    """Rottura del massimo/minimo delle ultime N sessioni complete."""

def engine_level_fader(df, level_choice=2, level_shift=0, tick_size=0.1, **kwargs):
    """Prezzo rompe livello (Pivot R1/S1 o H/L ieri ±tick) e poi ritorna (reversal)."""

def engine_reversal_hl(df, long_offset_ticks=0, short_offset_ticks=0, **kwargs):
    """
    LONG limit a L_d1 - offset. SHORT limit a H_d1 + offset.

    ⚠️ ORDINI LIMIT — vedi "Trappola limit-order" subito sotto questa firma.
       Il simulatore può registrare fake-fill se il prezzo tocca il livello
       senza che vi sia profondità sufficiente. Validare sempre la
       configurazione del simulatore (richiede attraversamento del livello,
       non solo touch) prima di accettare i risultati di questo motore.
    """

def engine_price_channel(df, channel_len=20, breakout_offset_ticks=0, direction=0,
                         dvol_min=0, **kwargs):
    """
    Price Channel / Donchian: stop sulla rottura del max/min delle ultime N BARRE
    rolling del timeframe corrente (NON sessioni — quello è engine_breakout_n_sessions).
    LONG stop a max(high, ultime N barre complete)+offset; SHORT speculare.
    Archetipo "Trend Following Price Channel" (NQ 30m). direction: 0 both/1 long/2 short.
    dvol_min: filtro volatilità "data2" (ATR giornaliero minimo in $, vedi session_atr).
    LEGGE ZERO: usa donchian(shift=1) -> solo barre complete; entry stop -> fill max(open,liv).
    """

def engine_volatility_breakout(df, vol_source=1, vol_mult=0.5, atr_len=14,
                               momentum=0, direction=0, **kwargs):
    """
    Volatility/ATR Breakout dall'APERTURA di sessione: stop a O_d0 ± k*VOL.
    vol_source: 1=range sessione prec (H_d1-L_d1) | 2=ATR giornaliero (data2, session_atr)
                | 3=ATR sulle barre (atr().shift(1)).
    Copre 3 strategie NQ: range-sessione-precedente (1), canale di volatilità (2), ATR (3).
    momentum: 0 off | 1 = C_d1 vs C_d2 | 2 = O_d0 vs C_d1.  direction: 0/1/2.
    LEGGE ZERO: O_d0 noto a inizio sessione; VOL da dati completi; entry stop.
    """
```

### Tipi di ordini — quando usarli

| Tipo ordine | Uso tipico | Rischio principale |
|-------------|-----------|--------------------|
| **Stop** | breakout (TF) | slippage elevato su livelli "ovvi" |
| **Limit** | ritracciamento (counter-trend) | mancato eseguito → finto profit nel simulatore |
| **Stop-Limit** | breakout protetto | rischio mancato eseguito + non sempre programmabile |
| **Market** | BIAS, ingressi cambio barra | slippage su barre standard → usare TF anomali (7min) |

> **Trappola limit-order**: in EasyLanguage e in alcuni backtester l'ordine limit viene considerato
> eseguito al solo toccare il livello — in realtà potresti essere dietro a 500 contratti. Il simulatore
> computerebbe un fake-gain. Verificare sempre il setup del simulatore per gli ordini limit.

---

### 5bis. Breakout avanzati, serie "data2" e nuove uscite (Trend Following su indici)

Famiglia di breakout intraday su indici (es. NQ), tutti **flat a fine sessione**, derivati
con il metodo standard Unger (trigger → uscita → filtri → ottimizza stop/target → valida).
Differenza rispetto al breakout base: il **livello d'ingresso è costruito da un canale
rolling o da un indicatore di volatilità**, non solo da H_d1/L_d1.

**Serie temporale 2 (data2) — NON è un problema di motore, è di preparazione dati.**
TradeStation usa una seconda serie giornaliera per conferme/filtri. In Python NON serve:
`calc_session_ohlc` già fornisce su ogni barra intraday l'OHLC delle sessioni (`d0..d5`),
cioè la serie giornaliera. Per gli **indicatori daily** (ATR giornaliero, ecc.) si calcola
l'indicatore sulla serie di SESSIONI COMPLETE e lo si rimappa sulle barre via `sess_id`
(con shift → solo sessioni chiuse). I motori/filtri **leggono** queste colonne; nessuno
carica un secondo dataset. Vantaggio: stessa fonte 1-min, nessun disallineamento, LEGGE ZERO
garantita per costruzione.

**Indicatori (`indicators.py`):**
```python
atr(df, n)                      # ATR Wilder sulle barre (shiftare se usato sulla barra i)
adx(df, n)                      # ADX Wilder sulle barre
donchian(df, n, shift=1)        # (upper, lower) = max/min ultime N barre COMPLETE (Price Channel)
session_atr(df, n, shift=1)     # ATR GIORNALIERO (data2): ATR sulle sessioni, rimappato su barra
session_range(df, lookback, shift=1)  # media range (H-L) delle ultime sessioni complete
```

**Nuove uscite (`risk.py` / simulatore) — LEGGE ZERO:** trailing e breakeven aggiornano lo
stop SOLO dopo i controlli di uscita della barra corrente (sul picco fino a i), quindi
proteggono dalla barra SUCCESSIVA → niente look-ahead intrabar.
```python
simulate(..., trailing_stop_dollars=0, breakeven_dollars=0)
# trailing: stop insegue il picco favorevole a X $ di distanza
# breakeven: stop → pareggio quando il profitto a favore raggiunge X $
```

**Toggle direzione** (`BaseEngine.apply_direction`): 0=long+short, 1=solo long, 2=solo short.
Utile su mercati strutturalmente direzionali (NQ è long-biased: il long-only riduce
l'overfitting a un mercato toro, ma va comunque validato OOS sul 2018/2022).

**Mappa archetipi → motori implementati** (i 4 sistemi NQ di Unger Academy):

| Strategia articolo | Motore | Configurazione chiave |
|--------------------|--------|------------------------|
| TF Price Channel (S2) | `PC` (engine_price_channel) | `channel_len`, breakout su Donchian N barre |
| Range sessione prec. (S3) | `VBO` | `vol_source=1` → O_d0 ± k·(H_d1−L_d1) |
| Breakout ATR (S4) | `VBO` | `vol_source=3` → O_d0 ± k·ATR, `momentum` |
| Canale di volatilità (S1) | `VBO` | `vol_source=2` → O_d0 ± k·ATR_giornaliero (data2) |

⚠️ **Bias long sugli indici**: i backtest "tutti gli anni positivi" su NQ riflettono il toro
2016–2024. Il test vero è l'OOS/walk-forward sul 2018 e 2022 (con floor dati al 2012 li
includiamo). Validare SEMPRE: l'articolo mostra solo l'in-sample.

---

## 6. FILTRI (`filters.py`)

```python
def time_window(df, start: str, end: str, pause_start=None, pause_end=None) -> pd.Series:
    """Finestra oraria con pausa opzionale. Gestisce finestre cross-midnight."""

def day_filter(df, skip_day: int = -1) -> pd.Series:
    """Esclude un giorno (0=Dom, 1=Lun, ..., 5=Ven, 6=Sab; -1=nessuno)."""

def month_filter(df, skip_month: int = 0) -> pd.Series:
    """Esclude un mese (1–12). 0=nessun filtro."""

def adx_filter(df, length=5, threshold=100, mode="below") -> pd.Series:
    """ADX < threshold (per MR) o ADX > threshold (per TF)."""

def atr_expansion_filter(df, period=2, mult=0.065, data2=None) -> pd.Series:
    """Filtro espansione volatilità: |O[n] - C| > range(n) * mult. Tipico per MR."""

def max_entries_per_session(df, max_entries=1) -> pd.Series:
    """Limite trades per sessione (equivalente EntriesTodaySession)."""
```

---

## 7. RISK MANAGEMENT (`risk.py`)

```python
def apply_stops(portfolio, stop_loss=0, take_profit=0, breakeven=0, trailing_stop=0,
                stop_long=None, stop_short=None, profit_long=None, profit_short=None):
    """Applica stop/target al portfolio vectorbt."""

def time_exit(portfolio, max_bars: int = None, exit_time: str = None):
    """Time stop: uscita dopo N barre, o a un orario specifico."""

def end_of_session_exit(df) -> pd.Series:
    """Maschera per uscita a fine sessione (intraday-only systems)."""
```

### Tipi di uscita — quando si usa cosa

| Uscita | TF systems | MR systems | BIAS systems |
|--------|-----------|-----------|--------------|
| Stop Loss | ✅ obbligatorio | ✅ obbligatorio | ✅ obbligatorio |
| Take Profit | ❌ spesso peggiora | ✅ logico (movimento esauribile) | ✅ ha senso |
| Trailing | ✅ utile | parziale | parziale |
| Time exit | parziale | ✅ tipico | ✅ tipico (a fine finestra BIAS) |
| Reverse | tipico in channel BO | meno comune | meno comune |

> Sui sistemi trend following il take profit "spesso porta a peggioramenti". "Senza stop loss"
> NON significa "aspetto e spero": significa che chiudo con un reverse o un time stop, non con
> uno stop monetario fisso.

---

## 8. VALUTAZIONE DI UN SISTEMA (`evaluation.py`)

Una volta ottenuta un'equity curve positiva, la domanda non è "guadagna?" ma:
**è un sistema utilizzabile?** La valutazione è altrettanto importante della costruzione.

### 8.1 Dimensioni della valutazione

```
                  Tecnico-pratiche                Psicologiche
                  ─────────────────                ────────────
                  Automatizzabile                  Approccio operativo
                  Replicabilità ordini             Frequenza vincite/perdite
                  Veridicità backtest              Drawdown & RunUp
                  Frequenza operativa              Intensità delle oscillazioni
```

Un sistema può essere ottimo statisticamente e **inutilizzabile psicologicamente**: drawdown
estesi, lunghe sequenze di perdite, profitti concentrati in pochi outlier — generano panico
e abbandono prima del recupero.

### 8.2 Metriche fondamentali

| Metrica | Cosa guardare | Soglia di buon senso |
|---------|--------------|---------------------|
| **Net Profit** | base solida, non confrontabile in assoluto | dipende dal mercato |
| **Periodical Profits** | distribuzione anno per anno | nessun anno "da favola" che falsa il totale |
| **Drawdown** | il peggiore + frequenza dei "simili" | DD recenti? rapporto col Net Profit? |
| **Numero di trade** | min ~12/anno; meno se sistema "raro" by design | <50 IS = troppo pochi |
| **Durata trade** | tempo a mercato = rischio | va valutata insieme all'avg trade |
| **Outlier** | profitti concentrati in pochi trade = fragilità | < 30% del NP da top trade |
| **Avg Trade** | dipende da tick value, volatilità, durata | almeno 6-7 tick; ~15% del range $ medio |

### 8.3 Equity Curve Trading

L'equity stessa diventa un segnale on/off:
```python
def equity_curve_filter(equity: pd.Series, ma_period: int = 30, mode="above") -> pd.Series:
    """
    Maschera per attivare trading solo quando equity è sopra (o sotto) la sua MA.
    mode='above': protezione da sistema morente — disattiva quando equity scende sotto MA.
    """
```

### 8.4 Z-Score / Trade Dependency

Le serie di trade hanno una struttura: alcuni sistemi alternano vincite/perdite,
altri formano sequenze. Lo Z-Score misura questa dipendenza:

```
              N * (R - 0.5) - X
Z-Score = ─────────────────────────
            sqrt( X*(X-N) / (N-1) )

N = numero trade, R = numero stringhe, W = winner, L = loser, X = 2*W*L
```

- `Z > +1.96`: alternanza (95% conf.) — saltare un trade dopo una vincita può aiutare
- `Z < -1.96`: continuazione — saltare un trade dopo una perdita può aiutare
- `|Z| < 1.64`: indeterminato — non agire sui risultati precedenti

### 8.5 Monte Carlo — "Cosa aspettarsi"

Permutando l'ordine dei trade reali (1000+ simulazioni) si ottiene la distribuzione
dei DD e dei NP possibili. Utile per:
- Stimare il DD atteso a 1, 2, 3, 10 anni
- Verificare se l'OOS reale batte il 95° percentile delle permutazioni
- Definire aspettative realistiche prima di andare live

### 8.6 In-Sample / Out-of-Sample / Forward

```
   Sviluppo IS    →    Validazione OOS    →    Validazione Forward    →    LIVE
                                                (paper trading)
```

Regole operative:
- OOS può essere più lungo o più corto dell'IS (non c'è regola fissa)
- L'IS può essere anche su periodi non consecutivi
- Forward validation aiuta a scoprire errori nascosti nel codice
- **Quanto migliore l'IS, tanto maggiore il rischio di calo OOS** (overfitting)
- Risultati basati su outlier sono molto più difficili da validare OOS

### 8.7 Riottimizzazione periodica & Walk-Forward Optimization

I mercati cambiano. Periodicamente i parametri vanno rivalutati. Quando questo processo
diventa sistematico e traslato continuamente, è una **Walk-Forward Optimization**:

```
Window 1:  [─── IS ───][─OOS─]
Window 2:           [─── IS ───][─OOS─]
Window 3:                    [─── IS ───][─OOS─]
                                       ...

Risultato live = concatenazione degli OOS
```

Approfondimento completo → `references/validazione.md`

---

## 9. POSITION SIZING (`sizing.py`)

> "Calcolare QUANTO utilizzare per ogni posizione." Money Management = stop loss + RR ratio +
> position sizing. Il position sizing è il moltiplicatore di efficacia di una strategia con edge positivo.

### 9.1 Modelli base

```python
def fixed_contracts(n: int = 1) -> int:
    """Sempre n contratti. Modello-baseline, conservativo."""

def percent_f(equity: float, f_pct: float, wcs: float) -> int:
    """
    %f: contracts = int(equity * f% / WCS)
    WCS = Worst Case Scenario: stop loss, peggior perdita storica,
          media degli N peggiori trade, worst daily, max DD storico.
    """

def fixed_ratio(equity: float, equity_init: float, delta: float) -> int:
    """
    Fixed Ratio (Ryan Jones):
       contracts = int( (1 + sqrt(1 + 8*(E-Ei)/delta)) / 2 )
    Partenza più aggressiva possibile (1 contratto), progressione che decelera.
    """

def percent_volatility(equity: float, vol_pct: float, atr: float, bpv: float) -> int:
    """
    %Vol (Van Tharp): contracts = int(equity * vol% / (ATR * BPV))
    Lega il sizing alla volatilità corrente dello strumento.
    """
```

**Worst Case Scenario alternatives per %f**:
- Stop Loss della strategia (più aggressivo)
- Peggior perdita storica
- Media degli ultimi N peggiori trade
- Worst time performance (daily, weekly)
- DrawDown storico massimo (→ "Secure f")

### 9.2 Modelli avanzati

```python
def reduced_f(equity: float, equity_init: float, wcs: float) -> int:
    """
    Reduced f: %f decrescente man mano che il capitale cresce.
    Più aggressivo all'inizio, più protettivo sui guadagni accumulati.
    Es: <2x Ei→10%, 2-3x→7.5%, 3-4x→5%, 4-7x→2.5%, >7x→1%
    """

def aggressive_ratio(equity: float, equity_init: float, base_delta: float) -> int:
    """
    Aggressive Ratio: Delta del Fixed Ratio decrescente con la crescita dell'equity.
    Opposto di Reduced f — diventa più aggressivo con i profitti.
    """

def asymmetric_ratio(equity: float, equity_init: float, delta: float) -> int:
    """
    Asymmetric Ratio (Ryan Jones): livelli di contratti fissati in salita,
    ma in discesa i livelli si avvicinano (ratchet) per rallentare il DD.
    """

def timid_bold_equity(equity: float, baseline: float,
                       timid_pct: float, bold_pct: float) -> int:
    """
    Timid-Bold (Van Tharp): rischio Timid sul capitale base, Bold sul Market Money
    (i guadagni). Reset quando l'equity raggiunge soglie predefinite.
    """
```

### 9.3 Risk of Ruin

Probabilità che l'equity scenda al livello di "non-operatività":

**Stima base (gambling, win=loss):**
```
RoR = ((1 - W) / W) ** (1 / f)    # W = winning%, f = % rischio
```

**Stima completa (Kaufman):**
```
E  = Σ PL_i * p_i                  # ritorno medio
E2 = Σ PL_i² * p_i                 # ritorno quadratico medio
P  = 0.5 + E / (2 * sqrt(E2))      # probabilità di trade vincente
D  = CA / sqrt(E2)                 # capitale a rischio normalizzato
RoR = ((1-P)/P) ** D
```

### 9.4 Quale modello quando — decision table

| Nr Sistemi | Modello | Suggerito | Alternativa |
|-----------|---------|-----------|-------------|
| **< 5** | modelli simili | Fixed Ratio | %f su SL o Worst Day |
| **< 5** | modelli diversi | Personalizzato per sistema | %f su Worst Day |
| **5–10** | modelli simili | Fixed Ratio | %f su Worst Day |
| **5–10** | modelli diversi | %f su Worst Day | Fixed Ratio |
| **10–25** | modelli simili | %f su Worst Day | Fixed Ratio |
| **10–25** | modelli diversi | %f su Worst Day | Percent Volatility |
| **>25** | indifferente | %f su Worst Day | Position Sizing "a mercato" |

> Preferenza personale Unger: **%f su Worst Day** — amalgama bene sistemi con caratteristiche
> diverse, facile applicazione. Tipicamente 1.25% su capitale totale.

### 9.5 Position Sizing "a mercato"

Quando ci sono molti sistemi sullo stesso strumento, il rischio è la **concentrazione**.
Si dimensiona per fasce di volatilità con fattore correttivo sul numero di sistemi attivi
(non c'è formula, solo buon senso ed esperienza).

Approfondimento completo, formule + esempi numerici → `references/position_sizing.md`

---

## 10. PORTFOLIO MANAGEMENT (`portfolio.py`)

### 10.1 La diversificazione non basta

Mettere insieme più sistemi **non garantisce** miglioramento del DD. Esempi tipici di trappola:

- **CL BIAS + CL Trend Following** che usa filtri basati sul BIAS → posizioni correlate
- **DAX BIAS + DAX Reversal** che operano nelle stesse ore della giornata → sofferenza comune
  nei momenti di "fuori fase" del mercato

> Test diagnostico: sovrapporre le curve di DrawDown delle singole strategie. Episodi isolati
> di DD coincidente sono inevitabili; ricorrenze sistematiche sono un campanello d'allarme.

### 10.2 Caratteristiche dei sistemi da combinare

| Da evitare | Da preferire |
|-----------|--------------|
| Stessa logica su mercati molto correlati (DAX/CAC/EuroStoxx/FTSEMIB) | Mercati molto diversi |
| Stesso motore con piccole varianti | Logiche di ingresso diverse |
| Ingressi nelle stesse ore | Orari diversi del giorno |
| Sistema con 1 trade/mese + sistema con 10/sett. | Frequenze comparabili |
| Mix scalping + long-term TF su close-equity | Stessa scala temporale |

### 10.3 Capitale di riferimento

Tre opzioni per il capitale base nel calcolo dei contratti:

1. **Equity con P&L in essere** — il più comodo, da preferire
2. Equity depurata dei margini delle posizioni aperte — conservativo
3. Equity depurata di margini + stop loss virtuali — molto conservativo

> Consiglio Unger: opzione 1 con percentuali di rischio meno aggressive.

### 10.4 Filtering ricorrente di strategie

Per portafogli grandi (>10 sistemi) è impraticabile valutare ogni sistema manualmente.
Si usano filtri parametrici sui dati daily di profitto:

```python
def long_term_filter(daily_pnl: pd.Series, lookback_periods: int = 12) -> bool:
    """Es: ultimo anno (12 mesi)."""
    return daily_pnl.tail(lookback_periods * 21).sum() > 0

def short_term_filter(daily_pnl: pd.Series, lookback_periods: int = 4) -> bool:
    """Es: ultimo trimestre (4 mesi)."""
    return daily_pnl.tail(lookback_periods * 21).sum() > 0
```

**Metodi di filtraggio classici:**
| Metodo | Logica |
|--------|--------|
| 1 | Media(short) > soglia × Media(long) AND Periodo short > 0 |
| 2 | Periodi positivi in short > X% del totale OR periodo long > 0 |
| 3 | Metodo 1 o 2 + AvgGain su N anni > AvgDD × moltiplicatore |
| 4 | Metodo 1 o 2 + Equity sopra propria MA a N periodi |
| 5 | Metodo 3 + Metodo 4 |

### 10.5 Quando i filtri funzionano male

Misurare quante volte il filtro decide bene. Se < 40% delle decisioni sono buone, si può
considerare di usare il **filtro invertito**. Doppia soglia:
- `SogliaPos` (es. 50%): sopra → si usa il filtro
- `SogliaNeg` (es. 60%): negativo > soglia → si fa l'opposto del filtro

### 10.6 Correlazione

Le formule di correlazione classiche (su close daily) sono di **scarsa utilità** per strategie
sistematiche dinamiche con ingressi a orari/direzioni variabili. Il fenomeno vero è la
**correlazione in stress** — quando shock di mercato attivano correlazioni altrimenti deboli
(es. Brexit attiva correlazione negativa US-Indices vs Gold).

**Approccio pratico**: misurare la correlazione dei P&L daily delle strategie. Se due strategie
mostrano correlazione > 0.75, dividere il numero di contratti calcolato dal position sizing
per `(1 + correlation)` come fattore correttivo.

### 10.7 Setup operativo Unger (preferenze personali)

| Parametro | Valore |
|-----------|--------|
| Finestra analisi | Mensile (primo sabato del mese) |
| Long Period | 12 mesi |
| Short Period | 4 mesi |
| Condizione attivazione | Long > 0 AND Short > 0 |
| Stabilità storica | AvgGain ultimi 3 anni > 2 × MaxDD ultimi 3 anni |
| Soglia filtri | SogliaPos 40%, SogliaNeg 60% |
| Soglia correlazione | > 0.75 |
| Position Sizing | %f 1.25% su Worst Day |
| Limite DD singola | non > 4% del capitale |

Approfondimento completo + flowchart → `references/portfolio.md`

### 10.8 Titan — l'implementazione di riferimento del tassello Portfolio

**Titan** è il software Unger Academy che applica in pratica tutto il §10 (e il position sizing
del §9): riceve i **profitti giornalieri per strategia** esportati da MultiCharts/TradeStation
(funzione EL `WriteDailiesCTitanReports` → un file `Strumento_NomeStrategia.txt` per sistema,
P&L calcolato su open equity) e a ogni ricalcolo periodico (default: mensile, 1° sabato) decide
quali strategie accendere/spegnere, con quanti contratti, e quali eliminare per capitale
insufficiente. Concetti chiave:

- **MinCap** = media di 3 capitali richiesti: `CapWCS` (worst day / DailyRisk%), `CapMaxDD`
  (MaxDD / MaxDD%), `CapMarginDD` (margine + MaxDD × MarginDD). Default: DailyRisk 1,25%, MaxDD 4%
  — gli stessi valori del setup §10.7;
- **Filtri on/off**: short/long period (short×th > long), media equity, avgGain/DD annuale,
  soglie di utilità thPassP/thPassN (= SogliaPos/SogliaNeg §10.5), Average Trade, ultimo picco,
  Bollinger sull'equity, Strategy Ranking per sottostante/categoria/timeframe, MonthStop,
  SuspendForDD;
- **Correlazione**: formula "AU" (giorni concordi / giorni comuni) sulle equity daily, soglia
  0,75 → contratti divisi per (1+corr) — è la regola del §10.6;
- **3 portafogli di confronto**: Filtered / Unfiltered / OneContract;
- il P&L è contabilizzato per **differenza di equity giornaliera**, non per trade: accensioni e
  spegnimenti a metà posizione si gestiscono col riallineamento del lunedì (curva "filtered
  with gap").

Dettaglio completo (tutti i parametri, default, pipeline, output, FAQ con rationale di Unger,
implicazioni per `portfolio.py`) → **`references/titan.md`**. Il manuale originale è
"MANUALE TITAN ITA.pdf" in `Mat_didattico/`.

---

## 11. MERCATI E STRUMENTI (`markets.py`)

Catalogo dei principali futures con caratteristiche operative:

```python
@dataclass
class Instrument:
    symbol: str
    name: str
    exchange: str
    tick_size: float
    big_point_value: float        # $ per punto
    session_hours: str            # "08:30-16:15" (Chicago) o "24/5"
    atr_45d_dollar: float         # $ATR su 45 giorni (riferimento)
    stop_intraday: tuple          # range tipico (min, max) USD
    stop_overnight: tuple
    liquidity: str                # "molto elevata", "buona", "media", "scarsa"
    main_character: str           # "trending", "mean reverting", "BIAS", "varie"
```

### Tabella sintetica $ATR 45gg (riferimento per confrontabilità)

| Strumento | $ATR | Strumento | $ATR | Strumento | $ATR |
|-----------|------|-----------|------|-----------|------|
| Cocoa | 520 | Corn | 1300 | Gold | 1300 |
| Sugar | 580 | Soybeans | 1350 | Silver | 1350 |
| Coffee | 1150 | Wheat | 1350 | Copper | 1350 |
| Lean Hogs | 610 | DAX (€) | 2850 | Platinum | 820 |
| Live Cattle | 640 | MiniSP500 | 820 | BP Future | 630 |
| Feeder Cattle | 1050 | MiniND | 800 | CAD | 450 |
| Crude Oil | 1100 | EuroStoxx (€) | 350 | EurUsd | 900 |
| Heating Oil | 1500 | Bund (€) | 750 | JPY | 950 |
| Gasoline | 1700 | 30Y T-Bond | 1250 | NZD | 600 |
| Natural Gas | 1000 | 10Y T-Note | 500 | CHF | 800 |

### Caratteristiche principali per famiglia

| Famiglia | Mercati top | Carattere | Note |
|----------|-------------|-----------|------|
| **Index** | ES, NQ, DAX, RTY | MR (ES), Trend (DAX, NQ) | Mini/Micro per capitalizzazioni limitate |
| **Energy** | CL, HO, RB, NG | Trend (CL, HO, RB), Varie (NG) | Mini-NG/HO/RB poco liquidi |
| **Metals** | GC, SI, HG, PL | Trend + BIAS (GC), Trend (SI) | MicroGold per piccoli capitali |
| **Grains** | ZC, ZS, ZW | Varie | No alternative real mini |
| **Bonds** | ZN, ZB, FGBL | Varie | BTP/OAT future come alternative |
| **Meats** | LE, GF, HE | Varie, sessioni brevi | Liquidità media |
| **Softs** | CC, SB, KC | Varie | Sessioni brevi |
| **FX Futures** | 6E, 6B, 6J, 6A, 6N, 6S, 6C | Varie | Forex come alternativa scalabile |

> Forex offre la **migliore scalabilità** (lotti decimalizzabili). Stop ridotti possibili,
> grande flessibilità per il position sizing su capitali limitati. Attenzione al broker
> (mercato non regolamentato come futures/azioni).

Catalogo completo + dettagli per ogni mercato → `references/mercati.md`

---

## 12. BACKTESTING E OTTIMIZZAZIONE (`backtest.py`)

```python
import vectorbt as vbt

def run_strategy(df, engine_fn, engine_params, pattern_params, filter_params,
                 risk_params, sizing_fn=None, size=1, fees=0.0, slippage=0.0) -> vbt.Portfolio:
    """Assembla tutti i componenti e lancia il backtest. Restituisce Portfolio vectorbt."""

def optimize(df, engine_fn, engine_params_grid, pattern_params_grid, fixed_params,
             metric="sharpe_ratio", n_jobs=-1) -> pd.DataFrame:
    """Grid search vettorializzato. Restituisce DataFrame con tutti i risultati."""
```

### Metriche disponibili (vectorbt)
`total_return`, `sharpe_ratio`, `sortino_ratio`, `max_drawdown`, `calmar_ratio`,
`total_trades`, `win_rate`, `profit_factor`, `avg_trade`, `expectancy`

---

## 13. INFRASTRUTTURA DI VALIDAZIONE — il vero vantaggio di Python

Python non serve ad aggiungere complessità al modello. Serve a validare la semplicità del
modello in modo rigoroso e automatizzato.

### 13.1 Walk-Forward automatizzato

```python
def walk_forward(df, run_fn, is_pct=0.7, n_windows=5):
    """
    Divide il dataset in N finestre. Su ciascuna ottimizza su IS,
    valida su OOS. Restituisce equity OOS concatenate.
    """
    n = len(df); window = n // n_windows; results = []
    for i in range(n_windows):
        start = i * window; end = start + window
        split = start + int(window * is_pct)
        best_params = run_fn(df.iloc[start:split], mode="optimize")
        results.append(run_fn(df.iloc[split:end], mode="run", params=best_params))
    return pd.concat(results)
```

**Cosa cercare**: equity OOS positiva e crescente anche se non ottimale. Un picco IS che
crolla in OOS è overfitting. Una strategia che guadagna meno in OOS ma mantiene direzione
e ratio simili è robusta.

### 13.2 Analisi stabilità parametri (plateau vs picco)

```python
def stability_map(opt_results: pd.DataFrame, param_x: str, param_y: str, metric: str):
    """Heatmap 2D. Plateau ampio = stabile. Picco isolato = overfitted."""
    pivot = opt_results.pivot(index=param_x, columns=param_y, values=metric)
    import seaborn as sns
    sns.heatmap(pivot, cmap="RdYlGn", center=0)
```

### 13.3 Monte Carlo su trade OOS

```python
def monte_carlo_oos(trades_oos: pd.Series, n_sim=1000, confidence=0.95):
    """Permuta l'ordine dei trade. La strategia reale deve battere il 95° percentile."""
    results = []
    for _ in range(n_sim):
        shuffled = trades_oos.sample(frac=1).reset_index(drop=True)
        cum = shuffled.cumsum()
        results.append({"net_profit": cum.iloc[-1],
                         "max_dd": (cum - cum.cummax()).min()})
    return pd.DataFrame(results)
```

### 13.4 Efficienza walk-forward — quanto edge sopravvive davvero

Il walk-forward con ri-ottimizzazione produce, oltre all'equity OOS concatenata, il numero
più onesto dell'intera ricerca:

```
efficienza_WF = avg_trade OOS / avg_trade IS      (media sulle finestre)
```

**Un valore < 1 non è un difetto: è aritmetica.** In ogni finestra si sceglie il massimo su
centinaia di migliaia di combinazioni; il massimo di N misure rumorose contiene edge vero
*più* fortuna, e fuori campione la fortuna non si ripresenta. Aspettarsi OOS ≈ IS significa
aspettarsi che il rumore si ripeta.

Misure reali su NQ (2026-07-28, 10 coppie motore-mercato): efficienza **0,14–0,50**, con
8 casi su 10 a OOS concatenato positivo. Regola pratica da applicare sempre:

> **avg trade atteso live ≈ avg trade in-sample × 0,2–0,5.**
> Il numero del backtest non è una previsione: va scontato prima di dimensionare (§9) e
> prima di inserire la strategia in portafoglio (§10).

Attenzione a non confondere due domande diverse:
- *quanto si è ristretto l'edge?* → efficienza WF;
- *quel che resta basta?* → criterio del metodo §8.2 (avg trade ≥ 6-7 tick / frazione del
  range $, DD sopportabile, anni positivi). È **questo** il criterio di accettazione.

### 13.5 Il test multiplo — il controllo che il metodo non prescrive

Il metodo difende dall'overfitting con pattern a zero parametri, plateau e OOS. Non conta
però **quante ipotesi sono state provate**: con uno sweep da centinaia di migliaia di
configurazioni, alcune superano l'OOS per puro caso, e nulla le distingue da quelle buone.

Controllo minimo, economico:

```python
def permutation_test(search_fn, df, n_perm=100):
    """
    La miglior strategia trovata sui dati veri deve battere il 95° percentile
    delle MIGLIORI strategie trovate su dati permutati (dove per costruzione
    non esiste edge). Se non lo batte, il risultato è rumore selezionato bene.
    """
```

Varianti formali: Reality Check di White, SPA di Hansen. Da usare quando lo spazio di
ricerca è grande e la strategia sta per andare a mercato.

### 13.6 UngerFit — oggettivare il "buon senso" di §8.2

Il metodo NON ha un punteggio: §8.2 è una tabella di soglie, e Unger sceglie a occhio
leggendo net profit, DD, numero trade, avg trade. Con un milione di configurazioni per
cella un ordinamento automatico serve per forza — ma va costruito con la variabile
decisionale del metodo, non con una qualsiasi.

```
E = avg_trade / soglia_avg_trade        edge per operazione (§8.2: 6-7 tick / 15% range)
R = max_dd / avg_trade                  trade per recuperare il DD peggiore (§8.1)
S = 100 / R
UngerFit = sqrt(E * S)                  1 = appena accettabile
```

**Perché non Profit/DD.** Vale l'identità `NP/DD = N/R`: il Profit/DD ratio è il numero
di trade diviso R. Ordinare su NP/DD significa ordinare sul numero di operazioni, quindi
penalizzare ogni filtro (pattern, orari) che ne riduce la quantità — l'opposto di quello
che fa il metodo, che nelle fasi intermedie classifica per average trade. Verificato sulle
run NQ 30m / GC 1h / CL 1h del 2026-07-29: su 18 candidate con pattern attivo, fuori
campione, NP/DD preferiva la versione SENZA pattern 10 volte su 18; UngerFit una sola
(l'unico caso in cui il pattern peggiorava sia avg trade sia R).

Profit/DD resta calcolato (`np_dd`) e resta un **cancello** di tradabilità OOS ("il DD va
confrontato col net profit"), ma non ordina nulla.

I due assi non esauriscono §8.2 per scelta: campione, anni positivi, concentrazione degli
outlier restano **soglie binarie** — condizioni di validità, non gradazioni di qualità.
Chi non le passa non prende punteggio, non prende un punteggio basso.

---

## 14. WORKFLOW OPERATIVO

### A) Tradurre una TOP_UA da EasyLanguage a Python

1. **Leggi** il file `.txt` della TOP_UA in `o_decoded_scripts/`
2. **Identifica**: motore, pattern usati (numeri), filtri attivi, risk management
3. **Costruisci** il DataFrame: carica dati, `calc_session_ohlc()`
4. **Applica motore**: funzione engine corrispondente
5. **Applica pattern**: `mask_long` e `mask_short` con funzioni pattern
6. **Applica filtri**: `time_window()`, `day_filter()`, ecc.
7. **Combina**: `entries_long = engine_long & mask_long & filtri`
8. **Lancia backtest**: `run_strategy()`
9. **Confronta** con equity originale EasyLanguage

### B) Costruire una nuova strategia (processo completo)

```
1. Trovare un trigger           (precedente H/L, time, indicator, pattern formato)
2. Unire un'uscita base         (stop loss + time exit | take profit | reverse | trailing)
3. Filtri operazioni            (giorno settimana, finestra, pattern Y/N)
4. Affinare stop/target         (sweep parametrico)
5. Verificare stabilità         (plateau analysis)
6. Validazione IS/OOS/Forward   (almeno 2 mesi OOS)
7. Position Sizing              (scegliere modello secondo decision table §9.4)
8. Inserimento in portafoglio   (verificare correlazione, DD coincidenti)
9. Monitor LIVE iniziale        (paper trading 2-3 mesi prima di reale)
10. Re-ottimizzazione periodica (Walk-Forward integrata se desiderato)
```

> ⚠️ **NON fare**: filtri di regime (trending/ranging), metriche composite
> (sharpe × profit_factor × ...), ottimizzare pattern + stop/target insieme (joint
> optimization), espandere librerie pattern oltre quelle documentate.

### C) Dove prendere idee (5 fonti Unger)

```
        ┌─ Teoria di base (channel BO, MA, support/res, cicli)
        │
Sistema ─── Osservazione (stare davanti al grafico, operare)
        │
        │── What-if? (test di ingressi/uscite particolari)
        │
        │── Altri sistemi aperti (forum, regali, mai pari pari)
        │
        └─ Fallimenti (analizzare cosa non ha funzionato)
```

### D) Schema ottimizzazione pattern (riferimento)

```python
# Fase 1: sweep pattern neutrali
opt_neut = optimize(df, engine_fn,
    pattern_params_grid={
        "ptn_neut_yes": range(1, 56),    # 1–55
        "ptn_neut_no":  range(1, 57),    # 1–56 (56=False=nessun filtro)
    },
    fixed_params={"ptn_dir_yes": 52, "ptn_dir_no": 53}  # dir disabilitati
)

# Fase 2: sweep pattern direzionali (tenendo i migliori neutrali)
opt_dir = optimize(df, engine_fn,
    pattern_params_grid={
        "ptn_dir_yes": range(-52, 53),   # ±1..±52
        "ptn_dir_no":  range(-53, 54),
    },
    fixed_params={"ptn_neut_yes": best_neut_yes, "ptn_neut_no": best_neut_no}
)

# Fase 3: stop/target/orari (mai congiunto con pattern)
opt_risk = optimize(df, engine_fn,
    engine_params_grid={"start_time": [...], "end_time": [...]},
    fixed_params={**best_patterns},
    risk_params_grid={"stop_loss": [...], "take_profit": [...]}
)
```

---

## 15. VALORI SENTINELLA (PATTERN SEMPRE TRUE/FALSE)

| Funzione Python | Sempre TRUE | Sempre FALSE |
|----------------|-------------|--------------|
| `pattern_neutral(df, n)` | n=55 | n=56 |
| `pattern_directional(df, n)` | n=±52 | n=±53 |
| `pattern_fast(df, n)` | n=152 | n>152 |
| `pattern_uaptnbase(df, n)` | n=41 | n=42 |
| `pattern_specchio(df, n)` | n=±62 | n=±63 |

---

## 16. CARICAMENTO DATI

```python
# Da CSV locale
df = pd.read_csv("GC_15min.csv", parse_dates=["datetime"], index_col="datetime")
df.columns = ["open", "high", "low", "close", "volume"]

# Da yfinance
import yfinance as yf
df = yf.download("GC=F", interval="15m", period="60d")
df.columns = [c.lower() for c in df.columns]

# Da Interactive Brokers
from ib_insync import IB, Future, util
ib = IB(); ib.connect("127.0.0.1", 7497, clientId=1)
contract = Future("GC", "202412", "COMEX")
bars = ib.reqHistoricalData(contract, endDateTime="", durationStr="1 Y",
                             barSizeSetting="15 mins", whatToShow="TRADES")
df = util.df(bars).set_index("date")[["open", "high", "low", "close", "volume"]]
```

---

## 17. RIFERIMENTI

- **Codice completo** implementazioni Python (session, pattern, motori): `references/codice_base.md`
- **Esempi** strategie tradotte da TOP_UA: `references/esempi_strategie.md`
- **Valutazione** metriche + IS/OOS/WFO + MC + Z-Score: `references/validazione.md`
- **Position Sizing** formule complete + esempi numerici + decision tree: `references/position_sizing.md`
- **Portfolio** correlation + filtering + flowchart operativo: `references/portfolio.md`
- **Titan** (software portfolio management UA): parametri, filtri, MinCap, pipeline, export EL: `references/titan.md`
- **Mercati** catalogo completo con caratteristiche operative: `references/mercati.md`
- **EasyLanguage originale**: `o_decoded_scripts/`
- **Materiale didattico Unger Academy**: `Mat_didattico/` (TSS2, TSE, PS, MPS + Manuale Titan)

Per generare il codice Python di un modulo specifico, leggi prima il file di riferimento corrispondente.

---

**Versione aggiornata**: integrazione contenuti TSS2 / TSE / PS / MPS + Manuale Titan — sviluppo +
valutazione + position sizing + portfolio (con l'implementazione operativa Titan). La struttura
segue i quattro tasselli del successo Unger:
*Strategia → Valutazione → Position Sizing → Portfolio*.

