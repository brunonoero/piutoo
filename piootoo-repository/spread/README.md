# Misure di spread

Una sottocartella per broker, con dentro i CSV prodotti da `PiootooSpreadDumpBot`:

```
spread/
  FTMO/
    FTMO_spread-by-symbol_20260801-20260831.csv   <- questo lo legge il backtest
    FTMO_spread-by-hour_20260801-20260831.csv     <- questo si legge a mano
  RAWTRADINGLTD/
    ...
```

Il bot scrive nella propria cartella di output (`%AppData%\PiootooSpreadDump` di default): i
file si copiano qui a mano. `BacktestingRequest.SpreadBroker` sceglie la cartella, e il
caricatore prende il `*spread-by-symbol*.csv` **più recente** — il nome porta la finestra di
misura, quindi non è fisso.

Non è la stessa cosa di `datafeed-external/`: quello è il feed di barre e tick che scrive il
server, e mescolarci file di misura farebbe sembrare dati di feed quello che non lo è.

Un broker senza cartella fa fallire l'avvio di un run che lo chiede: vale la regola del
datafeed mancante, mai proseguire in silenzio.

Regole complete in [`docs/domini/spread-e-costo-di-transazione.md`](../../docs/domini/spread-e-costo-di-transazione.md).
