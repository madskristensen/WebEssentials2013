using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CssDocument : IDisposable
    {
        private readonly string _file;
        private readonly FileSystemEventHandler _fileDeletedCallback;
        private readonly FileSystemWatcher _watcher;

        public CssDocument(string file, FileSystemEventHandler fileDeletedCallback)
        {
            _fileDeletedCallback = fileDeletedCallback;
            _file = file;
            _watcher = new FileSystemWatcher(file);
            _watcher.Changed += Reparse;
            _watcher.Deleted += ProxyDeletion;
            _watcher.Renamed += ProxyRename;
            Reparse(null, null);
        }

        public IEnumerable<CssRule> Rules { get; private set; }

        public void Dispose()
        {
            _watcher.Changed -= Reparse;
            _watcher.Deleted -= ProxyDeletion;
            _watcher.Renamed -= ProxyRename;
            _watcher.Dispose();
        }

        private void ProxyDeletion(object sender, FileSystemEventArgs e)
        {
            _fileDeletedCallback(sender, e);
        }

        private void ProxyRename(object sender, RenamedEventArgs e)
        {
            _fileDeletedCallback(sender, e);
        }

        private void Reparse(object sender, FileSystemEventArgs e)
        {
            var parser = new CssParser();
            var text = File.ReadAllText(_file);
            var parseResult = parser.Parse(text, false);
            Rules = parseResult.RuleSets.Select(x => new CssRule(_file, text, x)).ToList();
        }
    }
}