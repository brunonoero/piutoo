# Profilo per regime di mercato — 29/09/2026

Domanda: ha senso dare a ogni strategia il regime di mercato in cui rende (trend, laterale, calma,
agitazione) e spegnerla nei regimi sfavorevoli?

**Risposta breve: come descrizione sì, come interruttore no.** Spegnere le strategie nei regimi
che la storia indicava come dannosi avrebbe tolto guadagno in tutti e tre i tagli provati.

## Come

- Run: `piani-pesati-2026-09-27\run-storia-senza-pesi` (132 strategie, 75.984 trade, 2012-2025,
  size neutre: lo stesso input dei piani senza pesi).
- Barre giornaliere piegate per data UTC dal feed interno (`@SYM_1440`, altrimenti 240/60/30).
  KC c'e' solo sul feed FTMO dal 2023: 501 trade KC restano senza etichetta.
- Etichette del giorno `d` calcolate con le sole barre chiuse fino a `d-1`, cioe' quello che si sa
  all'ingresso:
  - **direzione**: efficiency ratio a 50 giorni; terzile alto del simbolo = `trend-su`/`trend-giu`
    secondo il segno, il resto `laterale`;
  - **volatilita'**: ATR(20)/prezzo, percentile sugli ultimi 250 giorni: `calma` < 33, `agitata` > 67.
- Per strategia e regime: trade, netto, average trade, e uno z dell'average trade del regime contro
  quello del resto (solo con almeno 30 trade da entrambe le parti).

## Risultati

**1. Qualche dipendenza dal regime c'e', ma poca.** Ci sono 64 confronti con |z| > 2 su 568
(11%), contro il 5% che darebbe il caso. Ventotto strategie positive hanno un regime con average
trade negativo. Quello ricorrente e' la **calma** sui breakout degli indici USA
(`NQ_BOS_001_15`, `YM_VBO_001_15`, `YM_PCH_002_30`, `ES_BSW_001_15`): e' plausibile, un breakout
senza volatilita' non corre.

**2. Il profilo non persiste.** Si divide la storia al 2019 e per ogni coppia strategia-regime si
misura quanto l'average trade del regime si scosta da quello del resto, prima e dopo il taglio.
- La correlazione fra lo scostamento di prima e quello di dopo e' 0,27.
- Lo scostamento ha lo stesso segno nel 58% dei casi: poco sopra il lancio di una moneta.

**3. Spegnere avrebbe fatto perdere.** La regola provata: per ogni strategia si spengono, dopo il
taglio, i regimi che prima del taglio avevano z < -2 e average trade negativo.

| taglio | coppie spente | trade tolti dopo | effetto sul netto |
|---|---:|---:|---:|
| 2016 | 14 | 2.659 | **-874.369** |
| 2019 | 15 | 1.829 | **-917.925** |
| 2022 | 15 | 780 | **-318.210** |

I casi peggiori spiegano il meccanismo.
- `FDAX_LFD_001_15` e `FDAX_RHL_002_60` in volatilita' `normale`: -130/-230k prima del 2016, poi
  +200k e +177k.
- `NQ_BOS_003_60` in `trend-giu`: -89k prima del 2019, +104k dopo.

Il regime "dannoso" era la perdita di un tratto della storia, non una proprieta' della strategia.
Tra le coppie spente, le sole che restano negative dopo il taglio sono di `YM_PCH_002_30` e
`YM_VBO_001_15`. Sono piccole e non ripagano le altre.

## Conclusione

- **Nessun interruttore di regime**, ne' manuale ne' meccanico, con queste definizioni. Conferma
  la regola del metodo (`metodo-unger/SKILL.md`: niente filtri di regime).
- **Il profilo serve come diagnosi.** Una strategia la cui perdita si concentra in un regime va
  guardata come si guarda il *segnale di regime* di `lettura-risultati`. Esempio: `YM_PCH_002_30`
  perde in calma e in trend-su in ogni sottoperiodo.
