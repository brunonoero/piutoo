# compare-0048 — Perché tre backtest cTrader dello stesso piano danno risultati inconciliabili

**Data:** 21/09/2026 · **Piano:** `PT3B-FDAX` (ICS) · **Strategia:** `PT3B_FDAX_PCH_001_240`
**Server 7.5.4, client 7.5.2, profilo di esecuzione `DalPiano`.**

Tre run `ExternalBroker` sullo stesso piano davano tre risposte diverse, una delle quali con il
conto azzerato in margin call. Il sospetto iniziale era sul cBot — size doppia o due posizioni
sovrapposte sullo stesso simbolo. **Il cBot non c'entra.** La causa è la modalità dati scelta nella
finestra di backtest di cTrader, che non compare in nessun artefatto.

## 1. Le due ipotesi sul cBot sono smentite dai dati

Su tutti e cinque i run presenti in `workspaces/v03-pt3b/backtests/`:

- `quantity = 25` su **ogni** trade, senza eccezioni: 901, 1.035, 1.317, 1.073, 75 record;
- **zero sovrapposizioni** temporali — nessun trade entra prima che il precedente sia uscito;
- intent con le stesse proporzioni ovunque: ~45% riempiti, ~2% rifiutati, ~53% annullati;
- stessa frequenza operativa: 23,0 trade/mese nel run peggiore, 23,2 nel migliore.

Da aggiungere che il filtro anti-doppio-ingresso di `TradingSessionService` vale sempre dal
15/09/2026, anche a lucchetti spenti, e che il run incriminato gira col profilo `DalPiano`:
`BacktestSorgente` non era nemmeno in gioco.

## 2. Quello che li divide: tick contro barre al minuto

La percentuale di ingressi che cadono sul **minuto tondo esatto** partiziona i run senza sfumature:

| run | periodo | trade | netto | entry al minuto tondo |
|---|---|---:|---:|---:|
| `...20220102-2300-...1030` | 2022-01 → 2026-09 | 1.317 | +$103.128 | **0%** |
| `...20230111-0000-...0935` | 2023-01 → 2026-09 | 1.035 | +$57.278 | **0%** |
| `...20230101-2203-...1502` | 2023-01 → 2026-03 | 901 | **−$98.613** | **100%** |
| `...20140801-0000-...1449` | 2014-08 → 2018-09 | 1.073 | −$98.080 | **100%** |
| `...20140801-0000-...1448` | 2014-08 → 2014-11 | 75 | +$21.297 | **100%** |

0% o 100%, mai una via di mezzo: i primi due girano su tick, gli altri tre su barre al minuto.

**Controprova diretta:** rilanciato il 01/01/2023 → 31/08/2026 a tick, stesso piano e stesso bot,
il run che a barre faceva −$98.613 chiude a **+33.535 € (+34%)**.

## 3. Dove finiscono i 145k

Ristretti alla finestra comune 2023-01-11 → 2026-03-31, i due run hanno **893 e 894 trade**:

| | a barre m1 | a tick |
|---|---:|---:|
| uscite `Closed` (tempo) | 618 → −$165.529 | 623 → −$164.378 |
| `TakeProfit` | 155 → +$717.911 | **167** → +$749.196 |
| `StopLoss` | **120** → −$639.668 | **104** → −$526.671 |

Le uscite a tempo coincidono. Tutta la differenza sta nelle uscite protettive: **16 trade che a
tick vanno a target, a barre vanno a stop**, più lo slittamento degli stop.

| su uno stop dichiarato da 200 punti | a barre m1 | a tick |
|---|---:|---:|
| distanza media | 211,4 pt | **201,0 pt** |
| massimo | **533,6 pt** | 212,3 pt |
| stop oltre 220 pt | 9 | **0** |
| distanza media dei target (dichiarati 180) | 187,0 pt | 181,1 pt |

## 4. Il meccanismo: il fill avviene all'estremo della barra, non al livello

Per i 13 stop più slittati del run a barre, confronto fra il prezzo di uscita e la barra del feed
ICS a un minuto che tocca per prima il livello:

