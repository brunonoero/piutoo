using Piootoo.Core.Services;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'universo operativo di un conto: quali simboli puo' davvero operare, e quindi quali strategie
/// gli vengono consegnate.
///
/// <para>La distinzione che questi test tengono ferma e' fra <c>IsSymbolEnabled</c> e
/// <c>SupportsSymbol</c>. Il primo risponde "la riga non lo vieta", e un simbolo <b>assente</b> dalla
/// tabella non lo vieta: passerebbe 1 a 1, cioe' con il contratto Piootoo su un broker che quello
/// strumento non lo ha mappato. Il secondo risponde "la tabella lo prevede?", ed e' la domanda che
/// decide se una strategia gira o viene ignorata.</para>
/// </summary>
public sealed class AccountUniverseTests
{
    /// <summary>
    /// Un conto senza tabella di conversione opera tutto: e' il conto neutro, ed e' la
    /// configurazione di ogni account non ancora mappato. Trattarlo come "non supporta niente"
    /// spegnerebbe l'operativita' di quei conti senza che nessuno abbia toccato una riga.
    /// </summary>
    [Fact]
    public void UnContoSenzaTabella_SupportaTutto()
    {
        var conversion = Conversion();

        Assert.False(conversion.HasSymbolTable);
        Assert.True(conversion.SupportsSymbol("@NQ"));
        Assert.True(conversion.SupportsSymbol("@QUALSIASI"));
    }

    /// <summary>
    /// Con una tabella, un simbolo che non compare non e' supportato — ed e' proprio il caso in cui
    /// <c>IsSymbolEnabled</c> direbbe di si', perche' nessuna riga lo vieta.
    /// </summary>
    [Fact]
    public void UnSimboloAssenteDallaTabella_NonESupportato_MaNonEDisabilitato()
    {
        var conversion = Conversion(Mapping("@NQ", enabled: true));

        Assert.True(conversion.HasSymbolTable);
        Assert.True(conversion.SupportsSymbol("@NQ"));

        Assert.False(conversion.SupportsSymbol("@GC"));
        // La differenza fra le due domande, in una riga: se fossero la stessa cosa @GC girerebbe.
        Assert.True(conversion.IsSymbolEnabled("@GC"));
    }

    /// <summary>Un simbolo mappato ma disabilitato non e' operativo: la riga lo vieta esplicitamente.</summary>
    [Fact]
    public void UnSimboloDisabilitato_NonESupportato()
    {
        var conversion = Conversion(Mapping("@NQ", enabled: false));

        Assert.False(conversion.SupportsSymbol("@NQ"));
        Assert.False(conversion.IsSymbolEnabled("@NQ"));
    }

    /// <summary>
    /// Stessa normalizzazione del resto del sistema: <c>@NQ</c>, <c>nq</c> e <c>NQ</c> sono lo
    /// stesso simbolo. Una tabella scritta in un verso e un catalogo nell'altro non devono produrre
    /// un universo vuoto.
    /// </summary>
    [Theory]
    [InlineData("@NQ")]
    [InlineData("NQ")]
    [InlineData("nq")]
    [InlineData("  @nq  ")]
    public void IlConfrontoSuiSimboli_IgnoraChiocciolaEMaiuscole(string symbol)
    {
        var conversion = Conversion(Mapping("nq", enabled: true));

        Assert.True(conversion.SupportsSymbol(symbol));
    }

    private static AccountSymbolMapping Mapping(string symbol, bool enabled) => new()
    {
        Symbol = symbol,
        AccountSymbol = symbol.Trim().TrimStart('@').ToUpperInvariant(),
        ContractMultiplier = 1m,
        Enabled = enabled
    };

    private static AccountSymbolConversion Conversion(params AccountSymbolMapping[] mappings)
    {
        var account = new WorkspaceAccount
        {
            Id = "acc",
            Name = "acc",
            AccountNumber = "1001",
            InitialBalance = TradingConventions.StrategyReferenceBalance
        };

        // Tabella vuota e non null: e' esattamente cio' che WorkspaceService.ResolveSymbolConversion
        // restituisce a un account che non ne referenzia una.
        return AccountSymbolConversion.FromAccount(account, new SymbolConversion
        {
            Code = mappings.Length == 0 ? string.Empty : "TAB",
            Name = mappings.Length == 0 ? string.Empty : "Tabella",
            Mappings = mappings.ToList()
        });
    }
}
