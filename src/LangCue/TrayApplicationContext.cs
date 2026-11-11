using System.Drawing;

namespace LangCue;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ContextMenuStrip _menu;
    private readonly Icon _appIcon;
    private readonly NotifyIcon _trayIcon;
    private readonly IndicatorForm _indicator = new();
    private readonly CaretWatcher _watcher = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 80 };

    private IndicatorSettings _settings = SettingsStore.Load();

    public TrayApplicationContext()
    {
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Settings...", null, OpenSettings);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit LangCue", null, (sender, args) => ExitThread());

        // The custom multi-resolution icon is embedded in the EXE at build time.
        _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            ?? (Icon)SystemIcons.Application.Clone();

        _trayIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "LangCue 0.2.0-preview.1",
            ContextMenuStrip = _menu,
            Visible = true
        };

        _timer.Tick += (sender, args) => UpdateIndicator();
        _timer.Start();
    }

    private void OpenSettings(object? sender, EventArgs e)
    {
        _indicator.HideDisplay();

        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings = form.SelectedSettings;
        }
    }

    private void UpdateIndicator()
    {
        CaretSnapshot? snapshot = _watcher.GetLatest();
        if (snapshot is not null)
        {
            _indicator.UpdateDisplay(snapshot, _settings);
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
        _watcher.Dispose();

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _appIcon.Dispose();
        _menu.Dispose();

        _indicator.Close();
        _indicator.Dispose();

        base.ExitThreadCore();
    }
}
