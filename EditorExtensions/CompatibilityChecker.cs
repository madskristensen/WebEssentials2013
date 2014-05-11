using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Call the static helper StartCheckingCompatibility to check if this version of Web Essentials
    /// is compatible with this version of Visual Studio.
    /// </summary>
    internal static class CompatibilityChecker
    {
        /// <summary>
        /// This is the entry point for checking if this version of WebEssentials is compatible
        /// with the running version of Visual Studio by using a web service.
        /// </summary>
        public async static Task StartCheckingCompatibility()
        {
            if (CanCheckCompatibility)
            {
                string weVersion = WebEssentialsVersionString;
                string vsVersion = VisualStudioVersionString;

                if (!string.IsNullOrEmpty(weVersion) && !string.IsNullOrEmpty(vsVersion))
                {
                    Uri uri = new Uri(string.Format("http://vswebessentials.com/update?we={0}&vs={1}", weVersion, vsVersion));
                    await StartRequestingCompatibility(uri);
                }
            }
        }

        /// <summary>
        /// Don't use the web service unless this returns true.
        /// </summary>
        private static bool CanCheckCompatibility
        {
            get
            {
                bool runCheck = true;
                try
                {
                    // GetString could throw ArgumentException
                    string nextCheckString = UserConfigStore.GetString("WebEssentials", "NextCompatibilityCheckDate");

                    DateTime nextCheckDate;
                    if (!string.IsNullOrEmpty(nextCheckString) &&
                        DateTime.TryParseExact(
                            nextCheckString, "u",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out nextCheckDate))
                    {
                        runCheck = DateTime.UtcNow > nextCheckDate;
                    }
                }
                catch (ArgumentException) { }

                return runCheck;
            }
        }

        /// <summary>
        /// Kicks off a web request to see if Web Essentials has an important update
        /// </summary>
        private static async Task StartRequestingCompatibility(Uri uri)
        {
            string responseText = string.Empty;

            try
            {
                HttpClient client = new HttpClient();
                responseText = await client.GetStringAsync(uri);
            }
            catch (WebException) { }
            catch (HttpRequestException) { }

            if (string.IsNullOrEmpty(responseText))
                return;

            Uri upgradeUri;

            if (Uri.TryCreate(responseText, UriKind.Absolute, out upgradeUri) && upgradeUri.Scheme == "http")
            {
                // Make sure the dialog gets shown from the main UI thread
                ThreadHelper.Generic.BeginInvoke(() => ShowDialog(upgradeUri));
            }
            else
            {
                Debug.Fail("Unexpected response: " + responseText);
            }
        }

        /// <summary>
        /// Asks the user if they want to upgrade, and browses to "upgradeUri" if they choose yes.
        /// </summary>
        private static void ShowDialog(Uri upgradeUri)
        {
            IVsUIShell shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;

            if (shell == null)
                return;

            int result;

            if (shell.ShowMessageBox(
                0, // unused
                Guid.Empty, // unused
                null, // title (actually becomes part of the dialog text, not really the title)
                "A new version of Web Essentials is available. It contains important updates. Do you want to install it?",
                null, // help file
                0, // help ID
                OLEMSGBUTTON.OLEMSGBUTTON_YESNO, // buttons
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, // default button
                OLEMSGICON.OLEMSGICON_QUERY, // icon
                0, // system modal
                out result) == VSConstants.S_OK)
            {
                if (result == 6) // IDYES
                {
                    Process.Start(upgradeUri.ToString());
                }
                else
                {
                    DelayNextCheck();
                }
            }
        }

        /// <summary>
        /// Writes a user setting that prevents checking for compatibility for some number of days
        /// </summary>
        private static void DelayNextCheck()
        {
            DateTime nextTime = DateTime.UtcNow.AddDays(7);
            string nextTimeString = nextTime.ToString("u", CultureInfo.InvariantCulture);

            try
            {
                WritableSettingsStore store = UserConfigStore;
                store.CreateCollection("WebEssentials");
                store.SetString("WebEssentials", "NextCompatibilityCheckDate", nextTimeString);
            }
            catch (ArgumentException)
            {
                Debug.Fail(@"Failed to set WebEssentials\NextCompatibilityCheckDate");
            }
        }

        /// <summary>
        /// Allows reading global VS settings
        /// </summary>
        private static Microsoft.VisualStudio.Settings.SettingsStore GlobalConfigStore
        {
            get
            {
                SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                return settingsManager.GetReadOnlySettingsStore(Microsoft.VisualStudio.Settings.SettingsScope.Configuration);
            }
        }

        /// <summary>
        /// Allows reading/writing user-specific settings
        /// </summary>
        private static WritableSettingsStore UserConfigStore
        {
            get
            {
                SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                return settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            }
        }

        private static string WebEssentialsVersionString
        {
            get
            {
                // TODO: See if there is a way to detect the version of the VSIX

                string versionString = string.Empty;

                // Get the version of this DLL
                foreach (AssemblyFileVersionAttribute versionAttribute in
                    typeof(EditorExtensionsPackage).Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false))
                {
                    versionString = versionAttribute.Version;
                    break;
                }

                return versionString;
            }
        }

        public static string VisualStudioVersionString
        {
            get
            {
                string versionString = string.Empty;

                // See if the version override is set in the registry
                try
                {
                    versionString = GlobalConfigStore.GetString("SplashInfo", "EnvVersion");
                }
                catch (ArgumentException) { }

                if (string.IsNullOrEmpty(versionString))
                {
                    IVsAppId appId = Package.GetGlobalService(typeof(IVsAppId)) as IVsAppId;
                    if (appId != null)
                    {
                        const int VSAPROPID_ReleaseVersion = -8597;
                        object versionObject = null;

                        if (appId.GetProperty(VSAPROPID_ReleaseVersion, out versionObject) == VSConstants.S_OK &&
                            (versionString = versionObject as string) == null)
                            versionString = string.Empty;
                    }
                }

                if (!string.IsNullOrEmpty(versionString) && versionString.IndexOf(' ') != -1)
                {
                    // Only get the version number, the update info afterwards isn't needed
                    versionString = versionString.Substring(0, versionString.IndexOf(' '));
                }

                return versionString;
            }
        }

        [Guid("1EAA526A-0898-11d3-B868-00C04F79F802")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IVsAppId
        {
            [PreserveSig]
            int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider pSP);

            [PreserveSig]
            int GetProperty(int propid, [MarshalAs(UnmanagedType.Struct)] out object pvar);

            [PreserveSig]
            int SetProperty(int propid, [MarshalAs(UnmanagedType.Struct)] object var);

            [PreserveSig]
            int GetGuidProperty(int propid, out Guid guid);

            [PreserveSig]
            int SetGuidProperty(int propid, ref Guid rguid);

            [PreserveSig]
            int Initialize();
        }
    }
}
