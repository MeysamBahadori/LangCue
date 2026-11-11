namespace LangCue;

/// <summary>
/// Clears previous caret observations when focus changes. Hooks are out-of-
/// process event notifications only; they do not intercept keyboard input.
/// The UI thread owns both hooks and its message loop.
/// </summary>
internal sealed class FocusChangeWatcher : IDisposable
{
    private readonly NativeMethods.WinEventProc _callback;
    private readonly Action _invalidate;
    private readonly IntPtr _foregroundHook;
    private readonly IntPtr _focusHook;
    private bool _disposed;

    public FocusChangeWatcher(Action invalidate)
    {
        _invalidate = invalidate;
        _callback = HandleEvent; // Keep the managed callback rooted.

        uint flags = NativeMethods.WinEventOutOfContext |
                     NativeMethods.WinEventSkipOwnProcess;

        _foregroundHook = NativeMethods.SetWinEventHook(
            NativeMethods.EventSystemForeground,
            NativeMethods.EventSystemForeground,
            IntPtr.Zero, _callback, 0, 0, flags);

        _focusHook = NativeMethods.SetWinEventHook(
            NativeMethods.EventObjectFocus,
            NativeMethods.EventObjectFocus,
            IntPtr.Zero, _callback, 0, 0, flags);
    }

    private void HandleEvent(
        IntPtr hook, uint eventType, IntPtr window,
        int objectId, int childId, uint eventThread, uint eventTime)
    {
        if (_disposed)
        {
            return;
        }

        IntPtr foreground = NativeMethods.GetForegroundWindow();
        if (eventType == NativeMethods.EventSystemForeground ||
            window == foreground ||
            (window != IntPtr.Zero &&
             NativeMethods.GetAncestor(window, NativeMethods.GaRoot) == foreground))
        {
            _invalidate();
        }
    }

    public void Dispose()
    {
        _disposed = true;

        if (_foregroundHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_foregroundHook);
        }

        if (_focusHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_focusHook);
        }

        GC.KeepAlive(_callback);
    }
}
