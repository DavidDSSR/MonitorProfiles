# Monitor Profile Controller Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows 11 desktop application that identifies displays and applies saved or custom display profiles with one action.

**Architecture:** Keep domain rules and profile application orchestration independent from WPF and native interop. Implement display enumeration/configuration behind a Windows-only service, persist a versioned JSON profile store under LocalAppData, and present it through a compact WPF UI with a notification-area menu.

**Tech Stack:** C# / .NET 10, WPF, Windows Display Configuration APIs, `System.Text.Json`, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-07-monitor-profile-controller-design.md`

## Global Constraints

- Target Windows 11 x64 only.
- Use .NET 10 and WPF; do not add web UI or cloud dependencies.
- Native display API structs and calls remain inside `MonitorProfiles.Windows`.
- Profiles are applied by stable display-device identity, never by current array order.
- When Windows does not expose a full mode catalog for an inactive display, allow explicit mode entry but require `SetDisplayConfig` validation before any configuration change.
- Applying a saved profile is one action; only preview of an uncommitted edit has a confirmation countdown.
- Never silently replace an unavailable configured physical display with another display.
- On apply failure or preview timeout/cancel, attempt restoration of the previously captured display configuration.
- Do not overwrite malformed profile data automatically.

---

## Planned File Structure

- `MonitorProfiles.sln` — solution containing app, core, platform, storage, and test projects.
- `src/MonitorProfiles.Core/` — domain types, validation, default-profile factory, display-service contracts, and profile application orchestration.
- `src/MonitorProfiles.Storage/` — versioned JSON document, LocalAppData path resolution, and profile repository.
- `src/MonitorProfiles.Windows/` — native DisplayConfig interop, display discovery, supported-mode enumeration, capture/apply/restore, and identify overlay coordination.
- `src/MonitorProfiles.App/` — WPF shell, view models, profile editor, preview/rollback UI, notification-area icon, and dependency composition.
- `tests/MonitorProfiles.Core.Tests/` — domain, persistence-independent, and orchestration tests.
- `tests/MonitorProfiles.Storage.Tests/` — JSON migration/corruption/round-trip tests.
- `tests/MonitorProfiles.Windows.Tests/` — safe interop layout and pure conversion tests; hardware behavior remains a Windows manual test.
- `README.md` — setup, run, publish, and hardware verification instructions.

## Task 1: Scaffold the solution and verify Windows desktop build

**Files:** create solution and project files listed above, `Directory.Build.props`, and initial `README.md`.

- [ ] Create `MonitorProfiles.sln` and projects targeting `net10.0` for Core/Storage/tests and `net10.0-windows` for Windows/App/platform tests.
- [ ] Enable nullable reference types, implicit usings, and deterministic builds centrally.
- [ ] Configure WPF and WinForms for the App only (WinForms is used solely for a native notification-area icon).
- [ ] Add project references so App depends on Core, Storage, and Windows; Windows and Storage depend on Core; Core has no platform dependency.
- [ ] Add xUnit test projects and references without bringing WPF/native dependencies into Core tests.
- [ ] Run `dotnet build MonitorProfiles.sln` and `dotnet test MonitorProfiles.sln`; verify clean scaffold succeeds.

## Task 2: Implement and test the core profile model

**Files:** `src/MonitorProfiles.Core/Models/DisplayIdentity.cs`, `DisplayDescriptor.cs`, `DisplayMode.cs`, `DisplayOrientation.cs`, `ProfileDisplayConfiguration.cs`, `DisplayProfile.cs`, `ProfileValidationResult.cs`; `src/MonitorProfiles.Core/Profiles/DisplayProfileValidator.cs`, `DefaultProfileFactory.cs`; matching tests in `tests/MonitorProfiles.Core.Tests/`.

- [ ] Write tests for nonempty/case-insensitively unique profile names, at least one active display, exactly one active primary display, and supported per-display configuration values.
- [ ] Write tests for defaults: Trabajo uses Monitor 2 at 2560×1440@144 landscape and Monitor 3 at 1080×1920@60 portrait-flipped; Competitivo uses only Monitor 2 at 1920×1080@144; Historia uses only Monitor 4 at 2560×1440@144; inactive displays are excluded.
- [ ] Model display identity as an opaque stable device path and keep the user-facing alias separate from identity.
- [ ] Model orientation explicitly as Landscape, Portrait, LandscapeFlipped, PortraitFlipped; distinguish logical width/height for portrait modes from native source-mode dimensions where necessary.
- [ ] Implement deterministic primary fallback: preferred primary if currently mapped and active; otherwise first active profile entry in stable identity order, returning a warning/result describing fallback.
- [ ] Run the Core test project and solution build.

