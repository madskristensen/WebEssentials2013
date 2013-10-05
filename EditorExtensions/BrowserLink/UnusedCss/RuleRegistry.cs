﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class RuleRegistry
    {
        public static IReadOnlyCollection<IStylingRule> GetAllRules()
        {
            //This lookup needs to be Project -> Browser -> Page (but page -> sheets should be tracked internally by the extension)
            var files = UnusedCssExtension.GetValidSheetUrls();
            var allRules = new List<IStylingRule>();

            foreach (var file in files)
            {
                var store = DocumentFactory.GetDocument(file.ToLowerInvariant(), true);

                if (store != null)
                {
                    store.IsProcessingUnusedCssRules = true;
                    allRules.AddRange(store.Rules);
                }
            }

            return allRules;
        }

        public static Task<IReadOnlyCollection<IStylingRule>> GetAllRulesAsync()
        {
            return Task.Factory.StartNew(() => AmbientRuleContext.GetAllRules());
        }

        public static async Task<HashSet<RuleUsage>> ResolveAsync(List<RawRuleUsage> rawUsageData)
        {
            var allRules = await GetAllRulesAsync();
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

        public static string StandardizeSelector(string selectorText)
        {
            var tmp = selectorText.Replace('\r', ' ').Replace('\n', ' ').Replace("\'", "").Replace("\"", "").Trim().ToLowerInvariant();

            while (tmp.Contains("  "))
            {
                tmp = tmp.Replace("  ", " ");
            }

            return tmp.Replace(", ", ",");
        }

        internal static HashSet<RuleUsage> Resolve(List<RawRuleUsage> rawUsageData)
        {
            var allRules = AmbientRuleContext.GetAllRules();
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
