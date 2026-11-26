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
    }
}
