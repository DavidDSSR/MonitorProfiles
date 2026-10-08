using MonitorProfiles.Core.Models;
using MonitorProfiles.Storage;

namespace MonitorProfiles.Storage.Tests;

public sealed class ProfileRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "MonitorProfilesTests", Guid.NewGuid().ToString("N"));
    private readonly string _path;

    public ProfileRepositoryTests()
    {
        _path = Path.Combine(_directory, "profiles.json");
    }

    [Fact]
    public async Task LoadAsync_returns_null_when_store_does_not_exist()
    {
        var repository = new ProfileRepository(_path);

        var result = await repository.LoadAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_and_LoadAsync_round_trip_profiles_and_display_mappings()
    {
        var repository = new ProfileRepository(_path);
        var profile = new DisplayProfile(
            Guid.NewGuid(),
            "Trabajo",
            [new DisplayAssignment("device-2", true, new DisplayMode(2560, 1440, 144, DisplayOrientation.Landscape), true)],
            IsBuiltIn: true);
        var document = new ProfileStoreDocument(
            ProfileStoreDocument.CurrentSchemaVersion,
            [profile],
            new Dictionary<string, string> { ["Monitor 2"] = "device-2" });

        await repository.SaveAsync(document);
        var result = await repository.LoadAsync();

        Assert.Equal(document.SchemaVersion, result!.SchemaVersion);
        var loadedProfile = Assert.Single(result.Profiles);
        Assert.Equal(profile.Id, loadedProfile.Id);
        Assert.Equal(profile.Name, loadedProfile.Name);
        Assert.Equal(profile.IsBuiltIn, loadedProfile.IsBuiltIn);
        Assert.Equal(profile.Displays, loadedProfile.Displays);
        Assert.Equal("device-2", result.DisplayMappings["Monitor 2"]);
    }

    [Fact]
    public async Task LoadAsync_preserves_malformed_store_and_reports_recovery_error()
    {
        Directory.CreateDirectory(_directory);
        const string malformed = "{ not valid json";
        await File.WriteAllTextAsync(_path, malformed);
        var repository = new ProfileRepository(_path);

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync());

        Assert.Equal(malformed, await File.ReadAllTextAsync(_path));
    }

    [Fact]
    public async Task LoadAsync_rejects_a_future_schema_version_without_overwriting_it()
    {
        Directory.CreateDirectory(_directory);
        const string future = "{\"schemaVersion\":999,\"profiles\":[],\"displayMappings\":{}}";
        await File.WriteAllTextAsync(_path, future);
        var repository = new ProfileRepository(_path);

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.LoadAsync());

        Assert.Equal(future, await File.ReadAllTextAsync(_path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
