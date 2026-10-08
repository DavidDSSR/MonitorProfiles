# Monitor Profile Controller — Design Specification

## Purpose

Build a focused Windows 11 desktop app that switches connected displays between named profiles. Applying a profile configures which displays are active, their resolution, refresh rate, orientation, and primary-display status. Users can identify and name their physical displays, create/edit/delete profiles, and apply profiles from either the main window or the Windows notification area.

## Goals

- Apply the selected display configuration with one action.
- Address displays by persistent device identity rather than relying on Windows' display numbering.
- Ship with the three requested profiles and allow user-defined profiles.
- Provide a time-limited confirmation and automatic rollback when testing a new or edited profile.
- Keep the interface focused on display status and profile switching.
- Target Windows 11 only.

## Non-goals

- Support for macOS or Linux.
- Cloud synchronization or account-based profile storage.
- Automated game detection or switching profiles based on foreground applications.
- Changing physical monitor controls such as brightness or input source.
- Replacing Windows' full display-arrangement editor.

## Technology and architecture

- C# and .NET 10 LTS.
- WPF for the desktop interface.
- Windows Display Configuration APIs behind an isolated display-service interface. The implementation will query active and available display paths/modes and apply topology and mode changes through Windows APIs; native interop must not leak into the UI or profile model.
- Local JSON storage under the current user's LocalAppData directory.
- Windows notification-area icon for quick profile actions.

### Components

1. **Core domain** — display identity, user-assigned display name, display mode, orientation, profile, validation, and primary-display fallback rules. No WPF or native Windows dependencies.
2. **Windows display service** — enumerate connected displays and supported modes; identify a display visually; capture the current configuration; apply a requested profile; restore a captured configuration.
3. **Profile storage** — load/save profiles and display-name mappings in a versioned JSON document. Handle missing, malformed, or older data without silently discarding recoverable user data.
4. **WPF application** — setup/identification flow, profile list, profile editor, preview confirmation countdown, status/error presentation, and notification-area menu.

## Display identity and initial setup

- Enumerate displays using Windows device paths/identifiers, not ordinal labels alone.
- On first run, guide the user to identify connected displays with an on-screen marker and assign names such as “Monitor 1”, “Monitor 2”, “Monitor 3”, and “Monitor 4”.
- Store the mapping between stable device identity and user-assigned name. If a display cannot be matched later, mark that profile assignment unavailable and ask the user to identify/remap it; do not silently substitute another physical display.
- The default profiles are seeded once the display setup has enough mappings to configure them. The user can edit the assignments if their current numbering differs.

## Default profiles

| Profile | Active displays | Display modes | Primary display |
|---|---|---|---|
| Trabajo | Monitor 2 and Monitor 3 | Monitor 2: 2560×1440 at 144 Hz, landscape. Monitor 3: 1080×1920 at 60 Hz, portrait flipped (“Vertical (volteado)”). | Monitor 2 when active; otherwise an active display. |
| Competitivo | Monitor 2 only | Monitor 2: 1920×1080 at 144 Hz, landscape. | Monitor 2. |
| Historia | Monitor 4 only | Monitor 4: 2560×1440 at 144 Hz, landscape. | Monitor 4. |

All other mapped displays are inactive in each profile. Windows may not expose the full mode catalog for a connected display that is currently inactive. The editor shows modes reported by Windows where available and allows explicit width, height, refresh-rate, and orientation values for displays without a complete catalog. Before applying a profile, the app must use `SetDisplayConfig` validation to confirm the complete topology and requested modes without changing the current display configuration. If Windows rejects a requested mode, the app reports that before application. Profiles store exact mode values rather than assuming every connected monitor supports them.

## Profile behavior

- Users can create, edit, duplicate, and delete custom profiles.
- A profile contains a display assignment for each configured display: active/inactive, resolution, refresh rate, orientation, and primary status.
- Profile names must be non-empty and unique without regard to letter casing.
- Validate that at least one display is active and exactly one active display is primary.
- The chosen primary display must be active. If a profile's preferred primary is absent or inactive at apply time, use an active display deterministically and show which display became primary.
- Save changes locally and reload them on the next launch.

## Applying, previewing, and rolling back a profile

1. Read and retain the current Windows display configuration.
2. Re-enumerate displays and verify assignments and requested modes against currently available devices/modes.
3. Apply active paths, requested modes/orientations, and primary-display selection through the Windows display service.
4. Applying an already-saved profile is a one-action operation from the window or notification area. Creating or editing a profile offers a **Preview** action: it applies the draft with a short confirmation countdown; confirmation retains the draft and timeout or cancellation restores the captured configuration.
5. Report failures clearly. If applying fails partway through, attempt to restore the captured configuration and display both the original failure and whether restoration succeeded.

The preview rollback flow must remain accessible while a profile is being tested and must not depend on the user finding the main window after a display topology change. The implementation should use a topmost confirmation prompt on the active desktop and expose a tray action for reverting while the countdown is running. Saved-profile switching remains one click as requested.

## User interface

- Main window is compact and organized around profile selection/application.
- Show current detected displays, their user-assigned names, connection/active state, and current mode.
- Profile cards or rows show a concise summary of active displays and modes, with one prominent apply action.
- Profile create/edit controls expose display activation, resolution, refresh rate, orientation, and primary selection. Show modes reported by Windows when available; for inactive displays with no complete catalog, allow explicit mode entry and validate it with Windows before any display change. Provide a Preview action for testing unsaved changes.
- Display identification is initiated from the display list and uses a temporary numbered/name marker.
- The notification-area menu provides Open, the available profiles, and Exit. Applying a profile from the menu follows the same confirmation/rollback rules.
- Do not add dashboards, telemetry, accounts, or unrelated settings.

## Error handling and persistence

- Missing display: identify the affected profile entry and let the user remap it.
- Unsupported mode: use `SetDisplayConfig` validation before applying; if rejected, explain which display/mode is unavailable and leave the current configuration unchanged.
- Native API failure: attempt rollback and communicate the result.
- Corrupt profile file: preserve the original file, show a recovery message, and offer default profiles without overwriting the damaged file automatically.
- Versioned profile data supports future schema migration.
- Application settings and profiles are per-user; no administrator privileges should be required for ordinary display configuration.

## Verification

- Unit-test profile validation, unique-name handling, primary-display fallback, JSON round-trip, and schema/error behavior.
- Test profile application orchestration against a fake display-service implementation, including one-action application of saved profiles, mode rejection, partial apply failure, preview confirmation, timeout rollback, and explicit cancellation.
- Manually verify on Windows 11 with the actual four-display setup: device identification, enable/disable transitions, 90/270-degree portrait-flipped mode, 1080×1920 at 60 Hz, 2560×1440 at 144 Hz, primary-display changes, unsupported modes, monitor disconnection, tray switching, and rollback.
- Build and package a Windows x64 release and verify a clean-user first-run setup and subsequent profile persistence.

## Acceptance criteria

1. The app runs on Windows 11 and detects the connected physical displays.
2. The user can identify/name the displays and the assignments remain associated with device identity across restarts.
3. The three default profiles match the table above and can be edited.
4. Applying a profile from the window or notification area switches active displays, modes, orientation, and primary status as specified.
5. Unsupported or missing display modes are reported through non-mutating Windows validation before applying.
6. Applying a saved profile takes one action; previewing an uncommitted profile that is not confirmed restores the prior display configuration.
7. Users can create, edit, duplicate, and delete profiles, and those profiles persist across restarts.
8. The UI remains focused on display identification, profile management, and applying profiles.
