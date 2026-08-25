using System.Reflection;
using System.Text.RegularExpressions;
using Piootoo.Shared;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La versione del progetto è una sola e sta in tre punti che devono muoversi insieme: la
/// costante <see cref="PiootooVersion.Current"/> (server e console WinForms), la
/// <c>VersionPrefix</c> di <c>Directory.Build.props</c> (gli assembly), e <c>BotVersion</c> del
/// cBot distribuito.
///
/// <para>Due di quei tre sono verificabili da qui e lo sono: se qualcuno bumpa la costante e si
/// dimentica il file di build — o viceversa — il test fallisce subito, invece di lasciare in giro
/// binari stampati con un numero e un server che ne dichiara un altro.</para>
///
/// <para>Il terzo, il cBot, resta fuori portata: lo compila cTrader, che non referenzia queste
/// assembly, quindi non esiste un modo di leggerlo a compile time. Qui si controlla almeno che il
/// sorgente del bot nel repository porti lo stesso numero, che è il massimo che si può fare senza
/// costruirlo.</para>
/// </summary>
public sealed class VersioneDelProgettoTests
{
    [Fact]
    public void LaVersioneDellAssemblyCoincideConQuellaDichiarataDalServer()
    {
        var informational = typeof(PiootooVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        Assert.False(string.IsNullOrWhiteSpace(informational),
            "Piootoo.Shared non ha InformationalVersion: manca Directory.Build.props?");

        // Il SDK puo' accodare il commit ('3.7.0+abc1234'): conta la parte prima del '+'.
        var versione = informational!.Split('+')[0];

        Assert.Equal(PiootooVersion.Current, versione);
    }

    [Fact]
    public void IlCBotDistribuitoDichiaraLaStessaVersioneDelServer()
    {
        var sorgente = Path.Combine(
            RadiceRepository(),
            "piootoo-repository", "ctrader", "PiootooDistributedExecutionBot.cs");

        Assert.True(File.Exists(sorgente), $"Sorgente del cBot non trovato: {sorgente}");

        var match = Regex.Match(
            File.ReadAllText(sorgente),
            @"BotVersion\s*=\s*""(?<version>[^""]+)""");

        Assert.True(match.Success,
            "BotVersion non trovata in PiootooDistributedExecutionBot: e' stata rinominata?");

        Assert.Equal(PiootooVersion.Current, match.Groups["version"].Value);
    }

    private static string RadiceRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                $"PiootooApp.sln non trovata risalendo da {AppContext.BaseDirectory}.");
    }
}
