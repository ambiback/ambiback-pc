using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace AmbiBack.PhilipsHue
{
    /// <summary>DTLS failure (handshake, alert from the peer, timeout or lost connection).</summary>
    public class HueDtlsException : Exception
    {
        /// <summary>TLS alert description when the peer sent an alert, otherwise -1.</summary>
        public int Alert { get; }

        public HueDtlsException(string message, int alert = -1, Exception inner = null) : base(message, inner)
        {
            Alert = alert;
        }
    }

    /// <summary>
    /// Minimal DTLS 1.2 client (RFC 6347) for the Hue Entertainment API: one cipher suite,
    /// TLS_PSK_WITH_AES_128_GCM_SHA256 (RFC 4279 / RFC 5487), no certificates, no extensions,
    /// no renegotiation, no session resumption. Handles HelloVerifyRequest cookies, retransmission
    /// with a doubling timer, fragmented and reordered handshake messages, epochs and sequence
    /// numbers, the Finished check in both directions and alerts.
    /// Not thread safe; one thread drives Connect, Send, Poll and Close.
    /// </summary>
    public sealed class HueDtlsClient : IDisposable
    {
        public const int HuePort = 2100;
        public const ushort CipherSuite = 0x00A8;     // TLS_PSK_WITH_AES_128_GCM_SHA256

        private const byte CtChangeCipherSpec = 20;
        private const byte CtAlert = 21;
        private const byte CtHandshake = 22;
        private const byte CtApplicationData = 23;

        private const byte HsClientHello = 1;
        private const byte HsServerHello = 2;
        private const byte HsHelloVerifyRequest = 3;
        private const byte HsServerKeyExchange = 12;
        private const byte HsCertificateRequest = 13;
        private const byte HsServerHelloDone = 14;
        private const byte HsClientKeyExchange = 16;
        private const byte HsFinished = 20;

        private const int RecordHeader = 13;
        private const int HandshakeHeader = 12;
        private const int ExplicitNonce = 8;
        private const int MaxDatagram = 1500;

        private const byte VersionMajor = 0xFE;      // DTLS 1.2 = {254, 253}
        private const byte VersionMinor = 0xFD;       // records of a HelloVerifyRequest may carry 0xFF (DTLS 1.0)

        private readonly IPEndPoint _remote;
        private readonly byte[] _identity;
        private readonly byte[] _psk;

        private Socket _socket;
        private readonly byte[] _recvBuffer = new byte[MaxDatagram * 2];

        // Write state
        private ushort _writeEpoch;
        private readonly ulong[] _writeSeq = new ulong[2];
        private AesGcmCng _writeCipher;
        private byte[] _writeIv;

        // Read state
        private ushort _readEpoch;
        private AesGcmCng _readCipher;
        private byte[] _readIv;
        private ulong _replayMax;
        private ulong _replayWindow;
        private bool _replayInit;

        // Handshake state
        private readonly byte[] _clientRandom = new byte[32];
        private byte[] _serverRandom;
        private byte[] _masterSecret;
        private byte[] _cookie = new byte[0];
        private ushort _sendMessageSeq;
        private int _recvMessageSeq;
        private MemoryStream _transcript = new MemoryStream();
        private readonly Dictionary<int, Fragments> _pending = new Dictionary<int, Fragments>();
        private List<FlightRecord> _flight = new List<FlightRecord>();
        private bool _serverHelloDone;
        private bool _serverCcs;
        private bool _finished;
        private byte[] _expectedServerVerify;

        public bool IsConnected { get; private set; }

        /// <summary>Optional debug sink. Never receives key material.</summary>
        public Action<string> Log { get; set; }

        public HueDtlsClient(IPEndPoint remote, string identity, byte[] psk)
        {
            _remote = remote ?? throw new ArgumentNullException(nameof(remote));
            if (string.IsNullOrEmpty(identity))
                throw new ArgumentException("PSK identity is empty.", nameof(identity));
            if (psk == null || psk.Length == 0)
                throw new ArgumentException("PSK is empty.", nameof(psk));
            _identity = Encoding.UTF8.GetBytes(identity);
            _psk = (byte[])psk.Clone();
        }

        #region Public API
        /// <summary>Runs the handshake. Throws <see cref="HueDtlsException"/> on failure or after timeoutMs.</summary>
        public void Connect(int timeoutMs = 10000)
        {
            if (_socket != null)
                throw new InvalidOperationException("Connect can only be called once.");

            _socket = new Socket(_remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Connect(_remote);    // only datagrams from the bridge are delivered

            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(_clientRandom);
            uint now = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            _clientRandom[0] = (byte)(now >> 24);
            _clientRandom[1] = (byte)(now >> 16);
            _clientRandom[2] = (byte)(now >> 8);
            _clientRandom[3] = (byte)now;

            // Flight 1: ClientHello without cookie (not part of the transcript, RFC 6347 4.2.1)
            StartFlight();
            AddHandshakeToFlight(HsClientHello, BuildClientHello(), false);
            SendFlight();

            Stopwatch total = Stopwatch.StartNew();
            int retransmitMs = 1000;
            Stopwatch timer = Stopwatch.StartNew();
            try
            {
                while (!_finished)
                {
                    int left = timeoutMs - (int)total.ElapsedMilliseconds;
                    if (left <= 0)
                    {
                        // DTLS servers drop records they cannot decrypt, so a wrong PSK shows up as silence
                        // after our Finished
                        throw new HueDtlsException(_serverHelloDone
                            ? "DTLS handshake timed out after Finished (wrong client key or user name?)."
                            : "DTLS handshake timed out (no answer from the bridge on port " + _remote.Port + ").");
                    }
                    int wait = Math.Min(left, Math.Max(1, retransmitMs - (int)timer.ElapsedMilliseconds));

                    if (_socket.Poll(wait * 1000, SelectMode.SelectRead))
                    {
                        int n = ReceiveDatagram();
                        if (n > 0 && ProcessDatagram(n, true))
                        {
                            // Progress: the new flight was sent, restart the timer (RFC 6347 4.2.4)
                            retransmitMs = 1000;
                            timer.Restart();
                        }
                    }
                    else if (timer.ElapsedMilliseconds >= retransmitMs)
                    {
                        Debug("retransmit flight");
                        SendFlight();
                        retransmitMs = Math.Min(retransmitMs * 2, 60000);
                        timer.Restart();
                    }
                }
            }
            catch (SocketException ex)
            {
                throw new HueDtlsException("DTLS connection failed: " + ex.SocketErrorCode, -1, ex);
            }

            IsConnected = true;
            _transcript.Dispose();
            _transcript = null;
            _flight = null;
            _pending.Clear();
            Debug("handshake complete");
        }

        /// <summary>Sends one application data record (one datagram).</summary>
        public void Send(byte[] data, int offset, int count)
        {
            if (!IsConnected)
                throw new HueDtlsException("DTLS session is not connected.");
            byte[] datagram = BuildRecord(CtApplicationData, 1, data, offset, count);
            try
            {
                _socket.Send(datagram);
            }
            catch (SocketException ex)
            {
                IsConnected = false;
                throw new HueDtlsException("DTLS send failed: " + ex.SocketErrorCode, -1, ex);
            }
        }

        /// <summary>
        /// Reads whatever arrived without blocking. Returns false (and IsConnected becomes false) when the
        /// peer closed the session with an alert or the network reported the port as unreachable.
        /// </summary>
        public bool Poll()
        {
            if (!IsConnected)
                return false;
            try
            {
                while (_socket.Available > 0 || _socket.Poll(0, SelectMode.SelectRead))
                {
                    int n = ReceiveDatagram();
                    if (n <= 0)
                        break;
                    ProcessDatagram(n, false);
                }
            }
            catch (HueDtlsException ex)
            {
                Debug(ex.Message);
                IsConnected = false;
            }
            catch (SocketException ex)
            {
                // WSAECONNRESET: ICMP port unreachable, e.g. the bridge restarted or stopped streaming
                Debug("socket error " + ex.SocketErrorCode);
                IsConnected = false;
            }
            return IsConnected;
        }

        /// <summary>Sends close_notify (best effort) and releases the socket and keys.</summary>
        public void Close()
        {
            if (IsConnected)
            {
                try
                {
                    byte[] alert = { 1, 0 };      // warning, close_notify
                    _socket.Send(BuildRecord(CtAlert, 1, alert, 0, alert.Length));
                }
                catch (Exception ex)
                {
                    Debug("close_notify failed: " + ex.Message);
                }
                IsConnected = false;
            }
            Dispose();
        }

        public void Dispose()
        {
            IsConnected = false;
            _socket?.Close();
            _socket = null;
            _writeCipher?.Dispose();
            _writeCipher = null;
            _readCipher?.Dispose();
            _readCipher = null;
            _transcript?.Dispose();
            _transcript = null;
            if (_masterSecret != null)
                Array.Clear(_masterSecret, 0, _masterSecret.Length);
            Array.Clear(_psk, 0, _psk.Length);
        }
        #endregion

        #region Handshake
        private byte[] BuildClientHello()
        {
            var b = new MemoryStream();
            b.WriteByte(VersionMajor);
            b.WriteByte(VersionMinor);
            b.Write(_clientRandom, 0, 32);
            b.WriteByte(0);                          // session_id: empty
            b.WriteByte((byte)_cookie.Length);
            b.Write(_cookie, 0, _cookie.Length);
            b.WriteByte(0); b.WriteByte(2);          // cipher_suites length
            b.WriteByte(CipherSuite >> 8); b.WriteByte(CipherSuite & 0xFF);
            b.WriteByte(1); b.WriteByte(0);          // compression: null
            return b.ToArray();                      // no extensions
        }

        /// <summary>Handles one complete handshake message. Returns true when a new flight was sent.</summary>
        private bool HandleHandshake(byte type, byte[] body, byte[] fullMessage)
        {
            switch (type)
            {
                case HsHelloVerifyRequest:
                {
                    if (_serverRandom != null)
                        return false;
                    if (body.Length < 3)
                        throw Fail("bad HelloVerifyRequest", 50);
                    int len = body[2];
                    if (body.Length < 3 + len || len > 255)
                        throw Fail("bad HelloVerifyRequest cookie", 50);
                    _cookie = new byte[len];
                    Buffer.BlockCopy(body, 3, _cookie, 0, len);
                    Debug("HelloVerifyRequest, cookie " + len + " bytes");

                    // Flight 3: ClientHello with cookie; the transcript starts here
                    _transcript.SetLength(0);
                    StartFlight();
                    AddHandshakeToFlight(HsClientHello, BuildClientHello(), true);
                    SendFlight();
                    return true;
                }

                case HsServerHello:
                {
                    if (_serverRandom != null)
                        return false;
                    if (_transcript.Length == 0)
                    {
                        // No HelloVerifyRequest: the first ClientHello is part of the transcript after all
                        FlightRecord hello = _flight[0];
                        _transcript.Write(hello.Data, 0, hello.Data.Length);
                    }
                    if (body.Length < 38 || body[0] != VersionMajor || body[1] != VersionMinor)
                        throw Fail("server does not speak DTLS 1.2", 70);
                    _serverRandom = new byte[32];
                    Buffer.BlockCopy(body, 2, _serverRandom, 0, 32);
                    int sidLen = body[34];
                    int p = 35 + sidLen;
                    if (body.Length < p + 3)
                        throw Fail("bad ServerHello", 50);
                    ushort suite = (ushort)((body[p] << 8) | body[p + 1]);
                    if (suite != CipherSuite || body[p + 2] != 0)
                        throw Fail("server chose cipher 0x" + suite.ToString("X4"), 40);
                    AppendTranscript(fullMessage);
                    return false;
                }

                case HsServerKeyExchange:
                    // PSK identity hint, not used by the Hue bridge
                    RequireServerHello();
                    AppendTranscript(fullMessage);
                    return false;

                case HsCertificateRequest:
                    throw Fail("server requested a certificate", 40);

                case HsServerHelloDone:
                    RequireServerHello();
                    AppendTranscript(fullMessage);
                    _serverHelloDone = true;
                    SendClientFinishedFlight();
                    return true;

                case HsFinished:
                {
                    if (!_serverCcs || _expectedServerVerify == null)
                        throw Fail("unexpected Finished", 10);
                    if (body.Length != 12 || !ConstantTimeEquals(body, _expectedServerVerify))
                        throw Fail("server Finished does not verify", 51);
                    _finished = true;
                    return false;
                }

                default:
                    throw Fail("unexpected handshake message " + type, 10);
            }
        }

        private void RequireServerHello()
        {
            if (_serverRandom == null || _serverHelloDone)
                throw Fail("unexpected handshake message order", 10);
        }

        /// <summary>Flight 5: ClientKeyExchange, ChangeCipherSpec, Finished.</summary>
        private void SendClientFinishedFlight()
        {
            byte[] cke = new byte[2 + _identity.Length];
            cke[0] = (byte)(_identity.Length >> 8);
            cke[1] = (byte)_identity.Length;
            Buffer.BlockCopy(_identity, 0, cke, 2, _identity.Length);

            StartFlight();
            AddHandshakeToFlight(HsClientKeyExchange, cke, true);

            // Keys (RFC 5246 6.3 / 8.1); AES-128-GCM: no MAC keys, 16 byte keys, 4 byte implicit IVs
            byte[] pms = TlsPrf.PskPremaster(_psk);
            _masterSecret = TlsPrf.Compute(pms, "master secret", Concat(_clientRandom, _serverRandom), 48);
            Array.Clear(pms, 0, pms.Length);
            byte[] keyBlock = TlsPrf.Compute(_masterSecret, "key expansion", Concat(_serverRandom, _clientRandom), 40);
            byte[] clientKey = Slice(keyBlock, 0, 16);
            byte[] serverKey = Slice(keyBlock, 16, 16);
            _writeIv = Slice(keyBlock, 32, 4);
            _readIv = Slice(keyBlock, 36, 4);
            _writeCipher?.Dispose();
            _readCipher?.Dispose();
            _writeCipher = new AesGcmCng(clientKey);
            _readCipher = new AesGcmCng(serverKey);
            Array.Clear(keyBlock, 0, keyBlock.Length);
            Array.Clear(clientKey, 0, clientKey.Length);
            Array.Clear(serverKey, 0, serverKey.Length);

            _flight.Add(new FlightRecord { Type = CtChangeCipherSpec, Epoch = 0, Data = new byte[] { 1 } });

            byte[] clientVerify = FinishedVerifyData(_masterSecret, "client finished", _transcript.ToArray());
            byte[] finished = AddHandshakeToFlight(HsFinished, clientVerify, true, 1);
            _expectedServerVerify = FinishedVerifyData(_masterSecret, "server finished", _transcript.ToArray());
            _writeEpoch = 1;
            SendFlight();
        }

        /// <summary>verify_data = PRF(master_secret, label, SHA-256(handshake_messages))[0..11]</summary>
        public static byte[] FinishedVerifyData(byte[] masterSecret, string label, byte[] transcript)
        {
            byte[] hash;
            using (var sha = SHA256.Create())
                hash = sha.ComputeHash(transcript);
            return TlsPrf.Compute(masterSecret, label, hash, 12);
        }

        private void StartFlight()
        {
            _flight = new List<FlightRecord>();
        }

        /// <summary>Builds a handshake message with the DTLS header (unfragmented) and adds it to the flight.</summary>
        private byte[] AddHandshakeToFlight(byte type, byte[] body, bool toTranscript, ushort epoch = 0)
        {
            byte[] msg = new byte[HandshakeHeader + body.Length];
            msg[0] = type;
            WriteUInt24(msg, 1, body.Length);
            msg[4] = (byte)(_sendMessageSeq >> 8);
            msg[5] = (byte)_sendMessageSeq;
            WriteUInt24(msg, 6, 0);                 // fragment_offset
            WriteUInt24(msg, 9, body.Length);       // fragment_length
            Buffer.BlockCopy(body, 0, msg, HandshakeHeader, body.Length);
            _sendMessageSeq++;
            if (toTranscript)
                AppendTranscript(msg);
            _flight.Add(new FlightRecord { Type = CtHandshake, Epoch = epoch, Data = msg });
            return msg;
        }

        /// <summary>(Re)sends the current flight in one datagram, with fresh record sequence numbers.</summary>
        private void SendFlight()
        {
            var datagram = new MemoryStream();
            foreach (FlightRecord r in _flight)
            {
                byte[] rec = BuildRecord(r.Type, r.Epoch, r.Data, 0, r.Data.Length);
                datagram.Write(rec, 0, rec.Length);
            }
            _socket.Send(datagram.ToArray());
        }

        private void AppendTranscript(byte[] msg)
        {
            _transcript.Write(msg, 0, msg.Length);
        }
        #endregion

        #region Record layer
        /// <summary>Builds one record; epoch 1 records are encrypted with AES-128-GCM (RFC 5288).</summary>
        private byte[] BuildRecord(byte type, ushort epoch, byte[] data, int offset, int count)
        {
            ulong seq = _writeSeq[epoch]++;
            if (seq >= (1UL << 48))
                throw new HueDtlsException("DTLS sequence number exhausted.");

            if (epoch == 0)
            {
                byte[] plain = new byte[RecordHeader + count];
                WriteRecordHeader(plain, type, epoch, seq, count);
                Buffer.BlockCopy(data, offset, plain, RecordHeader, count);
                return plain;
            }

            int len = ExplicitNonce + count + AesGcmCng.TagSize;
            byte[] rec = new byte[RecordHeader + len];
            WriteRecordHeader(rec, type, epoch, seq, len);
            // explicit nonce = epoch + sequence number (unique per key)
            Buffer.BlockCopy(rec, 3, rec, RecordHeader, ExplicitNonce);
            byte[] nonce = new byte[AesGcmCng.NonceSize];
            Buffer.BlockCopy(_writeIv, 0, nonce, 0, 4);
            Buffer.BlockCopy(rec, 3, nonce, 4, ExplicitNonce);
            byte[] aad = BuildAad(epoch, seq, type, count);
            _writeCipher.Encrypt(nonce, data, offset, count, rec, RecordHeader + ExplicitNonce,
                rec, RecordHeader + ExplicitNonce + count, aad);
            return rec;
        }

        /// <summary>additional_data = seq_num(epoch + sequence) + type + version + length (RFC 5246 6.2.3.3 / RFC 6347 4.1.2.1)</summary>
        private static byte[] BuildAad(ushort epoch, ulong seq, byte type, int plainLength)
        {
            byte[] aad = new byte[13];
            aad[0] = (byte)(epoch >> 8);
            aad[1] = (byte)epoch;
            WriteUInt48(aad, 2, seq);
            aad[8] = type;
            aad[9] = VersionMajor;
            aad[10] = VersionMinor;
            aad[11] = (byte)(plainLength >> 8);
            aad[12] = (byte)plainLength;
            return aad;
        }

        private static void WriteRecordHeader(byte[] buf, byte type, ushort epoch, ulong seq, int length)
        {
            buf[0] = type;
            buf[1] = VersionMajor;
            buf[2] = VersionMinor;
            buf[3] = (byte)(epoch >> 8);
            buf[4] = (byte)epoch;
            WriteUInt48(buf, 5, seq);
            buf[11] = (byte)(length >> 8);
            buf[12] = (byte)length;
        }

        private int ReceiveDatagram()
        {
            return _socket.Receive(_recvBuffer);
        }

        /// <summary>Processes all records of one datagram. Returns true when a new flight was sent.</summary>
        private bool ProcessDatagram(int length, bool handshaking)
        {
            bool sentFlight = false;
            int p = 0;
            while (p + RecordHeader <= length)
            {
                int recStart = p;
                byte type = _recvBuffer[p];
                ushort epoch = (ushort)((_recvBuffer[p + 3] << 8) | _recvBuffer[p + 4]);
                ulong seq = ReadUInt48(_recvBuffer, p + 5);
                int recLen = (_recvBuffer[p + 11] << 8) | _recvBuffer[p + 12];
                int start = p + RecordHeader;
                p = start + recLen;
                if (p > length)
                    break;                                       // truncated record: drop the rest
                if (_recvBuffer[recStart + 1] != VersionMajor)
                    continue;

                byte[] payload;
                if (epoch != _readEpoch)
                    continue;                                    // other epoch: stale or not yet readable
                if (epoch == 0)
                {
                    payload = new byte[recLen];
                    Buffer.BlockCopy(_recvBuffer, start, payload, 0, recLen);
                }
                else
                {
                    payload = DecryptRecord(type, epoch, seq, start, recLen);
                    if (payload == null)
                        continue;                               // invalid records are silently discarded (RFC 6347 4.1.2.7)
                }

                switch (type)
                {
                    case CtAlert:
                        HandleAlert(payload);
                        break;

                    case CtChangeCipherSpec:
                        if (handshaking && _expectedServerVerify != null && !_serverCcs && payload.Length == 1 && payload[0] == 1)
                        {
                            _serverCcs = true;
                            _readEpoch = 1;
                            _replayInit = false;
                        }
                        break;

                    case CtHandshake:
                        if (handshaking)
                            sentFlight |= HandleHandshakeRecord(payload);
                        break;

                    case CtApplicationData:
                        break;                                  // the bridge sends nothing we need
                }
            }
            return sentFlight;
        }

        private byte[] DecryptRecord(byte type, ushort epoch, ulong seq, int start, int recLen)
        {
            if (_readCipher == null || recLen < ExplicitNonce + AesGcmCng.TagSize)
                return null;
            if (!ReplayCheck(seq))
                return null;
            int plainLen = recLen - ExplicitNonce - AesGcmCng.TagSize;
            byte[] nonce = new byte[AesGcmCng.NonceSize];
            Buffer.BlockCopy(_readIv, 0, nonce, 0, 4);
            Buffer.BlockCopy(_recvBuffer, start, nonce, 4, ExplicitNonce);
            byte[] plain = new byte[plainLen];
            byte[] aad = BuildAad(epoch, seq, type, plainLen);
            if (!_readCipher.Decrypt(nonce, _recvBuffer, start + ExplicitNonce, plainLen,
                _recvBuffer, start + ExplicitNonce + plainLen, plain, 0, aad))
            {
                Debug("record failed authentication, dropped");
                return null;
            }
            ReplayMark(seq);
            return plain;
        }

        /// <summary>64 record sliding replay window (RFC 6347 4.1.2.6).</summary>
        private bool ReplayCheck(ulong seq)
        {
            if (!_replayInit || seq > _replayMax)
                return true;
            ulong diff = _replayMax - seq;
            return diff < 64 && (_replayWindow & (1UL << (int)diff)) == 0;
        }

        private void ReplayMark(ulong seq)
        {
            if (!_replayInit)
            {
                _replayInit = true;
                _replayMax = seq;
                _replayWindow = 1;
                return;
            }
            if (seq > _replayMax)
            {
                ulong shift = seq - _replayMax;
                _replayWindow = shift >= 64 ? 1 : (_replayWindow << (int)shift) | 1;
                _replayMax = seq;
            }
            else
            {
                _replayWindow |= 1UL << (int)(_replayMax - seq);
            }
        }

        private void HandleAlert(byte[] payload)
        {
            if (payload.Length < 2)
                return;
            int level = payload[0], description = payload[1];
            if (description == 0)
                throw new HueDtlsException("DTLS peer closed the session (close_notify).", 0);
            if (level == 2)
                throw new HueDtlsException("DTLS alert from peer: " + AlertName(description), description);
            Debug("warning alert " + description);
        }

        /// <summary>Collects handshake fragments of one record and handles completed messages in order.</summary>
        private bool HandleHandshakeRecord(byte[] payload)
        {
            bool sent = false;
            int p = 0;
            while (p + HandshakeHeader <= payload.Length)
            {
                byte type = payload[p];
                int length = ReadUInt24(payload, p + 1);
                int seq = (payload[p + 4] << 8) | payload[p + 5];
                int fragOffset = ReadUInt24(payload, p + 6);
                int fragLength = ReadUInt24(payload, p + 9);
                int start = p + HandshakeHeader;
                p = start + fragLength;
                if (p > payload.Length || fragOffset + fragLength > length || length > 65536)
                    break;

                // Servers differ in the message_seq of the ServerHello after a HelloVerifyRequest
                // (0 or 1); accept whatever the first ServerHello uses.
                if (type == HsServerHello && _serverRandom == null && seq != _recvMessageSeq && fragOffset == 0)
                {
                    _pending.Clear();
                    _recvMessageSeq = seq;
                }

                if (seq < _recvMessageSeq)
                {
                    // Retransmitted message we already have: the server lost our flight
                    if (_flight != null && _flight.Count > 0)
                    {
                        SendFlight();
                        sent = true;
                    }
                    continue;
                }

                if (!_pending.TryGetValue(seq, out Fragments frag))
                {
                    frag = new Fragments { Type = type, Body = new byte[length], Have = new bool[length] };
                    _pending[seq] = frag;
                }
                if (frag.Type != type || frag.Body.Length != length)
                    continue;
                Buffer.BlockCopy(payload, start, frag.Body, fragOffset, fragLength);
                for (int i = fragOffset; i < fragOffset + fragLength; i++)
                    frag.Have[i] = true;

                // Handle all complete messages in sequence order
                while (_pending.TryGetValue(_recvMessageSeq, out Fragments next) && next.IsComplete())
                {
                    _pending.Remove(_recvMessageSeq);
                    byte[] full = new byte[HandshakeHeader + next.Body.Length];
                    full[0] = next.Type;
                    WriteUInt24(full, 1, next.Body.Length);
                    full[4] = (byte)(_recvMessageSeq >> 8);
                    full[5] = (byte)_recvMessageSeq;
                    WriteUInt24(full, 6, 0);
                    WriteUInt24(full, 9, next.Body.Length);
                    Buffer.BlockCopy(next.Body, 0, full, HandshakeHeader, next.Body.Length);
                    _recvMessageSeq++;
                    sent |= HandleHandshake(next.Type, next.Body, full);
                }
            }
            return sent;
        }
        #endregion

        #region Helpers
        private HueDtlsException Fail(string message, int alert)
        {
            // Tell the server (best effort, unencrypted is fine before the keys are active)
            try
            {
                byte[] payload = { 2, (byte)alert };
                _socket?.Send(BuildRecord(CtAlert, _writeEpoch, payload, 0, 2));
            }
            catch (Exception)
            {
                // ignore
            }
            return new HueDtlsException("DTLS handshake failed: " + message);
        }

        private void Debug(string text)
        {
            Log?.Invoke("DTLS|" + text);
        }

        public static string AlertName(int description)
        {
            switch (description)
            {
                case 0: return "close_notify";
                case 10: return "unexpected_message";
                case 20: return "bad_record_mac";
                case 40: return "handshake_failure";
                case 47: return "illegal_parameter";
                case 50: return "decode_error";
                case 51: return "decrypt_error";
                case 70: return "protocol_version";
                case 80: return "internal_error";
                case 115: return "unknown_psk_identity";
                default: return "alert " + description;
            }
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            byte[] r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }

        private static byte[] Slice(byte[] a, int offset, int length)
        {
            byte[] r = new byte[length];
            Buffer.BlockCopy(a, offset, r, 0, length);
            return r;
        }

        private static void WriteUInt24(byte[] b, int p, int v)
        {
            b[p] = (byte)(v >> 16);
            b[p + 1] = (byte)(v >> 8);
            b[p + 2] = (byte)v;
        }

        private static int ReadUInt24(byte[] b, int p)
        {
            return (b[p] << 16) | (b[p + 1] << 8) | b[p + 2];
        }

        private static void WriteUInt48(byte[] b, int p, ulong v)
        {
            for (int i = 5; i >= 0; i--)
            {
                b[p + i] = (byte)v;
                v >>= 8;
            }
        }

        private static ulong ReadUInt48(byte[] b, int p)
        {
            ulong v = 0;
            for (int i = 0; i < 6; i++)
                v = (v << 8) | b[p + i];
            return v;
        }

        private sealed class FlightRecord
        {
            public byte Type;
            public ushort Epoch;
            public byte[] Data;
        }

        private sealed class Fragments
        {
            public byte Type;
            public byte[] Body;
            public bool[] Have;

            public bool IsComplete()
            {
                foreach (bool b in Have)
                    if (!b) return false;
                return true;
            }
        }
        #endregion
    }
}
