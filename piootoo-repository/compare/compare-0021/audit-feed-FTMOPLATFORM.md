# Audit di `datafeed-external/FTMOPLATFORM`

2026-09-05. 36 stream, 8 controlli per stream. Nato per validare `compare-0021`, ma il
risultato riguarda il feed, non quel confronto.

**Verdetto in tre righe.**

1. **Tutti e 17 gli stream oltre l'ora sono inutilizzabili** — griglia del broker, non della
   ricerca. Non serve correggerli: vanno cancellati e riraccolti con il bot 1.2.0.
2. **Le sessioni false ci sono e sono tante** — fino al 20% delle barre su `@FDAX_1440` — e
   **restano anche dopo la riraccolta**: sono una proprietà del feed CFD, non della griglia.
3. Tutto il resto è sano: zero duplicati, zero barre fuori ordine, zero OHLC incoerenti,
   zero prezzi o volumi impossibili, su **266.000 barre**.

## 1. Cosa è stato controllato

| # | controllo | esito |
|---|---|---|
| 1 | ordinamento stretto e duplicati di timestamp | **36/36 PASS** |
| 2 | allineamento al proprio timeframe | 19/19 PASS ≤60m, **0/17 PASS** >60m |
| 3 | coerenza OHLC (`low ≤ min(o,c)`, `high ≥ max(o,c)`, `high ≥ low`) | **36/36 PASS** |
| 4 | prezzi ≤ 0, volumi negativi | **36/36 PASS** |
| 5 | calendario di sessione contro il dossier §2.1.1 | **4 stream FAIL**, e vedi §3 |
| 6 | il 240m raccolto coincide col 240m ricostruito dal 60m dello stesso archivio | **0/7 PASS** |
| 7 | copertura e riscaldamento | 22 stream senza storia prima del 2025-07-01 |
| 8 | `feed-clocks.json` dichiarato | PASS (UTC, dichiarato dalla sorgente) |

## 2. La griglia oltre l'ora: 17 stream su 17 da rifare

Le barre `≤ 60m` sono sulla griglia UTC nativa e sono **giuste**: lo scarto di un fuso è un
numero intero di ore, quindi i bucket di cTrader e quelli del feed coincidono comunque.

Sopra l'ora no. L'ancoraggio locale misurato, per file:

| timeframe | ancoraggio nell'orologio della ricerca | atteso |
|---|---|---|
| tutti i `@*_240` (16 file) | **02:00 e 03:00**, misti nello stesso file | 00:00 |
| tutti i `@*_1440` (3 file) | **22:00 e 23:00**, misti nello stesso file | 00:00 |

Due cose, non una:

- **È l'orologio del broker**, non quello della ricerca. FTMO sta su EET/EEST, un'ora avanti
  a Roma: la sua mezzanotte cade alle 23:00 locali.
- **Non è nemmeno stabile**: dentro lo stesso file convivono due ancoraggi, perché la griglia
  della piattaforma segue l'ora legale del *simbolo* e nelle settimane di sfasamento
  USA/Europa si sposta di un'ora rispetto a Roma.

### La prova che chiude la questione

Rifacendo i bucket a 240m **dalle barre da 60m dello stesso archivio**, sull'ancoraggio della
ricerca, e confrontandoli col 240m raccolto:

| simbolo | ricostruite | raccolte | istanti in comune | OHLC identici |
|---|---|---|---|---|
| NQ | 1.812 | 1.812 | **0** | 0 |
| ES | 1.813 | 1.812 | **0** | 0 |
| GC | 5.719 | 5.709 | **0** | 0 |
| YM | 1.811 | 1.811 | **0** | 0 |
| NG | 1.814 | 1.811 | **0** | 0 |
| HO | 1.813 | 1.811 | **0** | 0 |
| BTC | 2.482 | 2.485 | **0** | 0 |

**Non una barra in comune su 1.812.** Il conteggio quasi identico dice però che la
riraccolta produrrà lo stesso volume di dati: non si perde niente.

### Perché, e perché non c'è niente da correggere nel codice

`PiootooDatafeedSyncBot` **1.1.0** prendeva le serie oltre l'ora dalla piattaforma. La
**1.2.0** (commit `aaec722`) le costruisce dalle barre da 60m — `NativeCeilingMinutes = 60`,
`BucketStartUtc` sull'orologio dichiarato — e lo scrive nel campo `source`. Tutti i 36 file
di questo archivio dicono:

    PiootooDatafeedSyncBot/1.1.0@FTMO Platform

