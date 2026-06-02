using SupportLaunchpad.Core.Abstractions;

namespace SupportLaunchpad.Core.Services;

public sealed class FileLogger : ILogger
{
    private readonly IAppPaths _appPaths;
    private readonly IFileSystem _fileSystem;

    public FileLogger(IAppPaths appPaths, IFileSystem fileSystem)
    {
        _appPaths = appPaths;
        _fileSystem = fileSystem;
    }

    public void LogLaunchAttempt(string buttonName, string actionType, string pathOrCommand, bool success, string? errorMessage)
    {
        var directory = Path.GetDirectoryName(_appPaths.LogFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            _fileSystem.CreateDirectory(directory);
        }

        var line = string.Join(" | ",
            DateTimeOffset.UtcNow.ToString("O"),
            $"Button={buttonName}",
            $"ActionType={actionType}",
            $"Target={pathOrCommand}",
            $"Success={success}",
            $"Error={errorMessage ?? string.Empty}");

        var existing = _fileSystem.FileExists(_appPaths.LogFilePath)
            ? _fileSystem.ReadAllText(_appPaths.LogFilePath)
            : string.Empty;

        _fileSystem.WriteAllText(_appPaths.LogFilePath, existing + line + Environment.NewLine);
    }
}
