using System.Text.Json;

namespace MonitorProfiles.Storage;

public sealed class ApplicationPreferencesRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _path;

    public ApplicationPreferencesRepository(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    public async Task<ApplicationPreferencesLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return new ApplicationPreferencesLoadResult(ApplicationPreferences.Default, null);
        }

        ApplicationPreferences? preferences;
        try
        {
            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            preferences = await JsonSerializer.DeserializeAsync<ApplicationPreferences>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return new ApplicationPreferencesLoadResult(
                ApplicationPreferences.Default,
                PreferencesRecoveryReason.Malformed);
        }

        if (preferences is null || preferences.SchemaVersion != ApplicationPreferences.CurrentSchemaVersion)
        {
            return new ApplicationPreferencesLoadResult(
                ApplicationPreferences.Default,
                PreferencesRecoveryReason.UnsupportedSchema);
        }

        return new ApplicationPreferencesLoadResult(preferences.Normalize(), null);
    }

    public async Task SaveAsync(ApplicationPreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (preferences.SchemaVersion != ApplicationPreferences.CurrentSchemaVersion)
        {
            throw new ArgumentException("Cannot save preferences with an unsupported schema version.", nameof(preferences));
        }

        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, preferences.Normalize(), JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
