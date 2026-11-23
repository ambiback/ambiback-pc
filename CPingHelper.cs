using System;

//Custom
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AmbiBack
{
    // Helper Class IcmpPacket
    public class IcmpPacket
    {
        public Byte Type;               // type of message
        public Byte SubCode;            // type of sub code
        public UInt16 CheckSum;         // ones complement checksum of struct
        public UInt16 Identifier;       // identifier
        public UInt16 SequenceNumber;   // sequence number  
        public Byte[] Data;         // The data
    }

    /// <summary>
    /// Summary description for CPingHelper.
    /// </summary>
    public class CPingHelper
    {
        #region Public ARRAYS needed
        // Displays Text representation of the DeviceType
        public static string[] IcmpDestFailedCodes = new string[]
        {
			// Grabbed from http://en.wikipedia.org/wiki/ICMP_Destination_Unreachable
			"Network unreachable error.",
            "Host unreachable error.",
            "Protocol unreachable error (the designated transport protocol is not supported).",
            "Port unreachable error (the designated protocol is unable to inform the host of the incoming message).",
            "The datagram is too big. Packet fragmentation is required but the 'don't fragment' (DF) flag is on.",
            "Source route failed error.",
            "Destination network unknown error.",
            "Destination host unknown error.",
            "Source host isolated error (military use only).",
            "The destination network is administratively prohibited.",
            "The destination host is administratively prohibited.",
            "The network is unreachable for Type Of Service.",
            "The host is unreachable for Type Of Service.",
            "Communication administratively prohibited (administrative filtering prevents packet from being forwarded).",
            "Host precedence violation (indicates the requested precedence is not permitted for the combination of host or network and port).",
            "Precedence cutoff in effect (precedence of datagram is below the level set by the network administrators).",
        };

        public static string[] IcmpTimeExceededCodes = new string[]
        {
            "time to live exceeded in transit",
            "fragment reassembly time exceeded"
        };
        #endregion

        // Stuff for the Ping
        public Socket mySocket;
        public Random myRnd;
        public string szLastErrorMsg;
        public string szPingTargetHost;
        public int iMyTimeout;
        public int iMyBytes2Send;

        public EndPoint epRecipient;
        public EndPoint epSender;
        public IPAddress myIPRecipient;
        public IPAddress myIPSender;
        public string szIPSender;

        // Needed Constant values
        public static byte error_success = 0;
        public static byte error_datagram_null = 1;
        public static byte error_datagram_not_complete = 4;
        public static byte error_datagram_checksum = 6;
        public static byte Icmp_EchoReply = 0;
        public static byte Icmp_DestinationUnreachable = 3;
        public static byte Icmp_SourceQuench = 4;
        public static byte Icmp_Redirect = 5;
        public static byte Icmp_Echo = 8;
        public static byte Icmp_TimeExceeded = 11;
        public static byte Icmp_ParameterProblem = 12;
        public static byte Icmp_Timestamp = 13;
        public static byte Icmp_TimestampReply = 14;
        public static byte Icmp_InformationRequest = 15;
        public static byte Icmp_InformationReply = 16;
        public static int SOCKET_ERROR = -1;
        public static int MAX_TTL = 256;        // Max TTL

        // Needed for receive handling!
        private byte[] m_bRecvBuf;


        public CPingHelper()
        {
            mySocket = null;
            myRnd = new Random();

            szPingTargetHost = "";
            szLastErrorMsg = "";
            epRecipient = null;
            epSender = null;

            iMyTimeout = 2000;              // 2000 ms is enough as default
            iMyBytes2Send = 32;             // 32 bytes per default

            m_bRecvBuf = new byte[4096];    // Default buffer
        }

        #region Targethost Handling
        public bool CreateEndPoints(string szNewHost)
        {
            // Set the host
            szPingTargetHost = szNewHost;

            //IPHostEntries needed for resolving
            myIPRecipient = null;
            myIPSender = null;

            // Reset if needed
            if (epRecipient != null)
                epRecipient = null;
            if (epSender != null)
                epSender = null;

            // Get the server endpoint
            try
            {
                lock (this)
                {
                    myIPRecipient = IPAddress.Parse(szPingTargetHost);
                }
            }
            catch (SocketException ex)
            {
                // fail
                szLastErrorMsg = "Host not found - Errormessage: " + ex.Message + " Code: " + ex.ErrorCode;
                return false;
            }
            IPAddress.Parse(szPingTargetHost);

            // Convert the server IP_EndPoint to an EndPoint
            epRecipient = new IPEndPoint(myIPRecipient, 0);

            // Set the receiving endpoint to the client machine
            IPAddress[] myIps = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress myIp in myIps)
            {
                if (myIPRecipient.AddressFamily == myIp.AddressFamily)
                {
                    myIPSender = myIp;
                    break;
                }
            }

            // Abort on failed sender
            if (myIPSender == null)
                return false;

            epSender = new IPEndPoint(myIPSender, 0);
            szIPSender = ((IPEndPoint)epSender).Address.ToString();

            //Got Address with success
            return true;
        }
        #endregion

        #region Socket Controlling Functions
        public void InitSocket()
        {
            //Initilize a Socket of the Type ICMP
            if (mySocket == null)
                mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);

            //Set Socket Timeouts
            mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, iMyTimeout);
            mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, iMyTimeout);
        }

        public void DestroySocket()
        {
            //close the socket
            if (mySocket != null)
                mySocket.Close();

            // leave no references
            mySocket = null;
        }

        public bool SendIcmpPacket(ref byte[] mySendBuf)
        {
            try
            {
                //send the Pack over the socket
                if (mySocket.SendTo(mySendBuf, mySendBuf.Length, 0, epRecipient) == SOCKET_ERROR)
                {
                    //DEBUG! 
                    szLastErrorMsg = "SocketError: cannot Send Packet";
                    return false;
                }
                else
                    return true;
            }
            catch (SocketException ex)
            {
                //Do nothing
                // DEBUG! 
                szLastErrorMsg = "SocketError: cannot Send Packet: " + ex.Message + " Code: " + ex.ErrorCode;
                return false;
            }
        }

        public bool RecvIcmpPacket(ref byte[] myRecvBuf, ref int iMyByteRecv)
        {
            myRecvBuf = m_bRecvBuf;

            /*			// Initialize the buffers. The receive buffer is the size of the
                        // ICMP header plus the IP header (20 bytes)
                        new Byte[ mySocket.Available + 20]; 
                        */
            iMyByteRecv = 0;

            try
            {
                iMyByteRecv = mySocket.ReceiveFrom(myRecvBuf, myRecvBuf.Length, 0, ref epSender);
                if (iMyByteRecv == SOCKET_ERROR || iMyByteRecv == 0)
                {
                    // Ping wasn't successfull!
                    szLastErrorMsg = "Error: Ping send, but Host not Responding";
                    return false;
                }

                // Return Success as we have received at least one Packet
                return true;
            }
            catch (Exception ex)
            {
                // Exception while receiving 
                szLastErrorMsg = "SocketError: cannot Receive Packet: " + ex.Message;
                return false;
            }
        }

        public bool ComparePacketForTraceRoute(ref byte[] myRecvBuf, int iMyByteRecv, ref byte[] mySendBuf, int iMyByteSend)
        {
            // Check if the data length, received packet must be 16 bytes more!
            if ((iMyByteRecv - 16) == (iMyByteSend))
            {
                // We compare byte 41to48, as it contains Sender & Recipient IP!
                string szIPSource = String.Format("{0}.{1}.{2}.{3}", myRecvBuf[40], myRecvBuf[41], myRecvBuf[42], myRecvBuf[43]);
                string szDest = String.Format("{0}.{1}.{2}.{3}", myRecvBuf[44], myRecvBuf[45], myRecvBuf[46], myRecvBuf[47]);

                if (szIPSender.Equals(szIPSource) && ((IPEndPoint)epRecipient).Address.ToString().Equals(szDest))
                {
                    // ok so far correct ;)! No check lat 8 bytes
                    int iDataLen = 8;
                    byte[] myDataReceived = new byte[iDataLen];
                    byte[] myDataSend = new byte[iDataLen];
                    Array.Copy(myRecvBuf, 48, myDataReceived, 0, iDataLen);
                    Array.Copy(mySendBuf, 0, myDataSend, 0, iDataLen);

                    return CGlobals.CompareByteArrays(myDataSend, myDataReceived, iDataLen);
                }
                else
                    return false;   // nonono
            }
            else
                // hrm check if we received last message or router msg!
                return CompareRecContentWithSend(ref myRecvBuf, iMyByteRecv, ref mySendBuf, iMyByteSend);
        }

        public bool CompareRecContentWithSend(ref byte[] myRecvBuf, int iMyByteRecv, ref byte[] mySendBuf, int iMyByteSend)
        {
            // Check if the data length matches
            if ((iMyByteRecv - 28) == (iMyByteSend - 8))
            {
                // packet seems to be for us, so lets compare the data!
                int iDataLen = iMyByteRecv - 28;
                byte[] myDataReceived = new byte[iDataLen];
                byte[] myDataSend = new byte[iDataLen];
                Array.Copy(myRecvBuf, 28, myDataReceived, 0, iDataLen);
                Array.Copy(mySendBuf, 8, myDataSend, 0, iDataLen);
                return CGlobals.CompareByteArrays(myDataSend, myDataReceived, iDataLen);
            }
            else if ((iMyByteRecv) == (iMyByteSend + 48))
            {
                // Seems like a router msg, so lets compare the last 40 bytes!
                int iDataLen = 40;
                byte[] myDataReceived = new byte[iDataLen];
                Array.Copy(myRecvBuf, 48, myDataReceived, 0, iDataLen);
                return CGlobals.CompareByteArrays(mySendBuf, myDataReceived, iDataLen);
            }
            else
                return false;   // Size doesn't match
        }


        public bool CheckSourceOfPacket(ref byte[] myRecvBuf, int iMyByteRecv, ref byte[] mySendBuf, int iMyByteSend)
        {
            bool bReturn = CPingHelper.CheckSourceOfPacket(ref myRecvBuf, iMyByteRecv, myIPRecipient.ToString());
            if (bReturn)    // Also compare packet content
                return CompareRecContentWithSend(ref myRecvBuf, iMyByteRecv, ref mySendBuf, iMyByteSend);

            // Reach here? Return our result!
            return bReturn;
        }

        public static bool CheckSourceOfPacket(ref byte[] myRecvBuf, int iMyByteRecv, string myIP2Match)
        {
            string strIPAddress = String.Format("{0}.{1}.{2}.{3}", myRecvBuf[12], myRecvBuf[13], myRecvBuf[14], myRecvBuf[15]);
            //			IPAddress srcIPAddress = System.Net.IPAddress.Parse(strIPAddress); 
            if (strIPAddress.Equals(myIP2Match))
                return true;    // packet is for us
            else
                return false;   // packet is not from us
        }
        #endregion

        #region IcmpPacket Controlling Functions
        public bool ResolveIcmpPacket(ref Byte[] bMyBuf, int iByteLen, ref IcmpPacket myPacket)
        {
            if (iByteLen <= 8)
            {
                // packet is to small!
                szLastErrorMsg = "Error, packet is to small!";
                return false;
            }

            if (myPacket == null)
                myPacket = new IcmpPacket();
            myPacket.Type = bMyBuf[20];
            myPacket.SubCode = bMyBuf[21];
            myPacket.CheckSum = (UInt16)(bMyBuf[22] + (bMyBuf[23] * 256));
            myPacket.SequenceNumber = (UInt16)(bMyBuf[24] + (bMyBuf[25] * 256));
            myPacket.Identifier = (UInt16)(bMyBuf[26] + (bMyBuf[27] * 256));

            // Copy data
            myPacket.Data = new byte[iByteLen - 28];
            Array.Copy(bMyBuf, 28, myPacket.Data, 0, iByteLen - 28);

            // reached here, return true!
            return true;
        }


        private void CreateIcmpPacket(ref IcmpPacket myPacket)
        {
            //Fixed, recreate the  whole packet everytime so there is a random ID!
            if (myPacket == null)
                myPacket = new IcmpPacket();
            myPacket.Type = Icmp_Echo; //8
            myPacket.SubCode = 0;
            myPacket.CheckSum = UInt16.Parse("0");
            myPacket.SequenceNumber = UInt16.Parse("0");
            myPacket.Identifier = (ushort)(myRnd.Next(65000));  //any number you feel is kinda unique  //UInt16.Parse("45"); 
            myPacket.Data = new Byte[iMyBytes2Send];

            // --- New create the data, first copy the packet header
            // 
            // Old Initilize the Packet.Data
            //			for (int i = 0; i < iMyBytes2Send; i++)
            //				myPacket.Data[i] = (byte)'#';
            myPacket.Data[0] = (byte)Icmp_Echo;
            myPacket.Data[1] = 0;
            myPacket.Data[2] = 0;
            myPacket.Data[3] = 0;
            myPacket.Data[4] = 0;
            myPacket.Data[5] = 0;
            myPacket.Data[6] = (byte)(myPacket.Identifier / 256);
            myPacket.Data[7] = (byte)(myPacket.Identifier % 256);
            byte[] myTsStamp = System.Text.Encoding.ASCII.GetBytes(DateTime.Now.Ticks.ToString());
            // Fill the data with the timeticks
            for (int i = 8; i < iMyBytes2Send; i += 8)
            {
                myPacket.Data[i] = myTsStamp[0];
                myPacket.Data[i + 1] = myTsStamp[1];
                myPacket.Data[i + 2] = myTsStamp[2];
                myPacket.Data[i + 3] = myTsStamp[3];
                myPacket.Data[i + 4] = myTsStamp[4];
                myPacket.Data[i + 5] = myTsStamp[5];
                myPacket.Data[i + 6] = myTsStamp[6];
                myPacket.Data[i + 7] = myTsStamp[7];
            }
            // --- 
        }
        public int CreateWholeIPPacket(IcmpPacket myPacket, ref byte[] mySendBuf)
        {
            // Check for myPacket first
            if (myPacket == null)
                CreateIcmpPacket(ref myPacket);

            //Variable to hold the total Packet size
            int PacketSize = iMyBytes2Send + 8;
            Byte[] icmp_pkt_buffer = new Byte[PacketSize];
            Int32 Index = 0;

            //Call a Method Serialize which counts
            //The total number of Bytes in the Packet
            Index = Serialize(myPacket, icmp_pkt_buffer, PacketSize, iMyBytes2Send);

            //Error in Packet Size
            if (Index == -1)
            {
                //DEBUG!
                szLastErrorMsg = "Error while creating ICMP Packet";
                return -1;
            }

            // now get this critter into a UInt16 array
            //Get the Half size of the Packet
            Double double_length = Convert.ToDouble(Index);
            Double dtemp = Math.Ceiling(double_length / 2);
            int cksum_buffer_length = Convert.ToInt32(dtemp);
            //Create a Byte Array
            UInt16[] cksum_buffer = new UInt16[cksum_buffer_length];

            //Code to initialize the Uint16 array 
            int icmp_header_buffer_index = 0;
            for (int i = 0; i < cksum_buffer_length; i++)
            {
                cksum_buffer[i] = BitConverter.ToUInt16(icmp_pkt_buffer, icmp_header_buffer_index);
                icmp_header_buffer_index += 2;
            }

            //Call a method which will return a checksum             
            UInt16 u_cksum = checksum(cksum_buffer, cksum_buffer_length);
            //Save the checksum to the Packet
            myPacket.CheckSum = u_cksum;

            // Now that we have the checksum, serialize the packet again
            mySendBuf = new Byte[PacketSize];

            //again check the packet size
            Index = Serialize(myPacket, mySendBuf, PacketSize, iMyBytes2Send);
            //if there is a error report it
            if (Index == -1)
            {
                //DEBUG! 
                szLastErrorMsg = "Error while creating ICMP Packet";
                return -1;
            }
            else
                return PacketSize;  // We return the datasize here!
        }
        #endregion

        #region PING Helper Functions
        /// <summary>
        ///  This method get the Packet and calculates the total size 
        ///  of the Pack by converting it to byte array
        /// </summary>
        private Int32 Serialize(IcmpPacket packet, Byte[] Buffer, Int32 PacketSize, Int32 PingData)
        {
            Int32 cbReturn = 0;
            // serialize the struct into the array
            int Index = 0;
            Byte[] b_type = new Byte[1];
            b_type[0] = (packet.Type);
            Byte[] b_code = new Byte[1];
            b_code[0] = (packet.SubCode);
            Byte[] b_cksum = BitConverter.GetBytes(packet.CheckSum);
            Byte[] b_id = BitConverter.GetBytes(packet.Identifier);
            Byte[] b_seq = BitConverter.GetBytes(packet.SequenceNumber);

            // Console.WriteLine("Serialize type ");
            Array.Copy(b_type, 0, Buffer, Index, b_type.Length);
            Index += b_type.Length;

            // Console.WriteLine("Serialize code ");
            Array.Copy(b_code, 0, Buffer, Index, b_code.Length);
            Index += b_code.Length;
            // Console.WriteLine("Serialize cksum ");
            Array.Copy(b_cksum, 0, Buffer, Index, b_cksum.Length);
            Index += b_cksum.Length;
            // Console.WriteLine("Serialize id ");
            Array.Copy(b_id, 0, Buffer, Index, b_id.Length);
            Index += b_id.Length;
            Array.Copy(b_seq, 0, Buffer, Index, b_seq.Length);
            Index += b_seq.Length;
            // copy the data        
            Array.Copy(packet.Data, 0, Buffer, Index, PingData);
            Index += PingData;
            if (Index != PacketSize/* sizeof(IcmpPacket)  */)
            {
                cbReturn = -1;
                return cbReturn;
            }
            cbReturn = Index;
            return cbReturn;
        }


        /// <summary>
        ///This Method has the algorithm to make a checksum 
        /// </summary>
        private UInt16 checksum(UInt16[] buffer, int size)
        {
            Int32 cksum = 0;
            int counter;
            counter = 0;
            while (size > 0)
            {
                UInt16 val = buffer[counter];
                cksum += Convert.ToInt32(buffer[counter]);
                counter += 1;
                size -= 1;
            }
            cksum = (cksum >> 16) + (cksum & 0xffff);
            cksum += (cksum >> 16);
            return (UInt16)(~cksum);
        }
        #endregion

    }
}
