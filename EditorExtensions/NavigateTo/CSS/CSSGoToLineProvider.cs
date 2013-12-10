using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.PlatformUI;

namespace MadsKristensen.EditorExtensions
{
    internal sealed class CssGoToLineProvider : DisposableObject, INavigateToItemProvider
    {
        private readonly CssGoToLineProviderFactory _owner;

        public CssGoToLineProvider(CssGoToLineProviderFactory owner)
        {
            _owner = owner;
        }

        public void StartSearch(INavigateToCallback callback, string searchValue)
        {
            if (searchValue.Length > 0 && (searchValue[0] == '.' || searchValue[0] == '#'))
            {
                CssParser parser = new CssParser();
                var state = new Tuple<CssParser, string, INavigateToCallback>(parser, searchValue, callback);

                System.Threading.ThreadPool.QueueUserWorkItem(DoWork, state);
            }
        }

        public void DoWork(object state)
        {
            var tuple = (Tuple<CssParser, string, INavigateToCallback>)state;
            var parser = tuple.Item1;
            var searchValue = tuple.Item2;
            var callback = tuple.Item3;

            try
            {
                IList<string> files = GetFiles().ToList();

                Parallel.For(0, files.Count, i =>
                {
                    string file = files[i];

                    IEnumerable<ParseItem> items = GetItems(file, parser, searchValue);

                    foreach (ParseItem sel in items)
                    {
                        callback.AddItem(new NavigateToItem(searchValue, NavigateToItemKind.Field, "CSS", searchValue, new CssGoToLineTag(sel, file), MatchKind.Exact, _owner));
                    }

                    callback.ReportProgress(i, files.Count);
                });
            }
            catch { }
            finally
            {
                callback.Done();
            }
        }

        public void StopSearch()
        {
        }

        private static IEnumerable<ParseItem> GetItems(string file, CssParser parser, string searchValue)
        {
            StyleSheet ss = parser.Parse(File.ReadAllText(file), true);

            return new CssItemAggregator<ParseItem>
            {
                (ClassSelector c) => c.Text.Contains(searchValue) ? c : null,
                (IdSelector c) => c.Text.Contains(searchValue) ? c : null
            }.Crawl(ss).Where(s => s != null);
        }

        private static IEnumerable<string> GetFiles()
        {
            string folder = ProjectHelpers.GetRootFolder();

            if (string.IsNullOrEmpty(folder))
            {
                var doc = EditorExtensionsPackage.DTE.ActiveDocument;
                if (doc != null)
                    folder = ProjectHelpers.GetProjectFolder(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
            }

            if (string.IsNullOrEmpty(folder))
                yield break;

            List<string> files = new List<string>();

            files.AddRange(Directory.GetFiles(folder, "*.less", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(folder, "*.css", SearchOption.AllDirectories));

            foreach (string file in files)
            {
                if (!file.Contains(".min.") && !file.Contains(".bundle."))
                    yield return file;
            }
        }
    }
}