using EnvDTE;
using Microsoft.VisualStudio.Web.BrowserLink;
using Microsoft.VisualStudio.Web.PageInspector.Package;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class CssRuleRegistry
    {
        public static ConcurrentDictionary<string, CssDocument> DocumentLookup = new ConcurrentDictionary<string, CssDocument>();

        public static IReadOnlyCollection<CssRule> GetAllRules(UnusedCssExtension extension)
        {
            //This lookup needs to be Project -> Browser -> Page (but page -> sheets should be tracked internally by the extension)
            var sheetLocations = extension.GetValidSheetUrlsForCurrentLocation();
            var files = GetFiles(extension, sheetLocations);
            var allRules = new List<CssRule>();

            foreach (var file in files)
            {
                var store = DocumentLookup.GetOrAdd(file.ToLowerInvariant(), f => new CssDocument(f, DeleteFile));
                allRules.AddRange(store.Rules);
            }

            return allRules;
        }

        public static Task<IReadOnlyCollection<CssRule>> GetAllRulesAsync(UnusedCssExtension extension)
        {
            return Task.Factory.StartNew(() => GetAllRules(extension));
        }

        public static IEnumerable<string> GetFiles(UnusedCssExtension extension, IEnumerable<string> locations)
        {
            var project = extension.Connection.Project;
            //TODO: This needs to expand bundles, convert urls to local file names, and move from .min.css files to .css files where applicable
            //NOTE: Project parameter here is for the discovery of linked files, ones that might exist outside of the project structure
            var projectPath = project.Properties.Item("FullPath").Value.ToString();
            var projectUri = new Uri(projectPath, UriKind.Absolute);

            foreach (var location in locations)
            {
                if (location == null)
                {
                    continue;
                }

                var locationUri = new Uri(location, UriKind.RelativeOrAbsolute);
                Uri realLocation;

                //No absolute paths, unless they map into the same project
                if (locationUri.IsAbsoluteUri)
                {
                    if (projectUri.IsBaseOf(locationUri))
                    {
                        locationUri = locationUri.MakeRelativeUri(projectUri);
                    }
                    else
                    {
                        //TODO: Fix this, it'll only work if the site is at the root of the server as is
                        locationUri = new Uri(locationUri.LocalPath, UriKind.Relative);
                    }

                    if (locationUri.IsAbsoluteUri)
                    {
                        continue;
                    }
                }

                var locationUrl = locationUri.ToString().TrimStart('/').ToLowerInvariant();

                //Hoist .min.css -> .css
                if (locationUrl.EndsWith(".min.css"))
                {
                    locationUrl = locationUrl.Substring(0, locationUrl.Length - 8) + ".css";
                }

                locationUri = new Uri(locationUrl, UriKind.Relative);

                if (Uri.TryCreate(projectUri, locationUri, out realLocation))
                {
                    yield return realLocation.LocalPath;
                }
            }

            yield break;
        }

        public static async Task<HashSet<RuleUsage>> ResolveAsync(UnusedCssExtension extension, List<RawRuleUsage> rawUsageData)
        {
            var allRules = await GetAllRulesAsync(extension);
            var result = new HashSet<RuleUsage>();

            foreach (var dataPoint in rawUsageData)
            {
                var selector = StandardizeSelector(dataPoint.Selector);
                var locations = new HashSet<SourceLocation>(dataPoint.SourceLocations.Where(x => x != null));

                foreach (var match in allRules.Where(x => x.CleansedSelectorName == selector))
                {
                    result.Add(new RuleUsage
                    {
                        SourceLocations = locations,
                        Rule = match
                    });
                }
            }

            return result;
        }

        private static void DeleteFile(object sender, FileSystemEventArgs e)
        {
            //NOTE: VS apparently deletes the file on save and creates a new one, disposing here makes things not work

            //var path = e.FullPath.ToLowerInvariant();
            //CssDocument result;
            //DocumentLookup.TryRemove(path, out result);
            //result.Dispose();
        }

        private static string StandardizeSelector(string selectorText)
        {
            return selectorText.Replace('\r', '\n').Replace("\n", "").Trim();
        }
    }
}
