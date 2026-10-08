using MonitorProfiles.Core.Models;
using MonitorProfiles.App.Localization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonitorProfiles.App.ViewModels;

public sealed class DisplayRowViewModel : INotifyPropertyChanged
{
    private string? _alias;

    public DisplayRowViewModel(DisplayDescriptor descriptor, string? alias)
    {
        Descriptor = descriptor;
        _alias = alias;
    }

    public static IReadOnlyList<string> DefaultAliases { get; } =
        ["Monitor 1", "Monitor 2", "Monitor 3", "Monitor 4"];

    public DisplayDescriptor Descriptor { get; }
    public string FriendlyName => Descriptor.FriendlyName;
    public string DeviceId => Descriptor.DisplayId;
    public string State => LocalizationService.Instance.Get(Descriptor.IsActive ? "Display.Active" : "Display.Inactive");
    public string ModeSummary => Descriptor.CurrentMode is { } mode
        ? $"{mode.Width}×{mode.Height} · {mode.RefreshRate} Hz · {OrientationLabel(mode.Orientation)}"
        : LocalizationService.Instance.Get("Display.NoMode");
    public IReadOnlyList<string> AliasOptions => DefaultAliases;
    public string? Alias
    {
        get => _alias;
        set
        {
            if (_alias == value)
            {
                return;
            }

            _alias = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Alias)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshLocalizedProperties()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ModeSummary)));
    }

    internal static string OrientationLabel(DisplayOrientation orientation) => orientation switch
    {
        DisplayOrientation.Landscape => LocalizationService.Instance.Get("Editor.Landscape"),
        DisplayOrientation.Portrait => LocalizationService.Instance.Get("Editor.Portrait"),
        DisplayOrientation.LandscapeFlipped => LocalizationService.Instance.Get("Editor.LandscapeFlipped"),
        DisplayOrientation.PortraitFlipped => LocalizationService.Instance.Get("Editor.PortraitFlipped"),
        _ => LocalizationService.Instance.Get("Editor.Landscape")
    };
}
