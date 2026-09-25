---
name: controllo-ran
description: >
  Misurare una strategia Piootoo contro il caso: stesse uscite, ingresso casuale con seme fisso (motore
  RAN), cento semi, percentile della strategia fuori campione. Usala quando si chiede se una strategia
  "vale per il segnale", "batte il caso", "il controllo RAN", o prima di mettere in un piano una
  finalista di una famiglia PT6EXO bizzarra.
---

# Il controllo RAN

**Bozza del 25/09/2026.** Il motore: `RandomEntryEngine` (contenitore `RC_RAN`). Il primo studio,
da copiare: `Piootoo.Strategies.Tests/Pt3b002RandomControlStudy.cs`.

## La domanda

Una strategia guadagna per il suo **segnale** o per la forma delle sue **uscite** e il mercato? RAN
tiene tutto cio' che non e' il segnale — stop, target, trailing, breakeven, tenuta, ora di uscita,
finestra, un ingresso per sessione — e rende casuali il *quando* e il *verso*. Cento semi danno la
distribuzione di cio' che quelle uscite fanno da sole.

## Come

1. **Copiare le uscite** della strategia nelle chiavi di `RC_RAN` (`StopLoss`/`TakeProfit` o
   `StopAtr`/`TargetAtr`, `MaxBars`, `IntradayOnly`, `ExitHour`, `StartHour`/`EndHour`,
   `MaxEntriesPerSession`, `TrailingStop`, `BreakEven`). Il segnale non si copia: e' il punto.
2. **Stessi feed, split, costi e orologio** del resoconto della strategia (di solito orologio al minuto,
   spread e swap dal broker, commissione per lato).
3. **Tarare `EntryProbability`** separatamente dentro e fuori campione, su pochi semi (8), finche' i
   trade medi stanno entro il 3% di quelli della strategia: la probabilita' vale per barra **flat**, e
   la relazione non e' lineare.
4. **Cento semi diversi da quelli della taratura** (1001-1100), in parallelo con un `SweepRunner` per
   thread.
5. Stampare, per netto, average trade, profit factor e netto/DD: valore della strategia, p5, mediana,
   p95 dei semi e **percentile** della strategia; CSV per seme in `ricerca/`.

Lancio, come ogni studio:

```powershell
$env:PIOOTOO_STUDI='1'; dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj --filter "FullyQualifiedName~<ClasseStudio>" --logger "console;verbosity=detailed" *>&1 | Out-File piootoo-repository/ricerca/<nome>.log -Encoding utf8
```

Mai insieme a un'altra griglia, sweep o studio (la coda guarda i `testhost` e aspetta). Durata: la 002
su dodici anni di FDAX 4h, orologio al minuto, 100 semi per due periodi, ha chiesto **13 minuti** su 8
core: il controllo si puo' fare su ogni cella che risponde.

## Lettura

- Conta il percentile **fuori campione**. Dentro il campione la strategia e' stata scelta: stare sopra
  i semi li' e' atteso.
- Sopra quasi tutti i semi fuori campione: il segnale porta qualcosa.
- Nel mezzo della distribuzione: il guadagno lo farebbe una moneta con le stesse uscite. La strategia
  puo' ancora servire a un piano come esposizione, ma non come "idea che funziona", e non si promuovono
  varianti del suo segnale.
- Il controllo non sostituisce le soglie del metodo (`lettura-risultati`): si aggiunge.

## Trappole

- Il caso di RAN e' una funzione di (seme, simbolo, timeframe, apertura della barra): stesso seme,
  stesse barre, in backtest e live. Il valore di un'estrazione e' fissato in
  `RandomEntryEngineTests.TheDrawIsStableAcrossProcesses`: se cambia, i controlli archiviati non si
  ripetono piu'.
- Con stop e target in ATR la finestra minima cresce (`AtrWarmupCandles`): e' gia' nella base.
