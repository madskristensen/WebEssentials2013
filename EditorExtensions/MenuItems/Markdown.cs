using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class MarkdownMenu
    {
        private OleMenuCommandService _mcs;
        private DTE2 _dte;
        private static HashSet<string> _extensions = new HashSet<string>() { ".md", ".mdown", ".markdown", ".mkd", ".mkdn", ".mdwn", ".mmd" };

        public MarkdownMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID css = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.CreateMarkdownStylesheet);
            OleMenuCommand cssCommand = new OleMenuCommand((s, e) => AddStylesheet(), css);
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

            menuCommand.Enabled = !File.Exists(MarkdownMargin.GetCustomStylesheetFilePath());
        }

        private void IsMarkdownFile(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            var paths = ProjectHelpers.GetSelectedItemPaths(_dte);

            menuCommand.Enabled = paths.Count() > 0 && paths.All(p => _extensions.Contains(Path.GetExtension(p)));
        }

        private void AddHtmlFiles()
        {
            var paths = ProjectHelpers.GetSelectedItemPaths(_dte);
            var compiler = MarkdownMargin.CreateCompiler();

            foreach (string path in paths)
            {
                try
                {
                    string result = compiler.Transform(File.ReadAllText(path));
                    string htmlFile = Path.ChangeExtension(path, ".html");

                    File.WriteAllText(htmlFile, result);
                    ProjectHelpers.AddFileToProject(path, htmlFile);
                }
                catch
                {
                    Logger.Log("Markdown: Couldn't generate .html file from menu button");
                }
            }
        }

        private static void AddStylesheet()
        {
            MarkdownMargin.CreateStylesheet();
        }
    }
}