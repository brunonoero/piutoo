# Position sizing e confine autorevole

La quantità è determinata dal server una sola volta:

`FinalQuantity = BaseQuantity × StrategyEquityMultiplier × MarketVolatilityMultiplier × PortfolioRiskMultiplier`.

Titano produce il coefficiente strategy-equity lento, efficace per periodo. Il
coefficiente market-volatility usa ATR calcolato soltanto sulle barre con
timestamp non successivo alla barra corrente, `DollarsPerPoint` e rischio
monetario target. Il coefficiente portfolio usa equity, peak/drawdown,
esposizione e, opzionalmente, floor/cushion CPPI.

I coefficienti sono clampati in `[0,1]` di default e la quantità finale non può
superare la base. Moduli aggressivi/anti-martingale/optimal-f sono disabilitati;
il contratto richiede abilitazione esplicita, fractional factor e cap. Queste
tecniche possono amplificare perdite e non sono una garanzia finanziaria.

`CreateTradingSessionRequest.instruments` accetta metadata broker autorevoli:
`symbol`, `dollarsPerPoint`, `minimumQuantity`, `quantityStep`, `roundingMode`.
I futures arrotondano per difetto a contratti interi; CFD/cTrader arrotondano al
volume step. Sotto il minimo viene persistito un signal/intent cancellato con
`BelowMinimumQuantity` e non nasce alcun ordine o trade.

ServerSimulated ed ExternalBroker ricevono la stessa `FinalQuantity`. cTrader
non deve scalarla né convertirla nuovamente. `signals.json` schema v2 conserva
base, tre coefficienti, finale e motivo; `trades.json` conserva la quantità
effettivamente filled. L'idempotency key della barra impedisce un secondo sizing
in replay.

Riferimenti codice: `Piootoo.Core/Services/PositionSizingService.cs`,
`Piootoo.Shared/Models/Trading/TradingSessionContracts.cs`.
