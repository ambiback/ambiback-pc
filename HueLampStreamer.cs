using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace AmbiBack.PhilipsHue
{
    /// <summary>
    /// Streams all running single light devices (<see cref="CDevicePhilipsHueLight"/>) over the
    /// Entertainment API, like the AmbiBack firmware does: one entertainment area of its own
    /// ("AmbiBack PC &lt;computer&gt;") is created or updated with exactly the active lights, then one DTLS
    /// session sends the colors of all lights at 25 to 50 fps. No color commands over HTTP.
    /// The light devices only compute their color and hand it over with <see cref="SetColor"/>.
    /// </summary>
    public static class HueLampStreamer
    {
        public const int MaxLights = 10;              // Hue limit per entertainment area
        private const int ReconfigureDelayMs = 500;    // devices start one after the other: wait for the last one
        private const int RetryMs = 5000;

        private static readonly object Sync = new object();
        private static readonly Dictionary<CDevicePhilipsHueLight, Member> members = new Dictionary<CDevicePhilipsHueLight, Member>();
        private static Thread thread;
        private static volatile bool shutdown;
        private static int membershipVersion;

        private class Member
        {
            public Color Color = Color.Black;
            public int FramesPerSecond = 25;
        }

        /// <summary>Area name as created on the bridge, at most 32 characters.</summary>
        public static string AreaName
        {
            get
            {
                string name = "AmbiBack PC " + Environment.MachineName;
                return name.Length > 32 ? name.Substring(0, 32) : name;
            }
        }

        /// <summary>Last state for the UI and the log.</summary>
        public static string Status { get; private set; } = "";

        /// <summary>A light device starts streaming (its thread runs).</summary>
        public static void Register(CDevicePhilipsHueLight device)
        {
            lock (Sync)
            {
                if (!members.ContainsKey(device))
                {
                    members[device] = new Member { FramesPerSecond = device.FramesPerSecond };
                    membershipVersion++;
                }
                shutdown = false;
                if (thread == null || !thread.IsAlive)
                {
                    thread = new Thread(Run) { IsBackground = true, Name = "HueLampStreamer", Priority = ThreadPriority.AboveNormal };
                    thread.Start();
                }
            }
        }

        /// <summary>A light device stops (Stop button, thread end). The light is switched off.</summary>
        public static void Unregister(CDevicePhilipsHueLight device)
        {
            lock (Sync)
            {
                if (members.Remove(device))
                    membershipVersion++;
            }
        }

        /// <summary>Latest color of a light (already with the color and brightness sliders applied).</summary>
        public static void SetColor(CDevicePhilipsHueLight device, Color color)
        {
            lock (Sync)
            {
                if (members.TryGetValue(device, out Member m))
                {
                    m.Color = color;
                    m.FramesPerSecond = device.FramesPerSecond;
                }
            }
        }

        /// <summary>App exit: stop streaming, switch the lights off and wait for it (at most timeoutMs).</summary>
        public static void Shutdown(int timeoutMs)
        {
            Thread t;
            lock (Sync)
            {
                members.Clear();
                membershipVersion++;
                shutdown = true;
                t = thread;
            }
            if (t != null && t.IsAlive)
                t.Join(Math.Max(0, timeoutMs));
        }

        /// <summary>Position in the area from the selected screen sectors (12 sectors, counterclockwise from bottom right).</summary>
        public static EntertainmentChannel PositionFromSectors(byte[] sectors)
        {
            // Sector centers: right bottom-up, top right-to-left, left top-down, bottom left-to-right
            double[,] centers =
            {
                { 1, -0.667 }, { 1, 0 }, { 1, 0.667 },
                { 0.667, 1 }, { 0, 1 }, { -0.667, 1 },
                { -1, 0.667 }, { -1, 0 }, { -1, -0.667 },
                { -0.667, -1 }, { 0, -1 }, { 0.667, -1 },
            };
            double x = 0, z = 0;
            int n = 0;
            for (int i = 0; sectors != null && i < sectors.Length && i < 12; i++)
            {
                if (sectors[i] == 1)
                {
                    x += centers[i, 0];
                    z += centers[i, 1];
                    n++;
                }
            }
            return n == 0 ? new EntertainmentChannel { Y = 1 } : new EntertainmentChannel { X = x / n, Y = 1, Z = z / n };
        }

        private static void Run()
        {
            HueEntertainmentSession session = null;
            int configuredVersion = -1;
            var changed = Stopwatch.StartNew();
            int lastVersion = -1;
            var retry = new Stopwatch();
            // channel -> light device, rebuilt after each area update
            var channelDevices = new List<KeyValuePair<byte, CDevicePhilipsHueLight>>();
            var usedLightIds = new HashSet<string>();
            byte[] ids = new byte[HueStreamMessage.MaxChannels];
            ushort[] rgb = new ushort[HueStreamMessage.MaxChannels * 3];

            Log("thread started");
            try
            {
                while (true)
                {
                    List<CDevicePhilipsHueLight> active;
                    int version;
                    lock (Sync)
                    {
                        active = new List<CDevicePhilipsHueLight>(members.Keys);
                        version = membershipVersion;
                    }
                    if (version != lastVersion)
                    {
                        lastVersion = version;
                        changed.Restart();
                    }

                    if (shutdown || active.Count == 0 || !CGlobals.monitorPowerStatus)
                    {
                        if (session != null)
                        {
                            session.Stop(turnOff: true);
                            session = null;
                            configuredVersion = -1;
                            usedLightIds.Clear();
                            SetStatus(active.Count == 0 ? "stopped" : "monitor off");
                        }
                        if (shutdown)
                            break;
                        Thread.Sleep(active.Count == 0 ? 200 : 1000);
                        if (active.Count == 0)
                        {
                            lock (Sync)
                            {
                                // Nobody left for a while: end the thread, Register starts a new one
                                if (members.Count == 0 && changed.ElapsedMilliseconds > 10000)
                                {
                                    thread = null;
                                    break;
                                }
                            }
                        }
                        continue;
                    }

                    // Membership changed: wait until it settles, then update the area
                    if (version != configuredVersion)
                    {
                        if (changed.ElapsedMilliseconds < ReconfigureDelayMs || (retry.IsRunning && retry.ElapsedMilliseconds < RetryMs))
                        {
                            Thread.Sleep(50);
                            continue;
                        }
                        session?.Stop(turnOff: false);
                        session = null;
                        try
                        {
                            session = Configure(active, channelDevices, usedLightIds);
                            configuredVersion = version;
                            retry.Reset();
                        }
                        catch (Exception ex)
                        {
                            SetStatus(HueClipV2.IsAreaLimitError(ex)
                                ? "area setup failed: the bridge already has 20 entertainment areas (its maximum); delete unused ones in the Hue dialog or the Hue app"
                                : "area setup failed: " + ex.Message);
                            retry.Restart();
                            continue;
                        }
                    }

                    if (!session.EnsureConnected())
                    {
                        SetStatus(session.State + (session.LastError != null ? ": " + session.LastError : ""));
                        Thread.Sleep(200);
                        continue;
                    }

                    // Send the latest colors of all lights
                    var clock = Stopwatch.StartNew();
                    int fps = 25, count = 0;
                    lock (Sync)
                    {
                        foreach (var entry in channelDevices)
                        {
                            if (count >= HueStreamMessage.MaxChannels || !members.TryGetValue(entry.Value, out Member m))
                                continue;
                            ids[count] = entry.Key;
                            rgb[count * 3] = (ushort)(m.Color.R * 257);
                            rgb[count * 3 + 1] = (ushort)(m.Color.G * 257);
                            rgb[count * 3 + 2] = (ushort)(m.Color.B * 257);
                            fps = Math.Max(fps, Math.Min(50, (int)m.FramesPerSecond));
                            count++;
                        }
                    }
                    session.Send(ids, rgb, count);
                    if (Status != "streaming")
                        SetStatus("streaming");
                    int sleep = 1000 / fps - (int)clock.ElapsedMilliseconds;
                    if (sleep > 0)
                        Thread.Sleep(sleep);
                }
            }
            catch (Exception ex)
            {
                Log("thread failed: " + ex.Message);
                CGlobals.DebugOutput(ex);
            }
            finally
            {
                try
                {
                    session?.Stop(turnOff: true);
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }
                lock (Sync)
                {
                    if (thread == Thread.CurrentThread)
                        thread = null;
                }
                Log("thread ended");
            }
        }

        /// <summary>
        /// Creates or updates the own area with the active lights and returns a session for it.
        /// Lights that left the area are switched off.
        /// </summary>
        private static HueEntertainmentSession Configure(List<CDevicePhilipsHueLight> active,
            List<KeyValuePair<byte, CDevicePhilipsHueLight>> channelDevices, HashSet<string> usedLightIds)
        {
            SetStatus("setting up area");
            List<StreamingLight> streamable = HueClipV2.GetStreamingLights();

            // Light device -> streaming light (via the v1 light id)
            var chosen = new List<KeyValuePair<StreamingLight, EntertainmentChannel>>();
            var deviceByService = new Dictionary<string, CDevicePhilipsHueLight>();
            foreach (CDevicePhilipsHueLight device in active)
            {
                string v1 = device.HueLight?.ID;
                StreamingLight light = streamable.Find(l => l.V1ID != null && l.V1ID == v1);
                if (light == null)
                {
                    Log("\"" + device.GetDeviceName() + "\" cannot stream (no entertainment support), skipped");
                    continue;
                }
                if (chosen.Count >= MaxLights)
                {
                    Log("more than " + MaxLights + " lights, \"" + device.GetDeviceName() + "\" skipped");
                    continue;
                }
                chosen.Add(new KeyValuePair<StreamingLight, EntertainmentChannel>(light, PositionFromSectors(device.ScreenSectors)));
                deviceByService[light.ServiceID] = device;
            }
            if (chosen.Count == 0)
                throw new HueException(0, "none of the lights supports entertainment streaming");

            // Switch off lights that are no longer part of the area
            var newLightIds = new HashSet<string>();
            foreach (var c in chosen)
                if (c.Key.LightID != null)
                    newLightIds.Add(c.Key.LightID);
            foreach (string old in usedLightIds)
            {
                if (newLightIds.Contains(old))
                    continue;
                try { HueClipV2.SetLightOn(old, false); }
                catch (Exception ex) { Log("switching light off failed: " + ex.Message); }
            }
            usedLightIds.Clear();
            usedLightIds.UnionWith(newLightIds);

            // Find our area by name (like the firmware) or create it
            string name = AreaName;
            EntertainmentArea area = HueClipV2.GetEntertainmentAreas().Find(a => a.Name == name);
            string id;
            if (area == null)
            {
                id = HueClipV2.CreateEntertainmentArea(name, chosen);
                Log("area \"" + name + "\" created with " + chosen.Count + " lights");
            }
            else
            {
                id = area.ID;
                if (area.IsActive)
                    HueClipV2.SetStreaming(id, false);   // a left over session of ours
                HueClipV2.UpdateEntertainmentArea(id, chosen);
                Log("area \"" + name + "\" updated with " + chosen.Count + " lights");
            }

            // Channels of the area -> light devices
            area = HueClipV2.GetEntertainmentArea(id);
            channelDevices.Clear();
            foreach (EntertainmentChannel ch in area.Channels)
                foreach (string service in ch.MemberServices)
                    if (deviceByService.TryGetValue(service, out CDevicePhilipsHueLight device))
                    {
                        channelDevices.Add(new KeyValuePair<byte, CDevicePhilipsHueLight>(ch.ID, device));
                        break;
                    }

            return new HueEntertainmentSession(id, Log);
        }

        private static void SetStatus(string status)
        {
            if (Status == status)
                return;
            Status = status;
            Log(status);
        }

        private static void Log(string text)
        {
            CGlobals.DebugOutput("HueLampStreamer|" + text);
            // Also into the debug view of the main window, through the running light devices
            List<CDevicePhilipsHueLight> list;
            lock (Sync)
                list = new List<CDevicePhilipsHueLight>(members.Keys);
            if (list.Count > 0)
                list[0].DoDebugOutput("HueStream|" + text);
        }
    }
}
