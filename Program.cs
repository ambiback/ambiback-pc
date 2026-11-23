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
        static void Main()
        {
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
                    return;
                }
                // ---

                // Copy the Startup Path
                CGlobals.szApplicationPath = Application.StartupPath;

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

        }
    }
}
