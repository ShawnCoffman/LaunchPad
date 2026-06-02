namespace SupportLaunchpad.Core.Models;

public sealed class LaunchpadTab
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsReadOnly { get; set; }

    public List<LaunchpadButton> Buttons { get; set; } = [];

    public LaunchpadTab Clone()
    {
        return new LaunchpadTab
        {
            Id = Id,
            Name = Name,
            IsReadOnly = IsReadOnly,
            Buttons = [.. Buttons.Select(button => button.Clone())]
        };
    }
}
