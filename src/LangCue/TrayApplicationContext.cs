using System.Drawing;

namespace LangCue;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _trayIcon;
    private readonly IndicatorForm _indicator = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 150 };

    public TrayApplicationContext()
    {
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Exit LangCue", null, (sender, args) => ExitThread());

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "LangCue",
            ContextMenuStrip = _menu,
            Visible = true
        };

        _timer.Tick += (sender, args) => UpdateIndicator();
        _timer.Start();
    }

    private void UpdateIndicator()
    {
        if (CaretMonitor.TryRead(out CaretSnapshot snapshot))
        {
            _indicator.UpdateDisplay(snapshot);
        }
        else
        {
            _indicator.HideDisplay();
        }
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _timer.Dispose();

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _menu.Dispose();

        _indicator.Close();
        _indicator.Dispose();

        base.ExitThreadCore();
    }
}
