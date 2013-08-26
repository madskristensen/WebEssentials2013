using EnvDTE;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class CssRuleRegistry
    {
        public static ConcurrentDictionary<string, CssDocument> DocumentLookup = new ConcurrentDictionary<string, CssDocument>();

        public static IReadOnlyCollection<CssRule> GetAllRules(UnusedCssExtension extension)
        {
            //This lookup needs to be Project -> Browser -> Page (but page -> sheets should be tracked internally by the extension)
            var sheetLocations = extension.GetValidSheetUrlsForCurrentLocation();
            var files = GetFiles(extension.Connection.Project, sheetLocations);
            var allRules = new List<CssRule>();

            foreach(var file in files)
            {
                var store = DocumentLookup.GetOrAdd(file.ToLowerInvariant(), f => new CssDocument(f, DeleteFile));
                allRules.AddRange(store.Rules);
            }

            return allRules;
        }

        public static IEnumerable<string> GetFiles(Project project, IEnumerable<string> locations)
        {
            //TODO: This needs to expand bundles, convert urls to local file names, and move from .min.css files to .css files where applicable
            //NOTE: Project parameter here is for the discovery of linked files, ones that might exist outside of the project structure
            return locations;
        }

        public static HashSet<RuleUsage> Resolve(UnusedCssExtension extension, List<RawRuleUsage> rawUsageData)
        {
            var allRules = GetAllRules(extension);
            var result = new HashSet<RuleUsage>();

            foreach (var dataPoint in rawUsageData)
            {
                var selector = StandardizeSelector(dataPoint.Selector);
                var xpaths = new List<string>(dataPoint.ReferencingXPaths.Where(x => x != "//invalid"));

                foreach (var match in allRules.Where(x => x.CleansedSelectorName == selector))
                {
                    result.Add(new RuleUsage
                    {
                        ReferencingXPaths = xpaths,
                        Rule = match
                    });
                }
            }

            return result;
        }

        private static void DeleteFile(object sender, FileSystemEventArgs e)
        {
            var path = e.FullPath.ToLowerInvariant();
            CssDocument result;
            DocumentLookup.TryRemove(path, out result);
            result.Dispose();
        }
        private static string StandardizeSelector(string selectorText)
        {
            return selectorText.Replace('\r', '\n').Replace("\n", "").Trim();
        }
    }
}
