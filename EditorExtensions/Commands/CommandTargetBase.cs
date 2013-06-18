using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal abstract class CommandTargetBase : IOleCommandTarget
    {
        private IOleCommandTarget _nextCommandTarget;
        protected IWpfTextView TextView;

        public Guid CommandGroup { get; set; }
        public uint[] CommandIds { get; set; }

        public CommandTargetBase(IVsTextView adapter, IWpfTextView textView, Guid commandGroup, params uint[] commandIds)
        {
            this.CommandGroup = commandGroup;
            this.CommandIds = commandIds;
            this.TextView = textView;
            adapter.AddCommandFilter(this, out _nextCommandTarget);
        }

        protected abstract bool IsEnabled();
        protected abstract bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut);

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == this.CommandGroup && this.CommandIds.Contains(nCmdID))
            {
                bool result = Execute(nCmdID, nCmdexecopt, pvaIn, pvaOut);

                if (result)
                {
                    return VSConstants.S_OK;
                }
            }

            return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == CommandGroup)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    if (CommandIds.Contains(prgCmds[i].cmdID))
                    {
                        if (IsEnabled())
                        {
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                        }

                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;// | (uint)OLECMDF.OLECMDF_INVISIBLE;
                        //return VSConstants.S_OK;
                    }
                }
            }

            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}
