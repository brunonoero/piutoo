# Strategie e catalogo

> Bozza — contenuto da scrivere.

Da coprire:

- Interfaccia `ITradingStrategy`: contratto minimo (`GenerateSignal`,
  `Initialize`) e cosa deve garantire un'implementazione.
- Catalogo strategie: come viene costruito e come si registra una nuova
  strategia.
- Generazione automatica da EasyLanguage (`Piootoo.Strategies/Easy/`): script
  `GenerateAllStrategies.ps1`, sorgenti in `piootoo-repository/easy/`, output
  `Easy_*.cs`. Vedi `Piootoo.Strategies/Easy/README.md` per i dettagli d'uso
  dello script.

Riferimenti codice: `Piootoo.Core/Services/StrategyFactory.cs`,
`Piootoo.Strategies/`.
