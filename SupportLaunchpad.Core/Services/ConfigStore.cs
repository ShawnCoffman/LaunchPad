using SupportLaunchpad.Core.Abstractions;
using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public sealed class ConfigStore
{
    private readonly IAppPaths _appPaths;
    private readonly AppSettingsStore _appSettingsStore;
    private readonly IFileSystem _fileSystem;
    private readonly LaunchpadConfigMerger _merger;

    public ConfigStore(IAppPaths appPaths, AppSettingsStore appSettingsStore, IFileSystem fileSystem, LaunchpadConfigMerger merger)
    {
        _appPaths = appPaths;
        _appSettingsStore = appSettingsStore;
        _fileSystem = fileSystem;
        _merger = merger;
    }

    public MergedLaunchpadState Load()
    {
        EnsureUserConfigExists();
        var appSettings = _appSettingsStore.Load();

        var sharedConfig = new LaunchpadConfig();
        var sharedConfigLoaded = false;
        var sharedConfigStatus = "Mode: Personal only";

        if (appSettings.UseSharedConfig)
        {
            if (!string.IsNullOrWhiteSpace(appSettings.SharedConfigPath) && _fileSystem.FileExists(appSettings.SharedConfigPath))
            {
                sharedConfig = LoadConfigOrDefault(appSettings.SharedConfigPath, new LaunchpadConfig());
                sharedConfigLoaded = true;
                sharedConfigStatus = $"Mode: Team + Personal ({appSettings.SharedConfigPath})";
            }
            else
            {
                sharedConfigStatus = appSettings.FallbackToLocalOnly
                    ? "Shared config unavailable, running local only"
                    : "Shared config unavailable";
            }
        }

        var userConfig = LoadConfigOrDefault(_appPaths.UserConfigPath, LaunchpadConfigFactory.CreateDefaultUserConfig());
        var effectiveConfig = _merger.Merge(sharedConfig, userConfig);

        return new MergedLaunchpadState
        {
            AppSettings = appSettings,
            SharedConfig = sharedConfig,
            UserConfig = userConfig,
            EffectiveConfig = effectiveConfig,
            SharedConfigLoaded = sharedConfigLoaded,
            SharedConfigStatus = sharedConfigStatus
        };
    }

    public void SaveUserConfig(LaunchpadConfig config)
    {
        var directory = Path.GetDirectoryName(_appPaths.UserConfigPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            _fileSystem.CreateDirectory(directory);
        }

        _fileSystem.WriteAllText(_appPaths.UserConfigPath, ConfigSerializer.Serialize(config));
    }

    public void SaveAppSettings(LocalAppSettings settings)
    {
        _appSettingsStore.Save(settings);
    }

    private void EnsureUserConfigExists()
    {
        if (_fileSystem.FileExists(_appPaths.UserConfigPath))
        {
            return;
        }

        SaveUserConfig(LaunchpadConfigFactory.CreateDefaultUserConfig());
    }

    private LaunchpadConfig LoadConfigOrDefault(string path, LaunchpadConfig fallback)
    {
        if (!_fileSystem.FileExists(path))
        {
            return fallback;
        }

        var json = _fileSystem.ReadAllText(path);
        return string.IsNullOrWhiteSpace(json) ? fallback : ConfigSerializer.Deserialize(json);
    }
}
