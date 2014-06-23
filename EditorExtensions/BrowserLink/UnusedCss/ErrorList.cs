using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class ErrorList
    {
        private static readonly ErrorListProvider ErrorListProvider = InitializeErrorListProvider();
        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private static ErrorListProvider InitializeErrorListProvider()
        {
            var errorListProvider = new ErrorListProvider(WebEssentialsPackage.Instance);

            try
            {
                errorListProvider.ProviderName = "Unused CSS Browser Link Extension";
                errorListProvider.ProviderGuid = new Guid("5BA8BB0D-D518-45ae-966C-864C536454F2");
            }
            catch
            {
                errorListProvider.Dispose();
            }

            return errorListProvider;
        }

        public static IDisposable UpdateSuspensionContext
        {
            get { return new ErrorListUpdateSuspensionContext(); }
        }

        public static void AddItem(Task task)
        {
            ErrorListProvider.Tasks.Add(task);
        }

        public static void RemoveItem(Task task)
        {
            ErrorListProvider.Tasks.Remove(task);
        }

        private class ErrorListUpdateSuspensionContext : IDisposable
        {
            private static readonly object Sync = new object();
            private static int _activeSuspensionRequestCount;
            public ErrorListUpdateSuspensionContext()
            {
                Lock.EnterWriteLock();

                lock (Sync)
                {
                    if (Interlocked.Increment(ref _activeSuspensionRequestCount) < 0)
                    {
                        Interlocked.Exchange(ref _activeSuspensionRequestCount, 1);
                    }
                }

                ErrorListProvider.SuspendRefresh();
            }

            public void Dispose()
            {
                Lock.ExitWriteLock();

                lock (Sync)
                {
                    if (Interlocked.Decrement(ref _activeSuspensionRequestCount) > 0)
                    {
                        return;
                    }
                }

                ErrorListProvider.ResumeRefresh();
            }
        }
    }
}
