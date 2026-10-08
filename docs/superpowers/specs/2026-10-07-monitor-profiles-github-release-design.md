# Monitor Profiles GitHub Publication — Design Specification

## Purpose

Publish Monitor Profiles as a public GitHub repository with editable source code and one straightforward official Windows download. Keep exactly one official release named `latest`; each approved publication replaces its single ZIP asset rather than accumulating numbered, beta, or preview releases.

## Confirmed decisions

- **GitHub account:** `DavidDSSR`.
- **Public repository:** `DavidDSSR/MonitorProfiles`.
- **Default branch:** `main`; rename the current local `master` branch before the initial push.
- **License:** MIT, so others may use, modify, and redistribute their own creations from the published source while retaining the license notice.
- **Official distribution:** one rolling GitHub Release with tag `latest` and stable asset name `MonitorProfiles-win-x64.zip`.
- **Release cadence:** manually publish after validation; pushes and pull requests do not create or replace official releases.
- **Audience:** people can download and run the app; developers can clone or fork the public source and modify their own copy.

## Repository experience

- The root README begins with a direct **Download latest for Windows x64** link to:
  `https://github.com/DavidDSSR/MonitorProfiles/releases/latest/download/MonitorProfiles-win-x64.zip`.
- Provide concise English and Spanish instructions for downloading, extracting, first-run display identification, profile switching, and building from source.
- State that the ZIP is self-contained for Windows 11 x64 and does not require a separate .NET installation.
- Include the MIT `LICENSE`.
- Keep build output, user profiles, local settings, and machine-specific artifacts out of Git.

## Build and release flow

Add a GitHub Actions workflow named **Publish official latest**, started manually from the `main` branch:

1. Check out the selected `main` commit and install the .NET 10 SDK.
2. Run `dotnet test MonitorProfiles.sln`.
3. Publish `src/MonitorProfiles.App/MonitorProfiles.App.csproj` for `win-x64` with `--self-contained true`.
4. Package the complete publish directory as `MonitorProfiles-win-x64.zip`.
5. Only after tests, publish, and ZIP creation succeed, create the initial release/tag `latest` or update that same release/tag and overwrite the same ZIP asset.
6. Grant the workflow only the `contents: write` permission needed to update the single tag/release.

Do not create semantic-version releases, prereleases, or extra platform assets. A failed test, build, or package step must leave the previously published release untouched. The `latest` release page and the README's direct ZIP link are the only official distribution entry points.

## Safety and source integrity

- Create the repository explicitly with public visibility under the confirmed owner/name.
- Push the reviewed local source history to `main` and make `main` the default branch.
- Before the first push, inspect tracked files/history for credentials, personal settings, generated binaries, and local monitor-profile data. Publish only reviewed source, documentation, tests, license, and workflow files.
- The release workflow packages build output only; it never packages `%LocalAppData%` or user display profiles.
- The GitHub release asset is a ZIP of the self-contained publish directory, not an installer or a temporary Actions artifact.

## Verification and acceptance

- Verify a clean local working tree and check `.gitignore` excludes build output and user data before pushing.
- After creation, verify `gh repo view DavidDSSR/MonitorProfiles --json visibility` reports `PUBLIC`, `main` is the default branch, and the remote points to that repository.
- After publishing, verify there is exactly one GitHub Release, its tag is `latest`, and it contains `MonitorProfiles-win-x64.zip`.
- Download the README's direct URL and verify the ZIP contains `MonitorProfiles.App.exe` and its self-contained runtime files.
- Launch the downloaded executable without applying any display profile and verify it opens to the main window.
- Verify a subsequent manual publication replaces the same `latest` asset instead of creating a second release.
