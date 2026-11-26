using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


using System.Diagnostics;
using Microsoft.Win32;

namespace AmbiBack
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // Command line self tests of the Hue and WLED streaming code (no UI, no second instance check)
            if (args.Length > 0 && (args[0].StartsWith("--hue-", StringComparison.Ordinal) || args[0] == "--wled-test" || args[0] == "--govee-test" || args[0] == "--openrgb-test"))
                return RunHueSelfTest(args);

            try
            {
                Application.SetCompatibleTextRenderingDefault(false);
                Application.EnableVisualStyles();
                Application.DoEvents();	// Fixes System.Runtime.InteropServices.SEHException exceptions!

                // --- Previous Instance Check
                Process myPrevious = CGlobals.CheckForRunningInstance();
                if (myPrevious != null)
                {
                    //Give the focus to the other instance
                    CGlobals.SetForegroundWindow(myPrevious.MainWindowHandle);

                    // Focus existing APP and quit!
                    CGlobals.KillApp();

                    // Abort
                    return 0;
                }
                // ---

                // Copy the Startup Path
                CGlobals.szApplicationPath = Application.StartupPath;

                // Settings of the previous version, and choices made in the setup
                UpgradeSettings();

                // Language from the settings, empty = Windows language
                L.Set(Properties.Settings.Default.szLanguage);
                ApplySetupChoices();

                // Show the SplashForm first
                frmSplash frmLoading = new frmSplash();
                CGlobals.frmLoading = frmLoading;
                frmLoading.Show();
                Application.DoEvents();

                // TODO: implement main config loader and device discovery here!
                // Init Main Config Object
                CGlobals.mainConfig = new CConfiguration();
                // Init Device Controller here!
                CGlobals.deviceController = new CDevicesController();
                // Other Global Init stuff
                CGlobals.InitDpiAwareness();
                CGlobals.InitWebBrowserControl();

                try
                {
                    // Load Configuration now!
                    CGlobals.mainConfig.LoadConfiguration();
#if USE_DREAMSCREEN
                    Console.WriteLine("DreamScreen Support:ENABLED"); 
#endif
                }
                catch (Exception ex)
                {
                    CGlobals.HandleException("LoadConfiguration", ex, true, false);
                }

                // Register Global SystemEvents
                SystemEvents.SessionEnded += new SessionEndedEventHandler(CGlobals.SessionEnded);
                SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(CGlobals.PowerModeChanged);
                SystemEvents.DisplaySettingsChanged += new EventHandler(CGlobals.DisplaySettingsChanged);

                try
                {
                    // Run the Main Form!
                    Application.Run(new frmMain());
                }
                catch (ReloadException)
                {
                    // TODO: IMPLEMENT
                }
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("Main", ex, true, true);

                // Byebye
                CGlobals.KillApp();
            }
            return 0;
        }

        /// <summary>
        /// User settings are stored per version. After an update the settings of the previous
        /// version are taken over once.
        /// </summary>
        private static void UpgradeSettings()
        {
            try
            {
                var settings = Properties.Settings.Default;
                if (!settings.bSettingsUpgradeRequired)
                    return;
                settings.Upgrade();
                settings.bSettingsUpgradeRequired = false;
                settings.Save();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        /// <summary>Registry key the setup writes its choices to.</summary>
        private const string SetupKey = @"Software\AmbiBack\PC";

        /// <summary>
        /// The setup stores the autostart choice as SetupAutostart (DWORD 0/1). It is taken over
        /// into the settings and the Run entry once and then removed, later changes are made in the options.
        /// This is the only start-up path that writes the Run entry.
        /// </summary>
        private static void ApplySetupChoices()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SetupKey, true))
                {
                    object value = key?.GetValue("SetupAutostart");
                    if (value == null)
                        return;
                    bool autostart = Convert.ToInt32(value) != 0;
                    Properties.Settings.Default.bLoadAmbiBackOnWindowsStartup = autostart;
                    Properties.Settings.Default.Save();
                    CGlobals.SetLoadWindowsStartup(autostart);
                    key.DeleteValue("SetupAutostart", false);
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        /// --hue-selftest [report.txt]
        /// --hue-dtls-interop host port identity pskhex [report.txt]
        /// Exit code 0 when all tests passed.
        /// </summary>
        private static int RunHueSelfTest(string[] args)
        {
            AttachConsole(-1);   // print into the calling console, if any
            var report = new System.Text.StringBuilder();
            bool ok;
            string reportFile = null;
            if (args[0] == "--hue-selftest")
            {
                ok = AmbiBack.PhilipsHue.HueSelfTest.Run(report);
                reportFile = args.Length > 1 ? args[1] : null;
            }
            else if (args[0] == "--hue-dtls-interop" && args.Length >= 5 && int.TryParse(args[2], out int port))
            {
                ok = AmbiBack.PhilipsHue.HueSelfTest.RunInterop(args[1], port, args[3], args[4], report);
                reportFile = args.Length > 5 ? args[5] : null;
            }
            else if (args[0] == "--hue-discover")
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var found = AmbiBack.PhilipsHue.HueDiscovery.Discover();
                Console.WriteLine(found.Count + " bridge(s) in " + watch.ElapsedMilliseconds + " ms");
                foreach (var b in found)
                    Console.WriteLine("  " + b + "  id=" + b.BridgeID + " model=" + b.ModelID);
                return found.Count > 0 ? 0 : 1;
            }
            else if (args[0] == "--wled-test" && args.Length >= 2)
            {
                // Streams a moving test picture to a WLED / DDP receiver
                int seconds = args.Length > 2 && int.TryParse(args[2], out int s) ? s : 10;
                return CDeviceWled.RunStreamTest(args[1], seconds, line => Console.WriteLine(line)) ? 0 : 1;
            }
            else if (args[0] == "--govee-test" && args.Length >= 2)
            {
                // Streams a moving test picture to a Govee light over the LAN API
                int seconds = args.Length > 2 && int.TryParse(args[2], out int s) ? s : 10;
                bool single = Array.IndexOf(args, "single") > 1;
                return CDeviceGovee.RunStreamTest(args[1], seconds, single, line => Console.WriteLine(line)) ? 0 : 1;
            }
            else if (args[0] == "--openrgb-test" && args.Length >= 2)
            {
                // Lists the controllers of an OpenRGB SDK server and streams a test picture to one
                int controller = args.Length > 2 && int.TryParse(args[2], out int c) ? c : 0;
                int seconds = args.Length > 3 && int.TryParse(args[3], out int s) ? s : 10;
                bool single = Array.IndexOf(args, "single") > 1;
                return CDeviceOpenRgb.RunStreamTest(args[1], controller, seconds, single, line => Console.WriteLine(line)) ? 0 : 1;
            }
            else if (args[0] == "--hue-bridge-info" && args.Length >= 3)
            {
                return AmbiBack.PhilipsHue.HueSelfTest.RunBridgeInfo(args[1], args[2], line => Console.WriteLine(line)) ? 0 : 1;
            }
            else if (args[0] == "--hue-bridge-test" && args.Length >= 3)
            {
                // Live output, the pairing step waits for the link button
                reportFile = args.Length > 5 && !args[5].StartsWith("--") ? args[5] : null;
                Action<string> output = line =>
                {
                    Console.WriteLine(line);
                    if (reportFile != null)
                        System.IO.File.AppendAllText(reportFile, line + Environment.NewLine);
                };
                int area = args.Length > 3 && int.TryParse(args[3], out int a) ? a : 0;
                int seconds = args.Length > 4 && int.TryParse(args[4], out int s) ? s : 15;
                bool simulateLoss = Array.IndexOf(args, "--simulate-loss") > 0;
                return AmbiBack.PhilipsHue.HueSelfTest.RunBridgeTest(args[1], args[2], area, seconds, simulateLoss, output) ? 0 : 1;
            }
            else
            {
                report.AppendLine("Usage: AmbiBack.exe --hue-selftest [report.txt]");
                report.AppendLine("       AmbiBack.exe --hue-bridge-test bridge-ip credfile [area-index] [seconds] [report.txt] [--simulate-loss]");
                report.AppendLine("       AmbiBack.exe --hue-dtls-interop host port identity pskhex [report.txt]");
                report.AppendLine("       AmbiBack.exe --wled-test host [seconds]");
                report.AppendLine("       AmbiBack.exe --govee-test host [seconds] [single]");
                report.AppendLine("       AmbiBack.exe --openrgb-test host [controller] [seconds] [single]");
                ok = false;
            }
            Console.Write(report.ToString());
            if (reportFile != null)
                System.IO.File.WriteAllText(reportFile, report.ToString());
            return ok ? 0 : 1;
        }
    }
}
