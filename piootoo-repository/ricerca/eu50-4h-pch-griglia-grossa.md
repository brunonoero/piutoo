# EU50 4h, Price Channel nudo: griglia grossa — nessun edge

Cella **@FESX (EU50.cash) × 4 ore × Price Channel**, leve: canale 1/20/50, 5 stop, 5 target, uscita
libera o alle 21, direzione entrambe/long/short. 450 combinazioni.

- **Feed e costi FTMO**, il broker su cui si opererebbe: spread 1,46 punti (finestra mobile di 30
  giorni misurata dal raccoglitore), swap long 1,13 / short 0,01 punti a notte (tasso base, riga a
  mano: la scheda del 23/09 portava l'aggiustamento per il dividendo), commissione zero.
- **Periodo** 2022-01 → 2026-09, split 2025-01: il feed FTMO parte a novembre 2020, quindi lo split
  corto. Non si somma alla cella FDAX, che e' su ICS e sullo split lungo.
- 14 minuti su 8 core, 24/09/2026. CSV `eu50-4h-griglia-grossa.csv`, log `eu50-4h-griglia-grossa.log`,
  classe `Eu50CoarseGridTests`, contenitore `PT3B_FESX_PCH_001_240`.

## Il risultato

- **Ammissibili 9 su 450** (≥ 250 trade e in utile nel campione), tutte appena sopra lo zero: la
  migliore fa +2.207 euro in tre anni su 332 trade, **6,6 euro a trade**. La soglia del metodo — 15%
  del range medio della barra, 24,6 punti × 10 euro — e' **37 euro**: nessuna ci si avvicina.
- **Fuori campione tutte e 9 in perdita**, da −1.772 a −22.331. **Equilibrate: 0.**
- Le ammissibili sono tutte a uscita alle 21 e a doppia direzione; 6 su 9 hanno il canale a una barra,
  che fuori campione perde in media −20.653.

## Cosa significa e cosa non significa

Il numero che lo spiega e' **lo spread in rapporto al range della barra**: su EU50 vale il **5,9%**,
contro l'1,9% del DAX. A parita' di motore il costo pesa tre volte tanto, e il poco margine lordo che
il canale trova se ne va li'. Non dice che l'Euro Stoxx non abbia un comportamento sfruttabile: dice
che sul CFD FTMO, a 4 ore, il Price Channel nudo non lo paga.

Non dice nulla nemmeno sull'edge del DAX, che resta: EU50 era il test di robustezza piu' vicino, e un
no qui sposta la domanda sul costo, non sul motore.

## Conclusione

**Nessuna classe.** Il contenitore resta per un eventuale giro a timeframe piu' alto, dove il range
cresce e lo spread no. In coda, nell'ordine del rapporto spread/range: UK100 (2,4%), FRA40 (3,3%),
JP225 (4,5%), US2000 (6,5%), argento (22,2%).

## Riferimenti

- `Piootoo.Strategies.Tests/Eu50CoarseGridTests.cs`, `Piootoo.Strategies/PT3BStrategies/PT3B_FESX_PCH_001_240.cs`
- `piootoo-repository/ricerca/coda.json`, `tools/coda-ricerca.ps1`
- Skill `lettura-risultati` per le soglie.
