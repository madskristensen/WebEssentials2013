using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.VisualStudio.Threading;

namespace MadsKristensen.EditorExtensions
{
    public class BundleFileObserver
    {
        private IBundleDocument _document;
        private FileSystemWatcher _watcher;
        private string[] _extensions = new[] { ".bundle", ".sprite" };
        private readonly AsyncReaderWriterLock rwLock = new AsyncReaderWriterLock();
        private static Dictionary<string, HashSet<Tuple<string, FileSystemWatcher>>> _watchedFiles = new Dictionary<string, HashSet<Tuple<string, FileSystemWatcher>>>();

        public async Task AttachFileObserver(IBundleDocument document, string fileName, Func<string, bool, Task> updateBundle)
        {
            _document = document;
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
                if (_watchedFiles.ContainsKey(_document.FileName) && _watchedFiles[_document.FileName].Any(s => s.Item1.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    return;
            }

            _watcher.Changed += new FileSystemEventHandler((_, __) => Changed(fileName, updateBundle));
            _watcher.Deleted += new FileSystemEventHandler((_, __) => Deleted(fileName));

            _watcher.EnableRaisingEvents = true;

            using (await rwLock.WriteLockAsync())
            {
                if (!_watchedFiles.ContainsKey(_document.FileName))
                    _watchedFiles.Add(_document.FileName, new HashSet<Tuple<string, FileSystemWatcher>>());

                _watchedFiles[_document.FileName].Add(new Tuple<string, FileSystemWatcher>(fileName, _watcher));
            }
        }

        private async void Changed(string fileName, Func<string, bool, Task> updateBundle)
        {
            _document = await BundleDocument.FromFile(_document.FileName);
            _watcher.EnableRaisingEvents = false;

            await updateBundle(_document.FileName, false);

            IEnumerable<Tuple<string, FileSystemWatcher>> tuples;

            using (await rwLock.ReadLockAsync())
            {
                tuples = _watchedFiles[_document.FileName].Where(x => !_extensions.Any(e => x.Item1.EndsWith(e)) && !_document.BundleAssets.Contains(x.Item1, StringComparer.OrdinalIgnoreCase));
            }

            using (await rwLock.WriteLockAsync())
            {
                StopMonitoring(tuples);

                _watchedFiles[_document.FileName].RemoveWhere(x => tuples.Contains(x));
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

                bool isConstituent = !_extensions.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase));
                IEnumerable<Tuple<string, FileSystemWatcher>> tuples = null;

                using (await rwLock.ReadLockAsync())
                {
                    tuples = !isConstituent ?
                             _watchedFiles[_document.FileName] :
                             _watchedFiles.SelectMany(p => p.Value.Where(v => v.Item1.Equals(fileName, StringComparison.OrdinalIgnoreCase)));
                }

                using (await rwLock.WriteLockAsync())
                {
                    StopMonitoring(tuples);

                    if (isConstituent)
                    {
                        _watchedFiles[_document.FileName].RemoveWhere(x => tuples.Contains(x));

                        return;
                    }

                    _watchedFiles.Remove(_document.FileName);
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
    }
}
