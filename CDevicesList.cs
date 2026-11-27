using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Custom
using System.IO;
using System.Xml;
using System.Collections;
using System.Xml.Serialization;
using System.Net;

namespace AmbiBack
{
    public class CDevicesList
    {
        // Used to store its own representation as XML
        private XmlDocument XMLdoc;

        // Private Variables 
        private string m_szListName;
        private string m_szFileName;
        private ArrayList m_aDevices;

        // Discovery tasks add devices from thread-pool threads while the UI enumerates the list
        private readonly object m_lock = new object();

        // Only set when the device file is unusable AND could not be moved aside. SaveList then must not
        // touch it, otherwise it would be replaced by an empty or shortened list.
        private bool m_bLoadFailed = false;

        // Two saves at once would write the same temp file
        private readonly object m_saveLock = new object();

        // Nodes we could not turn into a device (unknown type from a newer build, broken entry).
        // They are written back unchanged on save so the user's devices are never dropped silently.
        private List<KeyValuePair<string, string>> m_aPreservedNodes = new List<KeyValuePair<string, string>>();

        public CDevicesList(string szListName)
        {
            // Init stuff
            m_aDevices = null;
            m_szListName = szListName; 
            m_szFileName = AppPaths.ConfigFile("config_devices_" + m_szListName + ".xml");
            XMLdoc = null;
        }

        #region List Control Functions
        public int GetDevicesCount()
        {
            lock (m_lock)
            {
                return m_aDevices.Count;
            }
        }
        public string GetListName() { return m_szListName; }


        /// <summary>
        /// Loads the device file. If the main file is missing (interrupted save) or unreadable, the .bak is used.
        /// An unreadable main file is moved aside (never deleted) so the next save cannot overwrite it.
        /// </summary>
        public void InitList()
        {
            m_bLoadFailed = false;
            string szBak = this.m_szFileName + ".bak";
            try
            {
                ReadDevicesFromFile(this.m_szFileName);
                return;
            }
            catch (FileNotFoundException)
            {
                if (!File.Exists(szBak))
                {
                    // No file yet: first start, nothing to protect
                    CGlobals.IsFirstTime = true;
                    return;
                }
                // The main file is gone but a backup exists, e.g. a save was interrupted
                CGlobals.LogEvent("InitList: " + this.m_szFileName + " is missing, loading the backup");
            }
            catch (Exception ex)
            {
                CGlobals.LogEvent("InitList: device file is not readable: " + ex.Message);
                if (!MoveAsideCorrupt(this.m_szFileName))
                {
                    // Cannot preserve it, so SaveList must not overwrite it either
                    m_bLoadFailed = true;
                    return;
                }
            }

            // Main file missing or moved aside: the backup is the last good copy
            try
            {
                ReadDevicesFromFile(szBak);
                CGlobals.LogEvent("InitList: devices restored from " + szBak);
            }
            catch (Exception exBak)
            {
                if (!(exBak is FileNotFoundException))
                {
                    CGlobals.LogEvent("InitList: backup is not readable either: " + exBak.Message);
                    MoveAsideCorrupt(szBak);
                }
                // Nothing usable left (the broken files are kept aside): start with an empty list, saving is allowed
                lock (m_lock)
                {
                    m_aDevices.Clear();
                }
                m_aPreservedNodes.Clear();
                XMLdoc = new XmlDocument();
            }
        }

        /// <summary>Renames a broken file to name.corrupt-yyyyMMdd-HHmmss. Returns false if it could not be kept.</summary>
        private bool MoveAsideCorrupt(string szFile)
        {
            try
            {
                if (!File.Exists(szFile))
                    return true;
                string szTarget = szFile + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                if (File.Exists(szTarget))
                    szTarget += "-" + Environment.TickCount;
                File.Move(szFile, szTarget);
                CGlobals.LogEvent("InitList: kept the unreadable file as " + szTarget);
                return true;
            }
            catch (Exception ex)
            {
                CGlobals.LogEvent("InitList: could not move " + szFile + " aside: " + ex.Message);
                return false;
            }
        }

        // Throws if the file is missing or not usable
        private void ReadDevicesFromFile(string szFile)
        {
            m_aPreservedNodes.Clear();

            // Parse into a fresh document, a failed load must not leave a half-read one behind
            XMLdoc = new XmlDocument();

            lock (m_lock)
            {
                if (m_aDevices == null)
                    m_aDevices = new ArrayList();
                else
                    m_aDevices.Clear();
            }

            //Now load and parse XML
            XMLdoc.Load(szFile);

            // Get Primary nodes list
            XmlNodeList nodesDevices = XMLdoc.GetElementsByTagName("devices");
            if (nodesDevices.Count == 0)
                throw new InvalidDataException("no devices element in " + szFile);

            CDevice newDevice = null;
            foreach (XmlNode myNode in nodesDevices.Item(0).ChildNodes)
            {
                // Comments / whitespace are not devices
                if (myNode.NodeType != XmlNodeType.Element)
                    continue;

                newDevice = null;
                try
                {
#if USE_DREAMSCREEN
                if (myNode.Name == "AmbiBack.CDeviceDreamScreen")
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceDreamScreen));
                    StringReader stringIn = new StringReader(myNode.InnerText);

                    // Get Object from XML Stream
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else if (myNode.Name == "AmbiBack.CDeviceAmbiBack")
#else 
                if (myNode.Name == "AmbiBack.CDeviceAmbiBack")
#endif
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceAmbiBack));
                    StringReader stringIn = new StringReader(myNode.InnerText);

