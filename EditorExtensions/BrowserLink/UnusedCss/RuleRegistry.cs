using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class RuleRegistry
    {
        public static IReadOnlyCollection<IStylingRule> GetAllRules(UnusedCssExtension extension)
        {
            //This lookup needs to be Project -> Browser -> Page (but page -> sheets should be tracked internally by the extension)
            var files = UnusedCssExtension.GetValidSheetUrls();
            var allRules = new List<IStylingRule>();

            foreach (var file in files)
            {
                var store = DocumentFactory.GetDocument(file.ToLowerInvariant(), DeleteFile);

                if (store != null)
                {
                    allRules.AddRange(store.Rules);
                }
            }

            return allRules;
        }

        public static Task<IReadOnlyCollection<IStylingRule>> GetAllRulesAsync(UnusedCssExtension extension)
        {
            return Task.Factory.StartNew(() => GetAllRules(extension));
        }

        public static async Task<HashSet<RuleUsage>> ResolveAsync(UnusedCssExtension extension, List<RawRuleUsage> rawUsageData)
        {
            var allRules = await GetAllRulesAsync(extension);
            var result = new HashSet<RuleUsage>();

            foreach (var dataPoint in rawUsageData)
            {
                var selector = StandardizeSelector(dataPoint.Selector);
                var locations = new HashSet<SourceLocation>(dataPoint.SourceLocations.Where(x => x != null));

                foreach (var match in allRules.Where(x => x.IsMatch(selector)))
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

        public static string StandardizeSelector(string selectorText)
        {
            var tmp = selectorText.Replace('\r', ' ').Replace('\n', ' ').Trim().ToLowerInvariant();

            while (tmp.Contains("  "))
            {
                tmp = tmp.Replace("  ", " ");
            }

            return tmp.Replace(", ", ",");
        }

        internal static HashSet<RuleUsage> Resolve(UnusedCssExtension extension, List<RawRuleUsage> rawUsageData)
        {
            var allRules = GetAllRules(extension);
            var result = new HashSet<RuleUsage>();

            foreach (var dataPoint in rawUsageData)
            {
                var selector = StandardizeSelector(dataPoint.Selector);
                var locations = new HashSet<SourceLocation>(dataPoint.SourceLocations.Where(x => x != null));

                foreach (var match in allRules.Where(x => x.IsMatch(selector)))
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
    }
}
