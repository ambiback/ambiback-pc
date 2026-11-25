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
            // Command line self tests of the Hue streaming code (no UI, no second instance check)
            if (args.Length > 0 && args[0].StartsWith("--hue-", StringComparison.Ordinal))
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

                // Language from the settings, empty = Windows language
                L.Set(Properties.Settings.Default.szLanguage);

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
                ok = false;
            }
            Console.Write(report.ToString());
            if (reportFile != null)
                System.IO.File.WriteAllText(reportFile, report.ToString());
            return ok ? 0 : 1;
        }
    }
}
