using System.Diagnostics;
using SupportLaunchpad.Core.Abstractions;
using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public sealed class LaunchService
{
    private readonly IFileSystem _fileSystem;
    private readonly IProcessStarter _processStarter;
    private readonly ILogger _logger;
    private readonly LaunchpadValidator _validator;

    public LaunchService(IFileSystem fileSystem, IProcessStarter processStarter, ILogger logger, LaunchpadValidator validator)
    {
        _fileSystem = fileSystem;
        _processStarter = processStarter;
        _logger = logger;
        _validator = validator;
    }

    public ProcessStartInfo BuildStartInfo(LaunchRequest request)
    {
        var button = request.Button;
        var settings = request.Settings;

        if (button.ActionType == LaunchActionType.PowerShell)
        {
            if (!settings.AllowPowerShellScripts)
            {
                throw new InvalidOperationException("PowerShell scripts are disabled by configuration.");
            }

            if (settings.AllowedScriptDirectories.Count > 0 && !IsScriptAllowed(button.Path, settings.AllowedScriptDirectories))
            {
                throw new InvalidOperationException("This PowerShell script is outside the allowed directories.");
            }
        }

        return button.ActionType switch
        {
            LaunchActionType.Exe => CreateProcessStartInfo(button.Path, button.Arguments, button.WorkingDirectory, button.RunAsAdmin && settings.AllowRunAsAdmin),
            LaunchActionType.Folder => CreateProcessStartInfo("explorer.exe", QuoteIfNeeded(button.Path), button.WorkingDirectory, false),
            LaunchActionType.Url => CreateShellStartInfo(button.Path),
            LaunchActionType.PowerShell => CreateProcessStartInfo("powershell.exe", BuildPowerShellArguments(button), button.WorkingDirectory, button.RunAsAdmin && settings.AllowRunAsAdmin),
            LaunchActionType.Command => CreateProcessStartInfo("cmd.exe", $"/c {QuoteIfNeeded(button.Path)}", button.WorkingDirectory, button.RunAsAdmin && settings.AllowRunAsAdmin),
            _ => throw new InvalidOperationException("Unsupported action type.")
        };
    }

    public LaunchResult Launch(LaunchRequest request)
    {
        var validation = _validator.ValidateButton(request.Button);
        if (!validation.IsValid)
        {
            var message = string.Join(Environment.NewLine, validation.Issues.Where(issue => !issue.IsWarning).Select(issue => issue.Message));
            _logger.LogLaunchAttempt(request.Button.Name, request.Button.ActionType.ToString(), request.Button.Path, false, message);
            return new LaunchResult { Success = false, ErrorMessage = message };
        }

        try
        {
            var startInfo = BuildStartInfo(request);
            _processStarter.Start(startInfo);
            _logger.LogLaunchAttempt(request.Button.Name, request.Button.ActionType.ToString(), request.Button.Path, true, null);
            return new LaunchResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogLaunchAttempt(request.Button.Name, request.Button.ActionType.ToString(), request.Button.Path, false, ex.Message);
            return new LaunchResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName, string arguments, string workingDirectory, bool runAsAdmin)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? string.Empty : workingDirectory,
            UseShellExecute = true,
            Verb = runAsAdmin ? "runas" : string.Empty
        };
    }

    private static ProcessStartInfo CreateShellStartInfo(string target)
    {
        return new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        };
    }

    private static string BuildPowerShellArguments(LaunchpadButton button)
    {
        var parts = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            QuoteIfNeeded(button.Path)
        };

        if (!string.IsNullOrWhiteSpace(button.Arguments))
        {
            parts.Add(button.Arguments);
        }

        return string.Join(" ", parts);
    }

    private bool IsScriptAllowed(string scriptPath, IEnumerable<string> allowedDirectories)
    {
        var fullScriptPath = _fileSystem.GetFullPath(scriptPath);
        return allowedDirectories.Any(directory =>
        {
            var fullDirectoryPath = AppendDirectorySeparator(_fileSystem.GetFullPath(directory));
            return fullScriptPath.StartsWith(fullDirectoryPath, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string AppendDirectorySeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        return value.Contains(' ') ? $"\"{value}\"" : value;
    }
}
