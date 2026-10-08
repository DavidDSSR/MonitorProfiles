namespace MonitorProfiles.Core.Profiles;

public sealed record ProfileValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
