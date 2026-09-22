# Griglia grossa FDAX 4 ore sul periodo lungo — 450 combinazioni su dodici anni, 22/09/2026

**Cella:** Price Channel su `@FDAX` a 4 ore, veicolo `PT3B_FDAX_PCH_001_240` con il motore nudo
(pattern alle sentinelle, nessun filtro orario, niente trailing). Leve: canale {1, 20, 50} × stop
{1000…12000} × target {0…18000} × uscita {fine sessione, 21} × direzione {0, 1, 2}. Feed ICS, spread
**ICS** 0,50, swap ICS, commissione 19,23 per lato, orologio al minuto. **Campione 2014-07-18 →
2021-01-01, validazione → 2026-09-01.** 39 minuti su 8 core. Dati in
`fdax-4h-griglia-grossa-lunga.csv`, generati da `FdaxCoarseGridTests`.

**È la cella che conta di più**, perché è quella che ha prodotto `PT3B_FDAX_PCH_002_240`, l'unica
strategia validata del progetto. Finora era stata cercata solo su quattro anni (campione 2022-2025,
validazione 2025-2026).

## Il risultato: è la migliore delle cinque celle

- **332 celle su 450** ammissibili, contro 229 di BP, 203 di GC, 92 di NQ 15m, 87 di CL.
- **60 equilibrate** (netto/DD ≥ 1 dentro e fuori, ≥ 3 finestre su 4), contro le 34 di GC e zero
  altrove.
- E soprattutto: **33 celle con UngerFit ≥ 1 in campione**, massimo **1,95**. Su GC il migliore era
  0,76, su BP 0,18. **È l'unica delle cinque celle in cui qualcosa supera la soglia del metodo dove
  conta, cioè dentro il campione.**
- **27 celle con average trade sopra la soglia** di 266 dollari (15% del range medio della barra),
  massimo 404.

Resta sotto un criterio: **zero celle con guadagno annuo su drawdown massimo ≥ 2**, che è il filtro
Titan. Su questa cella il rapporto migliore in campione non arriva a 1.

### Le leve, media sulle 332 ammissibili

| leva | IS medio | OOS medio | equilibrate |
|---|---:|---:|---:|
| direzione entrambe | 88.304 | −19.072 | 21 su 116 |
| **direzione solo long** | 53.267 | **+71.224** | **39 su 113** |
| direzione solo short | 51.571 | **−77.180** | 0 su 103 |
| uscita a fine sessione | 53.435 | −9.581 | 20 su 130 |
| **uscita alle 21** | 72.414 | −4.297 | **40 su 202** |
| canale 1 | 68.299 | **−25.567** | 1 su 79 |
| **canale 20** | 56.680 | **+11.898** | **37 su 116** |
| canale 50 | 70.100 | −10.759 | 22 su 137 |

**L'uscita alle 21 regge su dodici anni**: quaranta equilibrate su sessanta contro venti, ed è la
leva che ha prodotto la 002. È la terza conferma indipendente, dopo la validazione della 002 stessa
e la griglia GC.

**Lo short sul DAX perde**, come sull'oro: zero equilibrate su 103 celle e media fuori campione
−77.180. È lo stesso fatto già annotato in `lavori-in-corso.md` e misurato ora su dodici anni.

## ⚠ La configurazione della 002 sta nella regione peggiore della griglia

La 002 ha **canale 1** ed **entrambe le direzioni**. Sono esattamente le due scelte che la griglia
lunga indica come le peggiori: il canale a una barra ha **una** cella equilibrata su 79 e media fuori
campione −25.567; entrambe le direzioni ne hanno 21 su 116 contro le 39 del solo long.

E la cella che corrisponde ai suoi parametri strutturali esiste nella griglia. Canale 1, stop 5.000,
target 4.500, uscita alle 21, entrambe le direzioni, **motore nudo**:

| | trade | netto | PF | finestre |
|---|---:|---:|---:|---:|
| campione 2014-2021 | 1.824 | +11.504 | 1,01 | |
| validazione 2021-2026 | 1.746 | **−121.891** | 0,95 | 1/4 |

Non è un caso isolato. **Tutte e venticinque** le combinazioni di stop e target con canale 1, uscita
alle 21 ed entrambe le direzioni hanno il fuori campione **negativo**, con profit factor fra 0,92 e
0,99.

