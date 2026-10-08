# Monitor Profiles UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the basic WPF presentation with an elegant, minimal, localized Windows 11 interface that supports System/Light/Dark themes and seven UI languages without changing monitor-profile behavior.

**Architecture:** Keep the existing WPF application and native display workflow. Add a small per-user preferences store, a runtime localization service backed by .resx satellite resources, and semantic light/dark ResourceDictionaries layered over the .NET 10 WPF Fluent theme. Refactor existing views and view models to use localized strings and semantic theme resources.

**Tech Stack:** C# / .NET 10, WPF Fluent ThemeMode, `System.Resources.ResourceManager`, WPF ResourceDictionaries, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-07-monitor-profiles-ui-design.md`

## Global Constraints

- Target Windows 11 x64; keep the existing WPF application and native display services.
- Default language is English; supported languages are English, Spanish, French, Italian, Japanese, German, and Simplified Chinese (`zh-Hans`).
- Default theme is System; explicit Light and Dark selections persist and apply immediately.
- Keep built-in names “Trabajo”, “Competitivo”, and “Historia” unchanged; never translate user-created names.
- Use semantic WPF theme resources; do not introduce hard-coded per-view colors or a new UI framework.
- Preserve monitor detection, display identity mapping, profile CRUD, one-action application, pre-validation, preview timeout, rollback, and tray actions.
- Keep the interface minimal and restrained: no neon, decorative gradients, emoji icons, or nested cards.
- Impeccable's web detector does not inspect WPF/XAML; use its Operate-mode principles and manual native-app review rather than claiming a web detector pass.
- Never apply a display profile during theme/localization tests.

---

## Planned File Structure

- `src/MonitorProfiles.Storage/ApplicationPreferences.cs` — versioned theme/language preference model and allowed values.
- `src/MonitorProfiles.Storage/ApplicationPreferencesRepository.cs` — per-user preferences JSON at `%LocalAppData%\MonitorProfileController\preferences.json`, atomic writes, safe defaults and corrupt-data behavior.
- `src/MonitorProfiles.App/Localization/LocalizationService.cs` — current culture, key lookup, `INotifyPropertyChanged`, `LanguageChanged` event and resource fallback.
- `src/MonitorProfiles.App/Localization/TranslateExtension.cs` — WPF markup extension that binds XAML text to the localization service and refreshes when language changes.
- `src/MonitorProfiles.App/Localization/LanguageOption.cs` — language code and native display label for the language menu.
- `src/MonitorProfiles.App/Resources/Strings.resx` — English default strings.
- `src/MonitorProfiles.App/Resources/Strings.es.resx`, `Strings.fr.resx`, `Strings.it.resx`, `Strings.ja.resx`, `Strings.de.resx`, `Strings.zh-Hans.resx` — complete translations for all UI-owned string keys.
- `src/MonitorProfiles.App/Services/ThemeService.cs` — maps System/Light/Dark preference to WPF `ThemeMode` and the app semantic theme dictionary.
- `src/MonitorProfiles.App/Resources/Themes/Light.xaml`, `Dark.xaml` — semantic brush/style tokens for app surfaces, text, borders, state and selection.
- `src/MonitorProfiles.App/App.xaml`, `App.xaml.cs` — load preferences, set theme/culture before showing a window, and share localization/theme services.
- `src/MonitorProfiles.App/MainWindow.xaml`, `MainWindow.xaml.cs` — refined header, profile actions, current-display tiles, language/theme menus and setup state.
- `src/MonitorProfiles.App/Views/ProfileEditorWindow.xaml`, `.xaml.cs` — localized themed profile editor, mode options and validation feedback.
- `src/MonitorProfiles.App/Views/PreviewConfirmationWindow.xaml`, `.xaml.cs` — localized themed keep/revert countdown.
- `src/MonitorProfiles.App/ViewModels/MainViewModel.cs`, `DisplayRowViewModel.cs`, `ProfileRowViewModel.cs`, `ProfileEditorDisplayRow.cs` — replace hard-coded presentation text with localized keys while retaining profile data unchanged.
- `src/MonitorProfiles.App/Services/TrayIconService.cs` — native notification-area labels/items refreshed on language change.
- `tests/MonitorProfiles.Storage.Tests/ApplicationPreferencesRepositoryTests.cs` — preferences defaults, round-trip, atomic storage and malformed-file behavior.
- `tests/MonitorProfiles.App.Tests/` — localization key coverage for all locales, fallback behavior and theme mapping.

