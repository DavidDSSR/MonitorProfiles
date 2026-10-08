using System.Windows;
using MonitorProfiles.App.Services;
using MonitorProfiles.App.ViewModels;
using MonitorProfiles.Storage;
using MonitorProfiles.Windows;

namespace MonitorProfiles.App;

public partial class App : System.Windows.Application
{
    private TrayIconService? _trayIconService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var displayService = new WindowsDisplayService();
        var repository = new ProfileRepository(LocalProfilePathProvider.GetPath());
        var viewModel = new MainViewModel(displayService, repository);
        var mainWindow = new MainWindow(viewModel, displayService);
        MainWindow = mainWindow;

        await viewModel.InitializeAsync();
        mainWindow.Show();
        _trayIconService = new TrayIconService(mainWindow, viewModel);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        base.OnExit(e);
    }
}
