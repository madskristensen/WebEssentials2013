using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.Projection;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Composition;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    class TextViewListener : IWpfTextViewCreationListener
    {
        public void TextViewCreated(IWpfTextView textView)
        {
            textView.Caret.PositionChanged += new TextViewHandler(textView).Caret_PositionChanged;
        }

        class TextViewHandler
        {
            public TextViewHandler(IWpfTextView textView) { this.textView = textView; }
            readonly IWpfTextView textView;

            IContentType lastContentType;

            public void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                var rootBuffer = ServiceManager.GetService<HtmlMainController>(textView).TextBuffer;

                var targetPoint = e.NewPosition.Point.GetPoint(b => !b.ContentType.IsOfType("Projection"), PositionAffinity.Predecessor);
                if (targetPoint == null) return;
                var targetBuffer = targetPoint.Value.Snapshot.TextBuffer;

                if (rootBuffer == targetBuffer)
                    return;
                if (lastContentType == targetBuffer.ContentType)
                    return;

                var contentTypeImportComposer = new ContentTypeImportComposer<ICodeLanguageEmbedder>(WebEditor.CompositionService);
                var embedder = contentTypeImportComposer.GetImport(targetBuffer.ContentType);
                if (embedder == null)
                    return;

                var pbm = ServiceManager.GetService<ProjectionBufferManager>(rootBuffer);
                var languageBuffer = pbm.GetProjectionBuffer(targetBuffer.ContentType);
                if (languageBuffer == null)
                    return;

                embedder.OnBlockEntered(rootBuffer, languageBuffer);

                lastContentType = targetBuffer.ContentType;
            }
        }
    }
}
