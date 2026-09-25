using Piootoo.Shared.Models;

namespace Piootoo.Shared.Interfaces;

/// <summary>
/// Estensione per le strategie che leggono le barre di <b>altri simboli</b> oltre al proprio: operano
/// su <see cref="ITradingStrategy.Symbol"/> e decidono guardando anche i simboli di riferimento, sullo
/// stesso timeframe. E' il contratto dei motori tra mercati (XMK, serie PT6EXO).
///
/// <para><b>Dove e' servito, dove no.</b> Al 25/09/2026 le serie di riferimento arrivano solo dalla
/// sweep (<c>SweepRunner</c>), cioe' dalla ricerca. Backtest completo, sessione live e cBot non le
/// portano: una strategia che le dichiara e non le riceve si ferma con un errore invece di valutare
/// alla cieca, perche' un dato mancante qui e' la stessa cosa del datafeed mancante.</para>
/// </summary>
public interface IMultiSymbolTradingStrategy : ITradingStrategy
{
    /// <summary>
    /// I simboli di riferimento, normalizzati come quelli delle strategie (<c>@GC</c>). Il simbolo
    /// operato non ci sta: arriva come serie primaria.
    /// </summary>
    IReadOnlyCollection<string> ReferenceSymbols { get; }

    /// <summary>
    /// Genera il segnale con la serie primaria e quelle di riferimento, sullo stesso timeframe; la
    /// chiave del dizionario e' il simbolo normalizzato.
    /// </summary>
    TradeSignal GenerateSignal(
        OhlcvData[] data,
        IReadOnlyDictionary<string, OhlcvData[]> referenceData,
        DateTime currentDate);
}
