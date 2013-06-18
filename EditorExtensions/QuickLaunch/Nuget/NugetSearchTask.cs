using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.MSDNSearch
{
    public class NugetSearchTask : VsSearchTask
    {
        private NugetSearchProvider provider;

        public NugetSearchTask(NugetSearchProvider provider, uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
            : base(dwCookie, pSearchQuery, pSearchCallback)
        {
            this.provider = provider;
        }

        // Startes the search by sending Query to MSDN
        protected override void OnStartSearch()
        {
            var webQuery = this.GetWebQuery(this.SearchQuery);

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
                
                foreach (var node in entries)
                {
                    var entry = node as XmlElement;
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
                            url = (linkNodes[0] as XmlElement).Attributes["href"].InnerText;
                        }

                        if (title != null && url != null)
                        {
                            var result = new NugetSearchResult(title, url, this.provider);

                            this.SearchCallback.ReportResult(this, result);
                        }
                    }
                }

                this.SearchCallback.ReportComplete(this, (uint)entries.Count);
            }
            catch (Exception)
            {
            }            
            
            //base.OnStartSearch();
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
            //"http://nuget.org/api/v2/Packages()?$orderby=DownloadCount%20desc,Id,LastUpdated%20desc&$filter=((((Id%20ne%20null)%20and%20substringof('{0}',tolower(Id)))%20or%20((Description%20ne%20null)%20and%20substringof('{0}',tolower(Description))))%20or%20((Tags%20ne%20null)%20and%20substringof('%20{0}%20',tolower(Tags))))%20and%20IsAbsoluteLatestVersion&$select=Id,Version,Authors,DownloadCount,VersionDownloadCount,PackageHash,PackageSize&$top=15",
            return string.Format(
                "http://nuget.org/api/v2/Packages()?$orderby=DownloadCount%20desc,Id,LastUpdated%20desc&$filter=((((Id%20ne%20null)%20and%20substringof('{0}',tolower(Id)))%20or%20((Description%20ne%20null)%20and%20substringof('{0}',tolower(Description))))%20or%20((Tags%20ne%20null)%20and%20substringof('%20{0}%20',tolower(Tags))))%20and%20IsAbsoluteLatestVersion&$select=Id&$top=10",
                HttpUtility.UrlEncode(pSearchQuery.SearchString));
        }
    }
}
