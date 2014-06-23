using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.Projection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Html.ContainedLanguage;
using Microsoft.VisualStudio.Html.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Web.Editor;
using Microsoft.VisualStudio.Web.Editor.Workspace;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.ContainedLanguage;
using VSConstants = Microsoft.VisualStudio.VSConstants;


namespace MadsKristensen.EditorExtensions.Markdown
{
    // Abandon all hope ye who enters here.
    // https://twitter.com/Schabse/status/393092191356076032
    // https://twitter.com/jasonmalinowski/status/393094145398407168

    // Based on decompiled code from Microsoft.VisualStudio.Html.ContainedLanguage.Server
    // Thanks to Jason Malinowski for helping me navigate this mess.
    // All of this can go away when the Roslyn editor ships.


    class ContainedLanguageAdapter
    {
        public static ContainedLanguageAdapter ForBuffer(ITextBuffer textBuffer)
        {
            var retVal = ServiceManager.GetService<ContainedLanguageAdapter>(textBuffer);
            if (retVal == null)
                retVal = new ContainedLanguageAdapter(textBuffer);
            return retVal;
        }

        public HtmlEditorDocument Document { get; private set; }
        IVsWebWorkspaceItem WorkspaceItem { get { return (IVsWebWorkspaceItem)Document.WorkspaceItem; } }
        readonly Dictionary<IContentType, LanguageBridge> languageBridges = new Dictionary<IContentType, LanguageBridge>();

        public ContainedLanguageAdapter(ITextBuffer textBuffer)
        {
            Document = ServiceManager.GetService<HtmlEditorDocument>(textBuffer);
            Document.OnDocumentClosing += OnDocumentClosing;

            ServiceManager.AddService(this, textBuffer);
        }

        sealed class LanguageBridge : IDisposable
        {
            public LanguageProjectionBuffer ProjectionBuffer { get; private set; }

            readonly ContainedLanguageAdapter owner;
            readonly IVsContainedLanguageFactory languageFactory;

            private IVsContainedLanguage containedLanguage;
            private IContainedLanguageHostVs containedLanguage2;

            private IVsContainedLanguageHost _containedLanguageHost;
            private IVsTextBufferCoordinator _textBufferCoordinator;
            private IVsTextLines _secondaryBuffer;
            private LegacyContainedLanguageCommandTarget _legacyCommandTarget;
            private readonly IVsHierarchy hierarchy;

            public LanguageBridge(ContainedLanguageAdapter owner, LanguageProjectionBuffer projectionBuffer, IVsContainedLanguageFactory languageFactory, IVsHierarchy hierarchy)
            {
                this.owner = owner;
                this.languageFactory = languageFactory;
                ProjectionBuffer = projectionBuffer;
                this.hierarchy = hierarchy;
                InitContainedLanguage();
            }

            public IVsContainedLanguageHost GetLegacyContainedLanguageHost()
            {
                if (_containedLanguageHost == null)
                    _containedLanguageHost = new VsLegacyContainedLanguageHost(owner.Document, ProjectionBuffer, hierarchy);
                return _containedLanguageHost;
            }

