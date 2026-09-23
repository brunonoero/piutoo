---
name: lettura-risultati
description: >
  Dare il verdetto su un risultato di ricerca Piootoo (griglia grossa, sweep, catalogo ai costi
  veri, backtest) con i criteri del metodo: ammissibilità, average trade contro soglia per simbolo,
  UngerFit, guadagno annuo su drawdown, confronto con il motore nudo, segnale di regime. Usala quando
  si chiede "cosa teniamo", "passa?", "ha senso?", o di leggere un CSV o un resoconto in ricerca/.
---

# Leggere un risultato

**Bozza del 23/09/2026.** Le soglie stanno **solo qui** e in `metodo/metodo-unger/SKILL.md` §8.2 e
§13.6; vanno riviste alla fine del percorso di messa a punto. Il verdetto lo scrive chi legge: gli
studi stampano numeri e non asseriscono.

## L'ordine delle domande

1. **È confrontabile?** Stesso feed (ICS ≠ vendor), stesso split, stessi costi, stesso orologio del
   riferimento. `backtest-summary.json` e i resoconti dichiarano tutto questo per un motivo.
2. **Quanti trade?** Sotto 250 in campione non si giudica (a 4 ore su 3 anni sono due a settimana).
   Fuori campione almeno 20 per finestra. Poche trade e nessuna perdita non sono qualità: sono un
   campione troppo piccolo.
3. **Ammissibile**: campione in utile con ≥ 250 trade. **Equilibrata**: netto/drawdown ≥ 1 dentro
   **e** fuori, ≥ 3 finestre su 4 in utile.
4. **Le soglie del metodo, dentro il campione** (fuori è la conferma, non la scoperta):
   - **average trade** ≥ max(6-7 tick, **15% del range medio della barra in dollari**);
   - **UngerFit** = √(E × 100 / R) ≥ 1, con E = average trade / soglia e R = drawdown peggiore /
     average trade (quante trade per recuperarlo);
   - **guadagno annuo / drawdown massimo ≥ 2** (il filtro di Titan). Finora nessuna cella lo passa:
     dirlo, non nasconderlo.
5. **Confronto con il motore nudo della stessa cella.** Se una finalista con i filtri fa quanto il
   nudo, i filtri non valgono. Se il nudo perde nella regione della finalista, tutto il margine sta
   nei filtri e va verificato sul periodo lungo (`Pt3b002LongPeriodTests` è il modello).
6. **Segnale di regime**: fuori campione batte dentro **ovunque**, per qualunque leva, e la
   volatilità è cresciuta nel frattempo. È il mercato che è cambiato (oro 2025-26, NQ 2021-26), non
   il sistema. Vale anche al contrario: un fuori campione che decresce finestra dopo finestra segue un
   rally che rallenta.
7. **Sconto walk-forward**: l'average trade in campione × 0,2-0,5 deve restare sopra soglia. La 002
   non passa questo filtro da sola: è una componente di paniere, non una strategia da conto.

## Soglie di average trade misurate (15% del range medio della barra, 1 contratto)

| cella | campione | validazione |
|---|---:|---:|
| FDAX 4h (2014-21 / 2021-26) | 266 | 365 |
| NQ 4h (2014-21) | 125 | |
| NQ 15m (2022-25) | 120 | |
| GC 4h (2014-21) | 100 | |
| CL 30m (2016-21) | 60 | |
| BP 60m (2014-21) | 40 | |

Una cella nuova la misura dal feed prima di leggere: la soglia non si eredita.

## Cosa dice il verdetto

Sempre tre cose, nell'ordine: **cosa regge** (con i numeri), **cosa non regge e perché**, **la
decisione operativa**: nessuna classe / classe accanto al contenitore / dove cercare dopo / cosa
rimisurare. Se qualcosa non si è potuto verificare, prima. Le leve che parlano (uscita alle 21,
short sul DAX e sull'oro, canale a una barra) si dicono con il conteggio che le sostiene, non come
impressioni.

## Dove non applicarla

- Non come filtro automatico: il verdetto lo scrive chi legge, dopo il punto 1.
- Non su numeri di un solo periodo corto (3 anni) per decidere una classe: la 002 è stata scelta su
  3 anni e verificata su 12 prima di fidarsene.
- Non sostituisce la rilettura in sessione fresca dei documenti che contano: quella va fatta senza
  aprire il codice, un'obiezione per punto.
