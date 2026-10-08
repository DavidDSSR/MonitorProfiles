using MonitorProfiles.Core.Models;
using MonitorProfiles.App.Localization;

namespace MonitorProfiles.App.ViewModels;

public sealed class ProfileRowViewModel(DisplayProfile profile, string summary)
{
    public DisplayProfile Profile { get; } = profile;
    public string Name => Profile.Name;
    public string Summary { get; } = summary;
    public string Kind => LocalizationService.Instance.Get(Profile.IsBuiltIn ? "Profiles.BuiltIn" : "Profiles.Custom");
}
