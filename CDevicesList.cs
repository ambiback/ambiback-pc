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
            return m_aDevices.Count;
        }
        public string GetListName() { return m_szListName; }


        public void InitList()
        {
            try
            {
                //Successfully read the file, no start parsing
                if (XMLdoc == null)
                    XMLdoc = new XmlDocument();

                if (m_aDevices == null)
                    m_aDevices = new ArrayList();
                else
                    m_aDevices.Clear(); 

                //Now load and parse XML
                XMLdoc.Load(this.m_szFileName);

                // Get Primary nodes list
                XmlNodeList nodesDevices = XMLdoc.GetElementsByTagName("devices");

                CDevice newDevice = null;
                foreach (XmlNode myNode in nodesDevices.Item(0).ChildNodes)
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
                    else
                    {
                        throw (new Exception("ERROR_INITLIST_UNKNOWN_TYPE"));
                    }

                    //Add into arraylist
                    if (newDevice != null) {
                        newDevice.SetDeviceID();
                        if (this.GetDevice(newDevice.GetDeviceID()) == null)
                            m_aDevices.Add(newDevice);
                        else
                            CGlobals.DebugOutput("ERROR, device with ID " + newDevice.GetDeviceID() + " already added!");
                    }
                }
            }
            catch (FileNotFoundException)
            {
                CGlobals.IsFirstTime = true;
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("InitList", ex);
            }
        }
        public void SaveList()
        {
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

                foreach (CDevice myDevice in m_aDevices)
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
                XMLdoc.Save(this.m_szFileName);
            }
        }

        public CDevice Add(CDevice newDevice)
        {
            CDevice addedDevice = null;
            if ((addedDevice = this.GetDevice(newDevice.GetDeviceID())) == null)
            {
                // Add new device
                m_aDevices.Add(newDevice);
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
            m_aDevices.Remove(removeDevice);
            m_aDevices.Add(addDevice);
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
            m_aDevices.Remove(delDevice);
        }
        public void DeleteEntry(int iIndex)
        {
            // Delete entry by indexnumber
            m_aDevices.RemoveAt(iIndex);
        }

        public CDevice GetDevice(int iIndex)
        {
            // Retur´n Host by Entry
            return (CDevice)m_aDevices[iIndex];
        }

        public CDevice GetDevice(string szId)
        {
            foreach (CDevice myDevice in m_aDevices)
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
            foreach (CDevice myDevice in m_aDevices)
            {
                // check if ConfigEntry was clicked
                if (myDevice.GetIPAddress() != null && 
                    myDevice.GetIPAddress().ToString() == ipAddress.ToString())
                    return myDevice;
            }

            // Not found at all
            return null;
        }


        public ArrayList GetDeviceList()
        {
            return m_aDevices;
        }

#endregion


    }
}
