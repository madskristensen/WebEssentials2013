using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;
using VSConstants = Microsoft.VisualStudio.VSConstants;

namespace MadsKristensen.EditorExtensions.Margin
{
    public class CssTextViewMargin : TextViewMargin
    {
        private MenuItem _goToMenuItem { get; set; }
        private CssCompilerResult _compilerResult { get; set; }

        public CssTextViewMargin(string targetContentType, ITextDocument document, IWpfTextView sourceView)
            : base(targetContentType, document, sourceView)
        {
            // Intercept F12 when it bubbles from the preview pane to the main TextView
            var adapterFactory = WebEditor.ExportProvider.GetExport<IVsEditorAdaptersFactoryService>().Value;
            var adapter = adapterFactory.GetViewAdapter(sourceView);
            if (adapter != null)
                sourceView.Properties.GetOrCreateSingletonProperty(() => new GoToDefinitionFilter(adapter, this));
        }

        protected override void UpdateMargin(CompilerResult result)
        {
            if (result.IsSuccess)
            {
                _compilerResult = result as CssCompilerResult;

                UpdateResults().DoNotWait("updating TextView property");
                SetText(result.Result);
            }
            else
                SetText("/*\r\n\r\nCompilation Error occurred (see error list to navigate to the error location):\r\n"
                      + string.Join("\r\n", result.Errors.Select(e => "Error found" + (e.Line > 0 ? " at line " + e.Line + (e.Column > 0 ? ", column " + e.Column : "") : "") + ":\r\n" + e.FullMessage))
                      + "\r\n\r\n*/");
        }

        private async Task UpdateResults()
        {
            if (_compilerResult == null)
                return;

            bool succeeded = true;

            if (SourceTextView.Properties.ContainsProperty("CssSourceMap"))
                SourceTextView.Properties.RemoveProperty("CssSourceMap");

            try
            {
                SourceTextView.Properties.AddProperty("CssSourceMap", await _compilerResult.SourceMap.ConfigureAwait(false));
            }
            catch (ArgumentException)
            {
                succeeded = false;
            }

            if (!succeeded) // retry to get the latest results
                await UpdateResults();
        }

        // This is necessary to prevent the CSS editor's handler
        // from running & failing.  Status checks do not seem to
        // work properly when the preview pane is focused.
        class GoToDefinitionFilter : CommandTargetBase<VSConstants.VSStd97CmdID>
        {
            CssTextViewMargin owner;
            public GoToDefinitionFilter(IVsTextView adapter, CssTextViewMargin owner)
                : base(adapter, owner.SourceTextView, VSConstants.VSStd97CmdID.GotoDefn)
            {
                this.owner = owner;
            }
            protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                if (!owner.PreviewTextHost.TextView.HasAggregateFocus)
                    return false;
                if (!owner.IsSourceMapAvailable())
                    return false;
                owner.GoToDefinitionCommandHandler();
                return true;
            }

            protected override bool IsEnabled()
            {
                if (!owner.PreviewTextHost.HostControl.IsFocused)
                    return false;
                return owner.IsSourceMapAvailable();
            }
        }

        private bool IsSourceMapAvailable()
        {
            return _compilerResult != null && _compilerResult.SourceMap.IsCompleted && SourceTextView.Properties.ContainsProperty("CssSourceMap");
        }

        protected override void AddSpecialItems(ItemsControl menu)
        {
            if (_goToMenuItem != null && PreviewTextHost.TextView.VisualElement.ContextMenu.Items.Contains(_goToMenuItem))
                PreviewTextHost.TextView.VisualElement.ContextMenu.Items.Remove(_goToMenuItem);

            _goToMenuItem = new MenuItem()
            {
                Header = "Go To Definition",
                InputGestureText = "F12",
                Command = new GoToDefinitionCommand(GoToDefinitionCommandHandler, IsSourceMapAvailable)
            };

            menu.Items.Add(_goToMenuItem);
        }

        private async void GoToDefinitionCommandHandler()
        {
            var buffer = PreviewTextHost.TextView.TextBuffer;
            var position = PreviewTextHost.TextView.Selection.Start.Position;
            var containingLine = position.GetContainingLine();
            int line = containingLine.LineNumber;
            var tree = CssEditorDocument.FromTextBuffer(buffer);
            var item = tree.StyleSheet.ItemBeforePosition(position);

            if (item == null)
                return;

            Selector selector = item.FindType<Selector>();

            if (selector == null)
                return;

            int column = Math.Max(0, selector.SimpleSelectors.Last().Start - containingLine.Start - 1);

            var sourceInfo = (await _compilerResult.SourceMap).MapNodes.FirstOrDefault(s => s.GeneratedLine == line && s.GeneratedColumn == column);

            if (sourceInfo == null)
            {
                if (selector.SimpleSelectors.Last().PreviousSibling == null)
                    return;

                // In case previous selector had > or + sign at the end,
                // LESS compiler does count it as well.
                var point = selector.SimpleSelectors.Last().PreviousSibling.AfterEnd - 1;

                column = Math.Max(0, point - containingLine.Start - 1);
                sourceInfo = (await _compilerResult.SourceMap).MapNodes.FirstOrDefault(s => s.GeneratedLine == line && s.GeneratedColumn == column);

                if (sourceInfo == null)
                    return;
            }

            if (sourceInfo.SourceFilePath != Document.FilePath)
                FileHelpers.OpenFileInPreviewTab(sourceInfo.SourceFilePath);

            string content = await FileHelpers.ReadAllTextRetry(sourceInfo.SourceFilePath);

            var finalPositionInSource = content.NthIndexOfCharInString('\n', sourceInfo.OriginalLine) + sourceInfo.OriginalColumn;

            Dispatch(sourceInfo.SourceFilePath, finalPositionInSource);
        }

        private void Dispatch(string file, int positionInFile)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    IWpfTextView view;
                    view = (file != Document.FilePath) ? ProjectHelpers.GetCurentTextView() : SourceTextView;
                    ITextSnapshot snapshot = view.TextBuffer.CurrentSnapshot;
                    view.Caret.MoveTo(new SnapshotPoint(snapshot, positionInFile));
                    view.ViewScroller.EnsureSpanVisible(new SnapshotSpan(snapshot, positionInFile, 1), EnsureSpanVisibleOptions.AlwaysCenter);
                    ((Control)view).Focus();
                }
                catch
                { }
            }), DispatcherPriority.ApplicationIdle, null);
        }

        private class GoToDefinitionCommand : System.Windows.Input.ICommand
        {
            private readonly Action _goToAction;
            private readonly Func<bool> _canExecuteAction;

            public GoToDefinitionCommand(Action executeAction, Func<bool> canExecuteAction)
            {
                _goToAction = executeAction;
                _canExecuteAction = canExecuteAction;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecuteAction();
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _goToAction();
            }
        }
    }
}
