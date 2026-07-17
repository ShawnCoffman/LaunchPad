using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public sealed class UserConfigEditor
{
    public LaunchpadConfig AddTab(LaunchpadConfig userConfig, LaunchpadConfig effectiveConfig, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tab name is required.", nameof(name));
        }

        var updated = userConfig.Clone();
        var existingIds = effectiveConfig.Tabs
            .Select(tab => tab.Id)
            .Concat(updated.Tabs.Select(tab => tab.Id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        updated.Tabs.Add(new LaunchpadTab
        {
            Id = CreateUniqueId(name, existingIds),
            Name = name.Trim(),
            IsReadOnly = false,
            Buttons = []
        });

        return updated;
    }

    public LaunchpadConfig RenameTab(LaunchpadConfig userConfig, LaunchpadConfig effectiveConfig, string tabId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Tab name is required.", nameof(newName));
        }

        if (IsReadOnlyEffectiveTab(effectiveConfig, tabId))
        {
            return userConfig.Clone();
        }

        var updated = userConfig.Clone();
        var tab = GetOrCreateEditableTab(updated, effectiveConfig, tabId);
        tab.Name = newName.Trim();
        return updated;
    }

    public LaunchpadConfig DeleteTab(LaunchpadConfig userConfig, LaunchpadConfig effectiveConfig, string tabId)
    {
        if (IsReadOnlyEffectiveTab(effectiveConfig, tabId))
        {
            return userConfig.Clone();
        }

        var updated = userConfig.Clone();
        updated.Tabs.RemoveAll(tab => tab.Id.Equals(tabId, StringComparison.OrdinalIgnoreCase));

        var stillExistsInEffective = effectiveConfig.Tabs.Any(tab => tab.Id.Equals(tabId, StringComparison.OrdinalIgnoreCase));
        if (stillExistsInEffective && !updated.HiddenTabIds.Contains(tabId, StringComparer.OrdinalIgnoreCase))
        {
            updated.HiddenTabIds.Add(tabId);
        }

        return updated;
    }

    public LaunchpadConfig UpsertButton(LaunchpadConfig userConfig, LaunchpadConfig effectiveConfig, string tabId, LaunchpadButton button)
    {
        if (IsReadOnlyEffectiveTab(effectiveConfig, tabId) || IsReadOnlyEffectiveButton(effectiveConfig, button.Id))
        {
            return userConfig.Clone();
        }

        var updated = userConfig.Clone();
        RemoveButtonEverywhere(updated, button.Id);

        var tab = GetOrCreateEditableTab(updated, effectiveConfig, tabId);
        button.IsReadOnly = false;

        var existingIndex = tab.Buttons.FindIndex(candidate => candidate.Id.Equals(button.Id, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            tab.Buttons[existingIndex] = button.Clone();
        }
        else
        {
            tab.Buttons.Add(button.Clone());
        }

        updated.HiddenButtonIds.RemoveAll(id => id.Equals(button.Id, StringComparison.OrdinalIgnoreCase));
        return updated;
    }

    public LaunchpadConfig DeleteButton(LaunchpadConfig userConfig, LaunchpadConfig effectiveConfig, string buttonId)
    {
        if (IsReadOnlyEffectiveButton(effectiveConfig, buttonId))
        {
            return userConfig.Clone();
        }

        var updated = userConfig.Clone();
        RemoveButtonEverywhere(updated, buttonId);
        if (!updated.HiddenButtonIds.Contains(buttonId, StringComparer.OrdinalIgnoreCase))
        {
            updated.HiddenButtonIds.Add(buttonId);
        }

        return updated;
    }

    public LaunchpadConfig MoveButtonToTab(LaunchpadConfig userConfig, LaunchpadConfig effectiveConfig, string sourceTabId, string targetTabId, string buttonId)
    {
        if (IsReadOnlyEffectiveTab(effectiveConfig, sourceTabId) || IsReadOnlyEffectiveTab(effectiveConfig, targetTabId))
        {
            return userConfig.Clone();
        }

        var effectiveButton = effectiveConfig.Tabs
            .First(tab => tab.Id.Equals(sourceTabId, StringComparison.OrdinalIgnoreCase))
            .Buttons.First(button => button.Id.Equals(buttonId, StringComparison.OrdinalIgnoreCase))
            .Clone();

        if (effectiveButton.IsReadOnly)
        {
            return userConfig.Clone();
        }

        effectiveButton.IsReadOnly = false;
        effectiveButton.ModifiedUtc = DateTime.UtcNow;

        var updated = DeleteButton(userConfig, effectiveConfig, buttonId);
        updated.HiddenButtonIds.RemoveAll(id => id.Equals(buttonId, StringComparison.OrdinalIgnoreCase));
        return UpsertButton(updated, effectiveConfig, targetTabId, effectiveButton);
    }

    public LaunchpadConfig MoveButtonWithinTab(LaunchpadConfig userConfig, LaunchpadConfig effectiveConfig, string tabId, string buttonId, int direction)
    {
        if (IsReadOnlyEffectiveTab(effectiveConfig, tabId))
        {
            return userConfig.Clone();
        }

        var effectiveButton = effectiveConfig.Tabs
            .First(tab => tab.Id.Equals(tabId, StringComparison.OrdinalIgnoreCase))
            .Buttons.FirstOrDefault(button => button.Id.Equals(buttonId, StringComparison.OrdinalIgnoreCase));
        if (effectiveButton?.IsReadOnly == true)
        {
            return userConfig.Clone();
        }

        var updated = userConfig.Clone();
        var editableTab = GetOrCreateEditableTab(updated, effectiveConfig, tabId);

        var effectiveOrder = effectiveConfig.Tabs
            .First(tab => tab.Id.Equals(tabId, StringComparison.OrdinalIgnoreCase))
            .Buttons.Select(button => button.Id)
            .ToList();
        var index = effectiveOrder.FindIndex(id => id.Equals(buttonId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return updated;
        }

        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= effectiveOrder.Count)
        {
            return updated;
        }

        (effectiveOrder[index], effectiveOrder[newIndex]) = (effectiveOrder[newIndex], effectiveOrder[index]);
        editableTab.ButtonOrder = effectiveOrder;
        return updated;
    }

    private static void RemoveButtonEverywhere(LaunchpadConfig config, string buttonId)
    {
        foreach (var tab in config.Tabs)
        {
            tab.Buttons.RemoveAll(button => button.Id.Equals(buttonId, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static LaunchpadTab GetOrCreateEditableTab(LaunchpadConfig userConfig, LaunchpadConfig effectiveConfig, string tabId)
    {
        var existing = userConfig.Tabs.FirstOrDefault(tab => tab.Id.Equals(tabId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var effectiveTab = effectiveConfig.Tabs.First(tab => tab.Id.Equals(tabId, StringComparison.OrdinalIgnoreCase)).Clone();
        effectiveTab.IsReadOnly = false;
        effectiveTab.Buttons = [];
        userConfig.Tabs.Add(effectiveTab);
        userConfig.HiddenTabIds.RemoveAll(id => id.Equals(tabId, StringComparison.OrdinalIgnoreCase));
        return effectiveTab;
    }

    private static bool IsReadOnlyEffectiveButton(LaunchpadConfig effectiveConfig, string buttonId)
    {
        return effectiveConfig.Tabs
            .SelectMany(tab => tab.Buttons)
            .Any(button => button.Id.Equals(buttonId, StringComparison.OrdinalIgnoreCase) && button.IsReadOnly);
    }

    private static bool IsReadOnlyEffectiveTab(LaunchpadConfig effectiveConfig, string tabId)
    {
        return effectiveConfig.Tabs.Any(tab =>
            tab.Id.Equals(tabId, StringComparison.OrdinalIgnoreCase) && tab.IsReadOnly);
    }

    public static string CreateId(string value)
    {
        var cleaned = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');

        return string.IsNullOrWhiteSpace(cleaned) ? Guid.NewGuid().ToString("N") : cleaned;
    }

    private static string CreateUniqueId(string value, ISet<string> existingIds)
    {
        var baseId = CreateId(value);
        if (!existingIds.Contains(baseId))
        {
            return baseId;
        }

        var suffix = 2;
        var candidate = $"{baseId}-{suffix}";
        while (existingIds.Contains(candidate))
        {
            suffix++;
            candidate = $"{baseId}-{suffix}";
        }

        return candidate;
    }
}
