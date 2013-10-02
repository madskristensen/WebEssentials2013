using System;
using System.Threading;

namespace MadsKristensen.EditorExtensions
{
    public class CssSyncSuppressionContext : IDisposable
    {
        private static int _suppressionCount;
        private readonly int _msAfterDisposeToWaitToRelease;

        private CssSyncSuppressionContext(int msAfterDisposeToWaitToRelease)
        {
            _msAfterDisposeToWaitToRelease = msAfterDisposeToWaitToRelease;
            Interlocked.Increment(ref _suppressionCount);
        }

        public static CssSyncSuppressionContext Get(int msAfterDisposeToWaitToRelease = 1000)
        {
            return new CssSyncSuppressionContext(msAfterDisposeToWaitToRelease);
        }

        public static bool IsSuppressed
        {
            get { return Volatile.Read(ref _suppressionCount) != 0; }
        }

        public void Dispose()
        {
            ThreadPool.QueueUserWorkItem(s =>
            {
                Thread.Sleep(_msAfterDisposeToWaitToRelease);
                Interlocked.Decrement(ref _suppressionCount);
            });
        }
    }
}
