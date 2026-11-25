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

### Setup und portables ZIP

`installer\build-installer.ps1` baut das Release (Warnungen gelten als Fehler), führt `AmbiBack.exe --hue-selftest` aus und erstellt das Setup ([Inno Setup 6](https://jrsoftware.org/isinfo.php), Skript `installer\AmbiBack.iss`) und ein portables ZIP:

```powershell
.\installer\build-installer.ps1 -Version 1.2.0 -BuildNumber 1
```

Ohne `-Version` wird die Version aus `Properties\AssemblyInfo.cs` verwendet. Die Ausgabe landet in `D:\.build\ambiback-pc\release\dist` (änderbar mit `-OutRoot`). Die Version wird nur für den Build in `AssemblyInfo.cs` eingetragen.

Das Setup installiert ohne Administratorrechte für den aktuellen Benutzer (`%LOCALAPPDATA%\Programs\AmbiBack`) oder wahlweise für alle Benutzer. Fehlen das .NET Framework (4.8.1, auf älterem Windows 4.8) oder die WebView2 Runtime, installiert es sie nach; der WebView2-Bootstrapper liegt in `installer\redist\`.

## Continuous Integration

`.github/workflows/ci.yml` läuft bei jedem Pull Request gegen `main`, bei Pushes auf `main` und auf Knopfdruck. Es führt `build-installer.ps1` ohne Signierung aus (warnungsfreier Build, Hue-Selbsttest, Setup und ZIP) und hebt Setup, ZIP und Testbericht 14 Tage als Workflow-Artefakt auf.

## Releases

1. Tag pushen: `git tag v1.2.0` und `git push origin v1.2.0`. Mit Bindestrich wird es ein Prerelease (`v1.2.0-beta1`).
2. Der Workflow `.github/workflows/release.yml` baut mit Version `1.2.0.<Run-Nummer>`, führt den Selbsttest aus und legt ein **Draft**-Release mit `AmbiBack-PC-<Version>-setup.exe` und `AmbiBack-PC-<Version>.zip` an. Selbsttest-Bericht und Dateien bleiben zusätzlich als Workflow-Artefakt erhalten.
3. Den Entwurf auf GitHub prüfen und veröffentlichen.

Der Release-Text (Deutsch und Englisch) enthält Downloads, Voraussetzungen und einen Changelog. Optionale Repository-Secrets:

| Secret | Zweck |
| --- | --- |
| `COPILOT_GITHUB_TOKEN` | Fine-grained PAT mit der Berechtigung „Copilot Requests“. Dann schreibt GitHub Copilot CLI den Changelog aus den Commits seit dem letzten Tag; ohne Token werden die von GitHub erzeugten Notes verwendet. |
| `SIGN_PFX_BASE64`, `SIGN_PFX_PASSWORD` | Code-Signing-Zertifikat (PFX als Base64) und Passwort. Dann werden AmbiBack.exe, Setup und Uninstaller signiert; ohne sie bleiben die Dateien unsigniert. |

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
