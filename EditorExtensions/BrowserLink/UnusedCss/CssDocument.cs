using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSS.Core;
using System.Threading;
using System.Collections.Concurrent;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CssDocument : IDocument
    {
        private readonly string _file;
        private FileSystemEventHandler _fileDeletedCallback;
        private readonly FileSystemWatcher _watcher;
        private readonly string _localFileName;
        private static readonly Dictionary<string, CssDocument> FileLookup = new Dictionary<string, CssDocument>();
        private static readonly object _sync = new object();

        private CssDocument(string file, FileSystemEventHandler fileDeletedCallback)
        {
            _fileDeletedCallback = fileDeletedCallback;
            _file = file;
            var path = Path.GetDirectoryName(file);
            _localFileName = Path.GetFileName(file).ToLowerInvariant();

            _watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = _localFileName //"*.css"
            };
            
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.DirectoryName;
            _watcher.Changed += Reparse;
            _watcher.Deleted += ProxyDeletion;
            _watcher.Renamed += ProxyRename;
            _watcher.Created += Reparse;
            _watcher.EnableRaisingEvents = true;
            Reparse();
        }

        public IEnumerable<IStylingRule> Rules { get; private set; }

        public void Dispose()
        {
            _watcher.Changed -= Reparse;
            _watcher.Deleted -= ProxyDeletion;
            _watcher.Renamed -= ProxyRename;
            _watcher.Dispose();
        }

        private void ProxyDeletion(object sender, FileSystemEventArgs e)
        {
            if (e.Name.ToLowerInvariant() != _localFileName)
            {
                return;
            }

            _fileDeletedCallback(sender, e);
        }

        private void ProxyRename(object sender, RenamedEventArgs e)
        {
            if (e.Name.ToLowerInvariant() == _localFileName)
            {
                Reparse();
            }
            else if(e.OldName.ToLowerInvariant() == _localFileName)
            {
                _fileDeletedCallback(sender, e);
            }
        }

        private async void Reparse(object sender, FileSystemEventArgs e)
        {
            if (e != null && e.Name.ToLowerInvariant() != _localFileName)
            {
                return;
            }

            var parser = new CssParser();
            var success = false;
            var tryCount = 0;
            const int maxTries = 20;
            
            while (!success && tryCount++ < maxTries)
            {
                try
                {
                    var text = File.ReadAllText(_file);
                    var parseResult = parser.Parse(text, false);
                    Rules = parseResult.RuleSets.Select(x => new CssRule(_file, text, x)).ToList();
                    success = true;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }

            await UsageRegistry.ResyncAsync();
            UnusedCssExtension.All(x => x.SnapshotPage());
        }

        public void Reparse()
        {
            Reparse(null, null);
        }

        internal static CssDocument For(string fullPath, FileSystemEventHandler fileDeletedCallback = null)
        {
            var fileName = fullPath.ToLowerInvariant();
            CssDocument existing;

            lock (_sync)
            {
                if (FileLookup.TryGetValue(fileName, out existing))
                {
                    if (fileDeletedCallback != null)
                    {
                        existing._fileDeletedCallback += fileDeletedCallback;
                    }

                    return existing;
                }

                if (fileDeletedCallback != null)
                {
                    return FileLookup[fileName] = new CssDocument(fileName, fileDeletedCallback);
                }

                return null;
            }
        }

        public void Reparse(string text)
        {
            var parser = new CssParser();
            var success = false;

            var parseResult = parser.Parse(text, false);
            Rules = parseResult.RuleSets.Select(x => new CssRule(_file, text, x)).ToList();
            success = true;
        }
    }
}