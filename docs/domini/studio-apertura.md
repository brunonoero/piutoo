# Studio dell'apertura (ORB e bandiera in finestra)

Nato il 30/09/2026 per mettere alla prova un'idea diffusa del day trading: i pattern valgono di piu'
all'apertura della borsa, dove entrano volumi e liquidita', e poco nelle ore morte. Nel catalogo nessun
motore la misurava: il breakout di sessione (SBO) prende i livelli dalla giornata della ricerca, che
comincia a mezzanotte o all'01:00 di Roma, e la bandiera (FLG) gira su tutta la giornata a 60 e 240
minuti, dove i primi trenta minuti dell'apertura non si vedono. **E' un'ipotesi da misurare, non una
regola**: si tiene solo cio' che passa griglia, fuori campione e costi veri (`lettura-risultati`).

**Tutto lo studio sta in una cartella**, `Piootoo.Strategies/OpeningStudy/` (test in
`Piootoo.Strategies.Tests/OpeningStudy/`), per toglierlo in un colpo se non porta a nulla. Fuori da li'
ci sono solo le righe di registrazione, marcate «Studio dell'apertura»: le sei righe della matrice in
`CoarseGridMatrixTests` con i due helper `OpeningFixed`/`FlagWindowFixed`, la sigla `ORB` in
`PtsNamingConventionTests`, le dodici celle in `ricerca/coda.json` e questo file.

## ORB — opening range dell'apertura

`OpeningRangeEngine`, contenitore `RC_ORB`. Il range sono le barre che aprono in
[apertura, apertura + `RangeMinutes`) nel giorno locale; devono esserci tutte, altrimenti il giorno non
opera. Solo sotto l'ora, range multiplo del timeframe, apertura sull'apertura di una barra.

- **L'apertura e' in ora di borsa** (`OpenHhmm`, `OpenClock` = borsa): 09:00 di Berlino per FDAX, 08:30
  di Chicago — le 09:30 di New York — per NQ. Mai Roma per un mercato americano: nelle settimane in cui
  le ore legali non sono allineate New York apre alle 14:30 di Roma, non alle 15:30.
- **Breakout** (`Mode` 0): buy stop sul massimo, sell stop sul minimo (piu' `OffsetTicks`), riemessi
  finche' dura `EntryWindowMinutes` dopo il range. Un lato gia' toccato dopo il range e' speso: una
  rottura per lato al giorno, ricavata dalle barre e non da uno stato.
- **Fade** (`Mode` 1), la «caccia alla liquidita'»: dopo il range una barra buca un estremo e chiude
  di nuovo dentro (o chiude dentro dopo una chiusura fuori); si entra contro a mercato sulla barra dopo.
- `StructureStop`: stop sul lato opposto del range (breakout) o oltre l'estremo toccato (fade);
  `TargetRange`: target in altezze del range; `MaxRangeAtr`: salta i giorni con range oltre k ATR di
  sessione. In matrice sono spenti: stop e target sono quelli della matrice, come per FLG.

## FLG in finestra — la bandiera solo dopo l'apertura

`RC_FLGW`: la bandiera di `RC_FLG` con una finestra **in ora di borsa, al minuto**
(`WindowStartHhmm`/`WindowEndHhmm`), sulla barra di segnale ed estremi inclusi. Non basta
`StartHour`/`EndHour` della griglia: sono ore piene di Roma, le 09:30 di New York non si scrivono. La
finestra si applica dopo le altre leve, perche' la griglia passa sempre `StartHour`/`EndHour` a -1; le
due finestre insieme sono un errore. **Il controllo e' la riga FLG sulla stessa cella**: la finestra
vale solo se batte la bandiera a tutta giornata.

## In coda

Sul feed del vendor (in campione 2008-2021, fuori 2022-2025, costi FTMO), subito dopo le celle FLG:

| riga | cosa | celle |
|---|---|---|
| `ORBEU` / `ORFEU` | breakout / fade, apertura Xetra 09:00, finestra 120 / 60 minuti | FDAX 15 |
| `ORBUS` / `ORFUS` | lo stesso, apertura New York | NQ 15 |
| `FLGEU` + `FLG` | bandiera 09:00-11:00 Berlino e il suo controllo | FDAX 15 e 30 |
| `FLGUS` + `FLG` | bandiera 08:30-10:30 Chicago e il suo controllo | NQ 15 e 30 |

Leva dell'ORB: `RangeMinutes` 15/30/60. Una riga `…EU` su NQ misurerebbe le 09:00 di Chicago: non si
lancia. `@FDAX_15` e `@FDAX_30` sono stati piegati il 30/09/2026 da `@FDAX_1` (il CSV del vendor non e'
piu' sul disco): la stessa piegatura rifatta a 60 minuti riproduce `@FDAX_60` barra per barra.

Non ancora misurato, e fuori da questo primo giro: la pausa di mezzogiorno come filtro e l'inversione
dopo la chiusura europea (17:30-19:00 di Roma).

## Riferimenti codice

- `Piootoo.Strategies/OpeningStudy/Engines/OpeningRangeEngine.cs`, `OpeningStudy/RC_ORB.cs`,
  `OpeningStudy/RC_FLGW.cs` (namespace dei contenitori `Piootoo.Strategies.ResearchContainers`, dove
  matrice e test li cercano)
- `Piootoo.Strategies.Tests/OpeningStudy/OpeningRangeEngineTests.cs`
- `Piootoo.Strategies.Tests/CoarseGridMatrixTests.cs` (righe `ORB*`, `ORF*`, `FLGEU`, `FLGUS`)
- `docs/domini/catalogo-pattern-classici.md` §FLG, `catalogo-idee-pt6exo.md` §HOD (orari in un fuso dichiarato)
