using System;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class TypeScriptSmartIndent : CommandTargetBase
    {
        private readonly ICompletionBroker _broker;

        public TypeScriptSmartIndent(IVsTextView adapter, IWpfTextView textView, ICompletionBroker broker)
            : base(adapter, textView, typeof(VSConstants.VSStd2KCmdID).GUID, 3)
        {
            _broker = broker;
         }

        protected override bool Execute(CommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_broker.IsCompletionActive(TextView))
                return false;

            int position = TextView.Caret.Position.BufferPosition.Position;

            if (position == 0 || position == TextView.TextBuffer.CurrentSnapshot.Length || TextView.Selection.SelectedSpans[0].Length > 0)
                return false;

            char before = TextView.TextBuffer.CurrentSnapshot.GetText(position - 1, 1)[0];
            char after = TextView.TextBuffer.CurrentSnapshot.GetText(position, 1)[0];

            if (before == '{' && after == '}')
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    using (EditorExtensionsPackage.UndoContext("Smart Indent"))
                    {
                        // HACK: A better way is needed. 
                        // We do this to get around the native TS formatter
                        SendKeys.Send("{TAB}{ENTER}{UP}");
                    }
                }), DispatcherPriority.Normal, null);
            }

            return false;
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}