# Correlazione fra motori su @NQ — misura del 22/09/2026

**Domanda:** sullo stesso simbolo, due strategie di motori diversi perdono negli stessi giorni?
E due dello stesso motore?

**Come:** tutte le 39 strategie `@NQ` del catalogo (PT2 e PTS), feed del vendor, orologio al
minuto, 2022-01-01 → 2026-09-01, commissione $4 per lato, overnight e fine settimana permessi.
P&L giornaliero = somma dei netti dei trade **chiusi** nel giorno UTC, zero nei giorni senza
chiusure. Correlazione di Pearson fra le serie. Entrano nella matrice le 37 con almeno 40 giorni
di chiusure. Matrice completa in `nq-correlazione-motori.csv`; generata da
`NqEngineCorrelationTests`.

## Il risultato

Correlazione media per coppia di famiglie (diagonale = dentro la famiglia):

| | PCH | RBM | SBO | TFM | TFU | VBO |
|---|---:|---:|---:|---:|---:|---:|
| **PCH** | 0,19 | 0,00 | 0,03 | 0,04 | 0,07 | 0,08 |
| **RBM** | 0,00 | n/d | 0,03 | 0,02 | 0,05 | 0,08 |
| **SBO** | 0,03 | 0,03 | 0,02 | 0,04 | 0,03 | 0,01 |
| **TFM** | 0,04 | 0,02 | 0,04 | 0,08 | 0,06 | 0,02 |
| **TFU** | 0,07 | 0,05 | 0,03 | 0,06 | 0,07 | 0,04 |
| **VBO** | 0,08 | 0,08 | 0,01 | 0,02 | 0,04 | −0,00 |

Media su tutte le 666 coppie: **0,056**. Massima 0,857, minima −0,078.

Le uniche coppie sopra 0,45 sono **gemelle**: stesso motore, stesso timeframe o adiacente, parametri
vicini —

| r | coppia |
|---:|---|
| 0,86 | `PTS_NQ_PCH_001_15` ~ `PTS_NQ_PCH_002_15` |
| 0,51 | `PTS_NQ_PCH_003_30` ~ `PTS_NQ_PCH_008_240` |
| 0,50 | `PTS_NQ_TFM_001_60` ~ `PTS_NQ_TFM_009_60` |
| 0,49 | `PTS_NQ_PCH_003_30` ~ `PTS_NQ_PCH_004_30` |
| 0,47 | `PTS_NQ_SBO_002_15` ~ `PTS_NQ_TFM_005_15` |

## Cosa dice, e cosa corregge

**La regola del pollice della sera prima era sbagliata.** Si era detto che due motori trend
following sullo stesso simbolo dovessero stare a 0,5-0,8, perché «entrano nella direzione dello
stesso movimento». Misurato: TFM contro TFU 0,06, TFM contro PCH 0,04, dentro TFM 0,08. Sullo stesso
NQ, con timeframe e parametri diversi, le strategie **non** perdono negli stessi giorni — tranne
quando sono quasi la stessa strategia. La famiglia del motore conta molto meno del timeframe e dei
filtri, e il reversal (RBM) non è più decorrelato dei trend fra loro: è tutto vicino a zero.

**Conseguenza per il paniere:** la diversificazione dentro un simbolo esiste ed è grande, purché
si evitino le gemelle. Su NQ, 37 strategie con correlazione media 0,056 si comportano quasi come
37 scommesse indipendenti sul piano dei giorni — è la condizione in cui il drawdown del paniere
scende molto sotto quello della singola. Prima di comporre, però, le coppie sopra 0,45 vanno
ridotte a una: tenere `PCH_001_15` e `PCH_002_15` insieme raddoppia l'esposizione senza comprare
nulla.

**Conseguenza per la scaletta dei motori:** portare motori nuovi in sweep vale, ma non per la
ragione detta ieri (la decorrelazione di famiglia): vale perché sono *altre* strategie, e ogni
strategia non gemella aggiunge diversificazione più o meno allo stesso modo. L'ordine fra motori
diventa una questione di quali hanno più classi di partenza e griglie già pronte, non di quale
famiglia «decorrela di più».

## I limiti della misura, dichiarati

- **Serie sparse.** Molte strategie chiudono 100-250 giorni su ~1.200: la serie ha molti zeri, e
  la correlazione fra due serie sparse è schiacciata verso zero per costruzione. La misura dice
  «non perdono negli stessi giorni», che è la domanda del drawdown; **non** dice quanto stanno in
  posizione insieme. Una correlazione sulle *posizioni aperte* (esposizione giornaliera) sarebbe
  più alta ed è un'altra misura, da fare se si vuole dimensionare il rischio lordo.
- **Un simbolo, un feed, un periodo.** 2022-2026 su NQ del vendor. Non è detto che valga su FDAX
  o su un mercato in tendenza lunga, dove tutti i trend sono dalla stessa parte per mesi.
- **Le PTS sono state cercate sull'orologio veloce** (vedi `decisioni.md` 21/09): i loro netti qui
  non sono un giudizio sulla loro bontà, solo il materiale con cui misurare la correlazione.

## Riferimenti

`Piootoo.Strategies.Tests/NqEngineCorrelationTests.cs`, `nq-correlazione-motori.csv`,
`docs/lavori-in-corso.md` (sera del 21/09, «il paniere è l'unità di misura»).
