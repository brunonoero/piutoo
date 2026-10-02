---
name: aggiorna-sito
description: >
  Aggiornare il sito web di Piootoo (manuale d'uso e piani per broker, servito da C:\piootoo-web) dopo
  ogni cambio nei piani: un piano promosso a best plan, messo in produzione nel broker workspace,
  duplicato o ritirato, una nuova misura di trade gemelli. Usala SEMPRE a fine lavoro quando si
  "promuove", si "mette in produzione", si "ritira" o si "duplica" un piano, anche se l'utente non
  nomina il sito, e quando chiede di "aggiornare il sito" o "la pagina dei piani".
---

# Aggiornare il sito dopo un cambio nei piani

**Bozza del 02/10/2026**, scritta sulla messa in produzione dei quattro piani PT8DAV. Regola
dell'utente: quando strategie o piani vengono promossi o vanno in produzione, il sito si aggiorna
nello stesso giro di lavoro, non dopo.

## Dove sta cosa

| | |
|---|---|
| Sorgente (in git) | `piootoo-repository\web\` — `LEGGIMI.md` dice come è fatto |
| Sito servito | `C:\piootoo-web`, sito IIS `piootoo-doc`, **http://localhost:81**. È una copia e non è un repository. Chi ci lavora direttamente (capita) riporta le modifiche nel sorgente: vedi il passo 5 |
| Dati generati | `web\data\piani.js`, da `aggiorna-dati.py`. Mai a mano |
| Dati scritti a mano | `web\data\compatibilita.json`: trade gemelli, avvertenze per piano, note e combinazioni per broker |
| Fonte dei numeri | `docs\registro-piani.md`. Se sito e file su disco non concordano, hanno ragione i file |

## Quando

- un piano promosso a **best plan** (`POST api/BestPlans`);
- un piano messo in **produzione** nel broker workspace, **duplicato** su un altro conto o **ritirato**;
- una nuova misura di **trade gemelli** fra due piani;
- una schermata della console che cambia: allora si corregge `manuale.html`, a mano.

## Procedura

1. **Registro prima del sito.** `docs\registro-piani.md` porta il piano toccato: codice di ricerca e di
   produzione, broker e conto, size, da quale run. Il sito ne è il colpo d'occhio, non la fonte.
2. **`compatibilita.json`**, solo ciò che nessun file dichiara:
   - `gemelli`: una riga per coppia misurata, con i codici **di ricerca** (il piano di produzione eredita
     da solo le misure del piano di origine), `esito` `ok` / `limite` / `no`, `quota`, `data`;
   - `piani`: una riga di avvertenza per ogni piano nuovo;
   - `broker.{BROKER}.note` e `combinazioni`: **rileggerle tutte**, perché invecchiano in silenzio. Una
     nota che dice «il passaggio in produzione è ancora da fare» o «nessun piano è in produzione» non la
     corregge nessuno script, e il conteggio dei piani sul conto demo nemmeno.
3. **Rigenerare**, dalla cartella del sito e senza server acceso:

   ```powershell
   cd C:\piootoo-dev\piootoo-repository\web; python aggiorna-dati.py
   ```

   Stampa una riga per broker (`FTMO: 5 produzione, 5 best-plan; 45 coppie, 0 non misurate`) e gli
   **avvisi**: un codice di `compatibilita.json` che non è più fra i piani promossi, un best plan senza
   `plan.json`. Gli avvisi si leggono e si risolvono, non si lasciano in testa alla pagina. Le coppie
   «non misurate» sono un lavoro da fare (`ricerca\pt8dav-piani\gemelli.py` è il modello) o da dichiarare
   all'utente: una coppia non misurata non è compatibile.
4. **Controllare i conteggi** contro quello che si è appena fatto: se i piani messi in produzione sono
   due, «produzione» sale di due e «best-plan» scende di due (un best plan promosso non fa più riga a
   sé). Se non torna, il server ha scritto altrove o la promozione non è avvenuta: `GET
   api/v1/broker-workspaces/{BROKER}`.
5. **Pubblicare** la copia servita, **in due tempi**. Prima la prova, che non scrive nulla:

   ```powershell
   robocopy C:\piootoo-dev\piootoo-repository\web C:\piootoo-web /MIR /XD __pycache__ .claude /L /NJH /NP /NDL
   ```

   L'elenco deve contenere **solo i file toccati in questo giro** (di norma `data\compatibilita.json` e
   `data\piani.js`). Qualsiasi altra riga — un file `Newer`/`Older` che non si è toccato, un `*EXTRA File`
   — vuol dire che qualcuno ha lavorato direttamente in `C:\piootoo-web`: l'utente ci apre sessioni
   (lo stile e il logo del 02/10/2026 sono nati lì). Allora **non si pubblica sopra**: prima si porta
   quel lavoro nel sorgente (copia del file da `C:\piootoo-web` a `piootoo-repository\web`, o unione a
   mano se il file è cambiato da entrambe le parti), si ripete la prova, e solo quando l'elenco è
   pulito si lancia lo stesso comando senza `/L`.

   Exit sotto 8 = riuscito (1 = file copiati; il wrapper lo riporta come errore, fa fede il numero).
   `/MIR` **cancella** da `C:\piootoo-web` ciò che non sta nel sorgente e sovrascrive senza chiedere:
   il 02/10/2026, lanciato senza prova, ha tolto il logo e lo stile che un'altra sessione aveva scritto
   tre minuti prima (recuperati dal suo transcript). `.claude` è escluso perché è la configurazione
   delle sessioni aperte in quella cartella, non parte del sito.
6. **Verificare quello che IIS serve**, non il file: `Invoke-WebRequest
   http://localhost:81/data/piani.js -UseBasicParsing` e cercarci il codice del piano nuovo.
7. **Commit dei soli propri file** (`git` di Visual Studio, percorsi espliciti): `docs/registro-piani.md`,
   `piootoo-repository/web/data/compatibilita.json`, `piootoo-repository/web/data/piani.js`, e
   `manuale.html` se toccato. `broker-workspaces/` e `best-plans/` non sono in git. Il lavoro riportato
   da `C:\piootoo-web` va in un commit a parte, che dice di chi è.

## A fine lavoro, da dire all'utente

- I conteggi per broker stampati dallo script e gli avvisi rimasti.
- Che un piano in produzione **non esegue** finché un'istanza cTrader non parte con il suo codice di
  produzione sul conto: lo script non lo fa.
- Le incompatibilità del piano nuovo con quelli già sul conto o sulla stessa prop.

## Dove non applicarla

- Per un piano di ricerca creato o modificato senza promozione: basta il registro, il sito mostra solo
  best plan e produzione.
- Per correggere un numero sul sito: si corregge la fonte (registro, `compatibilita.json`, o il file su
  disco che lo script legge) e si rigenera.
