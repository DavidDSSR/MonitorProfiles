using Microsoft.Win32;
using MonitorProfiles.App.Services;

namespace MonitorProfiles.App.Tests;

public sealed class StartupRegistrationServiceTests
{
    [Fact]
    public void IsBackgroundStartup_recognizes_the_startup_argument_case_insensitively()
    {
        Assert.True(StartupRegistrationService.IsBackgroundStartup(["--BACKGROUND"]));
        Assert.False(StartupRegistrationService.IsBackgroundStartup(["--other"]));
    }

    [Fact]
    public void SetEnabled_registers_the_executable_for_background_startup_and_removes_it_when_disabled()
    {
        const string executablePath = @"C:\Program Files\Monitor Profiles\MonitorProfiles.App.exe";
        var testRoot = $@"Software\MonitorProfilesStartupTests_{Guid.NewGuid():N}";
        var registrySubKey = $@"{testRoot}\Run";

        try
        {
            var service = new StartupRegistrationService(executablePath, registrySubKey);

            Assert.False(service.IsEnabled);

            service.SetEnabled(true);

            using (var key = Registry.CurrentUser.OpenSubKey(registrySubKey))
            {
                Assert.NotNull(key);
                Assert.Equal($"\"{executablePath}\" --background", key!.GetValue("MonitorProfiles"));
            }
            Assert.True(service.IsEnabled);

            service.SetEnabled(false);

            Assert.False(service.IsEnabled);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(testRoot, throwOnMissingSubKey: false);
        }
    }
}
