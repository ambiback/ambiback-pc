# AmbiBack PC

Windows-Client für AmbiBack. Er erfasst den Bildschirminhalt und sendet die Farben an AmbiBack-Geräte, DreamScreen und Philips-Hue-Lampen.

Die AmbiBack-Geräte werden seit rund fünf Jahren nicht mehr verkauft. Dieses Repository stellt den Quellcode für den Eigenbau und die Weiterentwicklung bereit. Mehr zum Projekt steht auf [ambiback.de](https://www.ambiback.de/). Die passende Firmware liegt in [ambiback-firmware](https://github.com/ambiback/ambiback-firmware).

## Funktionen

- Bildschirmaufnahme über DirectX mit Vorschau und Sektoren je Gerät
- Geräte im lokalen Netzwerk finden und verwalten
- Ansteuerung von AmbiBack, DreamScreen und Philips Hue
- Einstellungen für Helligkeit, Kontrast, Farbanteile und Übergänge
- Start mit Windows und Betrieb im Infobereich

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

## Sicherheitshinweis

Die Kommunikation mit Geräten und der Hue Bridge läuft unverschlüsselt im lokalen Netz, und die Geräte-API hat keine Anmeldung. Betreibe die Geräte nur in einem vertrauenswürdigen Netz.

## Marken

Philips Hue und DreamScreen sind Marken ihrer jeweiligen Inhaber. Dieses Projekt steht in keiner Verbindung zu ihnen.

## Lizenz

Copyright (C) 2019-2026 André Lorbach

Dieses Programm ist freie Software und steht unter der [GNU General Public License Version 3](LICENSE). Es wird ohne jede Gewährleistung bereitgestellt. Drittcode behält seine eigene Lizenz, siehe [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
