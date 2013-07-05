﻿using Microsoft.VisualStudio.Shell.Interop;
using System;

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
                    //pane.Activate();
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
                string message = ex.Message + Environment.NewLine + ex.StackTrace;

                if (!string.IsNullOrEmpty(ex.StackTrace))
                    message += Environment.NewLine + ex.StackTrace;

                Log(message);
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
                        pane = EditorExtensionsPackage.Instance.GetOutputPane(new Guid("f1536ef8-92ec-443c-9ed7-fdadf150da44"), "Web Essentials");
                    }
                }
            }

            return pane != null;
        }
    }
}
