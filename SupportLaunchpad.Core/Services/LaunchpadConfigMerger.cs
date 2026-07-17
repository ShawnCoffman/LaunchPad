using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public sealed class LaunchpadConfigMerger
{
    public LaunchpadConfig Merge(LaunchpadConfig sharedConfig, LaunchpadConfig userConfig)
    {
        var effective = sharedConfig.Clone();
        effective.Title = sharedConfig.Tabs.Count > 0 &&
                          string.Equals(userConfig.Title, "Support Launchpad", StringComparison.OrdinalIgnoreCase) &&
                          !string.IsNullOrWhiteSpace(sharedConfig.Title)
            ? sharedConfig.Title
            : string.IsNullOrWhiteSpace(userConfig.Title) ? sharedConfig.Title : userConfig.Title;
        effective.Version = Math.Max(sharedConfig.Version, userConfig.Version);
        effective.Settings = MergeSettings(sharedConfig.Settings, userConfig.Settings);

        var sharedTabs = effective.Tabs
            .GroupBy(tab => tab.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var hiddenTabId in userConfig.HiddenTabIds)
        {
            if (sharedTabs.TryGetValue(hiddenTabId, out var tab) && !tab.IsReadOnly)
            {
                sharedTabs.Remove(hiddenTabId);
            }
        }

        foreach (var sharedTab in sharedTabs.Values)
        {
            sharedTab.Buttons = sharedTab.Buttons
                .Where(button => button.IsReadOnly || !userConfig.HiddenButtonIds.Contains(button.Id, StringComparer.OrdinalIgnoreCase))
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
        mergedTab.IsReadOnly = sharedTab.IsReadOnly || userTab.IsReadOnly;

        var sharedButtons = mergedTab.Buttons
            .GroupBy(button => button.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var buttons = new List<LaunchpadButton>();
        var handledButtonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var userButton in userTab.Buttons)
        {
            if (hiddenButtonIds.Contains(userButton.Id, StringComparer.OrdinalIgnoreCase) &&
                (!sharedButtons.TryGetValue(userButton.Id, out var protectedButton) || !protectedButton.IsReadOnly))
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
                     (button.IsReadOnly || !hiddenButtonIds.Contains(button.Id, StringComparer.OrdinalIgnoreCase))))
        {
            buttons.Add(sharedButton.Clone());
        }

        mergedTab.ButtonOrder = [.. userTab.ButtonOrder];
        mergedTab.Buttons = ApplyButtonOrder(buttons, userTab.ButtonOrder);
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
            IsReadOnly = sharedButton.IsReadOnly || userButton.IsReadOnly,
            CreatedUtc = sharedButton.CreatedUtc == default ? userButton.CreatedUtc : sharedButton.CreatedUtc,
            ModifiedUtc = userButton.ModifiedUtc == default ? sharedButton.ModifiedUtc : userButton.ModifiedUtc
        };
    }

    private static LaunchpadSettings MergeSettings(LaunchpadSettings? shared, LaunchpadSettings? user)
    {
        shared ??= new LaunchpadSettings();
        user ??= new LaunchpadSettings();

        var sharedRestricted = shared.RestrictPowerShellToAllowedDirectories || shared.AllowedScriptDirectories.Count > 0;
        var userRestricted = user.RestrictPowerShellToAllowedDirectories || user.AllowedScriptDirectories.Count > 0;
        var restrictDirectories = sharedRestricted || userRestricted;
        var directories = (sharedRestricted, userRestricted) switch
        {
            (false, false) => [],
            (true, false) => [.. shared.AllowedScriptDirectories],
            (false, true) => [.. user.AllowedScriptDirectories],
            (true, true) => shared.AllowedScriptDirectories
                .Intersect(user.AllowedScriptDirectories, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        return new LaunchpadSettings
        {
            AllowPowerShellScripts = shared.AllowPowerShellScripts && user.AllowPowerShellScripts,
            AllowRunAsAdmin = shared.AllowRunAsAdmin && user.AllowRunAsAdmin,
            AllowedScriptDirectories = directories,
            RestrictPowerShellToAllowedDirectories = restrictDirectories
        };
    }

    private static List<LaunchpadButton> ApplyButtonOrder(List<LaunchpadButton> buttons, IReadOnlyList<string> order)
    {
        if (order.Count == 0)
        {
            return buttons;
        }

        var positions = order
            .Select((id, index) => (id, index))
            .GroupBy(item => item.id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.OrdinalIgnoreCase);

        return buttons
            .Select((button, originalIndex) => (button, originalIndex))
            .OrderBy(item => positions.TryGetValue(item.button.Id, out var position) ? position : int.MaxValue)
            .ThenBy(item => item.originalIndex)
            .Select(item => item.button)
            .ToList();
    }
}
