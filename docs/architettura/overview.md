# Architettura del solution

> Bozza — contenuto da scrivere.

Mappa moduli: cosa fa ciascun progetto della solution, chi dipende da chi,
dove passano i dati (workspace → backtest → Titano → sessione live).

Progetti coinvolti:

- `Piootoo.Shared` — modelli/contratti condivisi (nessuna dipendenza dagli altri).
- `Piootoo.Domain` — tipi di dominio di base.
- `Piootoo.Core` — servizi applicativi (workspace, backtesting, sizing, Titano,
  trading session). Vedi i singoli documenti in `../domini/`.
- `Piootoo.Strategies` — catalogo strategie (`ITradingStrategy`), incluse quelle
  generate da EasyLanguage.
- `Piootoo.FeedWorker` — worker/servizio Windows che alimenta le sessioni live.
- `PiootooApp.Server` — API ASP.NET Core, espone i controller HTTP.
- `piootooapp.clientform` — console WinForms per workspace/backtest/Titano/
  trading session (client interno di sviluppo).
- `piootooapp.client` — SPA Angular (non collegata al debug F5 del server, vedi
  nota in `../client/`).

Da completare: diagramma/flusso end-to-end e responsabilità di ogni confine
(chi genera i signal, chi decide la quantità finale, chi esegue).
