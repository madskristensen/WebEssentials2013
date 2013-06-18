using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [ContentType(CssContentTypeDefinition.CssContentType)]
    [Name("CSS Type Through Completion Controller")]
    [Order(Before = "Default Completion Controller")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class CssTypeThroughControllerProvider : IIntellisenseControllerProvider
    {
        public IIntellisenseController TryCreateIntellisenseController(ITextView view, IList<ITextBuffer> subjectBuffers)
        {
            if (subjectBuffers[0].ContentType.IsOfType(CssContentTypeDefinition.CssContentType))
            {
                var completionController = ServiceManager.GetService<TypeThroughController>(subjectBuffers[0]);

                if (completionController == null)
                    completionController = new CssTypeThroughController(view, subjectBuffers);

                return completionController;
            }

            return null;
        }
    }

    internal class CssTypeThroughController : TypeThroughController
    {
        public CssTypeThroughController(ITextView textView, IList<ITextBuffer> subjectBuffers)
            : base(textView, subjectBuffers)
        {
        }

        protected override bool CanComplete(ITextBuffer textBuffer, int position)
        {
            var document = CssEditorDocument.FromTextBuffer(textBuffer);

            var item = document.StyleSheet.ComplexItemFromRange(position, 0);
            if (item is Comment)
                return false;

            var tokenItem = document.StyleSheet.ItemFromRange(position, 0);
            var ti = tokenItem as TokenItem;
            if (ti != null)
            {
                if ((ti.TokenType == CssTokenType.String || ti.TokenType == CssTokenType.MultilineString) && !ti.IsUnclosed)
                    return false;
            }
            
            return true;
        }

        protected override char GetCompletionCharacter(char typedCharacter)
        {
            switch (typedCharacter)
            {
                //case '\"':
                //case '\'':
                //    return typedCharacter;

                //case '[':
                //    return ']';

                //case '(':
                //    return ')';

                case '{':
                    return '}';
            }

            return '\0';
        }
    }
}
