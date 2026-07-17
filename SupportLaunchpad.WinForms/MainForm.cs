using SupportLaunchpad.Core.Models;
using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.WinForms;

public sealed class MainForm : Form
{
    private readonly ConfigStore _configStore;
    private readonly LaunchpadValidator _validator;
    private readonly LaunchService _launchService;
    private readonly UserConfigEditor _userConfigEditor;

    private LocalAppSettings _appSettings = new();
    private LaunchpadConfig _userConfig = new();
    private LaunchpadConfig _sharedConfig = new();
    private LaunchpadConfig _effectiveConfig = new();
    private bool _editMode;

    private readonly Panel _toolbarPanel = new() { Dock = DockStyle.Top, Height = 92, Padding = new Padding(8) };
    private readonly FlowLayoutPanel _editActionsPanel = new() { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
    private readonly Button _toggleEditButton = new() { Width = 90, Text = "Edit", Anchor = AnchorStyles.Top | AnchorStyles.Right };
    private readonly Button _settingsButton = new() { Width = 90, Text = "Settings", Anchor = AnchorStyles.Top | AnchorStyles.Right };
    private readonly Button _refreshButton = new() { Width = 80, Text = "Refresh", Anchor = AnchorStyles.Top | AnchorStyles.Right };
    private readonly Button _manageTeamButton = new() { Width = 110, Text = "Manage Team", Anchor = AnchorStyles.Top | AnchorStyles.Right };
    private readonly TextBox _searchTextBox = new() { Width = 280, PlaceholderText = "Search tools, links, folders..." };
    private readonly Button _addTabButton = new() { Text = "Add Tab", AutoSize = true };
    private readonly Button _renameTabButton = new() { Text = "Rename Tab", AutoSize = true };
    private readonly Button _deleteTabButton = new() { Text = "Delete Tab", AutoSize = true };
    private readonly Button _addButtonButton = new() { Text = "Add Button", AutoSize = true };
    private readonly Button _saveButton = new() { Text = "Save", AutoSize = true };
    private readonly Label _statusLabel = new() { Dock = DockStyle.Bottom, Height = 18, AutoEllipsis = true, ForeColor = SystemColors.GrayText };
    private readonly TabControl _tabControl = new() { Dock = DockStyle.Fill };
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 12000, InitialDelay = 350, ReshowDelay = 100 };

    public MainForm(ConfigStore configStore, LaunchpadValidator validator, LaunchService launchService, UserConfigEditor userConfigEditor)
    {
        _configStore = configStore;
        _validator = validator;
        _launchService = launchService;
        _userConfigEditor = userConfigEditor;

        SuspendLayout();
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        _toggleEditButton.Location = new Point(Width - 130, 8);
        _settingsButton.Location = new Point(Width - 228, 8);
        _toggleEditButton.Click += (_, _) => ToggleEditMode();
        _settingsButton.Click += (_, _) => EditSettings();
        _refreshButton.Click += (_, _) => ReloadState();
        _manageTeamButton.Click += (_, _) => ManageTeamConfig();
        _searchTextBox.TextChanged += (_, _) => RenderTabs();
        _addTabButton.Click += (_, _) => AddTab();
        _renameTabButton.Click += (_, _) => RenameSelectedTab();
        _deleteTabButton.Click += (_, _) => DeleteSelectedTab();
        _addButtonButton.Click += (_, _) => AddButton();
        _saveButton.Click += (_, _) => SaveChanges();

        _editActionsPanel.Controls.AddRange([_addTabButton, _renameTabButton, _deleteTabButton, _addButtonButton, _saveButton]);
        _toolbarPanel.Controls.Add(_editActionsPanel);
        _toolbarPanel.Controls.Add(_settingsButton);
        _toolbarPanel.Controls.Add(_refreshButton);
        _toolbarPanel.Controls.Add(_manageTeamButton);
        _toolbarPanel.Controls.Add(_toggleEditButton);
        _toolbarPanel.Controls.Add(_searchTextBox);
        _toolbarPanel.Controls.Add(_statusLabel);
        _toolbarPanel.Resize += (_, _) => PositionToggleButton();
        _tabControl.SelectedIndexChanged += (_, _) => UpdateEditControls();

        Controls.Add(_tabControl);
        Controls.Add(_toolbarPanel);

        ResumeLayout();
        Load += (_, _) => ReloadState();
    }

