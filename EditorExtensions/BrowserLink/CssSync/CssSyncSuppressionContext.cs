using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions
{
    public class CssSyncSuppressionContext : IDisposable
    {
        private static int _suppressionCount;
        private readonly int _msAfterDisposeToWaitToRelease;
        private static readonly ConcurrentDictionary<BrowserLinkConnection, int> ConnectionsToExcludeLookup = new ConcurrentDictionary<BrowserLinkConnection, int>();
        private readonly IEnumerable<BrowserLinkConnection> _connectionsToExclude;
        private readonly bool _previousSuppressionState;

        public static bool SuppressAllBrowsers { get; private set; }
        public static IEnumerable<BrowserLinkConnection> ConnectionsToExclude
        {
            get { return ConnectionsToExcludeLookup.Where(x => x.Value > 0).Select(x => x.Key); }
        }

        private CssSyncSuppressionContext(int msAfterDisposeToWaitToRelease, ICollection<BrowserLinkConnection> connectionsToExclude)
        {
            _previousSuppressionState = SuppressAllBrowsers;

            if (!_previousSuppressionState && connectionsToExclude.Count == 0)
            {
                SuppressAllBrowsers = true;
            }

            _connectionsToExclude = connectionsToExclude;

            foreach (var connection in connectionsToExclude)
            {
                ConnectionsToExcludeLookup.AddOrUpdate(connection, x => 1, (x, c) => c + 1);
            }

            _msAfterDisposeToWaitToRelease = msAfterDisposeToWaitToRelease;
            Interlocked.Increment(ref _suppressionCount);
        }

        public static CssSyncSuppressionContext Get(int msAfterDisposeToWaitToRelease = 1000, params BrowserLinkConnection[] excludeSpecificConnections)
        {
            return new CssSyncSuppressionContext(msAfterDisposeToWaitToRelease, excludeSpecificConnections);
        }

        public static bool IsSuppressed
        {
            get { return Volatile.Read(ref _suppressionCount) != 0; }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThreadPool.QueueUserWorkItem(s =>
                {
                    Thread.Sleep(_msAfterDisposeToWaitToRelease);
                    Interlocked.Decrement(ref _suppressionCount);

                    foreach (var connectionToExclude in _connectionsToExclude)
                    {
                        ConnectionsToExcludeLookup.AddOrUpdate(connectionToExclude, x => 0, (x, c) => c - 1);
                    }

                    SuppressAllBrowsers = _previousSuppressionState;
                });

            }
        }

        ~CssSyncSuppressionContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
