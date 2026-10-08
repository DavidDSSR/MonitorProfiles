namespace MonitorProfiles.Core.Services;

public sealed record ProfileApplicationResult(
    bool Succeeded,
    string? Error,
    IReadOnlyList<string> Warnings,
    bool? RestorationSucceeded = null)
{
    public static ProfileApplicationResult Success(IReadOnlyList<string>? warnings = null) =>
        new(true, null, warnings ?? Array.Empty<string>());

    public static ProfileApplicationResult Failure(string error, bool? restorationSucceeded = null) =>
        new(false, error, Array.Empty<string>(), restorationSucceeded);
}
