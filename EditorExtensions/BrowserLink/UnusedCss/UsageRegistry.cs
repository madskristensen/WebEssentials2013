using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Web.BrowserLink;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class UsageRegistry
    {
        private static readonly ConcurrentDictionary<string, CompositeUsageData> UsageDataByLocation = new ConcurrentDictionary<string, CompositeUsageData>();

        private static readonly ConcurrentDictionary<BrowserLinkConnection, ConcurrentDictionary<string, CompositeUsageData>> UsageDataByConnectionAndLocation = new ConcurrentDictionary<BrowserLinkConnection, ConcurrentDictionary<string, CompositeUsageData>>();

        private static readonly ConcurrentDictionary<Project, CompositeUsageData> UsageDataByProject = new ConcurrentDictionary<Project, CompositeUsageData>();

        public static void Merge(BrowserLinkConnection connection, IUsageDataSource source)
        {
            var url = connection.Url.ToString().ToLowerInvariant();
            var crossBrowserPageBucket = UsageDataByLocation.GetOrAdd(url, location => new CompositeUsageData(connection.Project));

            var connectionSiteBucket = UsageDataByConnectionAndLocation.GetOrAdd(connection, conn => new ConcurrentDictionary<string, CompositeUsageData>());
            var connectionPageBucket = connectionSiteBucket.GetOrAdd(url, location => new CompositeUsageData(connection.Project));

            var projectBucket = UsageDataByProject.GetOrAdd(connection.Project, proj => new CompositeUsageData(connection.Project));

            crossBrowserPageBucket.AddUsageSource(source);
            connectionPageBucket.AddUsageSource(source);
            projectBucket.AddUsageSource(source);
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
    }
}
