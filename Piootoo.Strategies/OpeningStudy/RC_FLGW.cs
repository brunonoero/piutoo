using System.Globalization;
using Piootoo.Shared.Configuration;

namespace Piootoo.Strategies.ResearchContainers;

// Studio dell'apertura (30/09/2026): vedi RC_ORB.cs e docs/domini/studio-apertura.md.

/// <summary>
/// La bandiera di <c>RC_FLG</c> cercata solo in una finestra oraria scritta <b>in ora di borsa</b> e al
/// minuto: <c>WindowStartHhmm</c> e <c>WindowEndHhmm</c> (09:00-11:00 di Berlino per FDAX, 08:30-10:30 di
/// Chicago per NQ). La finestra vale sulla barra di segnale, estremi inclusi; l'ordine vive sulla barra dopo.
///
/// <para><b>Perche' un contenitore a parte.</b> <c>StartHour</c>/<c>EndHour</c> della griglia sono ore
/// piene di Roma: l'apertura di New York alle 09:30 non si scrive, e in Roma si sposterebbe di un'ora
/// nelle settimane in cui le ore legali non sono allineate. Senza finestra e' identico a <c>RC_FLG</c>, che
/// resta il controllo sulla stessa cella.</para>
/// </summary>
public sealed class RC_FLGW : PT7CLPStrategies.Engines.FlagEngine
{
    private readonly ResearchContainerIdentity _identity = new();
    private int _windowStartHhmm = -1;
    private int _windowEndHhmm = -1;

    public override string Name => "RC_FLGW";
    public override string Description => "Contenitore generico di ricerca: bandiera in una finestra di borsa";
    public override string Symbol => _identity.Symbol;
    public override int TimeframeMinutes => _identity.TimeframeMinutes;
    public override bool IsResearchContainer => true;

    /// <summary>Inizio della finestra, <c>HHMM</c> in ora di borsa; -1 = nessuna finestra.</summary>
    private int WindowStartHhmm
    {
        set => _windowStartHhmm = value;
    }

    /// <summary>Fine della finestra, inclusa, <c>HHMM</c> in ora di borsa; -1 = nessuna finestra.</summary>
    private int WindowEndHhmm
    {
        set => _windowEndHhmm = value;
    }

    public RC_FLGW()
    {
        ResearchContainerSettings.Prepare(this);
        PoleBars = 5;
        PoleAtr = 3m;
        AtrBars = 20;
        FlagMinBars = 3;
        FlagMaxBars = 10;
        MaxRetrace = 0.5m;
        ParallelTolerance = 0.1m;
        OffsetTicks = 0;
        StopAtFlag = 0;
        ExtremeBufferTicks = 0;
        TargetPole = 0m;
        Direction = 0;
        MaxEntriesPerSession = 1;
    }

    /// <summary>
    /// Applica le leve, poi la finestra: dopo, e non dentro l'ordine dei parametri, perche' la griglia
    /// passa sempre <c>StartHour</c>/<c>EndHour</c> a -1 e quelle, applicate dopo la finestra, la
    /// riporterebbero a tutta la giornata in silenzio.
    /// </summary>
    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        ResearchContainerSettings.Apply(this, _identity, parameters);

        if (_windowStartHhmm < 0 && _windowEndHhmm < 0)
            return;

        if (_windowStartHhmm < 0 || _windowEndHhmm < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(WindowStartHhmm),
                $"{Name}: la finestra di borsa vuole inizio e fine ({_windowStartHhmm}, {_windowEndHhmm}).");
        }

        if (parameters is not null && (IsOn(parameters, "StartHour") || IsOn(parameters, "EndHour")))
        {
            throw new ArgumentException(
                $"{Name}: StartHour/EndHour (ore di Roma) e WindowStartHhmm/WindowEndHhmm (ora di borsa) insieme: " +
                "due finestre in due orologi, se ne dichiara una.");
        }

        TradingWindow = ZonedWindow.Exchange(TimeFromLegacyHhmm(_windowStartHhmm), TimeFromLegacyHhmm(_windowEndHhmm));
    }

    private static bool IsOn(Dictionary<string, object> parameters, string key) =>
        parameters.TryGetValue(key, out var value) && System.Convert.ToInt32(value, CultureInfo.InvariantCulture) >= 0;
}