            [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
            private void InitContainedLanguage()
            {
                IVsTextLines vsTextLines = EnsureBufferCoordinator();
                IVsContainedLanguage vsContainedLanguage;

                languageFactory.GetLanguage(hierarchy, MarkdownCodeProject.FileItemId, _textBufferCoordinator, out vsContainedLanguage);

                if (vsContainedLanguage == null)
                {
                    Logger.Log("Markdown: Couldn't get IVsContainedLanguage for " + ProjectionBuffer.IProjectionBuffer.ContentType);
                    return;
                }

                Guid langService;
                vsContainedLanguage.GetLanguageServiceID(out langService);
                vsTextLines.SetLanguageServiceID(ref langService);

                containedLanguage = vsContainedLanguage;
                IVsContainedLanguageHost legacyContainedLanguageHost = GetLegacyContainedLanguageHost();
                vsContainedLanguage.SetHost(legacyContainedLanguageHost);
                _legacyCommandTarget = new LegacyContainedLanguageCommandTarget();

                IVsTextViewFilter textViewFilter;
                _legacyCommandTarget.Create(owner.Document, vsContainedLanguage, _textBufferCoordinator, ProjectionBuffer, out textViewFilter);
                IWebContainedLanguageHost webContainedLanguageHost = legacyContainedLanguageHost as IWebContainedLanguageHost;
                webContainedLanguageHost.SetContainedCommandTarget(_legacyCommandTarget.TextView, _legacyCommandTarget.ContainedLanguageTarget);
                containedLanguage2 = (webContainedLanguageHost as IContainedLanguageHostVs);
                containedLanguage2.TextViewFilter = textViewFilter;

                ProjectionBuffer.ResetMappings();

                WebEditor.TraceEvent(1005);
            }

            [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
            private IVsTextLines EnsureBufferCoordinator()
            {
                if (_secondaryBuffer != null)
                    return _secondaryBuffer;

                var vsTextBuffer = owner.Document.TextBuffer.QueryInterface<IVsTextBuffer>();

                IVsEditorAdaptersFactoryService adapterFactory = WebEditor.ExportProvider.GetExport<IVsEditorAdaptersFactoryService>().Value;
                _secondaryBuffer = (adapterFactory.GetBufferAdapter(ProjectionBuffer.IProjectionBuffer) as IVsTextLines);
                if (_secondaryBuffer == null)
                {
                    _secondaryBuffer = (adapterFactory.CreateVsTextBufferAdapterForSecondaryBuffer(vsTextBuffer.GetServiceProvider(), ProjectionBuffer.IProjectionBuffer) as IVsTextLines);
                }

                _secondaryBuffer.SetTextBufferData(VSConstants.VsTextBufferUserDataGuid.VsBufferDetectLangSID_guid, false);
                _secondaryBuffer.SetTextBufferData(VSConstants.VsTextBufferUserDataGuid.VsBufferMoniker_guid, owner.WorkspaceItem.PhysicalPath);

                IOleUndoManager oleUndoManager;
                _secondaryBuffer.GetUndoManager(out oleUndoManager);
                oleUndoManager.Enable(0);

                _textBufferCoordinator = adapterFactory.CreateVsTextBufferCoordinatorAdapter();
                vsTextBuffer.SetTextBufferData(HtmlConstants.SID_SBufferCoordinatorServerLanguage, _textBufferCoordinator);
                vsTextBuffer.SetTextBufferData(typeof(VsTextBufferCoordinatorClass).GUID, _textBufferCoordinator);

                _textBufferCoordinator.SetBuffers(vsTextBuffer as IVsTextLines, _secondaryBuffer);

                return _secondaryBuffer;
            }
            public void ClearBufferCoordinator()
            {
                IVsTextBuffer vsTextBuffer = owner.Document.TextBuffer.QueryInterface<IVsTextBuffer>();
                vsTextBuffer.SetTextBufferData(HtmlConstants.SID_SBufferCoordinatorServerLanguage, null);
                vsTextBuffer.SetTextBufferData(typeof(VsTextBufferCoordinatorClass).GUID, null);
            }

            [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
            public void Dispose()
            {
                if (_legacyCommandTarget != null && _legacyCommandTarget.TextView != null)
                    containedLanguage2.RemoveContainedCommandTarget(_legacyCommandTarget.TextView);
                containedLanguage2.ContainedLanguageDebugInfo = null;
                containedLanguage2.TextViewFilter = null;

                if (_legacyCommandTarget != null)
                {
                    _legacyCommandTarget.Dispose();
                    _legacyCommandTarget = null;
                }
                containedLanguage.SetHost(null);

                _textBufferCoordinator = null;
                _containedLanguageHost = null;
                if (_secondaryBuffer != null)
                {
                    ((_secondaryBuffer as IVsPersistDocData)).Close();
                    _secondaryBuffer = null;
                }

                if (Disposing != null)
                    Disposing(this, EventArgs.Empty);
            }

            public event EventHandler Disposing;
        }

        ///<summary>Creates a ContainedLanguage for the specified ProjectionBuffer, using an IVsIntellisenseProjectManager to initialize the language.</summary>
        ///<param name="projectionBuffer">The buffer to connect to the language service.</param>
        ///<param name="intellisenseGuid">The GUID of the IntellisenseProvider; used to create IVsIntellisenseProject.</param>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"),
         SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults"),
         SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public void AddIntellisenseProjectLanguage(LanguageProjectionBuffer projectionBuffer, Guid intellisenseGuid)
        {
            var contentType = projectionBuffer.IProjectionBuffer.ContentType;
            if (languageBridges.ContainsKey(contentType))
                return;

            Guid iid_vsip = typeof(IVsIntellisenseProject).GUID;

            // Needed so that webprj.dll can load
            var shell = (IVsShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsShell));
            Guid otherPackage = new Guid("{39c9c826-8ef8-4079-8c95-428f5b1c323f}");
            IVsPackage package;

            shell.LoadPackage(ref otherPackage, out package);

            var project = (IVsIntellisenseProject)WebEssentialsPackage.Instance.CreateInstance(ref intellisenseGuid, ref iid_vsip, typeof(IVsIntellisenseProject));

            string fileName = projectionBuffer.IProjectionBuffer.GetFileName();
            var displayName = contentType.DisplayName;
            if (displayName == "CSharp") displayName = "C#";
            if (displayName == "Basic") displayName = "VB";
            var hierarchy = new MarkdownCodeProject(fileName, displayName + " code in " + Path.GetFileName(fileName), WorkspaceItem.Hierarchy);

            project.Init(new ProjectHost(hierarchy));
            project.StartIntellisenseEngine();
            project.AddAssemblyReference(typeof(object).Assembly.Location);
            project.AddAssemblyReference(typeof(Uri).Assembly.Location);
            project.AddAssemblyReference(typeof(Enumerable).Assembly.Location);
            project.AddAssemblyReference(typeof(System.Net.Http.HttpClient).Assembly.Location);
            project.AddAssemblyReference(typeof(System.Net.Http.Formatting.JsonMediaTypeFormatter).Assembly.Location);
            project.AddAssemblyReference(typeof(System.Xml.Linq.XElement).Assembly.Location);
            project.AddAssemblyReference(typeof(System.Web.HttpContextBase).Assembly.Location);
            project.AddAssemblyReference(typeof(System.Windows.Forms.Form).Assembly.Location);
            project.AddAssemblyReference(typeof(System.Windows.Window).Assembly.Location);
            project.AddAssemblyReference(typeof(System.Data.DataSet).Assembly.Location);

            int needsFile;

            if (ErrorHandler.Succeeded(project.IsWebFileRequiredByProject(out needsFile)) && needsFile != 0)
                project.AddFile(fileName, MarkdownCodeProject.FileItemId);

            IVsContainedLanguageFactory factory;

            project.WaitForIntellisenseReady();
            project.GetContainedLanguageFactory(out factory);

            if (factory == null)
            {
                Logger.Log("Markdown: Couldn't create IVsContainedLanguageFactory for " + contentType);
                project.Close();
                return;
            }
            LanguageBridge bridge = new LanguageBridge(this, projectionBuffer, factory, hierarchy);
            bridge.Disposing += delegate { project.Close(); };
            languageBridges.Add(contentType, bridge);
        }

        class MarkdownCodeProject : IVsContainedLanguageProjectNameProvider, IVsHierarchy, IVsProject3
        {
            public const int FileItemId = 42775;
            readonly IVsHierarchy inner;
            private readonly string filePath;
            private readonly string caption;

            public string ProjectName { get; private set; }

            public MarkdownCodeProject(string filePath, string caption, IVsHierarchy inner)
            {
                this.caption = caption;
                this.filePath = filePath;
                this.inner = inner;
                // Make sure each language gets a unique name.
                // C# (but not VB) calls Path.GetFileName() on
                // this value, so having a slash breaks things
                ProjectName = Path.GetFileName(filePath) + Guid.NewGuid();
            }

            public int GetProjectName([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, out string pbstrProjectName)
            {
                pbstrProjectName = ProjectName;
                return 0;
            }
            public int GetMkDocument([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, out string pbstrMkDocument)
            {
                if (itemid == (uint)VSConstants.VSITEMID.Root)
                    pbstrMkDocument = ProjectName;
                else if (itemid == FileItemId)
                    pbstrMkDocument = filePath;
                else
                    throw new NotImplementedException();
                return 0;
            }

            public int IsDocumentInProject([ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszMkDocument, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int pfFound, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSDOCUMENTPRIORITY")]VSDOCUMENTPRIORITY[] pdwPriority, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]out uint pitemid)
            {
                if (pszMkDocument != filePath)
                {
                    pitemid = 0;
                    pfFound = 0;
                }
                else
                {
                    pitemid = FileItemId;
                    pfFound = 1;
                }
                return 0;
            }

            public int GetProperty([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSHPROPID")]int propid, out object pvar)
            {
                VSConstants.VSITEMID item = (VSConstants.VSITEMID)itemid;
                var prop = (__VSHPROPID)propid;
                switch (prop)
                {
                    case __VSHPROPID.VSHPROPID_Caption:
                        pvar = caption; // Shown in error list
                        return 0;
                    case __VSHPROPID.VSHPROPID_ItemSubType:
                        pvar = "";
                        return 0;

                    case __VSHPROPID.VSHPROPID_Name:
                        switch (item)
                        {
                            case VSConstants.VSITEMID.Root:
                                pvar = ProjectName;
                                return 0;
                            case (VSConstants.VSITEMID)FileItemId:
                                pvar = Path.GetFileName(filePath);  // Shown in error list
                                return 0;
                        }
                        break;
                    case __VSHPROPID.VSHPROPID_ProjectDir:
                        pvar = ProjectName;
                        return 0;

                    // Legacy:
                    case __VSHPROPID.VSHPROPID_Parent:
                        pvar = VSConstants.VSITEMID_NIL;
                        return 0;
                    case __VSHPROPID.VSHPROPID_ExtObject:
                        // Not returning this makes the native language service throw an access violation
                        return inner.GetProperty(itemid, propid, out pvar);
                    case __VSHPROPID.VSHPROPID_BrowseObject:
                        pvar = null;
                        return 0;
                    case __VSHPROPID.VSHPROPID_StateIconIndex:
                        pvar = VsStateIcon.STATEICON_EDITABLE;
                        return 0;
                    case (__VSHPROPID)__VSHPROPID4.VSHPROPID_TargetFrameworkMoniker:
                        pvar = ".NET Framework, Version=4.5";
                        return 0;
                    case (__VSHPROPID)__VSHPROPID2.VSHPROPID_IsLinkFile:
                        pvar = false;
                        return 0;
                    case __VSHPROPID.VSHPROPID_ParentHierarchy:
                        pvar = null;
                        return 0;
                }

                throw new NotImplementedException();
            }
            public int ParseCanonicalName([ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszName, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]out uint pitemid)
            {
                // Gets the ItemID for a file?
                pitemid = FileItemId;
                return 0;
            }

            public int AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")]out uint pdwCookie)
            {
                // We never change.
                pdwCookie = 42;
                return VSConstants.E_NOTIMPL;
            }
            public int UnadviseHierarchyEvents([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")]uint dwCookie)
            {
                // We never change.
                return VSConstants.E_NOTIMPL;
            }
            public int GetItemContext([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
            {
                ppSP = null;
                return VSConstants.E_NOTIMPL;
            }

            public int AddItem([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemidLoc, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDITEMOPERATION")]VSADDITEMOPERATION dwAddItemOperation, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszItemName, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint cFilesToOpen, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDRESULT")]VSADDRESULT[] pResult)
            { throw new NotImplementedException(); }
            public int AddItemWithSpecific([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemidLoc, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDITEMOPERATION")]VSADDITEMOPERATION dwAddItemOperation, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszItemName, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint cFilesToOpen, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSPECIFICEDITORFLAGS")]uint grfEditorFlags, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid rguidEditorType, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszPhysicalView, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid rguidLogicalView, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDRESULT")]VSADDRESULT[] pResult)
            { throw new NotImplementedException(); }
            public int Close() { throw new NotImplementedException(); }
            public int GenerateUniqueItemName([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemidLoc, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszExt, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszSuggestedRoot, out string pbstrItemName)
            { throw new NotImplementedException(); }
            public int GetCanonicalName([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, out string pbstrName)
            { throw new NotImplementedException(); }
            public int GetGuidProperty([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSHPROPID")]int propid, out Guid pguid)
            { throw new NotImplementedException(); }
            public int GetNestedHierarchy([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFIID")]ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]out uint pitemidNested)
            { throw new NotImplementedException(); }
            public int GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP) { throw new NotImplementedException(); }
            public int OpenItem([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
            { throw new NotImplementedException(); }
            public int OpenItemWithSpecific([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSPECIFICEDITORFLAGS")]uint grfEditorFlags, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid rguidEditorType, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszPhysicalView, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
            { throw new NotImplementedException(); }
            public int QueryClose([ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int pfCanClose)
            { throw new NotImplementedException(); }
            public int RemoveItem([ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint dwReserved, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int pfResult)
            { throw new NotImplementedException(); }
            public int ReopenItem([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid rguidEditorType, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszPhysicalView, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
            { throw new NotImplementedException(); }
            public int SetGuidProperty([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSHPROPID")]int propid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid rguid)
            { throw new NotImplementedException(); }
            public int SetProperty([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSHPROPID")]int propid, object var)
            { throw new NotImplementedException(); }
            public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp) { throw new NotImplementedException(); }
            public int TransferItem([ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszMkDocumentOld, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")]string pszMkDocumentNew, IVsWindowFrame punkWindowFrame)
            { throw new NotImplementedException(); }
            public int Unused0() { throw new NotImplementedException(); }
            public int Unused1() { throw new NotImplementedException(); }
            public int Unused2() { throw new NotImplementedException(); }
            public int Unused3() { throw new NotImplementedException(); }
            public int Unused4() { throw new NotImplementedException(); }
        }

        class ProjectHost : IVsIntellisenseProjectHost
        {
            readonly MarkdownCodeProject hierarchy;
            public ProjectHost(MarkdownCodeProject hierarchy)
            {
                this.hierarchy = hierarchy;
            }

            public int CreateFileCodeModel(string pszFilename, out object ppCodeModel)
            {
                ppCodeModel = null;
                return VSConstants.E_NOTIMPL;
            }

            public int GetCompilerOptions(out string pbstrOptions)
            {
                pbstrOptions = "";
                return 0;
            }

            public int GetHostProperty(uint dwPropID, out object pvar)
            {
                var prop = (HOSTPROPID)dwPropID;
                // Based on decompiled Microsoft.VisualStudio.ProjectSystem.VS.Implementation.Designers.LanguageServiceBase
                switch (prop)
                {
                    case HOSTPROPID.HOSTPROPID_PROJECTNAME:
                    case HOSTPROPID.HOSTPROPID_RELURL:
                        pvar = hierarchy.ProjectName;
                        return VSConstants.S_OK;
                    case HOSTPROPID.HOSTPROPID_HIERARCHY:
                        pvar = hierarchy;
                        return VSConstants.S_OK;
                    case HOSTPROPID.HOSTPROPID_INTELLISENSECACHE_FILENAME:
                        pvar = Path.GetTempPath();
                        return VSConstants.S_OK;
                    case (HOSTPROPID)(-2):
                        pvar = ".NET Framework, Version=4.5";   // configurationGeneral.TargetFrameworkMoniker.GetEvaluatedValueAtEndAsync
                        return VSConstants.S_OK;
                    case (HOSTPROPID)(-1):
                        pvar = false;   // SuppressShadowWarnings; probably an ugly hack for ASPX
                        return VSConstants.S_OK;

                    default:
                        pvar = null;
                        return VSConstants.E_INVALIDARG;
                }
            }

            public int GetOutputAssembly(out string pbstrOutputAssembly)
            {
                pbstrOutputAssembly = "MarkdownHostAssembly";
                return 0;
            }
        }

        #region Cleanup
        private void OnDocumentClosing(object sender, EventArgs e)
        {
            foreach (var b in languageBridges.Values)
                b.ClearBufferCoordinator();

            var service = ServiceManager.GetService<ContainedLanguageAdapter>(Document.TextBuffer);
            if (service != null)
            {
                ServiceManager.RemoveService<ContainedLanguageAdapter>(Document.TextBuffer);
                service.Dispose();
            }
            Document.OnDocumentClosing -= OnDocumentClosing;
            Document = null;
        }
        public void Dispose()
        {
            foreach (var b in languageBridges.Values)
                b.Dispose();
        }
        #endregion
    }


    // Don't ask
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    [Export(typeof(IWpfTextViewCreationListener))]
    [Export(typeof(IVsTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class VenusAdapterAdapterHack : IWpfTextViewCreationListener, IVsTextViewCreationListener
    {
        readonly IWpfTextViewCreationListener innerWpf;
        readonly IVsTextViewCreationListener innerVs;

        static readonly Type roslynType = Type.GetType("Roslyn.VisualStudio.Services.Implementation.Venus.VenusTextViewManager, Microsoft.VisualStudio.LanguageServices");

        public VenusAdapterAdapterHack()
        {
            if (roslynType == null)
                return;
            var e = WebEditor.ExportProvider.GetExports(roslynType, typeof(object), null).SingleOrDefault();
            if (e == null)
            {
                Logger.Log("Roslyn's Venus exports have changed.  Please email Schabse (Markdown@SLaks.net) and mention your installed Roslyn version");
                return;
            }
            innerWpf = e.Value as IWpfTextViewCreationListener;
            innerVs = e.Value as IVsTextViewCreationListener;

            if (innerWpf == null || innerVs == null)
                Logger.Log("Roslyn's Venus base types have changed.  Please email Schabse (Markdown@SLaks.net) and mention your installed Roslyn version");
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (innerWpf != null) innerWpf.TextViewCreated(textView);
        }
        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            if (innerVs != null) innerVs.VsTextViewCreated(textViewAdapter);
        }
    }
}
