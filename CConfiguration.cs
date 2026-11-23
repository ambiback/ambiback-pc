using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmbiBack
{
    public class CConfiguration
    {
        // Devices List
        public CDevicesList Devices;

        public CConfiguration()
        {
            // Create Devices Lists
            Devices = new CDevicesList("devices");
        }


        public void LoadConfiguration()
        {
            // Call Hue Init
            this.InitHueDevices(); 

            // Read saved Devices from file
            Devices.InitList();
        }

        public void InitHueDevices()
        {
            // Init Hue here: 
            if (!CGlobals.InitHue())
            {
                // Show error maybe
                CGlobals.bHueConnected = false;
            }
            else
            {
                CGlobals.bHueConnected = true;
            }
        }

        public void SaveConfiguration()
        {
            // Saved Devices into file
            Devices.SaveList();
        }
    }
}