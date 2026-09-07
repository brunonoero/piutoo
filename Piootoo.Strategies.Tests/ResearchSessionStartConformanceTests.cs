using System.Reflection;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'ora di inizio della sessione di ricerca è una proprietà dello <b>strumento</b>, non una scelta
/// della singola classe.
///
/// <para><b>Perché questo test esiste.</b> La tabella §2.4 del dossier dà 01:00 CET a sei mercati —
/// CC, CT, FDAX, HK, KC, SB — e 00:00 a tutti gli altri. HK e HO sono usciti dal registro il
/// 07/09/2026 con le loro PTS, quindi le liste qui sotto non li nominano piu'. `InstrumentSpec.ResearchSessionStartHour`
/// riporta quella tabella, ma non lo leggeva nessuno: le sette FDAX dichiaravano
/// <c>ResearchSession()</c> invece di <c>ResearchSession(1)</c>, cioè la sessione sbagliata sui due
/// maggiori contributori del paniere per P&amp;L fuori campione. Il documento
/// <c>domini/mappa-strategie-pts.md</c> affermava che la forma era «imposta da
/// StrategyClockConformanceTests»: quel test verifica che <i>un</i> fuso sia dichiarato, non
/// <i>quale</i> ora. È la differenza fra una convenzione scritta e una convenzione imposta.</para>
///
/// <para>Le classi su simboli non ancora verificati nel registro (JY e gli altri di
/// <c>KnownButUnverified</c>) non sono confrontabili con niente e vengono contate a parte: sono un
/// buco noto del registro, non un porting da correggere.</para>
/// </summary>
public sealed class ResearchSessionStartConformanceTests
{
    [Theory]
    [MemberData(nameof(PtsStrategyTypes))]
    public void OgniPtsDichiaraLOraDiSessioneDelProprioStrumento(Type type)
    {
        var strategy = (ITradingStrategy)Activator.CreateInstance(type)!;
        if (!InstrumentRegistry.TryGet(strategy.Symbol, out var spec))
            return; // simbolo non verificato: vedi il sommario

        var session = LeggiProtetta<ZonedWindow>(strategy, "Session");
        Assert.NotNull(session);

        var attesa = spec.ResearchSessionStartHour * 100;
        Assert.True(
            session!.StartHhmm == attesa,
            $"{type.Name}: sessione dichiarata a {session.StartHhmm:0000} ma " +
            $"{InstrumentRegistry.Normalize(strategy.Symbol)} apre a {attesa:0000} nell'orologio " +
            "della ricerca (tabella §2.4 del dossier, InstrumentSpec.ResearchSessionStartHour). " +
            "Usa ZonedWindow.ResearchSession(ora).");

        // La forma dev'essere quella a giornata piena: una sessione (ancoraggio, 2359) è ciò che
        // EasyLib.OHLCMulti5 riconosce come taglio della ricerca. Con una fine diversa ricadrebbe
        // sul percorso delle sessioni di borsa, dove le barre fuori orario non appartengono a
        // nessuna sessione.
        Assert.Equal(2359, session.EndHhmm);
        Assert.Equal(ZonedWindow.ResearchTimeZone, session.TimeZoneId);
    }

    /// <summary>
    /// I sei mercati che aprono all'01:00 sono esattamente quelli della tabella. Vale sul registro,
    /// non sulle strategie: è la tabella stessa a dover restare fedele al dossier, altrimenti il
    /// test sopra imporrebbe con precisione un numero sbagliato.
    /// </summary>
    [Fact]
    public void IlRegistroRiproduceLaTabellaDelDossier()
    {
        string[] alleUnaCet = ["CC", "CT", "FDAX", "KC", "SB"];

        foreach (var symbol in alleUnaCet)
        {
            Assert.True(InstrumentRegistry.TryGet(symbol, out var spec), $"{symbol} assente dal registro");
            Assert.Equal(1, spec.ResearchSessionStartHour);
        }

        foreach (var symbol in new[] { "BP", "BTC", "CL", "ES", "GC", "NG", "NQ", "PL", "YM" })
        {
            Assert.True(InstrumentRegistry.TryGet(symbol, out var spec), $"{symbol} assente dal registro");
            Assert.Equal(0, spec.ResearchSessionStartHour);
        }
    }

    private static T? LeggiProtetta<T>(object instance, string name) where T : class
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var property = type.GetProperty(
                name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (property is not null)
                return property.GetValue(instance) as T;
        }

        return null;
    }

    public static TheoryData<Type> PtsStrategyTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            var assembly = typeof(Piootoo.Strategies.Easy.EasyLib).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (type is { IsClass: true, IsAbstract: false } &&
                    type.Name.StartsWith("PTS_", StringComparison.Ordinal) &&
                    typeof(ITradingStrategy).IsAssignableFrom(type))
                {
                    data.Add(type);
                }
            }

            return data;
        }
    }
}
