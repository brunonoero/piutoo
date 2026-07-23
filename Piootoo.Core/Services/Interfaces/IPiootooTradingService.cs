using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services.Interfaces;

/// <summary>
/// Servizio per l'emulazione del trading
/// </summary>
public interface IPiootooTradingService
{
    /// <summary>
    /// Inizializza il trading emulator con capitale iniziale e commissione
    /// </summary>
    void Initialize(decimal initialCapital, decimal commissionPerContract = 2.0m);
    
    /// <summary>
    /// Elabora i segnali delle strategie e aggiorna lo stato
    /// </summary>
    /// <param name="signals">Lista di segnali dalle strategie</param>
    /// <param name="currentPrice">Prezzo corrente del mercato</param>
    /// <param name="currentTime">Data/ora corrente</param>
    TradingSnapshot ProcessSignals(List<TradeSignal> signals, decimal currentPrice, DateTime currentTime);

    /// <summary>
    /// Elabora i segnali usando una mappa prezzi per simbolo.
    /// </summary>
    TradingSnapshot ProcessSignals(List<TradeSignal> signals, Dictionary<string, decimal> currentPrices, DateTime currentTime);

    /// <summary>
    /// Elabora i segnali usando prezzi e candele OHLC per controlli intrabar.
    /// </summary>
    TradingSnapshot ProcessSignals(List<TradeSignal> signals, Dictionary<string, decimal> currentPrices, Dictionary<string, OhlcvData> currentBars, DateTime currentTime);

    /// <summary>Fornisce alla strategia lo stato di esecuzione posseduto dall'engine.</summary>
    StrategyExecutionSnapshot GetExecutionSnapshot(string strategyCode, string symbol, DateTime barTimeUtc);

    /// <summary>Memorizza la memoria tecnica restituita da un adapter stateless.</summary>
    void CaptureStrategyRuntimeState(string strategyCode, string symbol, IReadOnlyDictionary<string, object?> runtimeState);

    /// <summary>
    /// Aggiorna equity e controlli rischio usando gli ultimi prezzi disponibili per simbolo.
    /// </summary>
    TradingSnapshot UpdateMarketPrices(Dictionary<string, decimal> currentPrices, DateTime currentTime);

    /// <summary>
    /// Aggiorna equity e controlli rischio usando gli ultimi prezzi e candele OHLC disponibili per simbolo.
    /// </summary>
    TradingSnapshot UpdateMarketPrices(Dictionary<string, decimal> currentPrices, Dictionary<string, OhlcvData> currentBars, DateTime currentTime);
    
    /// <summary>Chiude tutte le posizioni aperte al prezzo di mercato corrente.</summary>
    TradingSnapshot CloseAllOpenPositions(Dictionary<string, decimal> currentPrices, Dictionary<string, OhlcvData> currentBars, DateTime currentTime);
    
    /// <summary>
    /// Ottiene lo snapshot corrente dello stato di trading
    /// </summary>
    TradingSnapshot GetSnapshot();

    /// <summary>Restituisce una copia dei trade realmente chiusi dall'engine.</summary>
    IReadOnlyList<TradingResult> GetClosedTrades();
    
    /// <summary>
    /// Applica un filtro di strategie ai risultati del backtesting
    /// </summary>
    /// <param name="result">Risultato originale del backtesting</param>
    /// <param name="enabledStrategies">Lista di strategie abilitate</param>
    /// <param name="multipliers">Moltiplicatori per ogni strategia</param>
    /// <returns>Nuovo risultato filtrato</returns>
    BacktestingResult ApplyStrategyFilter(BacktestingResult result, List<string> enabledStrategies, Dictionary<string, decimal> multipliers);
    
    /// <summary>
    /// Resetta lo stato del trading emulator
    /// </summary>
    void Reset();
}
