# Sito di Piootoo: manuale d'uso e piani per broker

Sito statico, senza dipendenze: si apre con un doppio clic su `index.html`, oppure da un server
qualsiasi (`python -m http.server 8377` da questa cartella, poi `http://localhost:8377`).

| Pagina | Contenuto |
|---|---|
| `index.html` | Ingresso, con la situazione dei piani per broker. |
| `manuale.html` | Il manuale d'uso: console, procedure, sorveglianza, problemi. Si corregge a mano. |
| `piani.html` | Per ogni broker: piani in produzione e best plan, strategie, compatibilità fra conti. |

## Tenere aggiornata la pagina dei piani

```
python aggiorna-dati.py
```

Rigenera `data/piani.js` leggendo dal disco, senza server acceso:

- `accounts/brokers.json`, `accounts/accounts.json`: broker e conti;
- `broker-workspaces/{BROKER}/plans/plans.json`: piani in produzione e ritirati, con le strategie
  dichiarate;
- `best-plans/*/best-plan.json` e `artifacts/plan.json`: i best plan, uno per codice di piano (la
  scheda promossa per ultima), con strategie e numeri del run, size e tenuta.

Un best plan già promosso in produzione non fa riga a sé: è l'origine della riga di produzione.

Le **strategie in comune** fra due piani le trova lo script. A mano, in `data/compatibilita.json`,
si scrive solo ciò che nessun file dichiara:

- `gemelli`: le coppie di piani su cui sono stati misurati i trade gemelli, con `esito`
  (`ok`, `limite`, `no`), `quota` e `data`. I codici sono quelli di ricerca: copie, versioni a size
  diversa e piani di produzione con le stesse strategie prendono lo stesso esito da soli;
- `equivalenti`: un piano che eredita le misure di un altro pur avendo strategie diverse (il gemello
  Fintokei senza il caffè);
- `piani`: una riga di avvertenza per piano;
- `broker`: regole del conto, note e combinazioni su più conti.

Una coppia senza strategie in comune e senza misura compare come «non misurato», non come
compatibile. Lo script stampa gli avvisi (un codice di `compatibilita.json` che non è più fra i
piani promossi, un best plan senza `plan.json`) e la pagina li mostra in testa.

**Quando si rilancia**: a ogni piano promosso a best plan, messo in produzione, duplicato o
ritirato, e a ogni nuova misura di trade gemelli. La fonte dei numeri resta
`docs/registro-piani.md`; se pagina e file su disco non concordano, hanno ragione i file.

**Pubblicazione**: il sito servito è una copia in `C:\piootoo-web` (IIS, sito `piootoo-doc`, porta 81).
Dopo aver rigenerato i dati: `robocopy . C:\piootoo-web /MIR /XD __pycache__ .claude`, **prima con `/L`**
per vedere cosa sovrascrive: se in `C:\piootoo-web` c'è lavoro che il sorgente non ha, va riportato qui
prima di pubblicare. La procedura intera è la skill `aggiorna-sito` (`.claude/skills/aggiorna-sito`).

## File

- `assets/style.css`: stile, tema chiaro e scuro.
- `assets/logo.svg`: il logo, usato nella barra in alto, nella home e come icona della scheda.
- `assets/site.js`: barra in alto e tema; `assets/piani.js`: pagina dei piani;
  `assets/manuale.js`: indice e ricerca del manuale.
- `data/piani.js`: **generato**, non si modifica a mano. È un `.js` e non un `.json` perché da
  `file://` il browser blocca il caricamento di un JSON.
