using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.MSDNSearch
{
    public class VSSearchResult : IVsSearchItemResult
    {
        private string url;

        public VSSearchResult(string displaytext, string url, VSSearchProvider provider)
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

        //action to be performed on selection of result from result list
        public void InvokeAction()
        {
            Process.Start(this.url);

        }

        //retrieves persistence data for this result
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
