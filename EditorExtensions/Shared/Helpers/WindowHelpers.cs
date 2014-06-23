using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    public static class WindowHelpers
    {
        /// <summary>
        /// Returns an IVsTextView for the given file path, if the given file is open in Visual Studio.
        /// </summary>
        /// <param name="filePath">Full Path of the file you are looking for.</param>
        /// <returns>The IVsTextView for this file, if it is open, null otherwise.</returns>
        /// <remarks>Based on http://stackoverflow.com/questions/2413530/find-an-ivstextview-or-iwpftextview-for-a-given-projectitem-in-vs-2010-rc-exten</remarks>
        public static IVsTextView GetIVsTextView(string filePath)
        {
            var sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)WebEssentialsPackage.DTE;
            using (var serviceProvider = new ServiceProvider(sp))
            {
                uint itemId;
                IVsUIHierarchy uiHierarchy;
                IVsWindowFrame windowFrame;

                if (VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty, out uiHierarchy, out itemId, out windowFrame))
                {
                    // Get the IVsTextView from the windowFrame.
                    return VsShellUtilities.GetTextView(windowFrame);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the IWpfTextView associated with an IVsTextView
        /// </summary>
        /// <param name="textView"></param>
        /// <returns></returns>
        /// <remarks>Based on http://stackoverflow.com/questions/2413530/find-an-ivstextview-or-iwpftextview-for-a-given-projectitem-in-vs-2010-rc-exten</remarks>
        public static IWpfTextView GetWpfTextView(IVsTextView textView)
        {
            IWpfTextView view = null;
            var userData = textView as IVsUserData;

            if (null != userData)
            {
                object holder;
                var guidViewHost = DefGuidList.guidIWpfTextViewHost;
                ErrorHandler.ThrowOnFailure(userData.GetData(ref guidViewHost, out holder));
                var viewHost = (IWpfTextViewHost)holder;
                view = viewHost.TextView;
            }

            return view;
        }
    }
}
