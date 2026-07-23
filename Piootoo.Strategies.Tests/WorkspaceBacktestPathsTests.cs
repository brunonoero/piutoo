using Piootoo.Core.Services;

namespace Piootoo.Strategies.Tests;

public class WorkspaceBacktestPathsTests
{
    [Theory]
    [InlineData("  Prova Estate 2026  ", "prova-estate-2026")]
    [InlineData("Alpha/Beta", "alpha-beta")]
    [InlineData("Nome:Test", "nome-test")]
    public void NormalizeFolderName_CreatesSafeFolderName(string input, string expected)
        => Assert.Equal(expected, WorkspaceBacktestPaths.NormalizeFolderName(input));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("../altro")]
    public void NormalizeFolderName_RejectsEmptyOrTraversal(string input)
        => Assert.Throws<ArgumentException>(() => WorkspaceBacktestPaths.NormalizeFolderName(input));

    [Fact]
    public void ResolveBacktestPath_RemainsUnderWorkspaceBacktests()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "piootoo-workspace");

        var resolved = WorkspaceBacktestPaths.ResolveBacktestPath(workspace, "Test Uno");

        Assert.Equal(
            Path.GetFullPath(Path.Combine(workspace, "backtests", "test-uno")),
            resolved);
    }
}
