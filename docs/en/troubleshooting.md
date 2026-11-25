# Troubleshooting and FAQ

**English** · [Deutsch](../de/troubleshooting.md) · [User guide](../user-guide.md)

## Installation and start

| Problem | Solution |
| --- | --- |
| "Windows protected your PC" (SmartScreen) | The files are not code-signed. Click **More info**, then **Run anyway**. Download only from the [releases page](https://github.com/ambiback/ambiback-pc/releases). |
| AmbiBack does not start after the setup | The .NET Framework is probably missing or Windows still needs a restart after installing it. Restart Windows; otherwise install the [.NET Framework 4.8.1](https://dotnet.microsoft.com/download/dotnet-framework). |
| Device web pages or *Details* stay empty | The WebView2 Runtime is missing. Run the setup again or install the [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). |
| Windows starts the wrong copy of AmbiBack | Open *Options* in the copy that should start. The line under *Start AmbiBack when Windows starts* shows which program Windows starts. Tick the box there and click **OK**. |
| Starting AmbiBack again does nothing | Only one AmbiBack runs. It is probably hidden in the notification area: click the arrow `^` in the taskbar and double-click the AmbiBack icon. |

## Windows Firewall

AmbiBack sends a search into the network and receives the answers of AmbiBack controllers on **UDP port 1337**. Windows Firewall must let these answers in. Sending colors and everything for Philips Hue are outgoing connections and are not affected.

**On the first start** Windows asks whether AmbiBack may communicate. Tick **Private networks** and click **Allow**.

**If you blocked it or the prompt did not appear:**

1. Check that your home network is set to *Private*: Windows *Settings › Network & internet › Wi-Fi* (or *Ethernet*) › your network › *Network profile type: Private network*. On a *Public* network Windows blocks the answers.
2. Open *Windows Security › Firewall & network protection › Allow an app through firewall*.
3. Click *Change settings*, find **AmbiBack** and tick **Private**. If it is missing, use *Allow another app…* and choose `AmbiBack.exe`.

**Or with PowerShell** (run as administrator; adjust the path for an installation for all users or the ZIP):

```powershell
New-NetFirewallRule -DisplayName "AmbiBack PC" -Direction Inbound -Action Allow -Protocol UDP -LocalPort 1337 -Profile Private -Program "$env:LOCALAPPDATA\Programs\AmbiBack\AmbiBack.exe"
```

For an installation for all users the program is `C:\Program Files\AmbiBack\AmbiBack.exe`.

Other firewalls or security suites (e.g. from an antivirus) need the same exception.

## AmbiBack devices

| Problem | Solution |
| --- | --- |
| The controller is not found | Is it on and on the same network (same router, not a guest Wi-Fi)? Check the [firewall](#windows-firewall). Otherwise **Add** it by IP address. |
| Nothing is ever found although the firewall allows it | Another program may already use UDP port 1337. Close other ambient light or AmbiBack tools and restart AmbiBack. |
| The controller over USB is not found | Install the driver of its USB chip ([FTDI](https://ftdichip.com/drivers/vcp-drivers/), [CP210x](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)); try another cable (some cables only charge). |
| "Wi-Fi settings not sent" | The controller must be connected over USB (blue dot). Check the cable and try again. |
| The controller does not join the Wi-Fi | Check SSID and password (case matters). The controller needs a 2.4 GHz network. |
| Colors are on the wrong side | Change the start corner or direction of the strip on the monitor card ([LED layout](ambiback-devices.md#set-up-the-led-layout)). |
| The light lags or stutters | Use *UDP*, move the router closer or lower *Frames per second*. Raise *Smoothing* for softer changes. |
| Offline entries cannot be removed | Use **Remove Offline**, or select the device and **Delete** it. |

## Philips Hue

| Problem | Solution |
| --- | --- |
| The Hue Bridge is not found | Check that the PC and the bridge are on the same network and click **Search**. Otherwise type its IP address (Hue app: *Settings › Bridges › i*). |
| "Connect" reports an error | Press the link button on the bridge and click *Connect* **within 30 seconds**. |
| "Without a streaming key" | The connection comes from an older version. Press the link button and *Connect* again. |
| The Hue lights stay dark | Are lights ticked in the group (or chosen in *Choose Lights*)? Is the group linked to the monitor, and is the monitor started? |
| The lights flicker or jump | Use one path per light: the PC group **or** an area from the Hue app, not both, and no other sync app (Hue Sync, Hue Sync Box, TV sync) at the same time. |
| "The bridge already has 20 entertainment areas" | Remove unused areas with **Delete…** in the Hue dialog or in the Hue app. |
| A light is missing from the list | Only color lights with entertainment support are shown; plugs and white lights are not. |
| Only 10 lights can be ticked | The bridge streams at most 10 lights per entertainment area, and only one area at a time. Choose the 10 lights closest to the screen. |
| The lights stay on after AmbiBack closed | AmbiBack switches them off on *Stop* and *Exit*. If Windows was shut down hard, switch them off in the Hue app. |

## FAQ

**Does AmbiBack work with games and videos?**
Yes, it captures the whole screen through DirectX. Content with copy protection (some streaming apps in a browser) can appear black to screen capture; the lights then stay dark for that window.

**Can I use several monitors?**
Yes. Each monitor has its own card; turn on **Use** and link the devices to it.

**Can I use AmbiBack controllers and Hue at the same time?**
Yes. Link each to the monitor it belongs to and click **Start All**.

**Does it need the internet?**
No. Only the Hue bridge search may ask the Hue discovery service when nothing is found on the local network, and the setup downloads the .NET Framework if it is missing.

**Can I still buy AmbiBack devices?**
No. AmbiBack devices have not been on sale for around five years. The project is for DIY builds; the parts are on the [AmbiBack parts page](https://www.ambiback.de/en/diy/parts/).

## Debug log

Turn on the *Debug log* in [Options](settings.md#options) and reproduce the problem. The log helps when you report an issue on [GitHub](https://github.com/ambiback/ambiback-pc/issues). Hue keys are never written to it.

## Privacy and security

- The connection to AmbiBack controllers is not encrypted in the local network, and the device API has no login. Run the devices only in a network you trust.
- Hue streaming is encrypted (DTLS with AES-GCM). Management commands to the bridge use HTTPS (CLIP API v2); the bridge certificate is accepted only for the bridge's own address.
- AmbiBack stores the bridge's streaming key encrypted for your Windows user account (DPAPI).
- To find the bridge, AmbiBack asks the local network first (mDNS, ARP cache). If that finds nothing, or when you click **Search**, it also asks the Hue discovery service `discovery.meethue.com`, which the Hue app uses as well. No data about you is sent; the service only sees the request from your internet connection.
