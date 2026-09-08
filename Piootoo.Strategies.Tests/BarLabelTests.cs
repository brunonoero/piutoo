using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Come si chiama una barra quando la si confronta con una soglia dei parametri.
///
/// <para><b>Il fatto.</b> Una barra copre un intervallo — 16:00→17:00 — ma porta un timestamp solo.
/// Il feed Piootoo la etichetta sull'<b>apertura</b> (la chiama <c>16:00</c>); TradeStation e il
/// motore di ricerca Python da cui le <c>PTS_*</c> vengono la etichettano sulla <b>chiusura</b>
/// (la chiamano <c>17:00</c>). Stessa barra, stessi prezzi, nome diverso.</para>
///
/// <para>Finché si confrontano barre con barre non cambia niente. Cambia quando si confronta il
/// <i>nome</i> della barra con un orario di parete preso dai parametri — <c>start_hour</c>,
/// <c>end_hour</c>, <c>skip_day</c>, un orario di uscita — perché quei numeri sono tarati contro
/// nomi di chiusura: il confronto si sposta di <b>esattamente una barra</b>.</para>
///
/// <para><b>La regola vive in un punto solo</b>, <see cref="SessionClock.BarLabelHhmm"/>, e i motori
/// la raggiungono da <c>EasyEngineBase.ParamHhmm</c>. Questi test difendono quel punto e il fatto
/// che il default sia il comportamento <i>corretto</i>: chi dimentica di configurare qualcosa
/// ottiene la conversione giusta, non quella vecchia.</para>
/// </summary>
public sealed class BarLabelTests
{
    // Finestra 09:00-17:00 nell'orologio della ricerca (Europe/Rome: d'inverno UTC+1), strategia a
    // 60 minuti. La barra che apre alle 08:00 locali chiude alle 09:00 ed è la prima dentro per la
    // ricerca; quella che apre alle 17:00 chiude alle 18:00 ed è la prima fuori.
    private static readonly DateTime ChiudeSullApertura = Utc(7);
    private static readonly DateTime DentroInEntrambe = Utc(8);
    private static readonly DateTime ApreSullaChiusura = Utc(16);

    [Fact]
    public void TheWindowReadsTheBarCloseLabel()
    {
        var strategy = new WindowProbe();

        Assert.True(strategy.IsInWindow(ChiudeSullApertura));
        Assert.True(strategy.IsInWindow(DentroInEntrambe));
        Assert.False(strategy.IsInWindow(ApreSullaChiusura));
    }

    /// <summary>
    /// L'interruttore esiste per una cosa sola: rimettere a confronto un run archiviato con uno
    /// nuovo. Acceso, la finestra torna a leggere l'apertura e si sposta di una barra in avanti.
    /// </summary>
    [Fact]
    public void LegacyLabelsShiftTheWindowForwardByOneBar()
    {
        var strategy = new WindowProbe { LegacyBarOpenLabels = true };

        Assert.False(strategy.IsInWindow(ChiudeSullApertura));
        Assert.True(strategy.IsInWindow(DentroInEntrambe));
        Assert.True(strategy.IsInWindow(ApreSullaChiusura));
    }

    /// <summary>
    /// <c>skip_day</c> legge lo stesso nome della finestra. Su una barra da 4h il giorno cambia su
    /// un bucket su sei; su una <b>giornaliera cambia sempre</b> — apre lunedì a mezzanotte e chiude
    /// martedì a mezzanotte — ed è il caso che nessun test copriva.
    /// </summary>
    [Fact]
    public void TheWeekdayFilterReadsTheSameLabelAsTheWindow()
    {
        // Lunedì 2025-02-10, apertura 23:00 UTC di domenica = mezzanotte locale di lunedì.
        var giornaliera = new DateTime(2025, 2, 9, 23, 0, 0, DateTimeKind.Utc);

        // 0 = lunedì nella convenzione pandas. L'apertura è lunedì, la chiusura martedì.
        Assert.Equal(1, new DailyProbe().Weekday(giornaliera));
        Assert.Equal(0, new DailyProbe { LegacyBarOpenLabels = true }.Weekday(giornaliera));
    }

    /// <summary>
    /// I minuti si sommano in UTC e la conversione al fuso viene dopo: sull'ora che al ritorno
    /// dell'ora solare esiste due volte le due strade divergono, ed è il posto in cui l'errore non
    /// si vedrebbe mai.
    /// </summary>
    [Fact]
    public void TheLabelAddsTheTimeframeInUtcNotOnTheLocalClock()
    {
        // 26/10/2025: le 00:00 UTC sono le 02:00 locali, e l'ora locale 02:00-03:00 esiste due
        // volte. La barra che apre alle 00:00 UTC chiude alle 01:00 UTC, cioè alle 02:00 locali
        // della SECONDA occorrenza — non alle 03:00.
        var ambigua = new DateTime(2025, 10, 26, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(200, new WindowProbe().WindowHhmm(ambigua));
    }

    private static DateTime Utc(int hour) => new(2025, 2, 10, hour, 0, 0, DateTimeKind.Utc);

    /// <summary>Il minimo che serve per interrogare la finestra, senza gate né ordini.</summary>
    private class WindowProbe : EasyEngineBase
    {
        public WindowProbe()
        {
            // Orari della ricerca, verbatim, come li scrive una PTS_*.
            TradingWindow = ZonedWindow.ResearchHours(9, 17);
        }

        public override string Name => "TEST_WINDOW";
        public override string Description => "Sonda sull'etichetta della barra";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 1;

        public bool IsInWindow(DateTime barTime) => InDeclaredWindow(barTime) ?? false;
        public int WindowHhmm(DateTime barTime) => WindowParamHhmm(barTime);
        public int Weekday(DateTime barTime) => PythonWeekday(barTime);

        public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
            Hold(0m, currentDate);
    }

    private sealed class DailyProbe : WindowProbe
    {
        public override int TimeframeMinutes => 1440;
    }
}
