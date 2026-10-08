using MonitorProfiles.Core.Models;

namespace MonitorProfiles.App.ViewModels;

public sealed class ProfileEditorDisplayRow
{
    public ProfileEditorDisplayRow(DisplayRowViewModel display, DisplayAssignment? existing)
    {
        Descriptor = display.Descriptor;
        DisplayId = display.DeviceId;
        Alias = display.Alias ?? display.FriendlyName;
        IsEnabled = existing?.IsEnabled ?? false;
        IsPrimary = existing?.IsPrimary ?? false;
        var initialMode = existing?.Mode ?? display.Descriptor.CurrentMode ?? DefaultModeForAlias(display.Alias);
        Width = initialMode.Width.ToString();
        Height = initialMode.Height.ToString();
        RefreshRate = initialMode.RefreshRate.ToString();
        Orientation = initialMode.Orientation;

        var options = display.Descriptor.SupportedModes
            .Where(mode => mode.Width >= 800 && mode.Height >= 600)
            .Distinct()
            .Select(mode => new ProfileEditorModeOption(mode, $"Detectado · {FormatMode(mode)}"))
            .ToList();
        if (!options.Any(option => option.Mode == initialMode))
        {
            var source = existing?.Mode is not null
                ? "Guardado"
                : display.Descriptor.CurrentMode is not null ? "Actual" : "Manual";
            options.Add(new ProfileEditorModeOption(initialMode, $"{source} · {FormatMode(initialMode)}"));
        }

        ModeOptions = options;
        SelectedModeOption = options.FirstOrDefault(option => option.Mode == initialMode);
    }

    public DisplayDescriptor Descriptor { get; }
    public string DisplayId { get; }
    public string Alias { get; }
    public bool IsEnabled { get; set; }
    public bool IsPrimary { get; set; }
    public string Width { get; set; }
    public string Height { get; set; }
    public string RefreshRate { get; set; }
    public DisplayOrientation Orientation { get; set; }
    public IReadOnlyList<ProfileEditorModeOption> ModeOptions { get; }
    public ProfileEditorModeOption? SelectedModeOption { get; set; }

    public void UseMode(ProfileEditorModeOption? option)
    {
        if (option is null)
        {
            return;
        }

        Width = option.Mode.Width.ToString();
        Height = option.Mode.Height.ToString();
        RefreshRate = option.Mode.RefreshRate.ToString();
        Orientation = option.Mode.Orientation;
    }

    public DisplayAssignment ToAssignment()
    {
        if (!IsEnabled)
        {
            return new DisplayAssignment(DisplayId, IsEnabled: false, Mode: null, IsPrimary: false);
        }

        if (!int.TryParse(Width, out var width) || width is < 320 or > 16384 ||
            !int.TryParse(Height, out var height) || height is < 320 or > 16384 ||
            !int.TryParse(RefreshRate, out var refreshRate) || refreshRate is < 24 or > 1000)
        {
            throw new InvalidOperationException($"La resolución o frecuencia de {Alias} no es válida.");
        }

        return new DisplayAssignment(
            DisplayId,
            IsEnabled,
            new DisplayMode(width, height, refreshRate, Orientation),
            IsPrimary);
    }

    private static string FormatMode(DisplayMode mode) =>
        $"{mode.Width}×{mode.Height} · {mode.RefreshRate} Hz · {DisplayRowViewModel.OrientationLabel(mode.Orientation)}";

    private static DisplayMode DefaultModeForAlias(string? alias) => alias switch
    {
        "Monitor 2" => new DisplayMode(2560, 1440, 144, DisplayOrientation.Landscape),
        "Monitor 3" => new DisplayMode(1080, 1920, 60, DisplayOrientation.PortraitFlipped),
        "Monitor 4" => new DisplayMode(2560, 1440, 144, DisplayOrientation.Landscape),
        _ => new DisplayMode(1920, 1080, 60, DisplayOrientation.Landscape)
    };
}

public sealed record ProfileEditorModeOption(DisplayMode Mode, string Label);
