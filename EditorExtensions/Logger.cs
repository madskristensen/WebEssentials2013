using System;
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
                    pane.OutputString(DateTime.Now.ToString() + ": " + message + Environment.NewLine).Equals(VSConstants.S_OK);
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
