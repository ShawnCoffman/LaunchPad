using SupportLaunchpad.Core.Models;
using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void ValidateButton_RejectsInvalidUrl()
    {
        var validator = new LaunchpadValidator(new FakeFileSystem());
        var button = new LaunchpadButton
        {
            Name = "Docs",
            ActionType = LaunchActionType.Url,
            Path = "not-a-url"
        };

        var result = validator.ValidateButton(button);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateButton_WarnsForMissingIcon()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"C:\Tools");
        var validator = new LaunchpadValidator(fileSystem);
        var button = new LaunchpadButton
        {
            Name = "Folder",
            ActionType = LaunchActionType.Folder,
            Path = @"C:\Tools",
            IconPath = @"C:\Missing\icon.png"
        };

        var result = validator.ValidateButton(button);

        Assert.True(result.IsValid);
        Assert.Single(result.Issues);
        Assert.True(result.Issues[0].IsWarning);
    }
}
