using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using AmbiBack.PhilipsHue;
using Newtonsoft.Json.Linq;

namespace AmbiBack
{
    /// <summary>
    /// Nanoleaf panels (Light Panels, Canvas, Shapes, Elements, Lines) over the Nanoleaf Open API: HTTP on
    /// port 16021 for pairing, layout and effects, UDP port 60222 for external control (extControl v2).
    ///
    /// Pairing: hold the power button 5–7 s, then POST /api/v1/new returns the auth token (kept with
    /// DPAPI). Every panel gets the color of a screen region at its place in the panel layout (rotated by the
    /// layout's global orientation), or all panels get the average color. On stop the effect that ran
    /// before is selected again.
    /// </summary>
    [Serializable]
    public class CDeviceNanoleaf : CDeviceLedStripUdp
    {
        public const int ApiPort = 16021;
        public const int StreamPort = 60222;
        public const byte ModePositions = 0;
        public const byte ModeSingleColor = 1;

        /// <summary>Shape types without light: Rhythm module (1) and Shapes controller (12).</summary>
        private static readonly int[] ShapesWithoutLight = { 1, 12 };
        /// <summary>The panel layout and the external control mode are checked again regularly.</summary>
        private const int ExtControlRefreshMs = 15000;
        private const int HttpTimeoutMs = 3000;

        /// <summary>A panel with light: its id and its place in the layout (Nanoleaf units, y up).</summary>
        public class Panel
        {
            public int Id;
            public int X;
            public int Y;
        }

        private string authToken;
        private string previousEffect;
        private readonly Stopwatch sinceExtControl = new Stopwatch();
        private byte[] frame;

        #region Stored settings
        /// <summary>The auth token, DPAPI protected (as stored in the configuration).</summary>
        public string AuthTokenProtected { get; set; } = "";
        public string Model { get; set; } = "";
        public List<Panel> Panels { get; set; } = new List<Panel>();
        /// <summary>Rotation of the layout in degrees, as set in the Nanoleaf app.</summary>
        public int GlobalOrientation { get; set; } = 0;
        #endregion

        public CDeviceNanoleaf()
        {
            this.SetDeviceType(DeviceType.Nanoleaf);
            this.SetDeviceName("Nanoleaf");
            this.InitClass();
        }

        public CDeviceNanoleaf(string szDeviceName, IPAddress ipAddress)
        {
            this.SetDeviceType(DeviceType.Nanoleaf);
            this.SetDeviceName(szDeviceName);
            this.SetIPAddress(ipAddress);
            this.InitClass();
        }

        /// <summary>The auth token in plain text; null when not paired or not readable for this Windows user.</summary>
        public string AuthToken
        {
            get { return authToken ?? (authToken = HueSecrets.Unprotect(this.AuthTokenProtected)); }
            set { authToken = value; this.AuthTokenProtected = HueSecrets.Protect(value); }
        }

        protected override int DefaultPort { get { return StreamPort; } }
        protected override byte DefaultFramesPerSecond { get { return 20; } }
        public override string[] ModeTextKeys { get { return new[] { "nanoleaf_mode_positions", "nanoleaf_mode_single" }; } }
        public override bool UsesStripLayout { get { return false; } }
        protected override int ColorCount { get { return this.Panels.Count; } }

        public override void InitClass()
        {
            base.InitClass();
            this.MaxFramesPerSecond = 30;
            this.InnerSizePercent = 25;     // size of the region around each panel (percent of the screen)
            this.FrameDelay = 100;          // panels look calmer with a little smoothing
        }

        protected override void WriteStripXml(XmlWriter writer)
        {
            this.WriteXmlElement(writer, "Mode", this.Mode);
            writer.WriteElementString("AuthToken", this.AuthTokenProtected ?? "");
            writer.WriteElementString("Model", this.Model ?? "");
            writer.WriteElementString("Panels", FormatPanels(this.Panels));
            this.WriteXmlElement(writer, "GlobalOrientation", this.GlobalOrientation);
        }

        protected override void ReadStripXml(XmlReader reader)
        {
            this.Mode = this.ReadXmlElementAsByte(reader, "Mode", ModePositions);
            if (this.Mode > ModeSingleColor)
                this.Mode = ModePositions;
            this.AuthTokenProtected = reader.ReadElementContentAsString("AuthToken", "");
            this.Model = reader.ReadElementContentAsString("Model", "");
            this.Panels = ParsePanels(reader.ReadElementContentAsString("Panels", ""));
            this.GlobalOrientation = this.ReadXmlElementAsInt(reader, "GlobalOrientation", 0);
            authToken = null;
        }

