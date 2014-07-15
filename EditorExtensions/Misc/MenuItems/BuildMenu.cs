using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Compilers;
using MadsKristensen.EditorExtensions.Images;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using MadsKristensen.EditorExtensions.SweetJs;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    internal class BuildMenu
    {
        private readonly DTE2 _dte;
        private readonly OleMenuCommandService _mcs;

        [Import]
        public IContentTypeRegistryService ContentTypes { get; set; }
        [Import]
        public IFileExtensionRegistryService FileExtensionRegistry { get; set; }
        [Import]
        public ProjectCompiler Compiler { get; set; }

        public BuildMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            Mef.SatisfyImportsOnce(this);
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            AddCommand(CommandId.BuildLess, ContentTypes.GetContentType(LessContentTypeDefinition.LessContentType));
            AddCommand(CommandId.BuildSass, ContentTypes.GetContentType(ScssContentTypeDefinition.ScssContentType));
            AddCommand(CommandId.BuildCoffeeScript, ContentTypes.GetContentType(CoffeeContentTypeDefinition.CoffeeContentType));
            AddCommand(CommandId.BuildSweetJs, ContentTypes.GetContentType(SweetJsContentTypeDefinition.SweetJsContentType));
            //TODO: Iced CoffeeScript?

            CommandID cmdBundles = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildBundles);
            OleMenuCommand menuBundles = new OleMenuCommand((s, e) => UpdateBundleFiles().DoNotWait("Web Essentials: Updating Bundles..."), cmdBundles);
            _mcs.AddCommand(menuBundles);

            CommandID cmdSprites = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildSprites);
            OleMenuCommand menuSprites = new OleMenuCommand((s, e) => UpdateSpriteFiles().DoNotWait("Web Essentials: Updating Sprites..."), cmdSprites);
            _mcs.AddCommand(menuSprites);

            CommandID cmdMinify = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildMinify);
            OleMenuCommand menuMinify = new OleMenuCommand((s, e) => Minify().DoNotWait("Web Essentials: Minifying files..."), cmdMinify);
            _mcs.AddCommand(menuMinify);
        }

        private void AddCommand(CommandId id, IContentType contentType)
        {
            var cid = new CommandID(CommandGuids.guidBuildCmdSet, (int)id);
            var command = new OleMenuCommand(async (s, e) =>
            {
                WebEssentialsPackage.DTE.StatusBar.Text = "Compiling " + contentType + "...";
                await Task.Run(() => Compiler.CompileSolutionAsync(contentType));
                WebEssentialsPackage.DTE.StatusBar.Clear();
            }, cid);
            _mcs.AddCommand(command);
        }

        private async static Task UpdateBundleFiles()
        {
            await BundleFilesMenu.UpdateAllBundlesAsync();
        }

        private async static Task UpdateSpriteFiles()
        {
            await SpriteImageMenu.UpdateAllSpritesAsync();
        }

        private async Task Minify()
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

            await Task.WhenAll(files.AsParallel().Select(async file =>
               await MinificationSaveListener.ReMinify(
                         FileExtensionRegistry.GetContentTypeForExtension(Path.GetExtension(file).TrimStart('.')),
                         file,
                         false
                     )));

            WebEssentialsPackage.DTE.StatusBar.Clear();
        }
    }
}
