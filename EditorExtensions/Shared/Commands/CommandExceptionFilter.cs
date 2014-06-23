using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Commands
{
    class CommandExceptionFilter : IOleCommandTarget
    {
        private readonly IOleCommandTarget _nextCommandTarget;
        private readonly ITextUndoHistoryRegistry _undoRegistry;
        readonly ITextView _textView;

        public CommandExceptionFilter(IVsTextView adapter, ITextView textView, ITextUndoHistoryRegistry undoRegistry)
        {
            ErrorHandler.ThrowOnFailure(adapter.AddCommandFilter(this, out _nextCommandTarget));
            _textView = textView;
            _undoRegistry = undoRegistry;
        }


        public int Exec(ref Guid pguidCmdGroup, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint nCmdID, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            try
            {
                return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            catch (Exception ex)
            {
                Logger.Log("An exception was thrown while executing command " + GetCommandName(pguidCmdGroup, nCmdID) + ":\n" + ex);

                if (WebEssentialsPackage.DTE.UndoContext.IsOpen)
                    WebEssentialsPackage.DTE.UndoContext.Close();

                foreach (var buffer in _textView.BufferGraph.GetTextBuffers(b => true))
                    AbortUndoTransaction(buffer);

                throw;
            }
        }
        private void AbortUndoTransaction(ITextBuffer buffer)
        {
            ITextUndoHistory history;
            if (!_undoRegistry.TryGetHistory(buffer, out history))
                return;

            while (history.CurrentTransaction != null)
                history.CurrentTransaction.Complete();
        }

        private static string GetCommandName(Guid cmdGroup, uint cmdId)
        {
            if (cmdGroup == VSConstants.GUID_VSStandardCommandSet97)
                return "Std97: " + (VSConstants.VSStd97CmdID)cmdId;
            if (cmdGroup == VSConstants.VSStd2K)
                return "Std2K: " + (VSConstants.VSStd2KCmdID)cmdId;
            if (cmdGroup == VSConstants.VsStd2010)
                return "Std2010: " + (VSConstants.VSStd2010CmdID)cmdId;
            if (cmdGroup == VSConstants.VsStd11)
                return "Std11: " + (VSConstants.VSStd11CmdID)cmdId;
            if (cmdGroup == VSConstants.VsStd12)
                return "Std12: " + (VSConstants.VSStd12CmdID)cmdId;

            return cmdGroup + ": " + cmdId;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint cCmds, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.OLECMD")]OLECMD[] prgCmds, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.OLECMDTEXT")]IntPtr pCmdText)
        {
            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}
