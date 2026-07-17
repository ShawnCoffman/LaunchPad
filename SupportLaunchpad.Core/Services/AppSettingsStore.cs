using SupportLaunchpad.Core.Abstractions;
using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public sealed class AppSettingsStore
{
    private readonly IAppPaths _appPaths;
    private readonly IFileSystem _fileSystem;

    public AppSettingsStore(IAppPaths appPaths, IFileSystem fileSystem)
    {
        _appPaths = appPaths;
        _fileSystem = fileSystem;
    }

    public LocalAppSettings Load()
    {
        EnsureExists();

        var json = _fileSystem.ReadAllText(_appPaths.AppSettingsPath);
        var settings = string.IsNullOrWhiteSpace(json) ? AppSettingsFactory.CreateDefault() : AppSettingsSerializer.Deserialize(json);
        var managedSharedPath = Environment.GetEnvironmentVariable("SUPPORT_LAUNCHPAD_SHARED_CONFIG");
        if (!string.IsNullOrWhiteSpace(managedSharedPath))
        {
            settings.UseSharedConfig = true;
            settings.SharedConfigPath = managedSharedPath.Trim();
        }

        return settings;
    }

    public void Save(LocalAppSettings settings)
    {
        var directory = Path.GetDirectoryName(_appPaths.AppSettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            _fileSystem.CreateDirectory(directory);
        }

        _fileSystem.WriteAllTextAtomic(_appPaths.AppSettingsPath, AppSettingsSerializer.Serialize(settings));
    }

    private void EnsureExists()
    {
        if (_fileSystem.FileExists(_appPaths.AppSettingsPath))
        {
            return;
        }

        Save(AppSettingsFactory.CreateDefault());
    }
}
