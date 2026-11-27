using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AmbiBack.PhilipsHue
{
    /// <summary>
    /// Self test of the Hue streaming building blocks without a bridge. Run with
    /// "AmbiBack.exe --hue-selftest [report.txt]". "--hue-dtls-interop host port identity pskhex [report.txt]"
    /// runs a handshake against any DTLS 1.2 PSK server (e.g. openssl s_server) and sends a few frames.
    /// </summary>
    public static class HueSelfTest
    {
        public static bool Run(StringBuilder report)
        {
            var tests = new List<KeyValuePair<string, Action>>
            {
                Test("HMAC-SHA256 RFC 4231 case 1", () => Hmac("0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b", Encoding.ASCII.GetBytes("Hi There"),
                    "b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7")),
                Test("HMAC-SHA256 RFC 4231 case 2", () => Hmac(TlsPrf.ToHex(Encoding.ASCII.GetBytes("Jefe")), Encoding.ASCII.GetBytes("what do ya want for nothing?"),
                    "5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843")),
                Test("TLS 1.2 PRF SHA-256 (IETF TLS WG test vector)", TestPrf),
                Test("AES-GCM NIST/McGrew-Viega case 1 (empty)", () => Gcm("00000000000000000000000000000000", "000000000000000000000000", "", "", "", "58e2fccefa7e3061367f1d57a4e7455a")),
                Test("AES-GCM NIST/McGrew-Viega case 2", () => Gcm("00000000000000000000000000000000", "000000000000000000000000",
                    "00000000000000000000000000000000", "", "0388dace60b6a392f328c2b971b2fe78", "ab6e47d42cec13bdf53a67b21257bddf")),
                Test("AES-GCM NIST/McGrew-Viega case 3", () => Gcm("feffe9928665731c6d6a8f9467308308", "cafebabefacedbaddecaf888",
                    "d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b391aafd255", "",
                    "42831ec2217774244b7221b784d0d49ce3aa212f2c02a4e035c17e2329aca12e21d514b25466931c7d8f6a5aac84aa051ba30b396a0aac973d58e091473f5985",
                    "4d5c2af327cd64a62cf35abd2ba6fab4")),
                Test("AES-GCM NIST/McGrew-Viega case 4 (with AAD)", () => Gcm("feffe9928665731c6d6a8f9467308308", "cafebabefacedbaddecaf888",
                    "d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b39",
                    "feedfacedeadbeeffeedfacedeadbeefabaddad2",
                    "42831ec2217774244b7221b784d0d49ce3aa212f2c02a4e035c17e2329aca12e21d514b25466931c7d8f6a5aac84aa051ba30b396a0aac973d58e091",
                    "5bc94fbc3221a5db94fae95ae7121a47")),
                Test("PSK premaster secret layout (RFC 4279)", TestPremaster),
                Test("Finished verify_data length and label separation", TestFinished),
                Test("HueStream v2 message layout", TestHueStream),
                Test("Channel position to screen region", TestScanRectangle),
                Test("Channel list round trip", TestChannelFormat),
                Test("Client key DPAPI round trip", TestSecrets),
                Test("Bridge certificate: CN matches the stored bridge id", TestBridgeCertMatch),
                Test("Bridge certificate: CN mismatch is refused", TestBridgeCertMismatch),
                Test("Bridge certificate: no stored id keeps working (trust on first use)", TestBridgeCertNoStoredId),
                Test("Entertainment device XML round trip", TestDeviceXml),
                Test("New area: light positions", TestNewAreaPositions),
                Test("Only color lights are listed", TestColorLight),
                Test("Light position from screen sectors", TestPositionFromSectors),
                Test("mDNS query for _hue._tcp.local", TestMdnsQuery),
                Test("Light assistant: automatic places", TestAutoPlaces),
                Test("Hue color pipeline: value gain and minimum brightness", TestLightSettings),
                Test("WLED: DDP packet layout", TestDdpPacket),
                Test("WLED: DDP split of long strips", TestDdpSplit),
                Test("WLED: /json/info parsing", TestWledInfo),
                Test("WLED: device XML round trip", TestWledXml),
                Test("WLED: layout for the reported LED count", TestWledLayout),
                Test("Strip regions: AmbiBack controller and WLED match", TestStripRectangles),
                Test("Govee: razer on/off packets", TestGoveeRazerEnable),
                Test("Govee: razer segment colors", TestGoveeRazerColors),
                Test("Govee: LAN commands as JSON", TestGoveeCommands),
                Test("Govee: scan answer parsing", TestGoveeScan),
                Test("Govee: device XML round trip", TestGoveeXml),
                Test("OpenRGB: packet header", TestOpenRgbHeader),
                Test("OpenRGB: UPDATELEDS layout", TestOpenRgbUpdateLeds),
                Test("OpenRGB: controller data, protocol 4 and 0", TestOpenRgbControllerData),
                Test("OpenRGB: device XML round trip", TestOpenRgbXml),
                Test("Nanoleaf: extControl v2 frame", TestNanoleafFrame),
                Test("Nanoleaf: device info and panel layout", TestNanoleafInfo),
                Test("Nanoleaf: panel places on the screen", TestNanoleafPlaces),
                Test("Nanoleaf: device XML round trip (token protected)", TestNanoleafXml),
                Test("LIFX: message header", TestLifxHeader),
                Test("LIFX: RGB to HSBK", TestLifxHsbk),
                Test("LIFX: SetColor and SetExtendedColorZones payloads", TestLifxPayloads),
                Test("LIFX: device XML round trip", TestLifxXml),
                Test("mDNS query for _wled._tcp.local", () => Equal("000000000001000000000000" + "055f776c6564" + "045f746370" + "056c6f63616c" + "00" + "000c8001",
                    TlsPrf.ToHex(HueDiscovery.BuildMdnsQuery("_wled")))),
            };

            int failed = 0;
            foreach (var t in tests)
            {
                try
                {
                    t.Value();
                    report.AppendLine("PASS  " + t.Key);
                }
                catch (Exception ex)
                {
                    failed++;
                    report.AppendLine("FAIL  " + t.Key + ": " + ex.Message);
                }
            }
            report.AppendLine(failed == 0 ? "All " + tests.Count + " tests passed." : failed + " of " + tests.Count + " tests failed.");
            return failed == 0;
        }

        /// <summary>Handshake and a few HueStream frames against a DTLS 1.2 PSK server.</summary>
        public static bool RunInterop(string host, int port, string identity, string pskHex, StringBuilder report)
        {
            try
            {
                IPAddress ip = Dns.GetHostAddresses(host)[0];
                using (var client = new HueDtlsClient(new IPEndPoint(ip, port), identity, TlsPrf.FromHex(pskHex)))
                {
                    client.Log = s => report.AppendLine(s);
                    client.Connect(10000);
                    report.AppendLine("PASS  handshake");

                    byte[] buffer = new byte[HueStreamMessage.HeaderSize + HueStreamMessage.ChannelSize * 2];
                    byte[] ids = { 0, 1 };
                    for (int i = 0; i < 5; i++)
                    {
                        ushort v = (ushort)(i * 0x3333);
                        int size = HueStreamMessage.Build(buffer, (byte)i, "00000000-0000-0000-0000-000000000000", ids,
                            new ushort[] { v, 0, 0, 0, v, 0 }, 2);
                        client.Send(buffer, 0, size);
                        Thread.Sleep(40);
                    }
                    if (!client.Poll())
                        throw new Exception("connection closed by peer");
                    report.AppendLine("PASS  5 frames sent");
                    client.Close();
                    report.AppendLine("PASS  close_notify sent");
                }
                return true;
            }
            catch (Exception ex)
            {
                report.AppendLine("FAIL  " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Test against a real bridge: pairing (link button, up to 90 s) unless credFile already holds
        /// DPAPI protected credentials, list areas, stream a color cycle to one area, stop.
        /// Never prints the user name or client key.
        /// </summary>
        public static bool RunBridgeTest(string ip, string credFile, int areaIndex, int seconds, bool simulateLoss, Action<string> output)
        {
            try
            {
                Configuration.DeviceIP = IPAddress.Parse(ip);
                Configuration.AppID = "AmbiBack.PC#selftest";
                string[] cred = System.IO.File.Exists(credFile) ? (HueSecrets.Unprotect(System.IO.File.ReadAllText(credFile)) ?? "").Split('\n') : new string[0];
                if (cred.Length == 2)
                {
                    Configuration.Username = cred[0];
                    Configuration.ClientKey = cred[1];
                    output("using stored credentials");
                }
                else
                {
                    output("PRESS THE LINK BUTTON NOW (waiting up to 90 s)");
                    var wait = System.Diagnostics.Stopwatch.StartNew();
                    while (true)
                    {
                        try
                        {
                            if (Configuration.RegisterNewUser())
                                break;
                        }
                        catch (HueException ex) when (ex.ErrorType == 101)
                        {
                            // link button not pressed yet
                        }
                        if (wait.ElapsedMilliseconds > 90000)
                            throw new Exception("link button was not pressed within 90 s");
                        Thread.Sleep(2000);
                    }
                    if (string.IsNullOrEmpty(Configuration.ClientKey))
                        throw new Exception("bridge returned no client key");
                    System.IO.File.WriteAllText(credFile, HueSecrets.Protect(Configuration.Username + "\n" + Configuration.ClientKey));
                    output("PASS  paired, client key received (" + Configuration.ClientKey.Length + " hex chars), stored DPAPI protected");
                }

                List<EntertainmentArea> areas = HueClipV2.GetEntertainmentAreas();
                output("PASS  CLIP v2: " + areas.Count + " entertainment area(s)");
                for (int i = 0; i < areas.Count; i++)
                {
                    output(string.Format("      [{0}] {1}: {2} channels, type {3}, status {4}", i, areas[i].Name, areas[i].Channels.Count, areas[i].ConfigurationType, areas[i].Status));
                    foreach (EntertainmentChannel ch in areas[i].Channels)
                        output("          channel " + ch);
                }
                if (areas.Count == 0)
                    throw new Exception("no entertainment area on the bridge");
                EntertainmentArea area = areas[Math.Min(areaIndex, areas.Count - 1)];

                using (var session = new HueEntertainmentSession(area.ID, output))
                {
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    while (!session.EnsureConnected())
                    {
                        if (clock.ElapsedMilliseconds > 20000)
                            throw new Exception("could not connect: " + session.LastError);
                        Thread.Sleep(200);
                    }
                    output("PASS  streaming connected after " + clock.ElapsedMilliseconds + " ms");
                    output("      bridge status now: " + HueClipV2.GetEntertainmentArea(area.ID).Status);

                    int n = Math.Min(session.Area.Channels.Count, HueStreamMessage.MaxChannels);
                    byte[] ids = new byte[n];
                    for (int i = 0; i < n; i++)
                        ids[i] = session.Area.Channels[i].ID;
                    ushort[] rgb = new ushort[n * 3];
                    int frames = 0;
                    clock.Restart();
                    bool interrupted = false;
                    int reconnects = 0;
                    bool wasStreaming = true;
                    while (clock.ElapsedMilliseconds < seconds * 1000)
                    {
                        // Halfway: end streaming mode from outside (like another app or a bridge reset)
                        if (simulateLoss && !interrupted && clock.ElapsedMilliseconds > seconds * 500)
                        {
                            interrupted = true;
                            HueClipV2.SetStreaming(area.ID, false);
                            output("      streaming stopped from outside at " + clock.ElapsedMilliseconds + " ms");
                        }
                        bool streaming = session.EnsureConnected();
                        if (streaming && !wasStreaming)
                        {
                            reconnects++;
                            output("PASS  reconnected at " + clock.ElapsedMilliseconds + " ms");
                        }
                        wasStreaming = streaming;
                        if (!streaming)
                        {
                            if (!simulateLoss)
                                throw new Exception("connection lost while streaming: " + session.LastError);
                            Thread.Sleep(100);
                            continue;
                        }
                        // Hue wheel, channels phase shifted, one turn every 5 s
                        double t = clock.ElapsedMilliseconds / 5000.0;
                        for (int i = 0; i < n; i++)
                        {
                            Color c = Wheel((t + (double)i / Math.Max(1, n)) % 1.0);
                            rgb[i * 3] = (ushort)(c.R * 257);
                            rgb[i * 3 + 1] = (ushort)(c.G * 257);
                            rgb[i * 3 + 2] = (ushort)(c.B * 257);
                        }
                        session.Send(ids, rgb, n);
                        frames++;
                        if (frames == 50)
                        {
                            foreach (string light in session.Area.LightServices)
                            {
                                bool? on = (bool?)HueClipV2.Send("GET", "/clip/v2/resource/light/" + light, null)["data"]?[0]?["on"]?["on"];
                                output("      while streaming: light " + light.Substring(0, 8) + "… on = " + on);
                            }
                        }
                        Thread.Sleep(20);
                    }
                    output(string.Format("PASS  {0} frames in {1} s ({2:0.0} fps), state {3}", frames, seconds, frames / (double)seconds, session.State));
                    if (simulateLoss && reconnects == 0)
                        throw new Exception("no reconnect after the simulated loss");
                    session.Stop();
                }
                Thread.Sleep(500);
                output("PASS  stopped, bridge status now: " + HueClipV2.GetEntertainmentArea(area.ID).Status);
                foreach (string light in HueClipV2.GetEntertainmentArea(area.ID).LightServices)
                {
                    bool? on = (bool?)HueClipV2.Send("GET", "/clip/v2/resource/light/" + light, null)["data"]?[0]?["on"]?["on"];
                    output((on == false ? "PASS" : "FAIL") + "  light " + light.Substring(0, 8) + "… on = " + on);
                }
                return true;
            }
            catch (Exception ex)
            {
                output("FAIL  " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static Color Wheel(double h)
        {
            double r = Math.Max(0, Math.Min(1, Math.Abs(h * 6 - 3) - 1));
            double g = Math.Max(0, Math.Min(1, 2 - Math.Abs(h * 6 - 2)));
            double b = Math.Max(0, Math.Min(1, 2 - Math.Abs(h * 6 - 4)));
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private static KeyValuePair<string, Action> Test(string name, Action body)
        {
            return new KeyValuePair<string, Action>(name, body);
        }

        private static void Hmac(string keyHex, byte[] data, string expected)
        {
            using (var h = new HMACSHA256(TlsPrf.FromHex(keyHex)))
                Equal(expected, TlsPrf.ToHex(h.ComputeHash(data)));
        }

        private static void TestPrf()
        {
            byte[] secret = TlsPrf.FromHex("9bbe436ba940f017b17652849a71db35");
            byte[] seed = TlsPrf.FromHex("a0ba9f936cda311827a6f796ffd5198c");
            byte[] output = TlsPrf.Compute(secret, "test label", seed, 100);
            Equal("e3f229ba727be17b8d122620557cd453c2aab21d07c3d495329b52d4e61edb5a6b301791e90d35c9c9a46b4e14baf9af0fa022f7077def17abfd3797c0564bab"
                + "4fbc91666e9def9b97fce34f796789baa48082d122ee42c5a72e5a5110fff70187347b66", TlsPrf.ToHex(output));
        }

        private static void Gcm(string keyHex, string ivHex, string ptHex, string aadHex, string ctHex, string tagHex)
        {
            byte[] key = TlsPrf.FromHex(keyHex), iv = TlsPrf.FromHex(ivHex), pt = TlsPrf.FromHex(ptHex);
            byte[] aad = aadHex.Length == 0 ? null : TlsPrf.FromHex(aadHex);
            byte[] ct = new byte[pt.Length], tag = new byte[16], back = new byte[pt.Length];
            using (var gcm = new AesGcmCng(key))
            {
                gcm.Encrypt(iv, pt, 0, pt.Length, ct, 0, tag, 0, aad);
                Equal(ctHex, TlsPrf.ToHex(ct));
                Equal(tagHex, TlsPrf.ToHex(tag));
                True(gcm.Decrypt(iv, ct, 0, ct.Length, tag, 0, back, 0, aad), "decrypt rejected a valid tag");
                Equal(ptHex, TlsPrf.ToHex(back));
                tag[0] ^= 1;
                True(!gcm.Decrypt(iv, ct, 0, ct.Length, tag, 0, back, 0, aad), "decrypt accepted a modified tag");
                if (ct.Length > 0)
                {
                    tag[0] ^= 1;
                    ct[ct.Length - 1] ^= 0x80;
                    True(!gcm.Decrypt(iv, ct, 0, ct.Length, tag, 0, back, 0, aad), "decrypt accepted modified ciphertext");
                }
            }
        }

        private static void TestPremaster()
        {
            byte[] pms = TlsPrf.PskPremaster(TlsPrf.FromHex("00112233445566778899aabbccddeeff"));
            Equal("0010" + "00000000000000000000000000000000" + "0010" + "00112233445566778899aabbccddeeff", TlsPrf.ToHex(pms));
        }

        private static void TestFinished()
        {
            byte[] master = new byte[48];
            byte[] transcript = Encoding.ASCII.GetBytes("handshake messages");
            byte[] client = HueDtlsClient.FinishedVerifyData(master, "client finished", transcript);
            byte[] server = HueDtlsClient.FinishedVerifyData(master, "server finished", transcript);
            True(client.Length == 12 && server.Length == 12, "verify_data must be 12 bytes");
            True(TlsPrf.ToHex(client) != TlsPrf.ToHex(server), "labels must give different values");
            byte[] hash;
            using (var sha = SHA256.Create())
                hash = sha.ComputeHash(transcript);
            Equal(TlsPrf.ToHex(TlsPrf.Compute(master, "client finished", hash, 12)), TlsPrf.ToHex(client));
        }

        private static void TestHueStream()
        {
            string id = "1A8D99CC-967B-44F2-9202-43F976C0FA6B";
            byte[] buffer = new byte[200];
            int size = HueStreamMessage.Build(buffer, 7, id, new byte[] { 0, 5 }, new ushort[] { 0xFFFF, 0x0102, 0, 0x1234, 0x8000, 0x00FF }, 2);
            Equal(66, size);
            Equal("HueStream", Encoding.ASCII.GetString(buffer, 0, 9));
            Equal("0200" + "07" + "0000" + "00" + "00", TlsPrf.ToHex(Slice(buffer, 9, 7)));
            Equal(id.ToLowerInvariant(), Encoding.ASCII.GetString(buffer, 16, 36));
            Equal("00" + "ffff" + "0102" + "0000", TlsPrf.ToHex(Slice(buffer, 52, 7)));
            Equal("05" + "1234" + "8000" + "00ff", TlsPrf.ToHex(Slice(buffer, 59, 7)));
        }

        private static void TestScanRectangle()
        {
            // Center light, 20 % region of a 640x360 frame
            Equal(new Rectangle(256, 144, 128, 72), CDeviceHueEntertainment.ScanRectangle(new EntertainmentChannel { X = 0, Z = 0 }, 640, 360, 20));
            // Top left corner stays inside the frame
            Equal(new Rectangle(0, 0, 128, 72), CDeviceHueEntertainment.ScanRectangle(new EntertainmentChannel { X = -1, Z = 1 }, 640, 360, 20));
            // Bottom right
            Equal(new Rectangle(512, 288, 128, 72), CDeviceHueEntertainment.ScanRectangle(new EntertainmentChannel { X = 1, Z = -1 }, 640, 360, 20));
            // Out of range positions are clamped
            Equal(new Rectangle(512, 0, 128, 72), CDeviceHueEntertainment.ScanRectangle(new EntertainmentChannel { X = 3, Z = 2 }, 640, 360, 20));
            // Lights never placed in the Hue app (all at 0/0/0) are spread from left to right
            var unplaced = new List<EntertainmentChannel> { new EntertainmentChannel { ID = 0 }, new EntertainmentChannel { ID = 1 } };
            List<EntertainmentChannel> spread = CDeviceHueEntertainment.EffectivePositions(unplaced);
            Equal(-0.5, spread[0].X);
            Equal(0.5, spread[1].X);
            Equal((byte)1, spread[1].ID);
            var placed = new List<EntertainmentChannel> { new EntertainmentChannel { ID = 0, X = -1 }, new EntertainmentChannel { ID = 1, X = 1 } };
            True(ReferenceEquals(placed, CDeviceHueEntertainment.EffectivePositions(placed)), "placed lights must keep their positions");
        }

        private static void TestChannelFormat()
        {
            var list = new List<EntertainmentChannel>
            {
                new EntertainmentChannel { ID = 0, X = -0.5, Y = 0.8, Z = 0.25 },
                new EntertainmentChannel { ID = 12, X = 1, Y = -1, Z = -0.125 },
            };
            string text = EntertainmentChannel.Format(list);
            Equal("0:-0.5:0.8:0.25;12:1:-1:-0.125", text);
            Equal(text, EntertainmentChannel.Format(EntertainmentChannel.Parse(text)));
        }

        private static void TestSecrets()
        {
            string secret = "0123456789abcdef0123456789ABCDEF";
            string stored = HueSecrets.Protect(secret);
            True(stored.StartsWith("dpapi:", StringComparison.Ordinal) && !stored.Contains(secret), "secret stored in plain text");
            Equal(secret, HueSecrets.Unprotect(stored));
            True(HueSecrets.Unprotect(secret) == null, "plain value must not be accepted");
        }

        private static void TestBridgeCertMatch()
        {
            True(HueClipV2.IsTrustedBridgeCertificate("001788fffe23bfc2", "001788fffe23bfc2"), "same id refused");
            // /api/0/config reports the id in upper case, the certificate in lower case
            True(HueClipV2.IsTrustedBridgeCertificate("001788FFFE23BFC2", "001788fffe23bfc2"), "case difference refused");
            Equal("001788fffe23bfc2", Configuration.NormalizeBridgeId(" 001788FFFE23BFC2 "));
        }

        private static void TestBridgeCertMismatch()
        {
            True(!HueClipV2.IsTrustedBridgeCertificate("001788fffe23bfc2", "001788fffe23bfc3"), "other bridge id accepted");
            True(!HueClipV2.IsTrustedBridgeCertificate("001788fffe23bfc2", "192.168.0.2"), "IP address as CN accepted");
            True(!HueClipV2.IsTrustedBridgeCertificate("001788fffe23bfc2", ""), "empty CN accepted");
            True(!HueClipV2.IsTrustedBridgeCertificate("001788fffe23bfc2", null), "missing CN accepted");
            True(!HueClipV2.IsTrustedBridgeCertificate("001788fffe23bfc2", "001788fffe23bfc2x"), "longer CN accepted");
        }

        private static void TestBridgeCertNoStoredId()
        {
            True(HueClipV2.IsTrustedBridgeCertificate(null, "001788fffe23bfc2"), "no stored id: certificate refused");
            True(HueClipV2.IsTrustedBridgeCertificate("", "anything"), "empty stored id: certificate refused");
            True(Configuration.NormalizeBridgeId("not-a-bridge-id") == null, "invalid id accepted");
            True(Configuration.NormalizeBridgeId("001788fffe23bfc") == null, "15 characters accepted");
        }

        private static void TestNewAreaPositions()
        {
            // auto, left, auto, top right: the two auto lights spread over the width
            List<EntertainmentChannel> p = frmHueCreateArea.ResolvePositions(new[] { 0, 1, 0, 4 });
            Equal(-0.5, p[0].X); Equal(0.0, p[0].Z);
            Equal(-1.0, p[1].X); Equal(0.0, p[1].Z);
            Equal(0.5, p[2].X);
            Equal(1.0, p[3].X); Equal(1.0, p[3].Z);
            Equal(1.0, p[3].Y);
        }

        private static void TestLightSettings()
        {
            Color dark = Color.FromArgb(40, 30, 20);
            // Neutral settings keep the color
            Equal(dark.ToArgb(), CDeviceHueEntertainment.ApplyLightSettings(dark, 100, 0, 100, 100, 100, 100).ToArgb());
            // Value gain 200 % doubles the brightness
            Equal(Color.FromArgb(80, 60, 40).ToArgb(), CDeviceHueEntertainment.ApplyLightSettings(dark, 200, 0, 100, 100, 100, 100).ToArgb());
            // ... and is capped at 255 per channel
            Equal(Color.FromArgb(255, 255, 100).ToArgb(), CDeviceHueEntertainment.ApplyLightSettings(Color.FromArgb(200, 150, 50), 200, 0, 100, 100, 100, 100).ToArgb());
            // Minimum brightness 20 % = 51 as lower limit per channel (after the gain, like the firmware)
            Equal(Color.FromArgb(51, 51, 51).ToArgb(), CDeviceHueEntertainment.ApplyLightSettings(Color.Black, 300, 20, 100, 100, 100, 100).ToArgb());
            // Maximum brightness and color shares last
            Equal(Color.FromArgb(40, 15, 0).ToArgb(), CDeviceHueEntertainment.ApplyLightSettings(Color.FromArgb(80, 60, 0), 100, 0, 100, 50, 100, 50).ToArgb());
        }

        private static void TestAutoPlaces()
        {
            Equal("3", string.Join(",", frmHueLamps.AutoPlaces(1)));          // top
            Equal("1,5", string.Join(",", frmHueLamps.AutoPlaces(2)));        // left, right
            Equal("1,3,5", string.Join(",", frmHueLamps.AutoPlaces(3)));
            Equal("1,3,5,7", string.Join(",", frmHueLamps.AutoPlaces(4)));
            Equal(10, frmHueLamps.AutoPlaces(10).Length);
        }

        private static void TestMdnsQuery()
        {
            Equal("000000000001000000000000" + "045f687565" + "045f746370" + "056c6f63616c" + "00" + "000c8001",
                TlsPrf.ToHex(HueDiscovery.BuildMdnsQuery()));
        }

        private static void TestPositionFromSectors()
        {
            // Sectors 0-2: right edge -> x = 1, z = 0
            EntertainmentChannel right = HuePcGroup.PositionFromSectors(new byte[] { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            Equal(1.0, right.X); Equal(0.0, Math.Round(right.Z, 3));
            // Sector 4: top center
            EntertainmentChannel top = HuePcGroup.PositionFromSectors(new byte[] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 });
            Equal(0.0, top.X); Equal(1.0, top.Z);
            // Sectors 6-8: left edge
            Equal(-1.0, HuePcGroup.PositionFromSectors(new byte[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 }).X);
            True(HuePcGroup.AreaName.StartsWith("AmbiBack PC ") && HuePcGroup.AreaName.Length <= 32, "area name");
        }

        private static void TestColorLight()
        {
            True(new Light { Type = "Extended color light" }.IsColorLight, "extended color light");
            True(new Light { Type = "Color light" }.IsColorLight, "color light");
            True(!new Light { Type = "On/Off plug-in unit" }.IsColorLight, "plug must be left out");
            True(!new Light { Type = "Dimmable light" }.IsColorLight, "white only light must be left out");
            True(!new Light { Type = "Color temperature light" }.IsColorLight, "color temperature light must be left out");
        }

        /// <summary>Read only: streaming capable lights (CLIP v2) and the v1 light types. Prints no secrets.</summary>
        public static bool RunBridgeInfo(string ip, string credFile, Action<string> output)
        {
            try
            {
                Configuration.DeviceIP = IPAddress.Parse(ip);
                string[] cred = (HueSecrets.Unprotect(System.IO.File.ReadAllText(credFile)) ?? "").Split('\n');
                if (cred.Length != 2)
                    throw new Exception("no stored credentials, run --hue-bridge-test first");
                Configuration.Username = cred[0];
                Configuration.ClientKey = cred[1];
                List<StreamingLight> lights = HueClipV2.GetStreamingLights();
                output("PASS  " + lights.Count + " streaming capable light(s):");
                foreach (StreamingLight l in lights)
                    output("      " + l.Name + " (" + l.Product + ")");
                output("      v1 lights:");
                foreach (Light l in new LightCollection())
                    output("      " + l.Name + ": " + l.Type + (l.IsColorLight ? "" : "  -> not listed"));
                return true;
            }
            catch (Exception ex)
            {
                output("FAIL  " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static void TestDeviceXml()
        {
            var area = new EntertainmentArea { ID = "1a8d99cc-967b-44f2-9202-43f976c0fa6b", Name = "TV" };
            area.Channels.Add(new EntertainmentChannel { ID = 0, X = -1, Y = 1, Z = 0.5 });
            area.Channels.Add(new EntertainmentChannel { ID = 1, X = 1, Y = 1, Z = 0.5 });
            var device = new CDeviceHueEntertainment(area) { FramesPerSecond = 40, InnerSizePercent = 30, ColorBluePercent = 80, ValueGainPercent = 250 };
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceHueEntertainment));
            var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, device);
            var copy = (CDeviceHueEntertainment)serializer.Deserialize(new System.IO.StringReader(writer.ToString()));
            copy.SetDeviceID();
            Equal(device.GetDeviceID(), copy.GetDeviceID());
            Equal(area.ID, copy.HueConfigID);
            Equal("TV", copy.GetDeviceName());
            Equal(device.HueChannels, copy.HueChannels);
            Equal((byte)40, copy.FramesPerSecond);
            Equal((byte)30, copy.InnerSizePercent);
            Equal((byte)80, copy.ColorBluePercent);
            Equal(250, copy.ValueGainPercent);
        }

        private static void TestDdpPacket()
        {
            var packets = new List<byte[]>();
            CDeviceWled.BuildDdpPackets(new byte[] { 1, 2, 3, 4, 5, 6 }, 6, 7, packets);
            Equal(1, packets.Count);
            // flags v1 + push, sequence, RGB 8 bit, id 1, offset 0, length 6, data
            Equal("41070b01" + "00000000" + "0006" + "010203040506", TlsPrf.ToHex(packets[0]));
        }

        private static void TestDdpSplit()
        {
            var packets = new List<byte[]>();
            byte[] data = new byte[600 * 3];
            data[1440] = 0xAB;
            CDeviceWled.BuildDdpPackets(data, data.Length, 1, packets);
            Equal(2, packets.Count);
            Equal((byte)0x40, packets[0][0]);   // no push on the first packet
            Equal((byte)0x41, packets[1][0]);   // push on the last
            Equal(CDeviceWled.DdpHeaderLength + 1440, packets[0].Length);
            Equal(CDeviceWled.DdpHeaderLength + 360, packets[1].Length);
            Equal("000005a0" + "0168", TlsPrf.ToHex(Slice(packets[1], 4, 6)));   // offset 1440, length 360
            Equal((byte)0xAB, packets[1][CDeviceWled.DdpHeaderLength]);
        }

        private static void TestWledInfo()
        {
            IPAddress ip = IPAddress.Parse("192.168.0.42");
            CDeviceWled.WledInfo info = CDeviceWled.ParseInfo(ip,
                "{\"ver\":\"0.15.0\",\"leds\":{\"count\":120,\"rgbw\":false},\"name\":\"TV strip\",\"brand\":\"WLED\",\"mac\":\"a8:03:2a:12:34:56\"}");
            Equal("TV strip", info.Name);
            Equal("0.15.0", info.Version);
            Equal(120, info.LedCount);
            Equal("a8032a123456", info.Mac);
            True(CDeviceWled.ParseInfo(ip, "{\"name\":\"Other\"}") == null, "a device without brand WLED must be left out");

            CDeviceWled device = CDeviceWled.FromInfo(info);
            Equal("WledDdp_a8032a123456", device.GetDeviceID());
            Equal(ip, device.GetIPAddress());
            Equal(120, device.PixelTotal);
        }

        private static void TestWledXml()
        {
            var device = new CDeviceWled("Desk", IPAddress.Parse("10.0.0.7"))
            {
                PixelHori = 30, PixelVert = 17, FlexSetupPort1 = (byte)DsFlexSetups.Top_Clock_top_left_right,
                Port = 4049, FramesPerSecond = 40, FrameDelay = 120, ValueGainPercent = 150, ColorRedPercent = 90,
                ReceiverLedCount = 94, ReceiverInfo = "WLED 0.15.0", InternalNetworkMAC = "a8032a123456",
            };
            device.SetDeviceID();
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceWled));
            var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, device);
            var copy = (CDeviceWled)serializer.Deserialize(new System.IO.StringReader(writer.ToString()));
            copy.SetDeviceID();
            Equal(device.GetDeviceID(), copy.GetDeviceID());
            Equal("Desk", copy.GetDeviceName());
            Equal(IPAddress.Parse("10.0.0.7"), copy.GetIPAddress());
            Equal((byte)30, copy.PixelHori);
            Equal((byte)17, copy.PixelVert);
            Equal((byte)DsFlexSetups.Top_Clock_top_left_right, copy.FlexSetupPort1);
            Equal(4049, copy.Port);
            Equal((byte)40, copy.FramesPerSecond);
            Equal(120, copy.FrameDelay);
            Equal(150, copy.ValueGainPercent);
            Equal((byte)90, copy.ColorRedPercent);
            Equal(94, copy.ReceiverLedCount);
            Equal("WLED 0.15.0", copy.ReceiverInfo);
        }

        private static void TestGoveeRazerEnable()
        {
            // Bytes and base64 as used by Govee's DreamView / Razer integration
            Equal("bb0001b1010a", TlsPrf.ToHex(CDeviceGovee.RazerEnablePacket(true)));
            Equal("bb0001b1000b", TlsPrf.ToHex(CDeviceGovee.RazerEnablePacket(false)));
            Equal("uwABsQEK", Convert.ToBase64String(CDeviceGovee.RazerEnablePacket(true)));
            Equal("uwABsQAL", Convert.ToBase64String(CDeviceGovee.RazerEnablePacket(false)));
        }

        private static void TestGoveeRazerColors()
        {
            byte[] colors = { 0xFF, 0x00, 0x00, 0x00, 0x80, 0x40 };
            byte[] p = CDeviceGovee.RazerColorPacket(colors, 2);
            // BB, length 2 + 3 * 2 = 8, B0, no gradient, 2 colors, RGB RGB, XOR of all bytes before
            byte xor = 0;
            for (int i = 0; i < p.Length - 1; i++)
                xor ^= p[i];
            Equal("bb0008b00002ff0000008040", TlsPrf.ToHex(p).Substring(0, 24));
            Equal(13, p.Length);
            Equal(xor, p[p.Length - 1]);

            // More LEDs than one frame takes: capped at 255, length in two bytes
            byte[] big = CDeviceGovee.RazerColorPacket(new byte[300 * 3], 300);
            Equal(255, (int)big[5]);
            Equal(2 + 3 * 255, big[1] << 8 | big[2]);
            Equal(4 + 2 + 3 * 255 + 1, big.Length);
        }

        private static void TestGoveeCommands()
        {
            Equal("{\"msg\":{\"cmd\":\"razer\",\"data\":{\"pt\":\"uwABsQEK\"}}}", CDeviceGovee.RazerCommand(CDeviceGovee.RazerEnablePacket(true)));
            Equal("{\"msg\":{\"cmd\":\"turn\",\"data\":{\"value\":1}}}", CDeviceGovee.TurnCommand(true));
            var color = Newtonsoft.Json.Linq.JObject.Parse(CDeviceGovee.ColorCommand(10, 20, 30));
            Equal("colorwc", (string)color["msg"]["cmd"]);
            Equal(20, (int)color["msg"]["data"]["color"]["g"]);
            Equal(0, (int)color["msg"]["data"]["colorTemInKelvin"]);
            Equal(0x0A141E, CDeviceGovee.AverageColor(new byte[] { 0, 0, 0, 20, 40, 60 }, 2));
        }

        private static void TestGoveeScan()
        {
            var info = CDeviceGovee.ParseScanResponse("{\"msg\":{\"cmd\":\"scan\",\"data\":{\"ip\":\"192.168.1.23\",\"device\":\"1F:80:C5:32:32:36:72:4E\",\"sku\":\"H6199\",\"bleVersionHard\":\"3.01.01\",\"bleVersionSoft\":\"1.03.01\",\"wifiVersionHard\":\"1.00.10\",\"wifiVersionSoft\":\"1.02.03\"}}}");
            Equal(IPAddress.Parse("192.168.1.23"), info.Address);
            Equal("H6199", info.Sku);
            Equal("1.02.03", info.WifiVersion);
            var device = CDeviceGovee.FromInfo(info);
            Equal("GoveeLan_1f80c5323236724e", device.GetDeviceID());
            Equal("Govee H6199", device.GetDeviceName());
            True(CDeviceGovee.ParseScanResponse("{\"msg\":{\"cmd\":\"devStatus\",\"data\":{}}}") == null, "only scan answers count");
        }

        private static void TestGoveeXml()
        {
            var device = new CDeviceGovee("TV light bars", IPAddress.Parse("10.0.0.9"))
            {
                PixelHori = 6, PixelVert = 4, Mode = CDeviceGovee.ModeSingleColor, FramesPerSecond = 20,
                FrameDelay = 200, ColorBluePercent = 80, ReceiverInfo = "H6046", InternalNetworkMAC = "1f80c5323236724e",
            };
            device.SetDeviceID();
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceGovee));
            var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, device);
            var copy = (CDeviceGovee)serializer.Deserialize(new System.IO.StringReader(writer.ToString()));
            copy.SetDeviceID();
            Equal(device.GetDeviceID(), copy.GetDeviceID());
            Equal(IPAddress.Parse("10.0.0.9"), copy.GetIPAddress());
            Equal((byte)6, copy.PixelHori);
            Equal((byte)4, copy.PixelVert);
            Equal(CDeviceGovee.ModeSingleColor, copy.Mode);
            Equal(CDeviceGovee.ControlPort, copy.Port);
            Equal((byte)20, copy.FramesPerSecond);
            Equal(200, copy.FrameDelay);
            Equal((byte)80, copy.ColorBluePercent);
            Equal("H6046", copy.ReceiverInfo);
        }

        private static void TestOpenRgbHeader()
        {
            byte[] p = CDeviceOpenRgb.BuildPacket(3, CDeviceOpenRgb.PacketRequestProtocolVersion, BitConverter.GetBytes(4u));
            // "ORGB", device 3, id 40, length 4 (little endian), then the version
            Equal("4f524742" + "03000000" + "28000000" + "04000000" + "04000000", TlsPrf.ToHex(p));
        }

        private static void TestOpenRgbUpdateLeds()
        {
            byte[] colors = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60 };
            byte[] p = new byte[16 + 6 + 4 * 3];
            CDeviceOpenRgb.FillUpdateLeds(p, 1, colors, 2, 3, false);
            // header: id 1050 = 0x41A, length 18; data size 18, 3 colors, R G B 0 each, third LED dark
            Equal("4f524742" + "01000000" + "1a040000" + "12000000" + "12000000" + "0300" + "10203000" + "40506000" + "00000000", TlsPrf.ToHex(p));
            CDeviceOpenRgb.FillUpdateLeds(p, 1, colors, 2, 3, true);
            Equal("28384800" + "28384800" + "28384800", TlsPrf.ToHex(p).Substring(2 * 22));
        }

        /// <summary>Controller data as an OpenRGB server writes it (one mode, two zones, one with a segment).</summary>
        private static byte[] OpenRgbControllerBlob(uint version)
        {
            var b = new List<byte>();
            Action<uint> u32 = v => b.AddRange(BitConverter.GetBytes(v));
            Action<int> u16 = v => b.AddRange(BitConverter.GetBytes((ushort)v));
            Action<string> str = s => { byte[] t = System.Text.Encoding.UTF8.GetBytes(s + " "); u16(t.Length); b.AddRange(t); };
            u32(0);                 // data size, filled below
            u32(4);                 // type: LED strip
            str("ARGB Strip");
            if (version >= 1) str("Acme");
            str("desc"); str("1.0"); str("SN123"); str("HID: /dev/x");
            u16(1); u32(0);         // one mode, active 0
            str("Direct"); u32(0); u32(0x20); u32(0); u32(0);
            if (version >= 3) { u32(0); u32(100); }
            u32(0); u32(0); u32(0);
            if (version >= 3) u32(100);
            u32(0); u32(1); u16(1); u32(0x00FF0000);
            u16(2);                 // zones
            str("Top"); u32(1); u32(0); u32(30); u32(30);
            u16(8 + 4 * 2); u32(1); u32(2); u32(0); u32(1);     // matrix 1 x 2
            if (version >= 4) { u16(1); str("Seg"); u32(0); u32(0); u32(10); }
            str("Side"); u32(1); u32(0); u32(20); u32(20); u16(0);
            if (version >= 4) u16(0);
            u16(50);                // LEDs
            for (int i = 0; i < 50; i++) { str("LED " + i); u32((uint)i); }
            u16(50);
            for (int i = 0; i < 50; i++) u32(0);
            byte[] data = b.ToArray();
            BitConverter.GetBytes((uint)data.Length).CopyTo(data, 0);
            return data;
        }

        private static void TestOpenRgbControllerData()
        {
            var info = CDeviceOpenRgb.ParseControllerData(OpenRgbControllerBlob(4), 4);
            Equal("ARGB Strip", info.Name);
            Equal("Acme", info.Vendor);
            Equal("SN123", info.Serial);
            Equal("HID: /dev/x", info.Location);
            Equal(CDeviceOpenRgb.DeviceTypeLedStrip, info.Type);
            Equal(50, info.LedCount);
            Equal("Top (30), Side (20)", string.Join(", ", info.Zones));
            var old = CDeviceOpenRgb.ParseControllerData(OpenRgbControllerBlob(0), 0);
            Equal("", old.Vendor);
            Equal(50, old.LedCount);
            var device = CDeviceOpenRgb.FromInfo(info, IPAddress.Loopback);
            Equal(CDeviceOpenRgb.ModeStrip, device.Mode);
            Equal("OpenRgb_" + CDeviceOpenRgb.StableId(info), device.GetDeviceID());
            Equal((byte)16, device.PixelHori);   // 50 LEDs: 16 + 9 per half
            Equal((byte)9, device.PixelVert);
        }

        private static void TestOpenRgbXml()
        {
            var device = new CDeviceOpenRgb("Mainboard", IPAddress.Parse("192.168.1.5"))
            {
                ControllerName = "ASUS Aura", ControllerLocation = "I2C: /dev/i2c-1", ControllerSerial = "", ControllerIndex = 2,
                Mode = CDeviceOpenRgb.ModeSingleColor, PixelHori = 10, PixelVert = 6, FramesPerSecond = 25, InternalNetworkMAC = "abcdef012345",
            };
            device.SetDeviceID();
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceOpenRgb));
            var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, device);
            var copy = (CDeviceOpenRgb)serializer.Deserialize(new System.IO.StringReader(writer.ToString()));
            copy.SetDeviceID();
            Equal(device.GetDeviceID(), copy.GetDeviceID());
            Equal("ASUS Aura", copy.ControllerName);
            Equal("I2C: /dev/i2c-1", copy.ControllerLocation);
            Equal(2, copy.ControllerIndex);
            Equal(CDeviceOpenRgb.ModeSingleColor, copy.Mode);
            Equal(CDeviceOpenRgb.SdkPort, copy.Port);
            Equal((byte)10, copy.PixelHori);
            Equal((byte)25, copy.FramesPerSecond);
        }

        private static void TestNanoleafFrame()
        {
            var panels = new List<CDeviceNanoleaf.Panel> { new CDeviceNanoleaf.Panel { Id = 0x1234 }, new CDeviceNanoleaf.Panel { Id = 7 } };
            byte[] colors = { 10, 20, 30, 40, 50, 60 };
            byte[] frame = new byte[2 + 8 * 2];
            CDeviceNanoleaf.FillFrameV2(frame, panels, colors, 2, false);
            // 2 panels; id, R G B, W 0, transition 0 (big endian)
            Equal("0002" + "1234" + "0a141e" + "00" + "0000" + "0007" + "28323c" + "00" + "0000", TlsPrf.ToHex(frame));
            CDeviceNanoleaf.FillFrameV2(frame, panels, colors, 2, true);
            Equal("19232d", TlsPrf.ToHex(frame).Substring(8, 6));
        }

        private const string NanoleafInfoJson = "{\"name\":\"Shapes 4C2A\",\"serialNo\":\"S21311A0123\",\"manufacturer\":\"Nanoleaf\",\"firmwareVersion\":\"9.2.4\",\"model\":\"NL42\","
            + "\"effects\":{\"select\":\"Northern Lights\",\"effectsList\":[\"Northern Lights\"]},"
            + "\"panelLayout\":{\"globalOrientation\":{\"value\":30,\"max\":360,\"min\":0},\"layout\":{\"numPanels\":4,\"sideLength\":134,\"positionData\":["
            + "{\"panelId\":101,\"x\":0,\"y\":0,\"o\":0,\"shapeType\":7},{\"panelId\":102,\"x\":116,\"y\":67,\"o\":60,\"shapeType\":7},"
            + "{\"panelId\":103,\"x\":232,\"y\":0,\"o\":0,\"shapeType\":7},{\"panelId\":0,\"x\":116,\"y\":-30,\"o\":0,\"shapeType\":12}]}}}";

        private static void TestNanoleafInfo()
        {
            var info = CDeviceNanoleaf.ParseInfo(NanoleafInfoJson);
            Equal("Shapes 4C2A", info.Name);
            Equal("S21311A0123", info.SerialNo);
            Equal("NL42", info.Model);
            Equal("Northern Lights", info.SelectedEffect);
            Equal(30, info.Orientation);
            Equal("101:0:0;102:116:67;103:232:0", CDeviceNanoleaf.FormatPanels(info.Panels));   // controller left out
            var device = CDeviceNanoleaf.FromInfo(info, IPAddress.Parse("192.168.1.30"), "tok123");
            Equal("Nanoleaf_s21311a0123", device.GetDeviceID());
            Equal(CDeviceNanoleaf.ModePositions, device.Mode);
            True(!device.UsesStripLayout, "Nanoleaf has no strip layout");
        }

        private static void TestNanoleafPlaces()
        {
            var panels = CDeviceNanoleaf.ParsePanels("1:0:0;2:100:0;3:0:100");
            System.Drawing.PointF[] p = CDeviceNanoleaf.ScreenPlaces(panels, 0);
            // y of the layout points up: (0,0) is bottom left, (0,100) top left
            Equal("0,1 1,1 0,0", string.Join(" ", System.Linq.Enumerable.Select(p, q => q.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + q.Y.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            p = CDeviceNanoleaf.ScreenPlaces(panels, 90);
            // turned clockwise by 90 degrees: the panel on the right moves to the bottom, the top one to the right
            True(Math.Abs(p[1].Y - 1) < 0.001 && Math.Abs(p[0].Y - p[2].Y) < 0.001 && p[2].X > p[0].X, "rotation by 90 degrees");
            Equal(new System.Drawing.Rectangle(0, 0, 80, 45), CDeviceNanoleaf.PanelRegion(new System.Drawing.PointF(0, 0), 320, 180, 25));
            Equal(new System.Drawing.Rectangle(120, 68, 80, 45), CDeviceNanoleaf.PanelRegion(new System.Drawing.PointF(0.5f, 0.5f), 320, 180, 25));
        }

        private static void TestNanoleafXml()
        {
            var device = CDeviceNanoleaf.FromInfo(CDeviceNanoleaf.ParseInfo(NanoleafInfoJson), IPAddress.Parse("10.0.0.40"), "secret-token");
            device.Mode = CDeviceNanoleaf.ModeSingleColor;
            device.FrameDelay = 200;
            True(device.AuthTokenProtected.StartsWith("dpapi:", StringComparison.Ordinal), "the token must be stored protected");
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceNanoleaf));
            var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, device);
            True(writer.ToString().IndexOf("secret-token", StringComparison.Ordinal) < 0, "the token must not be in the XML in plain text");
            var copy = (CDeviceNanoleaf)serializer.Deserialize(new System.IO.StringReader(writer.ToString()));
            copy.SetDeviceID();
            Equal(device.GetDeviceID(), copy.GetDeviceID());
            Equal("secret-token", copy.AuthToken);
            Equal("101:0:0;102:116:67;103:232:0", CDeviceNanoleaf.FormatPanels(copy.Panels));
            Equal(30, copy.GlobalOrientation);
            Equal(CDeviceNanoleaf.ModeSingleColor, copy.Mode);
            Equal(CDeviceNanoleaf.StreamPort, copy.Port);
            Equal(200, copy.FrameDelay);
        }

        private static void TestLifxHeader()
        {
            // GetService as broadcast: size 36, protocol 1024 + addressable + tagged = 0x3400, source, no target, res_required, seq 1, type 2
            byte[] m = CDeviceLifx.BuildMessage(CDeviceLifx.MsgGetService, new byte[0], new byte[8], true, true, 1, 0x01020304);
            Equal("2400" + "0034" + "04030201" + "0000000000000000" + "000000000000" + "01" + "01" + "0000000000000000" + "0200" + "0000", TlsPrf.ToHex(m));
            // Unicast to a light: target = MAC + 2 zero bytes, not tagged
            byte[] t = CDeviceLifx.MacTarget("d073d5123456");
            byte[] set = CDeviceLifx.BuildMessage(CDeviceLifx.MsgSetColor, new byte[13], t, false, false, 9, 7);
            Equal("3100" + "0014", TlsPrf.ToHex(set).Substring(0, 8));   // size 49, protocol 1024 + addressable, not tagged
            Equal("d073d51234560000", TlsPrf.ToHex(set).Substring(16, 16));
            Equal("d073d5123456", CDeviceLifx.MacText(set));
            Equal((ushort)102, CDeviceLifx.MessageType(set));
        }

        private static void TestLifxHsbk()
        {
            ushort h, s, b;
            CDeviceLifx.RgbToHsbk(255, 0, 0, out h, out s, out b);
            Equal("0,65535,65535", h + "," + s + "," + b);
            CDeviceLifx.RgbToHsbk(0, 255, 0, out h, out s, out b);
            Equal("21845,65535,65535", h + "," + s + "," + b);
            CDeviceLifx.RgbToHsbk(0, 0, 128, out h, out s, out b);
            Equal("43690,65535,32896", h + "," + s + "," + b);
            CDeviceLifx.RgbToHsbk(255, 255, 255, out h, out s, out b);
            Equal(0, (int)s);
            CDeviceLifx.RgbToHsbk(0, 0, 0, out h, out s, out b);
            Equal(0, (int)b);
        }

        private static void TestLifxPayloads()
        {
            // SetColor: reserved, H S B K, duration 50 ms
            Equal("00" + "0000" + "ffff" + "ffff" + "ac0d" + "32000000", TlsPrf.ToHex(CDeviceLifx.ColorPayload(255, 0, 0, 50)));
            Equal("ffff" + "f4010000", TlsPrf.ToHex(CDeviceLifx.PowerPayload(true, 500)));
            // Zones 1 and 2 of a strip: duration, apply, first zone 1, 2 colors, then 82 slots
            byte[] colors = { 0, 0, 0, 255, 0, 0, 0, 255, 0 };
            byte[] p = new byte[8 + 82 * 8];
            CDeviceLifx.FillExtendedZones(p, colors, 1, 2, 50);
            Equal("32000000" + "01" + "0100" + "02", TlsPrf.ToHex(p).Substring(0, 16));
            Equal("0000ffffffffac0d" + "5555ffffffffac0d", TlsPrf.ToHex(p).Substring(16, 32));
            Equal(664, p.Length);
        }

        private static void TestLifxXml()
        {
            var device = CDeviceLifx.FromInfo(new CDeviceLifx.LifxInfo { Address = IPAddress.Parse("10.0.0.50"), Mac = "d073d5aabbcc", Label = "TV Strip", Product = 32, Zones = 40 });
            Equal("Lifx_d073d5aabbcc", device.GetDeviceID());
            Equal(CDeviceLifx.ModeStrip, device.Mode);
            True(device.UsesStripLayout && device.ModeTextKeys != null, "a multizone light runs as strip");
            var bulb = CDeviceLifx.FromInfo(new CDeviceLifx.LifxInfo { Address = IPAddress.Parse("10.0.0.51"), Mac = "d073d5000001", Label = "", Product = 27, Zones = 0 });
            True(!bulb.UsesStripLayout && bulb.ModeTextKeys == null, "a bulb has one color and no modes");
            Equal("LIFX 000001", bulb.GetDeviceName());
            device.FramesPerSecond = 15;
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceLifx));
            var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, device);
            var copy = (CDeviceLifx)serializer.Deserialize(new System.IO.StringReader(writer.ToString()));
            copy.SetDeviceID();
            Equal(device.GetDeviceID(), copy.GetDeviceID());
            Equal(40, copy.ZoneCount);
            Equal(32, copy.ProductId);
            Equal(CDeviceLifx.ModeStrip, copy.Mode);
            Equal((byte)15, copy.FramesPerSecond);
            Equal(CDeviceLifx.LanPort, copy.Port);
        }

        private static void TestWledLayout()
        {
            var device = new CDeviceWled();
            device.SuggestLayout(120);
            Equal((byte)38, device.PixelHori);
            Equal((byte)22, device.PixelVert);
            device.SuggestLayout(1000);   // capped to what the monitor preview offers
            Equal((byte)CDeviceWled.MaxPixelHori, device.PixelHori);
            Equal((byte)CDeviceWled.MaxPixelVert, device.PixelVert);
        }

        private static void TestStripRectangles()
        {
            foreach (DsFlexSetups setup in Enum.GetValues(typeof(DsFlexSetups)))
            {
                var ambiBack = new CDeviceAmbiBack();
                ambiBack.SetPixelVert(9);
                ambiBack.SetPixelHori(16);
                ambiBack.SetFlexPort1((byte)setup);
                var expected = new Rectangle[ambiBack.PixelTotalPort1];
                ambiBack.CalcRectangles(0, 0, 1920, 1080, 0, expected);

                var wled = new CDeviceWled { PixelVert = 9, PixelHori = 16, FlexSetupPort1 = (byte)setup, InnerSizePercent = ambiBack.InnerSizePercent };
                var actual = new Rectangle[wled.PixelTotal];
                wled.CalcStripRectangles(wled.FlexSetupPort1, wled.PixelVert, wled.PixelHori, actual.Length, 0, 0, 1920, 1080, 0, actual);

                Equal(expected.Length, actual.Length);
                for (int i = 0; i < expected.Length; i++)
                    Equal(expected[i], actual[i]);
            }
            // First LED of "clockwise from bottom left up": left side, bottom
            var d = new CDeviceWled { PixelVert = 9, PixelHori = 16, FlexSetupPort1 = (byte)DsFlexSetups.Left_Clock_bottom_left_up };
            var rects = new Rectangle[d.PixelTotal];
            d.CalcStripRectangles(d.FlexSetupPort1, d.PixelVert, d.PixelHori, rects.Length, 0, 0, 1920, 1080, 0, rects);
            Equal(0, rects[0].Left);
            Equal(8 * 120, rects[0].Top);
        }

        private static byte[] Slice(byte[] a, int offset, int length)
        {
            byte[] r = new byte[length];
            Buffer.BlockCopy(a, offset, r, 0, length);
            return r;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("expected " + expected + ", got " + actual);
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }
    }
}
