
using System;
using System.Collections.Generic;
using System.Text;

// Custom
using System.Reflection;
using System.Resources;
using System.Globalization;

namespace AmbiBack
{
    public class CLanguage
    {
        // Wrapper to obtain string from ressource with backup 
        static public string GetStringFromRessource(string szRessourceID, string szDefaultString, ResourceManager myRessource, ResourceManager myBackupRessource)
        {
            try
            {
                string szReturn = null; 
                if ( myRessource != null ) 
                    szReturn = myRessource.GetString(szRessourceID);
                if (szReturn != null)
                    return szReturn;
                else
                {
                    if ( myBackupRessource != null ) 
                        // return from backup!
                        return GetStringFromRessource(szRessourceID, szDefaultString, myBackupRessource); 
                    else
                        // return default in this case
                        return szDefaultString;
                }
            }
            catch (Exception ex)
            {
                // Debug helper
                CGlobals.DebugOutput(ex);

                // return default in any exception case!
                return szDefaultString;
            }
        }

        // Helper function to obtain a ressource.
        static public string GetStringFromRessource(string szRessourceID, string szDefaultString, ResourceManager myRessource)
        {
            try
            {
                string szReturn = null; 
                if ( myRessource != null ) 
                    szReturn = myRessource.GetString(szRessourceID);
                if (szReturn != null)
                    return szReturn;
                else
                    // return default in this case
                    return szDefaultString;
            }
            catch (Exception ex)
            {
                // Debug helper
                CGlobals.DebugOutput(ex); 

                // return default in any exception case!
                return szDefaultString;
            }
        }

        // Helper function to obtain a ressource.
        static public string GetAndReplaceStringFromRessource(string szRessourceID, string szDefaultString, ResourceManager myRessource, string szReplace1 = null, string szReplace2 = null, string szReplace3 = null, string szReplace4 = null)
        {
            string szReturn = szDefaultString; 

            try
            {
                // Search for string ressource
                if (myRessource != null)
                    szReturn = myRessource.GetString(szRessourceID);

                // Set string back to default 
                if (szReturn == null)
                    szReturn = szDefaultString;

                // Replace strings
                if (szReplace1 != null)
                    szReturn = szReturn.Replace("%1", szReplace1);
                if (szReplace2 != null)
                    szReturn = szReturn.Replace("%2", szReplace2);
                if (szReplace3 != null)
                    szReturn = szReturn.Replace("%3", szReplace3);
                if (szReplace4 != null)
                    szReturn = szReturn.Replace("%4", szReplace4); 
            }
            catch (Exception ex)
            {
                // Debug helper
                CGlobals.DebugOutput(ex);
            }

            // return default in any exception case!
            return szReturn;
        }

        // Wrapper for calls without backup ressource
        static public string[] GetStringFromRessource(string[] szRessourceIDs, string[] szDefaultStrings, ResourceManager myRessource)
        {
            return GetStringFromRessource(szRessourceIDs, szDefaultStrings, myRessource, null); 
        }

        // Get language strings for a strinhg array including backup ressource 
        static public string[] GetStringFromRessource(string[] szRessourceIDs, string[] szDefaultStrings, ResourceManager myRessource, ResourceManager myBackupRessource)
        {
            // Create result array
            string[] aResults = new string[szRessourceIDs.Length]; 

            for (int iNum = 0; iNum < szRessourceIDs.Length; iNum++)
            {
                try
                {
                    aResults[iNum] = myRessource.GetString(szRessourceIDs[iNum]);
                    if (aResults[iNum] == null)
                    {
                        if ( myBackupRessource != null ) 
                            // Get ressource from backup!
                            aResults[iNum] = GetStringFromRessource(szRessourceIDs[iNum],szDefaultStrings[iNum],myBackupRessource);
                        else
                            // return default in this case
                            aResults[iNum] = szDefaultStrings[iNum];
                    }
                }
                catch (Exception ex)
                {
                    // Debug helper
                    CGlobals.DebugOutput(ex);

                    // return default in this case
                    aResults[iNum] = szDefaultStrings[iNum];
                }
            }

            // return localized array
            return aResults; 
        }


    }
}
