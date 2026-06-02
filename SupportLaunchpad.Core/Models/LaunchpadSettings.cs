namespace SupportLaunchpad.Core.Models;

public sealed class LaunchpadSettings
{
    public bool AllowPowerShellScripts { get; set; } = true;

    public bool AllowRunAsAdmin { get; set; } = true;

    public List<string> AllowedScriptDirectories { get; set; } = [];

    public LaunchpadSettings Clone()
    {
        return new LaunchpadSettings
        {
            AllowPowerShellScripts = AllowPowerShellScripts,
            AllowRunAsAdmin = AllowRunAsAdmin,
            AllowedScriptDirectories = [.. AllowedScriptDirectories]
        };
    }
}