senza il suffisso `griglia(60m->240m, Europe/Rome 00:00)` che la 1.2.0 aggiunge. Il bot in
working tree è già quello giusto, e lo stesso vale per `PiootooDistributedExecutionBot`
(stesso `NativeCeilingMinutes`, stesso `BucketStartUtc`, stesso default `SessionStartHour = 0`).

**Da fare:** cancellare i 19 file oltre l'ora e rilanciare il raccoglitore.

```
piootoo-repository/datafeed-external/FTMOPLATFORM/@*_240.json     (16 file)
piootoo-repository/datafeed-external/FTMOPLATFORM/@*_1440.json    (3 file: ES, FDAX, NQ)
```

I `≤60m` **non** vanno toccati: sono la sorgente da cui i bucket si ricostruiscono.

## 3. Le sessioni false — il problema che sopravvive alla riraccolta

§2.1.1 del dossier: *«un feed CFD quota anche quando il future è chiuso. Dove la ricerca non
ha quella sessione, il feed ne crea una che non è mai esistita: non genera trade, ma spezza
la sessione e con l'uscita a fine sessione chiude posizioni ancora valide. Misurato sul DAX:
11% del P&L.»*

Barre per giorno della settimana, orologio della ricerca:

| file | lun | mar | mer | gio | ven | sab | dom | fuori calendario |
|---|---|---|---|---|---|---|---|---|
| `@FDAX_1440` | 182 | 180 | 180 | 179 | **0** | 0 | **178** | **178 / 899 = 19,8%** |
| `@FDAX_240` | 1120 | 1143 | 1135 | 1135 | 935 | 0 | **186** | 186 / 5.654 |
| `@BTC_240` | 366 | 366 | 369 | 366 | 362 | **296** | 360 | 296 / 2.485 |
| `@BTC_60` | 1464 | 1464 | 1470 | 1464 | 1459 | **1065** | 1414 | 1.065 / 9.800 |

Nota il **venerdì a zero** su `@FDAX_1440`: con la barra giornaliera ancorata alla mezzanotte
del broker, la sessione di venerdì finisce etichettata giovedì. Su quel file non è solo una
sessione di troppo — è tutta la serie traslata di un giorno.

### La regola del dossier non basta come l'ho scritta io

Ho messo in `InstrumentRegistry` una lista di giorni ammessi per strumento
(`SessionDays`), presa dalla tabella §2.1.1. **Su FDAX, CC, CT, KC e SB funziona** (domenica
mai ammessa → i 186 + 178 falsi cadono tutti). **Sui CME non funziona**, e il numero lo
dimostra:

| | arco | domeniche | **domeniche/anno** |
|---|---|---|---|
| `datafeed/@NQ_240` (future, vendor) | 2006→2025, 19,4 anni | 68 | **3,5** |
| `datafeed-external/…/@NQ_240` (CFD FTMO) | 2025→2026, 1,2 anni | 61 | **52,1** |
| `datafeed/@GC_240` (future, vendor) | 2008→2025, 17,4 anni | 62 | **3,6** |
| `datafeed-external/…/@GC_240` (CFD FTMO) | 2022→2026, 3,7 anni | 189 | **51,2** |

Il future ha una sessione domenicale **3,5 volte l'anno** — solo nelle settimane in cui l'ora
legale americana ed europea sono sfasate, quando l'apertura CME delle 17:00 di Chicago cade
alle 23:00 di Roma invece che alle 00:00 di lunedì. Il CFD ne ha una **ogni settimana**. Una
lista "domenica ammessa per NQ" lascerebbe passare il **93%** delle sessioni false.

### La regola corretta, che è anche più semplice

Non serve una tabella di conteggi. Una sessione esiste in un dato giorno dell'orologio della
ricerca **se e solo se l'apertura di borsa dello strumento, convertita in quell'orologio,
cade in quel giorno**. Per i CME: `17:00 America/Chicago → Europe/Rome` dà lunedì 00:00 nelle
settimane normali e domenica 23:00 in quelle sfasate — cioè esattamente 3,5 domeniche l'anno,
da sé.

