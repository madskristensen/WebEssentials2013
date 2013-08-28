using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSS.Core;
using System.Threading;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CssDocument : IDisposable
    {
        private readonly string _file;
        private readonly FileSystemEventHandler _fileDeletedCallback;
        private readonly FileSystemWatcher _watcher;
        private readonly string _localFileName;

        public CssDocument(string file, FileSystemEventHandler fileDeletedCallback)
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
                Reparse(null, null);
            }
            else if(e.OldName.ToLowerInvariant() == _localFileName)
            {
                _fileDeletedCallback(sender, e);
            }
        }

        private void Reparse(object sender, FileSystemEventArgs e)
        {
            if (e != null && e.Name.ToLowerInvariant() != _localFileName)
            {
                return;
            }

            var parser = new CssParser();
            var success = false;
            
            while (!success)
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

            MessageDisplayManager.Refresh();
        }
    }
}