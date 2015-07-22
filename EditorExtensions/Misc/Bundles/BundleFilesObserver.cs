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
        private FileSystemEventHandler _changeEvent;
        private readonly string[] _extensions = { ".bundle", ".sprite" };
        private readonly AsyncReaderWriterLock _rwLock = new AsyncReaderWriterLock();
        private readonly static Dictionary<string, HashSet<Tuple<string, FileSystemWatcher>>> WatchedFiles = new Dictionary<string, HashSet<Tuple<string, FileSystemWatcher>>>();

        public void WatchFutureFiles(string path, string extension, Func<string, Task> callbackTask)
        {
            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(path),
                Filter = extension,
                IncludeSubdirectories = true
            };

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

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(fileName),
                Filter = Path.GetFileName(fileName),
                //_watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size;
                NotifyFilter = NotifyFilters.Attributes |
                               NotifyFilters.CreationTime |
                               NotifyFilters.FileName |
                               NotifyFilters.LastAccess |
                               NotifyFilters.LastWrite |
                               NotifyFilters.Size
            };

            using (await _rwLock.ReadLockAsync())
            {
                if (WatchedFiles.ContainsKey(_bundleFileName) && WatchedFiles[_bundleFileName].Any(s => s.Item1.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    return;
            }

            _changeEvent = (_, __) => Changed(updateBundle);

            _watcher.Changed += _changeEvent;
            _watcher.Deleted += (_, __) => Deleted(fileName);

            if (_extensions.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                _watcher.Renamed += (_, renamedEventArgument) => Renamed(renamedEventArgument, updateBundle);

            _watcher.EnableRaisingEvents = true;

            using (await _rwLock.WriteLockAsync())
            {
                if (!WatchedFiles.ContainsKey(_bundleFileName))
                    WatchedFiles.Add(_bundleFileName, new HashSet<Tuple<string, FileSystemWatcher>>());

                WatchedFiles[_bundleFileName].Add(new Tuple<string, FileSystemWatcher>(fileName, _watcher));
            }
        }

        private void Renamed(RenamedEventArgs renamedEventArgument, Func<string, bool, Task> updateBundle)
        {
            Task.Run(async () =>
            {
                using (await _rwLock.ReadLockAsync())
                {
                    if (!WatchedFiles.ContainsKey(renamedEventArgument.OldFullPath) ||
                        !renamedEventArgument.FullPath.StartsWith(ProjectHelpers.GetSolutionFolderPath(), StringComparison.OrdinalIgnoreCase))
                        return;
                }

                HashSet<Tuple<string, FileSystemWatcher>> oldValue;

                using (await _rwLock.ReadLockAsync())
                {
                    oldValue = WatchedFiles[renamedEventArgument.OldFullPath];
                }

                using (await _rwLock.WriteLockAsync())
                {
                    WatchedFiles.Remove(renamedEventArgument.OldFullPath);
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
            }).Wait();
        }

        private void Changed(Func<string, bool, Task> updateBundle)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= _changeEvent;

            Task.Run(async () =>
            {
                _document = await _document.LoadFromFile(_bundleFileName);

                if (_document == null)
                    return;

                await updateBundle(_bundleFileName, false);

                IEnumerable<Tuple<string, FileSystemWatcher>> tuples;

                using (await _rwLock.ReadLockAsync())
                {
                    tuples = WatchedFiles[_bundleFileName].Where(x => !_extensions.Any(e => x.Item1.EndsWith(e)) && !_document.BundleAssets.Contains(x.Item1, StringComparer.OrdinalIgnoreCase));
                }

                using (await _rwLock.WriteLockAsync())
                {
                    IList<Tuple<string, FileSystemWatcher>> enumerable = tuples as IList<Tuple<string, FileSystemWatcher>> ?? tuples.ToList();
                    StopMonitoring(enumerable);

                    WatchedFiles[_bundleFileName].RemoveWhere(x => enumerable.Contains(x));
                }
            }).Wait();

            try
            {
                _watcher.EnableRaisingEvents = true;
                _watcher.Changed += _changeEvent;
            }
            catch (FileNotFoundException)
            {
                //Well, if the file doesn't exists anymore, there is no use for this observer.
                Dispose();
            }
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

                using (await _rwLock.ReadLockAsync())
                {
                    if (!WatchedFiles.ContainsKey(fileName))
                        return;
                }

                bool isConstituent = !_extensions.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase));
                IEnumerable<Tuple<string, FileSystemWatcher>> tuples = null;

                using (await _rwLock.ReadLockAsync())
                {
                    tuples = !isConstituent ?
                             WatchedFiles[_bundleFileName] :
                             WatchedFiles.SelectMany(p => p.Value.Where(v => v.Item1.Equals(fileName, StringComparison.OrdinalIgnoreCase)));
                }

                using (await _rwLock.WriteLockAsync())
                {
                    IEnumerable<Tuple<string, FileSystemWatcher>> enumerable = tuples as IList<Tuple<string, FileSystemWatcher>> ?? tuples.ToList();
                    StopMonitoring(enumerable);

                    if (isConstituent)
                    {
                        WatchedFiles[_bundleFileName].RemoveWhere(x => enumerable.Contains(x));

                        return;
                    }

                    WatchedFiles.Remove(_bundleFileName);
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
