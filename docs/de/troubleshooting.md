# Problemlösung und FAQ

[English](../en/troubleshooting.md) · **Deutsch** · [Benutzerhandbuch](../benutzerhandbuch.md)

## Installation und Start

| Problem | Lösung |
| --- | --- |
| „Der Computer wurde durch Windows geschützt“ (SmartScreen) | Die Dateien sind nicht signiert. Klicke auf **Weitere Informationen** und dann auf **Trotzdem ausführen**. Lade nur von der [Release-Seite](https://github.com/ambiback/ambiback-pc/releases). |
| AmbiBack startet nach dem Setup nicht | Vermutlich fehlt das .NET Framework, oder Windows muss nach dessen Installation noch neu starten. Starte Windows neu; sonst installiere das [.NET Framework 4.8.1](https://dotnet.microsoft.com/download/dotnet-framework). |
| Geräte-Webseiten oder *Details* bleiben leer | Die WebView2 Runtime fehlt. Führe das Setup erneut aus oder installiere die [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). |
| Ein zweiter Start von AmbiBack tut nichts | AmbiBack läuft nur einmal. Es ist wahrscheinlich im Infobereich versteckt: Klicke auf den Pfeil `^` in der Taskleiste und doppelklicke auf das AmbiBack-Symbol. |

## Windows-Firewall

AmbiBack schickt eine Suchanfrage ins Netzwerk und empfängt die Antworten der AmbiBack-Controller auf **UDP-Port 1337**. Die Windows-Firewall muss diese Antworten durchlassen. Das Senden der Farben und alles rund um Philips Hue sind ausgehende Verbindungen und davon nicht betroffen.

**Beim ersten Start** fragt Windows, ob AmbiBack kommunizieren darf. Hake **Private Netzwerke** an und klicke auf **Zulassen**.

**Hast du es blockiert oder kam keine Abfrage:**

1. Prüfe, ob dein Heimnetz als *Privat* eingestellt ist: Windows *Einstellungen › Netzwerk und Internet › WLAN* (oder *Ethernet*) › dein Netzwerk › *Netzwerkprofiltyp: Privates Netzwerk*. In einem *öffentlichen* Netz blockiert Windows die Antworten.
2. Öffne *Windows-Sicherheit › Firewall- & Netzwerkschutz › Zugriff von App durch Firewall zulassen*.
3. Klicke auf *Einstellungen ändern*, suche **AmbiBack** und hake **Privat** an. Fehlt der Eintrag, wähle *Andere App zulassen…* und dann `AmbiBack.exe`.

**Oder mit PowerShell** (als Administrator ausführen; den Pfad bei einer Installation für alle Benutzer oder der ZIP-Version anpassen):

```powershell
New-NetFirewallRule -DisplayName "AmbiBack PC" -Direction Inbound -Action Allow -Protocol UDP -LocalPort 1337 -Profile Private -Program "$env:LOCALAPPDATA\Programs\AmbiBack\AmbiBack.exe"
```

Bei einer Installation für alle Benutzer ist das Programm `C:\Program Files\AmbiBack\AmbiBack.exe`.

Andere Firewalls oder Sicherheitspakete (z. B. von einem Virenscanner) brauchen dieselbe Ausnahme.

## AmbiBack-Geräte

| Problem | Lösung |
| --- | --- |
| Der Controller wird nicht gefunden | Ist er eingeschaltet und im selben Netz (gleicher Router, kein Gäste-WLAN)? Prüfe die [Firewall](#windows-firewall). Sonst **Hinzufügen** mit IP-Adresse. |
| Es wird nie etwas gefunden, obwohl die Firewall es erlaubt | Ein anderes Programm belegt eventuell schon UDP-Port 1337. Schließe andere Umgebungslicht- oder AmbiBack-Programme und starte AmbiBack neu. |
| Der Controller am USB wird nicht gefunden | Installiere den Treiber seines USB-Chips ([FTDI](https://ftdichip.com/drivers/vcp-drivers/), [CP210x](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)); probiere ein anderes Kabel (manche Kabel laden nur). |
| „WLAN-Daten nicht gesendet“ | Der Controller muss per USB verbunden sein (blauer Punkt). Prüfe das Kabel und versuche es erneut. |
| Der Controller kommt nicht ins WLAN | Prüfe SSID und Passwort (Groß-/Kleinschreibung). Der Controller braucht ein 2,4-GHz-Netz. |
| Farben sind auf der falschen Seite | Ändere Startecke oder Richtung des Streifens auf der Monitor-Karte ([LED-Anordnung](ambiback-devices.md#led-anordnung-einstellen)). |
| Das Licht hängt nach oder ruckelt | Verwende *UDP*, stelle den Router näher oder senke *Bilder pro Sekunde*. Erhöhe *Glättung* für weichere Übergänge. |
| Offline-Einträge lassen sich nicht entfernen | **Offline entfernen** verwenden oder das Gerät auswählen und **Löschen**. |

## Philips Hue

| Problem | Lösung |
| --- | --- |
| Die Hue Bridge wird nicht gefunden | Prüfe, ob PC und Bridge im selben Netz sind, und klicke **Suchen**. Hilft das nicht, trage die IP-Adresse von Hand ein (in der Hue-App unter *Einstellungen › Bridges › i*). |
| „Verbinden“ meldet einen Fehler | Den Link-Knopf an der Bridge drücken und **innerhalb von 30 Sekunden** auf *Verbinden* klicken. |
| „Ohne Streaming-Schlüssel“ | Die Verbindung stammt von einer älteren Version. Link-Knopf drücken und erneut *Verbinden*. |
| Die Hue-Lampen bleiben dunkel | Sind Lampen in der Gruppe angehakt (oder über *Lampen auswählen* gewählt)? Ist die Gruppe dem Monitor zugeordnet, und ist der Monitor gestartet? |
| Die Lampen flackern oder springen | Nur einen Weg pro Lampe nutzen: PC-Gruppe **oder** ein Bereich aus der Hue-App, nicht beides; keine andere Sync-App (Hue Sync, Hue Sync Box, TV-Sync) gleichzeitig. |
| „Die Bridge hat schon 20 Entertainment-Bereiche“ | Nicht benötigte Bereiche im Hue-Dialog mit **Löschen…** oder in der Hue-App entfernen. |
| Eine Lampe fehlt in der Auswahl | Nur Farblampen mit Entertainment-Unterstützung werden angezeigt; Steckdosen und Weißlampen nicht. |
| Es lassen sich nur 10 Lampen anhaken | Die Bridge streamt höchstens 10 Lampen pro Entertainment-Bereich und immer nur einen Bereich gleichzeitig. Wähle die 10 Lampen, die dem Bildschirm am nächsten sind. |
| Die Lampen bleiben an, nachdem AmbiBack beendet ist | AmbiBack schaltet sie bei *Stoppen* und *Beenden* aus. Wurde Windows hart ausgeschaltet, schalte sie in der Hue-App aus. |

## FAQ

**Funktioniert AmbiBack mit Spielen und Videos?**
Ja, es erfasst den ganzen Bildschirm über DirectX. Kopiergeschützte Inhalte (manche Streaming-Apps im Browser) können für die Bildschirmerfassung schwarz erscheinen; die Lichtquellen bleiben dann für dieses Fenster dunkel.

**Kann ich mehrere Monitore verwenden?**
Ja. Jeder Monitor hat eine eigene Karte; schalte **Verwenden** ein und ordne ihm die Geräte zu.

**Kann ich AmbiBack-Controller und Hue gleichzeitig nutzen?**
Ja. Ordne jedes Gerät dem passenden Monitor zu und klicke auf **Alle starten**.

**Braucht es Internet?**
Nein. Nur die Suche nach der Hue Bridge fragt eventuell den Hue-Suchdienst, wenn im lokalen Netz nichts gefunden wird, und das Setup lädt das .NET Framework herunter, falls es fehlt.

**Kann ich noch AmbiBack-Geräte kaufen?**
Nein. AmbiBack-Geräte werden seit rund fünf Jahren nicht mehr verkauft. Das Projekt richtet sich an den Selbstbau; die Bauteile stehen auf der [AmbiBack-Teileseite](https://www.ambiback.de/selbstbau/teile/).

## Debug-Protokoll

Schalte unter [Optionen](settings.md#optionen) das *Debug-Protokoll* ein und stelle das Problem nach. Das Protokoll hilft, wenn du ein Problem auf [GitHub](https://github.com/ambiback/ambiback-pc/issues) meldest. Hue-Schlüssel landen nie darin.

## Datenschutz und Sicherheit

- Die Verbindung zu AmbiBack-Controllern läuft unverschlüsselt im lokalen Netz; die Geräte-API hat keine Anmeldung. Betreibe die Geräte nur in einem vertrauenswürdigen Netz.
- Das Hue-Streaming ist verschlüsselt (DTLS mit AES-GCM). Verwaltungsbefehle an die Bridge laufen über HTTPS (CLIP API v2); das Zertifikat der Bridge wird nur für ihre eigene Adresse akzeptiert.
- Den Streaming-Schlüssel der Bridge speichert AmbiBack verschlüsselt für dein Windows-Benutzerkonto (DPAPI).
- Zum Finden der Bridge fragt AmbiBack zuerst das lokale Netz (mDNS, ARP-Cache). Findet es dort nichts, oder klickst du auf **Suchen**, fragt es zusätzlich den Hue-Suchdienst `discovery.meethue.com`, den auch die Hue-App nutzt. Dabei werden keine Daten von dir übertragen; der Dienst sieht nur die Anfrage von deinem Internetanschluss.
