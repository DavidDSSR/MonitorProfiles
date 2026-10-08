using System.Globalization;
using System.Resources;
using MonitorProfiles.App.Localization;

namespace MonitorProfiles.App.Tests;

public sealed class LocalizationServiceTests
{
    [Fact]
    public void Every_supported_locale_contains_every_required_ui_string()
    {
        var resourceManager = new ResourceManager("MonitorProfiles.App.Resources.Strings", typeof(LocalizationService).Assembly);

        foreach (var language in LocalizationService.SupportedLanguages)
        {
            var culture = language.Code == "en"
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(language.Code);
            var resourceSet = resourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);

            Assert.True(resourceSet is not null, $"No translated resource set was found for language '{language.Code}'.");
            foreach (var key in LocalizationService.RequiredResourceKeys)
            {
                Assert.False(string.IsNullOrWhiteSpace(resourceSet!.GetString(key)), $"Missing {key} in {language.Code}.");
            }
        }
    }

    [Fact]
    public void SetLanguage_updates_bound_text_and_notifies_open_views_and_tray()
    {
        var localization = new LocalizationService();
        var propertyChanges = 0;
        var languageChanges = 0;
        localization.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "Item[]")
            {
                propertyChanges++;
            }
        };
        localization.LanguageChanged += (_, _) => languageChanges++;

        localization.SetLanguage("fr");

        Assert.Equal("fr", localization.LanguageCode);
        Assert.Equal("Appliquer", localization["Profiles.Apply"]);
        Assert.Equal(1, propertyChanges);
        Assert.Equal(1, languageChanges);
    }

    [Fact]
    public void SetLanguage_falls_back_to_english_for_an_unknown_code()
    {
        var localization = new LocalizationService();

        localization.SetLanguage("xx-YY");

        Assert.Equal("en", localization.LanguageCode);
        Assert.Equal("Apply", localization["Profiles.Apply"]);
    }

    [Fact]
    public void Language_options_use_native_language_names_and_english_is_first()
    {
        Assert.Equal(
            new[] { "en", "es", "fr", "it", "ja", "de", "zh-Hans" },
            LocalizationService.SupportedLanguages.Select(language => language.Code));
        Assert.Contains(LocalizationService.SupportedLanguages, language => language.DisplayName == "日本語");
        Assert.Contains(LocalizationService.SupportedLanguages, language => language.DisplayName == "简体中文");
    }
}
