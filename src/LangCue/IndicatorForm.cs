using System.Drawing;
using System.Drawing.Drawing2D;

namespace LangCue;

internal sealed class IndicatorForm : Form
{
    private const int BadgeWidth = 40;
    private const int BadgeHeight = 24;
    private const int Gap = 8;

    private readonly Label _label;

    public IndicatorForm()
    {
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(BadgeWidth, BadgeHeight);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Opacity = 0.92;
        BackColor = Color.FromArgb(34, 91, 171);

        _label = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        Controls.Add(_label);

        using var path = new GraphicsPath();
        const int diameter = 12;
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(BadgeWidth - diameter - 1, 0, diameter, diameter, 270, 90);
        path.AddArc(BadgeWidth - diameter - 1, BadgeHeight - diameter - 1, diameter, diameter, 0, 90);
        path.AddArc(0, BadgeHeight - diameter - 1, diameter, diameter, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }

    // A click-through window that never becomes the active keyboard target.
    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExTransparent = 0x00000020;
            const int wsExToolWindow = 0x00000080;
            const int wsExLayered = 0x00080000;
            const int wsExNoActivate = 0x08000000;

            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= wsExTransparent | wsExToolWindow | wsExLayered | wsExNoActivate;
            return parameters;
        }
    }

    protected override bool ShowWithoutActivation => true;

    public void UpdateDisplay(CaretSnapshot snapshot)
    {
        if (_label.Text != snapshot.Language)
        {
            _label.Text = snapshot.Language;
            BackColor = snapshot.Language == "فا"
                ? Color.FromArgb(15, 119, 95)
                : Color.FromArgb(34, 91, 171);
        }

        Rectangle area = Screen.FromPoint(snapshot.Position).WorkingArea;
        int x = snapshot.Position.X + Gap;
        int y = snapshot.Position.Y - 3;

        if (x + Width > area.Right)
        {
            x = snapshot.Position.X - Width - Gap;
        }

        if (y + Height > area.Bottom)
        {
            y = snapshot.Position.Y - Height - Gap;
        }

        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - Width));
        y = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - Height));

        Point location = new(x, y);
        if (Location != location)
        {
            Location = location;
        }

        if (!Visible)
        {
            Show();
        }
    }

    public void HideDisplay()
    {
        if (Visible)
        {
            Hide();
        }
    }
}
