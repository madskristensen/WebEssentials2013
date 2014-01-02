using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using ThreadingTasks = System.Threading.Tasks;

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
            CommandID cmdBundles = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.BuildBundles);
            OleMenuCommand menuBundles = new OleMenuCommand((s, e) => UpdateBundleFiles(), cmdBundles);
            _mcs.AddCommand(menuBundles);

            CommandID cmdLess = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.BuildLess);
            OleMenuCommand menuLess = new OleMenuCommand(async (s, e) => await BuildLess(), cmdLess);
            _mcs.AddCommand(menuLess);

            //CommandID cmdTS = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.cmdBuildTypeScript);
            //OleMenuCommand menuTS = new OleMenuCommand((s, e) => BuildTypeScript(), cmdTS);
            //_mcs.AddCommand(menuTS);

            CommandID cmdMinify = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.BuildMinify);
            OleMenuCommand menuMinify = new OleMenuCommand((s, e) => Minify(), cmdMinify);
            _mcs.AddCommand(menuMinify);

            CommandID cmdCoffee = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.BuildCoffeeScript);
            OleMenuCommand menuCoffee = new OleMenuCommand(async (s, e) => await BuildCoffeeScript(), cmdCoffee);
            _mcs.AddCommand(menuCoffee);

            CommandID cmdIcedCoffee = new CommandID(GuidList.guidBuildCmdSet, (int)PkgCmdIDList.BuildIcedCoffeeScript);
            OleMenuCommand menuIcedCoffee = new OleMenuCommand(async (s, e) => await BuildIcedCoffeeScript(), cmdIcedCoffee);
            _mcs.AddCommand(menuIcedCoffee);
        }

        public async static ThreadingTasks.Task BuildCoffeeScript()
        {
            var projectTasks = ProjectHelpers.GetAllProjects().Select(project =>
            {
                return new CoffeeScriptProjectCompiler().CompileProject(project);
            });

            await ThreadingTasks.Task.WhenAll(projectTasks.ToArray());
        }

        public async static ThreadingTasks.Task BuildIcedCoffeeScript()
        {
            var projectTasks = ProjectHelpers.GetAllProjects().Select(project =>
            {
                return new IcedCoffeeScriptProjectCompiler().CompileProject(project);
            });

            await ThreadingTasks.Task.WhenAll(projectTasks.ToArray());
        }

        public static void UpdateBundleFiles()
        {
            //Logger.Log("Updating bundles...");
            BundleFilesMenu.UpdateBundles(null, true);
            //Logger.Log("Bundles updated");
        }

        public async static ThreadingTasks.Task BuildLess()
        {
            var projectTasks = ProjectHelpers.GetAllProjects().Select(project =>
            {
                return new LessProjectCompiler().CompileProject(project);
            });

            await ThreadingTasks.Task.WhenAll(projectTasks.ToArray());
        }

        //private void BuildTypeScript()
        //{
        //    foreach (Project project in _dte.Solution.Projects)
        //    {
        //        new TypeScriptMargin().CompileProjectFiles(project);
        //    }
        //}

        private static void Minify()
        {
            _dte.StatusBar.Text = "Web Essentials: Minifying files...";
            var files = GetFiles();

            foreach (string path in files)
            {
                string extension = Path.GetExtension(path);
                string minPath = MinifyFileMenu.GetMinFileName(path, extension);

                if (!path.EndsWith(".min" + extension, StringComparison.Ordinal) && File.Exists(minPath) && _dte.Solution.FindProjectItem(path) != null)
                {
                    if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        JavaScriptSaveListener.Minify(path, minPath, false);
                    }
                    else if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
                    {
                        CssSaveListener.Minify(path, minPath);
                    }
                    else if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                    {
                        HtmlSaveListener.Minify(path, minPath);
                    }
                }
            }

            _dte.StatusBar.Text = "Web Essentials: Files minified";
        }

        private static IEnumerable<string> GetFiles()
        {
            foreach (Project project in ProjectHelpers.GetAllProjects())
            {
                string dir = ProjectHelpers.GetRootFolder(project);

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