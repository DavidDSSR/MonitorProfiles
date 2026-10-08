using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MonitorProfiles.App.ViewModels;
using MonitorProfiles.App.Views;
using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Services;
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
        if (e.PropertyName is nameof(MainViewModel.IsReady) or nameof(MainViewModel.ErrorMessage) or nameof(MainViewModel.IsCorruptStore) or nameof(MainViewModel.IsBusy))
        {
            UpdatePanels();
        }
    }

    private void UpdatePanels()
    {
        IsEnabled = !_viewModel.IsBusy;
        SetupPanel.Visibility = !_viewModel.IsReady && !_viewModel.IsCorruptStore ? Visibility.Visible : Visibility.Collapsed;
        ProfilesPanel.Visibility = _viewModel.IsReady ? Visibility.Visible : Visibility.Collapsed;
        ErrorPanel.Visibility = string.IsNullOrWhiteSpace(_viewModel.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
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
                $"{row.FriendlyName} está apagada en Windows. Asígnala por su nombre o actívala temporalmente para identificarla.",
                "Pantalla apagada",
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
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 13, 17, 23)),
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
                        Text = row.Alias ?? "Pantalla",
                        FontSize = 22,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = row.FriendlyName,
                        FontSize = 13,
                        Foreground = System.Windows.Media.Brushes.LightGray,
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
            $"¿Eliminar el perfil {row.Name}?",
            "Eliminar perfil",
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
                System.Windows.MessageBox.Show(this, exception.Message, "No se pudo eliminar el perfil", MessageBoxButton.OK, MessageBoxImage.Error);
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
            System.Windows.MessageBox.Show(this, exception.Message, "Revisa el perfil", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            System.Windows.MessageBox.Show(this, exception.Message, "No se pudo iniciar la prueba", MessageBoxButton.OK, MessageBoxImage.Error);
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
                System.Windows.MessageBox.Show(this, exception.Message, "No se pudo guardar el perfil", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        try
        {
            await _displayService.RestoreConfigurationAsync(snapshot);
            await _viewModel.RefreshDisplayStateAsync();
            _viewModel.StatusMessage = "Se restauró la configuración anterior.";
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(this,
                $"No se pudo restaurar la configuración anterior: {exception.Message}",
                "Error al revertir",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