        public override void UpdateFrom(CDevice devCopyFrom)
        {
            base.UpdateFrom(devCopyFrom);
            CDeviceNanoleaf other = (CDeviceNanoleaf)devCopyFrom;
            // Paired again, or the layout changed in the Nanoleaf app
            if (!string.IsNullOrEmpty(other.AuthTokenProtected))
            {
                this.AuthTokenProtected = other.AuthTokenProtected;
                authToken = null;
            }
            if (other.Panels.Count > 0)
            {
                this.Panels = other.Panels;
                this.GlobalOrientation = other.GlobalOrientation;
                this.bInitDone = false;
            }
            if (!string.IsNullOrEmpty(other.Model))
                this.Model = other.Model;
        }

        #region Scan regions
        protected override void CalcScanRegions(int width, int height, Rectangle[] regions)
        {
            PointF[] places = ScreenPlaces(this.Panels, this.GlobalOrientation);
            for (int i = 0; i < regions.Length && i < places.Length; i++)
                regions[i] = PanelRegion(places[i], width, height, this.InnerSizePercent);
        }

        /// <summary>
        /// Places of the panels on the screen, 0..1 from left to right and top to bottom: the layout rotated by
        /// the global orientation and stretched over the screen (y of the layout points up).
        /// </summary>
        public static PointF[] ScreenPlaces(List<Panel> panels, int orientationDegrees)
        {
            var result = new PointF[panels.Count];
            if (panels.Count == 0)
                return result;
            double a = -orientationDegrees * Math.PI / 180.0;
            double cos = Math.Cos(a), sin = Math.Sin(a);
            var rx = new double[panels.Count];
            var ry = new double[panels.Count];
            for (int i = 0; i < panels.Count; i++)
            {
                rx[i] = panels[i].X * cos - panels[i].Y * sin;
                ry[i] = panels[i].X * sin + panels[i].Y * cos;
            }
            double minX = rx.Min(), maxX = rx.Max(), minY = ry.Min(), maxY = ry.Max();
            for (int i = 0; i < panels.Count; i++)
            {
                float x = maxX - minX < 1e-6 ? 0.5f : (float)((rx[i] - minX) / (maxX - minX));
                float y = maxY - minY < 1e-6 ? 0.5f : (float)((maxY - ry[i]) / (maxY - minY));
                result[i] = new PointF(x, y);
            }
            return result;
        }

        /// <summary>Region of the given size (percent of the screen) around a place, inside the screen.</summary>
        public static Rectangle PanelRegion(PointF place, int width, int height, int sizePercent)
        {
            double size = Math.Max(5, Math.Min(50, sizePercent)) / 100.0;
            int w = Math.Max(1, (int)(width * size));
            int h = Math.Max(1, (int)(height * size));
            int cx = (int)Math.Round(place.X * width);
            int cy = (int)Math.Round(place.Y * height);
            int left = Math.Max(0, Math.Min(width - w, cx - w / 2));
            int top = Math.Max(0, Math.Min(height - h, cy - h / 2));
            return new Rectangle(left, top, w, h);
        }
        #endregion

        #region Connection and sending
        protected override void Connect(IPAddress ip)
        {
            string token = this.AuthToken;
            if (string.IsNullOrEmpty(token))
                throw new IOException("Nanoleaf is not paired");
            // Layout and current effect; the panels may have been rearranged since the last time
            NanoleafInfo info = QueryInfo(ip, token);
            if (info == null)
                throw new IOException("Nanoleaf does not answer with this auth token");
            if (FormatPanels(info.Panels) != FormatPanels(this.Panels) || info.Orientation != this.GlobalOrientation)
            {
                this.Panels = info.Panels;
                this.GlobalOrientation = info.Orientation;
                this.bInitDone = false;
            }
            previousEffect = info.SelectedEffect;
            base.Connect(ip);
        }

        protected override void OnStreamStart()
        {
            IPAddress ip = this.GetIPAddress();
            Request("PUT", ApiUrl(ip, this.AuthToken, "state"), "{\"on\":{\"value\":true}}");
            StartExtControl(ip);
        }

        private void StartExtControl(IPAddress ip)
        {
            Request("PUT", ApiUrl(ip, this.AuthToken, "effects"), ExtControlRequest);
            sinceExtControl.Restart();
        }

        protected override void OnStreamStop()
        {
            // Back to the effect that ran before
            if (string.IsNullOrEmpty(previousEffect) || previousEffect.StartsWith("*", StringComparison.Ordinal))
                return;
            Request("PUT", ApiUrl(this.GetIPAddress(), this.AuthToken, "effects"),
                new JObject { ["select"] = previousEffect }.ToString(Newtonsoft.Json.Formatting.None));
        }

