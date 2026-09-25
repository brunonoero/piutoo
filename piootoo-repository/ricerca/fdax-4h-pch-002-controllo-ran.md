# PT3B_FDAX_PCH_002_240 contro il caso: controllo RAN

La 002 contro cento ingressi casuali con le **stesse uscite**: stop 5.000, target 4.500, 12 barre,
intraday con uscita alle 21:00, finestra 03-18, un ingresso per sessione e per lato. Cambia solo il
quando e il verso dell'ingresso. Primo uso del controllo RAN (skill `controllo-ran`).

Feed ICS 2014-07-18 → 2026-09-01, split 2021-01-01, orologio al minuto; spread mediano ICS 0,50 punti,
swap ICS, commissione 19,23 per lato — gli stessi di `Pt3b002LongPeriodTests`. Probabilita' per barra
flat tarata separatamente sui due periodi su 8 semi (campione 0,585, trade medi 1.580 contro 1.609;
validazione 0,640, 1.495 contro 1.527), poi i semi 1001-1100. Durata: 13 minuti su 8 core. Studio
`Pt3b002RandomControlStudy`, CSV per seme `fdax-4h-pch-002-controllo-ran.csv`, log
`fdax-4h-pch-002-controllo-ran.log`, 25/09/2026.

## Il risultato

- **Fuori campione la 002 sta al 97° percentile** su netto (106.254 contro p95 72.056), average trade
  (69,6 contro 48,1), netto/DD (1,48 contro 0,80) e al 96° sul profit factor (1,06 contro 1,04).
- **In campione sta sopra tutti i cento semi** (netto 157.820 contro p95 86.501).
- **Le uscite da sole perdono**: la mediana dei semi e' −111.306 fuori campione e −79.179 dentro, con
  profit factor 0,94-0,95. A questa frequenza di trade i costi (spread e 38,46 di commissione per
  giro) e la forma asimmetrica stop/target mangiano un ingresso senza informazione.

| | 002 | p5 semi | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| validazione, netto | 106.254 | −285.068 | −111.306 | 72.056 | 97% |
| validazione, average trade | 69,6 | −189,7 | −74,9 | 48,1 | 97% |
| validazione, profit factor | 1,06 | 0,85 | 0,94 | 1,04 | 96% |
| validazione, netto/DD | 1,48 | −0,92 | −0,58 | 0,80 | 97% |
| campione, netto | 157.820 | −210.008 | −79.179 | 86.501 | 100% |

## Cosa significa e cosa non significa

Significa che **il segnale della 002 porta informazione**: con le sue stesse uscite, un ingresso a caso
alla stessa frequenza perde in media piu' di centomila, e la 002 guadagna. Il 97° percentile fuori
campione e' la condizione che la skill chiede per dire "vale per il segnale", e la differenza non e'
al margine: la 002 sta sopra il p95 su ogni metrica.

**Non** significa che la 002 passi il metodo. Il suo average trade fuori campione e' 70, contro la soglia
di 365 del periodo (15% del range medio della barra, `Pt3b002LongPeriodTests`): il controllo RAN si
aggiunge alle soglie di `lettura-risultati`, non le sostituisce. Dice anche un'altra cosa, utile per
tutte le famiglie PT6EXO: su FDAX 4h, con questi costi, **le uscite non regalano niente** — un motore
che su questa cella chiude in utile non puo' doverlo alla forma dello stop e del target.

Un limite dello studio: i semi fanno in media il 2% di trade in meno della 002 (la taratura si ferma
sotto il 3%). Con una mediana cosi' negativa, pochi trade in piu' la spostano ancora piu' in basso: non
cambia il verdetto.

## Conclusione

La 002 **batte il caso** fuori campione (97° percentile su tutte le metriche) e resta **sotto la soglia
di average trade** del metodo. Nessuna decisione nuova sul piano: la 002 era gia' in discussione per
l'average trade, e questo controllo toglie solo il dubbio che il suo netto fosse un effetto delle uscite.
Il controllo RAN va ripetuto su ogni cella PT6EXO che risponde, prima di promuoverla.

## Riferimenti

- `Piootoo.Strategies.Tests/Pt3b002RandomControlStudy.cs`, `Piootoo.Strategies/PT6EXOStrategies/Engines/RandomEntryEngine.cs`
- `Piootoo.Strategies.Tests/Pt3b002LongPeriodTests.cs` (soglie di average trade del periodo)
- `docs/domini/catalogo-idee-pt6exo.md` §"Il controllo: RAN"; skill `controllo-ran`
