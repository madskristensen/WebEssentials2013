using EnvDTE;
using EnvDTE80;
using Microsoft.Ajax.Utilities;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows;

namespace MadsKristensen.EditorExtensions
{
    internal class MinifyFileMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public MinifyFileMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandCss = new CommandID(GuidList.guidMinifyCmdSet, (int)PkgCmdIDList.MinifyCss);
            OleMenuCommand menuCommandCss = new OleMenuCommand((s, e) => MinifyFile(".css"), commandCss);
            menuCommandCss.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".css"); };
            _mcs.AddCommand(menuCommandCss);

            CommandID commandJs = new CommandID(GuidList.guidMinifyCmdSet, (int)PkgCmdIDList.MinifyJs);
            OleMenuCommand menuCommandJs = new OleMenuCommand((s, e) => MinifyFile(".js"), commandJs);
            menuCommandJs.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".js"); };
            _mcs.AddCommand(menuCommandJs);

            //CommandID commandSelection = new CommandID(GuidList.guidMinifyCmdSet, (int)PkgCmdIDList.MinifySelection);
            //OleMenuCommand menuCommandSelection = new OleMenuCommand((s, e) => MinifySelection(), commandSelection);
            //menuCommandSelection.BeforeQueryStatus += menuCommandSelection_BeforeQueryStatus;
            //_mcs.AddCommand(menuCommandSelection);
        }

        private readonly string[] _supported = new[] { "CSS", "JAVASCRIPT" };

        //void menuCommandSelection_BeforeQueryStatus(object sender, EventArgs e)
        //{
        //    OleMenuCommand menu = sender as OleMenuCommand;
        //    var view = ProjectHelpers.GetCurentTextView();

        //    if (view != null && view.Selection.SelectedSpans.Count > 0)
        //    {
        //        menu.Enabled = view.Selection.SelectedSpans[0].Length > 0;
        //    }
        //    else
        //    {
        //        menu.Enabled = false;
        //    }
        //}

        void BeforeQueryStatus(object sender, string extension)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            var selectedPaths = GetSelectedFilePaths(_dte).Where(p => Path.GetExtension(p) == extension);
            bool enabled = false;

            foreach (string path in selectedPaths)
            {
                string minFile = GetMinFileName(path, extension);

                if (!path.EndsWith(".min" + extension) && !File.Exists(minFile))
                {
                    enabled = true;
                    break;
                }
            }

            menuCommand.Enabled = enabled;
        }

        //private void MinifySelection()
        //{
        //    var view = ProjectHelpers.GetCurentTextView();

        //    if (view != null)
        //    {
        //        _dte.UndoContext.Open("Minify");

        //        string content = view.Selection.SelectedSpans[0].GetText();
        //        string extension = Path.GetExtension(_dte.ActiveDocument.FullName).ToLowerInvariant();
        //        string result = MinifyString(extension, content);

        //        view.TextBuffer.Replace(view.Selection.SelectedSpans[0].Span, result);

        //        _dte.UndoContext.Close();
        //    }
        //}

        private void MinifyFile(string extension)
        {
            var selectedPaths = GetSelectedFilePaths(_dte);

            foreach (string path in selectedPaths.Where(p => p.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                string minPath = GetMinFileName(path, extension);

                if (!path.EndsWith(".min" + extension) && !File.Exists(minPath) && _dte.Solution.FindProjectItem(path) != null)
                {
                    if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        JavaScriptSaveListener.Minify(path, minPath, false);
                    }
                    else
                    {
                        CssSaveListener.Minify(path, minPath);
                    }

                    MarginBase.AddFileToProject(path, minPath);
                }
            }

            EnableSync(extension);
        }

        private void EnableSync(string extension)
        {
            string message = string.Format("Do you also want to enable automatic minification when the source file changes?", extension);

            if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase) && !WESettings.GetBoolean(WESettings.Keys.EnableCssMinification))
            {
                var result = MessageBox.Show(message, "Web Essentials", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Settings.SetValue(WESettings.Keys.EnableCssMinification, true);
                    Settings.Save();
                }
            }
            else if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) && !WESettings.GetBoolean(WESettings.Keys.EnableJsMinification))
            {
                var result = MessageBox.Show(message, "Web Essentials", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Settings.SetValue(WESettings.Keys.EnableJsMinification, true);
                    Settings.Save();
                }
            }
        }

        public static string GetMinFileName(string path, string extension)
        {
            return path.Insert(path.Length - extension.Length, ".min");
        }

        public static string MinifyString(string extension, string content)
        {
            if (extension == ".css")
            {
                Minifier minifier = new Minifier();
                CssSettings settings = new CssSettings();
                settings.CommentMode = CssComment.None;

                if (WESettings.GetBoolean(WESettings.Keys.KeepImportantComments))
                {
                    settings.CommentMode = CssComment.Important;
                }

                return minifier.MinifyStyleSheet(content, settings);
            }
            else if (extension == ".js")
            {
                Minifier minifier = new Minifier();
                CodeSettings settings = new CodeSettings()
                {
                    EvalTreatment = EvalTreatment.MakeImmediateSafe,
                    PreserveImportantComments = WESettings.GetBoolean(WESettings.Keys.KeepImportantComments)
                };

                return minifier.MinifyJavaScript(content, settings);
            }

            return null;
        }

        public static IEnumerable<string> GetSelectedFilePaths(DTE2 dte)
        {
            var selectedPaths = GetSelectedItemPaths(dte);
            List<string> list = new List<string>();

            foreach (string path in selectedPaths)
            {
                string extension = Path.GetExtension(path);

                if (!string.IsNullOrEmpty(extension))
                {
                    // file
                    list.Add(path);
                }
                else
                {
                    // Folder
                    if (Directory.Exists(path))
                    {
                        list.AddRange(Directory.GetFiles(path));
                    }
                }
            }

            return list;
        }

        private static IEnumerable<string> GetSelectedItemPaths(DTE2 dte)
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (UIHierarchyItem selItem in items)
            {
                var item = selItem.Object as ProjectItem;
                if (item != null)
                {
                    yield return item.Properties.Item("FullPath").Value.ToString();
                }
            }
        }

    }
}