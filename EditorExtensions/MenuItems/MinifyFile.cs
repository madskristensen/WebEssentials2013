using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using Microsoft.Ajax.Utilities;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
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
            _mcs.AddCommand(new MinifyFileCommand(ContentTypeManager.GetContentType("Css"), CommandId.MinifyCss).Command);
            _mcs.AddCommand(new MinifyFileCommand(ContentTypeManager.GetContentType("HTMLX"), CommandId.MinifyHtml).Command);
            _mcs.AddCommand(new MinifyFileCommand(ContentTypeManager.GetContentType("JavaScript"), CommandId.MinifyJs).Command);
        }

        //TODO: Add menu items for compilable files too
        class MinifyFileCommand
        {
            public MinificationSaveListener MinificationService { get; set; }
            public IFileExtensionRegistryService FileExtensionRegistry { get; set; }
            public OleMenuCommand Command { get; private set; }
            public IContentType ContentType { get; private set; }
            private readonly HashSet<string> _sourceExtensions;

            private IEnumerable<string> selectedFiles;

            public MinifyFileCommand(IContentType contentType, CommandId id)
            {
                Mef.SatisfyImportsOnce(this);
                ContentType = contentType;
                _sourceExtensions = new HashSet<string>(
                    FileExtensionRegistry.GetExtensionsForContentType(contentType)
                                         .Select(e => "." + e),
                    StringComparer.OrdinalIgnoreCase
                );

                Command = new OleMenuCommand((s, e) => Execute(), new CommandID(CommandGuids.guidMinifyCmdSet, (int)id));
                Command.BeforeQueryStatus += (s, e) => CheckVisible();
            }

            private void CheckVisible()
            {
                selectedFiles = ProjectHelpers.GetSelectedFilePaths()
                    .Where(p => _sourceExtensions.Contains(p)
                             && !MinificationSaveListener.ShouldMinify(p)
                             && !File.Exists(MinificationSaveListener.GetMinFileName(p))
                           );
                Command.Enabled = selectedFiles.Any();
            }
            private void Execute()
            {
                foreach (var file in selectedFiles)
                {
                    MinificationService.CreateMinFile(ContentType, file);
                }
                CheckEnableSync();
            }


            private void CheckEnableSync()
            {
                var settings = WESettings.Instance.ForContentType<IMinifierSettings>(ContentType);
                if (settings.AutoMinify)
                    return;
                if (MessageBoxResult.Yes == MessageBox.Show(
                        "Do you also want to enable automatic minification when the source file changes?",
                        "Web Essentials", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    )
                {
                    settings.AutoMinify = true;
                    SettingsStore.Save();
                }
            }
        }
    }
}