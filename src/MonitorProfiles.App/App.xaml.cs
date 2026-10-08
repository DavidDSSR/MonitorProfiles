using System.Windows;
using MonitorProfiles.App.Localization;
using MonitorProfiles.App.Services;
using MonitorProfiles.App.ViewModels;
using MonitorProfiles.Storage;
using MonitorProfiles.Windows;

namespace MonitorProfiles.App;

public partial class App : System.Windows.Application
{
    private TrayIconService? _trayIconService;
    private ThemeService? _themeService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var displayService = new WindowsDisplayService();
        var repository = new ProfileRepository(LocalProfilePathProvider.GetPath());
        var preferencesRepository = new ApplicationPreferencesRepository(LocalProfilePathProvider.GetPreferencesPath());
        var preferencesResult = await preferencesRepository.LoadAsync();
        var localization = LocalizationService.Instance;
        localization.SetLanguage(preferencesResult.Preferences.EffectiveLanguageCode);
        var startupRegistrationService = new StartupRegistrationService();
        _themeService = new ThemeService(this);
        _themeService.Apply(preferencesResult.Preferences.EffectiveTheme);
        var viewModel = new MainViewModel(
            displayService,
            repository,
            preferencesRepository,
            preferencesResult,
            _themeService,
            localization,
            startupRegistrationService);
        var mainWindow = new MainWindow(viewModel, displayService);
        MainWindow = mainWindow;

        await viewModel.InitializeAsync();
        if (!StartupRegistrationService.IsBackgroundStartup(e.Args))
        {
            mainWindow.Show();
        }
        _trayIconService = new TrayIconService(mainWindow, viewModel);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        _themeService?.Dispose();
        base.OnExit(e);
    }
}
