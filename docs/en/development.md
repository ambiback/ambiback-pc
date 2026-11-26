# Development

**English** · [Deutsch](../de/development.md) · [User guide](../user-guide.md)

For people who build AmbiBack PC from source or work on it.

## Building

Requires Windows, Visual Studio with .NET desktop development and the .NET Framework 4.7.2 Targeting Pack.

```powershell
msbuild AmbiBack.PC.sln -t:Restore -p:RestorePackagesConfig=true
msbuild AmbiBack.PC.csproj -p:Configuration=Release
```

The application is then in `bin\Release\`.

### DreamScreen

Support for DreamScreen devices is compiled only with the symbol `USE_DREAMSCREEN`. Release builds do not define it, so the published setup and ZIP have no DreamScreen support. To build with it, add `USE_DREAMSCREEN` to `DefineConstants` in `AmbiBack.PC.csproj`. DreamScreen devices are then found on UDP port 8888.

## Setup and portable ZIP

`installer\build-installer.ps1` builds the release (warnings are errors), runs `AmbiBack.exe --hue-selftest` and creates the setup ([Inno Setup 6](https://jrsoftware.org/isinfo.php), script `installer\AmbiBack.iss`) and a portable ZIP:

```powershell
.\installer\build-installer.ps1 -Version 1.2.0 -BuildNumber 1
```

Without `-Version` the version from `Properties\AssemblyInfo.cs` is used. Output goes to `D:\.build\ambiback-pc\release\dist` (change with `-OutRoot`). The version is written into `AssemblyInfo.cs` for the build only.

The setup installs for the current user without administrator rights (`%LOCALAPPDATA%\Programs\AmbiBack`) or, if chosen, for all users. It installs the .NET Framework (4.8.1, or 4.8 on older Windows) and the WebView2 Runtime when they are missing; the WebView2 bootstrapper is in `installer\redist\`.

## Self tests

- `AmbiBack.exe --hue-selftest [report.txt]` checks the encryption and protocols of the Hue code without the UI.
- `AmbiBack.exe --hue-discover` finds Hue bridges on the network.
- `AmbiBack.exe --wled-test host [seconds]` streams a moving test picture over DDP to a WLED or other DDP receiver. The self test also covers the DDP packet format and the WLED device.
- `AmbiBack.exe --govee-test host [seconds] [single]` streams a moving test picture over the Govee LAN API (segments, or one color with `single`). The self test also covers the razer and LAN command formats and the Govee device.

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request to `main`, on pushes to `main` and by hand. It runs `build-installer.ps1` without signing (warning-free build, Hue self test, setup and ZIP) and keeps setup, ZIP and test report as a workflow artifact for 14 days.

## Releases

1. Push a tag: `git tag v1.2.0` and `git push origin v1.2.0`. A hyphen makes a prerelease (`v1.2.0-beta1`).
2. The workflow `.github/workflows/release.yml` builds with version `1.2.0.<run number>`, runs the self test and publishes the release with `AmbiBack-PC-<version>-setup.exe` and `AmbiBack-PC-<version>.zip`. The self test report and the files are also kept as a workflow artifact.
3. When a tag is pushed again (e.g. after `git tag -f v1.2.0` and `git push -f origin v1.2.0`), the workflow replaces text and files of the existing release; leftover drafts for the tag are deleted.

The release text (German and English) contains downloads, requirements and a changelog. Optional repository secrets:

| Secret | Purpose |
| --- | --- |
| `COPILOT_GITHUB_TOKEN` | Fine-grained PAT with the "Copilot Requests" permission. GitHub Copilot CLI then writes the changelog from the commits since the previous tag; without it GitHub's generated notes are used. |
| `SIGN_PFX_BASE64`, `SIGN_PFX_PASSWORD` | Code signing certificate (PFX as Base64) and its password. AmbiBack.exe, setup and uninstaller are then signed; without them the files are unsigned. |

## Documentation

User docs live in `docs/en/` and `docs/de/` with the same file names; keep both languages in step. Screenshots are in `docs/images/` with the suffix `_en` or `_de`.
