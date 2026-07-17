using SupportLaunchpad.Core.Models;
using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.WinForms;

public sealed class TeamConfigEditorForm : Form
{
    private readonly LaunchpadValidator _validator;
    private readonly TextBox _titleTextBox = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _allowPowerShellCheckBox = new() { Text = "Allow PowerShell scripts", AutoSize = true };
    private readonly CheckBox _allowAdminCheckBox = new() { Text = "Allow Run as Administrator", AutoSize = true };
    private readonly CheckBox _restrictScriptsCheckBox = new() { Text = "Restrict PowerShell to approved directories", AutoSize = true };
    private readonly TextBox _scriptDirectoriesTextBox = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 52, Dock = DockStyle.Fill };
    private readonly ListBox _tabsList = new() { Dock = DockStyle.Fill, DisplayMember = nameof(LaunchpadTab.Name) };
    private readonly ListBox _buttonsList = new() { Dock = DockStyle.Fill, DisplayMember = nameof(LaunchpadButton.Name) };
    private readonly CheckBox _protectTabCheckBox = new() { Text = "Team-managed tab", AutoSize = true };

    public TeamConfigEditorForm(LaunchpadConfig config, LaunchpadValidator validator)
    {
        Config = config.Clone();
        _validator = validator;

        Text = "Manage Team Launchpad";
        Width = 920;
        Height = 680;
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterParent;

        _titleTextBox.Text = Config.Title;
        _allowPowerShellCheckBox.Checked = Config.Settings.AllowPowerShellScripts;
        _allowAdminCheckBox.Checked = Config.Settings.AllowRunAsAdmin;
        _restrictScriptsCheckBox.Checked = Config.Settings.RestrictPowerShellToAllowedDirectories;
        _scriptDirectoriesTextBox.Lines = [.. Config.Settings.AllowedScriptDirectories];
        _scriptDirectoriesTextBox.Enabled = _restrictScriptsCheckBox.Checked;

        _restrictScriptsCheckBox.CheckedChanged += (_, _) => _scriptDirectoriesTextBox.Enabled = _restrictScriptsCheckBox.Checked;
        _tabsList.SelectedIndexChanged += (_, _) => RefreshButtons();
        _protectTabCheckBox.CheckedChanged += (_, _) => UpdateTabProtection();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(BuildSettingsPanel(), 0, 0);
        root.Controls.Add(BuildEditorPanel(), 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);
        Controls.Add(root);

        RefreshTabs();
    }

    public LaunchpadConfig Config { get; }

    private Control BuildSettingsPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = "Team title", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        panel.Controls.Add(_titleTextBox, 1, 0);

        var policyPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Dock = DockStyle.Fill };
        policyPanel.Controls.AddRange([_allowPowerShellCheckBox, _allowAdminCheckBox, _restrictScriptsCheckBox]);
        panel.Controls.Add(policyPanel, 0, 1);
        panel.SetColumnSpan(policyPanel, 2);
        panel.Controls.Add(new Label { Text = "Approved script directories (one per line)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        panel.Controls.Add(_scriptDirectoriesTextBox, 1, 2);
        return panel;
    }

    private Control BuildEditorPanel()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 300 };
        split.Panel1.Controls.Add(BuildTabsPanel());
        split.Panel2.Controls.Add(BuildButtonsPanel());
        return split;
    }

    private Control BuildTabsPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 6, 0) };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 72, WrapContents = true };
        actions.Controls.Add(CreateActionButton("Add Tab", AddTab));
        actions.Controls.Add(CreateActionButton("Rename", RenameTab));
        actions.Controls.Add(CreateActionButton("Delete", DeleteTab));
        actions.Controls.Add(CreateActionButton("Move Up", () => MoveTab(-1)));
        actions.Controls.Add(CreateActionButton("Move Down", () => MoveTab(1)));
        actions.Controls.Add(_protectTabCheckBox);
        panel.Controls.Add(_tabsList);
        panel.Controls.Add(actions);
        panel.Controls.Add(new Label { Text = "Team tabs", Dock = DockStyle.Top, Height = 24, Font = new Font(Font, FontStyle.Bold) });
        return panel;
    }

    private Control BuildButtonsPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 10, 0, 0) };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 72, WrapContents = true };
        actions.Controls.Add(CreateActionButton("Add Resource", AddButton));
        actions.Controls.Add(CreateActionButton("Edit", EditButton));
        actions.Controls.Add(CreateActionButton("Delete", DeleteButton));
        actions.Controls.Add(CreateActionButton("Move Up", () => MoveButton(-1)));
        actions.Controls.Add(CreateActionButton("Move Down", () => MoveButton(1)));
        panel.Controls.Add(_buttonsList);
        panel.Controls.Add(actions);
        panel.Controls.Add(new Label { Text = "Links, folders, applications, and tools", Dock = DockStyle.Top, Height = 24, Font = new Font(Font, FontStyle.Bold) });
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var publish = new Button { Text = "Publish Team Config", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        publish.Click += (_, _) => Publish();
        footer.Controls.Add(publish);
        footer.Controls.Add(cancel);
        AcceptButton = publish;
        CancelButton = cancel;
        return footer;
    }

    private static Button CreateActionButton(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => action();
        return button;
    }

    private LaunchpadTab? SelectedTab => _tabsList.SelectedItem as LaunchpadTab;
    private LaunchpadButton? SelectedButton => _buttonsList.SelectedItem as LaunchpadButton;

    private void RefreshTabs(string? selectedId = null)
    {
        selectedId ??= SelectedTab?.Id;
        _tabsList.DataSource = null;
        _tabsList.DataSource = Config.Tabs;
        _tabsList.SelectedItem = Config.Tabs.FirstOrDefault(tab => tab.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase)) ?? Config.Tabs.FirstOrDefault();
        RefreshButtons();
    }

    private void RefreshButtons(string? selectedId = null)
    {
        selectedId ??= SelectedButton?.Id;
        var tab = SelectedTab;
        _buttonsList.DataSource = null;
        _buttonsList.DataSource = tab?.Buttons;
        if (tab is not null)
        {
            _buttonsList.SelectedItem = tab.Buttons.FirstOrDefault(button => button.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
            _protectTabCheckBox.Checked = tab.IsReadOnly;
            _protectTabCheckBox.Enabled = true;
        }
        else
        {
            _protectTabCheckBox.Checked = false;
            _protectTabCheckBox.Enabled = false;
        }
    }

    private void AddTab()
    {
        using var prompt = new TextPromptForm("Add Team Tab", "Tab name");
        if (prompt.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(prompt.Value)) return;
        var updated = new UserConfigEditor().AddTab(Config, Config, prompt.Value);
        Config.Tabs = updated.Tabs;
        RefreshTabs(Config.Tabs.Last().Id);
    }

    private void RenameTab()
    {
        var tab = SelectedTab;
        if (tab is null) return;
        using var prompt = new TextPromptForm("Rename Team Tab", "Tab name", tab.Name);
        if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(prompt.Value))
        {
            tab.Name = prompt.Value.Trim();
            RefreshTabs(tab.Id);
        }
    }

    private void DeleteTab()
    {
        var tab = SelectedTab;
        if (tab is null || MessageBox.Show(this, $"Delete '{tab.Name}' and all of its resources?", "Delete Team Tab", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Config.Tabs.Remove(tab);
        RefreshTabs();
    }

    private void MoveTab(int direction)
    {
        var tab = SelectedTab;
        if (tab is null) return;
        var index = Config.Tabs.IndexOf(tab);
        var target = index + direction;
        if (target < 0 || target >= Config.Tabs.Count) return;
        Config.Tabs.RemoveAt(index);
        Config.Tabs.Insert(target, tab);
        RefreshTabs(tab.Id);
    }

    private void UpdateTabProtection()
    {
        if (SelectedTab is { } tab && _protectTabCheckBox.Enabled)
        {
            tab.IsReadOnly = _protectTabCheckBox.Checked;
        }
    }

    private void AddButton()
    {
        var tab = SelectedTab;
        if (tab is null) return;
        var button = new LaunchpadButton { Id = Guid.NewGuid().ToString("N"), CreatedUtc = DateTime.UtcNow, ModifiedUtc = DateTime.UtcNow, IsReadOnly = true };
        using var form = new ButtonEditorForm(Config.Tabs, tab.Id, button, false, true);
        if (form.ShowDialog(this) != DialogResult.OK || !Validate(form.Button)) return;
        Config.Tabs.First(candidate => candidate.Id.Equals(form.SelectedTabId, StringComparison.OrdinalIgnoreCase)).Buttons.Add(form.Button.Clone());
        RefreshTabs(form.SelectedTabId);
        RefreshButtons(form.Button.Id);
    }

    private void EditButton()
    {
        var tab = SelectedTab;
        var button = SelectedButton;
        if (tab is null || button is null) return;
        var edited = button.Clone();
        using var form = new ButtonEditorForm(Config.Tabs, tab.Id, edited, false, true);
        if (form.ShowDialog(this) != DialogResult.OK || !Validate(form.Button)) return;
        var originalIndex = tab.Buttons.IndexOf(button);
        tab.Buttons.Remove(button);
        var destination = Config.Tabs.First(candidate => candidate.Id.Equals(form.SelectedTabId, StringComparison.OrdinalIgnoreCase));
        if (ReferenceEquals(tab, destination))
        {
            destination.Buttons.Insert(originalIndex, form.Button.Clone());
        }
        else
        {
            destination.Buttons.Add(form.Button.Clone());
        }
        RefreshTabs(form.SelectedTabId);
        RefreshButtons(form.Button.Id);
    }

    private void DeleteButton()
    {
        var tab = SelectedTab;
        var button = SelectedButton;
        if (tab is null || button is null || MessageBox.Show(this, $"Delete '{button.Name}'?", "Delete Team Resource", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        tab.Buttons.Remove(button);
        RefreshButtons();
    }

    private void MoveButton(int direction)
    {
        var tab = SelectedTab;
        var button = SelectedButton;
        if (tab is null || button is null) return;
        var index = tab.Buttons.IndexOf(button);
        var target = index + direction;
        if (target < 0 || target >= tab.Buttons.Count) return;
        tab.Buttons.RemoveAt(index);
        tab.Buttons.Insert(target, button);
        RefreshButtons(button.Id);
    }

    private bool Validate(LaunchpadButton button)
    {
        var result = _validator.ValidateButton(button);
        if (result.IsValid) return true;
        MessageBox.Show(this, string.Join(Environment.NewLine, result.Issues.Where(issue => !issue.IsWarning).Select(issue => issue.Message)), "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private void Publish()
    {
        Config.Title = string.IsNullOrWhiteSpace(_titleTextBox.Text) ? "Team Launchpad" : _titleTextBox.Text.Trim();
        Config.Settings.AllowPowerShellScripts = _allowPowerShellCheckBox.Checked;
        Config.Settings.AllowRunAsAdmin = _allowAdminCheckBox.Checked;
        Config.Settings.RestrictPowerShellToAllowedDirectories = _restrictScriptsCheckBox.Checked;
        Config.Settings.AllowedScriptDirectories = _scriptDirectoriesTextBox.Lines
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var invalid = Config.Tabs.SelectMany(tab => tab.Buttons).FirstOrDefault(button => !_validator.ValidateButton(button).IsValid);
        if (invalid is not null)
        {
            MessageBox.Show(this, $"'{invalid.Name}' is not valid. Edit it before publishing.", "Cannot Publish", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