| dir | uscito a | livello | barra che tocca il livello | estremo | scarto |
|---|---:|---:|---|---:|---:|
| Buy | 23181,3 | 23209,4 | 09:30 O=23216,3 H=23217,3 L=**23181,3** | 23181,3 | **0,0** |
| Buy | 23206,0 | 23222,1 | 08:02 O=23230,3 H=23231,9 L=**23206,0** | 23206,0 | **0,0** |
| Buy | 23676,0 | 23857,6 | 11:44 O=23883,2 H=23883,2 L=**23676,0** | 23676,0 | **0,0** |
| Sell | 24483,4 | 24421,6 | 15:56 O=24365,3 H=**24483,3** L=24365,3 | 24483,3 | 0,1 |
| Sell | 25339,8 | 25234,2 | 15:01 O=25176,7 H=**25339,7** L=25172,2 | 25339,7 | 0,1 |
| Sell | 22404,0 | 22070,4 | 11:05 O=21960,4 H=**22403,9** L=21960,4 | 22403,9 | 0,1 |

**12 casi su 13 con scarto 0,0 o 0,1** (lo 0,1 sugli short è mezzo tick di spread: il feed è Bid).
Non è slippage e non è casuale: lo stop si riempie **all'estremo della barra al minuto** — il low
per un long, l'high per uno short.

La ragione è che in modalità "m1 bars" cTrader non ha i tick e ne sintetizza una manciata per
barra dai soli OHLC. Il prezzo salta da open a high a low a close: un livello *interno* al range
non esiste come prezzo raggiungibile, e il primo valore sintetico che lo oltrepassa è l'estremo
della barra. **Lo slittamento non è un costo fisso, è l'intera escursione della barra oltre il
livello**: 15-30 punti su una barra tranquilla, 181 sul crollo del 23/05/2025 (apre 23883, chiude
23676 nello stesso minuto), 333 il 23/03/2026.

Stessa meccanica sui target, in senso opposto — 187 punti medi contro i 180 dichiarati. I due
errori non si compensano: quando una barra contiene sia stop sia target, la sequenza sintetica
O→H→L→C decide arbitrariamente chi scatta prima, ed è da lì che nascono i 16 trade migrati.

### I tre motori rispondono in tre modi

| | dove riempie lo stop | verso dell'errore |
|---|---|---|
| engine interno, feed m1 | **al livello esatto** (`PiootooTradingService.ProtectiveFillPrice`), salvo gap d'apertura e `StopFillSlippagePoints` | ottimista: nessuna barra costa più del livello |
| cTrader, tick | dove il tick lo attraversa davvero | è la realtà |
| cTrader, m1 bars | **all'estremo della barra** | pessimista, senza tetto |

Il run a tick misura 201,0 punti medi contro i 200 dichiarati: **la realtà è a un punto
dall'assunzione dell'engine interno**, e il backtest a m1 di cTrader è l'unico dei tre a sbagliare
di molto. Sbaglia perché risponde a una domanda che con le sole OHLC non è rispondibile, invece di
rifiutarsi di rispondere.

## 5. Nota a margine: un buco nel feed ICS il 23/03/2026

Il trade peggiore del run a barre (−$13.379) esce sul prezzo 22404, che nel feed ICS a un minuto è
un artefatto:

```
11:04  O=21962.4  H=21968.4  L=21957.4  C=21960.9
11:05  O=21960.4  H=22403.9  L=21960.4  C=22403.9    ← +443 punti in un minuto
11:11  O=22889.9                                      ← mancano 11:06-11:10
11:16  O=23242.9  H=23465.9
```

Su **3.987.553 barre** del feed ICS i salti sopra l'1% fra barre contigue sono **otto in dodici
anni**, e due di questi otto stanno in quella finestra. È un'anomalia locale, non un feed rotto:
non giustifica una ricostruzione, ma va tenuta presente se un singolo trade domina un risultato.

## 6. Il numero che resta, e non è buono

Il run a tick 2023-01 → 2026-09, che è quello sano:

| | 2023-01 → 2026-09 | 2022-01 → 2026-09 |
|---|---:|---:|
| finale da $100.000 | $157.278 | $203.128 |
| **minimo toccato** | **$37.386** | $83.318 |
| **max drawdown** | **$62.614 (62,6%)** | $75.119 (47,4%) |
| perdite consecutive | 8 | 13 |
| win rate | 49,4% | 49,5% |
| vincita / perdita media | $2.691 / −$2.515 | $2.742 / −$2.533 |
| peggior singolo trade | −$5.346 = **5,3% del conto** | −$6.466 = 6,5% |

Il drawdown vero è **62,6%, non il ~$47.000 stimato finora**. Circa 8% annuo composto per
rischiare sei decimi del conto. Il rapporto non si aggiusta con la size: a un terzo della
posizione il drawdown scende sotto il 20% e il rendimento con lui. Il problema non è quanto è
grande la posizione, è che il margine per trade è troppo sottile rispetto alla varianza.

### 6 bis. L'edge non è distinguibile dal caso

