namespace SupportLaunchpad.Core.Models;

public sealed class ValidationResult
{
    private readonly List<ValidationIssue> _issues = [];

    public IReadOnlyList<ValidationIssue> Issues => _issues;

    public bool IsValid => _issues.All(issue => issue.IsWarning);

    public void AddError(string message)
    {
        _issues.Add(new ValidationIssue(message, false));
    }

    public void AddWarning(string message)
    {
        _issues.Add(new ValidationIssue(message, true));
    }
}
