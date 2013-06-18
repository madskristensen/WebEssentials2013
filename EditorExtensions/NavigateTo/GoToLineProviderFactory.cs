using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(INavigateToItemProviderFactory))]
    internal sealed class GoToLineProviderFactory : INavigateToItemProviderFactory, INavigateToItemDisplayFactory
    {
        [Import]
        internal IEditorOperationsFactoryService EditorOperationsFactoryService = null;

        [Import]
        internal IOutliningManagerService OutliningManagerService = null;

        public bool TryCreateNavigateToItemProvider(IServiceProvider serviceProvider, out INavigateToItemProvider provider)
        {
            provider = new GoToLineProvider(this);
            return true;
        }

        public INavigateToItemDisplay CreateItemDisplay(NavigateToItem item)
        {
            return item.Tag as INavigateToItemDisplay;
        }
    }

    internal sealed class GoToLineProvider : DisposableObject, INavigateToItemProvider
    {
        private readonly GoToLineProviderFactory _owner;

        public GoToLineProvider(GoToLineProviderFactory owner)
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
                IEnumerable<string> files = GetFiles();

                Parallel.For(0, files.Count(), i =>
                {
                    string file = files.ElementAt(i);

                    IEnumerable<ParseItem> items = GetItems(file, parser, searchValue);

                    foreach (ParseItem sel in items)
                    {
                        callback.AddItem(new NavigateToItem(searchValue, NavigateToItemKind.Field, null, searchValue, new GoToLineTag(sel, file), MatchKind.Exact, _owner));
                    }

                    callback.ReportProgress(i, files.Count());
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

        private IEnumerable<ParseItem> GetItems(string file, CssParser parser, string searchValue)
        {
            StyleSheet ss = parser.Parse(File.ReadAllText(file), true);

            var visitorClass = new CssItemCollector<ClassSelector>(true);
            ss.Accept(visitorClass);

            var classes = from c in visitorClass.Items
                          where c.Text.Contains(searchValue)
                          select c;

            var visitorIDs = new CssItemCollector<IdSelector>(true);
            ss.Accept(visitorIDs);

            var ids = from c in visitorIDs.Items
                      where c.Text.Contains(searchValue)
                      select c;

            List<ParseItem> list = new List<ParseItem>();
            list.AddRange(classes);
            list.AddRange(ids);

            return list;
        }

        private IEnumerable<string> GetFiles()
        {
            string[] files = Directory.GetFiles(ProjectHelpers.GetRootFolder(), "*.css", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                if (!file.Contains(".min.") && !file.Contains(".bundle."))
                    yield return file;
            }
        }
    }

    internal class GoToLineTag : INavigateToItemDisplay
    {
        private ParseItem _selector;
        private string _file;

        public GoToLineTag(ParseItem selector, string file)
        {
            _selector = selector;
            _file = file;
        }

        public string AdditionalInformation
        {
            get
            {
                return "CSS selector - " + Path.GetFileName(_file);
            }
        }

        public string Description
        {
            get
            {
                return _selector.Text;
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<DescriptionItem> DescriptionItems
        {
            get { return null; }
        }

        public System.Drawing.Icon Glyph
        {
            get { return null; }
        }

        public string Name
        {
            get { return _selector.FindType<Selector>().Text; }
        }

        public void NavigateTo()
        {
            EditorExtensionsPackage.DTE.ItemOperations.OpenFile(_file);

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                var view = ProjectHelpers.GetCurentTextView();
                var textBuffer = ProjectHelpers.GetCurentTextBuffer();
                var span = new SnapshotSpan(textBuffer.CurrentSnapshot, _selector.Start, _selector.Length);
                var point = new SnapshotPoint(textBuffer.CurrentSnapshot, _selector.Start + _selector.Length);

                view.ViewScroller.EnsureSpanVisible(span);
                view.Caret.MoveTo(point);
                view.Selection.Select(span, false);


            }), DispatcherPriority.ApplicationIdle, null);
        }
    }
}
