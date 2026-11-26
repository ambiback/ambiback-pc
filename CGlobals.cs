using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Custom
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using AmbiBack.PhilipsHue;
using System.Runtime.CompilerServices;
using System.Drawing.Imaging;
using System.Globalization;
using System.Security;

namespace AmbiBack
{
    #region public Event defs
    public delegate void EventDebugOutput(object sender, System.EventArgs e, string szDebug);
    public delegate void EventUpdateStatus(object sender, System.EventArgs e, string szStatus);
    #endregion

    #region API Imports Enums
    // PlaySound ENUM Helper
    public enum PlaySoundFlags : int
    {
        SND_SYNC = 0x0000,  /* play synchronously (default) */
        SND_ASYNC = 0x0001,  /* play asynchronously */
        SND_NODEFAULT = 0x0002,  /* silence (!default) if sound not found */
        SND_MEMORY = 0x0004,  /* pszSound points to a memory file */
        SND_LOOP = 0x0008,  /* loop the sound until next sndPlaySound */
        SND_NOSTOP = 0x0010,  /* don't stop any currently playing sound */
        SND_NOWAIT = 0x00002000, /* don't wait if the driver is busy */
        SND_ALIAS = 0x00010000, /* name is a registry alias */
        SND_ALIAS_ID = 0x00110000, /* alias is a predefined ID */
        SND_FILENAME = 0x00020000, /* name is file name */
        SND_RESOURCE = 0x00040004  /* name is resource name or atom */
    }
    #endregion

    #region Public ENUMs needed
    // Enum for State
    public enum MoreModes
    {
        Link = 0,
        Button = 1,
        None = 2
    }

    public enum SerialSpeed
    {
        Normal = 115200, 
        FastESP32 = 921600,
        FastM5Stack = 1500000
    }
    #endregion

    #region Helper Class for ListBoxes
    public class ListBoxDevice
    {
        private string myShortName;
        private string myLongName;
        private int myID; 

        public ListBoxDevice(string strLongName, string strShortName, int iID)
        {

            this.myShortName = strShortName;
            this.myLongName = strLongName;
            this.myID = iID;
        }

        public int ID
        {
            get { return myID; }
        }

        public string ShortName
        {
            get { return myShortName; }
        }

        public string LongName
        {
            get { return myLongName; }
        }

        public override string ToString()
        {
            return this.ShortName + " - " + this.LongName;
        }
    }
    #endregion

    #region Helper exception class to reload Form constants!
    public class ReloadException : Exception
    {
        public ReloadException()
        {
            // nothing
            return;
        }

    }
    #endregion

