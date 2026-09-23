# La sweep del trend following unmirrored su NQ 15 minuti — una ricerca che non è partita. 23/09/2026

**Cosa:** sweep a fasi con `SweepSpaces.TrendFollowingUnmirrored` (`--engine TFU`), contenitore
`PT3B_NQ_TFU_001_15`, fasi pattern spezzate. Feed ICS, spread per ora peggiore ICS/FTMO (1,45-1,95
punti), swap peggiore, 19,23 per lato, orologio al minuto. Campione 2022-01 → 2025-01, validazione →
2026-09. Soglie: 50 trade, profit factor ≥ 1,25, utile medio ≥ 120. 104 minuti. Resoconto grezzo:
`nq-15m-tfu-ics-costo-peggiore-per-ora-peggior-tratto.md`.

È la seconda prova del trend following, quella che cambia tre cose insieme rispetto alla griglia TF
del 22/09 (`nq-4h-tf-griglia-grossa.md`): unmirrored, 15 minuti, pattern cercati.

## Il risultato: zero ammissibili in ogni fase, e per questo nessun verdetto

| fase | combinazioni | ammissibili |
|---|---:|---:|
| uscita base | 2 | 0 |
| i quattro pattern, uno alla volta | 610 | 0 |
| orari e giorni | 1.250 | 0 |
| uscita di sessione | 9 | 0 |
| stop e target | 1.352 | 0 |

Le configurazioni facevano fino a 797 trade in campione, quindi non è il minimo di trade. Sono le
altre due soglie. La configurazione di partenza — entrambi i lati, nessun filtro, stop 1.000, target
3.000 — misurata a parte sugli stessi costi:

| | trade | netto |
|---|---:|---:|
| campione 2022-2025 | 796 | **−21.923** |
| validazione 2025-2026 | 459 | **−67.346** |

Da lì nessun passo singolo arriva a un profit factor di 1,25 e a 120 dollari per trade: né un pattern
alla volta, né una finestra oraria, né uno stop. Siccome una fase senza ammissibili lascia i semi
invariati, **ogni fase è ripartita dalla configurazione iniziale**: la sweep ha misurato 3.223
combinazioni ed è rimasta ferma al punto di partenza.

**Questo non è un no sul trend following a 15 minuti.** È un no sullo strumento usato in questo
modo: le soglie di ammissibilità dentro le fasi funzionano quando la partenza è già vicina (la regione
FDAX partiva da un profit factor di 1,22), e bloccano la ricerca quando la partenza è lontana.

## Cosa segue

- **Già in coda**: la stessa cella senza le due soglie nelle fasi
  (`tools/coda-nq-tfu-senza-soglie-2026-09-23.ps1`, resoconto `nq-15m-tfu-senza-soglie-nelle-fasi.md`),
  dopo le griglie di stasera. Restano il minimo di 250 trade e i vincoli per tratto, e la validazione
  fuori campione non cambia: l'ordinamento può così salire un gradino alla volta.
- **Un dato da tenere accanto**: la TF unmirrored del catalogo che reggeva ai costi veri,
  `PTS_NQ_TFU_003_15`, è **multiday** con pattern 63/79 sul long e 37/137 sullo short e finestra
  17-03. La sweep parte da intraday e deve arrivarci da sola; se la seconda corsa non ci arriva, la
  prossima prova è un passo a mano da quella classe, con l'uscita alle 21 come ipotesi a priori.
- **Per lo skill `sweep-cella`**: le soglie di profit factor e utile medio sono un filtro per la
  validazione e per le celle che partono vicine, non un cancello per ogni fase.

## La seconda corsa, senza soglie nelle fasi: la ricerca parte, e il risultato è sovradattamento

`nq-15m-tfu-senza-soglie-nelle-fasi.md`, stessa cella, stessi costi, stesso split; profit factor e
utile medio tolti dalle fasi (l'utile medio a un valore negativo, perché con 0 il criterio scarta
comunque le configurazioni in perdita: il primo lancio con 0 si era bloccato di nuovo). Restano 250
trade minimi e i vincoli per tratto. 166 minuti.

Stavolta la ricerca sale, un filtro alla volta:

| dopo la fase | trade IS | netto IS | drawdown IS | PF |
|---|---:|---:|---:|---:|
| uscita base | 773 | −43.402 | 68.531 | 0,93 |
| pattern long richiesto (33) | 644 | 73.597 | 22.468 | 1,17 |
| pattern long vietato (115) | 609 | 72.479 | 20.391 | 1,18 |
| pattern short richiesto (25) | 439 | 94.643 | 18.216 | 1,34 |
| pattern short vietato (6) | 347 | 116.645 | 12.234 | 1,57 |
| orari (08 → 02), uscita, stop | 346 | **119.647** | 12.234 | 1,58 |

E fuori campione crolla: **tutte e cinque le finaliste perdono**, la migliore −31.155 su 220 trade,
una finestra su quattro in utile. Le cinque sono la stessa configurazione con `MaxBars` e `ExitHour`
diversi, entrambi inerti o quasi su una intraday.

**Questo è un no che vale.** Quattro pattern scelti uno dopo l'altro su tre anni hanno portato una
configurazione che perdeva 43.000 a guadagnarne 120.000 in campione, con un rapporto netto su drawdown
di quasi 10 — un numero troppo bello, come quelli che il 20/09 avevano segnalato le celle bocciate — e
il fuori campione non ne conserva niente. Il trend following unmirrored a 15 minuti su NQ, cercato da
zero, non ha un edge sui costi veri. La sola strada rimasta è il passo a mano dalla
`PTS_NQ_TFU_003_15`, con un'ipotesi a priori.

## Riferimenti

`nq-15m-tfu-ics-costo-peggiore-per-ora-peggior-tratto.md` e `.log`, `nq-4h-tf-griglia-grossa.md`,
`nq-catalogo-costi-veri.md`, `Piootoo.Core/Optimization/Sweep/SweepSpace.cs`
(`TrendFollowingUnmirrored`).
