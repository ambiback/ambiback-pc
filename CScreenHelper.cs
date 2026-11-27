using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Custom
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

// Hardware support 
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Threading;
using System.Drawing.Drawing2D;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;

namespace AmbiBack
{
    #region Public Event definitions
    public delegate void ScreenHelperReceivedScreenshot(object sender, System.EventArgs e, System.Drawing.Bitmap bmpScreenshot);
    #endregion

    public class CScreenHelper
    {
        #region Threadstuff
        public System.Threading.Thread myThread = null;	// My Thread Object
        public bool bAbortThread = false;			    // Used to finish the Thread
        public bool bSleepThread = false;               // Helper to sleep when interrupted
        public int myThreadID = -1;                     // INT Number used to identify the thread
        #endregion

        #region Public Events
        public event ScreenHelperReceivedScreenshot ReceivedScreenshot;
        public event EventUpdateStatus UpdateStatus;
        public event EventDebugOutput DebugOutput;

        /// <summary>Raised on the capture thread: true when no frames arrive because of an error, false when they do again.</summary>
        public event Action<bool> CaptureStateChanged;
        #endregion

        #region DX API STUFF
        // # of graphics card adapter
        int numAdapter = 0;

        // # of output device (i.e. monitor)
        int numOutput = 0;

        // Create DXGI Factory1
        SharpDX.DXGI.Factory1 factory = null;
        Adapter adapter = null;

        // Create device from Adapter
        Device device = null;

        // Get DXGI.Output
        Output output = null;
        Output1 output1 = null;
        Output5 output5 = null;

        // Width/Height of desktop to capture
        //        int boxPreview = 10; 
        int widthDesktop;
        int heightDesktop;
        int widthPreview;
        int heightPreview;
        int topPreview;
        int leftPreview;

        // Create Staging texture CPU-accessible
        Texture2DDescription screenStagingTextureDesc;
        Texture2D screenStagungTexture = null;
        Texture2DDescription screenTextureDesc;
        Texture2D screenTexture = null;
        ShaderResourceView screenTextureView = null;

        // Number of mip levels of the downscale texture (needs at least READBACK_MIP + 1)
        int m_iTextureScalingMipMap = 4;

        // Mip level that is read back: level 2 = 1/4 of the desktop size, the same bitmap size as before,
        // but averaged by the GPU instead of picking every second pixel on the CPU
        const int READBACK_MIP = 2;

        // Wait at most this long for a new frame so interrupts and abort still work on a static screen
        const int ACQUIRE_TIMEOUT_MS = 200;


        // Duplicate the output
        OutputDuplication duplicatedOutput = null;

        // Recreate factory, adapter, device and output before the next capture (device removed,
        // unexpected errors, or the duplication cannot be created again on the old output)
        volatile bool bReinitSharpDX = false;

        // Failed attempts to create the duplication in a row; after this many the DXGI objects are
        // recreated, as the output may be stale after the display was off or the session switched
        int iInitFailures = 0;
        const int INIT_FAILURES_BEFORE_REINIT = 5;

        // True while no frames arrive because of an error; logged once when it starts and ends
        bool bCaptureLost = false;
        #endregion

        // Bitmap helper 
        public System.Drawing.Bitmap bmpScreenshot = null;
        //        public System.Drawing.Bitmap bmpScreenshotResized = null;

        PictureBox pbPreview;
        Label lblStatus;
        #region get/set helpers
        public int GetNumAdapter() { return numAdapter; }
        public int GetNumOutput() { return numOutput; }
        public bool GetAbortThread() { return bAbortThread; }
        public void SetAbortThread(bool bNew) { bAbortThread = bNew; }
        public bool GetSleepThread() { return bSleepThread; }
        public void SetSleepThread(bool bNew) { bSleepThread = bNew; }

        //        public void CalcTotalPixels() { m_iTotalPixels = (m_iPixelVert * 2) + (m_iPixelHori * 2); }
        #endregion

