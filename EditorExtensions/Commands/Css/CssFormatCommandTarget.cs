using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class CssFormatProperties : IOleCommandTarget
    {
        private ITextView _textView;

        public CssFormatProperties(ITextView textView)
        {
            this._textView = textView;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                switch (nCmdID)
                {
                    case (uint)VSConstants.VSStd2KCmdID.FORMATSELECTION:
                    case (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
                        if (_textView.GetSelection("SCSS").HasValue)
                        {
                            return VSConstants.S_OK;
                        }

                        break;
                }
            }

            return (int)(Constants.MSOCMDERR_E_FIRST);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch (prgCmds[i].cmdID)
                    {
                        case (uint)VSConstants.VSStd2KCmdID.FORMATSELECTION:
                        case (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            }

            return (int)(Constants.OLECMDERR_E_NOTSUPPORTED);
        }
    }
}