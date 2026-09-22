# Griglia grossa GC 4h — motore nudo, 900 combinazioni, 22/09/2026

**Domanda:** le quattro finaliste della sweep GC (+90/115k fuori campione, solo long, con pattern)
hanno un edge, o cavalcano il rally dell'oro 2025-26? Il controllo è il **motore nudo**: Price
Channel senza pattern, senza filtri orari, con le sole leve grosse — canale {1, 20, 50} × stop
{1000…8000} × target {0…15000} × uscita {fine sessione, 19, 20, 21} × direzione {0, 1, 2} —
misurato al minuto, dentro e fuori campione, stessi costi e stesso split della sweep. Feed XAUUSD
ICS, spread FTMO 0,45, swap ICS, commissione $10,8 per lato. 135 minuti su 8 core. Dati in
`gc-4h-griglia-grossa.csv`, generati da `GcCoarseGridTests`.

## Il risultato

- **148 celle su 900** sono ammissibili (≥ 250 trade in campione e campione in utile).
- **146 di quelle 148 sono in utile fuori campione con almeno 3 finestre su 4.**
- Fuori campione medio delle ammissibili: **~110.000**, qualunque sia l'ora di uscita
  (113k a fine sessione, 103-109k alle 19-21) e qualunque sia la direzione (**entrambe 119k, solo
  long 92k**).
- In campione, le stesse celle rendono in media **14-17k** su ~350 trade in tre anni: sottile.

| leva | celle | OOS medio | IS medio |
|---|---:|---:|---:|
| uscita a fine sessione | 43 | 113.037 | 13.963 |
| uscita alle 19 | 34 | 108.723 | 15.751 |
| uscita alle 20 | 35 | 102.785 | 15.018 |
| uscita alle 21 | 36 | 102.867 | 17.464 |
| direzione entrambe | 81 | 119.550 | |
| solo long | 67 | 92.154 | |

## Cosa vuol dire

**Il fuori campione della sweep GC è il regime, non i pattern.** Un Price Channel qualunque, senza
un solo filtro, in 146 configurazioni su 148 guadagna 100-170k nel 2025-26 sull'oro. La finalista 3
della sweep (+115.029) sta esattamente nella media del motore nudo: i suoi pattern non hanno
comprato niente. E il segno lo dà la direzione: **entrambe le direzioni rendono più del solo
long**, cioè anche lo short guadagna — in un rally? Sì, perché il 2025-26 dell'oro ha anche
correzioni violente, e un breakout a 4 ore le prende. È un anno in cui il motore funziona da
qualunque parte lo si giri: il contrario di ciò che distingue una strategia dal suo mercato.

**In campione il motore nudo è sottile** (14-17k su tre anni, 350 trade) e le celle più
equilibrate — canale 1, stop 10 punti, solo long — fanno +54k dentro e +52k fuori su 590 e 462
trade: il profilo della 002, ma con lo stesso vento alle spalle fuori campione.

**L'ora di uscita sull'oro non conta**: 103-113k in ogni caso. Coerente con i nove orari sulla PTS
e con il fatto che XAUUSD non ha ore di CFD-senza-future come il DAX. Lo swap long ($59 a notte)
non basta a spostare il risultato quando il trend lo paga dieci volte.

## Conseguenze

1. **Nessuna classe GC oggi.** Promuovere la finalista 3 sarebbe promuovere il rally dell'oro.
2. **Il test che decide è un altro periodo.** L'archivio ICS di XAUUSD parte dal 2022-09: va
   raccolto indietro fino al 2014 (il bot lo fa, un anno per run) e la griglia va rifatta con
   campione 2014-2021 — sette anni con dentro rialzi, ribassi e laterali — e validazione 2022-2024.
   Se il motore nudo regge lì, c'è un edge; se no, il 2025-26 era il mercato.
3. **Il metodo funziona come controllo.** 900 combinazioni al minuto in due ore, ogni cella dentro
   e fuori, e una lettura per leva: ha detto in un colpo ciò che la sweep in 214 minuti non poteva
   dire di sé stessa.

## Riferimenti

`gc-4h-griglia-grossa.csv`, `gc-4h-pc-tutto-al-minuto.md` (la sweep), `gc-4h-exithour-a-mano.csv`
(i nove orari sulla PTS), `Piootoo.Strategies.Tests/GcCoarseGridTests.cs`.
