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
        Assert.Equal("Shared config unavailable or invalid, running local only", state.SharedConfigStatus);
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
    public void Load_FallsBackWhenSharedConfigJsonIsInvalid()
    {
        var fileSystem = new FakeFileSystem();
        var appPaths = new FakeAppPaths();
        var sharedPath = @"C:\Team\launchpad.shared.json";
        fileSystem.AddFile(sharedPath, "{ invalid json");
        fileSystem.AddFile(appPaths.AppSettingsPath, AppSettingsSerializer.Serialize(new LocalAppSettings
        {
            UseSharedConfig = true,
            SharedConfigPath = sharedPath,
            FallbackToLocalOnly = true
        }));
        var store = new ConfigStore(appPaths, new AppSettingsStore(appPaths, fileSystem), fileSystem, new LaunchpadConfigMerger());

        var state = store.Load();

        Assert.False(state.SharedConfigLoaded);
        Assert.Single(state.EffectiveConfig.Tabs);
        Assert.Contains("invalid", state.SharedConfigStatus);
    }

    [Fact]
    public void Merge_DoesNotAllowUserToLoosenSharedSecurityOrReadOnlyPolicy()
    {
        var shared = new LaunchpadConfig
        {
            Settings = new LaunchpadSettings { AllowPowerShellScripts = false, AllowRunAsAdmin = false },
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "team",
                    Name = "Team",
                    IsReadOnly = true,
                    Buttons = [new LaunchpadButton { Id = "tool", Name = "Tool", IsReadOnly = true }]
                }
            ]
        };
        var user = new LaunchpadConfig
        {
            Settings = new LaunchpadSettings { AllowPowerShellScripts = true, AllowRunAsAdmin = true },
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "team",
                    Name = "Renamed",
                    IsReadOnly = false,
                    Buttons = [new LaunchpadButton { Id = "tool", Name = "Changed", IsReadOnly = false }]
                }
            ]
        };

        var merged = new LaunchpadConfigMerger().Merge(shared, user);

        Assert.False(merged.Settings.AllowPowerShellScripts);
        Assert.False(merged.Settings.AllowRunAsAdmin);
        Assert.True(merged.Tabs[0].IsReadOnly);
        Assert.True(merged.Tabs[0].Buttons[0].IsReadOnly);
    }

    [Fact]
    public void Merge_EmptyScriptDirectoryIntersection_RemainsRestricted()
    {
        var shared = new LaunchpadConfig
        {
            Settings = new LaunchpadSettings
            {
                RestrictPowerShellToAllowedDirectories = true,
                AllowedScriptDirectories = [@"C:\TeamScripts"]
            }
        };
        var user = new LaunchpadConfig
        {
            Settings = new LaunchpadSettings
            {
                RestrictPowerShellToAllowedDirectories = true,
                AllowedScriptDirectories = [@"D:\PersonalScripts"]
            }
        };

        var merged = new LaunchpadConfigMerger().Merge(shared, user);

        Assert.True(merged.Settings.RestrictPowerShellToAllowedDirectories);
        Assert.Empty(merged.Settings.AllowedScriptDirectories);
    }

    [Fact]
    public void Merge_DoesNotHideReadOnlyTeamContent()
    {
        var shared = new LaunchpadConfig
        {
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "required",
                    Name = "Required",
                    IsReadOnly = true,
                    Buttons = [new LaunchpadButton { Id = "required-link", Name = "Required Link", IsReadOnly = true }]
                }
            ]
        };
        var user = new LaunchpadConfig
        {
            HiddenTabIds = ["required"],
            HiddenButtonIds = ["required-link"]
        };

        var merged = new LaunchpadConfigMerger().Merge(shared, user);

        Assert.Single(merged.Tabs);
        Assert.Single(merged.Tabs[0].Buttons);
    }

    [Fact]
    public void SaveSharedConfig_IncrementsVersionAndWritesConfig()
    {
        var fileSystem = new FakeFileSystem();
        var appPaths = new FakeAppPaths();
        var store = new ConfigStore(appPaths, new AppSettingsStore(appPaths, fileSystem), fileSystem, new LaunchpadConfigMerger());
        var config = new LaunchpadConfig { Version = 3, Title = "Operations" };

        store.SaveSharedConfig(@"C:\Team\launchpad.json", config);

        Assert.Equal(4, config.Version);
        Assert.Contains("Operations", fileSystem.ReadAllText(@"C:\Team\launchpad.json"));
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
