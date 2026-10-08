using MonitorProfiles.App.ViewModels;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace MonitorProfiles.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly MainViewModel _viewModel;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _contextMenu;

    public TrayIconService(MainWindow mainWindow, MainViewModel viewModel)
    {
        _mainWindow = mainWindow;
        _viewModel = viewModel;
        _contextMenu = new Forms.ContextMenuStrip();
        _contextMenu.Opening += (_, _) => RebuildMenu();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Monitor Profiles",
            Icon = SystemIcons.Application,
            ContextMenuStrip = _contextMenu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => _mainWindow.ShowFromTray();
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _contextMenu.Items.Clear();
        _contextMenu.Items.Add("Abrir Monitor Profiles", null, (_, _) => _mainWindow.ShowFromTray());

        if (_viewModel.Profiles.Count > 0)
        {
            _contextMenu.Items.Add(new Forms.ToolStripSeparator());
            foreach (var row in _viewModel.Profiles)
            {
                var item = new Forms.ToolStripMenuItem(row.Name);
                item.Click += async (_, _) =>
                {
                    if (!_viewModel.IsBusy)
                    {
                        await _viewModel.ApplyProfileAsync(row.Profile);
                    }
                };
                _contextMenu.Items.Add(item);
            }
        }

        if (_mainWindow.HasActivePreview)
        {
            _contextMenu.Items.Add(new Forms.ToolStripSeparator());
            _contextMenu.Items.Add("Revertir prueba", null, (_, _) => _mainWindow.RevertActivePreview());
        }

        _contextMenu.Items.Add(new Forms.ToolStripSeparator());
        _contextMenu.Items.Add("Salir", null, (_, _) =>
        {
            _mainWindow.ExitApplication();
            System.Windows.Application.Current.Shutdown();
        });
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
