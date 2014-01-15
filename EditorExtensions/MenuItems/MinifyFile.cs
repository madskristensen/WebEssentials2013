using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows;
using EnvDTE80;
using Microsoft.Ajax.Utilities;
using Microsoft.VisualStudio.Shell;
using WebMarkupMin.Core;
using WebMarkupMin.Core.Minifiers;
using WebMarkupMin.Core.Settings;

namespace MadsKristensen.EditorExtensions
{
    internal class MinifyFileMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private static List<string> _htmlExt = new List<string>() { ".html", ".htm", ".aspx", ".ascx", ".master", ".cshtml", ".vbhtml" };

        public MinifyFileMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandCss = new CommandID(CommandGuids.guidMinifyCmdSet, (int)CommandId.MinifyCss);
            OleMenuCommand menuCommandCss = new OleMenuCommand((s, e) => MinifyFile(".css"), commandCss);
            menuCommandCss.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".css"); };
            _mcs.AddCommand(menuCommandCss);

            CommandID commandJs = new CommandID(CommandGuids.guidMinifyCmdSet, (int)CommandId.MinifyJs);
            OleMenuCommand menuCommandJs = new OleMenuCommand((s, e) => MinifyFile(".js"), commandJs);
            menuCommandJs.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".js"); };
            _mcs.AddCommand(menuCommandJs);

            CommandID commandHtml = new CommandID(CommandGuids.guidMinifyCmdSet, (int)CommandId.MinifyHtml);
            OleMenuCommand menuCommandHtml = new OleMenuCommand((s, e) => MinifyFile(".html"), commandHtml);
            menuCommandHtml.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".html"); };
            _mcs.AddCommand(menuCommandHtml);
        }

        void BeforeQueryStatus(object sender, string extension)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            var selectedPaths = GetSelectedFilePaths(_dte).Where(p => Path.GetExtension(p) == extension);
            bool enabled = false;

            foreach (string path in selectedPaths)
            {
                string minFile = GetMinFileName(path, extension);

                if (!path.EndsWith(".min" + extension, StringComparison.OrdinalIgnoreCase) && !File.Exists(minFile))
                {
                    enabled = true;
                    break;
                }
            }

            menuCommand.Enabled = enabled;
        }

        private void MinifyFile(string extension)
        {
            var selectedPaths = GetSelectedFilePaths(_dte);

            foreach (string path in selectedPaths.Where(p => p.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                string minPath = GetMinFileName(path, extension);

                if (!path.EndsWith(".min" + extension, StringComparison.OrdinalIgnoreCase) && !File.Exists(minPath) && _dte.Solution.FindProjectItem(path) != null)
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

                    ProjectHelpers.AddFileToProject(path, minPath);
                }
            }

            EnableSync(extension);
        }

        private static void EnableSync(string extension)
        {
            string message = "Do you also want to enable automatic minification when the source file changes?";

            // TODO: Move to common code with map of extension to settings interface
            if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase) && !WESettings.Instance.Css.AutoMinify)
            {
                var result = MessageBox.Show(message, "Web Essentials", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    WESettings.Instance.Css.AutoMinify = true;
                    SettingsStore.Save();
                }
            }
            else if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) && !WESettings.Instance.JavaScript.AutoMinify)
            {
                var result = MessageBox.Show(message, "Web Essentials", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    WESettings.Instance.JavaScript.AutoMinify = true;
                    SettingsStore.Save();
                }
            }
            else if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) && !WESettings.Instance.Html.AutoMinify)
            {
                var result = MessageBox.Show(message, "Web Essentials", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    WESettings.Instance.Html.AutoMinify = true;
                    SettingsStore.Save();
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
                var settings = new Microsoft.Ajax.Utilities.CssSettings();
                settings.CommentMode = CssComment.None;

                if (WESettings.Instance.General.KeepImportantComments)
                {
                    settings.CommentMode = CssComment.Important;
                }

                return minifier.MinifyStyleSheet(content, settings);
            }
            else if (extension == ".js")
            {
                Minifier minifier = new Minifier();
                CodeSettings settings = new CodeSettings() {
                    EvalTreatment = EvalTreatment.MakeImmediateSafe,
                    PreserveImportantComments = WESettings.Instance.General.KeepImportantComments
                };

                return minifier.MinifyJavaScript(content, settings);
            }
            else if (_htmlExt.Contains(extension.ToLowerInvariant()))
            {
                var settings = new HtmlMinificationSettings {
                    RemoveOptionalEndTags = false,
                    AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes,
                    RemoveRedundantAttributes = false,
                };

                var minifier = new HtmlMinifier(settings);
                MarkupMinificationResult result = minifier.Minify(content, generateStatistics: true);

                if (result.Errors.Count == 0)
                {
                    EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: HTML minified by " + result.Statistics.SavedInPercent + "%";
                    return result.MinifiedContent;
                }
                else
                {
                    EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Cannot minify the current selection.  See Output Window for details.";
                    Logger.ShowMessage("Cannot minify the selection:\r\n\r\n" + String.Join(Environment.NewLine, result.Errors.Select(e => e.Message)));
                    return content;
                }
            }

            return null;
        }

        public static IEnumerable<string> GetSelectedFilePaths(DTE2 dte)
        {
            var selectedPaths = ProjectHelpers.GetSelectedItemPaths(dte);
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
                        list.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                    }
                }
            }

            return list;
        }
    }
}