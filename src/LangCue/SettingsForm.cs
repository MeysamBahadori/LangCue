using System.Drawing;

namespace LangCue;

internal sealed class SettingsForm : Form
{
    private readonly ComboBox _positionChoice;
    private readonly NumericUpDown _gapChoice;

    public IndicatorSettings SelectedSettings { get; private set; }

    public SettingsForm(IndicatorSettings current)
    {
        SelectedSettings = current;

        Text = "LangCue Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ShowIcon = false;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(365, 188);
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9f);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var positionLabel = new Label
        {
            Text = "Position",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _positionChoice = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _positionChoice.Items.AddRange(
        [
            new PositionOption(IndicatorPosition.Above, "Above caret"),
            new PositionOption(IndicatorPosition.Ahead, "Ahead of caret (text direction)"),
            new PositionOption(IndicatorPosition.Behind, "Behind caret (text direction)"),
            new PositionOption(IndicatorPosition.Below, "Below caret")
        ]);
        _positionChoice.SelectedIndex = (int)current.Position;

        var gapLabel = new Label
        {
            Text = "Gap (pixels)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _gapChoice = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 95,
            Minimum = 0,
            Maximum = 80,
            Value = current.Gap
        };

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += SaveSettings;

        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        footer.Controls.Add(saveButton);
        footer.Controls.Add(cancelButton);

        layout.Controls.Add(positionLabel, 0, 0);
        layout.Controls.Add(_positionChoice, 1, 0);
        layout.Controls.Add(gapLabel, 0, 1);
        layout.Controls.Add(_gapChoice, 1, 1);
        layout.Controls.Add(footer, 0, 2);
        layout.SetColumnSpan(footer, 2);
        Controls.Add(layout);
    }

    private void SaveSettings(object? sender, EventArgs e)
    {
        var position = ((PositionOption)_positionChoice.SelectedItem!).Value;
        var updated = new IndicatorSettings
        {
            Position = position,
            Gap = (int)_gapChoice.Value
        };

        try
        {
            SettingsStore.Save(updated);
            SelectedSettings = updated;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Cannot save settings:\n{exception.Message}",
                "LangCue", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed record PositionOption(IndicatorPosition Value, string Label)
    {
        public override string ToString() => Label;
    }
}
