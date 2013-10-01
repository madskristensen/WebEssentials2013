using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Intellisense;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICompletionSourceProvider))]
    [Order(Before = "High")]
    [ContentType("JavaScript"),
    Name("EnhancedJavaScriptCompletion")]
    public class JavaScriptCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService TextNavigator { get; set; }

        [Import]
        private ICssNameCache _classNames = null;

        public ICompletionSource TryCreateCompletionSource(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new JavaScriptCompletionSource(buffer, TextNavigator, _classNames)) as ICompletionSource;
        }
    }

    public class JavaScriptCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private ICssNameCache _classNames;
        private ITextStructureNavigatorSelectorService _navigator;
        private static ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphXmlItem, StandardGlyphItem.GlyphItemPublic);
        private static IEnumerable<Completion> _cache = AddHtmlTagNames();

        public JavaScriptCompletionSource(ITextBuffer buffer, ITextStructureNavigatorSelectorService navigator, ICssNameCache classNames)
        {
            _buffer = buffer;
            _navigator = navigator;
            _classNames = classNames;
        }

        static ImageSource _useDirectiveGlyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupIntrinsic, StandardGlyphItem.GlyphItemPublic);
        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            int position = session.TextView.Caret.Position.BufferPosition.Position;
            var line = _buffer.CurrentSnapshot.Lines.SingleOrDefault(l => l.Start <= position && l.End >= position);

            if (line == null)
                return;

            string text = line.GetText();

            var quote = text.SkipWhile(Char.IsWhiteSpace).FirstOrDefault();
            if (quote == '"' || quote == '\'')
            {
                var lineTextStart = line.Start + text.TakeWhile(Char.IsWhiteSpace).Count();
                ITrackingSpan span = _buffer.CurrentSnapshot.CreateTrackingSpan(lineTextStart, position - lineTextStart, SpanTrackingMode.EdgeInclusive);
                completionSets.Clear();

                var completions = new[] { "use strict", "use asm" }.Select(s => new Completion(
                    quote + s + quote + ";",
                    quote + s + quote + ";",
                    "Instructs that this block be processed in " + s.Substring(4) + " mode by supporting JS engines",
                    _useDirectiveGlyph,
                    null)
                );
                completionSets.Add(new CompletionSet("useDirectives", "Web Essentials", span, completions, null));
                return;
            }

            int tagIndex = text.IndexOf("getElementsByTagName(");
            int classIndex = text.IndexOf("getElementsByClassName(");
            int idIndex = text.IndexOf("getElementById(");

            CompletionSet set = null;

            if (tagIndex > -1 && position > line.Start + tagIndex)
                set = GetElementsByTagName(completionSets, position, line, text, tagIndex + 21);
            if (classIndex > -1 && position > line.Start + classIndex)
                set = GetElementsByClassName(completionSets, position, line, text, classIndex + 23);
            if (idIndex > -1 && position > line.Start + idIndex)
                set = GetElementById(completionSets, position, line, text, idIndex + 15);

            if (set != null)
            {
                completionSets.Clear();
                completionSets.Add(set);
                return;
            }
        }

        private CompletionSet GetElementsByTagName(IList<CompletionSet> completionSets, int position, ITextSnapshotLine line, string text, int index)
        {
            int end = text.IndexOf(')', index);

            if (position <= line.Start + end)
            {
                ITrackingSpan span = _buffer.CurrentSnapshot.CreateTrackingSpan(line.Start + index, end - index, SpanTrackingMode.EdgeInclusive);

                List<Completion> list = new List<Completion>();
                if (!span.GetText(_buffer.CurrentSnapshot).Contains("\""))
                    return null;

                AddExistingCompletions(completionSets, list);

                list.AddRange(_cache);
                var completions = list.OrderBy(x => x.DisplayText.TrimStart('\"'));

                return new CompletionSet("tagnames", "Web Essentials", span, completions, null);
            }

            return null;
        }

        private CompletionSet GetElementsByClassName(IList<CompletionSet> completionSets, int position, ITextSnapshotLine line, string text, int index)
        {
            int end = text.IndexOf(')', index);

            if (position <= line.Start + end)
            {
                ITrackingSpan span = _buffer.CurrentSnapshot.CreateTrackingSpan(line.Start + index, end - index, SpanTrackingMode.EdgeInclusive);

                List<Completion> list = new List<Completion>();
                if (!span.GetText(_buffer.CurrentSnapshot).Contains("\""))
                    return null;

                AddExistingCompletions(completionSets, list);

                var names = _classNames.GetNames(new System.Uri(EditorExtensionsPackage.DTE.ActiveDocument.FullName), line.Start.Add(index), CssNameType.Class);

                foreach (string name in names.Select(n => n.Name).Distinct())
                {
                    list.Add(GenerateCompletion(name));
                }

                var completions = list.OrderBy(x => x.DisplayText.TrimStart('\"'));

                return new CompletionSet("classnames", "Web Essentials", span, completions, null);
            }

            return null;
        }

        private CompletionSet GetElementById(IList<CompletionSet> completionSets, int position, ITextSnapshotLine line, string text, int index)
        {
            int end = text.IndexOf(')', index);

            if (position <= line.Start + end)
            {
                ITrackingSpan span = _buffer.CurrentSnapshot.CreateTrackingSpan(line.Start + index, end - index, SpanTrackingMode.EdgeInclusive);

                List<Completion> list = new List<Completion>();
                if (!span.GetText(_buffer.CurrentSnapshot).Contains("\""))
                    return null;

                AddExistingCompletions(completionSets, list);

                var names = _classNames.GetNames(new System.Uri(EditorExtensionsPackage.DTE.ActiveDocument.FullName), line.Start.Add(index), CssNameType.Id);

                foreach (string name in names.Select(n => n.Name).Distinct())
                {
                    list.Add(GenerateCompletion(name));
                }

                var completions = list.OrderBy(x => x.DisplayText.TrimStart('\"'));

                return new CompletionSet("ids", "Web Essentials", span, completions, null);
            }

            return null;
        }

        private static void AddExistingCompletions(IList<CompletionSet> completionSets, List<Completion> list)
        {
            if (completionSets.Count > 0)
            {
                for (int i = 0; i < completionSets[0].Completions.Count; i++)
                {
                    list.Add(completionSets[0].Completions[i]);
                }
            }
        }

        private static IEnumerable<Completion> AddHtmlTagNames()
        {
            List<string> list = new List<string>();
            foreach (var entry in TagCompletionProvider.GetListEntriesCache())
            {
                if (!list.Contains(entry.DisplayText))
                    list.Add(entry.DisplayText);
            }


            foreach (string name in list)
            {
                yield return GenerateCompletion(name);
            }
        }

        private static Completion GenerateCompletion(string name)
        {
            Completion c = new Completion();
            c.DisplayText = "\"" + name + "\"";
            c.IconSource = _glyph;
            c.InsertionText = "\"" + name + "\"";
            c.IconAutomationText = name;

            return c;
        }

        public void Dispose()
        {

        }
    }
}
