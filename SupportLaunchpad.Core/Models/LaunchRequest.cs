namespace SupportLaunchpad.Core.Models;

public sealed class LaunchRequest
{
    public required LaunchpadButton Button { get; init; }

    public required LaunchpadSettings Settings { get; init; }
}
