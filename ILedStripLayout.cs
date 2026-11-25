namespace AmbiBack
{
    /// <summary>
    /// A strip around the screen whose layout is set in the monitor preview: LEDs per side and the start
    /// corner with direction. Implemented by AmbiBack controllers (the layout is stored on the controller)
    /// and by WLED/DDP devices (the layout is stored only in AmbiBack PC).
    /// </summary>
    public interface ILedStripLayout
    {
        /// <summary>LEDs on the top and on the bottom side.</summary>
        byte PixelHori { get; }
        /// <summary>LEDs on the left and on the right side.</summary>
        byte PixelVert { get; }
        /// <summary>Start corner and direction, a <see cref="DsFlexSetups"/> value.</summary>
        byte FlexSetupPort1 { get; }

        void SetPixelHori(byte bNew);
        void SetPixelVert(byte bNew);
        void SetFlexPort1(byte bNew);

        /// <summary>Writes the changed value to the device, if it stores the layout itself.</summary>
        void SendLedHorizontal();
        void SendLedVertical();
        void SendLedDirection();
    }
}
