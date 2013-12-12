using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.VisualStudio.TextManager.Interop.IVsTextViewIntellisenseHostProvider.CreateIntellisenseHost(Microsoft.VisualStudio.TextManager.Interop.IVsTextBufferCoordinator,System.Guid@,System.IntPtr@)")]
        internal void Create(HtmlEditorDocument document, IVsContainedLanguage containedLanguage, IVsTextBufferCoordinator bufferCoordinator, LanguageProjectionBuffer languageBuffer, out IVsTextViewFilter containedLanguageViewfilter)
        {
            containedLanguageViewfilter = null;
            TextViewData textViewDataForBuffer = TextViewConnectionListener.GetTextViewDataForBuffer(document.TextBuffer);
            if (textViewDataForBuffer == null || textViewDataForBuffer.LastActiveView == null)
                return;
            TextView = textViewDataForBuffer.LastActiveView;
            IVsTextViewIntellisenseHostProvider vsTextViewIntellisenseHostProvider = TextView.QueryInterface<IVsTextViewIntellisenseHostProvider>();
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

            HtmlMainController htmlMainController = HtmlMainController.FromTextView(TextView);
            ICommandTarget chainedController = htmlMainController.ChainedController;
            if (chainedController == null)
                return;

            OleToCommandTargetShim oleToCommandTargetShim = chainedController as OleToCommandTargetShim;
            if (containedLanguage.GetTextViewFilter(vsTextViewIntellisenseHost, oleToCommandTargetShim.OleTarget, out containedLanguageViewfilter) != 0)
                return;

            IOleCommandTarget oleTarget = containedLanguageViewfilter as IOleCommandTarget;
            OleToCommandTargetShim containedLanguageTarget = new OleToCommandTargetShim(TextView, oleTarget);
            ContainedLanguageTarget = containedLanguageTarget;

            _languageBuffer = languageBuffer;
            _languageBuffer.MappingsChanged += OnMappingsChanged;
        }
        private void OnMappingsChanged(object sender, MappingsChangedEventArgs e)
        {
            if (e.Mappings.Count > 0)
            {
                _languageBuffer.MappingsChanged -= OnMappingsChanged;
                IContainedLanguageHost host = ContainedLanguageHost.GetHost(TextView, _languageBuffer.IProjectionBuffer);
                host.SetContainedCommandTarget(TextView, ContainedLanguageTarget);
                ContainedLanguageTarget = null;
                TextView = null;
                _languageBuffer = null;
            }
        }
        public void Dispose()
        {
            _languageBuffer = null;
            ContainedLanguageTarget = null;
            TextView = null;
        }
    }

    internal static class HtmlConstants
    {
        public static readonly Guid SID_SBufferCoordinatorServerLanguage = new Guid(1831698500u, 57048, 16919, 180, 0, 38, 95, 70, 224, 2, 65);
    }

    internal class VsLegacyContainedLanguageHost : IVsContainedLanguageHost, IContainedLanguageHostVs, IWebContainedLanguageHost, IContainedLanguageHost
    {
        private readonly IVsHierarchy hierarchy;
        private HtmlEditorDocument _vsDocument;
        private Dictionary<uint, IVsContainedLanguageHostEvents> _sinks = new Dictionary<uint, IVsContainedLanguageHostEvents>();
        private uint _cookie = 1u;
        private IWebContainedLanguageHost _modernContainedLanguageHost;
        private LanguageProjectionBuffer _secondaryBuffer;
        private bool _canReformatCode = true;

        public event EventHandler<ContainedLanguageHostClosingEventArgs> Closing;
        public IVsWebWorkspaceItem WorkspaceItem { get { return (IVsWebWorkspaceItem)_vsDocument.WorkspaceItem; } }
        public IWebWorkspaceItem WebWorkspaceItem { get { return _modernContainedLanguageHost.WebWorkspaceItem; } }
        public string DocumentPath { get { return _modernContainedLanguageHost.DocumentPath; } }
        public IBufferGraph BufferGraph { get { return _modernContainedLanguageHost.BufferGraph; } }

        public IContainedLanguageSettings ContainedLanguageSettings
        {
            get { return _modernContainedLanguageHost.ContainedLanguageSettings; }
            set { _modernContainedLanguageHost.ContainedLanguageSettings = value; }
        }

        public IVsTextViewFilter TextViewFilter
        {
            get { return ((IContainedLanguageHostVs)_modernContainedLanguageHost).TextViewFilter; }
            set { ((IContainedLanguageHostVs)_modernContainedLanguageHost).TextViewFilter = value; }
        }

        public IVsLanguageDebugInfo ContainedLanguageDebugInfo
        {
            get { return ((IContainedLanguageHostVs)_modernContainedLanguageHost).ContainedLanguageDebugInfo; }
            set { ((IContainedLanguageHostVs)_modernContainedLanguageHost).ContainedLanguageDebugInfo = value; }
        }

        public IVsLanguageContextProvider ContainedLanguageContextProvider
        {
            get { return ((IContainedLanguageHostVs)_modernContainedLanguageHost).ContainedLanguageContextProvider; }
            set { ((IContainedLanguageHostVs)_modernContainedLanguageHost).ContainedLanguageContextProvider = value; }
        }

        public VsLegacyContainedLanguageHost(HtmlEditorDocument vsDocument, LanguageProjectionBuffer secondaryBuffer, IVsHierarchy hierarchy)
        {
            _modernContainedLanguageHost = (ContainedLanguageHost.GetHost(vsDocument.PrimaryView, secondaryBuffer.IProjectionBuffer) as IWebContainedLanguageHost);
            _secondaryBuffer = secondaryBuffer;
            this.hierarchy = hierarchy;
            _vsDocument = vsDocument;
            _vsDocument.OnDocumentClosing += OnDocumentClosing;
            secondaryBuffer.MappingsChanging += OnMappingsChanging;
            secondaryBuffer.MappingsChanged += OnMappingsChanged;
        }

        private void OnMappingsChanging(object sender, EventArgs e)
        {
            _canReformatCode = false;
        }

        private void OnMappingsChanged(object sender, MappingsChangedEventArgs e)
        {
            _canReformatCode = true;
        }

        private void OnDocumentClosing(object sender, EventArgs e)
        {
            if (Closing != null)
            {
                Closing(this, new ContainedLanguageHostClosingEventArgs(this, _secondaryBuffer.IProjectionBuffer));
            }

            _secondaryBuffer.MappingsChanging -= OnMappingsChanging;
            _secondaryBuffer.MappingsChanged -= OnMappingsChanged;
            _vsDocument.OnDocumentClosing -= OnDocumentClosing;
            _vsDocument = null;
        }

        public int Advise(IVsContainedLanguageHostEvents pHost, out uint pvsCookie)
        {
            _sinks[_cookie] = pHost;
            pvsCookie = _cookie++;
            return 0;
        }

        public int CanReformatCode(out int pfCanReformat)
        {
            pfCanReformat = (_canReformatCode ? 1 : 0);
            return 0;
        }

        public int EnsureSecondaryBufferReady()
        {
            IAspNetSecondaryBufferGenerator service = ServiceManager.GetService<IAspNetSecondaryBufferGenerator>(_vsDocument.TextBuffer);
            if (service != null)
            {
                service.WaitForCodeReady();
                return 0;
            }
            return -2147467259;
        }

        public int EnsureSpanVisible(TextSpan tsPrimary)
        {
            ITextView primaryView = _vsDocument.PrimaryView;
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
            ContainedLineIndentSettings lineIndent = _modernContainedLanguageHost.GetLineIndent(lineNumber);
            indentString = lineIndent.IndentString;
            parentIndentLevel = lineIndent.ParentIndentLevel;
            // TODO: Return block quote prefix?
            indentSize = 0;// lineIndent.IndentSize;
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
            ppVsHierarchy = hierarchy;
            return 0;
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
            if (_sinks.TryGetValue(vsCookie, out vsContainedLanguageHostEvents))
            {
                _sinks.Remove(vsCookie);
                return 0;
            }
            return -2147467259;
        }

        public ICommandTarget GetContainedCommandTarget(ITextView textView)
        {
            return _modernContainedLanguageHost.GetContainedCommandTarget(textView);
        }

        public ContainedLineIndentSettings GetLineIndent(int lineNumber)
        {
            return _modernContainedLanguageHost.GetLineIndent(lineNumber);
        }

        public object SetContainedCommandTarget(ITextView textView, object containedCommandTarget)
        {
            return _modernContainedLanguageHost.SetContainedCommandTarget(textView, containedCommandTarget);
        }

        public void RemoveContainedCommandTarget(ITextView textView)
        {
            _modernContainedLanguageHost.RemoveContainedCommandTarget(textView);
        }
    }
}
