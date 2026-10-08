using System.Windows;
using System.Windows.Threading;
using MonitorProfiles.App.Localization;

namespace MonitorProfiles.App.Views;

public partial class PreviewConfirmationWindow : Window
{
    private readonly DispatcherTimer _timer;
    private int _secondsRemaining;

    public PreviewConfirmationWindow(int seconds = 15)
    {
        InitializeComponent();
        _secondsRemaining = seconds;
        UpdateCountdown();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        LocalizationService.Instance.LanguageChanged += Localization_LanguageChanged;
        Closed += (_, _) =>
        {
            _timer.Stop();
            LocalizationService.Instance.LanguageChanged -= Localization_LanguageChanged;
        };
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _secondsRemaining--;
        if (_secondsRemaining <= 0)
        {
            DialogResult = false;
            return;
        }

        UpdateCountdown();
    }

    private void Keep_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Revert_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Localization_LanguageChanged(object? sender, EventArgs e) => UpdateCountdown();

    public void RequestRevert()
    {
        if (Dispatcher.CheckAccess())
        {
            DialogResult = false;
        }
        else
        {
            Dispatcher.BeginInvoke(() => DialogResult = false);
        }
    }

    private void UpdateCountdown() => CountdownText.Text =
        LocalizationService.Instance.Get("Preview.Countdown", _secondsRemaining);
}
