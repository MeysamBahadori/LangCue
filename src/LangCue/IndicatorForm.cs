using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LangCue;

/// <summary>
/// Non-activating, click-through, per-pixel-alpha popup. Antialiased corners
/// are drawn into a layered window instead of clipping a rectangular Form region.
/// </summary>
internal sealed class IndicatorForm : Form
{
    private const int BadgeHeight = 25;
    private const int HorizontalPadding = 12;
    private const int MinBadgeWidth = 39;

    private string _badge = string.Empty;
    private bool _isRtl;
    private int _backgroundOpacity = -1;

    public IndicatorForm()
    {
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(MinBadgeWidth, BadgeHeight);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
    }

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

    public void UpdateDisplay(CaretSnapshot snapshot, IndicatorSettings settings)
    {
        // Never pin a caret that has scrolled out of the active viewport
        // to the nearest screen edge. Hide until fresh visible geometry exists.
        if (!CaretVisibility.IsWithinViewport(
                NativeMethods.GetForegroundWindow(), snapshot.Bounds))
        {
            HideDisplay();
            return;
        }

        string badge = snapshot.Language.Badge;
        bool isRtl = snapshot.Language.IsRightToLeft;

        // Three-letter languages and custom Windows layout IDs get more space.
        int width = Math.Max(MinBadgeWidth, badge.Length * 12 + HorizontalPadding * 2);
        bool mustRender = _badge != badge ||
                          _isRtl != isRtl ||
                          _backgroundOpacity != settings.BackgroundOpacity ||
                          Width != width || !Visible;

        _badge = badge;
        _isRtl = isRtl;
        _backgroundOpacity = settings.BackgroundOpacity;

        if (Width != width)
        {
            Width = width;
        }

        Rectangle caret = snapshot.Bounds;
        int gap = settings.Gap;
        int x;
        int y;

        switch (settings.Position)
        {
            case IndicatorPosition.Ahead:
                x = isRtl ? caret.Left - Width - gap : caret.Right + gap;
                y = caret.Top + (caret.Height - Height) / 2;
                break;
            case IndicatorPosition.Behind:
                x = isRtl ? caret.Right + gap : caret.Left - Width - gap;
                y = caret.Top + (caret.Height - Height) / 2;
                break;
            case IndicatorPosition.Below:
                x = caret.Left - Width / 2;
                y = caret.Bottom + gap;
                break;
            default:
                x = caret.Left - Width / 2;
                y = caret.Top - Height - gap;
                break;
        }

        Rectangle area = Screen.FromRectangle(caret).WorkingArea;

        if (settings.Position == IndicatorPosition.Above && y < area.Top)
        {
            y = caret.Bottom + gap;
        }
        else if (settings.Position == IndicatorPosition.Below && y + Height > area.Bottom)
        {
            y = caret.Top - Height - gap;
        }

        if (settings.Position is IndicatorPosition.Ahead or IndicatorPosition.Behind &&
            (x < area.Left || x + Width > area.Right))
        {
            x = x < caret.Left ? caret.Right + gap : caret.Left - Width - gap;
        }

        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - Width));
        y = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - Height));

        Point nextPosition = new(x, y);
        if (Location != nextPosition)
        {
            Location = nextPosition;
        }

        if (!Visible)
        {
            Show();
        }

        if (mustRender)
        {
            Render();
        }

        // A context menu/dropdown can be created *after* our topmost popup.
        // Reasserting the topmost Z-order brings the badge above regular menus
        // without activating it or stealing any input.
        NativeMethods.SetWindowPos(
            Handle, NativeMethods.HwndTopmost, 0, 0, 0, 0,
            NativeMethods.SwpNoSize | NativeMethods.SwpNoMove |
            NativeMethods.SwpNoActivate | NativeMethods.SwpNoOwnerZOrder);
    }

    private void Render()
    {
        using var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            using var backgroundPath = RoundedRectangle(new RectangleF(
                0.6f, 0.6f, Width - 1.2f, Height - 1.2f), 8f);

            Color color = _isRtl ? Color.FromArgb(12, 115, 102) : Color.FromArgb(30, 91, 164);
            using var backgroundBrush = new SolidBrush(Color.FromArgb(
                _backgroundOpacity * 255 / 100, color));
            g.FillPath(backgroundBrush, backgroundPath);

            using var pen = new Pen(Color.FromArgb(65, Color.White), 0.8f);
            g.DrawPath(pen, backgroundPath);

            using var font = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(Color.White);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = _isRtl ? StringFormatFlags.DirectionRightToLeft : 0
            };
            g.DrawString(_badge, font, textBrush, new RectangleF(0, 0, Width, Height), format);
        }

        IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
        {
            return;
        }

        IntPtr memoryDc = IntPtr.Zero;
        IntPtr bitmapHandle = IntPtr.Zero;
        IntPtr oldHandle = IntPtr.Zero;

        try
        {
            memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (memoryDc == IntPtr.Zero)
            {
                return;
            }

            bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
            oldHandle = NativeMethods.SelectObject(memoryDc, bitmapHandle);

            var destination = new NativeMethods.NativePoint { X = Left, Y = Top };
            var source = new NativeMethods.NativePoint();
            var size = new NativeMethods.NativeSize { Width = Width, Height = Height };
            var blend = new NativeMethods.BlendFunction
            {
                BlendOp = NativeMethods.AcSrcOver,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AcSrcAlpha
            };

            NativeMethods.UpdateLayeredWindow(
                Handle, screenDc, ref destination, ref size,
                memoryDc, ref source, 0, ref blend, NativeMethods.UlwAlpha);
        }
        finally
        {
            if (oldHandle != IntPtr.Zero && memoryDc != IntPtr.Zero)
            {
                NativeMethods.SelectObject(memoryDc, oldHandle);
            }
            if (bitmapHandle != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(bitmapHandle);
            }
            if (memoryDc != IntPtr.Zero)
            {
                NativeMethods.DeleteDC(memoryDc);
            }
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
    {
        float diameter = radius * 2f;
        var path = new GraphicsPath();

        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    public void HideDisplay()
    {
        if (Visible)
        {
            Hide();
        }
    }
}
