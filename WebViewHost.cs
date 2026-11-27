using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace AmbiBack
{
    /// <summary>
    /// Edge WebView2 (Chromium) for the device web pages and the video playlists.
    /// All views share one environment with its user data in %LOCALAPPDATA%\AmbiBack\PC\WebView2,
    /// so it also works when the program folder is not writable.
    /// </summary>
    internal static class WebViewHost
    {
        /// <summary>Host name of the local player page (mapped to a folder, served over https).</summary>
        public const string PlayerHost = "player.ambiback-pc.local";

        private static Task<CoreWebView2Environment> environment;

        public static string UserDataFolder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AmbiBack", "PC", "WebView2"); }
        }

        /// <summary>Version of the installed WebView2 runtime, or null when it is missing.</summary>
        public static string RuntimeVersion
        {
            get
            {
                try { return CoreWebView2Environment.GetAvailableBrowserVersionString(); }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput("WebViewHost: no WebView2 runtime: " + ex.Message);
                    return null;
                }
            }
        }

        /// <summary>Create the CoreWebView2 of a WebView2 control with the shared environment.</summary>
        public static async Task InitAsync(WebView2 view)
        {
            if (environment == null)
            {
                Directory.CreateDirectory(UserDataFolder);
                // The calibration and demo videos should start without a click
                var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
                environment = CoreWebView2Environment.CreateAsync(null, UserDataFolder, options);
            }
            Task<CoreWebView2Environment> envTask = environment;
            CoreWebView2Environment env;
            try
            {
                env = await envTask;
            }
            catch
            {
                // Do not cache a failed creation forever: the next call tries again
                if (ReferenceEquals(environment, envTask))
                    environment = null;
                throw;
            }
            await view.EnsureCoreWebView2Async(env);
            view.CoreWebView2.Settings.IsStatusBarEnabled = false;
            // Links that open a new window stay in this view
            view.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                view.CoreWebView2.Navigate(e.Uri);
            };
        }

        /// <summary>
        /// Map the local player page to https://player.ambiback-pc.local/. YouTube refuses embedded videos
        /// without an http(s) origin and referrer, so the iframe must not be loaded from a string or file.
        /// </summary>
        public static void MapPlayerPage(WebView2 view)
        {
            string folder = Path.Combine(UserDataFolder, "player");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "playlist.html"), PlayerHtml);
            view.CoreWebView2.SetVirtualHostNameToFolderMapping(PlayerHost, folder, CoreWebView2HostResourceAccessKind.DenyCors);
        }

        public static string PlaylistUrl(string listId)
        {
            return "https://" + PlayerHost + "/playlist.html?list=" + Uri.EscapeDataString(listId);
        }

        private const string PlayerHtml =
            "<!doctype html><html><head><meta charset=\"utf-8\">" +
            "<meta name=\"referrer\" content=\"strict-origin-when-cross-origin\">" +
            "<style>html,body{margin:0;height:100%;background:#000;overflow:hidden}iframe{border:0;width:100%;height:100%}</style>" +
            "</head><body><script>" +
            "var list=new URLSearchParams(location.search).get('list')||'';" +
            "var f=document.createElement('iframe');" +
            "f.src='https://www.youtube.com/embed/videoseries?list='+encodeURIComponent(list)+'&autoplay=1&rel=0';" +
            "f.allow='autoplay; encrypted-media; fullscreen; picture-in-picture';" +
            "f.allowFullscreen=true;" +
            "f.referrerPolicy='strict-origin-when-cross-origin';" +
            "document.body.appendChild(f);" +
            "</script></body></html>";
    }
}
