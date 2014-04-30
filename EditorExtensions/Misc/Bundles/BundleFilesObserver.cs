using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace MadsKristensen.EditorExtensions
{
    public class BundleFileObserver
    {
        private FileSystemWatcher _watcher;
        private readonly AsyncReaderWriterLock rwLock = new AsyncReaderWriterLock();
        private static Dictionary<string, HashSet<string>> _watchedFiles = new Dictionary<string, HashSet<string>>();

        public async Task AttachFileObserver(string fileName, string bundleFile, Func<string, bool, Task> updateBundle)
        {
            fileName = Path.GetFullPath(fileName);

            if (!File.Exists(fileName))
                return;

            _watcher = new FileSystemWatcher();
            _watcher.Path = Path.GetDirectoryName(fileName);
            _watcher.Filter = Path.GetFileName(fileName);
            //_watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size;
            _watcher.NotifyFilter = NotifyFilters.Attributes |
                                    NotifyFilters.CreationTime |
                                    NotifyFilters.FileName |
                                    NotifyFilters.LastAccess |
                                    NotifyFilters.LastWrite |
                                    NotifyFilters.Size;

            using (await rwLock.ReadLockAsync())
            {
                if (_watchedFiles.ContainsKey(bundleFile) && _watchedFiles[bundleFile].Contains(fileName))
                    return;
            }

            using (await rwLock.WriteLockAsync())
            {
                if (!_watchedFiles.ContainsKey(bundleFile))
                    _watchedFiles.Add(bundleFile, new HashSet<string>());

                _watchedFiles[bundleFile].Add(fileName);
            }

            _watcher.Changed += async (s, e) => await updateBundle(bundleFile, false);

            _watcher.EnableRaisingEvents = true;
        }
    }
}
