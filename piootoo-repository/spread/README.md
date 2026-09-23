# Misure di spread

Una sottocartella per broker, con dentro i CSV prodotti da `PiootooSpreadDumpBot`:

```
spread/
  FTMO/
    FTMO_spread-by-symbol_20260801-20260901.csv   <- il backtest legge il piu' recente
    FTMO_spread-by-hour_20260801-20260901.csv     <- il suo gemello, per lo spread per ora UTC
    storico/                                      <- misure grezze e coppie sostituite
  ICS/
    ...
```

**Li scrive il server**, non si copiano a mano. Dalla 3.0.0 il bot manda la misura a
`POST api/spread/measurements` e il server:

- sceglie la cartella dal **conto** su cui il bot ha misurato, con il registro dei broker: e' lo
  stesso nome di `datafeed-external/` e `symbol-info/` (`FTMO`, `ICS`);
- **unisce** la misura a quella che c'era: i simboli misurati adesso sostituiscono le proprie righe
  in tutti e due i file, gli altri restano. Il caricatore legge un file solo, quindi una misura
  parziale scritta accanto toglierebbe ai run lo spread dei simboli non rimisurati;
- mette in `storico/` la misura grezza (`misura-...`) e la coppia sostituita (`sostituito-...`).

`BacktestingRequest.SpreadBroker` sceglie la cartella. Un broker senza cartella fa fallire l'avvio
di un run che lo chiede: vale la regola del datafeed mancante, mai proseguire in silenzio.

`FTMOPLATFORM/` e' il nome che il bot ricavava da `Account.BrokerName` prima del registro: la sua
misura e' stata copiata in `FTMO/` il 23/09/2026, e la cartella resta solo per i resoconti di
ricerca che la citano.

Non e' la stessa cosa di `datafeed-external/`: quello e' il feed di barre e tick, e mescolarci file
di misura farebbe sembrare dati di feed quello che non lo e'.

Regole complete in [`docs/domini/spread-e-costo-di-transazione.md`](../../docs/domini/spread-e-costo-di-transazione.md).
