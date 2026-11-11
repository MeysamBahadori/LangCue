using System.Threading;

namespace LangCue;

/// <summary>
/// Runs accessibility queries on a background MTA worker. Observations are
/// bound to the foreground window and the current focus revision. A slow
/// UIA provider must never resurrect an indicator after focus has changed.
/// </summary>
internal sealed class CaretWatcher : IDisposable
{
    private sealed record Observation(
        CaretSnapshot? Snapshot, IntPtr Foreground, long Revision, long Timestamp);

    private readonly Thread _worker;
    private volatile bool _running = true;
    private long _revision;
    private Observation? _latest;

    public CaretWatcher()
    {
        _worker = new Thread(Run)
        {
            IsBackground = true,
            Name = "LangCue caret monitor"
        };
        _worker.SetApartmentState(ApartmentState.MTA);
        _worker.Start();
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _revision);
        Volatile.Write(ref _latest, null);
    }

    public CaretSnapshot? GetLatest()
    {
        Observation? observation = Volatile.Read(ref _latest);
        if (observation is null ||
            observation.Revision != Volatile.Read(ref _revision) ||
            Environment.TickCount64 - observation.Timestamp > 350 ||
            NativeMethods.GetForegroundWindow() != observation.Foreground)
        {
            return null;
        }

        return observation.Snapshot;
    }

    private void Run()
    {
        var automation = new UiAutomationCaretLocator();

        while (_running)
        {
            long revision = Volatile.Read(ref _revision);
            IntPtr foreground = IntPtr.Zero;
            CaretSnapshot? snapshot = null;

            try
            {
                if (CaretMonitor.TryReadForeground(
                    out foreground, out IntPtr focusWindow,
                    out KeyboardLanguage language, out var systemCaret))
                {
                    System.Drawing.Rectangle candidate = System.Drawing.Rectangle.Empty;

                    if (!systemCaret.IsEmpty)
                    {
                        candidate = systemCaret;
                    }
                    else if (MsaaCaretLocator.TryLocate(foreground, focusWindow, out var msaaCaret))
                    {
                        candidate = msaaCaret;
                    }

                    if (!candidate.IsEmpty &&
                        CaretVisibility.IsWithinViewport(foreground, candidate) &&
                        automation.IsFocusedCaretVisible(foreground, candidate))
                    {
                        snapshot = new CaretSnapshot(candidate, language);
                    }
                    else if (automation.TryLocate(foreground, language, out var uiaCaret) &&
                             CaretVisibility.IsWithinViewport(foreground, uiaCaret))
                    {
                        snapshot = new CaretSnapshot(uiaCaret, language);
                    }
                }
            }
            catch (Exception exception)
            {
                // Broken providers must not terminate the tray application.
                System.Diagnostics.Debug.WriteLine(exception);
            }

            // Drop results of a slow query if a newer focus event occurred.
            if (revision == Volatile.Read(ref _revision) &&
                foreground != IntPtr.Zero &&
                foreground == NativeMethods.GetForegroundWindow())
            {
                Volatile.Write(ref _latest, new Observation(
                    snapshot, foreground, revision, Environment.TickCount64));
            }

            Thread.Sleep(150);
        }
    }

    public void Dispose()
    {
        _running = false;
        _worker.Join(millisecondsTimeout: 250);
    }
}
