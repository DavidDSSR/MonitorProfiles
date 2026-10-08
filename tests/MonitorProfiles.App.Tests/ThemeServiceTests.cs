using MonitorProfiles.App.Services;
using MonitorProfiles.Storage;

namespace MonitorProfiles.App.Tests;

public sealed class ThemeServiceTests
{
    [Theory]
    [InlineData(ThemePreference.System, true, ThemeAppearance.Light)]
    [InlineData(ThemePreference.System, false, ThemeAppearance.Dark)]
    [InlineData(ThemePreference.Light, false, ThemeAppearance.Light)]
    [InlineData(ThemePreference.Dark, true, ThemeAppearance.Dark)]
    public void ResolveAppearance_uses_system_appearance_or_explicit_override(
        ThemePreference preference,
        bool windowsUsesLightAppearance,
        ThemeAppearance expected)
    {
        Assert.Equal(expected, ThemeService.ResolveAppearance(preference, windowsUsesLightAppearance));
    }
}
