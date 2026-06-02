namespace SupportLaunchpad.Core.Models;

public sealed class LaunchpadButton
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public LaunchActionType ActionType { get; set; }

    public string Path { get; set; } = string.Empty;

    public string Arguments { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;

    public bool RunAsAdmin { get; set; }

    public string IconPath { get; set; } = string.Empty;

    public bool IsReadOnly { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ModifiedUtc { get; set; }

    public LaunchpadButton Clone()
    {
        return new LaunchpadButton
        {
            Id = Id,
            Name = Name,
            Description = Description,
            ActionType = ActionType,
            Path = Path,
            Arguments = Arguments,
            WorkingDirectory = WorkingDirectory,
            RunAsAdmin = RunAsAdmin,
            IconPath = IconPath,
            IsReadOnly = IsReadOnly,
            CreatedUtc = CreatedUtc,
            ModifiedUtc = ModifiedUtc
        };
    }
}
