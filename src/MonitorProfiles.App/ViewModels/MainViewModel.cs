using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MonitorProfiles.App.Localization;
using MonitorProfiles.App.Services;
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
    private readonly ApplicationPreferencesRepository _preferencesRepository;
    private readonly IThemeService _themeService;
    private readonly IStartupRegistrationService _startupRegistrationService;
    private readonly LocalizationService _localization;
    private ApplicationPreferences _preferences;
    private readonly PreferencesRecoveryReason? _preferencesRecoveryReason;
    private readonly Dictionary<string, DisplayDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _displayMappings = new(StringComparer.OrdinalIgnoreCase);
    private string _statusMessageKey = "Status.Detecting";
    private object?[] _statusMessageArguments = [];
    private string? _errorMessageKey;
    private string? _technicalError;
    private object?[] _errorMessageArguments = [];
    private bool _isBusy;
    private bool _isReady;
    private bool _isCorruptStore;

    public MainViewModel(
        IDisplayService displayService,
        ProfileRepository repository,
        ApplicationPreferencesRepository preferencesRepository,
        ApplicationPreferencesLoadResult preferencesResult,
        IThemeService themeService,
        LocalizationService localization,
        IStartupRegistrationService startupRegistrationService)
    {
        _displayService = displayService;
        _applicationService = new ProfileApplicationService(displayService);
        _repository = repository;
        _preferencesRepository = preferencesRepository;
        _preferences = preferencesResult.Preferences;
        _preferencesRecoveryReason = preferencesResult.RecoveryReason;
        _themeService = themeService;
        _startupRegistrationService = startupRegistrationService;
        _localization = localization;
        _localization.LanguageChanged += Localization_LanguageChanged;
    }

    public ObservableCollection<DisplayRowViewModel> Displays { get; } = [];
    public ObservableCollection<ProfileRowViewModel> Profiles { get; } = [];
    public IReadOnlyList<LanguageOption> Languages => LocalizationService.SupportedLanguages;
    public string SelectedLanguageCode => _localization.LanguageCode;
    public ThemePreference SelectedTheme => _preferences.EffectiveTheme;
    public bool LaunchAtStartup => _preferences.LaunchAtStartup;
    public bool ShowStartupReminder => !LaunchAtStartup && !_preferences.StartupReminderDismissed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StatusMessage
    {
        get => _localization.Get(_statusMessageKey, _statusMessageArguments);
    }

    public string? ErrorMessage
    {
        get
        {
            if (_errorMessageKey is null)
            {
                return null;
            }

            var message = _localization.Get(_errorMessageKey, _errorMessageArguments);
            return string.IsNullOrWhiteSpace(_technicalError)
                ? message
                : $"{message}{Environment.NewLine}{_localization.Get("Error.TechnicalDetails", _technicalError)}";
        }
    }

    public bool HasErrorMessage => _errorMessageKey is not null;

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

    public void SetStatusMessage(string key, params object?[] arguments)
    {
        _statusMessageKey = key;
        _statusMessageArguments = arguments;
        OnPropertyChanged(nameof(StatusMessage));
    }

    private void SetErrorMessage(string? key, string? technicalError = null, params object?[] arguments)
    {
        _errorMessageKey = key;
        _errorMessageArguments = arguments;
        _technicalError = technicalError;
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasErrorMessage));
    }

    private void SetProfileError(string? error)
    {
        var details = error?.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries) ?? [];
        var message = ProfileValidationMessageResolver.ResolveMessage(details);
        var technicalDetails = message.ResourceKey == "Error.InvalidProfile" ? error : null;
        SetErrorMessage(message.ResourceKey, technicalDetails, message.Arguments.ToArray());
    }

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
            var mappings = document?.DisplayMappings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in BuildDisplayRows(_descriptors.Values, mappings))
            {
                AddDisplayRow(row);
            }

            if (document is null)
            {
                IsReady = false;
                SetStatusMessage("Status.SetupRequired");
            }
            else
            {
                _displayMappings = new Dictionary<string, string>(document.DisplayMappings, StringComparer.OrdinalIgnoreCase);
                RefreshProfiles(document.Profiles);
                IsReady = Profiles.Count > 0;
                SetStatusMessage("Status.ScreensDetected", _descriptors.Count);
            }

            if (_preferencesRecoveryReason is not null)
            {
                SetErrorMessage("Status.PreferencesRecovered");
            }
        }
        catch (InvalidDataException exception)
        {
            IsCorruptStore = true;
            SetErrorMessage("Error.ConfigMalformed", exception.Message);
            SetStatusMessage("Error.ConfigMalformed");
        }
        catch (NotSupportedException exception)
        {
            IsCorruptStore = true;
            SetErrorMessage("Error.ConfigFuture", exception.Message);
            SetStatusMessage("Error.ConfigFuture");
        }
        catch (Exception exception)
        {
            SetErrorMessage("Error.DisplayDetection", exception.Message);
            SetStatusMessage("Error.DisplayDetection");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanSaveSetup));
        }
    }

    public async Task SetLanguageAsync(string? languageCode)
    {
        _localization.SetLanguage(languageCode);
        _preferences = _preferences with { LanguageCode = _localization.LanguageCode };
        OnPropertyChanged(nameof(SelectedLanguageCode));
        RefreshProfiles(Profiles.Select(row => row.Profile).ToArray());
        await SavePreferencesAsync();
    }

    public async Task SetThemeAsync(ThemePreference theme)
    {
        _themeService.Apply(theme);
        _preferences = _preferences with { ThemeName = theme.ToString() };
        OnPropertyChanged(nameof(SelectedTheme));
        await SavePreferencesAsync();
    }

    public async Task SetLaunchAtStartupAsync(bool enabled)
    {
        var previous = _preferences;
        var registrationChanged = false;
        SetErrorMessage(null);

        try
        {
            _startupRegistrationService.SetEnabled(enabled);
            registrationChanged = true;
            _preferences = previous with
            {
                LaunchAtStartup = enabled,
                StartupReminderDismissed = true
            };
            OnPropertyChanged(nameof(LaunchAtStartup));
            OnPropertyChanged(nameof(ShowStartupReminder));

            if (!await SavePreferencesAsync())
            {
                _preferences = previous;
                OnPropertyChanged(nameof(LaunchAtStartup));
                OnPropertyChanged(nameof(ShowStartupReminder));
                _startupRegistrationService.SetEnabled(previous.LaunchAtStartup);
            }
        }
        catch (Exception exception)
        {
            _preferences = previous;
            OnPropertyChanged(nameof(LaunchAtStartup));
            OnPropertyChanged(nameof(ShowStartupReminder));
            if (registrationChanged)
            {
                try
                {
                    _startupRegistrationService.SetEnabled(previous.LaunchAtStartup);
                }
                catch (Exception rollbackException)
                {
                    SetErrorMessage("Error.StartupRegistration", $"{exception.Message}{Environment.NewLine}{rollbackException.Message}");
                    return;
                }
            }

            SetErrorMessage("Error.StartupRegistration", exception.Message);
        }
    }

    public async Task DismissStartupReminderAsync()
    {
        if (_preferences.StartupReminderDismissed)
        {
            return;
        }

        var previous = _preferences;
        _preferences = previous with { StartupReminderDismissed = true };
        OnPropertyChanged(nameof(ShowStartupReminder));
        if (!await SavePreferencesAsync())
        {
            _preferences = previous;
            OnPropertyChanged(nameof(ShowStartupReminder));
        }
    }

    public async Task<bool> SaveInitialMappingsAsync()
    {
        if (!CanSaveSetup || IsCorruptStore)
        {
            return false;
        }

        IsBusy = true;
        SetErrorMessage(null);
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
            SetStatusMessage("Status.ProfilesReady");
            return true;
        }
        catch (Exception exception)
        {
            SetErrorMessage("Error.ProfileSave", exception.Message);
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
            return ProfileApplicationResult.Failure(_localization.Get("Status.OperationInProgress"));
        }

        IsBusy = true;
        SetErrorMessage(null);
        SetStatusMessage("Status.ApplyingProfile", profile.Name);
        try
        {
            var result = await _applicationService.ApplySavedAsync(profile);
            if (!result.Succeeded)
            {
                SetProfileError(result.Error);
                SetStatusMessage(result.RestorationSucceeded == false ? "Status.RestoreFailed" : "Status.ApplyFailed");
                return result;
            }

            SetStatusMessage("Status.ProfileApplied", profile.Name);
            if (result.Warnings.Count > 0)
            {
                SetErrorMessage("Error.InvalidProfile", string.Join(Environment.NewLine, result.Warnings));
            }

            await RefreshDisplayStateAsync();
            return result;
        }
        catch (Exception exception)
        {
            SetErrorMessage("Error.InvalidProfile", exception.Message);
            SetStatusMessage("Status.ApplyFailed");
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
        SetStatusMessage("Status.ProfileSaved", profile.Name);
    }

    public async Task DeleteProfileAsync(DisplayProfile profile)
    {
        var profiles = Profiles.Select(row => row.Profile).Where(existing => existing.Id != profile.Id).ToArray();
        await PersistProfilesAsync(profiles);
        SetStatusMessage("Status.ProfileDeleted", profile.Name);
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
            foreach (var row in BuildDisplayRows(_descriptors.Values, _displayMappings))
            {
                AddDisplayRow(row);
            }
        }
        catch (Exception exception)
        {
            SetErrorMessage("Error.DisplayDetection", exception.Message);
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

    private async Task<bool> SavePreferencesAsync()
    {
        IsBusy = true;
        try
        {
            await _preferencesRepository.SaveAsync(_preferences);
            SetErrorMessage(null);
            return true;
        }
        catch (Exception exception)
        {
            SetErrorMessage("Error.Preferences", exception.Message);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Localization_LanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SelectedLanguageCode));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(ErrorMessage));
        foreach (var display in Displays)
        {
            display.RefreshLocalizedProperties();
        }
        RefreshProfiles(Profiles.Select(row => row.Profile).ToArray());
    }

    private string BuildSummary(DisplayProfile profile)
    {
        var active = profile.Displays
            .Where(display => display.IsEnabled)
            .Select(display =>
            {
                var alias = _displayMappings.FirstOrDefault(mapping =>
                    string.Equals(mapping.Value, display.DisplayId, StringComparison.OrdinalIgnoreCase)).Key ?? _localization.Get("Display.Unassigned");
                var mode = display.Mode is { } value
                    ? $"{value.Width}×{value.Height} · {value.RefreshRate} Hz · {DisplayRowViewModel.OrientationLabel(value.Orientation)}"
                    : _localization.Get("Display.NoMode");
                return $"{alias}  {mode}";
            });
        return string.Join("\n", active);
    }

    private static IEnumerable<DisplayRowViewModel> BuildDisplayRows(
        IEnumerable<DisplayDescriptor> descriptors,
        IReadOnlyDictionary<string, string> mappings)
    {
        return descriptors
            .Select(display => new DisplayRowViewModel(
                display,
                mappings.FirstOrDefault(mapping =>
                    string.Equals(mapping.Value, display.DisplayId, StringComparison.OrdinalIgnoreCase)).Key))
            .OrderBy(row => GetAliasOrder(row.Alias))
            .ThenBy(row => row.FriendlyName, StringComparer.CurrentCultureIgnoreCase);
    }

    private static int GetAliasOrder(string? alias)
    {
        for (var index = 0; index < DisplayRowViewModel.DefaultAliases.Count; index++)
        {
            if (string.Equals(DisplayRowViewModel.DefaultAliases[index], alias, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private void AddDisplayRow(DisplayRowViewModel row)
    {
        row.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DisplayRowViewModel.Alias))
            {
                OnPropertyChanged(nameof(CanSaveSetup));
            }
        };
        Displays.Add(row);
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
