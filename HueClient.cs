using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace AmbiBack.PhilipsHue
{
    /// <summary>Error reported by a Philips Hue bridge.</summary>
    public class HueException : Exception
    {
        public int ErrorType { get; }

        public HueException(int errorType, string message) : base(message)
        {
            ErrorType = errorType;
        }
    }

    /// <summary>Connection settings and user registration for the Hue bridge.</summary>
    public static class Configuration
    {
        private const int MaxDeviceTypeLength = 40;

        public static IPAddress DeviceIP { get; set; }
        public static string AppID { get; set; }
        public static string Username { get; set; }
        /// <summary>PSK for the Entertainment API (32 hex characters), null when the bridge gave none. Secret: never log it.</summary>
        public static string ClientKey { get; set; }

        /// <summary>
        /// True when plain HTTP may be used if the bridge has no HTTPS (first generation bridge): while
        /// pairing, for bridges paired over HTTP, and for old pairings never confirmed over HTTPS.
        /// Once HTTPS worked for a pairing it is never given up, so a host that spoofs the bridge IP
        /// and closes port 443 cannot get the key over HTTP.
        /// </summary>
        public static bool AllowHttpFallback { get; set; }

        /// <summary>True after the bridge was reached over HTTP because it has no HTTPS.</summary>
        public static bool UsesHttp { get; set; }

        /// <summary>HTTPS port closed (connection refused): the bridge has no HTTPS. TLS errors do not count.</summary>
        public static bool IsNoHttps(WebExceptionStatus status)
        {
            return status == WebExceptionStatus.ConnectFailure;
        }

        /// <summary>HTTP fallback for a saved pairing: paired over HTTP, or never confirmed over HTTPS (no bridge id).</summary>
        public static bool HttpFallbackAllowed(bool pairedOverHttp, string storedBridgeId)
        {
            return pairedOverHttp || NormalizeBridgeId(storedBridgeId) == null;
        }

        private static string _bridgeId;

        /// <summary>
        /// Bridge id (16 lowercase hex characters, e.g. 001788fffe23bfc2), null while unknown. The bridge
        /// certificate must carry it as subject CN before the application key is sent over HTTPS.
        /// </summary>
        public static string BridgeId
        {
            get { return _bridgeId; }
            set { _bridgeId = NormalizeBridgeId(value); }
        }

        /// <summary>Lowercase 16-hex bridge id, or null for anything else.</summary>
        public static string NormalizeBridgeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            id = id.Trim().ToLowerInvariant();
            if (id.Length != 16)
                return null;
            foreach (char c in id)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    return null;
            }
            return id;
        }

        /// <summary>
        /// Sets the bridge id only when none is known yet (trust on first use).
        /// Returns true when this call stored it.
        /// </summary>
        internal static bool LearnBridgeId(string id)
        {
            string normalized = NormalizeBridgeId(id);
            return normalized != null
                && System.Threading.Interlocked.CompareExchange(ref _bridgeId, normalized, null) == null;
        }

        /// <summary>
        /// Registers this app. The link button on the bridge must be pressed first. Also requests the
        /// client key for entertainment streaming (bridges with API 1.22 and later return it).
        /// </summary>
        /// <returns>True when the bridge returned a username.</returns>
        public static bool RegisterNewUser()
        {
            string deviceType = AppID ?? "AmbiBack";
            if (deviceType.Length > MaxDeviceTypeLength)
                deviceType = deviceType.Substring(0, MaxDeviceTypeLength);

            JObject body = new JObject { ["devicetype"] = deviceType, ["generateclientkey"] = true };
            string json = body.ToString(Newtonsoft.Json.Formatting.None);
            JToken response;
            try
            {
                // HTTPS: the reply carries the new user name and client key
                response = HueClipV2.SendUnauthenticated("POST", "/api", json);
            }
            catch (WebException ex) when (AllowHttpFallback && IsNoHttps(ex.Status))
            {
                CGlobals.LogEvent("Hue bridge " + DeviceIP + ": no HTTPS (first generation bridge?), pairing over HTTP.");
                UsesHttp = true;
                response = HueHttp.Send("POST", "/api", json);
            }
            HueHttp.ThrowOnError(response);

            JToken success = response is JArray items && items.Count > 0 ? items[0]["success"] : null;
            string username = (string)success?["username"];
            if (string.IsNullOrEmpty(username))
                return false;

            Username = username;
            ClientKey = (string)success["clientkey"];
            return true;
        }
    }

    /// <summary>One light known to the bridge (colors go only through entertainment streaming).</summary>
    public class Light
    {
        public string ID { get; internal set; }
        public string Name { get; internal set; }
        public string Type { get; internal set; }
        public string ModelID { get; internal set; }
        public string UniqueID { get; internal set; }
        /// <summary>CLIP v2 light resource id.</summary>
        public string ClipID { get; internal set; }
        /// <summary>Zigbee MAC address of the device (lower case), null for lights without Zigbee.</summary>
        public string MacAddress { get; internal set; }

        /// <summary>True for lights with a color gamut ("Extended color light", "Color light").</summary>
        public bool IsColorLight
        {
            get { return Type != null && Type.IndexOf("color light", StringComparison.OrdinalIgnoreCase) >= 0; }
        }

    }

    /// <summary>
    /// Loads the lights from the bridge once on creation, over CLIP API v2 (HTTPS with the pinned bridge
    /// certificate, see <see cref="HueClipV2"/>), so the application key never goes over plain HTTP.
    /// </summary>
    public class LightCollection : IEnumerable<Light>
    {
        private readonly List<Light> _lights;

        public LightCollection()
        {
            try
            {
                _lights = Parse(HueClipV2.Send("GET", "/clip/v2/resource/light", null),
                    HueClipV2.Send("GET", "/clip/v2/resource/device", null),
                    HueClipV2.Send("GET", "/clip/v2/resource/zigbee_connectivity", null));
            }
            catch (WebException ex) when (Configuration.AllowHttpFallback && Configuration.IsNoHttps(ex.Status))
            {
                CGlobals.LogEvent("Hue bridge " + Configuration.DeviceIP + ": no HTTPS (first generation bridge?), light list over HTTP.");
                Configuration.UsesHttp = true;
                _lights = LoadV1();
            }
        }

        /// <summary>Old API over plain HTTP, only for bridges without HTTPS (see <see cref="Configuration.AllowHttpFallback"/>).</summary>
        private static List<Light> LoadV1()
        {
            string path = "/api/" + Uri.EscapeDataString(Configuration.Username ?? "") + "/lights";
            JToken response = HueHttp.Send("GET", path, null);
            HueHttp.ThrowOnError(response);

            if (!(response is JObject lights))
                throw new HueException(0, "Unexpected response from Hue bridge.");

            var result = new List<Light>();
            foreach (JProperty entry in lights.Properties())
            {
                result.Add(new Light
                {
                    ID = entry.Name,
                    Name = (string)entry.Value["name"],
                    Type = (string)entry.Value["type"],
                    ModelID = (string)entry.Value["modelid"],
                    UniqueID = (string)entry.Value["uniqueid"],
                });
            }
            return result;
        }

        internal LightCollection(List<Light> lights)
        {
            _lights = lights;
        }

        /// <summary>
        /// Builds the light list from the CLIP v2 light, device and zigbee_connectivity resources.
        /// Name, type and model are filled in like the old API reported them, because they are stored in the
        /// device XML. UniqueID is the Zigbee MAC address of the device (the old API added an endpoint,
        /// e.g. "-0b"; <see cref="Find"/> matches both forms).
        /// </summary>
        internal static List<Light> Parse(JToken lightResponse, JToken deviceResponse, JToken zigbeeResponse)
        {
            var devices = new Dictionary<string, JToken>();
            if (deviceResponse?["data"] is JArray deviceData)
                foreach (JToken d in deviceData)
                    if ((string)d["id"] != null)
                        devices[(string)d["id"]] = d;

            var macByDevice = new Dictionary<string, string>();
            if (zigbeeResponse?["data"] is JArray zigbeeData)
                foreach (JToken z in zigbeeData)
                {
                    string owner = (string)z["owner"]?["rid"];
                    string mac = (string)z["mac_address"];
                    if (owner != null && !string.IsNullOrEmpty(mac))
                        macByDevice[owner] = mac.ToLowerInvariant();
                }

            var lights = new List<Light>();
            if (!(lightResponse?["data"] is JArray lightData))
                throw new HueException(0, "Unexpected response from Hue bridge.");

            var lightsPerDevice = new Dictionary<string, int>();
            foreach (JToken l in lightData)
            {
                string owner = (string)l["owner"]?["rid"] ?? "";
                lightsPerDevice.TryGetValue(owner, out int count);
                lightsPerDevice[owner] = count + 1;
            }

            foreach (JToken l in lightData)
            {
                string id = (string)l["id"];
                if (id == null)
                    continue;
                string owner = (string)l["owner"]?["rid"] ?? "";
                devices.TryGetValue(owner, out JToken device);
                macByDevice.TryGetValue(owner, out string mac);
                string v1 = (string)l["id_v1"];
                v1 = v1 != null && v1.StartsWith("/lights/", StringComparison.Ordinal) ? v1.Substring(8) : null;

                string uniqueId;
                if (mac == null)
                    uniqueId = "clip:" + id;                      // no Zigbee light (e.g. Matter): the v2 id
                else if (lightsPerDevice[owner] == 1)
                    uniqueId = mac;
                else
                    uniqueId = mac + "#" + (v1 ?? id);            // several lights in one device

                lights.Add(new Light
                {
                    ID = v1,
                    ClipID = id,
                    MacAddress = mac,
                    Name = (string)l["metadata"]?["name"] ?? (string)device?["metadata"]?["name"] ?? id,
                    Type = V1Type(l),
                    ModelID = (string)device?["product_data"]?["model_id"],
                    UniqueID = uniqueId,
                });
            }
            return lights;
        }

        /// <summary>Light type names of the old API, derived from the capabilities of the v2 light.</summary>
        private static string V1Type(JToken light)
        {
            bool color = light["color"] != null;
            bool temperature = light["color_temperature"] != null;
            if (color)
                return temperature ? "Extended color light" : "Color light";
            if (temperature)
                return "Color temperature light";
            return light["dimming"] != null ? "Dimmable light" : "On/Off light";
        }

        /// <summary>
        /// Light for a stored HueUniqueID: the same id, otherwise (devices saved with the old API id
        /// "00:17:88:01:xx:xx:xx:xx-0b") the only light with that MAC address.
        /// </summary>
        public Light Find(string storedUniqueId)
        {
            if (string.IsNullOrEmpty(storedUniqueId))
                return null;
            foreach (Light light in _lights)
                if (string.Equals(light.UniqueID, storedUniqueId, StringComparison.OrdinalIgnoreCase))
                    return light;

            string mac = storedUniqueId;
            int cut = mac.IndexOfAny(new[] { '-', '#' });
            if (cut > 0)
                mac = mac.Substring(0, cut);
            Light found = null;
            foreach (Light light in _lights)
            {
                if (light.MacAddress != null && string.Equals(light.MacAddress, mac, StringComparison.OrdinalIgnoreCase))
                {
                    if (found != null)
                        return null;                              // ambiguous: several lights in one device
                    found = light;
                }
            }
            return found;
        }

        public IEnumerator<Light> GetEnumerator()
        {
            return _lights.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal static class HueHttp
    {
        private const int TimeoutMs = 3000;

        internal static JToken Send(string method, string path, string body)
        {
            if (Configuration.DeviceIP == null)
                throw new InvalidOperationException("Hue bridge address is not set.");

            string host = Configuration.DeviceIP.ToString();
            if (Configuration.DeviceIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                host = "[" + host + "]";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + host + path);
            request.Method = method;
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            request.Proxy = null;

            if (body != null)
            {
                byte[] payload = Encoding.UTF8.GetBytes(body);
                request.ContentType = "application/json";
                request.ContentLength = payload.Length;
                using (Stream stream = request.GetRequestStream())
                    stream.Write(payload, 0, payload.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                return JToken.Parse(reader.ReadToEnd());
        }

        internal static JToken FirstError(JToken response)
        {
            if (response is JArray items)
            {
                foreach (JToken item in items)
                {
                    if (item is JObject entry && entry["error"] != null)
                        return entry["error"];
                }
            }
            return null;
        }

        internal static void ThrowOnError(JToken response)
        {
            JToken error = FirstError(response);
            if (error != null)
                throw new HueException((int?)error["type"] ?? 0, (string)error["description"] ?? "Hue bridge error");
        }
    }
}
