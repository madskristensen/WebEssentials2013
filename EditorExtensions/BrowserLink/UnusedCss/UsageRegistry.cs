using EnvDTE;
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

        private static readonly ConcurrentDictionary<Project, CompositeUsageData> UsageDataByProject = new ConcurrentDictionary<Project, CompositeUsageData>();

        public static void Merge(UnusedCssExtension extension, IUsageDataSource source)
        {
            var url = extension.Connection.Url.ToString().ToLowerInvariant();
            var crossBrowserPageBucket = UsageDataByLocation.GetOrAdd(url, location => new CompositeUsageData(extension));

            var connectionSiteBucket = UsageDataByConnectionAndLocation.GetOrAdd(extension.Connection, conn => new ConcurrentDictionary<string, CompositeUsageData>());
            var connectionPageBucket = connectionSiteBucket.GetOrAdd(url, location => new CompositeUsageData(extension));

            var projectBucket = UsageDataByProject.GetOrAdd(extension.Connection.Project, proj => new CompositeUsageData(extension));

            crossBrowserPageBucket.AddUsageSource(source);
            connectionPageBucket.AddUsageSource(source);
            projectBucket.AddUsageSource(source);

            OnUsageDataUpdated();
        }

        public static IEnumerable<Task> GetWarnings(Project project)
        {
            CompositeUsageData data;

            if (!UsageDataByProject.TryGetValue(project, out data))
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

        public static IEnumerable<CssRule> GetAllUnusedRules()
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
    }
}
