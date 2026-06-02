namespace SupportLaunchpad.Core.Models;

public sealed class MergedLaunchpadState
{
    public required LocalAppSettings AppSettings { get; init; }

    public required LaunchpadConfig SharedConfig { get; init; }

    public required LaunchpadConfig UserConfig { get; init; }

    public required LaunchpadConfig EffectiveConfig { get; init; }

    public required bool SharedConfigLoaded { get; init; }

    public required string SharedConfigStatus { get; init; }
}