    #region Helper Class BitMapFilters 
    public class ConvMatrix
    {
        public int TopLeft = 0, TopMid = 0, TopRight = 0;
        public int MidLeft = 0, Pixel = 1, MidRight = 0;
        public int BottomLeft = 0, BottomMid = 0, BottomRight = 0;
        public int Factor = 1;
        public int Offset = 0;
        public void SetAll(int nVal)
        {
            TopLeft = TopMid = TopRight = MidLeft = Pixel = MidRight = BottomLeft = BottomMid = BottomRight = nVal;
        }
    }
    public class BitmapFilter
    {
        public static bool Brightness(Bitmap b, byte bBrightNess)
        {
            bool bSuccess = false; 

            unsafe
            {
                BitmapData bmlData = null;

                try
                {
                    // Lock ImageData | workarround GetPixel() locking, using direct access on bit data!
                    bmlData = b.LockBits(new System.Drawing.Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    byte* srcPointer = (byte*)bmlData.Scan0;

                    // Found using performance profiler
                    int screenshotWidth = b.Width;
                    int screenshotHeight= b.Height;
                    int screenshotPixelsBytes = screenshotWidth * screenshotHeight * 4; // 4 bytes per pixel

                    for (int i=0; i < screenshotPixelsBytes; i++)
                    {
                        if (i % 4 != 3) {
                            srcPointer[i] = (byte) ((srcPointer[i] * bBrightNess) / 255);
                        }
                    }

                    bSuccess = true; 
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput("CGlobals|Brightness|Exception: " + ex.ToString());
                }
                finally
                {
                    // Unlock Imagedata
                    if (bmlData != null)
                        b.UnlockBits(bmlData);
                }
            }

            return bSuccess; 
        }

        private static bool Conv3x3(Bitmap b, ConvMatrix m)
        {
            // Avoid divide by zero errors
            if (0 == m.Factor) return false;

            Bitmap bSrc = (Bitmap)b.Clone();

            // GDI+ still lies to us - the return format is BGR, NOT RGB.
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int stride = bmData.Stride;
            int stride2 = stride * 2;
            System.IntPtr Scan0 = bmData.Scan0;
            System.IntPtr SrcScan0 = bmSrc.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;
                byte* pSrc = (byte*)(void*)SrcScan0;

                int nOffset = stride + 6 - b.Width * 3;
                int nWidth = b.Width - 2;
                int nHeight = b.Height - 2;

                int nPixel;

                for (int y = 0; y < nHeight; ++y)
                {
                    for (int x = 0; x < nWidth; ++x)
                    {
                        nPixel = ((((pSrc[2] * m.TopLeft) + (pSrc[5] * m.TopMid) + (pSrc[8] * m.TopRight) +
                            (pSrc[2 + stride] * m.MidLeft) + (pSrc[5 + stride] * m.Pixel) + (pSrc[8 + stride] * m.MidRight) +
                            (pSrc[2 + stride2] * m.BottomLeft) + (pSrc[5 + stride2] * m.BottomMid) + (pSrc[8 + stride2] * m.BottomRight)) / m.Factor) + m.Offset);

                        if (nPixel < 0) nPixel = 0;
                        if (nPixel > 255) nPixel = 255;

                        p[5 + stride] = (byte)nPixel;

                        nPixel = ((((pSrc[1] * m.TopLeft) + (pSrc[4] * m.TopMid) + (pSrc[7] * m.TopRight) +
                            (pSrc[1 + stride] * m.MidLeft) + (pSrc[4 + stride] * m.Pixel) + (pSrc[7 + stride] * m.MidRight) +
                            (pSrc[1 + stride2] * m.BottomLeft) + (pSrc[4 + stride2] * m.BottomMid) + (pSrc[7 + stride2] * m.BottomRight)) / m.Factor) + m.Offset);

                        if (nPixel < 0) nPixel = 0;
                        if (nPixel > 255) nPixel = 255;

                        p[4 + stride] = (byte)nPixel;

                        nPixel = ((((pSrc[0] * m.TopLeft) + (pSrc[3] * m.TopMid) + (pSrc[6] * m.TopRight) +
                            (pSrc[0 + stride] * m.MidLeft) + (pSrc[3 + stride] * m.Pixel) + (pSrc[6 + stride] * m.MidRight) +
                            (pSrc[0 + stride2] * m.BottomLeft) + (pSrc[3 + stride2] * m.BottomMid) + (pSrc[6 + stride2] * m.BottomRight)) / m.Factor) + m.Offset);

                        if (nPixel < 0) nPixel = 0;
                        if (nPixel > 255) nPixel = 255;

                        p[3 + stride] = (byte)nPixel;

                        p += 3;
                        pSrc += 3;
                    }

                    p += nOffset;
                    pSrc += nOffset;
                }
            }

            b.UnlockBits(bmData);
            bSrc.UnlockBits(bmSrc);

            return true;
        }

        public static bool GaussianBlur(Bitmap b, int nWeight /* default to 4*/)
        {
            ConvMatrix m = new ConvMatrix();
            m.SetAll(1);
            m.Pixel = nWeight;
            m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = 2;
            m.Factor = nWeight + 12;

            return BitmapFilter.Conv3x3(b, m);
        }
    }
    #endregion

    #region Public ENUMS needed
    public enum DeviceType
    {
        UnknownDevice = 0,
        DreamScreenHD = 1,
        DreamScreen4K = 2,
        DreamScreen4KSolo = 3,
        DreamScreenSideKick = 4,
        AmbiBackController = 5,
        PhilipsHueColorLight = 6,
        AmbiBack_Pc_Controller = 7,
        AmbiBack_Bridge_Controller = 8,
        AmbiBack_Cam_wr_Controller = 9,
        AmbiBack_Cam_tt_Controller = 10,
        AmbiBack_Cam_pl_Controller = 11,
        AmbiBack_Cam_jr_Controller = 12,
        AmbiBack_Cam_mi_Controller = 13,
        AmbiBack_Cam_m5_Controller = 14,
        AmbiBack_Cam_5x_Controller = 15,
        AmbiBack_Cam_5tx_Controller = 16,
        AmbiBack_Pc_M5AL_Controller = 17,
        AmbiBack_Pc_M5AX_Controller = 18,
        AmbiBack_Led_M5A1_Controller = 19,
        PhilipsHueEntertainment = 20,
        WledDdp = 21,
        GoveeLan = 22,
        OpenRgb = 23,
        Nanoleaf = 24,
    }
    public enum ConnectionTypes
    {
        TCP = 0,
        UDP = 1,
        USB = 2,
    }
    #endregion

