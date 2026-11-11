using System.Threading;

namespace LangCue;

/// <summary>
/// Runs UIA on a dedicated MTA thread so slow accessibility providers never freeze
/// the tray menu or Settings window. UI reads only immutable, timestamped snapshots.
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
            Environment.TickCount64 - observation.Timestamp > 900)
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
                        out IntPtr foreground, out string language, out var systemCaret))
                {
                    if (systemCaret != System.Drawing.Rectangle.Empty)
                    {
                        snapshot = new CaretSnapshot(systemCaret, language);
                    }
                    else if (automation.TryLocate(foreground, language, out var customCaret))
                    {
                        snapshot = new CaretSnapshot(customCaret, language);
                    }
                }
            }
            catch (Exception exception)
            {
                // A faulty accessibility provider must not terminate the tray app.
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
