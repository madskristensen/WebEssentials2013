using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Handlebars
{
    internal class HandlebarsMenu
    {
        private readonly OleMenuCommandService _mcs;
        private readonly DTE2 _dte;
        private readonly ISet<string> _extensions;
        private readonly IContentType _contentType;

        [Import]
        public IContentTypeRegistryService ContentTypes { get; set; }
        [Import]
        public IFileExtensionRegistryService FileExtensionRegistry { get; set; }

        public HandlebarsMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            Mef.SatisfyImportsOnce(this);
            _contentType = ContentTypes.GetContentType("Handlebars");

            if (_contentType != null)
                _extensions = FileExtensionRegistry.GetFileExtensionSet(_contentType);

            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID compile = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.HandlebarsCompile);
            OleMenuCommand compileCommand = new OleMenuCommand((s, e) => AddHtmlFiles(), compile);
            compileCommand.BeforeQueryStatus += IsHandlebarsFile;
            _mcs.AddCommand(compileCommand);
        }

        private void IsHandlebarsFile(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            var paths = ProjectHelpers.GetSelectedItemPaths(_dte);

            menuCommand.Enabled = paths.Any() && paths.All(p => _extensions.Contains(Path.GetExtension(p)));
        }

        private void AddHtmlFiles()
        {
            var paths = ProjectHelpers.GetSelectedItemPaths(_dte);
            var contentType = ContentTypeManager.GetContentType("Handlebars");
            var compiler = Mef.GetImport<ICompilerRunnerProvider>(contentType).GetCompiler(contentType);
            Parallel.ForEach(paths, f => compiler.CompileToDefaultOutputAsync(f).DoNotWait("compiling " + f));
        }
    }
}
