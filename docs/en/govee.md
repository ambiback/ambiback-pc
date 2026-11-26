# Govee lights

**English** · [Deutsch](../de/govee.md) · [User guide](../user-guide.md)

AmbiBack PC can drive Govee Wi-Fi lights (LED strips, light bars, TV backlights, floor lamps) over the **Govee LAN API**, directly on your network, without the Govee cloud and without an account.

## Before you start: switch on LAN Control

The LAN API is off by default. In the **Govee Home** app open the light, tap the gear icon for its settings and switch on **LAN Control**. Not every model offers it; without it AmbiBack PC cannot find or control the light.

The PC and the light must be on the same network.

## Add a Govee light

**Found on their own:** **Find Devices** (`F5`) also looks for Govee lights with LAN Control. Every light that answers appears in the list as *Govee (LAN)*, named after its model (e.g. *Govee H6199*).

**By hand:** click **Add**, enter the IP address of the light and choose **Govee (LAN)** as device type, or leave *Detect the device type automatically* ticked. You find the address in your router.

A light found again under a new address keeps its settings (it is recognized by its device ID).

## Choose the mode

Select the light and open the **Device** tab. The *Mode* setting replaces the connection setting there:

| Mode | What it does |
| --- | --- |
| **Segments (DreamView)** | Default. Sends one color per LED of the layout; the light spreads them over its segments. This uses the protocol behind Govee's DreamView and Razer Chroma support, which is not documented by Govee and works only on models with DreamView. |
| **Single color (any model)** | Sends the average color of the screen as one color, at most 10 times per second. Works on every light with LAN Control. |

If the light stays on its own scene in *Segments* mode, the model probably has no DreamView: switch to *Single color*.

## Set up the layout

In *Segments* mode the layout works like for an [AmbiBack controller](ambiback-devices.md#set-up-the-led-layout):

1. Click **Use** on the monitor card and tick the Govee light in the device list.
2. Select the light. On the monitor card set the number of LEDs **vertical** (one side) and **horizontal** (top or bottom), and the start corner and direction.
3. Click **Start**.

A new Govee light starts with 5 horizontal and 3 vertical (16 colors in total). Govee does not report how many segments a light has; set the counts so that the total matches the segments of your light, or the number the Govee app shows for DreamView. At most 255 colors are sent.

## Settings

All sliders on the [*Colors* and *Device* tabs](settings.md) work for Govee:

- **Frames per second**: 30 by default. In *Single color* mode at most 10 colors per second are sent anyway.
- **Smoothing**: 0 (off) by default. Higher values fade the colors more softly; AmbiBack PC does this before sending.
- **Red, Green, Blue**, **Color boost**, **Value gain**, **Maximum brightness** and **Minimum brightness** are applied in AmbiBack PC. The brightness set in the Govee app still limits the light.

## Start and stop

On start AmbiBack PC switches the light on. When it stops, the monitor turns off or the PC goes to sleep, a light in *Segments* mode returns to the scene or color set in the Govee app. In *Single color* mode it keeps the last color.

Only one program can drive the light at a time: close DreamView in the Govee app and other tools that control the light over the LAN.

## Check without a monitor

From a command line you can stream a moving test picture to a light:

```powershell
& "$env:LOCALAPPDATA\Programs\AmbiBack\AmbiBack.exe" --govee-test 192.168.0.43 10
```

Add `single` at the end for *Single color* mode. It reports what the light answers about itself (model, device ID, firmware) and the frames per second it sends. Stop the monitor that drives this light first, so that only one sends.

## Problems

| Problem | Solution |
| --- | --- |
| The light is not found | Is *LAN Control* switched on in the Govee Home app? Is the light on the same network? Some routers block multicast between Wi-Fi and LAN; **Add** it by IP address. A firewall must let UDP port 4002 in. |
| The light stays on its own scene | In *Segments* mode: the model may not support DreamView, switch to *Single color*. Close DreamView in the Govee app. |
| Colors on the wrong side | Change the start corner or direction on the monitor card. Check the counts per side against the segments of the light. |
| The colors change slowly | In *Single color* mode the light gets at most 10 colors per second. Use *Segments* if the model supports it. |
