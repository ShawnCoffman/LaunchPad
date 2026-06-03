using SupportLaunchpad.Core.Models;
using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.Tests;

public sealed class ConfigStoreTests
{
    [Fact]
    public void Load_CreatesDefaultUserConfig_WhenMissing()
    {
        var fileSystem = new FakeFileSystem();
        var appPaths = new FakeAppPaths();
        var appSettingsStore = new AppSettingsStore(appPaths, fileSystem);
        var store = new ConfigStore(appPaths, appSettingsStore, fileSystem, new LaunchpadConfigMerger());

        var state = store.Load();

        Assert.Single(state.UserConfig.Tabs);
        Assert.Equal("General", state.UserConfig.Tabs[0].Name);
        Assert.True(fileSystem.FileExists(appPaths.UserConfigPath));
        Assert.True(fileSystem.FileExists(appPaths.AppSettingsPath));
    }

    [Fact]
    public void SaveUserConfig_WritesJson()
    {
        var fileSystem = new FakeFileSystem();
        var appPaths = new FakeAppPaths();
        var appSettingsStore = new AppSettingsStore(appPaths, fileSystem);
        var store = new ConfigStore(appPaths, appSettingsStore, fileSystem, new LaunchpadConfigMerger());
        var config = LaunchpadConfigFactory.CreateDefaultUserConfig();

        store.SaveUserConfig(config);

        var json = fileSystem.ReadAllText(appPaths.UserConfigPath);
        Assert.Contains("\"title\": \"Support Launchpad\"", json);
    }

    [Fact]
    public void Load_UsesLocalOnlyMode_WhenSharedConfigDisabled()
    {
        var fileSystem = new FakeFileSystem();
        var appPaths = new FakeAppPaths();
        var settings = new LocalAppSettings { UseSharedConfig = false, SharedConfigPath = @"\\server\launchpad.shared.json" };
        fileSystem.AddFile(appPaths.AppSettingsPath, AppSettingsSerializer.Serialize(settings));
        var appSettingsStore = new AppSettingsStore(appPaths, fileSystem);
        var store = new ConfigStore(appPaths, appSettingsStore, fileSystem, new LaunchpadConfigMerger());

        var state = store.Load();

        Assert.False(state.SharedConfigLoaded);
        Assert.Equal("Mode: Personal only", state.SharedConfigStatus);
    }

    [Fact]
    public void Load_FallsBackWhenSharedConfigMissing()
    {
        var fileSystem = new FakeFileSystem();
        var appPaths = new FakeAppPaths();
        var settings = new LocalAppSettings { UseSharedConfig = true, SharedConfigPath = @"\\server\launchpad.shared.json" };
        fileSystem.AddFile(appPaths.AppSettingsPath, AppSettingsSerializer.Serialize(settings));
        var appSettingsStore = new AppSettingsStore(appPaths, fileSystem);
        var store = new ConfigStore(appPaths, appSettingsStore, fileSystem, new LaunchpadConfigMerger());

        var state = store.Load();

        Assert.False(state.SharedConfigLoaded);
        Assert.Equal("Shared config unavailable, running local only", state.SharedConfigStatus);
    }

    [Fact]
    public void Load_DoesNotUseLocalConfig_WhenSharedConfigMissingAndFallbackDisabled()
    {
        var fileSystem = new FakeFileSystem();
        var appPaths = new FakeAppPaths();
        var settings = new LocalAppSettings
        {
            UseSharedConfig = true,
            SharedConfigPath = @"\\server\launchpad.shared.json",
            FallbackToLocalOnly = false
        };
        fileSystem.AddFile(appPaths.AppSettingsPath, AppSettingsSerializer.Serialize(settings));
        var appSettingsStore = new AppSettingsStore(appPaths, fileSystem);
        var store = new ConfigStore(appPaths, appSettingsStore, fileSystem, new LaunchpadConfigMerger());

        var state = store.Load();

        Assert.False(state.SharedConfigLoaded);
        Assert.Equal("Shared config unavailable; local-only fallback disabled", state.SharedConfigStatus);
        Assert.Empty(state.EffectiveConfig.Tabs);
    }

    [Fact]
    public void Load_UsesDefaults_WhenUserConfigJsonIsInvalid()
    {
        var fileSystem = new FakeFileSystem();
        var appPaths = new FakeAppPaths();
        fileSystem.AddFile(appPaths.UserConfigPath, "{ invalid json");
        var appSettingsStore = new AppSettingsStore(appPaths, fileSystem);
        var store = new ConfigStore(appPaths, appSettingsStore, fileSystem, new LaunchpadConfigMerger());

        var state = store.Load();

        Assert.Single(state.UserConfig.Tabs);
        Assert.Equal("General", state.UserConfig.Tabs[0].Name);
    }

    [Fact]
    public void Merge_UsesUserOverrideForSharedButton()
    {
        var shared = new LaunchpadConfig
        {
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "General",
                    Buttons =
                    [
                        new LaunchpadButton { Id = "tool", Name = "Shared Tool", Path = @"C:\Tools\app.exe", ActionType = LaunchActionType.Exe }
                    ]
                }
            ]
        };

        var user = new LaunchpadConfig
        {
            Title = "My Launchpad",
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "My General",
                    Buttons =
                    [
                        new LaunchpadButton { Id = "tool", Name = "My Tool", Path = @"C:\Users\Me\app.exe", ActionType = LaunchActionType.Exe }
                    ]
                }
            ]
        };

        var merged = new LaunchpadConfigMerger().Merge(shared, user);

        Assert.Equal("My Launchpad", merged.Title);
        Assert.Equal("My General", merged.Tabs[0].Name);
        Assert.Equal("My Tool", merged.Tabs[0].Buttons[0].Name);
    }
}
