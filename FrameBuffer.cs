using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AmbiBack
{
    /// <summary>
    /// The latest captured frame of one monitor as BGRA bytes. The capture thread writes it once per
    /// frame; the device threads read it. Replaces one Bitmap clone per device and frame. All access
    /// goes through <see cref="Sync"/>, a lock per monitor instead of the global bmpLock.
    /// </summary>
    public class FrameBuffer
    {
        public readonly object Sync = new object();

        /// <summary>Pixels, 4 bytes per pixel (B, G, R, A), rows of Width * 4 bytes without padding.</summary>
        public byte[] Pixels { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>Incremented with every new frame; readers can skip work when it did not change.</summary>
        public long FrameNumber { get; private set; }

        /// <summary>Copy a 32 bpp bitmap into the buffer (one LockBits per frame). Called by the capture thread.</summary>
        public void Update(Bitmap source)
        {
            int w = source.Width, h = source.Height;
            BitmapData data = source.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                lock (Sync)
                {
                    if (Pixels == null || Width != w || Height != h)
                    {
                        Pixels = new byte[w * h * 4];
                        Width = w;
                        Height = h;
                    }
                    int row = w * 4;
                    if (data.Stride == row)
                        Marshal.Copy(data.Scan0, Pixels, 0, row * h);
                    else
                        for (int y = 0; y < h; y++)
                            Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), Pixels, y * row, row);
                    FrameNumber++;
                }
            }
            finally
            {
                source.UnlockBits(data);
            }
        }
    }
}
