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