## Task 3: Add versioned local profile storage

**Files:** `src/MonitorProfiles.Storage/ProfileDocument.cs`, `ProfileRepository.cs`, `ProfileJsonContext.cs` (if source generation is selected), `LocalProfilePathProvider.cs`; tests under `tests/MonitorProfiles.Storage.Tests/`.

- [ ] Test JSON round-trip for profiles, stable device identity mappings, and schema version.
- [ ] Test missing file behavior, unknown future schema behavior, malformed JSON preservation, and case-insensitive duplicate names.
- [ ] Implement atomic save using a temporary sibling file followed by replacement, under `%LocalAppData%\MonitorProfileController\profiles.json`.
- [ ] Implement a repository interface in Core or Storage with asynchronous load/save operations and typed recovery/errors.
- [ ] Ensure corrupt data is copied/preserved and never silently replaced by defaults.
- [ ] Run storage tests and solution build.

## Task 4: Prove and implement Windows display discovery and mode enumeration

**Files:** `src/MonitorProfiles.Core/Services/IDisplayService.cs`; `src/MonitorProfiles.Windows/Interop/DisplayConfigNative.cs`, `NativeDisplayConfig.cs`, `WindowsDisplayService.cs`, `DisplayModeConverter.cs`; tests under `tests/MonitorProfiles.Windows.Tests/`.

- [ ] Define service contracts for `GetDisplaysAsync`, `CaptureConfigurationAsync`, `ApplyProfileAsync`, and `RestoreConfigurationAsync`, plus data types for current configuration and supported modes.
- [ ] Add layout/conversion tests for native structures, refresh-rate rational conversion, rotation mapping, and display target identity extraction.
- [ ] Implement safe `QueryDisplayConfig` enumeration for connected targets, mapping monitor device paths and friendly names to stable identities; do not use enumeration position as identity.
- [ ] Enumerate supported width/height/refresh/orientation combinations for each target, including disconnected-from-desktop but physically connected targets where Windows reports them.
- [ ] Validate interop struct sizes/fields against Windows SDK definitions and add comments linking non-obvious flags to their SDK constants.
- [ ] Run build/tests and execute a diagnostic mode in the app that lists discovered displays/modes without changing the system. Confirm the four physical targets can be distinguished on the target PC before implementing apply behavior.

## Task 5: Implement profile apply, preview, and rollback orchestration

**Files:** `src/MonitorProfiles.Core/Services/ProfileApplicationService.cs`, `ProfileApplicationResult.cs`, `ProfilePreviewSession.cs`; tests under `tests/MonitorProfiles.Core.Tests/`.

- [ ] Write fake-display-service tests for one-action saved-profile apply, missing target/mode rejection before mutation, successful apply, partial apply failure followed by restore, and restore failure reporting.
- [ ] Write preview tests for confirmation retaining the draft, timeout restoring the captured configuration, cancellation restoring it, and preventing overlapping preview sessions.
- [ ] Implement orchestration: capture current topology/modes, re-enumerate and validate against current devices, resolve primary fallback, call platform service, and return actionable warnings/errors.
- [ ] Implement a cancellable preview session with an injected clock/timer abstraction so timeout behavior is deterministic in tests.
- [ ] Ensure applying a saved profile does not force an additional confirmation click.
- [ ] Run Core tests and solution build.

## Task 6: Implement native apply, capture, restore, and identify behavior

**Files:** `src/MonitorProfiles.Windows/WindowsDisplayService.cs`, `DisplayConfigurationSnapshot.cs`, `DisplayIdentifyOverlay.cs`, and additional interop files as needed.

