using System;
using System.IO;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class CssGoToLineTag : INavigateToItemDisplay
    {
        private ParseItem _selector;
        private string _file;

        public CssGoToLineTag(ParseItem selector, string file)
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
            WebEssentialsPackage.DTE.ItemOperations.OpenFile(_file);

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