Sui netti per trade del run sano, test t a una campionatura sulla media:

| run | n | media/trade | dev. std | errore std | **t** | intervallo 95% sulla media |
|---|---:|---:|---:|---:|---:|---|
| 2023-01 → 2026-09 | 1.035 | $55,3 | $3.142 | $97,7 | **0,57** | da **−$136** a +$247 |
| 2022-01 → 2026-09 | 1.317 | $78,3 | $3.182 | $87,7 | **0,89** | da **−$94** a +$250 |

Tradotto in utile annuo, l'intervallo di confidenza va da **−$38.000 a +$69.000**. Il risultato
positivo è perfettamente compatibile con un'aspettativa nulla: servirebbe `t ≈ 2` per distinguerlo
dal caso, e ne servirebbe di più visto che questa configurazione è stata *scelta* fra decine di
migliaia — la selezione alza l'asticella, non la abbassa.

Questo non dice che la strategia sia cattiva: dice che **1.035 trade su un simbolo non bastano a
sapere se sia buona**. La validazione fuori campione (52% del punteggio conservato) è l'unico
indizio a favore, ed è debole per costruzione. La potenza statistica non si compra con più anni
sullo stesso mercato — il campione cresce come la storia, e la storia vecchia ha costi che non
esistono più. Si compra con **più celle**: simbolo × timeframe × motore.

1. **Il nodo dei tre run è chiuso.** Nessun difetto nel cBot, nessun difetto nel server.
2. **La tabella dei costi 2014-2018 di `lavori-in-corso.md` non è valida**: era misurata contro il
   run `...1449`, che è a barre m1. Lo scarto di −$123 per trade contiene slittamento di
   simulazione in quantità ignota. La tesi — cercare dal 2014 significa cercare con costi che in
   quel periodo non esistevano — resta plausibile, ma il numero che la sosteneva va rifatto a tick.
3. **Un run `ExternalBroker` deve dichiarare la modalità dati del backtest cTrader**, come già
   dichiara broker, feed, versione, fattore di stop ed elenco delle strategie allargate. Finché non
   lo fa, due run non confrontabili hanno lo stesso aspetto di due confrontabili — che è esattamente
   ciò che è costato questa giornata.

## Rilettura del 22/09

Lettura a freddo, senza codice, del documento così com'è. Le obiezioni sono domande al testo, non
correzioni: quello che regge resta, quello che non regge è segnato come aperto in
`docs/lavori-in-corso.md`.

**R1. «Il run sano» sono due run diversi, e i numeri si mescolano.** §2 dice che il run
rilanciato a tick chiude a **+33.535 € (+34%)**. §6 chiama «il run sano» quello 2023-01 → 2026-09
e gli attribuisce **$157.278 finali (+57%)**, 1.035 trade, drawdown $62.614: sono i numeri del run
`...0935` della tabella di §2, non del rilancio. Due run a tick dello stesso piano su un periodo
quasi uguale (11/01 contro 01/01/2023; 09 contro 31/08/2026) che danno +57% in dollari e +34% in
euro **non coincidono**, e lo scarto — ordine di $18.000 al cambio corrente — non è spiegato da
dieci giorni di calendario a $55 di media per trade. O il rilancio è su un conto in euro con
condizioni diverse, o due run a tick dello stesso piano divergono per conto loro, e allora
«tick contro barre spiega tutto» è troppo forte. `lavori-in-corso.md` ha ereditato la
confusione: «+34% ... con un drawdown del 62,6% ($62.614)» cuce il rendimento di un run al
drawdown dell'altro. Da chiarire prima di usare qualunque numero di §6.

**R2. Il meccanismo è verificato sui 13 stop più slittati, e generalizzato a tutti.** La frase
«lo stop si riempie all'estremo della barra» vale per i casi *scelti perché* slittavano di più. Se
il meccanismo è universale, deve valere anche per i 107 stop restanti del run a barre: prezzo di
uscita = low (long) o high (short) della prima barra m1 che tocca il livello, con la stessa
tolleranza 0,0-0,1. È un conteggio, non un'interpretazione, e finché non c'è la tabella dei 13
prova che *può* succedere, non che succede *sempre*. Indizio a favore già nel testo: la media di
211,4 contro 201,0 è compatibile con un eccesso sistematico piccolo su tutti. Indizio contro:
nessuno.

