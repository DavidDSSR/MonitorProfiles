using System.Windows.Data;
using System.Windows.Markup;

namespace MonitorProfiles.App.Localization;

[MarkupExtensionReturnType(typeof(object))]
public sealed class TranslateExtension(string key) : MarkupExtension
{
    public string Key { get; } = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}