        // Constructor
        public CScreenHelper(int iNumAdapter, int iNumOutput)
        {
            this.numAdapter = iNumAdapter;
            this.numOutput = iNumOutput;

            // Init SharpDX
            this.InitSharpDX();
        }
        public void InitSharpDX()
        {
            /* Cleanup old handles */
            if (factory != null) {
                factory.Dispose();
                factory = null;
            }
            if (device != null) {
                device.Dispose();
                device = null;
            }
            if (adapter != null) {
                adapter.Dispose();
                adapter = null;
            }
            if (output != null) {
                output.Dispose();
                output = null;
            }
            if (output1 != null) { 
                output1.Dispose();
                output1 = null;
            }
            if (output5 != null) { 
                output5.Dispose();
                output5 = null;
            }

            // SharpDX.DXGI

            // Create DXGI Factory1
            factory = new SharpDX.DXGI.Factory1();
            adapter = factory.GetAdapter(numAdapter);

            // Create device from Adapter
            device = new Device(adapter/*, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport*/);

            if (adapter.GetOutputCount() > 0 )
            {
                // Get DXGI.Output
                output = adapter.GetOutput(numOutput);

                // Get Output Query Interface
                output1 = output.QueryInterface<Output1>();

                // Get Output Query Interface
                output5 = output.QueryInterface<Output5>();
            }
        }
        public int GetOutputCount()
        {
            if (adapter != null)
                return adapter.GetOutputCount();
            else
                return -1; 
        }
        public int GetAdapterCount()
        {
            if (factory != null)
                return factory.GetAdapterCount();
            else
                return -1;
        }
        public void DestroyDuplicatedOutput()
        {
            if (this.duplicatedOutput != null)
                this.duplicatedOutput.Dispose();
            this.duplicatedOutput = null; 
        }

        public void InitDuplicatedOutput()
        {
            this.DestroyDuplicatedOutput();

            try
            {
                CGlobals.DebugOutput("screenhelper:InitDuplicatedOutput:Init new DGXI API output5.DuplicateOutput1");
                Format[] formats = new Format[] { Format.B8G8R8A8_UNorm };
                this.duplicatedOutput = output5.DuplicateOutput1(device, 0, formats.Length, formats);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessDenied.Result.Code ||
                    ex.Descriptor.NativeApiCode == "E_ACCESSDENIED")
                    throw (ex);
                else {
                    CGlobals.DebugOutput("screenhelper:InitDuplicatedOutput:output5.DuplicateOutput1 failed with " + ex.ResultCode.Code.ToString());

                    CGlobals.DebugOutput("screenhelper:InitDuplicatedOutput:Init old DGXI API output1.DuplicateOutput");
                    this.duplicatedOutput = output1.DuplicateOutput(device);
                }
            }
        }

        public void InitScreenHelpers()
        {
            // Width/Height of desktop to capture
            widthDesktop = (output.Description.DesktopBounds.Right - output.Description.DesktopBounds.Left);
            heightDesktop = (output.Description.DesktopBounds.Bottom - output.Description.DesktopBounds.Top);

            // Free the resources of a previous run (pause/resume, resolution change)
            FreeScreenHelpers();

            // Size of the mip level that is read back (D3D11: max(1, size >> level))
            int iScaleHeight = Math.Max(1, heightDesktop >> READBACK_MIP);
            int iScaleWidth = Math.Max(1, widthDesktop >> READBACK_MIP);

            // Create new Image for primary, same size as the mip level
            bmpScreenshot = new System.Drawing.Bitmap(iScaleWidth, iScaleHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Create Staging Screen texture CPU-accessible
            screenStagingTextureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = iScaleWidth,
                Height = iScaleHeight,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            screenStagungTexture = new Texture2D(device, screenStagingTextureDesc);

            // Create Staging thumb texture CPU-accessible
            screenTextureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = widthDesktop,
                Height = heightDesktop,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                MipLevels = m_iTextureScalingMipMap,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Default 
            };
            
            screenTexture = new Texture2D(device, screenTextureDesc);
            screenTextureView = new ShaderResourceView(device, screenTexture);
        }

