# Griglia grossa GC 4 ore sul periodo lungo — 450 combinazioni, 22/09/2026

**Cella:** Price Channel su `@GC` a 4 ore, contenitore `PTS_GC_PCH_004_240` con il motore nudo
(pattern alle sentinelle, nessun filtro orario, niente trailing). Leve: canale {1, 20, 50} × stop
{1000…8000} × target {0…15000} × uscita {fine sessione, 21} × direzione {0, 1, 2}. Feed ICS, spread
**ICS** 0,09 punti, swap ICS (long 0,5949 pt/notte, short credito azzerato), commissione 10,8 per
lato, orologio al minuto. **Campione 2014-09-22 → 2021-01-01, validazione → 2026-09-01.** 36 minuti
su 8 core. Dati in `gc-4h-griglia-grossa-lunga.csv`, generati da `GcCoarseGridTests`.

## Perché è stata rifatta

Il primo giro (stesso giorno, `gc-4h-griglia-grossa.md`) girava su campione 2022-2025 e trovò 146
celle su 148 in utile fuori campione: la lettura fu che quel fuori campione era il **rally dell'oro**
e non i pattern, perché quando una cella qualunque guadagna ciò che si misura è il mercato. Il test
che quella lettura prescriveva era esattamente questo — un campione che contenga anche laterali e
ribassi — e allora non si poteva fare perché l'archivio ICS di XAUUSD partiva dal settembre 2022. La
raccolta del 21-22/09 lo ha portato al 2014-09.

Due cose cambiano oltre al periodo. Lo spread è ora quello **ICS misurato** (0,09 di mediana su 10,3
milioni di tick) e non più quello FTMO usato come ripiego. E le uscite scendono da quattro a due,
perché il primo giro aveva misurato che sull'oro l'ora di uscita non conta: **quella misura era
sbagliata**, e la smentita è qui sotto.

## Il risultato

- **203 celle su 450** ammissibili (≥ 250 trade in campione e campione in utile), contro le 148 su
  900 del giro corto.
- **34 equilibrate** (netto/DD ≥ 1 sia dentro sia fuori, ≥ 3 finestre su 4 in utile). Il giro corto
  ne aveva **zero**.

Le migliori, ordinate per netto/DD fuori campione:

| can | stop | targ | exit | dir | IS n | IS netto | IS DD | IS PF | OOS n | OOS netto | OOS DD | OOS PF | fin |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 50 | 1000 | 2000 | −1 | 0 | 580 | 24.240 | 20.780 | 1,13 | 602 | 107.231 | 11.899 | 1,39 | 4/4 |
| 50 | 2000 | 2000 | −1 | long | 321 | 16.782 | 10.592 | 1,15 | 369 | 108.881 | 12.302 | 1,55 | 3/4 |
| 50 | 2000 | 2000 | 21 | long | 340 | 23.182 | 11.159 | 1,21 | 384 | 107.386 | 12.302 | 1,54 | 3/4 |
| 20 | 3000 | 4000 | 21 | long | 503 | 28.354 | 13.514 | 1,17 | 532 | 146.567 | 17.809 | 1,42 | 3/4 |
| **50** | **1000** | **2000** | **21** | **long** | **341** | **23.925** | **8.553** | **1,23** | **386** | **78.620** | **10.169** | **1,45** | **4/4** |

L'ultima è la più equilibrata delle 34 e serve da riferimento nel resto del documento.

### Le leve, media sulle 203 ammissibili

| leva | IS medio | OOS medio | equilibrate |
|---|---:|---:|---:|
| direzione **entrambe** | 9.625 | 110.948 | 6 su 71 |
| direzione **solo long** | 17.746 | 102.104 | **28 su 89** |
| direzione **solo short** | 6.997 | **−13.193** | 0 su 43 |
| uscita a fine sessione | 9.112 | 68.553 | 8 su 97 |
| **uscita alle 21** | 15.847 | 91.959 | **26 su 106** |
| canale 1 | 9.362 | **−47.495** | 0 su 15 |
| canale 20 | 15.802 | 112.478 | 11 su 80 |
| canale 50 | 10.732 | 75.106 | 23 su 108 |

**Lo short sull'oro perde**, ed è l'unica leva con media fuori campione negativa insieme al canale a
una barra. Ventotto delle 34 equilibrate sono solo long.

**L'ora di uscita conta, al contrario di quanto diceva il giro corto.** Ventisei equilibrate su 34
hanno l'uscita alle 21 contro otto a fine sessione, e la media fuori campione è 91.959 contro 68.553.
Il giro corto aveva concluso «sull'oro l'ora di uscita non conta: 103-113k in ogni caso» perché in
quel periodo *tutto* guadagnava: una leva non si misura dove ogni valore funziona.

