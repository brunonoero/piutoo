---
name: promuovi-finalista
description: >
  Trasformare una configurazione di ricerca Piootoo (finalista di sweep, cella di griglia, passo a
  mano) in una classe PT3B eseguibile: progressivo, etichetta della barra, parametri verbatim,
  riproduzione dei numeri, masterfilter, documentazione. Usala quando si chiede di "scrivere la
  classe", "promuovere la finalista", creare la 00N di una serie, o mettere una strategia in un piano.
---

# Promuovere una finalista a classe

**Bozza del 23/09/2026.** Il primo porting PT3B ha perso 219k per un'etichetta di barra sbagliata:
la procedura esiste per quello. Riferimenti: `docs/domini/ricerca-parametri.md` §"Promuovere una
finalista", `porting-da-report-sweep.md`, `PT3B_FDAX_PCH_002_240.cs` come modello.

## Prima: ha davvero passato?

Verdetto con `/lettura-risultati`: ammissibile, equilibrata, sopra le soglie del metodo dentro il
campione, batte il riferimento (regione nuda o finalista precedente) e regge sul periodo lungo. Se
manca una di queste, non si promuove: si scrive perché nel rapporto.

## La classe

1. **Nome e numero.** `PT3B_{SYM}_{ENG}_{nnn}_{tf}`, progressivo contiguo per (serie, simbolo,
   motore) **senza distinguere il timeframe**: il prossimo numero libero, che sia un contenitore o
   una finalista. `PtsNamingConventionTests` lo impone. Il numero è un progressivo, non un grado.
2. **Il contenitore resta.** Serve al giro dopo; la finalista è una classe **accanto**.
3. **Ciò che i parametri non coprono** e nessun resoconto stampa: `ResearchLabelsBarsOnOpen = true`
   (tutte le PT3B: la griglia e la sweep girano così), il fuso della finestra
   (`ZonedWindow.ResearchHours(start, end)`, orari **verbatim**), il tick, l'ancoraggio di sessione
   dal calendario. Sbagliare l'etichetta sposta la finestra di un timeframe e ribalta il segno.
4. **I parametri verbatim** dalla finalista, con il valore della ricerca nel commento a fianco di
   ogni riga: canale, offset, direzione, pattern (sentinelle comprese), finestra, `SkipDay`,
   `IntradayOnly`, `SessionExitTime` (`ExitHour` della ricerca, nell'orologio della ricerca), stop,
   target, `MaxBars`, trailing, breakeven. `IsResearchContainer` **non** si dichiara.
5. **Il commento XML** dice: da quale ricerca viene (file in `ricerca/`, cella, split, costi), i
   numeri dentro e fuori campione, il riferimento che batte e di quanto, le riserve. È l'unico posto
   dove chi legge il catalogo trova il perché.
6. **`Initialize`** legge tutte le chiavi, come il contenitore: la classe deve poter essere rimisurata.

## Dopo la classe, prima di tutto il resto

7. **Riprodurre i numeri.** `piootoo-sweep --strategy <Id> --params "Chiave=valore"` con un
   parametro neutro, stessi costi e split della ricerca: deve dare **gli stessi trade** della
   finalista. Se non li dà, la classe è sbagliata, non la ricerca. Solo dopo il backtest.
8. **Compilare e far girare la suite** (`dotnet test`, studi spenti): conformità di orologio,
   sessione, nomi, holding, contenitori.
9. **Masterfilter e piano**: la classe entra nel masterfilter del workspace e nel piano che la
   opererà, con la `Holding` del piano coerente con quella con cui è stata cercata (`--flat-utc`
   o overnight libero). Il server rifiuta i contenitori: se rifiuta la classe, è dichiarata
   contenitore per errore.
10. **Documentare**: voce in `docs/decisioni.md`, riga in `lavori-in-corso.md`, il rapporto in
    `ricerca/` che la cita. Commit in italiano con i numeri.
11. **Quello che manca ancora**: il backtest a tick in cTrader e una sessione `ExternalBroker`
    (`docs/domini/...`, `compare/compare-0048/esito.md` su cosa succede quando le modalità dati non
    coincidono). Finché non è fatto, la classe è validata dal motore interno e basta.

## Dove non applicarla

- A una configurazione con il campione a zero e il fuori campione in utile: è vento, non edge.
- Per "salvare" numeri di un periodo corto: prima il periodo lungo.
- Al posto del contenitore: il contenitore non si cancella e non si rinomina.
