using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AmbiBack.PhilipsHue
{
    /// <summary>
    /// The entertainment area of this PC ("AmbiBack PC &lt;computer&gt;"), managed like the AmbiBack firmware
    /// does it: the single light devices with <see cref="CDevicePhilipsHueLight.InGroup"/> are its members,
    /// their screen sectors give the positions. The area is found by name, created or updated on the
    /// bridge, and shows up in the device list as a <see cref="CDeviceHueEntertainment"/> with
    /// <see cref="CDeviceHueEntertainment.PcGroup"/>; only that device streams (DTLS). Lights never send
    /// colors by themselves.
    /// </summary>
    public static class HuePcGroup
    {
        public const int MaxLights = 10;               // Hue limit per entertainment area
        private const int SyncDelayMs = 600;

        private static readonly object ConfigureLock = new object();
        private static readonly HashSet<string> usedLightIds = new HashSet<string>();
        private static int membershipVersion;
        private static Timer syncTimer;

        /// <summary>Area name on the bridge, at most 32 characters.</summary>
        public static string AreaName
        {
            get
            {
                string name = "AmbiBack PC " + Environment.MachineName;
                return name.Length > 32 ? name.Substring(0, 32) : name;
            }
        }

        /// <summary>Incremented whenever a light joins or leaves the group.</summary>
        public static int MembershipVersion { get { return Volatile.Read(ref membershipVersion); } }

        /// <summary>Result of <see cref="Configure"/>: area id and which light device feeds which channel.</summary>
        public class Layout
        {
            public string AreaID;
            public EntertainmentArea Area;
            public List<KeyValuePair<byte, CDevicePhilipsHueLight>> Channels = new List<KeyValuePair<byte, CDevicePhilipsHueLight>>();
        }

        /// <summary>Light devices that are members of the group.</summary>
        public static List<CDevicePhilipsHueLight> Members()
        {
            var result = new List<CDevicePhilipsHueLight>();
            if (CGlobals.mainConfig?.Devices == null)
                return result;
            foreach (CDevice device in CGlobals.mainConfig.Devices.GetDeviceList())
                if (device is CDevicePhilipsHueLight light && light.InGroup)
                    result.Add(light);
            return result;
        }

        /// <summary>
        /// A light joined or left. The streaming group device picks it up by itself; when it is not
        /// streaming, the area on the bridge is updated (and created and listed) in the background.
        /// </summary>
        public static void MembershipChanged()
        {
            Interlocked.Increment(ref membershipVersion);
            if (syncTimer == null)
                syncTimer = new Timer(_ => SyncInBackground(), null, Timeout.Infinite, Timeout.Infinite);
            syncTimer.Change(SyncDelayMs, Timeout.Infinite);
        }

        private static void SyncInBackground()
        {
            try
            {
                CDeviceHueEntertainment device = FindDevice();
                if (device != null && device.IsThreadRunningSafe())
                    return;                                   // the streaming device reconfigures itself
                List<CDevicePhilipsHueLight> members = Members();
                if (members.Count == 0 || string.IsNullOrEmpty(Configuration.ClientKey))
                    return;
                Layout layout = Configure(members, false);
                EnsureDevice(layout.Area);
            }
            catch (Exception ex)
            {
                Log(HueClipV2.IsAreaLimitError(ex) ? L.T("hue_area_limit") : "group update failed: " + ex.Message);
            }
        }

        /// <summary>The group device in the device list, or null.</summary>
        public static CDeviceHueEntertainment FindDevice()
        {
            if (CGlobals.mainConfig?.Devices == null)
                return null;
            foreach (CDevice device in CGlobals.mainConfig.Devices.GetDeviceList())
                if (device is CDeviceHueEntertainment area && area.PcGroup)
                    return area;
            return null;
        }

        /// <summary>Lists the group device (once), for the area as it is on the bridge.</summary>
        public static CDeviceHueEntertainment EnsureDevice(EntertainmentArea area)
        {
            CDeviceHueEntertainment device = FindDevice();
            if (device == null)
            {
                device = new CDeviceHueEntertainment(area) { PcGroup = true };
                device.SetDeviceID();
                device = (CDeviceHueEntertainment)CGlobals.mainConfig.Devices.Add(device);
            }
            else if (!device.IsThreadRunningSafe())
            {
                device.HueConfigID = area.ID;
                device.HueAreaName = area.Name;
                device.HueChannels = EntertainmentChannel.Format(area.Channels);
            }
            device.SetIsOnline(true);
            CGlobals.deviceController?.HandleDiscoveredDevice(device);
            return device;
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

        /// <summary>
        /// Creates or updates the area with the given lights (CLIP v2, never while it streams) and maps its
        /// channels to the light devices. Lights that left the group are switched off when switchOffRemoved.
        /// </summary>
        public static Layout Configure(List<CDevicePhilipsHueLight> lights, bool switchOffRemoved)
        {
            // Log lines are collected under the lock and written after it is released: the device debug output
            // does a synchronous Invoke to the UI thread, which may itself be waiting for this lock (OK button
            // of the lamps dialog), and that would deadlock
            var pending = new List<string>();
            try
            {
                lock (ConfigureLock)
                    return ConfigureLocked(lights, switchOffRemoved, pending);
            }
            finally
            {
                foreach (string line in pending)
                    Log(line);
            }
        }

        private static Layout ConfigureLocked(List<CDevicePhilipsHueLight> lights, bool switchOffRemoved, List<string> pending)
        {
            {
                List<StreamingLight> streamable = HueClipV2.GetStreamingLights();
                var chosen = new List<KeyValuePair<StreamingLight, EntertainmentChannel>>();
                var deviceByService = new Dictionary<string, CDevicePhilipsHueLight>();
                foreach (CDevicePhilipsHueLight device in lights)
                {
                    string v1 = device.HueLight?.ID;
                    StreamingLight light = streamable.Find(l => l.V1ID != null && l.V1ID == v1);
                    if (light == null)
                    {
                        pending.Add("\"" + device.GetDeviceName() + "\" cannot stream (no entertainment support), skipped");
                        continue;
                    }
                    if (chosen.Count >= MaxLights)
                    {
                        pending.Add("more than " + MaxLights + " lights, \"" + device.GetDeviceName() + "\" skipped");
                        continue;
                    }
                    chosen.Add(new KeyValuePair<StreamingLight, EntertainmentChannel>(light, PositionFromSectors(device.ScreenSectors)));
                    deviceByService[light.ServiceID] = device;
                }
                if (chosen.Count == 0)
                    throw new HueException(0, "none of the lights supports entertainment streaming");

                var newLightIds = new HashSet<string>();
                foreach (var c in chosen)
                    if (c.Key.LightID != null)
                        newLightIds.Add(c.Key.LightID);
                if (switchOffRemoved)
                {
                    foreach (string old in usedLightIds)
                    {
                        if (newLightIds.Contains(old))
                            continue;
                        try { HueClipV2.SetLightOn(old, false); }
                        catch (Exception ex) { pending.Add("switching light off failed: " + ex.Message); }
                    }
                }
                usedLightIds.Clear();
                usedLightIds.UnionWith(newLightIds);

                // Find the area by name (like the firmware) or create it
                string name = AreaName;
                EntertainmentArea area = HueClipV2.GetEntertainmentAreas().Find(a => a.Name == name);
                string id;
                if (area == null)
                {
                    id = HueClipV2.CreateEntertainmentArea(name, chosen);
                    pending.Add("area \"" + name + "\" created with " + chosen.Count + " lights");
                }
                else
                {
                    id = area.ID;
                    if (area.IsActive)
                        HueClipV2.SetStreaming(id, false);   // a left over session of ours
                    HueClipV2.UpdateEntertainmentArea(id, chosen);
                    pending.Add("area \"" + name + "\" updated with " + chosen.Count + " lights");
                }

                var layout = new Layout { AreaID = id, Area = HueClipV2.GetEntertainmentArea(id) };
                foreach (EntertainmentChannel ch in layout.Area.Channels)
                    foreach (string service in ch.MemberServices)
                        if (deviceByService.TryGetValue(service, out CDevicePhilipsHueLight device))
                        {
                            layout.Channels.Add(new KeyValuePair<byte, CDevicePhilipsHueLight>(ch.ID, device));
                            break;
                        }
                return layout;
            }
        }

        private static void Log(string text)
        {
            CGlobals.DebugOutput("HuePcGroup|" + text);
            FindDevice()?.DoDebugOutput("HuePcGroup|" + text);
        }
    }
}
