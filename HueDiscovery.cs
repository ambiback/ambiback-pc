using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AmbiBack.PhilipsHue
{
    /// <summary>A Hue bridge found on the local network.</summary>
    public class DiscoveredBridge
    {
        public IPAddress Address { get; set; }
        public string Name { get; set; }
        public string BridgeID { get; set; }
        public string ModelID { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? Address.ToString() : Name + " (" + Address + ")";
        }
    }

    /// <summary>
    /// Finds Hue bridges: an mDNS query for "_hue._tcp.local" on every IPv4 interface, the ARP cache
    /// entries with a Philips/Signify MAC prefix and, when asked for or when nothing was found locally,
    /// the Hue discovery service (discovery.meethue.com, used by the Hue app as well; a plain GET without
    /// any data, it returns the local addresses of the bridges behind the same internet connection).
    /// Every candidate is checked locally with the public GET /api/0/config, which needs no user name.
    /// </summary>
    public static class HueDiscovery
    {
        private static readonly IPEndPoint MdnsGroup = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
        private static readonly string[] HueMacPrefixes = { "00-17-88", "ec-b5-fa" };

        private const string CloudDiscoveryUrl = "https://discovery.meethue.com/";

        /// <summary>Searches the network; with alwaysCloud the Hue discovery service is asked as well.</summary>
        public static List<DiscoveredBridge> Discover(bool alwaysCloud = false, int timeoutMs = 2500)
        {
            var candidates = new HashSet<IPAddress>();
            Task<List<IPAddress>> mdns = Task.Run(() => QueryMdns(timeoutMs));
            Task<List<IPAddress>> cloud = alwaysCloud ? Task.Run(() => CloudCandidates()) : null;
            foreach (IPAddress ip in ArpCandidates())
                candidates.Add(ip);
            foreach (IPAddress ip in mdns.Result)
                candidates.Add(ip);
            if (cloud == null && candidates.Count == 0)
                cloud = Task.Run(() => CloudCandidates());
            if (cloud != null)
                foreach (IPAddress ip in cloud.Result)
                    candidates.Add(ip);

            var checks = candidates.Select(ip => Task.Run(() => Verify(ip))).ToArray();
            Task.WaitAll(checks);
            return checks.Select(t => t.Result).Where(b => b != null)
                .GroupBy(b => b.BridgeID ?? b.Address.ToString()).Select(g => g.First())
                .OrderBy(b => b.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        /// <summary>DNS question for PTR &lt;service&gt;._tcp.local (default _hue) with the "unicast response" bit.</summary>
        internal static byte[] BuildMdnsQuery(string service = "_hue")
        {
            var b = new List<byte> { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 };   // id, flags, 1 question
            foreach (string label in new[] { service, "_tcp", "local" })
            {
                b.Add((byte)label.Length);
                b.AddRange(Encoding.ASCII.GetBytes(label));
            }
            b.Add(0);
            b.AddRange(new byte[] { 0x00, 0x0C, 0x80, 0x01 });   // type PTR, class IN with QU bit
            return b.ToArray();
        }

        /// <summary>Addresses that answered an mDNS query for &lt;service&gt;._tcp.local on any IPv4 interface.</summary>
        internal static List<IPAddress> QueryMdns(int timeoutMs, string service = "_hue")
        {
            var found = new List<IPAddress>();
            byte[] query = BuildMdnsQuery(service);
            var sockets = new List<Socket>();
            try
            {
                foreach (IPAddress local in LocalIPv4Addresses())
                {
                    Socket s = null;
                    try
                    {
                        s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        s.Bind(new IPEndPoint(local, 0));
                        s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, local.GetAddressBytes());
                        s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 255);
                        s.SendTo(query, MdnsGroup);
                        sockets.Add(s);
                    }
                    catch (SocketException ex)
                    {
                        // Not in the list yet, so the cleanup below would never close it
                        s?.Close();
                        CGlobals.DebugOutput("HueDiscovery|mDNS on " + local + ": " + ex.SocketErrorCode);
                    }
                }

                var clock = Stopwatch.StartNew();
                byte[] buffer = new byte[9000];
                while (clock.ElapsedMilliseconds < timeoutMs && sockets.Count > 0)
                {
                    var readable = new List<Socket>(sockets);
                    int waitUs = (int)Math.Max(1, timeoutMs - clock.ElapsedMilliseconds) * 1000;
                    Socket.Select(readable, null, null, waitUs);
                    foreach (Socket s in readable)
                    {
                        EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                        int n;
                        try { n = s.ReceiveFrom(buffer, ref from); }
                        catch (SocketException) { continue; }
                        // Any answer that mentions the service comes from such a device
                        if (n > 12 && Encoding.ASCII.GetString(buffer, 0, n).IndexOf(service, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            IPAddress ip = ((IPEndPoint)from).Address;
                            if (!found.Contains(ip))
                                found.Add(ip);
                        }
                    }
                }
            }
            finally
            {
                foreach (Socket s in sockets)
                    s.Close();
            }
            return found;
        }

        internal static IEnumerable<IPAddress> LocalIPv4Addresses()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || !ni.SupportsMulticast)
                    continue;
                foreach (UnicastIPAddressInformation a in ni.GetIPProperties().UnicastAddresses)
                    if (a.Address.AddressFamily == AddressFamily.InterNetwork)
                        yield return a.Address;
            }
        }

        /// <summary>IPv4 addresses in the ARP cache whose MAC address belongs to Philips Lighting / Signify.</summary>
        private static List<IPAddress> ArpCandidates()
        {
            var result = new List<IPAddress>();
            try
            {
                var psi = new ProcessStartInfo("arp", "-a")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };
                using (Process p = Process.Start(psi))
                {
                    string text = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    foreach (Match m in Regex.Matches(text, @"(\d+\.\d+\.\d+\.\d+)\s+([0-9a-fA-F]{2}(?:-[0-9a-fA-F]{2}){5})"))
                    {
                        string mac = m.Groups[2].Value.ToLowerInvariant();
                        if (HueMacPrefixes.Any(prefix => mac.StartsWith(prefix, StringComparison.Ordinal))
                            && IPAddress.TryParse(m.Groups[1].Value, out IPAddress ip))
                            result.Add(ip);
                    }
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException || ex is IOException)
            {
                CGlobals.DebugOutput("HueDiscovery|arp: " + ex.Message);
            }
            return result;
        }

        /// <summary>Local addresses reported by the Hue discovery service; empty on any error (it is rate limited).</summary>
        private static List<IPAddress> CloudCandidates()
        {
            var result = new List<IPAddress>();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(CloudDiscoveryUrl);
                request.Timeout = 4000;
                request.ReadWriteTimeout = 4000;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    if (JToken.Parse(reader.ReadToEnd()) is JArray list)
                        foreach (JToken entry in list)
                            if (IPAddress.TryParse((string)entry["internalipaddress"], out IPAddress ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                                result.Add(ip);
                }
            }
            catch (Exception ex) when (ex is WebException || ex is IOException || ex is Newtonsoft.Json.JsonException)
            {
                CGlobals.DebugOutput("HueDiscovery|cloud: " + ex.Message);
            }
            return result;
        }

        /// <summary>Asks the public config of a candidate; null when it is no Hue bridge.</summary>
        private static DiscoveredBridge Verify(IPAddress ip)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + ip + "/api/0/config");
                request.Timeout = 2000;
                request.ReadWriteTimeout = 2000;
                request.Proxy = null;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    JObject config = JObject.Parse(reader.ReadToEnd());
                    string bridgeId = (string)config["bridgeid"];
                    if (string.IsNullOrEmpty(bridgeId))
                        return null;
                    return new DiscoveredBridge
                    {
                        Address = ip,
                        Name = (string)config["name"],
                        BridgeID = bridgeId,
                        ModelID = (string)config["modelid"],
                    };
                }
            }
            catch (Exception ex) when (ex is WebException || ex is IOException || ex is Newtonsoft.Json.JsonException)
            {
                return null;
            }
        }
    }
}
