using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    internal class MinifyFileMenu
    {
        private OleMenuCommandService _mcs;

        public MinifyFileMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            _mcs.AddCommand(new MinifyFileCommand(ContentTypeManager.GetContentType("Css"), MinifyCommandId.MinifyCss).Command);
            _mcs.AddCommand(new MinifyFileCommand(ContentTypeManager.GetContentType("HTMLX"), MinifyCommandId.MinifyHtml).Command);
            _mcs.AddCommand(new MinifyFileCommand(ContentTypeManager.GetContentType("JavaScript"), MinifyCommandId.MinifyJs).Command);
        }

        //TODO: Add menu items for compilable files too
        class MinifyFileCommand
        {
            [Import]
            public IFileExtensionRegistryService FileExtensionRegistry { get; set; }
            public OleMenuCommand Command { get; private set; }
            public IContentType ContentType { get; private set; }
            private readonly ISet<string> _sourceExtensions;

            private IEnumerable<string> selectedFiles;

            public MinifyFileCommand(IContentType contentType, MinifyCommandId id)
            {
                Mef.SatisfyImportsOnce(this);
                ContentType = contentType;
                _sourceExtensions = FileExtensionRegistry.GetFileExtensionSet(contentType);

                Command = new OleMenuCommand((s, e) => Execute(), new CommandID(CommandGuids.guidMinifyCmdSet, (int)id));
                Command.BeforeQueryStatus += (s, e) => CheckVisible();
            }

            private void CheckVisible()
            {
                selectedFiles = ProjectHelpers.GetSelectedFilePaths()
                    .Where(p => _sourceExtensions.Contains(Path.GetExtension(p))
                             && MinificationSaveListener.ShouldMinify(p)
                             && !File.Exists(MinificationSaveListener.GetMinFileName(p))
                           );
                Command.Enabled = selectedFiles.Any();
            }
            private void Execute()
            {
                foreach (var file in selectedFiles)
                {
                    MinificationSaveListener.CreateMinFile(ContentType, file);
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