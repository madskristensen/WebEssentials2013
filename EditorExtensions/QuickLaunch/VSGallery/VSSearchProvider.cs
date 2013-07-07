using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    ///  Search Provider for MSDN Library
    ///  GUID uniquely identifies and differentiates MSDN search from other QuickLaunch searches
    /// </summary>
    [Guid("042C2B4B-C7F7-49DB-B7A2-402EB8DC7892")]
    public class VSSearchProvider : IVsSearchProvider
    {
        // Defines all string variables like Description(Hover over Search Heading), Search Heading text, Category Shortcut
        private const string DescriptionString = "search through the Visual Studio Gallery";
        private const string DisplayTextString = "Visual Studio Gallery";
        private const string CategoryShortcutString = "vs";

        // Get the GUID that identifies this search provider
        public Guid Category
        {
            get { return GetType().GUID; }
        }

        //Main Search method that calls MSDNSearchTask to create and execute search query
        public IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
        {
            if (dwCookie == VSConstants.VSCOOKIE_NIL)
            {
                return null;
            }

            return new VSSearchTask(this, dwCookie, pSearchQuery, pSearchCallback);
        }

        //Verifies persistent data to populate MRU list with previously selected result
        public IVsSearchItemResult CreateItemResult(string lpszPersistenceData)
        {
            char[] delim = { '|' };
            string[] strArr = lpszPersistenceData.Split(delim);
            string displayText = strArr[0];
            string url = strArr[1];

            return new VSSearchResult(displayText, url, this);
        }

        //MSDN Search Category Heading 
        public string DisplayText
        {
            get { return DisplayTextString; }
        }

        //MSDN Search Description - shows as tooltip on hover over Search Category Heading
        public string Description
        {
            get
            {
                return DescriptionString;
            }
        }

        //
        public void ProvideSearchSettings(IVsUIDataSource pSearchOptions)
        {
        }

        //MSDN Category shortcut to scope results to to show only from MSDN Library
        public string Shortcut
        {
            get
            {
                return CategoryShortcutString;
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
                var image = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/vsgallery.png", UriKind.RelativeOrAbsolute));
                return WpfPropertyValue.CreateIconObject(image);
            }
        }
    }
}
