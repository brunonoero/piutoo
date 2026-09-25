using System.Reflection;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Una leva di un contenitore non puo' essere un campo pubblico: la valutazione gira su un clone che
/// copia i soli campi non pubblici, e una leva pubblica resta al default nel clone senza errori. E'
/// successo il 25/09/2026 al filtro di volatilita' di <see cref="RC_LFD"/>: la misura sembrava dire
/// che il filtro non cambiava niente, ed era il filtro a non esserci.
/// </summary>
public sealed class ResearchContainerFieldsTests
{
    [Fact]
    public void NoResearchContainerDeclaresAPublicInstanceField()
    {
        var offenders = typeof(RC_PCH).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(RC_PCH).Namespace && !type.IsAbstract && type.Name.StartsWith("RC_", StringComparison.Ordinal))
            .SelectMany(type => type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => $"{type.Name}.{field.Name}"))
            .ToList();

        Assert.True(offenders.Count == 0, "campi pubblici che il clone di valutazione non copia: " + string.Join(", ", offenders));
    }
}
