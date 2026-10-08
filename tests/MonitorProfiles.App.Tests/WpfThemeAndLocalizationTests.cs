using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MonitorProfiles.App.Localization;
using MonitorProfiles.App.Services;
using MonitorProfiles.Storage;

namespace MonitorProfiles.App.Tests;

public sealed class WpfThemeAndLocalizationTests
{
    [Fact]
    public void Theme_dictionary_and_bound_text_update_at_runtime()
    {
        RunOnSta(() =>
        {
            var application = new Application();
            application.Resources.MergedDictionaries.Add(LoadTheme("Light"));
            using var themeService = new ThemeService(application);

            themeService.Apply(ThemePreference.Light);
            var lightBackground = Assert.IsType<SolidColorBrush>(application.Resources["AppBackgroundBrush"]);
            Assert.Equal(Color.FromRgb(0xF3, 0xF2, 0xEE), lightBackground.Color);

            themeService.Apply(ThemePreference.Dark);
            var darkBackground = Assert.IsType<SolidColorBrush>(application.Resources["AppBackgroundBrush"]);
            Assert.Equal(Color.FromRgb(0x1B, 0x1E, 0x1C), darkBackground.Color);

            var text = new TextBlock();
            BindingOperations.SetBinding(text, TextBlock.TextProperty, new Binding("[Profiles.Apply]")
            {
                Source = LocalizationService.Instance
            });
            LocalizationService.Instance.SetLanguage("en");
            Assert.Equal("Apply", text.Text);

            LocalizationService.Instance.SetLanguage("ja");
            Assert.Equal("適用", text.Text);
            LocalizationService.Instance.SetLanguage("en");
            application.Shutdown();
        });
    }

    [Fact]
    public void Light_and_dark_palettes_define_the_same_semantic_brushes()
    {
        RunOnSta(() =>
        {
            var light = LoadTheme("Light");
            var dark = LoadTheme("Dark");
            var required = new[]
            {
                "AppBackgroundBrush", "SurfaceBrush", "RaisedSurfaceBrush", "TextPrimaryBrush",
                "TextSecondaryBrush", "BorderBrush", "AccentBrush", "AccentForegroundBrush",
                "FocusBrush", "SuccessBrush", "WarningBrush", "ErrorBrush", "DisabledBrush"
            };

            foreach (var key in required)
            {
                Assert.True(light.Contains(key), $"Light theme is missing {key}.");
                Assert.True(dark.Contains(key), $"Dark theme is missing {key}.");
            }
        });
    }

    private static ResourceDictionary LoadTheme(string name) => new()
    {
        Source = new Uri($"pack://application:,,,/MonitorProfiles.App;component/Resources/Themes/{name}.xaml", UriKind.Absolute)
    };

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw new InvalidOperationException("WPF theme/localization runtime test failed.", failure);
        }
    }
}
