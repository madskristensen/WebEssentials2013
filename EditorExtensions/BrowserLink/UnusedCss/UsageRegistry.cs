using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class UsageRegistry
    {
        private static readonly ConcurrentDictionary<string, SessionResult> UsageDataByLocation = new ConcurrentDictionary<string, SessionResult>();
        private static readonly ConcurrentDictionary<BrowserLinkConnection, ConcurrentDictionary<string, SessionResult>> UsageDataByConnectionAndLocation = new ConcurrentDictionary<BrowserLinkConnection, ConcurrentDictionary<string, SessionResult>>();
        private static readonly ConcurrentDictionary<string, SessionResult> UsageDataByProject = new ConcurrentDictionary<string, SessionResult>();
        public static event EventHandler UsageDataUpdated;

        public static bool IsAnyUsageDataCaptured
        {
            get { return UsageDataByLocation.Count > 0; }
        }

        public static void Merge(UnusedCssExtension extension, SessionResult source)
        {
            var url = extension.Connection.Url.ToString().ToLowerInvariant();
            var crossBrowserPageBucket = UsageDataByLocation.GetOrAdd(url, location => new SessionResult(extension));
            var connectionSiteBucket = UsageDataByConnectionAndLocation.GetOrAdd(extension.Connection, conn => new ConcurrentDictionary<string, SessionResult>());
            var connectionPageBucket = connectionSiteBucket.GetOrAdd(url, location => new SessionResult(extension));
            var projectBucket = UsageDataByProject.GetOrAdd(extension.Connection.Project.UniqueName, proj => new SessionResult(extension));

            crossBrowserPageBucket.Merge(source);
            connectionPageBucket.Merge(source);
            projectBucket.Merge(source);
            OnUsageDataUpdated();
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
            SessionResult data;

            try
            {
                if (!UsageDataByProject.TryGetValue(project.UniqueName, out data))
                {
                    return new Task[0];
                }
            }
            catch (COMException)
            {
                return new Task[0];
            }

            return data.GetWarnings();
        }

        public static IEnumerable<Task> GetWarnings(Uri uri)
        {
            SessionResult data;
            var url = uri.ToString().ToLowerInvariant();

            if (!UsageDataByLocation.TryGetValue(url, out data))
            {
                return new Task[0];
            }

            return data.GetWarnings(uri);
        }

        public static IEnumerable<IStylingRule> GetAllUnusedRules()
        {
            return UsageDataByProject.Values.SelectMany(x => x.GetUnusedRules()).Distinct().ToList();
        }

        private static void OnUsageDataUpdated()
        {
            if (UsageDataUpdated != null)
            {
                UsageDataUpdated(null, null);
            }
        }

        public static async System.Threading.Tasks.Task ResynchronizeAsync()
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

        public static void Resynchronize()
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
            using (AmbientRuleContext.GetOrCreate())
            {
                return sheetRules.Intersect(UsageDataByProject.Values.SelectMany(x => x.GetUnusedRules()));
            }
        }

        public static bool IsAProtectedClass(IStylingRule rule)
        {
            var selectorName = rule.DisplaySelectorName;
            var cleansedName = RuleRegistry.StandardizeSelector(selectorName);

            return cleansedName.IndexOf(":visited", StringComparison.Ordinal) > -1 || cleansedName.IndexOf(":hover", StringComparison.Ordinal) > -1 || cleansedName.IndexOf(":active", StringComparison.Ordinal) > -1;
        }

        internal static bool IsRuleUsed(RuleSet rule)
        {
            return GetAllUnusedRules().All(x => !x.Matches(rule));
        }
    }
}