        protected override void SendFrame(byte[] colors, int bytes)
        {
            if (this.Panels.Count == 0)
                return;
            // Another effect may have been chosen in the Nanoleaf app meanwhile
            if (sinceExtControl.ElapsedMilliseconds >= ExtControlRefreshMs)
                StartExtControl(this.GetIPAddress());
            int count = Math.Min(this.Panels.Count, bytes / 3);
            if (frame == null || frame.Length != 2 + 8 * count)
                frame = new byte[2 + 8 * count];
            FillFrameV2(frame, this.Panels, colors, count, this.Mode == ModeSingleColor);
            SendDatagram(frame);
        }
        #endregion

        #region Protocol
        public const string ExtControlRequest = "{\"write\":{\"command\":\"display\",\"animType\":\"extControl\",\"extControlVersion\":\"v2\"}}";

        /// <summary>
        /// extControl v2 frame: panel count (u16 big endian), then per panel: id (u16 BE), R, G, B, W (0),
        /// transition time (u16 BE, 100 ms units, 0: smoothing is done in AmbiBack PC).
        /// </summary>
        public static void FillFrameV2(byte[] frame, List<Panel> panels, byte[] colors, int count, bool singleColor)
        {
            int avg = singleColor ? AverageColor(colors, count) : 0;
            frame[0] = (byte)(count >> 8);
            frame[1] = (byte)count;
            int o = 2;
            for (int i = 0; i < count; i++, o += 8)
            {
                frame[o] = (byte)(panels[i].Id >> 8);
                frame[o + 1] = (byte)panels[i].Id;
                if (singleColor)
                {
                    frame[o + 2] = (byte)(avg >> 16);
                    frame[o + 3] = (byte)(avg >> 8);
                    frame[o + 4] = (byte)avg;
                }
                else
                {
                    frame[o + 2] = colors[i * 3];
                    frame[o + 3] = colors[i * 3 + 1];
                    frame[o + 4] = colors[i * 3 + 2];
                }
                frame[o + 5] = 0;
                frame[o + 6] = 0;
                frame[o + 7] = 0;
            }
        }

        private static string ApiUrl(IPAddress ip, string token, string path)
        {
            return "http://" + ip + ":" + ApiPort + "/api/v1/" + token + "/" + path;
        }

