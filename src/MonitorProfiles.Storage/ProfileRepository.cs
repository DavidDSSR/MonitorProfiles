using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonitorProfiles.Storage;

public sealed class ProfileRepository
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly string _path;

    public ProfileRepository(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    public async Task<ProfileStoreDocument?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        ProfileStoreDocument? document;
        try
        {
            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            document = await JsonSerializer.DeserializeAsync<ProfileStoreDocument>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The profile store contains malformed JSON and was left unchanged.", exception);
        }

        if (document is null)
        {
            throw new InvalidDataException("The profile store is empty or does not contain a profile document.");
        }

        if (document.SchemaVersion > ProfileStoreDocument.CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Profile schema {document.SchemaVersion} is newer than this app supports.");
        }

        if (document.SchemaVersion != ProfileStoreDocument.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Profile schema {document.SchemaVersion} is invalid or unsupported.");
        }

        if (document.Profiles is null || document.DisplayMappings is null)
        {
            throw new InvalidDataException("The profile store is missing required data and was left unchanged.");
        }

        return document;
    }

    public async Task SaveAsync(ProfileStoreDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.SchemaVersion != ProfileStoreDocument.CurrentSchemaVersion)
        {
            throw new ArgumentException("Cannot save a document with an unsupported schema version.", nameof(document));
        }

        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
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

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
