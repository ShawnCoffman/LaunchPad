using SupportLaunchpad.Core.Models;
using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.Tests;

public sealed class UserConfigEditorTests
{
    [Fact]
    public void DeleteTab_AllowsRemovingLastUserTab()
    {
        var editor = new UserConfigEditor();
        var userConfig = new LaunchpadConfig
        {
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "General"
                }
            ]
        };

        var effectiveConfig = userConfig.Clone();

        var updated = editor.DeleteTab(userConfig, effectiveConfig, "general");

        Assert.Empty(updated.Tabs);
    }
}
