namespace SupportLaunchpad.WinForms;

public sealed class TextPromptForm : Form
{
    private readonly TextBox _textBox = new() { Dock = DockStyle.Top };

    public TextPromptForm(string title, string labelText, string initialValue = "")
    {
        Text = title;
        Width = 380;
        Height = 150;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label { Text = labelText, Dock = DockStyle.Top, Height = 22 };
        _textBox.Text = initialValue;

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(okButton);

        Controls.Add(_textBox);
        Controls.Add(label);
        Controls.Add(buttons);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public string Value => _textBox.Text.Trim();
}