    public class CGlobals
    {
        #region PowerSetting Notification api
        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification",
        CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, Int32 Flags);
        [DllImport(@"User32", SetLastError = true, EntryPoint = "UnregisterPowerSettingNotification",
        CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr UnregisterPowerSettingNotification(IntPtr hRecipient);

        public const int WM_POWERBROADCAST = 0x0218;
        public static Guid GUID_BATTERY_PERCENTAGE_REMAINING = new Guid("A7AD8041-B45A-4CAE-87A3-EECBB468A9E1");
        public static Guid GUID_MONITOR_POWER_ON = new Guid(0x02731015, 0x4510, 0x4526, 0x99, 0xE6, 0xE5, 0xA1, 0x7E, 0xBD, 0x1A, 0xEA);
        public static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid(0x6fe69556, 0x704a, 0x47a0, 0x8f, 0x24, 0xc2, 0x8d, 0x93, 0x6f, 0xda, 0x47);
        public static Guid GUID_ACDC_POWER_SOURCE = new Guid(0x5D3E9A59, 0xE9D5, 0x4B00, 0xA6, 0xBD, 0xFF, 0x34, 0xFF, 0x51, 0x65, 0x48);
        public static Guid GUID_POWERSCHEME_PERSONALITY = new Guid(0x245D8541, 0x3943, 0x4422, 0xB0, 0x25, 0x13, 0xA7, 0x84, 0xF6, 0x79, 0xB7);
        public static Guid GUID_MAX_POWER_SAVINGS = new Guid(0xA1841308, 0x3541, 0x4FAB, 0xBC, 0x81, 0xF7, 0x15, 0x56, 0xF2, 0x0B, 0x4A);
        // GUID_SESSION_DISPLAY_STATUS  2B84C20E-AD23-4ddf-93DB-05FFBD7EFCA5
        // No Power Savings - Almost no power savings measures are used.
        public static Guid GUID_MIN_POWER_SAVINGS = new Guid(0x8C5E7FDA, 0xE8BF, 0x4A96, 0x9A, 0x85, 0xA6, 0xE2, 0x3A, 0x8C, 0x63, 0x5C);
        // Typical Power Savings - Fairly aggressive power savings measures are used.
        public static Guid GUID_TYPICAL_POWER_SAVINGS = new Guid(0x381B4222, 0xF694, 0x41F0, 0x96, 0x85, 0xFF, 0x5B, 0xB2, 0x60, 0xDF, 0x2E);

        // Win32 decls and defs
        const int PBT_APMQUERYSUSPEND = 0x0000;
        const int PBT_APMQUERYSTANDBY = 0x0001;
        const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
        const int PBT_APMQUERYSTANDBYFAILED = 0x0003;
        const int PBT_APMSUSPEND = 0x0004;
        const int PBT_APMSTANDBY = 0x0005;
        const int PBT_APMRESUMECRITICAL = 0x0006;
        const int PBT_APMRESUMESUSPEND = 0x0007;
        const int PBT_APMRESUMESTANDBY = 0x0008;
        const int PBT_APMBATTERYLOW = 0x0009;
        const int PBT_APMPOWERSTATUSCHANGE = 0x000A; // power status
        const int PBT_APMOEMEVENT = 0x000B;
        const int PBT_APMRESUMEAUTOMATIC = 0x0012;
        public const int PBT_POWERSETTINGCHANGE = 0x8013; // DPPE

        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        public const int DEVICE_NOTIFY_SERVICE_HANDLE = 0x00000001;

        // This structure is sent when the PBT_POWERSETTINGSCHANGE message is sent.
        // It describes the power setting that has changed and contains data about the change
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }
        #endregion

        #region API Imports
        [DllImport("winmm.dll")]
        public static extern bool PlaySound(string szSound, IntPtr hMod, PlaySoundFlags flags);

        [DllImport("kernel32.dll")]
        public static extern bool Beep(int freq, int dur);

        [DllImport("user32.dll")]
        private static extern int GetSystemMenu(int hWnd, int bRevert);
        [DllImport("user32.dll")]
        private static extern int DeleteMenu(int hMenu, int uPosition, int uFlags);
        [DllImport("user32.dll")]
        private static extern int DrawMenuBar(int hMenu);
        [DllImport("user32.dll")]
        private static extern int EnableMenuItem(int hMenu, int uIDEnableItem, int uEnable);

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);


        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetProcessDpiAwarenessContext(int dpiFlag);
        [DllImport("SHCore.dll", SetLastError = true)]
        internal static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
        [DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();
        internal enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }
        internal enum DPI_AWARENESS_CONTEXT
        {
            DPI_AWARENESS_CONTEXT_UNAWARE = 16,
            DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = 17,
            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = 18,
            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = 34
        }
        #endregion

        #region Public ARRAYs
        public static string[] CONNECTIONTYPES = new string[]
        {
            "TCP (Requires stable WIFI)",
            "UDP (All WIFI)",
            "USB (Serial)",
        };

