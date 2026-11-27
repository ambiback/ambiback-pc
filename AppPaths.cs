using System;
using System.IO;
using System.Windows.Forms;

namespace AmbiBack
{
    /// <summary>
    /// Folders AmbiBack PC writes to. The data root is the program folder when it is writable
    /// (portable use), otherwise %LOCALAPPDATA%\AmbiBack\PC.
    /// </summary>
    public static class AppPaths
    {
        private static string _dataRoot;

        public static string DataRoot
        {
            get
            {
                if (_dataRoot == null)
                {
                    _dataRoot = IsWritable(Application.StartupPath)
                        ? Application.StartupPath
                        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AmbiBack", "PC");
                    try { Directory.CreateDirectory(_dataRoot); } catch (Exception ex) { CGlobals.DebugOutput(ex); }
                }
                return _dataRoot;
            }
            set { _dataRoot = value; }
        }

        /// <summary>Default debug log, used when the setting szDebugFileLog is empty.</summary>
        public static string DefaultDebugLog { get { return Path.Combine(DataRoot, "debug.log"); } }

        /// <summary>Log of errors and events that Release builds write (size-limited).</summary>
        public static string ReleaseLog { get { return Path.Combine(DataRoot, "ambiback.log"); } }

        /// <summary>Debug log from the settings, or the default.</summary>
        public static string DebugLog
        {
            get
            {
                string s = Properties.Settings.Default.szDebugFileLog;
                return string.IsNullOrWhiteSpace(s) ? DefaultDebugLog : s.Trim();
            }
        }

        /// <summary>
        /// Path of a config file in the data root. When the data root is not the program folder and
        /// the file only exists in the program folder (older versions), it is copied over once.
        /// </summary>
        public static string ConfigFile(string fileName)
        {
            string target = Path.Combine(DataRoot, fileName);
            try
            {
                string legacy = Path.Combine(Application.StartupPath, fileName);
                if (!File.Exists(target) && File.Exists(legacy) &&
                    !string.Equals(Path.GetFullPath(target), Path.GetFullPath(legacy), StringComparison.OrdinalIgnoreCase))
                    File.Copy(legacy, target);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
            return target;
        }

        public static bool IsWritable(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return false;
                string probe = Path.Combine(dir, ".write-test-" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
