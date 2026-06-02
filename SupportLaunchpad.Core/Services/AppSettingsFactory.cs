using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public static class AppSettingsFactory
{
    public static LocalAppSettings CreateDefault()
    {
        return new LocalAppSettings
        {
            UseSharedConfig = false,
            SharedConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SupportLaunchpad",
                "launchpad.shared.json"),
            WarnIfSharedConfigUnavailable = true,
            FallbackToLocalOnly = true
        };
    }
}
