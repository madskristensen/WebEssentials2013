using Microsoft.CSS.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public abstract class DocumentBase : IDocument
    {
        private readonly string _file;
        private readonly FileSystemEventHandler _fileDeletedCallback;
        private readonly FileSystemWatcher _watcher;
        private readonly string _localFileName;

        protected DocumentBase(string file, FileSystemEventHandler fileDeletedCallback)
        {
            _fileDeletedCallback = fileDeletedCallback;
            _file = file;
            var path = Path.GetDirectoryName(file);
            _localFileName = (Path.GetFileName(file) ?? "").ToLowerInvariant();

            _watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = _localFileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.DirectoryName
            };

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
            else if (e.OldName.ToLowerInvariant() == _localFileName)
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

            var tryCount = 0;
            const int maxTries = 20;

            while (tryCount++ < maxTries)
            {
                try
                {
                    var text = File.ReadAllText(_file);
                    Reparse(text);
                    break;
                }
                catch (IOException)
                {
                }
                await Task.Delay(100);
            }

            await UsageRegistry.ResyncAsync();
            UnusedCssExtension.All(x => x.SnapshotPage(null));
        }

        public void Reparse()
        {
            Reparse(null, null);
        }

        protected static IDocument For(string fullPath, FileSystemEventHandler fileDeletedCallback, Func<string, FileSystemEventHandler, DocumentBase> documentFactory)
        {
            var fileName = fullPath.ToLowerInvariant();

            if (fileDeletedCallback != null)
            {
                return documentFactory(fileName, fileDeletedCallback);
            }

            return null;
        }

        public void Reparse(string text)
        {
            var parser = GetParser();
            var parseResult = parser.Parse(text, false);
            Rules = new CssItemAggregator<IStylingRule>(true) { (RuleSet rs) => CssRule.From(_file, text, rs, this) }
                        .Crawl(parseResult)
                        .Where(x => x != null)
                        .ToList();
        }

        protected abstract ICssParser GetParser();

        public virtual string GetSelectorName(RuleSet ruleSet)
        {
            return ExtractSelectorName(ruleSet);
        }

        internal static string ExtractSelectorName(RuleSet ruleSet)
        {
            return ruleSet.Text.Substring(0, ruleSet.Block.Start - ruleSet.Start);
        }
    }
}
