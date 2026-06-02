using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var fileSystem = new SystemFileSystem();
        var appPaths = new AppPaths();
        var merger = new LaunchpadConfigMerger();
        var appSettingsStore = new AppSettingsStore(appPaths, fileSystem);
        var configStore = new ConfigStore(appPaths, appSettingsStore, fileSystem, merger);
        var validator = new LaunchpadValidator(fileSystem);
        var logger = new FileLogger(appPaths, fileSystem);
        var processStarter = new SystemProcessStarter();
        var launchService = new LaunchService(fileSystem, processStarter, logger, validator);
        var userConfigEditor = new UserConfigEditor();

        Application.Run(new MainForm(configStore, validator, launchService, userConfigEditor));
    }
}
