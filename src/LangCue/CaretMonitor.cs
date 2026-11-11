using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LangCue;

internal sealed record CaretSnapshot(Rectangle Bounds, string Language);

internal static class CaretMonitor
{
    // Only inspect foreground focus, keyboard layout, and geometry. Never inspect typed text.
    public static bool TryReadForeground(
        out IntPtr foreground,
        out string language,
        out Rectangle systemCaret)
    {
        foreground = NativeMethods.GetForegroundWindow();
        language = string.Empty;
        systemCaret = Rectangle.Empty;

        if (foreground == IntPtr.Zero)
        {
            return false;
        }

        uint threadId = NativeMethods.GetWindowThreadProcessId(foreground, out uint processId);
        if (threadId == 0 || processId == (uint)Environment.ProcessId)
        {
            return false;
        }

        var info = new NativeMethods.GuiThreadInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>()
        };

        bool hasThreadInfo = NativeMethods.GetGUIThreadInfo(threadId, ref info);

        uint layoutThread = threadId;
        if (hasThreadInfo && info.FocusWindow != IntPtr.Zero)
        {
            uint focusThread = NativeMethods.GetWindowThreadProcessId(info.FocusWindow, out _);
            if (focusThread != 0)
            {
                layoutThread = focusThread;
            }
        }

        IntPtr layout = NativeMethods.GetKeyboardLayout(layoutThread);
        if (layout == IntPtr.Zero)
        {
            return false;
        }

        ushort languageId = (ushort)(layout.ToInt64() & 0xFFFF);
        try
        {
            string code = CultureInfo.GetCultureInfo(languageId).TwoLetterISOLanguageName;
            language = code == "fa" ? "فا" : code.ToUpperInvariant();
        }
        catch (CultureNotFoundException)
        {
            return false;
        }

        if (!hasThreadInfo ||
            info.CaretWindow == IntPtr.Zero ||
            (info.Flags & NativeMethods.GuiCaretBlinking) == 0 ||
            info.Caret.Bottom <= info.Caret.Top)
        {
            return true; // The modern UI Automation path may still find the caret.
        }

        var point = new NativeMethods.NativePoint
        {
            X = info.Caret.Left,
            Y = info.Caret.Top
        };

        if (NativeMethods.ClientToScreen(info.CaretWindow, ref point))
        {
            int width = Math.Max(2, info.Caret.Right - info.Caret.Left);
            int height = info.Caret.Bottom - info.Caret.Top;
            systemCaret = new Rectangle(point.X, point.Y, width, height);
        }

        return true;
    }
}
