using Microsoft.CSS.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public abstract class DocumentBase : IDocument
    {
        private readonly string _file;
        private FileSystemEventHandler _fileDeletedCallback;
        private readonly FileSystemWatcher _watcher;
        private readonly string _localFileName;
        private static readonly object Sync = new object();

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
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess |NotifyFilters.DirectoryName
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

            var success = false;
            var tryCount = 0;
            const int maxTries = 20;
            
            while (!success && tryCount++ < maxTries)
            {
                try
                {
                    var text = File.ReadAllText(_file);
                    Reparse(text);
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

        protected static IDocument For(string fullPath, FileSystemEventHandler fileDeletedCallback, Func<string, FileSystemEventHandler, DocumentBase> documentFactory)
        {
            var fileName = fullPath.ToLowerInvariant();

            lock (Sync)
            {
                if (fileDeletedCallback != null)
                {
                    return documentFactory(fileName, fileDeletedCallback);
                }

                return null;
            }
        }

        protected virtual IEnumerable<RuleSet> ExpandRuleSets(IEnumerable<RuleSet> ruleSets)
        {
            return ruleSets;
        }

        public void Reparse(string text)
        {
            var parser = GetParser();
            var parseResult = parser.Parse(text, false);
            Rules = ExpandRuleSets(parseResult.RuleSets).Select(x => CssRule.From(_file, text, x, this)).Where(x => x != null).ToList();
        }
 
        protected abstract ICssParser GetParser();

        public virtual string GetSelectorName(RuleSet ruleSet)
        {
            return ruleSet.Text.Substring(0, ruleSet.Block.Start - ruleSet.Start);
        }

        public void Import(StyleSheet styleSheet)
        {
            Rules = ExpandRuleSets(styleSheet.RuleSets).Select(x => CssRule.From(_file, styleSheet.Text, x, this)).Where(x => x != null).ToList();
        }
    }
}
