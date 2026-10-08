using MonitorProfiles.App.ViewModels;
using MonitorProfiles.App.Localization;
using System.Drawing;
using System.IO;
using Forms = System.Windows.Forms;

namespace MonitorProfiles.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly MainViewModel _viewModel;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _contextMenu;
    private readonly Icon _trayIcon;
    private readonly Stream _trayIconStream;

    public TrayIconService(MainWindow mainWindow, MainViewModel viewModel)
    {
        _mainWindow = mainWindow;
        _viewModel = viewModel;
        LocalizationService.Instance.LanguageChanged += Localization_LanguageChanged;
        var iconResource = System.Windows.Application.GetResourceStream(new Uri(
            "pack://application:,,,/MonitorProfiles.App;component/Resources/Brand/MonitorProfiles.ico",
            UriKind.Absolute)) ?? throw new InvalidOperationException("The Monitor Profiles tray icon resource is missing.");
        _trayIconStream = iconResource.Stream;
        _trayIcon = new Icon(_trayIconStream);
        _contextMenu = new Forms.ContextMenuStrip();
        _contextMenu.Opening += (_, _) => RebuildMenu();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Monitor Profiles",
            Icon = _trayIcon,
            ContextMenuStrip = _contextMenu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => _mainWindow.ShowFromTray();
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _contextMenu.Items.Clear();
        _contextMenu.Items.Add(LocalizationService.Instance.Get("Tray.Open"), null, (_, _) => _mainWindow.ShowFromTray());

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

        var languageMenu = new Forms.ToolStripMenuItem(LocalizationService.Instance.Get("Language.Menu"));
        foreach (var language in _viewModel.Languages)
        {
            var languageItem = new Forms.ToolStripMenuItem(language.DisplayName)
            {
                Checked = string.Equals(language.Code, _viewModel.SelectedLanguageCode, StringComparison.OrdinalIgnoreCase)
            };
            languageItem.Click += async (_, _) => await _viewModel.SetLanguageAsync(language.Code);
            languageMenu.DropDownItems.Add(languageItem);
        }
        _contextMenu.Items.Add(languageMenu);

        var themeMenu = new Forms.ToolStripMenuItem(LocalizationService.Instance.Get("Theme.Menu"));
        foreach (var (theme, key) in new[]
                 {
                     (MonitorProfiles.Storage.ThemePreference.System, "Theme.System"),
                     (MonitorProfiles.Storage.ThemePreference.Light, "Theme.Light"),
                     (MonitorProfiles.Storage.ThemePreference.Dark, "Theme.Dark")
                 })
        {
            var themeItem = new Forms.ToolStripMenuItem(LocalizationService.Instance.Get(key))
            {
                Checked = theme == _viewModel.SelectedTheme
            };
            themeItem.Click += async (_, _) => await _viewModel.SetThemeAsync(theme);
            themeMenu.DropDownItems.Add(themeItem);
        }
        _contextMenu.Items.Add(themeMenu);

        if (_mainWindow.HasActivePreview)
        {
            _contextMenu.Items.Add(new Forms.ToolStripSeparator());
            _contextMenu.Items.Add(LocalizationService.Instance.Get("Tray.Revert"), null, (_, _) => _mainWindow.RevertActivePreview());
        }

        _contextMenu.Items.Add(new Forms.ToolStripSeparator());
        _contextMenu.Items.Add(LocalizationService.Instance.Get("Tray.Exit"), null, (_, _) =>
        {
            _mainWindow.ExitApplication();
            System.Windows.Application.Current.Shutdown();
        });
    }

    private void Localization_LanguageChanged(object? sender, EventArgs e) => RebuildMenu();

    public void Dispose()
    {
        LocalizationService.Instance.LanguageChanged -= Localization_LanguageChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayIcon.Dispose();
        _trayIconStream.Dispose();
        _contextMenu.Dispose();
    }
}