        public static string[] DEVICETYPES = new string[]
        {
            "UnknownDevice",
            "DreamScreenHD",
            "DreamScreen4K",
            "DreamScreen4KSolo",
            "DreamScreenSideKick",
            "AmbiBackController",
            "PhilipsHueColorLight",
            "AmbiBack_Pc_Controller",
            "AmbiBack_Bridge_Controller",
            "AmbiBack_Cam_wr_Controller",
            "AmbiBack_Cam_tt_Controller",
            "AmbiBack_Cam_pl_Controller",
            "AmbiBack_Cam_jr_Controller",
            "AmbiBack_Cam_mi_Controller",
            "AmbiBack_Cam_m5_Controller",
            "AmbiBack_Cam_5x_Controller",
            "AmbiBack_Cam_5tx_Controller",
            "AmbiBack_Pc_M5AL_Controller",
            "AmbiBack_Pc_M5AX_Controller",
            "AmbiBack_Led_M5A1_Controller",
            "PhilipsHueEntertainment",
            "WledDdp",
            "GoveeLan",
            "OpenRgb",
            "Nanoleaf",
        };
        public static string[] DEVICETYPESDISPLAYNAME = new string[]
        {
            "Unknown Device",
            "DreamScreen HD",
            "DreamScreen 4K",
            "DreamScreen 4K Solo",
            "DreamScreen SideKick",
            "AmbiBack Controller",
            "Philips Hue Colorlight",
            "AmbiBack PC",
            "AmbiBack Bridge",
            "AmbiBack Cam",
            "AmbiBack Cam TTGO",
            "AmbiBack Cam TTGO Plus",
            "AmbiBack Cam TTGO T-Journal",
            "AmbiBack Cam TTGO Mic",
            "AmbiBack_Cam M5 ESP32 Cam",
            "AmbiBack_Cam M5 OV2640",
            "AmbiBack_Cam M5 OV3660",
            "AmbiBack PC M5 Lite",
            "AmbiBack PC M5 Matrix",
            "AmbiBack PC M5 LED REV1",
            "Philips Hue Entertainment",
            "WLED / DDP",
            "Govee (LAN)",
            "OpenRGB",
            "Nanoleaf",
        };
        public static string[] INSTALLATIONTYPES = new string[]
        {
            "Classic",
            "Mega",
            "Xtreme",
            "Flex/DIY",
        };
        public static string[] FLEXSETUPS = new string[]
        {
            "Unknown Setup!",
            "Right Counterclock - bottom right up",
            "Top Counterclock - top right left",
            "Left Counterclock - top left down",
            "Bottom Counterclock - bottom left right",
            "Right Clock - top right down",
            "Top Clock - top left right",
            "Left Clock - bottom left up",
            "Bottom Clock - bottom right left",
        };
        #endregion

        #region SystemEvents Handler 
        public static void SessionEnded(object sender, SessionEndedEventArgs e)
        {
            // e.Reason == SessionEndReasons.Logoff
            CGlobals.DebugOutput("CGlobals:SessionEnded:" + e.Reason.ToString());
        }

        public static void PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            CGlobals.DebugOutput("CGlobals:PowerModeChanged:" + e.Mode.ToString());
        }

        public static void DisplaySettingsChanged(object sender, EventArgs e)
        {
            CGlobals.DebugOutput("CGlobals:DisplaySettingsChanged:" + e.ToString() );
        }
        #endregion

        #region Global Static Variables
        static public bool monitorPowerStatus = true;           /* defines if Monitor Power is turned on */
        static public bool loggedinStatus = true;               /* defines if user is logged in */
        static public bool appIsStarting = true;                /* if Monitor Power is turned on */
        static public bool appIsStopping = false;               /* if App is being finished! */
        static public bool IsFirstTime = true;      
        static public string szApplicationPath;
        static public bool UseNewDXGI = false;                  /* If enabled, we can use newer DXGI API */

        // Static Constant strings
        static public int SCREENSHOT_WIDTH = 640;
        static public int SCREENSHOT_HEIGHT = 360;
        static public int SCREENSHOT_MAX_FPS = 60;

        // Static Constant strings
        static public string AMBIBACK_BROADCAST_DISCOVER = "HELLO AMBIBACK DEVICES"; 

        // Hue Properties
        static public bool bHueConnected = false;
        //        static public string szHueDeviceIP = "";
        static public string szHueAppID = string.Format("{0}#{1}#{2}", "AmbiBack.PC", Environment.MachineName, Environment.UserName);
        static public LightCollection HueLights = null;
        static public List<EntertainmentArea> HueEntertainmentAreas = null;   // null: not loaded (no client key or no bridge)

        static public readonly int SerialReadTimeout = 250;
        static public readonly int SerialWriteTimeout = 500;
        #endregion

        // Debug Helpers
#if (DEBUG)
        static private StreamWriter fwDebug = null;
