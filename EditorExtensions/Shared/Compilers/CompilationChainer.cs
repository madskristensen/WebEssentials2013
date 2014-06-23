using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using MadsKristensen.EditorExtensions.Helpers;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Compilers
{
    [Export(typeof(IVsTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("LESS")]
    [ContentType("SCSS")]
    class CompilationChainer : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            var contentType = textView.TextBuffer.ContentType;

            ITextDocument document;
            if (!TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
                return;

            var settings = WESettings.Instance.ForContentType<IChainableCompilerSettings>(contentType);

            var notifierProvider = Mef.GetImport<ICompilationNotifierProvider>(contentType);
            var notifier = notifierProvider.GetCompilationNotifier(document);

            var compilerProvider = Mef.GetImport<ICompilerRunnerProvider>(contentType);
            var compilerRunner = compilerProvider.GetCompiler(contentType);

            var graph = GetGraph(contentType);
            notifier.CompilationReady += async (s, e) =>
            {
                if (!settings.CompileOnSave || !settings.EnableChainCompilation || (bool)s)
                    return;

                var count = 0;
                foreach (var file in await graph.GetRecursiveDependentsAsync(e.CompilerResult.SourceFileName))
                {
                    if (File.Exists(compilerRunner.GetTargetPath(file)))
                    {
                        compilerRunner.CompileToDefaultOutputAsync(file).DoNotWait("compiling " + file);
                        count++;
                    }
                }
                WebEssentialsPackage.DTE.StatusBar.Text = "Compiling " + count + " dependent file" + (count == 1 ? "s" : "")
                                                           + " for " + Path.GetFileName(e.CompilerResult.SourceFileName);
            };
        }

        readonly HashSet<IContentType> registeredContentTypes = new HashSet<IContentType>();
        private DependencyGraph GetGraph(IContentType contentType)
        {
            var graph = (VsDependencyGraph)Mef.GetImport<DependencyGraph>(contentType);
            if (!registeredContentTypes.Add(contentType))
                return graph;

            // Add this event handler only once per ContentType
            var settings = WESettings.Instance.ForContentType<IChainableCompilerSettings>(contentType);
            graph.IsEnabled = settings.EnableChainCompilation;
            settings.EnableChainCompilationChanged += delegate { graph.IsEnabled = settings.EnableChainCompilation; };

            return graph;
        }
    }
}
