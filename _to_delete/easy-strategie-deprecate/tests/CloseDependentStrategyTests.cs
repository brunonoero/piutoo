using Piootoo.Strategies.Easy;

namespace Piootoo.Strategies.Tests;

public sealed class CloseDependentStrategyTests
{
    [Fact]
    public void Easy661_IsExcludedBecauseDistExitIsRecalculatedAtRuntime()
    {
        Assert.True(new Easy_661_GC_30().IsPositionCloseDependent);
    }
}
