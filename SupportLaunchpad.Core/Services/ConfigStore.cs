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
                sharedConfigLoaded = TryLoadConfig(appSettings.SharedConfigPath, out sharedConfig);
                sharedConfigStatus = sharedConfigLoaded
                    ? $"Mode: Team + Personal ({appSettings.SharedConfigPath})"
                    : GetSharedUnavailableStatus(appSettings);
            }
            else
            {
                sharedConfigStatus = GetSharedUnavailableStatus(appSettings);
            }
        }

        var userConfig = LoadConfigOrDefault(_appPaths.UserConfigPath, LaunchpadConfigFactory.CreateDefaultUserConfig());
        if (appSettings.UseSharedConfig && !sharedConfigLoaded && !appSettings.FallbackToLocalOnly)
        {
            return new MergedLaunchpadState
            {
                AppSettings = appSettings,
                SharedConfig = sharedConfig,
                UserConfig = userConfig,
                EffectiveConfig = new LaunchpadConfig { Tabs = [] },
                SharedConfigLoaded = false,
                SharedConfigStatus = "Shared config unavailable; local-only fallback disabled"
            };
        }

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

        _fileSystem.WriteAllTextAtomic(_appPaths.UserConfigPath, ConfigSerializer.Serialize(config));
    }

    public void SaveAppSettings(LocalAppSettings settings)
    {
        _appSettingsStore.Save(settings);
    }

    public void SaveSharedConfig(string path, LaunchpadConfig config)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A shared configuration path is required.", nameof(path));
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            _fileSystem.CreateDirectory(directory);
        }

        config.Version = Math.Max(1, config.Version + 1);
        if (_fileSystem.FileExists(path))
        {
            _fileSystem.CopyFile(path, path + ".bak", true);
        }
        _fileSystem.WriteAllTextAtomic(path, ConfigSerializer.Serialize(config));
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
        return TryLoadConfig(path, out var config) ? config : fallback;
    }

    private bool TryLoadConfig(string path, out LaunchpadConfig config)
    {
        config = new LaunchpadConfig();
        if (!_fileSystem.FileExists(path))
        {
            return false;
        }

        try
        {
            var json = _fileSystem.ReadAllText(path);
            return !string.IsNullOrWhiteSpace(json) && ConfigSerializer.TryDeserialize(json, out config);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string GetSharedUnavailableStatus(LocalAppSettings settings)
    {
        return settings.FallbackToLocalOnly
            ? "Shared config unavailable or invalid, running local only"
            : "Shared config unavailable or invalid";
    }
}
