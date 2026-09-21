# PT3B_FDAX_PCH_001_240 — specifica operativa

Price channel su **FDAX a 4 ore**, entrambe le direzioni, intraday. Prima strategia trovata
dall'ottimizzatore interno invece che da un dossier di ricerca.

## Da dove viene

| | |
|---|---|
| Ricerca | `piootoo-sweep`, sette fasi, feed **ICS** (CFD), spread FTMO per ora |
| Campione | 2014-07-17 → 2022-01-01 |
| Validazione | 2022-01-01 → 2026-09-17 (mai vista dalla ricerca) |
| Resoconto | `fdax-4h-ics-ftmo-per-ora.md` |

**Fuori campione**: 1.327 trade, **$153.781**, netto/drawdown 2,49, quattro finestre su quattro in
utile. In campione: 1.913 trade, $216.830.

**Con lo spread p90** invece della mediana — il costo che si paga una volta su dieci — il fuori
campione scende a $150.571, cioè il **2%**. Non vive dentro il costo di transazione.

## La regola

**Ordine STOP sul massimo e sul minimo della barra appena chiusa** (canale di Donchian a 1 barra,
quindi il canale *è* la barra di segnale).

- **LONG**: stop buy al **massimo** della barra da 4 ore appena chiusa
- **SHORT**: stop sell al **minimo** della stessa barra
- Nessun offset: il livello è l'estremo esatto
- L'ordine vale **solo per la barra successiva**. Se non viene toccato, scade e viene riemesso sulla
  barra dopo se le condizioni valgono ancora.
- Un solo ingresso per sessione **e per direzione**

## Quando può operare

- Opera sulle barre che **aprono** fra le **03:00 e le 18:00** ora italiana, estremi inclusi.
  Sulla griglia a 4 ore ancorata all'**01:00** sono i bucket che aprono alle **05:00, 09:00, 13:00 e
  17:00** — quattro su sei; restano fuori quello dell'01:00 e quello delle 21:00.
- Nessun giorno della settimana escluso.
- **Intraday**: la posizione viene chiusa alla fine della sessione, cioè all'01:00 del giorno dopo.

## I filtri

`d0` è la sessione in corso, `d1` la precedente, `d2` quella prima ancora. La sessione comincia
all'01:00 italiane.

| filtro | condizione | deve essere |
|---|---|---|
| neutrale 44 | `highD0 < lowD0 × 1,03` | **VERA** — la sessione in corso ha un'escursione sotto il 3% |
| neutrale 52 | `highD0 > highD1` **e** `lowD0 < lowD1` | **FALSA** — niente ingressi se la sessione ha già inglobato la precedente |
| direzionale −18, lato long | `closeD1 < closeD2 × 0,98` | **FALSA** — niente long dopo una sessione chiusa oltre il 2% sotto la precedente |
| direzionale −18, lato short | `closeD1 > closeD2 × 1,02` | **FALSA** — niente short dopo una sessione chiusa oltre il 2% sopra la precedente |

Gli ultimi due sono lo stesso parametro: il motore applica il segno al lato, quindi il filtro
impedisce di entrare nella direzione in cui il mercato si è **già** mosso di oltre il 2% il giorno
prima.

## Uscite

| | |
|---|---|
| Stop loss | **$5.000** per contratto = **200 punti** |
| Take profit | **$4.500** per contratto = **180 punti** |
| Uscita a tempo | **12 barre** da 4 ore (48 ore di barre, non di orologio) |
| Fine sessione | flat all'01:00 italiane |
| Trailing / break-even | nessuno |

Il punto FDAX vale $25, quindi i valori in denaro si convertono dividendo per 25.

## Attenzione, se il backtest si fa in cTrader

**La griglia a 4 ore di cTrader non è questa.** L'H4 della piattaforma è ancorato all'orologio del
broker, questa strategia alla sessione che comincia all'**01:00 italiane**. Le barre che ne escono
sono diverse, gli estremi del canale pure, e nessun errore lo segnala: i risultati semplicemente non
coincidono. Serve che il bot pieghi i bucket in codice a partire da una serie più fitta, come fanno
già i cBot Piootoo — è la regola in `CLAUDE.md`, «oltre l'ora la griglia la costruisce il codice, mai
la piattaforma».

**Usare Tick data (accurate).** Su dati a barre il simulatore di cTrader valuta lo stop anche contro
la barra d'ingresso, percorso pre-entrata incluso, e chiude a prezzi mai esistiti: su un port
precedente 201 trade su 359 uscivano nello stesso minuto del fill. È §5 del dossier v2.

**Il confronto va fatto sulle entrate**, timestamp e prezzo, non sul P&L: i P&L sono una conseguenza.
Su serie diverse — e il feed ICS che abbiamo usato è una serie, il tuo conto cTrader un'altra — un
port corretto mostra comunque uno scarto; quello che deve insospettire è uno scarto **sistematico**,
per esempio un'intera fascia oraria assente o tutte le entrate spostate della stessa quantità.

## Cosa resta da verificare

1. Backtest su tick in cTrader, confronto sulle entrate.
2. Sessione `ExternalBroker` col cBot: il feed ICS è a barre, e nessun backtest a barre sa cosa fa
   il broker dentro la barra dell'ingresso.
3. Se supera entrambi, la decisione se metterla in un piano è un'altra cosa ancora — riguarda la
   correlazione con quello che già opera, non la strategia in sé.
