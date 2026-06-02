using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public static class LaunchpadConfigFactory
{
    public static LaunchpadConfig CreateDefaultUserConfig()
    {
        return new LaunchpadConfig
        {
            Version = 1,
            Title = "Support Launchpad",
            Settings = new LaunchpadSettings
            {
                AllowPowerShellScripts = true,
                AllowRunAsAdmin = true
            },
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "General",
                    IsReadOnly = false,
                    Buttons = []
                }
            ]
        };
    }
}
