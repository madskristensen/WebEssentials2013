using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Export(typeof(IClassifierProvider))]
    [ContentType("plaintext")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class RobotsTxtClassifierProvider : IClassifierProvider, IVsTextViewCreationListener
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<RobotsTxtClassifier>(() => new RobotsTxtClassifier(Registry));
        }

        public void VsTextViewCreated(Microsoft.VisualStudio.TextManager.Interop.IVsTextView textViewAdapter)
        {
            ITextDocument document;
            RobotsTxtClassifier classifier;

            var view = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            if (TextDocumentFactoryService.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out document))
            {
                TextType type = GetTextType(document.FilePath);
                if (type == TextType.Unsupported)
                    return;

                view.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(RobotsTxtClassifier), out classifier);
                view.Properties.GetOrCreateSingletonProperty(() => new CommentCommandTarget(textViewAdapter, view, "#"));

                if (classifier != null)
                {
                    ITextSnapshot snapshot = view.TextBuffer.CurrentSnapshot;
                    classifier.OnClassificationChanged(new SnapshotSpan(snapshot, 0, snapshot.Length), type);
                }
            }
        }

        public static TextType GetTextType(string fileName)
        {
            switch (System.IO.Path.GetFileName(fileName).ToLowerInvariant())
            {
                case "robots.txt":
                    return TextType.Robots;

                case "humans.txt":
                    return TextType.Humans;
            }

            return TextType.Unsupported;
        }
    }

    public enum TextType
    {
        Unsupported = 0,
        Robots,
        Humans,
    }
}