        /// <summary>Dispose the textures and the bitmap created by InitScreenHelpers.</summary>
        void FreeScreenHelpers()
        {
            if (screenTextureView != null) { screenTextureView.Dispose(); screenTextureView = null; }
            if (screenTexture != null) { screenTexture.Dispose(); screenTexture = null; }
            if (screenStagungTexture != null) { screenStagungTexture.Dispose(); screenStagungTexture = null; }
            // Only the capture thread uses bmpScreenshot; consumers clone it inside ReceivedScreenshot
            if (bmpScreenshot != null) { bmpScreenshot.Dispose(); bmpScreenshot = null; }

        }
        public void SetStatusLabel(Label lblnew)
        {
            lblStatus = lblnew;
        }
        public void SetPreviewPB(PictureBox pbnew)
        {
            pbPreview = pbnew;
            this.SetPreviewSizes(); 
        }
        public void SetPreviewSizes()
        {
            topPreview = pbPreview.Top;
            leftPreview = pbPreview.Left;
            widthPreview = pbPreview.Width;
            heightPreview = pbPreview.Height;
        }
        public Label GetStatusLabel()
        {
            return lblStatus;
        }
        public PictureBox GetPreviewPB()
        {
            return pbPreview;
        }
        #region Thread Controlling Functions
        public void HandleThread()
        {
            CGlobals.DebugOutput("CScreenHelper|HandleThread|Begin Thread");

            while (bAbortThread == false)
            {
                try
                {
                    // (RE)Init Screen Helper Variables!
                    InitScreenHelpers();
                }
                catch (SharpDXException ex)
                {
                    CGlobals.DebugOutput(ex);
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }

                try
                {
                    // Framerate Control helpers (Stopwatch deadlines, see FramePacer)
                    FramePacer pacer = new FramePacer();
                    int iSecondsSinceStatus = 0;

                    while (true)
                    {
                      // Any error in here must not end the thread: Stop/Start cannot bring a finished
                      // thread back, and the lights would keep the last colors (e.g. the logon screen)
                      try
                      {
                        try
                        {
                            // Recreate the DXGI objects: after the display was off or the session switched
                            // the old output can be stale, so creating the duplication on it keeps failing
                            if (this.bReinitSharpDX || this.iInitFailures >= INIT_FAILURES_BEFORE_REINIT) {
                                CGlobals.DebugOutput("HandleThread:InitSharpDX after " + this.iInitFailures + " failed attempts");
                                this.bReinitSharpDX = false;
                                this.iInitFailures = 0;
                                this.DestroyDuplicatedOutput();
                                this.FreeScreenHelpers();
                                this.InitSharpDX();
                            }

                            // ReInit Dup Output, and the textures: the desktop size may have changed (AccessLost)
                            if (this.duplicatedOutput == null) {
                                CGlobals.DebugOutput("HandleThread:InitDuplicatedOutput");
                                this.InitScreenHelpers();
                                this.InitDuplicatedOutput();
                                this.iInitFailures = 0;
                            }
                        }
                        catch (Exception ex) when (!(ex is ThreadInterruptedException) && !(ex is ThreadAbortException))
                        {
                            // E_ACCESSDENIED while the logon or lock screen is shown is expected
                            string szReason = (ex is SharpDXException sdx) ? sdx.ResultCode.ToString() : ex.GetType().Name + ": " + ex.Message;
                            CGlobals.DebugOutput("HandleThread:InitDuplicatedOutput:" + szReason);
                            this.SetCaptureLost(szReason);
                            this.iInitFailures++;

                            // Make sure to Destroy dup output
                            this.DestroyDuplicatedOutput();

                            // Try again in ONE second
                            Thread.Sleep(1000);
                        }

                        /* Process only on if dup output available */
                        if (this.duplicatedOutput != null)
                        {
                            // Get Screenshot; false = no new desktop image (timeout, mouse only, or error)
                            if (!this.GetImageFromSharpDX())
                            {
                                // Managed wait, so Interrupt (pause) and Abort are handled
                                Thread.Sleep(1);
                                continue;
                            }
                            if (bmpScreenshot == null)
                                throw (new Exception("ERROR COULD NOT GRAB SCREENSHOT"));

                            this.SetCaptureRecovered();

                            if (ReceivedScreenshot != null)
                                ReceivedScreenshot(this, null, bmpScreenshot);

                            // Do Frame time Processing: wait for the next frame, report the rate every 5 seconds
                            int iImagesPerSec = pacer.FrameDone(CGlobals.SCREENSHOT_MAX_FPS);
                            if (iImagesPerSec >= 0 && ++iSecondsSinceStatus >= 5)
                            {
                                iSecondsSinceStatus = 0;
                                CGlobals.DebugOutput("Screenshot FPS: " + iImagesPerSec + "(" + CGlobals.SCREENSHOT_MAX_FPS + ")");
                                if (UpdateStatus != null)
                                    UpdateStatus(this, null, L.F("mon_fps_max", iImagesPerSec, CGlobals.SCREENSHOT_MAX_FPS));
                            }

/*
                            if (DateTime.Now.Second == iLastSecond)
                                iImagesPerSec++;
                            else
                            {
                                if (UpdateStatus != null)
                                    UpdateStatus(this, null, L.F("mon_fps", iImagesPerSec));

                                iLastSecond = DateTime.Now.Second;
                                iImagesPerSec = 0;
                            }
*/
                        }
                      }
                      catch (Exception ex) when (!(ex is ThreadInterruptedException) && !(ex is ThreadAbortException))
                      {
                          // Unexpected error: start over with new DXGI objects instead of ending the thread
                          CGlobals.DebugOutput(ex);
                          this.SetCaptureLost(ex.GetType().Name + ": " + ex.Message);
                          this.DestroyDuplicatedOutput();
                          this.bReinitSharpDX = true;
                          Thread.Sleep(1000);
                      }
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // Interrupted
                    if (DebugOutput != null)
                        DebugOutput(this, null, "HandleThread|Interrupted");

                    while (bSleepThread)
                    {
                        // Use Sleep instead of SpinWait 
                        Thread.Sleep(1);
                        // Thread.SpinWait(10000000);
                    }

                    // Stop/Start is the user's way to reset the capture: begin with new DXGI objects
                    this.DestroyDuplicatedOutput();
                    this.bReinitSharpDX = true;

                    // Interrupted
                    if (DebugOutput != null)
                        DebugOutput(this, null, "HandleThread|Restart");

                    // this.SetAbortThread(true);
                }
                catch (ThreadAbortException)
                {
                    // Aborted
                    if (DebugOutput != null)
                        DebugOutput(this, null, "HandleThread|Aborted");

                    this.SetAbortThread(true);
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                    if (DebugOutput != null)
                        DebugOutput(this, null, "HandleThread|Exception: " + ex.ToString());

                    this.SetAbortThread(true);
                }
                finally
                {
                    // nothing todo..
                }
            }

            if (DebugOutput != null)
                DebugOutput(this, null, "HandleThread|Finished Thread");
        }
        /// <summary>No frames arrive any more: log it once and show it instead of the last frame rate.</summary>
        void SetCaptureLost(string szReason)
        {
            if (this.bCaptureLost)
                return;
            this.bCaptureLost = true;
            CGlobals.LogEvent("Capture of adapter " + numAdapter + " output " + numOutput + " interrupted: " + szReason);
            CaptureStateChanged?.Invoke(true);
            if (UpdateStatus != null)
                UpdateStatus(this, null, L.T("mon_waiting"));
        }

        /// <summary>Frames arrive again after SetCaptureLost.</summary>
        void SetCaptureRecovered()
        {
            if (!this.bCaptureLost)
                return;
            this.bCaptureLost = false;
            CGlobals.LogEvent("Capture of adapter " + numAdapter + " output " + numOutput + " running again");
            CaptureStateChanged?.Invoke(false);
        }

        public void CreateThread()
        {
            if (myThread == null)
            {
                myThread = new Thread(new ThreadStart(HandleThread));
                // Lowest starved the capture under game load
                myThread.Priority = ThreadPriority.BelowNormal;

                // Set ThreadName here
                myThread.Name = "ScreenHelper";
            }
        }
        public void StartThread() 
        {
            try
            {
                // Reset Abort Thread
                this.SetAbortThread(false);

                // A finished thread cannot be started again: replace it, so Start always resumes capturing
                if (myThread != null &&
                    (myThread.ThreadState & (System.Threading.ThreadState.Stopped | System.Threading.ThreadState.Aborted)) != 0)
                {
                    CGlobals.LogEvent("Capture thread of adapter " + numAdapter + " output " + numOutput + " had ended (" + myThread.ThreadState + "), starting a new one");
                    myThread.Join(100);
                    myThread = null;
                    this.SetSleepThread(false);
                    this.bReinitSharpDX = true;
                }

                // Create new Thread each time we start!
                CreateThread();

                if (myThread.ThreadState == System.Threading.ThreadState.Unstarted)
                {
                    // Start the Thread
                    myThread.Start();
                }
                else if (myThread.ThreadState == System.Threading.ThreadState.Stopped)
                {
                    myThread.Join(100); // Wait max 1 second until the thread is gone...
                }
                else if (myThread.ThreadState == System.Threading.ThreadState.Running)
                {
                    // Continue thread
                    this.SetSleepThread(false); 
                }
                else if (myThread.ThreadState == System.Threading.ThreadState.Aborted)
                {
                    // Start the Thread
                    myThread.Start();
                }
                else if (myThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    this.SetSleepThread(false);
                }
                else
                {
                    if (DebugOutput != null)
                        DebugOutput(this, null, "CScreenHelper|StartThread|Unexpected Thread STATE: " + myThread.ThreadState.ToString());
                }
            }
            catch (System.InvalidOperationException)
            {
                // Bla we don't care
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                if (DebugOutput != null)
                    DebugOutput(this, null, "CScreenHelper|StartThread|Exception: " + ex.ToString() );
            }
        }
        public bool IsThreadRunning()
        {
            if (myThread.ThreadState == System.Threading.ThreadState.Running)
                return true;
            else
                return false;

        }     
        public bool SleepThread()
        {
            if (myThread != null)
            {
                if (myThread.ThreadState == System.Threading.ThreadState.Running ||
                        myThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    this.SetSleepThread(true);
                    myThread.Interrupt(); //  Interrupt only!;
                }

                // True means Interrupt does need to be taken care of
                return true;
            }
            else
                // False means Interrupt has to be ignored
                return false;
        }
        public bool AbortThread()
        {
            if (myThread != null)
            {
                if (myThread.ThreadState == System.Threading.ThreadState.Running ||
                        myThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    // Abort thread
                    this.SetSleepThread(false);
                    this.SetAbortThread(true);

                    myThread.Abort(); //  Abort();
                    myThread.Join(100); // Wait max 1 second until the thread is gone...
                    myThread = null;		// Byebye thread
                }

                // True means bAbortOperation does need to be taken care of
                return true;
            }
            else
                // False means bAbortOperation has to be ignored
                return false;
        }
        #endregion

        public bool GetImageFromSharpDX()
        {
            bool bCaptureDone = false;
            //            Bitmap bitmap = null;
            try
            {

                while(!bCaptureDone) 
                {
                    SharpDX.DXGI.Resource screenResource = null;
                    bool bFrameAcquired = false;
                    bool bMapped = false;
                    System.Drawing.Imaging.BitmapData mapDest = null;
                    try
                    {
                        OutputDuplicateFrameInformation duplicateFrameInformation;

                        // Wait for a new frame; on a static screen this times out
                        try
                        {
                            duplicatedOutput.AcquireNextFrame(ACQUIRE_TIMEOUT_MS, out duplicateFrameInformation, out screenResource);
                            bFrameAcquired = true;
                        }
                        catch (SharpDXException e) when (e.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                        {
                            return false;
                        }

                        // Only the mouse pointer changed: the desktop image is the same as before
                        if (duplicateFrameInformation.LastPresentTime == 0)
                            return false;

                        // Copy the frame to the texture with mip chain and let the GPU downscale it
                        using (Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>())
                            device.ImmediateContext.CopySubresourceRegion(screenTexture2D, 0, null, screenTexture, 0);
                        device.ImmediateContext.GenerateMips(screenTextureView);

                        // Read back only the small mip level
                        device.ImmediateContext.CopySubresourceRegion(screenTexture, READBACK_MIP, null, screenStagungTexture, 0);
                        var mapResizedSource = device.ImmediateContext.MapSubresource(screenStagungTexture, 0, MapMode.Read, MapFlags.None);
                        bMapped = true;

                        // Copy line by line: source and destination rows may be padded differently
                        var boundsRect = new System.Drawing.Rectangle(0, 0, bmpScreenshot.Width, bmpScreenshot.Height);
                        mapDest = bmpScreenshot.LockBits(boundsRect, ImageLockMode.WriteOnly, bmpScreenshot.PixelFormat);
                        int iRowBytes = bmpScreenshot.Width * 4;
                        for (int y = 0; y < bmpScreenshot.Height; y++)
                        {
                            Utilities.CopyMemory(IntPtr.Add(mapDest.Scan0, y * mapDest.Stride),
                                IntPtr.Add(mapResizedSource.DataPointer, y * mapResizedSource.RowPitch), iRowBytes);
                        }

                        // Capture done
                        bCaptureDone = true;
                    }
                    finally
                    {
                        if (mapDest != null)
                            bmpScreenshot.UnlockBits(mapDest);
                        if (bMapped)
                            device.ImmediateContext.UnmapSubresource(screenStagungTexture, 0);
                        if (screenResource != null)
                            screenResource.Dispose();
                        if (bFrameAcquired)
                        {
                            // Can fail after AccessLost; the frame is gone then anyway
                            try { duplicatedOutput.ReleaseFrame(); }
                            catch (SharpDXException) { }
                        }
                    }
                }
            }
            catch (SharpDXException e)
            {
                CGlobals.DebugOutput("screenhelper:GetImageFromSharpDX:SharpDXException:" + e.ToString());
                if (e.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessLost.Result.Code) {
                    // DXGI_ERROR_ACCESS_LOST - Access was lost (display off, lock screen, mode change)
                    this.SetCaptureLost("AccessLost");

                    // Do Events first!
                    Application.DoEvents();

                    while (!CGlobals.monitorPowerStatus)
                    {
                        // Wait for monitor to come back online
                        CGlobals.DebugOutput("screenhelper:GetImageFromSharpDX:DXGI_ERROR_DEVICE_REMOVED:WaitForMonitorLoop");

                        // Sleep for a second
                        Thread.Sleep(1000);
                    }

                    // Destroy Dupl Output, so it gets created again
                    this.DestroyDuplicatedOutput();
                } else if (e.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code) {
                    // DXGI_ERROR_DEVICE_REMOVED - Device was removed
                    this.SetCaptureLost("DeviceRemoved");

                    // Do Events first!
                    Application.DoEvents();

                    while (!CGlobals.monitorPowerStatus)
                    {
                        // Wait for monitor to come back online
                        CGlobals.DebugOutput("screenhelper:GetImageFromSharpDX:DXGI_ERROR_DEVICE_REMOVED:WaitForMonitorLoop");

                        // Sleep for a second
                        Thread.Sleep(1000);
                    }

                    // Destroy Dupl Output
                    this.DestroyDuplicatedOutput();

                    // ReInit SharpDX in HandleThread, where a failure is retried
                    this.bReinitSharpDX = true;
                }
                else
                {
                    // Unknown DXGI error: start over with new DXGI objects
                    this.SetCaptureLost(e.ResultCode.ToString());
                    this.DestroyDuplicatedOutput();
                    this.bReinitSharpDX = true;
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput("screenhelper:GetImageFromSharpDX:Exception:" + ex.ToString());
                this.SetCaptureLost(ex.GetType().Name + ": " + ex.Message);

                // Destroy dup output and try again
                this.DestroyDuplicatedOutput();
            }

            return bCaptureDone; 
        }

        public System.Drawing.Brush getColorFromPixelRregion(System.Drawing.Bitmap myScreenshot, int iLeft, int iTop, int iWidth, int iHeight)
        {
            System.Drawing.Brush myBrush = null;
            int iRed = 0;
            int iGreen = 0;
            int iBlue = 0;
            int iPixels = 0;

            unsafe
            {
                BitmapData bmlData = null;

                try
                {
                    // Lock ImageData | workarround GetPixel() locking, using direct access on bit data!
                    bmlData = myScreenshot.LockBits(new System.Drawing.Rectangle(iLeft, iTop, iWidth, iHeight), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    byte* srcPointer = (byte*)bmlData.Scan0;

                    // Found using performance profiler
                    int screenshotWidth = myScreenshot.Width;

                    int iIndex = 0;
                    for (int i = 0; i < iHeight; i++)
                    {
                        for (int j = 0; j < iWidth; j++)
                        {
                            iBlue += srcPointer[iIndex + 0];     // Blue
                            iGreen += srcPointer[iIndex + 1];    // Green
                            iRed += srcPointer[iIndex + 2];      // Red
                                                                 // not needed dstPointer[3] = srcPointer[3]; // Alpha
                                                                 /*
                                                                 srcPointer[iIndex + 0] = 0;
                                                                 srcPointer[iIndex + 1] = 0;
                                                                 srcPointer[iIndex + 2] = 0;
                                                                 */
                            iPixels++;
                            iIndex += 4; /* Add 4 bytes to step */
                        }

                        /* Fast Forward Pixels */
                        iIndex += (screenshotWidth - iWidth) * 4; 
                    }

                }
                catch (Exception ex)
                {
                    if (DebugOutput != null)
                        DebugOutput(this, null, "CScreenHelper|getColorFromPixelRregion|Exception: " + ex.ToString());
                }
                finally {
                    // Unlock Imagedata
                    if (bmlData != null)
                        myScreenshot.UnlockBits(bmlData);
                }
            }
            
            /* Calc avareg color */
            iRed = (iRed > 0 ? iRed / iPixels : 0);
            iGreen = (iGreen  > 0 ? iGreen / iPixels : 0);
            iBlue = (iBlue > 0 ? iBlue / iPixels : 0);

            /* Create solid brush from avarage color */
            myBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, iRed, iGreen, iBlue));
            return myBrush; 
        }

    }
}
