using System.Text.RegularExpressions;
using Piootoo.Core.Optimization.Sweep;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;
using Xunit;
using Xunit.Abstractions;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'ora di uscita di sessione (<c>SessionExitTime</c>, <c>ExitHour</c> nella sweep).
///
/// <para><b>Cosa impone.</b> Senza un'ora propria nulla cambia: la posizione chiude alla fine
/// della sessione, com'e' sempre stato e come fa il motore di ricerca. Con un'ora propria la
/// deadline cade li', e l'ingresso che nascerebbe <b>dopo</b> quell'ora non nasce affatto.</para>
///
/// <para><b>A cosa serve.</b> La sessione della ricerca e' il giorno di calendario europeo: per
/// FDAX finisce alle 00:59, cioe' dopo il rollover del broker (21:00 ICS, 20:59 FTMO). Una PC
/// dichiarata intraday paga percio' il finanziamento ogni giorno — su
/// <c>PT3B_FDAX_PCH_001_240</c> erano 879 trade su 1.317 e il 28% del lordo. Vedi
/// <c>docs/domini/ricerca-parametri.md</c> e <c>compare/compare-0048/esito.md</c>.</para>
///
/// <para><b>Vale per ogni motore, dal 23/09/2026.</b> Fino a quel giorno la leggeva il solo Price
/// Channel: il campo stava su <c>EasyEngineBase</c> e ogni classe poteva dichiararlo, ma sei motori
/// su sette chiudevano su <c>SessionEnd</c> e basta, e la griglia TF del 22/09 ha misurato 25
/// combinazioni credendo di misurarne 250. Ora la risolve un punto solo,
/// <c>EasyEngineBase.WithSessionExit</c>, e i test sul trend following qui sotto sono gli stessi
/// del Price Channel, con lo stesso esito.</para>
/// </summary>
public sealed class SessionExitHourTests(ITestOutputHelper output)
{
    /// <summary>
    /// Il trend following chiude alla stessa ora del Price Channel: le 20:00 di Roma sono le 19:00Z
    /// in gennaio. Prima del 23/09/2026 questo test avrebbe trovato la fine sessione, perche' il
    /// motore non leggeva il campo.
    /// </summary>
    [Fact]
    public void ATrendFollowingClosesAtItsExitHourLikeThePriceChannel()
    {
        var bars = BuildFourHourBars(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
        var signal = EvaluateTf(new IntradayTrendFollowing { ExitHour = new TimeOnly(20, 0) }, bars);

        // Mirrored col motore nudo: nascono entrambi i lati, il long e' il primario e lo short
        // viaggia come compagno. Devono chiudere allo stesso istante.
        Assert.Equal(SignalType.Buy, signal.Type);
        var atteso = new DateTime(2026, 1, 15, 19, 0, 0, DateTimeKind.Utc);
        Assert.Equal(atteso, signal.CloseAtUtc);
        var compagno = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Sell, compagno.Type);
        Assert.Equal(atteso, compagno.CloseAtUtc);
    }

