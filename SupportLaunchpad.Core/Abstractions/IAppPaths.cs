namespace SupportLaunchpad.Core.Abstractions;

public interface IAppPaths
{
    string UserConfigPath { get; }

    string LogFilePath { get; }

    string AppSettingsPath { get; }
}
