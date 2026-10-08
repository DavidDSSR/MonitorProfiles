using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MonitorProfiles.App.Localization;
using MonitorProfiles.App.ViewModels;
using MonitorProfiles.App.Views;
using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Services;
using MonitorProfiles.Storage;
using Forms = System.Windows.Forms;

namespace MonitorProfiles.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IDisplayService _displayService;
    private bool _allowClose;
    private PreviewConfirmationWindow? _activePreview;

    public MainWindow(MainViewModel viewModel, IDisplayService displayService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _displayService = displayService;
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdatePanels();
    }

    public MainViewModel ViewModel => _viewModel;
    public bool HasActivePreview => _activePreview is not null;

    public void RevertActivePreview()
    {
        if (Dispatcher.CheckAccess())
        {
            _activePreview?.RequestRevert();
        }
        else
        {
            Dispatcher.BeginInvoke(() => _activePreview?.RequestRevert());
        }
    }

    public void ExitApplication()
    {
        _allowClose = true;
        Close();
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsReady) or nameof(MainViewModel.ErrorMessage) or nameof(MainViewModel.IsCorruptStore) or nameof(MainViewModel.IsBusy) or nameof(MainViewModel.ShowStartupReminder))
        {
            UpdatePanels();
        }
    }

    private async void LanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedValue: string languageCode } &&
            !string.Equals(languageCode, _viewModel.SelectedLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            await _viewModel.SetLanguageAsync(languageCode);
        }
    }

    private async void ThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedValue: ThemePreference theme } && theme != _viewModel.SelectedTheme)
        {
            await _viewModel.SetThemeAsync(theme);
        }
    }

    private async void EnableStartup_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SetLaunchAtStartupAsync(true);
        UpdatePanels();
    }

    private async void DismissStartupReminder_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.DismissStartupReminderAsync();
        UpdatePanels();
    }

    private void UpdatePanels()
    {
        IsEnabled = !_viewModel.IsBusy;
        SetupPanel.Visibility = !_viewModel.IsReady && !_viewModel.IsCorruptStore ? Visibility.Visible : Visibility.Collapsed;
        ProfilesPanel.Visibility = _viewModel.IsReady ? Visibility.Visible : Visibility.Collapsed;
        ErrorPanel.Visibility = string.IsNullOrWhiteSpace(_viewModel.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
        StartupReminderPanel.Visibility = _viewModel.ShowStartupReminder ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void SaveSetup_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveInitialMappingsAsync();
        UpdatePanels();
    }

    private void IdentifyDisplay_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DisplayRowViewModel row })
        {
            return;
        }

        var deviceName = row.Descriptor.GdiDeviceName;
        var screen = string.IsNullOrWhiteSpace(deviceName)
            ? null
            : Forms.Screen.AllScreens.FirstOrDefault(candidate =>
                string.Equals(candidate.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        if (screen is null)
        {
            System.Windows.MessageBox.Show(this,
                LocalizationService.Instance.Get("Identify.OffDetail", row.FriendlyName),
                LocalizationService.Instance.Get("Identify.OffTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var marker = new Window
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Topmost = true,
            Background = (System.Windows.Media.Brush)Application.Current.Resources["RaisedSurfaceBrush"],
            Width = 330,
            Height = 190,
            Left = screen.Bounds.Left + (screen.Bounds.Width - 330) / 2,
            Top = screen.Bounds.Top + (screen.Bounds.Height - 190) / 2,
            Content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = row.Alias ?? LocalizationService.Instance.Get("Display.Unassigned"),
                        FontSize = 22,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextPrimaryBrush"],
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = row.FriendlyName,
                        FontSize = 13,
                        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                        Margin = new Thickness(0, 8, 0, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    }
                }
            }
        };

        marker.Show();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            marker.Close();
        };
        timer.Start();
    }

    private async void ApplyProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProfileRowViewModel row })
        {
            await _viewModel.ApplyProfileAsync(row.Profile);
        }
    }

    private async void NewProfile_Click(object sender, RoutedEventArgs e) => await EditProfileAsync(null);

    private async void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProfileRowViewModel row })
        {
            await EditProfileAsync(row.Profile);
        }
    }

    private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProfileRowViewModel row })
        {
            return;
        }

        var answer = System.Windows.MessageBox.Show(this,
            LocalizationService.Instance.Get("Dialog.DeleteConfirm", row.Name),
            LocalizationService.Instance.Get("Dialog.DeleteTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes)
        {
            try
            {
                await _viewModel.DeleteProfileAsync(row.Profile);
            }
            catch (Exception exception)
            {
                ShowError("Error.InvalidProfile", exception.Message);
            }
        }
    }

    private async Task EditProfileAsync(DisplayProfile? profile)
    {
        var editor = new ProfileEditorWindow(_viewModel.Displays, profile) { Owner = this };
        if (editor.ShowDialog() != true || editor.ResultProfile is null)
        {
            return;
        }

        if (editor.WantsPreview)
        {
            await PreviewProfileAsync(editor.ResultProfile);
            return;
        }

        try
        {
            await _viewModel.SaveProfileAsync(editor.ResultProfile);
        }
        catch (Exception exception)
        {
            var validationMessage = ProfileValidationMessageResolver.ResolveMessage(
                exception.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
            ShowError(
                validationMessage.ResourceKey,
                validationMessage.ResourceKey == "Error.InvalidProfile" ? exception.Message : null,
                MessageBoxImage.Warning,
                validationMessage.Arguments.ToArray());
        }
    }

    private async Task PreviewProfileAsync(DisplayProfile profile)
    {
        IDisplayConfigurationSnapshot snapshot;
        try
        {
            snapshot = await _displayService.CaptureConfigurationAsync();
        }
        catch (Exception exception)
        {
            ShowError("Error.DisplayDetection", exception.Message);
            return;
        }

        var result = await _viewModel.ApplyProfileAsync(profile);
        if (!result.Succeeded)
        {
            return;
        }

        var confirmation = new PreviewConfirmationWindow { Owner = this };
        _activePreview = confirmation;
        bool keepChanges;
        try
        {
            keepChanges = confirmation.ShowDialog() == true;
        }
        finally
        {
            _activePreview = null;
        }

        if (keepChanges)
        {
            try
            {
                await _viewModel.SaveProfileAsync(profile);
                return;
            }
            catch (Exception exception)
            {
                ShowError("Error.ProfileSave", exception.Message);
            }
        }

        try
        {
            await _displayService.RestoreConfigurationAsync(snapshot);
            await _viewModel.RefreshDisplayStateAsync();
            _viewModel.SetStatusMessage("Status.Restored");
        }
        catch (Exception exception)
        {
            ShowError("Dialog.RevertTitle", exception.Message);
        }
    }

    private void ShowError(
        string messageKey,
        string? detail,
        MessageBoxImage image = MessageBoxImage.Error,
        params object?[] arguments)
    {
        var message = LocalizationService.Instance.Get(messageKey, arguments);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            message = $"{message}{Environment.NewLine}{LocalizationService.Instance.Get("Error.TechnicalDetails", detail)}";
        }

        System.Windows.MessageBox.Show(
            this,
            message,
            LocalizationService.Instance.Get("Dialog.ErrorTitle"),
            MessageBoxButton.OK,
            image);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
