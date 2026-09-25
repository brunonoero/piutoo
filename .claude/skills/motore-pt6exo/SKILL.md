---
name: motore-pt6exo
description: >
  Scrivere un motore nuovo della serie PT6EXO di Piootoo (famiglie scorrelate dal catalogo): file e
  convenzioni, base EasyEngineBase, contenitore RC_{SIGLA}, test, sigla, matrice delle griglie, catalogo.
  Usala quando si chiede di "scrivere un motore", "aggiungere una famiglia", "un'idea nuova di strategia",
  o di portare in griglia un motore PT6EXO.
---

# Un motore PT6EXO

**Bozza del 25/09/2026**, dopo i primi diciotto motori. Il perche' della serie e il catalogo delle idee:
`docs/domini/catalogo-idee-pt6exo.md`. Modelli da copiare: `FailedBreakoutEngine` (livelli e stop
oltre l'estremo), `InternalBarStrengthEngine` (uscita a segnale `ExitOnly`), `HourOfDayEngine`
(orari in un fuso dichiarato, barra pianificata che deve esistere), `RandomEntryEngine` (caso senza
stato), `CrossMarketEngine` (altri simboli).

## La regola della serie

Una famiglia entra se guarda **qualcosa che il catalogo non guarda** — l'orologio, il calendario, la
forma della barra, il regime, un altro mercato — non una variante di un canale o di una banda. Il perche'
non si chiede; il metodo si' (griglia, fuori campione, costi veri, controllo RAN).

## I file

1. `Piootoo.Strategies/PT6EXOStrategies/Engines/<Nome>Engine.cs`: `public abstract class XxxEngine :
   EasyEngineBase` con `public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)`.
2. `Piootoo.Strategies/ResearchContainers/RC_<SIGLA>.cs`: stessa forma di `RC_HOD` —
   `ResearchContainerIdentity`, `IsResearchContainer => true`, `ResearchContainerSettings.Prepare(this)`
   e i default nel costruttore, `Initialize` → `ResearchContainerSettings.Apply`.
3. `Piootoo.Strategies.Tests/<Nome>EngineTests.cs`.
4. La sigla di tre lettere in `PtsNamingConventionTests.EngineCodes`, diversa da tutte quelle esistenti.
5. La riga nel catalogo (sezione della famiglia con "Scritto il…" e tabella in fondo).

## Le regole che i test fanno rispettare, e quelle che non vedono

- `AppliesSessionExit => SessionExitFromIntradayOnly` e `AppliesSessionExitDeclared => true`; ogni
  ingresso passa da `WithSessionExit` (puo' restituire null: allora `Hold`). `SessionExitHourTests`
  vieta di risolvere la fine sessione altrove.
- Mai `.Hour/.Minute/.DayOfWeek/.TimeOfDay` su un `DateTime` (`StrategyClockConformanceTests`): si passa
  da un `SessionClock` con il nome che finisce in `Clock` (`LocalClock.SessionDay(x).DayOfWeek`). Mai
  `DateTime.Now` ne' `DateTimeKind.Local` (`UtcOnlyConformanceTests`). Orari come `TimeOnly`; un intero
  solo come leva di griglia, convertito subito (`EntryHour` di HOD).
- Gate: `CurrentMP != 0` → nessun ingresso; `InDeclaredWindow(barTime) == false` → `Hold`. `Direction`
  0/1/2 dove ha senso.
- **Senza stato mutabile**: `StatelessEasyStrategyBase` clona i campi a ogni valutazione e li rimette in
  `RuntimeState`. Tutto si ricalcola dalla finestra; niente `System.Random`, niente `string.GetHashCode`.
- **Stessa decisione con qualunque lunghezza di storia**: backtest e live ricevono finestre diverse.
  Un indicatore ricorsivo (RSI) parte da un punto fisso all'indietro, non dall'inizio della finestra.
- Niente LINQ ne' riflessione nel percorso di valutazione.
- Una leva che il motore non ha **ferma** il contenitore (`ResearchContainerSettings`): le leve sono campi
  non readonly o proprieta' con setter, di tipo int/decimal/bool/enum/string.
- Configurazione incoerente → `ArgumentOutOfRangeException` con messaggio italiano.
- `RequiredCandles` copre tutto cio' che il motore legge, compreso `SessionsToCandles(AtrSessions + 2)`
  se usa `ClosedSessionAtrPoints`.
- Un orario programmato: l'ordine nasce sulla barra **prima**, e la barra pianificata deve esistere
  (`Grid.IsSessionDay` e `SessionMask.For(Symbol).Overlaps`), altrimenti l'ordine aprirebbe alla prima
  barra vera a un orario che nessuno ha dichiarato.

## I test

Classe di prova derivata con proprieta' che scrivono i campi protetti; `Evaluate` con
`StrategyEvaluationRequest` su serie sintetiche (`@NQ`: borsa Chicago, ricerca Roma, sessioni dom-ven,
finestra 17:00→16:00 Chicago). Coprire: segnale nei due lati, casi che non scattano, `Direction`,
posizione aperta, configurazione non valida, lettura delle leve del contenitore. Gli attesi si
calcolano a mano, con date UTC vere (inverno/estate, sabato senza sessione).

**Compilare fuori da `bin\Debug`** se uno studio in attesa usa quei binari:
`dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj -o Piootoo.Strategies.Tests\bin\verifica-<nome>`
(dentro il repository: i test cercano `PiootooApp.sln` risalendo dalla cartella di output).

## In griglia

Una riga in `CoarseGridMatrixTests.Engines`: contenitore, una leva strutturale con 3-4 valori, `Fixed`
per la variante, `Directions` se il motore non ha un "entrambi". `ResearchContainerSettingsTests` prova
da solo che il contenitore accetta le chiavi della griglia; una leva che e' una proprieta' di sola
scrittura va mappata nel suo `switch`. Poi le celle in `ricerca/coda.json` (`griglia-grossa`), prima
FDAX e NQ, dove i motori vecchi sono gia' misurati.
