using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [ContentType("TypeScript")]
    [Name("TypeScript Type Through Completion Controller")]
    [Order(Before = "Default Completion Controller")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class TypeScriptTypeThroughControllerProvider : IIntellisenseControllerProvider
    {
        public IIntellisenseController TryCreateIntellisenseController(ITextView view, IList<ITextBuffer> subjectBuffers)
        {
            if (subjectBuffers.Count > 0 && (subjectBuffers[0].ContentType.IsOfType("TypeScript")))
            {
                var completionController = ServiceManager.GetService<TypeThroughController>(subjectBuffers[0]);

                if (completionController == null)
                    completionController = new TypeScriptTypeThroughController(view, subjectBuffers);

                return completionController;
            }

            return null;
        }
    }

    internal class TypeScriptTypeThroughController : TypeThroughController
    {
        public TypeScriptTypeThroughController(ITextView textView, IList<ITextBuffer> subjectBuffers)
            : base(textView, subjectBuffers)
        {
        }

        protected override bool CanComplete(ITextBuffer textBuffer, int position)
        {
            bool result = WESettings.Instance.TypeScript.BraceCompletion;

            if (result)
            {
                var line = textBuffer.CurrentSnapshot.GetLineFromPosition(position);
                result = line.Start.Position + line.GetText().TrimEnd('\r', '\n', ' ', ';', ',').Length == position + 1;
            }

            return result;
        }

        protected override char GetCompletionCharacter(char typedCharacter)
        {
            switch (typedCharacter)
            {
                case '[':
                    return ']';

                case '(':
                    return ')';

                case '{':
                    return '}';
            }

            return '\0';
        }
    }
}