    private void ReloadState()
    {
        MergedLaunchpadState state;
        try
        {
            state = _configStore.Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"The launchpad configuration could not be loaded.\n\n{ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        _appSettings = state.AppSettings;
        _userConfig = state.UserConfig;
        _sharedConfig = state.SharedConfig;
        _effectiveConfig = state.EffectiveConfig;
        Text = _effectiveConfig.Title;
        _statusLabel.Text = $"{state.SharedConfigStatus} | Config v{_effectiveConfig.Version}";
        RenderTabs();
        UpdateEditControls();

        if (_appSettings.UseSharedConfig && !_editMode && !state.SharedConfigLoaded && _appSettings.WarnIfSharedConfigUnavailable)
        {
            BeginInvoke(new MethodInvoker(() =>
                MessageBox.Show(this, state.SharedConfigStatus, "Shared Config", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
        }
    }

    private void RenderTabs()
    {
        var selectedTabId = GetSelectedTab()?.Id;
        var query = _searchTextBox.Text.Trim();
        _tabControl.TabPages.Clear();

        foreach (var tab in _effectiveConfig.Tabs)
        {
            var matchingButtons = tab.Buttons.Where(button => MatchesSearch(tab, button, query)).ToList();
            if (!string.IsNullOrWhiteSpace(query) && matchingButtons.Count == 0)
            {
                continue;
            }

            var page = new TabPage(tab.Name) { Tag = tab };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16),
                WrapContents = true
            };

            foreach (var button in matchingButtons)
            {
                flow.Controls.Add(CreateLaunchButton(tab, button));
            }

            page.Controls.Add(flow);
            _tabControl.TabPages.Add(page);
        }

        if (!string.IsNullOrWhiteSpace(selectedTabId))
        {
            _tabControl.SelectedTab = _tabControl.TabPages.Cast<TabPage>()
                .FirstOrDefault(page => (page.Tag as LaunchpadTab)?.Id.Equals(selectedTabId, StringComparison.OrdinalIgnoreCase) == true);
        }
    }

    private Control CreateLaunchButton(LaunchpadTab tab, LaunchpadButton button)
    {
        var title = new Button
        {
            Width = 210,
            Height = 56,
            Margin = new Padding(8),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 12, 0),
            Text = $"{button.Name}\n{button.ActionType}{(IsTeamButton(button.Id) ? " • Team" : " • Personal")}",
            Tag = new ButtonContext(tab.Id, button.Id)
        };

        var tooltip = string.Join(Environment.NewLine,
            new[] { button.Description, button.Path }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        _toolTip.SetToolTip(title, tooltip);
        TryApplyIcon(title, button.IconPath);

        title.Click += (_, _) => LaunchButton(button);
        title.MouseUp += (_, args) => ShowButtonMenu(args, tab, button);
        return title;
    }

    private void ShowButtonMenu(MouseEventArgs args, LaunchpadTab tab, LaunchpadButton button)
    {
        if (!_editMode || args.Button != MouseButtons.Right)
        {
            return;
        }

        var menu = new ContextMenuStrip();
        menu.Items.Add("View Button", null, (_, _) => EditButton(tab.Id, button, true));

        if (!button.IsReadOnly)
        {
            menu.Items.Add("Edit Button", null, (_, _) => EditButton(tab.Id, button, false));
            menu.Items.Add("Delete Button", null, (_, _) => DeleteButton(button));

            var moveToTabMenu = new ToolStripMenuItem("Move to Tab");
            foreach (var candidate in _effectiveConfig.Tabs.Where(candidate =>
                         !candidate.IsReadOnly &&
                         !candidate.Id.Equals(tab.Id, StringComparison.OrdinalIgnoreCase)))
            {
                moveToTabMenu.DropDownItems.Add(candidate.Name, null, (_, _) => MoveButtonToTab(tab.Id, candidate.Id, button.Id));
            }

            menu.Items.Add(moveToTabMenu);
            menu.Items.Add("Move Left", null, (_, _) => MoveButton(tab.Id, button.Id, -1));
            menu.Items.Add("Move Right", null, (_, _) => MoveButton(tab.Id, button.Id, 1));
        }
        menu.Show(Cursor.Position);
    }

    private void LaunchButton(LaunchpadButton button)
    {
        var result = _launchService.Launch(new LaunchRequest
        {
            Button = button,
            Settings = _effectiveConfig.Settings
        });

        if (!result.Success)
        {
            MessageBox.Show(this, result.ErrorMessage, "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleEditMode()
    {
        _editMode = !_editMode;
        UpdateEditControls();
    }

    private void UpdateEditControls()
    {
        _toggleEditButton.Text = _editMode ? "Done" : "Edit";
        _editActionsPanel.Visible = _editMode;
        var selectedTabIsEditable = GetSelectedTab()?.IsReadOnly == false;
        _renameTabButton.Enabled = selectedTabIsEditable;
        _deleteTabButton.Enabled = selectedTabIsEditable;
        _addButtonButton.Enabled = selectedTabIsEditable;
        _manageTeamButton.Enabled = _appSettings.UseSharedConfig && !string.IsNullOrWhiteSpace(_appSettings.SharedConfigPath);
        PositionToggleButton();
    }

    private void PositionToggleButton()
    {
        _toggleEditButton.Left = _toolbarPanel.ClientSize.Width - _toggleEditButton.Width - 8;
        _settingsButton.Left = _toggleEditButton.Left - _settingsButton.Width - 8;
        _refreshButton.Left = _settingsButton.Left - _refreshButton.Width - 8;
        _manageTeamButton.Left = _refreshButton.Left - _manageTeamButton.Width - 8;
        _settingsButton.Top = 8;
        _refreshButton.Top = 8;
        _manageTeamButton.Top = 8;
        _toggleEditButton.Top = 8;
        _searchTextBox.Left = 8;
        _searchTextBox.Top = 46;
    }

    private static bool MatchesSearch(LaunchpadTab tab, LaunchpadButton button, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return new[] { tab.Name, button.Name, button.Description, button.Path, button.ActionType.ToString() }
            .Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsTeamButton(string buttonId)
    {
        return _sharedConfig.Tabs.SelectMany(tab => tab.Buttons)
            .Any(button => button.Id.Equals(buttonId, StringComparison.OrdinalIgnoreCase));
    }

    private static void TryApplyIcon(Button control, string iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return;
        }

        try
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(iconPath);
            if (!File.Exists(expandedPath))
            {
                return;
            }

            using var source = Image.FromFile(expandedPath);
            control.Image = new Bitmap(source, new Size(24, 24));
            control.ImageAlign = ContentAlignment.MiddleLeft;
            control.TextImageRelation = TextImageRelation.ImageBeforeText;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
        {
            // Invalid icons should not prevent the launchpad from rendering.
        }
    }

    private void ManageTeamConfig()
    {
        if (!_appSettings.UseSharedConfig || string.IsNullOrWhiteSpace(_appSettings.SharedConfigPath))
        {
            MessageBox.Show(this, "Choose a shared team configuration path in Settings first.", "Team Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var startingConfig = _sharedConfig.Tabs.Count > 0
            ? _sharedConfig
            : new LaunchpadConfig
            {
                Title = "Team Launchpad",
                Tabs = [new LaunchpadTab { Id = "start-here", Name = "Start Here", IsReadOnly = true }]
            };

        using var form = new TeamConfigEditorForm(startingConfig, _validator);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _configStore.SaveSharedConfig(_appSettings.SharedConfigPath, form.Config);
            MessageBox.Show(this, "The team launchpad was published successfully.", "Team Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"The team configuration could not be published. Check your access to the shared location.\n\n{ex.Message}", "Publish Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ReloadState();
    }

    private void EditSettings()
    {
        using var form = new SettingsForm(_appSettings.Clone());
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _configStore.SaveAppSettings(form.Settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"The settings could not be saved.\n\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ReloadState();
    }

    private LaunchpadTab? GetSelectedTab()
    {
        return _tabControl.SelectedTab?.Tag as LaunchpadTab;
    }

    private void AddTab()
    {
        using var prompt = new TextPromptForm("Add Tab", "Tab Name");
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(prompt.Value))
        {
            MessageBox.Show(this, "Tab name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _userConfig = _userConfigEditor.AddTab(_userConfig, _effectiveConfig, prompt.Value);
        SaveAndReload();
    }

    private void RenameSelectedTab()
    {
        var tab = GetSelectedTab();
        if (tab is null || tab.IsReadOnly)
        {
            return;
        }

        using var prompt = new TextPromptForm("Rename Tab", "Tab Name", tab.Name);
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(prompt.Value))
        {
            MessageBox.Show(this, "Tab name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _userConfig = _userConfigEditor.RenameTab(_userConfig, _effectiveConfig, tab.Id, prompt.Value);
        SaveAndReload();
    }

    private void DeleteSelectedTab()
    {
        var tab = GetSelectedTab();
        if (tab is null || tab.IsReadOnly)
        {
            return;
        }

        if (MessageBox.Show(this, $"Delete tab '{tab.Name}'?", "Delete Tab", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _userConfig = _userConfigEditor.DeleteTab(_userConfig, _effectiveConfig, tab.Id);
        SaveAndReload();
    }

    private void AddButton()
    {
        var tab = GetSelectedTab();
        if (tab is null || tab.IsReadOnly)
        {
            return;
        }

        var newButton = new LaunchpadButton
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow
        };

        using var form = new ButtonEditorForm(_effectiveConfig.Tabs, tab.Id, newButton, false);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var editedButton = form.Button;
        editedButton.ModifiedUtc = DateTime.UtcNow;
        var validation = _validator.ValidateButton(editedButton);
        if (!validation.IsValid)
        {
            ShowValidationErrors(validation);
            return;
        }

        _userConfig = _userConfigEditor.UpsertButton(_userConfig, _effectiveConfig, form.SelectedTabId, editedButton);
        SaveAndReload();
    }

    private void EditButton(string currentTabId, LaunchpadButton sourceButton, bool readOnly)
    {
        using var form = new ButtonEditorForm(_effectiveConfig.Tabs, currentTabId, sourceButton.Clone(), readOnly);
        if (form.ShowDialog(this) != DialogResult.OK || readOnly)
        {
            return;
        }

        var editedButton = form.Button;
        editedButton.ModifiedUtc = DateTime.UtcNow;
        if (editedButton.CreatedUtc == default)
        {
            editedButton.CreatedUtc = DateTime.UtcNow;
        }

        var validation = _validator.ValidateButton(editedButton);
        if (!validation.IsValid)
        {
            ShowValidationErrors(validation);
            return;
        }

        _userConfig = _userConfigEditor.UpsertButton(_userConfig, _effectiveConfig, form.SelectedTabId, editedButton);
        SaveAndReload();
    }

    private void DeleteButton(LaunchpadButton button)
    {
        if (MessageBox.Show(this, $"Remove '{button.Name}'?", "Remove Button", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _userConfig = _userConfigEditor.DeleteButton(_userConfig, _effectiveConfig, button.Id);
        SaveAndReload();
    }

    private void MoveButtonToTab(string sourceTabId, string targetTabId, string buttonId)
    {
        _userConfig = _userConfigEditor.MoveButtonToTab(_userConfig, _effectiveConfig, sourceTabId, targetTabId, buttonId);
        SaveAndReload();
    }

    private void MoveButton(string tabId, string buttonId, int direction)
    {
        _userConfig = _userConfigEditor.MoveButtonWithinTab(_userConfig, _effectiveConfig, tabId, buttonId, direction);
        SaveAndReload();
    }

    private void SaveChanges()
    {
        SaveAndReload();
        _editMode = false;
        UpdateEditControls();
    }

    private void SaveAndReload()
    {
        try
        {
            _configStore.SaveUserConfig(_userConfig);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Your changes could not be saved.\n\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ReloadState();
    }

    private void ShowValidationErrors(ValidationResult validation)
    {
        var errors = string.Join(Environment.NewLine, validation.Issues.Where(issue => !issue.IsWarning).Select(issue => issue.Message));
        MessageBox.Show(this, errors, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private sealed record ButtonContext(string TabId, string ButtonId);
}
