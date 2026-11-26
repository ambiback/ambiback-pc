# Nanoleaf panels

**English** · [Deutsch](../de/nanoleaf.md) · [User guide](../user-guide.md)

AmbiBack PC drives Nanoleaf panels (Light Panels, Canvas, Shapes, Elements, Lines) directly on your network through the **Nanoleaf Open API**, without the Nanoleaf app or cloud. Every panel gets the color of the screen area at its place in your layout.

## Pair a Nanoleaf

A Nanoleaf only accepts commands from programs that were paired with it once:

1. Click **Add**, untick *Detect the device type automatically* and choose **Nanoleaf** as device type.
2. Enter the IP address of the Nanoleaf, or leave the field empty: AmbiBack PC then uses the first Nanoleaf on the network that is not in the list yet.
3. Click **OK**. AmbiBack PC asks you to hold the **power button** on the Nanoleaf controller for 5–7 seconds until the lights flash. Then click **OK** within 30 seconds.

If the Nanoleaf was not in pairing mode, AmbiBack PC tells you so; hold the button again and click **Retry**.

The Nanoleaf appears in the list with its name. AmbiBack PC keeps the key from pairing protected for your Windows user (DPAPI). **Find Devices** (`F5`) finds a paired Nanoleaf again, also under a new address (it is recognized by its serial number). Give it a fixed IP address in your router anyway.

## Choose the mode

Select the Nanoleaf and open the **Device** tab. The *Mode* setting replaces the connection setting there:

| Mode | What it does |
| --- | --- |
| **Panels by their place** | Default. Every panel gets the color of the screen area at its place in the layout from the Nanoleaf app: panels on the left show the left of the picture, panels at the top the top. The layout is turned like in the app (*global orientation*). |
| **One color on all panels** | Every panel gets the average color of the screen. |

The monitor card shows no strip layout for a Nanoleaf: the places come from the Nanoleaf itself. If you rearrange the panels, AmbiBack PC reads the new layout on the next start.

## Start and stop

1. Click **Use** on the monitor card and tick the Nanoleaf in the device list.
2. Click **Start**.

AmbiBack PC switches the Nanoleaf on and to external control. When it stops, the effect that ran before is selected again. If you choose another effect in the Nanoleaf app while AmbiBack PC runs, AmbiBack PC takes over again within 15 seconds.

## Settings

All sliders on the [*Colors* and *Device* tabs](settings.md) work for Nanoleaf:

- **Frames per second**: 20 by default, up to 30.
- **Smoothing**: 100 ms by default, so that the panels change calmly. 0 shows every change at once.
- **Inner picture area (%)**: the size of the screen area around each panel's place (25 % by default).
- **Red, Green, Blue**, **Color boost**, **Value gain**, **Maximum brightness** and **Minimum brightness** are applied in AmbiBack PC. The brightness set on the Nanoleaf still limits the panels.

## Check without a monitor

From a command line you can pair with a Nanoleaf and stream a moving test picture:

```powershell
& "$env:LOCALAPPDATA\Programs\AmbiBack\AmbiBack.exe" --nanoleaf-test 192.168.0.44 10
```

Without a key it waits up to 30 seconds for the power button and prints the new key; pass it after the address next time. Add `single` at the end for *One color on all panels*.

## Problems

| Problem | Solution |
| --- | --- |
| Pairing fails | Hold the power button until the lights flash (5–7 seconds), then click within 30 seconds. On Lines and Elements it is the button on the controller. |
| The Nanoleaf is not found | Is it on the same network? Enter its IP address when adding. Some routers block mDNS between Wi-Fi and LAN. |
| A paired Nanoleaf stays offline | Click **Find Devices**. If it was reset, delete it from the list and pair it again. |
| The panels show the wrong side of the picture | Check the layout and its rotation in the Nanoleaf app; AmbiBack PC uses them as they are. |
| The panels stay on their effect | Is the Nanoleaf ticked for the monitor and the monitor started? Older Light Panels (Aurora) need firmware 3.1 or newer for external control. |
