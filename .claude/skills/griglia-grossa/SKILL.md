---
name: griglia-grossa
description: >
  Aprire una cella di ricerca Piootoo con la griglia grossa: contenitore PT3B, CoarseGridSpec,
  lancio con PIOOTOO_STUDI, lettura del CSV e rapporto in piootoo-repository/ricerca. Usala quando
  si chiede di "lanciare la griglia grossa" su un simbolo, un timeframe o un motore, di aprire una
  cella nuova, o di capire se un motore nudo ha un edge su un mercato.
---

# Griglia grossa: aprire una cella

**Bozza del 23/09/2026.** Le regole qui sotto hanno retto a sette griglie; le soglie vanno riviste
alla fine del percorso di messa a punto. Il codice: `Piootoo.Strategies.Tests/CoarseGridStudy.cs`;
il perché: `docs/domini/ricerca-parametri.md`, `decisioni.md` 2026-09-22.

## Cosa risponde e cosa no

Una cella è la terna **simbolo × timeframe × motore**. La griglia fa alla cella una domanda sola:
**il motore nudo ha un edge qui?** Permuta le sole leve grosse (leva strutturale del motore, stop,
target, ora di uscita, direzione se il motore la ha) con pattern alle sentinelle e nessun filtro
orario, ogni combinazione dentro **e** fuori campione, orologio al minuto. Non sceglie filtri: quello
è il mestiere della sweep (`/sweep-cella`), e solo dentro una cella che ha risposto sì.

Il timeframe **non** si permuta: un altro timeframe è un'altra cella, un'altra classe di test e di
solito un altro contenitore.

## Prima di lanciare

1. **Il feed.** `piootoo-repository/datafeed-external/{BROKER}/@SYM_{tf}.json` deve esistere insieme
   al minuto (`@SYM_1.json`). Un aggregato mancante si deriva con `rebuild-from-minutes` (server
   acceso), non si inventa. Senza minuto la cella non si lancia: ES, BP, EC del vendor si cercano con
   `ClockTimeframeMinutes = 15`, misurato equivalente; GC e CL vendor hanno solo il 30 e restano fermi.
2. **I costi**, sempre misurati: spread da `spread/{BROKER}/`, swap da `swap/{BROKER}/`, commissione
   **per lato** dalla scheda del broker (ICS DE40: 38,46 di round turn = 19,23). Un costo che manca
   non vale zero: `SpreadTable`/`SwapTable` fanno fallire l'avvio, e va bene così.
3. **Il contenitore.** Una classe `PT3B_{SYM}_{ENG}_{nnn}_{tf}` con `IsResearchContainer => true`,
   `ResearchLabelsBarsOnOpen = true`, `TradingWindow = ZonedWindow.AllDay`, pattern alle sentinelle
   e un `Initialize` che legge **tutte** le chiavi della griglia, compresa `ExitHour`. Modelli:
   `PT3B_FDAX_SBO_001_240` (BO), `PT3B_FDAX_RBM_001_240` (RBB), `PT3B_FDAX_VBO_001_240` (VBO),
   `PT3B_NQ_TFU_001_15` (TF), `PT3B_FDAX_PCH_003_60` (PC). Il progressivo è contiguo per (serie,
   simbolo, motore) **senza distinguere il timeframe**: `PtsNamingConventionTests` lo impone. Una
   strategia vera può fare da veicolo solo se il suo `Initialize` legge tutte le chiavi (la griglia
   FDAX 4h ha usato `PT3B_FDAX_PCH_001_240`): se non le legge, la leva è inerte **in silenzio**.
4. **Lo split.** Lo stesso per tutte le celle che si confronteranno: 2014-07 → 2021-01 → 2026-09 dove
   il feed arriva al 2014; 2022-01 → 2025-01 → 2026-09 dove parte dal 2022. Celle su split diversi non
   si sommano in un paniere.

## La classe di test

Copia `FdaxCoarseGridTests` o `FdaxEnginesCoarseGridTests`: `[Trait("Category", ResearchStudy.Category)]`,
una `CoarseGridSpec`, `await CoarseGridStudy.RunAsync(spec, output)`. Griglie tipiche: 3-5 valori per
leva, 5 stop attorno allo stop della finalista più vicina (da un quinto al doppio), 5 target con lo
0 = nessuno, uscite `[-1, 21]`, direzioni `[0, 1, 2]` solo se il motore legge `Direction`
(`VariesDirection: false` altrimenti, o gira tre volte la stessa cosa). Una leva decimale viaggia
con `FirstLeverDivisor`. 450 combinazioni su 12 anni a 4 ore: ~40 minuti su 8 core; a 15 minuti su 4
anni: ~3,5 ore.

## Lancio

Mai mentre gira una sweep o un'altra griglia: stessi core, tempi raddoppiati per entrambe.

```powershell
$env:PIOOTOO_STUDI='1'; dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj --filter "FullyQualifiedName~NomeClasse" --logger "console;verbosity=detailed" *>&1 | Out-File piootoo-repository/ricerca/<cella>.log -Encoding utf8
```

Per una coda di più celle: uno script in `tools/` che le lancia in sequenza e scrive un log unico
(`coda-griglie-2026-09-23.ps1` è il modello). Il CSV finisce in `ricerca/<CsvName>`.

## Lettura

Con `/lettura-risultati`. In breve: ammissibili (≥ 250 trade in campione, campione in utile),
equilibrate (netto/DD ≥ 1 dentro e fuori, ≥ 3 finestre su 4), e poi le soglie del metodo **dentro
il campione**: average trade ≥ 15% del range medio della barra in dollari, UngerFit ≥ 1. Le medie per
leva dicono quale leva conta (finora: uscita alle 21 sì, short no, canale 1 no).

## Rapporto

Un file `ricerca/<simbolo>-<tf>-<motore>-griglia-grossa[-lunga].md`, con questa forma:
cella e leve nella prima riga; costi, periodo, durata, CSV e classe; **il risultato** in tre-cinque
punti con i numeri; la tabella delle leve; **cosa significa e cosa non significa**; conclusione con
la decisione operativa (nessuna classe / classe accanto al contenitore / dove cercare); riferimenti.
Poi la riga nella tabella delle celle in `docs/lavori-in-corso.md`.

## Dove non applicarla

- Non per scegliere pattern o orari: la griglia li tiene fermi per costruzione.
- Non "tutte le celle": 240 celle sono dieci giorni di macchina e altrettante occasioni di trovare
  una fortunata. Ordine: altri motori sulla cella che ha un edge, poi altri timeframe dello stesso
  mercato, poi altri simboli **solo con i motori che hanno mostrato qualcosa**.
- Non su un feed diverso da quello su cui si opererà: il CFD e il future non sono la stessa serie.
