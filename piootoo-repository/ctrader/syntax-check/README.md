# Controllo sintattico dei cBot fuori da cTrader

I sorgenti in `piootoo-repository/ctrader/` si compilano **solo dentro cTrader**, e questo
significa che finora una modifica poteva restare per giorni sul disco con un errore di
sintassi dentro — `lavori-in-corso.md` lo dice esplicitamente: *«nulla di quanto segue è stato
compilato»*. Un cBot da duemila righe modificato a mano è esattamente il posto in cui un
refuso non si vede a lettura.

Questo progetto colma quel buco: `CalgoStub.cs` dichiara **la sola superficie dell'API cAlgo
che i bot usano** — `Robot`, `Parameter`, `Bars`, `Symbol`, `Server`, `Timer` e poco altro — e
il `.csproj` compila i sorgenti veri, presi dalla cartella sopra.

```bash
dotnet build piootoo-repository/ctrader/syntax-check/syntax-check.csproj
```

## Cosa verifica e cosa no

**Verifica**: sintassi, tipi, membri inesistenti, metodi rimasti senza chiamanti dopo un
refactor, `using` mancanti o superflui. È quanto basta a escludere che un file non compili
dentro cTrader per una svista.

**Non verifica**: il comportamento. Nessun metodo dello stub fa niente, non c'è un broker, non
c'è una serie di barre. Un bot che compila qui può ancora essere sbagliato — questo strumento
sostituisce il compilatore, non il collaudo sul conto demo.

## Lo stub va tenuto onesto

Se un bot comincia a usare un membro dell'API che lo stub non dichiara, la build fallisce con
"membro inesistente": va **aggiunto allo stub**, non aggirato. E la firma va copiata da cAlgo,
non inventata per far passare la build — uno stub che dichiara `Account.Number` come `int`
mentre cAlgo lo espone `long` farebbe passare qui codice che dentro cTrader non compila, cioè
peggio di non avere lo stub.

Verifica di fedeltà: **anche la versione precedente di un bot deve compilare**. Se lo stub
riesce a compilare solo la versione nuova, è stato piegato su di essa.

Oggi copre `PiootooDatafeedSyncBot` e `PiootooDistributedExecutionBot`. Gli altri bot si aggiungono
con una riga `<Compile Include=...>` — tenendo presente che due bot nello stesso progetto devono
avere classi con nomi diversi, cosa che oggi è vera.

Lo stub sta in due file per tenere separate due superfici che si allargano per ragioni diverse:
`CalgoStub.cs` ha la piattaforma (robot, parametri, barre, simboli, orologio), `CalgoStubTrading.cs`
l'esecuzione (posizioni, ordini pendenti, storico, grafico, i metodi di trading del `Robot`). Solo
l'esecutivo usa il secondo.
