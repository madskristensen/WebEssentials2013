using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.VisualStudio.Threading;

namespace MadsKristensen.EditorExtensions
{
    public class BundleFileObserver : IDisposable
    {
        private string _bundleFileName;
        private IBundleDocument _document;
        private FileSystemWatcher _watcher;
        private string[] _extensions = new[] { ".bundle", ".sprite" };
        private readonly AsyncReaderWriterLock rwLock = new AsyncReaderWriterLock();
        private static Dictionary<string, HashSet<Tuple<string, FileSystemWatcher>>> _watchedFiles = new Dictionary<string, HashSet<Tuple<string, FileSystemWatcher>>>();

        public void WatchFutureFiles(string path, string extension, Func<string, Task> callbackTask)
        {
            _watcher = new FileSystemWatcher();
            _watcher.Path = Path.GetDirectoryName(path);
            _watcher.Filter = extension;
            _watcher.IncludeSubdirectories = true;

            _watcher.Created += async (_, __) => await callbackTask(__.FullPath);

            _watcher.EnableRaisingEvents = true;
        }

        public async Task AttachFileObserver(IBundleDocument document, string fileName, Func<string, bool, Task> updateBundle)
        {
            _document = document;
            _bundleFileName = document.FileName;
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
                if (_watchedFiles.ContainsKey(_bundleFileName) && _watchedFiles[_bundleFileName].Any(s => s.Item1.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    return;
            }

            _watcher.Changed += new FileSystemEventHandler((_, __) => Changed(updateBundle));
            _watcher.Deleted += new FileSystemEventHandler((_, __) => Deleted(fileName));

            if (_extensions.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                _watcher.Renamed += new RenamedEventHandler((_, renamedEventArgument) => Renamed(renamedEventArgument, updateBundle));

            _watcher.EnableRaisingEvents = true;

            using (await rwLock.WriteLockAsync())
            {
                if (!_watchedFiles.ContainsKey(_bundleFileName))
                    _watchedFiles.Add(_bundleFileName, new HashSet<Tuple<string, FileSystemWatcher>>());

                _watchedFiles[_bundleFileName].Add(new Tuple<string, FileSystemWatcher>(fileName, _watcher));
            }
        }

        private async void Renamed(RenamedEventArgs renamedEventArgument, Func<string, bool, Task> updateBundle)
        {
            using (await rwLock.ReadLockAsync())
            {
                if (!_watchedFiles.ContainsKey(renamedEventArgument.OldFullPath) &&
                    !renamedEventArgument.FullPath.StartsWith(ProjectHelpers.GetSolutionFolderPath(), StringComparison.OrdinalIgnoreCase))
                    return;
            }

            HashSet<Tuple<string, FileSystemWatcher>> oldValue;

            using (await rwLock.ReadLockAsync())
            {
                oldValue = _watchedFiles[renamedEventArgument.OldFullPath];
            }

            using (await rwLock.WriteLockAsync())
            {
                _watchedFiles.Remove(renamedEventArgument.OldFullPath);
            }

            _document = await _document.LoadFromFile(renamedEventArgument.FullPath);

            foreach (Tuple<string, FileSystemWatcher> tuple in oldValue)
            {
                tuple.Item2.EnableRaisingEvents = false;

                tuple.Item2.Dispose();

                if (_extensions.Any(e => tuple.Item1.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                    await AttachFileObserver(_document, _document.FileName, updateBundle);
                else
                    await AttachFileObserver(_document, tuple.Item1, updateBundle);
            }
        }

        private async void Changed(Func<string, bool, Task> updateBundle)
        {
            _watcher.EnableRaisingEvents = false;

            _document = await _document.LoadFromFile(_bundleFileName);

            await updateBundle(_bundleFileName, false);

            IEnumerable<Tuple<string, FileSystemWatcher>> tuples;

            using (await rwLock.ReadLockAsync())
            {
                tuples = _watchedFiles[_bundleFileName].Where(x => !_extensions.Any(e => x.Item1.EndsWith(e)) && !_document.BundleAssets.Contains(x.Item1, StringComparer.OrdinalIgnoreCase));
            }

            using (await rwLock.WriteLockAsync())
            {
                StopMonitoring(tuples);

                _watchedFiles[_bundleFileName].RemoveWhere(x => tuples.Contains(x));
            }

            _watcher.EnableRaisingEvents = true;
        }

        private void Deleted(string fileName)
        {
            // Passive mode deletion: We have to wait if a
            // process is rewriting the file (rewrite = delete + recreate)
            Timer timer = new Timer(2 * 1000);

            timer.Elapsed += async (_, __) =>
            {
                timer.Enabled = false;

                // We have to confirm if the file is really deleted,
                // because FileSystemWatcher raises Deleted event anyway.
                if (File.Exists(fileName))
                    return;

                using (await rwLock.ReadLockAsync())
                {
                    if (!_watchedFiles.ContainsKey(fileName))
                        return;
                }

                bool isConstituent = !_extensions.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase));
                IEnumerable<Tuple<string, FileSystemWatcher>> tuples = null;

                using (await rwLock.ReadLockAsync())
                {
                    tuples = !isConstituent ?
                             _watchedFiles[_bundleFileName] :
                             _watchedFiles.SelectMany(p => p.Value.Where(v => v.Item1.Equals(fileName, StringComparison.OrdinalIgnoreCase)));
                }

                using (await rwLock.WriteLockAsync())
                {
                    StopMonitoring(tuples);

                    if (isConstituent)
                    {
                        _watchedFiles[_bundleFileName].RemoveWhere(x => tuples.Contains(x));

                        return;
                    }

                    _watchedFiles.Remove(_bundleFileName);
                }

                _watcher.EnableRaisingEvents = false;

                _watcher.Dispose();
            };

            timer.Enabled = true;
        }

        private static void StopMonitoring(IEnumerable<Tuple<string, FileSystemWatcher>> tuples)
        {
            foreach (Tuple<string, FileSystemWatcher> tuple in tuples)
            {
                tuple.Item2.EnableRaisingEvents = false;

                tuple.Item2.Dispose();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _watcher.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