**R3. Lo 0,1 sugli short è spiegato troppo in fretta.** «Mezzo tick di spread, il feed è Bid»:
uno stop short si copre sull'Ask, e la mediana ICS dello spread è 0,50, FTMO 1,23. Se il run avesse
lo spread vero, lo scarto sugli short sarebbe ≈0,5, non 0,1. Lo 0,1 dice piuttosto che il backtest
cTrader gira con uno spread fisso minimo o nullo. Non cambia la conclusione di §4, ma cambia la
confrontabilità dei P&L in §3 e §6 con qualunque run che lo spread lo paghi.

**R4. Il confronto che manca è con il motore interno.** Il documento confronta run cTrader fra
loro e mai con il backtest interno sullo stesso periodo, che è il riferimento su cui è costruito
tutto il sistema. I numeri ci sono già: la validazione interna della 001 su 2022-01 → 2026-09 fa
**$153.781 su 1.327 trade** (commissione $4 per lato, spread FTMO per ora, niente swap); il run a
tick 2022-01 → 2026-09 fa **$103.128 su 1.317 trade**, e da `lavori-in-corso.md` si ricava lordo
$212.344, swap $58.564, commissione 1.317 × $38,46 = $50.651. Quindi il lordo interno è ≈$164.000
e quello cTrader ≈$212.000: **il motore interno è più pessimista di ~$48.000 (23%) sul lordo**,
con lo stesso numero di trade. Non è un difetto dichiarato in nessun punto, e può venire da griglia
a 4 ore diversa (la scheda della 001 lo avverte), da spread per ora contro spread del broker, o da
altro. È la riconciliazione più utile che si può fare con i dati che ci sono, ed è assente.

**R5. Il test t tratta i netti per trade come indipendenti.** Un ingresso per sessione e per
lato, zero sovrapposizioni: la dipendenza seriale è debole ma c'è (regimi di volatilità, giorni
contigui), e l'errore standard vero è un po' più largo di $97,7. Non cambia il verdetto — t = 0,57
resta indistinguibile da zero anche allargando — ma la frase «servirebbe t ≈ 2» va letta come
«ancora di più». La conclusione di §6 bis regge; la precisione dichiarata no.

**R6. «Più celle, non più storia» è una preferenza motivata, non una deduzione.** L'argomento
contro la storia è che porta costi di un'altra epoca: è un argomento sul *modello dei costi*, non
sull'informazione nei dati — con uno spread stimato per epoca la storia vecchia tornerebbe
utilizzabile. L'argomento per le celle regge sul paniere (la correlazione misurata su NQ, 0,056, è
un buon indizio) ma **non sulla singola cella**: ogni cella è comunque scelta fra decine di
migliaia e il problema della selezione si moltiplica per il numero di celle invece di diluirsi.
Dire «non lo sappiamo» è giusto; dire che la strada per saperlo è una sola è una scelta.

**R7. Il salto del 23/03/2026 è chiamato artefatto senza controprova.** +443 punti in un minuto
con cinque minuti mancanti *può* essere un evento reale (un minuto di illiquidità sul CFD, un buco
del broker) o un difetto di raccolta: il feed del vendor ha il minuto FDAX ed è il confronto
naturale, e non è stato fatto. Finché non lo è, «anomalia locale» è un'ipotesi.

**R8. Due domande sul rimedio proposto in §7.**
- «Serve un run a tick sul 2014-2018»: cTrader ha davvero i tick ICS dal 2014, o oltre una certa
  profondità **ripiega in silenzio** sulle barre? Se ripiega, il run «a tick» del 2014 sarebbe di
  nuovo a barre e nessun artefatto lo direbbe. Da verificare prima di lanciarlo.
- «La modalità dati in `origin.json`»: se il cBot non può leggerla dalla piattaforma, il campo lo
  compila una persona, ed è esattamente la disciplina che il documento dice di non volere. La
  percentuale di ingressi sul minuto tondo è **calcolabile dai trade** e partiziona 0%/100%: come
  diagnostica automatica nel summary vale più di un campo dichiarato.

**Cosa regge senza riserve.** Le due ipotesi sul cBot sono smentite dai dati (§1). La partizione
0%/100% è una firma netta e la controprova diretta di §2 è decisiva. La tabella dei tre motori in
§4 è la cosa più utile del documento. La conclusione «la tabella dei costi 2014-2018 non è valida»
regge a prescindere da R8.

## Riferimenti codice

`Piootoo.Core/Services/PiootooTradingService.cs` (`ProtectiveFillPrice`, riga 1204),
`piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs` (`OnPositionClosed`,
`RegisterMissedClose`), `piootoo-repository/datafeed-external/ICS/@FDAX_1.json`,
`workspaces/v03-pt3b/backtests/`.
