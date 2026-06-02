namespace SupportLaunchpad.Core.Models;

public sealed class LaunchResult
{
    public bool Success { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;
}
