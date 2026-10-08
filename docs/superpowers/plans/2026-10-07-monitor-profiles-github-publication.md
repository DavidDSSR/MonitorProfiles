# Monitor Profiles GitHub Publication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish the Windows app and editable source at `https://github.com/DavidDSSR/MonitorProfiles`, with MIT licensing and exactly one rolling `latest` ZIP release.

**Architecture:** Rename the local primary branch to `main`, create the public GitHub repository and push the existing reviewed source history. A manually triggered GitHub Actions workflow tests and packages a self-contained Windows x64 build, then creates or updates the single `latest` release/tag and overwrites the same ZIP asset.

**Tech Stack:** Git/GitHub CLI (`gh`), GitHub Actions, .NET 10, Windows runner, PowerShell `Compress-Archive`.

**Spec:** `docs/superpowers/specs/2026-10-07-monitor-profiles-github-release-design.md`

## Global Constraints

- Repository: `DavidDSSR/MonitorProfiles` with **public** visibility.
- Default branch: `main`.
- License: MIT.
- The only official release/tag is `latest`.
- The only release asset is `MonitorProfiles-win-x64.zip`.
- Publish is manually triggered from `main`, after tests and self-contained publishing succeed.
- Publish the app from `src/MonitorProfiles.App/MonitorProfiles.App.csproj` with `--self-contained true` and `-r win-x64`.
- The README's download URL is `https://github.com/DavidDSSR/MonitorProfiles/releases/latest/download/MonitorProfiles-win-x64.zip`.
- Never stage binaries, local preferences/profile JSON, or credentials.
- Do not force-push or rewrite the `main` branch; only the workflow may move the dedicated `latest` tag to the newly published commit.

---

## Planned File Structure

- `LICENSE` — standard MIT license text naming DavidDSSR and the current year.
- `README.md` — English-first public landing/readme with language links, direct latest ZIP link, portable-run steps, build/test instructions and contribution guidance.
- `README.es.md` — Spanish equivalent with the same direct download link and truthful app requirements.
- `.github/workflows/publish-latest.yml` — manual, test-gated Windows x64 build/package and single-release update.
- Existing `.gitignore` — verify build output and user settings remain excluded; change only if a required ignore rule is missing.

## Task 1: Add MIT license and public-facing README

**Files:** `LICENSE`, `README.md`, `README.es.md`.

- [ ] Write the standard MIT license, copyright holder `DavidDSSR`, year `2026`.
- [ ] Add the direct download link `https://github.com/DavidDSSR/MonitorProfiles/releases/latest/download/MonitorProfiles-win-x64.zip` near the top of both README languages.
- [ ] Explain in both languages: Windows 11 x64; download and extract the ZIP; run `MonitorProfiles.App.exe`; first-run Monitor 1–4 identification; where profiles/preferences are stored; language/theme selection; profile test/revert.
- [ ] Document source development commands `dotnet test MonitorProfiles.sln` and `dotnet run --project src/MonitorProfiles.App/MonitorProfiles.App.csproj`.
- [ ] State that code is MIT-licensed and users can clone/fork it for their own modifications. Do not claim signing, installer, antivirus verification, or automatic updates.
- [ ] Check formatting/links manually and run `git diff --check`.

## Task 2: Add a one-release rolling publisher workflow

**Files:** `.github/workflows/publish-latest.yml`.