## Task 1: Add versioned application preferences (TDD)

**Files:** `src/MonitorProfiles.Storage/ApplicationPreferences.cs`, `ApplicationPreferencesRepository.cs`; `tests/MonitorProfiles.Storage.Tests/ApplicationPreferencesRepositoryTests.cs`.

- [ ] Write a test that missing preferences return `ThemePreference.System` and language code `en`.
- [ ] Run that test and verify it fails because preferences types/repository do not exist.
- [ ] Write round-trip tests for each theme enum and language-code persistence.
- [ ] Write tests that unknown theme/language values fall back to System/English without changing profile data.
- [ ] Implement a versioned document with validated language codes `en`, `es`, `fr`, `it`, `ja`, `de`, `zh-Hans`; reject unsupported values by falling back to defaults.
- [ ] Persist to `preferences.json` using a temporary sibling file and atomic replacement. On malformed data, preserve the original file and return defaults with a recovery result.
- [ ] Run `dotnet test tests/MonitorProfiles.Storage.Tests/MonitorProfiles.Storage.Tests.csproj` and verify all storage tests pass.

## Task 2: Build localization service and verify all seven resource sets (TDD)

**Files:** `src/MonitorProfiles.App/Localization/LocalizationService.cs`, `TranslateExtension.cs`, `LanguageOption.cs`; seven `.resx` files under `src/MonitorProfiles.App/Resources/`; `tests/MonitorProfiles.App.Tests/` and solution project references.

- [ ] Add `MonitorProfiles.App.Tests` targeting `net10.0-windows`, with references to the App and xUnit test infrastructure.
- [ ] Write a catalog test listing required resource keys and asserting each locale resolves every key to non-empty text.
- [ ] Run the catalog test and verify it fails because the localization catalog is missing/incomplete.
- [ ] Write tests for English fallback on unknown locale and `INotifyPropertyChanged` notification when language changes.
- [ ] Implement `LocalizationService` using a neutral English `.resx` plus six culture-specific resource files, a notifying indexer, and `event EventHandler LanguageChanged`. Keep custom and built-in profile names out of translation resources; these come from saved profile data.
- [ ] Implement `TranslateExtension` over the localization service indexer so XAML bindings update immediately after a locale switch.
- [ ] Add a required-key manifest grouped by app shell, setup, profile list, editor, preview, tray, validation, status, and errors. Translate every key in all six non-English resource files; preserve interpolation arguments and Unicode punctuation.
- [ ] Run `dotnet test tests/MonitorProfiles.App.Tests/MonitorProfiles.App.Tests.csproj` and verify every culture has complete keys and expected fallback behavior.

## Task 3: Add runtime Light/Dark/System theme service (TDD)

**Files:** `src/MonitorProfiles.App/Services/ThemeService.cs`, `Resources/Themes/Light.xaml`, `Dark.xaml`, `App.xaml`, `App.xaml.cs`; tests under `tests/MonitorProfiles.App.Tests/`.

- [ ] Write tests that each stored preference maps to System/Light/Dark behavior and that invalid values select System.
- [ ] Implement the theme service using .NET 10 WPF Fluent `ThemeMode` (`System`, `Light`, `Dark`). Suppress experimental warning `WPF0001` only at the code site required to change `Application.ThemeMode` at runtime.
- [ ] Define the same semantic keys in both dictionaries: `AppBackgroundBrush`, `SurfaceBrush`, `RaisedSurfaceBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`, `BorderBrush`, `AccentBrush`, `FocusBrush`, `SuccessBrush`, `WarningBrush`, and `ErrorBrush`.
- [ ] Use warm off-white/graphite surfaces in Light and warm charcoal/soft-white surfaces in Dark; use one muted brass/ochre accent and preserve contrast for both text and state indicators.
- [ ] Load the matching custom dictionary and native Fluent mode from the preference service; update the resource dictionary and notify open windows on a change.
- [ ] Test dictionary key parity between Light and Dark and test invalid preference fallback.
- [ ] Run App.Tests and build the WPF App to validate both XAML dictionaries.

## Task 4: Connect preferences to application startup and menus

