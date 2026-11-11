using System.Threading;

namespace LangCue;

/// <summary>
/// Accessibility calls run on a background MTA worker, never on the UI thread.
/// Snapshots expire quickly so the badge does not linger over other windows.
/// </summary>
internal sealed class CaretWatcher : IDisposable
{
    private sealed record Observation(CaretSnapshot? Snapshot, long Timestamp);

    private readonly Thread _worker;
    private volatile bool _running = true;
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

    public CaretSnapshot? GetLatest()
    {
        Observation? observation = Volatile.Read(ref _latest);
        if (observation is null ||
            Environment.TickCount64 - observation.Timestamp > 650)
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
            CaretSnapshot? snapshot = null;

            try
            {
                if (CaretMonitor.TryReadForeground(
                    out IntPtr foreground,
                    out IntPtr focusWindow,
                    out KeyboardLanguage language,
                    out var systemCaret))
                {
                    if (!systemCaret.IsEmpty)
                    {
                        snapshot = new CaretSnapshot(systemCaret, language);
                    }
                    else if (MsaaCaretLocator.TryLocate(foreground, focusWindow, out var msaaCaret))
                    {
                        snapshot = new CaretSnapshot(msaaCaret, language);
                    }
                    else if (automation.TryLocate(foreground, language, out var uiaCaret))
                    {
                        snapshot = new CaretSnapshot(uiaCaret, language);
                    }
                }
            }
            catch (Exception exception)
            {
                // Broken accessibility providers must not terminate the tray utility.
                System.Diagnostics.Debug.WriteLine(exception);
            }

            Volatile.Write(ref _latest, new Observation(snapshot, Environment.TickCount64));
            Thread.Sleep(150);
        }
    }

    public void Dispose()
    {
        _running = false;
        _worker.Join(millisecondsTimeout: 250);
    }
}
