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

        /// <summary>Registers this app. The link button on the bridge must be pressed first.</summary>
        /// <returns>True when the bridge returned a username.</returns>
        public static bool RegisterNewUser()
        {
            string deviceType = AppID ?? "AmbiBack";
            if (deviceType.Length > MaxDeviceTypeLength)
                deviceType = deviceType.Substring(0, MaxDeviceTypeLength);

            JObject body = new JObject { ["devicetype"] = deviceType };
            JToken response = HueHttp.Send("POST", "/api", body.ToString(Newtonsoft.Json.Formatting.None));
            HueHttp.ThrowOnError(response);

            string username = response is JArray items && items.Count > 0
                ? (string)items[0]["success"]?["username"]
                : null;
            if (string.IsNullOrEmpty(username))
                return false;

            Username = username;
            return true;
        }
    }

    /// <summary>Collects the values for one light state update.</summary>
    public class LightStateBuilder
    {
        private const int MinBrightness = 1;
        private const int MaxBrightness = 254;

        private readonly JObject _state = new JObject();

        internal string ToJson()
        {
            return _state.ToString(Newtonsoft.Json.Formatting.None);
        }

        public LightStateBuilder TurnOn()
        {
            _state["on"] = true;
            return this;
        }

        public LightStateBuilder TurnOff()
        {
            _state["on"] = false;
            return this;
        }

        /// <summary>Sets the transition time in steps of 100 ms.</summary>
        public LightStateBuilder TransitionTime(ushort steps)
        {
            _state["transitiontime"] = steps;
            return this;
        }

        /// <summary>Sets the color temperature in mired.</summary>
        public LightStateBuilder ColorTemperature(ushort mired)
        {
            _state["ct"] = mired;
            return this;
        }

        public LightStateBuilder Brightness(byte value)
        {
            _state["bri"] = Math.Max(MinBrightness, Math.Min(MaxBrightness, (int)value));
            return this;
        }

        public LightStateBuilder Color(Color color)
        {
            double[] xy = RgbToXy(color);
            _state["xy"] = new JArray(Math.Round(xy[0], 4), Math.Round(xy[1], 4));
            return this;
        }

        /// <summary>Converts sRGB to CIE xy. Black maps to the D65 white point.</summary>
        internal static double[] RgbToXy(Color color)
        {
            double red = ToLinear(color.R / 255.0);
            double green = ToLinear(color.G / 255.0);
            double blue = ToLinear(color.B / 255.0);

            double x = red * 0.4124 + green * 0.3576 + blue * 0.1805;
            double y = red * 0.2126 + green * 0.7152 + blue * 0.0722;
            double z = red * 0.0193 + green * 0.1192 + blue * 0.9505;
            double sum = x + y + z;

            return sum <= 0 ? new[] { 0.3127, 0.3290 } : new[] { x / sum, y / sum };
        }

        private static double ToLinear(double channel)
        {
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
    }

    /// <summary>One light known to the bridge.</summary>
    public class Light
    {
        public string ID { get; internal set; }
        public string Name { get; internal set; }
        public string Type { get; internal set; }
        public string ModelID { get; internal set; }
        public string UniqueID { get; internal set; }

        public void SetState(LightStateBuilder state)
        {
            string path = "/api/" + Uri.EscapeDataString(Configuration.Username ?? "") + "/lights/"
                + Uri.EscapeDataString(ID) + "/state";
            JToken response = HueHttp.Send("PUT", path, state.ToJson());

            // A light that is off or unreachable reports an error for every frame, so keep running.
            if (HueHttp.FirstError(response) is JToken error)
                CGlobals.DebugOutput("Hue|SetState|" + ID + "|" + (string)error["description"]);
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
