using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

using Microsoft.Html.Editor;
using Microsoft.Html.Editor.Projection;


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


namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    // Abandon all hope ye who enters here.
    // https://twitter.com/Schabse/status/393092191356076032
    // https://twitter.com/jasonmalinowski/status/393094145398407168

    // Based on decompiled code from Microsoft.VisualStudio.Html.ContainedLanguage.Server
    // Thanks to Jason Malinowski for helping me navigate this mess.
    // All of this can go away when the Roslyn editor ships.


    class ContainedLanguageAdapter
    {
        public static string ExtensionFromContentType(IContentType contentType)
        {
            IFileExtensionRegistryService value = WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>().Value;
            return value.GetExtensionsForContentType(contentType).FirstOrDefault();
        }
        public static Guid LanguageServiceFromContentType(IContentType contentType)
        {
            string extension = ExtensionFromContentType(contentType);
            if (extension == null)
                return Guid.Empty;

            Guid retVal;
            IVsTextManager globalService = Globals.GetGlobalService<IVsTextManager>(typeof(SVsTextManager));
            globalService.MapFilenameToLanguageSID("file." + extension, out retVal);
            return retVal;
        }

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
            Document.OnDocumentClosing += this.OnDocumentClosing;

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

            public LanguageBridge(ContainedLanguageAdapter owner, LanguageProjectionBuffer projectionBuffer, IVsContainedLanguageFactory languageFactory)
            {
                this.owner = owner;
                this.languageFactory = languageFactory;
                this.ProjectionBuffer = projectionBuffer;
                InitContainedLanguage();
            }

            public IVsContainedLanguageHost GetLegacyContainedLanguageHost()
            {
                if (this._containedLanguageHost == null)
                    this._containedLanguageHost = new VsLegacyContainedLanguageHost(owner.Document, ProjectionBuffer);
                return this._containedLanguageHost;
            }
            private void InitContainedLanguage()
            {
                IVsTextLines vsTextLines = this.EnsureBufferCoordinator();
                IVsContainedLanguage vsContainedLanguage;

                int hr = languageFactory.GetLanguage(owner.WorkspaceItem.Hierarchy, (uint)owner.WorkspaceItem.ItemId, this._textBufferCoordinator, out vsContainedLanguage);
                if (vsContainedLanguage == null)
                {
                    Logger.Log("Markdown: Couldn't get IVsContainedLanguage for " + ProjectionBuffer.IProjectionBuffer.ContentType);
                    return;
                }

                Guid langService;
                vsContainedLanguage.GetLanguageServiceID(out langService);
                vsTextLines.SetLanguageServiceID(ref langService);

                containedLanguage = vsContainedLanguage;
                IVsContainedLanguageHost legacyContainedLanguageHost = this.GetLegacyContainedLanguageHost();
                vsContainedLanguage.SetHost(legacyContainedLanguageHost);
                this._legacyCommandTarget = new LegacyContainedLanguageCommandTarget();

                IVsTextViewFilter textViewFilter;
                this._legacyCommandTarget.Create(owner.Document, vsContainedLanguage, this._textBufferCoordinator, ProjectionBuffer, out textViewFilter);
                IWebContainedLanguageHost webContainedLanguageHost = legacyContainedLanguageHost as IWebContainedLanguageHost;
                webContainedLanguageHost.SetContainedCommandTarget(this._legacyCommandTarget.TextView, this._legacyCommandTarget.ContainedLanguageTarget);
                containedLanguage2 = (webContainedLanguageHost as IContainedLanguageHostVs);
                containedLanguage2.TextViewFilter = textViewFilter;

                ProjectionBuffer.ResetMappings();

                WebEditor.TraceEvent(1005);
            }

            private IVsTextLines EnsureBufferCoordinator()
            {
                if (this._secondaryBuffer != null)
                    return this._secondaryBuffer;

                var vsTextBuffer = owner.Document.TextBuffer.QueryInterface<IVsTextBuffer>();

                IVsEditorAdaptersFactoryService adapterFactory = WebEditor.ExportProvider.GetExport<IVsEditorAdaptersFactoryService>().Value;
                this._secondaryBuffer = (adapterFactory.GetBufferAdapter(ProjectionBuffer.IProjectionBuffer) as IVsTextLines);
                if (this._secondaryBuffer == null)
                {
                    this._secondaryBuffer = (adapterFactory.CreateVsTextBufferAdapterForSecondaryBuffer(vsTextBuffer.GetServiceProvider(), ProjectionBuffer.IProjectionBuffer) as IVsTextLines);
                }

                this._secondaryBuffer.SetTextBufferData(VSConstants.VsTextBufferUserDataGuid.VsBufferDetectLangSID_guid, false);
                this._secondaryBuffer.SetTextBufferData(VSConstants.VsTextBufferUserDataGuid.VsBufferMoniker_guid, owner.WorkspaceItem.PhysicalPath);

                IOleUndoManager oleUndoManager;
                this._secondaryBuffer.GetUndoManager(out oleUndoManager);
                oleUndoManager.Enable(0);

                this._textBufferCoordinator = adapterFactory.CreateVsTextBufferCoordinatorAdapter();
                vsTextBuffer.SetTextBufferData(HtmlConstants.SID_SBufferCoordinatorServerLanguage, this._textBufferCoordinator);
                vsTextBuffer.SetTextBufferData(typeof(VsTextBufferCoordinatorClass).GUID, this._textBufferCoordinator);

                this._textBufferCoordinator.SetBuffers(vsTextBuffer as IVsTextLines, this._secondaryBuffer);

                return this._secondaryBuffer;
            }
            public void ClearBufferCoordinator()
            {
                IVsTextBuffer vsTextBuffer = owner.Document.TextBuffer.QueryInterface<IVsTextBuffer>();
                vsTextBuffer.SetTextBufferData(HtmlConstants.SID_SBufferCoordinatorServerLanguage, null);
                vsTextBuffer.SetTextBufferData(typeof(VsTextBufferCoordinatorClass).GUID, null);
            }

            public void Dispose()
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

                if (Disposing != null)
                    Disposing(this, EventArgs.Empty);
            }

            public event EventHandler Disposing;
        }


        IWebApplicationCtxSvc WebApplicationContextService
        {
            get { return ServiceProvider.GlobalProvider.GetService(typeof(SWebApplicationCtxSvc)) as IWebApplicationCtxSvc; }
        }

        ///<summary>Creates a ContainedLanguage for the specified ProjectionBuffer, using an IVsIntellisenseProjectManager to initialize the language.</summary>
        ///<param name="projectionBuffer">The buffer to connect to the language service.</param>
        ///<param name="intelliSenseGuid">The GUID of the IntellisenseProvider; used to create IVsIntellisenseProject.</param>
        public void AddIntellisenseProjectLanguage(LanguageProjectionBuffer projectionBuffer, Guid intelliSenseGuid)
        {
            var contentType = projectionBuffer.IProjectionBuffer.ContentType;
            if (languageBridges.ContainsKey(contentType))
                return;
            int hr;

            Guid iid_vsip = typeof(IVsIntellisenseProject).GUID;

            var shell = (IVsShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsShell));
            Guid otherPackage = new Guid("{39c9c826-8ef8-4079-8c95-428f5b1c323f}");
            IVsPackage package;
            hr = shell.LoadPackage(ref otherPackage, out package);

            var project = (IVsIntellisenseProject)EditorExtensionsPackage.Instance.CreateInstance(ref intelliSenseGuid, ref iid_vsip, typeof(IVsIntellisenseProject));

            string projectPath;
            WorkspaceItem.FileItemContext.GetWebRootPath(out projectPath);
            hr = project.Init(new ProjectHost(WorkspaceItem.Hierarchy, projectPath));
            hr = project.StartIntellisenseEngine();
            hr = project.WaitForIntellisenseReady();
            //hr = project.ResumePostedNotifications();
            hr = project.AddAssemblyReference(typeof(object).Assembly.Location);
            hr = project.AddAssemblyReference(typeof(Uri).Assembly.Location);
            hr = project.AddAssemblyReference(typeof(Enumerable).Assembly.Location);
            hr = project.AddAssemblyReference(typeof(System.Xml.Linq.XElement).Assembly.Location);
            hr = project.AddAssemblyReference(typeof(System.Web.HttpContextBase).Assembly.Location);
            hr = project.AddAssemblyReference(typeof(System.Windows.Forms.Form).Assembly.Location);
            hr = project.AddAssemblyReference(typeof(System.Windows.Window).Assembly.Location);
            hr = project.AddAssemblyReference(typeof(System.Data.DataSet).Assembly.Location);

            int needsFile;
            project.IsWebFileRequiredByProject(out needsFile);
            if (needsFile != 0)
                project.AddFile(projectionBuffer.IProjectionBuffer.GetFileName(), (uint)WorkspaceItem.ItemId);

            hr = project.WaitForIntellisenseReady();
            IVsContainedLanguageFactory factory;
            hr = project.GetContainedLanguageFactory(out factory);
            if (factory == null)
            {
                Logger.Log("Markdown: Couldn't create IVsContainedLanguageFactory for " + contentType);
                project.Close();
                return;
            }
            LanguageBridge bridge = new LanguageBridge(this, projectionBuffer, factory);
            bridge.Disposing += delegate { project.Close(); };
            languageBridges.Add(contentType, bridge);
        }

        class ProjectHost : IVsIntellisenseProjectHost
        {
            readonly IVsHierarchy hierarchy;
            readonly string projectPath;
            public ProjectHost(IVsHierarchy hierarchy, string projectPath)
            {
                this.hierarchy = hierarchy;
                this.projectPath = projectPath + Guid.NewGuid();
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
                        pvar = projectPath;
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
                        pvar = false;   // No clue...
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

            var service = ServiceManager.GetService<ContainedLanguageAdapter>(this.Document.TextBuffer);
            if (service != null)
            {
                ServiceManager.RemoveService<ContainedLanguageAdapter>(this.Document.TextBuffer);
                service.Dispose();
            }
            Document.OnDocumentClosing -= this.OnDocumentClosing;
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
