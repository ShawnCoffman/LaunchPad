namespace SupportLaunchpad.Core.Models;

public sealed class ValidationIssue
{
    public ValidationIssue(string message, bool isWarning)
    {
        Message = message;
        IsWarning = isWarning;
    }

    public string Message { get; }

    public bool IsWarning { get; }
}
