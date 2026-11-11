using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LangCue;

internal readonly record struct CaretSnapshot(Point Position, string Language);

internal static class CaretMonitor
{
    public static bool TryRead(out CaretSnapshot snapshot)
    {
        snapshot = default;

        IntPtr foreground = NativeMethods.GetForegroundWindow();
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

        if (!NativeMethods.GetGUIThreadInfo(threadId, ref info) ||
            info.FocusWindow == IntPtr.Zero ||
            info.CaretWindow == IntPtr.Zero ||
            (info.Flags & NativeMethods.GuiCaretBlinking) == 0 ||
            info.Caret.Bottom <= info.Caret.Top)
        {
            return false;
        }

        // GetGUIThreadInfo reports caret coordinates relative to the caret window.
        var position = new NativeMethods.NativePoint
        {
            X = info.Caret.Right,
            Y = info.Caret.Top
        };

        if (!NativeMethods.ClientToScreen(info.CaretWindow, ref position))
        {
            return false;
        }

        uint focusThreadId = NativeMethods.GetWindowThreadProcessId(info.FocusWindow, out _);
        IntPtr layout = NativeMethods.GetKeyboardLayout(
            focusThreadId != 0 ? focusThreadId : threadId);

        if (layout == IntPtr.Zero)
        {
            return false;
        }

        ushort languageId = (ushort)(layout.ToInt64() & 0xFFFF);
        string language;

        try
        {
            string code = CultureInfo.GetCultureInfo(languageId).TwoLetterISOLanguageName;
            language = code == "fa" ? "فا" : code.ToUpperInvariant();
        }
        catch (CultureNotFoundException)
        {
            return false;
        }

        snapshot = new CaretSnapshot(new Point(position.X, position.Y), language);
        return true;
    }
}
