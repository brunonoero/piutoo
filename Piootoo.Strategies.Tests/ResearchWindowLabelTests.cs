using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La finestra operativa e l'etichetta della barra.
///
/// <para><b>Il difetto.</b> Il feed Piootoo etichetta ogni barra sull'<b>apertura</b>; il motore di
/// ricerca da cui <c>start_hour</c>/<c>end_hour</c> vengono lavora su barre etichettate sulla
/// <b>chiusura</b> — le righe del vendor sono la fine del minuto, e il resample etichetta il bucket
/// a <c>inizio + Δ</c> — e <c>filters.py</c> confronta la finestra con <i>quella</i> etichetta.
/// Confrontarla con l'apertura sposta la finestra di <b>una barra in avanti</b>: si prende la barra
/// dopo la fine e si perde quella prima dell'inizio. La misura indipendente sta nel report della
/// ricerca: allineando i suoi <c>entry_time</c> ai nostri <c>entryTimeUtc</c> su NQ 15m il massimo
/// di corrispondenze è a −15 minuti, 345 contro 78 a offset nullo.</para>
///
/// <para><b>Perché un interruttore e non una correzione.</b> Accenderlo cambia quali segnali
/// nascono su 83 delle 124 <c>PTS_*</c>. Va misurato su due run identici che differiscono solo per
/// questo campo, e finché la misura non c'è il default resta il comportamento storico.</para>
/// </summary>
public sealed class ResearchWindowLabelTests
{
    // Finestra 09:00-17:00 nell'orologio della ricerca (Europe/Rome: d'inverno UTC+1), strategia a
    // 60 minuti. La barra che apre alle 08:00 locali chiude alle 09:00 e per la ricerca è la prima
    // dentro; quella che apre alle 17:00 chiude alle 18:00 ed è la prima fuori.
    private static readonly DateTime PrimaDellApertura = Utc(7);
    private static readonly DateTime SullApertura = Utc(8);
    private static readonly DateTime SullaChiusura = Utc(16);

    [Fact]
    public void ByDefault_TheWindowReadsTheBarOpenLabel()
    {
        var strategy = new WindowProbe();

        Assert.False(strategy.IsInWindow(PrimaDellApertura));
        Assert.True(strategy.IsInWindow(SullApertura));
        Assert.True(strategy.IsInWindow(SullaChiusura));
    }

    [Fact]
    public void OnBarClose_TheWindowShiftsBackByExactlyOneBar()
    {
        var strategy = new WindowProbe { WindowsOnBarClose = true };

        // La barra che la ricerca chiama "09:00" entra, quella che chiama "18:00" esce.
        Assert.True(strategy.IsInWindow(PrimaDellApertura));
        Assert.True(strategy.IsInWindow(SullApertura));
        Assert.False(strategy.IsInWindow(SullaChiusura));
    }

    /// <summary>
    /// I minuti si sommano in UTC e l'<c>Hhmm</c> si prende dopo. Sull'ultima barra prima del
    /// ritorno all'ora solare le due strade divergono: sommare sull'orario locale darebbe un'ora
    /// diversa, ed è il posto in cui l'errore non si vedrebbe mai.
    /// </summary>
    [Fact]
    public void OnBarClose_AddsTheTimeframeInUtc_NotOnTheLocalClock()
    {
        var strategy = new WindowProbe { WindowsOnBarClose = true };

        // 26/10/2025, ritorno all'ora solare europea: le 00:00 UTC sono le 02:00 locali, e l'ora
        // locale 02:00-03:00 esiste due volte. La barra che apre alle 00:00 UTC chiude alle 01:00
        // UTC, cioè alle 02:00 locali della SECONDA occorrenza — non alle 03:00.
        var ambigua = new DateTime(2025, 10, 26, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(200, strategy.WindowHhmm(ambigua));
    }

    private static DateTime Utc(int hour) => new(2025, 2, 10, hour, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Il minimo che serve per interrogare la finestra: una strategia vera porterebbe con sé gate,
    /// pattern e ordini, che qui non c'entrano.
    /// </summary>
    private sealed class WindowProbe : EasyEngineBase
    {
        public WindowProbe()
        {
            // Orari della ricerca, verbatim, come li scrive una PTS_*.
            TradingWindow = ZonedWindow.ResearchHours(9, 17);
        }

        public override string Name => "TEST_WINDOW";
        public override string Description => "Sonda sulla finestra operativa";

        // Ancoraggio 0 e fuso della ricerca: d'inverno l'ora locale è UTC+1.
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 1;

        public bool IsInWindow(DateTime barTime) => InDeclaredWindow(barTime) ?? false;

        public int WindowHhmm(DateTime barTime) => WindowClock.Hhmm(WindowInstant(barTime));

        public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
            Hold(0m, currentDate);
    }
}
