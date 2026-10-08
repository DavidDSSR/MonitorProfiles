namespace MonitorProfiles.Storage;

public static class LocalProfilePathProvider
{
    public static string GetPath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("Windows did not provide a LocalAppData directory.");
        }

        return Path.Combine(localApplicationData, "MonitorProfileController", "profiles.json");
    }

    public static string GetPreferencesPath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("Windows did not provide a LocalAppData directory.");
        }

        return Path.Combine(localApplicationData, "MonitorProfileController", "preferences.json");
    }
}
