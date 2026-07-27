# Feed worker

> Bozza — contenuto da scrivere.

Da coprire:

- Ruolo di `FeedRunner`/`FeedWorker`: non è ospitato dall'API, gira come
  eseguibile o servizio Windows, oppure come cBot cTrader.
- Cosa invia (solo barre chiuse) e verso quale endpoint
  (`POST /{sessionId}/bars`, vedi `trading-sessions-api.md`).
- Come risolve l'ultimo run Titano applicabile per timestamp barra (vedi
  `titano-rotation.md`, sezione "API e cTrader").

Riferimenti codice: `Piootoo.FeedWorker/`, `Piootoo.Core/Services/
PiootooDataFeedService.cs`.
