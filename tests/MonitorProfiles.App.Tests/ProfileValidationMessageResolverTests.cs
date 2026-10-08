using MonitorProfiles.App.Localization;

namespace MonitorProfiles.App.Tests;

public sealed class ProfileValidationMessageResolverTests
{
    [Fact]
    public void Resolve_uses_the_localized_required_name_message()
    {
        Assert.Equal(
            "Editor.NameRequired",
            ProfileValidationMessageResolver.Resolve(["Profile name cannot be empty."]));
    }

    [Fact]
    public void Resolve_uses_the_localized_duplicate_name_message()
    {
        Assert.Equal(
            "Editor.DuplicateName",
            ProfileValidationMessageResolver.Resolve(["A profile named 'Work' already exists."]));
    }

    [Fact]
    public void Resolve_uses_the_localized_unavailable_mode_message_and_display_name()
    {
        var result = ProfileValidationMessageResolver.ResolveMessage(["The requested mode is unavailable on 'Q270W16-1'."]);

        Assert.Equal("Error.ProfileModeUnavailable", result.ResourceKey);
        Assert.Equal("Q270W16-1", Assert.Single(result.Arguments));
    }
}
