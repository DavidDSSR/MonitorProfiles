using MonitorProfiles.Core.Models;
using MonitorProfiles.Core.Profiles;

namespace MonitorProfiles.Core.Tests;

public sealed class DisplayProfileValidatorTests
{
    [Fact]
    public void Validate_rejects_a_blank_name()
    {
        var result = DisplayProfileValidator.Validate(CreateProfile("  "));

        Assert.Contains(result.Errors, error => error.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_duplicate_names_case_insensitively_but_ignores_same_profile()
    {
        var existing = CreateProfile("Work");

        var duplicate = DisplayProfileValidator.Validate(CreateProfile("work"), [existing]);
        var self = DisplayProfileValidator.Validate(existing, [existing]);

        Assert.Contains(duplicate.Errors, error => error.Contains("already exists", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(self.Errors, error => error.Contains("already exists", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_requires_one_active_primary_display()
    {
        var noPrimary = CreateProfile("No primary", isPrimary: false);
        var twoPrimaries = CreateProfile("Two primary", includeSecondActivePrimary: true);

        Assert.Contains(DisplayProfileValidator.Validate(noPrimary).Errors, error => error.Contains("primary", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(DisplayProfileValidator.Validate(twoPrimaries).Errors, error => error.Contains("primary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_enabled_display_without_a_mode()
    {
        var profile = new DisplayProfile(Guid.NewGuid(), "Broken", [new DisplayAssignment("device-1", true, null, true)]);

        var result = DisplayProfileValidator.Validate(profile);

        Assert.Contains(result.Errors, error => error.Contains("mode", StringComparison.OrdinalIgnoreCase));
    }

    private static DisplayProfile CreateProfile(
        string name,
        bool isPrimary = true,
        bool includeSecondActivePrimary = false)
    {
        var displays = new List<DisplayAssignment>
        {
            new("device-1", true, new DisplayMode(1920, 1080, 60, DisplayOrientation.Landscape), isPrimary)
        };
        if (includeSecondActivePrimary)
        {
            displays.Add(new DisplayAssignment("device-2", true, new DisplayMode(1920, 1080, 60, DisplayOrientation.Landscape), true));
        }

        return new DisplayProfile(Guid.NewGuid(), name, displays);
    }
}
