using MonitorProfiles.Core.Models;

namespace MonitorProfiles.App.ViewModels;

public sealed class ProfileRowViewModel(DisplayProfile profile, string summary)
{
    public DisplayProfile Profile { get; } = profile;
    public string Name => Profile.Name;
    public string Summary { get; } = summary;
    public string Kind => Profile.IsBuiltIn ? "INCLUIDO" : "PERSONALIZADO";
}
