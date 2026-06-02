using SupportLaunchpad.Core.Abstractions;

namespace SupportLaunchpad.Core.Services;

public sealed class AppPaths : IAppPaths
{
    public string UserConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SupportLaunchpad", "launchpad.user.json");

    public string LogFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SupportLaunchpad", "Logs", "launchpad.log");

    public string AppSettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SupportLaunchpad", "appsettings.json");
}