## Letta con i criteri del metodo (`metodo-unger`, §8.2 e §13.6)

La soglia dell'average trade è il 15% del range medio della barra, e sull'oro **il range è cambiato
di due volte e mezzo fra i due periodi**:

| periodo | barre 4h | range medio | soglia 15% |
|---|---:|---:|---:|
| campione 2014-2021 | 9.694 | 6,66 punti = 666 $ | **100 $** |
| validazione 2021-2026 | 8.772 | 16,71 punti = 1.671 $ | **251 $** |

Applicando i due criteri alle 34 equilibrate:

| criterio | soglia | quante passano |
|---|---|---:|
| UngerFit in campione | ≥ 1 | **0 su 34** |
| UngerFit fuori campione | ≥ 1 | 16 su 34 |
| guadagno annuo / DD massimo in campione | ≥ 2 | **0 su 34** |
| guadagno annuo / DD massimo fuori campione | ≥ 2 | **0 su 34** |
| tutti e quattro | | **0 su 34** |

Sulla cella di riferimento (canale 50, stop 1000, target 2000, uscita 21, solo long):

- **Average trade**: 70 $ in campione contro una soglia di 100, 204 $ fuori contro 251. **Sotto in
  entrambi i periodi**, e sotto nello stesso modo — circa il 70-80% della soglia.
- **UngerFit**: 0,76 in campione, 1,27 fuori. Sotto 1 dove conta, cioè dove la configurazione è
  stata scelta.
- **Guadagno annuo / DD massimo**: 0,45 in campione, 1,36 fuori. La soglia è 2.

Il migliore UngerFit in campione di tutte e 34 è **0,76**. Il migliore rapporto guadagno/DD in
campione è **0,45**.

## Conclusione

**Nessuna classe GC neanche stavolta**, ma per una ragione diversa e più istruttiva di quella del
giro corto.

Il giro corto diceva «non si distingue la strategia dal mercato». Il giro lungo lo distingue: su sei
anni di oro laterale il motore nudo **guadagna** — 203 celle ammissibili su 450, PF fino a 1,23 su
341 trade — e le leve che sceglie hanno un senso che si ripete fuori campione: solo long, canale
lungo, uscita prima del rollover. Non è rumore.

Ma **è troppo poco**. L'average trade in campione sta al 70% della soglia del metodo, il rapporto
guadagno/DD è 0,45 contro 2, e l'UngerFit non arriva a 1 su nessuna delle 34. Un sistema così non è
sbagliato: è sottile, e il costo di transazione più la varianza se lo mangiano.

E c'è un fatto che va detto per non rileggerlo male fra un mese: **fuori campione tutto migliora di
un fattore tre o quattro, e la volatilità dell'oro è cresciuta di due volte e mezzo nello stesso
periodo.** L'average trade passa da 70 a 204 mentre la soglia passa da 100 a 251: il sistema non è
migliorato, ha seguito il mercato. È la conferma quantitativa di ciò che il giro corto aveva
sospettato, ottenuta questa volta con un campione che permette di sottrarre il regime.

## Cosa ne segue

1. **Sul Price Channel nudo su GC a 4 ore si chiude qui.** Tre griglie su tre celle — GC 4h, NQ 15m,
   CL 30m — dicono la stessa cosa: il motore nudo non ha un edge che regga le soglie. Aggiungere
   filtri a un motore che parte sotto soglia è la strada che la sweep ha già percorso su FDAX e NQ
   senza arrivare da nessuna parte.
2. **Le due leve misurate restano**, e valgono per qualunque cosa si faccia sull'oro: lo short perde,
   e l'uscita prima del rollover batte quella a fine sessione. La seconda è la stessa leva che su
   FDAX ha prodotto `PT3B_FDAX_PCH_002_240`.
3. **Il prossimo test non è un'altra griglia sulla stessa cella**, è un motore diverso. Sul catalogo
   le uniche coerenti su NQ erano trend following con average trade da 250-400 dollari, non Price
   Channel.

## Riferimenti

`gc-4h-griglia-grossa-lunga.csv`, `gc-4h-griglia-grossa.md` (il giro corto, che questo supera),
`gc-4h-pc-tutto-al-minuto.md` (la sweep), `nq-15m-griglia-grossa.md` e `cl-30m-griglia-grossa.csv`
(le altre due celle), `Piootoo.Strategies.Tests/GcCoarseGridTests.cs`,
`metodo/metodo-unger/SKILL.md` §8.2 e §13.6.
