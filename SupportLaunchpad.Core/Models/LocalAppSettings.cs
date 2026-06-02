namespace SupportLaunchpad.Core.Models;

public sealed class LocalAppSettings
{
    public bool UseSharedConfig { get; set; }

    public string SharedConfigPath { get; set; } = string.Empty;

    public bool WarnIfSharedConfigUnavailable { get; set; } = true;

    public bool FallbackToLocalOnly { get; set; } = true;

    public LocalAppSettings Clone()
    {
        return new LocalAppSettings
        {
            UseSharedConfig = UseSharedConfig,
            SharedConfigPath = SharedConfigPath,
            WarnIfSharedConfigUnavailable = WarnIfSharedConfigUnavailable,
            FallbackToLocalOnly = FallbackToLocalOnly
        };
    }
}
