# AmbiBack PC

[English](README.md) · **Deutsch**

Windows-Client für AmbiBack. Er erfasst den Bildschirminhalt und sendet die Farben an AmbiBack-Geräte, DreamScreen und Philips-Hue-Lampen.

**Benutzerhandbuch:** [docs/benutzerhandbuch.md](docs/benutzerhandbuch.md) (mit Screenshots).

Er gehört zum [AmbiBack-Projekt](https://github.com/ambiback) und arbeitet mit der [AmbiBack-Firmware](https://github.com/ambiback/ambiback-firmware).

## Status

- Version 0.9.0.180. Die PC-Version blieb ein Prototyp und wurde nie zum fertigen Produkt.
- Der Release-Build läuft durch, Formulare und Ressourcen laden.
- Philips Hue (Entertainment-Streaming, Bridge-Suche, eigener Entertainment-Bereich) wurde mit einer echten Hue Bridge v2 getestet. Andere Teile der Anwendung sind seit der Migration weniger getestet.
- AmbiBack-Geräte werden seit rund fünf Jahren nicht mehr verkauft. Dieses Repository dient dem Eigenbau und der Weiterentwicklung, ohne Produktsupport. Mehr auf [ambiback.de](https://www.ambiback.de/).

## Funktionen

- Bildschirmaufnahme über DirectX mit Vorschau und Sektoren je Gerät
- Geräte im lokalen Netzwerk finden und verwalten
- Ansteuerung von AmbiBack-Controllern, Philips Hue und DreamScreen (DreamScreen nur in Builds mit `USE_DREAMSCREEN`)
- Philips Hue über die Entertainment API: verschlüsseltes DTLS-Streaming mit 25 bis 50 fps, automatische Bridge-Suche, eigener Entertainment-Bereich aus den gewählten Lampen
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

Die Kommunikation mit AmbiBack-Geräten läuft unverschlüsselt im lokalen Netz, und die Geräte-API hat keine Anmeldung. Betreibe die Geräte nur in einem vertrauenswürdigen Netz. Das Hue-Streaming ist verschlüsselt (DTLS, AES-GCM), Verwaltungsbefehle an die Bridge laufen über HTTPS, und der Bridge-Schlüssel wird mit Windows-DPAPI gespeichert.

## Marken

Philips Hue und DreamScreen sind Marken ihrer jeweiligen Inhaber. Dieses Projekt steht in keiner Verbindung zu ihnen.

## Lizenz

Copyright (C) 2019-2026 Andre Lorbach

Dieses Programm ist freie Software und steht unter der [GNU General Public License Version 3](LICENSE). Es wird ohne jede Gewährleistung bereitgestellt. Drittcode behält seine eigene Lizenz, siehe [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
