using System;
using System.Net;
using System.Net.Sockets;

namespace AmbiBack
{
    /// <summary>LED strip device that receives its frames over UDP (WLED/DDP, Govee).</summary>
    [Serializable]
    public abstract class CDeviceLedStripUdp : CDeviceLedStrip
    {
        private UdpClient udp;

        protected override void Connect(IPAddress ip)
        {
            udp = new UdpClient(ip.AddressFamily);
            udp.Connect(new IPEndPoint(ip, this.Port));
        }

        protected override void Disconnect()
        {
            try
            {
                udp?.Close();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
            udp = null;
        }

        /// <summary>Sends one datagram to the device.</summary>
        protected void SendDatagram(byte[] data)
        {
            udp.Send(data, data.Length);
        }

        /// <summary>The next datagram from the device, null after the timeout.</summary>
        protected byte[] ReceiveDatagram(int timeoutMs)
        {
            udp.Client.ReceiveTimeout = Math.Max(1, timeoutMs);
            try
            {
                IPEndPoint from = null;
                return udp.Receive(ref from);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut || ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                return null;
            }
        }
    }
}
