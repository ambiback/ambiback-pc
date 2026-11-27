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

        // Set when the file existed but could not be read/parsed. SaveList then must not overwrite it,
        // otherwise a corrupt or half-readable file would be replaced by an empty or shortened list.
        private bool m_bLoadFailed = false;

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


        public void InitList()
        {
            try
            {
                m_bLoadFailed = false;
                m_aPreservedNodes.Clear();

                //Successfully read the file, no start parsing
                if (XMLdoc == null)
                    XMLdoc = new XmlDocument();

                lock (m_lock)
                {
                    if (m_aDevices == null)
                        m_aDevices = new ArrayList();
                    else
                        m_aDevices.Clear();
                }

                //Now load and parse XML. From here on a failure means the file exists but is not usable,
                //so SaveList must keep its hands off it.
                m_bLoadFailed = true;
                XMLdoc.Load(this.m_szFileName);

                // Get Primary nodes list
                XmlNodeList nodesDevices = XMLdoc.GetElementsByTagName("devices");

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

                // Every node was handled, saving is safe again
                m_bLoadFailed = false;
            }
            catch (FileNotFoundException)
            {
                // No file yet: nothing to protect
                m_bLoadFailed = false;
                CGlobals.IsFirstTime = true;
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("InitList", ex);
            }
        }
        public void SaveList()
        {
            // The file existed but could not be read completely: keep it exactly as it is
            if (m_bLoadFailed)
            {
                CGlobals.LogEvent("SaveList: device file was not loaded correctly, not overwriting " + this.m_szFileName);
                return;
            }

            // Only write when the XML was built completely, a half-built list would drop devices
            bool bBuilt = false;
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
                bBuilt = true;

                // Always keep an backup of the last hostfile
                File.Delete(this.m_szFileName + ".bak");
                File.Move(this.m_szFileName, this.m_szFileName + ".bak");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.FileName);
                if (CGlobals.IsFirstTime == false)
                    CGlobals.HandleException("SaveList Failed moving old file", ex);
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("SaveList", ex);
            }
            finally
            {
                // Now save XML Elements
                if (bBuilt)
                    XMLdoc.Save(this.m_szFileName);
            }
        }

        public CDevice Add(CDevice newDevice)
        {
            CDevice addedDevice = null;
            if ((addedDevice = this.GetDevice(newDevice.GetDeviceID())) == null)
            {
                // Add new device
                lock (m_lock)
                {
                    m_aDevices.Add(newDevice);
                }
                addedDevice = newDevice; 
            }
            else
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
            foreach (CDevice myDevice in GetDeviceList())
            {
                // check if ConfigEntry was clicked
                if (myDevice.GetDeviceID() == szId)
                    return myDevice;
            }

            // Not found at all
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
