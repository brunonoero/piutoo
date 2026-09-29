# Catalogo delle figure armoniche (PTARM)

La serie `PTARM_*` raccoglie le **figure armoniche** — Gartley, Bat, Butterfly, Crab — tradotte in regole.
Nasce il 29/09/2026, accanto alle figure classiche di `catalogo-pattern-classici.md`, con lo stesso metodo:
griglia grossa in campione, fuori campione, costi veri, controllo RAN, correlazione con il catalogo. Le
soglie stanno in `lettura-risultati`.

## La struttura comune

Cinque punti, **X-A-B-C-D**, quattro gambe alternate. Le figure differiscono solo per i rapporti di
Fibonacci fra le gambe (valori da manuale, Carney), scritti per la figura rialzista:

| sigla | figura | B ritraccia XA | C ritraccia AB | CD su BC | D su XA |
|---|---|---|---|---|---|
| GAR | Gartley | 0,618 | 0,382–0,886 | 1,27–1,618 | 0,786 |
| BAT | Bat | 0,382–0,50 | 0,382–0,886 | 1,618–2,618 | 0,886 |
| BUT | Butterfly | 0,786 | 0,382–0,886 | 1,618–2,24 | 1,27 (oltre X) |
| CRB | Crab | 0,382–0,618 | 0,382–0,886 | 2,24–3,618 | 1,618 (oltre X) |

**Scritto il 29/09/2026**: base `HarmonicEngine` e le quattro figure in
`PTARMStrategies/Engines/HarmonicEngines.cs`, contenitori `RC_GAR`, `RC_BAT`, `RC_BUT`, `RC_CRB` in
`ResearchContainers/RC_Harmonic.cs`. Scelte fatte scrivendolo:

- **I rapporti non sono leve.** Stanno nella classe della figura: sono fissati a priori, e la griglia varia
  solo la scala degli swing. E' il vantaggio di metodo della famiglia rispetto alla bandiera.
- **Swing a pivot di k barre** (`PivotBars`), non ZigZag: il pivot e' locale, lo ZigZag dipende da dove
  comincia la finestra. Si usano solo pivot confermati (k barre dopo). Una barra pivot alto e basso insieme
  non conta; due pivot consecutivi dello stesso tipo sono lo stesso swing e vale il piu' estremo.
- **X chiuso.** La scansione va all'indietro dalla barra piu' recente e vuole, oltre a C, B, A e X, lo swing
  opposto prima di X: senza, X potrebbe essere l'inizio troncato di uno swing piu' lungo e backtest e live
  vedrebbero figure diverse. Finestra di ricerca `LookbackBars`.
- **La figura in corso**: C e' l'ultimo swing confermato, dopo C nessuna barra lo supera, i rapporti stanno
  negli intervalli allargati di `Tolerance` (basso × (1 − t), alto × (1 + t)), CD/BC si misura con la D
  calcolata. Da C al massimo `MaxDBars` barre.
- **D toccata = figura consumata.** Se dopo C una barra ha gia' raggiunto D, niente ordine: o era gia'
  stato riempito, o il prezzo e' arrivato prima che C fosse confermato, e inseguirlo a posteriori sarebbe
  proprio il ridisegno che gonfia gli indicatori commerciali.
- **Ordine limit su D**, arrotondato al tick verso l'interno della figura, valido una barra e riemesso
  finche' la figura regge. **Livello superato a mercato** (`CrossedLevelPolicy.Market`), deciso per la serie
  il 29/09/2026.
- **Uscite**: quelle comuni, oppure `StopStructure` (oltre il piu' esterno fra X e D di `StopBufferAtr` ATR
  delle ultime `AtrBars` barre) e `TargetAd` (frazione della gamba AD dal livello, 0,382 e 0,618 da
  manuale). Su Butterfly e Crab D sta oltre X: senza margine lo stop strutturale cadrebbe sull'ingresso e
  resta quello comune.
- **Il controllo `RatioScale`**: moltiplica tutti i rapporti. A 1 la figura da manuale; a 0,9 la stessa
  struttura con rapporti falsati. Se la falsata guadagna quanto la vera, il merito non e' di Fibonacci ma
  del comprare dopo un ritracciamento, e la famiglia e' un parente di LevelFader, RBB e FBO.

Leve: `PivotBars`, `Tolerance`, `MaxDBars`, `LookbackBars`, `RatioScale`, `StopStructure`, `StopBufferAtr`,
`AtrBars`, `TargetAd`, `Direction`. Test in `HarmonicEngineTests`.

*Trappole:* figure rare, soprattutto il Crab — sul campione breve FTMO non arrivano all'ammissibilita', per
questo la prima griglia e' sul vendor. Sono figure di **inversione**: il rischio di correlazione e' con i
reversal del catalogo, non con i breakout.

## In matrice

`CoarseGridMatrixTests`, motori `GAR`, `BAT`, `BUT`, `CRB`: leva `PivotBars` 2 / 3 / 5 / 8, tolleranza 5%,
D entro 30 barre, finestra 300, stop e target della matrice. Le righe `GARF`, `BATF`, `BUTF`, `CRBF` sono
il controllo a rapporti falsati (scala 0,9) e si lanciano solo su una cella che risponde, sulla stessa
cella e con lo stesso split. In coda sul feed lungo del vendor (in campione 2008-2021, fuori 2022-2025),
FDAX e NQ a 60 minuti: `matrice-{sim}-60-{figura}-vendor`. Il 240 si apre se il 60 risponde: a quattro ore
le figure sono troppo poche.

| sigla | figura | stato |
|---|---|---|
| GAR | Gartley | `GartleyEngine`, `RC_GAR`; due celle vendor in coda |
| BAT | Bat | `BatEngine`, `RC_BAT`; due celle vendor in coda |
| BUT | Butterfly | `ButterflyEngine`, `RC_BUT`; due celle vendor in coda |
| CRB | Crab | `CrabEngine`, `RC_CRB`; due celle vendor in coda |

## Riferimenti codice

- `Piootoo.Strategies/PTARMStrategies/Engines/HarmonicEngines.cs` (`HarmonicRatios`, `HarmonicEngine`, le quattro figure)
- `Piootoo.Strategies/ResearchContainers/RC_Harmonic.cs`
- `Piootoo.Strategies.Tests/HarmonicEngineTests.cs`; serie e sigle in `PtsNamingConventionTests`
- `Piootoo.Strategies.Tests/CoarseGridMatrixTests.cs` (`HarmonicFixed`), `piootoo-repository/ricerca/coda.json`
- `docs/domini/catalogo-pattern-classici.md` (figure classiche), `catalogo-idee-pt6exo.md` (RAN, dai motori ai piani)
