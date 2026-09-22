# Griglia grossa CL 30 minuti — 450 combinazioni su dieci anni, 22/09/2026

**Cella:** Price Channel su `@CL` a 30 minuti, contenitore `PT3B_CL_PCH_001_30` con il motore nudo
(pattern alle sentinelle, nessun filtro orario, niente trailing). Leve: canale {1, 20, 50} × stop
{150…1800} × target {0…2500} × uscita {fine sessione, 21} × direzione {0, 1, 2}. Feed ICS, spread
**ICS** 0,02, swap ICS (long credito azzerato, short 0,2132 pt/notte), **commissione zero** — la
scheda XTIUSD la dichiara zero, a differenza dell'oro — orologio al minuto. **Campione 2016-06-09 →
2023-01-01, validazione → 2026-09-01.** 62 minuti su 8 core. Dati in `cl-30m-griglia-grossa.csv`,
generati da `ClCoarseGridTests`.

I 30 minuti non sono una scelta di ricerca: sono il solo aggregato che l'archivio ICS ha sopra il
minuto per questo simbolo.

**Attenzione alla scala del denaro.** Un contratto CL è 1.000 barili, quindi **1.000 dollari per
punto** e 10 dollari per tick (0,01). Gli stop della griglia, da 150 a 1.800 dollari per contratto,
sono da 0,15 a 1,80 dollari al barile: dallo 0,2% al 2,6% con il greggio a 70. Non è la scala di GC
(100 $/punto) né di NQ (20).

## Il risultato

- **87 celle su 450** ammissibili (≥ 250 trade in campione e campione in utile).
- 37 con il fuori campione positivo, 8 con almeno 3 finestre su 4.
- **Zero equilibrate** (netto/DD ≥ 1 sia dentro sia fuori).

Le migliori per netto fuori campione:

| can | stop | targ | exit | dir | IS n | IS netto | IS DD | IS PF | OOS n | OOS netto | OOS DD | OOS PF | fin |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 50 | 1000 | 2500 | −1 | long | 1.037 | 4.950 | 27.830 | 1,01 | 572 | 19.300 | 18.130 | 1,09 | 1/4 |
| 50 | 1800 | 0 | 21 | short | 869 | 33.687 | 32.233 | 1,09 | 513 | 16.670 | 24.790 | 1,07 | 2/4 |
| 50 | 1000 | 0 | 21 | short | 869 | 23.240 | 29.490 | 1,07 | 513 | 16.520 | 18.980 | 1,08 | 2/4 |
| 20 | 1800 | 2500 | −1 | long | 1.499 | 11.700 | 34.840 | 1,02 | 847 | 12.050 | 26.700 | 1,03 | 1/4 |

### Le leve, media sulle 87 ammissibili

| leva | IS medio | OOS medio |
|---|---:|---:|
| direzione entrambe | 19.389 | −7.509 |
| direzione solo long | 8.711 | −6.257 |
| **direzione solo short** | 19.480 | **+436** |
| uscita a fine sessione | 12.368 | −3.128 |
| uscita alle 21 | 17.881 | −4.976 |
| canale 1 | 10.113 | −21.918 |
| canale 20 | 12.898 | −9.501 |
| **canale 50** | 18.909 | **+1.930** |

## Letta con i criteri del metodo (`metodo-unger`, §8.2)

Il numero che chiude la questione è il **profit factor**: sulle migliori sta fra 1,00 e 1,09, su
mille e passa trade in sei anni e mezzo. Non è un margine sottile, è assenza di margine.

L'average trade lo conferma. La cella col miglior fuori campione fa 4.950 dollari su 1.037 trade in
campione, cioè **4,77 dollari per trade**: il tick di CL vale 10 dollari, quindi quel margine non
copre nemmeno mezzo tick, contro una soglia di 6-7 tick. La migliore per campione arriva a 38,8
dollari per trade, ancora sotto la soglia prima dello sconto walk-forward.

E i drawdown sono **più grandi dei netti** quasi ovunque: 27.830 contro 4.950 sulla prima,
32.233 contro 33.687 sulla seconda.

## Conclusione

**Su CL a 30 minuti il Price Channel nudo non ha edge.** Non "poco edge": profit factor 1,0.

Il campione era quello giusto, ed è la parte che vale. Dieci anni con dentro il crollo del 2020 e lo
shock del 2022, costi ICS veri su tutte e tre le voci, commissione zero perché la scheda la dichiara
zero. Non è un no ottenuto con ipotesi pessimistiche: è un no ottenuto con il modello di costo più
favorevole dei tre simboli provati.

Due leve però parlano, e vanno nella direzione opposta a quella dell'oro: su CL **lo short è l'unica
direzione con media fuori campione positiva** (+436 contro −6.257 del long), e il canale lungo è
l'unico che non perde. L'ora di uscita non aiuta, a differenza di GC e FDAX: entrambe le varianti
perdono fuori campione.

## Cosa ne segue

Con GC 4h e NQ 15m, fanno **tre celle su tre** in cui il Price Channel nudo non regge le soglie del
metodo. Il prossimo test sull'energia non è un'altra griglia sul Price Channel: è un motore diverso,
o un timeframe che l'archivio oggi non ha — su CL manca tutto fra i 30 minuti e il giorno.

## Riferimenti

`cl-30m-griglia-grossa.csv`, `gc-4h-griglia-grossa-lunga.md` e `nq-15m-griglia-grossa.md` (le altre
due celle), `Piootoo.Strategies.Tests/ClCoarseGridTests.cs`, `metodo/metodo-unger/SKILL.md` §8.2.
