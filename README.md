# AmbiBack PC

**English** · [Deutsch](README.de.md)

Windows client for AmbiBack. It captures the screen content and sends the colors to AmbiBack devices, DreamScreen and Philips Hue lights.

**User guide:** [docs/user-guide.md](docs/user-guide.md) (with screenshots).

It is part of the [AmbiBack project](https://github.com/ambiback) and works with the [AmbiBack firmware](https://github.com/ambiback/ambiback-firmware).

## Status

- Version 0.9.0.180. The PC version remained a prototype and was never a finished product.
- The Release build passes, and forms and resources load.
- Philips Hue (Entertainment streaming, bridge discovery, own entertainment area) was tested against a real Hue Bridge v2. Other parts of the application are tested less after the migration.
- AmbiBack devices have not been on sale for around five years. This repository is for DIY use and further development, with no product support. More at [ambiback.de](https://www.ambiback.de/).

## Features

- Screen capture through DirectX with preview and per-device sectors
- Find and manage devices on the local network
- Control of AmbiBack controllers, Philips Hue and DreamScreen (DreamScreen only in builds with `USE_DREAMSCREEN`)
- Philips Hue through the Entertainment API: encrypted DTLS streaming at 25 to 50 fps, automatic bridge discovery, an entertainment area of its own built from the lights you choose
- Settings for brightness, contrast, color balance and transitions
- Start with Windows and run from the notification area

## Hardware

The parts for a DIY build are on the [AmbiBack parts page](https://www.ambiback.de/en/diy/parts/).

## Building

Requires Windows, Visual Studio with .NET desktop development and the .NET Framework 4.7.2 Targeting Pack.

```powershell
msbuild AmbiBack.PC.sln -t:Restore -p:RestorePackagesConfig=true
msbuild AmbiBack.PC.csproj -p:Configuration=Release
```

The application is then in `bin\Release\`.

### Setup and portable ZIP

`installer\build-installer.ps1` builds the release (warnings are errors), runs `AmbiBack.exe --hue-selftest` and creates the setup ([Inno Setup 6](https://jrsoftware.org/isinfo.php), script `installer\AmbiBack.iss`) and a portable ZIP:

```powershell
.\installer\build-installer.ps1 -Version 1.2.0 -BuildNumber 1
```

Without `-Version` the version from `Properties\AssemblyInfo.cs` is used. Output goes to `D:\.build\ambiback-pc\release\dist` (change with `-OutRoot`). The version is written into `AssemblyInfo.cs` for the build only.

The setup installs for the current user without administrator rights (`%LOCALAPPDATA%\Programs\AmbiBack`) or, if chosen, for all users. It installs the .NET Framework (4.8.1, or 4.8 on older Windows) and the WebView2 Runtime when they are missing; the WebView2 bootstrapper is in `installer\redist\`.

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request to `main`, on pushes to `main` and by hand. It runs `build-installer.ps1` without signing (warning-free build, Hue self test, setup and ZIP) and keeps setup, ZIP and test report as a workflow artifact for 14 days.

## Releases

1. Push a tag: `git tag v1.2.0` and `git push origin v1.2.0`. A hyphen makes a prerelease (`v1.2.0-beta1`).
2. The workflow `.github/workflows/release.yml` builds with version `1.2.0.<run number>`, runs the self test and creates a **draft** release with `AmbiBack-PC-<version>-setup.exe` and `AmbiBack-PC-<version>.zip`. The self test report and the files are also kept as a workflow artifact.
3. Check the draft on GitHub and publish it.

The release text (German and English) contains downloads, requirements and a changelog. Optional repository secrets:

| Secret | Purpose |
| --- | --- |
| `COPILOT_GITHUB_TOKEN` | Fine-grained PAT with the "Copilot Requests" permission. GitHub Copilot CLI then writes the changelog from the commits since the previous tag; without it GitHub's generated notes are used. |
| `SIGN_PFX_BASE64`, `SIGN_PFX_PASSWORD` | Code signing certificate (PFX as Base64) and its password. AmbiBack.exe, setup and uninstaller are then signed; without them the files are unsigned. |

## USB drivers

Depending on the module, USB access needs a driver from the chip manufacturer. It is not included here:

- [FTDI VCP Drivers](https://ftdichip.com/drivers/vcp-drivers/)
- [Silicon Labs CP210x Drivers](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)

## Security

Communication with AmbiBack devices is unencrypted on the local network, and the device API has no login. Run devices only on a trusted network. Hue streaming is encrypted (DTLS, AES-GCM), management commands to the bridge use HTTPS, and the bridge key is stored with Windows DPAPI.

## Trademarks

Philips Hue and DreamScreen are trademarks of their respective owners. This project is not affiliated with them.

## License

Copyright (C) 2019-2026 Andre Lorbach

This program is free software under the [GNU General Public License version 3](LICENSE). It comes with no warranty. Third-party code keeps its own license, see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
