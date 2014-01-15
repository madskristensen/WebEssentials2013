using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    internal class BuildMenu
    {
        private static DTE2 _dte;
        private OleMenuCommandService _mcs;

        public BuildMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID cmdBundles = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildBundles);
            OleMenuCommand menuBundles = new OleMenuCommand((s, e) => UpdateBundleFiles(), cmdBundles);
            _mcs.AddCommand(menuBundles);

            CommandID cmdLess = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildLess);
            OleMenuCommand menuLess = new OleMenuCommand(async (s, e) => await BuildLess(), cmdLess);
            _mcs.AddCommand(menuLess);

            CommandID cmdSass = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildSass);
            OleMenuCommand menuSass = new OleMenuCommand(async (s, e) => await BuildSass(), cmdSass);
            _mcs.AddCommand(menuSass);

            CommandID cmdMinify = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildMinify);
            OleMenuCommand menuMinify = new OleMenuCommand((s, e) => Task.Run(new Action(Minify)), cmdMinify);
            _mcs.AddCommand(menuMinify);

            CommandID cmdCoffee = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildCoffeeScript);
            OleMenuCommand menuCoffee = new OleMenuCommand(async (s, e) => await BuildCoffeeScript(), cmdCoffee);
            _mcs.AddCommand(menuCoffee);
        }

        public async static Task BuildCoffeeScript()
        {
            EditorExtensionsPackage.DTE.StatusBar.Text = "Compiling CofeeScript...";

            var compilers = new[] { new CoffeeScriptProjectCompiler(), new IcedCoffeeScriptProjectCompiler() };
            await Task.WhenAll(
                ProjectHelpers.GetAllProjects()
                              .SelectMany(p => compilers.Select(c => c.CompileProject(p)))
            );

            EditorExtensionsPackage.DTE.StatusBar.Clear();
        }

        public async static Task BuildLess()
        {
            EditorExtensionsPackage.DTE.StatusBar.Text = "Compiling LESS...";
            await Task.WhenAll(
                ProjectHelpers.GetAllProjects()
                              .Select(new LessProjectCompiler().CompileProject)
            );
            EditorExtensionsPackage.DTE.StatusBar.Clear();
        }

        public async static Task BuildSass()
        {
            EditorExtensionsPackage.DTE.StatusBar.Text = "Compiling SASS...";
            await Task.WhenAll(
                ProjectHelpers.GetAllProjects()
                              .Select(new SassProjectCompiler().CompileProject)
            );
            EditorExtensionsPackage.DTE.StatusBar.Clear();
        }

        public static void UpdateBundleFiles()
        {
            EditorExtensionsPackage.DTE.StatusBar.Text = "Updating bundles...";
            BundleFilesMenu.UpdateBundles(null, true);
            EditorExtensionsPackage.DTE.StatusBar.Clear();
        }

        private static void Minify()
        {
            _dte.StatusBar.Text = "Web Essentials: Minifying files...";
            var extensions = new HashSet<string>(
                Mef.GetSupportedExtensions<IFileMinifier>(),
                StringComparer.OrdinalIgnoreCase
            );

            var files = ProjectHelpers.GetAllProjects()
                            .Select(ProjectHelpers.GetRootFolder)
                            .SelectMany(p => Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                            .Where(f => extensions.Contains(Path.GetExtension(f)));

            var extensionService = WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>();
            var minifyService = WebEditor.ExportProvider.GetExport<MinificationSaveListener>();

            // Perform expensive blocking work in parallel
            Parallel.ForEach(files, file =>
                minifyService.Value.ReMinify(
                    extensionService.Value.GetContentTypeForExtension(Path.GetExtension(file).TrimStart('.')),
                    file
                )
            );

            _dte.StatusBar.Text = "Web Essentials: Files minified";
        }
    }
}