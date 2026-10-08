# Monitor Profiles Interface Redesign — Design Specification

## Purpose

Redesign the existing Windows 11 WPF interface so it feels polished, elegant, and distinctive while remaining minimal and task-first. The interface must support light and dark appearance and offer English, Spanish, French, Italian, Japanese, German, and Simplified Chinese. Existing display-profile behavior remains intact.

## Product context

- **Platform:** Windows 11 desktop, WPF / .NET 10.
- **Users:** both everyday multi-monitor users and advanced users who tune resolution, refresh rate, and rotation.
- **Core job:** switch among saved multi-monitor configurations with one action and understand which displays are active.
- **Operating context:** work and gaming setups across connected displays, including monitors that are currently off.
- **Existing capabilities to preserve:** first-run display identification and naming, three included profiles, profile creation/editing/deletion, one-action apply, tray-menu access, native mode validation, test timeout, and rollback.
- **Terminology constraint:** keep the built-in profile names “Trabajo”, “Competitivo”, and “Historia” unchanged in every UI language. User-created profile names are never translated.
- **Visual constraints:** minimal, elegant, restrained; no neon colors, decorative gradients, or unnecessary dashboard content.

## Design direction

### Quiet precision

Use a disciplined grid and compact editorial hierarchy inspired by Swiss minimalism, expressed through native Windows controls rather than a web-dashboard imitation. The surfaces should feel carefully crafted and calm; display outlines and monitor states provide the product-specific visual signature.

- Use Segoe UI Variable / Windows system font fallback for native rendering and broad glyph coverage.
- Establish a compact type scale, consistent 4/8 spacing rhythm, restrained separators, and subtle elevation only where it aids hierarchy.
- Use a muted brass/ochre accent for primary actions and selected state; reserve semantic colors for success, warning, error, and connection state.
- Light appearance uses a warm off-white canvas, white/soft stone surfaces, graphite text, and visible warm-gray borders.
- Dark appearance uses warm charcoal surfaces, soft-white text, visible graphite separators, and the same restrained accent.
- Avoid neon, gradients, glass blur, nested cards, emoji icons, and ornamental motion.

### Main window information hierarchy

1. **Top bar:** compact app mark and “Monitor Profiles” title; current connection/active-display status; language menu and appearance menu aligned to the right.
2. **Profiles:** three clear profile actions with profile name, active-screen summary, resolution/refresh/orientation summary, and one prominent Apply button per profile. Profile apply remains one action. Edit and delete remain available with less visual emphasis.
3. **Current displays:** four compact monitor tiles showing assigned alias, detected model, current on/off state, and current resolution/refresh/orientation. Draw a simple monitor outline per tile; indicate a portrait screen with its actual orientation. This view describes state, not editable physical placement.
4. **Setup state:** first-run identification uses the same visual tokens and shows model/current mode, alias selection, and Identify. Inactive displays remain identifiable by device name and are never silently substituted.
5. **Profile editor and preview:** preserve all profile controls and the 15-second keep/revert safety flow. Use the same theme, language, field hierarchy, and feedback styles as the main window.

At narrower window widths, stack profile and display sections vertically without hiding Apply, mode, state, or language/theme controls. Keep normal WPF keyboard navigation, visible focus, and standard button/selection semantics.

## Theme behavior

- Provide **System**, **Light**, and **Dark** choices in a compact appearance menu. System is the initial default and tracks the Windows appearance; an explicit Light or Dark choice is saved.
- Centralize semantic brushes and styles in shared WPF ResourceDictionaries; use a light and dark palette dictionary. Theme changes take effect immediately across open windows, dialogs, and menu labels/state, without restarting the app.
- Use dynamic resource lookup only for runtime-switchable theme tokens; retain native Fluent/WPF control affordances.
- Keep text and meaningful non-text state indicators distinguishable in both themes; never rely on color alone for on/off, selected, error, or warning state.

## Localization behavior

- Initial language: English.
- Available languages: English, Spanish, French, Italian, Japanese, German, and Simplified Chinese (`zh-Hans`).
- The language button opens a short list with each language named natively (for example, Español, Français, Italiano, 日本語, Deutsch, 简体中文).
- Change the visible interface immediately: main window, setup, profile editor, preview countdown, app-owned errors/statuses, and tray menu.
- Persist selected language locally. If the stored code is absent or unsupported, fall back to English without preventing startup.
- Built-in profile names and user-created profile names remain exactly as saved. Resolutions, frequencies, monitor aliases, and physical model names are not translated.
- Translated strings must fit natural WPF text wrapping and supported window sizes; labels must not clip or overlap in Japanese or Chinese.
- Preserve a localized explanation alongside a raw Win32 error code when native display validation fails, so the code remains searchable.

## Preferences and persistence

- Store theme and language preferences separately from monitor profiles, under the existing per-user LocalAppData application directory.
- Default values for a missing preferences file are `System` theme and English language.
- Preference writes are atomic. A malformed or unsupported preferences file must not overwrite valid profile data; recover with defaults and show a localized non-blocking notice.
- Changing appearance or language must not rewrite profiles, display identity mappings, or mode values.

## Implementation architecture

1. **Theme service:** owns the current System/Light/Dark preference, resolves System appearance, swaps semantic theme dictionaries, and raises an update notification.
2. **Localization service:** loads the seven language dictionaries, provides localized text to WPF bindings and code-behind, updates all open views, and refreshes the tray menu.
3. **Preferences store:** versioned per-user JSON for theme/language only, with safe defaults and atomic persistence.
4. **WPF views/view models:** use resource keys instead of hard-coded visible strings and hex colors; preserve existing profile/display/application services.

Do not replace WPF, add a web view, or introduce a component library solely for appearance. Use the existing application structure and native display workflow.

## Error and edge behavior

- Localization fallback is English for an unknown or missing locale code.
- Theme fallback is System for an unknown or missing theme value.
- If preferences cannot be read/written, continue with defaults and show a localized notice; do not block profile application.
- Rebuilding the tray menu on a locale change must preserve profile entries and the active-preview Revert action.
- Preview and rollback operations preserve the user's saved theme/language preference; they modify display configuration only.

## Verification

- Unit-test preference round-trip, default values, unknown locale/theme fallback, and atomic save behavior.
- Verify every required resource key exists in all seven language dictionaries and that all language dictionaries contain non-empty translations for visible strings.
- Test WPF resource-dictionary switching in both directions; test change immediately in an open editor/preview and in tray-menu regeneration.
- Run `dotnet test MonitorProfiles.sln`, Release build, and self-contained `win-x64` publish.
- Manually inspect English, Spanish, Japanese, and Simplified Chinese at the default and minimum supported window widths in both Light and Dark themes. Check text wrapping, focus visibility, contrast, profile apply availability, setup, profile editor, preview rollback, tray language, and mode/orientation labels.
- Do not apply a display profile as part of theme/localization verification.

## Acceptance criteria

1. The app has a coherent, elegant, minimal WPF interface and uses the approved light/dark visual system.
2. System, Light, and Dark modes can be selected; System follows Windows appearance, and a manual choice persists across restarts.
3. English is the default and the user can change instantly among English, Spanish, French, Italian, Japanese, German, and Simplified Chinese.
4. Main view, setup, profile editor, preview, app-owned errors/status, and tray menu honor the active language.
5. Included profile names remain “Trabajo”, “Competitivo”, and “Historia” in all locales; custom profile names remain unchanged.
6. All current monitor detection, profile CRUD, one-action apply, native pre-validation, preview countdown, and rollback behavior remains available.
7. Long translations and theme changes do not clip controls or obscure keyboard focus at supported widths.
