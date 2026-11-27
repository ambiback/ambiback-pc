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
            JToken response = HueHttp.Send("POST", "/api", body.ToString(Newtonsoft.Json.Formatting.None));
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

        /// <summary>True for lights with a color gamut ("Extended color light", "Color light").</summary>
        public bool IsColorLight
        {
            get { return Type != null && Type.IndexOf("color light", StringComparison.OrdinalIgnoreCase) >= 0; }
        }

    }

    /// <summary>Loads the lights from the bridge once on creation.</summary>
    public class LightCollection : IEnumerable<Light>
    {
        private readonly List<Light> _lights = new List<Light>();

        public LightCollection()
        {
            string path = "/api/" + Uri.EscapeDataString(Configuration.Username ?? "") + "/lights";
            JToken response = HueHttp.Send("GET", path, null);
            HueHttp.ThrowOnError(response);

            if (!(response is JObject lights))
                throw new HueException(0, "Unexpected response from Hue bridge.");

            foreach (JProperty entry in lights.Properties())
            {
                _lights.Add(new Light
                {
                    ID = entry.Name,
                    Name = (string)entry.Value["name"],
                    Type = (string)entry.Value["type"],
                    ModelID = (string)entry.Value["modelid"],
                    UniqueID = (string)entry.Value["uniqueid"],
                });
            }
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
