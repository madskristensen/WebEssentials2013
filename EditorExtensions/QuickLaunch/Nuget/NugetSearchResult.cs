using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.MSDNSearch
{
    public class NugetSearchResult : IVsSearchItemResult
    {
        private string url;

        public NugetSearchResult(string displaytext, string url, NugetSearchProvider provider)
        {
            this.DisplayText = displaytext;
            this.SearchProvider = provider;
            this.url = url;
            this.PersistenceData = displaytext + "|" + url;
            this.Icon = provider.Icon;
        }

        public VisualStudio.OLE.Interop.IDataObject DataObject
        {
            get { return null; }
        }

        public string Description
        {
            get;
            private set;
        }

        public string DisplayText
        {
            get;
            private set;
        }

        public IVsUIObject Icon
        {
            get;
            private set;
        }

        public void InvokeAction()
        {
            Process.Start(this.url);
        }

        public string PersistenceData
        {
            get;
            private set;
        }

        public IVsSearchProvider SearchProvider
        {
            get;
            private set;
        }

        public string Tooltip
        {
            get;
            private set;
        }
    }
}
