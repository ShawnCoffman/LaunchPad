using System.Diagnostics;
using SupportLaunchpad.Core.Abstractions;

namespace SupportLaunchpad.Tests;

internal sealed class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

    public bool FileExists(string path) => _files.ContainsKey(Normalize(path));

    public bool DirectoryExists(string path) => _directories.Contains(Normalize(path));

    public string ReadAllText(string path) => _files[Normalize(path)];

    public void WriteAllText(string path, string contents)
    {
        _files[Normalize(path)] = contents;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            _directories.Add(Normalize(directory));
        }
    }

    public void WriteAllTextAtomic(string path, string contents) => WriteAllText(path, contents);

    public void AppendAllText(string path, string contents)
    {
        _files.TryGetValue(Normalize(path), out var existing);
        WriteAllText(path, existing + contents);
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        if (!overwrite && FileExists(destinationPath))
        {
            throw new IOException("Destination exists.");
        }

        WriteAllText(destinationPath, ReadAllText(sourcePath));
    }

    public void CreateDirectory(string path)
    {
        _directories.Add(Normalize(path));
    }

    public string GetFullPath(string path) => Normalize(path);

    public void AddFile(string path, string contents = "")
    {
        WriteAllText(path, contents);
    }

    public void AddDirectory(string path)
    {
        _directories.Add(Normalize(path));
    }

    private static string Normalize(string path) => path.Replace('/', '\\').TrimEnd('\\');
}

internal sealed class FakeProcessStarter : IProcessStarter
{
    public ProcessStartInfo? LastStartInfo { get; private set; }

    public void Start(ProcessStartInfo startInfo)
    {
        LastStartInfo = startInfo;
    }
}

internal sealed class FakeAppPaths : IAppPaths
{
    public string UserConfigPath { get; init; } = @"C:\Users\Test\AppData\Roaming\SupportLaunchpad\launchpad.user.json";

    public string LogFilePath { get; init; } = @"C:\Users\Test\AppData\Local\SupportLaunchpad\Logs\launchpad.log";

    public string AppSettingsPath { get; init; } = @"C:\Users\Test\AppData\Roaming\SupportLaunchpad\appsettings.json";
}

internal sealed class FakeLogger : ILogger
{
    public readonly List<string> Entries = [];

    public void LogLaunchAttempt(string buttonName, string actionType, string pathOrCommand, bool success, string? errorMessage)
    {
        Entries.Add($"{buttonName}|{actionType}|{pathOrCommand}|{success}|{errorMessage}");
    }
}