#endif
        #region CHECKSUM Helper 
        static byte CRC_START_8 = 0x00;
        /*
         * static uint8_t sht75_crc_table[];
         *
         * The SHT75 humidity sensor is capable of calculating an 8 bit CRC checksum to
         * ensure data integrity. The lookup table crc_table[] is used to recalculate
         * the CRC. 
         */
        static readonly byte[] sht75_crc_table = new byte[]{
          0,   49,  98,  83,  196, 245, 166, 151, 185, 136, 219, 234, 125, 76,  31,  46,
          67,  114, 33,  16,  135, 182, 229, 212, 250, 203, 152, 169, 62,  15,  92,  109,
          134, 183, 228, 213, 66,  115, 32,  17,  63,  14,  93,  108, 251, 202, 153, 168,
          197, 244, 167, 150, 1,   48,  99,  82,  124, 77,  30,  47,  184, 137, 218, 235,
          61,  12,  95,  110, 249, 200, 155, 170, 132, 181, 230, 215, 64,  113, 34,  19,
          126, 79,  28,  45,  186, 139, 216, 233, 199, 246, 165, 148, 3,   50,  97,  80,
          187, 138, 217, 232, 127, 78,  29,  44,  2,   51,  96,  81,  198, 247, 164, 149,
          248, 201, 154, 171, 60,  13,  94,  111, 65,  112, 35,  18,  133, 180, 231, 214,
          122, 75,  24,  41,  190, 143, 220, 237, 195, 242, 161, 144, 7,   54,  101, 84,
          57,  8,   91,  106, 253, 204, 159, 174, 128, 177, 226, 211, 68,  117, 38,  23,
          252, 205, 158, 175, 56,  9,   90,  107, 69,  116, 39,  22,  129, 176, 227, 210,
          191, 142, 221, 236, 123, 74,  25,  40,  6,   55,  100, 85,  194, 243, 160, 145,
          71,  118, 37,  20,  131, 178, 225, 208, 254, 207, 156, 173, 58,  11,  88,  105,
          4,   53,  102, 87,  192, 241, 162, 147, 189, 140, 223, 238, 121, 72,  27,  42,
          193, 240, 163, 146, 5,   52,  103, 86,  120, 73,  26,  43,  188, 141, 222, 239,
          130, 179, 224, 209, 70,  119, 36,  21,  59,  10,  89,  104, 255, 206, 157, 172
        };
        /*
         * uint8_t crc_8( const unsigned char *input_str, size_t num_bytes );
         *
         * The function crc_8() calculates the 8 bit wide CRC of an input string of a
         * given length.
         */
        public static byte crc_8(byte[] input_bytes, int num_bytes, int skip_bytes = 0)
        {
            int idx = skip_bytes;
            byte crc = CRC_START_8;

            while (idx < num_bytes)
            {
                crc = sht75_crc_table[(byte)((input_bytes[idx]) ^ crc)];
                idx++;
            }
            return crc;
        }  /* crc_8 */

        /*
         * uint8_t update_crc_8( unsigned char crc, unsigned char val );
         *
         * Given a databyte and the previous value of the CRC value, the function
         * update_crc_8() calculates and returns the new actual CRC value of the data
         * comming in.
         */
        public static byte update_crc_8(byte crc, byte singlebyte)
        {
            return sht75_crc_table[singlebyte ^ crc];
        }  /* update_crc_8 */
        // --- 
        #endregion 

        // Static Helper Object references
        static public frmMain mainForm;
        static public frmSplash frmLoading;
        static public CConfiguration mainConfig;
        static public CDevicesController deviceController;

        // Global Screen Bitmaps Helpers
        static public Object bmpLock = new Object();

        #region Static Global Functions

        static public void InitDpiAwareness()
        {
            // we try to set ProcessDpiAwarenessContext to support newer DGXI API 
            CGlobals.SetProcessDpiAwarenessContext((int)CGlobals.DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            return;
/*
            if (Environment.OSVersion.Version >= new Version(6, 3, 0)) // win 8.1 added support for per monitor dpi
            {
                if (Environment.OSVersion.Version >= new Version(10, 0, 15063)) // win 10 creators update added support for per monitor v2
                {
                    CGlobals.SetProcessDpiAwarenessContext((int)CGlobals.DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                    UseNewDXGI = true; 
                }
                else CGlobals.SetProcessDpiAwareness(CGlobals.PROCESS_DPI_AWARENESS.Process_Per_Monitor_DPI_Aware);
            }
            else
                CGlobals.SetProcessDPIAware();
*/
        }

        static public void InitWebBrowserControl()
        {
            int BrowserVer, RegVal;
            try
            {
                // get the installed IE version
                using (WebBrowser Wb = new WebBrowser())
                    BrowserVer = Wb.Version.Major;

                // set the appropriate IE version
                if (BrowserVer >= 11)
                    RegVal = 11001;
                else if (BrowserVer == 10)
                    RegVal = 10001;
                else if (BrowserVer == 9)
                    RegVal = 9999;
                else if (BrowserVer == 8)
                    RegVal = 8888;
                else
                    RegVal = 7000;

                // set the actual key
                using (RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", RegistryKeyPermissionCheck.ReadWriteSubTree))
                    if (Key.GetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe") == null)
                        Key.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe", RegVal, RegistryValueKind.DWord);
                // set the actual key
                using (RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_GPU_RENDERING", RegistryKeyPermissionCheck.ReadWriteSubTree))
                    if (Key.GetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe") == null)
                        Key.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe", 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }
        #endregion

        #region Debug functions
        public static void DebugOutput(string szDebug, [CallerMemberName] string callerName = "Unknown")
        {
            CGlobals.DebugOutput(szDebug, callerName, "Info"); 
        }

        public static void DebugOutput(string szDebug, string szPosition, string szFacility)
        {
            try
            {
                string szTime = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);

#if (DEBUG)
                if (Properties.Settings.Default.bDebugFileLogging && fwDebug == null)
                {
                    string szDebugFileName = AppPaths.DebugLog;

                    // Need to lock in this case
                    lock (typeof(CGlobals))
                    {

                        fwDebug = new StreamWriter(szDebugFileName, (Properties.Settings.Default.bDebugDeleteExisting ? false : true) );
                        fwDebug.AutoFlush = true;
                        fwDebug.WriteLine("\tTime\t\t\t|\tPosition\t\t|\tFacility\t|\tDebugMessage");
                        fwDebug.WriteLine("\t====\t\t\t|\t========\t\t|\t========\t|\t============");
                    }
                }
#endif
                // Create Debugstring
                string szDbgMsg = szTime + "\t|\t" +
                                    szPosition + (szPosition.Length > 15 ? "\t" : (szPosition.Length > 7 ? "\t\t" : "\t\t\t")) + "|\t" +
                                    szFacility + "\t\t|\t" +
                                    szDebug;
#if (DEBUG)
                if (Properties.Settings.Default.bDebugFileLogging)
                {
                    lock (typeof(CGlobals))
                    {
                        if (szDbgMsg.EndsWith("\r\n"))
                            fwDebug.Write(szDbgMsg);
                        else
                            fwDebug.WriteLine(szDbgMsg);
                    }
                }
#endif
                if (szDbgMsg.EndsWith("\r\n"))
                    Debug.Write(szDbgMsg);
                else
                    Debug.WriteLine(szDbgMsg);
            }
            catch (Exception)
            {
#if (DEBUG)
                // Need to lock in this case
                lock (typeof(CGlobals))
                {
                    if (fwDebug != null)
                    {
                        // only delete the file handle
                        fwDebug.Close();
                        fwDebug = null;
                    }
                }
#endif
            }

        }

        public static void DebugOutput(Exception ex)
        {
            if (ex.TargetSite != null)
                CGlobals.DebugOutput("Unhandled Exception: " + ex.ToString(), ex.TargetSite.Name, "Error");
            else
                CGlobals.DebugOutput("Unhandled Exception: " + ex.ToString(), "Unknown", "Error");
        }

        #endregion

        #region Error Handlers
        static public void HandleException(string szFuncSource, Exception ex)
        {
            HandleException(szFuncSource, ex, true, false);
        }
        static public void HandleException(string szFuncSource, Exception ex, bool bShowErrorDialog)
        {
            HandleException(szFuncSource, ex, bShowErrorDialog, false);
        }
        static public void HandleException(string szFuncSource, Exception ex, bool bShowErrorDialog, bool bCritical)
        {
            string szErrorText = "Exception occured in " + szFuncSource +
                "\r\nException Type: " + ex.GetType().ToString() +
                "\r\nDetails: \r\n\r\n" + ex.ToString();

            // Write into Debugfile first!
            CGlobals.DebugOutput(szErrorText, szFuncSource, "Error" );

            /*
            if (bShowErrorDialog)
            {
                string szTitleMsg = "";
                if (bCritical)
                    szTitleMsg = "A Critical Exception occured! AliveMon will be terminated.";
                else
                    szTitleMsg = "An Unexpected handeled Exception has been occurred";

                string szErrorMsg = "ExceptionType:\t" + ex.GetType().ToString() + "\r\n" +
                            "Location:\t\t" + szFuncSource + "\r\n" +
                            "Details:\t\t" + ex.Message.Replace("\r\n", "\r\n\t\t") + "\r\n\r\n";
#if (DEBUG)
                szErrorMsg += "SourceFunction:\r\n" + ex.TargetSite + "\r\n\r\n" +
                            "CallStack:\r\n" + ex.StackTrace;
#endif
                CMisc.MyMessageBox(CSettings.myMainForm, szTitleMsg,
                    szErrorMsg, false, "", MessageBoxIcon.Error, 0, "www.alivemon.com/en/support/",
                    "AliveMon Support", MessageBoxButtons.OK);
            }
            */
        }

        public static void KillApp()
        {
            // And now byebye
            Application.Exit();

            // wtf we still exit? kill kill kill :)
            Process myProcess = Process.GetCurrentProcess();
            myProcess.Kill();
        }
        #endregion

        #region Helper functions
        public static string GetImageKey(DeviceType myDeviceType)
        {
            string szReturn = "ambiback"; 
            switch (myDeviceType)
            {
                case DeviceType.DreamScreen4K:
                    szReturn = "DreamScreen4K";
                    break;
                case DeviceType.DreamScreenHD:
                    szReturn = "DreamScreenHD";
                    break;
                case DeviceType.DreamScreenSideKick:
                    szReturn = "DreamScreenSideKick";
                    break;
                case DeviceType.PhilipsHueColorLight:
                case DeviceType.PhilipsHueEntertainment:
                    szReturn = "PhilipsHueColorLight";
                    break;
                case DeviceType.AmbiBack_Bridge_Controller:
                    szReturn = "ambiback_bridge";
                    break;
                case DeviceType.AmbiBack_Pc_Controller:
                    szReturn = "ambiback_pc";
                    break;
                case DeviceType.AmbiBack_Cam_wr_Controller:
                case DeviceType.AmbiBack_Cam_jr_Controller:
                case DeviceType.AmbiBack_Cam_mi_Controller:
                case DeviceType.AmbiBack_Cam_pl_Controller:
                case DeviceType.AmbiBack_Cam_tt_Controller:
                    szReturn = "ambiback_cam";
                    break;
            }
            return szReturn;
        }

        public static int FindDeviceId(string szId)
        {
            int iIndex = (int) DeviceType.UnknownDevice;

            for(int i = 0; i < DEVICETYPES.Length; i++) {
                if (DEVICETYPES[i] == szId){
                    return i; 
                }
            }

            return iIndex;
        } 
        // reverse byte order (16-bit)
        public static UInt16 ReverseBytes(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        public static Process CheckForRunningInstance()
        {
            try
            {
                if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).GetUpperBound(0) > 0)
                {
                    // Process already running
                    foreach (Process myProcess in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
                    {
                        if (myProcess.MainWindowTitle == Application.ProductName )
                            return myProcess;   // return the correct process
                    }

                    // hrm damn the main form is hidden, return first occurrence
                    return Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)[0];
                }

                // reach here? Error or Process not running
                return null;
            }
            catch (Exception ex)
            {
                // Fixed bug 2006-01: Happens on my machine because Performance Counters are disabled!
                CGlobals.HandleException("CheckForRunningInstance", ex);

                // reach here? Error or Process not running
                return null;
            }
        }

        public static void HandleLinkLableClick(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e, LinkLabel lblMyLink)
        {
            // Determine which link was clicked within the LinkLabel.
            lblMyLink.Links[lblMyLink.Links.IndexOf(e.Link)].Visited = true;

            // Display the appropriate link based on the value of the 
            // LinkData property of the Link object.
            string target = e.Link.LinkData as string;

            // If the value looks like a URL, navigate to it.
            // Otherwise, display it in a message box.
            if (null != target && (target.StartsWith("http") || target.StartsWith("www")))
                System.Diagnostics.Process.Start(target);           // Open Link from Linkdata
            else if (lblMyLink.Text.StartsWith("http://") || lblMyLink.Text.StartsWith("https://"))
                System.Diagnostics.Process.Start(lblMyLink.Text);   // Open Link from Textdata
        }

        public static bool CompareByteArrays(byte[] bSource, byte[] bDest, int iCompareLen)
        {
            for (int ix = 0; ix < iCompareLen; ix++)
            {
                if (bSource[ix] != bDest[ix])
                    return false;
            }

            // reached here, thern it is equal:
            return true;
        }

        public static byte[] GetByteArrayFromString(string szData)
        {
            char[] chTmp = szData.ToCharArray();
            byte[] myBytes = new byte[chTmp.Length];
            int i = 0;
            foreach (char myChar in chTmp)
            {
                myBytes[i] = (byte)myChar;
                i++;
            }

            // return byte array
            return myBytes;
        }
        #endregion

        #region Message Box Helpers 
        public static DialogResult MyMessageBox(Form myOwner, string szMyTitle, string szMyMessage, bool bMyShowAskAgain, string szMyRegValueName, MessageBoxIcon myIcon)
        {
            // Wrapper 
            return CGlobals.MyMessageBox(myOwner, szMyTitle, szMyMessage, bMyShowAskAgain, szMyRegValueName, myIcon, MoreModes.None, "", "", MessageBoxButtons.OKCancel);
        }

        public static DialogResult MyMessageBox(Form myOwner, string szMyTitle, string szMyMessage, bool bMyShowAskAgain, string szMyRegValueName, MessageBoxIcon myIcon, MessageBoxButtons myButtons)
        {
            // Another Wrapper 
            return CGlobals.MyMessageBox(myOwner, szMyTitle, szMyMessage, bMyShowAskAgain, szMyRegValueName, myIcon, MoreModes.None, "", "", myButtons);
        }

        public static DialogResult MyMessageBox(Form myOwner, string szMyTitle, string szMyMessage, bool bMyShowAskAgain, string szMyRegValueName, MessageBoxIcon myIcon, string szMoreLink)
        {
            // More Wrapper lol
            return CGlobals.MyMessageBox(myOwner, szMyTitle, szMyMessage, bMyShowAskAgain, szMyRegValueName, myIcon, MoreModes.Link, szMoreLink, "", MessageBoxButtons.OKCancel);
        }

        public static DialogResult MyMessageBox(Form myOwner, string szMyTitle, string szMyMessage, bool bMyShowAskAgain, string szMyRegValueName, MessageBoxIcon myIcon, MoreModes iMoreMode, string szMoreLink, string szMoreTitle, MessageBoxButtons myButtons)
        {
            frmMessageBox myFrm = new frmMessageBox();
            myFrm.szTitle = szMyTitle;
            myFrm.szMessage = szMyMessage;
            myFrm.bShowAskAgain = bMyShowAskAgain;
            myFrm.szRegValueName = szMyRegValueName;

            myFrm.iMoreMode = (int)iMoreMode;
            myFrm.szMoreLink = szMoreLink;
            myFrm.szMoreTitle = szMoreTitle;

            myFrm.myIcon = myIcon;
            myFrm.myButtons = myButtons;

            try
            {	// We may get a problem if the Mainform is disposed!
                if (myOwner == null)
                    myFrm.SetOwnerFormAvailable(false);
                else
                    myFrm.SetOwnerFormAvailable(true);

                // return result
                return myFrm.ShowDialog(myOwner);
            }
            catch (System.ObjectDisposedException)
            {
                myFrm.SetOwnerFormAvailable(false);
                return myFrm.ShowDialog();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                return DialogResult.OK;
            }
        }

        #endregion

        #region Hue Helper Functions
        static public bool InitHue() {
            bool bReturn = false;

            try
            {
                string szHueDeviceIP = Properties.Settings.Default.szHueDeviceIP;
                string szHueUsername = Properties.Settings.Default.szHueUsername;

                // Init Hue Configuration
                Configuration.DeviceIP = System.Net.IPAddress.Parse(szHueDeviceIP);
                Configuration.AppID = CGlobals.szHueAppID;
                Configuration.Username = szHueUsername;
                Configuration.ClientKey = HueSecrets.Unprotect(Properties.Settings.Default.szHueClientKey);

                // Git list of Hue Lights
                CGlobals.HueLights = new LightCollection();

                // Entertainment areas need the client key (pairing with generateclientkey)
                CGlobals.HueEntertainmentAreas = null;
                if (!string.IsNullOrEmpty(Configuration.ClientKey))
                {
                    try
                    {
                        CGlobals.HueEntertainmentAreas = HueClipV2.GetEntertainmentAreas();
                    }
                    catch (Exception ex)
                    {
                        CGlobals.DebugOutput("InitHue|Entertainment areas: " + ex.Message);
                    }
                }

                // If we reach this point, we had success
                bReturn = true; 
            }
            catch (System.FormatException)
            {
                // Invalid IP specified, ignore!
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);   
            }

            return bReturn; 
        }

        static public Light GetHueLightByUniqueID(string szUniqueId)
        {
            if (szUniqueId != null && szUniqueId.Length > 0 && CGlobals.HueLights != null) {
                foreach (Light myLight in CGlobals.HueLights)
                {
                    if (myLight.UniqueID == szUniqueId)
                        return myLight;
                }
            }

            // null if not found
            return null; 
        }
        #endregion

        #region Other Helper Functions
        /// <summary>Run key in HKCU that holds the Windows autostart entries.</summary>
        private const string RunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        /// <summary>
        /// Program the autostart entry "AmbiBack" points to, without quotes and arguments.
        /// Null when there is no entry.
        /// </summary>
        static public string GetWindowsStartupPath()
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    string value = rk?.GetValue("AmbiBack") as string;
                    if (string.IsNullOrWhiteSpace(value))
                        return null;
                    value = value.Trim();
                    if (value.StartsWith("\""))
                    {
                        int end = value.IndexOf('"', 1);
                        return end > 1 ? value.Substring(1, end - 1) : value.Trim('"');
                    }
                    int exe = value.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                    return exe > 0 ? value.Substring(0, exe + 4) : value;
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                return null;
            }
        }

        /// <summary>True when the autostart entry points to this copy of AmbiBack.</summary>
        static public bool IsWindowsStartupThisCopy()
        {
            return IsThisCopy(GetWindowsStartupPath());
        }

        static public bool IsThisCopy(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            try
            {
                return string.Equals(System.IO.Path.GetFullPath(path), System.IO.Path.GetFullPath(Application.ExecutablePath), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Adds this copy to the Windows autostart or removes the entry. Only called when the user
        /// changes the option or the setup passes its choice on; a plain start never touches the
        /// entry, so a second copy (portable ZIP, build folder) does not take the autostart over.
        /// </summary>
        static public void SetLoadWindowsStartup(bool enable)
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    // Continue if access succeeded
                    if (rk != null)
                    {
                        if (enable)
                        {
                            if (!IsWindowsStartupThisCopy())
                                rk.SetValue("AmbiBack", Application.ExecutablePath);
                        }
                        else
                            rk.DeleteValue("AmbiBack", false);
                    }
                }
            }
            catch (SecurityException)
            {
                CGlobals.MyMessageBox(null, L.T("msg_startup_title"),
                    L.T("msg_startup_text"),
                    false, "", MessageBoxIcon.Error, 0, "", "", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("SetLoadWindowsStartup", ex);
            }
        }
        #endregion

    }
}