**Files:** `src/MonitorProfiles.App/App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `Services/TrayIconService.cs`, `ViewModels/MainViewModel.cs`.

- [ ] Load preferences before creating the main window; default to English and System theme when no valid preference exists.
- [ ] Add a compact language dropdown whose entries are named in their own language and a System/Light/Dark appearance dropdown to the main header.
- [ ] Save the selected setting separately from profiles and refresh all open WPF views immediately.
- [ ] Rebuild tray menu labels and profile actions when the selected language changes; preserve the active-preview Revert item.
- [ ] Show a localized non-blocking warning on corrupt preferences while continuing with defaults and leaving the file/profile data intact.
- [ ] Add a unit test that changing language invokes the tray refresh notification.
- [ ] Run the app and confirm that changing language and theme works without restarting and remains selected after relaunch.

## Task 5: Redesign the main profile and current-display views

**Files:** `src/MonitorProfiles.App/MainWindow.xaml`, `.xaml.cs`, `ViewModels/MainViewModel.cs`, `DisplayRowViewModel.cs`, `ProfileRowViewModel.cs`, theme dictionaries and strings resources.

- [ ] Replace all visible literals and hard-coded color hex values in the main window/setup XAML with localization keys and semantic theme resources.
- [ ] Implement the approved header, restrained status indicator, three direct-apply profile actions with concise display/mode summaries, and four compact monitor-state tiles using simple WPF vector outlines.
- [ ] Keep Apply as a one-action button for each saved profile; make edit/delete quieter secondary actions without removing them.
- [ ] Keep setup functional: identify active screens, assign aliases, show unavailable/unknown screens clearly, and disable Save until four unique aliases are assigned.
- [ ] Make the layout adapt from profile/display columns to a vertical stack as the window narrows; do not hide Apply, state, language or theme controls.
- [ ] Verify current state uses native display descriptors; do not add/rewrite monitor topology or profile logic.
- [ ] Build and manually inspect the main/setup screen in both themes and all target window sizes before proceeding.

## Task 6: Redesign editor, preview, error and tray surfaces

**Files:** `ProfileEditorWindow.xaml/.xaml.cs`, `PreviewConfirmationWindow.xaml/.xaml.cs`, `MainWindow.xaml.cs`, `TrayIconService.cs`, `ProfileEditorDisplayRow.cs`, translations and theme dictionaries.

- [ ] Replace all visible English/Spanish literals and hard-coded colors with localized keys and semantic theme resources in editor and preview.
- [ ] Localize orientation labels, mode-source labels, form headings, validation errors, keep/revert text, countdown, restore warnings, delete confirmation, status strings, and the identify overlay.
- [ ] Preserve fixed names of built-in profiles and all custom profile names; keep resolution/frequency values and device model names unmodified.
- [ ] Ensure locale/theme changes refresh existing WPF surfaces and tray menus without resetting unsaved editor values or the preview timer.
- [ ] Keep preview countdown, topmost prompt, explicit Revert and tray Revert behavior unchanged.
- [ ] Test profile editor and preview labels for longest translations, especially German, Japanese, and Simplified Chinese, with keyboard-only focus traversal.
- [ ] Run all app/storage/core tests and confirm direct Apply, Preview, rollback, profile CRUD and tray flows remain available.

## Task 7: Final quality and release verification

**Files:** as needed from prior tasks; update `README.md` only if user-facing operation or supported locale/theme information changed.

- [ ] Run `dotnet test MonitorProfiles.sln` and `dotnet build MonitorProfiles.sln -c Release`.
- [ ] Publish with `dotnet publish src/MonitorProfiles.App/MonitorProfiles.App.csproj -c Release -r win-x64 --self-contained true`.
- [ ] Launch the published app without applying a display profile; inspect English/Spanish/Japanese/Simplified Chinese in System/Light/Dark and at default/minimum window widths.
- [ ] Check every visible text has a resource key, all resource-key tests pass, focus is visible, translations wrap, status color is not the only state cue, and user preferences persist after restart.
- [ ] Confirm the current user's saved profiles and screen mode data are unchanged by theme/language tests.
- [ ] Record any manual-only visual check in the final handoff; do not report the interface polished based on compilation alone.

## Self-review

- Spec coverage: visual direction, both themes, seven locales, language/theme menus, persistent preferences, preserved profile names, all existing screens/states, tray refresh, accessibility/focus, and manual verification each map to an implementation task.
- Placeholder scan: each task has concrete files, behavior and verification commands; no unspecified package, translation, or theme values remain.
- Type consistency: localization keys resolve through `LocalizationService`; WPF uses `TranslateExtension`; theme services expose semantic dictionaries; profile and display services remain unchanged.
