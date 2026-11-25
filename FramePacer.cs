using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AmbiBack
{
    /// <summary>
    /// Keeps a loop at a target frame rate with Stopwatch deadlines instead of DateTime.Now.
    /// The timer resolution is raised to 1 ms once per process, so Thread.Sleep(16) takes about 16 ms
    /// instead of up to 31 ms (default 15.6 ms ticks). Falling behind does not cause a burst of frames.
    /// </summary>
    public sealed class FramePacer
    {
        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uMilliseconds);

        private static int timerResolutionSet;

        private readonly Stopwatch clock = Stopwatch.StartNew();
        private long nextFrameTicks = -1;
        private long secondStartTicks;
        private int framesThisSecond;

        public FramePacer()
        {
            // Since Windows 10 2004 this only affects this process; it ends with the process
            if (Interlocked.Exchange(ref timerResolutionSet, 1) == 0)
            {
                try { timeBeginPeriod(1); }
                catch (Exception ex) { CGlobals.DebugOutput(ex); }
            }
        }

        /// <summary>
        /// Count the frame that was just done and sleep until the next one is due.
        /// Returns the frame rate of the last full second once per second, otherwise -1.
        /// </summary>
        public int FrameDone(int framesPerSecond)
        {
            long period = Stopwatch.Frequency / Math.Max(1, framesPerSecond);
            long now = clock.ElapsedTicks;
            if (nextFrameTicks < 0)
            {
                nextFrameTicks = now;
                secondStartTicks = now;
            }

            framesThisSecond++;
            int fps = -1;
            if (now - secondStartTicks >= Stopwatch.Frequency)
            {
                fps = (int)Math.Round(framesThisSecond * (double)Stopwatch.Frequency / (now - secondStartTicks));
                framesThisSecond = 0;
                secondStartTicks = now;
            }

            nextFrameTicks += period;
            // More than one frame behind (slow send, pause): start again from now instead of catching up
            if (nextFrameTicks < now - period)
                nextFrameTicks = now;

            long waitMs = (nextFrameTicks - now) * 1000 / Stopwatch.Frequency;
            if (waitMs > 0)
                Thread.Sleep((int)waitMs);
            return fps;
        }
    }
}
