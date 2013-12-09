using System;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.EditorExtensions
{
    public static class Logger
    {
        private static IVsOutputWindowPane pane;
        private static object _syncRoot = new object();

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                if (EnsurePane())
                {
                    pane.OutputString(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
                }
            }
            catch
            {
                // Do nothing
            }
        }

        public static void Log(Exception ex)
        {
            if (ex != null)
            {
                Log(ex.ToString());
            }
        }

        public static void ShowMessage(string message, string title = "Web Essentials",
            MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK,
            MessageBoxIcon messageBoxIcon = MessageBoxIcon.Warning,
            MessageBoxDefaultButton messageBoxDefaultButton = MessageBoxDefaultButton.Button1)
        {
            if (WESettings.GetBoolean(WESettings.Keys.AllMessagesToOutputWindow))
            {
                Log(String.Format("{0}: {1}", title, message));
            }
            else
            {
                MessageBox.Show(message, title, messageBoxButtons, messageBoxIcon, messageBoxDefaultButton);
            }
        }

        private static bool EnsurePane()
        {
            if (pane == null)
            {
                lock (_syncRoot)
                {
                    if (pane == null)
                    {
                        pane = EditorExtensionsPackage.Instance.GetOutputPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, "Web Essentials");
                    }
                }
            }

            return pane != null;
        }
    }
}
