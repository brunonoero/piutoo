# La sweep dentro la regione FDAX 4 ore — una finalista passa, e non batte la regione nuda. 23/09/2026

**Cosa:** sweep a fasi del Price Channel su `@FDAX` a 4 ore **dentro la regione** che la griglia lunga
ha indicato (`fdax-4h-griglia-grossa-lunga.md`): `--fix "ChannelBars=20;Direction=1;ExitHour=21"`.
La sweep sceglie pattern, orari, stop, target e trailing. Costi del paniere: spread **per ora**
peggiore fra ICS e FTMO (1,13-4,00 punti), swap peggiore, 19,23 per lato. Campione 2014-07 → 2021-01,
validazione → 2026-09, criterio `worst-period`, beam 2, orologio al minuto. 334 minuti. Resoconto
grezzo: `fdax-4h-003-ics-costo-peggiore-per-ora-peggior-tratto-regione.md`; misure di confronto:
`fdax-4h-004-confronti.log`.

## Il risultato in breve

- **Una finalista su cinque passa la validazione**: 130 trade e +47.113 in campione, 142 trade e
  +26.938 fuori, tenuta 51% (esattamente la soglia), tre finestre su quattro.
- **Ma non batte il riferimento.** La regione **nuda**, misurata con gli stessi costi, fa di più sia
  dentro sia fuori, e fuori ha anche un rapporto netto su drawdown migliore.
- **Le cinque finaliste sono una configurazione sola** in cinque varianti di trailing e breakeven, e
  quella che passa è la terza del campione. La prima, identica senza trailing, fuori tiene il 14%.
- **Decisione: nessuna classe `PT3B_FDAX_PCH_004_240` oggi.** Si aspetta la misura A della coda di
  stasera (i filtri della 002 sulla stessa regione, ipotesi a priori).

## La finalista

Canale 20, solo long, uscita alle 21 (fissati); pattern neutrale **31 richiesto** e **32 vietato**,
cioè il range della sessione in corso fra lo 0,5% e lo 0,75%; direzionale **−5 vietato**, cioè niente
long se la sessione è scesa dall'apertura più di una volta e mezza quanto il giorno prima; tutto il
giorno, nessun giorno escluso; stop 1.000 (40 punti), target 10.000, `MaxBars` 12, trailing 2.000.
L'ablation finale non ha tolto nessun pattern.

## Il confronto che decide

Stessi costi, stesso split, stesso orologio: la sweep, la regione nuda con lo stop migliore della
griglia, e la regione nuda con il rischio della finalista. Il drawdown è ricavato dal punteggio di
validazione (netto su drawdown).

| | trade IS | netto IS | netto/DD IS | trade OOS | netto OOS | netto/DD OOS | finestre |
|---|---:|---:|---:|---:|---:|---:|---:|
| **finalista 3** (pattern + trailing 2.000) | 130 | 47.113 | **5,71** | 142 | 26.938 | 2,90 | 3/4 |
| finalista 1 (stessi pattern, niente trailing) | 130 | 58.254 | 6,48 | 142 | 18.841 | 0,88 | 2/4 |
| **regione nuda**, stop 2.500, target 9.000 | 577 | **75.815** | 3,37 | 528 | **87.567** | **3,51** | 3/4 |
| regione nuda, rischio della finalista | 585 | 63.896 | 3,72 | 539 | 31.260 | 1,90 | 3/4 |

**I filtri migliorano il rapporto in campione e lo peggiorano fuori**: da 3,37 a 5,71 dentro, da
3,51 a 2,90 fuori. È la firma del sovradattamento, e in una forma che non lascia margini: i pattern
tolgono tre trade su quattro, e quello che tolgono fuori campione valeva più di quello che tengono.

La regione nuda sui costi del paniere fa 75.815 e 87.567, contro i 96.324 e 102.480 della griglia sui
costi ICS: **il modello di costo vale circa 20.000 in campione e 15.000 fuori**, e i due numeri non si
confrontano fra loro. Qui sono confrontati solo numeri sullo stesso modello.

## Letta con i criteri del metodo

| | avg trade IS | soglia | avg trade OOS | soglia | UngerFit IS | guadagno annuo / DD |
|---|---:|---:|---:|---:|---:|---:|
| finalista 3 | **362** (136%) | 266 | 190 (52%) | 365 | **2,4** | 0,88 IS · 0,51 OOS |
| regione nuda | 131 (49%) | 266 | 166 (45%) | 365 | 0,53 | 0,52 IS · 0,62 OOS |

Sulla carta la finalista è la prima configurazione del progetto che passa average trade e UngerFit
**dentro il campione**. Non vale come promozione per tre ragioni, in ordine di peso:

1. **Fuori campione dimezza** (362 → 190, al 52% della soglia) mentre la regione nuda resta stabile
   (131 → 166). La finalista passa le soglie perché fa un quinto dei trade, e il vantaggio per trade
   non sopravvive allo split.
2. **La selezione ha guardato il fuori campione.** Si validano le prime cinque del campione e passa la
   terza, la sola con trailing 2.000; la prima, stessa configurazione senza trailing, fuori tiene il
   14%. Scegliere il trailing perché è quello che passa la validazione è scegliere sulla validazione.
3. **Il filtro neutrale è una fascia stretta**, range di sessione fra 0,5% e 0,75%, trovata fra 3.025
   combinazioni: il genere di condizione che un campione di 130 trade in sei anni e mezzo seleziona
   per caso.

Resta un dato da non buttare: **nell'ultima finestra, 2025-04 → 2026-09, la regione nuda perde
(−5.440) e la finalista guadagna (+15.009)**. Su 35 trade non basta a dire nulla, ma se la misura A
mostrasse la stessa cosa con i filtri della 002, sarebbe un indizio su cosa filtrare.

## Conclusione

**La regione regge da sola, e la sweep non l'ha migliorata.** La regione nuda — canale 20, solo long,
uscita alle 21 — guadagna in entrambe le metà di dodici anni sui costi peggiori del paniere, con un
rapporto netto su drawdown fra 3,4 e 3,5; ma resta a metà della soglia di average trade, come la 002.
La finalista ha i numeri per il metodo solo dentro il campione e li perde fuori.

Cosa segue, in ordine:

1. **Misura A della coda** (`fdax-4h-regione-filtri-002.log`, stasera): i due filtri della 002 sulla
   stessa regione. Sono l'ipotesi a priori che questa sweep non aveva: se portano l'average trade verso
   la soglia **senza** far scendere il fuori campione, quella è la 004.
2. Se non basta, la regione nuda è comunque una **componente di paniere** con lo stesso profilo della
   002 — edge reale, piccolo per unità di rischio — e va confrontata con la 002 sugli stessi costi per
   capire se sono la stessa scommessa o due.
3. **Lezione per la sweep**: dentro una regione con un edge il criterio `worst-period` sceglie ancora
   il conteggio, qui 130 trade con il pavimento a 50. Il prossimo lancio dentro una regione dovrebbe
   avere `--min-trades` al livello della griglia, 250.

## Riferimenti

`fdax-4h-003-ics-costo-peggiore-per-ora-peggior-tratto-regione.md` e `.log` (la sweep),
`fdax-4h-004-confronti.log` (le quattro misure), `fdax-4h-griglia-grossa-lunga.md` (la regione),
`Piootoo.Strategies/PT3BStrategies/PT3B_FDAX_PCH_002_240.cs`, skill `lettura-risultati`.
