---
name: deploy-piootoo
description: >
  Rilasciare Piootoo su questa macchina: versione (PiootooVersion, VersionPrefix, BotVersion),
  commit e push con il git di Visual Studio, server e console con tools/aggiorna-installazione.ps1,
  cBot con tools/aggiorna-cbot.ps1 e riavvio manuale delle istanze in cTrader. Usala quando si
  chiede di "fare il deploy", "pubblicare", "aggiornare il server/il bot", o "cambiare la versione".
---

# Rilascio

**Bozza del 23/09/2026**, verificata sui rilasci 7.4.3 → 7.6.0. Tre pezzi in quest'ordine: versione
e test, git, installazione (server e console, poi cBot).

## 0. La versione, prima del commit

Tre numeri che devono concordare, e `VersioneDelProgettoTests` lo verifica:
`PiootooVersion.Current`, `VersionPrefix` in `Directory.Build.props`, `BotVersion` nel cBot
`piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs`.

- **Patch** (7.6.0 → 7.6.1) se il contratto HTTP con il cBot non cambia: il bot resta alla sua
  versione e non si ricompila.
- **Minor** se cambia il contratto: un campo nuovo nel descriptor (`SessionFlatWindowMinutes`,
  7.6.0; `TradingWindow`, 7.5.0) lo è. Allora anche `BotVersion`, e il bot va ricompilato e
  riavviato.

Poi build della solution e suite verde (`dotnet test`, studi spenti: ~4-6 minuti). Non si
committa con test rossi.

## 1. Git

`git` non è nel PATH: si usa quello di Visual Studio, in una variabile
`$git = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"`
e `& $git -C C:\piootoo-dev ...`. Prima `git log --oneline -3` e `git status`: il tree può contenere
commit e file di altre sessioni (PDF didattici, `.log.err`) che non vanno presi. `git add` per
elenco di file, non `-A`. Messaggio in italiano, descrittivo, con i numeri che contano, scritto su
file nello scratchpad e passato con `-F` (una here-string nel comando viene bloccata). `git push
origin main`: stampa su stderr e il wrapper riporta un errore anche quando è andato; fa fede la riga
`a..b main -> main`.

## 2. Server e console installati

`powershell -NoProfile -ExecutionPolicy Bypass -File tools\aggiorna-installazione.ps1` (opzione
`-Target server|client`). Pubblica in temp, copia le DLL dell'applicazione in
`C:\piootoo\server\publish_run` e `C:\piootoo\client\win-x64`, non tocca appsettings, exe e
runtime; ferma e riavvia server e console se girano dall'installazione. Se avvisa che il
`deps.json` è cambiato serve la pubblicazione completa: due `dotnet publish -c Release -r win-x64
--self-contained true -o <publish>` e `robocopy <publish> <destinazione> /E /XF appsettings.json
appsettings.Development.json` (exit < 8 = ok), a server e console chiusi.

Se il log dello script mostra `fermo` ma non `sostituiti`, il server è rimasto giù: rilanciare lo
script e riavviarlo a mano.

**Verifica**: il server installato ascolta su **http://localhost:5000** (5142 è solo dev).
Avviarlo, `GET /api/strategies`, leggere `[Piootoo] Server vX.Y.Z` nella prima riga del log.

Dopo una modifica a `BarAggregator` gli aggregati di broker sopra il minuto sono cache da
ricostruire: `POST /api/datafeed-external/rebuild-from-minutes?broker=...`.

## 3. cBot

`powershell -NoProfile -ExecutionPolicy Bypass -File tools\aggiorna-cbot.ps1`: copia il sorgente dal
repository nel progetto cTrader, compila senza `config.json`, installa l'`.algo` (lascia `.bak`). Si
ferma se la copia in cTrader è più recente del repository: riportarla nel repository, oppure
`-Force` sapendo cosa si perde. Warning CS0618 e CGEN001 sono normali.

**Le istanze già avviate restano sulla versione vecchia**: vanno fermate e riavviate a mano in
cTrader, lo script non lo fa, e finché non lo si fa il server nuovo parla con un bot vecchio. Se il
contratto è cambiato, il bot vecchio viene rifiutato: è voluto.

## Dove non applicarla

- Con una sweep o una griglia in corso: la build fallisce a metà sulle DLL bloccate, e il run in
  corso può morire.
- Per "provare" una modifica: il rilascio è del codice committato e testato, non del tree.
