﻿using System.Collections.Generic;
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

namespace MadsKristensen.EditorExtensions.Markdown
{
    internal class MarkdownMenu
    {
        private readonly OleMenuCommandService _mcs;
        private readonly DTE2 _dte;
        private readonly ISet<string> _extensions;
        private readonly IContentType _contentType;

        [Import]
        public IContentTypeRegistryService ContentTypes { get; set; }
        [Import]
        public IFileExtensionRegistryService FileExtensionRegistry { get; set; }

        public MarkdownMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            Mef.SatisfyImportsOnce(this);
            _contentType = ContentTypes.GetContentType("Markdown");

            if (_contentType != null)
                _extensions = FileExtensionRegistry.GetFileExtensionSet(_contentType);

            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID css = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.CreateMarkdownStylesheet);
            OleMenuCommand cssCommand = new OleMenuCommand(async (s, e) => await AddStylesheet(), css);
            cssCommand.BeforeQueryStatus += HasStylesheet;
            _mcs.AddCommand(cssCommand);

            CommandID compile = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.MarkdownCompile);
            OleMenuCommand compileCommand = new OleMenuCommand((s, e) => AddHtmlFiles(), compile);
            compileCommand.BeforeQueryStatus += IsMarkdownFile;
            _mcs.AddCommand(compileCommand);
        }

        private void HasStylesheet(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            menuCommand.Enabled = !MarkdownMargin.HasCustomStylesheet;
        }

        private void IsMarkdownFile(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            var paths = ProjectHelpers.GetSelectedItemPaths(_dte);

            menuCommand.Enabled = paths.Any() && paths.All(p => _extensions.Contains(Path.GetExtension(p)));
        }

        private void AddHtmlFiles()
        {
            var paths = ProjectHelpers.GetSelectedItemPaths(_dte);
            var contentType = ContentTypeManager.GetContentType("Markdown");
            var compiler = Mef.GetImport<ICompilerRunnerProvider>(contentType).GetCompiler(contentType);

            Parallel.ForEach(paths, f => compiler.CompileToDefaultOutputAsync(f).DoNotWait("compiling " + f));
        }

        private async static System.Threading.Tasks.Task AddStylesheet()
        {
            await MarkdownMargin.CreateStylesheet();
        }
    }
}