### Cosa significa, e cosa non significa

**Non significa che la 002 sia sbagliata.** Il confronto non è alla pari: la 002 non è il motore
nudo, ha due filtri che la griglia non ha — il pattern neutro 44 (sessione stretta, escursione sotto
il 3%) e la finestra operativa 03-18. La griglia misura il motore senza filtri, la 002 è il motore
più due filtri.

**Significa che tutto il suo margine sta in quei due filtri.** Il breakout su cui poggia, sui suoi
parametri e su dodici anni, perde. La 002 non è "una buona cella con un filtro sopra": è un filtro
applicato a un breakout che da solo non funziona. E quei due filtri sono stati scelti su un campione
di tre anni, 2022-2025.

Questa è una condizione classica di sovradattamento, e andava verificata invece che assunta in un
senso o nell'altro. **La verifica è stata fatta** (`Pt3b002LongPeriodTests`): la 002 così com'è,
senza toccarle un parametro, sullo stesso periodo lungo e con gli stessi costi.

### ✔ I filtri della 002 reggono su dodici anni

| | trade | netto | DD | PF | avg trade |
|---|---:|---:|---:|---:|---:|
| **002** campione 2014-2021 | 1.609 | +157.820 | 52.650 | 1,11 | 98 |
| **002** validazione 2021-2026 | 1.527 | +106.254 | 71.589 | 1,06 | 70 |
| 001 campione 2014-2021 | 1.642 | +115.601 | 62.923 | 1,07 | 70 |
| 001 validazione 2021-2026 | 1.585 | +24.504 | 80.329 | 1,01 | 15 |

**La 002 guadagna in entrambe le metà di dodici anni**, e non è quindi una configurazione trovata
dentro il rumore di un periodo corto. I suoi due filtri erano stati scelti su tre anni e tengono su
dodici: sul fuori campione portano da **−121.891** del motore nudo a **+106.254**, cioè valgono
228.145; in campione valgono 146.316.

**E la leva dell'uscita alle 21 si conferma la terza volta**, qui sul campione più lungo possibile:
la 002 batte la 001 in entrambe le metà, e nella validazione la 001 quasi muore — 15 dollari di
average trade contro 70, profit factor 1,01 contro 1,06.

### Ma l'average trade resta molto sotto soglia

98 dollari in campione contro una soglia di 266, 70 fuori contro 365: il **37% e il 19%**. È lo
stesso limite già misurato per altra via sul run cBot, dove il rapporto guadagno annuo su drawdown
era 0,55 contro una soglia di 2.

Le due cose stanno insieme e non si contraddicono: la 002 **ha un edge reale e stabile su dodici
anni**, e quell'edge è **troppo piccolo per unità di rischio**. Non va buttata e non va nemmeno
caricata: è una strategia da tenere in portafoglio con una size proporzionata, non da far portare da
sola un conto.

## Conclusione

Il Price Channel su FDAX a 4 ore è **l'unica delle cinque celle che supera una soglia del metodo
dove conta**, e le leve che sceglie su dodici anni sono coerenti con quelle scelte su quattro: solo
long, uscita prima del rollover, canale lungo. In questo senso la cella ha qualcosa che GC, NQ, CL e
BP non hanno.

Ma la strategia che ne è uscita non sta in quella regione. Sta in canale 1 con entrambe le
direzioni, cioè dove il motore nudo perde in modo sistematico, e ci sta grazie a due filtri scelti su
tre anni. **La cosa più utile che questa griglia dice non è "FDAX è meglio", è "la 002 va
riverificata sul lungo prima di fidarsene", e indica anche dove cercare la 003**: canale 20, solo
long, uscita alle 21.

## Riferimenti

`fdax-4h-griglia-grossa-lunga.csv`, `gc-4h-griglia-grossa-lunga.md`, `bp-60m-griglia-grossa.md`,
`cl-30m-griglia-grossa.md`, `nq-15m-griglia-grossa.md` (le altre quattro celle),
`fdax-4h-pc-uscita-sessione.md` e `Piootoo.Strategies/PT3BStrategies/PT3B_FDAX_PCH_002_240.cs` (da
dove viene la 002), `Piootoo.Strategies.Tests/FdaxCoarseGridTests.cs`,
`metodo/metodo-unger/SKILL.md` §8.2 e §13.6.
