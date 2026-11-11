using System.Drawing;
using System.Runtime.InteropServices;

namespace LangCue;

internal sealed record CaretSnapshot(Rectangle Bounds, KeyboardLanguage Language);

internal static class CaretMonitor
{
    // No keystroke hooks and no text access. Only foreground input state and geometry.
    public static bool TryReadForeground(
        out IntPtr foreground,
        out IntPtr focusWindow,
        out KeyboardLanguage language,
        out Rectangle systemCaret)
    {
        foreground = NativeMethods.GetForegroundWindow();
        focusWindow = IntPtr.Zero;
        language = new KeyboardLanguage("?", false);
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
            focusWindow = info.FocusWindow;
            uint focusThread = NativeMethods.GetWindowThreadProcessId(focusWindow, out _);
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

        language = KeyboardLanguage.FromLayout(layout);

        if (!hasThreadInfo ||
            info.CaretWindow == IntPtr.Zero ||
            (info.Flags & NativeMethods.GuiCaretBlinking) == 0 ||
            info.Caret.Bottom <= info.Caret.Top)
        {
            return true; // MSAA / UIA may still expose a custom-drawn caret.
        }

        var point = new NativeMethods.NativePoint
        {
            X = info.Caret.Left,
            Y = info.Caret.Top
        };

        if (NativeMethods.ClientToScreen(info.CaretWindow, ref point))
        {
            systemCaret = new Rectangle(
                point.X, point.Y,
                Math.Max(2, info.Caret.Right - info.Caret.Left),
                info.Caret.Bottom - info.Caret.Top);
        }

        return true;
    }
}
