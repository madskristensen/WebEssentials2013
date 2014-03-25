using System;
using System.IO;
using System.Threading.Tasks;
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
        public CssTextViewMargin(string targetContentType, ITextDocument document, IWpfTextView sourceView)
            : base(targetContentType, document, sourceView)
        { }

        protected override void AddSpecialItems(ItemsControl menu)
        {
            menu.Items.Add(new MenuItem()
            {
                Header = "Go To Definition",
                InputGestureText = "F12",
                Command = new GoToDefinitionCommand(GoToDefinitionCommandHandler, () =>
                              { return CssSourceMap.ExistsAsync(TargetFileName); })
            });
        }

        private async void GoToDefinitionCommandHandler()
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

            var sourceInfo = await CssSourceMap.GetSourcePositionAsync(TargetFileName, line, column);

            if (sourceInfo == null)
                return;

            if (sourceInfo.file != Document.FilePath)
                FileHelpers.OpenFileInPreviewTab(sourceInfo.file);

            string content = File.ReadAllText(sourceInfo.file);

            var finalPositionInSource = content.NthIndexOfCharInString('\n', (int)sourceInfo.line) + sourceInfo.column;

            Dispatch(sourceInfo.file, finalPositionInSource);
        }

        private void Dispatch(string file, int positionInFile)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    IWpfTextView view;
                    view = (file != Document.FilePath) ? ProjectHelpers.GetCurentTextView() : SourceView;
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
            private readonly Func<Task<bool>> _canExecuteAction;
            private bool _canExecute;

            public GoToDefinitionCommand(Action executeAction, Func<Task<bool>> canExecuteAction)
            {
                _goToAction = executeAction;
                _canExecuteAction = canExecuteAction;
                _canExecute = false;
            }

            public bool CanExecute(object parameter)
            {
                CanExecuteChanged += async (sender, e) => { _canExecute = await _canExecuteAction(); };
                return _canExecute;
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
