using System.Drawing;

namespace LangCue;

/// <summary>
/// A caret is not usable merely because an API returned coordinates.
/// Coordinates outside the visible app client area must never be clamped to
/// a screen edge and presented as though the caret were still visible.
/// </summary>
internal static class CaretVisibility
{
    public static bool IsWithinViewport(IntPtr foreground, Rectangle caret)
    {
        if (foreground == IntPtr.Zero ||
            !NativeMethods.IsWindowVisible(foreground) ||
            NativeMethods.IsIconic(foreground) ||
            caret.IsEmpty || caret.Height > 150 || caret.Width > 80)
        {
            return false;
        }

        var middle = new Point(caret.Left, caret.Top + caret.Height / 2);

        // Screen.FromPoint returns the nearest screen even for off-screen
        // coordinates; always check actual containment.
        if (!Screen.FromPoint(middle).Bounds.Contains(middle))
        {
            return false;
        }

        if (!TryClientBounds(foreground, out Rectangle clientArea))
        {
            return false;
        }

        // A small inset also rejects caret coordinates pinned to the viewport
        // boundary when a focused field is scrolled out of view.
        const int margin = 2;
        return middle.X >= clientArea.Left + margin &&
               middle.X < clientArea.Right - margin &&
               middle.Y >= clientArea.Top + margin &&
               middle.Y < clientArea.Bottom - margin;
    }

    private static bool TryClientBounds(IntPtr window, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;

        if (!NativeMethods.GetClientRect(window, out var rect) ||
            rect.Right <= rect.Left || rect.Bottom <= rect.Top)
        {
            return false;
        }

        var origin = new NativeMethods.NativePoint { X = rect.Left, Y = rect.Top };
        if (!NativeMethods.ClientToScreen(window, ref origin))
        {
            return false;
        }

        bounds = new Rectangle(origin.X, origin.Y,
            rect.Right - rect.Left, rect.Bottom - rect.Top);
        return true;
    }

    public static bool IsWithinElementClip(Rectangle caret, Rectangle visibleArea)
    {
        if (visibleArea.IsEmpty || caret.IsEmpty)
        {
            return false;
        }

        int midY = caret.Top + caret.Height / 2;
        // Reject edge-pinned carets. This is intentionally conservative:
        // hiding is preferable to showing a badge over unrelated content.
        const int inset = 2;
        return caret.Left >= visibleArea.Left + inset &&
               caret.Left < visibleArea.Right - inset &&
               midY > visibleArea.Top + inset &&
               midY < visibleArea.Bottom - inset;
    }
}