- [ ] Capture enough native path/mode state to restore the current configuration exactly after a failed apply or preview timeout.
- [ ] Build a requested topology using stable target IDs; keep inactive targets out of the active path set and map resolution, refresh rate, rotation, and primary source to valid Windows mode structures.
- [ ] Validate requested configurations with Windows before committing where supported; return native error codes and translated display/mode context.
- [ ] Apply requested topology/modes through `SetDisplayConfig` (or the best documented DisplayConfig API sequence established by the discovery spike) and verify the resulting state by querying again.
- [ ] Implement restoration using the captured configuration, including previous primary display and active paths.
- [ ] Implement a temporary always-visible identification marker associated with the selected physical target; close it automatically and allow dismissal.
- [ ] Manually test one reversible configuration change on the target PC before enabling all profile operations. Verify timeout restoration and no-administrator behavior.

## Task 7: Build WPF identification, status, and profile management UI

**Files:** `src/MonitorProfiles.App/App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `ViewModels/MainViewModel.cs`, `ViewModels/DisplayViewModel.cs`, `ViewModels/ProfileViewModel.cs`, `Views/ProfileEditorWindow.xaml`, `Views/ProfileEditorWindow.xaml.cs`, `Views/PreviewConfirmationWindow.xaml`, `Views/PreviewConfirmationWindow.xaml.cs`.

- [ ] Create a compact dark-neutral layout with detected displays and current modes, profile rows/cards, concise summaries, and a single prominent apply action per saved profile.
- [ ] Add first-run setup to identify each detected physical display and assign Monitor 1–4 names; persist mappings and seed default profiles once those names are assigned.
- [ ] Display disconnected/unmapped profile assignments explicitly; do not bind them to another display automatically.
- [ ] Add create, edit, duplicate, and delete flows. Editor controls include activation, resolution, refresh rate, orientation, and primary selection; use Windows-enumerated modes where available and explicit values for inactive screens without a full catalog.
- [ ] Validate drafts before preview/save and show precise unsupported or missing display explanations.
- [ ] Add Preview with topmost countdown, Keep Changes and Revert controls, plus countdown timeout restore.
- [ ] Build and run the WPF app; verify all views at standard Windows scaling and keyboard navigation for core actions.

## Task 8: Add notification-area access and application lifecycle

**Files:** `src/MonitorProfiles.App/Services/TrayIconService.cs`, `App.xaml.cs`, `ViewModels/MainViewModel.cs`.

- [ ] Create a notification-area icon with Open, one menu item per saved profile, and Exit.
- [ ] Route tray profile actions through the same single-action saved-profile service as the main window.
- [ ] Expose Revert while a profile preview is active; ensure closing the main window does not terminate the active preview timer.
- [ ] Ensure exit during an active preview restores the captured configuration before process shutdown.
- [ ] Verify tray menu behavior, repeated clicks, app shutdown, and display topology changes manually.

## Task 9: Package, document, and run end-to-end verification

**Files:** `README.md`, publish configuration (project file or `src/MonitorProfiles.App/Properties/PublishProfiles/win-x64.pubxml`), and any installer definition only if packaging tests show one is needed.

- [ ] Document prerequisites, first-run display identification, profile semantics, preview/revert, and troubleshooting for unavailable modes or disconnected monitors.
- [ ] Run `dotnet test MonitorProfiles.sln` and `dotnet publish src/MonitorProfiles.App/MonitorProfiles.App.csproj -c Release -r win-x64 --self-contained true`.
- [ ] Run the published app under a clean user profile and verify first-run mapping and JSON persistence across restart.
- [ ] Execute the spec's four-monitor manual matrix: three default profiles, orientation and frequencies, main-display changes, missing/unsupported mode reporting, disconnected monitor handling, tray switching, preview rollback, and failure recovery.
- [ ] Record any hardware-specific limitation in README and fix all functional regressions before declaring completion.

## Self-review

- Spec coverage: profile defaults, stable display identity, mapping, profile CRUD, JSON persistence/recovery, one-action apply, unsupported-mode validation, primary fallback, preview rollback, notification area, Windows 11 packaging, and hardware verification each have explicit tasks.
- Placeholder scan: no TODO/TBD instructions; hardware-dependent checks are identified as manual verification with concrete scenarios.
- Type consistency: Core defines display/profile/service contracts; Windows implements the display service; the app consumes the orchestration and storage services. Native interop stays out of Core and WPF view models.