    /// <summary>Senza l'ora propria il trend following chiude a fine sessione, come ha sempre fatto.</summary>
    [Fact]
    public void ATrendFollowingWithoutAnExitHourStillClosesAtTheEndOfTheSession()
    {
        var bars = BuildFourHourBars(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
        var signal = EvaluateTf(new IntradayTrendFollowing(), bars);

        Assert.Equal(SignalType.Buy, signal.Type);
        var grid = new SessionGrid(MarketCalendarRegistry.Current.Get("@FDAX"));
        var sessionClose = grid.SessionOpenUtc(grid.SessionDayOf(signal.ValidFromUtc!.Value).AddDays(1));
        Assert.Equal(sessionClose.AddMinutes(-1), signal.CloseAtUtc);
    }

    /// <summary>
    /// Anche sul trend following l'ingresso che nascerebbe dopo la propria ora di uscita non nasce:
    /// e' lo scarto di <c>WithSessionExit</c>, e vale per entrambi i lati.
    /// </summary>
    [Fact]
    public void ATrendFollowingEntryBornAfterItsExitHourIsNotEmitted()
    {
        var bars = BuildFourHourBars(new DateTime(2026, 1, 15, 16, 0, 0, DateTimeKind.Utc));

        var senzaOra = EvaluateTf(new IntradayTrendFollowing(), bars);
        Assert.Equal(SignalType.Buy, senzaOra.Type);

        var conOra = EvaluateTf(new IntradayTrendFollowing { ExitHour = new TimeOnly(20, 0) }, bars);
        Assert.Equal(SignalType.Hold, conOra.Type);
    }

    /// <summary>
    /// Toglie commenti di riga: il vincolo e' sul codice, e una spiegazione che nomina la forma
    /// vietata per dire di non usarla non e' una violazione.
    /// </summary>
    private static readonly Regex SessionEndDeadline = new(
        @"ResolveCloseAtUtc\s*\([^;]*\bSessionEnd\b", RegexOptions.Compiled);

    /// <summary>
    /// <b>La deadline di fine sessione la risolve un punto solo.</b> Un motore che chiamasse
    /// <c>ResolveCloseAtUtc(..., SessionEnd)</c> per conto proprio ignorerebbe <c>SessionExitTime</c>
    /// e riaprirebbe il difetto del 22/09: una classe che dichiara l'ora di uscita e non la ottiene,
    /// in silenzio. Il controllo e' sul sorgente perche' e' li' che il difetto nasce, e perche' un
    /// motore nuovo lo erediterebbe copiando <c>WithPythonSettings</c> da uno vecchio.
    /// </summary>
    [Fact]
    public void EveryEngineResolvesTheSessionExitInOnePlace()
    {
        var root = FindRepositoryRoot();
        var engines = Path.Combine(root, "Piootoo.Strategies", "Easy", "Engines");
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(engines, "*.cs"))
        {
            if (Path.GetFileName(file) == "EasyEngineBase.cs") continue;

            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var trimmed = lines[index].TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
                var code = lines[index].Split("//", 2)[0];
                if (SessionEndDeadline.IsMatch(code))
                    violations.Add($"{Path.GetRelativePath(root, file)}({index + 1}): {trimmed}");
            }
        }

        Assert.True(violations.Count == 0,
            "Un motore non risolve la deadline di fine sessione da solo: passa da " +
            "EasyEngineBase.WithSessionExit, che legge SessionExitTime per tutti." +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException($"PiootooApp.sln non trovata risalendo da {AppContext.BaseDirectory}.");
    }

