using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public sealed class LaunchpadConfigMerger
{
    public LaunchpadConfig Merge(LaunchpadConfig sharedConfig, LaunchpadConfig userConfig)
    {
        var effective = sharedConfig.Clone();
        effective.Title = string.IsNullOrWhiteSpace(userConfig.Title) ? sharedConfig.Title : userConfig.Title;
        effective.Version = Math.Max(sharedConfig.Version, userConfig.Version);
        effective.Settings = userConfig.Settings?.Clone() ?? sharedConfig.Settings.Clone();

        var sharedTabs = effective.Tabs.ToDictionary(tab => tab.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var hiddenTabId in userConfig.HiddenTabIds)
        {
            sharedTabs.Remove(hiddenTabId);
        }

        foreach (var sharedTab in sharedTabs.Values)
        {
            sharedTab.Buttons = sharedTab.Buttons
                .Where(button => !userConfig.HiddenButtonIds.Contains(button.Id, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        var mergedTabs = new List<LaunchpadTab>();
        var handledTabIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var userTab in userConfig.Tabs)
        {
            if (sharedTabs.TryGetValue(userTab.Id, out var sharedTab))
            {
                mergedTabs.Add(MergeTab(sharedTab, userTab, userConfig.HiddenButtonIds));
            }
            else
            {
                mergedTabs.Add(userTab.Clone());
            }

            handledTabIds.Add(userTab.Id);
        }

        foreach (var sharedTab in sharedTabs.Values.Where(tab => !handledTabIds.Contains(tab.Id)))
        {
            mergedTabs.Add(sharedTab.Clone());
        }

        effective.Tabs = mergedTabs;
        return effective;
    }

    private static LaunchpadTab MergeTab(LaunchpadTab sharedTab, LaunchpadTab userTab, IReadOnlyCollection<string> hiddenButtonIds)
    {
        var mergedTab = sharedTab.Clone();
        mergedTab.Name = string.IsNullOrWhiteSpace(userTab.Name) ? sharedTab.Name : userTab.Name;
        mergedTab.IsReadOnly = userTab.IsReadOnly;

        var sharedButtons = mergedTab.Buttons.ToDictionary(button => button.Id, StringComparer.OrdinalIgnoreCase);
        var buttons = new List<LaunchpadButton>();
        var handledButtonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var userButton in userTab.Buttons)
        {
            if (hiddenButtonIds.Contains(userButton.Id, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (sharedButtons.TryGetValue(userButton.Id, out var sharedButton))
            {
                buttons.Add(MergeButton(sharedButton, userButton));
            }
            else
            {
                buttons.Add(userButton.Clone());
            }

            handledButtonIds.Add(userButton.Id);
        }

        foreach (var sharedButton in sharedButtons.Values.Where(button =>
                     !handledButtonIds.Contains(button.Id) &&
                     !hiddenButtonIds.Contains(button.Id, StringComparer.OrdinalIgnoreCase)))
        {
            buttons.Add(sharedButton.Clone());
        }

        mergedTab.Buttons = buttons;
        return mergedTab;
    }

    private static LaunchpadButton MergeButton(LaunchpadButton sharedButton, LaunchpadButton userButton)
    {
        return new LaunchpadButton
        {
            Id = sharedButton.Id,
            Name = string.IsNullOrWhiteSpace(userButton.Name) ? sharedButton.Name : userButton.Name,
            Description = string.IsNullOrWhiteSpace(userButton.Description) ? sharedButton.Description : userButton.Description,
            ActionType = userButton.ActionType,
            Path = string.IsNullOrWhiteSpace(userButton.Path) ? sharedButton.Path : userButton.Path,
            Arguments = userButton.Arguments,
            WorkingDirectory = userButton.WorkingDirectory,
            RunAsAdmin = userButton.RunAsAdmin,
            IconPath = userButton.IconPath,
            IsReadOnly = userButton.IsReadOnly,
            CreatedUtc = sharedButton.CreatedUtc == default ? userButton.CreatedUtc : sharedButton.CreatedUtc,
            ModifiedUtc = userButton.ModifiedUtc == default ? sharedButton.ModifiedUtc : userButton.ModifiedUtc
        };
    }
}
