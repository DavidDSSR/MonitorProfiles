using MonitorProfiles.Core.Models;

namespace MonitorProfiles.Storage;

public sealed record ProfileStoreDocument(
    int SchemaVersion,
    IReadOnlyList<DisplayProfile> Profiles,
    IReadOnlyDictionary<string, string> DisplayMappings)
{
    public const int CurrentSchemaVersion = 1;
}
