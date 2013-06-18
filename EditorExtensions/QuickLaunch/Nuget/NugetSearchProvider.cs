using System;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.MSDNSearch
{
    /// <summary>
    ///  Search Provider for MSDN Library
    ///  GUID uniquely identifies and differentiates MSDN search from other QuickLaunch searches
    /// </summary>
    [Guid("042C2B4B-C7F7-49DB-B7A2-402EB8DC7891")]
    public class NugetSearchProvider : IVsSearchProvider
    {
        // Defines all string variables like Description(Hover over Search Heading), Search Heading text, Category Shortcut
        private const string _description = "search through Nuget gallery";
        private const string _displayText = "Nuget Gallery"; 
        private const string _categoryShortcut = "nuget";

        public Guid Category
        {
            get { return GetType().GUID; }
        }

        public IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
        {
            if (dwCookie == VSConstants.VSCOOKIE_NIL)
            {
                return null;
            }

            return new NugetSearchTask(this, dwCookie, pSearchQuery, pSearchCallback);
        }

        public IVsSearchItemResult CreateItemResult(string lpszPersistenceData) 
        {
            char[] delim = { '|' };
            string[] strArr = lpszPersistenceData.Split(delim);
            string displayText = strArr[0];
            string url = strArr[1];

            return new NugetSearchResult(displayText, url, this);
        }

        public string DisplayText
        {
            get { return _displayText; }
        }
        
        public string Description
        {
            get 
            {
                return _description;
            }
        }

        public void ProvideSearchSettings(IVsUIDataSource pSearchOptions)
        {
        }

        public string Shortcut
        {
            get
            {
                return _categoryShortcut;
            }
        }

        public string Tooltip
        {
            get { return null; } //no additional tooltip
        }

        public IVsUIObject Icon
        {
            get
            {
                var image = BitmapFrame.Create(new Uri("pack://application:,,,/EditorExtensions;component/Resources/nuget.png", UriKind.RelativeOrAbsolute));
                return WpfPropertyValue.CreateIconObject(image);
            }
        }
    }
}
