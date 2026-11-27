using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AmbiBack.PhilipsHue
{
    /// <summary>
    /// One streaming session to an entertainment area: CLIP v2 start, DTLS connection, HueStream
    /// frames, health check, reconnect with exponential backoff and stop. Driven by one device thread;
    /// only the health check runs on the thread pool.
    /// </summary>
    public sealed class HueEntertainmentSession : IDisposable
    {
        public enum SessionState { Idle, Connecting, Streaming, Waiting }

        private const int MinBackoffMs = 1000;
        private const int MaxBackoffMs = 30000;
        private const int HealthCheckIntervalMs = 10000;
        private const int HandshakeTimeoutMs = 8000;

        private readonly string _configId;
        private readonly Action<string> _log;
        private readonly byte[] _message = new byte[HueStreamMessage.HeaderSize + HueStreamMessage.ChannelSize * HueStreamMessage.MaxChannels];

        private HueDtlsClient _dtls;
        private bool _streamingStarted;
        private bool _lightsUsed;      // streamed at least once since the last switch off
        private byte _sequence;
        private int _backoffMs = MinBackoffMs;
        private readonly Stopwatch _sinceFailure = new Stopwatch();
        private readonly Stopwatch _sinceHealthCheck = new Stopwatch();
        private Task _healthTask;
        private volatile bool _healthFailed;
        private int _healthErrors;

        public SessionState State { get; private set; } = SessionState.Idle;
        public string LastError { get; private set; }
        public EntertainmentArea Area { get; private set; }

        /// <summary>Raised (on the device thread) when the area or its channels were read from the bridge.</summary>
        public event Action<EntertainmentArea> AreaLoaded;

        public HueEntertainmentSession(string configId, Action<string> log)
        {
            if (string.IsNullOrEmpty(configId))
                throw new ArgumentException("Entertainment configuration id is empty.", nameof(configId));
            _configId = configId;
            _log = log;
        }

        /// <summary>Connects when needed and allowed by the backoff. Returns true while streaming.</summary>
        public bool EnsureConnected()
        {
            if (State == SessionState.Streaming)
            {
                if (_healthFailed || !_dtls.Poll())
                    Fail(_healthFailed ? "bridge reports the area as not streaming" : "connection lost");
                else
                    StartHealthCheckIfDue();
            }
            if (State == SessionState.Streaming)
                return true;
            if (State == SessionState.Waiting && _sinceFailure.ElapsedMilliseconds < _backoffMs)
                return false;
            return Connect();
        }

        /// <summary>Sends one frame. rgb16 holds R, G, B per channel (0..65535).</summary>
        public void Send(byte[] channelIds, ushort[] rgb16, int count)
        {
            if (State != SessionState.Streaming)
                return;
            int n = Math.Min(count, HueStreamMessage.MaxChannels);
            int size = HueStreamMessage.Build(_message, _sequence++, _configId, channelIds, rgb16, n);
            try
            {
                _dtls.Send(_message, 0, size);
            }
            catch (HueDtlsException ex)
            {
                Fail(ex.Message);
            }
        }

        /// <summary>
        /// Closes the DTLS session and ends streaming mode on the bridge. With turnOff the lights of the
        /// area are switched off: the bridge keeps the last streamed color (or the state from before)
        /// otherwise, unlike the AmbiBack controllers that go dark without data.
        /// </summary>
        public void Stop(bool turnOff = true)
        {
            if (State == SessionState.Streaming && turnOff)
                SendBlack();
            CloseDtls(true);
            StopStreamingMode();
            State = SessionState.Idle;
            if (turnOff && _lightsUsed)
            {
                _lightsUsed = false;
                TurnLightsOff();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>A few black frames so the lights go dark at once, before the session ends.</summary>
        private void SendBlack()
        {
            EntertainmentArea area = Area;
            if (area == null)
                return;
            int n = Math.Min(area.Channels.Count, HueStreamMessage.MaxChannels);
            byte[] ids = new byte[n];
            for (int i = 0; i < n; i++)
                ids[i] = area.Channels[i].ID;
            ushort[] black = new ushort[n * 3];
            for (int i = 0; i < 3 && State == SessionState.Streaming; i++)
            {
                Send(ids, black, n);
                Thread.Sleep(20);
            }
        }

        private void TurnLightsOff()
        {
            EntertainmentArea area = Area;
            if (area == null)
                return;
            foreach (string light in area.LightServices)
            {
                try
                {
                    HueClipV2.SetLightOn(light, false);
                }
                catch (Exception ex)
                {
                    Log("switching light off failed: " + ex.Message);
                }
            }
            Log("lights of \"" + area.Name + "\" switched off");
        }

        private bool Connect()
        {
            State = SessionState.Connecting;
            try
            {
                string clientKey = Configuration.ClientKey;
                if (string.IsNullOrEmpty(clientKey))
                    throw new HueException(0, "No Hue client key, pair the bridge again.");

                // Fresh channel list (positions may have changed in the Hue app)
                Area = HueClipV2.GetEntertainmentArea(_configId);
                AreaLoaded?.Invoke(Area);

                // Lights switched off (e.g. by our own Stop) do not light up from streaming alone
                if (!_lightsUsed)
                    foreach (string light in Area.LightServices)
                        HueClipV2.SetLightOn(light, true);

                HueClipV2.SetStreaming(_configId, true);
                _streamingStarted = true;

                _dtls = new HueDtlsClient(new IPEndPoint(Configuration.DeviceIP, HueDtlsClient.HuePort),
                    Configuration.Username, TlsPrf.FromHex(clientKey))
                {
                    Log = _log,
                };
                _dtls.Connect(HandshakeTimeoutMs);

                State = SessionState.Streaming;
                _lightsUsed = true;
                LastError = null;
                _backoffMs = MinBackoffMs;
                _healthFailed = false;
                _healthErrors = 0;
                _sinceHealthCheck.Restart();
                Log("streaming to \"" + Area.Name + "\" with " + Area.Channels.Count + " channels");
                return true;
            }
            catch (Exception ex) when (ex is HueException || ex is HueDtlsException || ex is WebException
                || ex is System.Net.Sockets.SocketException || ex is InvalidOperationException || ex is FormatException
                || ex is System.Security.Cryptography.CryptographicException
                // A CLIP v2 request whose stream breaks while reading or writing throws IOException, not WebException
                || ex is System.IO.IOException || ex is TimeoutException)
            {
                Fail(ex.Message);
                return false;
            }
        }

        private void Fail(string reason)
        {
            LastError = reason;
            Log("disconnected: " + reason + ", retry in " + (_backoffMs / 1000) + " s");
            CloseDtls(false);
            StopStreamingMode();
            State = SessionState.Waiting;
            _sinceFailure.Restart();
            _backoffMs = Math.Min(_backoffMs * 2, MaxBackoffMs);
        }

        private void CloseDtls(bool notify)
        {
            HueDtlsClient dtls = _dtls;
            _dtls = null;
            if (dtls == null)
                return;
            if (notify)
                dtls.Close();
            else
                dtls.Dispose();
        }

        private void StopStreamingMode()
        {
            if (!_streamingStarted)
                return;
            _streamingStarted = false;
            try
            {
                HueClipV2.SetStreaming(_configId, false);
            }
            catch (Exception ex)
            {
                Log("stop failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Every few seconds read the area status in the background. Detects a bridge that is gone
        /// without an ICMP error (power cut, Wi-Fi) or another app that took over the area.
        /// </summary>
        private void StartHealthCheckIfDue()
        {
            if (_sinceHealthCheck.ElapsedMilliseconds < HealthCheckIntervalMs || (_healthTask != null && !_healthTask.IsCompleted))
                return;
            _sinceHealthCheck.Restart();
            string id = _configId;
            _healthTask = Task.Run(() =>
            {
                try
                {
                    EntertainmentArea area = HueClipV2.GetEntertainmentArea(id);
                    _healthErrors = 0;
                    if (!area.IsActive)
                        _healthFailed = true;
                }
                catch (Exception ex)
                {
                    // Two misses in a row (about 20 s) count as a lost bridge
                    if (Interlocked.Increment(ref _healthErrors) >= 2)
                        _healthFailed = true;
                    Log("health check failed: " + ex.Message);
                }
            });
        }

        private void Log(string text)
        {
            _log?.Invoke("HueStream|" + text);
        }
    }
}
