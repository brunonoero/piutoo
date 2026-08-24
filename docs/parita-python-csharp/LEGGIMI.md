# parita.py — confronto fra un backtest Piootoo (C#) e il run Python di riferimento

## Cosa confronta, e perché solo quello

I run Python girano su contratti continui **back-adjusted**: NQ vale 4.588 nel 2012 e il
datafeed Piootoo ne dà 18.000 sullo stesso strumento. Non è un errore di nessuno dei due —
è il retro-aggiustamento dei rollover — ma rende inutile qualunque confronto sui livelli, e
con esso su stop e target espressi in punti.

Restano confrontabili, ed è ciò che lo script guarda:

| | |
|---|---|
| **timestamp di ingresso** | quanti ingressi in comune, quanti solo da un lato |
| **direzione** | long/short sullo stesso ingresso |
| **P&L in dollari** | invariante al back-adjustment |
| **causa di uscita** | SL / TP / MAXBARS / SL_SAMEBAR / ... |
| **prima divergenza** | il punto da cui i due sistemi smettono di concordare |

Una divergenza che parte da un punto preciso e poi non recupera più è un problema di stato
(una posizione aperta da uno e non dall'altro). Una divergenza diffusa e simmetrica è un
problema di dati o di condizione d'ingresso.

## Uso

```bash
# confronto vero
python3 parita.py --python piootoo-repository/run-engine \
                  --csharp  <workspace>/backtests/<cartella>/trades.json

# più file di trade insieme
python3 parita.py --python piootoo-repository/run-engine \
                  --csharp run1/trades.json run2/trades.jsonl

# autotest: due run Python fra loro (attesa: corrispondenza totale)
python3 parita.py --python run-x/consegna --python2 run-06-gosto/consegna
```

`--python` accetta sia una singola `consegna/` sia una radice da esplorare
ricorsivamente: puntalo a `run-engine/` e trova da solo tutti i `parametri.csv`.
Serve, perché un backtest solo attinge a famiglie che stanno in run diversi.

Opzioni:

- `--offset auto|<minuti>` — minuti da sottrarre ai timestamp C# prima di accoppiare.
  Con `auto` cerca il valore che massimizza le corrispondenze. Serve: secondo
  `docs/domini/porting-da-report-sweep.md` il massimo oggi si trova a **−15 minuti**
  (345 corrispondenze contro 78 a offset nullo), e nessuno sa ancora perché.
- `--tolleranza <minuti>` — tolleranza sull'accoppiamento, default 1.

## Abbinamento automatico

`PTS_*` → famiglia Python per `(symbol, timeframe, motore, stop_loss_pt, take_profit_pt)`,
letti da `parametri.csv` e dai trade C#. Le strategie non abbinate vengono elencate con il
motivo; quelle ambigue (più famiglie con gli stessi parametri, tipico quando lo stesso run
è stato consegnato due volte — `run-06-gosto` e `run-x` sono identici) prendono la prima e
lo segnalano.

Sulle 32 strategie viste nei trade attuali, l'abbinamento riesce per **26** quando gli si
danno tutti e sette i run. Le sei che restano fuori — `PTS_NQ_PCH_001_15`,
`PTS_NQ_PCH_002_15`, `PTS_ES_PCH_001/002_60`, `PTS_ES_BSW_001_60`, `PTS_NQ_TFM_001_60` —
vengono da uno sweep che in `run-engine/` non c'è. Sono anche le due che pesano di più nei
campioni recenti: se hai quella cartella da qualche parte, aggiungila.

## Verifiche fatte sullo script

| test | atteso | ottenuto |
|---|---|---|
| `run-x` contro `run-06-gosto` (identici) | 100% | **100%**, 0 divergenze, P&L identico su tutte e 6 le famiglie |
| famiglia 1 contro famiglia 2 (diverse) | pochissime | **2,3%**, prima divergenza alla prima barra, cause di uscita incompatibili |
| Python 2012–2025 contro backtest C# 2025–2026 | nessuna sovrapposizione | rilevata e segnalata |

## Il passo che manca

I run Python finiscono tutti fra aprile e maggio 2025; i backtest C# che esistono oggi
partono da giugno 2025. **Zero giorni in comune**, quindi al momento lo script non ha nulla
da confrontare — e lo dice.

Il datafeed `NQ` 15m copre `2006-01-03 → 2025-05-30`, quindi il backtest sul periodo giusto
si può fare. Serve un run C# su **2012-01 → 2025-05** con le 26 strategie abbinate e
`closeAllPositionsAtWeekEnd: false` (il riferimento non chiude a fine settimana).
Poi questo script diventa utile.

## Cosa aspettarsi

Dall'unico confronto manuale mai eseguito (`docs/domini/porting-da-report-sweep.md`,
2 strategie su 39):

| | Python | Piootoo |
|---|---|---|
| PCH_001 | 925 trade, $204.200 | 1.084 trade, $165.909 |
| PCH_002 | 691 trade, $170.281 | 816 trade, $104.666 |

Il C# fa ~17% di trade in più e dal 20 al 40% di profitto in meno. Quei numeri però sono
anteriori a due bug corretti il 17/08/2026 e il documento stesso li dichiara scaduti.

Tre divergenze restano aperte e vanno tenute presenti leggendo l'output:

1. **Etichettatura delle barre.** Il datafeed etichetta sull'apertura, `EasyLib.OHLCMulti5`
   assume la chiusura. Su 1.684 sessioni 2020–2025 l'open di sessione differisce nell'82,6%
   dei casi. Per PCH_001 vale 175 trade e $36.785 di utile che la fonte non ha.
2. **Slippage.** Il riferimento applica 1 tick sui fill stop, l'engine Piootoo zero, e non
   esiste un parametro di slippage: la rettifica è manuale, ~$10/trade su NQ.
3. **Finestra oraria HHMM.** Allineata in `PriceChannelEngine`, ancora al confronto sulle
   sole ore in `VolatilityBreakoutEngine`, `LevelFaderEngine` e `SessionBreakoutEngine` —
   quindi la finestra si allarga fino a `HH:59`. Tocca le 5 strategie SBO in produzione.

Quindi: uno scarto non è di per sé un bug del porting. Ma sono tre cause note e finite, e
lo script serve proprio a vedere se lo scarto è spiegato da quelle o da qualcos'altro.
