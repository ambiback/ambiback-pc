# Entwicklung

[English](../en/development.md) · **Deutsch** · [Benutzerhandbuch](../benutzerhandbuch.md)

Für alle, die AmbiBack PC aus dem Quellcode bauen oder daran arbeiten.

## Bauen

Voraussetzungen: Windows, Visual Studio mit .NET-Desktopentwicklung und das .NET Framework 4.7.2 Targeting Pack.

```powershell
msbuild AmbiBack.PC.sln -t:Restore -p:RestorePackagesConfig=true
msbuild AmbiBack.PC.csproj -p:Configuration=Release
```

Die Anwendung liegt danach in `bin\Release\`.

### DreamScreen

Die Unterstützung für DreamScreen-Geräte wird nur mit dem Symbol `USE_DREAMSCREEN` kompiliert. Release-Builds definieren es nicht, das veröffentlichte Setup und die ZIP-Datei haben also keine DreamScreen-Unterstützung. Um mit DreamScreen zu bauen, ergänze `USE_DREAMSCREEN` in `DefineConstants` in `AmbiBack.PC.csproj`. DreamScreen-Geräte werden dann über UDP-Port 8888 gefunden.

## Setup und portable ZIP

`installer\build-installer.ps1` baut das Release (Warnungen gelten als Fehler), führt `AmbiBack.exe --hue-selftest` aus und erstellt das Setup ([Inno Setup 6](https://jrsoftware.org/isinfo.php), Skript `installer\AmbiBack.iss`) sowie eine portable ZIP-Datei:

```powershell
.\installer\build-installer.ps1 -Version 1.2.0 -BuildNumber 1
```

Ohne `-Version` wird die Version aus `Properties\AssemblyInfo.cs` verwendet. Die Ausgabe landet in `D:\.build\ambiback-pc\release\dist` (änderbar mit `-OutRoot`). Die Version wird nur für den Build in `AssemblyInfo.cs` geschrieben.

Das Setup installiert ohne Administratorrechte für den aktuellen Benutzer (`%LOCALAPPDATA%\Programs\AmbiBack`) oder wahlweise für alle Benutzer. Es installiert das .NET Framework (4.8.1, auf älterem Windows 4.8) und die WebView2 Runtime, wenn sie fehlen; der WebView2-Bootstrapper liegt in `installer\redist\`.

## Selbsttests

- `AmbiBack.exe --hue-selftest [bericht.txt]` prüft Verschlüsselung und Protokolle des Hue-Codes ohne Oberfläche.
- `AmbiBack.exe --hue-discover` sucht Hue Bridges im Netzwerk.

## Continuous Integration

`.github/workflows/ci.yml` läuft bei jedem Pull Request auf `main`, bei Pushes auf `main` und von Hand. Es führt `build-installer.ps1` ohne Signierung aus (Build ohne Warnungen, Hue-Selbsttest, Setup und ZIP) und hebt Setup, ZIP und Testbericht 14 Tage als Workflow-Artefakt auf.

## Releases

1. Tag pushen: `git tag v1.2.0` und `git push origin v1.2.0`. Ein Bindestrich macht daraus ein Vorab-Release (`v1.2.0-beta1`).
2. Der Workflow `.github/workflows/release.yml` baut mit Version `1.2.0.<Laufnummer>`, führt den Selbsttest aus und veröffentlicht das Release mit `AmbiBack-PC-<Version>-setup.exe` und `AmbiBack-PC-<Version>.zip`. Selbsttest-Bericht und Dateien bleiben außerdem als Workflow-Artefakt erhalten.
3. Wird ein Tag erneut gepusht (z. B. nach `git tag -f v1.2.0` und `git push -f origin v1.2.0`), ersetzt der Workflow Text und Dateien des bestehenden Releases; übrig gebliebene Entwürfe für den Tag werden gelöscht.

Der Release-Text (Deutsch und Englisch) enthält Downloads, Voraussetzungen und ein Änderungsprotokoll. Optionale Repository-Secrets:

| Secret | Zweck |
| --- | --- |
| `COPILOT_GITHUB_TOKEN` | Fine-grained PAT mit der Berechtigung „Copilot Requests“. GitHub Copilot CLI schreibt dann das Änderungsprotokoll aus den Commits seit dem vorigen Tag; ohne ihn werden die von GitHub erzeugten Notizen verwendet. |
| `SIGN_PFX_BASE64`, `SIGN_PFX_PASSWORD` | Code-Signing-Zertifikat (PFX als Base64) und sein Passwort. AmbiBack.exe, Setup und Deinstallationsprogramm werden dann signiert; ohne sie bleiben die Dateien unsigniert. |

## Dokumentation

Die Benutzerdokumentation liegt in `docs/en/` und `docs/de/` mit gleichen Dateinamen; halte beide Sprachen auf demselben Stand. Screenshots liegen in `docs/images/` mit der Endung `_en` oder `_de`.
