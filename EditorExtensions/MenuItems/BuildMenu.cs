using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;

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
            CommandID cmdBundles = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.cmdBuildBundles);
            OleMenuCommand menuBundles = new OleMenuCommand((s, e) => UpdateBundleFiles(), cmdBundles);
            _mcs.AddCommand(menuBundles);

            CommandID cmdLess = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.cmdBuildLess);
            OleMenuCommand menuLess = new OleMenuCommand((s, e) => BuildLess(), cmdLess);
            _mcs.AddCommand(menuLess);

            CommandID cmdTS = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.cmdBuildTypeScript);
            OleMenuCommand menuTS = new OleMenuCommand((s, e) => BuildTypeScript(), cmdTS);
            _mcs.AddCommand(menuTS);

            CommandID cmdMinify = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.cmdBuildMinify);
            OleMenuCommand menuMinify = new OleMenuCommand((s, e) => Minify(), cmdMinify);
            _mcs.AddCommand(menuMinify);

            CommandID cmdCoffee = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.cmdBuildCoffeeScript);
            OleMenuCommand menuCoffee = new OleMenuCommand((s, e) => BuildCoffeeScript(), cmdCoffee);
            _mcs.AddCommand(menuCoffee);
        }

        private void BuildCoffeeScript()
        {
            foreach (Project project in _dte.Solution.Projects)
            {
                CoffeeScriptMargin margin = new CoffeeScriptMargin();
                margin.CompileProject(project);
            }
        }

        private void UpdateBundleFiles()
        {
            //Logger.Log("Updating bundles...");
            BundleFilesMenu.UpdateBundles(null, true);
            //Logger.Log("Bundles updated");
        }

        private void BuildLess()
        {
            foreach (Project project in _dte.Solution.Projects)
            {
                LessProjectCompiler.CompileProject(project);
            }
        }

        private void BuildTypeScript()
        {
            foreach (Project project in _dte.Solution.Projects)
            {
                new TypeScriptMargin().CompileProjectFiles(project);
            }
        }

        private void Minify()
        {
            _dte.StatusBar.Text = "Web Essentials: Minifying files...";
            var files = GetFiles();

            foreach (string path in files)
            {
                string extension = Path.GetExtension(path);
                string minPath = MinifyFileMenu.GetMinFileName(path, extension);

                if (!path.EndsWith(".min" + extension) && File.Exists(minPath) && _dte.Solution.FindProjectItem(path) != null)
                {
                    if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        JavaScriptSaveListener.Minify(path, minPath, false);
                    }
                    else
                    {
                        CssSaveListener.Minify(path, minPath);
                    }
                }
            }

            _dte.StatusBar.Text = "Web Essentials: Files minified";
        }

        private IEnumerable<string> GetFiles()
        {
            //Project project = ProjectHelpers.GetActiveProject();

            foreach (Project project in _dte.Solution.Projects)
            {
                if (string.IsNullOrEmpty(project.FullName))
                    continue;

                string dir = Path.GetDirectoryName(project.FullName);

                List<string> list = new List<string>();
                list.AddRange(Directory.GetFiles(dir, "*.css", SearchOption.AllDirectories));
                list.AddRange(Directory.GetFiles(dir, "*.js", SearchOption.AllDirectories));

                foreach (string file in list.Where(f => !f.Contains(".min.")))
                {
                    string extension = Path.GetExtension(file);

                    if (extension == ".css")
                    {
                        if (!File.Exists(file.Replace(".css", ".less")) &&
                            !File.Exists(file.Replace(".css", ".scss")) &&
                            !File.Exists(file + ".bundle"))

                            yield return file;
                    }
                    if (extension == ".js")
                    {
                        if (!File.Exists(file.Replace(".js", ".coffee")) &&
                            !File.Exists(file.Replace(".js", ".ts")) &&
                            !File.Exists(file + ".bundle"))

                            yield return file;
                    }
                }
            }

            yield break;
        }
    }
}