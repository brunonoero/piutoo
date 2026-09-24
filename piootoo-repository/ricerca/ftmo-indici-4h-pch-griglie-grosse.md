# Cinque celle FTMO a 4 ore, Price Channel nudo: griglie grosse — un segnale su JP225, nessuna classe

Celle **UK100 (@Z), FRA40 (@FCE), JP225 (@NIY), US2000 (@RTY), argento (@SI) × 4 ore × Price
Channel**, le stesse leve e lo stesso periodo di EU50 (`eu50-4h-pch-griglia-grossa.md`): canale
1/20/50, 5 stop e 5 target (la griglia FDAX riscalata sul range in denaro di ogni mercato), uscita
libera o alle 21, direzione entrambe/long/short. 450 combinazioni per cella, feed e costi FTMO,
2022-01 → 2026-09 con split 2025-01. Lanciate dalla coda il 24/09/2026, circa 15 minuti ciascuna.

## Il risultato

| cella | spread / range 4h | ammissibili | equilibrate | esito |
|---|---:|---:|---:|---|
| UK100 | 2,4% | 15 | 0 | no |
| FRA40 | 3,3% | 8 | 0 | no |
| **JP225** | 4,5% | **151** | **23** | segnale, sotto soglia |
| US2000 | 6,5% | 12 | 0 | no |
| argento | 22,2% | 0 | 0 | no |
| (EU50) | 5,9% | 9 | 0 | no |

- **UK100**: le ammissibili sono tutte vicine allo zero nel campione (la migliore +7.935 sterline in
  tre anni); le due in utile fuori campione su 3 finestre hanno un campione quasi nullo (+1 e +1.314).
- **FRA40**: 7 ammissibili su 8 a canale 1, solo long, uscita alle 21; tutte in perdita fuori campione.
- **US2000**: tutte in perdita fuori campione, da −7.251 a −53.666.
- **Argento**: nessuna combinazione in utile nel campione. Lo spread vale il 22% del range della
  barra da 4 ore: a questo timeframe il costo non lascia niente.

### JP225

Soglia di average trade in campione: 15% di 111.856 yen = **16.778 yen**.

- **23 equilibrate**, 21 delle quali **solo long**; la migliore per average trade (canale 20, stop
  60.000, target 570.000, uscita alle 21, long) fa **11.441 yen** a trade su 286 trade, il **68%**
  della soglia. **UngerFit massimo 0,83** (canale 20, stop 160.000, uscita alle 21, long). Nessuna
  passa le due soglie.
- Le medie per leva sulle 151 ammissibili: **long** fuori campione +3,43 milioni medio contro −0,21
  delle doppie; **uscita alle 21** +2,13 milioni contro +0,99; nessuna combinazione short e'
  ammissibile.
- **Segnale di regime** (punto 6 del metodo): fuori campione batte dentro su quasi tutte le
  equilibrate — average trade fuori fino al doppio di dentro — e le vincenti sono long. Sul feed il
  Nikkei e' salito anche nel campione, da 29.000 (gennaio 2022) a 39.400 (gennaio 2025), e poi fino a
  65.500 (settembre 2026): tutto il periodo e' un rialzo, e un canale long lo segue. E' il mercato, non
  il sistema, come per l'oro e NQ.

## Cosa significa e cosa non significa

Il rapporto **spread/range** ordina bene i no (argento, US2000, EU50) ma non spiega da solo i si':
UK100 e FRA40 costano meno di JP225 e non hanno niente. JP225 ha un comportamento che il canale
lungo coglie, ma dentro un rally, e con un margine per trade sotto soglia: e' il profilo della 002
del DAX prima della verifica lunga, non quello di una strategia.

## Conclusione

- **Nessuna classe** per le cinque celle.
- **JP225 e' l'unica cella con qualcosa da cercare**, nella regione canale 20, long, uscita alle 21,
  stop 60.000-160.000. Prima di una sweep serve la verifica che il regime non sia tutto il segnale:
  il feed FTMO parte a novembre 2020, e da marzo a dicembre 2021 il Nikkei e' rimasto fra 28.400 e
  29.600: dieci mesi laterali da usare come controllo **all'indietro** sulle stesse combinazioni. Se
  li' il canale long non tiene, il segnale e' il rialzo.
- Le altre quattro si chiudono a 4 ore. Se si vuole riprovare, a timeframe piu' alto: il range cresce
  e lo spread no.

## Riferimenti

- CSV `{uk100,fra40,jp225,us2000,xag}-4h-griglia-grossa.csv`; classi `{Uk100,Fra40,Jp225,Us2000,Xag}CoarseGridTests`.
- Contenitori `PT3B_{Z,FCE,NIY,RTY,SI}_PCH_001_240`.
- Skill `lettura-risultati` per le soglie.
