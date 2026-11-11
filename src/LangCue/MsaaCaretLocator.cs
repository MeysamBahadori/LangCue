using Accessibility;
using System.Drawing;
using System.Runtime.InteropServices;

namespace LangCue;

/// <summary>
/// Asks Active Accessibility for the current caret location. Useful for Chromium
/// and custom text controls that do not expose a classic Win32 caret.
/// </summary>
internal static class MsaaCaretLocator
{
    private static readonly Guid AccessibleId = new("618736E0-3C3D-11CF-810C-00AA00389B71");

    public static bool TryLocate(IntPtr foreground, IntPtr focusWindow, out Rectangle caret)
    {
        caret = Rectangle.Empty;

        // Prefer the actual focused child window, then the top-level window.
        if (focusWindow != IntPtr.Zero && TryLocateWindow(focusWindow, out caret))
        {
            return NativeMethods.GetForegroundWindow() == foreground;
        }

        if (TryLocateWindow(foreground, out caret))
        {
            return NativeMethods.GetForegroundWindow() == foreground;
        }

        return false;
    }

    private static bool TryLocateWindow(IntPtr window, out Rectangle caret)
    {
        caret = Rectangle.Empty;
        IAccessible? accessible = null;

        try
        {
            Guid iid = AccessibleId;
            int hr = NativeMethods.AccessibleObjectFromWindow(
                window, NativeMethods.ObjIdCaret, ref iid, out IAccessible result);

            if (hr < 0 || result is null)
            {
                return false;
            }

            accessible = result;
            accessible.accLocation(out int x, out int y, out int width, out int height, 0);

            // Reject absent or plainly invalid caret coordinates.
            if (height < 2 || height > 150 ||
                width < 0 || width > 50 ||
                Math.Abs((long)x) > 100000 || Math.Abs((long)y) > 100000)
            {
                return false;
            }

            caret = new Rectangle(x, y, Math.Max(2, width), height);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        finally
        {
            if (accessible is not null && Marshal.IsComObject(accessible))
            {
                Marshal.ReleaseComObject(accessible);
            }
        }
    }
}
