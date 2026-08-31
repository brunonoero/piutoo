# compare-0013 — NQ a 15 minuti, interno contro cBot

Analizzato il 2026-08-31. Report:
https://claude.ai/code/artifact/8f0d51b6-630a-4386-8a6a-fbb8f230bbfb

## Le due gambe

| | file | tipo | motore / prezzi | arco |
|---|---|---|---|---|
| INT | `trades-internal.json` | `interno-futures` (dedotto) | `PiootooTradingService` su `datafeed/@NQ_15` | tutto il 2024, 623 trade |
| EXT | `trades-external.json` | `cbot-cfd-ICS` (dedotto) | cBot su conto 1075035, USTEC | 02/07 → 31/12, 321 trade |

**I nomi non seguono la convenzione e la cartella non ha né `origin.json` né
`backtest-summary.json`**: broker, `holding` e `engineVersion` sono stati dedotti,
non letti. Il run interno non è quello di `compare-0012`: sulle NQ_15 in comune 259
trade su ~350 coincidono, 90 coppie escono diverse, saldo −8.945.

## Esito

Finestra comune **2024-07-02 → 2024-12-30**: INT 325 trade / 86.337 USD contro EXT
320 / 37.785 USD convertiti trade per trade (cambio implicito 0,893 → 0,966).
**Divario 48.552 USD, il 56%.** Non è deriva: il segno cambia a ottobre.

Scomposizione, coppie appaiate a tolleranza 2 ore:

| causa | USD | quota |
|---|---|---|
| 51 trade che prende solo il bot | −28.664 | 59% |
| protettiva → protettiva (slippage sugli stop) | +12.294 | 25% |
| coppie tagliate dal fine settimana | +5.752 | 12% |
| target → target | +1.738 | |
| trade solo interni (56) | +458 | |
| tempo → tempo | +44 | |

I 51 solo-EXT sono 36 stop (−52.955), 6 target (+19.114), 9 uscite a tempo (+5.178);
nessun doppione ravvicinato e **dieci entrano di domenica**, quando `@NQ` non ha barre
e USTEC sì — la stessa asimmetria @GC/XAUUSD. Dei 56 solo-INT, 41 hanno un solo-EXT di
stessa strategia e lato entro 24 ore.

## Aperto

- **Il fine settimana è disallineato e invalida una parte del confronto.** L'interno
  chiude 26 posizioni il venerdì alle 20:45 UTC (`WeekEnd`), l'esterno ne porta **24
  oltre il sabato** (17.080 USD, la più lunga quasi 6 giorni). Tempo con almeno una
  posizione aperta: INT 32%, EXT 54%. Le 12 coppie con lo scarto più grande sono tutte
  di questo tipo. Da allineare (`AccountHoldingPolicy` dal piano) prima di rimisurare.
- **Slippage sugli stop: quantificato, ~74 USD per uscita protettiva.** L'interno
  riempie *esattamente* al livello dichiarato su tutte e 12 le strategie (il modello di
  slittamento non era acceso), l'esterno da +0,3 a +0,9 punti in mediana ma media 4
  punti, su 167 coppie protettiva→protettiva fuori dal fine settimana. Su
  `compare-0007` erano 180 USD a stop: qui è un quarto del divario, non il grosso.

## Chiuso — non riaprire

- **"Le posizioni aperte bloccano gli ingressi successivi" non è una divergenza.**
  L'engine tiene una posizione sola per `(simbolo, strategia)` (`MakePositionKey`) e il
  cBot annulla l'intent con lo stesso criterio (`alreadyOpenOnStrategy`, per strategia
  *e* simbolo dall'11/08/2026). Sui 325 trade interni zero si sovrappongono, **per
  costruzione**: dai soli trade l'ipotesi non è misurabile, servirebbero i segnali
  interni e gli intent annullati del bot. Su NQ 15 comunque non morderebbe (durata
  mediana 4 ore da entrambi i lati, conteggi allineati su 12 strategie su 12); su GC
  pesava perché `TFU_001_30` ha `MaxBars = 460`, 9,6 giorni.
- **`MaxConcurrentTrades` non morde.** Esiste solo sul percorso di sessione, ma
  entrambi i lati arrivano comunque a 9-10 posizioni contemporanee.

## Cosa torna

Size 1:1 (20 unità × 1 USD/punto = 1 contratto × 20); stop e target realizzati identici
ai livelli dichiarati al decimo di punto; scarto di ingresso 4 minuti in mediana (p90
12); offset @NQ/USTEC −947 a luglio → −525 a dicembre.
