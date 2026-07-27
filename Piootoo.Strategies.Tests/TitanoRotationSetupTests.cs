using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Optimization;

namespace Piootoo.Strategies.Tests;

public sealed class TitanoRotationSetupTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-titano-setup-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void CreatesThreeProfessionalSetups()
    {
        var service = CreateService();

        var setups = service.ListSetups();

        Assert.Contains(setups, setup => setup.Id == "conservativo");
        Assert.Contains(setups, setup => setup.Id == "bilanciato");
        Assert.Contains(setups, setup => setup.Id == "dinamico");
        Assert.All(setups, setup => Assert.False(string.IsNullOrWhiteSpace(setup.Description)));
    }

    [Fact]
    public void SavesAndReloadsNameDescriptionAndParameters()
    {
        var service = CreateService();
        var saved = service.SaveSetup(new TitanoRotationSetup
        {
            Name = "Mandato istituzionale",
            Description = "Parametri approvati dal comitato rischio.",
            ShortWindowDays = 120,
            LongWindowDays = 400,
            MaximumCurrentDrawdown = 0.12m,
            HardStopDrawdown = 0.30m
        });

        var loaded = service.GetSetup(saved.Id);

        Assert.Equal("mandato-istituzionale", loaded.Id);
        Assert.Equal("Parametri approvati dal comitato rischio.", loaded.Description);
        Assert.Equal(120, loaded.ShortWindowDays);
        Assert.Equal(0.12m, loaded.MaximumCurrentDrawdown);
    }

    private TitanoRotationSetupService CreateService() =>
        new(new PiootooSettings { SettingsPath = _root });

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
