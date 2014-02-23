using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using MadsKristensen.EditorExtensions.BrowserLink.PixelPushing;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.Editor;
using ThreadingTask = System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(CommandGuids.guidEditorExtensionsPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideOptionPage(typeof(Settings.GeneralOptions), "Web Essentials", "General", 101, 101, true, new[] { "ZenCoding", "Mustache", "Handlebars", "Comments", "Bundling", "Bundle" })]
    [ProvideOptionPage(typeof(Settings.CssOptions), "Web Essentials", "CSS", 101, 102, true, new[] { "Minify", "Minification", "W3C", "CSS3" })]
    [ProvideOptionPage(typeof(Settings.LessOptions), "Web Essentials", "LESS", 101, 105, true, new[] { "LESS", "Complier", "Minification", "Minify" })]
    [ProvideOptionPage(typeof(Settings.SassOptions), "Web Essentials", "SASS", 101, 113, true, new[] { "SASS", "Complier", "Minification", "Minify" })]
    [ProvideOptionPage(typeof(Settings.CoffeeScriptOptions), "Web Essentials", "CoffeeScript", 101, 106, true, new[] { "Iced", "JavaScript", "JS", "JScript" })]
    [ProvideOptionPage(typeof(Settings.JavaScriptOptions), "Web Essentials", "JavaScript", 101, 107, true, new[] { "JScript", "JS", "Minify", "Minification", "EcmaScript" })]
    [ProvideOptionPage(typeof(Settings.BrowserLinkOptions), "Web Essentials", "Browser Link", 101, 108, true, new[] { "HTML menu", "BrowserLink" })]
    [ProvideOptionPage(typeof(Settings.MarkdownOptions), "Web Essentials", "Markdown", 101, 109, true, new[] { "markdown", "Markdown", "md" })]
    [ProvideOptionPage(typeof(Settings.CodeGenOptions), "Web Essentials", "Code Generation", 101, 210, true, new[] { "CodeGeneration", "codeGeneration" })]
    [ProvideOptionPage(typeof(Settings.TypeScriptOptions), "Web Essentials", "TypeScript", 101, 210, true, new[] { "TypeScript", "TS" })]
    [ProvideOptionPage(typeof(Settings.HtmlOptions), "Web Essentials", "HTML", 101, 111, true, new[] { "html", "angular", "xhtml" })]
    [ProvideOptionPage(typeof(Settings.SweetJsOptions), "Web Essentials", "Sweet.js", 101, 111, true, new[] { "Sweet", "SJS", "Sweet.js" })]
    public sealed class EditorExtensionsPackage : Package
    {
        private static DTE2 _dte;
        private static IVsRegisterPriorityCommandTarget _pct;
        private OleMenuCommand _topMenu;

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void Initialize()
        {
            base.Initialize();

            Instance = this;

            SettingsStore.Load();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (null != mcs)
            {
                TransformMenu transform = new TransformMenu(DTE, mcs);
                DiffMenu diffMenu = new DiffMenu(mcs);
                MinifyFileMenu minifyMenu = new MinifyFileMenu(mcs);
                BundleFilesMenu bundleMenu = new BundleFilesMenu(DTE, mcs);
                JsHintMenu jsHintMenu = new JsHintMenu(DTE, mcs);
                TsLintMenu tsLintMenu = new TsLintMenu(DTE, mcs);
                CoffeeLintMenu coffeeLintMenu = new CoffeeLintMenu(DTE, mcs);
                JsCodeStyle jsCodeStyleMenu = new JsCodeStyle(DTE, mcs);
                ProjectSettingsMenu projectSettingsMenu = new ProjectSettingsMenu(DTE, mcs);
                SolutionColorsMenu solutionColorsMenu = new SolutionColorsMenu(mcs);
                BuildMenu buildMenu = new BuildMenu(DTE, mcs);
                MarkdownMenu markdownMenu = new MarkdownMenu(DTE, mcs);
                AddIntellisenseFileMenu intellisenseFile = new AddIntellisenseFileMenu(DTE, mcs);
                UnusedCssMenu unusedCssMenu = new UnusedCssMenu(mcs);
                PixelPushingMenu pixelPushingMenu = new PixelPushingMenu(mcs);
                ReferenceJsMenu referenceJsMenu = new ReferenceJsMenu(mcs);
                CompressImageMenu compressImageMenu = new CompressImageMenu(mcs);
                SpriteImageMenu spriteImageMenu = new SpriteImageMenu(DTE, mcs);
                //ChainCompilationMenu chainCompilationMenu = new ChainCompilationMenu(DTE, mcs);

                HandleMenuVisibility(mcs);
                referenceJsMenu.SetupCommands();
                pixelPushingMenu.SetupCommands();
                unusedCssMenu.SetupCommands();
                intellisenseFile.SetupCommands();
                markdownMenu.SetupCommands();
                buildMenu.SetupCommands();
                solutionColorsMenu.SetupCommands();
                projectSettingsMenu.SetupCommands();
                jsHintMenu.SetupCommands();
                tsLintMenu.SetupCommands();
                coffeeLintMenu.SetupCommands();
                jsCodeStyleMenu.SetupCommands();
                bundleMenu.SetupCommands();
                minifyMenu.SetupCommands();
                diffMenu.SetupCommands();
                transform.SetupCommands();
                compressImageMenu.SetupCommands();
                spriteImageMenu.SetupCommands();
                //chainCompilationMenu.SetupCommands();
            }

            IconRegistration.RegisterIcons();

            // Hook up event handlers
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                DTE.Events.BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
                DTE.Events.SolutionEvents.Opened += delegate { SettingsStore.Load(); ShowTopMenu(); };
                DTE.Events.SolutionEvents.AfterClosing += delegate { DTE.StatusBar.Clear(); ShowTopMenu(); };

            }), DispatcherPriority.ApplicationIdle, null);
        }

        private void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            bool success = _dte.Solution.SolutionBuild.LastBuildInfo == 0;
            if (!success)
            {
                string text = _dte.StatusBar.Text; // respect localization of "Build failed"
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    _dte.StatusBar.Text = text;
                }), DispatcherPriority.ApplicationIdle, null);

                return;
            }

            if (Action != vsBuildAction.vsBuildActionClean)
            {
                var compiler = WebEditor.Host.ExportProvider.GetExport<ProjectCompiler>();
                ThreadingTask.Task.Run(() =>
                {
                    Parallel.ForEach(
                        Mef.GetSupportedContentTypes<ICompilerRunnerProvider>()
                           .Where(c => WESettings.Instance.ForContentType<ICompilerInvocationSettings>(c).CompileOnBuild),
                        c => compiler.Value.CompileSolutionAsync(c).DontWait("compiling solution-wide " + c.DisplayName)
                    );
                    BuildMenu.UpdateBundleFiles();
                }).DontWait("running solution-wide compilers");

                if (WESettings.Instance.JavaScript.LintOnBuild)
                {
                    LintFileInvoker.RunOnAllFilesInProjectAsync(new[] { "*.js" }, f => new JavaScriptLintReporter(new JsHintCompiler(), f))
                        .DontWait("running solution-wide JSHint");
                    LintFileInvoker.RunOnAllFilesInProjectAsync(new[] { "*.js" }, f => new JavaScriptLintReporter(new JsCodeStyleCompiler(), f))
                        .DontWait("running solution-wide JSCS");
                }

                if (WESettings.Instance.TypeScript.LintOnBuild)
                    LintFileInvoker.RunOnAllFilesInProjectAsync(new[] { "*.ts" }, f => new LintReporter(new TsLintCompiler(), WESettings.Instance.TypeScript, f))
                        .DontWait("running solution-wide TSLint");

                if (WESettings.Instance.CoffeeScript.LintOnBuild)
                    LintFileInvoker.RunOnAllFilesInProjectAsync(new[] { "*.coffee", "*.iced" }, f => new LintReporter(new CoffeeLintCompiler(), WESettings.Instance.CoffeeScript, f))
                        .DontWait("running solution-wide CoffeeLint");
            }
            else if (Action == vsBuildAction.vsBuildActionClean)
            {
                LintReporter.Reset();
            }
        }

        public static void ExecuteCommand(string commandName, string commandArgs = "")
        {
            var command = EditorExtensionsPackage.DTE.Commands.Item(commandName);

            if (command.IsAvailable)
            {
                try
                {
                    EditorExtensionsPackage.DTE.ExecuteCommand(commandName, commandArgs);
                }
                catch
                { }
            }
        }

        private void HandleMenuVisibility(OleMenuCommandService mcs)
        {
            CommandID commandId = new CommandID(CommandGuids.guidCssIntellisenseCmdSet, (int)CommandId.CssIntellisenseSubMenu);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => { }, commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            mcs.AddCommand(menuCommand);

            CommandID cmdTopMenu = new CommandID(CommandGuids.guidTopMenu, (int)CommandId.TopMenu);
            _topMenu = new OleMenuCommand((s, e) => { }, cmdTopMenu);
            mcs.AddCommand(_topMenu);
        }

        private void ShowTopMenu()
        {
            _topMenu.Visible = _dte.Solution != null && !string.IsNullOrEmpty(_dte.Solution.FullName);
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

        ///<summary>Opens an Undo context, and returns an IDisposable that will close the context when disposed.</summary>
        ///<remarks>Use this method in a using() block to make sure that exceptions don't break Undo.</remarks>
        public static IDisposable UndoContext(string name)
        {
            EditorExtensionsPackage.DTE.UndoContext.Open(name);

            return new Disposable(DTE.UndoContext.Close);
        }
    }
}
