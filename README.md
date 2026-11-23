# AmbiBack PC

**English** · [Deutsch](README.de.md)

Windows client for AmbiBack. It captures the screen content and sends the colors to AmbiBack devices, DreamScreen and Philips Hue lights.

It is part of the [AmbiBack project](https://github.com/ambiback) and works with the [AmbiBack firmware](https://github.com/ambiback/ambiback-firmware).

## Status

- Version 0.9.0.180. The PC version remained a prototype and was never a finished product.
- The Release build passes, and forms and resources load.
- **The full application has not been started since the migration.** The new Hue client has only been tested against a simulated bridge, not a real one.
- AmbiBack devices have not been on sale for around five years. This repository is for DIY use and further development, with no product support. More at [ambiback.de](https://www.ambiback.de/).

## Features

- Screen capture through DirectX with preview and per-device sectors
- Find and manage devices on the local network
- Control of AmbiBack, DreamScreen and Philips Hue
- Settings for brightness, contrast, color balance and transitions
- Start with Windows and run from the notification area

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

Communication with devices and the Hue Bridge is unencrypted on the local network, and the device API has no login. Run devices only on a trusted network.

## Trademarks

Philips Hue and DreamScreen are trademarks of their respective owners. This project is not affiliated with them.

## License

Copyright (C) 2019-2026 Andre Lorbach

This program is free software under the [GNU General Public License version 3](LICENSE). It comes with no warranty. Third-party code keeps its own license, see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
