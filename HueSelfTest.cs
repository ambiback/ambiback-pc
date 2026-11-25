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
                Test("Entertainment device XML round trip", TestDeviceXml),
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

        private static void TestDeviceXml()
        {
            var area = new EntertainmentArea { ID = "1a8d99cc-967b-44f2-9202-43f976c0fa6b", Name = "TV" };
            area.Channels.Add(new EntertainmentChannel { ID = 0, X = -1, Y = 1, Z = 0.5 });
            area.Channels.Add(new EntertainmentChannel { ID = 1, X = 1, Y = 1, Z = 0.5 });
            var device = new CDeviceHueEntertainment(area) { FramesPerSecond = 40, InnerSizePercent = 30, ColorBluePercent = 80 };
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
