using System;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.MSDNSearch
{
    public class VSSearchTask : VsSearchTask
    {
        private VSSearchProvider provider;

        public VSSearchTask(VSSearchProvider provider, uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
            : base(dwCookie, pSearchQuery, pSearchCallback)
        {
            this.provider = provider;
        }

        protected override void OnStartSearch()
        {
            string webQuery = this.GetWebQuery(this.SearchQuery);

            try
            {
                //parser code to parse through RSS results
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(webQuery);
                var root = xmlDocument.DocumentElement;

                //each item/entry is a unique result
                var entries = root.GetElementsByTagName("item");
                if (entries.Count == 0)
                    entries = root.GetElementsByTagName("entry");

                for (int i = 0; i < Math.Min(10, entries.Count); i++)
                {
                    var entry = entries[i] as XmlElement;

                    if (entry != null)
                    {
                        string title = null;
                        string url = null;

                        //title tag provides result title
                        var titleNodes = entry.GetElementsByTagName("title");
                        if (titleNodes.Count > 0)
                        {
                            title = (titleNodes[0] as XmlElement).InnerText;
                        }

                        //link / url / id tag provides the URL linking the result string to its page
                        var linkNodes = entry.GetElementsByTagName("link");
                        if (linkNodes.Count == 0)
                            linkNodes = entry.GetElementsByTagName("url");
                        if (linkNodes.Count == 0)
                            linkNodes = entry.GetElementsByTagName("id");

                        if (linkNodes.Count > 0)
                        {
                            url = (linkNodes[0] as XmlElement).InnerText;
                        }

                        if (title != null && url != null)
                        {
                            var result = new VSSearchResult(title, url, this.provider);

                            this.SearchCallback.ReportResult(this, result);
                        }
                    }
                }

                this.SearchCallback.ReportComplete(this, (uint)entries.Count);
            }
            catch (Exception)
            {
                this.SearchCallback.ReportComplete(this, 0);
            }
        }

        protected new IVsSearchProviderCallback SearchCallback
        {
            get
            {
                return (IVsSearchProviderCallback)base.SearchCallback;
            }
        }

        public string GetWebQuery(IVsSearchQuery pSearchQuery)
        {
            return string.Format(
                "http://visualstudiogallery.msdn.microsoft.com/site/feeds/searchRss?f%5B0%5D.Type=SearchText&f%5B0%5D.Value={0}&f%5B1%5D.Type=RootCategory&f%5B1%5D.Value=tools&f%5B1%5D.Text=Tools&sortBy=Relevance",
                HttpUtility.UrlEncode(pSearchQuery.SearchString));
        }
    }
}
