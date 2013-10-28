using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.Html.Editor;
using Microsoft.Html.Editor.Projection;
using Microsoft.Html.Editor.WebForms;

using Microsoft.VisualStudio.Html.ContainedLanguage;
using Microsoft.VisualStudio.Html.Editor;
using Microsoft.VisualStudio.Html.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;

using Microsoft.VisualStudio.Web.Editor;
using Microsoft.VisualStudio.Web.Editor.Workspace;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.ContainedLanguage;
using Microsoft.Web.Editor.Workspace;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    // Abandon all hope ye who enters here.
    // https://twitter.com/Schabse/status/393092191356076032
    // https://twitter.com/jasonmalinowski/status/393094145398407168

    // Decompiled from Microsoft.VisualStudio.Html.ContainedLanguage.Server
    // Hopefully, we can end these nightmares when the Roslyn editor ships.

    internal sealed class LegacyContainedLanguageCommandTarget : IDisposable
    {
        private LanguageProjectionBuffer _languageBuffer;
        public ITextView TextView { get; private set; }
        public ICommandTarget ContainedLanguageTarget { get; private set; }

        internal void Create(HtmlEditorDocument document, IVsContainedLanguage containedLanguage, IVsTextBufferCoordinator bufferCoordinator, LanguageProjectionBuffer languageBuffer, out IVsTextViewFilter containedLanguageViewfilter)
        {
            containedLanguageViewfilter = null;
            TextViewData textViewDataForBuffer = TextViewConnectionListener.GetTextViewDataForBuffer(document.TextBuffer);
            if (textViewDataForBuffer == null || textViewDataForBuffer.LastActiveView == null)
                return;
            this.TextView = textViewDataForBuffer.LastActiveView;
            IVsTextViewIntellisenseHostProvider vsTextViewIntellisenseHostProvider = this.TextView.QueryInterface<IVsTextViewIntellisenseHostProvider>();
            if (vsTextViewIntellisenseHostProvider == null)
                return;

            Guid gUID = typeof(IVsTextViewIntellisenseHost).GUID;
            IntPtr intPtr;
            vsTextViewIntellisenseHostProvider.CreateIntellisenseHost(bufferCoordinator, ref gUID, out intPtr);
            if (intPtr == IntPtr.Zero)
                return;

            IVsTextViewIntellisenseHost vsTextViewIntellisenseHost = Marshal.GetObjectForIUnknown(intPtr) as IVsTextViewIntellisenseHost;
            Marshal.Release(intPtr);
            if (vsTextViewIntellisenseHost == null)
                return;

            HtmlMainController htmlMainController = HtmlMainController.FromTextView(this.TextView);
            ICommandTarget chainedController = htmlMainController.ChainedController;
            if (chainedController == null)
                return;

            OleToCommandTargetShim oleToCommandTargetShim = chainedController as OleToCommandTargetShim;
            if (containedLanguage.GetTextViewFilter(vsTextViewIntellisenseHost, oleToCommandTargetShim.OleTarget, out containedLanguageViewfilter) != 0)
                return;

            IOleCommandTarget oleTarget = containedLanguageViewfilter as IOleCommandTarget;
            OleToCommandTargetShim containedLanguageTarget = new OleToCommandTargetShim(this.TextView, oleTarget);
            this.ContainedLanguageTarget = containedLanguageTarget;

            this._languageBuffer = languageBuffer;
            this._languageBuffer.MappingsChanged += this.OnMappingsChanged;
        }
        private void OnMappingsChanged(object sender, MappingsChangedEventArgs e)
        {
            if (e.Mappings.Count > 0)
            {
                this._languageBuffer.MappingsChanged -= this.OnMappingsChanged;
                IContainedLanguageHost host = ContainedLanguageHost.GetHost(this.TextView, this._languageBuffer.IProjectionBuffer);
                host.SetContainedCommandTarget(this.TextView, this.ContainedLanguageTarget);
                this.ContainedLanguageTarget = null;
                this.TextView = null;
                this._languageBuffer = null;
            }
        }
        public void Dispose()
        {
            this._languageBuffer = null;
            this.ContainedLanguageTarget = null;
            this.TextView = null;
        }
    }

    internal static class HtmlConstants
    {
        public static readonly Guid SID_SBufferCoordinatorServerLanguage = new Guid(1831698500u, 57048, 16919, 180, 0, 38, 95, 70, 224, 2, 65);
    }

    internal class VsLegacyContainedLanguageHost : IVsContainedLanguageHost, IContainedLanguageHostVs, IWebContainedLanguageHost, IContainedLanguageHost
    {
        private HtmlEditorDocument _vsDocument;
        private Dictionary<uint, IVsContainedLanguageHostEvents> _sinks = new Dictionary<uint, IVsContainedLanguageHostEvents>();
        private uint _cookie = 1u;
        private IWebContainedLanguageHost _modernContainedLanguageHost;
        private LanguageProjectionBuffer _secondaryBuffer;
        private bool _canReformatCode = true;
        public event EventHandler<ContainedLanguageHostClosingEventArgs> Closing;

        public IVsWebWorkspaceItem WorkspaceItem { get { return (IVsWebWorkspaceItem)_vsDocument.WorkspaceItem; } }


        public IWebWorkspaceItem WebWorkspaceItem { get { return this._modernContainedLanguageHost.WebWorkspaceItem; } }
        public string DocumentPath { get { return this._modernContainedLanguageHost.DocumentPath; } }
        public IBufferGraph BufferGraph { get { return this._modernContainedLanguageHost.BufferGraph; } }
        public IContainedLanguageSettings ContainedLanguageSettings
        {
            get { return this._modernContainedLanguageHost.ContainedLanguageSettings; }
            set { this._modernContainedLanguageHost.ContainedLanguageSettings = value; }
        }
        public IVsTextViewFilter TextViewFilter
        {
            get { return ((IContainedLanguageHostVs)this._modernContainedLanguageHost).TextViewFilter; }
            set { ((IContainedLanguageHostVs)this._modernContainedLanguageHost).TextViewFilter = value; }
        }
        public IVsLanguageDebugInfo ContainedLanguageDebugInfo
        {
            get { return ((IContainedLanguageHostVs)this._modernContainedLanguageHost).ContainedLanguageDebugInfo; }
            set { ((IContainedLanguageHostVs)this._modernContainedLanguageHost).ContainedLanguageDebugInfo = value; }
        }
        public IVsLanguageContextProvider ContainedLanguageContextProvider
        {
            get { return ((IContainedLanguageHostVs)this._modernContainedLanguageHost).ContainedLanguageContextProvider; }
            set { ((IContainedLanguageHostVs)this._modernContainedLanguageHost).ContainedLanguageContextProvider = value; }
        }

        public VsLegacyContainedLanguageHost(HtmlEditorDocument vsDocument, LanguageProjectionBuffer secondaryBuffer)
        {
            this._modernContainedLanguageHost = (ContainedLanguageHost.GetHost(vsDocument.PrimaryView, secondaryBuffer.IProjectionBuffer) as IWebContainedLanguageHost);
            this._secondaryBuffer = secondaryBuffer;
            this._vsDocument = vsDocument;
            this._vsDocument.OnDocumentClosing += this.OnDocumentClosing;
            secondaryBuffer.MappingsChanging += this.OnMappingsChanging;
            secondaryBuffer.MappingsChanged += this.OnMappingsChanged;
        }
        private void OnMappingsChanging(object sender, EventArgs e)
        {
            this._canReformatCode = false;
        }
        private void OnMappingsChanged(object sender, MappingsChangedEventArgs e)
        {
            this._canReformatCode = true;
        }
        private void OnDocumentClosing(object sender, EventArgs e)
        {
            if (this.Closing != null)
            {
                this.Closing(this, new ContainedLanguageHostClosingEventArgs(this, this._secondaryBuffer.IProjectionBuffer));
            }
            _secondaryBuffer.MappingsChanging -= this.OnMappingsChanging;
            _secondaryBuffer.MappingsChanged -= this.OnMappingsChanged;
            this._vsDocument.OnDocumentClosing -= this.OnDocumentClosing;
            this._vsDocument = null;
        }
        public int Advise(IVsContainedLanguageHostEvents pHost, out uint pvsCookie)
        {
            this._sinks[this._cookie] = pHost;
            pvsCookie = this._cookie++;
            return 0;
        }
        public int CanReformatCode(out int pfCanReformat)
        {
            pfCanReformat = (this._canReformatCode ? 1 : 0);
            return 0;
        }
        public int EnsureSecondaryBufferReady()
        {
            IAspNetSecondaryBufferGenerator service = ServiceManager.GetService<IAspNetSecondaryBufferGenerator>(this._vsDocument.TextBuffer);
            if (service != null)
            {
                service.WaitForCodeReady();
                return 0;
            }
            return -2147467259;
        }
        public int EnsureSpanVisible(TextSpan tsPrimary)
        {
            ITextView primaryView = this._vsDocument.PrimaryView;
            if (primaryView != null)
            {
                try
                {
                    ITextSnapshot currentSnapshot = primaryView.TextBuffer.CurrentSnapshot;
                    ITextSnapshotLine lineFromLineNumber = currentSnapshot.GetLineFromLineNumber(tsPrimary.iStartLine);
                    ITextSnapshotLine lineFromLineNumber2 = currentSnapshot.GetLineFromLineNumber(tsPrimary.iEndLine);
                    int num = lineFromLineNumber.Start + tsPrimary.iStartIndex;
                    int num2 = lineFromLineNumber2.Start + tsPrimary.iEndIndex;
                    SnapshotSpan span = new SnapshotSpan(primaryView.TextBuffer.CurrentSnapshot, num, num2 - num);
                    primaryView.ViewScroller.EnsureSpanVisible(span);
                }
                catch (Exception)
                {
                    return -2147467259;
                }
                return 0;
            }
            return 0;
        }
        public int GetErrorProviderInformation(out string pbstrTaskProviderName, out Guid pguidTaskProviderGuid)
        {
            pbstrTaskProviderName = "HTML";
            pguidTaskProviderGuid = Guid.Empty;
            return 0;
        }
        public int GetLineIndent(int lineNumber, out string indentString, out int parentIndentLevel, out int indentSize, out int tabs, out int tabSize)
        {
            ContainedLineIndentSettings lineIndent = this._modernContainedLanguageHost.GetLineIndent(lineNumber);
            indentString = lineIndent.IndentString;
            parentIndentLevel = lineIndent.ParentIndentLevel;
            indentSize = lineIndent.IndentSize;
            tabs = (lineIndent.Tabs ? 1 : 0);
            tabSize = lineIndent.TabSize;
            return 0;
        }
        public int GetNearestVisibleToken(TextSpan tsSecondaryToken, TextSpan[] ptsPrimaryToken)
        {
            return 0;
        }
        public int GetVSHierarchy(out IVsHierarchy ppVsHierarchy)
        {
            ppVsHierarchy = null;
            if (this._vsDocument != null)
            {
                ppVsHierarchy = WorkspaceItem.Hierarchy;
                return 0;
            }
            return -2147467259;
        }

        public int InsertControl(string pwcFullType, string pwcID) { return 0; }
        public int InsertImportsDirective(ref ushort __MIDL_0011) { return 0; }
        public int InsertReference(ref ushort __MIDL_0010) { return 0; }
        public int OnContainedLanguageEditorSettingsChange() { return 0; }
        public int OnRenamed(ContainedLanguageRenameType clrt, string bstrOldID, string bstrNewID) { return 0; }

        public int QueryEditFile()
        {
            string[] fileNames = { WorkspaceItem.PhysicalPath };
            bool flag = false;
            EditorUtilities.QueryCheckOut(VsQueryEditFlags.AllowInMemoryEdits, fileNames, out flag);
            if (!flag)
            {
                return -2147217407;
            }
            return 0;
        }
        public int Unadvise(uint vsCookie)
        {
            IVsContainedLanguageHostEvents vsContainedLanguageHostEvents;
            if (this._sinks.TryGetValue(vsCookie, out vsContainedLanguageHostEvents))
            {
                this._sinks.Remove(vsCookie);
                return 0;
            }
            return -2147467259;
        }
        public ICommandTarget GetContainedCommandTarget(ITextView textView)
        {
            return this._modernContainedLanguageHost.GetContainedCommandTarget(textView);
        }
        public ContainedLineIndentSettings GetLineIndent(int lineNumber)
        {
            return this._modernContainedLanguageHost.GetLineIndent(lineNumber);
        }
        public object SetContainedCommandTarget(ITextView textView, object containedCommandTarget)
        {
            return this._modernContainedLanguageHost.SetContainedCommandTarget(textView, containedCommandTarget);
        }
        public void RemoveContainedCommandTarget(ITextView textView)
        {
            this._modernContainedLanguageHost.RemoveContainedCommandTarget(textView);
        }
        public void SetTextViewFilter(object textViewFilter) { throw new NotImplementedException(); }
        public void RemoveTextViewFilter() { throw new NotImplementedException(); }
        public void SetContainedLanguageDebugInfo(object debugInfo) { throw new NotImplementedException(); }
        public void RemoveContainedLanguageDebugInfo() { throw new NotImplementedException(); }
        public void SetContainedLanguageContextProvider(object languageContextProvider) { throw new NotImplementedException(); }
        public void RemoveContainedLanguageContextProvider() { throw new NotImplementedException(); }
        public void SetContainedLanguageSettings(IContainedLanguageSettings containedLanguageSettings) { throw new NotImplementedException(); }
        public void RemoveContainedLanguageSettings() { throw new NotImplementedException(); }
        public void GetLineIndent(int lineNumber, out string indentString)
        {
            ((IVsContainedLanguageHost3)this._modernContainedLanguageHost).GetLineIndent(lineNumber, out indentString);
        }
        public void GetIndentSize(out int indentSize)
        {
            ((IVsContainedLanguageHost3)this._modernContainedLanguageHost).GetIndentSize(out indentSize);
        }
        public void GetTabs(out int tabs)
        {
            ((IVsContainedLanguageHost3)this._modernContainedLanguageHost).GetTabs(out tabs);
        }
        public void GetTabSize(out int tabSize)
        {
            ((IVsContainedLanguageHost3)this._modernContainedLanguageHost).GetTabSize(out tabSize);
        }
    }
}
