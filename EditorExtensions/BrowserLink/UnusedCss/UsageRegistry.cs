using EnvDTE;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Web.BrowserLink;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class UsageRegistry
    {
        private static readonly ConcurrentDictionary<string, CompositeUsageData> UsageDataByLocation = new ConcurrentDictionary<string, CompositeUsageData>();

        private static readonly ConcurrentDictionary<BrowserLinkConnection, ConcurrentDictionary<string, CompositeUsageData>> UsageDataByConnectionAndLocation = new ConcurrentDictionary<BrowserLinkConnection, ConcurrentDictionary<string, CompositeUsageData>>();

        private static readonly ConcurrentDictionary<string, CompositeUsageData> UsageDataByProject = new ConcurrentDictionary<string, CompositeUsageData>();

        public static void Merge(UnusedCssExtension extension, IUsageDataSource source)
        {
            var url = extension.Connection.Url.ToString().ToLowerInvariant();
            var crossBrowserPageBucket = UsageDataByLocation.GetOrAdd(url, location => new CompositeUsageData(extension));

            var connectionSiteBucket = UsageDataByConnectionAndLocation.GetOrAdd(extension.Connection, conn => new ConcurrentDictionary<string, CompositeUsageData>());
            var connectionPageBucket = connectionSiteBucket.GetOrAdd(url, location => new CompositeUsageData(extension));

            var projectBucket = UsageDataByProject.GetOrAdd(extension.Connection.Project.UniqueName, proj => new CompositeUsageData(extension));

            crossBrowserPageBucket.AddUsageSource(source);
            connectionPageBucket.AddUsageSource(source);
            projectBucket.AddUsageSource(source);

            OnUsageDataUpdated();
        }

        public static bool IsAnyUsageDataCaptured
        {
            get { return UsageDataByLocation.Count > 0; }
        }

        public static void Reset()
        {
            UsageDataByLocation.Clear();
            UsageDataByConnectionAndLocation.Clear();
            UsageDataByProject.Clear();

            OnUsageDataUpdated();
        }

        public static IEnumerable<Task> GetWarnings(Project project)
        {
            CompositeUsageData data;

            if (!UsageDataByProject.TryGetValue(project.UniqueName, out data))
            {
                return new Task[0];
            }

            return data.GetWarnings();
        }

        public static IEnumerable<Task> GetWarnings(Uri uri)
        {
            CompositeUsageData data;
            var url = uri.ToString().ToLowerInvariant();

            if (!UsageDataByLocation.TryGetValue(url, out data))
            {
                return new Task[0];
            }

            return data.GetWarnings(uri);
        }

        public static IEnumerable<IStylingRule> GetAllUnusedRules()
        {
            return UsageDataByProject.Values.SelectMany(x => x.GetUnusedRules()).Distinct();
        }

        public static event EventHandler UsageDataUpdated;

        private static void OnUsageDataUpdated()
        {
            if (UsageDataUpdated != null)
            {
                UsageDataUpdated(null, null);
            }
        }

        public static async System.Threading.Tasks.Task ResyncAsync()
        {
            foreach (var value in UsageDataByProject.Values)
            {
                await value.ResyncAsync();
            }

            foreach (var value in UsageDataByLocation.Values)
            {
                await value.ResyncAsync();
            }

            foreach (var bag in UsageDataByConnectionAndLocation.Values)
            {
                foreach (var value in bag.Values)
                {
                    await value.ResyncAsync();
                }
            }

            OnUsageDataUpdated();
            MessageDisplayManager.Refresh();
        }

        public static void Resync()
        {
            foreach (var value in UsageDataByProject.Values)
            {
                value.Resync();
            }

            foreach (var value in UsageDataByLocation.Values)
            {
                value.Resync();
            }

            foreach (var bag in UsageDataByConnectionAndLocation.Values)
            {
                foreach (var value in bag.Values)
                {
                    value.Resync();
                }
            }

            OnUsageDataUpdated();
            MessageDisplayManager.Refresh();
        }

        internal static IEnumerable<IStylingRule> GetAllUnusedRules(HashSet<IStylingRule> sheetRules)
        {
            return sheetRules.Intersect(UsageDataByProject.Values.SelectMany(x => x.GetUnusedRules()));
        }
        
        public static bool IsAProtectedClass(IStylingRule rule)
        {
            var selectorName = rule.DisplaySelectorName;
            var cleansedName = RuleRegistry.StandardizeSelector(selectorName);
            return cleansedName.IndexOf(":visited", StringComparison.Ordinal) > -1 || cleansedName.IndexOf(":hover", StringComparison.Ordinal) > -1 || cleansedName.IndexOf(":active", StringComparison.Ordinal) > -1;
        }

        internal static bool IsRuleUsed(RuleSet rule)
        {
            return GetAllUnusedRules().All(x => !x.Is(rule));
        }
    }
}
