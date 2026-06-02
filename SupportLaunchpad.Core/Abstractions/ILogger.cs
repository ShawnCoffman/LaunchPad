namespace SupportLaunchpad.Core.Abstractions;

public interface ILogger
{
    void LogLaunchAttempt(string buttonName, string actionType, string pathOrCommand, bool success, string? errorMessage);
}
