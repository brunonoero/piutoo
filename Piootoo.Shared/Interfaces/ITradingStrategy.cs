using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Shared.Interfaces;

/// <summary>
/// Interfaccia base per le strategie di trading
/// </summary>
public interface ITradingStrategy
{

    /// <summary>
    /// Nome della strategia
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Descrizione della strategia
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Simbolo per cui la strategia è stata progettata (es. @ES, @NQ, @CL)
    /// </summary>
    string Symbol { get; }
    
    /// <summary>
    /// Timeframe in minuti per cui la strategia è progettata (es. 60 per 1 ora, 15 per 15 minuti)
    /// </summary>
    int TimeframeMinutes { get; }
    
    /// <summary>
    /// Numero minimo di candele storiche richieste per il calcolo della strategia (es. per medie mobili, RSI, etc.)
    /// </summary>
    int RequiredCandles { get; }

    /// <summary>
    /// True quando la strategia decide l'uscita a runtime — tipicamente verificando un pattern di
    /// uscita barra per barra — e quindi non è in grado di descrivere l'uscita nel segnale di
    /// ingresso.
    ///
    /// <para><b>Queste strategie sono escluse dal catalogo</b> (<see cref="!:StrategyFactory.GetRegisteredStrategies"/>):
    /// non sono selezionabili nel masterfilter, non producono segnali e non entrano nei backtest
    /// né nelle sessioni. L'engine gestisce solo uscite autonome (stop loss, take profit, uscita a
    /// tempo, numero massimo di barre) descritte nel segnale di ingresso.</para>
    ///
    /// <para>Le uscite a tempo (fine sessione, barra N della sessione) NON rendono una strategia
    /// close-dependent: vanno espresse come <c>CloseAtUtc</c> o <c>MaxBarsInPosition</c> sul
    /// segnale di ingresso e sono gestite dall'engine.</para>
    /// </summary>
    bool IsPositionCloseDependent => false;

    /// <summary>
    /// Cosa la strategia <b>vuole</b> tenere: la notte, il fine settimana, nessuno dei due. E' una
    /// dichiarazione, non un permesso — l'ultima parola e' del piano, che puo' tagliare comunque
    /// (vedi <see cref="AccountHoldingPolicy"/>).
    ///
    /// <para>Il default e' <see cref="StrategyHolding.Multiday"/> perche' una strategia che non
    /// dichiara nulla non emette alcuna uscita a tempo, e quindi <i>di fatto</i> tiene: dichiarare
    /// il contrario descriverebbe qualcosa che il codice non fa. I motori Easy lo derivano dal
    /// proprio <c>IntradayOnly</c>.</para>
    /// </summary>
    StrategyHolding Holding => StrategyHolding.Multiday;


    /// <summary>
    /// Valuta la strategia usando dati OHLC e lo stato di esecuzione fornito
    /// dall'engine. Le nuove strategie devono implementare questo metodo e
    /// non mantenere stato di posizione internamente.
    /// </summary>
    TradeSignal Evaluate(StrategyEvaluationRequest request)
        => GenerateSignal(request.Ohlcv, request.BarTimeUtc);

    /// <summary>
    /// API legacy. Le implementazioni nuove devono usare <see cref="Evaluate"/>.
    /// </summary>
    [Obsolete("Use Evaluate(StrategyEvaluationRequest) so execution state is engine-owned.")]
    TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate);
    
    /// <summary>
    /// Inizializza la strategia con parametri specifici
    /// </summary>
    void Initialize(Dictionary<string, object>? parameters = null);
}
