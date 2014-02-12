using System.Collections.Generic;
using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Helpers;
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
    //[ContentType("SASS")]

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

            var notifierProvider = Mef.GetImport<ICompilationNotifierProvider>(contentType);
            var notifier = notifierProvider.GetCompilationNotifier(document);

            var compilerProvider = Mef.GetImport<ICompilerRunnerProvider>(contentType);
            var compilerRunner = compilerProvider.GetCompiler(contentType);

            var graph = Mef.GetImport<DependencyGraph>(contentType);
            notifier.CompilationReady += async (s, e) =>
            {
                if (!e.CompilerResult.IsSuccess || string.IsNullOrEmpty(e.CompilerResult.TargetFileName))
                    return;
                foreach (var file in await graph.GetRecursiveDependentsAsync(e.CompilerResult.SourceFileName))
                    compilerRunner.CompileToDefaultOutputAsync(file).DontWait("compiling " + file);
            };
        }

        readonly HashSet<IContentType> registeredContentTypes = new HashSet<IContentType>();
        private DependencyGraph GetGraph(IContentType contentType)
        {
            var graph = Mef.GetImport<DependencyGraph>(contentType);
            if (registeredContentTypes.Add(contentType))
                return graph;

            // TODO: Add settings change event handler, once per content type
            return graph;
        }
    }
}