    /// <summary>
    /// Il default non cambia niente: senza <c>SessionExitTime</c> la deadline resta l'ultimo minuto
    /// della sessione. E' la garanzia che questo parametro non tocchi le strategie gia' portate.
    /// </summary>
    [Fact]
    public void WithoutAnExitHourTheDeadlineIsStillTheEndOfTheSession()
    {
        var bars = BuildFourHourBars(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
        var signal = Evaluate(new IntradayPriceChannel(), bars);

        Assert.Equal(SignalType.Buy, signal.Type);

        var grid = new SessionGrid(MarketCalendarRegistry.Current.Get("@FDAX"));
        var sessionDay = grid.SessionDayOf(signal.ValidFromUtc!.Value);
        var sessionClose = grid.SessionOpenUtc(sessionDay.AddDays(1));

        output.WriteLine($"valido dalle {signal.ValidFromUtc:yyyy-MM-dd HH:mm}Z");
        output.WriteLine($"chiusura     {signal.CloseAtUtc:yyyy-MM-dd HH:mm}Z");
        output.WriteLine($"fine sessione{sessionClose:yyyy-MM-dd HH:mm}Z");

        Assert.Equal(sessionClose.AddMinutes(-1), signal.CloseAtUtc);
    }

    /// <summary>
    /// Con un'ora propria la deadline cade a quell'ora della sessione dell'ordine, e non alla fine.
    /// Le 20:00 di Roma sono le 19:00Z in gennaio: prima del rollover, che e' il punto.
    /// </summary>
    [Fact]
    public void AnExitHourClosesThePositionAtThatHourOfItsOwnSession()
    {
        var bars = BuildFourHourBars(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
        var signal = Evaluate(new IntradayPriceChannel { ExitHour = new TimeOnly(20, 0) }, bars);

        Assert.Equal(SignalType.Buy, signal.Type);

        var grid = new SessionGrid(MarketCalendarRegistry.Current.Get("@FDAX"));
        var sessionClose = grid.SessionOpenUtc(grid.SessionDayOf(signal.ValidFromUtc!.Value).AddDays(1));

        output.WriteLine($"chiusura      {signal.CloseAtUtc:yyyy-MM-dd HH:mm}Z");
        output.WriteLine($"fine sessione {sessionClose:yyyy-MM-dd HH:mm}Z");

        Assert.NotNull(signal.CloseAtUtc);
        Assert.True(signal.CloseAtUtc < sessionClose.AddMinutes(-1),
            "la chiusura deve cadere prima della fine sessione, altrimenti il parametro non fa nulla");
        Assert.Equal(new DateTime(2026, 1, 15, 19, 0, 0, DateTimeKind.Utc), signal.CloseAtUtc);
    }

    /// <summary>
    /// L'ingresso che nascerebbe dopo la propria ora di chiusura non nasce.
    ///
    /// <para>E' il caso che rende il parametro onesto: la barra delle 21:00 locali appartiene ancora
    /// alla sessione del giorno, ma un ordine con uscita alle 20:00 non potrebbe stare aperto
    /// nemmeno un minuto. Rimandare la deadline alla sessione successiva — che e' il comportamento
    /// storico di <c>ResolveCloseAtUtc</c> — trasformerebbe in overnight proprio la posizione che
    /// quell'ora esiste per chiudere prima della notte, e la <c>Holding</c> dichiarata dalla
    /// strategia non sarebbe piu' vera.</para>
    /// </summary>
    [Fact]
    public void AnEntryBornAfterItsOwnExitHourIsNotEmitted()
    {
        // Ultima barra alle 16:00Z: l'ordine "next bar" e' valido dalle 20:00Z, cioe' le 21:00 di
        // Roma in gennaio — un'ora oltre l'uscita, e ancora dentro la sessione del 15 (che per FDAX
        // va dalle 00:00Z del 15 alle 00:00Z del 16). Sulla barra DOPO, quella delle 20:00Z,
        // l'ordine sarebbe valido dalle 00:00Z e apparterrebbe gia' alla sessione nuova: li' le
        // 20:00 sono un appuntamento futuro e l'ingresso e' legittimo.
        var bars = BuildFourHourBars(new DateTime(2026, 1, 15, 16, 0, 0, DateTimeKind.Utc));

        var senzaOra = Evaluate(new IntradayPriceChannel(), bars);
        Assert.Equal(SignalType.Buy, senzaOra.Type);

        var conOra = Evaluate(new IntradayPriceChannel { ExitHour = new TimeOnly(20, 0) }, bars);

        output.WriteLine($"senza ora di uscita: {senzaOra.Type}, chiude {senzaOra.CloseAtUtc:yyyy-MM-dd HH:mm}Z");
        output.WriteLine($"con uscita 20:00:    {conOra.Type}");

        Assert.Equal(SignalType.Hold, conOra.Type);
    }

    /// <summary>
    /// Una multiday (<c>IntradayOnly = false</c>) non ha uscita di sessione, quindi l'ora non la
    /// tocca: il parametro e' inerte e non deve inventare una deadline che la strategia non vuole.
    /// </summary>
    [Fact]
    public void AMultidayStrategyIgnoresTheExitHour()
    {
        var bars = BuildFourHourBars(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
        var signal = Evaluate(
            new IntradayPriceChannel { Multiday = true, ExitHour = new TimeOnly(20, 0) }, bars);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Null(signal.CloseAtUtc);
    }

    /// <summary>
    /// Lo spazio di ricerca: <c>ExitHour</c> esiste a timeframe intraday, la sentinella e' il
    /// <b>primo</b> valore — cosi' la sweep parte dal comportamento del motore di ricerca — e la sua
    /// fase gira sull'orologio fitto. Senza quest'ultima, una chiusura alle 20:00 misurata a 240
    /// minuti cadrebbe sulla barra dopo: la misura direbbe il contrario del vero proprio sul costo
    /// che il parametro esiste per togliere.
    /// </summary>
    [Fact]
    public void TheSweepSpaceDeclaresExitHourWithItsSentinelFirstAndOnTheAccurateClock()
    {
        var space = SweepSpaces.PriceChannel(240);

        var parameter = Assert.Single(space.Parameters, p => p.Key == "ExitHour");
        Assert.Equal(-1, Convert.ToInt32(parameter.Values[0]));
        Assert.Equal(-1, Convert.ToInt32(parameter.OffSentinel!));

        var phase = Assert.Single(space.Phases, p => p.Keys.Contains("ExitHour"));
        Assert.True(phase.RequiresAccurateClock,
            "l'ora di uscita si misura sull'orologio al minuto, o il finanziamento risparmiato non si vede");

        var orari = space.Phases.ToList().FindIndex(p => p.Keys.Contains("StartHour"));
        var uscita = space.Phases.ToList().FindIndex(p => p.Keys.Contains("ExitHour"));
        var stop = space.Phases.ToList().FindIndex(p => p.Keys.Contains("StopLoss"));
        Assert.True(orari < uscita && uscita < stop,
            "la durata si sceglie dopo gli orari e prima dello stop, che le si tara addosso");
    }

    /// <summary>
    /// Su daily l'uscita di sessione non si applica (regola di parita' del motore di ricerca),
    /// quindi il parametro non deve nemmeno comparire: sarebbe una fase intera spesa a permutare
    /// qualcosa di inerte.
    /// </summary>
    [Fact]
    public void TheDailySweepSpaceHasNoExitHour()
    {
        var space = SweepSpaces.PriceChannel(1440);

        Assert.DoesNotContain(space.Parameters, p => p.Key == "ExitHour");
        Assert.DoesNotContain(space.Phases, p => p.Keys.Contains("ExitHour"));
    }

    private static TradeSignal Evaluate(PriceChannelEngine strategy, OhlcvData[] bars) =>
        strategy.Evaluate(Request(strategy, bars));

    private static TradeSignal EvaluateTf(TfEngineBase strategy, OhlcvData[] bars) =>
        strategy.Evaluate(Request(strategy, bars));

    private static StrategyEvaluationRequest Request(EasyEngineBase strategy, OhlcvData[] bars) =>
        new()
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "FDAX",
                BarTimeUtc = bars[^1].DateTime
            }
        };

    /// <summary>
    /// Barre da 4 ore piatte, con l'ultima che allarga il canale verso l'alto: basta a far nascere
    /// un solo ingresso long, che e' tutto quello che serve per guardarne la deadline.
    /// </summary>
    private static OhlcvData[] BuildFourHourBars(DateTime lastTime)
    {
        var bars = new OhlcvData[300];
        for (var index = 0; index < bars.Length; index++)
        {
            bars[index] = new OhlcvData
            {
                DateTime = lastTime.AddHours((index - bars.Length + 1) * 4),
                Open = 100m,
                High = 110m,
                Low = 100m,
                Close = 105m,
                Volume = 1m
            };
        }

        return bars;
    }

    private sealed class IntradayPriceChannel : PriceChannelEngine
    {
        public IntradayPriceChannel()
        {
            ChannelBars = 3;
            TickSize = 0.5m;
            IntradayOnly = true;
            Direction = 1; // solo long: un segnale per barra, piu' semplice da leggere
        }

        public TimeOnly? ExitHour { set => SessionExitTime = value; }
        public bool Multiday { set => IntradayOnly = !value; }

        public override string Name => "TEST_EXITHOUR_PC_FDAX_240";
        public override string Description => "Price Channel di prova con ora di uscita";
        public override string Symbol => "@FDAX";
        public override int TimeframeMinutes => 240;
    }

    /// <summary>
    /// Trend following mirrored nudo: le sentinelle dei pattern fanno passare entrambi i lati, e
    /// sulle barre piatte del test l'estremo della sessione precedente e' un livello valido.
    /// </summary>
    private sealed class IntradayTrendFollowing : TfMirroredEngine
    {
        public IntradayTrendFollowing()
        {
            IntradayOnly = true;
            SkipDay = -1;
        }

        public TimeOnly? ExitHour { set => SessionExitTime = value; }

        public override string Name => "TEST_EXITHOUR_TF_FDAX_240";
        public override string Description => "Trend following di prova con ora di uscita";
        public override string Symbol => "@FDAX";
        public override int TimeframeMinutes => 240;
    }
}
