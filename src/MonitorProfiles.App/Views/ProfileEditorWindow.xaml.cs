using System.Windows;
using MonitorProfiles.App.Localization;
using MonitorProfiles.App.ViewModels;
using MonitorProfiles.Core.Models;

namespace MonitorProfiles.App.Views;

public partial class ProfileEditorWindow : Window
{
    private readonly DisplayProfile? _existingProfile;

    public ProfileEditorWindow(IEnumerable<DisplayRowViewModel> displays, DisplayProfile? profile = null)
    {
        InitializeComponent();
        _existingProfile = profile;
        NameTextBox.Text = profile?.Name ?? string.Empty;

        var assignments = profile?.Displays.ToDictionary(display => display.DisplayId, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, DisplayAssignment>(StringComparer.OrdinalIgnoreCase);
        DisplayList.ItemsSource = displays
            .Select(display => new ProfileEditorDisplayRow(
                display,
                assignments.GetValueOrDefault(display.DeviceId)))
            .ToArray();
        Closed += (_, _) =>
        {
            foreach (var row in DisplayList.Items.Cast<ProfileEditorDisplayRow>())
            {
                row.Dispose();
            }
        };
    }

    public DisplayProfile? ResultProfile { get; private set; }
    public bool WantsPreview { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e) => SaveDraft(preview: false);

    private void Preview_Click(object sender, RoutedEventArgs e) => SaveDraft(preview: true);

    private void DetectedMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ComboBox { DataContext: ProfileEditorDisplayRow row } comboBox)
        {
            row.UseMode(comboBox.SelectedItem as ProfileEditorModeOption);
        }
    }

    private void SaveDraft(bool preview)
    {
        try
        {
            var assignments = DisplayList.Items.Cast<ProfileEditorDisplayRow>()
                .Select(row => row.ToAssignment())
                .ToArray();
            var profile = new DisplayProfile(
                _existingProfile?.Id ?? Guid.NewGuid(),
                NameTextBox.Text.Trim(),
                assignments,
                _existingProfile?.IsBuiltIn ?? false);

            ResultProfile = profile;
            WantsPreview = preview;
            DialogResult = true;
        }
        catch (Exception exception)
        {
            var message = $"{LocalizationService.Instance.Get("Error.InvalidProfile")}{Environment.NewLine}{LocalizationService.Instance.Get("Error.TechnicalDetails", exception.Message)}";
            System.Windows.MessageBox.Show(this, message, LocalizationService.Instance.Get("Dialog.ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
