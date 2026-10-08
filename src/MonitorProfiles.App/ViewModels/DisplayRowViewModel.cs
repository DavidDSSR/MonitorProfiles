using MonitorProfiles.Core.Models;
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
    public string State => Descriptor.IsActive ? "Encendida" : "Apagada";
    public string ModeSummary => Descriptor.CurrentMode is { } mode
        ? $"{mode.Width}×{mode.Height} · {mode.RefreshRate} Hz · {OrientationLabel(mode.Orientation)}"
        : "Sin modo activo";
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

    internal static string OrientationLabel(DisplayOrientation orientation) => orientation switch
    {
        DisplayOrientation.Landscape => "Horizontal",
        DisplayOrientation.Portrait => "Vertical",
        DisplayOrientation.LandscapeFlipped => "Horizontal volteado",
        DisplayOrientation.PortraitFlipped => "Vertical volteado",
        _ => orientation.ToString()
    };
}
