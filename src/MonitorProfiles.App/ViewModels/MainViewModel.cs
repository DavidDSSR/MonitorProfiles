using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Profiles;
using MonitorProfiles.Core.Services;
using MonitorProfiles.Storage;

namespace MonitorProfiles.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IDisplayService _displayService;
    private readonly ProfileApplicationService _applicationService;
    private readonly ProfileRepository _repository;
    private readonly Dictionary<string, DisplayDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _displayMappings = new(StringComparer.OrdinalIgnoreCase);
    private string _statusMessage = "Detectando pantallas…";
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isReady;
    private bool _isCorruptStore;

    public MainViewModel(IDisplayService displayService, ProfileRepository repository)
    {
        _displayService = displayService;
        _applicationService = new ProfileApplicationService(displayService);
        _repository = repository;
    }

    public ObservableCollection<DisplayRowViewModel> Displays { get; } = [];
    public ObservableCollection<ProfileRowViewModel> Profiles { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanSaveSetup));
            }
        }
    }

    public bool IsReady
    {
        get => _isReady;
        private set => SetField(ref _isReady, value);
    }

    public bool IsCorruptStore
    {
        get => _isCorruptStore;
        private set => SetField(ref _isCorruptStore, value);
    }

    public bool CanSaveSetup => !IsBusy && Displays.Count == 4 &&
        Displays.All(row => !string.IsNullOrWhiteSpace(row.Alias)) &&
        Displays.Select(row => row.Alias).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 4;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var detected = await _displayService.GetDisplaysAsync();
            _descriptors.Clear();
            foreach (var display in detected.Where(display => display.IsConnected))
            {
                _descriptors[display.DisplayId] = display;
            }

            var document = await _repository.LoadAsync();
            Displays.Clear();
            foreach (var display in _descriptors.Values.OrderBy(display => display.FriendlyName, StringComparer.CurrentCultureIgnoreCase))
            {
                var alias = document?.DisplayMappings.FirstOrDefault(mapping =>
                    string.Equals(mapping.Value, display.DisplayId, StringComparison.OrdinalIgnoreCase)).Key;
                var row = new DisplayRowViewModel(display, alias);
                row.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(DisplayRowViewModel.Alias))
                    {
                        OnPropertyChanged(nameof(CanSaveSetup));
                    }
                };
                Displays.Add(row);
            }

            if (document is null)
            {
                IsReady = false;
                StatusMessage = "Identifica tus pantallas para preparar los perfiles.";
            }
            else
            {
                _displayMappings = new Dictionary<string, string>(document.DisplayMappings, StringComparer.OrdinalIgnoreCase);
                RefreshProfiles(document.Profiles);
                IsReady = Profiles.Count > 0;
                StatusMessage = $"{_descriptors.Count} pantallas detectadas";
            }
        }
        catch (InvalidDataException exception)
        {
            IsCorruptStore = true;
            ErrorMessage = exception.Message;
            StatusMessage = "No se pudo leer la configuración guardada.";
        }
        catch (NotSupportedException exception)
        {
            IsCorruptStore = true;
            ErrorMessage = exception.Message;
            StatusMessage = "La configuración fue creada por una versión más reciente.";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "No se pudieron detectar las pantallas.";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanSaveSetup));
        }
    }

    public async Task<bool> SaveInitialMappingsAsync()
    {
        if (!CanSaveSetup || IsCorruptStore)
        {
            return false;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            _displayMappings = Displays.ToDictionary(row => row.Alias!, row => row.DeviceId, StringComparer.OrdinalIgnoreCase);
            var profiles = DefaultProfileFactory.Create(_displayMappings);
            await _repository.SaveAsync(new ProfileStoreDocument(
                ProfileStoreDocument.CurrentSchemaVersion,
                profiles,
                _displayMappings));
            RefreshProfiles(profiles);
            IsReady = true;
            StatusMessage = "Perfiles listos para usar.";
            return true;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<ProfileApplicationResult> ApplyProfileAsync(DisplayProfile profile)
    {
        if (IsBusy)
        {
            return ProfileApplicationResult.Failure("Ya hay una operación de pantalla en curso.");
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Aplicando {profile.Name}…";
        try
        {
            var result = await _applicationService.ApplySavedAsync(profile);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error;
                StatusMessage = result.RestorationSucceeded == false
                    ? "Falló el perfil y no se pudo restaurar la configuración anterior."
                    : "No se pudo aplicar el perfil.";
                return result;
            }

            StatusMessage = $"Perfil {profile.Name} aplicado.";
            if (result.Warnings.Count > 0)
            {
                ErrorMessage = string.Join(Environment.NewLine, result.Warnings);
            }

            await RefreshDisplayStateAsync();
            return result;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "No se pudo aplicar el perfil.";
            return ProfileApplicationResult.Failure(exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveProfileAsync(DisplayProfile profile)
    {
        var existing = Profiles.Select(row => row.Profile).ToArray();
        var validation = DisplayProfileValidator.Validate(profile, existing);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
        }

        var profiles = existing.Where(existingProfile => existingProfile.Id != profile.Id).Append(profile).ToArray();
        await PersistProfilesAsync(profiles);
        StatusMessage = $"Perfil {profile.Name} guardado.";
    }

    public async Task DeleteProfileAsync(DisplayProfile profile)
    {
        var profiles = Profiles.Select(row => row.Profile).Where(existing => existing.Id != profile.Id).ToArray();
        await PersistProfilesAsync(profiles);
        StatusMessage = $"Perfil {profile.Name} eliminado.";
    }

    public void RefreshProfiles(IEnumerable<DisplayProfile> profiles)
    {
        Profiles.Clear();
        foreach (var profile in profiles)
        {
            Profiles.Add(new ProfileRowViewModel(profile, BuildSummary(profile)));
        }

        OnPropertyChanged(nameof(CanSaveSetup));
    }

    public async Task RefreshDisplayStateAsync()
    {
        try
        {
            var detected = await _displayService.GetDisplaysAsync();
            foreach (var display in detected)
            {
                _descriptors[display.DisplayId] = display;
            }

            Displays.Clear();
            foreach (var display in _descriptors.Values.OrderBy(display => display.FriendlyName, StringComparer.CurrentCultureIgnoreCase))
            {
                var alias = _displayMappings.FirstOrDefault(mapping =>
                    string.Equals(mapping.Value, display.DisplayId, StringComparison.OrdinalIgnoreCase)).Key;
                Displays.Add(new DisplayRowViewModel(display, alias));
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private async Task PersistProfilesAsync(IReadOnlyList<DisplayProfile> profiles)
    {
        IsBusy = true;
        try
        {
            await _repository.SaveAsync(new ProfileStoreDocument(
                ProfileStoreDocument.CurrentSchemaVersion,
                profiles,
                _displayMappings));
            RefreshProfiles(profiles);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string BuildSummary(DisplayProfile profile)
    {
        var active = profile.Displays
            .Where(display => display.IsEnabled)
            .Select(display =>
            {
                var alias = _displayMappings.FirstOrDefault(mapping =>
                    string.Equals(mapping.Value, display.DisplayId, StringComparison.OrdinalIgnoreCase)).Key ?? "Pantalla sin asignar";
                var mode = display.Mode is { } value
                    ? $"{value.Width}×{value.Height} · {value.RefreshRate} Hz · {DisplayRowViewModel.OrientationLabel(value.Orientation)}"
                    : "Modo sin asignar";
                return $"{alias}  {mode}";
            });
        return string.Join("\n", active);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
