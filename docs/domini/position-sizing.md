# Position sizing e confine autorevole

**Le strategie dichiarano l'ingresso, non la size**: tutte espongono `Contracts = 1`
nel costruttore, e la quantità nasce dai layer a valle. Il capitale rispetto a cui
quel contratto è dichiarato è `TradingConventions.StrategyReferenceBalance`
(1.000.000), lo stesso numero proposto come capitale del backtest interno e usato
come denominatore di `BalanceScale` nelle sessioni.

Attenzione: `Initialize` accetta un parametro `"Contracts"` che sovrascrive quella
base, inoltrato da `StrategyFactory`. È una leva fuori dai layer — chi la valorizza
sposta la base da cui tutti i moltiplicatori partono.

**Il backtest interno applica solo l'allocazione Titano.**
`PiootooBacktestingService` non chiama `PositionSizingService`: volatilità,
freni di portafoglio, arrotondamento e minimo agiscono soltanto nelle sessioni.
È voluto — quel run è il campione sorgente di Titano e deve misurare le strategie
a un contratto — ma significa che le quantità di un `trades.json` interno e quelle
di una sessione non sono omogenee.

Nelle sessioni la quantità è determinata dal server una sola volta:

`FinalQuantity = BaseQuantity × StrategyEquityMultiplier × MarketVolatilityMultiplier × PortfolioRiskMultiplier`.

Titano produce il coefficiente strategy-equity lento, efficace per periodo. Il
coefficiente market-volatility usa ATR calcolato soltanto sulle barre con
timestamp non successivo alla barra corrente, `DollarsPerPoint` e rischio
monetario target. Il coefficiente portfolio riduce la size in proporzione al
drawdown dal picco e all'esposizione lorda, e si azzera oltre le rispettive
soglie.

Il coefficiente portfolio è **attivo solo in `ServerSimulated`**: dipende da
capitale ed equity della sessione, che in `ExternalBroker` il server non
possiede — l'equity è del broker e ogni account ha il proprio saldo. Lì il
rischio di portafoglio è governato dal broker
(`PiootooRiskGuardianBot`), coerentemente con l'invariante "il server decide
*cosa*, il broker decide *se e a che prezzo*"; `ResolvePositionSizing` disattiva
il blocco all'apertura della sessione invece di farlo girare su un denominatore
fittizio. L'overlay CPPI è stato rimosso: vedi `docs/decisioni.md` (2026-08-05).

In `ExternalBroker` la `FinalQuantity` viene poi scalata **per account** al
momento del claim: `BalanceScale` (saldo del conto sul milione di riferimento)
per `ContractMultiplier` (lotto broker su contratto Piootoo). Vedi
[`account-e-conversione-symbol.md`](account-e-conversione-symbol.md).

I coefficienti sono clampati in `[0,1]` di default e la quantità finale non può
superare la base. Moduli aggressivi/anti-martingale/optimal-f sono disabilitati;
il contratto richiede abilitazione esplicita, fractional factor e cap. Queste
tecniche possono amplificare perdite e non sono una garanzia finanziaria.

`CreateTradingSessionRequest.instruments` accetta metadata broker autorevoli:
`symbol`, `dollarsPerPoint`, `minimumQuantity`, `quantityStep`, `roundingMode`.
I futures arrotondano per difetto a contratti interi; CFD/cTrader arrotondano al
volume step. Sotto il minimo viene persistito un signal/intent cancellato con
`BelowMinimumQuantity` e non nasce alcun ordine o trade.

ServerSimulated ed ExternalBroker ricevono la stessa `FinalQuantity` dal sizing;
in ExternalBroker la conversione per account viene applicata dal server al
claim, non dal client. cTrader non deve scalarla né convertirla nuovamente.
`signals.json` schema v2 conserva
base, tre coefficienti, finale e motivo; `trades.json` conserva la quantità
effettivamente filled. L'idempotency key della barra impedisce un secondo sizing
in replay.

Riferimenti codice: `Piootoo.Core/Services/PositionSizingService.cs`,
`Piootoo.Shared/Models/Trading/TradingSessionContracts.cs`,
`Piootoo.Shared/Models/Trading/TradingConventions.cs` (capitale di riferimento),
`Piootoo.Core/Services/TradingSessionService.cs` (`ResolvePositionSizing`, `CloneForClaim`).
