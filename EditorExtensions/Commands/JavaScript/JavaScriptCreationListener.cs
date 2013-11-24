using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType("JavaScript")]
    [ContentType("Node.js")]
    [ContentType("htmlx")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class JavaScriptSortPropertiesViewCreationListener : IWpfTextViewConnectionListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import(typeof(ITextStructureNavigatorSelectorService))]
        internal ITextStructureNavigatorSelectorService Navigator { get; set; }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            var jsBuffer = subjectBuffers.FirstOrDefault(b => b.ContentType.IsOfType("JavaScript"));
            if (jsBuffer == null)
                return;

            var textViewAdapter = EditorAdaptersFactoryService.GetViewAdapter(textView);

            textView.Properties.GetOrCreateSingletonProperty<MinifySelection>(() => new MinifySelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<JavaScriptFindReferences>(() => new JavaScriptFindReferences(textViewAdapter, textView, Navigator));
            textView.Properties.GetOrCreateSingletonProperty<CssExtractToFile>(() => new CssExtractToFile(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<NodeModuleGoToDefinition>(() => new NodeModuleGoToDefinition(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<ReferenceTagGoToDefinition>(() => new ReferenceTagGoToDefinition(textViewAdapter, textView));

            ITextDocument document;
            jsBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document);

            if (document != null)
            {
                JsHintProjectRunner runner = new JsHintProjectRunner(document);
                textView.Closed += (s, e) => runner.Dispose();

                textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => runner);
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }
    }
}
