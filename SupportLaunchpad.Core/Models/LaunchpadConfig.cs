namespace SupportLaunchpad.Core.Models;

public sealed class LaunchpadConfig
{
    public int Version { get; set; } = 1;

    public string Title { get; set; } = "Support Launchpad";

    public LaunchpadSettings Settings { get; set; } = new();

    public List<LaunchpadTab> Tabs { get; set; } = [];

    public List<string> HiddenTabIds { get; set; } = [];

    public List<string> HiddenButtonIds { get; set; } = [];

    public LaunchpadConfig Clone()
    {
        return new LaunchpadConfig
        {
            Version = Version,
            Title = Title,
            Settings = Settings.Clone(),
            Tabs = [.. Tabs.Select(tab => tab.Clone())],
            HiddenTabIds = [.. HiddenTabIds],
            HiddenButtonIds = [.. HiddenButtonIds]
        };
    }
}
