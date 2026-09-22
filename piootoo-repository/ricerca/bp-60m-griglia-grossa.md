# Griglia grossa BP 60 minuti — 450 combinazioni su dodici anni, 22/09/2026

**Cella:** Price Channel su `@BP` a 60 minuti, contenitore `PT3B_BP_PCH_001_60` con il motore nudo
(pattern alle sentinelle, nessun filtro orario, niente trailing). Leve: canale {1, 20, 50} × stop
{100…1200} × target {0…1600} × uscita {fine sessione, 21} × direzione {0, 1, 2}. Feed ICS, spread
**ICS** 0,00002 (0,2 pip), swap ICS, commissione 4 per lato, orologio al minuto. **Campione
2014-09-21 → 2021-01-01, validazione → 2026-09-01.** 51 minuti su 8 core. Dati in
`bp-60m-griglia-grossa.csv`, generati da `BpCoarseGridTests`.

È la prima **valuta** dopo un indice (NQ), un metallo (GC) e un'energia (CL).

## Il risultato

- **229 celle su 450** ammissibili (≥ 250 trade in campione e campione in utile). È la quota più
  alta delle quattro celle provate.
- **Zero equilibrate** (netto/DD ≥ 1 sia dentro sia fuori, ≥ 3 finestre).
- **Zero celle con average trade sopra soglia**, e non è una questione di poco: la soglia è 40
  dollari (6-7 tick da 6,25) e il massimo su 229 celle ammissibili è **12,2**.

Le migliori per netto fuori campione:

| can | stop | targ | exit | dir | IS n | IS netto | avg IS | PF IS | OOS n | OOS netto | avg OOS | PF OOS | fin |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 700 | 1600 | −1 | short | 1.621 | 9.707 | 6,0 | 1,03 | 1.469 | 8.132 | 5,5 | 1,04 | 3/4 |
| 50 | 200 | 800 | 21 | long | 640 | 1.943 | 3,0 | 1,03 | 586 | 7.771 | 13,3 | 1,14 | 3/4 |
| 1 | 400 | 1600 | −1 | short | 1.621 | 14.404 | 8,9 | 1,05 | 1.469 | 7.677 | 5,2 | 1,04 | 3/4 |
| 1 | 400 | 0 | −1 | short | 1.621 | 19.756 | **12,2** | 1,07 | 1.469 | 5.874 | 4,0 | 1,03 | 3/4 |

### Le leve, media sulle 229 ammissibili

| leva | IS medio | OOS medio |
|---|---:|---:|
| direzione entrambe | 9.450 | −8.312 |
| direzione solo long | 2.590 | −601 |
| direzione solo short | 9.397 | −4.114 |
| uscita a fine sessione | 8.745 | −4.981 |
| uscita alle 21 | 7.975 | −5.001 |
| canale 1 | 8.016 | −3.532 |
| canale 20 | 7.871 | −7.292 |
| canale 50 | 8.767 | −3.787 |

**Nessuna leva ha media fuori campione positiva.** E l'ora di uscita, che su GC e FDAX è la leva che
conta, qui non sposta nulla: −4.981 contro −5.001. Era prevedibile e conferma la lettura: quella
leva vale dove esistono ore in cui il sottostante è chiuso e quota il solo CFD, e su un cambio non
esistono.

## Letta con i criteri del metodo (`metodo-unger`, §8.2 e §13.6)

| criterio | soglia | migliore delle 229 |
|---|---|---:|
| average trade in campione | ≥ 40 $ | **12,2** |
| UngerFit in campione | ≥ 1 | **0,18** |
| guadagno annuo / DD massimo in campione | ≥ 2 | **0,26** |

**Su questa cella la soglia che morde è quella dei tick, non quella del range.** Il range medio della
barra da 60 minuti è 22 pip nel campione e 18 fuori, quindi il 15% vale 20 e 17 dollari: meno dei 40
che valgono 6-7 tick. Sulle altre tre celle era il contrario, e il range dettava la soglia.

**Il costo spiega quasi tutto.** Otto dollari di commissione per il giro più circa 1,25 di spread
fanno **9,25 dollari per trade**. La cella migliore ne guadagna 12,2 netti, cioè un lordo di 21,5 di
cui il costo si prende il 43%. Non è un sistema con poco margine: è un sistema il cui margine lordo è
dello stesso ordine del costo di transazione.

## Conclusione

**Su BP a 60 minuti il Price Channel nudo non ha edge, e il margine lordo non basta nemmeno a pagare
il giro.** È il no più netto delle quattro celle: su GC 34 configurazioni arrivavano a essere
equilibrate prima di cadere sui criteri, qui nessuna arriva neanche a un average trade decente.

**E con questo la risposta non è più sui mercati, è sul motore.** Quattro celle su quattro classi di
sottostante — indice, metallo, energia, valuta — con dodici, dodici, dieci e dodici anni di storia e
costi misurati su ognuna:

| cella | ammissibili | equilibrate | miglior avg trade IS | soglia | miglior UngerFit IS |
|---|---:|---:|---:|---:|---:|
| GC 4h | 203/450 | 34 | 77 | 100 | 0,76 |
| NQ 15m | 92/450 | 1 | 119 | 120 | 0,35 |
| CL 30m | 87/450 | 0 | 38,8 | 60 | — |
| **BP 60m** | 229/450 | **0** | **12,2** | **40** | **0,18** |

Il Price Channel nudo **non produce un average trade sopra soglia su nessuna delle quattro**. GC è
la più vicina, al 77% della soglia; BP è al 30%.

## Cosa ne segue

1. **Si chiude la ricerca sul Price Channel nudo.** Non perché quattro no siano una prova, ma perché
   sono quattro no coerenti fra loro su mercati che non si somigliano, con la stessa forma: molte
   celle ammissibili, profit factor appena sopra 1, average trade sotto soglia. Aggiungere pattern e
   orari a un motore che parte al 30-77% della soglia è ciò che la sweep ha già fatto su FDAX e NQ
   senza arrivare da nessuna parte.
2. **Il prossimo test è un motore diverso**, e la griglia va estesa per poterlo fare: oggi le sue
   leve — canale, stop, target, uscita, direzione — sono quelle del Price Channel. Sul catalogo le
   uniche strategie coerenti su NQ erano trend following con average trade fra 250 e 400 dollari,
   cioè da tre a cinque volte il migliore che il Price Channel abbia prodotto su qualunque cella.
3. **Una nota su BP che vale a prescindere dal motore**: con 4 dollari di commissione per lato, un
   sistema su questo simbolo deve fare almeno 40 dollari lordi per trade per avere senso. A 22 pip
   di range orario, significa catturare un terzo del movimento di una barra. È un vincolo del
   mercato, non del motore.

## Riferimenti

`bp-60m-griglia-grossa.csv`, `gc-4h-griglia-grossa-lunga.md`, `nq-15m-griglia-grossa.md`,
`cl-30m-griglia-grossa.md` (le altre tre celle),
`Piootoo.Strategies.Tests/BpCoarseGridTests.cs`, `metodo/metodo-unger/SKILL.md` §8.2 e §13.6.