                    // Get Object from XML Stream
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else if (myNode.Name == "AmbiBack.CDevicePhilipsHueLight")
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDevicePhilipsHueLight));
                    StringReader stringIn = new StringReader(myNode.InnerText);

                    // Get Object from XML Stream
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else if (myNode.Name == "AmbiBack.CDeviceHueEntertainment")
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceHueEntertainment));
                    StringReader stringIn = new StringReader(myNode.InnerText);
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else if (myNode.Name == "AmbiBack.CDeviceWled")
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceWled));
                    StringReader stringIn = new StringReader(myNode.InnerText);
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else if (myNode.Name == "AmbiBack.CDeviceGovee")
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceGovee));
                    StringReader stringIn = new StringReader(myNode.InnerText);
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else if (myNode.Name == "AmbiBack.CDeviceOpenRgb")
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceOpenRgb));
                    StringReader stringIn = new StringReader(myNode.InnerText);
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else if (myNode.Name == "AmbiBack.CDeviceNanoleaf")
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceNanoleaf));
                    StringReader stringIn = new StringReader(myNode.InnerText);
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else if (myNode.Name == "AmbiBack.CDeviceLifx")
                {
                    XmlSerializer xmlIn = new System.Xml.Serialization.XmlSerializer(typeof(CDeviceLifx));
                    StringReader stringIn = new StringReader(myNode.InnerText);
                    newDevice = (CDevice)xmlIn.Deserialize(stringIn);
                }
                else
                {
                    // Unknown type (e.g. written by a newer build): skip it, but keep it for the next save
                    CGlobals.LogEvent("InitList: skipping unknown device type " + myNode.Name);
                    m_aPreservedNodes.Add(new KeyValuePair<string, string>(myNode.Name, myNode.InnerText));
                }
                }
                catch (Exception exNode)
                {
                    // One broken entry must not cost the devices behind it
                    CGlobals.LogEvent("InitList: could not read device node " + myNode.Name + ": " + exNode.Message);
                    m_aPreservedNodes.Add(new KeyValuePair<string, string>(myNode.Name, myNode.InnerText));
                    newDevice = null;
                }

                //Add into arraylist
                if (newDevice != null) {
                    newDevice.SetDeviceID();
                    lock (m_lock)
                    {
                        if (this.GetDevice(newDevice.GetDeviceID()) == null)
                            m_aDevices.Add(newDevice);
                        else
                            CGlobals.DebugOutput("ERROR, device with ID " + newDevice.GetDeviceID() + " already added!");
                    }
                }
            }
        }
        public void SaveList()
        {
            // The file is unreadable and could not be moved aside: keep it exactly as it is
            if (m_bLoadFailed)
            {
                CGlobals.LogEvent("SaveList: device file was not loaded correctly, not overwriting " + this.m_szFileName);
                return;
            }

            lock (m_saveLock)
            {
                WriteDeviceFile();
            }
        }

        // Builds the XML from the list and replaces the device file safely
        private void WriteDeviceFile()
        {
            string szTmp = this.m_szFileName + ".tmp";
            try
            {
//                XmlNode myChildNode;
                XmlNode myNode;

                //--- Reset the fields
                XmlNodeList nodesDevices = XMLdoc.GetElementsByTagName("devices");

                //Clear if necessary
                if (nodesDevices.Count > 0)
                    nodesDevices.Item(0).RemoveAll();
                else
                {
                    myNode = XMLdoc.CreateNode(XmlNodeType.Element, "devices", "");
                    XMLdoc.AppendChild(myNode);
                    nodesDevices = XMLdoc.GetElementsByTagName("devices");
                }

                CDevice[] aSnapshot;
                lock (m_lock)
                {
                    aSnapshot = (CDevice[])m_aDevices.ToArray(typeof(CDevice));
                }

                foreach (CDevice myDevice in aSnapshot)
                {
                    XmlSerializer xmlSerial = new System.Xml.Serialization.XmlSerializer(myDevice.GetType());
                    StringWriter stringOut = new StringWriter();
                    xmlSerial.Serialize(stringOut, myDevice);

                    myNode = XMLdoc.CreateNode(XmlNodeType.Element, myDevice.GetType().ToString() /*"device"*/, "");
                    myNode.InnerText = stringOut.ToString();

                    /*                    // Add Monitors
                                        myChildNode = myXML.CreateNode(XmlNodeType.Element, "Monitors", "");
                                        myHost.MakeXmlMonitorList(myXML, myChildNode);
                                        myNode.AppendChild(myChildNode);

                                        // Add Actions
                                        myChildNode = myXML.CreateNode(XmlNodeType.Element, "Actions", "");
                                        myHost.MakeXmlActionList(myXML, myChildNode);
                                        myNode.AppendChild(myChildNode);
                    */

                    // Add Field into list
                    nodesDevices.Item(0).AppendChild(myNode);
                }

                // Write back the entries we could not read, so they survive the save
                foreach (KeyValuePair<string, string> kvPreserved in m_aPreservedNodes)
                {
                    myNode = XMLdoc.CreateNode(XmlNodeType.Element, kvPreserved.Key, "");
                    myNode.InnerText = kvPreserved.Value;
                    nodesDevices.Item(0).AppendChild(myNode);
                }

                // Write the complete new file next to the real one and check that it reads back. Until then
                // the existing file and its .bak stay untouched, so a failed write loses nothing.
                XMLdoc.Save(szTmp);
                new XmlDocument().Load(szTmp);

                // Only now rotate: File.Replace turns the old main file into the .bak and puts the new one in
                // its place in one step; no copy is ever deleted before its successor exists.
                if (File.Exists(this.m_szFileName))
                    File.Replace(szTmp, this.m_szFileName, this.m_szFileName + ".bak");
                else
                    File.Move(szTmp, this.m_szFileName);
            }
            catch (Exception ex)
            {
                CGlobals.LogEvent("SaveList: could not write " + this.m_szFileName + ", the old files are kept: " + ex.Message);
                CGlobals.HandleException("SaveList", ex);
                try { File.Delete(szTmp); } catch (Exception) { }
            }
        }

        public CDevice Add(CDevice newDevice)
        {
            CDevice addedDevice = null;
            bool bAdded = false;
            // Lookup and add in one lock scope, two discovery tasks must not add the same ID twice
            lock (m_lock)
            {
                addedDevice = FindDeviceUnlocked(newDevice.GetDeviceID());
                if (addedDevice == null)
                {
                    // Add new device
                    m_aDevices.Add(newDevice);
                    addedDevice = newDevice;
                    bAdded = true;
                }
            }

            if (!bAdded)
            {
                // Update existing device with newDevice settings!
                addedDevice.CopyFrom(newDevice); 
            }

            return addedDevice;
        }

        public void ReplaceEntry(CDevice removeDevice, CDevice addDevice)
        {
            lock (m_lock)
            {
                m_aDevices.Remove(removeDevice);
                m_aDevices.Add(addDevice);
            }
        }

        /*
        public CDevice CreateDevice()
        {
            // Create a new ENtry
            CDevice newDevice = new CDevice();

            return newDevice;
        }*/

        public void DeleteEntry(CDevice delDevice)
        {
            // Delete entry from list
            lock (m_lock)
            {
                m_aDevices.Remove(delDevice);
            }
        }
        public void DeleteEntry(int iIndex)
        {
            // Delete entry by indexnumber
            lock (m_lock)
            {
                m_aDevices.RemoveAt(iIndex);
            }
        }

        public CDevice GetDevice(int iIndex)
        {
            // Retur´n Host by Entry
            lock (m_lock)
            {
                return (CDevice)m_aDevices[iIndex];
            }
        }

        public CDevice GetDevice(string szId)
        {
            lock (m_lock)
            {
                return FindDeviceUnlocked(szId);
            }
        }

        // Caller must hold m_lock
        private CDevice FindDeviceUnlocked(string szId)
        {
            foreach (CDevice myDevice in m_aDevices)
            {
                if (myDevice.GetDeviceID() == szId)
                {
                    return myDevice;
                }
            }

            return null;
        }

        public CDevice GetDevice(IPAddress ipAddress)
        {
            foreach (CDevice myDevice in GetDeviceList())
            {
                // check if ConfigEntry was clicked
                if (myDevice.GetIPAddress() != null && 
                    myDevice.GetIPAddress().ToString() == ipAddress.ToString())
                    return myDevice;
            }

            // Not found at all
            return null;
        }


        // Returns a snapshot: safe to enumerate while discovery threads add devices.
        // Add/remove must go through Add/DeleteEntry/ReplaceEntry, not through the returned list.
        public ArrayList GetDeviceList()
        {
            lock (m_lock)
            {
                return m_aDevices != null ? (ArrayList)m_aDevices.Clone() : new ArrayList();
            }
        }

#endregion


    }
}
