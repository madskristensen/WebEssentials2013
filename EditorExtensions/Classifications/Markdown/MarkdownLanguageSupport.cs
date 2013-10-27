using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.Projection;
using Microsoft.Html.Editor.WebForms;
using Microsoft.VisualStudio.Editor;
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
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Web.Editor;
using Microsoft.VisualStudio.Web.Editor.Workspace;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.ContainedLanguage;
using Microsoft.Web.Editor.Workspace;
using VSConstants = Microsoft.VisualStudio.VSConstants;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    // Abandon all hope ye who enters here.
    // https://twitter.com/Schabse/status/393092191356076032
    // https://twitter.com/jasonmalinowski/status/393094145398407168

    // Decompiled from Microsoft.VisualStudio.Html.ContainedLanguage.Server
    // and modified to support both C# and VB.Net editors in the same file.
    // Hopefully, we can end these nightmares when the Roslyn editor ships.
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    [Export(typeof(ITextViewCreationListener))]
    internal sealed class ServerContainedLanguageSupportViewTracker : ITextViewCreationListener
    {
        public void OnTextViewCreated(ITextView textView, ITextBuffer textBuffer)
        {
            MarkdownLanguageSupport.EnsureConnected(textBuffer);
        }
    }

    internal sealed class LegacyContainedLanguageCommandTarget : IDisposable
    {
        private LanguageProjectionBuffer _languageBuffer;
        public ITextView TextView
        {
            get;
            private set;
        }
        public ICommandTarget ContainedLanguageTarget
        {
            get;
            private set;
        }
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

    class MarkdownLanguageSupport : IVsIntellisenseProjectEventSink, IDisposable
    {
        private INTELLIPROJSTATUS _intellisenseProjectStatus = INTELLIPROJSTATUS.INTELLIPROJSTATUS_LOADING;
        private HtmlEditorDocument _document;
        private uint _cookie;
        private bool _notifyEditorReady = true;
        private IVsIntellisenseProjectManager _intelisenseProjectManager;
        private readonly List<LanguageBridge> languageBridges = new List<LanguageBridge>();

        class LanguageBridge
        {
            readonly string fileExtension, serviceName;
            readonly MarkdownLanguageSupport owner;

            private IVsContainedLanguage containedLanguage;
            private IContainedLanguageHostVs containedLanguage2;

            private IVsContainedLanguageHost _containedLanguageHost;
            private IVsTextBufferCoordinator _textBufferCoordinator;
            private IVsTextLines _secondaryBuffer;
            private LegacyContainedLanguageCommandTarget _legacyCommandTarget;

            public LanguageBridge(MarkdownLanguageSupport owner, string serviceName, string fileExtension)
            {
                this.owner = owner;
                this.fileExtension = fileExtension;
                this.serviceName = serviceName;
                InitContainedLanguage();
            }

            public IVsContainedLanguageHost GetLegacyContainedLanguageHost(ITextBuffer containedLanguageBuffer)
            {
                if (this._containedLanguageHost == null)
                    this._containedLanguageHost = new VsLegacyContainedLanguageHost(owner._document, containedLanguageBuffer);
                return this._containedLanguageHost;
            }
            private void InitContainedLanguage()
            {
                IVsContainedLanguageFactory vsContainedLanguageFactory;
                owner._intelisenseProjectManager.GetContainedLanguageFactory(serviceName, out vsContainedLanguageFactory);
                if (vsContainedLanguageFactory == null)
                    return;

                IVsTextLines vsTextLines = this.EnsureBufferCoordinator(fileExtension);
                IVsContainedLanguage vsContainedLanguage;

                vsContainedLanguageFactory.GetLanguage(owner.WorkspaceItem.Hierarchy, (uint)owner.WorkspaceItem.ItemId, this._textBufferCoordinator, out vsContainedLanguage);
                if (vsContainedLanguage == null)
                    return;

                Guid langService;
                vsContainedLanguage.GetLanguageServiceID(out langService);
                vsTextLines.SetLanguageServiceID(ref langService);

                containedLanguage = vsContainedLanguage;
                IVsContainedLanguageHost legacyContainedLanguageHost = this.GetLegacyContainedLanguageHost(vsTextLines.ToITextBuffer());
                vsContainedLanguage.SetHost(legacyContainedLanguageHost);
                this._legacyCommandTarget = new LegacyContainedLanguageCommandTarget();

                var projectionBufferManager = ProjectionBufferManager.FromTextBuffer(owner._document.TextBuffer);
                var langBuffer = projectionBufferManager.GetProjectionBuffer(fileExtension) as LanguageProjectionBuffer;

                IVsTextViewFilter textViewFilter;
                this._legacyCommandTarget.Create(owner._document, vsContainedLanguage, this._textBufferCoordinator, langBuffer, out textViewFilter);
                IWebContainedLanguageHost webContainedLanguageHost = legacyContainedLanguageHost as IWebContainedLanguageHost;
                webContainedLanguageHost.SetContainedCommandTarget(this._legacyCommandTarget.TextView, this._legacyCommandTarget.ContainedLanguageTarget);
                containedLanguage2 = (webContainedLanguageHost as IContainedLanguageHostVs);
                containedLanguage2.TextViewFilter = textViewFilter;

                langBuffer.ResetMappings();

                WebEditor.TraceEvent(1005);
            }

            private IVsTextLines EnsureBufferCoordinator(string fileExtension)
            {
                if (this._secondaryBuffer != null)
                    return this._secondaryBuffer;
                ProjectionBufferManager pbm = ServiceManager.GetService<ProjectionBufferManager>(owner._document.TextBuffer);

                IProjectionBuffer iProjectionBuffer = pbm.GetProjectionBuffer(fileExtension).IProjectionBuffer;
                IVsTextBuffer vsTextBuffer = owner._document.TextBuffer.QueryInterface<IVsTextBuffer>();
                IObjectWithSite objectWithSite = vsTextBuffer as IObjectWithSite;
                Guid gUID = typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider).GUID;
                IntPtr pUnk;
                objectWithSite.GetSite(ref gUID, out pUnk);
                Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = Marshal.GetObjectForIUnknown(pUnk) as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
                Marshal.Release(pUnk);
                IVsEditorAdaptersFactoryService adapterFactory = WebEditor.ExportProvider.GetExport<IVsEditorAdaptersFactoryService>().Value;
                this._secondaryBuffer = (adapterFactory.GetBufferAdapter(iProjectionBuffer) as IVsTextLines);
                if (this._secondaryBuffer == null)
                {
                    this._secondaryBuffer = (adapterFactory.CreateVsTextBufferAdapterForSecondaryBuffer(serviceProvider, iProjectionBuffer) as IVsTextLines);
                }
                // TODO: Move after CreateVsTextBufferCoordinatorAdapter()?
                vsTextBuffer.SetTextBufferData(HtmlConstants.SID_SBufferCoordinatorServerLanguage, this._textBufferCoordinator);
                vsTextBuffer.SetTextBufferData(typeof(VsTextBufferCoordinatorClass).GUID, this._textBufferCoordinator);

                this._secondaryBuffer.SetTextBufferData(VSConstants.VsTextBufferUserDataGuid.VsBufferDetectLangSID_guid, false);
                this._secondaryBuffer.SetTextBufferData(VSConstants.VsTextBufferUserDataGuid.VsBufferMoniker_guid, owner.WorkspaceItem.PhysicalPath);
                IOleUndoManager oleUndoManager;
                this._secondaryBuffer.GetUndoManager(out oleUndoManager);
                oleUndoManager.Enable(0);
                this._textBufferCoordinator = adapterFactory.CreateVsTextBufferCoordinatorAdapter();
                this._textBufferCoordinator.SetBuffers(vsTextBuffer as IVsTextLines, this._secondaryBuffer);

                return this._secondaryBuffer;
            }
            public void ClearBufferCoordinator()
            {
                IVsTextBuffer vsTextBuffer = owner._document.TextBuffer.QueryInterface<IVsTextBuffer>();
                vsTextBuffer.SetTextBufferData(HtmlConstants.SID_SBufferCoordinatorServerLanguage, null);
                vsTextBuffer.SetTextBufferData(typeof(VsTextBufferCoordinatorClass).GUID, null);
            }


            public void Terminate()
            {
                if (this._legacyCommandTarget != null && this._legacyCommandTarget.TextView != null)
                    containedLanguage2.RemoveContainedCommandTarget(this._legacyCommandTarget.TextView);
                containedLanguage2.ContainedLanguageDebugInfo = null;
                containedLanguage2.TextViewFilter = null;

                if (this._legacyCommandTarget != null)
                {
                    this._legacyCommandTarget.Dispose();
                    this._legacyCommandTarget = null;
                }
                containedLanguage.SetHost(null);

                this._textBufferCoordinator = null;
                this._containedLanguageHost = null;
                if (this._secondaryBuffer != null)
                {
                    (this._secondaryBuffer as IVsPersistDocData).Close();
                    this._secondaryBuffer = null;
                }
            }
        }

        public IVsWebWorkspaceItem WorkspaceItem { get { return (IVsWebWorkspaceItem)_document.WorkspaceItem; } }

        public IWebApplicationCtxSvc WebApplicationContextService
        {
            get { return ServiceProvider.GlobalProvider.GetService(typeof(SWebApplicationCtxSvc)) as IWebApplicationCtxSvc; }
        }
        public MarkdownLanguageSupport(ITextBuffer textBuffer)
        {
            this._document = ServiceManager.GetService<HtmlEditorDocument>(textBuffer);
            this._document.OnDocumentClosing += this.OnDocumentClosing;

            WebEditor.OnIdle += this.OnIdle;
            ServiceManager.AddService<MarkdownLanguageSupport>(this, textBuffer);
        }
        public static void EnsureConnected(ITextBuffer textBuffer)
        {
            if (ServiceManager.GetService<MarkdownLanguageSupport>(textBuffer) != null)
                return;

            ProjectionBufferManager service = ServiceManager.GetService<ProjectionBufferManager>(textBuffer);
            if (service != null)
                new MarkdownLanguageSupport(textBuffer).ToString();
        }
        private void InitContainedLanguages()
        {
            this._notifyEditorReady = true;
            languageBridges.Add(new LanguageBridge(this, "C#", ".cs"));
            languageBridges.Add(new LanguageBridge(this, "VB", ".vb"));
        }
        private void InitIntellisenseProject()
        {
            if (WorkspaceItem.Hierarchy != null && this._cookie == 0u)
            {
                Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp;
                this.WebApplicationContextService.GetItemContext(WorkspaceItem.Hierarchy, (uint)WorkspaceItem.ItemId, out sp);
                ServiceProvider serviceProvider = new ServiceProvider(sp);
                object obj;
                serviceProvider.QueryService(typeof(SVsIntellisenseProjectManager).GUID, out obj);
                this._intelisenseProjectManager = (obj as IVsIntellisenseProjectManager);
                if (this._intelisenseProjectManager != null)
                {
                    this._intelisenseProjectManager.AdviseIntellisenseProjectEvents(this, out this._cookie);
                }
            }
        }
        private void TermIntellisenseProject()
        {
            if (this._intelisenseProjectManager != null)
            {
                if (this._cookie != 0u)
                {
                    this._intelisenseProjectManager.UnadviseIntellisenseProjectEvents(this._cookie);
                    this._cookie = 0u;
                }
                foreach (var c2 in languageBridges)
                {
                    c2.Terminate();
                }

                this._intelisenseProjectManager.CloseIntellisenseProject();
                this._intelisenseProjectManager = null;
            }
        }
        int IVsIntellisenseProjectEventSink.OnCodeFileChange(string pszOldCodeFile, string pszNewCodeFile)
        {
            return 0;
        }
        int IVsIntellisenseProjectEventSink.OnConfigChange()
        {
            return 0;
        }
        int IVsIntellisenseProjectEventSink.OnReferenceChange(uint dwChangeType, string pszAssemblyPath)
        {
            return 0;
        }
        int IVsIntellisenseProjectEventSink.OnStatusChange(uint dwStatus)
        {
            switch (dwStatus)
            {
                case 1u:
                    this._intellisenseProjectStatus = (INTELLIPROJSTATUS)dwStatus;
                    if (!HtmlUtilities.IsSolutionLoading(ServiceProvider.GlobalProvider))
                    {
                        this.EnsureIntellisenseProjectLoaded();
                    }
                    break;
                case 2u:
                    this._intellisenseProjectStatus = (INTELLIPROJSTATUS)dwStatus;
                    this.InitContainedLanguages();
                    this.NotifyEditorReady();
                    break;
                case 3u:
                    this.Reset(false);
                    this._intellisenseProjectStatus = (INTELLIPROJSTATUS)dwStatus;
                    break;
                case 4u:
                    this.EnsureIntellisenseProjectLoaded();
                    this.NotifyEditorReady();
                    break;
            }
            return 0;
        }
        internal void Reset(bool createIntellisenseProject)
        {
            this.TermIntellisenseProject();
            if (createIntellisenseProject)
            {
                this.InitIntellisenseProject();
            }
        }
        internal void NotifyEditorReady()
        {
            if (this._notifyEditorReady)
            {
                if (!HtmlUtilities.IsSolutionLoading(ServiceProvider.GlobalProvider))
                {
                    this.EnsureIntellisenseProjectLoaded();
                }
                if (this._intelisenseProjectManager != null)
                {
                    this._intelisenseProjectManager.OnEditorReady();
                    this._notifyEditorReady = false;
                }
            }
        }
        public void EnsureIntellisenseProjectLoaded()
        {
            if (this._intellisenseProjectStatus == INTELLIPROJSTATUS.INTELLIPROJSTATUS_LOADING && this._intelisenseProjectManager != null)
            {
                this._intelisenseProjectManager.CompleteIntellisenseProjectLoad();
            }
        }
        private void OnIdle(object sender, EventArgs e)
        {
            if (this._document != null)
            {
                this.InitIntellisenseProject();
                WebEditor.OnIdle -= this.OnIdle;
            }
        }
        private void OnDocumentClosing(object sender, EventArgs e)
        {
            foreach (var b in languageBridges)
                b.ClearBufferCoordinator();

            MarkdownLanguageSupport service = ServiceManager.GetService<MarkdownLanguageSupport>(this._document.TextBuffer);
            if (service != null)
            {
                ServiceManager.RemoveService<MarkdownLanguageSupport>(this._document.TextBuffer);
                service.Dispose();
            }
            WebEditor.OnIdle -= this.OnIdle;
            this._document.OnDocumentClosing -= this.OnDocumentClosing;
            this._document = null;
        }
        public void Dispose()
        {
            this.TermIntellisenseProject();
        }
    }


    internal static class HtmlConstants
    {
        public static class ClipboardFormat
        {
            public const int CF_TEXT = 1;
            public const int CF_UNICODETEXT = 13;
            public const int CF_HDROP = 15;
        }
        public static class BOOL
        {
            public const int FALSE = 0;
            public const int TRUE = 1;
        }
        public static readonly Guid SID_SBufferCoordinatorServerLanguage = new Guid(1831698500u, 57048, 16919, 180, 0, 38, 95, 70, 224, 2, 65);
        public static readonly Guid JSCRIPT_LANGUAGE_SERVICE_GUID = new Guid("59E2F421-410A-4fc9-9803-1F4E79216BE8");
    }



    internal class VsLegacyContainedLanguageHost : IVsContainedLanguageHost, IContainedLanguageHostVs, IWebContainedLanguageHost, IContainedLanguageHost
    {
        private HtmlEditorDocument _vsDocument;
        private Dictionary<uint, IVsContainedLanguageHostEvents> _sinks = new Dictionary<uint, IVsContainedLanguageHostEvents>();
        private uint _cookie = 1u;
        private IWebContainedLanguageHost _modernContainedLanguageHost;
        private ITextBuffer _secondaryBuffer;
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
        public VsLegacyContainedLanguageHost(HtmlEditorDocument vsDocument, ITextBuffer secondaryBuffer)
        {
            this._modernContainedLanguageHost = (ContainedLanguageHost.GetHost(vsDocument.PrimaryView, secondaryBuffer) as IWebContainedLanguageHost);
            this._secondaryBuffer = secondaryBuffer;
            this._vsDocument = vsDocument;
            this._vsDocument.OnDocumentClosing += this.OnDocumentClosing;
            ProjectionBufferManager projectionBufferManager = ProjectionBufferManager.FromTextBuffer(this._vsDocument.TextBuffer);
            LanguageProjectionBuffer projectionBuffer = projectionBufferManager.GetProjectionBuffer(this._secondaryBuffer.ContentType);
            projectionBuffer.MappingsChanging += this.OnMappingsChanging;
            projectionBuffer.MappingsChanged += this.OnMappingsChanged;
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
                this.Closing(this, new ContainedLanguageHostClosingEventArgs(this, this._secondaryBuffer));
            }
            ProjectionBufferManager projectionBufferManager = ProjectionBufferManager.FromTextBuffer(this._vsDocument.TextBuffer);
            LanguageProjectionBuffer projectionBuffer = projectionBufferManager.GetProjectionBuffer(this._secondaryBuffer.ContentType);
            projectionBuffer.MappingsChanging -= this.OnMappingsChanging;
            projectionBuffer.MappingsChanged -= this.OnMappingsChanged;
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
        public int InsertControl(string pwcFullType, string pwcID)
        {
            return 0;
        }
        public int InsertImportsDirective(ref ushort __MIDL_0011)
        {
            return 0;
        }
        public int InsertReference(ref ushort __MIDL_0010)
        {
            return 0;
        }
        public int OnContainedLanguageEditorSettingsChange()
        {
            return 0;
        }
        public int OnRenamed(ContainedLanguageRenameType clrt, string bstrOldID, string bstrNewID)
        {
            return 0;
        }
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
        public void SetTextViewFilter(object textViewFilter)
        {
            throw new NotImplementedException();
        }
        public void RemoveTextViewFilter()
        {
            throw new NotImplementedException();
        }
        public void SetContainedLanguageDebugInfo(object debugInfo)
        {
            throw new NotImplementedException();
        }
        public void RemoveContainedLanguageDebugInfo()
        {
            throw new NotImplementedException();
        }
        public void SetContainedLanguageContextProvider(object languageContextProvider)
        {
            throw new NotImplementedException();
        }
        public void RemoveContainedLanguageContextProvider()
        {
            throw new NotImplementedException();
        }
        public void SetContainedLanguageSettings(IContainedLanguageSettings containedLanguageSettings)
        {
            throw new NotImplementedException();
        }
        public void RemoveContainedLanguageSettings()
        {
            throw new NotImplementedException();
        }
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
