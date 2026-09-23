# Sweep @NQ 15m — TFU

- Strategia di partenza: `PT3B_NQ_TFU_001_15`
- Datafeed: ICS
- Spread: ICS,FTMOPLATFORM, mediana **per ora UTC** (NQ da 1.45 a 1.95 pt); costante di riserva: NQ 1.45 pt
- Swap: **NQ** long 6.739 pt/notte, short 0.355 pt/notte, rollover 20:59 UTC (ICS,FTMO)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Tenuta: **overnight e overweek liberi** (parita' con il motore di ricerca)
- Campione di ricerca: 2022-01-01 → 2025-01-01
- Validazione: 2025-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 50 trade, 25 per tratto e 10 in perdita, utile medio ≥ 120, profit factor ≥ 1.25
- Beam: 2 · **fasi pattern spezzate** (deviazione dal motore di ricerca: il pattern richiesto e quello vietato sono ottimizzati uno alla volta, quindi le coppie che rendono solo insieme non sono raggiungibili)
- Spazio: intero
- Durata: 104.1 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| uscita base | 2 | 0 | 0.2 | nessuna ammissibile |
| pattern long: PtnLyYes | 153 | 0 | 4.7 | nessuna ammissibile |
| pattern long: PtnLyNo | 152 | 0 | 4.5 | nessuna ammissibile |
| pattern short: PtnSyYes | 153 | 0 | 4.2 | nessuna ammissibile |
| pattern short: PtnSyNo | 152 | 0 | 4.4 | nessuna ammissibile |
| orari e giorni | 1,250 | 0 | 35.3 | nessuna ammissibile |
| uscita di sessione | 9 | 0 | 0.6 | nessuna ammissibile |
| stop e target | 1,352 | 0 | 50.0 | nessuna ammissibile |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|

## Esito: nessuna finalista sopravvive alla validazione fuori campione.