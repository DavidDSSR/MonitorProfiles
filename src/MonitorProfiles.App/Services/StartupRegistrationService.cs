using Microsoft.Win32;

namespace MonitorProfiles.App.Services;

public interface IStartupRegistrationService
{
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}

public sealed class StartupRegistrationService : IStartupRegistrationService
{
    public const string BackgroundStartupArgument = "--background";
    private const string RegistryValueName = "MonitorProfiles";
    private const string DefaultRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _executablePath;
    private readonly string _runKeyPath;

    public StartupRegistrationService(string? executablePath = null, string? runKeyPath = null)
    {
        _executablePath = Path.GetFullPath(executablePath ?? Environment.ProcessPath ??
            throw new InvalidOperationException("The current executable path could not be determined."));
        _runKeyPath = runKeyPath ?? DefaultRunKeyPath;
    }

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(_runKeyPath);
            return string.Equals(
                key?.GetValue(RegistryValueName) as string,
                BuildStartupCommand(),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(_runKeyPath, writable: true)
                ?? throw new InvalidOperationException("The current user's Windows startup settings could not be opened.");
            key.SetValue(RegistryValueName, BuildStartupCommand(), RegistryValueKind.String);
            return;
        }

        using var existingKey = Registry.CurrentUser.OpenSubKey(_runKeyPath, writable: true);
        existingKey?.DeleteValue(RegistryValueName, throwOnMissingValue: false);
    }

    public static bool IsBackgroundStartup(IEnumerable<string> arguments) =>
        arguments.Any(argument => string.Equals(argument, BackgroundStartupArgument, StringComparison.OrdinalIgnoreCase));

    private string BuildStartupCommand() => $"\"{_executablePath}\" {BackgroundStartupArgument}";
}
