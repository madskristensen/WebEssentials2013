using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.Web.Editor;
using Intel = Microsoft.VisualStudio.Language.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    internal class JsDocCompletionSource : StringCompletionSource
    {
        private static readonly Type jsTaggerType = typeof(Microsoft.VisualStudio.JSLS.JavaScriptLanguageService).Assembly.GetType("Microsoft.VisualStudio.JSLS.Classification.Tagger");
        private static readonly ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPublic);
        private static List<Intel.Completion> _completions = new List<Intel.Completion>();

        public override Span? GetInvocationSpan(string text, int linePosition, SnapshotPoint position)
        {
            if (!IsComment(position) || (linePosition > 0 && !text.Substring(0, linePosition - 1).Contains('*')))
                return null;

            int startIndex = text.LastIndexOf('@', linePosition - 1);

            if (startIndex == -1 || text.Substring(startIndex, linePosition - startIndex).Contains(' '))
                return null;

            var endIndex = linePosition + text.Substring(linePosition).TakeWhile(Char.IsLetter).Count();

            return Span.FromBounds(startIndex, endIndex);
        }

        private static bool IsComment(SnapshotPoint position)
        {
            if (position.Position < 2)
                return false;

            var tagger = position.Snapshot.TextBuffer.Properties.GetProperty<ITagger<ClassificationTag>>(jsTaggerType);

            Span span = Span.FromBounds(position.Position - 1, position.Position);
            var spans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(position.Snapshot, span));
            var classifications = tagger.GetTags(spans);

            return classifications.Any(c => c.Tag.ClassificationType.IsOfType("comment"));
        }

        public override IEnumerable<Intel.Completion> GetEntries(char quote, SnapshotPoint caret)
        {
            if (_completions.Count == 0)
            {
                // Source: http://code.google.com/p/jsdoc-toolkit/wiki/TagReference
                AddCompletion("augments", "Indicate this class uses another class as its base");
                AddCompletion("author", "Indicate the author of the code being documented");
                AddCompletion("borrows", "that as this - Document that class's member as if it were a member of this class");
                AddCompletion("class", "Provide a description of the class (versus the constructor)");
                AddCompletion("constant", "Indicate that a variable's value is a constant");
                AddCompletion("constructor", "Marks a function as a constructor");
                AddCompletion("constructs", "Identicate that a lent function will be used as a constructor");
                AddCompletion("default", "Describe the default value of a variable");
                AddCompletion("deprecated", "Marks a method as deprecated");
                AddCompletion("description", "Provide a description (synonym for an untagged first-line)");
                AddCompletion("event", "Describe an event handled by a class");
                AddCompletion("example", "Provide a small code example, illustrating usage");
                AddCompletion("exception", "Synonym for @throws");
                AddCompletion("field", "Indicate that the variable refers to a non-function");
                AddCompletion("fileOverview", "Provides information about the entire file");
                AddCompletion("function", "Indicate that the variable refers to a function");
                AddCompletion("inner", "Indicate that the variable refers to an inner function (and so is also @private)");
                AddCompletion("lends", "that all an object literal's members are members of a given class");
                AddCompletion("link", "Like @see but can be used within the text of other tags");
                AddCompletion("memberOf", "Document that this variable refers to a member of a given class");
                AddCompletion("namespace", "an object literal is being used as a namespace");
                AddCompletion("param", "Documents a method parameter; a datatype indicator can be added between curly braces");
                AddCompletion("private", "Signifies that a method is private");
                AddCompletion("property", "Document a property of a class from within the constructor's doclet");
                AddCompletion("public", "an inner variable is public");
                AddCompletion("requires", "Describe a required resource");
                AddCompletion("returns", "Synonym for @return");
                AddCompletion("see", "Documents an association to another object");
                AddCompletion("since", "Indicate that a feature has only been available on and after a certain version number");
                AddCompletion("static", "Indicate that accessing the variable does not require instantiation of its parent");
                AddCompletion("this", "Specifies the type of the object to which the keyword \"this\" refers within a function");
                AddCompletion("throws", "Documents an exception thrown by a method");
                AddCompletion("type", "Describe the expected type of a variable's value or the value returned by a function");
                AddCompletion("version", "Provides the version number of a library");
            }

            return _completions;
        }

        private static void AddCompletion(string text, string description)
        {
            _completions.Add(new Intel.Completion("@" + text, "@" + text, description, _glyph, null));
        }
    }
}