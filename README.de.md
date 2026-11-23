# AmbiBack PC

[English](README.md) · **Deutsch**

Windows-Client für AmbiBack. Er erfasst den Bildschirminhalt und sendet die Farben an AmbiBack-Geräte, DreamScreen und Philips-Hue-Lampen.

Er gehört zum [AmbiBack-Projekt](https://github.com/ambiback) und arbeitet mit der [AmbiBack-Firmware](https://github.com/ambiback/ambiback-firmware).

## Status

- Version 0.9.0.180. Die PC-Version blieb ein Prototyp und wurde nie zum fertigen Produkt.
- Der Release-Build läuft durch, Formulare und Ressourcen laden.
- **Die gesamte Anwendung wurde seit der Migration nicht gestartet.** Der neue Hue-Client wurde nur gegen eine simulierte Bridge getestet, nicht gegen eine echte.
- AmbiBack-Geräte werden seit rund fünf Jahren nicht mehr verkauft. Dieses Repository dient dem Eigenbau und der Weiterentwicklung, ohne Produktsupport. Mehr auf [ambiback.de](https://www.ambiback.de/).

## Funktionen

- Bildschirmaufnahme über DirectX mit Vorschau und Sektoren je Gerät
- Geräte im lokalen Netzwerk finden und verwalten
- Ansteuerung von AmbiBack, DreamScreen und Philips Hue
- Einstellungen für Helligkeit, Kontrast, Farbanteile und Übergänge
- Start mit Windows und Betrieb im Infobereich

## Hardware

Die Bauteile für den Selbstbau stehen auf der [AmbiBack-Teileseite](https://www.ambiback.de/selbstbau/teile/).

## Bauen

Voraussetzungen sind Windows, Visual Studio mit .NET-Desktop-Entwicklung und das .NET Framework 4.7.2 Targeting Pack.

```powershell
msbuild AmbiBack.PC.sln -t:Restore -p:RestorePackagesConfig=true
msbuild AmbiBack.PC.csproj -p:Configuration=Release
```

Die fertige Anwendung liegt danach in `bin\Release\`.

## USB-Treiber

Für den Zugriff auf Geräte per USB wird je nach Modul ein Treiber des Chip-Herstellers benötigt. Er ist hier nicht enthalten:

- [FTDI VCP Drivers](https://ftdichip.com/drivers/vcp-drivers/)
- [Silicon Labs CP210x Drivers](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)

## Sicherheit

Die Kommunikation mit Geräten und der Hue Bridge läuft unverschlüsselt im lokalen Netz, und die Geräte-API hat keine Anmeldung. Betreibe die Geräte nur in einem vertrauenswürdigen Netz.

## Marken

Philips Hue und DreamScreen sind Marken ihrer jeweiligen Inhaber. Dieses Projekt steht in keiner Verbindung zu ihnen.

## Lizenz

Copyright (C) 2019-2026 Andre Lorbach

Dieses Programm ist freie Software und steht unter der [GNU General Public License Version 3](LICENSE). Es wird ohne jede Gewährleistung bereitgestellt. Drittcode behält seine eigene Lizenz, siehe [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
