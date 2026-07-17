using SupportLaunchpad.Core.Models;
using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.Tests;

public sealed class UserConfigEditorTests
{
    [Fact]
    public void DeleteTab_AllowsRemovingLastUserTab()
    {
        var editor = new UserConfigEditor();
        var userConfig = new LaunchpadConfig
        {
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "General"
                }
            ]
        };

        var effectiveConfig = userConfig.Clone();

        var updated = editor.DeleteTab(userConfig, effectiveConfig, "general");

        Assert.Empty(updated.Tabs);
    }

    [Fact]
    public void AddTab_CreatesUniqueId_WhenNameAlreadyExists()
    {
        var editor = new UserConfigEditor();
        var userConfig = new LaunchpadConfig
        {
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "General"
                }
            ]
        };

        var updated = editor.AddTab(userConfig, userConfig.Clone(), "General");

        Assert.Equal("general-2", updated.Tabs[1].Id);
    }

    [Fact]
    public void RenameTab_DoesNotCopySharedButtonsIntoUserConfig()
    {
        var editor = new UserConfigEditor();
        var userConfig = new LaunchpadConfig();
        var effectiveConfig = new LaunchpadConfig
        {
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "General",
                    Buttons =
                    [
                        new LaunchpadButton { Id = "shared", Name = "Shared", IsReadOnly = true }
                    ]
                }
            ]
        };

        var updated = editor.RenameTab(userConfig, effectiveConfig, "general", "Renamed");

        Assert.Single(updated.Tabs);
        Assert.Empty(updated.Tabs[0].Buttons);
    }

    [Fact]
    public void MoveButtonWithinTab_DoesNotMoveReadOnlySharedButton()
    {
        var editor = new UserConfigEditor();
        var userConfig = new LaunchpadConfig();
        var effectiveConfig = new LaunchpadConfig
        {
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "General",
                    Buttons =
                    [
                        new LaunchpadButton { Id = "shared", Name = "Shared", IsReadOnly = true },
                        new LaunchpadButton { Id = "user", Name = "User" }
                    ]
                }
            ]
        };

        var updated = editor.MoveButtonWithinTab(userConfig, effectiveConfig, "general", "shared", 1);

        Assert.Empty(updated.Tabs);
    }

    [Fact]
    public void MoveButtonWithinTab_PersistsOrderForSharedButtonsWithoutCopyingThem()
    {
        var editor = new UserConfigEditor();
        var userConfig = new LaunchpadConfig();
        var effectiveConfig = new LaunchpadConfig
        {
            Tabs =
            [
                new LaunchpadTab
                {
                    Id = "general",
                    Name = "General",
                    Buttons =
                    [
                        new LaunchpadButton { Id = "first", Name = "First" },
                        new LaunchpadButton { Id = "second", Name = "Second" }
                    ]
                }
            ]
        };

        var updated = editor.MoveButtonWithinTab(userConfig, effectiveConfig, "general", "second", -1);
        var merged = new LaunchpadConfigMerger().Merge(effectiveConfig, updated);

        Assert.Empty(updated.Tabs[0].Buttons);
        Assert.Equal(["second", "first"], updated.Tabs[0].ButtonOrder);
        Assert.Equal(["second", "first"], merged.Tabs[0].Buttons.Select(button => button.Id));
    }

    [Fact]
    public void TabEdits_DoNotModifyReadOnlySharedTab()
    {
        var editor = new UserConfigEditor();
        var userConfig = new LaunchpadConfig();
        var effectiveConfig = new LaunchpadConfig
        {
            Tabs = [new LaunchpadTab { Id = "team", Name = "Team", IsReadOnly = true }]
        };

        var renamed = editor.RenameTab(userConfig, effectiveConfig, "team", "Changed");
        var deleted = editor.DeleteTab(userConfig, effectiveConfig, "team");

        Assert.Empty(renamed.Tabs);
        Assert.Empty(deleted.HiddenTabIds);
    }

    [Fact]
    public void DeleteButton_DoesNotHideReadOnlySharedButton()
    {
        var editor = new UserConfigEditor();
        var userConfig = new LaunchpadConfig();
        var effectiveConfig = new LaunchpadConfig
        {
            Tabs = [new LaunchpadTab { Id = "team", Buttons = [new LaunchpadButton { Id = "required", IsReadOnly = true }] }]
        };

        var updated = editor.DeleteButton(userConfig, effectiveConfig, "required");

        Assert.Empty(updated.HiddenButtonIds);
    }
}