- [ ] Create a workflow named `Publish official latest`, triggered only by `workflow_dispatch`, with job permission `contents: write`.
- [ ] Run the job on `windows-latest` and fail unless `github.ref` is exactly `refs/heads/main`.
- [ ] Check out the selected commit and install .NET SDK `10.0.x` using `actions/checkout@v4` and `actions/setup-dotnet@v4`.
- [ ] Run `dotnet test MonitorProfiles.sln -c Release` before publishing.
- [ ] Publish using `dotnet publish src/MonitorProfiles.App/MonitorProfiles.App.csproj -c Release -r win-x64 --self-contained true -o <runner-temp-publish-dir>`.
- [ ] Create `<runner-temp>/MonitorProfiles-win-x64.zip` from every file in the publish directory using PowerShell `Compress-Archive`.
- [ ] Set `GH_TOKEN: ${{ github.token }}` for the release step only. If release tag `latest` exists, upload the ZIP with `gh release upload latest ... --clobber`, move only `refs/tags/latest` to `${{ github.sha }}` with the GitHub refs API (`force=true`), and edit the existing release title/notes. If no release exists, create the `latest` tag/release at the current commit with the ZIP asset.
- [ ] Check each `gh`/API step's exit code and fail the workflow on error. Tests, publish, and ZIP packaging must complete before mutating the official release.
- [ ] Keep release notes factual: identify it as the single latest Windows x64 build, point to the public `main` source and README instructions, and do not add a version archive or beta release.

## Task 3: Prepare and publish the public `main` repository

**Files:** Git branch/remote configuration; GitHub repository metadata; all reviewed tracked repository files.

- [ ] Run `git status --short`, `git diff --check`, `git log --oneline -10`, inspect `git ls-files`, and search tracked files for credential-like values before public upload.
- [ ] Verify `.gitignore` excludes `**/bin/`, `**/obj/`, `.vs/`, `artifacts/`, `.worktrees/`, and keep `%LocalAppData%` data outside the repository.
- [ ] Rename the current local branch `master` to `main` with `git branch -m master main`; confirm `git status --short` contains only intended committed project files.
- [ ] Confirm `gh auth status` shows account `DavidDSSR`; confirm `gh repo view DavidDSSR/MonitorProfiles` does not resolve to an existing repository.
- [ ] Create and push with `gh repo create DavidDSSR/MonitorProfiles --public --source . --remote origin --push`.
- [ ] Verify `gh repo view DavidDSSR/MonitorProfiles --json visibility,defaultBranchRef,url` reports `PUBLIC`, `main`, and the expected URL.

## Task 4: Run the official publish workflow and verify public downloads

**Files:** GitHub Actions run and GitHub Release `latest`; downloaded ZIP in a temporary verification directory only.

- [ ] Start the first release using `gh workflow run publish-latest.yml --ref main --repo DavidDSSR/MonitorProfiles`.
- [ ] Find the run id with `gh run list --repo DavidDSSR/MonitorProfiles --workflow publish-latest.yml --limit 1` and wait using `gh run watch <run-id> --repo DavidDSSR/MonitorProfiles`.
- [ ] If the workflow fails, inspect its logs with `gh run view <run-id> --log --repo DavidDSSR/MonitorProfiles`, fix the workflow, push the fix to `main`, and rerun it; do not report a release until the workflow succeeds.
- [ ] Verify `gh release list --repo DavidDSSR/MonitorProfiles` shows exactly one official release, tag `latest`.
- [ ] Verify the release asset list contains exactly `MonitorProfiles-win-x64.zip` and that the release is marked Latest.
- [ ] Download through the README URL; inspect the archive listing and verify it contains `MonitorProfiles.App.exe` and the published self-contained runtime files.
- [ ] Launch the downloaded executable, verify the window title is `Monitor Profiles`, then close only the test process. Do not apply a monitor profile during release verification.
- [ ] Verify the local main worktree and final `main` commit history are clean and the published public URL is directly usable without signing in.

## Self-review

- Spec coverage: public owner/name/branch, MIT license, editable source, bilingual direct download instructions, self-contained ZIP, one rolling release, manually controlled cadence, main-only publish, permissions, source safety, release verification, and download smoke test all map to explicit tasks.
- Placeholder scan: commands, repository name, branch, tag, asset, owner, workflow events, and outputs are concrete; runner temporary output is a runner-local path, not a deferred implementation decision.
- Safety review: the workflow builds/tests before release mutation, changes only the dedicated `latest` tag, never force-pushes `main`, and excludes local settings and binaries.