- **Resta da provare l'uso nella composizione dei piani.** Si tratta di controllare che un piano non
  abbia tutte le strategie che perdono nello stesso regime. E' una misura sul piano, non un
  interruttore, e non ha il problema del punto 3.

## I piani in produzione per regime

Tabelle complete in `piani-per-regime.md` (`piani.py`). I piani misurati sono `FTMO-EUROPA`,
`FTMO-USA` e `FTMO-O4-X05`, i tre del demo 17202911. Le viste sono due:
- **FTMO 2022-2026**: run con il `PlanCode` di EUROPA e USA, O4 dal run neutro `PT5DAV-TUTTE`
  (stessa holding) a size 0,5. Etichette dal feed FTMO.
- **Storia 2012-2025**: le sette di O4 dal run storico. Le PT3B non hanno un run lungo.

**Il conto non ha un regime scoperto.** In tutti e sei i regimi, sia del mercato di ogni trade sia
dell'S&P come mercato comune, il conto guadagna. Il P&L medio al giorno va da +245 (ES in
trend-giu, 49 giorni soli) a +1.294 (ES agitato). La quota di giorni in perdita resta fra 37% e 42%
in ogni regime. Nella storia O4 da sola fa lo stesso: 0 strategie su 7 in perdita in ogni regime di
direzione, una sola nella volatilita' normale.

**Il regime piu' debole e' la calma**: +182/giorno nella storia di O4 contro +551/+582 negli altri
due. E' lo stesso segnale del paniere (i breakout in calma corrono poco), ma resta positivo.

**Una sola cella negativa pesa: `FTMO-EUROPA` in calma, -23.951.** Viene tutta da
`PT3B_FDAX_PCH_002_240`. `FDAX_RHL` e `FESX_RHL` in calma fanno 14 trade in quattro anni, quindi
il "3/3 in perdita" della tabella non significa niente. E non persiste: in calma la PCH_002 fa -43k
nel 2023, -18k nel 2024, -7k nel 2025 e **+44k nel 2026**. E' il meccanismo del punto 3: spegnerla
in calma a fine 2025 avrebbe tolto il 2026 migliore.

**I mesi peggiori non hanno un regime comune.** 2026-06 (-43.558, di cui O4 -25.692) e' ES in
trend-su agitato; 2023-07, 2023-09, 2023-04 e 2023-01 sono calma; 2024-10 e' normale. Nella storia
di O4 prevale ES agitato, cioe' il regime in cui O4 guadagna di piu' in media.

Limiti:
- 2022-2026 ha solo 49 giorni di ES in trend-giu (il ribasso del 2022 cade quasi tutto prima delle
  etichette FTMO, che partono a marzo 2021 per ES e a settembre 2022 per NQ).
- `FTMO-USA` fa 72 trade in quattro anni, troppo pochi per una lettura per regime.
- O4 nella vista FTMO viene dal run neutro, non da un run con il suo `PlanCode`.

**Conclusione sui piani**: nessuna azione. La composizione attuale gia' copre i regimi, che era lo
scopo dello scorrelamento. Il controllo "un piano non perde tutto nello stesso regime" si puo'
aggiungere al resoconto di `piootoo-plan-builder` come verifica, e oggi darebbe esito pulito.

## File

- `regimi.py`: etichette per giorno e profilo per strategia → `profilo-regimi.csv` (una riga per
  strategia e regime) e `fonti-e-quote.json` (fonte delle barre e quota di giorni per regime).
- `analisi.py <trades.json> <anno>`: conteggio delle dipendenze e persistenza al taglio.
- `spegnimento.py`: la prova dello spegnimento a tre tagli.
- `piani.py` → `piani-per-regime.md`: i tre piani in produzione per regime.
- `verifica_europa.py`: `FTMO-EUROPA` per strategia, volatilita' e anno.