Tutto il necessario è già nel registro: `InstrumentSpec.SessionTimeZone` tiene l'orologio di
borsa, e le ore di apertura sono già documentate lì come commento (CME 1700, COMEX/NYMEX
1800, Eurex 0800, ICE US 0400, HKEX 0915). I conteggi del dossier diventano la **verifica**
della regola invece della regola stessa.

**Stato:** la tabella `SessionDays` è in `InstrumentRegistry` ma **non è letta da nessuno**.
Il filtro non è stato cablato apposta: applicarlo con la regola sbagliata corromperebbe il
feed in silenzio, che è peggio di non filtrare.

## 4. Riscaldamento: 22 stream partono col run

Zero barre prima del 2025-07-01 su tutti gli `@NQ_*`, `@ES_*`, `@BTC_*`, `@HO_*`, `@NG_*`,
`@CC_*`, `@CT_240`, `@KC_240`, `@PL_240`, `@SB_240`, `@YM_*`, `@HK_*`.

Un backtest che parte il 2025-07-01 brucia quindi il proprio `RequiredCandles` dentro la
finestra: su `@NQ_15` (576 richieste) sono 630 valutazioni saltate, cioè **cieco fino al 9
luglio**. Hanno storia solo `@BP_*`, `@CL_30`, `@GC_*`, `@FDAX_240` e `@FDAX_1440`.

Non è un difetto del feed — è la data in cui la raccolta è cominciata — ma è una condizione
da conoscere: **un run che parte dove parte il feed non è confrontabile con uno che ha il
riscaldamento.** Il rimedio è far partire il backtest almeno `RequiredCandles` dopo l'inizio
del feed, oppure raccogliere all'indietro.

## 5. Cose sane, che non vanno più guardate

- **266.000 barre, zero duplicati e zero fuori ordine**, su tutti e 36 gli stream.
- **Zero OHLC incoerenti**, zero prezzi ≤ 0, zero volumi negativi.
- **Griglia UTC nativa perfetta** su tutti i 19 stream ≤ 60m: nessuna barra fuori dal
  multiplo del proprio timeframe.
- `feed-clocks.json` presente e dichiarato UTC, con la nota che spiega perché qui UTC non è
  un'assunzione.
- I bucket incompleti nella ricostruzione a 240m (NQ: 4 bucket con una sola barra da 60m, 7
  con due, 299 con tre) sono **normali**: sono le code di sessione, non buchi.

## 6. Trovato per strada, fuori tema ma da non perdere

- **`appsettings.json` punta a `D:\Piootoo\PiootooApp\piootoo-repository`, che su questa
  macchina non esiste.** Funziona solo perché `appsettings.Development.json` lo sovrascrive
  con il percorso su C: e `launchSettings` forza `ASPNETCORE_ENVIRONMENT=Development`.
  Lanciare il server fuori da quel profilo lo fa puntare a una cartella inesistente.
- **Il workspace `all-in`, con cui gira il piano `FTMO-ALL`, non è in
  `piootoo-repository/workspaces/`.** Ci sono `ftmo`, `plans`, `pts-02`, `pts-only-1`,
  `small`. Il run interno di `compare-0021` lo dichiara come proprio workspace.
- **`JY` non è in `InstrumentRegistry`** (sta in `KnownButUnverified`) ma il catalogo ha 8
  strategie `PTS_JY_*`. `InstrumentRegistry.Get` lancia: quelle strategie non possono
  convertire denaro in punti.
- **Le sette strategie FDAX usano `ResearchSession()` = 00:00** mentre il dossier dichiara
  `01:00` per FDAX. È una deviazione **deliberata e documentata** nel sorgente
  (`OHLCMulti5` compensa l'etichettatura solo per `session_start_hour = 0`): non toccata, ma
  va risolta quando la compensazione coprirà qualunque ora.

## 7. Come rifare questo audit

Gli script stanno nello scratchpad di sessione e non sono committati — sono 150 righe di
Python che leggono i JSON e non dipendono da niente. I controlli, in ordine: ordinamento e
duplicati sui timestamp; `(ora*60+min) % tf` per l'allineamento nativo; l'insieme di
`(minuti_locali % tf)` per l'ancoraggio oltre l'ora, che deve avere **un solo** elemento e
valere 0; le tre disuguaglianze OHLC; `weekday()` nell'orologio della ricerca contro la
tabella §2.1.1; il refold 60m→240m con confronto per istante.

Il controllo n. 6 (refold) è quello che vale: gli altri trovano difetti, quello dimostra
un'incompatibilità.
