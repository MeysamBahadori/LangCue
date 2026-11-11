using System.Runtime.InteropServices;
using Accessibility;

namespace LangCue;

internal static class NativeMethods
{
    internal const uint GuiCaretBlinking = 0x00000001;
    internal const uint ObjIdCaret = 0xFFFFFFF8;

    internal static readonly IntPtr HwndTopmost = new(-1);
    internal const uint SwpNoSize = 0x0001;
    internal const uint SwpNoMove = 0x0002;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpNoOwnerZOrder = 0x0200;

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetKeyboardLayout(uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(
        IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("oleacc.dll", ExactSpelling = true)]
    internal static extern int AccessibleObjectFromWindow(
        IntPtr hwnd, uint objectId, ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out IAccessible accessible);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    internal static extern int ReleaseDC(IntPtr window, IntPtr dc);

    [DllImport("gdi32.dll", SetLastError = true)]
    internal static extern IntPtr CreateCompatibleDC(IntPtr dc);

    [DllImport("gdi32.dll", SetLastError = true)]
    internal static extern bool DeleteDC(IntPtr dc);

    [DllImport("gdi32.dll", SetLastError = true)]
    internal static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

    [DllImport("gdi32.dll", SetLastError = true)]
    internal static extern bool DeleteObject(IntPtr obj);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UpdateLayeredWindow(
        IntPtr window, IntPtr destinationDc, ref NativePoint destination, ref NativeSize size,
        IntPtr sourceDc, ref NativePoint source, uint colorKey, ref BlendFunction blend,
        uint flags);

    internal const uint UlwAlpha = 0x00000002;
    internal const byte AcSrcOver = 0;
    internal const byte AcSrcAlpha = 1;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeSize
    {
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GuiThreadInfo
    {
        public uint Size;
        public uint Flags;
        public IntPtr ActiveWindow;
        public IntPtr FocusWindow;
        public IntPtr CaptureWindow;
        public IntPtr MenuOwnerWindow;
        public IntPtr MoveSizeWindow;
        public IntPtr CaretWindow;
        public NativeRect Caret;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        public int X;
        public int Y;
    }
}
