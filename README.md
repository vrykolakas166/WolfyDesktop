<h1 align="center">
  <br>
  <img src="https://raw.githubusercontent.com/vrykolakas166/WolfyDesktop/master/logo_tran_1.png" alt="wolfy" width="200">
  <br>
  WolfyDesktop
  <br>
</h1>

<h4 align="center">A minimal screen desktop app built on top of <a href="https://learn.microsoft.com/en-us/windows/apps/winui/winui3/" target="_blank">WinUI 3</a>.</h4>

## Install

Download `WolfyInc.WolfyDesktop-win-Setup.exe` from the [latest release](https://github.com/vrykolakas166/WolfyDesktop/releases/latest) and run it. It installs for the current user (no administrator prompt) and needs nothing else installed.

The app keeps itself up to date: it checks for a new version shortly after starting, downloads it in the background and offers to restart. Press **F10** to check manually.

> Coming from version 1.1.2 or earlier? Install the new version once. On first launch it copies your music library over and removes the old installation.

## How To Use

| Key | Action |
| --- | --- |
| **F2** | Change theme |
| **F4** | Close the application |
| **F6** | Open the audio manager (download Lofi Rain, add or remove your own music) |
| **F10** | Version and updates |
| **F11** | Toggle full screen |
| **F12** | Toggle chill mode (background video and music) |
| **Space** | Play / pause music |
| **Up / Down** | Volume |

Your settings and music live in `%LocalAppData%\WolfyDesktop`, so they survive updates and reinstalls.

## Development

Requirements: .NET 10 SDK and Visual Studio 2026 with the *Windows application development* and *Desktop development with C++* workloads (the C++ tools are needed for Native AOT).

```powershell
dotnet build WolfyDesktop.sln -p:Platform=x64   # build
dotnet test WolfyDesktop.Tests                  # unit tests
```

| Project | Contents |
| --- | --- |
| `WolfyDesktop` | WinUI 3 app: views, view models, media playback |
| `WolfyDesktop.Core` | UI-free logic: settings, music library, downloads, updates |
| `WolfyDesktop.Tests` | Unit tests for `WolfyDesktop.Core` |

## Releasing

The version number lives only in git tags ([MinVer](https://github.com/adamralph/minver)), so there is no file to edit. Pick one:

| How | Command |
| --- | --- |
| Next patch, automatically | `./scripts/New-Release.ps1` (v1.2.0 → v1.2.1) |
| Next minor / major | `./scripts/New-Release.ps1 -Bump minor` or `-Bump major` |
| Exact version | `./scripts/New-Release.ps1 -Version 2.0.0` |
| Plain git | `git tag v1.2.1 && git push origin v1.2.1` |
| From the browser | GitHub → Actions → **Release** → *Run workflow* (choose bump or version) |

`New-Release.ps1` checks that you are on an up-to-date, clean `master`, shows the version and asks before tagging (`-WhatIf` previews, `-Yes` skips the prompt). A version with a suffix such as `1.3.0-beta.1` is published as a pre-release and not offered to installed copies.

The *Release* workflow then builds the app as Native AOT, packages it with [Velopack](https://velopack.io) (including a delta from the previous release) and publishes a GitHub release. Installed copies update from there.

To build the installer locally without publishing, run `./scripts/Publish-Release.ps1`. The output goes to `artifacts/releases`. Development builds show a version like `1.2.1-preview.0.3` (three commits after v1.2.0).
