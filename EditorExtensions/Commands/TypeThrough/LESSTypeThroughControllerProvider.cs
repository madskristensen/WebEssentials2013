using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [ContentType(LessContentTypeDefinition.LessContentType)]
    [Name("LESS Type Through Completion Controller")]
    [Order(Before = "Default Completion Controller")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class LessTypeThroughControllerProvider : IIntellisenseControllerProvider
    {
        public IIntellisenseController TryCreateIntellisenseController(ITextView view, IList<ITextBuffer> subjectBuffers)
        {
            var completionController = ServiceManager.GetService<LessTypeThroughController>(subjectBuffers[0]);

            if (completionController == null)
                completionController = new LessTypeThroughController(view, subjectBuffers);

            return completionController;
        }
    }

    internal class LessTypeThroughController : CssTypeThroughController
    {
        public LessTypeThroughController(ITextView view, IList<ITextBuffer> subjectBuffers) :
            base(view, subjectBuffers)
        {
        }

        protected override bool CanComplete(ITextBuffer textBuffer, int position)
        {
            var document = CssEditorDocument.FromTextBuffer(textBuffer);

            var item = document.StyleSheet.ComplexItemFromRange(position, 0);
            if (item is CppComment || item is CppCommentText)
                return false;

            var tokenItem = document.StyleSheet.ItemFromRange(position, 0);
            var ti = tokenItem as TokenItem;
            if (ti != null)
            {
                if (ti.Token.IsComment)
                    return false;
            }

            return base.CanComplete(textBuffer, position);
        }
    }
}
