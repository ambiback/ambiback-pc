using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AmbiBack.PhilipsHue
{
    /// <summary>One channel of an entertainment area with its position in the room.</summary>
    public class EntertainmentChannel
    {
        public byte ID { get; set; }
        /// <summary>-1 = left, +1 = right.</summary>
        public double X { get; set; }
        /// <summary>-1 = back (behind the user), +1 = front (screen side).</summary>
        public double Y { get; set; }
        /// <summary>-1 = bottom, +1 = top.</summary>
        public double Z { get; set; }
        /// <summary>Entertainment service ids of the lights in this channel (not stored in the device XML).</summary>
        public List<string> MemberServices { get; } = new List<string>();

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:0.###}:{2:0.###}:{3:0.###}", ID, X, Y, Z);
        }

        /// <summary>Parses "id:x:y:z;id:x:y:z" as written by <see cref="Format"/>.</summary>
        public static List<EntertainmentChannel> Parse(string text)
        {
            var result = new List<EntertainmentChannel>();
            if (string.IsNullOrEmpty(text))
                return result;
            foreach (string entry in text.Split(';'))
            {
                string[] p = entry.Split(':');
                if (p.Length != 4)
                    continue;
                if (byte.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte id)
                    && double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                    && double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                    && double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                    result.Add(new EntertainmentChannel { ID = id, X = x, Y = y, Z = z });
            }
            return result;
        }

        public static string Format(IEnumerable<EntertainmentChannel> channels)
        {
            return string.Join(";", channels);
        }
    }

    /// <summary>A light that can stream (CLIP v2 entertainment service with renderer).</summary>
    public class StreamingLight
    {
        /// <summary>Id of the entertainment service (not the light or device id).</summary>
        public string ServiceID { get; set; }
        /// <summary>CLIP v2 light resource id (on/off).</summary>
        public string LightID { get; set; }
        /// <summary>Light id of the old API (v1 "/lights/{id}"), to match the light devices.</summary>
        public string V1ID { get; set; }
        public string Name { get; set; }
        public string Product { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>Entertainment configuration (area) as reported by CLIP API v2.</summary>
    public class EntertainmentArea
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string ConfigurationType { get; set; }
        /// <summary>"active" while somebody streams to the area, otherwise "inactive".</summary>
        public string Status { get; set; }
        /// <summary>Resource id of the application that streams, when active.</summary>
        public string ActiveStreamer { get; set; }
        public List<EntertainmentChannel> Channels { get; } = new List<EntertainmentChannel>();
        /// <summary>Light resource ids of the area (to switch them off after streaming).</summary>
        public List<string> LightServices { get; } = new List<string>();

        public bool IsActive { get { return string.Equals(Status, "active", StringComparison.OrdinalIgnoreCase); } }

        public override string ToString()
        {
            return Name;
        }

        internal static EntertainmentArea FromJson(JToken item)
        {
            var area = new EntertainmentArea
            {
                ID = (string)item["id"],
                Name = (string)item["metadata"]?["name"] ?? (string)item["name"] ?? (string)item["id"],
                ConfigurationType = (string)item["configuration_type"],
                Status = (string)item["status"],
                ActiveStreamer = (string)item["active_streamer"]?["rid"],
            };
            if (item["light_services"] is JArray lights)
                foreach (JToken l in lights)
                    if ((string)l["rid"] != null)
                        area.LightServices.Add((string)l["rid"]);
            if (item["channels"] is JArray channels)
            {
                foreach (JToken ch in channels)
                {
                    var channel = new EntertainmentChannel
                    {
                        ID = (byte)((int?)ch["channel_id"] ?? 0),
                        X = (double?)ch["position"]?["x"] ?? 0,
                        Y = (double?)ch["position"]?["y"] ?? 0,
                        Z = (double?)ch["position"]?["z"] ?? 0,
                    };
                    if (ch["members"] is JArray members)
                        foreach (JToken m in members)
                            if ((string)m["service"]?["rid"] != null)
                                channel.MemberServices.Add((string)m["service"]["rid"]);
                    area.Channels.Add(channel);
                }
            }
            return area;
        }
    }

    /// <summary>
    /// CLIP API v2 calls needed for streaming (HTTPS with the hue-application-key header).
    /// The bridge uses a certificate that Windows does not trust (self signed or signed by the
    /// Signify root CA); the validation callback accepts it only for requests to the configured
    /// bridge address and only per request, never globally.
    /// </summary>
    public static class HueClipV2
    {
        private const int TimeoutMs = 4000;

        public static List<EntertainmentArea> GetEntertainmentAreas()
        {
            JToken response = Send("GET", "/clip/v2/resource/entertainment_configuration", null);
            var result = new List<EntertainmentArea>();
            if (response["data"] is JArray data)
                foreach (JToken item in data)
                    result.Add(EntertainmentArea.FromJson(item));
            return result;
        }

        public static EntertainmentArea GetEntertainmentArea(string id)
        {
            JToken response = Send("GET", "/clip/v2/resource/entertainment_configuration/" + Uri.EscapeDataString(id), null);
            if (response["data"] is JArray data && data.Count > 0)
                return EntertainmentArea.FromJson(data[0]);
            throw new HueException(0, "Entertainment area not found.");
        }

        /// <summary>
        /// Lights that can take part in entertainment streaming (entertainment services with renderer = true).
        /// Plugs, switches and other devices without a color renderer are not included.
        /// </summary>
        public static List<StreamingLight> GetStreamingLights()
        {
            var devices = new Dictionary<string, JToken>();
            JToken deviceResponse = Send("GET", "/clip/v2/resource/device", null);
            if (deviceResponse["data"] is JArray deviceData)
                foreach (JToken d in deviceData)
                    if ((string)d["id"] != null)
                        devices[(string)d["id"]] = d;

            // Light resources by owner device: v2 light id and the v1 id ("/lights/3")
            var lightsByDevice = new Dictionary<string, JToken>();
            JToken lightResponse = Send("GET", "/clip/v2/resource/light", null);
            if (lightResponse["data"] is JArray lightData)
                foreach (JToken l in lightData)
                    if ((string)l["owner"]?["rid"] != null)
                        lightsByDevice[(string)l["owner"]["rid"]] = l;

            var result = new List<StreamingLight>();
            JToken response = Send("GET", "/clip/v2/resource/entertainment", null);
            if (response["data"] is JArray data)
            {
                foreach (JToken e in data)
                {
                    if ((bool?)e["renderer"] != true)
                        continue;
                    string owner = (string)e["owner"]?["rid"];
                    devices.TryGetValue(owner ?? "", out JToken device);
                    lightsByDevice.TryGetValue(owner ?? "", out JToken light);
                    string v1 = (string)light?["id_v1"];
                    result.Add(new StreamingLight
                    {
                        ServiceID = (string)e["id"],
                        LightID = (string)light?["id"],
                        V1ID = v1 != null && v1.StartsWith("/lights/", StringComparison.Ordinal) ? v1.Substring(8) : null,
                        Name = (string)device?["metadata"]?["name"] ?? (string)e["id"],
                        Product = (string)device?["product_data"]?["product_name"] ?? "",
                    });
                }
            }
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }

        /// <summary>Creates a screen entertainment area with the given lights and positions; returns its id.</summary>
        public static string CreateEntertainmentArea(string name, IList<KeyValuePair<StreamingLight, EntertainmentChannel>> lights)
        {
            JObject body = new JObject
            {
                ["type"] = "entertainment_configuration",
                ["metadata"] = new JObject { ["name"] = name },
                ["configuration_type"] = "screen",
                ["locations"] = new JObject { ["service_locations"] = BuildLocations(lights) },
            };
            JToken response = Send("POST", "/clip/v2/resource/entertainment_configuration", body.ToString(Newtonsoft.Json.Formatting.None));
            string id = response["data"] is JArray data && data.Count > 0 ? (string)data[0]["rid"] : null;
            if (string.IsNullOrEmpty(id))
                throw new HueException(0, "Hue bridge did not return the new entertainment area.");
            return id;
        }

        /// <summary>Replaces the lights and positions of an area (only while it is not streaming).</summary>
        public static void UpdateEntertainmentArea(string id, IList<KeyValuePair<StreamingLight, EntertainmentChannel>> lights)
        {
            JObject body = new JObject { ["locations"] = new JObject { ["service_locations"] = BuildLocations(lights) } };
            Send("PUT", "/clip/v2/resource/entertainment_configuration/" + Uri.EscapeDataString(id), body.ToString(Newtonsoft.Json.Formatting.None));
        }

        private static JArray BuildLocations(IList<KeyValuePair<StreamingLight, EntertainmentChannel>> lights)
        {
            var locations = new JArray();
            foreach (var entry in lights)
            {
                locations.Add(new JObject
                {
                    ["service"] = new JObject { ["rid"] = entry.Key.ServiceID, ["rtype"] = "entertainment" },
                    ["positions"] = new JArray(new JObject
                    {
                        ["x"] = Math.Round(entry.Value.X, 3),
                        ["y"] = Math.Round(entry.Value.Y, 3),
                        ["z"] = Math.Round(entry.Value.Z, 3),
                    }),
                });
            }
            return locations;
        }

        /// <summary>Switches a light (CLIP v2 light resource id) on or off.</summary>
        public static void SetLightOn(string lightId, bool on)
        {
            JObject body = new JObject { ["on"] = new JObject { ["on"] = on } };
            Send("PUT", "/clip/v2/resource/light/" + Uri.EscapeDataString(lightId), body.ToString(Newtonsoft.Json.Formatting.None));
        }

        /// <summary>Deletes an entertainment area on the bridge.</summary>
        public static void DeleteEntertainmentArea(string id)
        {
            Send("DELETE", "/clip/v2/resource/entertainment_configuration/" + Uri.EscapeDataString(id), null);
        }

        /// <summary>The bridge holds at most 20 entertainment areas; creating one more fails with this text.</summary>
        public static bool IsAreaLimitError(Exception ex)
        {
            return ex != null && ex.Message.IndexOf("Insufficient resources", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Starts (true) or stops (false) streaming mode of the area.</summary>
        public static void SetStreaming(string id, bool start)
        {
            JObject body = new JObject { ["action"] = start ? "start" : "stop" };
            Send("PUT", "/clip/v2/resource/entertainment_configuration/" + Uri.EscapeDataString(id),
                body.ToString(Newtonsoft.Json.Formatting.None));
        }

        internal static JToken Send(string method, string path, string body)
        {
            if (string.IsNullOrEmpty(Configuration.Username))
                throw new InvalidOperationException("Not paired with the Hue bridge.");
            return SendHttps(method, path, body, Configuration.Username);
        }

        /// <summary>HTTPS request without the application key (pairing: POST /api).</summary>
        internal static JToken SendUnauthenticated(string method, string path, string body)
        {
            return SendHttps(method, path, body, null);
        }

        /// <summary>
        /// HTTPS request to the bridge with the pinned certificate check. A bridge without HTTPS (first
        /// generation) fails with <see cref="WebExceptionStatus.ConnectFailure"/>, see <see cref="Configuration.IsNoHttps"/>.
        /// </summary>
        private static JToken SendHttps(string method, string path, string body, string applicationKey)
        {
            if (Configuration.DeviceIP == null)
                throw new InvalidOperationException("Hue bridge address is not set.");

            string host = Configuration.DeviceIP.ToString();
            if (Configuration.DeviceIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                host = "[" + host + "]";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://" + host + path);
            request.Method = method;
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            request.Proxy = null;
            if (applicationKey != null)
                request.Headers["hue-application-key"] = applicationKey;
            IPAddress bridge = Configuration.DeviceIP;
            request.ServerCertificateValidationCallback = (sender, cert, chain, errors) => AcceptBridgeCertificate(sender, bridge, cert, errors);

            if (body != null)
            {
                byte[] payload = Encoding.UTF8.GetBytes(body);
                request.ContentType = "application/json";
                request.ContentLength = payload.Length;
                using (Stream stream = request.GetRequestStream())
                    stream.Write(payload, 0, payload.Length);
            }

            string text;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    text = reader.ReadToEnd();
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse error)
            {
                int status = (int)error.StatusCode;    // read before the response is disposed
                using (error)
                using (StreamReader reader = new StreamReader(error.GetResponseStream(), Encoding.UTF8))
                    text = reader.ReadToEnd();
                ThrowOnErrors(SafeParse(text), status);
                throw new HueException(status, "Hue bridge: HTTP " + status + " " + (text.Length > 200 ? text.Substring(0, 200) : text));
            }

            JToken json = SafeParse(text);
            ThrowOnErrors(json, 0);
            return json;
        }

        /// <summary>
        /// The bridge certificate is not signed by a CA that Windows trusts, so policy errors are accepted,
        /// but only for the bridge IP and only when the subject CN is the stored bridge id. Without a stored
        /// id (pairings from older versions) the first certificate seen is trusted and its CN is stored.
        /// </summary>
        private static bool AcceptBridgeCertificate(object sender, IPAddress bridge, X509Certificate cert, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;
            // Only for the bridge IP, never for other hosts (e.g. redirects)
            if (!(sender is HttpWebRequest req && IPAddress.TryParse(req.RequestUri.Host.Trim('[', ']'), out IPAddress host)
                  && host.Equals(bridge)))
                return false;

            string cn = CertificateCommonName(cert);
            string stored = Configuration.BridgeId;
            if (!IsTrustedBridgeCertificate(stored, cn))
            {
                CGlobals.LogEvent("Hue bridge " + bridge + ": certificate CN '" + cn + "' is not the paired bridge id "
                    + stored + ", connection refused. Pair the bridge again if it was replaced.");
                return false;
            }
            if (stored == null && Configuration.LearnBridgeId(cn))
                CGlobals.LogEvent("Hue bridge " + bridge + ": bridge id " + Configuration.BridgeId + " stored (first connection).");
            return true;
        }

        /// <summary>
        /// True when a bridge certificate with this subject CN may be accepted: it must equal the stored bridge
        /// id; without a stored id any certificate is accepted (trust on first use).
        /// </summary>
        internal static bool IsTrustedBridgeCertificate(string storedBridgeId, string certificateCn)
        {
            string stored = Configuration.NormalizeBridgeId(storedBridgeId);
            if (stored == null)
                return true;
            return stored == Configuration.NormalizeBridgeId(certificateCn);
        }

        private static string CertificateCommonName(X509Certificate cert)
        {
            if (cert == null)
                return null;
            try
            {
                using (var cert2 = new X509Certificate2(cert))
                    return cert2.GetNameInfo(X509NameType.SimpleName, false);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return null;
            }
        }

        private static JToken SafeParse(string text)
        {
            try
            {
                return JToken.Parse(string.IsNullOrEmpty(text) ? "{}" : text);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return new JObject();
            }
        }

        private static void ThrowOnErrors(JToken json, int status)
        {
            if (json is JObject obj && obj["errors"] is JArray errors && errors.Count > 0)
                throw new HueException(status, "Hue bridge: " + (string)errors[0]["description"]);
        }
    }

    /// <summary>
    /// Keeps the Hue client key out of user.config in plain text: DPAPI, bound to the Windows user.
    /// </summary>
    public static class HueSecrets
    {
        private const string Prefix = "dpapi:";
        private static readonly byte[] Entropy = Encoding.ASCII.GetBytes("AmbiBack.PC.HueClientKey");

        public static string Protect(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return "";
            byte[] data = System.Security.Cryptography.ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), Entropy,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(data);
        }

        /// <summary>Returns null when empty or not readable (e.g. settings copied from another user).</summary>
        public static string Unprotect(string stored)
        {
            if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal))
                return null;
            try
            {
                byte[] data = System.Security.Cryptography.ProtectedData.Unprotect(Convert.FromBase64String(stored.Substring(Prefix.Length)),
                    Entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException || ex is FormatException)
            {
                return null;
            }
        }
    }

    /// <summary>Builds HueStream API version 2 messages.</summary>
    public static class HueStreamMessage
    {
        public const int HeaderSize = 52;
        public const int ChannelSize = 7;
        public const int MaxChannels = 20;
        private static readonly byte[] Protocol = Encoding.ASCII.GetBytes("HueStream");

        /// <summary>
        /// Header: "HueStream", version 2.0, sequence id, 2 reserved, color space (0 = RGB), 1 reserved,
        /// entertainment configuration id (36 ASCII chars); then per channel: id and 16 bit R, G, B (big endian).
        /// </summary>
        public static int Build(byte[] buffer, byte sequence, string configId, byte[] channelIds, ushort[] rgb16, int count)
        {
            if (configId == null || configId.Length != 36)
                throw new ArgumentException("Entertainment configuration id must be a 36 character UUID.", nameof(configId));
            if (count < 0 || count > MaxChannels || count > channelIds.Length || rgb16.Length < count * 3)
                throw new ArgumentOutOfRangeException(nameof(count));
            int size = HeaderSize + ChannelSize * count;
            if (buffer.Length < size)
                throw new ArgumentException("Buffer too small.", nameof(buffer));

            Buffer.BlockCopy(Protocol, 0, buffer, 0, 9);
            buffer[9] = 0x02;     // major version
            buffer[10] = 0x00;    // minor version
            buffer[11] = sequence;
            buffer[12] = 0x00;    // reserved
            buffer[13] = 0x00;
            buffer[14] = 0x00;    // color space RGB
            buffer[15] = 0x00;    // reserved
            Encoding.ASCII.GetBytes(configId.ToLowerInvariant(), 0, 36, buffer, 16);

            int p = HeaderSize;
            for (int i = 0; i < count; i++)
            {
                buffer[p++] = channelIds[i];
                for (int c = 0; c < 3; c++)
                {
                    ushort v = rgb16[i * 3 + c];
                    buffer[p++] = (byte)(v >> 8);
                    buffer[p++] = (byte)v;
                }
            }
            return size;
        }
    }
}
