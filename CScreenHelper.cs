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

        // Sclaing Faktor for Desktop Screenshots
        int m_iTextureScaling = 2;
        int m_iTextureScalingExtra = 2;
        int m_iTextureScalingMipMap = 4; /* WTF is with this setting o.O */


        // Duplicate the output
        OutputDuplication duplicatedOutput = null;
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
            if (factory != null)
                factory.Dispose();
            if (device != null)
                device.Dispose();
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

            int iScaleHeight = (heightDesktop / m_iTextureScaling);
            int iScaleWidth = (widthDesktop / m_iTextureScaling);

            // Create new Image for primary
            bmpScreenshot = new System.Drawing.Bitmap(iScaleWidth / m_iTextureScalingExtra, iScaleHeight / m_iTextureScalingExtra, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

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
                    // Framerate Control helpers
                    int iFrameTimeMs = 1000 / CGlobals.SCREENSHOT_MAX_FPS;
                    int iLastSecond = DateTime.Now.Second;
                    int iImagesPerSec = 0;
                    int iLastMilliSec = DateTime.Now.Millisecond;

                    while (true)
                    {
                        try
                        {
                            // ReInit Dup Output
                            if (this.duplicatedOutput == null) {
                                CGlobals.DebugOutput("HandleThread:InitDuplicatedOutput");
                                this.InitDuplicatedOutput();
                            }
                        }
                        catch (SharpDXException ex)
                        {
                            CGlobals.DebugOutput("HandleThread:InitDuplicatedOutput:" + ex.ResultCode.Code.ToString());
//                            if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessDenied.Result.Code)

                            // Try again in ONE second
                            Thread.Sleep(1000);

                            // Make sure to Destroy dup output
                            this.DestroyDuplicatedOutput();
                        }
                        
                        /* Process only on if dup output available */
                        if (this.duplicatedOutput != null)
                        {
                            // Get Screenshot
                            this.GetImageFromSharpDX();
                            if (bmpScreenshot == null)
                                throw (new Exception("ERROR COULD NOT GRAB SCREENSHOT"));

                            if (ReceivedScreenshot != null)
                                ReceivedScreenshot(this, null, bmpScreenshot);

                            // Do Frame time Processing
                            if (DateTime.Now.Second == iLastSecond)
                            {
                                // Add little sleeptime if necessary!
                                int iMillSecondsDone = DateTime.Now.Millisecond - iLastMilliSec;
                                if (iMillSecondsDone < iFrameTimeMs)
                                    Thread.Sleep(iFrameTimeMs - iMillSecondsDone);

                                // Safe last millsec now
                                iLastMilliSec = DateTime.Now.Millisecond;

                                // Increment Counter
                                iImagesPerSec++;
                            }
                            else
                            {
                                if (DateTime.Now.Second % 5 == 0)
                                {
                                    CGlobals.DebugOutput("Screenshot FPS: " + iImagesPerSec + "(" + CGlobals.SCREENSHOT_MAX_FPS + ")");
                                    if (UpdateStatus != null)
                                        UpdateStatus(this, null, "Capture FPS: " + iImagesPerSec + "(" + CGlobals.SCREENSHOT_MAX_FPS + ")");
                                }

                                    // Reset variables
                                    iLastSecond = DateTime.Now.Second;
                                iLastMilliSec = DateTime.Now.Millisecond;
                                iImagesPerSec = 0;

                                // Reset Frame Sleeptime 
                                iFrameTimeMs = 1000 / CGlobals.SCREENSHOT_MAX_FPS;
                            }

/*
                            if (DateTime.Now.Second == iLastSecond)
                                iImagesPerSec++;
                            else
                            {
                                if (UpdateStatus != null)
                                    UpdateStatus(this, null, "Capture FPS: " + iImagesPerSec);

                                iLastSecond = DateTime.Now.Second;
                                iImagesPerSec = 0;
                            }
*/
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
        public void CreateThread()
        {
            if (myThread == null)
            {
                myThread = new Thread(new ThreadStart(HandleThread));
                myThread.Priority = ThreadPriority.Lowest;

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
                    try
                    {
                        SharpDX.DXGI.Resource screenResource;
                        OutputDuplicateFrameInformation duplicateFrameInformation;

                        // Try to get duplicated frame within given time
                        duplicatedOutput.AcquireNextFrame(-1, out duplicateFrameInformation, out screenResource);

                        // copy resource into memory that can be accessed by the CPU
                        Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>();
                        device.ImmediateContext.CopySubresourceRegion(screenTexture2D, 0, null, screenTexture, 0);

                        // Generates mipmap for screen resize View
                        device.ImmediateContext.GenerateMips(screenTextureView);

                        // Copy screenTexture to screenStagungTexture 
                        device.ImmediateContext.CopySubresourceRegion(screenTexture, 1, null, screenStagungTexture, 0);

                        // Get the desktop capture texture
                        var mapResizedSource = device.ImmediateContext.MapSubresource(screenStagungTexture, 0, MapMode.Read, MapFlags.None);

                        // Copy pixels from screen capture Texture to GDI bitmap
                        var boundsRect = new System.Drawing.Rectangle(0, 0, bmpScreenshot.Width, bmpScreenshot.Height);
                        var mapDest = bmpScreenshot.LockBits(boundsRect, ImageLockMode.WriteOnly, bmpScreenshot.PixelFormat);
                        var sourcePtr = mapResizedSource.DataPointer;
                        var destPtr = mapDest.Scan0;
                        int iScaleHeight = (heightDesktop / m_iTextureScaling); // Screencapture Height
                        int iScaleWidth  = (widthDesktop / m_iTextureScaling);  // Screencapture Width
                        int iBytesCopy = 4;                                     // How many bytes to copy for a pixel

                        /* Fix height resolution if not devideable to 2: Then we skip last line */
                        if (iScaleHeight % 2 == 1)
                            iScaleHeight--; 

                        // Copy only every second PIXEL at the moment to reduce CPU usage 
                        for (int y = 0; y < iScaleHeight; y+= m_iTextureScalingExtra)
                        {
                            for (int x = 0; x < iScaleWidth; x+= m_iTextureScalingExtra)
                            {
                                // Copy every second byte only
                                Utilities.CopyMemory(destPtr, sourcePtr, iBytesCopy /* 4 Bytes */ );
                                destPtr = IntPtr.Add(destPtr, 4);
                                sourcePtr = IntPtr.Add(sourcePtr, 4 * m_iTextureScalingExtra /* 8 */); 
                            }

// Copy a single line 
//                                Utilities.CopyMemory(destPtr, sourcePtr, iScaleWidth * 4 /* 4 Bytes */ );

                            // Advance pointers | Skip next horizontal line
                            sourcePtr = IntPtr.Add(sourcePtr, mapResizedSource.RowPitch);
//                                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                        }

                        // Release source and dest locks
                        bmpScreenshot.UnlockBits(mapDest);
                        device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                        // Save the output
                        //                        bitmap.Save(outputFileName);

                        // Capture done
                        bCaptureDone = true;

                        screenResource.Dispose();
                        duplicatedOutput.ReleaseFrame();

                    }
                    catch (SharpDXException e)
                    {
                        if (e.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                        {
                            // Ignore this err
                            CGlobals.DebugOutput("screenhelper:GetImageFromSharpDX:SharpDXException:WaitTimeout:IgnoreRetry");
                        }
                        else {
                            throw e;
                        }
                    }
                    finally
                    {
                        device.ImmediateContext.UnmapSubresource(screenStagungTexture, 0);
                    }
                }
            }
            catch (SharpDXException e)
            {
                CGlobals.DebugOutput("screenhelper:GetImageFromSharpDX:SharpDXException:" + e.ToString());
                if (e.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessLost.Result.Code) {
                    // DXGI_ERROR_ACCESS_LOST - Access was lost

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

                    // ReInit SharpDX
                    this.InitSharpDX();
                }
                else
                {
                    throw e;
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput("screenhelper:GetImageFromSharpDX:Exception:" + ex.ToString());

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