        /// <summary>HTTP request to the Nanoleaf; returns the status code and the body (an error status does not throw).</summary>
        private static KeyValuePair<int, string> Request(string method, string url, string body)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Timeout = HttpTimeoutMs;
            request.ReadWriteTimeout = HttpTimeoutMs;
            request.Proxy = null;
            if (body != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(body);
                request.ContentType = "application/json";
                request.ContentLength = data.Length;
                using (Stream s = request.GetRequestStream())
                    s.Write(data, 0, data.Length);
            }
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    return new KeyValuePair<int, string>((int)response.StatusCode, reader.ReadToEnd());
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse error)
            {
                using (error)
                    return new KeyValuePair<int, string>((int)error.StatusCode, "");
            }
            catch (WebException ex)
            {
                throw new IOException("Nanoleaf at " + request.RequestUri.Host + ": " + ex.Message, ex);
            }
        }

        public static string FormatPanels(List<Panel> panels)
        {
            return string.Join(";", panels.Select(p => p.Id + ":" + p.X + ":" + p.Y));
        }

        public static List<Panel> ParsePanels(string text)
        {
            var result = new List<Panel>();
            foreach (string item in (text ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] f = item.Split(':');
                if (f.Length == 3 && int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                    && int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    && int.TryParse(f[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                    result.Add(new Panel { Id = id, X = x, Y = y });
            }
            return result;
        }
        #endregion

        #region Pairing, info and discovery
        /// <summary>What a paired Nanoleaf reports under /api/v1/{token}/.</summary>
        public class NanoleafInfo
        {
            public string Name = "";
            public string SerialNo = "";
            public string Model = "";
            public string Firmware = "";
            public string SelectedEffect = "";
            public int Orientation;
            public List<Panel> Panels = new List<Panel>();
        }

        /// <summary>
        /// Asks for an auth token. Returns null while the Nanoleaf is not in pairing mode (hold the power
        /// button 5–7 s first); throws IOException when nothing answers.
        /// </summary>
        public static string Pair(IPAddress ip)
        {
            KeyValuePair<int, string> answer = Request("POST", "http://" + ip + ":" + ApiPort + "/api/v1/new", "");
            if (answer.Key != 200)
                return null;
            try
            {
                return (string)JObject.Parse(answer.Value)["auth_token"];
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return null;
            }
        }

        /// <summary>Info of a paired Nanoleaf; null when the token is not accepted.</summary>
        public static NanoleafInfo QueryInfo(IPAddress ip, string token)
        {
            KeyValuePair<int, string> answer = Request("GET", ApiUrl(ip, token, ""), null);
            if (answer.Key != 200)
                return null;
            try
            {
                return ParseInfo(answer.Value);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return null;
            }
        }

        /// <summary>Parses the device info: name, serial, model, firmware, current effect and the panels with light.</summary>
        public static NanoleafInfo ParseInfo(string json)
        {
            JObject o = JObject.Parse(json);
            var info = new NanoleafInfo
            {
                Name = (string)o["name"] ?? "Nanoleaf",
                SerialNo = (string)o["serialNo"] ?? "",
                Model = (string)o["model"] ?? "",
                Firmware = (string)o["firmwareVersion"] ?? "",
                SelectedEffect = (string)o["effects"]?["select"] ?? "",
                Orientation = (int?)o["panelLayout"]?["globalOrientation"]?["value"] ?? 0,
            };
            if (o["panelLayout"]?["layout"]?["positionData"] is JArray positions)
            {
                foreach (JToken p in positions)
                {
                    int shape = (int?)p["shapeType"] ?? 0;
                    if (Array.IndexOf(ShapesWithoutLight, shape) >= 0)
                        continue;
                    info.Panels.Add(new Panel { Id = (int?)p["panelId"] ?? 0, X = (int?)p["x"] ?? 0, Y = (int?)p["y"] ?? 0 });
                }
            }
            return info;
        }

        /// <summary>Addresses of Nanoleaf devices that answer an mDNS query for _nanoleafapi._tcp.local.</summary>
        public static List<IPAddress> Discover(int timeoutMs = 2000)
        {
            return HueDiscovery.QueryMdns(timeoutMs, "_nanoleafapi");
        }

        /// <summary>New device from a paired Nanoleaf: its name, serial for the id, token and layout.</summary>
        public static CDeviceNanoleaf FromInfo(NanoleafInfo info, IPAddress ip, string token)
        {
            var device = new CDeviceNanoleaf(info.Name, ip)
            {
                Model = info.Model,
                ReceiverInfo = (info.Model + (string.IsNullOrEmpty(info.Firmware) ? "" : ", " + info.Firmware)).Trim(),
                ReceiverLedCount = info.Panels.Count,
                Panels = info.Panels,
                GlobalOrientation = info.Orientation,
                InternalNetworkMAC = string.IsNullOrEmpty(info.SerialNo) ? null : info.SerialNo.ToLowerInvariant(),
            };
            device.AuthToken = token;
            device.SetDeviceID();
            device.SetIsOnline(true);
            return device;
        }

        /// <summary>
        /// Command line test (--nanoleaf-test host [token] [seconds] [single]): pairs when no token is given
        /// (waits up to 30 s for the power button), then streams a moving test picture. True when frames were sent.
        /// </summary>
        public static bool RunStreamTest(string host, string token, int seconds, bool singleColor, Action<string> output)
        {
            IPAddress ip = ResolveHost(host, output);
            if (ip == null)
                return false;
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    output("Hold the power button of the Nanoleaf for 5-7 seconds until the lights flash ...");
                    var clock = Stopwatch.StartNew();
                    while ((token = Pair(ip)) == null && clock.ElapsedMilliseconds < 30000)
                        System.Threading.Thread.Sleep(1000);
                    if (token == null)
                    {
                        output("Not paired: the Nanoleaf did not enter pairing mode.");
                        return false;
                    }
                    output("Paired, auth token " + token);
                }
                NanoleafInfo info = QueryInfo(ip, token);
                if (info == null)
                {
                    output("The Nanoleaf does not accept this auth token.");
                    return false;
                }
                output(info.Model + " \"" + info.Name + "\" (serial " + info.SerialNo + ", firmware " + info.Firmware + "), "
                    + info.Panels.Count + " panels, orientation " + info.Orientation + ", effect \"" + info.SelectedEffect + "\"");
                CDeviceNanoleaf device = FromInfo(info, ip, token);
                device.Mode = singleColor ? ModeSingleColor : ModePositions;
                int framesReported = StreamTestPicture(device, seconds, output);
                output(framesReported > 0 ? "Done, the previous effect is selected again." : "No frames were sent.");
                return framesReported > 0;
            }
            catch (IOException ex)
            {
                output(ex.Message);
                return false;
            }
        }
        #endregion
    }
}
