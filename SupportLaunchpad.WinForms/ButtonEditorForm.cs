using SupportLaunchpad.Core.Models;
using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.WinForms;

public sealed class ButtonEditorForm : Form
{
    private readonly ComboBox _actionTypeComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _tabComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _descriptionTextBox = new();
    private readonly TextBox _pathTextBox = new();
    private readonly TextBox _argumentsTextBox = new();
    private readonly TextBox _workingDirectoryTextBox = new();
    private readonly CheckBox _runAsAdminCheckBox = new() { Text = "Run As Admin" };
    private readonly Button _okButton = new() { Text = "OK", DialogResult = DialogResult.OK };

    public ButtonEditorForm(IReadOnlyList<LaunchpadTab> tabs, string selectedTabId, LaunchpadButton button, bool readOnly)
    {
        Button = button;
        SelectedTabId = selectedTabId;

        Text = readOnly ? "View Button" : "Edit Button";
        Width = 520;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 0,
            AutoSize = true
        };

        _nameTextBox.Text = button.Name;
        _descriptionTextBox.Text = button.Description;
        _pathTextBox.Text = button.Path;
        _argumentsTextBox.Text = button.Arguments;
        _workingDirectoryTextBox.Text = button.WorkingDirectory;
        _runAsAdminCheckBox.Checked = button.RunAsAdmin;

        foreach (var actionType in Enum.GetValues<LaunchActionType>())
        {
            _actionTypeComboBox.Items.Add(actionType);
        }
        _actionTypeComboBox.SelectedItem = button.ActionType;

        var tabOptions = tabs.Select(tab => new TabOption(tab.Id, tab.Name)).ToList();
        foreach (var tabOption in tabOptions)
        {
            _tabComboBox.Items.Add(tabOption);
        }
        _tabComboBox.SelectedItem = tabOptions.First(tab => tab.Id.Equals(selectedTabId, StringComparison.OrdinalIgnoreCase));

        AddRow(layout, "Tab", _tabComboBox);
        AddRow(layout, "Name", _nameTextBox);
        AddRow(layout, "Description", _descriptionTextBox);
        AddRow(layout, "Action Type", _actionTypeComboBox);
        AddRow(layout, "Path / Command", _pathTextBox);
        AddRow(layout, "Arguments", _argumentsTextBox);
        AddRow(layout, "Working Directory", _workingDirectoryTextBox);
        layout.Controls.Add(_runAsAdminCheckBox, 1, layout.RowCount++);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft };
        var cancelButton = new Button { Text = readOnly ? "Close" : "Cancel", DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(cancelButton);
        if (!readOnly)
        {
            buttons.Controls.Add(_okButton);
            _okButton.Click += (_, eventArgs) => SaveButton();
        }

        SetReadOnly(readOnly);
        Controls.Add(layout);
        Controls.Add(buttons);

        AcceptButton = readOnly ? cancelButton : _okButton;
        CancelButton = cancelButton;
    }

    public LaunchpadButton Button { get; }

    public string SelectedTabId { get; private set; }

    private void SaveButton()
    {
        Button.Name = _nameTextBox.Text.Trim();
        Button.Description = _descriptionTextBox.Text.Trim();
        Button.ActionType = (LaunchActionType)_actionTypeComboBox.SelectedItem!;
        Button.Path = _pathTextBox.Text.Trim();
        Button.Arguments = _argumentsTextBox.Text.Trim();
        Button.WorkingDirectory = _workingDirectoryTextBox.Text.Trim();
        Button.RunAsAdmin = _runAsAdminCheckBox.Checked;
        Button.Id = string.IsNullOrWhiteSpace(Button.Id) ? UserConfigEditor.CreateId(Button.Name) : Button.Id;
        SelectedTabId = ((TabOption)_tabComboBox.SelectedItem!).Id;
    }

    private void SetReadOnly(bool readOnly)
    {
        foreach (var control in Controls.OfType<TableLayoutPanel>().SelectMany(panel => panel.Controls.OfType<Control>()))
        {
            control.Enabled = !readOnly;
        }
    }

    private static void AddRow(TableLayoutPanel layout, string labelText, Control control)
    {
        var rowIndex = layout.RowCount;
        layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = labelText, AutoSize = true }, 0, rowIndex);
        control.Width = 320;
        layout.Controls.Add(control, 1, rowIndex);
    }

    private sealed record TabOption(string Id, string Name)
    {
        public override string ToString() => Name;
    }
}
