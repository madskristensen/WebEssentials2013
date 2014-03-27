using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions.Margin
{
    public class CssTextViewMargin : TextViewMargin
    {
        private CssCompilerResult _compilerResult { get; set; }

        public CssTextViewMargin(string targetContentType, ITextDocument document, IWpfTextView sourceView)
            : base(targetContentType, document, sourceView)
        { }

        protected override void UpdateMargin(CompilerResult result)
        {
            if (result.IsSuccess)
            {
                _compilerResult = result as CssCompilerResult;

                if (SourceTextView.Properties.ContainsProperty("CssCompilerResult"))
                    SourceTextView.Properties.RemoveProperty("CssCompilerResult");

                SourceTextView.Properties.AddProperty("CssCompilerResult", _compilerResult);
                SetText(result.Result);
            }
            else
                SetText("/*\r\n\r\nCompilation Error. \r\nSee error list for details\r\n"
                      + string.Join("\r\n", result.Errors.Select(e => e.Message))
                      + "\r\n\r\n*/");
        }

        protected override void AddSpecialItems(ItemsControl menu)
        {
            menu.Items.Add(new MenuItem()
            {
                Header = "Go To Definition",
                InputGestureText = "F12",
                Command = new GoToDefinitionCommand(GoToDefinitionCommandHandler, () =>
                { return _compilerResult != null && _compilerResult.SourceMap != null; })
            });
        }

        private void GoToDefinitionCommandHandler()
        {
            var buffer = PreviewTextHost.TextView.TextBuffer;
            var position = PreviewTextHost.TextView.Selection.Start.Position;
            var containingLine = position.GetContainingLine();
            int line = containingLine.LineNumber;
            var tree = CssEditorDocument.FromTextBuffer(buffer);
            Selector selector = tree.StyleSheet.ItemBeforePosition(position).FindType<Selector>();

            if (selector == null)
                return;

            int start = selector.Start;
            int column = start - containingLine.Start;

            var sourceInfo = _compilerResult.SourceMap.MapNodes.FirstOrDefault(s => s.GeneratedLine == line && s.GeneratedColumn == column);

            if (sourceInfo == null)
                return;

            if (sourceInfo.SourceFilePath != Document.FilePath)
                FileHelpers.OpenFileInPreviewTab(sourceInfo.SourceFilePath);

            string content = File.ReadAllText(sourceInfo.SourceFilePath);

            var finalPositionInSource = content.NthIndexOfCharInString('\n', (int)sourceInfo.OriginalLine) + sourceInfo.OriginalColumn;

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
                }
                catch
                { }
            }), DispatcherPriority.ApplicationIdle, null);
        }

        private class GoToDefinitionCommand : ICommand
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
