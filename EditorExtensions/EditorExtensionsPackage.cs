using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    // The key for registering option pages in Text Editors -> CSS
    //HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\11.0_Config\Languages\Language Services\CSS\EditorToolsOptions\Format


    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidEditorExtensionsPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideOptionPage(typeof(GeneralOptions), "Web Essentials", "General", 101, 101, true, new[] { "ZenCoding", "Mustache", "Handlebars", "Comments", "Bundling", "Bundle" })]
    [ProvideOptionPage(typeof(CssOptions), "Web Essentials", "CSS", 101, 102, true, new[] { "Minify", "Minification", "W3C", "CSS3" })]
    [ProvideOptionPage(typeof(JsHintOptions), "Web Essentials", "JSHint", 101, 103, true, new[] { "JSLint", "Lint" })]
    [ProvideOptionPage(typeof(LessOptions), "Web Essentials", "LESS", 101, 105, true)]
    [ProvideOptionPage(typeof(CoffeeScriptOptions), "Web Essentials", "CoffeeScript", 101, 106, true, new[] { "Iced", "JavaScript", "JS", "JScript" })]
    [ProvideOptionPage(typeof(JavaScriptOptions), "Web Essentials", "JavaScript", 101, 107, true, new[] { "JScript", "JS", "Minify", "Minification", "EcmaScript" })]
    [ProvideSearchProvider(typeof(VSSearchProvider), "VS Gallery Search")]
    public sealed class EditorExtensionsPackage : ExtensionPointPackage
    {
        private static DTE2 _dte;
        private static IVsRegisterPriorityCommandTarget _pct;

        internal static DTE2 DTE
        {
            get
            {
                if (_dte == null)
                    _dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;

                return _dte;
            }
        }

        internal static IVsRegisterPriorityCommandTarget PriorityCommandTarget
        {
            get
            {
                if (_pct == null)
                    _pct = ServiceProvider.GlobalProvider.GetService(typeof(SVsRegisterPriorityCommandTarget)) as IVsRegisterPriorityCommandTarget;

                return _pct;
            }
        }

        public static EditorExtensionsPackage Instance { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();
            Instance = this;
            JsDocComments.Register();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                HandleMenuVisibility(mcs);

                TransformMenu transform = new TransformMenu(DTE, mcs);
                transform.SetupCommands();

                DiffMenu diffMenu = new DiffMenu(DTE, mcs);
                diffMenu.SetupCommands();

                MinifyFileMenu minifyMenu = new MinifyFileMenu(DTE, mcs);
                minifyMenu.SetupCommands();

                BundleFilesMenu bundleMenu = new BundleFilesMenu(DTE, mcs);
                bundleMenu.SetupCommands();

                JsHintMenu jsHintMenu = new JsHintMenu(DTE, mcs);
                jsHintMenu.SetupCommands();

                ProjectSettingsMenu projectSettingsMenu = new ProjectSettingsMenu(DTE, mcs);
                projectSettingsMenu.SetupCommands();

                SolutionColorsMenu solutionColorsMenu = new SolutionColorsMenu(DTE, mcs);
                solutionColorsMenu.SetupCommands();

                BuildMenu buildMenu = new BuildMenu(DTE, mcs);
                buildMenu.SetupCommands();

                MarkdownStylesheetMenu markdownMenu = new MarkdownStylesheetMenu(DTE, mcs);
                markdownMenu.SetupCommands();
            }

            // Hook up event handlers
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
           {
               DTE.Events.BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
               DTE.Events.SolutionEvents.Opened += delegate { Settings.UpdateCache(); Settings.UpdateStatusBar("applied"); };
               DTE.Events.SolutionEvents.AfterClosing += delegate { DTE.StatusBar.Clear(); };

           }), DispatcherPriority.ApplicationIdle, null);
        }

        private void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            if (Action != vsBuildAction.vsBuildActionClean)
            {
                if (WESettings.GetBoolean(WESettings.Keys.LessCompileOnBuild))
                    _dte.Commands.Raise(GuidList.guidBuildCmdSetString, (int)PkgCmdIDList.cmdBuildLess, null, null);

                if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptCompileOnBuild))
                    _dte.Commands.Raise(GuidList.guidBuildCmdSetString, (int)PkgCmdIDList.cmdBuildCoffeeScript, null, null);

                _dte.Commands.Raise(GuidList.guidBuildCmdSetString, (int)PkgCmdIDList.cmdBuildBundles, null, null);

                if (WESettings.GetBoolean(WESettings.Keys.RunJsHintOnBuild))
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(
                        new Action(() => JsHintProjectRunner.RunOnAllFilesInProject()), DispatcherPriority.ApplicationIdle, null);
                }
            }
            else if (Action == vsBuildAction.vsBuildActionClean)
            {
                System.Threading.Tasks.Task.Run(() => JsHintRunner.Reset());
            }
        }

        public static void ExecuteCommand(string commandName)
        {
            var command = EditorExtensionsPackage.DTE.Commands.Item(commandName);
            if (command.IsAvailable)
            {
                EditorExtensionsPackage.DTE.ExecuteCommand(commandName);
            }
        }

        private void HandleMenuVisibility(OleMenuCommandService mcs)
        {
            CommandID commandId = new CommandID(GuidList.guidCssIntellisenseCmdSet, (int)PkgCmdIDList.CssIntellisenseSubMenu);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => { }, commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            mcs.AddCommand(menuCommand);
        }

        private readonly string[] _supported = new[] { "CSS", "LESS", "SCSS", "JAVASCRIPT", "PROJECTION", "TYPESCRIPT", "MARKDOWN" };

        void menuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menu = (OleMenuCommand)sender;
            var buffer = ProjectHelpers.GetCurentTextBuffer();

            menu.Visible = buffer != null && _supported.Contains(buffer.ContentType.DisplayName.ToUpperInvariant());
        }

        public static T GetGlobalService<T>(Type type = null) where T : class
        {
            return Microsoft.VisualStudio.Shell.Package.GetGlobalService(type ?? typeof(T)) as T;
        }

        public static IComponentModel ComponentModel
        {
            get { return GetGlobalService<IComponentModel>(typeof(SComponentModel)); }
        }
    }
}
