using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.WinForms;

public sealed class SettingsForm : Form
{
    private readonly CheckBox _useSharedConfigCheckBox = new() { Text = "Use shared team config", AutoSize = true };
    private readonly TextBox _sharedConfigPathTextBox = new();
    private readonly CheckBox _warnIfUnavailableCheckBox = new() { Text = "Warn if shared config is unavailable", AutoSize = true };
    private readonly CheckBox _fallbackToLocalOnlyCheckBox = new() { Text = "If unavailable, continue with local-only mode", AutoSize = true };

    public SettingsForm(LocalAppSettings settings)
    {
        Settings = settings;

        Text = "Settings";
        Width = 620;
        Height = 240;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _useSharedConfigCheckBox.Checked = settings.UseSharedConfig;
        _sharedConfigPathTextBox.Text = settings.SharedConfigPath;
        _warnIfUnavailableCheckBox.Checked = settings.WarnIfSharedConfigUnavailable;
        _fallbackToLocalOnlyCheckBox.Checked = settings.FallbackToLocalOnly;
        _useSharedConfigCheckBox.CheckedChanged += (_, _) => UpdateEnabledState();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 0
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        layout.Controls.Add(_useSharedConfigCheckBox, 0, layout.RowCount);
        layout.SetColumnSpan(_useSharedConfigCheckBox, 3);
        layout.RowCount++;

        AddPathRow(layout);
        layout.Controls.Add(_warnIfUnavailableCheckBox, 0, layout.RowCount);
        layout.SetColumnSpan(_warnIfUnavailableCheckBox, 3);
        layout.RowCount++;
        layout.Controls.Add(_fallbackToLocalOnlyCheckBox, 0, layout.RowCount);
        layout.SetColumnSpan(_fallbackToLocalOnlyCheckBox, 3);
        layout.RowCount++;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft };
        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        okButton.Click += (_, _) => SaveSettings();
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(okButton);

        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = okButton;
        CancelButton = cancelButton;

        UpdateEnabledState();
    }

    public LocalAppSettings Settings { get; }

    private void AddPathRow(TableLayoutPanel layout)
    {
        var browseButton = new Button { Text = "Browse..." };
        var testButton = new Button { Text = "Test" };
        browseButton.Click += (_, _) => BrowseForConfig();
        testButton.Click += (_, _) => TestPath();

        layout.Controls.Add(new Label { Text = "Shared Config Path", AutoSize = true, Anchor = AnchorStyles.Left }, 0, layout.RowCount);
        _sharedConfigPathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sharedConfigPathTextBox.Width = 360;
        layout.Controls.Add(_sharedConfigPathTextBox, 1, layout.RowCount);

        var buttonPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        buttonPanel.Controls.Add(browseButton);
        buttonPanel.Controls.Add(testButton);
        layout.Controls.Add(buttonPanel, 2, layout.RowCount);
        layout.RowCount++;
    }

    private void BrowseForConfig()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = _sharedConfigPathTextBox.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _sharedConfigPathTextBox.Text = dialog.FileName;
        }
    }

    private void TestPath()
    {
        var exists = File.Exists(_sharedConfigPathTextBox.Text.Trim());
        MessageBox.Show(this, exists ? "Shared config file found." : "Shared config file was not found.", "Test Shared Config", MessageBoxButtons.OK, exists ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private void SaveSettings()
    {
        Settings.UseSharedConfig = _useSharedConfigCheckBox.Checked;
        Settings.SharedConfigPath = _sharedConfigPathTextBox.Text.Trim();
        Settings.WarnIfSharedConfigUnavailable = _warnIfUnavailableCheckBox.Checked;
        Settings.FallbackToLocalOnly = _fallbackToLocalOnlyCheckBox.Checked;
    }

    private void UpdateEnabledState()
    {
        var enabled = _useSharedConfigCheckBox.Checked;
        _sharedConfigPathTextBox.Enabled = enabled;
        _warnIfUnavailableCheckBox.Enabled = enabled;
        _fallbackToLocalOnlyCheckBox.Enabled = enabled;
    }
}
